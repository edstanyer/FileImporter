using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace NRG.File_IO.Import
{
	
	public class ASCIIImporter
	{

		public List<String> RawData = new List<string>();
		public string FileName = "";

		public void ReadFile(string fileName = "")
		{
			if (fileName == null ||fileName == "" || File.Exists(fileName) == false)
			{
				string txt = Microsoft.VisualBasic.Interaction.GetSetting("NRG", "ASCII Import", "Last Directory");
				OpenFileDialog dlg = new OpenFileDialog();

				if (txt != null && txt != "" && Directory.Exists(txt))
				{
					dlg.Title = "ASCII Import";
					dlg.InitialDirectory = txt;					
					dlg.Filter = "ASCII Files(*.txt; *.csv; *.gpf)| *.txt; *.csv; *.gpf" ;
				}
				dlg.Filter = "ASCII Files(*.txt; *.csv; *.gpf)| *.txt; *.csv; *.gpf";

				DialogResult res = dlg.ShowDialog();
				if (res == DialogResult.OK)
				{
					fileName = dlg.FileName;
					if (File.Exists(fileName))
					{
						FileName = fileName;
						RawData = new List<string>();
						using (StreamReader sr = new StreamReader(fileName))
						{
							string line = "";
							while ((line = sr.ReadLine()) != null)
							{
								RawData.Add(line);
							}
							sr.Close();
						}
					}	
				}
			}
		}


		//private List<String> ParseLine

	}

}

