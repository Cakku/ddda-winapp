using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace dda_winapp
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

		[DllImportAttribute("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImportAttribute("user32.dll")]
		public static extern bool ReleaseCapture();

		public Form1()
        {
            InitializeComponent();
        }

        const int PROCESS_WM_READ = 0x0010;
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;

		private Process process;
        private Int32 memAddress;
		private int counter;
		private DateTime timer;


		private void Title_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}

		private void button3_Click(object sender, EventArgs e)
        {
            textBox3.Text = string.Empty;
        }
        private void button1_Click(object sender, EventArgs e)
        {
			var processList = Process.GetProcesses();
			comboBox1.Items.AddRange(processList);
			comboBox1.DisplayMember = "ProcessName";
			textBox3.AppendText($"got process list with {processList.Count()} entries");

		}

        private void button2_Click(object sender, EventArgs e)
        {
            textBox3.Text = Environment.NewLine + "Doing stuffs..";
			if (string.IsNullOrEmpty(textBox2.Text)) {
				memAddress = Convert.ToInt32("123490CC", 16);
				textBox3.Text += Environment.NewLine + "using default address 123490CC !";
			}
			else if (!Int32.TryParse(textBox2.Text, out memAddress))
            {
				memAddress = Convert.ToInt32(textBox2.Text, 16);
				textBox3.Text += Environment.NewLine + "\nfailed to parse address 123490CC !";
            }
			if (comboBox1.SelectedItem == null)
				process = Process.GetProcessesByName("DDDA")[0];
			else
				process = (Process)comboBox1.SelectedItem;
			ReadingsMemory();
        }


        private void ReadingsMemory()
        {

			
            if(process == null)
            {
                textBox3.Text += Environment.NewLine+"process null";
                return;
            }
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);


            textBox3.Text += $"{Environment.NewLine}process: {process.ProcessName} {process.Id} address: {memAddress}" ;


			int bytesRead = 0;
            byte[] buffer = new byte[4];

            ReadProcessMemory((int)processHandle, memAddress, buffer, buffer.Length, ref bytesRead);
			int intvalue = (int)BitConverter.ToSingle(buffer, 0);

			textBox3.AppendText($"{Environment.NewLine}read {bytesRead} value: {intvalue}");
			
			var listener = new ValueChange(16, ReadValue);
			listener.PropertyChanged += WriteLog;
		}

		private int ReadValue() {
			IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
			int bytesRead = 0;
			byte[] buffer = new byte[4];

			ReadProcessMemory((int)processHandle, memAddress, buffer, buffer.Length, ref bytesRead);
			return (int)BitConverter.ToSingle(buffer, 0);
		}

		private void WriteLog(object sender, PropertyChangedEventArgs e) {
			textBox3.AppendText(Environment.NewLine + e.PropertyName);
			counter += int.Parse(e.PropertyName);
			textBox1.Text = (DateTime.Now - timer).ToString() + Environment.NewLine + counter;
		}

		private void button5_Click(object sender, EventArgs e)
		{
			counter = 0;
			timer = DateTime.Now;
			textBox1.BringToFront();
		}
	}

	public class ValueChange : INotifyPropertyChanged
	{
		private int _value;
		private readonly System.Timers.Timer _timer;
		private readonly Func<int> _getValue;

		public ValueChange(double pollingInterval, Func<int> getValue)
		{
			_getValue = getValue;
			_value = _getValue();

			_timer = new System.Timers.Timer { AutoReset = false, Interval = pollingInterval };
			_timer.Elapsed += TimerElapsed;
			_timer.Start();
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			var newValue = _getValue();

			if (_value != newValue)
			{
				int diff = newValue - _value;
				_value = newValue;
				RaisePropertyChanged(diff.ToString());
			}

			_timer.Start();

		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void RaisePropertyChanged(string caller)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(caller));
			}
		}
	}
}
