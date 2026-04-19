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

namespace Crysis3RemasteredTrainer
{
    internal sealed class MainForm : Form
    {
        private readonly ProcessMemory _memory = new ProcessMemory();
        private readonly Dictionary<string, CheatRuntime> _runtimes = new Dictionary<string, CheatRuntime>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, HotkeyBinding> _hotkeyMap = new Dictionary<int, HotkeyBinding>();
        private readonly TableLayoutPanel _cheatPanel = new TableLayoutPanel();
        private readonly TextBox _logBox = new TextBox();
        private readonly Label _statusLabel = new Label();
        private readonly Button _refreshButton = new Button();
        private readonly Button _disableAllButton = new Button();
        private readonly object _fileLogLock = new object();
        private readonly object _trainerLock = new object();
        private TableLayoutPanel _rootLayout;
        private TrainerProfile _profile;
        private string _profilePath;
        private string _logFilePath;
        private int _attachedProcessId;
        private int _pollInProgress;
        private bool _isClosing;
        private HookState _healthCollectorHook;
        private Crysis3CoreState _crysis3Core;
        private Crysis3AmmoState _crysis3Ammo;
        private System.Threading.Timer _pollTimer;

        internal MainForm()
        {
            InitializeUi();
            Load += OnLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeUi()
        {
            Text = "Crysis 3 Remastered Trainer";
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            Width = 860;
            Height = 640;
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            _rootLayout = new TableLayoutPanel();
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.ColumnCount = 1;
            _rootLayout.RowCount = 4;
            _rootLayout.Padding = new Padding(8);
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            Controls.Add(_rootLayout);

            Label title = new Label();
            title.Text = "Cheat Deck companion trainer";
            title.Dock = DockStyle.Fill;
            title.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = new Padding(6, 0, 0, 0);
            _rootLayout.Controls.Add(title, 0, 0);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Fill;
            topBar.Padding = new Padding(6, 4, 6, 4);
            _rootLayout.Controls.Add(topBar, 0, 1);

            _statusLabel.AutoSize = false;
            _statusLabel.Text = "Status: loading profile";
            _statusLabel.Width = 520;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Dock = DockStyle.Left;
            topBar.Controls.Add(_statusLabel);

            _disableAllButton.Text = "Disable All";
            _disableAllButton.Width = 96;
            _disableAllButton.Dock = DockStyle.Right;
            _disableAllButton.Click += delegate { QueueTrainerWork(delegate { DisableAllCheats(); }, null); };
            topBar.Controls.Add(_disableAllButton);

            _refreshButton.Text = "Refresh Attach";
            _refreshButton.Width = 96;
            _refreshButton.Dock = DockStyle.Right;
            _refreshButton.Click += delegate { QueueTrainerWork(delegate { RefreshAttachment(); }, "Refresh attach failed"); };
            topBar.Controls.Add(_refreshButton);

            _cheatPanel.Dock = DockStyle.Fill;
            _cheatPanel.ColumnCount = 2;
            _cheatPanel.RowCount = 3;
            _cheatPanel.Padding = new Padding(4);
            _cheatPanel.Margin = new Padding(0, 2, 0, 4);
            _cheatPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            _cheatPanel.AutoScroll = false;
            _cheatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _cheatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _rootLayout.Controls.Add(_cheatPanel, 0, 2);

            _logBox.Dock = DockStyle.Fill;
            _logBox.Multiline = true;
            _logBox.ReadOnly = true;
            _logBox.ScrollBars = ScrollBars.Vertical;
            _logBox.WordWrap = false;
            _logBox.Font = new Font("Consolas", 8);
            _logBox.Margin = new Padding(0, 2, 0, 0);
            _rootLayout.Controls.Add(_logBox, 0, 3);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            InitializeFileLog();
            Log("Session started.");
            Log("File log: " + _logFilePath);
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles", "crysis3-remastered.fr-v1.4.json");
            if (File.Exists(_profilePath))
            {
                _profile = TrainerProfile.Load(_profilePath);
                Log("Loaded external profile: " + _profilePath);
            }
            else
            {
                _profile = TrainerProfile.LoadFromJson(EmbeddedProfile.GetDefaultProfileJson());
                Log("External profile file not found. Loaded embedded FR v1.4 profile.");
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
                    if (_isClosing)
                    {
                        return;
                    }

                    RefreshAttachment();
                    MaintainDynamicCheats();
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
                UninstallCrysis3Hooks();
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
                int row = index / 2;
                int column = index % 2;
                _cheatPanel.Controls.Add(card, column, row);
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
                    if (_attachedProcessId != 0 && newProcessId == 0)
                    {
                        Log("Detached from PID " + _attachedProcessId + ".");
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
                        EnableAllCheatsOnAttach();
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
            _healthCollectorHook = null;
            _crysis3Core = null;
            _crysis3Ammo = null;
            foreach (CheatRuntime runtime in _runtimes.Values)
            {
                runtime.IsEnabled = false;
                runtime.OriginalBytes = null;
                runtime.PatchedAddress = IntPtr.Zero;
                runtime.Hook = null;
                UpdateRuntimeUi(runtime);
            }
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
            string actionType = GetActionType(runtime.Definition);
            Log("Enabling " + runtime.Definition.Name + " using action type " + actionType + ".");

            if (IsCrysis3FlagAction(actionType))
            {
                SetCrysis3Flag(runtime, true);
                runtime.IsEnabled = true;
                Log("Enabled: " + runtime.Definition.Name);
                return;
            }

            if (actionType == "godmode")
            {
                InstallHealthCollectorHook();
                runtime.IsEnabled = true;
                Log("Enabled: " + runtime.Definition.Name);
                return;
            }

            if (actionType == "onehitkill")
            {
                if (runtime.Hook == null)
                {
                    runtime.Hook = InstallOneHitKillHook();
                }

                runtime.IsEnabled = true;
                Log("Enabled: " + runtime.Definition.Name);
                return;
            }

            IntPtr address = ResolveAddress(runtime);
            Log(runtime.Definition.Name + " resolved target address 0x" + address.ToInt64().ToString("X") + ".");
            byte[] expectedBytes = ByteHelper.ParseBytes(runtime.Definition.ExpectedBytes);
            if (expectedBytes.Length > 0)
            {
                byte[] currentBytes = _memory.ReadBytes(address, expectedBytes.Length);
                if (!currentBytes.SequenceEqual(expectedBytes))
                {
                    throw new InvalidOperationException(
                        "Expected bytes do not match. Expected [" + FormatBytes(expectedBytes) + "] but found [" + FormatBytes(currentBytes) + "].");
                }
            }

            if (actionType == "setbytes")
            {
                byte[] enableBytes = ByteHelper.ParseBytes(runtime.Definition.EnableBytes);
                if (enableBytes.Length == 0)
                {
                    throw new InvalidOperationException("Enable bytes are empty.");
                }

                if (runtime.OriginalBytes == null)
                {
                    runtime.OriginalBytes = _memory.ReadBytes(address, enableBytes.Length);
                }

                _memory.WriteBytes(address, enableBytes);
            }
            else
            {
                byte[] patchBytes = ByteHelper.ParseBytes(runtime.Definition.PatchBytes);
                if (patchBytes.Length == 0)
                {
                    throw new InvalidOperationException("Patch bytes are empty.");
                }

                if (runtime.OriginalBytes == null)
                {
                    runtime.OriginalBytes = _memory.ReadBytes(address, patchBytes.Length);
                }

                _memory.WriteBytes(address, patchBytes);
            }

            runtime.PatchedAddress = address;
            runtime.IsEnabled = true;
            Log("Enabled: " + runtime.Definition.Name + " at 0x" + address.ToInt64().ToString("X"));
        }

        private void DisableCheat(CheatRuntime runtime)
        {
            string actionType = GetActionType(runtime.Definition);
            if (IsCrysis3FlagAction(actionType))
            {
                SetCrysis3Flag(runtime, false);
                runtime.IsEnabled = false;
                MaybeUninstallCrysis3Hooks();
                Log("Disabled: " + runtime.Definition.Name);
                return;
            }

            if (actionType == "godmode")
            {
                runtime.IsEnabled = false;
                if (!IsAnyRuntimeEnabled("godmode"))
                {
                    UninstallHook(ref _healthCollectorHook);
                }
                Log("Disabled: " + runtime.Definition.Name);
                return;
            }

            if (actionType == "onehitkill")
            {
                if (runtime.Hook != null)
                {
                    UninstallHook(ref runtime.Hook);
                }

                runtime.IsEnabled = false;
                Log("Disabled: " + runtime.Definition.Name);
                return;
            }

            if (!runtime.IsEnabled || runtime.PatchedAddress == IntPtr.Zero)
            {
                runtime.IsEnabled = false;
                return;
            }

            EnsureAttached();
            if (actionType == "setbytes")
            {
                byte[] disableBytes = ByteHelper.ParseBytes(runtime.Definition.DisableBytes);
                if (disableBytes.Length > 0)
                {
                    _memory.WriteBytes(runtime.PatchedAddress, disableBytes);
                }
                else if (runtime.OriginalBytes != null)
                {
                    _memory.WriteBytes(runtime.PatchedAddress, runtime.OriginalBytes);
                }
            }
            else if (runtime.OriginalBytes != null)
            {
                _memory.WriteBytes(runtime.PatchedAddress, runtime.OriginalBytes);
            }

            runtime.IsEnabled = false;
            runtime.PatchedAddress = IntPtr.Zero;
            runtime.OriginalBytes = null;
            Log("Disabled: " + runtime.Definition.Name);
        }

        private void MaintainDynamicCheats()
        {
            if (!_memory.IsAttached)
            {
                return;
            }

            foreach (CheatRuntime runtime in _runtimes.Values)
            {
                if (!runtime.IsEnabled)
                {
                    continue;
                }

                if (GetActionType(runtime.Definition) == "godmode")
                {
                    try
                    {
                        MaintainGodMode();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void MaintainGodMode()
        {
            InstallHealthCollectorHook();
            if (_healthCollectorHook == null || _healthCollectorHook.DataAddress == IntPtr.Zero)
            {
                return;
            }

            byte[] ptrBytes = _memory.ReadBytes(_healthCollectorHook.DataAddress, 8);
            long baseAddress = BitConverter.ToInt64(ptrBytes, 0);
            if (baseAddress == 0)
            {
                return;
            }

            IntPtr maxAddress = new IntPtr(baseAddress + 0x368);
            IntPtr healthAddress = new IntPtr(baseAddress + 0x364);
            byte[] maxHealth = _memory.ReadBytes(maxAddress, 4);
            _memory.WriteBytes(healthAddress, maxHealth);
        }

        private void InstallHealthCollectorHook()
        {
            if (_healthCollectorHook != null)
            {
                return;
            }

            IntPtr hookAddress = FindPatternAddress("48 8B 4B 08 45 8B CC");
            Log("Installing God Mode collector hook at 0x" + hookAddress.ToInt64().ToString("X") + ".");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 7);
            IntPtr dataAddress = _memory.Allocate(8);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 128);

            List<byte> cave = new List<byte>();
            cave.Add(0x50);
            cave.Add(0x48);
            cave.Add(0xB8);
            cave.AddRange(BitConverter.GetBytes(dataAddress.ToInt64()));
            cave.Add(0x48);
            cave.Add(0x89);
            cave.Add(0x18);
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x48, 0x8B, 0x4B, 0x08, 0x45, 0x8B, 0xCC });
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 7)));

            _memory.WriteBytes(caveAddress, cave.ToArray());
            _memory.WriteBytes(hookAddress, BuildJumpPatch(hookAddress, caveAddress, 7));

            _healthCollectorHook = new HookState();
            _healthCollectorHook.HookAddress = hookAddress;
            _healthCollectorHook.CaveAddress = caveAddress;
            _healthCollectorHook.DataAddress = dataAddress;
            _healthCollectorHook.OriginalBytes = originalBytes;
            _healthCollectorHook.OverwriteSize = 7;
        }

        private HookState InstallOneHitKillHook()
        {
            IntPtr hookAddress = FindPatternAddress("C5 FA 10 81 64 03 00 00");
            Log("Installing 1-Hit Kill hook at 0x" + hookAddress.ToInt64().ToString("X") + ".");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 8);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 128);

            List<byte> cave = new List<byte>();
            cave.AddRange(new byte[] { 0x66, 0x81, 0x79, 0x10, 0x77, 0x77 });
            cave.AddRange(new byte[] { 0x74, 0x16 });
            cave.AddRange(new byte[] { 0x81, 0xB9, 0x64, 0x03, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41 });
            cave.AddRange(new byte[] { 0x76, 0x0A });
            cave.AddRange(new byte[] { 0xC7, 0x81, 0x64, 0x03, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41 });
            cave.AddRange(new byte[] { 0xC5, 0xFA, 0x10, 0x81, 0x64, 0x03, 0x00, 0x00 });
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 8)));

            _memory.WriteBytes(caveAddress, cave.ToArray());
            _memory.WriteBytes(hookAddress, BuildJumpPatch(hookAddress, caveAddress, 8));

            HookState hook = new HookState();
            hook.HookAddress = hookAddress;
            hook.CaveAddress = caveAddress;
            hook.OriginalBytes = originalBytes;
            hook.OverwriteSize = 8;
            return hook;
        }

        private void UninstallHook(ref HookState hook)
        {
            if (hook == null)
            {
                return;
            }

            if (_memory.IsAttached && hook.HookAddress != IntPtr.Zero && hook.OriginalBytes != null)
            {
                _memory.WriteBytes(hook.HookAddress, hook.OriginalBytes);
            }

            if (_memory.IsAttached)
            {
                if (hook.CaveAddress != IntPtr.Zero)
                {
                    _memory.Free(hook.CaveAddress);
                }

                if (hook.DataAddress != IntPtr.Zero)
                {
                    _memory.Free(hook.DataAddress);
                }
            }

            hook = null;
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

            Log("Scanning module " + resolvedModuleName + " at 0x" + moduleBase.ToInt64().ToString("X") + " (size 0x" + moduleSize.ToString("X") + ") for pattern " + pattern + ".");
            byte[] moduleBytes = _memory.ReadModule(moduleBase, moduleSize);
            int found = PatternScanner.Find(moduleBytes, pattern);
            if (found < 0)
            {
                throw new InvalidOperationException("Pattern not found: " + pattern);
            }

            return IntPtr.Add(moduleBase, found);
        }

        private static byte[] BuildJumpPatch(IntPtr fromAddress, IntPtr toAddress, int overwriteSize)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BuildRelativeJump(fromAddress, toAddress));
            while (bytes.Count < overwriteSize)
            {
                bytes.Add(0x90);
            }

            return bytes.ToArray();
        }

        private static byte[] BuildRelativeJump(IntPtr fromAddress, IntPtr toAddress)
        {
            long diff = toAddress.ToInt64() - (fromAddress.ToInt64() + 5);
            if (diff < int.MinValue || diff > int.MaxValue)
            {
                throw new InvalidOperationException("Relative jump target is out of range. Near allocation failed.");
            }

            List<byte> bytes = new List<byte>();
            bytes.Add(0xE9);
            bytes.AddRange(BitConverter.GetBytes((int)diff));
            return bytes.ToArray();
        }

        private static bool IsCrysis3FlagAction(string actionType)
        {
            return actionType == "c3-core-flag" || actionType == "c3-ammo-flag";
        }

        private void SetCrysis3Flag(CheatRuntime runtime, bool enabled)
        {
            string actionType = GetActionType(runtime.Definition);
            if (actionType == "c3-core-flag")
            {
                if (!enabled && _crysis3Core == null)
                {
                    return;
                }

                EnsureCrysis3CoreHooks();
                if (runtime.Definition.Id == "lock-energy")
                {
                    WriteByteIfAttached(_crysis3Core.EnergyLockFlagAddress, enabled);
                }
                else if (runtime.Definition.Id == "lock-health")
                {
                    WriteByteIfAttached(_crysis3Core.HealthLockFlagAddress, enabled);
                }
                else if (runtime.Definition.Id == "one-hit-kill")
                {
                    WriteByteIfAttached(_crysis3Core.HealthKillFlagAddress, enabled);
                }
                else
                {
                    throw new InvalidOperationException("Unknown Crysis 3 core flag: " + runtime.Definition.Id);
                }

                return;
            }

            if (actionType == "c3-ammo-flag")
            {
                if (!enabled && _crysis3Ammo == null)
                {
                    return;
                }

                EnsureCrysis3AmmoHooks();
                if (runtime.Definition.Id == "lock-holster")
                {
                    WriteByteIfAttached(_crysis3Ammo.HolsterFlagAddress, enabled);
                }
                else if (runtime.Definition.Id == "lock-clip")
                {
                    WriteByteIfAttached(_crysis3Ammo.ClipFlagAddress, enabled);
                }
                else
                {
                    throw new InvalidOperationException("Unknown Crysis 3 ammo flag: " + runtime.Definition.Id);
                }

                return;
            }

            throw new InvalidOperationException("Unsupported Crysis 3 action type: " + actionType);
        }

        private void EnsureCrysis3CoreHooks()
        {
            EnsureAttached();
            if (_crysis3Core != null)
            {
                return;
            }

            Crysis3CoreState state = new Crysis3CoreState();
            try
            {
                state.EnergyPointerAddress = AllocateAndWrite(8, new byte[8]);
                state.EnergyLockFlagAddress = AllocateAndWrite(1, new byte[] { 0 });
                state.HealthPointerAddress = AllocateAndWrite(8, new byte[8]);
                state.HealthValueAddress = AllocateAndWrite(4, BitConverter.GetBytes(1000f));
                state.HealthInfoAddress = AllocateAndWrite(4, new byte[4]);
                state.HealthMaxAddress = AllocateAndWrite(4, BitConverter.GetBytes(10f));
                state.HealthLockFlagAddress = AllocateAndWrite(1, new byte[] { 0 });
                state.HealthKillFlagAddress = AllocateAndWrite(1, new byte[] { 0 });
                state.StaminaPointerAddress = AllocateAndWrite(8, new byte[8]);

                state.EnergyHook = InstallCrysis3EnergyCollectorHook(state);
                state.EnergyLockHook = InstallCrysis3EnergyLockHook(state);
                state.HealthHook = InstallCrysis3HealthHook(state);
                state.StaminaHook = InstallCrysis3StaminaHook(state);
                _crysis3Core = state;
                Log("Installed Crysis 3 core hooks from FR v1.4 table.");
            }
            catch
            {
                _crysis3Core = state;
                UninstallCrysis3CoreHooks();
                throw;
            }
        }

        private void EnsureCrysis3AmmoHooks()
        {
            EnsureAttached();
            if (_crysis3Ammo != null)
            {
                return;
            }

            Crysis3AmmoState state = new Crysis3AmmoState();
            try
            {
                state.HolsterPointerAddress = AllocateAndWrite(8, new byte[8]);
                state.HolsterFlagAddress = AllocateAndWrite(1, new byte[] { 0 });
                state.ClipPointerAddress = AllocateAndWrite(8, new byte[8]);
                state.ClipFlagAddress = AllocateAndWrite(1, new byte[] { 0 });

                state.HolsterClampHook = InstallCrysis3AmmoHolsterClampHook(state);
                state.HolsterPointerHook = InstallCrysis3AmmoHolsterPointerHook(state);
                state.ClipHook = InstallCrysis3AmmoClipHook(state);
                _crysis3Ammo = state;
                Log("Installed Crysis 3 ammo hooks from FR v1.4 table.");
            }
            catch
            {
                _crysis3Ammo = state;
                UninstallCrysis3AmmoHooks();
                throw;
            }
        }

        private HookState InstallCrysis3EnergyCollectorHook(Crysis3CoreState state)
        {
            IntPtr hookAddress = FindPatternAddress("C5 7A 10 AC 32 B0 00 00 00");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 9);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 256);

            List<byte> cave = new List<byte>();
            cave.AddRange(new byte[] { 0x41, 0x57 });
            cave.AddRange(new byte[] { 0x49, 0x89, 0xD7 });
            cave.AddRange(new byte[] { 0x49, 0x01, 0xF7 });
            cave.Add(0x50);
            EmitMovRax(cave, state.EnergyPointerAddress);
            cave.AddRange(new byte[] { 0x4C, 0x89, 0x38 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x41, 0x5F });
            cave.AddRange(originalBytes);
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 9)));

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 9);
        }

        private HookState InstallCrysis3EnergyLockHook(Crysis3CoreState state)
        {
            IntPtr hookAddress = FindPatternAddress("C5 FA 11 0B 48 8B 5C 24 70");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 9);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 256);

            List<byte> cave = new List<byte>();
            cave.Add(0x50);
            EmitMovRax(cave, state.EnergyLockFlagAddress);
            cave.AddRange(new byte[] { 0x80, 0x38, 0x01 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x74, 0x04 });
            cave.AddRange(new byte[] { 0xC5, 0xFA, 0x11, 0x0B });
            cave.AddRange(new byte[] { 0x48, 0x8B, 0x5C, 0x24, 0x70 });
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 9)));

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 9);
        }

        private HookState InstallCrysis3HealthHook(Crysis3CoreState state)
        {
            IntPtr hookAddress = FindPatternAddress("8B 4E 44 89 7C 24 68");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 7);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 512);

            List<byte> cave = new List<byte>();
            cave.Add(0x53);
            cave.AddRange(new byte[] { 0x66, 0x81, 0x7E, 0x10, 0x77, 0x77 });
            int jePlayer = EmitJccPlaceholder(cave, 0x84);

            cave.Add(0x50);
            EmitMovRax(cave, state.HealthKillFlagAddress);
            cave.AddRange(new byte[] { 0x80, 0x38, 0x01 });
            cave.Add(0x58);
            int jneCodeFromKill = EmitJccPlaceholder(cave, 0x85);
            cave.AddRange(new byte[] { 0x8B, 0x5E, 0x44 });
            cave.AddRange(new byte[] { 0x33, 0x5E, 0x40 });
            cave.AddRange(new byte[] { 0x81, 0xFB, 0x00, 0x00, 0x20, 0x41 });
            int jbeCodeFromEnemyHealth = EmitJccPlaceholder(cave, 0x86);
            cave.Add(0x50);
            EmitMovRax(cave, state.HealthMaxAddress);
            cave.AddRange(new byte[] { 0x8B, 0x18 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x33, 0x5E, 0x44 });
            cave.AddRange(new byte[] { 0x89, 0x5E, 0x40 });
            int jmpCodeAfterKill = EmitJmpPlaceholder(cave);

            int playerOffset = cave.Count;
            cave.Add(0x50);
            EmitMovRax(cave, state.HealthPointerAddress);
            cave.AddRange(new byte[] { 0x48, 0x89, 0x30 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x8B, 0x5E, 0x40 });
            cave.AddRange(new byte[] { 0x33, 0x5E, 0x44 });
            cave.Add(0x50);
            EmitMovRax(cave, state.HealthInfoAddress);
            cave.AddRange(new byte[] { 0x89, 0x18 });
            cave.Add(0x58);
            cave.Add(0x50);
            EmitMovRax(cave, state.HealthLockFlagAddress);
            cave.AddRange(new byte[] { 0x80, 0x38, 0x01 });
            cave.Add(0x58);
            int jneCodeFromHealthLock = EmitJccPlaceholder(cave, 0x85);
            cave.Add(0x50);
            EmitMovRax(cave, state.HealthValueAddress);
            cave.AddRange(new byte[] { 0x8B, 0x18 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x33, 0x5E, 0x44 });
            cave.AddRange(new byte[] { 0x89, 0x5E, 0x40 });

            int codeOffset = cave.Count;
            cave.Add(0x5B);
            cave.AddRange(originalBytes);
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 7)));

            PatchRelative(cave, jePlayer, playerOffset);
            PatchRelative(cave, jneCodeFromKill, codeOffset);
            PatchRelative(cave, jbeCodeFromEnemyHealth, codeOffset);
            PatchRelative(cave, jmpCodeAfterKill, codeOffset);
            PatchRelative(cave, jneCodeFromHealthLock, codeOffset);

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 7);
        }

        private HookState InstallCrysis3StaminaHook(Crysis3CoreState state)
        {
            IntPtr hookAddress = FindPatternAddress("C6 43 2C 00 C7 43 28 00 00 00 00 48 8B");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 11);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 256);

            List<byte> cave = new List<byte>();
            cave.Add(0x50);
            EmitMovRax(cave, state.StaminaPointerAddress);
            cave.AddRange(new byte[] { 0x48, 0x89, 0x18 });
            cave.Add(0x58);
            cave.AddRange(originalBytes);
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 11)));

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 11);
        }

        private HookState InstallCrysis3AmmoHolsterClampHook(Crysis3AmmoState state)
        {
            IntPtr hookAddress = FindPatternAddress("8B DE 0F 4C D8");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 5);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 256);

            List<byte> cave = new List<byte>();
            cave.AddRange(originalBytes);
            cave.Add(0x50);
            EmitMovRax(cave, state.HolsterFlagAddress);
            cave.AddRange(new byte[] { 0x80, 0x38, 0x01 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x75, 0x03 });
            cave.AddRange(new byte[] { 0x0F, 0x4D, 0xD8 });
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 5)));

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 5);
        }

        private HookState InstallCrysis3AmmoHolsterPointerHook(Crysis3AmmoState state)
        {
            IntPtr holsterBase = FindPatternAddress("8B DE 0F 4C D8");
            IntPtr hookAddress = IntPtr.Add(holsterBase, 0x1C);
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 7);
            byte[] expectedBytes = ByteHelper.ParseBytes("C6 87 79 01 00 00 01");
            if (!originalBytes.SequenceEqual(expectedBytes))
            {
                throw new InvalidOperationException("Ammo holster pointer expected bytes do not match. Found [" + FormatBytes(originalBytes) + "].");
            }

            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 256);
            List<byte> cave = new List<byte>();
            cave.Add(0x52);
            EmitMovRdx(cave, state.HolsterPointerAddress);
            cave.AddRange(new byte[] { 0x48, 0x89, 0x02 });
            cave.Add(0x5A);
            cave.AddRange(originalBytes);
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 7)));

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 7);
        }

        private HookState InstallCrysis3AmmoClipHook(Crysis3AmmoState state)
        {
            IntPtr hookAddress = FindPatternAddress("44 2B 75 90 44 8B C6");
            byte[] originalBytes = _memory.ReadBytes(hookAddress, 7);
            IntPtr caveAddress = _memory.AllocateNear(hookAddress, 256);

            List<byte> cave = new List<byte>();
            cave.Add(0x50);
            EmitMovRax(cave, state.ClipFlagAddress);
            cave.AddRange(new byte[] { 0x80, 0x38, 0x01 });
            cave.Add(0x58);
            cave.AddRange(new byte[] { 0x74, 0x04 });
            cave.AddRange(new byte[] { 0x44, 0x2B, 0x75, 0x90 });
            cave.AddRange(new byte[] { 0x44, 0x8B, 0xC6 });
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, 7)));

            return InstallHook(hookAddress, caveAddress, originalBytes, cave.ToArray(), 7);
        }

        private HookState InstallHook(IntPtr hookAddress, IntPtr caveAddress, byte[] originalBytes, byte[] caveBytes, int overwriteSize)
        {
            _memory.WriteBytes(caveAddress, caveBytes);
            _memory.WriteBytes(hookAddress, BuildJumpPatch(hookAddress, caveAddress, overwriteSize));

            HookState hook = new HookState();
            hook.HookAddress = hookAddress;
            hook.CaveAddress = caveAddress;
            hook.OriginalBytes = originalBytes;
            hook.OverwriteSize = overwriteSize;
            return hook;
        }

        private IntPtr AllocateAndWrite(int size, byte[] bytes)
        {
            IntPtr address = _memory.Allocate(size);
            _memory.WriteBytes(address, bytes);
            return address;
        }

        private void WriteByteIfAttached(IntPtr address, bool enabled)
        {
            if (_memory.IsAttached && address != IntPtr.Zero)
            {
                _memory.WriteBytes(address, new byte[] { enabled ? (byte)1 : (byte)0 });
            }
        }

        private void MaybeUninstallCrysis3Hooks()
        {
            if (!HasEnabledCrysis3CoreFlag())
            {
                UninstallCrysis3CoreHooks();
            }

            if (!HasEnabledCrysis3AmmoFlag())
            {
                UninstallCrysis3AmmoHooks();
            }
        }

        private bool HasEnabledCrysis3CoreFlag()
        {
            return IsRuntimeEnabled("lock-energy") || IsRuntimeEnabled("lock-health") || IsRuntimeEnabled("one-hit-kill");
        }

        private bool HasEnabledCrysis3AmmoFlag()
        {
            return IsRuntimeEnabled("lock-holster") || IsRuntimeEnabled("lock-clip");
        }

        private bool IsRuntimeEnabled(string id)
        {
            CheatRuntime runtime;
            return _runtimes.TryGetValue(id, out runtime) && runtime.IsEnabled;
        }

        private void UninstallCrysis3Hooks()
        {
            UninstallCrysis3CoreHooks();
            UninstallCrysis3AmmoHooks();
        }

        private void UninstallCrysis3CoreHooks()
        {
            if (_crysis3Core == null)
            {
                return;
            }

            WriteByteIfAttached(_crysis3Core.EnergyLockFlagAddress, false);
            WriteByteIfAttached(_crysis3Core.HealthLockFlagAddress, false);
            WriteByteIfAttached(_crysis3Core.HealthKillFlagAddress, false);
            UninstallHook(ref _crysis3Core.EnergyHook);
            UninstallHook(ref _crysis3Core.EnergyLockHook);
            UninstallHook(ref _crysis3Core.HealthHook);
            UninstallHook(ref _crysis3Core.StaminaHook);
            FreeIfAttached(_crysis3Core.EnergyPointerAddress);
            FreeIfAttached(_crysis3Core.EnergyLockFlagAddress);
            FreeIfAttached(_crysis3Core.HealthPointerAddress);
            FreeIfAttached(_crysis3Core.HealthValueAddress);
            FreeIfAttached(_crysis3Core.HealthInfoAddress);
            FreeIfAttached(_crysis3Core.HealthMaxAddress);
            FreeIfAttached(_crysis3Core.HealthLockFlagAddress);
            FreeIfAttached(_crysis3Core.HealthKillFlagAddress);
            FreeIfAttached(_crysis3Core.StaminaPointerAddress);
            _crysis3Core = null;
        }

        private void UninstallCrysis3AmmoHooks()
        {
            if (_crysis3Ammo == null)
            {
                return;
            }

            WriteByteIfAttached(_crysis3Ammo.HolsterFlagAddress, false);
            WriteByteIfAttached(_crysis3Ammo.ClipFlagAddress, false);
            UninstallHook(ref _crysis3Ammo.HolsterClampHook);
            UninstallHook(ref _crysis3Ammo.HolsterPointerHook);
            UninstallHook(ref _crysis3Ammo.ClipHook);
            FreeIfAttached(_crysis3Ammo.HolsterPointerAddress);
            FreeIfAttached(_crysis3Ammo.HolsterFlagAddress);
            FreeIfAttached(_crysis3Ammo.ClipPointerAddress);
            FreeIfAttached(_crysis3Ammo.ClipFlagAddress);
            _crysis3Ammo = null;
        }

        private void FreeIfAttached(IntPtr address)
        {
            if (_memory.IsAttached && address != IntPtr.Zero)
            {
                _memory.Free(address);
            }
        }

        private static void EmitMovRax(List<byte> bytes, IntPtr value)
        {
            bytes.AddRange(new byte[] { 0x48, 0xB8 });
            bytes.AddRange(BitConverter.GetBytes(value.ToInt64()));
        }

        private static void EmitMovRdx(List<byte> bytes, IntPtr value)
        {
            bytes.AddRange(new byte[] { 0x48, 0xBA });
            bytes.AddRange(BitConverter.GetBytes(value.ToInt64()));
        }

        private static int EmitJccPlaceholder(List<byte> bytes, byte condition)
        {
            bytes.Add(0x0F);
            bytes.Add(condition);
            int relIndex = bytes.Count;
            bytes.AddRange(new byte[4]);
            return relIndex;
        }

        private static int EmitJmpPlaceholder(List<byte> bytes)
        {
            bytes.Add(0xE9);
            int relIndex = bytes.Count;
            bytes.AddRange(new byte[4]);
            return relIndex;
        }

        private static void PatchRelative(List<byte> bytes, int relIndex, int targetOffset)
        {
            int relative = targetOffset - (relIndex + 4);
            byte[] relativeBytes = BitConverter.GetBytes(relative);
            for (int i = 0; i < relativeBytes.Length; i++)
            {
                bytes[relIndex + i] = relativeBytes[i];
            }
        }

        private IntPtr ResolveAddress(CheatRuntime runtime)
        {
            if (runtime.PatchedAddress != IntPtr.Zero)
            {
                return runtime.PatchedAddress;
            }

            int moduleSize;
            string resolvedModuleName;
            IntPtr moduleBase = _memory.GetModuleBase(_profile.ModuleName, out moduleSize, out resolvedModuleName);
            if (moduleBase == IntPtr.Zero || moduleSize <= 0)
            {
                throw new InvalidOperationException("Module not found: " + _profile.ModuleName);
            }

            Log("Resolving " + runtime.Definition.Name + " inside module " + resolvedModuleName + " at 0x" + moduleBase.ToInt64().ToString("X") + " (size 0x" + moduleSize.ToString("X") + ").");
            int? moduleOffset = ByteHelper.ParseHexInt(runtime.Definition.ModuleOffsetHex);
            if (moduleOffset.HasValue)
            {
                return IntPtr.Add(moduleBase, moduleOffset.Value);
            }

            if (string.IsNullOrWhiteSpace(runtime.Definition.Pattern))
            {
                throw new InvalidOperationException("Cheat has neither moduleOffsetHex nor pattern.");
            }

            byte[] moduleBytes = _memory.ReadModule(moduleBase, moduleSize);
            int found = PatternScanner.Find(moduleBytes, runtime.Definition.Pattern);
            if (found < 0)
            {
                throw new InvalidOperationException("Pattern not found: " + runtime.Definition.Pattern);
            }

            if (runtime.Definition.RelativeReadOffset != 0)
            {
                int displacementOffset = found + runtime.Definition.RelativeReadOffset;
                if (displacementOffset < 0 || displacementOffset + 4 > moduleBytes.Length)
                {
                    throw new InvalidOperationException("Relative displacement is out of range.");
                }

                int displacement = BitConverter.ToInt32(moduleBytes, displacementOffset);
                int relativeAddress = found + runtime.Definition.RelativeReadOffset + 4 + displacement + runtime.Definition.RelativeAdjust;
                return IntPtr.Add(moduleBase, relativeAddress);
            }

            return IntPtr.Add(moduleBase, found + runtime.Definition.PatchOffset);
        }

        private static string GetActionType(CheatDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.ActionType) ? "patch" : definition.ActionType.Trim().ToLowerInvariant();
        }

        private bool IsAnyRuntimeEnabled(string actionType)
        {
            foreach (CheatRuntime runtime in _runtimes.Values)
            {
                if (runtime.IsEnabled && GetActionType(runtime.Definition) == actionType)
                {
                    return true;
                }
            }

            return false;
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
        }

        private void EnableAllCheatsOnAttach()
        {
            Log("Auto-enable on attach started.");

            foreach (CheatRuntime runtime in _runtimes.Values.ToList())
            {
                try
                {
                    if (!runtime.IsEnabled)
                    {
                        ToggleCheat(runtime, true);
                    }
                }
                catch (Exception ex)
                {
                    runtime.IsEnabled = false;
                    UpdateRuntimeUi(runtime);
                    LogError(runtime.Definition.Name + " auto-enable failed", ex);
                }
            }

            Log("Auto-enable on attach finished.");
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

            if (!NativeMethods.RegisterHotKey(Handle, 998, 0, (uint)Keys.D7))
            {
                Log("Hotkey registration failed for 7 disable-all key: " + GetLastWin32ErrorMessage() + ".");
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
                if (id == 999)
                {
                    DisableAllCheats();
                    Log("Panic key pressed.");
                    return;
                }

                if (id == 998)
                {
                    QueueTrainerWork(delegate { DisableAllCheats(); }, "Disable-all hotkey failed");
                    Log("Hotkey pressed: 7 -> Disable All.");
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

            Exception inner = ex.InnerException;
            while (inner != null)
            {
                details.AppendLine("Inner: " + inner.GetType().FullName + " - " + inner.Message);
                inner = inner.InnerException;
            }

            details.AppendLine("Stack:");
            details.AppendLine(ex.StackTrace ?? "(no stack trace)");
            details.AppendLine();
            AppendRawToFileLog(details.ToString());
        }

        private void InitializeFileLog()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Crysis3RemasteredTrainer.log");

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
            title.Width = 185;
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
                case "lock-energy":
                    key = Keys.D1;
                    return true;
                case "lock-holster":
                    key = Keys.D2;
                    return true;
                case "lock-clip":
                    key = Keys.D3;
                    return true;
                case "lock-health":
                    key = Keys.D4;
                    return true;
                case "one-hit-kill":
                    key = Keys.D5;
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
            RunOnUiThread(delegate
            {
                _statusLabel.Text = text;
            });
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

        private static string FormatBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            return BitConverter.ToString(bytes).Replace("-", " ");
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
            }

            internal CheatDefinition Definition;
            internal bool IsEnabled;
            internal byte[] OriginalBytes;
            internal IntPtr PatchedAddress;
            internal HookState Hook;
            internal Button EnableButton;
            internal Button DisableButton;
            internal Label StateLabel;
        }

        private sealed class HotkeyBinding
        {
            internal CheatDefinition Cheat;
            internal string DisplayKey;
        }

        private sealed class Crysis3CoreState
        {
            internal IntPtr EnergyPointerAddress;
            internal IntPtr EnergyLockFlagAddress;
            internal IntPtr HealthPointerAddress;
            internal IntPtr HealthValueAddress;
            internal IntPtr HealthInfoAddress;
            internal IntPtr HealthMaxAddress;
            internal IntPtr HealthLockFlagAddress;
            internal IntPtr HealthKillFlagAddress;
            internal IntPtr StaminaPointerAddress;
            internal HookState EnergyHook;
            internal HookState EnergyLockHook;
            internal HookState HealthHook;
            internal HookState StaminaHook;
        }

        private sealed class Crysis3AmmoState
        {
            internal IntPtr HolsterPointerAddress;
            internal IntPtr HolsterFlagAddress;
            internal IntPtr ClipPointerAddress;
            internal IntPtr ClipFlagAddress;
            internal HookState HolsterClampHook;
            internal HookState HolsterPointerHook;
            internal HookState ClipHook;
        }

        private sealed class HookState
        {
            internal IntPtr HookAddress;
            internal IntPtr CaveAddress;
            internal IntPtr DataAddress;
            internal byte[] OriginalBytes;
            internal int OverwriteSize;
        }
    }
}
