namespace WinFormsMultiViewAppExample
{
	partial class MainForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MainForm ) );
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.workAreaWithoutViewsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.workAreaWith1ViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.workAreaWith4ViewsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.entityTypesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertiesWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.outputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.example3DViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.statusBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.documentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripGeneral = new System.Windows.Forms.ToolStrip();
			this.toolStripButtonOpen = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonOptions = new System.Windows.Forms.ToolStripButton();
			this.timer1 = new System.Windows.Forms.Timer( this.components );
			this.timerShowLogicEditor = new System.Windows.Forms.Timer( this.components );
			this.menuStrip.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.toolStripGeneral.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem} );
			this.menuStrip.Location = new System.Drawing.Point( 0, 0 );
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size( 921, 27 );
			this.menuStrip.TabIndex = 0;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem} );
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size( 41, 23 );
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = global::WinFormsMultiViewAppExample.Properties.Resources.Open;
			this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size( 152, 24 );
			this.openToolStripMenuItem.Text = "Open map...";
			this.openToolStripMenuItem.Click += new System.EventHandler( this.openToolStripMenuItem_Click );
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.Size = new System.Drawing.Size( 152, 24 );
			this.closeToolStripMenuItem.Text = "Close map";
			this.closeToolStripMenuItem.Click += new System.EventHandler( this.closeToolStripMenuItem_Click );
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size( 149, 6 );
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size( 152, 24 );
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler( this.exitToolStripMenuItem_Click );
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.workAreaWithoutViewsToolStripMenuItem,
            this.workAreaWith1ViewToolStripMenuItem,
            this.workAreaWith4ViewsToolStripMenuItem,
            this.toolStripSeparator1,
            this.entityTypesToolStripMenuItem,
            this.propertiesWindowToolStripMenuItem,
            this.outputToolStripMenuItem,
            this.example3DViewToolStripMenuItem,
            this.toolStripSeparator6,
            this.statusBarToolStripMenuItem} );
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size( 50, 23 );
			this.viewToolStripMenuItem.Text = "&View";
			// 
			// workAreaWithoutViewsToolStripMenuItem
			// 
			this.workAreaWithoutViewsToolStripMenuItem.Name = "workAreaWithoutViewsToolStripMenuItem";
			this.workAreaWithoutViewsToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.workAreaWithoutViewsToolStripMenuItem.Text = "Work Area without Views";
			this.workAreaWithoutViewsToolStripMenuItem.Click += new System.EventHandler( this.workAreaWithoutViewsToolStripMenuItem_Click );
			// 
			// workAreaWith1ViewToolStripMenuItem
			// 
			this.workAreaWith1ViewToolStripMenuItem.Name = "workAreaWith1ViewToolStripMenuItem";
			this.workAreaWith1ViewToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.workAreaWith1ViewToolStripMenuItem.Text = "Work Area with 1 View";
			this.workAreaWith1ViewToolStripMenuItem.Click += new System.EventHandler( this.workAreaWith1ViewToolStripMenuItem_Click );
			// 
			// workAreaWith4ViewsToolStripMenuItem
			// 
			this.workAreaWith4ViewsToolStripMenuItem.Name = "workAreaWith4ViewsToolStripMenuItem";
			this.workAreaWith4ViewsToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.workAreaWith4ViewsToolStripMenuItem.Text = "Work Area with 4 Views";
			this.workAreaWith4ViewsToolStripMenuItem.Click += new System.EventHandler( this.workAreaWith4ViewsToolStripMenuItem_Click );
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size( 230, 6 );
			// 
			// entityTypesToolStripMenuItem
			// 
			this.entityTypesToolStripMenuItem.Name = "entityTypesToolStripMenuItem";
			this.entityTypesToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.entityTypesToolStripMenuItem.Text = "&Entity Types Window";
			this.entityTypesToolStripMenuItem.Click += new System.EventHandler( this.entityTypesToolStripMenuItem_Click );
			// 
			// propertiesWindowToolStripMenuItem
			// 
			this.propertiesWindowToolStripMenuItem.Name = "propertiesWindowToolStripMenuItem";
			this.propertiesWindowToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.propertiesWindowToolStripMenuItem.Text = "&Properties Window";
			this.propertiesWindowToolStripMenuItem.Click += new System.EventHandler( this.propertiesWindowToolStripMenuItem_Click );
			// 
			// outputToolStripMenuItem
			// 
			this.outputToolStripMenuItem.Name = "outputToolStripMenuItem";
			this.outputToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.outputToolStripMenuItem.Text = "&Output Window";
			this.outputToolStripMenuItem.Click += new System.EventHandler( this.outputToolStripMenuItem_Click );
			// 
			// example3DViewToolStripMenuItem
			// 
			this.example3DViewToolStripMenuItem.Name = "example3DViewToolStripMenuItem";
			this.example3DViewToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.example3DViewToolStripMenuItem.Text = "&3D View Window";
			this.example3DViewToolStripMenuItem.Click += new System.EventHandler( this.example3DViewToolStripMenuItem_Click );
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size( 230, 6 );
			// 
			// statusBarToolStripMenuItem
			// 
			this.statusBarToolStripMenuItem.Checked = true;
			this.statusBarToolStripMenuItem.CheckOnClick = true;
			this.statusBarToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.statusBarToolStripMenuItem.Name = "statusBarToolStripMenuItem";
			this.statusBarToolStripMenuItem.Size = new System.Drawing.Size( 233, 24 );
			this.statusBarToolStripMenuItem.Text = "Status Bar";
			this.statusBarToolStripMenuItem.Click += new System.EventHandler( this.statusBarToolStripMenuItem_Click );
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem} );
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size( 53, 23 );
			this.toolsToolStripMenuItem.Text = "&Tools";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.Image = global::WinFormsMultiViewAppExample.Properties.Resources.Options;
			this.optionsToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size( 136, 24 );
			this.optionsToolStripMenuItem.Text = "&Options...";
			this.optionsToolStripMenuItem.Click += new System.EventHandler( this.optionsToolStripMenuItem_Click );
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.documentationToolStripMenuItem,
            this.toolStripMenuItem4,
            this.aboutToolStripMenuItem} );
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size( 49, 23 );
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// documentationToolStripMenuItem
			// 
			this.documentationToolStripMenuItem.Name = "documentationToolStripMenuItem";
			this.documentationToolStripMenuItem.Size = new System.Drawing.Size( 173, 24 );
			this.documentationToolStripMenuItem.Text = "Documentation";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size( 170, 6 );
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size( 173, 24 );
			this.aboutToolStripMenuItem.Text = "&About...";
			this.aboutToolStripMenuItem.Click += new System.EventHandler( this.aboutToolStripMenuItem_Click );
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1} );
			this.statusStrip1.Location = new System.Drawing.Point( 0, 629 );
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size( 921, 24 );
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.BackColor = System.Drawing.Color.Transparent;
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size( 46, 19 );
			this.toolStripStatusLabel1.Text = "Ready";
			// 
			// toolStripGeneral
			// 
			this.toolStripGeneral.AutoSize = false;
			this.toolStripGeneral.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonOpen,
            this.toolStripSeparator4,
            this.toolStripButtonOptions} );
			this.toolStripGeneral.Location = new System.Drawing.Point( 0, 27 );
			this.toolStripGeneral.Name = "toolStripGeneral";
			this.toolStripGeneral.Size = new System.Drawing.Size( 921, 28 );
			this.toolStripGeneral.TabIndex = 2;
			this.toolStripGeneral.Text = "toolStripGeneral";
			// 
			// toolStripButtonOpen
			// 
			this.toolStripButtonOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonOpen.Image = global::WinFormsMultiViewAppExample.Properties.Resources.Open;
			this.toolStripButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonOpen.Name = "toolStripButtonOpen";
			this.toolStripButtonOpen.Size = new System.Drawing.Size( 23, 25 );
			this.toolStripButtonOpen.Text = "Open";
			this.toolStripButtonOpen.Click += new System.EventHandler( this.openToolStripMenuItem_Click );
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size( 6, 28 );
			// 
			// toolStripButtonOptions
			// 
			this.toolStripButtonOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonOptions.Image = global::WinFormsMultiViewAppExample.Properties.Resources.Options;
			this.toolStripButtonOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonOptions.Name = "toolStripButtonOptions";
			this.toolStripButtonOptions.Size = new System.Drawing.Size( 23, 25 );
			this.toolStripButtonOptions.Text = "Options";
			this.toolStripButtonOptions.Click += new System.EventHandler( this.optionsToolStripMenuItem_Click );
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler( this.timer1_Tick );
			// 
			// timerShowLogicEditor
			// 
			this.timerShowLogicEditor.Interval = 200;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 921, 653 );
			this.Controls.Add( this.toolStripGeneral );
			this.Controls.Add( this.statusStrip1 );
			this.Controls.Add( this.menuStrip );
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.MainMenuStrip = this.menuStrip;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Multi View Application";
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MainForm_FormClosed );
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.MainForm_FormClosing );
			this.menuStrip.ResumeLayout( false );
			this.menuStrip.PerformLayout();
			this.statusStrip1.ResumeLayout( false );
			this.statusStrip1.PerformLayout();
			this.toolStripGeneral.ResumeLayout( false );
			this.toolStripGeneral.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem entityTypesToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripMenuItem example3DViewToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStrip toolStripGeneral;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ToolStripMenuItem propertiesWindowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem outputToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.Timer timerShowLogicEditor;
		private System.Windows.Forms.ToolStripButton toolStripButtonOpen;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripButton toolStripButtonOptions;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem documentationToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem statusBarToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem workAreaWithoutViewsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem workAreaWith1ViewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem workAreaWith4ViewsToolStripMenuItem;
	}
}

