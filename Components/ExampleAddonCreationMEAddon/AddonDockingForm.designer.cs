namespace ExampleAddonCreationMEAddon
{
	partial class AddonDockingForm
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
			this.checkBoxOverrideSceneObjects = new EditorBase.Theme.EditorCheckBox();
			this.checkBoxInteractive2DGUI = new EditorBase.Theme.EditorCheckBox();
			this.renderTargetUserControl1 = new EditorBase.RenderTargetUserControl();
			this.SuspendLayout();
			// 
			// checkBoxOverrideSceneObjects
			// 
			this.checkBoxOverrideSceneObjects.AutoSize = true;
			this.checkBoxOverrideSceneObjects.Checked = true;
			this.checkBoxOverrideSceneObjects.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOverrideSceneObjects.Location = new System.Drawing.Point(12, 12);
			this.checkBoxOverrideSceneObjects.Name = "checkBoxOverrideSceneObjects";
			this.checkBoxOverrideSceneObjects.Size = new System.Drawing.Size(169, 20);
			this.checkBoxOverrideSceneObjects.TabIndex = 1;
			this.checkBoxOverrideSceneObjects.Text = "Override scene objects";
			this.checkBoxOverrideSceneObjects.UseVisualStyleBackColor = true;
			// 
			// checkBoxInteractive2DGUI
			// 
			this.checkBoxInteractive2DGUI.AutoSize = true;
			this.checkBoxInteractive2DGUI.Location = new System.Drawing.Point(12, 38);
			this.checkBoxInteractive2DGUI.Name = "checkBoxInteractive2DGUI";
			this.checkBoxInteractive2DGUI.Size = new System.Drawing.Size(137, 20);
			this.checkBoxInteractive2DGUI.TabIndex = 1;
			this.checkBoxInteractive2DGUI.Text = "Interactive 2D GUI";
			this.checkBoxInteractive2DGUI.UseVisualStyleBackColor = true;
			this.checkBoxInteractive2DGUI.CheckedChanged += new System.EventHandler(this.checkBoxInteractive2DGUI_CheckedChanged);
			// 
			// renderTargetUserControl1
			// 
			this.renderTargetUserControl1.AllowCreateRenderWindow = true;
			this.renderTargetUserControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.renderTargetUserControl1.BackColor = System.Drawing.Color.Black;
			this.renderTargetUserControl1.Location = new System.Drawing.Point(0, 65);
			this.renderTargetUserControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.renderTargetUserControl1.MouseRelativeMode = false;
			this.renderTargetUserControl1.Name = "renderTargetUserControl1";
			this.renderTargetUserControl1.Size = new System.Drawing.Size(546, 328);
			this.renderTargetUserControl1.TabIndex = 2;
			// 
			// AddonDockingForm
			// 
			this.ClientSize = new System.Drawing.Size(546, 393);
			this.Controls.Add(this.renderTargetUserControl1);
			this.Controls.Add(this.checkBoxInteractive2DGUI);
			this.Controls.Add(this.checkBoxOverrideSceneObjects);
			this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)(((((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft) 
            | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight) 
            | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop) 
            | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.471698F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.HideOnClose = true;
			this.Name = "AddonDockingForm";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockRight;
			this.TabText = "Add-on Window";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.AddonDockingForm_FormClosed);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private EditorBase.Theme.EditorCheckBox checkBoxOverrideSceneObjects;
		private EditorBase.Theme.EditorCheckBox checkBoxInteractive2DGUI;
		private EditorBase.RenderTargetUserControl renderTargetUserControl1;



	}
}
