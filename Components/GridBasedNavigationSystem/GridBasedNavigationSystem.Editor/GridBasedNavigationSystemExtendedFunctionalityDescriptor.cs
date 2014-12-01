// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Engine.MapSystem.Editor;
using Engine.EntitySystem;
using Engine.Utils;

namespace Engine.Editor
{
	public class GridBasedNavigationSystemExtendedFunctionalityDescriptor : MapGeneralObjectExtendedFunctionalityDescriptor
	{
		GridBasedNavigationSystem system;

		//NumericUpDown numericUpDownMaxFieldsDistance;
		//NumericUpDown numericUpDownMaxFieldsToCheck;
		CheckBox checkBoxSmooth;
		CheckBox checkBoxVisualize;

		//

		public GridBasedNavigationSystemExtendedFunctionalityDescriptor( Control parentControl, object owner )
			: base( parentControl, owner )
		{
			system = (GridBasedNavigationSystem)owner;

			int width = parentControl.Size.Width - 16;

			Button button;
			//NumericUpDown numericUpDown;
			//Label label;
			CheckBox checkBox;

			int posY = 8;

			//Update button
			button = new Button();
			parentControl.Controls.Add( button );
			button.Location = new System.Drawing.Point( 8, posY );
			button.Size = new System.Drawing.Size( 120, 32 );
			button.Text = Translate( "Update" );
			button.UseVisualStyleBackColor = true;
			button.Click += delegate( object sender, EventArgs e )
			{
				system.UpdateMotionMap();
			};

			//Path test button
			button = new Button();
			parentControl.Controls.Add( button );
			button.Location = new System.Drawing.Point( 120 + 8 * 2, posY );
			button.Size = new System.Drawing.Size( 120, 32 );
			button.Text = Translate( "Test" );
			button.UseVisualStyleBackColor = true;
			button.Click += new EventHandler( buttonTestPathFind_Click );

			posY += 36;

			////numericUpDownMaxFieldsDistance

			//label = new Label();
			//label.Text = "Test: Max fields distance";
			//label.Location = new System.Drawing.Point( 8, posY ); //posY += 20;
			//label.AutoSize = true;
			//parentControl.Controls.Add( label );

			//numericUpDown = new NumericUpDown();
			//numericUpDownMaxFieldsDistance = numericUpDown;
			//parentControl.Controls.Add( numericUpDown );
			//numericUpDown.Location = new System.Drawing.Point( 8 + label.Size.Width + 8, posY ); posY += 24;
			//numericUpDown.Size = new System.Drawing.Size( 75, numericUpDown.Size.Height );
			//numericUpDown.TextAlign = HorizontalAlignment.Right;
			//numericUpDown.Minimum = 1;
			//numericUpDown.Maximum = 10000000;
			//numericUpDown.Value = 10000000;

			////numericUpDownMaxFieldsToCheck

			//label = new Label();
			//label.Text = "Test: Max fields to check";
			//label.Location = new System.Drawing.Point( 8, posY ); //posY += 20;
			//label.AutoSize = true;
			//parentControl.Controls.Add( label );

			//numericUpDown = new NumericUpDown();
			//numericUpDownMaxFieldsToCheck = numericUpDown;
			//parentControl.Controls.Add( numericUpDown );
			//numericUpDown.Location = new System.Drawing.Point( 8 + label.Size.Width + 8, posY ); posY += 24;
			//numericUpDown.Size = new System.Drawing.Size( 75, numericUpDown.Size.Height );
			//numericUpDown.TextAlign = HorizontalAlignment.Right;
			//numericUpDown.Minimum = 1;
			//numericUpDown.Maximum = 10000000;
			//numericUpDown.Value = 10000000;

			//checkBoxSmooth
			checkBox = new CheckBox();
			checkBoxSmooth = checkBox;
			parentControl.Controls.Add( checkBox );
			checkBox.Location = new System.Drawing.Point( 8, posY );
			checkBox.AutoSize = true;
			checkBox.Text = Translate( "Smooth the path in test mode" );
			checkBox.UseVisualStyleBackColor = true;
			checkBox.Checked = true;

			posY += 26;

			//checkBoxVisualize
			checkBox = new CheckBox();
			checkBoxVisualize = checkBox;
			parentControl.Controls.Add( checkBox );
			checkBox.Location = new System.Drawing.Point( 8, posY );
			checkBox.AutoSize = true;
			checkBox.Text = Translate( "Visualize path finding in test mode" );
			checkBox.UseVisualStyleBackColor = true;
		}

		//public int MaxFieldsDistance
		//{
		//   get { return (int)numericUpDownMaxFieldsDistance.Value; }
		//}

		//public int MaxFieldsToCheck
		//{
		//   get { return (int)numericUpDownMaxFieldsToCheck.Value; }
		//}

		public bool Smooth
		{
			get { return checkBoxSmooth.Checked; }
		}

		public bool Visualize
		{
			get { return checkBoxVisualize.Checked; }
		}

		void buttonTestPathFind_Click( object sender, EventArgs e )
		{
			if( MapEditorInterface.Instance.FunctionalityArea != null &&
				MapEditorInterface.Instance.FunctionalityArea is GridBasedNavigationSystemFunctionalityArea )
			{
				MapEditorInterface.Instance.FunctionalityArea = null;
			}
			else
			{
				MapEditorInterface.Instance.FunctionalityArea =
					new GridBasedNavigationSystemFunctionalityArea( system, this );
			}
		}

		string Translate( string text )
		{
			return ToolsLocalization.Translate( "GridBasedNavigationSystemExtendedFunctionalityDescriptor", text );
		}
	}
}
