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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.tabControl1 = new EditorBase.Theme.EditorTabControl();
			this.tabPageGeneral = new EditorBase.Theme.EditorTabPage();
			this.buttonRestart = new EditorBase.Theme.EditorButton();
			this.label21 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.checkBoxLocalizeToolset = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxLocalizeEngine = new EditorBase.Theme.EditorCheckBox();
			this.label10 = new System.Windows.Forms.Label();
			this.comboBoxLanguages = new EditorBase.Theme.EditorComboBox();
			this.checkBoxVerticalSync = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxFullScreen = new EditorBase.Theme.EditorCheckBox();
			this.comboBoxVideoMode = new EditorBase.Theme.EditorComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.buttonRunGame = new EditorBase.Theme.EditorButton();
			this.label15 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.buttonRunResourceEditor = new EditorBase.Theme.EditorButton();
			this.label14 = new System.Windows.Forms.Label();
			this.pictureBox1MapEditor = new System.Windows.Forms.PictureBox();
			this.buttonRunMapEditor = new EditorBase.Theme.EditorButton();
			this.tabPageAdvanced = new EditorBase.Theme.EditorTabPage();
			this.label11 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.comboBoxDepthBufferAccess = new EditorBase.Theme.EditorComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.comboBoxRenderTechnique = new EditorBase.Theme.EditorComboBox();
			this.label19 = new System.Windows.Forms.Label();
			this.comboBoxFiltering = new EditorBase.Theme.EditorComboBox();
			this.label9 = new System.Windows.Forms.Label();
			this.comboBoxAntialiasing = new EditorBase.Theme.EditorComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.checkBoxAllowShaders = new EditorBase.Theme.EditorCheckBox();
			this.label26 = new System.Windows.Forms.Label();
			this.pictureBox4 = new System.Windows.Forms.PictureBox();
			this.buttonRunShaderCacheCompiler = new EditorBase.Theme.EditorButton();
			this.label24 = new System.Windows.Forms.Label();
			this.label25 = new System.Windows.Forms.Label();
			this.pictureBox3 = new System.Windows.Forms.PictureBox();
			this.buttonRunDeploymentTool = new EditorBase.Theme.EditorButton();
			this.label23 = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.comboBoxTextureSkipMipLevels = new EditorBase.Theme.EditorComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.comboBoxRenderingDevices = new EditorBase.Theme.EditorComboBox();
			this.label12 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxRenderSystems = new EditorBase.Theme.EditorComboBox();
			this.label18 = new System.Windows.Forms.Label();
			this.comboBoxPhysicsSystems = new EditorBase.Theme.EditorComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxSoundSystems = new EditorBase.Theme.EditorComboBox();
			this.buttonOK = new EditorBase.Theme.EditorButton();
			this.buttonCancel = new EditorBase.Theme.EditorButton();
			this.label2 = new System.Windows.Forms.Label();
			this.tabControl1.SuspendLayout();
			this.tabPageGeneral.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1MapEditor)).BeginInit();
			this.tabPageAdvanced.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabPageGeneral);
			this.tabControl1.Controls.Add(this.tabPageAdvanced);
			this.tabControl1.ItemSize = new System.Drawing.Size(64, 27);
			this.tabControl1.Location = new System.Drawing.Point(16, 15);
			this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.Padding = new System.Drawing.Point(8, 5);
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(778, 723);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.UseSelectable = true;
			// 
			// tabPageGeneral
			// 
			this.tabPageGeneral.BackColor = System.Drawing.SystemColors.Control;
			this.tabPageGeneral.Controls.Add(this.buttonRestart);
			this.tabPageGeneral.Controls.Add(this.label21);
			this.tabPageGeneral.Controls.Add(this.label17);
			this.tabPageGeneral.Controls.Add(this.label7);
			this.tabPageGeneral.Controls.Add(this.checkBoxLocalizeToolset);
			this.tabPageGeneral.Controls.Add(this.checkBoxLocalizeEngine);
			this.tabPageGeneral.Controls.Add(this.label10);
			this.tabPageGeneral.Controls.Add(this.comboBoxLanguages);
			this.tabPageGeneral.Controls.Add(this.checkBoxVerticalSync);
			this.tabPageGeneral.Controls.Add(this.checkBoxFullScreen);
			this.tabPageGeneral.Controls.Add(this.comboBoxVideoMode);
			this.tabPageGeneral.Controls.Add(this.label4);
			this.tabPageGeneral.Controls.Add(this.label16);
			this.tabPageGeneral.Controls.Add(this.pictureBox2);
			this.tabPageGeneral.Controls.Add(this.buttonRunGame);
			this.tabPageGeneral.Controls.Add(this.label15);
			this.tabPageGeneral.Controls.Add(this.pictureBox1);
			this.tabPageGeneral.Controls.Add(this.buttonRunResourceEditor);
			this.tabPageGeneral.Controls.Add(this.label14);
			this.tabPageGeneral.Controls.Add(this.pictureBox1MapEditor);
			this.tabPageGeneral.Controls.Add(this.buttonRunMapEditor);
			this.tabPageGeneral.Location = new System.Drawing.Point(4, 31);
			this.tabPageGeneral.Name = "tabPageGeneral";
			this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageGeneral.Size = new System.Drawing.Size(770, 688);
			this.tabPageGeneral.TabIndex = 5;
			this.tabPageGeneral.Text = "General";
			// 
			// buttonRestart
			// 
			this.buttonRestart.Enabled = false;
			this.buttonRestart.Location = new System.Drawing.Point(37, 152);
			this.buttonRestart.Name = "buttonRestart";
			this.buttonRestart.Size = new System.Drawing.Size(117, 32);
			this.buttonRestart.TabIndex = 3;
			this.buttonRestart.Text = "Restart";
			this.buttonRestart.UseVisualStyleBackColor = true;
			this.buttonRestart.Click += new System.EventHandler(this.buttonRestart_Click);
			// 
			// label21
			// 
			this.label21.AutoSize = true;
			this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label21.Location = new System.Drawing.Point(33, 248);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(112, 24);
			this.label21.TabIndex = 22;
			this.label21.Text = "Applications";
			this.label21.Click += new System.EventHandler(this.label21_Click);
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label17.Location = new System.Drawing.Point(387, 19);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(71, 24);
			this.label17.TabIndex = 16;
			this.label17.Text = "Screen";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label7.Location = new System.Drawing.Point(32, 20);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(108, 24);
			this.label7.TabIndex = 15;
			this.label7.Text = "Localization";
			// 
			// checkBoxLocalizeToolset
			// 
			this.checkBoxLocalizeToolset.AutoSize = true;
			this.checkBoxLocalizeToolset.Location = new System.Drawing.Point(37, 128);
			this.checkBoxLocalizeToolset.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxLocalizeToolset.Name = "checkBoxLocalizeToolset";
			this.checkBoxLocalizeToolset.Size = new System.Drawing.Size(116, 21);
			this.checkBoxLocalizeToolset.TabIndex = 2;
			this.checkBoxLocalizeToolset.Text = "Localize tools";
			this.checkBoxLocalizeToolset.UseVisualStyleBackColor = true;
			this.checkBoxLocalizeToolset.CheckedChanged += new System.EventHandler(this.checkBoxLocalizeToolset_CheckedChanged);
			// 
			// checkBoxLocalizeEngine
			// 
			this.checkBoxLocalizeEngine.AutoSize = true;
			this.checkBoxLocalizeEngine.Location = new System.Drawing.Point(37, 103);
			this.checkBoxLocalizeEngine.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxLocalizeEngine.Name = "checkBoxLocalizeEngine";
			this.checkBoxLocalizeEngine.Size = new System.Drawing.Size(121, 21);
			this.checkBoxLocalizeEngine.TabIndex = 1;
			this.checkBoxLocalizeEngine.Text = "Localize game";
			this.checkBoxLocalizeEngine.UseVisualStyleBackColor = true;
			this.checkBoxLocalizeEngine.CheckedChanged += new System.EventHandler(this.checkBoxLocalizeEngine_CheckedChanged);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(33, 51);
			this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(76, 17);
			this.label10.TabIndex = 8;
			this.label10.Text = "Language:";
			// 
			// comboBoxLanguages
			// 
			this.comboBoxLanguages.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxLanguages.FormattingEnabled = true;
			this.comboBoxLanguages.Location = new System.Drawing.Point(37, 71);
			this.comboBoxLanguages.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxLanguages.Name = "comboBoxLanguages";
			this.comboBoxLanguages.Size = new System.Drawing.Size(280, 23);
			this.comboBoxLanguages.TabIndex = 0;
			this.comboBoxLanguages.SelectedIndexChanged += new System.EventHandler(this.comboBoxLanguages_SelectedIndexChanged);
			// 
			// checkBoxVerticalSync
			// 
			this.checkBoxVerticalSync.AutoSize = true;
			this.checkBoxVerticalSync.Location = new System.Drawing.Point(392, 129);
			this.checkBoxVerticalSync.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxVerticalSync.Name = "checkBoxVerticalSync";
			this.checkBoxVerticalSync.Size = new System.Drawing.Size(110, 21);
			this.checkBoxVerticalSync.TabIndex = 6;
			this.checkBoxVerticalSync.Text = "Vertical sync";
			this.checkBoxVerticalSync.UseVisualStyleBackColor = true;
			// 
			// checkBoxFullScreen
			// 
			this.checkBoxFullScreen.AutoSize = true;
			this.checkBoxFullScreen.Location = new System.Drawing.Point(392, 104);
			this.checkBoxFullScreen.Margin = new System.Windows.Forms.Padding(4);
			this.checkBoxFullScreen.Name = "checkBoxFullScreen";
			this.checkBoxFullScreen.Size = new System.Drawing.Size(99, 21);
			this.checkBoxFullScreen.TabIndex = 5;
			this.checkBoxFullScreen.Text = "Full screen";
			this.checkBoxFullScreen.UseVisualStyleBackColor = true;
			// 
			// comboBoxVideoMode
			// 
			this.comboBoxVideoMode.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxVideoMode.DropDownHeight = 212;
			this.comboBoxVideoMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVideoMode.FormattingEnabled = true;
			this.comboBoxVideoMode.IntegralHeight = false;
			this.comboBoxVideoMode.Location = new System.Drawing.Point(392, 71);
			this.comboBoxVideoMode.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxVideoMode.Name = "comboBoxVideoMode";
			this.comboBoxVideoMode.Size = new System.Drawing.Size(280, 23);
			this.comboBoxVideoMode.TabIndex = 4;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(388, 52);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(87, 17);
			this.label4.TabIndex = 14;
			this.label4.Text = "Video mode:";
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(138, 296);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(453, 96);
			this.label16.TabIndex = 8;
			this.label16.Text = resources.GetString("label16.Text");
			// 
			// pictureBox2
			// 
			this.pictureBox2.Image = global::Configurator.Properties.Resources.game_256x256;
			this.pictureBox2.Location = new System.Drawing.Point(37, 296);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(80, 80);
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox2.TabIndex = 7;
			this.pictureBox2.TabStop = false;
			this.pictureBox2.Click += new System.EventHandler(this.buttonRunGame_Click);
			// 
			// buttonRunGame
			// 
			this.buttonRunGame.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.buttonRunGame.Location = new System.Drawing.Point(609, 296);
			this.buttonRunGame.Name = "buttonRunGame";
			this.buttonRunGame.Size = new System.Drawing.Size(117, 32);
			this.buttonRunGame.TabIndex = 7;
			this.buttonRunGame.Text = "Run";
			this.buttonRunGame.UseVisualStyleBackColor = true;
			this.buttonRunGame.Click += new System.EventHandler(this.buttonRunGame_Click);
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(138, 505);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(450, 80);
			this.label15.TabIndex = 5;
			this.label15.Text = resources.GetString("label15.Text");
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::Configurator.Properties.Resources.resource_editor_256x256;
			this.pictureBox1.Location = new System.Drawing.Point(37, 505);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(80, 80);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1.TabIndex = 4;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Click += new System.EventHandler(this.buttonRunResourceEditor_Click);
			// 
			// buttonRunResourceEditor
			// 
			this.buttonRunResourceEditor.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.buttonRunResourceEditor.Location = new System.Drawing.Point(609, 505);
			this.buttonRunResourceEditor.Name = "buttonRunResourceEditor";
			this.buttonRunResourceEditor.Size = new System.Drawing.Size(117, 32);
			this.buttonRunResourceEditor.TabIndex = 9;
			this.buttonRunResourceEditor.Text = "Run";
			this.buttonRunResourceEditor.UseVisualStyleBackColor = true;
			this.buttonRunResourceEditor.Click += new System.EventHandler(this.buttonRunResourceEditor_Click);
			// 
			// label14
			// 
			this.label14.Location = new System.Drawing.Point(138, 400);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(450, 80);
			this.label14.TabIndex = 2;
			this.label14.Text = "Map Editor is a tool for creation worlds of your project. The tool is a complex e" +
    "ditor to manage objects on the map.";
			// 
			// pictureBox1MapEditor
			// 
			this.pictureBox1MapEditor.Image = global::Configurator.Properties.Resources.map_editor_256x256;
			this.pictureBox1MapEditor.Location = new System.Drawing.Point(37, 400);
			this.pictureBox1MapEditor.Name = "pictureBox1MapEditor";
			this.pictureBox1MapEditor.Size = new System.Drawing.Size(80, 80);
			this.pictureBox1MapEditor.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1MapEditor.TabIndex = 1;
			this.pictureBox1MapEditor.TabStop = false;
			this.pictureBox1MapEditor.Click += new System.EventHandler(this.buttonRunMapEditor_Click);
			// 
			// buttonRunMapEditor
			// 
			this.buttonRunMapEditor.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.buttonRunMapEditor.Location = new System.Drawing.Point(609, 400);
			this.buttonRunMapEditor.Name = "buttonRunMapEditor";
			this.buttonRunMapEditor.Size = new System.Drawing.Size(117, 32);
			this.buttonRunMapEditor.TabIndex = 8;
			this.buttonRunMapEditor.Text = "Run";
			this.buttonRunMapEditor.UseVisualStyleBackColor = true;
			this.buttonRunMapEditor.Click += new System.EventHandler(this.buttonRunMapEditor_Click);
			// 
			// tabPageAdvanced
			// 
			this.tabPageAdvanced.BackColor = System.Drawing.SystemColors.Control;
			this.tabPageAdvanced.Controls.Add(this.label11);
			this.tabPageAdvanced.Controls.Add(this.label20);
			this.tabPageAdvanced.Controls.Add(this.comboBoxDepthBufferAccess);
			this.tabPageAdvanced.Controls.Add(this.label6);
			this.tabPageAdvanced.Controls.Add(this.comboBoxRenderTechnique);
			this.tabPageAdvanced.Controls.Add(this.label19);
			this.tabPageAdvanced.Controls.Add(this.comboBoxFiltering);
			this.tabPageAdvanced.Controls.Add(this.label9);
			this.tabPageAdvanced.Controls.Add(this.comboBoxAntialiasing);
			this.tabPageAdvanced.Controls.Add(this.label8);
			this.tabPageAdvanced.Controls.Add(this.checkBoxAllowShaders);
			this.tabPageAdvanced.Controls.Add(this.label26);
			this.tabPageAdvanced.Controls.Add(this.pictureBox4);
			this.tabPageAdvanced.Controls.Add(this.buttonRunShaderCacheCompiler);
			this.tabPageAdvanced.Controls.Add(this.label24);
			this.tabPageAdvanced.Controls.Add(this.label25);
			this.tabPageAdvanced.Controls.Add(this.pictureBox3);
			this.tabPageAdvanced.Controls.Add(this.buttonRunDeploymentTool);
			this.tabPageAdvanced.Controls.Add(this.label23);
			this.tabPageAdvanced.Controls.Add(this.label22);
			this.tabPageAdvanced.Controls.Add(this.label13);
			this.tabPageAdvanced.Controls.Add(this.comboBoxTextureSkipMipLevels);
			this.tabPageAdvanced.Controls.Add(this.label5);
			this.tabPageAdvanced.Controls.Add(this.comboBoxRenderingDevices);
			this.tabPageAdvanced.Controls.Add(this.label12);
			this.tabPageAdvanced.Controls.Add(this.label1);
			this.tabPageAdvanced.Controls.Add(this.comboBoxRenderSystems);
			this.tabPageAdvanced.Controls.Add(this.label18);
			this.tabPageAdvanced.Controls.Add(this.comboBoxPhysicsSystems);
			this.tabPageAdvanced.Controls.Add(this.label3);
			this.tabPageAdvanced.Controls.Add(this.comboBoxSoundSystems);
			this.tabPageAdvanced.Location = new System.Drawing.Point(4, 31);
			this.tabPageAdvanced.Name = "tabPageAdvanced";
			this.tabPageAdvanced.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageAdvanced.Size = new System.Drawing.Size(770, 688);
			this.tabPageAdvanced.TabIndex = 6;
			this.tabPageAdvanced.Text = "Advanced";
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(357, 256);
			this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(392, 206);
			this.label11.TabIndex = 23;
			this.label11.Text = resources.GetString("label11.Text");
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label20.Location = new System.Drawing.Point(355, 20);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(99, 24);
			this.label20.TabIndex = 17;
			this.label20.Text = "Rendering";
			// 
			// comboBoxDepthBufferAccess
			// 
			this.comboBoxDepthBufferAccess.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxDepthBufferAccess.DropDownHeight = 212;
			this.comboBoxDepthBufferAccess.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDepthBufferAccess.FormattingEnabled = true;
			this.comboBoxDepthBufferAccess.IntegralHeight = false;
			this.comboBoxDepthBufferAccess.Location = new System.Drawing.Point(359, 172);
			this.comboBoxDepthBufferAccess.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxDepthBufferAccess.Name = "comboBoxDepthBufferAccess";
			this.comboBoxDepthBufferAccess.Size = new System.Drawing.Size(280, 23);
			this.comboBoxDepthBufferAccess.TabIndex = 8;
			this.comboBoxDepthBufferAccess.SelectedIndexChanged += new System.EventHandler(this.comboBoxDepthBufferAccess_SelectedIndexChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(355, 152);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(139, 17);
			this.label6.TabIndex = 21;
			this.label6.Text = "Depth buffer access:";
			// 
			// comboBoxRenderTechnique
			// 
			this.comboBoxRenderTechnique.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxRenderTechnique.DropDownHeight = 212;
			this.comboBoxRenderTechnique.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderTechnique.FormattingEnabled = true;
			this.comboBoxRenderTechnique.IntegralHeight = false;
			this.comboBoxRenderTechnique.Location = new System.Drawing.Point(359, 73);
			this.comboBoxRenderTechnique.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxRenderTechnique.Name = "comboBoxRenderTechnique";
			this.comboBoxRenderTechnique.Size = new System.Drawing.Size(280, 23);
			this.comboBoxRenderTechnique.TabIndex = 6;
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point(355, 54);
			this.label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(125, 17);
			this.label19.TabIndex = 21;
			this.label19.Text = "Render technique:";
			// 
			// comboBoxFiltering
			// 
			this.comboBoxFiltering.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxFiltering.DropDownHeight = 212;
			this.comboBoxFiltering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFiltering.FormattingEnabled = true;
			this.comboBoxFiltering.IntegralHeight = false;
			this.comboBoxFiltering.Location = new System.Drawing.Point(359, 123);
			this.comboBoxFiltering.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxFiltering.Name = "comboBoxFiltering";
			this.comboBoxFiltering.Size = new System.Drawing.Size(280, 23);
			this.comboBoxFiltering.TabIndex = 7;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(355, 103);
			this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(62, 17);
			this.label9.TabIndex = 19;
			this.label9.Text = "Filtering:";
			// 
			// comboBoxAntialiasing
			// 
			this.comboBoxAntialiasing.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxAntialiasing.DropDownHeight = 212;
			this.comboBoxAntialiasing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAntialiasing.FormattingEnabled = true;
			this.comboBoxAntialiasing.IntegralHeight = false;
			this.comboBoxAntialiasing.Location = new System.Drawing.Point(358, 222);
			this.comboBoxAntialiasing.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxAntialiasing.Name = "comboBoxAntialiasing";
			this.comboBoxAntialiasing.Size = new System.Drawing.Size(280, 23);
			this.comboBoxAntialiasing.TabIndex = 9;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(354, 203);
			this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(152, 17);
			this.label8.TabIndex = 17;
			this.label8.Text = "Full-scene antialiasing:";
			// 
			// checkBoxAllowShaders
			// 
			this.checkBoxAllowShaders.AutoSize = true;
			this.checkBoxAllowShaders.Location = new System.Drawing.Point(37, 201);
			this.checkBoxAllowShaders.Name = "checkBoxAllowShaders";
			this.checkBoxAllowShaders.Size = new System.Drawing.Size(117, 21);
			this.checkBoxAllowShaders.TabIndex = 3;
			this.checkBoxAllowShaders.Text = "Allow shaders";
			this.checkBoxAllowShaders.UseVisualStyleBackColor = true;
			// 
			// label26
			// 
			this.label26.Location = new System.Drawing.Point(140, 474);
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size(453, 80);
			this.label26.TabIndex = 33;
			this.label26.Text = resources.GetString("label26.Text");
			// 
			// pictureBox4
			// 
			this.pictureBox4.Image = global::Configurator.Properties.Resources.utilities_256x256;
			this.pictureBox4.Location = new System.Drawing.Point(39, 474);
			this.pictureBox4.Name = "pictureBox4";
			this.pictureBox4.Size = new System.Drawing.Size(80, 80);
			this.pictureBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox4.TabIndex = 32;
			this.pictureBox4.TabStop = false;
			this.pictureBox4.Click += new System.EventHandler(this.buttonRunShaderCacheCompiler_Click);
			// 
			// buttonRunShaderCacheCompiler
			// 
			this.buttonRunShaderCacheCompiler.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.buttonRunShaderCacheCompiler.Location = new System.Drawing.Point(611, 474);
			this.buttonRunShaderCacheCompiler.Name = "buttonRunShaderCacheCompiler";
			this.buttonRunShaderCacheCompiler.Size = new System.Drawing.Size(117, 32);
			this.buttonRunShaderCacheCompiler.TabIndex = 10;
			this.buttonRunShaderCacheCompiler.Text = "Run";
			this.buttonRunShaderCacheCompiler.UseVisualStyleBackColor = true;
			this.buttonRunShaderCacheCompiler.Click += new System.EventHandler(this.buttonRunShaderCacheCompiler_Click);
			// 
			// label24
			// 
			this.label24.AutoSize = true;
			this.label24.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label24.Location = new System.Drawing.Point(34, 430);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(112, 24);
			this.label24.TabIndex = 31;
			this.label24.Text = "Applications";
			// 
			// label25
			// 
			this.label25.Location = new System.Drawing.Point(141, 575);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(452, 96);
			this.label25.TabIndex = 29;
			this.label25.Text = "Deployment Tool is a tool to deploy the final version of your application to spec" +
    "ified platforms. This utility is useful to automate the final product\'s creation" +
    ".";
			// 
			// pictureBox3
			// 
			this.pictureBox3.Image = global::Configurator.Properties.Resources.deployment_tool_256x256;
			this.pictureBox3.Location = new System.Drawing.Point(39, 575);
			this.pictureBox3.Name = "pictureBox3";
			this.pictureBox3.Size = new System.Drawing.Size(80, 80);
			this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox3.TabIndex = 28;
			this.pictureBox3.TabStop = false;
			this.pictureBox3.Click += new System.EventHandler(this.buttonRunDeploymentTool_Click);
			// 
			// buttonRunDeploymentTool
			// 
			this.buttonRunDeploymentTool.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.buttonRunDeploymentTool.Location = new System.Drawing.Point(611, 575);
			this.buttonRunDeploymentTool.Name = "buttonRunDeploymentTool";
			this.buttonRunDeploymentTool.Size = new System.Drawing.Size(117, 32);
			this.buttonRunDeploymentTool.TabIndex = 11;
			this.buttonRunDeploymentTool.Text = "Run";
			this.buttonRunDeploymentTool.UseVisualStyleBackColor = true;
			this.buttonRunDeploymentTool.Click += new System.EventHandler(this.buttonRunDeploymentTool_Click);
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label23.Location = new System.Drawing.Point(33, 326);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(60, 24);
			this.label23.TabIndex = 27;
			this.label23.Text = "Audio";
			// 
			// label22
			// 
			this.label22.AutoSize = true;
			this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label22.Location = new System.Drawing.Point(31, 241);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(74, 24);
			this.label22.TabIndex = 26;
			this.label22.Text = "Physics";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label13.Location = new System.Drawing.Point(32, 19);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(99, 24);
			this.label13.TabIndex = 25;
			this.label13.Text = "Rendering";
			// 
			// comboBoxTextureSkipMipLevels
			// 
			this.comboBoxTextureSkipMipLevels.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxTextureSkipMipLevels.DropDownHeight = 212;
			this.comboBoxTextureSkipMipLevels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTextureSkipMipLevels.FormattingEnabled = true;
			this.comboBoxTextureSkipMipLevels.IntegralHeight = false;
			this.comboBoxTextureSkipMipLevels.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7"});
			this.comboBoxTextureSkipMipLevels.Location = new System.Drawing.Point(37, 169);
			this.comboBoxTextureSkipMipLevels.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxTextureSkipMipLevels.Name = "comboBoxTextureSkipMipLevels";
			this.comboBoxTextureSkipMipLevels.Size = new System.Drawing.Size(280, 23);
			this.comboBoxTextureSkipMipLevels.TabIndex = 2;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(33, 149);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(152, 17);
			this.label5.TabIndex = 24;
			this.label5.Text = "Skip texture mip levels:";
			// 
			// comboBoxRenderingDevices
			// 
			this.comboBoxRenderingDevices.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxRenderingDevices.DropDownHeight = 212;
			this.comboBoxRenderingDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderingDevices.FormattingEnabled = true;
			this.comboBoxRenderingDevices.IntegralHeight = false;
			this.comboBoxRenderingDevices.Location = new System.Drawing.Point(37, 120);
			this.comboBoxRenderingDevices.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxRenderingDevices.Name = "comboBoxRenderingDevices";
			this.comboBoxRenderingDevices.Size = new System.Drawing.Size(280, 23);
			this.comboBoxRenderingDevices.TabIndex = 1;
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(33, 100);
			this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(123, 17);
			this.label12.TabIndex = 24;
			this.label12.Text = "Rendering device:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(33, 51);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(107, 17);
			this.label1.TabIndex = 3;
			this.label1.Text = "Render system:";
			// 
			// comboBoxRenderSystems
			// 
			this.comboBoxRenderSystems.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderSystems.FormattingEnabled = true;
			this.comboBoxRenderSystems.Location = new System.Drawing.Point(37, 72);
			this.comboBoxRenderSystems.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxRenderSystems.Name = "comboBoxRenderSystems";
			this.comboBoxRenderSystems.Size = new System.Drawing.Size(280, 23);
			this.comboBoxRenderSystems.TabIndex = 0;
			this.comboBoxRenderSystems.SelectedIndexChanged += new System.EventHandler(this.comboBoxRenderSystems_SelectedIndexChanged);
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(32, 272);
			this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(108, 17);
			this.label18.TabIndex = 9;
			this.label18.Text = "Physics system:";
			// 
			// comboBoxPhysicsSystems
			// 
			this.comboBoxPhysicsSystems.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxPhysicsSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPhysicsSystems.FormattingEnabled = true;
			this.comboBoxPhysicsSystems.Location = new System.Drawing.Point(36, 292);
			this.comboBoxPhysicsSystems.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxPhysicsSystems.Name = "comboBoxPhysicsSystems";
			this.comboBoxPhysicsSystems.Size = new System.Drawing.Size(280, 23);
			this.comboBoxPhysicsSystems.TabIndex = 4;
			this.comboBoxPhysicsSystems.SelectedIndexChanged += new System.EventHandler(this.comboBoxPhysicsSystems_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(34, 354);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(101, 17);
			this.label3.TabIndex = 7;
			this.label3.Text = "Sound system:";
			// 
			// comboBoxSoundSystems
			// 
			this.comboBoxSoundSystems.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.comboBoxSoundSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxSoundSystems.FormattingEnabled = true;
			this.comboBoxSoundSystems.Location = new System.Drawing.Point(36, 374);
			this.comboBoxSoundSystems.Margin = new System.Windows.Forms.Padding(4);
			this.comboBoxSoundSystems.Name = "comboBoxSoundSystems";
			this.comboBoxSoundSystems.Size = new System.Drawing.Size(280, 23);
			this.comboBoxSoundSystems.TabIndex = 5;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(551, 746);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(4);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(117, 32);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(677, 746);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(4);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(117, 32);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 761);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(167, 17);
			this.label2.TabIndex = 3;
			this.label2.Text = "2016 NeoAxis Group Ltd.";
			// 
			// MainForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(810, 793);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.tabControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Configurator";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.tabControl1.ResumeLayout(false);
			this.tabPageGeneral.ResumeLayout(false);
			this.tabPageGeneral.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1MapEditor)).EndInit();
			this.tabPageAdvanced.ResumeLayout(false);
			this.tabPageAdvanced.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private EditorBase.Theme.EditorTabControl tabControl1;
		private EditorBase.Theme.EditorButton buttonOK;
		private EditorBase.Theme.EditorButton buttonCancel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private EditorBase.Theme.EditorComboBox comboBoxRenderSystems;
		private System.Windows.Forms.Label label3;
		private EditorBase.Theme.EditorComboBox comboBoxSoundSystems;
		private EditorBase.Theme.EditorCheckBox checkBoxFullScreen;
		private EditorBase.Theme.EditorComboBox comboBoxVideoMode;
		private EditorBase.Theme.EditorComboBox comboBoxAntialiasing;
		private System.Windows.Forms.Label label8;
		private EditorBase.Theme.EditorCheckBox checkBoxVerticalSync;
		private EditorBase.Theme.EditorComboBox comboBoxFiltering;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label18;
		private EditorBase.Theme.EditorComboBox comboBoxPhysicsSystems;
		private EditorBase.Theme.EditorComboBox comboBoxRenderTechnique;
		private System.Windows.Forms.Label label19;
		private EditorBase.Theme.EditorComboBox comboBoxLanguages;
		private EditorBase.Theme.EditorComboBox comboBoxRenderingDevices;
		private System.Windows.Forms.Label label12;
		private EditorBase.Theme.EditorCheckBox checkBoxLocalizeToolset;
		private EditorBase.Theme.EditorCheckBox checkBoxLocalizeEngine;
		private EditorBase.Theme.EditorComboBox comboBoxDepthBufferAccess;
		private System.Windows.Forms.Label label6;
		private EditorBase.Theme.EditorTabPage tabPageGeneral;
		private EditorBase.Theme.EditorTabPage tabPageAdvanced;
		private EditorBase.Theme.EditorButton buttonRunMapEditor;
		private System.Windows.Forms.PictureBox pictureBox1MapEditor;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.PictureBox pictureBox1;
		private EditorBase.Theme.EditorButton buttonRunResourceEditor;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.PictureBox pictureBox2;
		private EditorBase.Theme.EditorButton buttonRunGame;
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
		private EditorBase.Theme.EditorButton buttonRunDeploymentTool;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.PictureBox pictureBox4;
		private EditorBase.Theme.EditorButton buttonRunShaderCacheCompiler;
		private EditorBase.Theme.EditorButton buttonRestart;
		private EditorBase.Theme.EditorCheckBox checkBoxAllowShaders;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label4;
		private EditorBase.Theme.EditorComboBox comboBoxTextureSkipMipLevels;
		private System.Windows.Forms.Label label5;
	}
}

