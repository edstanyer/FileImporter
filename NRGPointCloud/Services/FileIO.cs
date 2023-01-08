using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NRG.Services
{
    public static class FileIO
    {
        #region Methods

        public static string LoadFileDialog(string fileExtension)
        {
            string fileName = "";

            OpenFileDialog fileDLG = new OpenFileDialog();
            fileDLG.Multiselect = false;
            fileDLG.Filter = fileExtension + " FILES (*." + fileExtension + ")| *." + fileExtension;
            DialogResult result = fileDLG.ShowDialog();
            if (result == DialogResult.OK)
            {
                fileName = fileDLG.FileName;
            }

            return fileName;
        }

        public static List<string> LoadAsciiToStringList(string inputFile, bool ignoreFirstLine)
        {
            List<string> outputList = new List<string>();
            try
            {   // Open the text file using a stream reader.
                using (StreamReader stream = new StreamReader(inputFile))
                {
                    if (ignoreFirstLine) { string temp = stream.ReadLine(); }
                    string line;
                    while ((line = stream.ReadLine()) != null)
                    {
                        if (line.Length > 0)
                            outputList.Add(line);
                    }
                }
                return outputList;
            }
            catch
            {
                return null;
            }
        }


        public static string SaveFileDialog(string fileExtension, string path)
        {
            string fileName = "NOFILE";

            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = fileExtension + " files (*." + fileExtension + "| *." + fileExtension;
            saveFileDialog1.FilterIndex = 2;

            if (path.Length > 0)
            {
                saveFileDialog1.InitialDirectory = Path.GetDirectoryName(path);
                if (path.Substring(path.Length - 4, 4).Contains("."))
                    saveFileDialog1.FileName = path.Substring(0, path.Length - 4) + "." + fileExtension;
            }
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                fileName = saveFileDialog1.FileName;
            }

            return fileName;
        }

        public static bool SaveStringToFile(string inputString, string filePath)
        {
            string path = filePath;
            if (filePath.Length > 0)
            {
                try
                {
                    // Delete the file if it exists.
                    if (File.Exists(path))
                    {
                        // Note that no lock is put on the
                        // file and the possibility exists
                        // that another process could do
                        // something with it between
                        // the calls to Exists and Delete.
                        File.Delete(path);
                    }

                    // Create the file.
                    using (FileStream fs = File.Create(path))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes(inputString);
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                    }

                    return true;
                }

                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static void TrimStrings(ref string[] inputarray)
        {
            for (int i = 0; i < inputarray.Length; i++)
            {
                inputarray[i] = inputarray[i].TrimStart().TrimEnd();
            }
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        #endregion
    }
}
