using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NRG.Models;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Globalization;
namespace NRG.File_IO.Native
{
    public class CoordinateTransformation
    {

		#region Setup

		#endregion

		#region Properties
		public Point3D OriginPoint1 { get;set;} = new Point3D();
		public Point3D OriginPoint2 { get;set; } = new Point3D();
		public Point3D OriginPoint3 { get; set; } = new Point3D(); //Unused in this software but kept for old DTM

		public Point3D DestinationPoint1 { get; set; } = new Point3D();
		public Point3D DestinationPoint2 { get; set; } = new Point3D();
		public Point3D DestinationPoint3 { get; set; } = new Point3D(); //Unused in this software but kept for old DTM

		public HashSet<DTMPoint> PointsToTransform { get; set; }

		public string FilePath { get; set; }

		public bool Shift { get; set; } = false;
		public bool Rotate { get; set; } = false;
		public bool Stretch { get; set; } = false;
		public bool Tilt { get; set; } = false;
		public bool SelectedOnly { get; set; } = false;
		#endregion


		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileReadSuccessfully">True if file was readable, false if the file couldn't be read due to irregular/corrupted data in the file.</param>
		/// <param name="filename"></param>
		/// <param name="initialDirectory"></param>
		/// <returns>True if file was read successfully, false if the file open dialog was cancelled or closed by the user</returns>
		public bool ReadFile(out bool fileReadSuccessfully, string filename = "", string initialDirectory = "")
		{
			fileReadSuccessfully = true;

			if (filename == null || filename == "" || File.Exists(filename) == false)
			{
				OpenFileDialog dlg = new OpenFileDialog();

				if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
					dlg.InitialDirectory = initialDirectory;


				dlg.Filter = "Coordinate Transformation Files | *.ctrans";
				dlg.ShowDialog();
				if (!string.IsNullOrWhiteSpace(dlg.FileName))
				{
					filename = dlg.FileName;
				}
			}
			if (File.Exists(filename) == false)
			{
				return false;
			}

			try
			{
				StreamReader filereader = new StreamReader(filename);
				FilePath = filename;

				List<string> Values = new List<string>();
				while (filereader.EndOfStream == false)
				{
					string linetoread = filereader.ReadLine();

					var line = linetoread.Split(',');
					Values.AddRange(line.ToList());
				}

				if (Values.Count < 23) //File is missing fields. Could put in some default values but if the file is modified the user would want to know rather than implicitly change stuff.
				{
					filereader.Dispose();
					fileReadSuccessfully = false;
					return false;
				}

				//Origin Point 1
				if (Double.TryParse(Values[0], out double origin1X)) { OriginPoint1.X = origin1X; }
				if (Double.TryParse(Values[1], out double origin1Y)) { OriginPoint1.Y = origin1Y; }
				if (Double.TryParse(Values[2], out double origin1Z)) { OriginPoint1.Z = origin1Z; }
				//Origin Point 2
				if (Double.TryParse(Values[3], out double origin2X)) { OriginPoint2.X = origin2X; }
				if (Double.TryParse(Values[4], out double origin2Y)) { OriginPoint2.Y = origin2Y; }
				if (Double.TryParse(Values[5], out double origin2Z)) { OriginPoint2.Z = origin2Z; }
				//Origin Point 3
				if (Double.TryParse(Values[6], out double origin3X)) { OriginPoint3.X = origin3X; }
				if (Double.TryParse(Values[7], out double origin3Y)) { OriginPoint3.Y = origin3Y; }
				if (Double.TryParse(Values[8], out double origin3Z)) { OriginPoint3.Z = origin3Z; }
				//Destination Point 1
				if (Double.TryParse(Values[9], out double destination1X)) { DestinationPoint1.X = destination1X; }
				if (Double.TryParse(Values[10], out double destination1Y)) { DestinationPoint1.Y = destination1Y; }
				if (Double.TryParse(Values[11], out double destination1Z)) { DestinationPoint1.Z = destination1Z; }
				//Destination Point 2
				if (Double.TryParse(Values[12], out double destination2X)) { DestinationPoint2.X = destination2X; }
				if (Double.TryParse(Values[13], out double destination2Y)) { DestinationPoint2.Y = destination2Y; }
				if (Double.TryParse(Values[14], out double destination2Z)) { DestinationPoint2.Z = destination2Z; }
				//Destination Point 3
				if (Double.TryParse(Values[15], out double destination3X)) { DestinationPoint3.X = destination3X; }
				if (Double.TryParse(Values[16], out double destination3Y)) { DestinationPoint3.Y = destination3Y; }
				if (Double.TryParse(Values[17], out double destination3Z)) { DestinationPoint3.Z = destination3Z; }
				//Shift
				if (Values[18] == "0") { Shift = false; }
				else { Shift = true; }
				//Rotate
				if (Values[19] == "0") { Rotate = false; }
				else { Rotate = true; }
				//Stretch
				if (Values[20] == "0") { Stretch = false; }
				else { Stretch = true; }
				//Tilt
				if (Values[21] == "0") { Tilt = false; }
				else { Tilt = true; }
				//Selected Only
				if (Values[22] == "0") { SelectedOnly = false; }
				else { SelectedOnly = true; }


				filereader.Dispose();
			}
            catch
            {
				fileReadSuccessfully = false;
				return false;
            }


			return true;
		}


		public bool WriteFile(string filename = "", string initialDirectory = "")
        {
			if (string.IsNullOrWhiteSpace(filename) || File.Exists(filename) == false)
            {
				SaveFileDialog dlg = new SaveFileDialog();

				if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
					dlg.InitialDirectory = initialDirectory;

				dlg.Filter = "Coordinate Transformation Files | *.ctrans";
				dlg.ShowDialog();
				
				if (dlg.FileName == null || string.IsNullOrWhiteSpace(dlg.FileName) || File.Exists(dlg.FileName) == false) 
				{ 
					return false; 
				}

				this.FilePath = dlg.FileName;
            }


			StreamWriter filewriter = new StreamWriter(this.FilePath);
			//OriginPoints
			filewriter.Write(this.OriginPoint1.X.ToString() + "," + this.OriginPoint1.Y.ToString() + "," + this.OriginPoint1.Z.ToString() + ",");
			filewriter.Write(this.OriginPoint2.X.ToString() + "," + this.OriginPoint2.Y.ToString() + "," + this.OriginPoint2.Z.ToString() + ",");
			filewriter.Write(this.OriginPoint3.X.ToString() + "," + this.OriginPoint3.Y.ToString() + "," + this.OriginPoint3.Z.ToString());
			filewriter.WriteLine();
			//Destination Points
			filewriter.Write(this.DestinationPoint1.X.ToString() + "," + this.DestinationPoint1.Y.ToString() + "," + this.DestinationPoint1.Z.ToString() + ",");
			filewriter.Write(this.DestinationPoint2.X.ToString() + "," + this.DestinationPoint2.Y.ToString() + "," + this.DestinationPoint2.Z.ToString() + ",");
			filewriter.Write(this.DestinationPoint3.X.ToString() + "," + this.DestinationPoint3.Y.ToString() + "," + this.DestinationPoint3.Z.ToString());
			filewriter.WriteLine();
			//Settings
			filewriter.Write(Convert.ToInt32(Shift).ToString() + "," + Convert.ToInt32(Rotate).ToString() + "," + Convert.ToInt32(Stretch).ToString() + "," + Convert.ToInt32(Tilt).ToString() + "," + Convert.ToInt32(SelectedOnly).ToString());

			filewriter.Flush();
			filewriter.Dispose();
			return true;
        }


		#endregion

	}
}
