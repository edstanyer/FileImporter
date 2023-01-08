using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NRG.Models
{


	public class TunnelMachines
	{

		public List<TBMControlPoints> MachineCoordinateSets = new List<TBMControlPoints>();

		public String CurrentMachine = "";

		//public double ShieldRadius = 2.85;	 //nominally for the machine we're dealing with at for the Delhi metro
		//public double TBMLength = 4.38;     // same reason as above
		//public double CentreOffsetX = 0;
		//public double CentreOffsetY = -2.1;  //same - we might need to some more rational logic to these defaults
		//public bool ApplyCant = true;
		string FilePath = "";

		

		public TunnelMachines(string ContractPath = "")
		{
			FilePath = ContractPath + "\\Pictures\\TBMControl.TBM";
			ReadFile();
		}

		public TBMControlPoints GetCurrentMachine(string ThisMachine = "")
		{
			ReadFile();
			if (ThisMachine != "")
			{
				foreach (TBMControlPoints m in MachineCoordinateSets)
				{
					if (m.TBMID == ThisMachine)
					{
						CurrentMachine = ThisMachine;
						break;
					}
				}
			}
			
			
			if (CurrentMachine != "")
			{
				foreach (TBMControlPoints tbm in MachineCoordinateSets)
				{
					if (CurrentMachine == tbm.TBMID)
						return tbm;
				}
			}
			return null;
		}


		public void ReadFile()

		{
			if (File.Exists(FilePath))
			{

				StreamReader fs = new StreamReader(FilePath);

				TBMControlPoints thisTBM = new TBMControlPoints();

				MachineCoordinateSets = new List<TBMControlPoints>();

				while (fs.EndOfStream==false) 
				{
					string txt = fs.ReadLine();

					if (txt.StartsWith("[") && txt.EndsWith("]") && txt.Length > 2)
					{
						thisTBM = new TBMControlPoints();
						string id = txt.Replace("[", "");
						thisTBM.TBMID = id.Replace("]", "");
						MachineCoordinateSets.Add(thisTBM);
					}
					else if (txt.StartsWith("{") && txt.EndsWith("}"))	
					{
						string id = txt.Replace("{", "");
						CurrentMachine = id.Replace("}", "");
					}
					else
					{
						if (thisTBM != null && thisTBM.TBMID != "")
						{

							if (txt.ToUpper().Contains("SHIELD RADIUS"))
							{
								string[] arr = txt.Split('=');
								if (arr.Length > 1)
								{
									double.TryParse(arr[1], out thisTBM.ShieldRadius);
								}

							}
							else if (txt.ToUpper().Contains("START CHAINAGE"))
							{
								string[] arr = txt.Split('=');
								if (arr.Length > 1)
								{
									double.TryParse(arr[1], out thisTBM.StartChainage);
								}

							}

							else if (txt.ToUpper().Contains("MACHINE LENGTH"))
							{
								string[] arr = txt.Split('=');
								if (arr.Length > 1)
								{
									double.TryParse(arr[1], out thisTBM.TBMLength);
								}
							}
							else if (txt.ToUpper().Contains("CENTRE OFFSET X"))
							{
								string[] arr = txt.Split('=');
								if (arr.Length > 1)
								{
									double.TryParse(arr[1], out thisTBM.CentreOffsetX);
								}
							}
							else if (txt.ToUpper().Contains("CENTRE OFFSET Y"))
							{
								string[] arr = txt.Split('=');
								if (arr.Length > 1)
								{
									double.TryParse(arr[1], out thisTBM.CentreOffsetY);
								}
							}
							else
							{
								string[] arr = txt.Split(',');
								if (arr.Length >= 4)
								{
									DTMPoint pt = new DTMPoint();
									pt.PointID = arr[0];
									if (double.TryParse(arr[1], out double x))
									{
										if (double.TryParse(arr[2], out double y))
										{
											if (double.TryParse(arr[3], out double z))
											{
												pt.X = x; pt.Y = y; pt.Z = z;
												thisTBM.AddPoint(pt);
											}
										}
									}

								}
							}
						}			
					}
				}
				fs.Close();
			}
		}

		public void WriteFile(string current = "")
		{
			StreamWriter fs = new StreamWriter(FilePath);
			if (current != "")
			{
				fs.WriteLine("{" + current + "}");
			}

			//fs.WriteLine("Shield Radius=" + this.ShieldRadius.ToString());
			//fs.WriteLine("Machine Length=" + this.ShieldRadius.ToString());
			//fs.WriteLine("Centre Offset X=" + this.CentreOffsetX.ToString());
			//fs.WriteLine("Centre Offset Y=" + this.CentreOffsetY.ToString());
			
			foreach (TBMControlPoints m in MachineCoordinateSets)
			{
				m.SaveToFile(fs);
			}
			
			fs.Close();
		}

		
	}


	public class TBMControlPoints
	{
		public string TBMID = "";
		public Dictionary< string, DTMPoint> ControlCoordinates  = new Dictionary<string, DTMPoint>();
		public double StartChainage = 0;        //tunnel start chainage
		public double ShieldRadius = 2.85;		//nominally for the machine we're dealing with at for the Delhi metro
		public double TBMLength = 4.38;			//same reason as above
		public double CentreOffsetX = 0;
		public double CentreOffsetY = -2.1;		//same - we might need to some more rational logic to these defaults
		public bool ApplyCant = true;

		public void SaveToFile(StreamWriter fs)
		{
			fs.WriteLine("[" + TBMID + "]");
			fs.WriteLine("Start Chainage=" + this.StartChainage.ToString());
			fs.WriteLine("Shield Radius=" + this.ShieldRadius.ToString());
			fs.WriteLine("Machine Length=" + this.TBMLength.ToString());
			fs.WriteLine("Centre Offset X=" + this.CentreOffsetX.ToString());
			fs.WriteLine("Centre Offset Y=" + this.CentreOffsetY.ToString());
			foreach (var pt in ControlCoordinates)
			{
				string txt = "";
				txt = pt.Value.PointID + "," +  pt.Value.X.ToString() + "," + pt.Value.Y.ToString() + "," + pt.Value.Z.ToString();
				fs.WriteLine(txt);
			}
			fs.Close();
			
		}

		public bool AddPoint(DTMPoint newPt)
		{
			if (newPt.PointID == null || newPt.PointID == "" || ControlCoordinates.ContainsKey(newPt.PointID))
			{
				newPt.PointID = GetUniqueID();
			}
			
			ControlCoordinates.Add(newPt.PointID, newPt);

			return true;
		}

		private string GetUniqueID()
		{
			int i = 0;
			for ( i=1; i < 1000000;i++)
			{
				string txt = i.ToString().PadLeft(4, '0');
				if (ControlCoordinates.ContainsKey(txt) == false)
					return txt;
			
			}
			return "1000001";
		}

		public DTMPoint GetPoint(string ID)
		{
			if (ID == "" || ControlCoordinates.Count == 0 || ControlCoordinates.ContainsKey(ID) == false)
				return null;

			DTMPoint op = new DTMPoint();
			if (ControlCoordinates.TryGetValue(ID, out op))
				return op; 

			return null;
		}

		

	}

	public class MachineGraphics
	{
		public Point2D RearVector = new Point2D();
		public Point2D FrontVector = new Point2D();
		public Point2D BackwardVector = new Point2D();
		public Point2D ForwardVector = new Point2D();
		public Point2D LeftVector = new Point2D();
		public Point2D RightVector = new Point2D();

		public Point2D TopVector = new Point2D();
		public Point2D BottomVector = new Point2D();

	}


}
