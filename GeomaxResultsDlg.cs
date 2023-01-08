using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileImporter
{
    public partial class GeomaxResultsDlg : Form
    {
        public GeomaxResultsDlg()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void AddResultText(string newText)
        {
            resList.Items.Add(newText);
        }
    }
}
