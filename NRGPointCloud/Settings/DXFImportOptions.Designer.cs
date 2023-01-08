
namespace NRG.Settings
{
	partial class DXFImportOptions
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DXFImportOptions));
			this.LayerOption = new System.Windows.Forms.RadioButton();
			this.LineTypeOption = new System.Windows.Forms.RadioButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.OKButton = new System.Windows.Forms.Button();
			this.CancelButton = new System.Windows.Forms.Button();
			this.MainPic = new System.Windows.Forms.PictureBox();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MainPic)).BeginInit();
			this.SuspendLayout();
			// 
			// LayerOption
			// 
			this.LayerOption.AutoSize = true;
			this.LayerOption.Checked = true;
			this.LayerOption.Location = new System.Drawing.Point(91, 42);
			this.LayerOption.Name = "LayerOption";
			this.LayerOption.Size = new System.Drawing.Size(82, 17);
			this.LayerOption.TabIndex = 0;
			this.LayerOption.TabStop = true;
			this.LayerOption.Text = "Layer Name";
			this.LayerOption.UseVisualStyleBackColor = true;
			// 
			// LineTypeOption
			// 
			this.LineTypeOption.AutoSize = true;
			this.LineTypeOption.Location = new System.Drawing.Point(91, 65);
			this.LineTypeOption.Name = "LineTypeOption";
			this.LineTypeOption.Size = new System.Drawing.Size(72, 17);
			this.LineTypeOption.TabIndex = 1;
			this.LineTypeOption.Text = "Line Type";
			this.LineTypeOption.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.LayerOption);
			this.panel1.Controls.Add(this.LineTypeOption);
			this.panel1.Location = new System.Drawing.Point(559, 1);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(279, 312);
			this.panel1.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(222, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "For lines, polyline, splines and arcs, label with:";
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// OKButton
			// 
			this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OKButton.Location = new System.Drawing.Point(662, 319);
			this.OKButton.Name = "OKButton";
			this.OKButton.Size = new System.Drawing.Size(75, 23);
			this.OKButton.TabIndex = 3;
			this.OKButton.Text = "&OK";
			this.OKButton.UseVisualStyleBackColor = true;
			this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
			// 
			// CancelButton
			// 
			this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelButton.Location = new System.Drawing.Point(752, 319);
			this.CancelButton.Name = "CancelButton";
			this.CancelButton.Size = new System.Drawing.Size(75, 23);
			this.CancelButton.TabIndex = 4;
			this.CancelButton.Text = "&Cancel";
			this.CancelButton.UseVisualStyleBackColor = true;
			this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
			// 
			// MainPic
			// 
			this.MainPic.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainPic.Location = new System.Drawing.Point(0, 1);
			this.MainPic.Name = "MainPic";
			this.MainPic.Size = new System.Drawing.Size(553, 341);
			this.MainPic.TabIndex = 5;
			this.MainPic.TabStop = false;
			this.MainPic.Click += new System.EventHandler(this.MainPic_Click);
			this.MainPic.Paint += new System.Windows.Forms.PaintEventHandler(this.MainPic_Paint);
			// 
			// DXFImportOptions
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(840, 346);
			this.ControlBox = false;
			this.Controls.Add(this.MainPic);
			this.Controls.Add(this.CancelButton);
			this.Controls.Add(this.OKButton);
			this.Controls.Add(this.panel1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "DXFImportOptions";
			this.Text = "DXF Import Options";
			this.Load += new System.EventHandler(this.DXFImportOptions_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MainPic)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RadioButton LayerOption;
		private System.Windows.Forms.RadioButton LineTypeOption;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button OKButton;
		private System.Windows.Forms.Button CancelButton;
		private System.Windows.Forms.PictureBox MainPic;
	}
}