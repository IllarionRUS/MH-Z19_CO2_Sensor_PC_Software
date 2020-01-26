using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms.DataVisualization.Charting;


namespace graph1
{
	public partial class MainForm : Form
	{
		byte[] rx_data = new byte[10];
		
		int main_time_cnt = 0;
		int packet_cnt = 0;
		
		int co2_value = 0;
		int temperature = 0;
		string[] ser_ports;
		
		public MainForm()
		{
			InitializeComponent();
			chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
			chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
			chart1.ChartAreas[0].AxisX.LabelStyle.Format = "hh:hh";
			chart1.Series[0].XValueType = ChartValueType.Time;
			//Нормативы тут - https://olegon.ru/showthread.php?t=29134
			toolTip1.SetToolTip(label1, "Нормативы концентрации CO2 (ppm)\r\n" +
				"Строительные нормативы (ГОСТ 30494-2011), мнение физиологов, согласно санитарно-гигиеническим исследованиям.\r\n" +
	"Атмосферный воздух 300 – 400\r\n—\r\nИдеальное самочувствие и бодрость\r\n " +
 "");
		}
		
		void Button1Click(object sender, EventArgs e)
		{
			int index = comboBox1.SelectedIndex;
			if (serialPort1.IsOpen == false)
			{
				if (index > -1)
				{
					serialPort1.PortName = ser_ports[index];
					serialPort1.Open();
					button1.Text = "CLOSE";
				}
			}
			else
			{
				serialPort1.Close();
				button1.Text = "OPEN";
			}
		}

		public static void IncTime(int T, out int H, out int M, out int S)
		{
			H = 0;
			M = 0;
			S = 0;

			if (T >= 3600)
			{
				H = (T - (T % 3600)) / 3600;
				T = T - H * 3600;
			}
			if (T >= 60)
			{
				M = (T - (T % 60)) / 60;
				T = T - M * 60;
			}
			S = T;
		}

		void Timer1Tick(object sender, EventArgs e)
		{
			main_time_cnt++;
			//if (main_time_cnt > 86399) 
			//{
			//	toolStripStatusLabel1.Text = "TIME: " + TimeSpan.FromSeconds(main_time_cnt).TotalDays.ToString() + " days"; }
			//if (main_time_cnt > 3599)
			//{
			//	toolStripStatusLabel1.Text = "TIME: " + TimeSpan.FromSeconds(main_time_cnt).TotalHours.ToString() + " hours";
			//}
			//if (main_time_cnt > 59)
			//{
			//	toolStripStatusLabel1.Text = "TIME: " + TimeSpan.FromSeconds(main_time_cnt).TotalMinutes.ToString() + " minutes";
			//}
			//else

			int i = 0;
			List<DateTime> TimeList = new List<DateTime>();

			toolStripStatusLabel1.Text = "TIME: " + main_time_cnt.ToString() + " sec";
			if (((main_time_cnt%10) == 0) && (serialPort1.IsOpen == true))
			{
				//request data
				request_value();
				System.Threading.Thread.Sleep(100);
				if (serialPort1.BytesToRead == 9)
				{
					serialPort1.Read(rx_data,0,9);
					packet_cnt++;
					process_data(rx_data);
					//chart1.Series["Series1"].Points.Add(co2_value);
					string now = DateTime.Now.ToLongTimeString();
					//TimeList.Add(now);
					//chart1.Series["Series1"].Points.
					chart1.Series["Series1"].Points.AddXY(now, co2_value);
					chart1.Series["Series2"].Points.AddXY(now, temperature*10);
					i += 2;
				}
				else
				{
					serialPort1.ReadExisting();
				}
			}
			toolStripStatusLabel2.Text = "PACKET: " + packet_cnt.ToString();
			label1.Text = "CO2: " + co2_value.ToString() + ", TEMP: " + temperature.ToString();
			
			if (serialPort1.IsOpen == true)
			{
				toolStripProgressBar1.Value = main_time_cnt%10;
			}
			else
			{
				toolStripProgressBar1.Value = 0;
			}
			
		}
		
		void process_data(byte[] data)
		{
			int result = -1;
			int i;
			
			string tmp_str = "";
			
			
			if  (data[1] == 0x86)
			{
				result = data[2]*256 + data[3];
				
				co2_value = result;
				temperature = data[4] - 40;
			}
			
			for (i=2;i<8;i++)
			{
				tmp_str+= "BYTE" + i.ToString() +": " +data[i].ToString() +"\r\n" ;
			}
			
			lblRawData.Text = tmp_str + "TEMP - " + temperature;
		}
		
		void request_value()
		{
			byte[] data_to_send = new byte[9];
			data_to_send[0] = 0xFF;
			data_to_send[1] = 0x01;
			data_to_send[2] = 0x86;
			data_to_send[8] = 0x79;
			serialPort1.Write(data_to_send,0,9);
		}
		
		void MainFormLoad(object sender, EventArgs e)
		{
			ser_ports = SerialPort.GetPortNames();
			foreach(string port in ser_ports)
            {
                comboBox1.Items.Add(port);
            }
		}

		private void MainForm_ResizeEnd(object sender, EventArgs e)
		{
			chart1.ChartAreas[0].RecalculateAxesScale();
		}
	}
}
