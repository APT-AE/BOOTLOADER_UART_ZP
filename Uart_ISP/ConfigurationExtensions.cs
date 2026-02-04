using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace System.Configuration
{
    public static class ConfigurationExtensions
    { 

        ///<summary>依据连接串名字connectionName返回数据连接字符串 </summary> 
        ///<param name="connectionName">连接串的</param> 
        ///<param name="config"></param>
        ///<returns></returns> 
        public static string GetConnectionStringsConfig(this Configuration config, string connectionName)
        {
            string connectionString = config.ConnectionStrings.ConnectionStrings[connectionName].ConnectionString;
            ////Console.WriteLine(connectionString); 
            return connectionString;
        }

        ///<summary> 
        ///更新连接字符串  
        ///</summary> 
        ///<param name="newName">连接字符串名称</param> 
        ///<param name="newConString">连接字符串内容</param> 
        ///<param name="newProviderName">数据提供程序名称</param> 
        ///<param name="config">Configuration实例</param>
        public static void UpdateConnectionStringsConfig(this Configuration config, string newName, string newConString, string newProviderName)
        {
            bool isModified = false;
            //记录该连接串是否已经存在      
            //如果要更改的连接串已经存在      
            if (config.ConnectionStrings.ConnectionStrings[newName] != null)
            { isModified = true; }

            //新建一个连接字符串实例      
            ConnectionStringSettings mySettings = new ConnectionStringSettings(newName, newConString, newProviderName);

            // 如果连接串已存在，首先删除它      
            if (isModified)
            {
                config.ConnectionStrings.ConnectionStrings.Remove(newName);
            }
            // 将新的连接串添加到配置文件中.      
            config.ConnectionStrings.ConnectionStrings.Add(mySettings);
            // 保存对配置文件所作的更改      
            config.Save(ConfigurationSaveMode.Modified);
        }


        ///<summary> 
        ///返回config文件中appSettings配置节的value项  
        ///</summary> 
        ///<param name="strKey"></param> 
        ///<param name="config">Configuration实例</param>
        ///<returns></returns> 
        public static string GetAppSettingsItemValue(this Configuration config, string strKey)
        {
            foreach (KeyValueConfigurationElement key in config.AppSettings.Settings)
            {
                if (key.Key == strKey)
                {
                    return config.AppSettings.Settings[strKey].Value;
                }
            }
            return string.Empty;
        }


        /// <summary>
        /// 获取所有的appSettings的节点。
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetAppSettings(this Configuration config)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (KeyValueConfigurationElement key in config.AppSettings.Settings)
            {
                dict[key.Key] = key.Value;
            }
            return dict;
        }


        ///<summary>  
        ///更新在config文件中appSettings配置节增加一对键、值对。
        ///</summary>  
        ///<param name="newKey"></param>  
        ///<param name="newValue"></param>  
        ///<param name="config"></param>
        public static void UpdateAppSettingsItemValue(this Configuration config, string newKey, string newValue)
        {
            UpdateAppSettingsItemNoSave(config, newKey, newValue);
            ////// Save the changes in App.config file.      
            config.Save(ConfigurationSaveMode.Modified);

            ////// Force a reload of a changed section.      
            ////ConfigurationManager.RefreshSection("appSettings");
        }



        /// <summary>
        /// 删除 appSettings的一个节点。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="key"></param>
        public static void RemoveAppSettingsItemValue(this Configuration config, string key)
        {
            config.AppSettings.Settings.Remove(key);
            config.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        /// 删除 appSettings的多个节点
        /// </summary>
        /// <param name="config"></param>
        /// <param name="keys"></param>
        public static void RemoveAppSettingsItems(this Configuration config, string[] keys)
        {
            foreach (string key in keys)
                config.AppSettings.Settings.Remove(key);
            config.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        ///更新在config文件中appSettings配置节增加多对键、值对。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="items"></param>
        public static void UpdateAppSettings(this Configuration config, Dictionary<string, string> items)
        {
            foreach (string key in items.Keys)
            {
                UpdateAppSettingsItemNoSave(config, key, items[key]);
            }
            config.Save(ConfigurationSaveMode.Modified);
        }

        private static void UpdateAppSettingsItemNoSave(Configuration config, string newKey, string newValue)
        {
            bool isModified = false;
            foreach (KeyValueConfigurationElement key in config.AppSettings.Settings)
            {
                if (key.Key == newKey)
                { isModified = true; }
            }

            // You need to remove the old settings object before you can replace it      
            if (isModified)
            { config.AppSettings.Settings.Remove(newKey); }

            // Add an Application Setting.      
            config.AppSettings.Settings.Add(newKey, newValue);
        }


        /// <summary>
        ///     通用获取key-value 键值对Section值的集合，
        ///     可用于DictionarySectionHandler或NameValueSectionHandler 定义的配置节
        ///     NameValueSectionHandler的Key值不能重复
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="config"></param>
        /// <returns>没有配置节时返回null</returns>
        public static Dictionary<string, string> GetKeyValueSectionValues(this Configuration config, string sectionName)
        {
            ////KeyValueConfigurationSection appSettings = (KeyValueConfigurationSection)config.GetSection(sectionName);
            var section = config.GetSection(sectionName);

            if (section == null)
                return null;

            Dictionary<string, string> result = new Dictionary<string, string>();

            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(section.SectionInformation.GetRawXml());
            System.Xml.XmlNode xnode = xdoc.ChildNodes[0];

            IDictionary dict = (IDictionary)(new DictionarySectionHandler().Create(null, null, xnode));
            foreach (string str in dict.Keys)
            {
                result[str] = (string)dict[str];
            }

            return result;
        }

        /// <summary>
        ///     获取子节点为key-value 键值对的值，
        ///     可用于DictionarySectionHandler或NameValueSectionHandler 定义的配置节
        /// </summary>
        /// <param name="sectionName">定点名称</param>
        /// <param name="key">key 的值，不存在的Key值将返回空</param>
        /// <param name="config">打开的配置文件。</param>
        /// <returns></returns>
        public static string GetKeyValueSectionItemValue(this Configuration config, string sectionName, string key)
        {
            var section = config.GetSection(sectionName).SectionInformation;
            if (section == null)
                return null;

            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(section.GetRawXml());
            System.Xml.XmlNode xnode = xdoc.ChildNodes[0];

            IDictionary dict = (IDictionary)(new DictionarySectionHandler().Create(null, null, xnode));
            if (dict.Contains(key))
                return (string)dict[key];
            else
                return null;
        }


        /// <summary>
        /// 更新配置节，相同的就修改，没有的就增加。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <param name="items"></param>
        public static void UpdateKeyValueSectionValues(this Configuration config, string sectionName, Dictionary<string, string> items)
        {
            Dictionary<string, string> orgItem = GetKeyValueSectionValues(config, sectionName);
            if (orgItem == null)
            {
                orgItem = new Dictionary<string, string>();
            }
            foreach (string key in items.Keys)
            {
                orgItem[key] = items[key];
            }
            UpdateKeyValueSection(config, sectionName, orgItem);
        }

        private static void UpdateKeyValueSection(Configuration config, string sectionName, Dictionary<string, string> items)
        {
            config.Sections.Remove(sectionName);

            AppSettingsSection section = new AppSettingsSection();
            config.Sections.Add(sectionName, section);

            foreach (string key in items.Keys)
            {
                section.Settings.Add(new KeyValueConfigurationElement(key, items[key]));
            }
            section.SectionInformation.Type = typeof(DictionarySectionHandler).AssemblyQualifiedName;
            config.Save(ConfigurationSaveMode.Modified);
        }


        /// <summary>
        /// 删除配置点的一些配置。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <param name="items"></param>
        public static void RemoveKeyValueSectionValues(this Configuration config, string sectionName, Dictionary<string, string> items)
        {
            Dictionary<string, string> orgItem = GetKeyValueSectionValues(config, sectionName);
            if (orgItem != null)
            {
                foreach (string key in items.Keys)
                {
                    orgItem.Remove(key);
                }
                UpdateKeyValueSection(config, sectionName, orgItem);
            }
        }

        /// <summary>
        /// 删除配置节。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        public static void RemoveSection(this Configuration config, string sectionName)
        {
            config.Sections.Remove(sectionName);
            config.Save(ConfigurationSaveMode.Modified);
        }


        /// <summary>
        /// 获取appSettings的配置值。Key值 和 T 的静态属性相同才能取出来。
        /// </summary>
        /// <param name="config">打开的配置实例</param>
        /// <typeparam name="T">要取值的类，类的静态属性要和Key值对应</typeparam>
        public static void GetAppSettingsConfigValue<T>(this Configuration config) where T : class
        {
            //通过反射自动值，增加属性只需把配置的key值和属性的名称相同即可。
            try
            {
                ////Type cfgType = typeof(ConfigUtility);

                ////MethodInfo getAppConfigMethod = cfgType.GetMethod("GetAppConfig", BindingFlags.Static | BindingFlags.Public);
                Type etyType = typeof(T);
                foreach (PropertyInfo pinfo in etyType.GetProperties(BindingFlags.Static | BindingFlags.Public))
                {
                    string keyName = pinfo.Name;
                    string rslt = GetAppSettingsItemValue(config, keyName);
                    Type dType = pinfo.DeclaringType;
                    if (pinfo.PropertyType.IsValueType)
                    {
                        //类型转换
                        if (!String.IsNullOrEmpty(rslt))
                        {
                            try
                            {
                                MethodInfo minfo = pinfo.PropertyType.GetMethod("Parse", new Type[] { typeof(string) });
                                if (minfo != null)
                                {
                                    pinfo.SetValue(null, minfo.Invoke(null, new object[] { rslt }), null);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                            }
                        }
                    }
                    else
                        pinfo.SetValue(null, rslt, null);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }       


        /// <summary>
        /// 获取SingleTagSectionHandler某节点的值。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string GetSingleTagSectionItemValue(this Configuration config, string sectionName, string property)
        {
            Dictionary<string, string> dict = GetSingleTagSectionValues(config, sectionName);
            if (dict != null && dict.Count > 0)
            {
                if (dict.ContainsKey(property))
                    return (string)dict[property];
            }
            return null;
        }

        /// <summary>
        /// 获取SingleTagSectionHandler节点的值。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetSingleTagSectionValues(this Configuration config, string sectionName)
        {
            var section = config.GetSection(sectionName);
            if (section == null || section.SectionInformation == null)
                return null;
            ConfigXmlDocument xdoc = new ConfigXmlDocument();
            xdoc.LoadXml(section.SectionInformation.GetRawXml());
            System.Xml.XmlNode xnode = xdoc.ChildNodes[0];

            Dictionary<string, string> result = new Dictionary<string, string>();
            IDictionary dict = (IDictionary)(new SingleTagSectionHandler().Create(null, null, xnode));
            foreach (string str in dict.Keys)
            {
                result[str] = (string)dict[str];
            }

            return result;
        }


        /// <summary>
        /// 更新配置节，相同的就修改，没有的就增加。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <param name="items"></param>
        public static void UpdateSingleTagSectionValues(this Configuration config, string sectionName, Dictionary<string, string> items)
        {
            Dictionary<string, string> orgItem = GetSingleTagSectionValues(config, sectionName);
            if (orgItem == null)
                orgItem = new Dictionary<string, string>();
            foreach (string key in items.Keys)
            {
                orgItem[key] = items[key];
            }
            UpdateSingleTagSection(config, sectionName, orgItem);
        }

        /// <summary>
        /// 删除配置点的一些配置。
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <param name="items"></param>
        public static void RemoveSingleTagSectionValues(this Configuration config, string sectionName, Dictionary<string, string> items)
        {
            Dictionary<string, string> orgItem = GetSingleTagSectionValues(config, sectionName);
            if (orgItem != null)
            {
                foreach (string key in items.Keys)
                {
                    orgItem.Remove(key);
                }
                UpdateSingleTagSection(config, sectionName, orgItem);
            }
        }


        private static void UpdateSingleTagSection(Configuration config, string sectionName, Dictionary<string, string> items)
        {
            config.Sections.Remove(sectionName);

            DefaultSection section = new DefaultSection();
            config.Sections.Add(sectionName, section);
            ConfigXmlDocument xdoc = new ConfigXmlDocument();
            XmlNode secNode = xdoc.CreateNode(XmlNodeType.Element, sectionName, xdoc.NamespaceURI);
            xdoc.AppendChild(secNode);
            foreach (string key in items.Keys)
            {
                XmlAttribute attr = xdoc.CreateAttribute(key);
                attr.Value = items[key];
                secNode.Attributes.Append(attr);
            }
            section.SectionInformation.SetRawXml(xdoc.OuterXml);
            section.SectionInformation.Type = typeof(SingleTagSectionHandler).AssemblyQualifiedName;
            config.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        /// 打开默认的配置文件.exe.config 
        /// </summary>
        /// <returns></returns>
        public static Configuration GetConfiguration()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        /// <summary>
        /// 获取指定的配置文件。
        /// </summary>
        /// <param name="configFullPath">配置文件的全名称</param>
        /// <returns></returns>
        public static Configuration GetConfiguration(string configFullPath)
        {
            ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
            configFile.ExeConfigFilename = configFullPath;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);
            return config;
        } 

        /// <summary>
        /// 获取自定义的配置节。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static T GetCustomSection<T>(this Configuration config, string sectionName) where T : ConfigurationSection
        {
            return (T)config.GetSection(sectionName);
        }

        /// <summary>
        /// 保存自定义配置节。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="section"></param>
        /// <param name="sectionName"></param>
        public static void SaveCustomSection<T>(this Configuration config, T section, string sectionName) where T : ConfigurationSection
        {
            config.RemoveSection(sectionName);
            config.Sections.Add(sectionName, section);
            config.Save(ConfigurationSaveMode.Modified);
        }


        private static void SetPropertiesValue(object obj, Dictionary<string, string> items, Type cfgEtyType)
        {
            BindingFlags bf = BindingFlags.Public;
            if (obj == null)
                bf |= BindingFlags.Static;
            else
                bf |= BindingFlags.Instance;

            PropertyInfo[] pinfos = cfgEtyType.GetProperties(bf);

            foreach (PropertyInfo p in pinfos)
            {
                try
                {
                    if (items.ContainsKey(p.Name))
                    {
                        string val = items[p.Name];
                        if (!string.IsNullOrEmpty(val))
                        {
                            if (p.PropertyType.IsEnum)
                            {
                                //判断是否为数字
                                if (isDigital(val))
                                {
                                    p.SetValue(obj, Enum.Parse(p.PropertyType, val, true), null);
                                }
                                else
                                {
                                    //判断是否存在该名称
                                    if (isExistEnumKey(p.PropertyType, val))
                                        p.SetValue(obj, Enum.Parse(p.PropertyType, val, true), null);
                                }
                            }
                            else if (p.PropertyType.IsValueType)   //值类型
                            {
                                MethodInfo minfo = p.PropertyType.GetMethod("Parse", new Type[] { typeof(string) });
                                if (minfo != null)
                                {
                                    p.SetValue(obj, minfo.Invoke(null, new object[] { val }), null);
                                }
                            }
                            else
                                p.SetValue(obj, val, null);
                        }
                        else if (!p.PropertyType.IsEnum && !p.PropertyType.IsValueType)
                            p.SetValue(obj, val, null);
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// 判断枚举值是否为数字
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        private static bool isDigital(string strValue)
        {
            return Regex.IsMatch(strValue, @"^(\d+)$");
        }

        /// <summary>
        /// 判断是否存在枚举的名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private static bool isExistEnumKey(Type type, string keyName)
        {
            bool isExist = false;
            foreach (string key in Enum.GetNames(type))
            {
                if (key.Equals(keyName, StringComparison.OrdinalIgnoreCase))
                {
                    isExist = true;
                    break;
                }
            }
            return isExist;
        }

        /// <summary>
        /// 使用反射获取实体配置节的值，实体的静态属性必需和配置的Key相同。
        /// </summary>属性值必需和配置节的Key等相同，包括大小写
        /// <param name="config">打开的配置实例</param>
        /// <typeparam name="T">要取值的类，类的静态属性要和Key值对应</typeparam>
        /// <param name="sectionName">T 对就的配置节的名称</param>
        public static void GetKeyValueSectionConfigValue<T>(this Configuration config, string sectionName) where T : class
        {
            try
            {
                Dictionary<string, string> dict = GetKeyValueSectionValues(config, sectionName);
                Type cfgEtyType = typeof(T);
                SetPropertiesValue(null, dict, cfgEtyType);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 由集合根据字段的属性生成实体。属性名为key，属性值为value。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T GetConfigEntityByItems<T>(this Configuration config, Dictionary<string, string> items) where T : class
        {
            Type etyType = typeof(T);
            Func<object> func = etyType.CreateInstanceDelegate();
            object ety = func.Invoke();

            SetPropertiesValue(ety, items, etyType);

            return (T)ety;
        }

        /// <summary>
        /// 由实例生成配置的项。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config">没有实际使用到</param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetItemsByConfigEntity<T>(this Configuration config, T instance) where T : class
        {
            Type cfgEtyType = typeof(T);
            BindingFlags bf = BindingFlags.Public;
            if (instance == null)
                bf |= BindingFlags.Static;
            else
                bf |= BindingFlags.Instance;
            PropertyInfo[] pinfos = cfgEtyType.GetProperties(bf);
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (PropertyInfo p in pinfos)
            {
                dict[p.Name] = "" + p.GetValue(instance, null);
            }
            return dict;
        }

        /// <summary>
        /// 由类的静态属性生成配置项。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Dictionary<string, string> GetItemsByClass<T>(this Configuration config) where T : class
        {
            return GetItemsByConfigEntity<T>(config, null);
        }

        /// <summary>
        /// 获取appSettings的配置值。Key值 和 T 的静态属性相同才能取出来。
        /// </summary>
        /// <param name="config">打开的配置实例</param>
        /// <typeparam name="T">要取值的类，类的静态属性要和Key值对应</typeparam>
        public static void GetAppSettingsConfigValueSimple<T>(this Configuration config) where T : class
        {
            //通过反射自动值，增加属性只需把配置的key值和属性的名称相同即可。
            ////Type cfgType = typeof(ConfigUtility);

            ////MethodInfo getAppConfigMethod = cfgType.GetMethod("GetAppConfig", BindingFlags.Static | BindingFlags.Public);
            Type etyType = typeof(T);

            Dictionary<string, string> items = GetAppSettings(config);

            SetPropertiesValue(null, items, etyType);
        }

        /// <summary>
        /// 保存 Section 配置的值。保存为DictionarySectionHandler配置节。
        /// </summary>
        /// <param name="config"></param>
        /// <typeparam name="T"></typeparam>
        /// <param name="sectionName"></param>
        public static void SaveKeyValueSectionConfig<T>(this Configuration config, string sectionName) where T : class
        {
            var orgsection = config.GetSection(sectionName);
            if (orgsection != null)
                config.Sections.Remove(sectionName);

            AppSettingsSection section = new AppSettingsSection();
            config.Sections.Add(sectionName, section);

            Type etyType = typeof(T);
            foreach (PropertyInfo pinfo in etyType.GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                string keyName = pinfo.Name;
                object objValue = pinfo.GetValue(null, null);
                ////section.Settings.Remove(keyName);
                section.Settings.Add(new KeyValueConfigurationElement(keyName, "" + objValue));
            }
            section.SectionInformation.Type = typeof(DictionarySectionHandler).AssemblyQualifiedName;
            config.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        /// 保存为 AppSettings 配置的值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void SaveAppSettingsConfig<T>(this Configuration config) where T : class
        {
            try
            {
                Type etyType = typeof(T);
                foreach (PropertyInfo pinfo in etyType.GetProperties(BindingFlags.Static | BindingFlags.Public))
                {
                    string keyName = pinfo.Name;

                    object objValue = pinfo.GetValue(null, null);

                    UpdateAppSettingsItemNoSave(config, keyName, "" + objValue);
                }

                config.Save(ConfigurationSaveMode.Modified);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

}
