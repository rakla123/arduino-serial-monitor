using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ArduinoSerialMonitor
{
    public partial class SerialMonitor : Form
    {
        private const int BaudRate = 9600;
        private const int MaximumLogCharacters = 250000;

        private readonly Timer _timer;
        private readonly TimeSpan _connectionCooldown = TimeSpan.FromSeconds(2);
        private readonly TimeSpan _detectionLogCooldown = TimeSpan.FromSeconds(15);

        private SerialPort _serialPort;
        private bool _autoReconnectEnabled = true;
        private bool _isTryingToConnect;
        private bool _pendingCarriageReturn;
        private bool _updatingPortList;
        private DateTime _lastAttempt = DateTime.MinValue;
        private DateTime _lastDetectionLog = DateTime.MinValue;
        private string _lastConnectedPort;

        public SerialMonitor()
        {
            InitializeComponent();

            _timer = new Timer { Interval = 250 };
            _timer.Tick += Timer_Tick;

            SetStatus("Idle");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            RefreshPortList();
            _autoReconnectEnabled = true;
            btnConnect.Text = "Connecting...";
            SetStatus("Connecting...");
            _timer.Start();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (IsConnected())
            {
                _autoReconnectEnabled = false;
                Disconnect(userInitiated: true);
                btnConnect.Text = "Connect";
                SetStatus("Disconnected");
                return;
            }

            _autoReconnectEnabled = true;
            btnConnect.Text = "Connecting...";
            SetStatus("Connecting...");
            TryConnect(auto: false);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshPortList();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
        }

        private void cmbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_updatingPortList || cmbPorts.SelectedItem == null)
                return;

            SavePreferredPort(cmbPorts.SelectedItem.ToString());
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsConnected())
            {
                if (_autoReconnectEnabled)
                    TryConnect(auto: true);
                return;
            }

            try
            {
                string data = _serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                    AppendRaw(NormalizeLineEndings(data));
            }
            catch (Exception ex) when (ex is InvalidOperationException ||
                                       ex is System.IO.IOException ||
                                       ex is UnauthorizedAccessException)
            {
                AppendLine("Connection lost: " + ex.Message);
                Disconnect(userInitiated: false, silent: true);
                UpdateDisconnectedState();
            }
        }

        private void TryConnect(bool auto)
        {
            if (_isTryingToConnect || DateTime.Now - _lastAttempt < _connectionCooldown)
                return;

            _isTryingToConnect = true;
            _lastAttempt = DateTime.Now;

            try
            {
                string port = FindArduinoPort();
                if (port == null)
                {
                    LogDetectionFailure(auto);
                    SetStatus("Not detected");
                    btnConnect.Text = "Connect";
                    return;
                }

                bool isReconnect = !string.IsNullOrEmpty(_lastConnectedPort);
                Disconnect(userInitiated: false, silent: true);

                _serialPort = new SerialPort(port, BaudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    NewLine = "\n",
                    ReadTimeout = 200,
                    WriteTimeout = 200,
                    Encoding = Encoding.ASCII,
                    DtrEnable = true,
                    RtsEnable = false
                };

                _serialPort.Open();
                _lastConnectedPort = port;
                SelectPort(port);
                SavePreferredPort(port);

                btnConnect.Text = "Disconnect";
                SetStatus("Connected (" + port + ", " + BaudRate + " baud)");
                AppendLine(auto && isReconnect ? "Reconnected to " + port : "Connected to " + port);
            }
            catch (Exception ex)
            {
                AppendLine("Connection failed: " + ex.Message);
                Disconnect(userInitiated: false, silent: true);
                UpdateDisconnectedState();
            }
            finally
            {
                _isTryingToConnect = false;
            }
        }

        private string FindArduinoPort()
        {
            string[] availablePorts = GetAvailablePorts();

            string selected = cmbPorts.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected) && ContainsPort(availablePorts, selected))
                return selected;

            string preferred = Properties.Settings.Default.LastComPort;
            if (!string.IsNullOrEmpty(preferred) && ContainsPort(availablePorts, preferred))
                return preferred;

            if (!string.IsNullOrEmpty(_lastConnectedPort) && ContainsPort(availablePorts, _lastConnectedPort))
                return _lastConnectedPort;

            var detected = new List<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
                using (ManagementObjectCollection devices = searcher.Get())
                {
                    foreach (ManagementObject device in devices)
                    {
                        using (device)
                        {
                            string name = Convert.ToString(device["Name"]);
                            if (!LooksLikeArduinoAdapter(name))
                                continue;

                            Match match = Regex.Match(name, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
                            if (match.Success && ContainsPort(availablePorts, match.Groups[1].Value))
                                detected.Add(match.Groups[1].Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDetectionMessage("Windows device detection failed: " + ex.Message);
            }

            string detectedPort = detected
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(PortSortKey)
                .FirstOrDefault();

            if (detectedPort != null)
                return detectedPort;

            return availablePorts.Length == 1 ? availablePorts[0] : null;
        }

        private static bool LooksLikeArduinoAdapter(string deviceName)
        {
            string name = (deviceName ?? string.Empty).ToLowerInvariant();
            return name.Contains("arduino") ||
                   name.Contains("ch340") ||
                   name.Contains("ch341") ||
                   name.Contains("ch910") ||
                   name.Contains("cp210") ||
                   name.Contains("ftdi") ||
                   name.Contains("usb serial") ||
                   name.Contains("usb-serial") ||
                   name.Contains("wch");
        }

        private void RefreshPortList()
        {
            string selected = cmbPorts.SelectedItem as string;
            string preferred = selected ?? Properties.Settings.Default.LastComPort;
            string[] ports = GetAvailablePorts();

            _updatingPortList = true;
            try
            {
                cmbPorts.BeginUpdate();
                cmbPorts.Items.Clear();
                cmbPorts.Items.AddRange(ports);

                if (!string.IsNullOrEmpty(preferred) && ContainsPort(ports, preferred))
                    cmbPorts.SelectedItem = ports.First(p => string.Equals(p, preferred, StringComparison.OrdinalIgnoreCase));
                else if (ports.Length == 1)
                    cmbPorts.SelectedIndex = 0;
                else
                    cmbPorts.SelectedIndex = -1;
            }
            finally
            {
                cmbPorts.EndUpdate();
                _updatingPortList = false;
            }

            AppendLine(ports.Length == 0
                ? "No serial ports found."
                : "Serial ports: " + string.Join(", ", ports));
        }

        private void SelectPort(string port)
        {
            if (!cmbPorts.Items.Contains(port))
                RefreshPortList();

            if (cmbPorts.Items.Contains(port))
                cmbPorts.SelectedItem = port;
        }

        private void SavePreferredPort(string port)
        {
            try
            {
                Properties.Settings.Default.LastComPort = port ?? string.Empty;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex) when (ex is System.Configuration.ConfigurationErrorsException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is System.IO.IOException)
            {
                LogDetectionMessage("Could not save preferred port: " + ex.Message);
            }
        }

        private void LogDetectionFailure(bool auto)
        {
            string message = GetAvailablePorts().Length > 1
                ? "Arduino not identified. Select its COM port and click Connect."
                : "Arduino not detected.";

            LogDetectionMessage(message);
            if (!auto)
                MessageBox.Show(this, message, "Arduino Serial Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LogDetectionMessage(string message)
        {
            if (DateTime.Now - _lastDetectionLog < _detectionLogCooldown)
                return;

            _lastDetectionLog = DateTime.Now;
            AppendLine(message);
        }

        private void UpdateDisconnectedState()
        {
            if (_autoReconnectEnabled)
            {
                SetStatus("Reconnecting...");
                btnConnect.Text = "Connecting...";
            }
            else
            {
                SetStatus("Disconnected");
                btnConnect.Text = "Connect";
            }
        }

        private void Disconnect(bool userInitiated, bool silent = false)
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is System.IO.IOException)
                {
                    if (!silent)
                        AppendLine("Disconnect warning: " + ex.Message);
                }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }

            _pendingCarriageReturn = false;
            if (!silent)
                AppendLine(userInitiated ? "Disconnected." : "Disconnected (automatic)." );
        }

        private bool IsConnected()
        {
            return _serialPort != null && _serialPort.IsOpen;
        }

        private string NormalizeLineEndings(string text)
        {
            if (_pendingCarriageReturn)
            {
                text = "\r" + text;
                _pendingCarriageReturn = false;
            }

            if (text.EndsWith("\r", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 1);
                _pendingCarriageReturn = true;
            }

            return text.Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", Environment.NewLine);
        }

        private void SetStatus(string status)
        {
            lblStatus.Text = "Status: " + status;
        }

        private void AppendLine(string text)
        {
            AppendRaw(DateTime.Now.ToString("HH:mm:ss") + " " + text + Environment.NewLine);
        }

        private void AppendRaw(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int excess = txtOutput.TextLength + text.Length - MaximumLogCharacters;
            if (excess > 0)
            {
                int removeCount = Math.Max(excess, MaximumLogCharacters / 10);
                removeCount = Math.Min(removeCount, txtOutput.TextLength);
                int nextLine = txtOutput.Text.IndexOf('\n', Math.Max(0, removeCount - 1));
                if (nextLine >= 0)
                    removeCount = nextLine + 1;

                txtOutput.Select(0, removeCount);
                txtOutput.SelectedText = string.Empty;
            }

            txtOutput.AppendText(text);
        }

        private static bool ContainsPort(IEnumerable<string> ports, string port)
        {
            return ports.Any(p => string.Equals(p, port, StringComparison.OrdinalIgnoreCase));
        }

        private string[] GetAvailablePorts()
        {
            try
            {
                return SerialPort.GetPortNames().OrderBy(PortSortKey).ToArray();
            }
            catch (Exception ex)
            {
                LogDetectionMessage("Could not enumerate serial ports: " + ex.Message);
                return new string[0];
            }
        }

        private static int PortSortKey(string port)
        {
            Match match = Regex.Match(port ?? string.Empty, @"\d+");
            int number;
            return match.Success && int.TryParse(match.Value, out number) ? number : int.MaxValue;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _autoReconnectEnabled = false;
            _timer.Stop();
            Disconnect(userInitiated: true, silent: true);
            base.OnFormClosing(e);
        }
    }
}
