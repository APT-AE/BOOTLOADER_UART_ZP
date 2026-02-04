using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            textBox1.Text = mainForm.binplot(mainForm.startpattern);
            textBox2.Text = mainForm.binplot(mainForm.rstpattern);
        }

        private void Form5_FormClosing(object sender, FormClosingEventArgs e)
        {
            update_syncPatterns();
            mainForm.plot_syncpatterns();
            mainForm.rstPatternWindowOn = false;
        }

        private bool update_syncPatterns()
        {
            string[] tmp = new string[2];
            tmp[0] = textBox1.Text;
            tmp[1] = textBox2.Text;
            bool ret = mainForm.get_syncPatterns(tmp);
            if (ret)
            {
                form5configSave();
            }
            return ret;
        }

        private bool form5configSave()
        {
            bool ret = false;
            string[] param = new string[5];
            param[0] = textBox1.Text;
            param[1] = textBox2.Text;

            param = param.Take(2).ToArray();
            ret = MyAppconfig.Save("syncPatterns", param, param.Length);

            return ret;
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox1.Text.Length <= 0) return;
            textBox1.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                string str = textBox1.Text;
                byte[] tmp;
                bool ret = mainForm.read_hex_mat(str,out tmp);
                if (ret)
                {
                    textBox1.ForeColor = Color.Black;
                    mainForm.startpattern = tmp;
                }
            }
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox2.Text.Length <= 0) return;
            textBox2.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                string str = textBox2.Text;
                byte[] tmp;
                bool ret = mainForm.read_hex_mat(str, out tmp);
                if (ret)
                {
                    textBox2.ForeColor = Color.Black;
                    mainForm.rstpattern = tmp;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool ret = update_syncPatterns();
            if(ret == false)
            {
                button1.Text = "更新配置失败";
                button1.ForeColor = Color.Red;
                textBox1.ForeColor = Color.Red;
                textBox2.ForeColor = Color.Red;
            }
            else
            {
                button1.Text = "更新配置成功";
                button1.ForeColor = Color.Green;
                textBox1.ForeColor = Color.Black;
                textBox2.ForeColor = Color.Black;
            }
        }
    }
}
