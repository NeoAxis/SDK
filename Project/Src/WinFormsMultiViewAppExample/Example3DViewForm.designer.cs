namespace WinFormsMultiViewAppExample
{
	partial class Example3DViewForm
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
			this.SuspendLayout();
			// 
			// renderTargetUserControl1
			// 
			this.renderTargetUserControl1.AutomaticUpdateFPS = 30F;
			this.renderTargetUserControl1.BackColor = System.Drawing.Color.Black;
			this.renderTargetUserControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.renderTargetUserControl1.Location = new System.Drawing.Point( 0, 0 );
			this.renderTargetUserControl1.MouseRelativeMode = false;
			this.renderTargetUserControl1.Name = "renderTargetUserControl1";
			this.renderTargetUserControl1.Size = new System.Drawing.Size( 460, 406 );
			this.renderTargetUserControl1.TabIndex = 0;
			// 
			// Example3DViewForm
			// 
			this.ClientSize = new System.Drawing.Size( 460, 406 );
			this.Controls.Add( this.renderTargetUserControl1 );
			this.DockAreas = ( (WeifenLuo.WinFormsUI.Docking.DockAreas)( ( ( ( ( WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom ) ) );
			this.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.471698F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.HideOnClose = true;
			this.Name = "Example3DViewForm";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockRight;
			this.TabText = "3D View";
			this.Load += new System.EventHandler( this.Example3DViewForm_Load );
			this.ResumeLayout( false );

		}

		#endregion

		private WinFormsAppFramework.RenderTargetUserControl renderTargetUserControl1;


	}
}
