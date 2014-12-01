namespace WinFormsAppExample
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
			this.buttonExit = new System.Windows.Forms.Button();
			this.labelEngineVersion = new System.Windows.Forms.Label();
			this.renderTargetUserControl1 = new WinFormsAppFramework.RenderTargetUserControl();
			this.buttonAdditionalForm = new System.Windows.Forms.Button();
			this.buttonCreateBox = new System.Windows.Forms.Button();
			this.trackBarVolume = new System.Windows.Forms.TrackBar();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonShowUI = new System.Windows.Forms.Button();
			this.buttonConnect = new System.Windows.Forms.Button();
			this.comboBoxMaps = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.buttonLoadMap = new System.Windows.Forms.Button();
			this.buttonDestroy = new System.Windows.Forms.Button();
			this.labelNetworkStatus = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxServerAddress = new System.Windows.Forms.TextBox();
			this.textBoxUserName = new System.Windows.Forms.TextBox();
			( (System.ComponentModel.ISupportInitialize)( this.trackBarVolume ) ).BeginInit();
			this.SuspendLayout();
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonExit.Location = new System.Drawing.Point( 738, 12 );
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size( 88, 26 );
			this.buttonExit.TabIndex = 6;
			this.buttonExit.Text = "E&xit";
			this.buttonExit.UseVisualStyleBackColor = true;
			this.buttonExit.Click += new System.EventHandler( this.buttonExit_Click );
			// 
			// labelEngineVersion
			// 
			this.labelEngineVersion.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.labelEngineVersion.AutoSize = true;
			this.labelEngineVersion.Location = new System.Drawing.Point( 9, 583 );
			this.labelEngineVersion.Name = "labelEngineVersion";
			this.labelEngineVersion.Size = new System.Drawing.Size( 143, 15 );
			this.labelEngineVersion.TabIndex = 4;
			this.labelEngineVersion.Text = "2014 NeoAxis Group Ltd.";
			// 
			// renderTargetUserControl1
			// 
			this.renderTargetUserControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.renderTargetUserControl1.AutomaticUpdateFPS = 30F;
			this.renderTargetUserControl1.BackColor = System.Drawing.Color.Black;
			this.renderTargetUserControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.renderTargetUserControl1.Location = new System.Drawing.Point( 12, 92 );
			this.renderTargetUserControl1.MouseRelativeMode = false;
			this.renderTargetUserControl1.Name = "renderTargetUserControl1";
			this.renderTargetUserControl1.Size = new System.Drawing.Size( 720, 487 );
			this.renderTargetUserControl1.TabIndex = 4;
			// 
			// buttonAdditionalForm
			// 
			this.buttonAdditionalForm.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonAdditionalForm.Location = new System.Drawing.Point( 738, 175 );
			this.buttonAdditionalForm.Name = "buttonAdditionalForm";
			this.buttonAdditionalForm.Size = new System.Drawing.Size( 88, 26 );
			this.buttonAdditionalForm.TabIndex = 7;
			this.buttonAdditionalForm.Text = "New Form";
			this.buttonAdditionalForm.UseVisualStyleBackColor = true;
			this.buttonAdditionalForm.Click += new System.EventHandler( this.buttonAdditionalForm_Click );
			// 
			// buttonCreateBox
			// 
			this.buttonCreateBox.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCreateBox.Location = new System.Drawing.Point( 738, 229 );
			this.buttonCreateBox.Name = "buttonCreateBox";
			this.buttonCreateBox.Size = new System.Drawing.Size( 88, 26 );
			this.buttonCreateBox.TabIndex = 8;
			this.buttonCreateBox.Text = "Create box";
			this.buttonCreateBox.UseVisualStyleBackColor = true;
			this.buttonCreateBox.Click += new System.EventHandler( this.buttonCreateBox_Click );
			// 
			// trackBarVolume
			// 
			this.trackBarVolume.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.trackBarVolume.LargeChange = 50;
			this.trackBarVolume.Location = new System.Drawing.Point( 745, 331 );
			this.trackBarVolume.Maximum = 100;
			this.trackBarVolume.Name = "trackBarVolume";
			this.trackBarVolume.Size = new System.Drawing.Size( 81, 50 );
			this.trackBarVolume.TabIndex = 10;
			this.trackBarVolume.TickFrequency = 10;
			this.trackBarVolume.Value = 25;
			this.trackBarVolume.Scroll += new System.EventHandler( this.trackBarVolume_Scroll );
			// 
			// label1
			// 
			this.label1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 748, 313 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 52, 15 );
			this.label1.TabIndex = 8;
			this.label1.Text = "Volume:";
			// 
			// buttonShowUI
			// 
			this.buttonShowUI.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonShowUI.Location = new System.Drawing.Point( 738, 258 );
			this.buttonShowUI.Name = "buttonShowUI";
			this.buttonShowUI.Size = new System.Drawing.Size( 88, 26 );
			this.buttonShowUI.TabIndex = 9;
			this.buttonShowUI.Text = "Show UI";
			this.buttonShowUI.UseVisualStyleBackColor = true;
			this.buttonShowUI.Click += new System.EventHandler( this.buttonShowUI_Click );
			// 
			// buttonConnect
			// 
			this.buttonConnect.Location = new System.Drawing.Point( 364, 54 );
			this.buttonConnect.Name = "buttonConnect";
			this.buttonConnect.Size = new System.Drawing.Size( 88, 26 );
			this.buttonConnect.TabIndex = 5;
			this.buttonConnect.Text = "Connect";
			this.buttonConnect.UseVisualStyleBackColor = true;
			this.buttonConnect.Click += new System.EventHandler( this.buttonConnect_Click );
			// 
			// comboBoxMaps
			// 
			this.comboBoxMaps.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMaps.FormattingEnabled = true;
			this.comboBoxMaps.Location = new System.Drawing.Point( 67, 15 );
			this.comboBoxMaps.Name = "comboBoxMaps";
			this.comboBoxMaps.Size = new System.Drawing.Size( 243, 21 );
			this.comboBoxMaps.TabIndex = 0;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 12, 16 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 35, 15 );
			this.label3.TabIndex = 17;
			this.label3.Text = "Map:";
			// 
			// buttonLoadMap
			// 
			this.buttonLoadMap.Location = new System.Drawing.Point( 316, 12 );
			this.buttonLoadMap.Name = "buttonLoadMap";
			this.buttonLoadMap.Size = new System.Drawing.Size( 88, 26 );
			this.buttonLoadMap.TabIndex = 1;
			this.buttonLoadMap.Text = "Load";
			this.buttonLoadMap.UseVisualStyleBackColor = true;
			this.buttonLoadMap.Click += new System.EventHandler( this.buttonLoadMap_Click );
			// 
			// buttonDestroy
			// 
			this.buttonDestroy.Location = new System.Drawing.Point( 499, 12 );
			this.buttonDestroy.Name = "buttonDestroy";
			this.buttonDestroy.Size = new System.Drawing.Size( 88, 26 );
			this.buttonDestroy.TabIndex = 2;
			this.buttonDestroy.Text = "Destroy";
			this.buttonDestroy.UseVisualStyleBackColor = true;
			this.buttonDestroy.Click += new System.EventHandler( this.buttonDestroy_Click );
			// 
			// labelNetworkStatus
			// 
			this.labelNetworkStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelNetworkStatus.Location = new System.Drawing.Point( 458, 57 );
			this.labelNetworkStatus.Name = "labelNetworkStatus";
			this.labelNetworkStatus.Size = new System.Drawing.Size( 131, 20 );
			this.labelNetworkStatus.TabIndex = 3;
			this.labelNetworkStatus.Text = "{Status}";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 12, 57 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 104, 15 );
			this.label2.TabIndex = 8;
			this.label2.Text = "Connect to server:";
			// 
			// textBoxServerAddress
			// 
			this.textBoxServerAddress.Location = new System.Drawing.Point( 122, 57 );
			this.textBoxServerAddress.Name = "textBoxServerAddress";
			this.textBoxServerAddress.Size = new System.Drawing.Size( 132, 20 );
			this.textBoxServerAddress.TabIndex = 3;
			this.textBoxServerAddress.Text = "127.0.0.1";
			// 
			// textBoxUserName
			// 
			this.textBoxUserName.Location = new System.Drawing.Point( 260, 57 );
			this.textBoxUserName.Name = "textBoxUserName";
			this.textBoxUserName.Size = new System.Drawing.Size( 98, 20 );
			this.textBoxUserName.TabIndex = 4;
			this.textBoxUserName.Text = "TestUser";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 838, 605 );
			this.Controls.Add( this.textBoxUserName );
			this.Controls.Add( this.textBoxServerAddress );
			this.Controls.Add( this.labelNetworkStatus );
			this.Controls.Add( this.buttonDestroy );
			this.Controls.Add( this.buttonLoadMap );
			this.Controls.Add( this.label3 );
			this.Controls.Add( this.comboBoxMaps );
			this.Controls.Add( this.buttonConnect );
			this.Controls.Add( this.buttonShowUI );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.trackBarVolume );
			this.Controls.Add( this.buttonCreateBox );
			this.Controls.Add( this.buttonAdditionalForm );
			this.Controls.Add( this.renderTargetUserControl1 );
			this.Controls.Add( this.labelEngineVersion );
			this.Controls.Add( this.buttonExit );
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Windows Application Example";
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MainForm_FormClosed );
			( (System.ComponentModel.ISupportInitialize)( this.trackBarVolume ) ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Label labelEngineVersion;
		private WinFormsAppFramework.RenderTargetUserControl renderTargetUserControl1;
		private System.Windows.Forms.Button buttonAdditionalForm;
		private System.Windows.Forms.Button buttonCreateBox;
		private System.Windows.Forms.TrackBar trackBarVolume;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonShowUI;
		private System.Windows.Forms.Button buttonConnect;
		private System.Windows.Forms.ComboBox comboBoxMaps;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonLoadMap;
		private System.Windows.Forms.Button buttonDestroy;
		private System.Windows.Forms.Label labelNetworkStatus;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxServerAddress;
		private System.Windows.Forms.TextBox textBoxUserName;

	}
}

