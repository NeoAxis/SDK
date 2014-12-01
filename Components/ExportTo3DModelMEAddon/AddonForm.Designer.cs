namespace ExportTo3DModelMEAddon
{
	partial class AddonForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.buttonClose = new System.Windows.Forms.Button();
			this.checkBoxExportSelectedObjectsOnly = new System.Windows.Forms.CheckBox();
			this.textBoxOutputFileName = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.buttonExport = new System.Windows.Forms.Button();
			this.buttonSelectOutputFileName = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.label1.Location = new System.Drawing.Point( 16, 11 );
			this.label1.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 461, 75 );
			this.label1.TabIndex = 6;
			this.label1.Text = "You can export scene data to 3D model. FBX, COLLADA formats are supported.";
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point( 505, 54 );
			this.buttonClose.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size( 117, 32 );
			this.buttonClose.TabIndex = 3;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// checkBoxExportSelectedObjectsOnly
			// 
			this.checkBoxExportSelectedObjectsOnly.AutoSize = true;
			this.checkBoxExportSelectedObjectsOnly.Location = new System.Drawing.Point( 16, 156 );
			this.checkBoxExportSelectedObjectsOnly.Margin = new System.Windows.Forms.Padding( 4 );
			this.checkBoxExportSelectedObjectsOnly.Name = "checkBoxExportSelectedObjectsOnly";
			this.checkBoxExportSelectedObjectsOnly.Size = new System.Drawing.Size( 206, 21 );
			this.checkBoxExportSelectedObjectsOnly.TabIndex = 1;
			this.checkBoxExportSelectedObjectsOnly.Text = "Export selected objects only";
			this.checkBoxExportSelectedObjectsOnly.UseVisualStyleBackColor = true;
			// 
			// textBoxOutputFileName
			// 
			this.textBoxOutputFileName.Location = new System.Drawing.Point( 20, 108 );
			this.textBoxOutputFileName.Margin = new System.Windows.Forms.Padding( 4 );
			this.textBoxOutputFileName.Name = "textBoxOutputFileName";
			this.textBoxOutputFileName.Size = new System.Drawing.Size( 427, 22 );
			this.textBoxOutputFileName.TabIndex = 0;
			this.textBoxOutputFileName.Text = "C:\\Export.fbx";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 12, 86 );
			this.label3.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 77, 17 );
			this.label3.TabIndex = 12;
			this.label3.Text = "Output file:";
			// 
			// buttonExport
			// 
			this.buttonExport.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonExport.Location = new System.Drawing.Point( 505, 11 );
			this.buttonExport.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonExport.Name = "buttonExport";
			this.buttonExport.Size = new System.Drawing.Size( 117, 32 );
			this.buttonExport.TabIndex = 2;
			this.buttonExport.Text = "Export";
			this.buttonExport.UseVisualStyleBackColor = true;
			this.buttonExport.Click += new System.EventHandler( this.buttonExport_Click );
			// 
			// buttonSelectOutputFileName
			// 
			this.buttonSelectOutputFileName.Location = new System.Drawing.Point( 448, 107 );
			this.buttonSelectOutputFileName.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonSelectOutputFileName.Name = "buttonSelectOutputFileName";
			this.buttonSelectOutputFileName.Size = new System.Drawing.Size( 40, 27 );
			this.buttonSelectOutputFileName.TabIndex = 1;
			this.buttonSelectOutputFileName.Text = "...";
			this.buttonSelectOutputFileName.UseVisualStyleBackColor = true;
			this.buttonSelectOutputFileName.Click += new System.EventHandler( this.buttonSelectOutputFileName_Click );
			// 
			// AddonForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 8F, 16F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size( 639, 207 );
			this.Controls.Add( this.buttonSelectOutputFileName );
			this.Controls.Add( this.buttonExport );
			this.Controls.Add( this.label3 );
			this.Controls.Add( this.textBoxOutputFileName );
			this.Controls.Add( this.checkBoxExportSelectedObjectsOnly );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.buttonClose );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding( 4 );
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddonForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Exporting To 3D Model Add-on";
			this.Load += new System.EventHandler( this.ExportTo3DModelMEAddonForm_Load );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.CheckBox checkBoxExportSelectedObjectsOnly;
		private System.Windows.Forms.TextBox textBoxOutputFileName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonExport;
		private System.Windows.Forms.Button buttonSelectOutputFileName;

	}
}