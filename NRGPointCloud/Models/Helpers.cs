using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Export;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using GlmNet;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace NRG.Models
{
    public static class Helpers
    {
		#region Methods

		public static Point RelativeToForm(this Control control)
		{

			Form form = control.FindForm();
			if (form is null)
				return new Point(0, 0);

			Control parent = control.Parent;

			Point offset = control.Location;

			while (parent != null)
			{
				offset.X += parent.Left;
				offset.Y += parent.Top;
				parent = parent.Parent;
			}

			offset.X -= form.Left;
			offset.Y -= form.Top;

			return offset;

		}

		public static bool IsValidNumericEntry(string value, string currentText, int caretBrush)
        {
            try
            {
                double val = 0;
                if (value == "-" && (currentText.Contains("-") || caretBrush != 0))
                    return false;
                else if (value == "-")
                    return true;
                else if (value == "." && currentText.Contains(".") == false)
                    return true;
                else if (!double.TryParse(value, out val))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object ReadPointCloudFile(string filename)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read)))
                {
                    //Create object
                    var count = reader.ReadInt32();
                    var bytes = reader.ReadBytes(count);
                    return DeserializePointCloudFileFromBytes<Services.OldFileVersions.PointCloudFile>(bytes);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static object DeserializePointCloudFileFromBytes<T>(byte[] bytes)
        {
            try
            {
                var formatter = new BinaryFormatter();
                formatter.Binder = new Import.PointCloudFileDeserializer();
                using (var stream = new MemoryStream(bytes))
                {
                    return formatter.Deserialize(stream);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static object DeserializeFromBytes<T>(byte[] bytes)
        {
            try
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream(bytes))
                {
                    return formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static bool PrintBinary(string filename, object data)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create, FileAccess.ReadWrite)))
                {
                    //Create byte array
                    var bytes = SerializeToBytes(data);
                    Int32 count = bytes.Count();
                    writer.Write(count);
                    writer.Write(bytes);
                    writer.Close();
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public static byte[] SerializeToBytes<T>(T item)
        {
            try
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, item);
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream.ToArray();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static List<string> GetAlignmentList(string alignmentDirectory)
        {
            var alignmentList = new List<string>();

            if (!Directory.Exists(alignmentDirectory))
                return alignmentList;

            foreach (var directory in Directory.GetDirectories(alignmentDirectory))
            {
                var filename = directory + "/" + Path.GetFileName(directory) + ".nst";
                if(File.Exists(filename))
                {
                    alignmentList.Add(Path.GetFileNameWithoutExtension(filename));
                }
                //var filename = Path.GetFileName(directory);

                //foreach (var file in Directory.GetFiles(directory))
                //{
                //    if (Path.GetFileNameWithoutExtension(file).ToUpper() == filename.ToUpper())
                //    {
                //        if (Path.GetFileName(file).Replace(Path.GetFileNameWithoutExtension(file), "").ToUpper() == ".NST")
                //        {
                //            alignmentList.Add(filename);
                //            break;
                //        }
                //    }
                //}
            }

            return alignmentList;
        }

        public static string GetSaneIniFile(string filename)
        {
            try
            {
                var saneLines = new List<string>();
                var lines = new List<string>();
                var line = "";
                using (var sr = new StreamReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    while((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                for (int i = 0; i < lines.Count(); i++)
                {
                    line = lines[i];

                    if (line != null)
                    {
                        if (i != 0)
                        {
                            var lastLine = lines[i - 1];
                            if (line == lastLine)
                            {
                                continue;
                            }
                        }

                        if (line.Contains("="))
                        {
                            if (line.IndexOf("=") != 0)
                                saneLines.Add(line);
                        }
                        else if (line.Contains("[") && line.Contains("]"))
                            saneLines.Add(line);
                    }
                }

                //Stich lines back together
                string saneLine = "";
                foreach(var l in saneLines)
                {
                    saneLine += l + Environment.NewLine;
                }
                return saneLine;
            }
            catch
            {
                return "";
            }
        }

        public static long GetFreeSpaceOnDrive(string path)
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(path));

            return driveInfo.AvailableFreeSpace;
        }

        #endregion
    }
}
