using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AVIFiltering
{
    static class Program
    {
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            String startupDir = "";
            bool start = false;
            int frameVal = 0;
            bool klatkiOtoczeniaSet = false;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Equals("/Start"))
                {
                    start = args[i].Equals("/Start");
                }else if (args[i].StartsWith("/Frame"))
                {
                    klatkiOtoczeniaSet = Int32.TryParse(args[i].Replace("/Frame=", ""), out frameVal);                       
                }
                else
                {
                    startupDir = args[1];
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(startupDir, start, klatkiOtoczeniaSet, frameVal));
        }
    }
}
