using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NRG.Models;

namespace NRG.File_IO.Export
{
	public partial class ExportAlignmentSelector : Form
	{
		private List<Alignment> Alignments = new List<Alignment>();

		public ExportAlignmentSelector(List<Alignment> alignments)
		{
			InitializeComponent();
			foreach (Alignment alg in alignments)
			{
				stringList.Items.Add(alg.StringName);
			}
			Alignments = alignments;
		}

		public List<Alignment> GetSelectedAlignments()
		{
			List<Alignment> outlist = new List<Alignment>();

			if (Alignments != null)
			{
				for (int i = 0; i < stringList.Items.Count; i++)
				{
					if (stringList.GetItemChecked(i) == true)
					{
						if (i < Alignments.Count)
						{
							outlist.Add(Alignments[i]);
						}
					}
				}
				return outlist;
			}
			return null;
		}
	}
}
