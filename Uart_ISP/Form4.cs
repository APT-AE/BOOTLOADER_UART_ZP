using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WindowsFormsApplication1
{
    public partial class Form4 : Form
    {
        public delegate void saveHexfile(string path,string name,string content);
        private saveHexfile saveCombinehex;

        bool[] fileSelectedbuf = new bool[AUTO_CONTROL_MAXCNT];
        bool configfileloaded = false;

        public Form4(saveHexfile p,string initPath)
        {
            this.saveCombinehex = p;
            InitializeComponent();
            for(int i=0;i< AUTO_CONTROL_MAXCNT; i++)
            {
                filepathBuf[i] = "";
                fileSelected[i] = false;
            }
            textBox4.Text = initPath;
            combFilepath = textBox4.Text;
            textBox4.ForeColor = Color.Black;
            int carrry = 0;
            textBox2.Text = BinClass.flashStartAddr.ToString("X");
            if (BinClass.flashEndAddr >= BinClass.flashStartAddr) {
                if (((BinClass.flashEndAddr - BinClass.flashStartAddr) % 1024) !=0) carrry = 1;
                textBox3.Text = ((BinClass.flashEndAddr - BinClass.flashStartAddr)/1024 + carrry).ToString() + "k";
            }
            else
            {
                textBox3.Text = "0k";
            }

            //如果存在配置文件就用配置文件更新配置
            form4LoadConfig();

            if (combMode == 0)
            {
                radioButton1.Checked = true;
            }


            textBox5.Text = combFilename;

            textBox4.ForeColor = Color.Black;
            textBox5.ForeColor = Color.Black;

            if (IsInt(textBox1.Text) && textBox1.Text != "")
            {
                int cnt = Convert.ToInt32(textBox1.Text);
                if (cnt > 8 || cnt < 0) cnt = 0;
                for (int i = 0; i <cnt;i++)
                {
                    button4_Click(button4, null);
                }
                if (configfileloaded)
                {
                    for (int i = 0; i < cnt; i++)
                    {
                        autoTxt[i].Text = filepathBuf[i];
                        if (fileSelectedbuf[i] == false) autoChkBox[i].Checked = false;
                        else { autoChkBox[i].Checked = true; }
                    }
                }
            }


        }

        const int AUTO_CONTROL_MAXCNT = 8;     //自动生成的控件的最大个数
        private const int NRowOfAutoCtrls = 4; //自动生成的控件，在窗口中排布时的总行数
        private bool reshaped = false;        
        private string combFilepath = "";
        private string combFilename = "combHex.ihex";
        
        private enum atuoCtype
        {
            Lb = 0,
            Txt,
            Combo,
            ChkBOX,
            BTNS,
        }

        private string[] filepathBuf = new string[AUTO_CONTROL_MAXCNT];
        private bool[] fileSelected = new bool[AUTO_CONTROL_MAXCNT];
        private int combMode = 0;
        private Label[] autoLb = new Label[AUTO_CONTROL_MAXCNT];
        private TextBox[] autoTxt = new TextBox[AUTO_CONTROL_MAXCNT];
        private ComboBox[] autoComboT = new ComboBox[AUTO_CONTROL_MAXCNT];
        private CheckBox[] autoChkBox = new CheckBox[AUTO_CONTROL_MAXCNT];
        private Button[] autoBtn = new Button[AUTO_CONTROL_MAXCNT];

        private void AddControl(Control cParent, Control cc, string name, int sizeX, int sizeY, int locX, int locY)
        {
            cc.Name = name;
            cc.Size = new Size(sizeX, sizeY);
            cc.Location = new Point(locX, locY);
            cParent.Controls.Add(cc);
        }

        private void RemoveControl(Control cParent, string CCname)
        {
            foreach (Control c in cParent.Controls)
            {
                if (c.Name == CCname)
                {
                    c.Controls.Clear();
                    c.Dispose();
                }
            }
        }

        private void RemoveControl(Control cParent, bool delall)
        {
            if (delall == true)
            {
                do
                {
                    foreach (Control c in cParent.Controls)
                    {

                        c.Controls.Clear();
                        c.Dispose();
                    }
                } while (cParent.Controls.Count > 0);
            }
        }

        private void add_GroupControl(int tmpidx)
        {
            int[] width = { 60, 15, 10 };
            int offsetX = 0;
            int offsetY = 0;
            int gboxw = groupBox1.Size.Width;
            if (gboxw < 100) return;
            if (tmpidx >= NRowOfAutoCtrls)
            {
                gboxw = gboxw / 2;
            }
            if (tmpidx < NRowOfAutoCtrls) offsetX = 0;
            else offsetX = gboxw;
            offsetY = (tmpidx % NRowOfAutoCtrls) * 40;

            autoTxt[tmpidx] = new TextBox();
            autoBtn[tmpidx] = new Button();
            autoChkBox[tmpidx] = new CheckBox();

            AddControl(groupBox1, autoTxt[tmpidx], "Tx" + tmpidx.ToString(), gboxw * width[0] / 100, 20, 20 + offsetX, 50 + offsetY);
            offsetX = offsetX + gboxw * width[0] / 100 + 10;
            AddControl(groupBox1, autoBtn[tmpidx], "Autobtn" + tmpidx.ToString(), gboxw * width[1] / 100, 20, 20 + offsetX, 50 + offsetY);
            offsetX = offsetX + gboxw * width[1] / 100 + 10;
            autoBtn[tmpidx].Click += new System.EventHandler(autobtn_click);
            AddControl(groupBox1, autoChkBox[tmpidx], "autoChkBox" + tmpidx.ToString(), gboxw * width[2] / 100, 20, 20 + offsetX, 50 + offsetY);
            autoChkBox[tmpidx].CheckedChanged += new System.EventHandler(autoChkBox_CheckedChanged);
            autoTxt[tmpidx].Text = "";
            autoBtn[tmpidx].Text = "打开";
            autoChkBox[tmpidx].Checked = true;
            fileSelected[tmpidx] = true;
        }


        private bool btnchangeText1 = false;

        private void clear_auto_controls()
        {
            RemoveControl(groupBox1, true);
            reshaped = false;
            curFileNum = 0;
            for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
            {
                filepathBuf[i] = "";
                fileSelected[i] = false;
            }
        }

        private int curFileNum = 0; //当前文件总数
        private void button4_Click(object sender, EventArgs e)
        {
            if (curFileNum >= AUTO_CONTROL_MAXCNT) return;
            if (reshaped == false && curFileNum == NRowOfAutoCtrls)
            {
                RemoveControl(groupBox1, true);
                for (int i = 0; i < 5; i++)
                {
                    add_GroupControl(i);
                }
                reshaped = true;
            }
            else
            {
                add_GroupControl(curFileNum);
            }
            curFileNum++;
            btnchangeText1 = true;
            textBox1.Text = curFileNum.ToString();
        }

        public static bool IsInt(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*$");
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
           if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
           {
                if (btnchangeText1 == true)
                {
                    btnchangeText1 = false;
                    return;
                }
                if (IsInt(textBox1.Text) && textBox1.Text != "")
                {
                    textBox1.ReadOnly = true;
                    int Nloop = Convert.ToInt32(textBox1.Text);
                    if (Nloop > AUTO_CONTROL_MAXCNT || Nloop < 1)
                    {
                        clear_auto_controls();
                        textBox1.ReadOnly = false;
                        return;
                    }
                    clear_auto_controls();
                    curFileNum = 0;
                    reshaped = false;
                    curFileNum = Nloop - 1;
                    for (int i = 0; i < Nloop; i++)
                    {
                        add_GroupControl(i);
                    }
                    if (Nloop > NRowOfAutoCtrls) reshaped = true;
                    textBox1.ReadOnly = false;
                }
           }
        }


        

        private bool getfileNamebyGUI(int indx)
        {
            bool fileopenSucceed = false;
            FileInfo tmpinfo;
            string fpath = "";
            filepathBuf[indx] = "";
            autoTxt[indx].Text = "";
            if (fpath == "")
            {
                openFileDialog1.FileName = "";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    fpath = openFileDialog1.FileName;
                    tmpinfo = new FileInfo(@fpath);
                    if (tmpinfo.Length > 1024 * 1024 * 2) {
                        MessageBox.Show("不支持2M以上大文件！");
                    }
                    else {
                        autoTxt[indx].Text = fpath;
                        filepathBuf[indx] = fpath;
                    }
                    fileopenSucceed = true;
                }
                else
                {
                    filepathBuf[indx] = "";
                   // MessageBox.Show("文件不存在");
                }
            }
            return fileopenSucceed;
        }

     #region 动态添加的控件绑定的事件函数

        private void autobtn_click(object sender, EventArgs e)
        {
            Button btnx = (Button)sender;
            int btnIdx = Convert.ToInt32((btnx.Name.Replace("Autobtn", "")));
            getfileNamebyGUI(btnIdx);
        }

        private void autoChkBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkx = (CheckBox)sender;
            int chkboxIdx = Convert.ToInt32((chkx.Name.Replace("autoChkBox", "")));
            if (autoChkBox[chkboxIdx].Checked)
            {
                fileSelected[chkboxIdx] = true;
            }
            else
            {
                fileSelected[chkboxIdx] = false;
            }
        }

        #endregion

        private bool isIhexline(string str)
        {
            int len = str.Length;
            if (len % 2 == 0 || len < 11) return false;
            string patternStr = "^:[A-Fa-f0-9]{"+ (len-1).ToString() + "}$";
            return Validator.IsMatch(str,patternStr,true);
        }

        private string[] SplitIhexline(string str)
        {
            if (isIhexline(str) == false) return null;
            int len = str.Length;
            string[] sstr = new string[6];
            int[] fieldwidth = { 1, 2, 4, 2, len -11,2 };
            int curpos = 0;
            for(int i = 0; i < 6; i++)
            {
                sstr[i] = str.Substring(curpos, fieldwidth[i]);
                curpos += fieldwidth[i];
            }
            // data byte check
            if (Convert.ToInt32(sstr[1], 16) != (sstr[4].Length) / 2)
            {
                MessageBox.Show("文件内容有误\n" + "字节域与实际数据字节数不符\n" + str);
                return null;
            }
            // checksum 
            int[] tmpBmat = new int[len];
            string substr = str.Substring(1);
            for (int i = 0; i < (len-1)/2; i++)
            {
                try
                {
                    // 每两个字符是一个 byte。 
                    tmpBmat[i] = int.Parse(substr.Substring(i * 2, 2),System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    MessageBox.Show(" not a valid hex number!");
                    return null;
                }
            }
            if(tmpBmat.Sum() % 256 != 0 )
            {
                MessageBox.Show("文件内容有误,校验和不等\n" + str);
                return null;
            }
            return sstr;
        }

        const int SECITON_INTERVAL_MAXCNT = 200;

        private struct interval
        {
            public uint min;
            public uint max;
        }

        /// <summary>
        /// sectionProfile 用于描述hex文件中具体的内容起止信息
        /// hex中程序所占据的地址可能是不连续的，这样不连续的一块块地址占用情况，由sectionProfile具体描述
        /// </summary>
        private struct sectionProfile
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = SECITON_INTERVAL_MAXCNT, ArraySubType = UnmanagedType.Struct)]
            public interval[] intvalx;   
            public bool fileused;
            public int fileindx;
            public int contentLinecnt;
            public string sectionMark;
            public int regioncnt;
            public string fileFpath;
        }

        private struct OvrideRet
        {
            public bool overide;
            public int fileAidx;
            public interval segA;
            public int fileBidx;
            public interval segB;
        }

        private OvrideRet OverideCheckret;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pp">  段描述 </param>
        /// <param name="curindx"> 当前段描述 </param>
        /// <returns></returns>
        /// 
        private OvrideRet IsHexcontentOveride(sectionProfile[] pp,int curindx)
        {
            OvrideRet ret = new OvrideRet();
            //if (curindx < 1) {ret.overide = false;return ret;}
            uint tmpmin, tmpmax;

            //当前文件内部是否有地址重叠
            for (int k = 0; k < pp[curindx].regioncnt; k++)
            {
                tmpmin = pp[curindx].intvalx[k].min;
                tmpmax = pp[curindx].intvalx[k].max;
                
                for (int j = 0; j < SECITON_INTERVAL_MAXCNT; j++)
                {
                    if (j == pp[curindx].regioncnt) break;
                    if (j == k) continue;
                    if (!((tmpmin > pp[curindx].intvalx[j].max) || (tmpmax < pp[curindx].intvalx[j].min)))
                    {
                        ret.overide = true;
                        ret.fileAidx = curindx;
                        ret.segA = pp[curindx].intvalx[k];
                        ret.fileBidx = curindx;
                        ret.segB = pp[curindx].intvalx[j];
                        return ret;
                    }
                }
            }

            //与其他文件是否有否有地址重叠
            for (int k = 0; k < pp[curindx].regioncnt; k++)
            {
                tmpmin = pp[curindx].intvalx[k].min;
                tmpmax = pp[curindx].intvalx[k].max;
                
                for (int i = 0; i < curindx; i++)
                {
                    for (int j = 0; j < SECITON_INTERVAL_MAXCNT; j++)
                    {
                        if (j == pp[i].regioncnt) break;
                        if(!((tmpmin > pp[i].intvalx[j].max) || (tmpmax < pp[i].intvalx[j].min)))
                        {
                            ret.overide = true;
                            ret.fileAidx = curindx;
                            ret.segA = pp[curindx].intvalx[k];
                            ret.fileBidx = i;
                            ret.segB = pp[i].intvalx[j];
                            return ret;
                        }
                    }
                }
            }
            ret.overide = false;
            return ret;
        }

        private struct secProfileSortcontrol
        {
            public int indx;
            public int Sourcefileindx;
            public interval segx;
            public interval lineno;
        }

        public class MyComparer : IComparer<secProfileSortcontrol>
        {
            #region IComparer<secProfileSortcontrol> 成员

            int IComparer<secProfileSortcontrol>.Compare(secProfileSortcontrol x, secProfileSortcontrol y)
            {
                if (x.segx.min > y.segx.max) return 1;
                else if (x.segx.max < y.segx.min) return -1;
                else return 0;
            }

            #endregion
        }


        private string SortCombinehex(string rawstr, sectionProfile[] pp)
        {
            string outstr = "";
            int totalsegnt = 0;
            for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
            {
                totalsegnt += pp[i].regioncnt;
            }
            if (totalsegnt == 0) return "";
            if (totalsegnt == 1) return rawstr;
            secProfileSortcontrol[] ctrol = new secProfileSortcontrol[totalsegnt];
            int k = 0;
            uint offset = 0;
            for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
            {
                if (pp[i].fileused == false) continue;
                string[] temp = (pp[i].sectionMark).Split(',');
                uint[] tmpcnt= Array.ConvertAll(temp, uint.Parse);
                if(tmpcnt.Length < pp[i].regioncnt)
                {
                    MessageBox.Show("sectionProfile[] pp 参数有误\n");
                    return "";
                }
                for (int j = 0; j < pp[i].regioncnt; j++)
                {
                    ctrol[k].indx = k;
                    ctrol[k].Sourcefileindx = i;
                    ctrol[k].segx = pp[i].intvalx[j];
                    ctrol[k].lineno.min = tmpcnt[j] + 1 + offset;
                    ctrol[k].lineno.max = tmpcnt[j+1] + offset;
                    k++;
                }
                offset += ctrol[k-1].lineno.max;
            }
            Array.Sort(ctrol, new MyComparer());
            string[] buf = rawstr.Split((System.Environment.NewLine).ToCharArray(), StringSplitOptions.None);
            buf = buf.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            for (int i = 0; i < ctrol.Length; i++)
            {
                int sta = (int)(ctrol[i].lineno.min)-1;
                int end = (int)(ctrol[i].lineno.max);
                for (int j = sta; j < end; j++) {
                    outstr += buf[j];
                    outstr += System.Environment.NewLine;
                }
            }
            int len = buf.Length;
            outstr += buf[len - 2];
            outstr += System.Environment.NewLine;
            outstr += buf[len - 1];
            outstr += System.Environment.NewLine;
            return outstr;
        }

        private bool CombineHexfiles()
        {
            string combineContent = "";
            string temp = "";
            uint[] ROMrange = new uint[2];
            ROMrange[0] = BinClass.flashStartAddr;
            ROMrange[1] = BinClass.flashEndAddr;
            ArrayList al = new ArrayList();
            sectionProfile[] SecProfile = new sectionProfile[AUTO_CONTROL_MAXCNT];
            uint[] eipmat = new uint[AUTO_CONTROL_MAXCNT];
            string[] eiplineStr = new string[AUTO_CONTROL_MAXCNT];

            //段定义缓存初始化
            for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
            {
                SecProfile[i].intvalx = new interval[SECITON_INTERVAL_MAXCNT];
                for (int j = 0; j < SECITON_INTERVAL_MAXCNT; j++) {                    
                    SecProfile[i].intvalx[j].min = 0;
                    SecProfile[i].intvalx[j].max = 0;
                }
                SecProfile[i].fileused = false;
                SecProfile[i].fileindx = 0;
                SecProfile[i].contentLinecnt = 0;
                SecProfile[i].sectionMark = "0";
                SecProfile[i].regioncnt = 0;                
                SecProfile[i].fileFpath = "";
            }
            int filecnt = 0;

            string errstr = "";

            for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
            {
                eipmat[i] = ROMrange[1] + 10;
                eiplineStr[i] = "";
                if (combMode == 1)
                {
                    if (fileSelected[i] == false) continue;
                }
                temp = "";
                string path = filepathBuf[i];
                if (path == "") continue;
                if (!File.Exists(path))
                {
                    MessageBox.Show("文件路径或名称有误:\n" + path);
                    return false;
                }
                filecnt++;
                using (StreamReader sr = new StreamReader(path))
                {
                    temp = string.Empty;
                    temp = sr.ReadToEnd();
                }
                SecProfile[i].fileused = true;
                uint staddr = 0;
                uint nextStartaddr = 0;
                uint CurrentStartaddr = 0;
                uint endaddr = 0;
                uint linecnt = 0;
                uint extSegAddr = 0;
                uint CS_IP_Addr = 0;
                uint extLAddr = 0;
                uint EIPaddr = 0;
                bool EOF = false;
                string tempRomdata = "";
                string linestr = "";
                int intvalcnt = 0;
                bool skipExROMregion = false;
                bool newintval = true;
                int Err = 0;
                SecProfile[i].fileindx = i;
                SecProfile[i].fileFpath = path;

                using (StringReader strr = new StringReader(temp))
                {
                    while ((linestr = strr.ReadLine()) != null)
                    {
                        string[] linedata = SplitIhexline(linestr);
                        linecnt++;
                        if (linedata == null) {
                            Err = 1;
                            goto HandlefileErr;
                        }
                        switch (linedata[3])
                        {
                            case "00":
                                if (skipExROMregion == true) break;
                                if (newintval)
                                {    
                                    staddr = Convert.ToUInt32(linedata[2], 16) + extLAddr + extSegAddr;
                                    nextStartaddr = staddr + Convert.ToUInt32(linedata[1], 16);
                                    newintval = false;
                                    if(intvalcnt >= SECITON_INTERVAL_MAXCNT)
                                    {
                                        Err = 10;
                                        goto HandlefileErr;
                                    }
                                    SecProfile[i].intvalx[intvalcnt].min = staddr;
                                    SecProfile[i].regioncnt = intvalcnt;                                    
                                    if (staddr < ROMrange[0] || nextStartaddr > ROMrange[1])
                                    {
                                        Err = 2;
                                        goto HandlefileErr;
                                    }
                                }
                                else { 
                                    CurrentStartaddr = Convert.ToUInt32(linedata[2], 16) + extLAddr + extSegAddr;
                                    if (nextStartaddr != CurrentStartaddr)
                                    {
                                        if(CurrentStartaddr > ROMrange[1]){ Err = 3; goto HandlefileErr; }
                                        else if(CurrentStartaddr <= nextStartaddr) { Err = 11; goto HandlefileErr; }
                                        else
                                        {
                                            newintval = true;
                                            SecProfile[i].intvalx[intvalcnt].max = nextStartaddr;
                                            SecProfile[i].sectionMark += "," + (SecProfile[i].contentLinecnt).ToString();
                                        }             
                                    }                                    
                                    nextStartaddr = CurrentStartaddr + Convert.ToUInt32(linedata[1], 16);
                                    if (nextStartaddr > 0) endaddr = nextStartaddr - 1;
                                    else endaddr = 0;
                                    if(endaddr > ROMrange[1]) { Err = 4; goto HandlefileErr; }
                                    if (newintval == true)
                                    {
                                        intvalcnt++;
                                        if (intvalcnt >= SECITON_INTERVAL_MAXCNT)
                                        {
                                            Err = 10;
                                            goto HandlefileErr;
                                        }
                                        SecProfile[i].intvalx[intvalcnt].min = CurrentStartaddr;
                                        SecProfile[i].regioncnt = intvalcnt;
                                        newintval = false;
                                    }
                                }
                                tempRomdata += linestr;
                                tempRomdata += System.Environment.NewLine;
                                SecProfile[i].contentLinecnt++;
                                break;
                            case "01":
                                SecProfile[i].intvalx[intvalcnt].max = endaddr;
                                SecProfile[i].sectionMark += "," + (SecProfile[i].contentLinecnt).ToString();

                                intvalcnt++;
                                SecProfile[i].regioncnt = intvalcnt;
                                EOF = true;
                                OverideCheckret.overide = false;
                                OverideCheckret = IsHexcontentOveride(SecProfile, i);
                                if (OverideCheckret.overide == false)
                                {
                                    combineContent += tempRomdata;
                                }
                                else
                                {
                                    errstr = "hex文件内容占用了相同的ROM区间！\n"+
                                        "文件:" + SecProfile[OverideCheckret.fileAidx].fileFpath +"\n"+
                                        "ROM区间: [0x" + OverideCheckret.segA.min.ToString("X") + " 0x"+OverideCheckret.segA.max.ToString("X") + "]\n" +
                                        "文件:" + SecProfile[OverideCheckret.fileBidx].fileFpath + "\n" +
                                        "ROM区间: [0x" + OverideCheckret.segB.min.ToString("X") + " 0x" + OverideCheckret.segB.max.ToString("X") + "]\n"
                                        ;
                                    Err = 12;
                                    goto HandlefileErr;
                                }
                                break;
                            case "02":
                                if (Convert.ToUInt32(linedata[1], 16) == 2)
                                {
                                    extSegAddr = (Convert.ToUInt32(linedata[4], 16)) << 4;
                                    if (extSegAddr > ROMrange[1]) skipExROMregion = true;
                                    else
                                    {
                                        skipExROMregion = false;
                                        newintval = true;
                                        SecProfile[i].intvalx[intvalcnt].max = endaddr;
                                        SecProfile[i].sectionMark += "," + (SecProfile[i].contentLinecnt).ToString();

                                        tempRomdata += linestr;
                                        tempRomdata += System.Environment.NewLine;
                                        SecProfile[i].contentLinecnt++;
                                    }
                                }
                                else
                                {
                                    Err = 5;
                                    goto HandlefileErr;
                                }
                                break;
                            case "03":
                                break;
                            case "04":
                                if (Convert.ToUInt32(linedata[1], 16) == 2)
                                {
                                    extLAddr = (Convert.ToUInt32(linedata[4], 16)) << 16;
                                    if (extLAddr > ROMrange[1]) skipExROMregion = true;
                                    else
                                    {

                                        skipExROMregion = false;
                                        newintval = true;
                                        SecProfile[i].intvalx[intvalcnt].max = endaddr;
                                        SecProfile[i].sectionMark += "," + (SecProfile[i].contentLinecnt).ToString();

                                        tempRomdata += linestr;
                                        tempRomdata += System.Environment.NewLine;
                                        SecProfile[i].contentLinecnt++;
                                    }
                                }
                                else
                                {
                                    Err = 6;
                                    goto HandlefileErr;
                                }
                                break;
                            case "05":
                                if (Convert.ToUInt32(linedata[1], 16) == 4)
                                {
                                    EIPaddr = (Convert.ToUInt32(linedata[4], 16));
                                    eipmat[i] = EIPaddr;
                                    eiplineStr[i] = linestr;
                                    if (EIPaddr+ 8 >= ROMrange[1]) { Err = 7; goto HandlefileErr; }
                                }
                                else
                                {
                                    Err = 8;
                                    goto HandlefileErr;
                                }
                                break;
                            default:
                                break;
                        }
                        if (EOF) break;
                        HandlefileErr:
                        if(Err !=0 )
                        {
                            string showerrstr = "hex文件有误,错误码: " + Err.ToString() + "\n";
                            switch (Err)
                            {
                                case 10:
                                    showerrstr += "hex文件中地址不连续的分块个数必须小于等于"+ SECITON_INTERVAL_MAXCNT.ToString()+"\n";
                                    break;
                                case 12:
                                    showerrstr += errstr;
                                    break;
                                default:
                                    showerrstr = showerrstr + path + ", 第" + linecnt.ToString() + "行\n" + linestr;
                                    break;
                            }
                            MessageBox.Show(showerrstr);
                            return false;
                        }
                    }
                }
            }
            if (filecnt == 0)
            {
                MessageBox.Show("未选择任何文件!");
                return false;
            }
            int minindx = Array.IndexOf(eipmat,eipmat.Min());
            combineContent += eiplineStr[minindx];
            combineContent += System.Environment.NewLine;
            combineContent += ":00000001FF";
            combineContent += System.Environment.NewLine;            
            saveCombinehex(combFilepath, combFilename,SortCombinehex(combineContent, SecProfile) );
            return true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(curFileNum > 1)
            {
                CombineHexfiles();
            }            
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton1.Checked) combMode = 0;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked) combMode = 1;
        }

        public void writeConfigfile(string fullname, string content)
        {
            //定义文件信息对象
            FileInfo finfo = new FileInfo(fullname);
            //判断文件是否存在
            if (finfo.Exists)
            {
                //删除该文件
                finfo.Delete();
            }
            TextWriter bw = null;
            try
            {
                //创建只写文件流
                using (FileStream fs = finfo.OpenWrite())
                {
                    //根据上面创建的文件流创建写数据流
                    StreamWriter w = new StreamWriter(fs);

                    //设置写数据流的起始位置为文件流的末尾
                    //w.BaseStream.Seek(0, SeekOrigin.End);

                    //写入内容并换行
                    w.Write(content);

                    //清空缓冲区内容，并把缓冲区内容写入基础流

                    w.Flush();
                    //关闭写数据流
                    w.Close();
                }
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message + "\r\n");
                return;
            }
            finally
            {
                if (bw != null)
                {
                    bw.Close();
                }
            }
        }

        private bool form4configSave()
        {
            bool ret = false;
            string[] param = new string[20];
            param[0] = textBox1.Text;
            param[1] = textBox2.Text;
            param[2] = textBox3.Text;
            param[3] = textBox4.Text;
            param[4] = textBox5.Text;
            param[5] = combMode.ToString();
            int sel = 0;
            for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
            {
                if (fileSelected[i]) { sel += (0x1 << i); param[6+i] = filepathBuf[i]; }
                else param[6 + i] = "";
            }
            param[6 + AUTO_CONTROL_MAXCNT] = sel.ToString();

            param = param.Take(6 + AUTO_CONTROL_MAXCNT + 1).ToArray();
            ret = MyAppconfig.Save("hexCombine",param, 6 + AUTO_CONTROL_MAXCNT + 1);
            return ret;
        }
        private bool checkParam(Dictionary<string, string> item)
        {
            bool ret = false;
            if (item == null) return false;
            string numstr = item["Form4-filesel"];
            if (Validator.IsIntegerNotNagtive(numstr) ==false) return false;
            numstr = item["Form4-textbox1"];
            if (Validator.IsInteger(numstr) == false) return false;
            if (File.Exists(item["Form4-textbox5"])==false) return false;
            return ret;
        }

        private void form4LoadConfig()
        {
            bool ret = false;
            Dictionary<string, string> Item = new Dictionary<string, string>();        
            ret = MyAppconfig.Load("hexCombine",ref Item);
            configfileloaded = false;
            if (ret && checkParam(Item))
            {
                textBox1.Text = Item["Form4-textbox1"];
                textBox2.Text = Item["Form4-textbox2"];
                textBox3.Text = Item["Form4-textbox3"];
                textBox4.Text = Item["Form4-textbox4"];
                combFilename = Item["Form4-textbox5"];
                if (Item["Form4-Combmode"]== "0") combMode = 0;
                else combMode = 1;
                string selstr = Item["Form4-filesel"];
                int sel = Convert.ToInt32(selstr);
                string str = "Form4-file";
                for (int i = 0; i < AUTO_CONTROL_MAXCNT; i++)
                {
                    if ((sel & 0x1) == 0x1) fileSelectedbuf[i] = true;
                    else fileSelectedbuf[i] = false;
                    filepathBuf[i] = Item[str + i.ToString()];
                    sel = sel >> 1;
                }
                configfileloaded = true;          
            }
            else
            {
                textBox1.Text = "2";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            form4configSave();
            button3.Enabled = true;
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox2.ForeColor != Color.Red) textBox2.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
           {
                string numstr = textBox2.Text;
                int err = 0;
                if (Validator.IsIntegerNotNagtive(numstr)) {
                    BinClass.flashStartAddr = Convert.ToUInt32(numstr);
                    BinClass.flashEndAddr = BinClass.flashStartAddr + BinClass.flashSize;
                }
                else if (Validator.IsHexadecimal(numstr))
                {
                    BinClass.flashStartAddr = Convert.ToUInt32(numstr, 16);
                    BinClass.flashEndAddr = BinClass.flashStartAddr + BinClass.flashSize;
                }
                else
                {
                    err = 1;
                    MessageBox.Show("输入错误  " + numstr +"\n"+
                        "正确的例子如下:\n" +
                        "0\n" +
                        "128\n" +
                        "0x1000\n"
                        );
                }
                if(err ==0) textBox2.ForeColor = Color.Black;
            }
        }

        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox3.ForeColor != Color.Red) textBox3.ForeColor = Color.Red;
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                int tmpsize = 100 * 1024;
                string sizestr;
                sizestr = textBox3.Text;
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
                        BinClass.flashSize = (uint)tmpsize;
                        BinClass.flashEndAddr = BinClass.flashStartAddr + BinClass.flashSize;
                    }
                    textBox3.ForeColor = Color.Black;
                }
                else
                {
                    MessageBox.Show("输入数据格式有误！\n"+ 
                        "正确的例子如下:\n"+
                        "512B\n"+
                        "32K\n"+
                        "36K\n"+
                        "64M\n"
                        );
                }               
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdlg = new FolderBrowserDialog();
            //folder控件描述Environment.SpecialFolder.Desktop;
            fdlg.Description = "请选择Hex文件保存路径";
            //指定folder根=桌面
            fdlg.RootFolder = Environment.SpecialFolder.Desktop;
            //是否添加新建文件夹的按钮
            fdlg.ShowNewFolderButton = true;

            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = fdlg.SelectedPath;
                combFilepath = textBox4.Text;
                textBox4.ForeColor = Color.Black;
            }
        }

        private void textBox4_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                combFilepath = textBox4.Text;
                textBox4.ForeColor = Color.Black;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            textBox4.ForeColor = Color.Red;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            textBox5.ForeColor = Color.Red;
        }

        private void textBox5_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                if (textBox5.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    MessageBox.Show("文件名包含非法字符");
                    textBox5.Text = combFilename;
                    textBox5.ForeColor = Color.Black;
                }
                else
                {
                    combFilename = textBox5.Text;
                    textBox5.ForeColor = Color.Black;
                }
            }
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
           mainForm.combinWindowOn = false;
        }
    }
}
