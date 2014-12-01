namespace ChatExample
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MainForm ) );
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonServer = new System.Windows.Forms.Button();
			this.buttonClient = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonCancel.Location = new System.Drawing.Point( 204, 12 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 88, 26 );
			this.buttonCancel.TabIndex = 0;
			this.buttonCancel.Text = "Close";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler( this.buttonCancel_Click );
			// 
			// buttonServer
			// 
			this.buttonServer.Location = new System.Drawing.Point( 12, 12 );
			this.buttonServer.Name = "buttonServer";
			this.buttonServer.Size = new System.Drawing.Size( 120, 26 );
			this.buttonServer.TabIndex = 1;
			this.buttonServer.Text = "Create Server";
			this.buttonServer.UseVisualStyleBackColor = true;
			this.buttonServer.Click += new System.EventHandler( this.buttonServer_Click );
			// 
			// buttonClient
			// 
			this.buttonClient.Location = new System.Drawing.Point( 12, 42 );
			this.buttonClient.Name = "buttonClient";
			this.buttonClient.Size = new System.Drawing.Size( 120, 26 );
			this.buttonClient.TabIndex = 2;
			this.buttonClient.Text = "Create Client";
			this.buttonClient.UseVisualStyleBackColor = true;
			this.buttonClient.Click += new System.EventHandler( this.buttonClient_Click );
			// 
			// MainForm
			// 
			this.AcceptButton = this.buttonCancel;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 304, 80 );
			this.Controls.Add( this.buttonClient );
			this.Controls.Add( this.buttonServer );
			this.Controls.Add( this.buttonCancel );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Chat Example";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonServer;
		private System.Windows.Forms.Button buttonClient;
	}
}

