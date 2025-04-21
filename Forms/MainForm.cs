using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Objects;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AVIFiltering.Forms;
using AVIFiltering.Lib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace AVIFiltering
{
    public partial class MainForm : Form
    {
        private String folderPath = String.Empty;
        private bool autoStart = false;
        private bool klatkiOtoczeniaSet = false;

        public MainForm(String startupDir, bool autoStart, bool klatkiOtoczeniaSet, int klatkiOtoczenia)
        {
            InitializeComponent();
            CenterToScreen();
            label1.Height = this.Height - 100;
            listView1.Dock = DockStyle.Fill;

            this.klatkiOtoczeniaSet = klatkiOtoczeniaSet;

            if (klatkiOtoczenia > 0)
            {
                textBox1.Text = klatkiOtoczenia.ToString();
            }

            if (!startupDir.Equals(String.Empty) && Directory.Exists(startupDir))
                ProcessDirectory(new string[] { startupDir });

            if (autoStart)
                this.autoStart = autoStart;
            {
                Console.WriteLine("AppStart");
                start();
            }
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDirectory((string[])e.Data.GetData(DataFormats.FileDrop, false));
        }

        private void ProcessDirectory(string[] path)
        {
            string[] s = path;
            int i;
            for (i = 0; i < 1; i++)
            {
                if (Directory.Exists(s[i]))
                {
                    string[] filesInDir = Directory.GetFiles(s[i], "*.*").Where(it => it.EndsWith(".avi") || it.EndsWith(".AVI")).ToArray();
                    foreach (string fileInDir in filesInDir)
                    {
                        AddListViewItem(fileInDir);
                    }
                }
                else
                {
                    AddListViewItem(s[i]);
                }
                lblCatalog.Text = s[i];
                folderPath = s[i];
                CheckRoiFileExists(folderPath);
            }

            label1.Visible = false;
            listView1.Visible = true;
            
            if (listView1.Items.Count > 0)
                listView1.Items[0].Selected = true;
        }

        private void AddListViewItem(string fileWithPath)
        {
            FileInfo fI = new FileInfo(fileWithPath);
            long lengthInK = fI.Length / 1024;
            string forDisplay = lengthInK.ToString("N0") + " KB";

            AddListViewItem(
                Path.GetFileName(fileWithPath),
                File.GetCreationTime(fileWithPath).ToString(),
                Path.GetExtension(fileWithPath),
                forDisplay
            );
        }

        private void AddListViewItem(string nazwa, string dataModyfikacji, string typ, string rozmiar)
        {
            listView1.Items.Add(new ListViewItem(new[] { nazwa, dataModyfikacji, typ, rozmiar }) );
        }

        private void SetAdminMode()
        {
            if (Directory.Exists(folderPath))
            {
                AdminForm adminForm = new AdminForm(folderPath, listView1.Items);
                adminForm.ShowDialog();
                CheckRoiFileExists(folderPath);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode==Keys.F && e.Control && e.Shift)
            {
                SetAdminMode();
            }
        }

        private bool CheckRoiFileExists(String folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*_ROI.csv");
            bool result = files.Count() == 1;
            if (result)
            {
                btnAnalizuj.Enabled = result;
            }
            return result;
        }

        private void btnAnalizuj_Click(object sender, EventArgs e)
        {
            start();
        }

        private void start()
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                backgroundWorker1.CancelAsync();
            }
        }

        private FileAnalizeResult AnalizeFile(String fileToAnalize, int startFrame, int fileTotalY, int fileTotalN)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();

            String fDetFile = Functions.GetAnalizePathFileName(folderPath, "Filtering_Details");

            List<String> filteringDetails = new List<String>();
            
            if (File.Exists(fDetFile))
            {
                filteringDetails.AddRange( File.ReadAllLines(fDetFile) );
            }

            List<Rectangle> rectList = Functions.GetRoiRect(folderPath);
            double simWspVal = Functions.GetSimWspValue(folderPath);
    
            VideoCapture cptr = new VideoCapture(fileToAnalize);
            Mat m;
            List<double> prevAvg = new List<double>();

            foreach(Rectangle rect in rectList)
            {
                prevAvg.Add(0);
            }

            int totalFrames = (int)cptr.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
          
            if (progressBarFrames.InvokeRequired)
            {
                progressBarFrames.Invoke(new MethodInvoker(delegate
                {
                    progressBarFrames.Maximum = totalFrames;
                }));
            }

            int fileY = fileTotalY;
            int fileN = fileTotalN;
            int framesInFile = startFrame;

            int frameNumber = 0;
            
            m = cptr.QueryFrame();
           
            while (true)
            {
                if (m == null)
                {
                    break;
                }

                if (backgroundWorker1.CancellationPending)
                {
                    return null;
                }
                framesInFile++;
                if (progressBarFrames.InvokeRequired)
                {
                    progressBarFrames.Invoke(new MethodInvoker(delegate
                    {
                        if (frameNumber > progressBarFrames.Maximum)
                        {
                            progressBarFrames.Maximum = frameNumber;
                        }
                        progressBarFrames.Value = frameNumber;
                    }));
                }
                if (startFrame > 0 && frameNumber < (startFrame))
                {
                    m = cptr.QueryFrame();
                    frameNumber++;
                    continue;
                }
                //cptr.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, frameNumber);
                //cptr.Read(m);

                List<double> avg = new List<double>();

                foreach (Rectangle rect in rectList)
                {
                    try
                    {
                        using (Image<Bgr, byte> img = m.ToImage<Bgr, byte>())
                        {
                            img.ROI = rect;
                            avg.Add(Functions.calculateAvg(img.Bitmap));
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            using (Image<Gray, byte> img = m.ToImage<Gray, byte>())
                            {
                                img.ROI = rect;
                                avg.Add(Functions.calculateAvg(img.Bitmap));
                            }
                        }
                        catch (Exception e)
                        {
                            Environment.Exit(1);
                        }
                    }

                }
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //img.Save(String.Format("C:\\frames\\{0}.jpg", frameNumber));


                if (startFrame>0 && frameNumber == startFrame)
                {
                    prevAvg.Clear();
                    prevAvg.AddRange(avg);
                }
                if (frameNumber == 0)
                {
                    filteringDetails.Add(String.Format("{0};{1};Y", Path.GetFileNameWithoutExtension(fileToAnalize), (frameNumber + 1)));
                    fileN++;
                }
                else
                {
                    for (int i = 0; i < avg.Count; i++)
                    {
                        double similarity = Functions.calcluateSimilarity(prevAvg[i], avg[i]);

                        if (similarity >= simWspVal)
                        {
                            filteringDetails.Add(String.Format("{0};{1};N", Path.GetFileNameWithoutExtension(fileToAnalize), (frameNumber + 1)));
                            fileY++;
                        }
                        else
                        {
                            filteringDetails.Add(String.Format("{0};{1};Y", Path.GetFileNameWithoutExtension(fileToAnalize), (frameNumber + 1)));
                            fileN++;
                        }
                    }
                }
                prevAvg.Clear();
                prevAvg.AddRange(avg);

                //File.WriteAllLines(fDetFile, filteringDetails.ToArray());
                m.Dispose();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                m = cptr.QueryFrame();

                frameNumber++;
            }

            File.WriteAllLines(fDetFile, filteringDetails.ToArray());

            if (progressBarFrames.InvokeRequired)
            {
                progressBarFrames.Invoke(new MethodInvoker(delegate
                {
                    progressBarFrames.Maximum = frameNumber;
                }));
            }

            //m.Dispose();
            cptr.Dispose();
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            
            FileAnalizeResult result = new FileAnalizeResult();
            result.totalFrames = framesInFile;
            result.totalN = fileN;
            result.totalY = fileY;

            sw.Stop();
            Console.WriteLine("Elapsed={0} FileName=[{1}]", sw.Elapsed, fileToAnalize);

            return result;
        }

        private FileAnalizeResult GetFileFramesData(String fileName, string filteringDetailsFile)
        {
            FileAnalizeResult result = new FileAnalizeResult();
            result.totalY = 0;
            result.totalN = 0;
            result.totalFrames = 0;

            using (var reader = new StreamReader(filteringDetailsFile))
            {
                string fName = String.Empty;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');

                    if (fileName.Equals(values[0]))
                    {
                        if (values[2].Equals("Y"))
                        {
                            result.totalY++;
                        }
                        else
                        {
                            result.totalN++;
                        }
                        result.totalFrames++;
                    }
                }
            }

            return result;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> filesToAnalize = new List<string>();

            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    foreach (ListViewItem itm in listView1.Items)
                    {
                        filesToAnalize.Add(Functions.getFileWithPath(folderPath, itm.Text));
                    }
                }));
            }

            Dictionary<string, int> analizedFiles = new Dictionary<string, int>();
            String fDetFile = Functions.GetAnalizePathFileName(folderPath, "Filtering_Details");
            int lineCnt = 0;
            if (File.Exists(fDetFile))
            {
                List<String> tmpLines = new List<String>();
                using (var reader = new StreamReader(fDetFile))
                {
                    string fName = String.Empty;
                    while (!reader.EndOfStream)
                    {
                        lineCnt++;
                        var line = reader.ReadLine();
                        var values = line.Split(';');

                        if (values.Count() == 3) {
                            if (!fName.Equals(values[0]))
                            {
                                if (analizedFiles.ContainsKey(values[0]))
                                {
                                    analizedFiles[values[0]] = Int32.Parse(values[1]);
                                }
                                else
                                {
                                    analizedFiles.Add(values[0], Int32.Parse(values[1]));
                                }
                            }
                            tmpLines.Add(line);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (lineCnt != tmpLines.Count())
                {
                    File.Delete(fDetFile);
                    File.WriteAllLines(fDetFile, tmpLines);
                }
            }

            int allFilestotalY = 0;
            int allFilestotalN = 0;
            int allFilesFrames = 0;

            List<String> filtering = new List<String>();
            int file = 0;
            foreach (String fileNameWithPath in filesToAnalize)
            {
                file++;
                String fileName = Path.GetFileNameWithoutExtension(fileNameWithPath);

                VideoCapture cptr = new VideoCapture(fileNameWithPath);
                int totalFrames = (int)cptr.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
                cptr.Dispose();
                FileAnalizeResult result = null;

                if (label4.InvokeRequired)
                {
                    label4.Invoke(new MethodInvoker(delegate
                    {
                        label4.Text = String.Format("{0}/{1}", file, listView1.Items.Count);
                        label4.Refresh();
                    }));
                }

                if (analizedFiles.ContainsKey(fileName) && analizedFiles[fileName] < totalFrames)
                {
                    FileAnalizeResult tmp = GetFileFramesData(fileName, fDetFile);
                    result = AnalizeFile(fileNameWithPath, analizedFiles[fileName]/*nie trzeba dodawac 1*/, tmp.totalY, tmp.totalN);
                }
                else if(!analizedFiles.ContainsKey(fileName))
                {
                    result =  AnalizeFile(fileNameWithPath, 0, 0, 0);
                }
                else
                {
                    result = GetFileFramesData(fileName, fDetFile);
                }

                if (result != null)
                {
                    allFilestotalY += result.totalY;
                    allFilestotalN += result.totalN;
                    allFilesFrames += result.totalFrames;

                    double fileTimeWorkRaw = Functions.GetTimeFromFrames(result.totalFrames);
                    double fileTimeAfterFiltering = Functions.GetTimeFromFrames(result.totalN);
                    filtering.Add(String.Format("{0};{1};{2}", Path.GetFileNameWithoutExtension(fileNameWithPath), result.totalFrames, Functions.CalcRatio(fileTimeAfterFiltering, fileTimeWorkRaw)));
                }

                if (backgroundWorker1.CancellationPending)
                {
                    return;
                }
            }
           

            double timeTotal = Functions.GetTimeFromFrames(allFilesFrames);
            double afterFilteringTotalTime = Functions.GetTimeFromFrames(allFilestotalN);
            double totalRatio = Functions.CalcRatio(afterFilteringTotalTime, timeTotal);

            filtering.Add("Timework_RAW: " + Functions.FormatTimeWork(timeTotal));
            filtering.Add("Timework_After_Filtering: " + Functions.FormatTimeWork(afterFilteringTotalTime));
            filtering.Add("Ratio: " + totalRatio);

            String fFilteringFile = Functions.GetAnalizePathFileName(folderPath, "Filtering");
            File.Delete(fFilteringFile);
            File.WriteAllLines(fFilteringFile, filtering.ToArray());

            konwertujDetails(6, fDetFile, true);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBarFrames.Value = 0;
            if (autoStart)
            {
                if (klatkiOtoczeniaSet)
                {
                    Konwersja();
                }
                Console.WriteLine("AppEnd");
                //Environment.Exit(0);
                Application.Exit();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Konwersja();
        }

        private void Konwersja()
        {
            try
            {
                int ileKlatek = 0;
                Int32.TryParse(textBox1.Text, out ileKlatek);
                if (ileKlatek <= 0)
                {
                    if(!autoStart)
                        MessageBox.Show("Podaj ilość klatek");
                }
                else
                {
                    String fName = Functions.GetAnalizePathFileName(folderPath, "Filtering_Details");
                    konwertujDetails(ileKlatek, fName, autoStart);
                }
            }
            catch (Exception ex)
            {
                Environment.Exit(1);
            }
        }

        private void konwertujDetails(int ileKlatek, String detailsFile, Boolean silent)
        {
            DetailsFileProcesor dfp = new DetailsFileProcesor(detailsFile, ileKlatek);
            if (dfp.ProcesFile())
            {
                if (label5.InvokeRequired)
                {
                    label5.Invoke(new MethodInvoker(delegate
                    {
                        label5.Text = ileKlatek.ToString();
                        label5.Refresh();
                    }));
                }
                else
                {
                    label5.Text = ileKlatek.ToString();
                }
                if (!silent)
                {
                    MessageBox.Show(String.Format("Plik {0} został skonwertowany", detailsFile));
                }
            }
        }

    }
}
