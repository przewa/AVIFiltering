using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AVIFiltering
{
    class DetailsFileProcesor
    {
        private String filePath { get; set; }
        private int fillCnt { get; set; }
        public DetailsFileProcesor(String filePath, int fillCnt)
        {
            this.filePath = filePath;
            this.fillCnt = fillCnt;
        }

        public bool ProcesFile()
        {
            String dirBase = String.Format("{0}\\0_FD", Path.GetDirectoryName(filePath));
            if (!Directory.Exists(dirBase))
            {
                Directory.CreateDirectory(dirBase);
            }
            String _fd = String.Format("{0}\\{1}", dirBase, Path.GetFileName(filePath));

            if (File.Exists(_fd))
            {
                File.Delete(_fd);
            }

            File.Copy(filePath, _fd);

            if (!File.Exists(filePath))
            {
                MessageBox.Show(String.Format("Brak pliku {0}", filePath));
                return false;
            }
            String[] fileLines = File.ReadAllLines(filePath);
            List<String> newDetails = new List<String>();
            newDetails.Add(fileLines[0]);
            int i = 1;
            while(i<fileLines.Length)
            {
                String line = fileLines[i];
                string[] splitedLine = line.Split(';');
                if (splitedLine[2].Equals("Y"))
                {
                    int od = newDetails.Count - fillCnt;
                    if (od < 0)
                    {
                        od = 0;
                    }
                    int doVal = fillCnt;
                    if (doVal > newDetails.Count)
                    {
                        doVal = newDetails.Count;
                    }
                    newDetails.RemoveRange(od, doVal);
                    FillY(i, fileLines, newDetails);
                    i = i + fillCnt + 1;
                }
                else
                {
                    newDetails.Add(line);
                    i++;
                }
            }
            File.WriteAllLines(filePath, newDetails);
            return true;
        }

        public void FillY(int yPoz, String[] lines, List<String> newDetails)
        {
            int startPoz = yPoz - fillCnt;
            int stopPoz = yPoz + fillCnt;

            if (startPoz < 0)
            {
                startPoz = 0;
            }

            if (stopPoz >= lines.Length)
            {
                stopPoz = lines.Length-1;
            }

            for(int i = startPoz; i <= stopPoz; i++)
            {
                String[] splLine = lines[i].Split(';');
                newDetails.Add(String.Format("{0};{1};Y", splLine[0], splLine[1]));
            }
        }
    }
}
