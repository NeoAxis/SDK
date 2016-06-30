namespace ExampleAddonCreationMEAddon
{
	partial class AddonForm
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
			this.buttonOK = new EditorBase.Theme.EditorButton();
			this.checkBoxAddItemToMainMenu = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxAddButtonToToolbar = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxDrawTextOnScreen = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxAddItemToContextMenuOfWorkingArea = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxAddItemToContextMenuOfLayer = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxOverrideCameraSettings = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxAddPageToOptions = new EditorBase.Theme.EditorCheckBox();
			this.buttonCancel = new EditorBase.Theme.EditorButton();
			this.checkBoxAddDockingWindow = new EditorBase.Theme.EditorCheckBox();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point( 427, 13 );
			this.buttonOK.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size( 117, 32 );
			this.buttonOK.TabIndex = 8;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// checkBoxAddItemToMainMenu
			// 
			this.checkBoxAddItemToMainMenu.AutoSize = true;
			this.checkBoxAddItemToMainMenu.Location = new System.Drawing.Point( 12, 40 );
			this.checkBoxAddItemToMainMenu.Name = "checkBoxAddItemToMainMenu";
			this.checkBoxAddItemToMainMenu.Size = new System.Drawing.Size( 174, 21 );
			this.checkBoxAddItemToMainMenu.TabIndex = 1;
			this.checkBoxAddItemToMainMenu.Text = "Add item to main menu";
			this.checkBoxAddItemToMainMenu.UseVisualStyleBackColor = true;
			// 
			// checkBoxAddButtonToToolbar
			// 
			this.checkBoxAddButtonToToolbar.AutoSize = true;
			this.checkBoxAddButtonToToolbar.Location = new System.Drawing.Point( 12, 67 );
			this.checkBoxAddButtonToToolbar.Name = "checkBoxAddButtonToToolbar";
			this.checkBoxAddButtonToToolbar.Size = new System.Drawing.Size( 167, 21 );
			this.checkBoxAddButtonToToolbar.TabIndex = 2;
			this.checkBoxAddButtonToToolbar.Text = "Add button to tool bar";
			this.checkBoxAddButtonToToolbar.UseVisualStyleBackColor = true;
			// 
			// checkBoxDrawTextOnScreen
			// 
			this.checkBoxDrawTextOnScreen.AutoSize = true;
			this.checkBoxDrawTextOnScreen.Location = new System.Drawing.Point( 12, 94 );
			this.checkBoxDrawTextOnScreen.Name = "checkBoxDrawTextOnScreen";
			this.checkBoxDrawTextOnScreen.Size = new System.Drawing.Size( 179, 21 );
			this.checkBoxDrawTextOnScreen.TabIndex = 3;
			this.checkBoxDrawTextOnScreen.Text = "Draw text on the screen";
			this.checkBoxDrawTextOnScreen.UseVisualStyleBackColor = true;
			// 
			// checkBoxAddItemToContextMenuOfWorkingArea
			// 
			this.checkBoxAddItemToContextMenuOfWorkingArea.AutoSize = true;
			this.checkBoxAddItemToContextMenuOfWorkingArea.Location = new System.Drawing.Point( 12, 121 );
			this.checkBoxAddItemToContextMenuOfWorkingArea.Name = "checkBoxAddItemToContextMenuOfWorkingArea";
			this.checkBoxAddItemToContextMenuOfWorkingArea.Size = new System.Drawing.Size( 290, 21 );
			this.checkBoxAddItemToContextMenuOfWorkingArea.TabIndex = 4;
			this.checkBoxAddItemToContextMenuOfWorkingArea.Text = "Add item to context menu of working area";
			this.checkBoxAddItemToContextMenuOfWorkingArea.UseVisualStyleBackColor = true;
			// 
			// checkBoxAddItemToContextMenuOfLayer
			// 
			this.checkBoxAddItemToContextMenuOfLayer.AutoSize = true;
			this.checkBoxAddItemToContextMenuOfLayer.Location = new System.Drawing.Point( 12, 148 );
			this.checkBoxAddItemToContextMenuOfLayer.Name = "checkBoxAddItemToContextMenuOfLayer";
			this.checkBoxAddItemToContextMenuOfLayer.Size = new System.Drawing.Size( 240, 21 );
			this.checkBoxAddItemToContextMenuOfLayer.TabIndex = 5;
			this.checkBoxAddItemToContextMenuOfLayer.Text = "Add item to context menu of layer";
			this.checkBoxAddItemToContextMenuOfLayer.UseVisualStyleBackColor = true;
			// 
			// checkBoxOverrideCameraSettings
			// 
			this.checkBoxOverrideCameraSettings.AutoSize = true;
			this.checkBoxOverrideCameraSettings.Location = new System.Drawing.Point( 12, 175 );
			this.checkBoxOverrideCameraSettings.Name = "checkBoxOverrideCameraSettings";
			this.checkBoxOverrideCameraSettings.Size = new System.Drawing.Size( 189, 21 );
			this.checkBoxOverrideCameraSettings.TabIndex = 6;
			this.checkBoxOverrideCameraSettings.Text = "Override camera settings";
			this.checkBoxOverrideCameraSettings.UseVisualStyleBackColor = true;
			// 
			// checkBoxAddPageToOptions
			// 
			this.checkBoxAddPageToOptions.AutoSize = true;
			this.checkBoxAddPageToOptions.Location = new System.Drawing.Point( 12, 202 );
			this.checkBoxAddPageToOptions.Name = "checkBoxAddPageToOptions";
			this.checkBoxAddPageToOptions.Size = new System.Drawing.Size( 160, 21 );
			this.checkBoxAddPageToOptions.TabIndex = 7;
			this.checkBoxAddPageToOptions.Text = "Add page to Options";
			this.checkBoxAddPageToOptions.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 427, 53 );
			this.buttonCancel.Margin = new System.Windows.Forms.Padding( 4 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 117, 32 );
			this.buttonCancel.TabIndex = 9;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// checkBoxAddDockingWindow
			// 
			this.checkBoxAddDockingWindow.AutoSize = true;
			this.checkBoxAddDockingWindow.Location = new System.Drawing.Point( 12, 13 );
			this.checkBoxAddDockingWindow.Name = "checkBoxAddDockingWindow";
			this.checkBoxAddDockingWindow.Size = new System.Drawing.Size( 157, 21 );
			this.checkBoxAddDockingWindow.TabIndex = 0;
			this.checkBoxAddDockingWindow.Text = "Add docking window";
			this.checkBoxAddDockingWindow.UseVisualStyleBackColor = true;
			// 
			// AddonForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 8F, 16F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 557, 245 );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.checkBoxAddPageToOptions );
			this.Controls.Add( this.checkBoxOverrideCameraSettings );
			this.Controls.Add( this.checkBoxAddItemToContextMenuOfLayer );
			this.Controls.Add( this.checkBoxAddItemToContextMenuOfWorkingArea );
			this.Controls.Add( this.checkBoxDrawTextOnScreen );
			this.Controls.Add( this.checkBoxAddButtonToToolbar );
			this.Controls.Add( this.checkBoxAddDockingWindow );
			this.Controls.Add( this.checkBoxAddItemToMainMenu );
			this.Controls.Add( this.buttonOK );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding( 4 );
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddonForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Example of Add-on Creation";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.AddonForm_FormClosed );
			this.Load += new System.EventHandler( this.AddonForm_Load );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private EditorBase.Theme.EditorButton buttonOK;
		private EditorBase.Theme.EditorCheckBox checkBoxAddItemToMainMenu;
		private EditorBase.Theme.EditorCheckBox checkBoxAddButtonToToolbar;
		private EditorBase.Theme.EditorCheckBox checkBoxDrawTextOnScreen;
		private EditorBase.Theme.EditorCheckBox checkBoxAddItemToContextMenuOfWorkingArea;
		private EditorBase.Theme.EditorCheckBox checkBoxAddItemToContextMenuOfLayer;
		private EditorBase.Theme.EditorCheckBox checkBoxOverrideCameraSettings;
		private EditorBase.Theme.EditorCheckBox checkBoxAddPageToOptions;
		private EditorBase.Theme.EditorButton buttonCancel;
		private EditorBase.Theme.EditorCheckBox checkBoxAddDockingWindow;

	}
}