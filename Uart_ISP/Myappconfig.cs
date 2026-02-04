using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class MyAppconfig
    {
        public static void writeConfigfile(string fullname, string content)
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

        public static string[] form4keylist =
        {
            "Form4-textbox1",
            "Form4-textbox2",
            "Form4-textbox3",
            "Form4-textbox4",
            "Form4-textbox5",
            "Form4-Combmode",
            "Form4-file0",
            "Form4-file1",
            "Form4-file2",
            "Form4-file3",
            "Form4-file4",
            "Form4-file5",
            "Form4-file6",
            "Form4-file7",
            "Form4-filesel",
        };

        public static string[] form5keylist =
{
            "Form5-STApattern",
            "Form5-RSTpattern",
        };

        public static string[] uartkeylist =
        {
            "Uart-Port",
            "Uart-Baud",
            "Uart-Dbits",
            "Uart-Parity",
            "Uart-STPbits",
            "Uart-FrameSize",
            "Uart-FrameInterval",
        };

        public static string[] misclist =
        {
            "tool-version",
            "ROMstart",
            "ROMsize",
            "Padding",
            "VerifyEn",
        };

        public static bool Load(string sect, ref Dictionary<string, string> items)
        {
            bool ret = false;
            //获取Configuration实例：
            string m_curPath = AppDomain.CurrentDomain.BaseDirectory;
            string m_ConfigFullName = Path.Combine(m_curPath, mainForm.toolname + "_GlobalSetup.config");
            Configuration config = null;
            if (File.Exists(m_ConfigFullName))
            {
                config = ConfigurationExtensions.GetConfiguration(m_ConfigFullName);
                Dictionary<string, string> orgItem = ConfigurationExtensions.GetKeyValueSectionValues(config, sect);
                if (orgItem == null || orgItem.Keys.Count<=0)
                {
                    return false;
                }
                string[] tg = null;
                switch (sect)
                {
                    case "syncPatterns":
                        tg = form5keylist;
                        break;
                    case "hexCombine":
                        tg = form4keylist;
                        break;
                    case "uartportSet":
                        tg = uartkeylist;
                        break;
                    case "misc":
                        tg = misclist;
                        break;
                    default:
                        break;
                }
                if (tg == null) return false;

                Dictionary<string, string> remItem = new Dictionary<string, string>();

                for (int i = 0; i < orgItem.Keys.Count; i++)
                {
                    string key = orgItem.Keys.ToArray()[i];
                    if (tg.ToList().IndexOf(key) == -1)
                    {
                        remItem[key] = orgItem[key];
                        orgItem.Remove(key);                 
                    }
                }
                ConfigurationExtensions.RemoveKeyValueSectionValues(config, sect, remItem);
                ret = true;
                foreach (string key in tg)
                {
                    if (orgItem.Keys.ToList().IndexOf(key) == -1)
                    {
                        ret = false;
                        break;
                    }
                }
                if(ret == false)
                {
                    MessageBox.Show("配置文件有误！\n");
                }
                else
                {
                    items = orgItem;
                    ret = true;
                }
            }
            else
            {
                ret = false;
            }   
           
            return ret;
        }


        public static bool Save(string sect,string[] param,int paranum)
        {
            bool ret = false;
            //获取Configuration实例：
            string m_curPath = AppDomain.CurrentDomain.BaseDirectory;
            string m_ConfigFullName = Path.Combine(m_curPath, mainForm.toolname + "_GlobalSetup.config");
            Configuration config = null;
            if (File.Exists(m_ConfigFullName))
            {
                config = ConfigurationExtensions.GetConfiguration(m_ConfigFullName);
            }
            else
            {
                string toolver = mainForm.versionstring;
                string rn = Environment.NewLine;
                string initstr = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + rn +
                                 "<configuration>" + rn +
                                 "  <uartportSet>" + rn +
                                 "    <add key=\"Uart-Port\" value=\"4\" />" + rn +
                                 "    <add key=\"Uart-Baud\" value=\"115200\" />" + rn +
                                 "    <add key=\"Uart-Dbits\" value=\"8\" />" + rn +
                                 "    <add key=\"Uart-Parity\" value=\"0\" />" + rn +
                                 "    <add key=\"Uart-STPbits\" value=\"1\" />" + rn +
                                 "    <add key=\"Uart-FrameSize\" value=\"64\" />" + rn +
                                 "    <add key=\"Uart-FrameInterval\" value=\"4\" />" + rn +
                                 "  </uartportSet>" + rn +
                                 "  <hexCombine>" + rn +
                                 "    <add key=\"Form4-textbox1\" value=\"2\" />" + rn +
                                 "    <add key=\"Form4-textbox2\" value=\"0\" />" + rn +
                                 "    <add key=\"Form4-textbox3\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-textbox4\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-textbox5\" value=\"combHex.ihex\" />" + rn +
                                 "    <add key=\"Form4-Combmode\" value=\"0\" />" + rn +              
                                 "    <add key=\"Form4-file0\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file1\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file2\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file3\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file4\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file5\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file6\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-file7\" value=\"\" />" + rn +
                                 "    <add key=\"Form4-filesel\" value=\"3\" />" + rn +
                                 "  </hexCombine>" + rn +
                                 "  <misc>" + rn +
                                 "    <add key=\"tool-version\" value=\"" + toolver + "\" />" + rn +
                                 "  </misc>" + rn +
                                 "  <syncPatterns>" + rn +
                                 "    <add key=\"Form5-STApattern\" value=\"0xEF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0xFF\" />" + rn +
                                 "    <add key=\"Form5-RSTpattern\" value=\"0xEF, 0xF5, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xFF\" />" + rn +
                                 "  </syncPatterns>" + rn +
                                 "</configuration>";
                writeConfigfile(m_ConfigFullName, initstr);
            }
            if (config != null)
            {
                Dictionary<string, string> tmpdict = new Dictionary<string, string>();
                string[] tg = null;
                switch (sect)
                {
                    case "syncPatterns":
                        tg = form5keylist;
                        break;
                    case "hexCombine":
                        tg = form4keylist;
                        break;
                    case "uartportSet":
                        tg = uartkeylist;
                        break;
                    case "misc":
                        tg = misclist;
                        break;
                    default:
                        break;
                }
                if (tg == null) return false;
                for (int i = 0; i < tg.Length; i++)
                {
                    tmpdict.Add(tg[i], param[i]);
                }
                config.Sections.Remove(sect);
                ConfigurationExtensions.UpdateKeyValueSectionValues(config, sect, tmpdict);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(sect);
            }
            //MessageBox.Show("暂未实现此功能 :)");
            //MessageBox.Show("配置已保存");
            return true;
        }
    }
}
