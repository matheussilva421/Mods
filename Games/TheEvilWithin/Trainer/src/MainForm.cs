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

namespace TheEvilWithinTrainer
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
        private TrainerProfile _profile;
        private string _profilePath;
        private string _logFilePath;
        private int _attachedProcessId;
        private int _pollInProgress;
        private bool _isClosing;
        private System.Threading.Timer _pollTimer;

        internal MainForm()
        {
            InitializeUi();
            Load += OnLoad;
            FormClosing += OnFormClosing;
        }

        private void InitializeUi()
        {
            Text = "The Evil Within Trainer";
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
            title.Text = "Epic Games companion trainer";
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
            _profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles", "the-evil-within-epic.json");
            if (File.Exists(_profilePath))
            {
                _profile = TrainerProfile.Load(_profilePath);
                Log("Loaded external profile: " + _profilePath);
            }
            else
            {
                _profile = TrainerProfile.LoadFromJson(EmbeddedProfile.GetDefaultProfileJson());
                Log("External profile file not found. Loaded embedded Epic profile.");
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
                _cheatPanel.Controls.Add(CreateCheatCard(runtime), index % 2, index / 2);
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

            if (actionType == "hook")
            {
                InstallHook(runtime);
            }
            else
            {
                InstallPatch(runtime, actionType);
            }

            runtime.IsEnabled = true;
            Log("Enabled: " + runtime.Definition.Name);
        }

        private void DisableCheat(CheatRuntime runtime)
        {
            if (!runtime.IsEnabled)
            {
                return;
            }

            if (runtime.Hook != null)
            {
                UninstallHook(runtime);
            }
            else if (runtime.PatchedAddress != IntPtr.Zero)
            {
                EnsureAttached();
                if (runtime.OriginalBytes != null)
                {
                    _memory.WriteBytes(runtime.PatchedAddress, runtime.OriginalBytes);
                }
            }

            runtime.IsEnabled = false;
            runtime.PatchedAddress = IntPtr.Zero;
            runtime.OriginalBytes = null;
            Log("Disabled: " + runtime.Definition.Name);
        }

        private void InstallPatch(CheatRuntime runtime, string actionType)
        {
            IntPtr address = ResolveAddress(runtime);
            byte[] expectedBytes = ByteHelper.ParseBytes(runtime.Definition.ExpectedBytes);
            if (expectedBytes.Length > 0)
            {
                VerifyBytes(address, expectedBytes);
            }

            byte[] patchBytes;
            if (actionType == "setbytes")
            {
                patchBytes = ByteHelper.ParseBytes(runtime.Definition.EnableBytes);
            }
            else if (actionType == "patch")
            {
                patchBytes = ByteHelper.ParseBytes(runtime.Definition.PatchBytes);
            }
            else
            {
                throw new InvalidOperationException("Unsupported action type: " + actionType);
            }

            if (patchBytes.Length == 0)
            {
                throw new InvalidOperationException("Patch bytes are empty.");
            }

            if (runtime.OriginalBytes == null)
            {
                runtime.OriginalBytes = _memory.ReadBytes(address, patchBytes.Length);
            }

            _memory.WriteBytes(address, patchBytes);
            runtime.PatchedAddress = address;
            Log(runtime.Definition.Name + " patched at 0x" + address.ToInt64().ToString("X") + ".");
        }

        private void InstallHook(CheatRuntime runtime)
        {
            if (runtime.Hook != null)
            {
                return;
            }

            IntPtr hookAddress = ResolveAddress(runtime);
            int overwriteSize = runtime.Definition.OverwriteSize;
            if (overwriteSize < 5)
            {
                throw new InvalidOperationException("Hook overwrite size must be at least 5.");
            }

            byte[] expectedBytes = ByteHelper.ParseBytes(runtime.Definition.ExpectedBytes);
            if (expectedBytes.Length > 0)
            {
                VerifyBytes(hookAddress, expectedBytes);
            }

            byte[] originalBytes = _memory.ReadBytes(hookAddress, overwriteSize);
            byte[] caveBody = ByteHelper.ParseBytes(runtime.Definition.CaveBytes);
            if (caveBody.Length == 0)
            {
                throw new InvalidOperationException("Hook cave bytes are empty.");
            }

            IntPtr caveAddress = _memory.AllocateNear(hookAddress, Math.Max(128, caveBody.Length + 16));
            List<byte> cave = new List<byte>(caveBody);
            cave.AddRange(BuildRelativeJump(IntPtr.Add(caveAddress, cave.Count), IntPtr.Add(hookAddress, overwriteSize)));

            _memory.WriteBytes(caveAddress, cave.ToArray());
            _memory.WriteBytes(hookAddress, BuildJumpPatch(hookAddress, caveAddress, overwriteSize));

            runtime.Hook = new HookState();
            runtime.Hook.HookAddress = hookAddress;
            runtime.Hook.CaveAddress = caveAddress;
            runtime.Hook.OriginalBytes = originalBytes;
            runtime.Hook.OverwriteSize = overwriteSize;
            Log(runtime.Definition.Name + " hook installed at 0x" + hookAddress.ToInt64().ToString("X") + ", cave 0x" + caveAddress.ToInt64().ToString("X") + ".");
        }

        private void UninstallHook(CheatRuntime runtime)
        {
            HookState hook = runtime.Hook;
            if (hook == null)
            {
                return;
            }

            if (_memory.IsAttached && hook.HookAddress != IntPtr.Zero && hook.OriginalBytes != null)
            {
                _memory.WriteBytes(hook.HookAddress, hook.OriginalBytes);
            }

            if (_memory.IsAttached && hook.CaveAddress != IntPtr.Zero)
            {
                _memory.Free(hook.CaveAddress);
            }

            runtime.Hook = null;
        }

        private void VerifyBytes(IntPtr address, byte[] expectedBytes)
        {
            byte[] currentBytes = _memory.ReadBytes(address, expectedBytes.Length);
            if (!currentBytes.SequenceEqual(expectedBytes))
            {
                throw new InvalidOperationException(
                    "Expected bytes do not match. Expected [" + FormatBytes(expectedBytes) + "] but found [" + FormatBytes(currentBytes) + "].");
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

            int? moduleOffset = ByteHelper.ParseHexInt(runtime.Definition.ModuleOffsetHex);
            if (moduleOffset.HasValue)
            {
                return IntPtr.Add(moduleBase, moduleOffset.Value);
            }

            if (string.IsNullOrWhiteSpace(runtime.Definition.Pattern))
            {
                throw new InvalidOperationException("Cheat has neither moduleOffsetHex nor pattern.");
            }

            Log("Scanning module " + resolvedModuleName + " for " + runtime.Definition.Name + ".");
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

        private static string GetActionType(CheatDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.ActionType) ? "patch" : definition.ActionType.Trim().ToLowerInvariant();
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
                case "infinite-health":
                    key = Keys.D1;
                    return true;
                case "infinite-stamina":
                    key = Keys.D2;
                    return true;
                case "infinite-items":
                    key = Keys.D3;
                    return true;
                case "infinite-green-gel":
                    key = Keys.D4;
                    return true;
                case "infinite-parts":
                    key = Keys.D5;
                    return true;
                case "infinite-keys":
                    key = Keys.D6;
                    return true;
                case "no-spread":
                    key = Keys.D7;
                    return true;
                case "freeze-enemies":
                    key = Keys.D9;
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
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TheEvilWithinTrainer.log");
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

        private sealed class HookState
        {
            internal IntPtr HookAddress;
            internal IntPtr CaveAddress;
            internal byte[] OriginalBytes;
            internal int OverwriteSize;
        }
    }
}
