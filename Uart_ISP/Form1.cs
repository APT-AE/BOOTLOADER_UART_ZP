using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Configuration;
using System.Reflection;
using System.Management;
using System.Collections;

namespace WindowsFormsApplication1
{
    public partial class mainForm : Form
    {
        public static string toolname = "APT_UartISP";
        public static string versionstring = "1.1.11";
        private string vernotestr = "version:" + versionstring + " new specs:" + Environment.NewLine +
                            "  uartport param can enter manually." + Environment.NewLine +
                            "  frame param can modify manually." + Environment.NewLine +
                            "  try open com after setting,and save com param." + Environment.NewLine +
                            "  add file select filter." + Environment.NewLine +
                            "  bin file supported" + Environment.NewLine +
                            "  optimized for \"CH340x\" tool" + Environment.NewLine +
                            "  reset and start pattern configurable" + Environment.NewLine;


        public static byte[] startpattern = { 0xEF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0xFF };
        public static byte[] rstpattern =   { 0xEF, 0xF5, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xFF };
        

        public static mainForm Instance;
        public static int[] usartParamBuf = { 4,115200,8,0,1,64,2};
        SerialDevInfo opendcom;
        private const int NuartParam = 7;
        private int byteperOnesend =64;
        private int onesendDly = 2;

        public static string[] combHexsFiles;
        public static int NuberOfcombHexs =2;


        private Point currentMFromLoc = new Point(0,0);

        private bool isUartReceiving = false;
        private bool isUartClosing = false;
        private bool isUartOpening = false;
        private bool isUartNewstart = false;

        private bool isSavelogEN;
        private bool hex2binDone = false;
        public static bool Starthex2bin = false;
        BinClass binx = new BinClass();
        private UInt32 binStartAddr = 0;
        private UInt32 binEndAddr = 0;
        private UInt32 APPStartAddr = 0;
        private UInt32 binsCheckSum = 0;
        private UInt32 binsCheckSumRec = 0;
        private bool ChecksumReced = false;
        private bool checkVreifyEn = false;
        private bool verifySkiped = true;
        private byte[] tempUartSendbuf= new byte[8];
        private int UartRecdatacnt = 0;

        private bool isTargetOff= false;
        private bool isContinueBtn= false;
        private bool waitcontinue = false;
        private bool ackcheckEn = false;
        private bool ackReced = false;
        private bool progEventdone = true;
        private uint progEventStatus = 0;
        private uint progBinsSendStatus = 0;
        private bool isQuit = false;


        #region 控件缩放
        double formWidth;//窗体原始宽度
        double formHeight;//窗体原始高度
        double scaleX;//水平缩放比例
        double scaleY;//垂直缩放比例
        Dictionary<string, string> ControlsInfo = new Dictionary<string, string>();//控件中心Left,Top,控件Width,控件Height,控件字体Size

        #endregion

        public mainForm()
        {
            Instance = this;
            InitializeComponent();
            GetAllInitInfo(this.Controls[0]);
            currentMFromLoc = new Point(this.Location.X, this.Location.Y);
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            form2LoadConfig();
            disp_uart_params();
            progEventStatus = 0;
            progBinsSendStatus = 0;
            form1LoadConfig();
            烧录后验证ToolStripMenuItem.Checked = !verifySkiped;
            this.Text = "APT mcu flash ISP demo V" + versionstring;

            form5LoadConfig();

            controlmsgPlot("start Pattern:" + binplot(startpattern) + "\n");
            controlmsgPlot("reset Pattern:" + binplot(rstpattern) + "\n");
        }

        public static void plotTorixhtxtbox2(string text)
        {
            controlmsgPlot(text);
        }

        protected void GetAllInitInfo(Control ctrlContainer)
        {
            if (ctrlContainer.Parent == this)//获取窗体的高度和宽度
            {
                formWidth = Convert.ToDouble(ctrlContainer.Width);
                formHeight = Convert.ToDouble(ctrlContainer.Height);
            }
            foreach (Control item in ctrlContainer.Controls)
            {
                if (item.Name.Trim() != "")
                {
                    //添加信息：键值：控件名，内容：据左边距离，距顶部距离，控件宽度，控件高度，控件字体。
                    ControlsInfo.Add(item.Name, (item.Left + item.Width / 2) + "," + (item.Top + item.Height / 2) + "," + item.Width + "," + item.Height + "," + item.Font.Size);
                }
                if ((item as UserControl) == null && item.Controls.Count > 0)
                {
                    GetAllInitInfo(item);
                }
            }

        }
        private void ControlsChaneInit(Control ctrlContainer)
        {
            scaleX = (Convert.ToDouble(ctrlContainer.Width) / formWidth);
            scaleY = (Convert.ToDouble(ctrlContainer.Height) / formHeight);
        }
        /// <summary>
        /// 改变控件大小
        /// </summary>
        /// <param name="ctrlContainer"></param>
        private void ControlsChange(Control ctrlContainer)
        {
            double[] pos = new double[5];//pos数组保存当前控件中心Left,Top,控件Width,控件Height,控件字体Size
            foreach (Control item in ctrlContainer.Controls)//遍历控件
            {
                if (item.Name.Trim() != "")//如果控件名不是空，则执行
                {
                    if ((item as UserControl) == null && item.Controls.Count > 0)//如果不是自定义控件
                    {
                        ControlsChange(item);//循环执行
                    }
                    string[] strs = ControlsInfo[item.Name].Split(',');//从字典中查出的数据，以‘，’分割成字符串组

                    for (int i = 0; i < 5; i++)
                    {
                        pos[i] = Convert.ToDouble(strs[i]);//添加到临时数组
                    }
                    double itemWidth = pos[2] * scaleX;     //计算控件宽度，double类型
                    double itemHeight = pos[3] * scaleY;    //计算控件高度
                    item.Left = Convert.ToInt32(pos[0] * scaleX - itemWidth / 2);//计算控件距离左边距离
                    item.Top = Convert.ToInt32(pos[1] * scaleY - itemHeight / 2);//计算控件距离顶部距离
                    item.Width = Convert.ToInt32(itemWidth);//控件宽度，int类型
                    item.Height = Convert.ToInt32(itemHeight);//控件高度
                    float Hfont = float.Parse((pos[4] * Math.Min(scaleX, scaleY)).ToString());
                    if (Hfont <= 0) Hfont = 1;
                    item.Font = new Font(item.Font.Name, Hfont);//字体

                }
            }

        }

        private void 工具ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        public static string binplot(byte[] bb)
        {
            int length = bb.Length;
            string ss = "";
            int j = 0;
            for (int i = 0; i < length; i++)
            {
                ss += bb[i].ToString("X2") + " ";
                j++;
                if (j == 16)
                {
                    j = 0;
                    ss += "\n";
                }
            }
            return ss;
        }

        private string openHexfile(string path)
        {
            bool fileopenSucceed = false;
            hex2binDone = false;
            string fileContent;
            FileInfo tmpinfo;
            string fileext = "";
            richTextBox1.Clear();
            if (path == "")
            {
                openFileDialog1.FileName = "";
                openFileDialog1.Filter = "hex文件|*.ihex;*.hex|bin文件|*.bin;*.aptbin|All file|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    path = openFileDialog1.FileName;
                    fileext = Path.GetExtension(path);
                    tmpinfo = new FileInfo(@path);
                    if (tmpinfo.Length > 1024 * 1024 * 2) { MessageBox.Show("不支持2M以上大文件！"); }
                    textBox1.Text = path;
                    fileopenSucceed = true;
                }
            }
            else
            {
                if (File.Exists(@path))
                {
                    fileext = Path.GetExtension(path);
                    tmpinfo = new FileInfo(@path);
                    if (tmpinfo.Length > 1024 * 1024 * 2) { MessageBox.Show("不支持2M以上大文件！"); }
                    else { fileopenSucceed = true; }

                }
                else
                {
                    MessageBox.Show("文件路径或文件不存在！\n");
                    MessageBox.Show("请重新输入文件路径或通过“开始”菜单栏选择 \n");
                }
            }
            if (fileopenSucceed)
            {
                bool hex2binErr = false;
                switch (fileext)
                {
                    case ".ihex":
                    case ".hex":
                        richTextBox1.LoadFile(path, RichTextBoxStreamType.PlainText);
                        using (StreamReader sr = new StreamReader(path))
                        {

                            fileContent = string.Empty;
                            fileContent = sr.ReadToEnd();
                            hex2binErr = binx.hexget(fileContent);
                            if (!hex2binErr)
                            {
                                controlmsgPlot("hex2bin Done!\n");
                                hex2binDone = true;
                            }
                            else
                            {
                                controlmsgPlot("hex2bin failed!\n");
                                controlmsgPlot(BinClass.Errstring);
                                hex2binDone = false;
                            }
                        }
                        break;
                    case ".bin":
                    case ".aptbin":
                        hex2binDone = binx.binget(path);
                        if (hex2binDone)
                        {
                            richTextBox1.Text = binplot(BinClass.binbytes);
                            controlmsgPlot("open binfile Done!\n");
                        }
                        else
                        {
                            controlmsgPlot("open binfile failed!\n");
                        }
                        break;
                    default:
                        MessageBox.Show("不支持的文件类型！\n");
                        break;
                }
                return path;
            }
            else
            {
                return "";
            }
        }


        private void openFile_Click(object sender, EventArgs e)
        {
            string filepath = "";
            openHexfile(filepath);
        }


        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            string filepath = "";
            openHexfile(filepath);
            button1.Enabled = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            isQuit = true;
            if (serialPort1.IsOpen)
            {
                if (serialPort1.IsOpen)
                {
                    set_serial_interrupt(false);
                }
                controlmsgPlot("# mcu ack is timeout\n ");
                ackcheckEn = false;
                ackReced = false;
                progEventdone = true;
                isTargetOff = false;
                isContinueBtn = false;
                button3.Text = "Program";
                button3.Enabled = true;
                progEventStatus = 0;
                UartClose();
                while (progEventdone == false) Application.DoEvents();
                progEventdone = true;
                progEventStatus = 0;
                isQuit = false;
            }
            else
            {
                progEventdone = true;
                progEventStatus = 0;
                isQuit = false;
            }
        }

        public static void OutMsg(RichTextBox rtb, string msg, Color color)
        {
            rtb.Invoke(new EventHandler(delegate
            {
                rtb.SelectionStart = rtb.Text.Length;//设置插入符位置为文本框末
                rtb.SelectionColor = color;//设置文本颜色
                rtb.AppendText(msg);//输出文本，换行
                rtb.ScrollToCaret();//滚动条滚到到最新插入行
            }));
        }

        private void UpdateRecDatadiplay(string rtext)
        {
            OutMsg(richTextBox2, rtext, Color.Orange);
        }

        public static void controlmsgPlot(string msg)
        {
            OutMsg(Instance.richTextBox2, msg, Color.Black);
        }

        public static void controlErrPlot(string msg,int level)
        {
            switch (level)
            {
                case 0: //err
                    OutMsg(Instance.richTextBox2, msg, Color.Red);
                    break;
                case 1: //warning
                    OutMsg(Instance.richTextBox2, msg, Color.Brown);
                    break;
                default:
                    break;
            }
           
        }

        private struct SerialDevInfo
        {
            public string ComNo;
            public string Name;
            public string Manufacturer;
        }
        private SerialDevInfo[] getPortDeviceInfo()
        {
            SerialDevInfo[] ret = null;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher
            ("select * from Win32_PnPEntity where Name like '%(COM%'"))
            {
                var hardInfos = searcher.Get();
                int len = hardInfos.Count;
                if (len <= 0) return ret;
                ret = new SerialDevInfo[len];
                int i = 0;
                foreach (var hardInfo in hardInfos)
                {
                    if (hardInfo.Properties["Name"].Value != null)
                    {
                        string deviceName = hardInfo.Properties["Name"].Value.ToString();
                        string Manu = hardInfo.Properties["Manufacturer"].Value.ToString();
                        ret[i].Name = deviceName;
                        ret[i].Manufacturer = Manu;
                        i++;
                    }
                }
            }
            return ret;
        }

        private bool get_serialport_info(string name)
        {
            bool ret = false;
            if (name.StartsWith("COM") == false) return false;
            SerialDevInfo[] cominfo = getPortDeviceInfo();
            if (cominfo == null) return false;
            for (int i = 0; i < cominfo.Length; i++)
            {
                if (cominfo[i].Name.Contains(name))
                {                   
                    opendcom = cominfo[i];
                    opendcom.ComNo = name;
                    //string temp = "\n Name: " + cominfo[i].Name + "\n Manufacturer: " + cominfo[i].Manufacturer + "\n";
                    //controlmsgPlot(temp);
                    ret = true;
                    break;
                }
            }
            
            return ret;
        }

        private bool UartOpen()
        {
            try
            {
                isUartOpening = true;
                serialPort1.Open();
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                isUartOpening = false;
                if (!serialPort1.IsOpen)
                {
                    return false;
                }
                else
                {
                    if (get_serialport_info(serialPort1.PortName) ==false)
                    {
                        controlErrPlot("warning: serial port name not found!\n",1);
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private bool UartClose()
        {
            
            try
            {
                if (!serialPort1.IsOpen)
                {
                    return true;
                }
                else
                {
                    if (isUartClosing == true) return false;
                    isUartClosing = true;
                    DelayMs(20);
                    serialPort1.DataReceived -= DataReceviedHandler;//反注册事件，避免下次再执行进来。
                    //最大延迟2秒，并检测到OnComm退出则退出，处理系统消息队列中的消息
                    int i = Environment.TickCount;
                    while (Environment.TickCount - i < 2000 && isUartReceiving) Application.DoEvents();
                     serialPort1.Close();
                    DelayMs(20);
                    isUartClosing = false;
                }
                if (serialPort1.IsOpen)
                {
                    return false;
                }
                else
                {
                    isUartClosing = false;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //delegate void HandleInterfaceUpdateDelegate(string text);  //委托，此为重点
        //HandleInterfaceUpdateDelegate interfaceUpdateHandle;

        private string rxbuf = "";
        private string temprxdata = "";
        private void DataReceviedHandler(object sender,SerialDataReceivedEventArgs e)
        {
            if (isUartClosing == true) return;
            try
            {
                isUartReceiving = true;
                SerialPort sp = (SerialPort)sender;
                temprxdata = sp.ReadExisting();
                if (ackcheckEn)
                {
                    rxbuf += temprxdata;
                    if (rxbuf.Contains("RSTACK"))
                    {
                        ackReced = true;
                        rxbuf = "";
                    }
                }
                if (checkVreifyEn)
                {
                    rxbuf += temprxdata;
                    if (rxbuf.Contains("VrfCkSum"))
                    {
                        int len = rxbuf.Length;
                        int idx = rxbuf.IndexOf("VrfCkSum");                       
                        if (idx + 16 <= len) {
                            string checksumstr = rxbuf.Substring(idx+8, 8);
                            if (BinClass.IsHexadecimal(checksumstr) && checksumstr != "")
                            {
                                binsCheckSumRec = Convert.ToUInt32(checksumstr, 16);
                                ChecksumReced = true;
                                rxbuf = "";
                            }  
                        }                       
                    }                    
                }
                // this.Invoke(interfaceUpdateHandle,temprxdata);
                if (waitcontinue == false)
                {
                    UpdateRecDatadiplay(temprxdata);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理接收数据时出错"+ ex.ToString());
            }
            finally
            {
                isUartReceiving = false;
            }
        }

        private void serialWrtie(byte[] bb,int len)
        {
            try
            {
                serialPort1.Write(bb, 0, len);
            }
            catch(Exception ex)
            {
                UartClose();
                string com = "COM" + usartParamBuf[0].ToString();
                controlmsgPlot(com + " 不存在或被占用！以下是本机串口列表： \n");
                string[] ArryPort = SerialPort.GetPortNames();
                if (ArryPort.Length == 0) controlmsgPlot("不存在\n");
                else
                {
                    for (int i = 0; i < ArryPort.Length; i++)
                    {
                        controlmsgPlot(ArryPort[i] + "\n");
                    }
                }
            }
        }

        private void hold_bypasspin_ForAlongtime()
        {
            byte[] zero = { 0x0 ,0x0};
            if (serialPort1.IsOpen)
            {
                while (serialPort1.BytesToWrite > 0) { };
                DelayMs(1);
                if (serialPort1.IsOpen)
                {
                    set_serial_interrupt(true);
                }
                DelayMs(200);
                if (serialPort1.IsOpen)
                {
                    set_serial_interrupt(false);
                }
                DelayMs(1);
            }                
        }

        private void init_ack_ckeck()
        {
            ackReced = false;
            rxbuf = "";
            ackcheckEn = true;
        }

        private void block_ack_check()
        {
            ackcheckEn = false;
            ackReced = false;
            rxbuf = "";
        }

        private bool check_rst_ack(int cnt)
        {
            int timeoutcnt = cnt; 
            while (timeoutcnt > 0)
            {
                if (ackReced)
                {
                    DelayMs(80); //接收到ack后至少等待80 ms,以便目标mcu发送完全部三个ack序列
                    break;
                }
                timeoutcnt -= 10;
                DelayMs(10);
            }
            ackcheckEn = false;
            ackReced = false;
            if (timeoutcnt <= 0)
            {
                init_ack_ckeck();
                return false;
            }
            else
            {
                init_ack_ckeck();
                return true;
            }
        }

        void timer_init()
        {
            timer1.Interval = onesendDly; //unit :ms
            timer1.Enabled = true;
            timer1.Start();
        }

        private double Progedpecent =0;
        private double BytetoProg = 1;


        private bool program_flash(byte[] bins)
        {
            bool ret = true;
            byte[] tmpaddrbytes = new byte[4];
            try
            {
                //发送bin数据
                BytetoProg = 1;
                if (bins != null)
                {
                    BytetoProg = bins.Length;
                    Progedpecent = 0;
                    timer_init();
                    byteperOnesend = usartParamBuf[5];
                    onesendDly = usartParamBuf[6];
                    if (byteperOnesend <= 0) byteperOnesend = 1;
                    int loopcnt = bins.Length / byteperOnesend;
                    int bytecnt = 0;
                    for (int i = 0; i < loopcnt; i++)
                    {
                        serialPort1.Write(bins, bytecnt, byteperOnesend);
                        do
                        {
                            Progedpecent = (bytecnt + byteperOnesend - serialPort1.BytesToWrite) / BytetoProg;
                            DelayMs(onesendDly);
                        } while (serialPort1.BytesToWrite > 0);
                        bytecnt += byteperOnesend;
                    }             
                    if (bins.Length > bytecnt)
                    {
                        serialPort1.Write(bins, bytecnt, bins.Length - bytecnt);
                        do
                        {
                            Progedpecent = (BytetoProg - serialPort1.BytesToWrite )/ BytetoProg;
                            DelayMs(onesendDly);
                        } while (serialPort1.BytesToWrite > 0);
                    }
                    //清空接收缓存
                    rxbuf = "";
                    ChecksumReced = false;
                    checkVreifyEn = true;
                    //发送结束标志
                    int endbytes = byteperOnesend + 1;                    
                    byte[] tmp = new byte[endbytes];
                    for (int i = 0; i < endbytes; i++)
                    {
                        tmp[i] = (byte)(BinClass.flashPading & 0xFF);
                    }
                    serialPort1.Write(tmp, 0, endbytes);
                    DelayMs(onesendDly);
                    while (serialPort1.BytesToWrite > 0) { };
                    timer1.Stop();
                }
                else
                {
                    BytetoProg = 1;
                }
            }
            catch
            {
                if (serialPort1.IsOpen == false) {
                    controlmsgPlot("串口被强制关闭了\n");
                }
            }
            return ret;
        }

        private bool verify_flash(UInt32 addr, byte[] bins)
        {
            if (verifySkiped)
            {
                return true;
            }
            if (binsCheckSumRec != BinClass.ImageChecksum()) return false;
            else return true;
        }

        private bool open_com()
        {
            string com = "COM" + usartParamBuf[0].ToString();
            //打开串口
            if (serialPort1.IsOpen)
            {
                try
                {
                    UartClose();
                }
                catch
                {
                    controlmsgPlot(com + "无法解除占用，推出烧录！！\n");
                    return false;
                }
            }
            try
            {
                serialPort1 = new SerialPort(com);
                serialPort1.PortName = com;//串口号
                serialPort1.BaudRate = usartParamBuf[1];//波特率
                serialPort1.DataBits = usartParamBuf[2];//数据位
                serialPort1.Parity = (Parity)usartParamBuf[3];//奇偶校验
                serialPort1.StopBits = (StopBits)usartParamBuf[4];//停止位
                serialPort1.Handshake = Handshake.None;
                serialPort1.RtsEnable = false;
                serialPort1.DtrEnable = false;
                serialPort1.ReadTimeout = 2000; //读超时，也是连接打开端口超时时间
                serialPort1.WriteTimeout = 1000; //写入超时
                serialPort1.Encoding = System.Text.Encoding.GetEncoding("iso-8859-1");
                serialPort1.ReceivedBytesThreshold = 1; //接收缓存中byte数大于等于Threshold时触发一次事件。
                serialPort1.ReadBufferSize = 10 * 1024;    //10KB 串口接收缓存
                serialPort1.WriteBufferSize = 10 * 1024;    //10KB  串口发送缓存
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(DataReceviedHandler);
                if (serialPort1.IsOpen)
                {
                    controlmsgPlot(com + " 已被占用！\n");
                    return false;
                }
                else
                {
                    //interfaceUpdateHandle = new HandleInterfaceUpdateDelegate(UpdateRecDatadiplay);  //实例化委托对象
                    UartOpen();
                    controlmsgPlot(com + " 已打开 \n");
                    DelayMs(100);               
                }
                return true;
            }
            catch (Exception)
            {
                UartClose();
                controlmsgPlot(com + " 不存在或被占用！以下是本机串口列表： \n");
                string[] ArryPort = SerialPort.GetPortNames();
                if (ArryPort.Length == 0) controlmsgPlot("不存在\n");
                else
                {
                    for (int i = 0; i < ArryPort.Length; i++)
                    {
                        controlmsgPlot(ArryPort[i] + "\n");
                    }
                }
                return false;
            }
        }

        private bool set_serial_interrupt(bool onoff)
        {
            bool ret = false;
            if (serialPort1 == null || opendcom.ComNo == null || opendcom.ComNo != serialPort1.PortName || opendcom.Name == null || opendcom.Manufacturer == null)
            {
                controlErrPlot("COMx not ready!\n", 1);
                return false;
            }
            int manu = 0;
            if (opendcom.Manufacturer.Contains("FTDI"))
            {
                manu = 1;
            }
            if (opendcom.Manufacturer.Contains("wch.cn") || opendcom.Name.Contains("CH340"))
            {
                manu = 2;
            }
            try
            {
                switch (manu)
                {
                    case 1:
                        serialPort1.BreakState = onoff;
                        break;
                    case 2:
                        if (serialPort1.IsOpen)
                        {
                            serialPort1.BreakState = onoff;
                            ret = true;
                        }
                        else
                        {
                            controlErrPlot(serialPort1.PortName + " is Closed!\n", 0);
                            ret = false;
                        }
                        break;
                    default:
                        serialPort1.BreakState = onoff;
                        break;
                }
            }
            catch (Exception ex)
            {
                controlErrPlot(ex.ToString(), 1);
                return false;
            }
            return ret;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            if (isTargetOff == true)
            {
                button3.Text = "Continue";
               // SendKeys.Send("ENTER");
            }
            else
            {
                button3.Text = "Program";
            }

            switch (button3.Text)
            {
                case "Program":
                    if (progEventdone == false) { controlmsgPlot("正在烧录中,请等待烧录完成！\n"); return; }
                    button3.Enabled = false;
                    progEventdone = false;
                    progEventStatus = 0;
                    progBinsSendStatus = 0;
                    label3.Text = "0%";
                    SetTextMessage(0);
                    string com = "COM" + usartParamBuf[0].ToString();
                    if (open_com() == false)
                    {
                        progEventdone = true;
                        button3.Enabled = true;
                        return;
                    }
                    progEventStatus = 0;
                    break;
                case "Continue":
                    isContinueBtn = true;
                    progEventStatus = 2;
                    break;
                default:
                    break;
            }



            while (progEventStatus < 100)
            {
                switch (progEventStatus)
                {
                    case 0:
                        //
                        //检查bin文件是否位空
                        if (BinClass.binbytes == null || BinClass.binbytes.Length == 0)
                        {
                            controlErrPlot("\n# bin data is empty!\n", 0);
                            progEventStatus = 15;
                            break;
                        }

                        controlmsgPlot("# programming\n");
                        controlmsgPlot("# Send mcu rst cmd\n");
                        //发送复位mcu指令
                       
                        serialWrtie(rstpattern, 10);
                        init_ack_ckeck();
                        serialWrtie(rstpattern, 10);
                        DelayMs(10);
                        serialWrtie(rstpattern, 10);
                        DelayMs(10);
                        waitcontinue = false;
                        isTargetOff = false;
                        isContinueBtn = false;
                        checkVreifyEn = false;
                        progBinsSendStatus = 0;
                        progEventStatus++;
                        break;
                    case 1:
                        //检查mcu返回的ACK
                        controlmsgPlot("\n# check Ack befroe target rst\n");
                        if (check_rst_ack(500) == false)
                        {
                            controlmsgPlot("\n# can not connect to mcu!\n");
                            controlmsgPlot("  check the serial port parameters \n");
                            controlmsgPlot("  maybe Switch the power is needed \n");
                            isTargetOff = true;
                            isContinueBtn = false;
                            if (serialPort1.IsOpen)
                            {
                                set_serial_interrupt(true);
                            }
                            button3.Text = "Continue";
                            button3.Enabled = true;
                            //MessageBox.Show("\n无法连接mcu，建议关闭电源再开启以复位mcu\n");
                            progEventStatus = 2;
                            break;
                        }
                        else
                        {
                            DelayMs(100);
                            while (true)
                            {
                                if (isQuit == true)
                                {
                                    progEventStatus = 15;
                                    break;
                                }
                                else
                                {
                                    if (serialPort1.IsOpen)
                                    {
                                        if (serialPort1.BytesToRead == 0)
                                        {
                                            progEventStatus++;
                                            break;
                                        }
                                    }
                                }
                                Application.DoEvents();
                            }
                        }
                        break;
                    case 2:
                        //DelayMs(1);
                        //将mcu串口接收脚拉低并hold住一段时间然后释放掉
                        controlmsgPlot("\n# sync the target rst\n");
                        if (isTargetOff == true)
                        {
                            if (serialPort1.IsOpen)
                            {
                                if (set_serial_interrupt(true)==false)
                                {
                                    progEventStatus = 15;
                                    break;
                                }
                            }                
                            while (isContinueBtn == false) {
                                waitcontinue = true;
                                Application.DoEvents();
                            }
                            waitcontinue = false;
                            isContinueBtn = false;
                        }
                        hold_bypasspin_ForAlongtime();
                        DelayMs(2);
                        progEventStatus++;
                        break;
                    case 3:
                        //检查mcu返回的ACK
                        if (check_rst_ack(500) == false)
                        {
                            controlmsgPlot("\n# mcu ack is timeout\n");
                            isTargetOff = true;
                            progEventStatus = 15;
                            break;
                        }
                        else
                        {
                            isTargetOff = false;
                            while (true)
                            {
                                if (isQuit == true)
                                {
                                    progEventStatus = 15;
                                    break;
                                }
                                else
                                {
                                    if (serialPort1.IsOpen)
                                    {
                                        if (serialPort1.BytesToRead == 0)
                                        {
                                            progEventStatus++;
                                            break;
                                        }
                                    }
                                }
                                Application.DoEvents();
                            }
                        }
                        break;
                    case 4:
                        //发送编程开始序列                
                        serialWrtie(startpattern, 10);
                        controlmsgPlot("\n# send program start pattern\n");
                        serialWrtie(startpattern, 10);
                        DelayMs(2);
                        serialWrtie(startpattern, 10);
                        DelayMs(2);
                        progBinsSendStatus = 0;
                        progEventStatus++;
                        break;
                    case 5:
                        //检查mcu返回的ACK
                        if (check_rst_ack(4000) == false)
                        {
                            controlmsgPlot("\n# mcu ack is timeout\n");
                            isTargetOff = true;
                            progEventStatus = 15;
                            break;
                        }
                        else
                        {
                            isTargetOff = false;
                            DelayMs(100);
                            while (true)
                            {
                                if (isQuit == true)
                                {
                                    progEventStatus = 15;
                                    break;
                                }
                                else
                                {
                                    if (serialPort1.IsOpen)
                                    {
                                        if (serialPort1.BytesToRead == 0)
                                        {
                                            progEventStatus++;                                            
                                            break;
                                        }
                                    }
                                }
                                Application.DoEvents();
                            }
                        }
                        break;
                    case 6:
                        switch (progBinsSendStatus) {
                            case 0:
                                //开始烧录镜像
                                controlmsgPlot("\n# mcu ready for program\n");
                                //检查bin文件是否位空
                                if (BinClass.binbytes == null || BinClass.binbytes.Length == 0)
                                {
                                    controlErrPlot("\n# bin data is empty!\n", 0);
                                    progEventStatus = 15;
                                    break;
                                }
                                //发送bin的起始地址
                                controlmsgPlot("\n# send binStartAddr\n");
                                binStartAddr = BinClass.addrbegin();
                                for(int i = 0; i < 4; i++)
                                {
                                    tempUartSendbuf[i] = (byte)((binStartAddr >> ((3 - i) * 8)) & 0xFF);
                                }
                                serialWrtie(tempUartSendbuf, 4);
                                DelayMs(2);
                                //等待ACK
                                progEventStatus = 5;
                                progBinsSendStatus++;
                                break;
                            case 1:
                                //发送bin的结束地址
                                controlmsgPlot("\n# send binEndAddr\n");
                                binEndAddr = BinClass.addrEnd();
                                for (int i = 0; i < 4; i++)
                                {
                                    tempUartSendbuf[i] = (byte)((binEndAddr >> ((3 - i) * 8)) & 0xFF);
                                }
                                serialWrtie(tempUartSendbuf, 4);
                                DelayMs(2);
                                //等待ACK
                                progEventStatus = 5;
                                progBinsSendStatus++;
                                break;
                            case 2:
                                //发送跳转到APP的地址
                                controlmsgPlot("\n# send EIPAddr\n");
                                APPStartAddr = BinClass.ImageEIPaddr();
                                for (int i = 0; i < 4; i++)
                                {
                                    tempUartSendbuf[i] = (byte)((APPStartAddr >> ((3 - i) * 8)) & 0xFF);
                                }
                                serialWrtie(tempUartSendbuf, 4);
                                progEventStatus = 5;
                                progBinsSendStatus++;
                                break; 
                            case 3:
                                //发送bin的校验和
                                controlmsgPlot("\n# send ImageChecksum\n");                                
                                binsCheckSum = BinClass.ImageChecksum();
                                binsCheckSumRec = binsCheckSum+1;
                                ChecksumReced = false;
                                for (int i = 0; i < 4; i++)
                                {
                                    tempUartSendbuf[i] = (byte)((binsCheckSum >> ((3 - i) * 8)) & 0xFF);
                                }
                                serialWrtie(tempUartSendbuf, 4);
                                progEventStatus = 5;
                                progBinsSendStatus++;
                                break;
                            case 4:
                                block_ack_check();              
                                if (BinClass.binbytes == null || BinClass.binbytes.Length == 0)
                                {
                                    controlmsgPlot("\n# bin data is null!\n");
                                    progEventStatus = 15;
                                }
                                else {
                                    controlmsgPlot("\n# bin data sending\n");
                                    serialPort1.DiscardInBuffer();
                                    //serialPort1.ReceivedBytesThreshold = byteperOnesend;
                                    serialPort1.ReceivedBytesThreshold = 4;
                                    DelayMs(2);
                                    program_flash(BinClass.binbytes);
                                    progEventStatus++;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case 7:
                        //是否需要校验
                        if (verifySkiped)
                        {
                            controlErrPlot("\n# verify skiped\n",1);
                            serialPort1.DiscardInBuffer();
                            progEventStatus++;
                            break;
                        }
                        //等待下位机回送校验码
                        controlmsgPlot("\n# flash verify by checksum\n");
                        int dlycnt = 15;
                        while (true)
                        {
                            if (ChecksumReced) break;
                            DelayMs(10);
                            dlycnt--;
                            if (dlycnt <= 0) break;
                        }                                                                    
                        //烧录后验证
                        if (verify_flash(binStartAddr, BinClass.binbytes) == false)
                        {
                            progEventStatus = 15;                            
                            controlmsgPlot("# flash verify failed !!\n"+
                                "binsCheckSumRec : 0x"+ binsCheckSumRec.ToString("X8")+"\n"                               
                            );
                            if (ChecksumReced)
                            {
                                controlmsgPlot("binsCheckSumCal : 0x" + BinClass.ImageChecksum().ToString("X8") + "\n");
                            }
                            else
                            {
                                controlmsgPlot("no checksum stirng received\n");
                            }

                        }
                        else
                        {
                            controlmsgPlot("# flash verify OK\n");
                        }
                        //清空接收缓存
                        controlmsgPlot("\n# clear serialPort Rx buffer\n");
                        serialPort1.DiscardInBuffer();
                        progEventStatus++;
                        break;
                    case 8:
                        //正常结束烧录
                        progEventdone = true;
                        isTargetOff = false;
                        isContinueBtn = false;
                        checkVreifyEn = false;
                        waitcontinue = false;
                        button3.Text = "Program";
                        button3.Enabled = true;
                        if (verifySkiped)
                        {
                            controlErrPlot("\n# please check target backdata in this window," +
                                                "  and look for the text \"verify\" witch forecolor is orange\n",1);
                            controlmsgPlot("\n# programming finish\n");
                        }
                        else
                        {
                            controlmsgPlot("\n# programming Succeed :)\n\n");
                        }
                        progEventStatus = 0;
                        DelayMs(1000);
                        UartClose();
                        return;
                    case 15:
                        //异常结束烧录
                        UartClose();                        
                        ackcheckEn = false;
                        ackReced = false;
                        progEventdone = true;
                        isTargetOff = false;
                        checkVreifyEn = false;
                        isContinueBtn = false;
                        waitcontinue = false;
                        button3.Text = "Program";
                        button3.Enabled = true;
                        progEventStatus = 0;
                        controlmsgPlot("\n# programming failed\n");
                        return;
                    default:
                        progEventStatus = 15;
                        break;
                }
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            textBox1.Clear();
            label3.Text = "0%";
            SetTextMessage(0);
            BinClass.Clear();
        }

        private void 关于ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show( "version"+ versionstring + Environment.NewLine +
                            "Copyright 2002-2020 APT,Inc. All rights reserved");
        }

        private void 保存烧录日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string curAppDir = Directory.GetCurrentDirectory();
            //MessageBox.Show(curAppDir);
            controlmsgPlot(curAppDir);

            string filepath = curAppDir + "\\" + "APT_ISP.log";
            if (File.Exists(filepath)) File.Delete(filepath);
            FileStream fs = new FileStream(filepath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            (sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            if ((sender as ToolStripMenuItem).Checked) isSavelogEN = true;
            string wdata = richTextBox2.Text;
            if (isSavelogEN)
            {
                isSavelogEN = false;
                //开始写入
                sw.Write(wdata);
                //清空缓冲区
                sw.Flush();
                //关闭流
                sw.Close();
                fs.Close();                
            }
        }

        private void disp_uart_params()
        {
            string disp;
            int i;
            string[] token = { "Port: COM", "Baudrate: ", "Databits: ", "Parity: ", "Stopbits: " , "FrameSize: ", "FrameInterval: "};
            string[] Parityoption = { "None", "Odd", "Even", "Mark", "Space" };
            string[] Stopbitsopt = { "0", "1", "2", "1.5" };

            disp = "串口参数\r\n";
            for (i = 0; i < 3; i++)
            {
                disp += token[i] + usartParamBuf[i].ToString() + "\r\n";
            }
            i = 3;
            disp += token[i] + Parityoption[usartParamBuf[i]] + "\r\n";
            i++;
            disp += token[i] + Stopbitsopt[usartParamBuf[i]] + "\r\n";
            i++;
            disp += token[i] + usartParamBuf[i].ToString() + "\r\n";
            i++;
            disp += token[i] + usartParamBuf[i].ToString() + "\r\n";
            controlmsgPlot(disp);
        }

        private void recUartparam(int[] buf)
        {
            richTextBox2.Clear();
            for (int  i = 0; i < NuartParam; i++)
            {
               usartParamBuf[i] = buf[i];
            }
            disp_uart_params();
            DelayMs(10);
            open_com();
        }


        public static bool COMsettingWindowOn = false;
        Form2 Fm2 = null;

        private void 串口设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (COMsettingWindowOn)
            {
                Fm2.Activate();
                Fm2.WindowState = FormWindowState.Normal;
                return;
            }
            COMsettingWindowOn = true;
            Fm2 = new Form2(recUartparam);
            Fm2.StartPosition = FormStartPosition.Manual;
            Fm2.Location = new Point(currentMFromLoc.X + 100, currentMFromLoc.Y + 40);//
            Fm2.Show();
            isTargetOff = false;
            button3.Text = "Program";
            progEventdone = true;
        }

        private void 使用说明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
          Help.ShowHelp(this, Application.StartupPath +@"\APTispUserguide.chm");
        }

        private static void DelayMs(int milliSecond)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < milliSecond)
            {
                Application.DoEvents();
            }
        }

        private static void SleepMs(int milliSecond)
        {
            System.Threading.Thread.Sleep(milliSecond);
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(serialPort1.IsOpen) UartClose();
            DelayMs(50);
            form1configSave();
            DelayMs(50);
            System.Environment.Exit(0);            
            DelayMs(50);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            richTextBox2.Clear();
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox2.Text == "prog")
                {
                    button3_Click(this, e);
                    textBox2.Clear();
                }
                else
                {
                    textBox2.Clear();
                }
            }
        }

        private void statusStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void panel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void mainForm_SizeChanged(object sender, EventArgs e)
        {
            if (ControlsInfo.Count > 0)//如果字典中有数据，即窗体改变
            {
                ControlsChaneInit(this.Controls[0]);//表示pannel控件
                ControlsChange(this.Controls[0]);

            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private delegate void SetPos(int ipos);

        private void SetTextMessage(int ipos)
        {
            if (this.InvokeRequired)
            {
                SetPos setpos = new SetPos(SetTextMessage);
                this.Invoke(setpos, new object[] { ipos });
            }
            else
            {
                this.toolStripProgressBar2.Value = Convert.ToInt32(ipos);
            }
        }

        private void toolStripProgressBar2_Click(object sender, EventArgs e)
        {

        }

        private void writebinTofile(string fullname, byte[] bb)
        {
            //定义文件信息对象
            FileInfo finfo = new FileInfo(fullname);
            //判断文件是否存在
            if (finfo.Exists)
            {
                //删除该文件
                finfo.Delete();
            }
            BinaryWriter bw = null;
            try
            {
               // 创建文件
                bw = new BinaryWriter(new FileStream(fullname,FileMode.Create));
                // 写入文件
                bw.Write(bb);
                // 关闭文件
                bw.Close();
            }
            catch (IOException e)
            {
               MessageBox.Show(e.Message+"\r\n");
               return;
            }
            finally
            {
                if(bw != null)
                {
                    bw.Close();
                }
            }
        }

        private void writeIhexTofile(string fullname, string content)
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



        private void 生成bin文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fLASH参数设置ToolStripMenuItem_Click(sender,e);
            while (Starthex2bin == false) Application.DoEvents();
            controlmsgPlot("flash SIze is:" + (BinClass.flashSize / 1024).ToString() + "KB\r\n");
            string filepath = textBox1.Text;
            filepath = openHexfile(filepath);
            if(hex2binDone == true)
            {
                string curAppDir = Directory.GetCurrentDirectory();
                string binfpath = curAppDir + "\\" + Path.GetFileNameWithoutExtension(filepath) + ".bin";
                controlmsgPlot("bin filepath: " + binfpath + "\r\n");
                writebinTofile(binfpath, BinClass.binbytes);
                //MessageBox.Show("转换完成\r\n");
                controlmsgPlot("save binfile done! \r\n");
            }
            else
            {
                MessageBox.Show("hex文件有误，转换失败 \r\n");
            }
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
           // richTextBox2.SelectionStart = richTextBox2.TextLength;
           // Scrolls the contents of the control to the current caret position.
           // richTextBox2.ScrollToCaret();
        }

        private void aPT32F01ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void mainForm_Move(object sender, EventArgs e)
        {
            currentMFromLoc = new Point(this.Location.X, this.Location.Y);
        }

        private void saveCombineIhexfile(string combfpath, string combfname, string instr)
        {
            string curAppDir = Directory.GetCurrentDirectory();
            if(!Directory.Exists(combfpath)) combfpath = curAppDir + "\\" + combfname;
            else combfpath = combfpath + "\\" + combfname;
            controlmsgPlot("Combine file path: " + combfpath + "\r\n");
            writeIhexTofile(combfpath, instr);
            controlmsgPlot("save CombIhexfile done! \r\n");
            MessageBox.Show("合并完成\n");
        }


        public static bool combinWindowOn = false;
        Form4 Fm4 = null;


        private void 合并hex文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (combinWindowOn)
            {
                Fm4.Activate();
                Fm4.WindowState = FormWindowState.Normal;
                return;
            }
            string curAppDir = Directory.GetCurrentDirectory();
            Fm4 = new Form4(saveCombineIhexfile, curAppDir);
            Fm4.StartPosition = FormStartPosition.Manual;
            Fm4.Location = new Point(currentMFromLoc.X + 100, currentMFromLoc.Y + 40);//通过修改Point里的坐标就可以自己设定起始位置了 
            Fm4.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label3.Text = ((int)(100 * Progedpecent)).ToString() + "%";
            SetTextMessage((int)(100 * Progedpecent));
        }

        private void versionNoteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(vernotestr);
        }

        private void fLASH参数设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Starthex2bin = false;
            Form3 Fm3 = new Form3();
            Fm3.StartPosition = FormStartPosition.Manual;
            Fm3.Location = new Point(currentMFromLoc.X + 100, currentMFromLoc.Y + 40);//通过修改Point里的坐标就可以自己设定起始位置了
            Fm3.Show();
        }

        private void 烧录后验证ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            verifySkiped = !verifySkiped;
            烧录后验证ToolStripMenuItem.Checked = !verifySkiped;
        }

        private bool form1configSave()
        {
            bool ret = false;
            string[] param = new string[20];
            param[0] = versionstring;
            param[1] = BinClass.flashStartAddr.ToString();
            param[2] = BinClass.flashSize.ToString();
            param[3] = BinClass.flashPading.ToString();
            if (verifySkiped) param[4] = "1";
            else param[4] = "0";

            param = param.Take(5).ToArray();
            ret = MyAppconfig.Save("misc", param,5);
            return ret;
        }


        private bool checkParam(Dictionary<string, string> item)
        {
            bool ret = true;
            if (item.Keys == null) return false;
            if (item.Keys.Count != MyAppconfig.misclist.Length) return false;
            foreach (string key in item.Keys)
            {
                if (MyAppconfig.misclist.ToList().IndexOf(key) == -1)
                {
                    return false;
                }
                switch (key)
                {
                    case "tool-version":
                        break;
                    default:
                        if (Validator.IsUnsignedInteger(item[key], sizeof(UInt32)) == false)
                        {
                            return false;
                        }
                        break;
                }
   
            }
            return ret;
        }


        private bool form1LoadConfig()
        {
            bool ret = false;
            Dictionary<string, string> Item = new Dictionary<string, string>();
            ret = MyAppconfig.Load("misc", ref Item);
            if (ret && checkParam(Item))
            { 
                BinClass.flashStartAddr = Convert.ToUInt32(Item["ROMstart"]);
                BinClass.flashSize = Convert.ToUInt32(Item["ROMsize"]);
                BinClass.flashEndAddr = BinClass.flashSize + BinClass.flashStartAddr;
                BinClass.flashPading = Convert.ToUInt32(Item["Padding"]);
                if(Convert.ToUInt32(Item["VerifyEn"])!=0) verifySkiped = true;
                else verifySkiped = false;
                ret = true;
            }
            else
            {
                ret = false;
            }
            return ret;
        }

        private bool checkFrom2Param(Dictionary<string, string> item)
        {
            bool ret = false;            
            if (item == null || item.Keys == null) return false;
            if (item.Keys.Count != MyAppconfig.uartkeylist.Length) return false;
            foreach (string key in item.Keys)
            {
                if (MyAppconfig.uartkeylist.ToList().IndexOf(key) == -1)
                {
                    return false;
                }
                if (Validator.IsIntegerNotNagtive(item[key]) == false)
                {
                    return false;
                }
            }

            ret = true;
            return ret;
        }
        private bool form2LoadConfig()
        {
            bool ret = false;
            Dictionary<string, string> Item = new Dictionary<string, string>();
            ret = MyAppconfig.Load("uartportSet", ref Item);
            if (ret && checkFrom2Param(Item))
            {
                usartParamBuf[0] = Convert.ToInt32(Item["Uart-Port"]);
                usartParamBuf[1] = Convert.ToInt32(Item["Uart-Baud"]);
                usartParamBuf[2] = Convert.ToInt32(Item["Uart-Dbits"]);
                usartParamBuf[3] = Convert.ToInt32(Item["Uart-Parity"]);
                usartParamBuf[4] = Convert.ToInt32(Item["Uart-STPbits"]);
                usartParamBuf[5] = Convert.ToInt32(Item["Uart-FrameSize"]);
                usartParamBuf[6] = Convert.ToInt32(Item["Uart-FrameInterval"]);
            }
            else
            {
                ret = false;
            }
            return ret;
        }

        private bool checkFrom5Param(Dictionary<string, string> item)
        {
            bool ret = false;
            if (item == null || item.Keys == null) return false;
            if (item.Keys.Count != MyAppconfig.form5keylist.Length) return false;
            foreach (string key in item.Keys)
            {
                if (MyAppconfig.form5keylist.ToList().IndexOf(key) == -1)
                {
                    return false;
                }
            }

            ret = true;
            return ret;
        }

        public static bool read_hex_mat(string ss,out byte[] hexmat)
        {
            bool ret = false;
            hexmat = null;
            ss = ss.Replace(" ", "").Replace("0x", "").Replace("0X", "");
            ss = ss.Replace(",", "").Replace(":", "").Replace(";", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
            ret = Validator.IsHexadecimal(ss);
            if (ret == false) return ret;
            int charcnt = 20;
            if (ss.Length < charcnt) return false;            
            ss = ss.Substring(0, charcnt);
            charcnt = charcnt / 2;
            byte[] tmphex = new byte[charcnt];
            for (int i = 0; i < charcnt; i++)
            {
                try
                {
                    // 每两个字符是一个 byte。 
                    tmphex[i] = byte.Parse(ss.Substring(i * 2, 2),System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    MessageBox.Show(" not a valid hex number!");
                    return false;
                }
            }
            hexmat = tmphex;
            ret = true;
            return ret;
        }

        public static bool get_syncPatterns(string[] ss)
        {
            bool ret = false;
            if (ss == null || ss.Length < 2) return false;
            byte[] tmp = new byte[startpattern.Length];
            ArrayList al = new ArrayList();
            for (int i = 0; i < 2; i++)
            {
                ret = read_hex_mat(ss[i],out tmp);
                if (ret == false) return false;
                al.Add(tmp);
            }
            if(ret == true)
            {
                startpattern = (byte[])al[0];
                rstpattern = (byte[])al[1];
            }
            return ret;
        }

        private bool form5LoadConfig()
        {
            bool ret = false;
            Dictionary<string, string> Item = new Dictionary<string, string>();
            ret = MyAppconfig.Load("syncPatterns", ref Item);
            string[] tmp = new string[2];
            if (ret && checkFrom5Param(Item))
            {
                tmp[0] = Item["Form5-STApattern"];
                tmp[1] = Item["Form5-RSTpattern"];
                get_syncPatterns(tmp);
            }
            else
            {
                ret = false;
            }
            return ret;
        }


        public static void plot_syncpatterns()
        {
            controlmsgPlot("start Pattern:" + binplot(startpattern) + "\n");
            controlmsgPlot("reset Pattern:" + binplot(rstpattern) + "\n");
        }


        public static bool rstPatternWindowOn = false;
        Form5 Fm5= null;


        private void 开始和复位字符序列ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (rstPatternWindowOn)
            {
                Fm5.Activate();
                Fm5.WindowState = FormWindowState.Normal;
                return;
            }
            rstPatternWindowOn = true;
            Fm5 = new Form5();
            Fm5.StartPosition = FormStartPosition.Manual;
            Fm5.Location = new Point(currentMFromLoc.X + 100, currentMFromLoc.Y + 40);//
            Fm5.Show();
        }
    }
}
