namespace ChatExample
{
	partial class ServerForm
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
			this.listBoxLog = new System.Windows.Forms.ListBox();
			this.buttonCreate = new System.Windows.Forms.Button();
			this.buttonDestroy = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer( this.components );
			this.listBoxUsers = new System.Windows.Forms.ListBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBoxLog
			// 
			this.listBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxLog.FormattingEnabled = true;
			this.listBoxLog.HorizontalScrollbar = true;
			this.listBoxLog.IntegralHeight = false;
			this.listBoxLog.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxLog.Name = "listBoxLog";
			this.listBoxLog.Size = new System.Drawing.Size( 300, 268 );
			this.listBoxLog.TabIndex = 0;
			// 
			// buttonCreate
			// 
			this.buttonCreate.Location = new System.Drawing.Point( 12, 12 );
			this.buttonCreate.Name = "buttonCreate";
			this.buttonCreate.Size = new System.Drawing.Size( 88, 26 );
			this.buttonCreate.TabIndex = 1;
			this.buttonCreate.Text = "Create";
			this.buttonCreate.UseVisualStyleBackColor = true;
			this.buttonCreate.Click += new System.EventHandler( this.buttonCreate_Click );
			// 
			// buttonDestroy
			// 
			this.buttonDestroy.Enabled = false;
			this.buttonDestroy.Location = new System.Drawing.Point( 106, 12 );
			this.buttonDestroy.Name = "buttonDestroy";
			this.buttonDestroy.Size = new System.Drawing.Size( 88, 26 );
			this.buttonDestroy.TabIndex = 2;
			this.buttonDestroy.Text = "Destroy";
			this.buttonDestroy.UseVisualStyleBackColor = true;
			this.buttonDestroy.Click += new System.EventHandler( this.buttonDestroy_Click );
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler( this.timer1_Tick );
			// 
			// listBoxUsers
			// 
			this.listBoxUsers.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxUsers.FormattingEnabled = true;
			this.listBoxUsers.IntegralHeight = false;
			this.listBoxUsers.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxUsers.Name = "listBoxUsers";
			this.listBoxUsers.Size = new System.Drawing.Size( 145, 268 );
			this.listBoxUsers.TabIndex = 3;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.splitContainer1.Location = new System.Drawing.Point( 12, 44 );
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add( this.listBoxLog );
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add( this.listBoxUsers );
			this.splitContainer1.Size = new System.Drawing.Size( 449, 268 );
			this.splitContainer1.SplitterDistance = 300;
			this.splitContainer1.TabIndex = 4;
			// 
			// ServerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 473, 324 );
			this.Controls.Add( this.splitContainer1 );
			this.Controls.Add( this.buttonDestroy );
			this.Controls.Add( this.buttonCreate );
			this.Name = "ServerForm";
			this.ShowIcon = false;
			this.Text = "Server";
			this.splitContainer1.Panel1.ResumeLayout( false );
			this.splitContainer1.Panel2.ResumeLayout( false );
			this.splitContainer1.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxLog;
		private System.Windows.Forms.Button buttonCreate;
		private System.Windows.Forms.Button buttonDestroy;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ListBox listBoxUsers;
		private System.Windows.Forms.SplitContainer splitContainer1;
	}
}