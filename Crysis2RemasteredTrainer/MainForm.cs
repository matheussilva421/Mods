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
            Height = 560;
            MinimumSize = new Size(640, 480);
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
            if (!File.Exists(_profilePath))
            {
                Log("Profile file not found: " + _profilePath);
                _statusLabel.Text = "Status: profile file missing";
                return;
            }

            _profile = TrainerProfile.Load(_profilePath);
            _attachTimer.Interval = _profile.PollIntervalMs;
            _attachTimer.Tick += delegate { RefreshAttachment(); };
            BuildCheatList();
            RegisterHotkeys();
            RefreshAttachment();
            _attachTimer.Start();
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
                toggle.Width = 320;
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

        private void ToggleCheat(CheatRuntime runtime, bool enable)
        {
            if (enable)
            {
                try
                {
                    EnableCheat(runtime);
                    runtime.Toggle.Checked = true;
                }
                catch (Exception ex)
                {
                    runtime.Toggle.Checked = false;
                    Log(runtime.Definition.Name + " failed: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    DisableCheat(runtime);
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

            IntPtr address = ResolveAddress(runtime);
            string actionType = GetActionType(runtime.Definition);
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
            if (!runtime.IsEnabled || runtime.PatchedAddress == IntPtr.Zero)
            {
                runtime.IsEnabled = false;
                return;
            }

            EnsureAttached();
            string actionType = GetActionType(runtime.Definition);
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
            Log("Disabled: " + runtime.Definition.Name);
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

        private void DisableAllCheats()
        {
            foreach (CheatRuntime runtime in _runtimes.Values)
            {
                try
                {
                    DisableCheat(runtime);
                }
                catch (Exception ex)
                {
                    Log(runtime.Definition.Name + " disable-all error: " + ex.Message);
                }

                if (runtime.Toggle.Checked)
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
        }
    }
}
