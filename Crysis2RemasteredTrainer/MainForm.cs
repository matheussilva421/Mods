using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Crysis2RemasteredTrainer
{
    internal sealed class MainForm : Form
    {
        private readonly ProcessMemory _memory = new ProcessMemory();
        private readonly Dictionary<string, CheatRuntime> _runtimes = new Dictionary<string, CheatRuntime>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, CheatDefinition> _hotkeyMap = new Dictionary<int, CheatDefinition>();
        private readonly FlowLayoutPanel _cheatPanel = new FlowLayoutPanel();
        private readonly TextBox _logBox = new TextBox();
        private readonly Label _statusLabel = new Label();
        private readonly Button _refreshButton = new Button();
        private readonly Button _disableAllButton = new Button();
        private readonly Timer _attachTimer = new Timer();
        private TrainerProfile _profile;
        private string _profilePath;
        private int _attachedProcessId;
        private HookState _healthCollectorHook;

        internal MainForm()
        {
            InitializeUi();
            Load += OnLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeUi()
        {
            Text = "Crysis 2 Remastered Trainer";
            Width = 760;
            Height = 580;
            MinimumSize = new Size(680, 500);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            Controls.Add(root);

            Label title = new Label();
            title.Text = "Cheat Deck companion trainer";
            title.Dock = DockStyle.Fill;
            title.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = new Padding(12, 0, 0, 0);
            root.Controls.Add(title, 0, 0);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Fill;
            topBar.Padding = new Padding(12, 8, 12, 8);
            root.Controls.Add(topBar, 0, 1);

            _statusLabel.AutoSize = false;
            _statusLabel.Text = "Status: loading profile";
            _statusLabel.Width = 420;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Dock = DockStyle.Left;
            topBar.Controls.Add(_statusLabel);

            _disableAllButton.Text = "Disable All";
            _disableAllButton.Width = 110;
            _disableAllButton.Dock = DockStyle.Right;
            _disableAllButton.Click += delegate { DisableAllCheats(); };
            topBar.Controls.Add(_disableAllButton);

            _refreshButton.Text = "Refresh Attach";
            _refreshButton.Width = 110;
            _refreshButton.Dock = DockStyle.Right;
            _refreshButton.Click += delegate { RefreshAttachment(); };
            topBar.Controls.Add(_refreshButton);

            _cheatPanel.Dock = DockStyle.Fill;
            _cheatPanel.FlowDirection = FlowDirection.TopDown;
            _cheatPanel.WrapContents = false;
            _cheatPanel.AutoScroll = true;
            _cheatPanel.Padding = new Padding(12, 8, 12, 8);
            root.Controls.Add(_cheatPanel, 0, 2);

            _logBox.Dock = DockStyle.Fill;
            _logBox.Multiline = true;
            _logBox.ReadOnly = true;
            _logBox.ScrollBars = ScrollBars.Vertical;
            _logBox.Font = new Font("Consolas", 9);
            root.Controls.Add(_logBox, 0, 3);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles", "crysis2-remastered.fr-v1.4.json");
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
            _attachTimer.Interval = _profile.PollIntervalMs;
            _attachTimer.Tick += OnAttachTimerTick;
            BuildCheatList();
            RegisterHotkeys();
            RefreshAttachment();
            _attachTimer.Start();
        }

        private void OnAttachTimerTick(object sender, EventArgs e)
        {
            RefreshAttachment();
            MaintainDynamicCheats();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _attachTimer.Stop();
            DisableAllCheats();
            UnregisterHotkeys();
            _memory.Dispose();
        }

        private void BuildCheatList()
        {
            _cheatPanel.Controls.Clear();
            _runtimes.Clear();
            _hotkeyMap.Clear();

            foreach (CheatDefinition cheat in _profile.Cheats)
            {
                CheatRuntime runtime = new CheatRuntime(cheat);
                _runtimes[cheat.Id] = runtime;

                Panel row = new Panel();
                row.Width = 680;
                row.Height = 72;
                row.Margin = new Padding(0, 0, 0, 8);
                row.BorderStyle = BorderStyle.FixedSingle;

                CheckBox toggle = new CheckBox();
                toggle.Left = 12;
                toggle.Top = 12;
                toggle.Width = 340;
                toggle.Text = cheat.Name + " (" + cheat.Hotkey + ")";
                toggle.CheckedChanged += delegate
                {
                    if (toggle.Focused)
                    {
                        ToggleCheat(runtime, toggle.Checked);
                    }
                };
                row.Controls.Add(toggle);

                Label description = new Label();
                description.Left = 12;
                description.Top = 36;
                description.Width = 640;
                description.Height = 28;
                description.Text = string.IsNullOrWhiteSpace(cheat.Description) ? "No description." : cheat.Description;
                row.Controls.Add(description);

                runtime.Toggle = toggle;
                _cheatPanel.Controls.Add(row);
            }

            _statusLabel.Text = "Status: profile loaded, waiting for process";
            Log("Loaded profile: " + _profile.ProfileName);
        }

        private void RefreshAttachment()
        {
            try
            {
                bool attached = _memory.Attach(_profile.ProcessName);
                int newProcessId = attached ? _memory.ProcessId : 0;
                if (newProcessId != _attachedProcessId)
                {
                    ResetProcessScopedState();
                    _attachedProcessId = newProcessId;
                }

                if (attached)
                {
                    _statusLabel.Text = "Status: attached to " + _memory.Process.ProcessName + " (PID " + _memory.Process.Id + ")";
                }
                else
                {
                    _statusLabel.Text = "Status: waiting for " + _profile.ProcessName;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Status: attach failed";
                Log("Attach failed: " + ex.Message);
            }
        }

        private void ResetProcessScopedState()
        {
            _healthCollectorHook = null;
            foreach (CheatRuntime runtime in _runtimes.Values)
            {
                runtime.IsEnabled = false;
                runtime.OriginalBytes = null;
                runtime.PatchedAddress = IntPtr.Zero;
                runtime.Hook = null;
                if (runtime.Toggle != null && runtime.Toggle.Checked)
                {
                    runtime.Toggle.Checked = false;
                }
            }
        }

        private void ToggleCheat(CheatRuntime runtime, bool enable)
        {
            if (enable)
            {
                try
                {
                    EnableCheat(runtime);
                    runtime.Toggle.Checked = runtime.IsEnabled;
                }
                catch (Exception ex)
                {
                    runtime.IsEnabled = false;
                    runtime.Toggle.Checked = false;
                    Log(runtime.Definition.Name + " failed: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    DisableCheat(runtime);
                    runtime.Toggle.Checked = false;
                }
                catch (Exception ex)
                {
                    Log(runtime.Definition.Name + " restore failed: " + ex.Message);
                }
            }
        }

        private void EnableCheat(CheatRuntime runtime)
        {
            EnsureAttached();
            string actionType = GetActionType(runtime.Definition);

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
            byte[] expectedBytes = ByteHelper.ParseBytes(runtime.Definition.ExpectedBytes);
            if (expectedBytes.Length > 0)
            {
                byte[] currentBytes = _memory.ReadBytes(address, expectedBytes.Length);
                if (!currentBytes.SequenceEqual(expectedBytes))
                {
                    throw new InvalidOperationException("Expected bytes do not match. Update the signature or patch.");
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
            IntPtr moduleBase = _memory.GetModuleBase(_profile.ModuleName, out moduleSize);
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

        private IntPtr ResolveAddress(CheatRuntime runtime)
        {
            if (runtime.PatchedAddress != IntPtr.Zero)
            {
                return runtime.PatchedAddress;
            }

            int moduleSize;
            IntPtr moduleBase = _memory.GetModuleBase(_profile.ModuleName, out moduleSize);
            if (moduleBase == IntPtr.Zero || moduleSize <= 0)
            {
                throw new InvalidOperationException("Module not found: " + _profile.ModuleName);
            }

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
                throw new InvalidOperationException("Pattern not found.");
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
                    Log(runtime.Definition.Name + " disable-all error: " + ex.Message);
                }

                if (runtime.Toggle != null && runtime.Toggle.Checked)
                {
                    runtime.Toggle.Checked = false;
                }
            }
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
                Keys parsedKey;
                if (!Enum.TryParse(cheat.Hotkey, true, out parsedKey))
                {
                    continue;
                }

                if (NativeMethods.RegisterHotKey(Handle, id, 0, (uint)parsedKey))
                {
                    _hotkeyMap[id] = cheat;
                    id++;
                }
            }

            NativeMethods.RegisterHotKey(Handle, 999, 0, (uint)Keys.F12);
        }

        private void UnregisterHotkeys()
        {
            foreach (int id in _hotkeyMap.Keys.ToList())
            {
                NativeMethods.UnregisterHotKey(Handle, id);
            }

            NativeMethods.UnregisterHotKey(Handle, 999);
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

                CheatDefinition cheat;
                if (_hotkeyMap.TryGetValue(id, out cheat))
                {
                    CheatRuntime runtime;
                    if (_runtimes.TryGetValue(cheat.Id, out runtime))
                    {
                        bool nextState = !runtime.IsEnabled;
                        ToggleCheat(runtime, nextState);
                        runtime.Toggle.Checked = runtime.IsEnabled;
                    }
                }
            }

            base.WndProc(ref m);
        }

        private void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            _logBox.AppendText(line + Environment.NewLine);
        }

        private sealed class CheatRuntime
        {
            internal CheatRuntime(CheatDefinition definition)
            {
                Definition = definition;
            }

            internal CheatDefinition Definition;
            internal CheckBox Toggle;
            internal bool IsEnabled;
            internal byte[] OriginalBytes;
            internal IntPtr PatchedAddress;
            internal HookState Hook;
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

