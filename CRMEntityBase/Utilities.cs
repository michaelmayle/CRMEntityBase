using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections;

namespace CRMEntityBase
{
    public static class Data
    {
        public static bool ByteArrayEquals(byte[] b1, byte[] b2)
        {
            if (b1 == b2)
                return true;

            if (b1 == null || b2 == null)
                return false;

            if (b1.Length != b2.Length)
                return false;

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                    return false;
            }

            return true;
        }

        public static bool DictionaryEquals(IDictionary targetValue, IDictionary compareValue)
        {
            if (targetValue == null || compareValue == null)
                return false;

            if (targetValue.Count != compareValue.Count)
                return false;

            foreach (object key in targetValue.Keys)
            {
                if (!compareValue.Contains(key))
                    return false;

                if (!compareValue[key].Equals(targetValue[key]))
                    return false;
            }

            return true;
        }

        public static bool ListEquals(IList targetValue, IList compareValue)
        {
            if (targetValue == null || compareValue == null)
                return false;

            if (targetValue.Count != compareValue.Count)
                return false;

            for (int i = 0; i < targetValue.Count; i++)
            {
                if (!targetValue[i].Equals(compareValue[i]))
                    return false;
            }

            return true;
        }

        public static bool DateEquals(DateTime Date1, DateTime Date2)
        {
            Date1 = new DateTime(
                Date1.Ticks - (Date1.Ticks % TimeSpan.TicksPerSecond),
                Date1.Kind
                );

            Date2 = new DateTime(
                Date2.Ticks - (Date2.Ticks % TimeSpan.TicksPerSecond),
                Date2.Kind
                );

            return Math.Abs(Date1.Subtract(Date2).TotalMilliseconds) <= 10;
        }

        public static object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }
            else
            {
                return null;
            }
        }
    }

    public static class Config
    {
        private static NameValueCollection _configCollection;
        private static DateTime _refreshTime;
        private static string _strServer;
        private static string _currentDirectory;
        public static string Environment;

        /// <summary>
        /// Builds a list of custom config keys that are specific to a certain environment as determined by the host server.
        /// </summary>
        public static void LoadConfig()
        {
            Config.Environment = Config.GetEnvironment();
            _configCollection = ConfigurationManager.GetSection("AwareConfig/Environments/" + Config.Environment) as NameValueCollection;
            _refreshTime = DateTime.Now;
        }

        /// <summary>
        ///  used for unit testing
        /// </summary>
        /// <param name="Environment"></param>
        /// <param name="RefreshTime"></param>
        public static void SetConfig(string Environment, DateTime RefreshTime)
        {
            Config.Environment = Environment;
            _configCollection = ConfigurationManager.GetSection("AwareConfig/Environments/" + Config.Environment) as NameValueCollection;
            _refreshTime = RefreshTime;
        }

        /// <summary>
        ///  used for unit testing
        /// </summary>
        /// <param name="Server"></param>
        /// <param name="CurDirectory"></param>
        /// <param name="RefreshTime"></param>
        public static void SetConfig(string Server, string CurDirectory, DateTime RefreshTime)
        {
            _strServer = Server;
            _currentDirectory = CurDirectory;
            Config.GetEnvironment();
            _configCollection = ConfigurationManager.GetSection("AwareConfig/Environments/" + Config.Environment) as NameValueCollection;
            _refreshTime = RefreshTime;
        }

        /// <summary>
        /// used to cleanup unit test messes
        /// </summary>
        public static void ResetConfig()
        {
            _configCollection = null;
            _refreshTime = DateTime.MinValue;
            _strServer = null;
            _currentDirectory = null;
            Environment = null;
        }

        /// <summary>
        /// Gets the environment.
        /// </summary>
        /// <returns>environment</returns>
        public static string GetEnvironment()
        {
            _strServer = System.Environment.MachineName.ToUpper();
            _currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            NameValueCollection configSections = ConfigurationManager.GetSection("AwareConfig/HostServer") as NameValueCollection;
            string strCollection = configSections[_strServer];

            if (strCollection == null)
                throw new ApplicationException("HostServer not defined. See AwareConfig/HostServer in configuration file.");

            return strCollection;
        }

        public static System.Collections.Specialized.NameObjectCollectionBase.KeysCollection GetEnvironments()
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            return config.SectionGroups["AwareConfig"].SectionGroups["Environments"].Sections.Keys;
        }

        /// <summary>
        /// Get a value from the custom config collection.
        /// </summary>
        /// <param name="pKey">Key parameter is used to find a specific value in the custom config collection.</param>
        /// <returns>
        /// A string value.
        /// </returns>
        public static string GetValue(string pKey)
        {
            try
            {
                if (_refreshTime.AddMinutes(5) < DateTime.Now)
                    _configCollection = null;

                if (_configCollection == null || _configCollection.Count == 0)
                    Config.LoadConfig();

                if (_configCollection[pKey] != null)
                    return _configCollection[pKey].ToString();
                else if (ConfigurationManager.AppSettings[pKey] != null)
                    return ConfigurationManager.AppSettings[pKey];
                else
                    return string.Empty;
            }
            catch
            {
                return ConfigurationManager.AppSettings[pKey];
            }
        }

        /// <summary>
        /// Gets the config info.
        /// </summary>
        /// <returns>config info</returns>
        public static string GetConfigInfo()
        {
            if (_refreshTime.AddMinutes(5) < DateTime.Now)
                _configCollection = null;

            if (_configCollection == null)
                Config.LoadConfig();

            string strConfigInfo = "Server : " + _strServer + "\n";
            strConfigInfo += "Directory : " + _currentDirectory + "\n";
            strConfigInfo += "Refresh Time : " + _refreshTime.ToString() + "\n";
            strConfigInfo += "\nConfig Settings:\n";

            foreach (string strKey in _configCollection.Keys)
            {
                strConfigInfo += "\t" + strKey + " : " + _configCollection[strKey] + "\n";
            }

            return strConfigInfo;
        }
    }
}
