using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using netDxf;
using System.Drawing.Printing;

using System.IO;

namespace NRG.Settings
{
	public partial class DXFImportOptions : Form
	{
		public string dxfFile { get; set; }

		public int LabelType()
		{
			if (LayerOption.Checked)
				return 0;
			else
				return 1;
	
		}

		public DXFImportOptions()
		{
			InitializeComponent();
		}

		private void label1_Click(object sender, EventArgs e)
		{

		}

		private void OKButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		public void PreviewFile()
		{
			if (dxfFile == null || dxfFile == "" || File.Exists(dxfFile)==false)
			{
				return;
			}

			//using (var img = Aspose.CAD.Image.Load(dxfFile))

			//{

			//	// create an instance of CadRasterizationOptions & set resultant page size

			//	var rasterizationOptions = new Aspose.CAD.ImageOptions.CadRasterizationOptions()

			//	{

			//		PageSize = new Aspose.CAD.SizeF(1200, 1200),

			//	};

			//	img.Save(  dxfFile + ".pdf", new Aspose.CAD.ImageOptions.PngOptions() { VectorRasterizationOptions = rasterizationOptions });
			//	MainPic.Load(dxfFile + ".pdf");
			//	//	// save resultant PDF

			//	//	img.Save(dir + "output.pdf", new Aspose.CAD.ImageOptions.PdfOptions() { VectorRasterizationOptions = rasterizationOptions });

			//}

			//if (dxfFile != null)
			//{
			//	Rectangle displayRectangle = new Rectangle(0, 0, MainPic.Width, MainPic.Height);
			//	Bitmap image = new Bitmap(displayRectangle.Width, displayRectangle.Height);
			//	Graphics graphics = Graphics.FromImage(image);
			//	SolidBrush brush = new SolidBrush(Color.Black);
			//	graphics.FillRectangle(brush, displayRectangle);
			//	Pen pen = new Pen(Color.Blue, 2f);
			//	Pen pen2 = new Pen(Color.Blue, 200f);
			//	Pen pen3 = new Pen(Color.FromArgb(50, 50, 50), 1f);
			//	for (int i = 1; (i * 40) < displayRectangle.Height; i++)
			//	{
			//		graphics.DrawLine(pen3, 0, 40 * i, displayRectangle.Width,40 * 1);
			//	}

			//	for (int i = 1; (i * 40) < displayRectangle.Width; i++)
			//	{
			//		graphics.DrawLine(pen3, 40 * i, 0, 40 * i, displayRectangle.Height);
			//	}

			//MainPic.Image = image;

			//}

		}

		private void DXFImportOptions_Load(object sender, EventArgs e)
		{
			PreviewFile();
		}

		private void MainPic_Click(object sender, EventArgs e)
		{

		}

		private void MainPic_Paint(object sender, PaintEventArgs e)
		{
			Rectangle displayRectangle = new Rectangle(0, 0, MainPic.Width, MainPic.Height);
			Bitmap image = new Bitmap(displayRectangle.Width, displayRectangle.Height);
			Graphics graphics = Graphics.FromImage(image);
			SolidBrush brush = new SolidBrush(Color.Black);
			graphics.FillRectangle(brush, displayRectangle);
			Pen pen = new Pen(Color.Blue, 2f);
			Pen pen2 = new Pen(Color.Blue, 200f);
			Pen pen3 = new Pen(Color.FromArgb(50, 50, 50), 1f);
			for (int i = 1; (i * 40) < displayRectangle.Height; i++)
			{
				graphics.DrawLine(pen3, 0, 40 * i, displayRectangle.Width, 40 * i);
			}

			for (int i = 1; (i * 40) < displayRectangle.Width; i++)
			{
				graphics.DrawLine(pen3, 40 * i, 0, 40 * i, displayRectangle.Height);
			}

			MainPic.Image = image;
		}
	}
}
