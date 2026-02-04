using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = comboBox1.Items.IndexOf(getflashSizeStr(BinClass.flashSize));
            if (comboBox1.SelectedIndex == -1) comboBox1.Text = getflashSizeStr(BinClass.flashSize);
            textBox1.Text = "0x"+BinClass.flashStartAddr.ToString("X");
            comboBox2.SelectedIndex = comboBox2.Items.IndexOf("0x" + BinClass.flashPading.ToString("X8"));
            if (comboBox2.SelectedIndex == -1) comboBox2.SelectedIndex = 0;

        }

        private string getflashSizeStr( UInt32 size)
        {
            if (size < 1024)
            {
                return size.ToString();
            }
            else if(size < 1024 * 1024)
            {
                return (size/1024).ToString()+"K";
            }
            else if(size < 1024 * 1024 * 1024)
            {
                return (size / (1024*1024)).ToString() + "M";
            }
            else
            {
                return "invalid size";
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mainForm.Starthex2bin = true;
            this.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int tmpsize = 100 * 1024;
            string sizestr;
            sizestr = comboBox1.Text;
            Regex reg = new Regex(@"^\d+[KMBkb]{1}$");
            Match match = reg.Match(sizestr);
            if (match != null && match.Success)
            { 
                string str = match.Groups[0].Value;
                if (str != "")
                {
                    tmpsize = int.Parse(str.Substring(0, str.Length - 1));
                    char[] tmpbyts = str.ToCharArray();
                    char unit = tmpbyts[tmpbyts.Length - 1];
                    switch (unit)
                    {
                        case 'K':
                        case 'k':
                            tmpsize = tmpsize * 1024;
                            break;
                        case 'M':
                            tmpsize = tmpsize * 1024 * 1024;
                            break;
                        default:
                            break;
                    }
                    BinClass.flashSize = (UInt32)tmpsize;
                    BinClass.flashEndAddr = BinClass.flashSize + BinClass.flashStartAddr;
                }
            }
            else
            {
                MessageBox.Show("输入数据格式有误！\n");
            }
        }

        private void comboBox1_KeyUp(object sender, KeyEventArgs e)
        {
            comboBox1.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                int tmpsize = 100 * 1024;
                string sizestr;
                sizestr = comboBox1.Text;
                Regex reg = new Regex(@"^\d+[KMBkb]{1}$");
                Match match = reg.Match(sizestr);
                if (match != null && match.Success)
                {
                    string str = match.Groups[0].Value;
                    if (str != "")
                    {
                        tmpsize = int.Parse(str.Substring(0, str.Length - 1));
                        char[] tmpbyts = str.ToCharArray();
                        char unit = tmpbyts[tmpbyts.Length - 1];
                        switch (unit)
                        {
                            case 'K':
                            case 'k':
                                tmpsize = tmpsize * 1024;
                                break;
                            case 'M':
                                tmpsize = tmpsize * 1024 * 1024;
                                break;
                            default:
                                break;
                        }
                        BinClass.flashSize = (UInt32)tmpsize;
                        BinClass.flashEndAddr = BinClass.flashSize + BinClass.flashStartAddr;
                        comboBox1.ForeColor = Color.Black;
                    }
                }
                else
                {
                    MessageBox.Show("输入数据格式有误！\n");
                }
            }
        }


        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            textBox1.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                string txt = textBox1.Text;
                if (BinClass.IsHexadecimal(txt) && txt != "")
                {
                    textBox1.ReadOnly = true;
                    BinClass.flashStartAddr = Convert.ToUInt32(txt,16);
                    BinClass.flashEndAddr = BinClass.flashSize + BinClass.flashStartAddr;
                    //MessageBox.Show("0x" + BinClass.flashStartAddr.ToString("X8"));
                    textBox1.ReadOnly = false;
                    textBox1.ForeColor = Color.Black;
                }
                else
                {
                    MessageBox.Show("输入数据格式有误！\n"+ txt);
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string txt = comboBox2.Text;
            if (BinClass.IsHexadecimal(txt) && txt != "")
            {
                BinClass.flashPading = Convert.ToUInt32(txt, 16);
                if (BinClass.ImageReady()) { 
                    mainForm.controlmsgPlot("\n# flash padingvalue changed!\n" +
                   "new padding :" + BinClass.flashPading.ToString("X") + "\n" +
                   "new checksum :" + BinClass.getBinsChecksum().ToString("X") + "\n"
                   );
                }
               //MessageBox.Show("0x" + BinClass.flashPading.ToString("X8"));
            }
            else
            {
                MessageBox.Show("输入数据格式有误！\n" + txt);
            }
        }


    }
}
