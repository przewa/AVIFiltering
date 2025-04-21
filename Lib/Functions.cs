using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVIFiltering.Lib
{
    public static class Functions
    {
        public static String getFileWithPath(string folderPath, string file)
        {
            return String.Format("{0}\\{1}", folderPath, file);
        }

        public static double calcluateSimilarity(double avgA, double avgB)
        {
            return 1 - ((Math.Max(avgA, avgB) - Math.Min(avgA, avgB)) / (Math.Max(avgA, avgB)));
        }

        public static unsafe double calculateAvg(Bitmap bmp)
        {
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int depth = Image.GetPixelFormatSize(bmpData.PixelFormat);
            int pixelCount = bmpData.Width * bmpData.Height; 
            int bytesPerPixel = depth / 8;                  

            byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
            int stride = bmpData.Stride;

            byte unmatchingValue = 0;
            byte matchingValue = 255;

            int[] rgbPointsSummed = new int[bmpData.Height * bmpData.Width];
            int cnt = 0;

            for (int y = 0; y < bmpData.Height; y++)
            {
                byte* row = scan0 + (y * stride);

                for (int x = 0; x < bmpData.Width; x++)
                {
                    int bIndex = x * bytesPerPixel;
                    int gIndex = bIndex + 1;
                    int rIndex = bIndex + 2;

                    byte pixelR = row[rIndex];
                    byte pixelG = row[gIndex];
                    byte pixelB = row[bIndex];

                    rgbPointsSummed[cnt] = pixelR + pixelG + pixelB;
                    cnt++;
                }
            }

            bmp.UnlockBits(bmpData);
            double avg = rgbPointsSummed.Average();

            return avg;
        }

        public static String GetAnalizePathFileName(String folderPath, String fileName)
        {
            string[] pathParts = folderPath.Split('\\');
            return String.Format("{0}\\{1}_{2}_{3}.csv", folderPath, pathParts[pathParts.Count() - 2], pathParts[pathParts.Count() - 1], fileName);
        }

        public static String GetAviFileWithPath(String folderPath, String fileName, String ext = "avi")
        {
            string[] pathParts = folderPath.Split('\\');
            return String.Format("{0}\\{1}.{2}", folderPath, fileName, ext);
        }

        public static double GetSimWspValue(String folderPath)
        {
            string[] lines = File.ReadAllLines(GetAnalizePathFileName(folderPath, "ROI"));
            return Double.Parse(lines[0]);
        }

        public static List<Rectangle> GetRoiRect(String folderPath)
        {
            List<Rectangle> result = new List<Rectangle>();
            try
            {
                string[] lines = File.ReadAllLines(GetAnalizePathFileName(folderPath, "ROI"));

                for (int i = 1; i < lines.Length - 3; i = i + 4)
                {
                    int x = Int32.Parse(lines[i]);
                    int y = Int32.Parse(lines[i + 1]);
                    int w = Int32.Parse(lines[i + 2]);
                    int h = Int32.Parse(lines[i + 3]);

                    result.Add(new Rectangle(x, y, w, h));
                }
            }catch(Exception ex)
            {
                Environment.Exit(1);
            }

            return result;
        }

        public static double CalcRatio(double afterFiltering, double timeRaw)
        {
            return afterFiltering/timeRaw;
        }

        public static double GetTimeFromFrames(int frames)
        {
            return ((double)frames / 10);
        }

        public static String FormatTimeWork(double seconds)
        {
            return String.Format("{0:00}h {1:00}m {2:00}s", seconds / 3600, (seconds / 60) % 60, seconds % 60);
        }
    }
}
