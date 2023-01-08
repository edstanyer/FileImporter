namespace NRG.Models
{
    partial class PickDuplicateLine
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PickDuplicateLine));
			this.Grid = new FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, resources.GetObject("resource1"));
			this.label2 = new System.Windows.Forms.Label();
			this.OKButton = new System.Windows.Forms.Button();
			this.Grid_Sheet1 = this.Grid.GetSheet(0);
			((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
			this.SuspendLayout();
			// 
			// Grid
			// 
			this.Grid.AccessibleDescription = "Grid, Sheet1, Row 0, Column 0";
			this.Grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.Grid.Font = new System.Drawing.Font("Calibri", 11.25F);
			this.Grid.Location = new System.Drawing.Point(7, 45);
			this.Grid.Name = "Grid";
			this.Grid.Size = new System.Drawing.Size(534, 178);
			this.Grid.TabIndex = 3;
			this.Grid.SelectionChanged += new FarPoint.Win.Spread.SelectionChangedEventHandler(this.Grid_SelectionChanged);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(4, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(537, 33);
			this.label2.TabIndex = 4;
			this.label2.Text = "A point at this location was found to be a node on several lines. Please click on" +
    " the row in the grid to choose the line you wish to select:";
			// 
			// OKButton
			// 
			this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OKButton.Location = new System.Drawing.Point(447, 231);
			this.OKButton.Name = "OKButton";
			this.OKButton.Size = new System.Drawing.Size(75, 23);
			this.OKButton.TabIndex = 6;
			this.OKButton.Text = "Finished";
			this.OKButton.UseVisualStyleBackColor = true;
			this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
			// 
			// PickDuplicateLine
			// 
			this.AcceptButton = this.OKButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(545, 264);
			this.ControlBox = false;
			this.Controls.Add(this.OKButton);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.Grid);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PickDuplicateLine";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Pick Duplicate Line";
			this.Load += new System.EventHandler(this.PickDuplicateLine_Load);
			((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion
        private FarPoint.Win.Spread.FpSpread Grid;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button OKButton;
		private FarPoint.Win.Spread.SheetView Grid_Sheet1;
	}
}