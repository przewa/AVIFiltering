using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AVIFiltering.Lib
{
    public sealed class AppConfig
    {
        private static AppConfig m_oInstance = null;
        private IniData data;
        private AppConfig()
        {
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\AVIFiltering.ini";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, Properties.Resources.AVIFiltering);
            }
            var parser = new FileIniDataParser();
            data = parser.ReadFile(path);
        }

        public static AppConfig Instance
        {
            get
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new AppConfig();
                }
                return m_oInstance;
            }
        }

        public double getPodobienstwoValueChange()
        {
            return Double.Parse(data["Config"]["ValueChange"]);
        }
    }
}
