using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BioshockRemasteredTrainer
{
    internal sealed class MainForm : Form
    {
        private readonly ProcessMemory _memory = new ProcessMemory();
        private readonly Dictionary<string, CheatRuntime> _runtimes = new Dictionary<string, CheatRuntime>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, HotkeyBinding> _hotkeyMap = new Dictionary<int, HotkeyBinding>();
        private readonly Dictionary<string, IntPtr> _dataAddresses = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        private readonly List<HookState> _setupHooks = new List<HookState>();
        private readonly List<HookState> _transientHooks = new List<HookState>();
        private readonly TableLayoutPanel _cheatPanel = new TableLayoutPanel();
        private readonly TextBox _logBox = new TextBox();
        private readonly Label _statusLabel = new Label();
        private readonly Button _refreshButton = new Button();
        private readonly Button _disableAllButton = new Button();
        private readonly object _fileLogLock = new object();
        private readonly object _trainerLock = new object();
        private TrainerProfile _profile;
        private string _profilePath;
        private string _logFilePath;
        private int _attachedProcessId;
        private int _pollInProgress;
        private bool _isClosing;
        private bool _setupInstalled;
        private System.Threading.Timer _pollTimer;

        internal MainForm()
        {
            InitializeUi();
            Load += OnLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeUi()
        {
            Text = "BioShock Remastered Trainer";
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            Width = 900;
            Height = 660;
            MinimumSize = new Size(780, 560);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            TableLayoutPanel rootLayout = new TableLayoutPanel();
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.Padding = new Padding(8);
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            Controls.Add(rootLayout);

            Label title = new Label();
            title.Text = "Cheat Deck companion trainer";
            title.Dock = DockStyle.Fill;
            title.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = new Padding(6, 0, 0, 0);
            rootLayout.Controls.Add(title, 0, 0);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Fill;
            topBar.Padding = new Padding(6, 4, 6, 4);
            rootLayout.Controls.Add(topBar, 0, 1);

            _statusLabel.AutoSize = false;
            _statusLabel.Text = "Status: loading profile";
            _statusLabel.Width = 560;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Dock = DockStyle.Left;
            topBar.Controls.Add(_statusLabel);

            _disableAllButton.Text = "Disable All";
            _disableAllButton.Width = 96;
            _disableAllButton.Dock = DockStyle.Right;
            _disableAllButton.Click += delegate { QueueTrainerWork(delegate { DisableAllCheats(); }, null); };
            topBar.Controls.Add(_disableAllButton);

            _refreshButton.Text = "Refresh Attach";
            _refreshButton.Width = 104;
            _refreshButton.Dock = DockStyle.Right;
            _refreshButton.Click += delegate { QueueTrainerWork(delegate { RefreshAttachment(); }, "Refresh attach failed"); };
            topBar.Controls.Add(_refreshButton);

            _cheatPanel.Dock = DockStyle.Fill;
            _cheatPanel.ColumnCount = 2;
            _cheatPanel.RowCount = 4;
            _cheatPanel.Padding = new Padding(4);
            _cheatPanel.Margin = new Padding(0, 2, 0, 4);
            _cheatPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            _cheatPanel.AutoScroll = false;
            _cheatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _cheatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            rootLayout.Controls.Add(_cheatPanel, 0, 2);

            _logBox.Dock = DockStyle.Fill;
            _logBox.Multiline = true;
            _logBox.ReadOnly = true;
            _logBox.ScrollBars = ScrollBars.Vertical;
            _logBox.WordWrap = false;
            _logBox.Font = new Font("Consolas", 8);
            _logBox.Margin = new Padding(0, 2, 0, 0);
            rootLayout.Controls.Add(_logBox, 0, 3);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            InitializeFileLog();
            Log("Session started.");
            Log("File log: " + _logFilePath);
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles", "bioshock-remastered.steam-v1.0.122872.json");
            if (File.Exists(_profilePath))
            {
                _profile = TrainerProfile.Load(_profilePath);
                Log("Loaded external profile: " + _profilePath);
            }
            else
            {
                _profile = TrainerProfile.LoadFromJson(EmbeddedProfile.GetDefaultProfileJson());
                Log("External profile file not found. Loaded embedded BioShock profile.");
            }

            BuildCheatList();
            RegisterHotkeys();
            QueueTrainerWork(delegate { RefreshAttachment(); }, "Initial attach failed");
            _pollTimer = new System.Threading.Timer(OnPollTimerTick, null, _profile.PollIntervalMs, _profile.PollIntervalMs);
        }

        private void OnPollTimerTick(object state)
        {
            if (_isClosing)
            {
                return;
            }

            if (Interlocked.Exchange(ref _pollInProgress, 1) != 0)
            {
                return;
            }

            try
            {
                lock (_trainerLock)
                {
                    if (!_isClosing)
                    {
                        RefreshAttachment();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Background poll failed", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _pollInProgress, 0);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }

            lock (_trainerLock)
            {
                DisableAllCheats();
            }

            UnregisterHotkeys();
            _memory.Dispose();
        }

        private void BuildCheatList()
        {
            _cheatPanel.Controls.Clear();
            _runtimes.Clear();
            _hotkeyMap.Clear();
            _cheatPanel.RowStyles.Clear();

            int totalCheats = _profile.Cheats.Count;
            int rowCount = Math.Max(1, (int)Math.Ceiling(totalCheats / 2.0));
            _cheatPanel.RowCount = rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                _cheatPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
            }

            for (int index = 0; index < totalCheats; index++)
            {
                CheatDefinition cheat = _profile.Cheats[index];
                CheatRuntime runtime = new CheatRuntime(cheat);
                _runtimes[cheat.Id] = runtime;
                Panel card = CreateCheatCard(runtime);
                _cheatPanel.Controls.Add(card, index % 2, index / 2);
            }

            SetStatus("Status: profile loaded, waiting for process");
            Log("Loaded profile: " + _profile.ProfileName);
        }

        private void RefreshAttachment()
        {
            try
            {
                bool attached = _memory.Attach(_profile.ProcessName);
                int newProcessId = attached ? _memory.ProcessId : 0;
                bool processChanged = newProcessId != _attachedProcessId;
                if (processChanged)
                {
                    if (_attachedProcessId != 0)
                    {
                        Log("Process changed or detached. Resetting trainer state.");
                    }

                    ResetProcessScopedState();
                    _attachedProcessId = newProcessId;
                }

                if (attached)
                {
                    SetStatus("Status: attached to " + _memory.Process.ProcessName + " (PID " + _memory.Process.Id + ")");
                    if (processChanged && newProcessId != 0)
                    {
                        Log("Attached to " + _memory.Process.ProcessName + " (PID " + _memory.Process.Id + ")" + FormatProcessPath());
                    }
                }
                else
                {
                    SetStatus("Status: waiting for " + _profile.ProcessName);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Status: attach failed");
                LogError("Attach failed", ex);
            }
        }

        private void ResetProcessScopedState()
        {
            foreach (CheatRuntime runtime in _runtimes.Values)
            {
                runtime.IsEnabled = false;
                runtime.Hook = null;
                UpdateRuntimeUi(runtime);
            }

            _setupInstalled = false;
            _setupHooks.Clear();
            _transientHooks.Clear();
            _dataAddresses.Clear();
        }

        private void ToggleCheat(CheatRuntime runtime, bool enable)
        {
            if (enable)
            {
                try
                {
                    EnableCheat(runtime);
                    UpdateRuntimeUi(runtime);
                }
                catch (Exception ex)
                {
                    runtime.IsEnabled = false;
                    UpdateRuntimeUi(runtime);
                    LogError(runtime.Definition.Name + " failed", ex);
                }
            }
            else
            {
                try
                {
                    DisableCheat(runtime);
                    UpdateRuntimeUi(runtime);
                }
                catch (Exception ex)
                {
                    LogError(runtime.Definition.Name + " restore failed", ex);
                }
            }
        }

        private void EnableCheat(CheatRuntime runtime)
        {
            EnsureAttached();
            string id = runtime.Definition.Id;
            Log("Enabling " + runtime.Definition.Name + ".");

            if (id == "god-mode")
            {
                EnsureSetupHooks();
                WriteDataByte("bGodmode", 1);
            }
            else if (id == "invisible")
            {
                EnsureSetupHooks();
                WriteDataByte("bInvisible", 0);
            }
            else if (id == "lock-consumables")
            {
                InstallLockConsumables(runtime);
            }
            else if (id == "one-hit-kill")
            {
                EnsureSetupHooks();
                InstallOneHitKill(runtime);
            }
            else if (id == "no-alerts")
            {
                InstallNoAlerts(runtime);
                WriteDataByte("bAlert", 1);
            }
            else if (id == "protect-little-sister")
            {
                InstallSingleHook(runtime, InstallProtectLittleSisterHook());
            }
            else if (id == "unlock-gene-slots")
            {
                InstallSingleHook(runtime, InstallUnlockGeneSlotsHook());
            }
            else
            {
                throw new InvalidOperationException("Unsupported BioShock cheat id: " + id);
            }

            runtime.IsEnabled = true;
            Log("Enabled: " + runtime.Definition.Name);
        }

        private void DisableCheat(CheatRuntime runtime)
        {
            string id = runtime.Definition.Id;
            if (id == "god-mode")
            {
                if (_setupInstalled)
                {
                    WriteDataByte("bGodmode", 0);
                }
            }
            else if (id == "invisible")
            {
                if (_setupInstalled)
                {
                    WriteDataByte("bInvisible", 1);
                }
            }
            else if (id == "no-alerts")
            {
                if (_dataAddresses.ContainsKey("bAlert"))
                {
                    WriteDataByte("bAlert", 0);
                }

                UninstallRuntimeHooks(runtime);
            }
            else
            {
                UninstallRuntimeHooks(runtime);
            }

            runtime.IsEnabled = false;
            Log("Disabled: " + runtime.Definition.Name);
        }

        private void InstallSingleHook(CheatRuntime runtime, HookState hook)
        {
            runtime.Hooks.Add(hook);
            _transientHooks.Add(hook);
        }

        private void InstallLockConsumables(CheatRuntime runtime)
        {
            if (runtime.Hooks.Count > 0)
            {
                return;
            }

            EnsureData("pAmmo", 4);
            InstallSingleHook(runtime, InstallHook("LockConsume1", "29 06 8B 0E 8B 45 0C", 0, 7, new byte[] { 0x8B, 0x0E, 0x8B, 0x45, 0x0C }));
            List<byte> holster = new List<byte>();
            holster.Add(0x89);
            holster.Add(0x15);
            AddInt32(holster, Address32(EnsureData("pAmmo", 4)));
            holster.AddRange(new byte[] { 0x89, 0x4A, 0x54 });
            InstallSingleHook(runtime, InstallHook("LockConsume2", "2B C8 89 4A 54", 0, 5, holster.ToArray()));
            InstallSingleHook(runtime, InstallHook("LockConsume3", "29 9E F4 0A 00 00", 0, 6, new byte[0]));
            InstallSingleHook(runtime, InstallHook("LockConsume4", "29 86 EC 0A 00 00", 0, 6, new byte[0]));
            InstallSingleHook(runtime, InstallHook("LockConsume5", "D8 65 08 D9 5C 24 0C F3", 0, 7, new byte[] { 0xD9, 0x5C, 0x24, 0x0C }));
        }

        private void InstallOneHitKill(CheatRuntime runtime)
        {
            if (runtime.Hooks.Count > 0)
            {
                return;
            }

            IntPtr fHealthMax = EnsureData("fHealthMax", 4);
            _memory.WriteBytes(fHealthMax, BitConverter.GetBytes(1.0f));

            X86CodeBuilder code = new X86CodeBuilder();
            code.Add(0x3B, 0x0D);
            code.AddInt32(Address32(EnsureData("pPawn", 4)));
            code.JccNear(0x84, "original");
            code.Add(0x8B, 0x81);
            code.AddInt32(0x434);
            code.Add(0x3D);
            code.AddInt32(0x9EE);
            code.JccNear(0x84, "original");
            code.Add(0xA1);
            code.AddInt32(Address32(fHealthMax));
            code.Add(0x83, 0xF8, 0x00);
            code.JccNear(0x86, "original");
            code.Add(0x39, 0x81);
            code.AddInt32(0x57C);
            code.JccNear(0x86, "original");
            code.Add(0x89, 0x81);
            code.AddInt32(0x57C);
            code.Label("original");
            code.Add(0xF3, 0x0F, 0x10, 0x81);
            code.AddInt32(0x57C);

            InstallSingleHook(runtime, InstallHook("1HitKill", "F3 0F 10 81 7C 05 00 00", 0, 8, code.ToArray()));
        }

        private void InstallNoAlerts(CheatRuntime runtime)
        {
            if (runtime.Hooks.Count > 0)
            {
                return;
            }

            EnsureData("pSecurityMgr", 4);
            EnsureData("bAlert", 2);
            X86CodeBuilder code = new X86CodeBuilder();
            code.Add(0x89, 0x35);
            code.AddInt32(Address32(EnsureData("pSecurityMgr", 4)));
            code.Add(0x80, 0x3D);
            code.AddInt32(Address32(EnsureData("bAlert", 2)));
            code.Add(0x01);
            code.JccNear(0x85, "original");
            code.Add(0xF3, 0x0F, 0x10, 0x7E, 0x60);
            code.Add(0xF3, 0x0F, 0x5C, 0xBE);
            code.AddInt32(0xB8);
            code.Add(0xF3, 0x0F, 0x11, 0x7E, 0x60);
            code.Label("original");
            code.Add(0xD9, 0x46, 0x64, 0xD9, 0xC9);

            InstallSingleHook(runtime, InstallHook("AlertCam", "D9 46 64 D9 C9", 0, 5, code.ToArray()));
        }

        private HookState InstallProtectLittleSisterHook()
        {
            X86CodeBuilder code = new X86CodeBuilder();
            code.Add(0x81, 0xBF);
            code.AddInt32(0x434);
            code.AddInt32(0x9EE);
            code.JccNear(0x85, "original");
            code.Add(0xC6, 0x87);
            code.AddInt32(0x74C);
            code.Add(0x01);
            code.Label("original");
            code.Add(0xD9, 0x87);
            code.AddInt32(0x57C);
            return InstallHook("GodSister", "D9 87 7C 05 00 00", 0, 6, code.ToArray());
        }

        private HookState InstallUnlockGeneSlotsHook()
        {
            X86CodeBuilder code = new X86CodeBuilder();
            code.Add(0x8B, 0x01);
            code.Add(0xC7, 0x40, 0x40);
            code.AddInt32(6);
            code.Add(0x3B, 0x70, 0x40);
            return InstallHook("GeneSlot", "8B 01 3B 70 40", 0, 5, code.ToArray());
        }

        private void EnsureSetupHooks()
        {
            if (_setupInstalled)
            {
                return;
            }

            IntPtr pController = EnsureData("pController", 4);
            IntPtr pPawn = EnsureData("pPawn", 4);
            IntPtr bGodmode = EnsureData("bGodmode", 1);
            IntPtr bInvisible = EnsureData("bInvisible", 1);
            WriteDataByte("bGodmode", 0);
            WriteDataByte("bInvisible", 1);

            X86CodeBuilder controller = new X86CodeBuilder();
            controller.Add(0x8B, 0x7F, 0x1C);
            controller.Add(0x89, 0x4C, 0x24, 0x30);
            controller.Add(0x53);
            controller.Add(0x85, 0xFF);
            controller.JccNear(0x84, "done");
            controller.Add(0x89, 0x3D);
            controller.AddInt32(Address32(pController));
            controller.Add(0x8B, 0xDF);
            controller.Add(0x81, 0xC3);
            controller.AddInt32(0x450);
            controller.Add(0x8B, 0x1B);
            controller.Add(0x89, 0x1D);
            controller.AddInt32(Address32(pPawn));
            controller.Add(0x80, 0x3D);
            controller.AddInt32(Address32(bGodmode));
            controller.Add(0x01);
            controller.JccNear(0x85, "godOff");
            controller.Add(0x80, 0x8F);
            controller.AddInt32(0x468);
            controller.Add(0x02);
            controller.JmpNear("done");
            controller.Label("godOff");
            controller.Add(0x80, 0xA7);
            controller.AddInt32(0x468);
            controller.Add(0xFD);
            controller.Label("done");
            controller.Add(0x5B);
            _setupHooks.Add(InstallHook("ColStruct", "8B 7F 1C 89 4C 24 30", 0, 7, controller.ToArray()));

            List<byte> invisible = new List<byte>();
            invisible.Add(0x50);
            invisible.Add(0xA0);
            AddInt32(invisible, Address32(bInvisible));
            invisible.AddRange(new byte[] { 0x88, 0x81, 0x38, 0x0C, 0x00, 0x00, 0x58, 0xF6, 0x81, 0x00, 0x07, 0x00, 0x00, 0x01 });
            _setupHooks.Add(InstallHook("InvisibleBase", "F6 81 00 07 00 00 01", 0, 7, invisible.ToArray()));

            _setupInstalled = true;
            Log("Installed BioShock base collector hooks.");
        }

        private HookState InstallHook(string name, string pattern, int patchOffset, int overwriteSize, byte[] caveCode)
        {
            IntPtr hookAddress = IntPtr.Add(FindPatternAddress(pattern), patchOffset);
            byte[] originalBytes = _memory.ReadBytes(hookAddress, overwriteSize);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, Math.Max(128, caveCode.Length + 32));
            List<byte> cave = new List<byte>();
            if (caveCode != null && caveCode.Length > 0)
            {
                cave.AddRange(caveCode);
            }

            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, overwriteSize)));
            _memory.WriteBytes(caveAddress, cave.ToArray());
            _memory.WriteBytes(hookAddress, BuildJumpPatch(hookAddress, caveAddress, overwriteSize));

            HookState hook = new HookState();
            hook.Name = name;
            hook.HookAddress = hookAddress;
            hook.CaveAddress = caveAddress;
            hook.OriginalBytes = originalBytes;
            hook.OverwriteSize = overwriteSize;
            Log("Installed " + name + " hook at 0x" + hookAddress.ToInt64().ToString("X") + ".");
            return hook;
        }

        private IntPtr FindPatternAddress(string pattern)
        {
            int moduleSize;
            string resolvedModuleName;
            IntPtr moduleBase = _memory.GetModuleBase(_profile.ModuleName, out moduleSize, out resolvedModuleName);
            if (moduleBase == IntPtr.Zero || moduleSize <= 0)
            {
                throw new InvalidOperationException("Module not found: " + _profile.ModuleName);
            }

            byte[] moduleBytes = _memory.ReadModule(moduleBase, moduleSize);
            int found = PatternScanner.Find(moduleBytes, pattern);
            if (found < 0)
            {
                throw new InvalidOperationException("Pattern not found: " + pattern);
            }

            return IntPtr.Add(moduleBase, found);
        }

        private void UninstallRuntimeHooks(CheatRuntime runtime)
        {
            foreach (HookState hook in runtime.Hooks.ToList())
            {
                UninstallHook(hook);
                _transientHooks.Remove(hook);
            }

            runtime.Hooks.Clear();
        }

        private void UninstallHook(HookState hook)
        {
            if (hook == null || hook.HookAddress == IntPtr.Zero)
            {
                return;
            }

            EnsureAttached();
            _memory.WriteBytes(hook.HookAddress, hook.OriginalBytes);
            _memory.Free(hook.CaveAddress);
            Log("Restored " + hook.Name + " hook.");
            hook.HookAddress = IntPtr.Zero;
            hook.CaveAddress = IntPtr.Zero;
        }

        private void DisableAllCheats()
        {
            foreach (CheatRuntime runtime in _runtimes.Values.ToList())
            {
                try
                {
                    DisableCheat(runtime);
                }
                catch (Exception ex)
                {
                    LogError(runtime.Definition.Name + " disable-all error", ex);
                }

                UpdateRuntimeUi(runtime);
            }

            foreach (HookState hook in _setupHooks.ToList())
            {
                try
                {
                    UninstallHook(hook);
                }
                catch (Exception ex)
                {
                    LogError(hook.Name + " setup restore error", ex);
                }
            }

            _setupHooks.Clear();
            _transientHooks.Clear();
            _setupInstalled = false;
        }

        private IntPtr EnsureData(string key, int size)
        {
            IntPtr address;
            if (_dataAddresses.TryGetValue(key, out address) && address != IntPtr.Zero)
            {
                return address;
            }

            address = _memory.Allocate(size);
            _memory.WriteBytes(address, new byte[size]);
            _dataAddresses[key] = address;
            Log("Allocated " + key + " at 0x" + address.ToInt64().ToString("X") + ".");
            return address;
        }

        private void WriteDataByte(string key, byte value)
        {
            IntPtr address = EnsureData(key, 1);
            _memory.WriteBytes(address, new[] { value });
        }

        private static byte[] BuildJumpPatch(IntPtr source, IntPtr destination, int overwriteSize)
        {
            if (overwriteSize < 5)
            {
                throw new InvalidOperationException("x86 jump patch needs at least 5 bytes.");
            }

            byte[] patch = new byte[overwriteSize];
            patch[0] = 0xE9;
            int relative = checked((int)(destination.ToInt64() - source.ToInt64() - 5));
            BitConverter.GetBytes(relative).CopyTo(patch, 1);
            for (int i = 5; i < patch.Length; i++)
            {
                patch[i] = 0x90;
            }

            return patch;
        }

        private static byte[] BuildRelativeJump(IntPtr source, IntPtr destination)
        {
            byte[] jump = new byte[5];
            jump[0] = 0xE9;
            int relative = checked((int)(destination.ToInt64() - source.ToInt64() - 5));
            BitConverter.GetBytes(relative).CopyTo(jump, 1);
            return jump;
        }

        private static int Address32(IntPtr address)
        {
            long value = address.ToInt64();
            if (value < 0 || value > uint.MaxValue)
            {
                throw new InvalidOperationException("Address is outside x86 range: 0x" + value.ToString("X") + ".");
            }

            return unchecked((int)(uint)value);
        }

        private static void AddInt32(List<byte> bytes, int value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private void EnsureAttached()
        {
            if (!_memory.IsAttached)
            {
                RefreshAttachment();
            }

            if (!_memory.IsAttached)
            {
                throw new InvalidOperationException("Game process is not attached.");
            }
        }

        private void RegisterHotkeys()
        {
            int id = 100;
            foreach (CheatDefinition cheat in _profile.Cheats)
            {
                id = RegisterHotkeyBinding(id, cheat, cheat.Hotkey);

                Keys secondaryKey;
                if (TryGetDigitHotkey(cheat, out secondaryKey))
                {
                    id = RegisterHotkeyBinding(id, cheat, secondaryKey.ToString());
                }
            }

            if (!NativeMethods.RegisterHotKey(Handle, 999, 0, (uint)Keys.F12))
            {
                Log("Hotkey registration failed for F12 panic key: " + GetLastWin32ErrorMessage() + ".");
            }

            if (!NativeMethods.RegisterHotKey(Handle, 998, 0, (uint)Keys.D8))
            {
                Log("Hotkey registration failed for 8 disable-all key: " + GetLastWin32ErrorMessage() + ".");
            }
        }

        private void UnregisterHotkeys()
        {
            foreach (int id in _hotkeyMap.Keys.ToList())
            {
                NativeMethods.UnregisterHotKey(Handle, id);
            }

            NativeMethods.UnregisterHotKey(Handle, 999);
            NativeMethods.UnregisterHotKey(Handle, 998);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WmHotKey)
            {
                int id = m.WParam.ToInt32();
                if (id == 999 || id == 998)
                {
                    QueueTrainerWork(delegate { DisableAllCheats(); }, "Disable-all hotkey failed");
                    Log("Disable-all hotkey pressed.");
                    return;
                }

                HotkeyBinding binding;
                if (_hotkeyMap.TryGetValue(id, out binding))
                {
                    CheatRuntime runtime;
                    if (_runtimes.TryGetValue(binding.Cheat.Id, out runtime))
                    {
                        Log("Hotkey pressed: " + binding.DisplayKey + " -> " + binding.Cheat.Name + ".");
                        bool nextState = !runtime.IsEnabled;
                        QueueTrainerWork(delegate { ToggleCheat(runtime, nextState); }, binding.Cheat.Name + " hotkey failed");
                    }
                }
            }

            base.WndProc(ref m);
        }

        private Panel CreateCheatCard(CheatRuntime runtime)
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(4);
            card.Padding = new Padding(8, 6, 8, 6);
            card.BorderStyle = BorderStyle.FixedSingle;

            Label description = new Label();
            description.Dock = DockStyle.Fill;
            description.Text = string.IsNullOrWhiteSpace(runtime.Definition.Description) ? "No description." : runtime.Definition.Description;
            description.Font = new Font(Font.FontFamily, 8, FontStyle.Regular);
            description.AutoEllipsis = true;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 28;
            actions.WrapContents = false;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.Margin = new Padding(0);

            Label title = new Label();
            title.AutoSize = false;
            title.Width = 205;
            title.Height = 24;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Text = runtime.Definition.Name + " (" + GetDisplayHotkeys(runtime.Definition) + ")";
            title.Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);

            Button enableButton = new Button();
            enableButton.Width = 72;
            enableButton.Height = 26;
            enableButton.Text = "Enable";
            enableButton.MouseDown += delegate
            {
                QueueTrainerWork(delegate { RequestCheatState(runtime, true, "button-mousedown"); }, runtime.Definition.Name + " enable failed");
            };

            Button disableButton = new Button();
            disableButton.Width = 72;
            disableButton.Height = 26;
            disableButton.Text = "Disable";
            disableButton.MouseDown += delegate
            {
                QueueTrainerWork(delegate { RequestCheatState(runtime, false, "button-mousedown"); }, runtime.Definition.Name + " disable failed");
            };

            Label stateLabel = new Label();
            stateLabel.AutoSize = false;
            stateLabel.Width = 70;
            stateLabel.Height = 24;
            stateLabel.TextAlign = ContentAlignment.MiddleCenter;
            stateLabel.Font = new Font(Font.FontFamily, 8, FontStyle.Bold);

            runtime.EnableButton = enableButton;
            runtime.DisableButton = disableButton;
            runtime.StateLabel = stateLabel;

            actions.Controls.Add(title);
            actions.Controls.Add(enableButton);
            actions.Controls.Add(disableButton);
            actions.Controls.Add(stateLabel);
            card.Controls.Add(description);
            card.Controls.Add(actions);
            UpdateRuntimeUi(runtime);
            return card;
        }

        private void RequestCheatState(CheatRuntime runtime, bool enable, string source)
        {
            if (runtime == null)
            {
                return;
            }

            Log("Requested: " + runtime.Definition.Name + " -> " + (enable ? "enable" : "disable") + " via " + source + ".");
            ToggleCheat(runtime, enable);
            Log("Current state: " + runtime.Definition.Name + " -> " + (runtime.IsEnabled ? "enabled" : "disabled") + ".");
        }

        private int RegisterHotkeyBinding(int id, CheatDefinition cheat, string hotkeyText)
        {
            if (cheat == null || string.IsNullOrWhiteSpace(hotkeyText))
            {
                return id;
            }

            Keys parsedKey;
            if (!Enum.TryParse(hotkeyText, true, out parsedKey))
            {
                return id;
            }

            if (NativeMethods.RegisterHotKey(Handle, id, 0, (uint)parsedKey))
            {
                HotkeyBinding binding = new HotkeyBinding();
                binding.Cheat = cheat;
                binding.DisplayKey = NormalizeHotkeyLabel(parsedKey, hotkeyText);
                _hotkeyMap[id] = binding;
                return id + 1;
            }

            Log("Hotkey registration failed for " + cheat.Name + " (" + NormalizeHotkeyLabel(parsedKey, hotkeyText) + "): " + GetLastWin32ErrorMessage() + ".");
            return id;
        }

        private static bool TryGetDigitHotkey(CheatDefinition cheat, out Keys key)
        {
            key = Keys.None;
            if (cheat == null)
            {
                return false;
            }

            switch (cheat.Id)
            {
                case "god-mode":
                    key = Keys.D1;
                    return true;
                case "invisible":
                    key = Keys.D2;
                    return true;
                case "lock-consumables":
                    key = Keys.D3;
                    return true;
                case "one-hit-kill":
                    key = Keys.D4;
                    return true;
                case "no-alerts":
                    key = Keys.D5;
                    return true;
                case "protect-little-sister":
                    key = Keys.D6;
                    return true;
                case "unlock-gene-slots":
                    key = Keys.D7;
                    return true;
                default:
                    return false;
            }
        }

        private static string GetDisplayHotkeys(CheatDefinition cheat)
        {
            if (cheat == null)
            {
                return string.Empty;
            }

            Keys key;
            if (TryGetDigitHotkey(cheat, out key))
            {
                return cheat.Hotkey + " / " + NormalizeHotkeyLabel(key, key.ToString());
            }

            return cheat.Hotkey;
        }

        private static string NormalizeHotkeyLabel(Keys key, string fallback)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((int)(key - Keys.D0)).ToString();
            }

            return fallback;
        }

        private void UpdateRuntimeUi(CheatRuntime runtime)
        {
            if (runtime == null)
            {
                return;
            }

            RunOnUiThread(delegate
            {
                if (runtime.EnableButton != null)
                {
                    runtime.EnableButton.Enabled = !runtime.IsEnabled;
                }

                if (runtime.DisableButton != null)
                {
                    runtime.DisableButton.Enabled = runtime.IsEnabled;
                }

                if (runtime.StateLabel != null)
                {
                    runtime.StateLabel.Text = runtime.IsEnabled ? "Enabled" : "Disabled";
                    runtime.StateLabel.ForeColor = runtime.IsEnabled ? Color.DarkGreen : Color.DarkRed;
                }
            });
        }

        private void SetStatus(string text)
        {
            RunOnUiThread(delegate { _statusLabel.Text = text; });
        }

        private void RunOnUiThread(Action action)
        {
            if (action == null || _isClosing || IsDisposed)
            {
                return;
            }

            if (!IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate { action(); });
                }
                catch
                {
                }

                return;
            }

            action();
        }

        private void QueueTrainerWork(Action action, string errorContext)
        {
            if (action == null || _isClosing)
            {
                return;
            }

            Task.Run(delegate
            {
                lock (_trainerLock)
                {
                    if (_isClosing)
                    {
                        return;
                    }

                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        if (!string.IsNullOrWhiteSpace(errorContext))
                        {
                            LogError(errorContext, ex);
                        }
                    }
                }
            });
        }

        private void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            RunOnUiThread(delegate
            {
                _logBox.AppendText(line + Environment.NewLine);
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.ScrollToCaret();
                _logBox.Refresh();
            });
            AppendLineToFileLog(line);
        }

        private void LogError(string context, Exception ex)
        {
            Log(context + ": " + ex.Message);
            if (string.IsNullOrWhiteSpace(_logFilePath))
            {
                return;
            }

            StringBuilder details = new StringBuilder();
            details.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ERROR " + context);
            details.AppendLine("Type: " + ex.GetType().FullName);
            details.AppendLine("Message: " + ex.Message);
            details.AppendLine("Stack:");
            details.AppendLine(ex.StackTrace ?? "(no stack trace)");
            details.AppendLine();
            AppendRawToFileLog(details.ToString());
        }

        private void InitializeFileLog()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BioshockRemasteredTrainer.log");
            StringBuilder header = new StringBuilder();
            header.AppendLine("============================================================");
            header.AppendLine("Session started at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            header.AppendLine("Log file: " + _logFilePath);
            header.AppendLine("============================================================");
            AppendRawToFileLog(header.ToString());
        }

        private void AppendLineToFileLog(string line)
        {
            AppendRawToFileLog(line + Environment.NewLine);
        }

        private void AppendRawToFileLog(string content)
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
            {
                return;
            }

            try
            {
                lock (_fileLogLock)
                {
                    File.AppendAllText(_logFilePath, content, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private string FormatProcessPath()
        {
            string processPath = _memory.GetProcessPath();
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return string.Empty;
            }

            return " at " + processPath;
        }

        private static string GetLastWin32ErrorMessage()
        {
            return new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()).Message;
        }

        private sealed class CheatRuntime
        {
            internal CheatRuntime(CheatDefinition definition)
            {
                Definition = definition;
                Hooks = new List<HookState>();
            }

            internal CheatDefinition Definition;
            internal bool IsEnabled;
            internal HookState Hook;
            internal List<HookState> Hooks;
            internal Button EnableButton;
            internal Button DisableButton;
            internal Label StateLabel;
        }

        private sealed class HotkeyBinding
        {
            internal CheatDefinition Cheat;
            internal string DisplayKey;
        }

        private sealed class HookState
        {
            internal string Name;
            internal IntPtr HookAddress;
            internal IntPtr CaveAddress;
            internal byte[] OriginalBytes;
            internal int OverwriteSize;
        }

        private sealed class X86CodeBuilder
        {
            private readonly List<byte> _bytes = new List<byte>();
            private readonly Dictionary<string, int> _labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            private readonly List<Fixup> _fixups = new List<Fixup>();

            internal void Add(params byte[] bytes)
            {
                _bytes.AddRange(bytes);
            }

            internal void AddInt32(int value)
            {
                _bytes.AddRange(BitConverter.GetBytes(value));
            }

            internal void Label(string name)
            {
                _labels[name] = _bytes.Count;
            }

            internal void JmpNear(string label)
            {
                _bytes.Add(0xE9);
                AddFixup(label);
            }

            internal void JccNear(byte condition, string label)
            {
                _bytes.Add(0x0F);
                _bytes.Add(condition);
                AddFixup(label);
            }

            internal byte[] ToArray()
            {
                byte[] result = _bytes.ToArray();
                foreach (Fixup fixup in _fixups)
                {
                    int target;
                    if (!_labels.TryGetValue(fixup.Label, out target))
                    {
                        throw new InvalidOperationException("Missing x86 label: " + fixup.Label);
                    }

                    int relative = target - (fixup.Offset + 4);
                    BitConverter.GetBytes(relative).CopyTo(result, fixup.Offset);
                }

                return result;
            }

            private void AddFixup(string label)
            {
                Fixup fixup = new Fixup();
                fixup.Label = label;
                fixup.Offset = _bytes.Count;
                _fixups.Add(fixup);
                AddInt32(0);
            }

            private sealed class Fixup
            {
                internal string Label;
                internal int Offset;
            }
        }
    }
}
