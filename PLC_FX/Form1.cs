using HslCommunication.Profinet.Melsec;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PLC_FX
{
    public partial class Form1 : Form
    {
        // Mitsubishi FX programming-port serial settings (fixed on the FX3S).
        private const int BaudRate = 9600;
        private const int DataBits = 7;
        private const Parity SerialParity = Parity.Even;
        private const StopBits SerialStopBits = StopBits.One;

        // How long to wait between PLC polls. Keeps the serial line and the CPU
        // from being hammered by a tight read loop.
        private const int PollIntervalMs = 100;

        // Colours used for the round status indicators.
        private static readonly Color OnColor = Color.FromArgb(46, 204, 113);   // green
        private static readonly Color OffColor = Color.FromArgb(231, 76, 60);   // red
        private static readonly Color IdleColor = Color.FromArgb(189, 195, 199); // grey (disconnected)

        private readonly MelsecFxSerial _plc = new MelsecFxSerial();

        private PictureBox[] _inputPbs;
        private PictureBox[] _outputPbs;

        // The inputs the PLC loop reads, in the same order as _inputPbs.
        private static readonly string[] InputAddresses =
            { "X00", "X01", "X02", "X03", "X04", "X05", "X06", "X07" };

        // The outputs the PLC loop reads (FX3S-14MT has Y00..Y05).
        private static readonly string[] OutputAddresses =
            { "Y00", "Y01", "Y02", "Y03", "Y04", "Y05" };

        private Thread _plcThread;
        private volatile bool _threadRunning;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _inputPbs = new[] { x00_pb, x01_pb, x02_pb, x03_pb, x04_pb, x05_pb, x06_pb, x07_pb };
            _outputPbs = new[] { y00_pb, y01_pb, y02_pb, y03_pb, y04_pb, y05_pb, y06_pb, y07_pb, laserLeft_pb, laserRight_pb };

            foreach (var pb in _inputPbs.Concat(_outputPbs))
                MakeCircular(pb);

            RefreshPortList();
            SetIndicatorsIdle();
            SetConnectedState(false);
        }

        /// <summary>Opens the serial connection to the PLC.</summary>
        private bool PlcConnect(string comPort)
        {
            _plc.SerialPortInni(sp =>
            {
                sp.PortName = comPort;
                sp.BaudRate = BaudRate;
                sp.DataBits = DataBits;
                sp.StopBits = SerialStopBits;
                sp.Parity = SerialParity;
            });

            var open = _plc.Open();
            return open.IsSuccess;
        }

        private void connect_btn_Click(object sender, EventArgs e)
        {
            if (comPorts_cb.SelectedItem == null)
            {
                MessageBox.Show(this, "Please select a COM port first.", "No port selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_threadRunning)
                return; // already connected

            string comPort = comPorts_cb.SelectedItem.ToString();

            try
            {
                if (!PlcConnect(comPort))
                {
                    MessageBox.Show(this, "Could not open " + comPort + ".\n" +
                        "Check the cable, the COM port and that no other program is using it.",
                        "Connection failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Connection error: " + ex.Message,
                    "Connection failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetConnectedState(true);

            _threadRunning = true;
            _plcThread = new Thread(ReadPlcLoop) { IsBackground = true, Name = "PLC poll" };
            _plcThread.Start();
        }

        private void disconnect_btn_Click(object sender, EventArgs e)
        {
            StopPolling();
            _plc.Close();
            SetIndicatorsIdle();
            SetConnectedState(false);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopPolling();
            _plc.Close();
        }

        private void StopPolling()
        {
            _threadRunning = false;

            if (_plcThread != null && _plcThread.IsAlive)
            {
                // Give the background thread a moment to leave its loop cleanly.
                // It is a background thread, so it will not keep the app alive
                // even if it is mid-read when we give up waiting.
                _plcThread.Join(1000);
            }

            _plcThread = null;
        }

        private void ReadPlcLoop()
        {
            try
            {
                while (_threadRunning)
                {
                    bool[] inputs = ReadBools(InputAddresses);
                    bool[] outputs = ReadBools(OutputAddresses);

                    if (inputs != null && outputs != null && IsHandleCreated && !IsDisposed)
                    {
                        BeginInvoke(new Action(() => UpdateIndicators(inputs, outputs)));
                    }

                    Thread.Sleep(PollIntervalMs);
                }
            }
            catch (Exception ex)
            {
                ShowErrorOnUiThread("PLC read error: " + ex.Message);
            }
        }

        /// <summary>Reads a list of bit addresses. Returns null if any read fails.</summary>
        private bool[] ReadBools(string[] addresses)
        {
            var values = new bool[addresses.Length];
            for (int i = 0; i < addresses.Length; i++)
            {
                var result = _plc.ReadBool(addresses[i]);
                if (!result.IsSuccess)
                    return null;
                values[i] = result.Content;
            }
            return values;
        }

        private void UpdateIndicators(bool[] inputs, bool[] outputs)
        {
            for (int i = 0; i < inputs.Length; i++)
                _inputPbs[i].BackColor = inputs[i] ? OnColor : OffColor;

            for (int i = 0; i < outputs.Length; i++)
                _outputPbs[i].BackColor = outputs[i] ? OnColor : OffColor;

            // Laser detectors: lit red when the beam is broken (X04 = left, X05 = right).
            laserLeft_pb.BackColor = inputs[4] ? OffColor : OnColor;
            laserRight_pb.BackColor = inputs[5] ? OffColor : OnColor;

            // X03 is a normally-closed emergency stop, X00 is the key switch.
            emergency_lb.Text = inputs[3] ? "False" : "True";
            key_lb.Text = inputs[0] ? "True" : "False";
        }

        // --- Operator controls -------------------------------------------------

        private void left_btn_Click(object sender, EventArgs e) => Toggle("M16"); // conveyor left

        private void right_btn_Click(object sender, EventArgs e) => Toggle("M32"); // conveyor right

        private void stop_btn_Click(object sender, EventArgs e)
        {
            // Stop the conveyor by clearing both direction coils if they are set.
            if (ReadBit("M32")) _plc.Write("M32", false);
            if (ReadBit("M16")) _plc.Write("M16", false);
        }

        private void red_btn_Click(object sender, EventArgs e) => Toggle("M48"); // red lamp

        private void green_btn_Click(object sender, EventArgs e)
        {
            Toggle("Y00"); // green lamp
            Toggle("Y04"); // cooler
        }

        /// <summary>Flips a single bit (read-modify-write).</summary>
        private void Toggle(string address)
        {
            if (!_threadRunning)
            {
                MessageBox.Show(this, "Connect to the PLC first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _plc.Write(address, !ReadBit(address));
        }

        private bool ReadBit(string address) => _plc.ReadBool(address).Content;

        // --- UI helpers --------------------------------------------------------

        private void RefreshPortList()
        {
            string[] ports = SerialPort.GetPortNames();
            comPorts_cb.Items.Clear();
            comPorts_cb.Items.AddRange(ports);
            if (ports.Length > 0)
                comPorts_cb.SelectedIndex = 0;
        }

        private void SetConnectedState(bool connected)
        {
            connect_btn.Enabled = !connected;
            disconnect_btn.Enabled = connected;
            comPorts_cb.Enabled = !connected;
        }

        private void SetIndicatorsIdle()
        {
            foreach (var pb in _inputPbs.Concat(_outputPbs))
                pb.BackColor = IdleColor;

            emergency_lb.Text = "-";
            key_lb.Text = "-";
        }

        private void ShowErrorOnUiThread(string message)
        {
            if (!IsHandleCreated || IsDisposed)
                return;
            BeginInvoke(new Action(() =>
                MessageBox.Show(this, message, "PLC error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
        }

        private static void MakeCircular(PictureBox pic)
        {
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, pic.Width, pic.Height);
            pic.Region = new Region(path);
        }
    }
}
