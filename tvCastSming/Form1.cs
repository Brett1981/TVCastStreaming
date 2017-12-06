using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Management;

namespace tvCastSming
{
    public partial class Form1 : Form
    {
        // Chrome
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;

        // Mouse Event
        [DllImport("user32.dll")]
        private static extern void mouse_event(UInt32 dwFlags, UInt32 dx, UInt32 dy, UInt32 dwData, IntPtr dwExtraInfo);
        private const int WM_LBUTTONDOWN = 0x201;   //Left mousebutton down
        private const int WM_LBUTTONUP = 0x202;     //Left mousebutton up

        // CPU Usage, RAM Usage
        private Thread addDataRunner;
        private Thread addRamDataRunner;
        public delegate void AddDataDelegate();
        public AddDataDelegate addDataDel;
        public delegate void AddRamDataDelegate();
        public AddRamDataDelegate addRamDataDel;
        PerformanceCounter p1;
        PerformanceCounter p2;
        private int MAXIMUM_RAM_SIZE;

        private int cpu_percent = 0;
        private int ram_percent = 0;

        // Open Chrome Windows Thread
        private Thread addChromeWindow;

        // Calc Average CPU Usage
        int nAverCPU;
        int nCalcAverCPU = 0;
        Queue<int> nOldCPU = new Queue<int>();
        int nCalcTickCount = 0;

        public Form1()
        {
            InitializeComponent();

            InitSetting();
        }

        private void InitSetting()
        {
            this.Load += Form1_Load;
            this.FormClosed += Form1_FormClosed;

            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);
            ManagementObjectCollection queryCollection = searcher.Get();
            ulong memory = 0;
            foreach (ManagementObject item in queryCollection)
            {
                memory = ulong.Parse(item["TotalVisibleMemorySize"].ToString());
            }
            MAXIMUM_RAM_SIZE = (int)(memory / 1024);


            radioButton3.Hide();
            numericUpDown1.Value = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;

            if (checkBox1.Checked == true
                && numericUpDown1.Value > 0)
            {
                StartSettingProcess();
            }
            else
            {
                StartProcess();
            }
        }

        private void StartSettingProcess()
        {
            if (radioButton5.Checked)
                numericUpDown1.Value -= 1;
            CallBrowser(textBox1.Text, false);

            Form2 attention = new Form2(this);
            attention.Show();
        }

        public void StartProcess()
        {
            if (radioButton3.Checked)
            {
            }
            else if (radioButton4.Checked)
            {
                ThreadStart auto = new ThreadStart(autoCount);
                addChromeWindow = new Thread(auto);
                addChromeWindow.Start();
            }
            else if (radioButton5.Checked)
            {
                ThreadStart manual = new ThreadStart(manualCount);
                addChromeWindow = new Thread(manual);
                addChromeWindow.Start();
            }
            else
            {
                // error check
            }
        }

        private void autoCount()
        {
            while (nAverCPU < int.Parse(textBox3.Text))
            {
                CallBrowser(textBox1.Text);
            }

            this.BeginInvoke(new MethodInvoker(() =>
            {
                button1.Enabled = true;
            }));
            if (addChromeWindow != null)
                addChromeWindow.Abort();
        }

        private void manualCount()
        {
            while (numericUpDown1.Value > 0)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    this.numericUpDown1.Value--;
                }));
                CallBrowser(textBox1.Text);
            }

            this.BeginInvoke(new MethodInvoker(() =>
            {
                button1.Enabled = true;
            }));
            if (addChromeWindow != null)
                addChromeWindow.Abort();
        }

        private void CallBrowser(string url, bool bHide = true)
        {
            ProcessStartInfo info = new ProcessStartInfo("chrome", "--incognito --new-window " + url);
            Process ps = Process.Start(info);

            if (bHide)
                CallHideMainProcess(int.Parse(textBox2.Text));
        }

        public void CallHideMainProcess(int timer)
        {
            Thread.Sleep(timer);

            Process proc = null;
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals("chrome"))
                {
                    proc = process;

                    IntPtr hWnd = proc.MainWindowHandle;
                    if (!hWnd.Equals(IntPtr.Zero))
                    {
                        ShowWindowAsync(hWnd, SW_SHOWMINIMIZED);
                    }
                }
            }
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            p1 = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            addDataRunner.Start();

            p2 = new PerformanceCounter("Memory", "Available MBytes");
            addRamDataRunner.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (radioButton5 != null)
                radioButton5.Checked = true;
            if (checkBox1 != null)
                checkBox1.Checked = true;

            toolTip1.SetToolTip(checkBox1, "첫 번째 창에서 볼륨, 해상도, 반복 설정등을 해주세요.");

            ThreadStart addDataThreadStart = new ThreadStart(AddDataThreadLoop);
            addDataRunner = new Thread(addDataThreadStart);
            addDataDel += new AddDataDelegate(AddData);
            progressBar1.Maximum = 100;

            ThreadStart addRamDataThreadStart = new ThreadStart(AddRamDataThreadLoop);
            addRamDataRunner = new Thread(addRamDataThreadStart);
            addRamDataDel += new AddRamDataDelegate(AddRamData);
            progressBar2.Maximum = 100;

            btnShow_Click(null, null);
        }

        private void AddDataThreadLoop()
        {
            while (true)
            {
                progressBar1.Invoke(addDataDel);
                Thread.Sleep(500);
            }
        }

        private void AddRamDataThreadLoop()
        {
            while (true)
            {
                progressBar2.Invoke(addRamDataDel);
                Thread.Sleep(500);
            }
        }

        public void AddData()
        {
            cpu_percent = (int)p1.NextValue();
            progressBar1.Value = cpu_percent;
            lbUsage.Text = cpu_percent.ToString() + "%";

            if(nCalcTickCount < 5)
                nCalcTickCount++;
            else
                nCalcAverCPU -= nOldCPU.Dequeue();
            nCalcAverCPU += cpu_percent;
            nOldCPU.Enqueue(cpu_percent);
            nAverCPU = nCalcAverCPU / nCalcTickCount;
        }

        public void AddRamData()
        {
            ram_percent = (int)((MAXIMUM_RAM_SIZE - p2.NextValue()) / (float)MAXIMUM_RAM_SIZE * 100.0f);
            progressBar2.Value = ram_percent;
            lbRamUsage.Text = ram_percent.ToString() + "%";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (addDataRunner != null)
                addDataRunner.Abort();
            if (addRamDataRunner != null)
                addRamDataRunner.Abort();
            if (addChromeWindow != null)
                addChromeWindow.Abort();
        }
    }
}
