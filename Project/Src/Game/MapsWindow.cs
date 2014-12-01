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
using Engine.MapSystem;
using Engine.Utils;

namespace Game
{
	/// <summary>
	/// Defines a window of map choice.
	/// </summary>
	public class MapsWindow : Control
	{
		const string exampleOfProceduralMapCreationText = "[The example of a procedural map creation]";

		[Config( "MapsWindow", "recentlyLoadedMaps" )]
		public static string recentlyLoadedMaps = "";

		//

		ListBox listBoxMaps;
		ListBox listBoxRecentlyLoaded;
		Control window;
		ComboBox comboBoxAutorunMap;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			EngineApp.Instance.Config.RegisterClassParameters( typeof( MapsWindow ) );

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MapsWindow.gui" );
			Controls.Add( window );

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;

			string[] mapList = VirtualDirectory.GetFiles( "", "*.map", SearchOption.AllDirectories );

			//maps listBox
			{
				listBoxMaps = (ListBox)window.Controls[ "List" ];

				//procedural map creation
				listBoxMaps.Items.Add( exampleOfProceduralMapCreationText );

				foreach( string name in mapList )
				{
					listBoxMaps.Items.Add( name );
					if( Map.Instance != null )
					{
						if( string.Compare( name.Replace( '/', '\\' ),
							Map.Instance.VirtualFileName.Replace( '/', '\\' ), true ) == 0 )
							listBoxMaps.SelectedIndex = listBoxMaps.Items.Count - 1;
					}
				}

				listBoxMaps.SelectedIndexChange += listBoxMaps_SelectedIndexChanged;
				if( listBoxMaps.Items.Count != 0 && listBoxMaps.SelectedIndex == -1 )
					listBoxMaps.SelectedIndex = 0;
				if( listBoxMaps.Items.Count != 0 )
					listBoxMaps_SelectedIndexChanged( null );

				listBoxMaps.ItemMouseDoubleClick += delegate( object sender, ListBox.ItemMouseEventArgs e )
				{
					RunMap( (string)e.Item );
				};
			}

			//recently loaded
			{
				listBoxRecentlyLoaded = (ListBox)window.Controls[ "RecentlyLoaded" ];

				string[] list = recentlyLoadedMaps.Split( new char[] { '|' },
					StringSplitOptions.RemoveEmptyEntries );

				listBoxRecentlyLoaded.Items.Add(
					LanguageManager.Instance.Translate( "UISystem", "(No selection)" ) );
				foreach( string mapName in list )
				{
					if( VirtualFile.Exists( mapName ) )
						listBoxRecentlyLoaded.Items.Add( mapName );
				}
				listBoxRecentlyLoaded.SelectedIndex = 0;

				listBoxRecentlyLoaded.SelectedIndexChange += listBoxRecentlyLoaded_SelectedIndexChanged;
				listBoxRecentlyLoaded.ItemMouseDoubleClick +=
					delegate( object sender, ListBox.ItemMouseEventArgs e )
					{
						if( e.ItemIndex != 0 )
							RunMap( (string)e.Item );
					};
			}

			//autorunMap
			comboBoxAutorunMap = (ComboBox)window.Controls[ "autorunMap" ];
			if( comboBoxAutorunMap != null )
			{
				comboBoxAutorunMap.Items.Add( LanguageManager.Instance.Translate( "UISystem", "(None)" ) );
				comboBoxAutorunMap.SelectedIndex = 0;
				foreach( string name in mapList )
				{
					comboBoxAutorunMap.Items.Add( name );

					if( string.Compare( GameEngineApp.autorunMapName.Replace( '/', '\\' ),
						name.Replace( '/', '\\' ), true ) == 0 )
					{
						comboBoxAutorunMap.SelectedIndex = comboBoxAutorunMap.Items.Count - 1;
					}
				}

				comboBoxAutorunMap.SelectedIndexChange += delegate( ComboBox sender )
				{
					if( sender.SelectedIndex != 0 )
						GameEngineApp.autorunMapName = (string)sender.SelectedItem;
					else
						GameEngineApp.autorunMapName = "";
				};
			}

			//Run button event handler
			( (Button)window.Controls[ "Run" ] ).Click += delegate( Button sender )
			{
				if( listBoxRecentlyLoaded.SelectedIndex > 0 )
				{
					RunMap( (string)listBoxRecentlyLoaded.SelectedItem );
				}
				else if( listBoxMaps.SelectedIndex != -1 )
				{
					RunMap( (string)listBoxMaps.SelectedItem );
				}
			};

			//Quit button event handler
			( (Button)window.Controls[ "Quit" ] ).Click += delegate( Button sender )
			{
				SetShouldDetach();
			};
		}

		void UpdateMapPreviewImageAndDescription()
		{
			//find selected map
			string mapName = null;
			if( listBoxRecentlyLoaded != null && listBoxRecentlyLoaded.SelectedIndex > 0 )
			{
				mapName = (string)listBoxRecentlyLoaded.SelectedItem;
			}
			else
			{
				if( listBoxMaps != null && listBoxMaps.SelectedIndex != -1 )
				{
					string mapName2 = (string)listBoxMaps.SelectedItem;
					if( mapName2 != exampleOfProceduralMapCreationText )
						mapName = mapName2;
				}
			}

			//get texture and description
			Texture texture = null;
			string description = "";

			if( !string.IsNullOrEmpty( mapName ) )
			{
				string mapDirectory = Path.GetDirectoryName( mapName );

				//get texture
				string textureName = mapDirectory + "\\Description\\Preview";
				string textureFileName = null;
				string[] extensions = new string[] { "jpg", "png", "tga" };
				foreach( string extension in extensions )
				{
					string fileName = textureName + "." + extension;
					if( VirtualFile.Exists( fileName ) )
					{
						textureFileName = fileName;
						break;
					}
				}
				if( textureFileName != null )
					texture = TextureManager.Instance.Load( textureFileName );

				//get description text
				string descriptionFileName = mapDirectory + "\\Description\\Description.config";
				if( VirtualFile.Exists( descriptionFileName ) )
				{
					string error;
					TextBlock block = TextBlockUtils.LoadFromVirtualFile( descriptionFileName, out error );
					if( block != null )
						description = block.GetAttribute( "description" );
				}
			}

			//update controls
			window.Controls[ "Preview" ].BackTexture = texture;
			window.Controls[ "Description" ].Text = description;
		}

		void listBoxMaps_SelectedIndexChanged( object sender )
		{
			UpdateMapPreviewImageAndDescription();
		}

		void listBoxRecentlyLoaded_SelectedIndexChanged( object sender )
		{
			UpdateMapPreviewImageAndDescription();

			listBoxMaps.Enable = listBoxRecentlyLoaded.SelectedIndex <= 0;
		}

		void UpdateRecentlyLoadedMapsList( string name )
		{
			string[] oldList = recentlyLoadedMaps.Split( new char[] { '|' },
				StringSplitOptions.RemoveEmptyEntries );

			List<string> newList = new List<string>();
			newList.Add( name );
			foreach( string mapName in oldList )
			{
				if( !newList.Contains( mapName ) && VirtualFile.Exists( mapName ) )
					newList.Add( mapName );
				if( newList.Count >= 3 )
					break;
			}

			recentlyLoadedMaps = "";
			foreach( string mapName in newList )
				recentlyLoadedMaps += mapName + "|";
		}

		void RunMap( string name )
		{
			if( name == exampleOfProceduralMapCreationText )
			{
				GameEngineApp.Instance.SetNeedRunExampleOfProceduralMapCreation();
			}
			else
			{
				UpdateRecentlyLoadedMapsList( name );

				//begin loading a map
				GameEngineApp.Instance.SetNeedMapLoad( name );
			}
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

	}
}
