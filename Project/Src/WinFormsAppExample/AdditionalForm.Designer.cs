namespace WinFormsAppExample
{
	partial class AdditionalForm
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
			this.renderTargetUserControl1 = new WinFormsAppFramework.RenderTargetUserControl();
			this.buttonClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// renderTargetUserControl1
			// 
			this.renderTargetUserControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.renderTargetUserControl1.AutomaticUpdateFPS = 30F;
			this.renderTargetUserControl1.BackColor = System.Drawing.Color.Black;
			this.renderTargetUserControl1.Location = new System.Drawing.Point( 12, 12 );
			this.renderTargetUserControl1.MouseRelativeMode = false;
			this.renderTargetUserControl1.Name = "renderTargetUserControl1";
			this.renderTargetUserControl1.Size = new System.Drawing.Size( 461, 356 );
			this.renderTargetUserControl1.TabIndex = 0;
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Location = new System.Drawing.Point( 479, 12 );
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size( 88, 26 );
			this.buttonClose.TabIndex = 1;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler( this.buttonClose_Click );
			// 
			// AdditionalForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size( 579, 380 );
			this.Controls.Add( this.buttonClose );
			this.Controls.Add( this.renderTargetUserControl1 );
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AdditionalForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Additional Form";
			this.Load += new System.EventHandler( this.AdditionalForm_Load );
			this.ResumeLayout( false );

		}

		#endregion

		private WinFormsAppFramework.RenderTargetUserControl renderTargetUserControl1;
		private System.Windows.Forms.Button buttonClose;
	}
}