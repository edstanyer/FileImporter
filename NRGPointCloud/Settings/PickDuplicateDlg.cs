using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace NRG.Models
{
    public partial class PickDuplicateDlg : Form
    {

        private List<DTMPoint> pts= new List<DTMPoint>();
        public List<DTMPoint> Points
        {
            get =>pts;
            set { 
                
                pts = value;
                FillGrid();
            } 
        }

        public DTMPoint SelectedPoint 
        { 
            get{ if (Grid.Sheets[0].ActiveRowIndex >= 0 && Grid.Sheets[0].ActiveRowIndex < pts.Count) { return pts[Grid.Sheets[0].ActiveRowIndex]; } else { return pts[0];}}  
        }
        
        
     
            
        public PickDuplicateDlg()
        {
            InitializeComponent();
        }

        private void FillGrid()
        {
            Grid.Sheets[0].OperationMode = FarPoint.Win.Spread.OperationMode.RowMode;


            eLabel.Text = "Easting: " + pts[0].X.ToString("0.000");
            nLabel.Text = "Northing: " + pts[0].Y.ToString("0.000");

            Grid.Sheets[0].ClearRange(1,1, Grid.Sheets[0].RowCount, Grid.Sheets[0].ColumnCount, true);
            Grid.Sheets[0].RowCount = pts.Count;
            int r = 0;
            foreach (DTMPoint p in pts)
            {
                Grid.Sheets[0].SetText(r, 0, p.PointID);
                Grid.Sheets[0].SetText(r, 1, p.X.ToString("0.000"));
                Grid.Sheets[0].SetText(r, 2, p.Y.ToString("0.000"));
                Grid.Sheets[0].SetText(r, 3, p.Z.ToString("0.000"));
                Grid.Sheets[0].SetText(r, 4, p.PointLabel);
                r++;
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
