namespace DedicatedServer
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
			this.buttonClose = new System.Windows.Forms.Button();
			this.buttonCreate = new System.Windows.Forms.Button();
			this.buttonDestroy = new System.Windows.Forms.Button();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.listBoxLog = new System.Windows.Forms.ListBox();
			this.listBoxUsers = new System.Windows.Forms.ListBox();
			this.timer1 = new System.Windows.Forms.Timer( this.components );
			this.comboBoxMaps = new System.Windows.Forms.ComboBox();
			this.buttonDoSomething = new System.Windows.Forms.Button();
			this.checkBoxLoadMapAtStartup = new System.Windows.Forms.CheckBox();
			this.buttonMapLoad = new System.Windows.Forms.Button();
			this.buttonMapUnload = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.buttonMapChange = new System.Windows.Forms.Button();
			this.checkBoxAllowCustomClientCommands = new System.Windows.Forms.CheckBox();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Location = new System.Drawing.Point( 558, 11 );
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size( 88, 26 );
			this.buttonClose.TabIndex = 4;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			this.buttonClose.Click += new System.EventHandler( this.buttonClose_Click );
			// 
			// buttonCreate
			// 
			this.buttonCreate.Location = new System.Drawing.Point( 63, 12 );
			this.buttonCreate.Name = "buttonCreate";
			this.buttonCreate.Size = new System.Drawing.Size( 88, 26 );
			this.buttonCreate.TabIndex = 0;
			this.buttonCreate.Text = "Create";
			this.buttonCreate.UseVisualStyleBackColor = true;
			this.buttonCreate.Click += new System.EventHandler( this.buttonCreate_Click );
			// 
			// buttonDestroy
			// 
			this.buttonDestroy.Enabled = false;
			this.buttonDestroy.Location = new System.Drawing.Point( 157, 12 );
			this.buttonDestroy.Name = "buttonDestroy";
			this.buttonDestroy.Size = new System.Drawing.Size( 88, 26 );
			this.buttonDestroy.TabIndex = 1;
			this.buttonDestroy.Text = "Destroy";
			this.buttonDestroy.UseVisualStyleBackColor = true;
			this.buttonDestroy.Click += new System.EventHandler( this.buttonDestroy_Click );
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.splitContainer1.Location = new System.Drawing.Point( 12, 105 );
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add( this.listBoxLog );
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add( this.listBoxUsers );
			this.splitContainer1.Size = new System.Drawing.Size( 634, 347 );
			this.splitContainer1.SplitterDistance = 378;
			this.splitContainer1.TabIndex = 5;
			// 
			// listBoxLog
			// 
			this.listBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxLog.FormattingEnabled = true;
			this.listBoxLog.HorizontalScrollbar = true;
			this.listBoxLog.IntegralHeight = false;
			this.listBoxLog.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxLog.Name = "listBoxLog";
			this.listBoxLog.Size = new System.Drawing.Size( 378, 347 );
			this.listBoxLog.TabIndex = 0;
			// 
			// listBoxUsers
			// 
			this.listBoxUsers.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxUsers.FormattingEnabled = true;
			this.listBoxUsers.IntegralHeight = false;
			this.listBoxUsers.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxUsers.Name = "listBoxUsers";
			this.listBoxUsers.Size = new System.Drawing.Size( 252, 347 );
			this.listBoxUsers.TabIndex = 0;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 5;
			this.timer1.Tick += new System.EventHandler( this.timer1_Tick );
			// 
			// comboBoxMaps
			// 
			this.comboBoxMaps.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.comboBoxMaps.DropDownHeight = 318;
			this.comboBoxMaps.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMaps.FormattingEnabled = true;
			this.comboBoxMaps.IntegralHeight = false;
			this.comboBoxMaps.Location = new System.Drawing.Point( 345, 48 );
			this.comboBoxMaps.Name = "comboBoxMaps";
			this.comboBoxMaps.Size = new System.Drawing.Size( 301, 21 );
			this.comboBoxMaps.TabIndex = 8;
			// 
			// buttonDoSomething
			// 
			this.buttonDoSomething.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonDoSomething.Location = new System.Drawing.Point( 452, 11 );
			this.buttonDoSomething.Name = "buttonDoSomething";
			this.buttonDoSomething.Size = new System.Drawing.Size( 100, 26 );
			this.buttonDoSomething.TabIndex = 3;
			this.buttonDoSomething.Text = "Do something";
			this.buttonDoSomething.UseVisualStyleBackColor = true;
			this.buttonDoSomething.Click += new System.EventHandler( this.buttonDoSomething_Click );
			// 
			// checkBoxLoadMapAtStartup
			// 
			this.checkBoxLoadMapAtStartup.AutoSize = true;
			this.checkBoxLoadMapAtStartup.Location = new System.Drawing.Point( 255, 16 );
			this.checkBoxLoadMapAtStartup.Name = "checkBoxLoadMapAtStartup";
			this.checkBoxLoadMapAtStartup.Size = new System.Drawing.Size( 135, 19 );
			this.checkBoxLoadMapAtStartup.TabIndex = 2;
			this.checkBoxLoadMapAtStartup.Text = "Load map at startup";
			this.checkBoxLoadMapAtStartup.UseVisualStyleBackColor = true;
			this.checkBoxLoadMapAtStartup.CheckedChanged += new System.EventHandler( this.checkBoxLoadMapAtStartup_CheckedChanged );
			// 
			// buttonMapLoad
			// 
			this.buttonMapLoad.Enabled = false;
			this.buttonMapLoad.Location = new System.Drawing.Point( 63, 44 );
			this.buttonMapLoad.Name = "buttonMapLoad";
			this.buttonMapLoad.Size = new System.Drawing.Size( 88, 26 );
			this.buttonMapLoad.TabIndex = 5;
			this.buttonMapLoad.Text = "Load";
			this.buttonMapLoad.UseVisualStyleBackColor = true;
			this.buttonMapLoad.Click += new System.EventHandler( this.buttonMapLoad_Click );
			// 
			// buttonMapUnload
			// 
			this.buttonMapUnload.Enabled = false;
			this.buttonMapUnload.Location = new System.Drawing.Point( 251, 44 );
			this.buttonMapUnload.Name = "buttonMapUnload";
			this.buttonMapUnload.Size = new System.Drawing.Size( 88, 26 );
			this.buttonMapUnload.TabIndex = 7;
			this.buttonMapUnload.Text = "Unload";
			this.buttonMapUnload.UseVisualStyleBackColor = true;
			this.buttonMapUnload.Click += new System.EventHandler( this.buttonMapUnload_Click );
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 12, 15 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 45, 15 );
			this.label2.TabIndex = 10;
			this.label2.Text = "Server:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 12, 48 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 35, 15 );
			this.label3.TabIndex = 11;
			this.label3.Text = "Map:";
			// 
			// buttonMapChange
			// 
			this.buttonMapChange.Enabled = false;
			this.buttonMapChange.Location = new System.Drawing.Point( 157, 44 );
			this.buttonMapChange.Name = "buttonMapChange";
			this.buttonMapChange.Size = new System.Drawing.Size( 88, 26 );
			this.buttonMapChange.TabIndex = 6;
			this.buttonMapChange.Text = "Change";
			this.buttonMapChange.UseVisualStyleBackColor = true;
			this.buttonMapChange.Click += new System.EventHandler( this.buttonMapChange_Click );
			// 
			// checkBoxAllowCustomClientCommands
			// 
			this.checkBoxAllowCustomClientCommands.AutoSize = true;
			this.checkBoxAllowCustomClientCommands.Location = new System.Drawing.Point( 12, 80 );
			this.checkBoxAllowCustomClientCommands.Name = "checkBoxAllowCustomClientCommands";
			this.checkBoxAllowCustomClientCommands.Size = new System.Drawing.Size( 364, 19 );
			this.checkBoxAllowCustomClientCommands.TabIndex = 12;
			this.checkBoxAllowCustomClientCommands.Text = "Allow custom client commands (MapLoad, CreateMapObject).";
			this.checkBoxAllowCustomClientCommands.UseVisualStyleBackColor = true;
			this.checkBoxAllowCustomClientCommands.CheckedChanged += new System.EventHandler( this.checkBoxAllowCustomClientCommands_CheckedChanged );
			// 
			// MainForm
			// 
			this.AcceptButton = this.buttonClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 658, 464 );
			this.Controls.Add( this.checkBoxAllowCustomClientCommands );
			this.Controls.Add( this.label3 );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.buttonMapUnload );
			this.Controls.Add( this.buttonMapChange );
			this.Controls.Add( this.buttonMapLoad );
			this.Controls.Add( this.checkBoxLoadMapAtStartup );
			this.Controls.Add( this.buttonDoSomething );
			this.Controls.Add( this.comboBoxMaps );
			this.Controls.Add( this.splitContainer1 );
			this.Controls.Add( this.buttonDestroy );
			this.Controls.Add( this.buttonCreate );
			this.Controls.Add( this.buttonClose );
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Dedicated Server";
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MainForm_FormClosed );
			this.splitContainer1.Panel1.ResumeLayout( false );
			this.splitContainer1.Panel2.ResumeLayout( false );
			this.splitContainer1.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.Button buttonCreate;
		private System.Windows.Forms.Button buttonDestroy;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ListBox listBoxLog;
		private System.Windows.Forms.ListBox listBoxUsers;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ComboBox comboBoxMaps;
		private System.Windows.Forms.Button buttonDoSomething;
		private System.Windows.Forms.CheckBox checkBoxLoadMapAtStartup;
		private System.Windows.Forms.Button buttonMapLoad;
		private System.Windows.Forms.Button buttonMapUnload;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonMapChange;
		private System.Windows.Forms.CheckBox checkBoxAllowCustomClientCommands;
	}
}

