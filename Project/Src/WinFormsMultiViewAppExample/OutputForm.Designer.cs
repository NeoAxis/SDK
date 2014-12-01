namespace WinFormsMultiViewAppExample
{
	partial class OutputForm
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
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// richTextBox1
			// 
			this.richTextBox1.AcceptsTab = true;
			this.richTextBox1.BackColor = System.Drawing.SystemColors.Window;
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Font = new System.Drawing.Font( "Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.richTextBox1.Location = new System.Drawing.Point( 0, 0 );
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size( 519, 106 );
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			this.richTextBox1.WordWrap = false;
			this.richTextBox1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler( this.richTextBox1_PreviewKeyDown );
			// 
			// OutputForm
			// 
			this.ClientSize = new System.Drawing.Size( 519, 106 );
			this.Controls.Add( this.richTextBox1 );
			this.DockAreas = ( (WeifenLuo.WinFormsUI.Docking.DockAreas)( ( ( ( ( WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom ) ) );
			this.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.471698F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.HideOnClose = true;
			this.Name = "OutputForm";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide;
			this.TabText = "Output";
			this.Text = "Output";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.RichTextBox richTextBox1;

	}
}
