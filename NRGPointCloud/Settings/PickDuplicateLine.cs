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
    public partial class PickDuplicateLine : Form
    {
        public object SelectedLine { get; private set; }
        public List<object> FoundLines { get; set; }

        private DTM Model { get; set; }

        public PickDuplicateLine(DTM model)
        {
            InitializeComponent();
            Model = model;
        }

        private void FillGrid()
        {
            Grid.Sheets[0].RowCount = FoundLines.Count;
            var s = Grid.Sheets[0];
            s.OperationMode = FarPoint.Win.Spread.OperationMode.RowMode;

            int lineIndex = 0;
            foreach (var line in FoundLines)
            {

                switch (line)
                {
                    case PolyLine pl:
                        CreatePolyLineRow(pl, lineIndex);
                        break;
                    case StandardLine sl:
                        CreateStandardLineRow(sl, lineIndex);
                        break;
                    case LineFeatureItem lf:
                        CreateLineFeatureItemRow(lf, lineIndex);
                        break;
                }

                lineIndex++;
            }
            s.Protect = true;
            s.Columns[0].Locked = true;
            s.Columns[1].Locked = false;
            s.Columns[2].Locked = true;
            s.Columns[3].Locked = true;

            int r = 0;
			foreach (var line in FoundLines)
			{
				r++;
			}
		}
        private void CreatePolyLineRow(PolyLine pl, int lineIndex)
        {
            var s = Grid.Sheets[0];

            //Line Type
            s.Cells[lineIndex, 0].Text = "PolyLine";

            //Nodes
            List<string> nodeRows = new List<string>();
            foreach (var node in pl.Nodes)
            {
                nodeRows.Add((Model.Points.IndexOf(node) + 1).ToString());
            }
            nodeRows[0] = "First - " + nodeRows[0];
            nodeRows[nodeRows.Count - 1] = "Last - " + nodeRows[nodeRows.Count - 1];

            FarPoint.Win.Spread.CellType.ComboBoxCellType comboNodes = new FarPoint.Win.Spread.CellType.ComboBoxCellType();
            comboNodes.Items = nodeRows.ToArray();
            s.Cells[lineIndex, 1].CellType = comboNodes;
            s.Cells[lineIndex, 1].Text = nodeRows[0];

            //Layers
            s.Cells[lineIndex, 2].Text = pl.Layer;
            
            //Colour
            s.Cells[lineIndex, 3].Value = Color.FromArgb(255, pl.Colour);
        }
        private void CreateStandardLineRow(StandardLine sl, int lineIndex)
		{
            var s = Grid.Sheets[0];

            //Line Type
            s.Cells[lineIndex, 0].Text = "StandardLine";

            //Nodes
            List<string> nodeRows = new List<string>();

            nodeRows.Add(Model.Points.IndexOf(sl.StartPoint).ToString());
            nodeRows.Add(Model.Points.IndexOf(sl.EndPoint).ToString());

            nodeRows[0] = "First - " + nodeRows[0];
            nodeRows[nodeRows.Count - 1] = "Last - " + nodeRows[nodeRows.Count - 1];

            FarPoint.Win.Spread.CellType.ComboBoxCellType comboNodes = new FarPoint.Win.Spread.CellType.ComboBoxCellType();
            comboNodes.Items = nodeRows.ToArray();
            s.Cells[lineIndex, 1].CellType = comboNodes;
            s.Cells[lineIndex, 1].Text = nodeRows[0];

            //Layers
            s.Cells[lineIndex, 2].Text = sl.Layer;

            //Colour
            s.Cells[lineIndex, 3].Value = Color.FromArgb(255, sl.Colour);
        }

        private void CreateLineFeatureItemRow(LineFeatureItem lf, int lineIndex)
        {
            var s = Grid.Sheets[0];

            //Line Type
            s.Cells[lineIndex, 0].Text = "FeatureLine";

            //Nodes
            List<string> nodeRows = new List<string>();
            foreach (var node in lf.Points)
            {
                nodeRows.Add(Model.Points.IndexOf(node).ToString());
            }
            nodeRows[0] = "First - " + nodeRows[0];
            nodeRows[nodeRows.Count - 1] = "Last - " + nodeRows[nodeRows.Count - 1];

            FarPoint.Win.Spread.CellType.ComboBoxCellType comboNodes = new FarPoint.Win.Spread.CellType.ComboBoxCellType();
            comboNodes.Items = nodeRows.ToArray();
            s.Cells[lineIndex, 1].CellType = comboNodes;
            s.Cells[lineIndex, 1].Text = nodeRows[0];

            //Layers
            s.Cells[lineIndex, 2].Text = lf.FeatureCollection.Feature.Layer;

            //Colour
            s.Cells[lineIndex, 3].Value = Color.FromArgb(255, lf.FeatureCollection.Feature.R, lf.FeatureCollection.Feature.G, lf.FeatureCollection.Feature.B);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (SelectedLine == null)
                SelectedLine = FoundLines[0];

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Grid_SelectionChanged(object sender, FarPoint.Win.Spread.SelectionChangedEventArgs e)
        {
            if (Grid.Sheets[0].ActiveRowIndex >= 0 && Grid.Sheets[0].ActiveRowIndex < FoundLines.Count)
            {
                SelectedLine = FoundLines[Grid.Sheets[0].ActiveRowIndex];
            }
        }

		private void PickDuplicateLine_Load(object sender, EventArgs e)
		{
            FillGrid();
		}
	}
}
