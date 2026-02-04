using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    public partial class Form2 : Form
    {
        private int usartComNo;
        private int  usartBaudrate;
        private int usartbitPerD;
        private int usartParity;
        private int usartStopbits;
        private int usartFrameSize;
        private int usartFrameInterval;

        private int[] uartParamBuf;
        private const int NuartParam = 7;

        private string[] Parityoption = { "None", "Odd", "Even", "Mark", "Space" };
        private string[] Stopbitsopt = {"0", "1", "2", "1.5"};

        private void updateUartParambuf()
        {
            uartParamBuf[0] = usartComNo;  
            uartParamBuf[1] = usartBaudrate;   
            uartParamBuf[2] = usartbitPerD;
            uartParamBuf[3] = usartParity;
            uartParamBuf[4] = usartStopbits;
            uartParamBuf[5] = usartFrameSize;
            uartParamBuf[6] = usartFrameInterval;
        }

        public delegate void saveParam(int[] param);
        private saveParam saveParamUart;

        public Form2(saveParam p)
        {
            this.saveParamUart = p;
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            uartParamBuf = new int[NuartParam];
            usartComNo = mainForm.usartParamBuf[0];
            usartBaudrate = mainForm.usartParamBuf[1];
            usartbitPerD = mainForm.usartParamBuf[2];
            usartParity = mainForm.usartParamBuf[3];
            usartStopbits = mainForm.usartParamBuf[4];
            usartFrameSize = mainForm.usartParamBuf[5];
            usartFrameInterval = mainForm.usartParamBuf[6];

            form2LoadConfig();

            comboBox1.SelectedIndex = comboBox1.Items.IndexOf("COM"+ usartComNo.ToString());
            if (comboBox1.SelectedIndex == -1) comboBox1.Text = "COM" + usartComNo.ToString();
            comboBox2.SelectedIndex = comboBox2.Items.IndexOf(usartBaudrate.ToString());
            if (comboBox2.SelectedIndex == -1) comboBox2.Text = usartBaudrate.ToString();
            comboBox3.SelectedIndex = comboBox3.Items.IndexOf(usartbitPerD.ToString());
            comboBox4.SelectedIndex = comboBox4.Items.IndexOf(Parityoption[usartParity]);
            comboBox5.SelectedIndex = comboBox5.Items.IndexOf(Stopbitsopt[usartStopbits]);

            comboBox6.SelectedIndex = comboBox6.Items.IndexOf(usartFrameSize.ToString());
            if (comboBox6.SelectedIndex == -1) comboBox6.Text = usartFrameSize.ToString();
            comboBox7.SelectedIndex = comboBox7.Items.IndexOf(usartFrameInterval.ToString());
            if (comboBox7.SelectedIndex == -1) comboBox7.Text = usartFrameInterval.ToString();

            

            updateUartParambuf();

        }


        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            form2configSave();
            this.saveParamUart(uartParamBuf);
            mainForm.COMsettingWindowOn = false;
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }




        public static bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }

        public static bool IsInt(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*$");
        }
        public static bool IsUnsign(string value)
        {
            return Regex.IsMatch(value, @"^\d*[.]?\d*$");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text.Length <= 0) return;
            usartComNo = comboBox1.SelectedIndex;
            updateUartParambuf();
        }

        private void comboBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (comboBox1.Text.Length <= 0) return;
            comboBox1.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                string str = comboBox1.Text;
                if (str.Length < 3) return;
                if (str.Substring(0, 3) != "COM") return;
                str = str.Substring(3, str.Length - 3);
                if (str == null) return;
                if (IsInt(str))
                {
                    Int32 num = Convert.ToInt32(str);
                    if (num < 256 && num >= 0)
                    {
                        usartComNo = num;
                        updateUartParambuf();
                        comboBox1.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.Text.Length <= 0) return;
            if (IsInt(comboBox2.Text)) { 
                usartBaudrate = Convert.ToInt32(comboBox2.Text);
                updateUartParambuf();
            }
        }

        private void comboBox2_KeyUp(object sender, KeyEventArgs e)
        {
            comboBox2.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                if (comboBox2.Text.Length <= 0) return;
                if (IsInt(comboBox2.Text))
                {
                    usartBaudrate = Convert.ToInt32(comboBox2.Text);
                    updateUartParambuf();
                    comboBox2.ForeColor = Color.Black;
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsInt(comboBox3.Text))
            {
                usartbitPerD = Convert.ToInt32(comboBox3.Text);
                updateUartParambuf();
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            usartParity = comboBox4.SelectedIndex;
            updateUartParambuf();

        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            usartStopbits = comboBox5.SelectedIndex;
            updateUartParambuf();
        }

        private void comboBox6_KeyUp(object sender, KeyEventArgs e)
        {
            comboBox6.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                if (comboBox6.Text.Length <= 0) return;
                if (IsInt(comboBox6.Text))
                {
                    Int32 temp = Convert.ToInt32(comboBox6.Text);
                    if(temp > 4096)
                    {
                        MessageBox.Show("FrameSize must mot large than 4096\n");
                    }
                    else
                    {
                        usartFrameSize = temp;
                    }
                    updateUartParambuf();
                    comboBox6.ForeColor = Color.Black;
                }
            }
        }
        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.Text.Length <= 0) return;
            if (IsInt(comboBox6.Text))
            {
                Int32 temp = Convert.ToInt32(comboBox6.Text);
                if (temp > 4096)
                {
                    MessageBox.Show("FrameSize must mot large than 4096\n");
                }
                else
                {
                    usartFrameSize = temp;
                }
                updateUartParambuf();
            }
        }

        private void comboBox7_KeyUp(object sender, KeyEventArgs e)
        {
            comboBox7.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                if (comboBox7.Text.Length <= 0) return;
                if (IsInt(comboBox7.Text))
                {
                    usartFrameInterval = Convert.ToInt32(comboBox7.Text);
                    updateUartParambuf();
                    comboBox7.ForeColor = Color.Black;
                }
            }
        }



        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox7.Text.Length <= 0) return;
            if (IsInt(comboBox7.Text))
            {
                usartFrameInterval = Convert.ToInt32(comboBox7.Text);
                updateUartParambuf();
            }

        }

        private static string[] uartkeylist =
        {
            "Uart-Port",
            "Uart-Baud",
            "Uart-Dbits",
            "Uart-Parity",
            "Uart-STPbits",
            "Uart-FrameSize",
            "Uart-FrameInterval",
        };

        private bool form2LoadConfig()
        {
            bool ret = false;
            Dictionary<string, string> Item = new Dictionary<string, string>();
            ret = MyAppconfig.Load("uartportSet", ref Item);
            if (ret && Item != null)
            {
                if (Item.Keys == null) return false;
                if (Item.Keys.Count != uartkeylist.Length) return false;
                foreach (string key in Item.Keys)
                {
                    if (uartkeylist.ToList().IndexOf(key) == -1)
                    {
                        return false;
                    }
                    if (Validator.IsIntegerNotNagtive(Item[key]) == false)
                    {
                        return false;
                    }
                }
                usartComNo = Convert.ToInt32(Item["Uart-Port"]);
                usartBaudrate = Convert.ToInt32(Item["Uart-Baud"]);
                usartbitPerD = Convert.ToInt32(Item["Uart-Dbits"]);
                usartParity = Convert.ToInt32(Item["Uart-Parity"]);
                usartStopbits = Convert.ToInt32(Item["Uart-STPbits"]);
                usartFrameSize = Convert.ToInt32(Item["Uart-FrameSize"]);
                usartFrameInterval = Convert.ToInt32(Item["Uart-FrameInterval"]);
            }
            else
            {
                ret = false;
            }
            return ret;
        }

      

        private bool form2configSave()
        {
            bool ret = false;
            string[] param = new string[20];
            //param[0] = comboBox1.Text;
            //param[1] = comboBox2.Text;
            //param[2] = comboBox3.Text;
            //param[3] = comboBox4.Text;
            //param[4] = comboBox5.Text;
            //param[5] = comboBox6.Text;
            //param[6] = comboBox7.Text;

            param[0] = usartComNo.ToString();
            param[1] = usartBaudrate.ToString();
            param[2] = usartbitPerD.ToString();
            param[3] = usartParity.ToString();
            param[4] = usartStopbits.ToString();
            param[5] = usartFrameSize.ToString();
            param[6] = usartFrameInterval.ToString();


            param = param.Take(7).ToArray();
            ret = MyAppconfig.Save("uartportSet", param,param.Length);

            return ret;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            button1.Enabled = false;
           bool ret = form2configSave();
            if (ret)
            {
                button1.Text = "保存成功:)";
                button1.ForeColor = Color.Green;
            }
            else
            {
                button1.Text = "保存失败!";
                button1.ForeColor = Color.Red;
            }
            button1.Enabled = true;

        }
    }
}
