using AVIFiltering.Lib;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListView;

namespace AVIFiltering.Forms
{
    public partial class AdminForm : Form
    {
       
        private Boolean isMouseDown = false;
        private Point locationXY;
        private Point locationX1Y1;
        private Rectangle roiRect;
        private String analizePath;
        private Color dfColor;
        private List<Rectangle> rectList;

        public AdminForm(String analizePath, ListViewItemCollection listViewItems)
        {
            InitializeComponent();
            CenterToParent();
            rectList = new List<Rectangle>();
            dfColor = btnPodobienstwoZmniejsz.BackColor;
            this.analizePath = analizePath;
            foreach (ListViewItem item in listViewItems)
            {
                this.listView1.Items.Add((ListViewItem)item.Clone());
            }
            if (File.Exists(Functions.GetAnalizePathFileName(analizePath, "ROI")))
            {
                rectList.AddRange( Functions.GetRoiRect(analizePath) );
            }
        }
        private String getFileWithPath(int pos = 0)
        {
            if (listView1.InvokeRequired)
            {
                string tmp = String.Empty;
                listView1.Invoke(new MethodInvoker(delegate
                {
                    tmp = String.Format("{0}\\{1}", analizePath, listView1.SelectedItems[pos].Text);
                }));
                return tmp;
            }
            else
            {
                return String.Format("{0}\\{1}", analizePath, listView1.SelectedItems[pos].Text);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if(backgroundWorker1.IsBusy)
            {
                return;
            }
            isMouseDown = true;
            locationXY = e.Location;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouseDown) return;
            int x = Math.Min(locationXY.X, e.X);
            int y = Math.Min(locationXY.Y, e.Y);
            int width = Math.Max(locationXY.X, e.X) - Math.Min(locationXY.X, e.X);
            int height = Math.Max(locationXY.Y, e.Y) - Math.Min(locationXY.Y, e.Y);
            roiRect = new Rectangle(x, y, width, height);
            try
            {
                pictureBox1.Refresh();
            }
            catch (Exception ex)
            {
                //blad techniczny
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isMouseDown == true)
            {
                locationX1Y1 = e.Location;
                isMouseDown = false;
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Red, 2))
            {
                foreach (Rectangle rect in rectList)
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }

                e.Graphics.DrawRectangle(pen, roiRect);
            }
        }

        private void ShowSingleFrame(string fileToShow, int frame=0)
        {
            VideoCapture cptr = new VideoCapture(fileToShow);
            hScrollBar1.Maximum = (int)cptr.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
            Mat m = new Mat();
            cptr.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, frame);
            cptr.Read(m);
            Image<Bgr, byte> img = m.ToImage<Bgr, byte>();

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }

            pictureBox1.Image = img.Bitmap;
            pictureBox1.Refresh();
            pictureBox1.Width = img.Width;
            pictureBox1.Height = img.Height;

            m.Dispose();
            cptr.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private double getSimWsp()
        {
            return Double.Parse(txtSimWsp.Text);
        }

        private void showNumerAnalizowanejKlatki(int nrKlatki, int totalFrames)
        {
            lbNranalizowanejKlatki.Text = String.Format("{0}/{1}", (nrKlatki).ToString(), totalFrames);
            lbNranalizowanejKlatki.Refresh();
        }

        void AdminAnalize()
        {
            string fileToAnalize = getFileWithPath();

            VideoCapture cptr = new VideoCapture(fileToAnalize);

            Mat m = new Mat();

            List<double> prevAvg = new List<double>();

            foreach(Rectangle rect in rectList)
            {
                prevAvg.Add(0);
            }
                   
            int totalFrames = (int)cptr.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);

            for (int i = 0; i < totalFrames; i++)
            {
                if (backgroundWorker1.CancellationPending)
                {
                    break;
                }

                backgroundWorker1.ReportProgress(i, totalFrames);
                
                cptr.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, i);
                cptr.Read(m);
                Image<Bgr, byte> img = m.ToImage<Bgr, byte>();

                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                if (pictureBox1.InvokeRequired)
                {
                    pictureBox1.Invoke(new MethodInvoker(delegate
                    {
                        pictureBox1.Image = img.Bitmap;
                        pictureBox1.Refresh();
                    }));
                }
                List<double> avgList = new List<double>();
                foreach (Rectangle rect in rectList)
                {
                    img.ROI = rect;
                    avgList.Add(Functions.calculateAvg(img.Bitmap));
                }

                if (i > 0)
                {
                    for (int k = 0; k < avgList.Count; k++)
                    {
                        double similarity = Functions.calcluateSimilarity(prevAvg[k], avgList[k]);

                        if (label4.InvokeRequired)
                        {
                            label4.Invoke(new MethodInvoker(delegate
                            {
                                label4.Text = similarity.ToString();
                                label4.Refresh();
                            }));
                        }

                        if (similarity >= getSimWsp())
                        {
                            backgroundWorker1.ReportProgress(-1, totalFrames);
                        }
                        else
                        {
                            backgroundWorker1.ReportProgress(-2, totalFrames);
                        }
                        Thread.Sleep(50);
                    }
                }

                prevAvg.Clear();
                prevAvg.AddRange(avgList);

            }
            m.Dispose();
            cptr.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                ShowSingleFrame(getFileWithPath());
                hScrollBar1.Value = 0;
            }
        }

        private double getSimChangeValue()
        {
            return AppConfig.Instance.getPodobienstwoValueChange();
        }

        private void btnPodobienstwoZwieksz_Click(object sender, EventArgs e)
        {
            txtSimWsp.Text = ( getSimWsp() + getSimChangeValue() ).ToString();
        }

        private void btnPodobienstwoZmniejsz_Click(object sender, EventArgs e)
        {
            txtSimWsp.Text = (getSimWsp() - getSimChangeValue()).ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.WorkerSupportsCancellation = true;
                backgroundWorker1.WorkerReportsProgress = true;
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                backgroundWorker1.CancelAsync();
            }
        }

        private void AdminForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
            List<string> roilines = new List<string>();
            roilines.Add(getSimWsp().ToString());
            foreach (Rectangle rect in rectList)
            {
                string[] lines = new string[4];
                
                lines[0] = rect.X.ToString();
                lines[1] = rect.Y.ToString();
                lines[2] = rect.Width.ToString();
                lines[3] = rect.Height.ToString();

                roilines.AddRange(lines);
            }
            
            File.WriteAllLines(Functions.GetAnalizePathFileName(analizePath, "ROI"), roilines); 
        }

        private void hScrollBar1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
                ShowSingleFrame(getFileWithPath(), hScrollBar1.Value);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            AdminAnalize();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int numerKlatki = e.ProgressPercentage;
            if (numerKlatki >=0)
            {
                numerKlatki++;
                int totalFrames = (int)e.UserState;
                hScrollBar1.Value = numerKlatki + 1;
                showNumerAnalizowanejKlatki(numerKlatki, totalFrames);
            }
            else
            {
                if(numerKlatki == -1)
                {
                    btnPodobienstwoZmniejsz.BackColor = Color.LimeGreen;
                    btnPodobienstwoZwieksz.BackColor = Color.LimeGreen;
                }else if(numerKlatki == -2)
                {
                    btnPodobienstwoZmniejsz.BackColor = dfColor;
                    btnPodobienstwoZwieksz.BackColor = dfColor;
                }
                btnPodobienstwoZmniejsz.Refresh();
                btnPodobienstwoZwieksz.Refresh();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            rectList.Clear();
            roiRect = new Rectangle();
            pictureBox1.Refresh();
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            rectList.Add(roiRect);
            pictureBox1.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (rectList.Count()>0)
            {
                rectList.Remove(rectList.Last());
                pictureBox1.Refresh();
            }
        }

        private void AdminForm_Load(object sender, EventArgs e)
        {

        }
    }
}
