
namespace NRG.File_IO.Export
{
	partial class ExportAlignmentSelector
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
			this.stringList = new System.Windows.Forms.CheckedListBox();
			this.OKButton = new System.Windows.Forms.Button();
			this.CancelButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// stringList
			// 
			this.stringList.CheckOnClick = true;
			this.stringList.FormattingEnabled = true;
			this.stringList.Location = new System.Drawing.Point(3, -1);
			this.stringList.Name = "stringList";
			this.stringList.Size = new System.Drawing.Size(395, 229);
			this.stringList.TabIndex = 0;
			// 
			// OKButton
			// 
			this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OKButton.Location = new System.Drawing.Point(242, 234);
			this.OKButton.Name = "OKButton";
			this.OKButton.Size = new System.Drawing.Size(75, 23);
			this.OKButton.TabIndex = 1;
			this.OKButton.Text = "&OK";
			this.OKButton.UseVisualStyleBackColor = true;
			// 
			// CancelButton
			// 
			this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelButton.Location = new System.Drawing.Point(323, 234);
			this.CancelButton.Name = "CancelButton";
			this.CancelButton.Size = new System.Drawing.Size(75, 23);
			this.CancelButton.TabIndex = 2;
			this.CancelButton.Text = "&Cancel";
			this.CancelButton.UseVisualStyleBackColor = true;
			// 
			// ExportAlignmentSelector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(401, 261);
			this.ControlBox = false;
			this.Controls.Add(this.CancelButton);
			this.Controls.Add(this.OKButton);
			this.Controls.Add(this.stringList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "ExportAlignmentSelector";
			this.Text = "Alignment Selector";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.CheckedListBox stringList;
		private System.Windows.Forms.Button OKButton;
		private System.Windows.Forms.Button CancelButton;
	}
}