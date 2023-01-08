namespace NRG.Models
{
    partial class PickDuplicateDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PickDuplicateDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.eLabel = new System.Windows.Forms.Label();
			this.nLabel = new System.Windows.Forms.Label();
			this.Grid = new FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, resources.GetObject("resource1"));
			this.Grid_Sheet1 = this.Grid.GetSheet(0);
			this.label2 = new System.Windows.Forms.Label();
			this.OKButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(115, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "A point was located at:";
			// 
			// eLabel
			// 
			this.eLabel.AutoSize = true;
			this.eLabel.Location = new System.Drawing.Point(128, 9);
			this.eLabel.Name = "eLabel";
			this.eLabel.Size = new System.Drawing.Size(45, 13);
			this.eLabel.TabIndex = 1;
			this.eLabel.Text = "Easting:";
			// 
			// nLabel
			// 
			this.nLabel.AutoSize = true;
			this.nLabel.Location = new System.Drawing.Point(128, 32);
			this.nLabel.Name = "nLabel";
			this.nLabel.Size = new System.Drawing.Size(50, 13);
			this.nLabel.TabIndex = 2;
			this.nLabel.Text = "Northing:";
			// 
			// Grid
			// 
			this.Grid.AccessibleDescription = "Grid, Sheet1, Row 0, Column 0";
			this.Grid.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Grid.Font = new System.Drawing.Font("Calibri", 11.25F);
			this.Grid.Location = new System.Drawing.Point(7, 92);
			this.Grid.Name = "Grid";
			this.Grid.Size = new System.Drawing.Size(534, 145);
			this.Grid.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(4, 58);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(537, 33);
			this.label2.TabIndex = 4;
			this.label2.Text = "However, duplicate points were detected at this location. Please click on the row" +
    " in the grid to choose the point you wish to select:";
			// 
			// OKButton
			// 
			this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OKButton.Location = new System.Drawing.Point(442, 243);
			this.OKButton.Name = "OKButton";
			this.OKButton.Size = new System.Drawing.Size(75, 23);
			this.OKButton.TabIndex = 6;
			this.OKButton.Text = "Finished";
			this.OKButton.UseVisualStyleBackColor = true;
			this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
			// 
			// PickDuplicateDlg
			// 
			this.AcceptButton = this.OKButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(545, 269);
			this.ControlBox = false;
			this.Controls.Add(this.OKButton);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.Grid);
			this.Controls.Add(this.nLabel);
			this.Controls.Add(this.eLabel);
			this.Controls.Add(this.label1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PickDuplicateDlg";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Pick Duplicate Point";
			((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label eLabel;
        private System.Windows.Forms.Label nLabel;
        private FarPoint.Win.Spread.FpSpread Grid;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button OKButton;
        private FarPoint.Win.Spread.SheetView Grid_Sheet1;
    }
}