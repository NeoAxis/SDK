// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;
using EditorBase;

namespace Configurator
{
	public partial class MainForm : EditorBase.Theme.EditorForm
	{
		bool formLoaded;
		bool needRestart;

		///////////////////////////////////////////

		class ComboBoxItem
		{
			string identifier;
			string displayName;

			public ComboBoxItem( string identifier, string displayName )
			{
				this.identifier = identifier;
				this.displayName = displayName;
			}

			public string Identifier
			{
				get { return identifier; }
			}

			public string DisplayName
			{
				get { return displayName; }
			}

			public override string ToString()
			{
				return Translate( displayName );
			}
		}

		///////////////////////////////////////////

		const int SW_SHOW = 5;
		const int SW_RESTORE = 9;

		[DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
		static extern bool ShowWindow( IntPtr hWnd, int nCmdShow );

		[DllImport( "user32.dll" )]
		static extern bool IsIconic( IntPtr hWnd );

		[DllImport( "user32.dll" )]
		static extern bool SetForegroundWindow( IntPtr hWnd );

		///////////////////////////////////////////

		public MainForm()
		{
			InitializeComponent();
		}

		string DetectLanguage()
		{
			string name = CultureInfo.CurrentUICulture.EnglishName;

			List<string> languages = new List<string>();
			{
				string[] directories = VirtualDirectory.GetDirectories( LanguageManager.LanguagesDirectory,
					"*.*", SearchOption.TopDirectoryOnly );
				foreach( string directory in directories )
				{
					string lang = Path.GetFileNameWithoutExtension( directory );
					languages.Add( lang );
				}
			}

			//1. find by exact name
			foreach( string lang in languages )
			{
				if( string.Compare( lang, name, true ) == 0 )
					return lang;
			}

			//2. find by including substring
			foreach( string lang in languages )
			{
				if( name.ToLower().Contains( lang.ToLower() ) )
					return lang;
			}

			return "English";
		}

		EngineComponentManager.ComponentInfo[] GetSortedComponentsByType( EngineComponentManager.ComponentTypeFlags type )
		{
			EngineComponentManager.ComponentInfo[] components = EngineComponentManager.Instance.GetComponentsByType(
				type, true );


			ArrayUtils.SelectionSort<EngineComponentManager.ComponentInfo>( components,
				delegate( EngineComponentManager.ComponentInfo info1, EngineComponentManager.ComponentInfo info2 )
				{
					if( info1.Name.Contains( "NULL" ) )
						return -1;
					if( info2.Name.Contains( "NULL" ) )
						return 1;
					return string.Compare( info1.FullName, info2.FullName, true );
				} );

			return components;
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			//Engine.config parameters
			string renderingSystemComponentName = "";
			bool allowShaders = true;
			bool depthBufferAccess = true;
			string fullSceneAntialiasing = "";
			RendererWorld.FilteringModes filtering = RendererWorld.FilteringModes.RecommendedSetting;
			string renderTechnique = "";
			bool fullScreen = true;
			string videoMode = "";
			bool multiMonitorMode = false;
			bool verticalSync = true;
			bool allowChangeDisplayFrequency = true;
			string physicsSystemComponentName = "";
			//bool physicsAllowHardwareAcceleration = false;
			string soundSystemComponentName = "";
			string language = "Autodetect";
			bool localizeEngine = true;
			bool localizeToolset = true;
			string renderingDeviceName = "";
			int renderingDeviceIndex = 0;
			int textureSkipMipLevels = 0;

			//load from Deployment.config
			if( VirtualFileSystem.Deployed )
			{
				if( !string.IsNullOrEmpty( VirtualFileSystem.DeploymentParameters.DefaultLanguage ) )
					language = VirtualFileSystem.DeploymentParameters.DefaultLanguage;
			}

			//load from Engine.config
			{
				string error;
				TextBlock block = TextBlockUtils.LoadFromRealFile(
					VirtualFileSystem.GetRealPathByVirtual( "user:Configs/Engine.config" ),
					out error );
				if( block != null )
				{
					//Renderer
					TextBlock rendererBlock = block.FindChild( "Renderer" );
					if( rendererBlock != null )
					{
						renderingSystemComponentName = rendererBlock.GetAttribute( "implementationComponent" );

						if( rendererBlock.IsAttributeExist( "renderingDeviceName" ) )
							renderingDeviceName = rendererBlock.GetAttribute( "renderingDeviceName" );
						if( rendererBlock.IsAttributeExist( "renderingDeviceIndex" ) )
							renderingDeviceIndex = int.Parse( rendererBlock.GetAttribute( "renderingDeviceIndex" ) );
						if( rendererBlock.IsAttributeExist( "allowShaders" ) )
							allowShaders = bool.Parse( rendererBlock.GetAttribute( "allowShaders" ) );
						if( rendererBlock.IsAttributeExist( "depthBufferAccess" ) )
							depthBufferAccess = bool.Parse( rendererBlock.GetAttribute( "depthBufferAccess" ) );
						if( rendererBlock.IsAttributeExist( "fullSceneAntialiasing" ) )
							fullSceneAntialiasing = rendererBlock.GetAttribute( "fullSceneAntialiasing" );

						if( rendererBlock.IsAttributeExist( "filtering" ) )
						{
							try
							{
								filtering = (RendererWorld.FilteringModes)
									Enum.Parse( typeof( RendererWorld.FilteringModes ),
									rendererBlock.GetAttribute( "filtering" ) );
							}
							catch { }
						}

						if( rendererBlock.IsAttributeExist( "renderTechnique" ) )
							renderTechnique = rendererBlock.GetAttribute( "renderTechnique" );

						if( rendererBlock.IsAttributeExist( "fullScreen" ) )
							fullScreen = bool.Parse( rendererBlock.GetAttribute( "fullScreen" ) );

						if( rendererBlock.IsAttributeExist( "videoMode" ) )
							videoMode = rendererBlock.GetAttribute( "videoMode" );

						if( rendererBlock.IsAttributeExist( "multiMonitorMode" ) )
							multiMonitorMode = bool.Parse( rendererBlock.GetAttribute( "multiMonitorMode" ) );

						if( rendererBlock.IsAttributeExist( "verticalSync" ) )
							verticalSync = bool.Parse( rendererBlock.GetAttribute( "verticalSync" ) );

						if( rendererBlock.IsAttributeExist( "allowChangeDisplayFrequency" ) )
							allowChangeDisplayFrequency = bool.Parse(
								rendererBlock.GetAttribute( "allowChangeDisplayFrequency" ) );

						if( rendererBlock.IsAttributeExist( "textureSkipMipLevels" ) )
							textureSkipMipLevels = int.Parse( rendererBlock.GetAttribute( "textureSkipMipLevels" ) );
					}

					//Physics system
					TextBlock physicsSystemBlock = block.FindChild( "PhysicsSystem" );
					if( physicsSystemBlock != null )
					{
						physicsSystemComponentName = physicsSystemBlock.GetAttribute( "implementationComponent" );
						//if( physicsSystemBlock.IsAttributeExist( "allowHardwareAcceleration" ) )
						//{
						//   physicsAllowHardwareAcceleration =
						//      bool.Parse( physicsSystemBlock.GetAttribute( "allowHardwareAcceleration" ) );
						//}
					}

					//Sound system
					TextBlock soundSystemBlock = block.FindChild( "SoundSystem" );
					if( soundSystemBlock != null )
						soundSystemComponentName = soundSystemBlock.GetAttribute( "implementationComponent" );

					//Localization
					TextBlock localizationBlock = block.FindChild( "Localization" );
					if( localizationBlock != null )
					{
						if( localizationBlock.IsAttributeExist( "language" ) )
							language = localizationBlock.GetAttribute( "language" );
						if( localizationBlock.IsAttributeExist( "localizeEngine" ) )
							localizeEngine = bool.Parse( localizationBlock.GetAttribute( "localizeEngine" ) );
						if( localizationBlock.IsAttributeExist( "localizeToolset" ) )
							localizeToolset = bool.Parse( localizationBlock.GetAttribute( "localizeToolset" ) );
					}
				}
			}

			//init toolset language
			if( localizeToolset )
			{
				if( !string.IsNullOrEmpty( language ) )
				{
					string language2 = language;
					if( string.Compare( language2, "autodetect", true ) == 0 )
						language2 = DetectLanguage();
					string languageDirectory = Path.Combine( LanguageManager.LanguagesDirectory, language2 );
					string fileName = Path.Combine( languageDirectory, "Configurator.language" );
					ToolsLocalization.Init( fileName );
				}
			}

			//fill render system
			{
				EngineComponentManager.ComponentInfo[] components = GetSortedComponentsByType(
					EngineComponentManager.ComponentTypeFlags.RenderingSystem );

				if( string.IsNullOrEmpty( renderingSystemComponentName ) )
				{
					//find component by default
					foreach( EngineComponentManager.ComponentInfo component2 in components )
					{
						if( component2.IsEnabledByDefaultForThisPlatform() )
						{
							renderingSystemComponentName = component2.Name;
							break;
						}
					}
				}

				//rendering systems combo box
				foreach( EngineComponentManager.ComponentInfo component in components )
				{
					string text = component.FullName;
					if( component.IsEnabledByDefaultForThisPlatform() )
						text = string.Format( Translate( "{0} (default)" ), text );
					int itemId = comboBoxRenderSystems.Items.Add( text );
					if( renderingSystemComponentName == component.Name )
						comboBoxRenderSystems.SelectedIndex = itemId;
				}
				if( comboBoxRenderSystems.Items.Count != 0 && comboBoxRenderSystems.SelectedIndex == -1 )
					comboBoxRenderSystems.SelectedIndex = 0;

				//rendering device
				{
					if( comboBoxRenderingDevices.Items.Count > 1 && !string.IsNullOrEmpty( renderingDeviceName ) )
					{
						int deviceCountWithSelectedName = 0;

						for( int n = 1; n < comboBoxRenderingDevices.Items.Count; n++ )
						{
							string name = (string)comboBoxRenderingDevices.Items[ n ];
							if( name == renderingDeviceName )
							{
								comboBoxRenderingDevices.SelectedIndex = n;
								deviceCountWithSelectedName++;
							}
						}

						if( deviceCountWithSelectedName > 1 )
						{
							int comboBoxIndex = renderingDeviceIndex + 1;
							if( comboBoxIndex < comboBoxRenderingDevices.Items.Count )
							{
								string name = (string)comboBoxRenderingDevices.Items[ comboBoxIndex ];
								if( name == renderingDeviceName )
									comboBoxRenderingDevices.SelectedIndex = comboBoxIndex;
							}
						}
					}

					if( comboBoxRenderingDevices.SelectedIndex == -1 && comboBoxRenderingDevices.Items.Count != 0 )
						comboBoxRenderingDevices.SelectedIndex = 0;
				}

				//allowShaders
				checkBoxAllowShaders.Checked = allowShaders;

				//depthBufferAccess
				comboBoxDepthBufferAccess.Items.Add( Translate( "No" ) );
				comboBoxDepthBufferAccess.Items.Add( Translate( "Yes" ) );
				comboBoxDepthBufferAccess.SelectedIndex = depthBufferAccess ? 1 : 0;

				//fullSceneAntialiasing
				for( int n = 0; n < comboBoxAntialiasing.Items.Count; n++ )
				{
					ComboBoxItem item = (ComboBoxItem)comboBoxAntialiasing.Items[ n ];
					if( item.Identifier == fullSceneAntialiasing )
						comboBoxAntialiasing.SelectedIndex = n;
				}
				if( comboBoxAntialiasing.SelectedIndex == -1 )
					comboBoxAntialiasing.SelectedIndex = 0;

				//filtering
				{
					Type enumType = typeof( RendererWorld.FilteringModes );
					LocalizedEnumConverter enumConverter = new LocalizedEnumConverter( enumType );

					RendererWorld.FilteringModes[] values =
						(RendererWorld.FilteringModes[])Enum.GetValues( enumType );
					for( int n = 0; n < values.Length; n++ )
					{
						RendererWorld.FilteringModes value = values[ n ];
						int index = comboBoxFiltering.Items.Add( enumConverter.ConvertToString( value ) );
						if( filtering == value )
							comboBoxFiltering.SelectedIndex = index;
					}
				}

				//renderTechnique
				{
					comboBoxRenderTechnique.Items.Add( new ComboBoxItem( "RecommendedSetting", "Recommended setting" ) );
					comboBoxRenderTechnique.Items.Add( new ComboBoxItem( "Standard", "Low Dynamic Range (Standard)" ) );
					comboBoxRenderTechnique.Items.Add( new ComboBoxItem( "HDR", "64-bit High Dynamic Range (HDR)" ) );

					for( int n = 0; n < comboBoxRenderTechnique.Items.Count; n++ )
					{
						ComboBoxItem item = (ComboBoxItem)comboBoxRenderTechnique.Items[ n ];
						if( item.Identifier == renderTechnique )
							comboBoxRenderTechnique.SelectedIndex = n;
					}
					if( comboBoxRenderTechnique.SelectedIndex == -1 )
						comboBoxRenderTechnique.SelectedIndex = 0;
				}

				//video mode
				{
					comboBoxVideoMode.Items.Add( Translate( "Current screen resolution" ) );
					comboBoxVideoMode.SelectedIndex = 0;

					comboBoxVideoMode.Items.Add( Translate( "Use all displays (multi-monitor mode)" ) );
					if( multiMonitorMode )
						comboBoxVideoMode.SelectedIndex = 1;

					foreach( Vec2I mode in DisplaySettings.VideoModes )
					{
						if( mode.X < 640 )
							continue;
						comboBoxVideoMode.Items.Add( string.Format( "{0}x{1}", mode.X, mode.Y ) );
						if( mode.ToString() == videoMode )
							comboBoxVideoMode.SelectedIndex = comboBoxVideoMode.Items.Count - 1;
					}

					if( !string.IsNullOrEmpty( videoMode ) && comboBoxVideoMode.SelectedIndex == 0 )
					{
						try
						{
							Vec2I mode = Vec2I.Parse( videoMode );
							comboBoxVideoMode.Items.Add( string.Format( "{0}x{1}", mode.X, mode.Y ) );
							comboBoxVideoMode.SelectedIndex = comboBoxVideoMode.Items.Count - 1;
						}
						catch { }
					}
				}

				//full screen
				checkBoxFullScreen.Checked = fullScreen;

				//vertical sync
				checkBoxVerticalSync.Checked = verticalSync;

				//comboBoxTextureSkipMipLevels
				if( textureSkipMipLevels < comboBoxTextureSkipMipLevels.Items.Count )
					comboBoxTextureSkipMipLevels.SelectedIndex = textureSkipMipLevels;
			}

			//fill physics system page
			{
				EngineComponentManager.ComponentInfo[] components = GetSortedComponentsByType(
					EngineComponentManager.ComponentTypeFlags.PhysicsSystem );

				if( string.IsNullOrEmpty( physicsSystemComponentName ) )
				{
					//find component by default
					foreach( EngineComponentManager.ComponentInfo component2 in components )
					{
						if( component2.IsEnabledByDefaultForThisPlatform() )
						{
							physicsSystemComponentName = component2.Name;
							break;
						}
					}
				}

				//update combo box
				foreach( EngineComponentManager.ComponentInfo component in components )
				{
					string text = component.FullName;
					if( component.IsEnabledByDefaultForThisPlatform() )
						text = string.Format( Translate( "{0} (default)" ), text );

					int itemId = comboBoxPhysicsSystems.Items.Add( text );
					if( physicsSystemComponentName == component.Name )
						comboBoxPhysicsSystems.SelectedIndex = itemId;
				}
				if( comboBoxPhysicsSystems.SelectedIndex == -1 )
					comboBoxPhysicsSystems.SelectedIndex = 0;

				//if( checkBoxPhysicsAllowHardwareAcceleration.Enabled )
				//   checkBoxPhysicsAllowHardwareAcceleration.Checked = physicsAllowHardwareAcceleration;
			}

			//fill sound system page
			{
				EngineComponentManager.ComponentInfo[] components = GetSortedComponentsByType(
					EngineComponentManager.ComponentTypeFlags.SoundSystem );

				if( string.IsNullOrEmpty( soundSystemComponentName ) )
				{
					//find component by default
					foreach( EngineComponentManager.ComponentInfo component2 in components )
					{
						if( component2.IsEnabledByDefaultForThisPlatform() )
						{
							soundSystemComponentName = component2.Name;
							break;
						}
					}
				}

				//update combo box
				foreach( EngineComponentManager.ComponentInfo component in components )
				{
					string text = component.FullName;
					if( component.IsEnabledByDefaultForThisPlatform() )
						text = string.Format( Translate( "{0} (default)" ), text );

					int itemId = comboBoxSoundSystems.Items.Add( text );
					if( soundSystemComponentName == component.Name )
						comboBoxSoundSystems.SelectedIndex = itemId;
				}
				if( comboBoxSoundSystems.SelectedIndex == -1 )
					comboBoxSoundSystems.SelectedIndex = 0;
			}

			//fill localization page
			{
				List<string> languages = new List<string>();
				{
					languages.Add( Translate( "Autodetect" ) );
					string[] directories = VirtualDirectory.GetDirectories( LanguageManager.LanguagesDirectory,
						"*.*", SearchOption.TopDirectoryOnly );
					foreach( string directory in directories )
						languages.Add( Path.GetFileNameWithoutExtension( directory ) );
				}

				foreach( string lang in languages )
				{
					int itemId = comboBoxLanguages.Items.Add( lang );
					if( string.Compare( language, lang, true ) == 0 )
						comboBoxLanguages.SelectedIndex = itemId;
				}

				if( comboBoxLanguages.SelectedIndex == -1 )
					comboBoxLanguages.SelectedIndex = 0;

				checkBoxLocalizeEngine.Checked = localizeEngine;
				checkBoxLocalizeToolset.Checked = localizeToolset;
			}

			Translate();

			formLoaded = true;
		}

		bool SaveEngineConfig()
		{
			TextBlock block = new TextBlock();

			//Renderer
			{
				EngineComponentManager.ComponentInfo[] components = GetSortedComponentsByType(
					EngineComponentManager.ComponentTypeFlags.RenderingSystem );

				EngineComponentManager.ComponentInfo component = null;
				if( comboBoxRenderSystems.SelectedIndex != -1 )
					component = components[ comboBoxRenderSystems.SelectedIndex ];

				TextBlock rendererBlock = block.AddChild( "Renderer" );
				if( component != null )
					rendererBlock.SetAttribute( "implementationComponent", component.Name );

				//rendering device
				if( component != null && component.Name.Contains( "Direct3D" ) )
				{
					rendererBlock.SetAttribute( "renderingDeviceName", (string)comboBoxRenderingDevices.SelectedItem );
					rendererBlock.SetAttribute( "renderingDeviceIndex", ( comboBoxRenderingDevices.SelectedIndex - 1 ).ToString() );
				}

				if( !checkBoxAllowShaders.Checked )
					rendererBlock.SetAttribute( "allowShaders", checkBoxAllowShaders.Checked.ToString() );

				//depthBufferAccess
				if( comboBoxDepthBufferAccess.SelectedIndex != -1 )
				{
					rendererBlock.SetAttribute( "depthBufferAccess",
						( comboBoxDepthBufferAccess.SelectedIndex == 1 ).ToString() );
				}

				//fullSceneAntialiasing
				if( comboBoxAntialiasing.SelectedIndex != -1 )
				{
					ComboBoxItem item = (ComboBoxItem)comboBoxAntialiasing.SelectedItem;
					rendererBlock.SetAttribute( "fullSceneAntialiasing", item.Identifier );
				}

				//filtering
				if( comboBoxFiltering.SelectedIndex != -1 )
				{
					RendererWorld.FilteringModes filtering = (RendererWorld.FilteringModes)
						comboBoxFiltering.SelectedIndex;
					rendererBlock.SetAttribute( "filtering", filtering.ToString() );
				}

				//renderTechnique
				if( comboBoxRenderTechnique.SelectedIndex != -1 )
				{
					ComboBoxItem item = (ComboBoxItem)comboBoxRenderTechnique.SelectedItem;
					rendererBlock.SetAttribute( "renderTechnique", item.Identifier );
				}

				//multiMonitorMode
				if( comboBoxVideoMode.SelectedIndex == 1 )
					rendererBlock.SetAttribute( "multiMonitorMode", true.ToString() );

				//videoMode
				if( comboBoxVideoMode.SelectedIndex >= 2 )
				{
					string[] strings = ( (string)comboBoxVideoMode.SelectedItem ).
						Split( new char[] { 'x' } );
					Vec2I videoMode = new Vec2I( int.Parse( strings[ 0 ] ),
						int.Parse( strings[ 1 ] ) );
					rendererBlock.SetAttribute( "videoMode", videoMode.ToString() );
				}

				//fullScreen
				rendererBlock.SetAttribute( "fullScreen", checkBoxFullScreen.Checked.ToString() );

				//vertical sync
				rendererBlock.SetAttribute( "verticalSync",
					checkBoxVerticalSync.Checked.ToString() );

				//texture skip mip levels
				int levels = comboBoxTextureSkipMipLevels.SelectedIndex;
				if( levels < 0 )
					levels = 0;
				rendererBlock.SetAttribute( "textureSkipMipLevels", levels.ToString() );
			}

			//Physics system
			{
				EngineComponentManager.ComponentInfo[] components = GetSortedComponentsByType(
					EngineComponentManager.ComponentTypeFlags.PhysicsSystem );

				EngineComponentManager.ComponentInfo component = null;
				if( comboBoxPhysicsSystems.SelectedIndex != -1 )
					component = components[ comboBoxPhysicsSystems.SelectedIndex ];

				if( component != null )
				{
					TextBlock physicsSystemBlock = block.AddChild( "PhysicsSystem" );
					physicsSystemBlock.SetAttribute( "implementationComponent", component.Name );
					//physicsSystemBlock.SetAttribute( "allowHardwareAcceleration",
					//   checkBoxPhysicsAllowHardwareAcceleration.Checked.ToString() );
				}
			}

			//Sound system
			{
				EngineComponentManager.ComponentInfo[] components = GetSortedComponentsByType(
					EngineComponentManager.ComponentTypeFlags.SoundSystem );

				EngineComponentManager.ComponentInfo component = null;
				if( comboBoxSoundSystems.SelectedIndex != -1 )
					component = components[ comboBoxSoundSystems.SelectedIndex ];

				if( component != null )
				{
					TextBlock soundSystemBlock = block.AddChild( "SoundSystem" );
					soundSystemBlock.SetAttribute( "implementationComponent", component.Name );
				}
			}

			//Localization
			{
				string language = "Autodetect";
				if( comboBoxLanguages.SelectedIndex > 0 )
					language = (string)comboBoxLanguages.SelectedItem;

				TextBlock localizationBlock = block.AddChild( "Localization" );
				localizationBlock.SetAttribute( "language", language );
				if( !checkBoxLocalizeEngine.Checked )
					localizationBlock.SetAttribute( "localizeEngine", checkBoxLocalizeEngine.Checked.ToString() );
				if( !checkBoxLocalizeToolset.Checked )
					localizationBlock.SetAttribute( "localizeToolset", checkBoxLocalizeToolset.Checked.ToString() );
			}

			//save file
			{
				string fileName = VirtualFileSystem.GetRealPathByVirtual(
					"user:Configs/Engine.config" );

				try
				{
					string directoryName = Path.GetDirectoryName( fileName );
					if( directoryName != "" && !Directory.Exists( directoryName ) )
						Directory.CreateDirectory( directoryName );
					using( StreamWriter writer = new StreamWriter( fileName ) )
					{
						writer.Write( block.DumpToString() );
					}
				}
				catch
				{
					string text = string.Format( "Saving file failed \"{0}\".", fileName );
					MessageBox.Show( text, "Configurator", MessageBoxButtons.OK,
						MessageBoxIcon.Warning );
					return false;
				}
			}

			return true;
		}

		protected override void OnFormClosing( FormClosingEventArgs e )
		{
			if( DialogResult == DialogResult.OK )
			{
				if( !SaveEngineConfig() )
				{
					e.Cancel = true;
					return;
				}
			}

			//update tools localization file
			if( ToolsLocalization.IsInitialized && ToolsLocalization.NewKeysWasAdded )
				ToolsLocalization.Save();

			base.OnFormClosing( e );
		}

		private void buttonOK_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buttonCancel_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void comboBoxRenderSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( comboBoxRenderSystems.SelectedIndex == -1 )
				return;

			//Update max shaders

			string renderSystemName = (string)comboBoxRenderSystems.SelectedItem;
			bool isDirect3D = renderSystemName.Contains( "Direct3D" );

			//comboBoxRenderingDevices
			{
				int lastSelectedIndex = comboBoxRenderingDevices.SelectedIndex;

				comboBoxRenderingDevices.Items.Clear();

				comboBoxRenderingDevices.Items.Add( Translate( "Default rendering device" ) );

				if( isDirect3D )
				{
					_Direct3D9Utils.AdapterIdentifier[] adapters = _Direct3D9Utils.GetAdapters();
					if( adapters != null )
					{
						foreach( _Direct3D9Utils.AdapterIdentifier adapter in adapters )
							comboBoxRenderingDevices.Items.Add( adapter.Description );
					}
				}

				if( lastSelectedIndex >= 0 && lastSelectedIndex < comboBoxRenderingDevices.Items.Count )
					comboBoxRenderingDevices.SelectedIndex = lastSelectedIndex;
				else
					comboBoxRenderingDevices.SelectedIndex = 0;

				comboBoxRenderingDevices.Enabled = isDirect3D;
			}

			//comboBoxDepthBufferAccess
			{
				comboBoxDepthBufferAccess.Enabled = isDirect3D;
				if( comboBoxDepthBufferAccess.Items.Count != 0 )
				{
					if( !comboBoxDepthBufferAccess.Enabled )
						comboBoxDepthBufferAccess.SelectedIndex = 0;
					else
						comboBoxDepthBufferAccess.SelectedIndex = 1;
				}
			}
		}

		private void comboBoxDepthBufferAccess_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( comboBoxDepthBufferAccess.SelectedIndex == -1 )
				return;

			//comboBoxAntialiasing
			{
				int lastSelectedIndex = comboBoxAntialiasing.SelectedIndex;

				comboBoxAntialiasing.Items.Clear();

				comboBoxAntialiasing.Items.Add( new ComboBoxItem( "RecommendedSetting", "Recommended setting" ) );
				comboBoxAntialiasing.Items.Add( new ComboBoxItem( "0", "No" ) );
				if( comboBoxDepthBufferAccess.SelectedIndex == 0 )
				{
					comboBoxAntialiasing.Items.Add( new ComboBoxItem( "2", "2" ) );
					comboBoxAntialiasing.Items.Add( new ComboBoxItem( "4", "4" ) );
					comboBoxAntialiasing.Items.Add( new ComboBoxItem( "6", "6" ) );
					comboBoxAntialiasing.Items.Add( new ComboBoxItem( "8", "8" ) );
				}
				comboBoxAntialiasing.Items.Add(
					new ComboBoxItem( "FXAA", "Fast Approximate Antialiasing (FXAA)" ) );

				if( lastSelectedIndex >= 0 && lastSelectedIndex <= 1 )
					comboBoxAntialiasing.SelectedIndex = lastSelectedIndex;
				else
					comboBoxAntialiasing.SelectedIndex = 0;
			}

		}

		private void comboBoxPhysicsSystems_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( comboBoxPhysicsSystems.SelectedIndex == -1 )
				return;

			string physicsSystemName = (string)comboBoxPhysicsSystems.SelectedItem;
			bool isPhysX = physicsSystemName.ToLower().Contains( "physx" );

			//checkBoxPhysicsAllowHardwareAcceleration.Enabled = isPhysX;
			//if( !isPhysX )
			//   checkBoxPhysicsAllowHardwareAcceleration.Checked = false;
		}

		static string Translate( string text )
		{
			return ToolsLocalization.Translate( "MainForm", text );
		}

		void Translate()
		{
			Text = Translate( Text );

			foreach( Control page in tabControl1.Controls )
			{
				page.Text = Translate( page.Text );

				foreach( Control control in page.Controls )
				{
					if( control is Label || control is Button || control is CheckBox )
						control.Text = Translate( control.Text );
				}
			}

			buttonOK.Text = Translate( buttonOK.Text );
			buttonCancel.Text = Translate( buttonCancel.Text );
		}

		void ActivateApplication( Process process )
		{
			if( IsIconic( process.MainWindowHandle ) )
				ShowWindow( process.MainWindowHandle, SW_RESTORE );
			SetForegroundWindow( process.MainWindowHandle );
		}

		void RunApplication( string fileName, bool checkAlreadyRunning )
		{
			try
			{
				string directory = Path.GetDirectoryName( Process.GetCurrentProcess().MainModule.FileName );
				string path = Path.Combine( directory, fileName );

				if( checkAlreadyRunning )
				{
					foreach( Process process in System.Diagnostics.Process.GetProcesses() )
					{
						try
						{
							if( string.Compare( process.MainModule.FileName, path, true ) == 0 )
							{
								ActivateApplication( process );
								return;
							}
						}
						catch { }
					}
				}

				Process.Start( path, "" );
			}
			catch( Exception ex )
			{
				MessageBox.Show( ex.Message, "Configurator", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			}
		}

		private void buttonRunGame_Click( object sender, EventArgs e )
		{
			if( !SaveEngineConfig() )
				return;
			RunApplication( "Game.exe", true );
		}

		private void buttonRunResourceEditor_Click( object sender, EventArgs e )
		{
			if( !SaveEngineConfig() )
				return;
			RunApplication( "ResourceEditor.exe", true );
		}

		private void buttonRunMapEditor_Click( object sender, EventArgs e )
		{
			if( !SaveEngineConfig() )
				return;
			RunApplication( "MapEditor.exe", true );
		}

		private void buttonRunDeploymentTool_Click( object sender, EventArgs e )
		{
			if( !SaveEngineConfig() )
				return;
			RunApplication( "DeploymentTool.exe", true );
		}

		private void buttonRunShaderCacheCompiler_Click( object sender, EventArgs e )
		{
			if( !SaveEngineConfig() )
				return;
			RunApplication( "ShaderCacheCompiler.exe", true );
		}

		private void comboBoxLanguages_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( formLoaded )
				buttonRestart.Enabled = true;
		}

		private void checkBoxLocalizeEngine_CheckedChanged( object sender, EventArgs e )
		{
			if( formLoaded )
				buttonRestart.Enabled = true;
		}

		private void checkBoxLocalizeToolset_CheckedChanged( object sender, EventArgs e )
		{
			if( formLoaded )
				buttonRestart.Enabled = true;
		}

		private void buttonRestart_Click( object sender, EventArgs e )
		{
			needRestart = true;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			if( needRestart )
				RunApplication( "Configurator.exe", false );
		}

		private void label21_Click( object sender, EventArgs e )
		{

		}
	}
}
