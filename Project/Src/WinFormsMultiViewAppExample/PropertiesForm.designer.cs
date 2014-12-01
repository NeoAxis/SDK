namespace WinFormsMultiViewAppExample
{
	partial class PropertiesForm
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
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.SuspendLayout();
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid1.Enabled = false;
			this.propertyGrid1.Location = new System.Drawing.Point( 0, 0 );
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.SelectedObject = this.propertyGrid1;
			this.propertyGrid1.Size = new System.Drawing.Size( 214, 328 );
			this.propertyGrid1.TabIndex = 1;
			// 
			// PropertiesForm
			// 
			this.ClientSize = new System.Drawing.Size( 214, 328 );
			this.Controls.Add( this.propertyGrid1 );
			this.DockAreas = ( (WeifenLuo.WinFormsUI.Docking.DockAreas)( ( ( ( ( WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop )
							| WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom ) ) );
			this.Font = new System.Drawing.Font( "Microsoft Sans Serif", 7.471698F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ( (byte)( 204 ) ) );
			this.HideOnClose = true;
			this.Name = "PropertiesForm";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockRight;
			this.TabText = "Properties";
			this.Text = "Properties";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.PropertyGrid propertyGrid1;


	}
}
