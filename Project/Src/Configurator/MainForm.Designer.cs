namespace Configurator
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPageGeneral = new System.Windows.Forms.TabPage();
			this.buttonRestart = new System.Windows.Forms.Button();
			this.label11 = new System.Windows.Forms.Label();
			this.label21 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.checkBoxLocalizeToolset = new System.Windows.Forms.CheckBox();
			this.checkBoxLocalizeEngine = new System.Windows.Forms.CheckBox();
			this.label10 = new System.Windows.Forms.Label();
			this.comboBoxLanguages = new System.Windows.Forms.ComboBox();
			this.checkBoxVerticalSync = new System.Windows.Forms.CheckBox();
			this.checkBoxFullScreen = new System.Windows.Forms.CheckBox();
			this.comboBoxVideoMode = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.buttonRunGame = new System.Windows.Forms.Button();
			this.label15 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.buttonRunResourceEditor = new System.Windows.Forms.Button();
			this.label14 = new System.Windows.Forms.Label();
			this.pictureBox1MapEditor = new System.Windows.Forms.PictureBox();
			this.buttonRunMapEditor = new System.Windows.Forms.Button();
			this.comboBoxDepthBufferAccess = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.comboBoxRenderTechnique = new System.Windows.Forms.ComboBox();
			this.label19 = new System.Windows.Forms.Label();
			this.comboBoxFiltering = new System.Windows.Forms.ComboBox();
			this.label9 = new System.Windows.Forms.Label();
			this.comboBoxAntialiasing = new System.Windows.Forms.ComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.tabPageAdvanced = new System.Windows.Forms.TabPage();
			this.checkBoxAllowShaders = new System.Windows.Forms.CheckBox();
			this.label26 = new System.Windows.Forms.Label();
			this.pictureBox4 = new System.Windows.Forms.PictureBox();
			this.buttonRunShaderCacheCompiler = new System.Windows.Forms.Button();
			this.label24 = new System.Windows.Forms.Label();
			this.label25 = new System.Windows.Forms.Label();
			this.pictureBox3 = new System.Windows.Forms.PictureBox();
			this.buttonRunDeploymentTool = new System.Windows.Forms.Button();
			this.label23 = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.comboBoxRenderingDevices = new System.Windows.Forms.ComboBox();
			this.label12 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxRenderSystems = new System.Windows.Forms.ComboBox();
			this.label18 = new System.Windows.Forms.Label();
			this.comboBoxPhysicsSystems = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxSoundSystems = new System.Windows.Forms.ComboBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.tabControl1.SuspendLayout();
			this.tabPageGeneral.SuspendLayout();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox2 ) ).BeginInit();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox1 ) ).BeginInit();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox1MapEditor ) ).BeginInit();
			this.tabPageAdvanced.SuspendLayout();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox4 ) ).BeginInit();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox3 ) ).BeginInit();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.tabControl1.Controls.Add( this.tabPageGeneral );
			this.tabControl1.Controls.Add( this.tabPageAdvanced );
			this.tabControl1.Location = new System.Drawing.Point( 16, 15 );
			this.tabControl1.Margin = new System.Windows.Forms.Padding( 4 );
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.Padding = new System.Drawing.Point( 8, 5 );
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size( 778, 750 );
			this.tabControl1.TabIndex = 0;
			// 
			// tabPageGeneral
			// 
			this.tabPageGeneral.Controls.Add( this.buttonRestart );
			this.tabPageGeneral.Controls.Add( this.label11 );
			this.tabPageGeneral.Controls.Add( this.label21 );
			this.tabPageGeneral.Controls.Add( this.label20 );
			this.tabPageGeneral.Controls.Add( this.label17 );
			this.tabPageGeneral.Controls.Add( this.label7 );
			this.tabPageGeneral.Controls.Add( this.checkBoxLocalizeToolset );
			this.tabPageGeneral.Controls.Add( this.checkBoxLocalizeEngine );
			this.tabPageGeneral.Controls.Add( this.label10 );
			this.tabPageGeneral.Controls.Add( this.comboBoxLanguages );
			this.tabPageGeneral.Controls.Add( this.checkBoxVerticalSync );
			this.tabPageGeneral.Controls.Add( this.checkBoxFullScreen );
			this.tabPageGeneral.Controls.Add( this.comboBoxVideoMode );
			this.tabPageGeneral.Controls.Add( this.label4 );
			this.tabPageGeneral.Controls.Add( this.label16 );
			this.tabPageGeneral.Controls.Add( this.pictureBox2 );
			this.tabPageGeneral.Controls.Add( this.buttonRunGame );
			this.tabPageGeneral.Controls.Add( this.label15 );
			this.tabPageGeneral.Controls.Add( this.pictureBox1 );
			this.tabPageGeneral.Controls.Add( this.buttonRunResourceEditor );
			this.tabPageGeneral.Controls.Add( this.label14 );
			this.tabPageGeneral.Controls.Add( this.pictureBox1MapEditor );
			this.tabPageGeneral.Controls.Add( this.buttonRunMapEditor );
			this.tabPageGeneral.Controls.Add( this.comboBoxDepthBufferAccess );
			this.tabPageGeneral.Controls.Add( this.label6 );
			this.tabPageGeneral.Controls.Add( this.comboBoxRenderTechnique );
			this.tabPageGeneral.Controls.Add( this.label19 );
			this.tabPageGeneral.Controls.Add( this.comboBoxFiltering );
			this.tabPageGeneral.Controls.Add( this.label9 );
			this.tabPageGeneral.Controls.Add( this.comboBoxAntialiasing );
			this.tabPageGeneral.Controls.Add( this.label8 );
			this.tabPageGeneral.Location = new System.Drawing.Point( 4, 29 );
			this.tabPageGeneral.Name = "tabPageGeneral";
			this.tabPageGeneral.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageGeneral.Size = new System.Drawing.Size( 770, 717 );
			this.tabPageGeneral.TabIndex = 5;
			this.tabPageGeneral.Text = "General";
			this.tabPageGeneral.UseVisualStyleBackColor = true;
			// 
			// buttonRestart
			// 
			this.buttonRestart.Enabled = false;
			this.buttonRestart.Location = new System.Drawing.Point( 33, 148 );
			this.buttonRestart.Name = "buttonRestart";
			this.buttonRestart.Size = new System.Drawing.Size( 117, 32 );
			this.buttonRestart.TabIndex = 3;
			this.buttonRestart.Text = "Restart";
			this.buttonRestart.UseVisualStyleBackColor = true;
			this.buttonRestart.Click += new System.EventHandler( this.buttonRestart_Click );
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point( 341, 252 );
			this.label11.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size( 399, 155 );
			this.label11.TabIndex = 23;
			this.label11.Text = resources.GetString( "label11.Text" );
			// 
			// label21
			// 
			this.label21.AutoSize = true;
			this.label21.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label21.Location = new System.Drawing.Point( 29, 386 );
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size( 112, 24 );
			this.label21.TabIndex = 22;
			this.label21.Text = "Applications";
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label20.Location = new System.Drawing.Point( 340, 16 );
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size( 99, 24 );
			this.label20.TabIndex = 17;
			this.label20.Text = "Rendering";
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label17.Location = new System.Drawing.Point( 28, 207 );
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size( 71, 24 );
			this.label17.TabIndex = 16;
			this.label17.Text = "Screen";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label7.Location = new System.Drawing.Point( 28, 16 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 108, 24 );
			this.label7.TabIndex = 15;
			this.label7.Text = "Localization";
			// 
			// checkBoxLocalizeToolset
			// 
			this.checkBoxLocalizeToolset.AutoSize = true;
			this.checkBoxLocalizeToolset.Location = new System.Drawing.Point( 33, 124 );
			this.checkBoxLocalizeToolset.Margin = new System.Windows.Forms.Padding( 4 );
			this.checkBoxLocalizeToolset.Name = "checkBoxLocalizeToolset";
			this.checkBoxLocalizeToolset.Size = new System.Drawing.Size( 116, 21 );
			this.checkBoxLocalizeToolset.TabIndex = 2;
			this.checkBoxLocalizeToolset.Text = "Localize tools";
			this.checkBoxLocalizeToolset.UseVisualStyleBackColor = true;
			this.checkBoxLocalizeToolset.CheckedChanged += new System.EventHandler( this.checkBoxLocalizeToolset_CheckedChanged );
			// 
			// checkBoxLocalizeEngine
			// 
			this.checkBoxLocalizeEngine.AutoSize = true;
			this.checkBoxLocalizeEngine.Location = new System.Drawing.Point( 33, 99 );
			this.checkBoxLocalizeEngine.Margin = new System.Windows.Forms.Padding( 4 );
			this.checkBoxLocalizeEngine.Name = "checkBoxLocalizeEngine";
			this.checkBoxLocalizeEngine.Size = new System.Drawing.Size( 121, 21 );
			this.checkBoxLocalizeEngine.TabIndex = 1;
			this.checkBoxLocalizeEngine.Text = "Localize game";
			this.checkBoxLocalizeEngine.UseVisualStyleBackColor = true;
			this.checkBoxLocalizeEngine.CheckedChanged += new System.EventHandler( this.checkBoxLocalizeEngine_CheckedChanged );
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point( 29, 47 );
			this.label10.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size( 76, 17 );
			this.label10.TabIndex = 8;
			this.label10.Text = "Language:";
			// 
			// comboBoxLanguages
			// 
			this.comboBoxLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxLanguages.FormattingEnabled = true;
			this.comboBoxLanguages.Location = new System.Drawing.Point( 33, 67 );
			this.comboBoxLanguages.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxLanguages.Name = "comboBoxLanguages";
			this.comboBoxLanguages.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxLanguages.TabIndex = 0;
			this.comboBoxLanguages.SelectedIndexChanged += new System.EventHandler( this.comboBoxLanguages_SelectedIndexChanged );
			// 
			// checkBoxVerticalSync
			// 
			this.checkBoxVerticalSync.AutoSize = true;
			this.checkBoxVerticalSync.Location = new System.Drawing.Point( 33, 317 );
			this.checkBoxVerticalSync.Margin = new System.Windows.Forms.Padding( 4 );
			this.checkBoxVerticalSync.Name = "checkBoxVerticalSync";
			this.checkBoxVerticalSync.Size = new System.Drawing.Size( 110, 21 );
			this.checkBoxVerticalSync.TabIndex = 6;
			this.checkBoxVerticalSync.Text = "Vertical sync";
			this.checkBoxVerticalSync.UseVisualStyleBackColor = true;
			// 
			// checkBoxFullScreen
			// 
			this.checkBoxFullScreen.AutoSize = true;
			this.checkBoxFullScreen.Location = new System.Drawing.Point( 33, 292 );
			this.checkBoxFullScreen.Margin = new System.Windows.Forms.Padding( 4 );
			this.checkBoxFullScreen.Name = "checkBoxFullScreen";
			this.checkBoxFullScreen.Size = new System.Drawing.Size( 99, 21 );
			this.checkBoxFullScreen.TabIndex = 5;
			this.checkBoxFullScreen.Text = "Full screen";
			this.checkBoxFullScreen.UseVisualStyleBackColor = true;
			// 
			// comboBoxVideoMode
			// 
			this.comboBoxVideoMode.DropDownHeight = 212;
			this.comboBoxVideoMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVideoMode.FormattingEnabled = true;
			this.comboBoxVideoMode.IntegralHeight = false;
			this.comboBoxVideoMode.Location = new System.Drawing.Point( 33, 259 );
			this.comboBoxVideoMode.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxVideoMode.Name = "comboBoxVideoMode";
			this.comboBoxVideoMode.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxVideoMode.TabIndex = 4;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 29, 240 );
			this.label4.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 87, 17 );
			this.label4.TabIndex = 14;
			this.label4.Text = "Video mode:";
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point( 133, 422 );
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size( 453, 96 );
			this.label16.TabIndex = 8;
			this.label16.Text = resources.GetString( "label16.Text" );
			// 
			// pictureBox2
			// 
			this.pictureBox2.Image = global::Configurator.Properties.Resources.game_256x256;
			this.pictureBox2.Location = new System.Drawing.Point( 32, 422 );
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size( 80, 80 );
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox2.TabIndex = 7;
			this.pictureBox2.TabStop = false;
			// 
			// buttonRunGame
			// 
			this.buttonRunGame.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.buttonRunGame.Location = new System.Drawing.Point( 604, 422 );
			this.buttonRunGame.Name = "buttonRunGame";
			this.buttonRunGame.Size = new System.Drawing.Size( 117, 32 );
			this.buttonRunGame.TabIndex = 11;
			this.buttonRunGame.Text = "Run";
			this.buttonRunGame.UseVisualStyleBackColor = true;
			this.buttonRunGame.Click += new System.EventHandler( this.buttonRunGame_Click );
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point( 133, 518 );
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size( 450, 80 );
			this.label15.TabIndex = 5;
			this.label15.Text = resources.GetString( "label15.Text" );
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::Configurator.Properties.Resources.resource_editor_256x256;
			this.pictureBox1.Location = new System.Drawing.Point( 32, 518 );
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size( 80, 80 );
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1.TabIndex = 4;
			this.pictureBox1.TabStop = false;
			// 
			// buttonRunResourceEditor
			// 
			this.buttonRunResourceEditor.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.buttonRunResourceEditor.Location = new System.Drawing.Point( 604, 518 );
			this.buttonRunResourceEditor.Name = "buttonRunResourceEditor";
			this.buttonRunResourceEditor.Size = new System.Drawing.Size( 117, 32 );
			this.buttonRunResourceEditor.TabIndex = 12;
			this.buttonRunResourceEditor.Text = "Run";
			this.buttonRunResourceEditor.UseVisualStyleBackColor = true;
			this.buttonRunResourceEditor.Click += new System.EventHandler( this.buttonRunResourceEditor_Click );
			// 
			// label14
			// 
			this.label14.Location = new System.Drawing.Point( 133, 613 );
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size( 450, 80 );
			this.label14.TabIndex = 2;
			this.label14.Text = "Map Editor is a tool for creation worlds of your project. The tool is a complex e" +
				 "ditor to manage objects on the map.";
			// 
			// pictureBox1MapEditor
			// 
			this.pictureBox1MapEditor.Image = global::Configurator.Properties.Resources.map_editor_256x256;
			this.pictureBox1MapEditor.Location = new System.Drawing.Point( 32, 613 );
			this.pictureBox1MapEditor.Name = "pictureBox1MapEditor";
			this.pictureBox1MapEditor.Size = new System.Drawing.Size( 80, 80 );
			this.pictureBox1MapEditor.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1MapEditor.TabIndex = 1;
			this.pictureBox1MapEditor.TabStop = false;
			// 
			// buttonRunMapEditor
			// 
			this.buttonRunMapEditor.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.buttonRunMapEditor.Location = new System.Drawing.Point( 604, 613 );
			this.buttonRunMapEditor.Name = "buttonRunMapEditor";
			this.buttonRunMapEditor.Size = new System.Drawing.Size( 117, 32 );
			this.buttonRunMapEditor.TabIndex = 13;
			this.buttonRunMapEditor.Text = "Run";
			this.buttonRunMapEditor.UseVisualStyleBackColor = true;
			this.buttonRunMapEditor.Click += new System.EventHandler( this.buttonRunMapEditor_Click );
			// 
			// comboBoxDepthBufferAccess
			// 
			this.comboBoxDepthBufferAccess.DropDownHeight = 212;
			this.comboBoxDepthBufferAccess.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDepthBufferAccess.FormattingEnabled = true;
			this.comboBoxDepthBufferAccess.IntegralHeight = false;
			this.comboBoxDepthBufferAccess.Location = new System.Drawing.Point( 344, 168 );
			this.comboBoxDepthBufferAccess.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxDepthBufferAccess.Name = "comboBoxDepthBufferAccess";
			this.comboBoxDepthBufferAccess.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxDepthBufferAccess.TabIndex = 9;
			this.comboBoxDepthBufferAccess.SelectedIndexChanged += new System.EventHandler( this.comboBoxDepthBufferAccess_SelectedIndexChanged );
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 340, 148 );
			this.label6.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 139, 17 );
			this.label6.TabIndex = 21;
			this.label6.Text = "Depth buffer access:";
			// 
			// comboBoxRenderTechnique
			// 
			this.comboBoxRenderTechnique.DropDownHeight = 212;
			this.comboBoxRenderTechnique.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderTechnique.FormattingEnabled = true;
			this.comboBoxRenderTechnique.IntegralHeight = false;
			this.comboBoxRenderTechnique.Location = new System.Drawing.Point( 344, 69 );
			this.comboBoxRenderTechnique.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxRenderTechnique.Name = "comboBoxRenderTechnique";
			this.comboBoxRenderTechnique.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxRenderTechnique.TabIndex = 7;
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point( 340, 50 );
			this.label19.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size( 125, 17 );
			this.label19.TabIndex = 21;
			this.label19.Text = "Render technique:";
			// 
			// comboBoxFiltering
			// 
			this.comboBoxFiltering.DropDownHeight = 212;
			this.comboBoxFiltering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFiltering.FormattingEnabled = true;
			this.comboBoxFiltering.IntegralHeight = false;
			this.comboBoxFiltering.Location = new System.Drawing.Point( 344, 119 );
			this.comboBoxFiltering.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxFiltering.Name = "comboBoxFiltering";
			this.comboBoxFiltering.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxFiltering.TabIndex = 8;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point( 340, 99 );
			this.label9.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 62, 17 );
			this.label9.TabIndex = 19;
			this.label9.Text = "Filtering:";
			// 
			// comboBoxAntialiasing
			// 
			this.comboBoxAntialiasing.DropDownHeight = 212;
			this.comboBoxAntialiasing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAntialiasing.FormattingEnabled = true;
			this.comboBoxAntialiasing.IntegralHeight = false;
			this.comboBoxAntialiasing.Location = new System.Drawing.Point( 343, 218 );
			this.comboBoxAntialiasing.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxAntialiasing.Name = "comboBoxAntialiasing";
			this.comboBoxAntialiasing.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxAntialiasing.TabIndex = 10;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point( 339, 199 );
			this.label8.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 152, 17 );
			this.label8.TabIndex = 17;
			this.label8.Text = "Full-scene antialiasing:";
			// 
			// tabPageAdvanced
			// 
			this.tabPageAdvanced.Controls.Add( this.checkBoxAllowShaders );
			this.tabPageAdvanced.Controls.Add( this.label26 );
			this.tabPageAdvanced.Controls.Add( this.pictureBox4 );
			this.tabPageAdvanced.Controls.Add( this.buttonRunShaderCacheCompiler );
			this.tabPageAdvanced.Controls.Add( this.label24 );
			this.tabPageAdvanced.Controls.Add( this.label25 );
			this.tabPageAdvanced.Controls.Add( this.pictureBox3 );
			this.tabPageAdvanced.Controls.Add( this.buttonRunDeploymentTool );
			this.tabPageAdvanced.Controls.Add( this.label23 );
			this.tabPageAdvanced.Controls.Add( this.label22 );
			this.tabPageAdvanced.Controls.Add( this.label13 );
			this.tabPageAdvanced.Controls.Add( this.comboBoxRenderingDevices );
			this.tabPageAdvanced.Controls.Add( this.label12 );
			this.tabPageAdvanced.Controls.Add( this.label1 );
			this.tabPageAdvanced.Controls.Add( this.comboBoxRenderSystems );
			this.tabPageAdvanced.Controls.Add( this.label18 );
			this.tabPageAdvanced.Controls.Add( this.comboBoxPhysicsSystems );
			this.tabPageAdvanced.Controls.Add( this.label3 );
			this.tabPageAdvanced.Controls.Add( this.comboBoxSoundSystems );
			this.tabPageAdvanced.Location = new System.Drawing.Point( 4, 29 );
			this.tabPageAdvanced.Name = "tabPageAdvanced";
			this.tabPageAdvanced.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageAdvanced.Size = new System.Drawing.Size( 770, 717 );
			this.tabPageAdvanced.TabIndex = 6;
			this.tabPageAdvanced.Text = "Advanced";
			this.tabPageAdvanced.UseVisualStyleBackColor = true;
			// 
			// checkBoxAllowShaders
			// 
			this.checkBoxAllowShaders.AutoSize = true;
			this.checkBoxAllowShaders.Location = new System.Drawing.Point( 32, 154 );
			this.checkBoxAllowShaders.Name = "checkBoxAllowShaders";
			this.checkBoxAllowShaders.Size = new System.Drawing.Size( 117, 21 );
			this.checkBoxAllowShaders.TabIndex = 2;
			this.checkBoxAllowShaders.Text = "Allow shaders";
			this.checkBoxAllowShaders.UseVisualStyleBackColor = true;
			// 
			// label26
			// 
			this.label26.Location = new System.Drawing.Point( 136, 479 );
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size( 453, 80 );
			this.label26.TabIndex = 33;
			this.label26.Text = resources.GetString( "label26.Text" );
			// 
			// pictureBox4
			// 
			this.pictureBox4.Image = global::Configurator.Properties.Resources.utilities_256x256;
			this.pictureBox4.Location = new System.Drawing.Point( 35, 479 );
			this.pictureBox4.Name = "pictureBox4";
			this.pictureBox4.Size = new System.Drawing.Size( 80, 80 );
			this.pictureBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox4.TabIndex = 32;
			this.pictureBox4.TabStop = false;
			// 
			// buttonRunShaderCacheCompiler
			// 
			this.buttonRunShaderCacheCompiler.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.buttonRunShaderCacheCompiler.Location = new System.Drawing.Point( 607, 479 );
			this.buttonRunShaderCacheCompiler.Name = "buttonRunShaderCacheCompiler";
			this.buttonRunShaderCacheCompiler.Size = new System.Drawing.Size( 117, 32 );
			this.buttonRunShaderCacheCompiler.TabIndex = 5;
			this.buttonRunShaderCacheCompiler.Text = "Run";
			this.buttonRunShaderCacheCompiler.UseVisualStyleBackColor = true;
			this.buttonRunShaderCacheCompiler.Click += new System.EventHandler( this.buttonRunShaderCacheCompiler_Click );
			// 
			// label24
			// 
			this.label24.AutoSize = true;
			this.label24.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label24.Location = new System.Drawing.Point( 30, 443 );
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size( 112, 24 );
			this.label24.TabIndex = 31;
			this.label24.Text = "Applications";
			// 
			// label25
			// 
			this.label25.Location = new System.Drawing.Point( 137, 573 );
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size( 452, 96 );
			this.label25.TabIndex = 29;
			this.label25.Text = "Deployment Tool is a tool to deploy the final version of your application to spec" +
				 "ified platforms. This utility is useful to automate the final product\'s creation" +
				 ".";
			// 
			// pictureBox3
			// 
			this.pictureBox3.Image = global::Configurator.Properties.Resources.deployment_tool_256x256;
			this.pictureBox3.Location = new System.Drawing.Point( 35, 573 );
			this.pictureBox3.Name = "pictureBox3";
			this.pictureBox3.Size = new System.Drawing.Size( 80, 80 );
			this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox3.TabIndex = 28;
			this.pictureBox3.TabStop = false;
			// 
			// buttonRunDeploymentTool
			// 
			this.buttonRunDeploymentTool.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.buttonRunDeploymentTool.Location = new System.Drawing.Point( 607, 573 );
			this.buttonRunDeploymentTool.Name = "buttonRunDeploymentTool";
			this.buttonRunDeploymentTool.Size = new System.Drawing.Size( 117, 32 );
			this.buttonRunDeploymentTool.TabIndex = 6;
			this.buttonRunDeploymentTool.Text = "Run";
			this.buttonRunDeploymentTool.UseVisualStyleBackColor = true;
			this.buttonRunDeploymentTool.Click += new System.EventHandler( this.buttonRunDeploymentTool_Click );
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label23.Location = new System.Drawing.Point( 30, 312 );
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size( 60, 24 );
			this.label23.TabIndex = 27;
			this.label23.Text = "Audio";
			// 
			// label22
			// 
			this.label22.AutoSize = true;
			this.label22.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label22.Location = new System.Drawing.Point( 29, 213 );
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size( 74, 24 );
			this.label22.TabIndex = 26;
			this.label22.Text = "Physics";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Font = new System.Drawing.Font( "Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.label13.Location = new System.Drawing.Point( 28, 15 );
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size( 99, 24 );
			this.label13.TabIndex = 25;
			this.label13.Text = "Rendering";
			// 
			// comboBoxRenderingDevices
			// 
			this.comboBoxRenderingDevices.DropDownHeight = 212;
			this.comboBoxRenderingDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderingDevices.FormattingEnabled = true;
			this.comboBoxRenderingDevices.IntegralHeight = false;
			this.comboBoxRenderingDevices.Location = new System.Drawing.Point( 33, 116 );
			this.comboBoxRenderingDevices.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxRenderingDevices.Name = "comboBoxRenderingDevices";
			this.comboBoxRenderingDevices.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxRenderingDevices.TabIndex = 1;
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point( 29, 96 );
			this.label12.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size( 123, 17 );
			this.label12.TabIndex = 24;
			this.label12.Text = "Rendering device:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 29, 47 );
			this.label1.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 107, 17 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Render system:";
			// 
			// comboBoxRenderSystems
			// 
			this.comboBoxRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderSystems.FormattingEnabled = true;
			this.comboBoxRenderSystems.Location = new System.Drawing.Point( 33, 68 );
			this.comboBoxRenderSystems.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxRenderSystems.Name = "comboBoxRenderSystems";
			this.comboBoxRenderSystems.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxRenderSystems.TabIndex = 0;
			this.comboBoxRenderSystems.SelectedIndexChanged += new System.EventHandler( this.comboBoxRenderSystems_SelectedIndexChanged );
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point( 30, 244 );
			this.label18.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size( 108, 17 );
			this.label18.TabIndex = 9;
			this.label18.Text = "Physics system:";
			// 
			// comboBoxPhysicsSystems
			// 
			this.comboBoxPhysicsSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPhysicsSystems.FormattingEnabled = true;
			this.comboBoxPhysicsSystems.Location = new System.Drawing.Point( 34, 264 );
			this.comboBoxPhysicsSystems.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxPhysicsSystems.Name = "comboBoxPhysicsSystems";
			this.comboBoxPhysicsSystems.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxPhysicsSystems.TabIndex = 3;
			this.comboBoxPhysicsSystems.SelectedIndexChanged += new System.EventHandler( this.comboBoxPhysicsSystems_SelectedIndexChanged );
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 31, 343 );
			this.label3.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 101, 17 );
			this.label3.TabIndex = 7;
			this.label3.Text = "Sound system:";
			// 
			// comboBoxSoundSystems
			// 
			this.comboBoxSoundSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxSoundSystems.FormattingEnabled = true;
			this.comboBoxSoundSystems.Location = new System.Drawing.Point( 33, 360 );
			this.comboBoxSoundSystems.Margin = new System.Windows.Forms.Padding( 4 );
			this.comboBoxSoundSystems.Name = "comboBoxSoundSystems";
			this.comboBoxSoundSystems.Size = new System.Drawing.Size( 280, 24 );
			this.comboBoxSoundSystems.TabIndex = 4;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point( 551, 776 );
			this.buttonOK.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size( 117, 32 );
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler( this.buttonOK_Click );
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 677, 776 );
			this.buttonCancel.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 117, 32 );
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler( this.buttonCancel_Click );
			// 
			// label2
			// 
			this.label2.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 13, 791 );
			this.label2.Margin = new System.Windows.Forms.Padding( 4, 0, 4, 0 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 167, 17 );
			this.label2.TabIndex = 3;
			this.label2.Text = "2014 NeoAxis Group Ltd.";
			// 
			// MainForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 8F, 16F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 810, 823 );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.tabControl1 );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.Margin = new System.Windows.Forms.Padding( 4 );
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Configurator";
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MainForm_FormClosed );
			this.tabControl1.ResumeLayout( false );
			this.tabPageGeneral.ResumeLayout( false );
			this.tabPageGeneral.PerformLayout();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox2 ) ).EndInit();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox1 ) ).EndInit();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox1MapEditor ) ).EndInit();
			this.tabPageAdvanced.ResumeLayout( false );
			this.tabPageAdvanced.PerformLayout();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox4 ) ).EndInit();
			( (System.ComponentModel.ISupportInitialize)( this.pictureBox3 ) ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxRenderSystems;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxSoundSystems;
		private System.Windows.Forms.CheckBox checkBoxFullScreen;
		private System.Windows.Forms.ComboBox comboBoxVideoMode;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox comboBoxAntialiasing;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxVerticalSync;
		private System.Windows.Forms.ComboBox comboBoxFiltering;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.ComboBox comboBoxPhysicsSystems;
		private System.Windows.Forms.ComboBox comboBoxRenderTechnique;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.ComboBox comboBoxLanguages;
		private System.Windows.Forms.ComboBox comboBoxRenderingDevices;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.CheckBox checkBoxLocalizeToolset;
		private System.Windows.Forms.CheckBox checkBoxLocalizeEngine;
		private System.Windows.Forms.ComboBox comboBoxDepthBufferAccess;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TabPage tabPageGeneral;
		private System.Windows.Forms.TabPage tabPageAdvanced;
		private System.Windows.Forms.Button buttonRunMapEditor;
		private System.Windows.Forms.PictureBox pictureBox1MapEditor;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button buttonRunResourceEditor;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.Button buttonRunGame;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label23;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.Label label24;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.PictureBox pictureBox3;
		private System.Windows.Forms.Button buttonRunDeploymentTool;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.PictureBox pictureBox4;
		private System.Windows.Forms.Button buttonRunShaderCacheCompiler;
		private System.Windows.Forms.Button buttonRestart;
		private System.Windows.Forms.CheckBox checkBoxAllowShaders;
	}
}

