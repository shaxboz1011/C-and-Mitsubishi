using HslCommunication;
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
        MelsecFxSerial FXPlc = new MelsecFxSerial();
        public PictureBox[] inputPbs;
        public PictureBox[] outputPbs;

        private Thread plcThread;
        private bool threadRunning = true;


        // PLC Connect
        public void PLC_Connect(string comPort)
        {
            FXPlc.SerialPortInni(sp =>
            {
                sp.PortName = comPort;
                sp.BaudRate = 9600;
                sp.DataBits = 7;
                sp.StopBits = System.IO.Ports.StopBits.One;
                sp.Parity = System.IO.Ports.Parity.Even;
            });

            FXPlc.Open();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void MakeCircular(PictureBox pic)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, pic.Width, pic.Height);
            pic.Region = new Region(path);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            threadRunning = false;

            if (plcThread != null && plcThread.IsAlive)
            {
                // Wait up to 1 second for thread to stop
                if (!plcThread.Join(1000))
                {
                    // If still not done, abort (not recommended, only fallback)
                    plcThread.Abort(); // ⚠️ Use only if absolutely necessary
                }
            }// wait for thread to finish
            FXPlc.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            inputPbs = new PictureBox[] { x00_pb, x01_pb, x02_pb, x03_pb, x04_pb, x05_pb, x06_pb, x07_pb };
            outputPbs = new PictureBox[] { y00_pb, y01_pb, y02_pb, y03_pb, y04_pb, y05_pb, y06_pb, y07_pb, laserLeft_pb, laserRight_pb };

            foreach (var pb in inputPbs.Concat(outputPbs))
                MakeCircular(pb);

            string[] ports = SerialPort.GetPortNames();
            comPorts_cb.Items.Clear();
            comPorts_cb.Items.AddRange(ports);

            if (ports.Length > 0)
                comPorts_cb.SelectedIndex = 0;
        }

        private void connect_btn_Click(object sender, EventArgs e)
        {
            PLC_Connect(comPorts_cb.SelectedItem.ToString());

            string plcType = FXPlc.GetType().Name;
            MessageBox.Show("Connected PLC Type: " + plcType);

            // Start background thread
            plcThread = new Thread(ReadPlcLoop);
            plcThread.IsBackground = true;
            plcThread.Start();
        }

        private void disconnect_btn_Click(object sender, EventArgs e)
        {
            threadRunning = false;

            if (plcThread != null && plcThread.IsAlive)
            {
                // Wait up to 1 second for thread to stop
                if (!plcThread.Join(1000))
                {
                    // If still not done, abort (not recommended, only fallback)
                    plcThread.Abort(); // ⚠️ Use only if absolutely necessary
                }
            }
            FXPlc.Close();
        }
        private bool[] readDataPlc(string[] s)
        {
            bool[] pin = new bool[s.Length];
            for(int i=0; i<s.Length; i++)
            {
                pin[i] = FXPlc.ReadBool(s[i]).Content;
            }

            return pin;
        }

        private void ReadPlcLoop()
        {
            try
            {
                while (threadRunning)
                {
                    string[] input = new string[] { "X00", "X01", "X02", "X03", "X04", "X05", "X06", "X07" };
                    string[] output = new string[] { "Y00", "Y01", "Y02", "Y03", "Y04", "Y05" };

                    bool[] inputs = readDataPlc(input);
                    bool[] outputs = readDataPlc(output);


                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke(new Action(() =>
                        {
                            laserRight_pb.BackColor = FXPlc.ReadBool("X05").Content ? Color.Red : Color.Green;
                            laserLeft_pb.BackColor = FXPlc.ReadBool("X04").Content ? Color.Red : Color.Green;

                            emergency_lb.Text = FXPlc.ReadBool("X03").Content ? "False" : "True";
                            key_lb.Text = FXPlc.ReadBool("X00").Content ? "True" : "False";

                            for (int i = 0; i < input.Length; i++)
                                inputPbs[i].BackColor = inputs[i] ? Color.Green : Color.Red;

                            for (int i = 0; i < output.Length; i++)
                                outputPbs[i].BackColor = outputs[i] ? Color.Green : Color.Red;
                        }));

                    }
                }
            }
            catch (Exception ex)
            {
                // Log or show error if needed
                MessageBox.Show("Thread Error: " + ex.Message);
            }

        }

        private void left_btn_Click(object sender, EventArgs e)
        {
            // Left 
            bool current = FXPlc.ReadBool("M16").Content;
            FXPlc.Write("M16", !current);
        }

        private void stop_btn_Click(object sender, EventArgs e)
        {
            // Right
            bool right = FXPlc.ReadBool("M32").Content;

            // Left 
            bool left = FXPlc.ReadBool("M16").Content;

            if(right)
            {
                FXPlc.Write("M32", !right);
            }

            if (left)
            {
                FXPlc.Write("M16", !left);
            }
        }

        private void right_btn_Click(object sender, EventArgs e)
        {
            // Right
            bool current = FXPlc.ReadBool("M32").Content;
            FXPlc.Write("M32", !current);
        }

        private void red_btn_Click(object sender, EventArgs e)
        {
            // Red Lamp
            bool current = FXPlc.ReadBool("M48").Content;
            FXPlc.Write("M48", !current);
        }

        private void green_btn_Click(object sender, EventArgs e)
        {
            // Green Lamp
            bool green = FXPlc.ReadBool("Y00").Content;
            FXPlc.Write("Y00", !green);

            // Cooler
            bool cooler = FXPlc.ReadBool("Y04").Content;
            FXPlc.Write("Y04", !cooler);
        }
    }
}
