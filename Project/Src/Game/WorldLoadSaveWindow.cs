// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.MathEx;
using Engine.UISystem;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace Game
{
	public class WorldLoadSaveWindow : Control
	{
		const string savedWorldsDirectory = "user:SavedWorlds";
		const string emptySlotText = "[Empty slot]";
		const int slotCount = 16;

		Control window;
		ListBox listBox;
		Button loadButton;
		Button saveButton;

		//

		static string GetWorldFileName( int slotIndex )
		{
			return string.Format( "{0}\\Slot{1}\\World.world",
				savedWorldsDirectory, slotIndex.ToString( "D02" ) );
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\WorldLoadSaveWindow.gui" );
			Controls.Add( window );

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;

			//worlds listBox
			{
				listBox = (ListBox)window.Controls[ "List" ];

				for( int slotIndex = 1; slotIndex <= slotCount; slotIndex++ )
				{
					string fileName = GetWorldFileName( slotIndex );

					string item;
					if( VirtualFile.Exists( fileName ) )
						item = fileName;
					else
						item = emptySlotText;

					listBox.Items.Add( item );
				}

				listBox.SelectedIndexChange += listBox_SelectedIndexChanged;
				if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
					listBox.SelectedIndex = 0;
				if( listBox.Items.Count != 0 )
					listBox_SelectedIndexChanged( null );
			}

			//Load button event handler
			loadButton = (Button)window.Controls[ "Load" ];
			loadButton.Click += delegate( Button sender )
			{
				string item = (string)listBox.SelectedItem;
				if( item != null && item != emptySlotText )
					Load( item );
			};

			//Save button event handler
			saveButton = (Button)window.Controls[ "Save" ];
			saveButton.Click += delegate( Button sender )
			{
				string item = (string)listBox.SelectedItem;
				if( item != null )
				{
					if( item == emptySlotText )
						item = GetWorldFileName( listBox.SelectedIndex + 1 );
					Save( item );
				}
			};

			//Close button event handler
			( (Button)window.Controls[ "Close" ] ).Click += delegate( Button sender )
			{
				SetShouldDetach();
			};

			UpdateButtonsEnabledFlag();
		}

		void listBox_SelectedIndexChanged( object sender )
		{
			//Texture texture = null;

			//if( listBox.SelectedIndex != -1 )
			//{
			//   string mapDirectory = Path.GetDirectoryName( (string)listBox.SelectedItem );
			//   string textureName = mapDirectory + "\\Description\\Preview";

			//   string textureFileName = null;

			//   bool finded = false;

			//   string[] extensions = new string[] { "dds", "tga", "png", "jpg" };
			//   foreach( string extension in extensions )
			//   {
			//      textureFileName = textureName + "." + extension;
			//      if( VirtualFile.Exists( textureFileName ) )
			//      {
			//         finded = true;
			//         break;
			//      }
			//   }

			//   if( finded )
			//      texture = TextureManager.Instance.Load( textureFileName );
			//}

			//window.Controls[ "Preview" ].Controls[ "TexturePlacer" ].BackTexture = texture;

			UpdateButtonsEnabledFlag();
		}

		void Load( string fileName )
		{
			GameEngineApp.Instance.SetNeedWorldLoad( fileName );
		}

		void Save( string fileName )
		{
			if( VirtualFile.Exists( fileName ) )
			{
				//overwrite check by MessageBox need here
			}

			if( !GameEngineApp.Instance.WorldSave( fileName ) )
				return;

			string text = string.Format( "World saved to \n\"{0}\".", fileName );
			GameEngineApp.Instance.ControlManager.Controls.Add(
				new MessageBoxWindow( text, "Load/Save", null ) );
			SetShouldDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape )
			{
				SetShouldDetach();
				return true;
			}
			return false;
		}

		void UpdateButtonsEnabledFlag()
		{
			string selectedItem = (string)listBox.SelectedItem;
			if( loadButton != null )
				loadButton.Enable = selectedItem != null && selectedItem != emptySlotText;
			if( saveButton != null )
			{
				saveButton.Enable = GameWindow.Instance != null &&
					EntitySystemWorld.Instance.IsSingle() && selectedItem != null;
			}
		}

	}
}
