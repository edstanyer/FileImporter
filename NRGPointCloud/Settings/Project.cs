using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NRG.Settings
{
	public class Project
	{
		#region Properties

		public string Name { get; private set;}
		public string ProjectPath { get; set;}
		public DateTime Created { get;  set; }
		public DateTime Modified { get;  set; }
		public double ScaleFactor {  get; set; }
		public double ExtentsMinX { get; set; }
		public double ExtentsMaxX { get; set; }
		public double ExtentsMinY { get; set; }
		public double ExtentsMaxY { get; set; }



		#endregion

		#region Setup
		public Project(string projectName)
		{
			string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\NRG Projects\\Default Project";
			if (!Directory.Exists(defaultPath)) { Directory.CreateDirectory(defaultPath); }

			Name = projectName;

			ProjectPath = defaultPath;
			Created = DateTime.Now;
			Modified = DateTime.Now;
			ScaleFactor = 1f;

			ExtentsMinX = 0f;
			ExtentsMaxX = 0f;
			ExtentsMinY = 0f;
			ExtentsMaxY = 0f;

		}
		#endregion

		#region Methods

		public string FormatToSettingString()
		{
			string setting = "";

			setting += ProjectPath;
			setting += "^";
			setting += Created.ToLongTimeString();
			setting += "^";
			setting += Modified.ToLongTimeString();
			setting += "^";
			setting += ScaleFactor.ToString();
			setting += "^";

			setting += ExtentsMinX.ToString();
			setting += "^";
			setting += ExtentsMaxX.ToString();
			setting += "^";
			setting += ExtentsMinY.ToString();
			setting += "^";
			setting += ExtentsMaxY.ToString();

			return setting;
		}

		public Project FormatFromSettingString(string settingString)
		{
			try
			{
				var splitSetting = settingString.Split(new string[] { "^" }, StringSplitOptions.None);
				ProjectPath = splitSetting[0];
				Created = Convert.ToDateTime(splitSetting[1]);
				Modified = Convert.ToDateTime(splitSetting[2]);
				ScaleFactor = Double.Parse(splitSetting[3]);

				ExtentsMinX = Double.Parse(splitSetting[4]);
				ExtentsMaxX = Double.Parse(splitSetting[5]);
				ExtentsMinY = Double.Parse(splitSetting[6]);
				ExtentsMaxY = Double.Parse(splitSetting[7]);

				return this;
			}

            catch
            {
				return new Project("Default Project"); //If the project can't be read (registry broken), then we return the default project
            }
		}
		#endregion

	}
}
