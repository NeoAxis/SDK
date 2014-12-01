namespace WinFormsMultiViewAppExample
{
	partial class EntityTypesForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( EntityTypesForm ) );
			this.treeView = new System.Windows.Forms.TreeView();
			this.imageList1 = new System.Windows.Forms.ImageList( this.components );
			this.SuspendLayout();
			// 
			// treeView
			// 
			this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView.HideSelection = false;
			this.treeView.ImageIndex = 0;
			this.treeView.ImageList = this.imageList1;
			this.treeView.Location = new System.Drawing.Point( 0, 0 );
			this.treeView.Name = "treeView";
			this.treeView.SelectedImageIndex = 0;
			this.treeView.Size = new System.Drawing.Size( 292, 270 );
			this.treeView.TabIndex = 1;
			this.treeView.Text = "s";
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler( this.treeView_AfterSelect );
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ( (System.Windows.Forms.ImageListStreamer)( resources.GetObject( "imageList1.ImageStream" ) ) );
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName( 0, "Folder_16.png" );
			this.imageList1.Images.SetKeyName( 1, "EntityType_16.png" );
			// 
			// EntityTypesForm
			// 
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size( 292, 270 );
			this.Controls.Add( this.treeView );
			this.DockAreas = ( (WeifenLuo.WinFormsUI.Docking.DockAreas)( ( ( ( ( WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom ) ) );
			this.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.471698F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.HideOnClose = true;
			this.Name = "EntityTypesForm";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockRight;
			this.TabText = "Entity Types";
			this.Text = "Entity Types";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.ImageList imageList1;


	}
}
