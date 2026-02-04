using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class BinClass
    {
        public BinClass()
        {
            InitializeComponent();
        }

        private void statusPlot(string ss)
        {
            mainForm.controlmsgPlot(ss);
        }

        private void ErrPlot(string ss,int level)
        {
            mainForm.controlErrPlot(ss,level);
        }
        

        public static byte[] binbytes;
        public static UInt32 flashSize = 2*1024*1024;
        public static UInt32 flashStartAddr = 0;
        public static UInt32 flashEndAddr = flashSize + flashStartAddr;
        public static UInt32 flashPading = 0xFFFFFFFF;
        public static UInt32 Pagesize = 1024;

        public static UInt32 ramSize = 2 * 1024;
        public static UInt32 ramStartAddr = 0;
        public static UInt32 ramEndAddr = ramSize + ramStartAddr;

        public static string Errstring;


        public static UInt32 extSegAddr = 0;
        public static UInt32 CS_IP_Addr = 0;
        public static UInt32 extLAddr = 0;
        public static UInt32 EIPaddr = 0;
        public static UInt32 Checksum = 0;
        public static bool hexfileReady = false;

        private static UInt32 startaddr = 0;
        private static UInt32 Endaddr = 0;


        private struct oneline {
            public byte mark;
            public UInt32 reclen;
            public UInt32 offset;
            public byte rectyp;
            public byte[] data;
            public int chksum;
        }



        private oneline linex;




        private void InitializeComponent()
        {

        }

        public static void Clear() {
            binbytes = null;
            Errstring = "";
            extSegAddr = 0;
            CS_IP_Addr = 0;
            extLAddr = 0;
            EIPaddr = 0;
            Checksum = 0;
        }




        /// <summary>
        /// 判断是否十六进制格式字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsHexadecimal(string str)
        {
            const string PATTERN = @"^[A-Fa-f0-9]+$";
            if (str.Length > 2)
            {
                string temp = str.Substring(0, 2);
                if (temp == "0x" || temp == "0X")
                {
                    str = str.Substring(2);
                }
            }
            return System.Text.RegularExpressions.Regex.IsMatch(str, PATTERN);
        }

        /// <summary>
        /// 判断是否八进制格式字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsOctal(string str)
        {
            const string PATTERN = @"^[0-7]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(str, PATTERN);
        }

        /// <summary>
        /// 判断是否二进制格式字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBinary(string str)
        {
            const string PATTERN = @"^[0-1]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(str, PATTERN);
        }

        /// <summary>
        /// 判断是否十进制格式字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDecimal(string str)
        {
            const string PATTERN = @"^[0-9]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(str, PATTERN);
        }

        public static int GetLineNum()
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1, true);
            return st.GetFrame(0).GetFileLineNumber();
        }



        public static UInt32 FlashStart()
        {
            return flashStartAddr;        
        }

        public static UInt32 FlashEnd()
        {
            return flashEndAddr;
        }

        public static UInt32 addrEnd()
        {
            return Endaddr;
        }

        public static UInt32 pagesize()
        {
            return Pagesize;
        }


        public static UInt32 addrbegin()
        {
            return startaddr;
        }

        public static UInt32 ImageEIPaddr()
        {
            return EIPaddr;
        }

        public static UInt32 ImageChecksum()
        {
            return Checksum;
        }

        public static bool ImageReady()
        {
            return hexfileReady;
        }


        public static UInt32 getBinsChecksum()
        {
            long sum = 0;
            long temp32 = 0;
            int i, j;
            if (binbytes == null) return 0;
            int loop = binbytes.Length / 4;
            int rem = binbytes.Length % 4;
            for (i = 0; i < loop; i++)
            {
                temp32 = 0;
                for (j = 0; j < 4; j++)
                {
                    temp32 <<= 8;
                    temp32 += binbytes[i * 4 + 3 - j];
                }
                sum += temp32;
            }
            if (rem > 0)
            {
                temp32 = flashPading;
                for (j = 0; j < rem; j++)
                {
                    temp32 <<= 8;
                    temp32 += binbytes[i * 4 + rem - 1 - j];
                }
                sum += temp32;
            }
            Checksum = (UInt32)(sum & 0xFFFFFFFF);
            return Checksum;
        }

        public bool binget(string fname)
        {
            bool ret = false;
            BinaryReader br=null;
            int imageSize = 0;
            try
            {
                FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
                imageSize = (int)fs.Length;
                br = new BinaryReader(fs);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
            if (br != null && imageSize>0)
            {
                hexfileReady = false;
                byte[] tmpbytes = new byte[imageSize];
                for (int i = 0; i < imageSize; i++)
                {
                    tmpbytes[i] = 0xFF;
                }
                tmpbytes = br.ReadBytes(imageSize);
                int skip = 0;
                string fileext = Path.GetExtension(fname);
                switch (fileext)
                {
                    case ".bin":
                        startaddr = 0;
                        break;
                    case ".aptbin":
                        skip = 32;
                        if (imageSize <= 32) return false;
                        imageSize = imageSize - 32;
                        startaddr = (UInt32)((tmpbytes[3] << 24) + (tmpbytes[2] << 16) + (tmpbytes[1] << 8) + (tmpbytes[0] << 0));
                        break;
                    default:
                        startaddr = 0;
                        break;
                }                
                binbytes = new byte[imageSize];
                Endaddr = startaddr + (UInt32)imageSize - 1;
                Array.Copy(tmpbytes, skip, binbytes, 0, imageSize);
                getBinsChecksum();
                statusPlot("\nbins startAddr:  " + "0x" + startaddr.ToString("X") + "\r\n");
                statusPlot("bins EndAddr:  " + "0x" + Endaddr.ToString("X") + "\r\n");
                statusPlot("bins EIPaddr:  " + "????" + "\r\n");
                statusPlot("bins checksum:  " + "0x" + Checksum.ToString("X") + "\r\n");
                statusPlot("bins paddingvalue  " + "0x" + flashPading.ToString("X") + "\r\n");
                statusPlot("data byte(no padding) count:  " + imageSize.ToString() + "\r\n");
                statusPlot("image byte count:  " + imageSize.ToString() + "\r\n");
                statusPlot("image Located addr range: 0x" + startaddr.ToString("X") + "(" + ((startaddr - flashStartAddr) / 1024.0).ToString() + "kB)" + " - 0x" + Endaddr.ToString("X") + "(" + Math.Round(((linex.offset - flashStartAddr) / 1024.0), 3).ToString() + "kB)" + "\r\n");
                hexfileReady = true;
                ret = true;
            }
            return ret;
        }


        public bool hexget(string hex)
        {
            string linebytes;
            bool err = false;
            byte[] tmpbytes = new byte[flashSize];
            int linecnt = 0;
            bool EOF = false;
            bool skipRAMSegment = false;
            extSegAddr = 0;
            CS_IP_Addr = 0;
            extLAddr = 0;
            EIPaddr = 0;

            for (int i=0;i< flashSize; i++)
            {
                tmpbytes[i] = 0xFF;
            }

            bool flashsettingErr = false;

            using (StringReader sr = new StringReader(hex))
            {

                hexfileReady = false;
                UInt32 datacnt = 0;
                UInt32 delta = 0;
                int tmpcnt = 0;
                string hexstring;
                UInt32 curaddr = 0;
                linex.offset = 0;
                byte[] tmpBmat = new byte[22];
                int errcodeline = 0;
     

                while ((linebytes = sr.ReadLine()) != null)
                {
                    linecnt++;
                    byte[] bb = Encoding.UTF8.GetBytes(linebytes);
                    linex.mark = bb[0];
                    if (linex.mark != ':') { errcodeline = GetLineNum(); err = true; break; }
                    tmpcnt = bb.Length;
                    if (tmpcnt < 11 || tmpcnt > 43 || tmpcnt%2 ==0) { errcodeline = GetLineNum(); err = true; break; }
                    hexstring = linebytes.Substring(1, linebytes.Length - 1);
                    if (!IsHexadecimal(hexstring)) { errcodeline = GetLineNum(); err = true; break; }
                    tmpcnt = (tmpcnt -1)/2;
                    for (int i = 0; i < tmpcnt; i++)
                    {
                        try
                        {
                            // 每两个字符是一个 byte。 
                            tmpBmat[i] = byte.Parse(hexstring.Substring(i * 2, 2),
                            System.Globalization.NumberStyles.HexNumber);
                        }
                        catch
                        {
                            MessageBox.Show(" not a valid hex number!");
                            { errcodeline = GetLineNum(); err = true; break; }
                        }
                    }
                    //检查检验和
                    linex.chksum = 0x00;
                    for (int i = 0; i < tmpcnt; i++)
                    {
                        linex.chksum += tmpBmat[i];
                    }
                    linex.chksum %= 256;
                    if (linex.chksum != 0x0) { errcodeline = GetLineNum(); err = true; break; }
                    //检查数据个数
                    linex.reclen = tmpBmat[0];
                    if (linex.reclen != tmpcnt -5) { errcodeline = GetLineNum(); err = true; break; }
                    linex.rectyp = tmpBmat[3];
                    if (skipRAMSegment == true && linex.rectyp != 5) linex.rectyp = 6;
                    if (linex.rectyp == 5) skipRAMSegment = false;
                    switch (linex.rectyp)
                    {
                        case 0:
                            curaddr = (UInt32)(tmpBmat[1] * 256 + tmpBmat[2]) + extLAddr + extSegAddr;
                            if (curaddr < linex.offset)
                            {
                                statusPlot("\nErr!! :本行数据与上行数据有交叉，ihex文件有误！\n");
                                errcodeline = GetLineNum(); err = true;
                            }
                            if (linecnt > 1)
                            {
                                //if (curaddr != linex.offset) { errcodeline = GetLineNum(); err = true; break; }
                            }
                            linex.offset = curaddr + linex.reclen;
                            if (linecnt == 1)
                            {
                                startaddr = (UInt32)curaddr;
                                if (startaddr % 4 != 0)
                                {
                                    MessageBox.Show("起始地址必须4字节对齐，即满足：startaddr % 4 = 0 \n");
                                    errcodeline = GetLineNum(); err = true;
                                }
                            }
                            if (flashStartAddr <= linex.offset  && linex.offset <= flashEndAddr)
                            {
                                Array.Copy(tmpBmat, 4, tmpbytes, curaddr-flashStartAddr, linex.reclen);
                            }
                            else
                            {
                                errcodeline = GetLineNum(); err = true;
                            }
                            datacnt += linex.reclen;
                            if(datacnt + startaddr + delta != linex.offset)
                            {
                                //statusPlot("\nwarning!:本行地址与上行的地址不连续 \n" + linebytes +"\n");
                                //statusPlot("0x" + (datacnt+ startaddr).ToString("X") + " 0x"+ linex.offset.ToString("X") + "\n\n");
                                delta = linex.offset - startaddr - datacnt;
                            }   
                            break;
                        case 1:
                            UInt32 imagebytecnt = linex.offset - startaddr;
                            if (datacnt > 0)
                            {                                
                                binbytes = new byte[imagebytecnt];
                                Array.Copy(tmpbytes, startaddr- flashStartAddr, binbytes,0,imagebytecnt);
                            }
                            //binbytes = binbytes.Skip(1).ToArray();
                            //startaddr = startaddr + 1;
                            if ((startaddr % 4)!=0) {
                                statusPlot("\n the startAddr must be aligned,hex file wrong!!");
                                hexfileReady = false;
                            }
                            if (linex.offset > 0) Endaddr = linex.offset - 1;
                            else Endaddr = 0;
                            getBinsChecksum();
                            statusPlot("\nbins startAddr:  " + "0x" +  startaddr.ToString("X") + "\r\n");
                            statusPlot("bins EndAddr:  " + "0x" + Endaddr.ToString("X") + "\r\n");
                            statusPlot("bins EIPaddr:  " + "0x" + EIPaddr.ToString("X") + "\r\n");
                            statusPlot("bins checksum:  " + "0x" + Checksum.ToString("X") + "\r\n");
                            statusPlot("bins paddingvalue  " + "0x" + flashPading.ToString("X") + "\r\n");
                            statusPlot("data byte(no padding) count:  " + datacnt.ToString() + "\r\n");
                            statusPlot("image byte count:  " + imagebytecnt.ToString() + "\r\n");
                            statusPlot("image Located addr range: 0x" + startaddr.ToString("X")+"(" + ((startaddr-flashStartAddr)/1024.0).ToString() +"kB)" + " - 0x" + Endaddr.ToString("X") + "(" + Math.Round(((linex.offset - flashStartAddr) / 1024.0),3).ToString() + "kB)" + "\r\n");
                            if (startaddr > Endaddr)
                            {
                                ErrPlot("\n Err: bin addr data is wrong,maybe flash setting is wrong!!\n", 0);
                                flashsettingErr = true;
                                hexfileReady = false;
                            }
                            else
                            {
                                hexfileReady = true;
                            }
                            EOF = true;
                            break;
                        case 2:
                            if (linex.reclen == 2) { 
                                extSegAddr = (UInt32)((tmpBmat[4] << 12) + (tmpBmat[5] << 4));
                             }
                            else{
                                errcodeline = GetLineNum(); err = true;
                            }
                             break;
                        case 3:
                            break;
                        case 4:
                            if (linex.reclen == 2)
                            {
                                extLAddr = (UInt32)((tmpBmat[4] << 24) + (tmpBmat[5] << 16));
                                if (extLAddr >= flashEndAddr)
                                {
                                    ErrPlot("\n Warning:address exceed flashEndAddr setting!!\n", 1);
                                    skipRAMSegment = true;
                                }
                                if (linecnt == 1)
                                {
                                    startaddr = (UInt32)(extLAddr + extSegAddr);
                                    if (startaddr % 4 != 0)
                                    {
                                        MessageBox.Show("起始地址必须4字节对齐，即满足：startaddr % 4 = 0 \n");
                                        errcodeline = GetLineNum(); err = true;
                                    }
                                }
                            }
                            else{
                                errcodeline = GetLineNum(); err = true;
                            }
                            break;
                        case 5:
                            if (linex.reclen == 4)
                            {
                                EIPaddr = (UInt32)((tmpBmat[4] << 24) + (tmpBmat[5] << 16) + (tmpBmat[6] << 8) + (tmpBmat[7] << 0));
                            }
                            else
                            {
                                errcodeline = GetLineNum(); err = true;
                            }
                            break;
                        case 6:
                            //skip data located at RAM range
                            break;
                        default:
                            errcodeline = GetLineNum();
                            err = true;
                            break;
                    }
                    if(err || EOF) break;
                }
                if (err)
                {
                    Errstring = "Hex文件有误，请检查hex文件\nErrcode:" + errcodeline.ToString() + "\n" + linecnt.ToString() + " " + linebytes;
                    MessageBox.Show(Errstring);
                }
            }
            if (flashsettingErr)
            {
                Errstring = "startaddr > Endaddr, maybe flash setting is wrong\n";
                err = true;
            }
            return err;
        }
    }

}
