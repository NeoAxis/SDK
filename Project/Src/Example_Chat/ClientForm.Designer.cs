namespace ChatExample
{
	partial class ClientForm
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
			this.buttonConnect = new System.Windows.Forms.Button();
			this.buttonDisconnect = new System.Windows.Forms.Button();
			this.textBoxAddress = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.timer1 = new System.Windows.Forms.Timer( this.components );
			this.textBoxEnterText = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonSend = new System.Windows.Forms.Button();
			this.listBoxUsers = new System.Windows.Forms.ListBox();
			this.textBoxUserName = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
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
			this.listBoxLog.Size = new System.Drawing.Size( 330, 212 );
			this.listBoxLog.TabIndex = 0;
			// 
			// buttonConnect
			// 
			this.buttonConnect.Location = new System.Drawing.Point( 12, 12 );
			this.buttonConnect.Name = "buttonConnect";
			this.buttonConnect.Size = new System.Drawing.Size( 88, 26 );
			this.buttonConnect.TabIndex = 1;
			this.buttonConnect.Text = "Connect";
			this.buttonConnect.UseVisualStyleBackColor = true;
			this.buttonConnect.Click += new System.EventHandler( this.buttonConnect_Click );
			// 
			// buttonDisconnect
			// 
			this.buttonDisconnect.Enabled = false;
			this.buttonDisconnect.Location = new System.Drawing.Point( 106, 11 );
			this.buttonDisconnect.Name = "buttonDisconnect";
			this.buttonDisconnect.Size = new System.Drawing.Size( 88, 26 );
			this.buttonDisconnect.TabIndex = 2;
			this.buttonDisconnect.Text = "Disconnect";
			this.buttonDisconnect.UseVisualStyleBackColor = true;
			this.buttonDisconnect.Click += new System.EventHandler( this.buttonDisconnect_Click );
			// 
			// textBoxAddress
			// 
			this.textBoxAddress.Location = new System.Drawing.Point( 307, 18 );
			this.textBoxAddress.Name = "textBoxAddress";
			this.textBoxAddress.Size = new System.Drawing.Size( 130, 20 );
			this.textBoxAddress.TabIndex = 0;
			this.textBoxAddress.Text = "127.0.0.1";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 209, 18 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 92, 15 );
			this.label1.TabIndex = 4;
			this.label1.Text = "Server address:";
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 10;
			this.timer1.Tick += new System.EventHandler( this.timer1_Tick );
			// 
			// textBoxEnterText
			// 
			this.textBoxEnterText.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.textBoxEnterText.Location = new System.Drawing.Point( 12, 305 );
			this.textBoxEnterText.Name = "textBoxEnterText";
			this.textBoxEnterText.ReadOnly = true;
			this.textBoxEnterText.Size = new System.Drawing.Size( 371, 20 );
			this.textBoxEnterText.TabIndex = 2;
			this.textBoxEnterText.KeyDown += new System.Windows.Forms.KeyEventHandler( this.textBoxEnterText_KeyDown );
			// 
			// label2
			// 
			this.label2.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 12, 289 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 89, 15 );
			this.label2.TabIndex = 6;
			this.label2.Text = "Enter text here:";
			// 
			// buttonSend
			// 
			this.buttonSend.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonSend.Location = new System.Drawing.Point( 389, 301 );
			this.buttonSend.Name = "buttonSend";
			this.buttonSend.Size = new System.Drawing.Size( 88, 26 );
			this.buttonSend.TabIndex = 3;
			this.buttonSend.Text = "Send";
			this.buttonSend.UseVisualStyleBackColor = true;
			this.buttonSend.Click += new System.EventHandler( this.buttonSend_Click );
			// 
			// listBoxUsers
			// 
			this.listBoxUsers.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxUsers.FormattingEnabled = true;
			this.listBoxUsers.IntegralHeight = false;
			this.listBoxUsers.Location = new System.Drawing.Point( 0, 0 );
			this.listBoxUsers.Name = "listBoxUsers";
			this.listBoxUsers.Size = new System.Drawing.Size( 131, 212 );
			this.listBoxUsers.TabIndex = 8;
			// 
			// textBoxUserName
			// 
			this.textBoxUserName.Location = new System.Drawing.Point( 62, 46 );
			this.textBoxUserName.Name = "textBoxUserName";
			this.textBoxUserName.Size = new System.Drawing.Size( 206, 20 );
			this.textBoxUserName.TabIndex = 1;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 12, 47 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 44, 15 );
			this.label3.TabIndex = 10;
			this.label3.Text = "Name:";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.splitContainer1.Location = new System.Drawing.Point( 12, 72 );
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add( this.listBoxLog );
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add( this.listBoxUsers );
			this.splitContainer1.Size = new System.Drawing.Size( 465, 212 );
			this.splitContainer1.SplitterDistance = 330;
			this.splitContainer1.TabIndex = 11;
			// 
			// ClientForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 489, 337 );
			this.Controls.Add( this.splitContainer1 );
			this.Controls.Add( this.label3 );
			this.Controls.Add( this.textBoxUserName );
			this.Controls.Add( this.buttonSend );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.textBoxEnterText );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.textBoxAddress );
			this.Controls.Add( this.buttonDisconnect );
			this.Controls.Add( this.buttonConnect );
			this.Name = "ClientForm";
			this.ShowIcon = false;
			this.Text = "Client";
			this.splitContainer1.Panel1.ResumeLayout( false );
			this.splitContainer1.Panel2.ResumeLayout( false );
			this.splitContainer1.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxLog;
		private System.Windows.Forms.Button buttonConnect;
		private System.Windows.Forms.Button buttonDisconnect;
		private System.Windows.Forms.TextBox textBoxAddress;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.TextBox textBoxEnterText;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonSend;
		private System.Windows.Forms.ListBox listBoxUsers;
		private System.Windows.Forms.TextBox textBoxUserName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.SplitContainer splitContainer1;
	}
}