// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.Utils;
using Engine.SoundSystem;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a window of options.
	/// </summary>
	public class OptionsWindow : Control
	{
		static int lastPageIndex;

		Control window;
		TabControl tabControl;
		Button[] pageButtons = new Button[ 5 ];

		ComboBox comboBoxResolution;
		ComboBox comboBoxInputDevices;
		CheckBox checkBoxDepthBufferAccess;
		ComboBox comboBoxAntialiasing;

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
				return displayName;
			}
		}

		///////////////////////////////////////////

		public class ShadowTechniqueItem
		{
			ShadowTechniques technique;
			string text;

			public ShadowTechniqueItem( ShadowTechniques technique, string text )
			{
				this.technique = technique;
				this.text = text;
			}

			public ShadowTechniques Technique
			{
				get { return technique; }
			}

			public override string ToString()
			{
				return text;
			}
		}

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			ComboBox comboBox;
			ScrollBar scrollBar;
			CheckBox checkBox;

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\OptionsWindow.gui" );
			Controls.Add( window );

			tabControl = (TabControl)window.Controls[ "TabControl" ];

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;

			//load Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			TextBlock rendererBlock = null;
			if( engineConfigBlock != null )
				rendererBlock = engineConfigBlock.FindChild( "Renderer" );

			//page buttons
			pageButtons[ 0 ] = (Button)window.Controls[ "ButtonVideo" ];
			pageButtons[ 1 ] = (Button)window.Controls[ "ButtonShadows" ];
			pageButtons[ 2 ] = (Button)window.Controls[ "ButtonSound" ];
			pageButtons[ 3 ] = (Button)window.Controls[ "ButtonControls" ];
			pageButtons[ 4 ] = (Button)window.Controls[ "ButtonLanguage" ];
			foreach( Button pageButton in pageButtons )
				pageButton.Click += new Button.ClickDelegate( pageButton_Click );

			//Close button
			( (Button)window.Controls[ "Close" ] ).Click += delegate( Button sender )
			{
				SetShouldDetach();
			};

			//pageVideo
			{
				Control pageVideo = tabControl.Controls[ "Video" ];

				Vec2I currentMode = EngineApp.Instance.VideoMode;

				//screenResolutionComboBox
				comboBox = (ComboBox)pageVideo.Controls[ "ScreenResolution" ];
				comboBox.Enable = !EngineApp.Instance.MultiMonitorMode;
				comboBoxResolution = comboBox;

				if( EngineApp.Instance.MultiMonitorMode )
				{
					comboBox.Items.Add( string.Format( "{0}x{1} (multi-monitor)", currentMode.X,
						currentMode.Y ) );
					comboBox.SelectedIndex = 0;
				}
				else
				{
					foreach( Vec2I mode in DisplaySettings.VideoModes )
					{
						if( mode.X < 640 )
							continue;

						comboBox.Items.Add( string.Format( "{0}x{1}", mode.X, mode.Y ) );

						if( mode == currentMode )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}

					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						ChangeVideoMode();
					};
				}

				//gamma
				scrollBar = (ScrollBar)pageVideo.Controls[ "Gamma" ];
				scrollBar.Value = GameEngineApp._Gamma;
				scrollBar.Enable = true;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					GameEngineApp._Gamma = value;
					pageVideo.Controls[ "GammaValue" ].Text = value.ToString( "F1" );
				};
				pageVideo.Controls[ "GammaValue" ].Text = GameEngineApp._Gamma.ToString( "F1" );

				//MaterialScheme
				{
					comboBox = (ComboBox)pageVideo.Controls[ "MaterialScheme" ];
					foreach( MaterialSchemes materialScheme in
						Enum.GetValues( typeof( MaterialSchemes ) ) )
					{
						comboBox.Items.Add( materialScheme.ToString() );

						if( GameEngineApp.MaterialScheme == materialScheme )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
							GameEngineApp.MaterialScheme = (MaterialSchemes)sender.SelectedIndex;
					};
				}

				//fullScreen
				checkBox = (CheckBox)pageVideo.Controls[ "FullScreen" ];
				checkBox.Enable = !EngineApp.Instance.MultiMonitorMode;
				checkBox.Checked = EngineApp.Instance.FullScreen;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					EngineApp.Instance.FullScreen = sender.Checked;
				};

				//RenderTechnique
				{
					comboBox = (ComboBox)pageVideo.Controls[ "RenderTechnique" ];
					comboBox.Items.Add( new ComboBoxItem( "RecommendedSetting", Translate( "Recommended setting" ) ) );
					comboBox.Items.Add( new ComboBoxItem( "Standard", Translate( "Low Dynamic Range (Standard)" ) ) );
					comboBox.Items.Add( new ComboBoxItem( "HDR", Translate( "High Dynamic Range (HDR)" ) ) );

					string renderTechnique = "";
					if( rendererBlock != null && rendererBlock.IsAttributeExist( "renderTechnique" ) )
						renderTechnique = rendererBlock.GetAttribute( "renderTechnique" );

					for( int n = 0; n < comboBox.Items.Count; n++ )
					{
						ComboBoxItem item = (ComboBoxItem)comboBox.Items[ n ];
						if( item.Identifier == renderTechnique )
							comboBox.SelectedIndex = n;
					}
					if( comboBox.SelectedIndex == -1 )
						comboBox.SelectedIndex = 0;

					comboBox.SelectedIndexChange += comboBoxRenderTechnique_SelectedIndexChange;
				}

				//Filtering
				{
					comboBox = (ComboBox)pageVideo.Controls[ "Filtering" ];

					Type enumType = typeof( RendererWorld.FilteringModes );
					LocalizedEnumConverter enumConverter = new LocalizedEnumConverter( enumType );

					RendererWorld.FilteringModes filtering = RendererWorld.FilteringModes.RecommendedSetting;
					//get value from Engine.config.
					if( rendererBlock != null && rendererBlock.IsAttributeExist( "filtering" ) )
					{
						try
						{
							filtering = (RendererWorld.FilteringModes)Enum.Parse( enumType, rendererBlock.GetAttribute( "filtering" ) );
						}
						catch { }
					}

					RendererWorld.FilteringModes[] values = (RendererWorld.FilteringModes[])Enum.GetValues( enumType );
					for( int n = 0; n < values.Length; n++ )
					{
						RendererWorld.FilteringModes value = values[ n ];
						string valueStr = enumConverter.ConvertToString( value );
						comboBox.Items.Add( new ComboBoxItem( value.ToString(), Translate( valueStr ) ) );
						if( filtering == value )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					if( comboBox.SelectedIndex == -1 )
						comboBox.SelectedIndex = 0;

					comboBox.SelectedIndexChange += comboBoxFiltering_SelectedIndexChange;
				}

				//DepthBufferAccess
				{
					checkBox = (CheckBox)pageVideo.Controls[ "DepthBufferAccess" ];
					checkBoxDepthBufferAccess = checkBox;

					bool depthBufferAccess = true;
					//get value from Engine.config.
					if( rendererBlock != null && rendererBlock.IsAttributeExist( "depthBufferAccess" ) )
						depthBufferAccess = bool.Parse( rendererBlock.GetAttribute( "depthBufferAccess" ) );
					checkBox.Checked = depthBufferAccess;

					checkBox.CheckedChange += checkBoxDepthBufferAccess_CheckedChange;
				}

				//FSAA
				{
					comboBox = (ComboBox)pageVideo.Controls[ "FSAA" ];
					comboBoxAntialiasing = comboBox;

					UpdateComboBoxAntialiasing();

					string fullSceneAntialiasing = "";
					if( rendererBlock != null && rendererBlock.IsAttributeExist( "fullSceneAntialiasing" ) )
						fullSceneAntialiasing = rendererBlock.GetAttribute( "fullSceneAntialiasing" );
					for( int n = 0; n < comboBoxAntialiasing.Items.Count; n++ )
					{
						ComboBoxItem item = (ComboBoxItem)comboBoxAntialiasing.Items[ n ];
						if( item.Identifier == fullSceneAntialiasing )
							comboBoxAntialiasing.SelectedIndex = n;
					}

					comboBoxAntialiasing.SelectedIndexChange += comboBoxAntialiasing_SelectedIndexChange;
				}

				//VerticalSync
				{
					checkBox = (CheckBox)pageVideo.Controls[ "VerticalSync" ];

					bool verticalSync = RendererWorld.InitializationOptions.VerticalSync;
					//get value from Engine.config.
					if( rendererBlock != null && rendererBlock.IsAttributeExist( "verticalSync" ) )
						verticalSync = bool.Parse( rendererBlock.GetAttribute( "verticalSync" ) );
					checkBox.Checked = verticalSync;

					checkBox.CheckedChange += checkBoxVerticalSync_CheckedChange;
				}

				{
					int levels = RendererWorld.InitializationOptions.TextureSkipMipLevels;
					//get value from Engine.config.
					if( rendererBlock != null && rendererBlock.IsAttributeExist( "textureSkipMipLevels" ) )
						levels = int.Parse( rendererBlock.GetAttribute( "textureSkipMipLevels" ) );

					comboBox = (ComboBox)pageVideo.Controls[ "TextureSkipMipLevels" ];
					for( int n = 0; n <= 7; n++ )
					{
						comboBox.Items.Add( n );
						if( levels == n )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					if( comboBox.SelectedIndex < 0 )
						comboBox.SelectedIndex = 0;

					comboBox.SelectedIndexChange += comboBoxTextureSkipMipLevels_SelectedIndexChange;
				}

				//VideoRestart
				{
					Button button = (Button)pageVideo.Controls[ "VideoRestart" ];
					button.Click += buttonVideoRestart_Click;
				}

				//waterReflectionLevel
				comboBox = (ComboBox)pageVideo.Controls[ "WaterReflectionLevel" ];
				foreach( WaterPlane.ReflectionLevels level in Enum.GetValues(
					typeof( WaterPlane.ReflectionLevels ) ) )
				{
					comboBox.Items.Add( level );
					if( GameEngineApp.WaterReflectionLevel == level )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}
				comboBox.SelectedIndexChange += delegate( ComboBox sender )
				{
					GameEngineApp.WaterReflectionLevel = (WaterPlane.ReflectionLevels)sender.SelectedItem;
				};

				//showDecorativeObjects
				checkBox = (CheckBox)pageVideo.Controls[ "ShowDecorativeObjects" ];
				checkBox.Checked = GameEngineApp.ShowDecorativeObjects;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameEngineApp.ShowDecorativeObjects = sender.Checked;
				};

				//showSystemCursorCheckBox
				checkBox = (CheckBox)pageVideo.Controls[ "ShowSystemCursor" ];
				checkBox.Checked = GameEngineApp._ShowSystemCursor;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameEngineApp._ShowSystemCursor = sender.Checked;
					sender.Checked = GameEngineApp._ShowSystemCursor;
				};

				//showFPSCheckBox
				checkBox = (CheckBox)pageVideo.Controls[ "ShowFPS" ];
				checkBox.Checked = GameEngineApp._DrawFPS;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameEngineApp._DrawFPS = sender.Checked;
					sender.Checked = GameEngineApp._DrawFPS;
				};
			}

			//pageShadows
			{
				Control pageShadows = tabControl.Controls[ "Shadows" ];

				//ShadowTechnique
				{
					comboBox = (ComboBox)pageShadows.Controls[ "ShadowTechnique" ];

					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.None, "None" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapLow, "Shadowmap Low" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapMedium, "Shadowmap Medium" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapHigh, "Shadowmap High" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapLowPSSM, "PSSMx3 Low" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapMediumPSSM, "PSSMx3 Medium" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapHighPSSM, "PSSMx3 High" ) );

					for( int n = 0; n < comboBox.Items.Count; n++ )
					{
						ShadowTechniqueItem item = (ShadowTechniqueItem)comboBox.Items[ n ];
						if( item.Technique == GameEngineApp.ShadowTechnique )
							comboBox.SelectedIndex = n;
					}

					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
						{
							ShadowTechniqueItem item = (ShadowTechniqueItem)sender.SelectedItem;
							GameEngineApp.ShadowTechnique = item.Technique;
						}
						UpdateShadowControlsEnable();
					};
					UpdateShadowControlsEnable();
				}

				//ShadowUseMapSettings
				{
					checkBox = (CheckBox)pageShadows.Controls[ "ShadowUseMapSettings" ];
					checkBox.Checked = GameEngineApp.ShadowUseMapSettings;
					checkBox.CheckedChange += delegate( CheckBox sender )
					{
						GameEngineApp.ShadowUseMapSettings = sender.Checked;
						if( sender.Checked && Map.Instance != null )
						{
							GameEngineApp.ShadowPSSMSplitFactors = Map.Instance.InitialShadowPSSMSplitFactors;
							GameEngineApp.ShadowFarDistance = Map.Instance.InitialShadowFarDistance;
							GameEngineApp.ShadowColor = Map.Instance.InitialShadowColor;
						}

						UpdateShadowControlsEnable();

						if( sender.Checked )
						{
							( (ScrollBar)pageShadows.Controls[ "ShadowFarDistance" ] ).Value =
								GameEngineApp.ShadowFarDistance;

							pageShadows.Controls[ "ShadowFarDistanceValue" ].Text =
								( (int)GameEngineApp.ShadowFarDistance ).ToString();

							ColorValue color = GameEngineApp.ShadowColor;
							( (ScrollBar)pageShadows.Controls[ "ShadowColor" ] ).Value =
								( color.Red + color.Green + color.Blue ) / 3;
						}
					};
				}

				//ShadowPSSMSplitFactor1
				scrollBar = (ScrollBar)pageShadows.Controls[ "ShadowPSSMSplitFactor1" ];
				scrollBar.Value = GameEngineApp.ShadowPSSMSplitFactors[ 0 ];
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.ShadowPSSMSplitFactors = new Vec2(
						sender.Value, GameEngineApp.ShadowPSSMSplitFactors[ 1 ] );
					pageShadows.Controls[ "ShadowPSSMSplitFactor1Value" ].Text =
						( GameEngineApp.ShadowPSSMSplitFactors[ 0 ].ToString( "F2" ) ).ToString();
				};
				pageShadows.Controls[ "ShadowPSSMSplitFactor1Value" ].Text =
					( GameEngineApp.ShadowPSSMSplitFactors[ 0 ].ToString( "F2" ) ).ToString();

				//ShadowPSSMSplitFactor2
				scrollBar = (ScrollBar)pageShadows.Controls[ "ShadowPSSMSplitFactor2" ];
				scrollBar.Value = GameEngineApp.ShadowPSSMSplitFactors[ 1 ];
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.ShadowPSSMSplitFactors = new Vec2(
						GameEngineApp.ShadowPSSMSplitFactors[ 0 ], sender.Value );
					pageShadows.Controls[ "ShadowPSSMSplitFactor2Value" ].Text =
						( GameEngineApp.ShadowPSSMSplitFactors[ 1 ].ToString( "F2" ) ).ToString();
				};
				pageShadows.Controls[ "ShadowPSSMSplitFactor2Value" ].Text =
					( GameEngineApp.ShadowPSSMSplitFactors[ 1 ].ToString( "F2" ) ).ToString();

				//ShadowFarDistance
				scrollBar = (ScrollBar)pageShadows.Controls[ "ShadowFarDistance" ];
				scrollBar.Value = GameEngineApp.ShadowFarDistance;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.ShadowFarDistance = sender.Value;
					pageShadows.Controls[ "ShadowFarDistanceValue" ].Text =
						( (int)GameEngineApp.ShadowFarDistance ).ToString();
				};
				pageShadows.Controls[ "ShadowFarDistanceValue" ].Text =
					( (int)GameEngineApp.ShadowFarDistance ).ToString();

				//ShadowColor
				scrollBar = (ScrollBar)pageShadows.Controls[ "ShadowColor" ];
				scrollBar.Value = ( GameEngineApp.ShadowColor.Red + GameEngineApp.ShadowColor.Green +
					GameEngineApp.ShadowColor.Blue ) / 3;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					float color = sender.Value;
					GameEngineApp.ShadowColor = new ColorValue( color, color, color, color );
				};

				//ShadowDirectionalLightTextureSize
				{
					comboBox = (ComboBox)pageShadows.Controls[ "ShadowDirectionalLightTextureSize" ];
					for( int value = 256, index = 0; value <= 8192; value *= 2, index++ )
					{
						comboBox.Items.Add( value );
						if( GameEngineApp.ShadowDirectionalLightTextureSize == value )
							comboBox.SelectedIndex = index;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowDirectionalLightTextureSize = (int)sender.SelectedItem;
					};
				}

				////ShadowDirectionalLightMaxTextureCount
				//{
				//   comboBox = (EComboBox)pageVideo.Controls[ "ShadowDirectionalLightMaxTextureCount" ];
				//   for( int n = 0; n < 3; n++ )
				//   {
				//      int count = n + 1;
				//      comboBox.Items.Add( count );
				//      if( count == GameEngineApp.ShadowDirectionalLightMaxTextureCount )
				//         comboBox.SelectedIndex = n;
				//   }
				//   comboBox.SelectedIndexChange += delegate( EComboBox sender )
				//   {
				//      GameEngineApp.ShadowDirectionalLightMaxTextureCount = (int)sender.SelectedItem;
				//   };
				//}

				//ShadowSpotLightTextureSize
				{
					comboBox = (ComboBox)pageShadows.Controls[ "ShadowSpotLightTextureSize" ];
					for( int value = 256, index = 0; value <= 8192; value *= 2, index++ )
					{
						comboBox.Items.Add( value );
						if( GameEngineApp.ShadowSpotLightTextureSize == value )
							comboBox.SelectedIndex = index;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowSpotLightTextureSize = (int)sender.SelectedItem;
					};
				}

				//ShadowSpotLightMaxTextureCount
				{
					comboBox = (ComboBox)pageShadows.Controls[ "ShadowSpotLightMaxTextureCount" ];
					for( int n = 0; n < 4; n++ )
					{
						comboBox.Items.Add( n );
						if( n == GameEngineApp.ShadowSpotLightMaxTextureCount )
							comboBox.SelectedIndex = n;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowSpotLightMaxTextureCount = (int)sender.SelectedItem;
					};
				}

				//ShadowPointLightTextureSize
				{
					comboBox = (ComboBox)pageShadows.Controls[ "ShadowPointLightTextureSize" ];
					for( int value = 256, index = 0; value <= 8192; value *= 2, index++ )
					{
						comboBox.Items.Add( value );
						if( GameEngineApp.ShadowPointLightTextureSize == value )
							comboBox.SelectedIndex = index;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowPointLightTextureSize = (int)sender.SelectedItem;
					};
				}

				//ShadowPointLightMaxTextureCount
				{
					comboBox = (ComboBox)pageShadows.Controls[ "ShadowPointLightMaxTextureCount" ];
					for( int n = 0; n < 4; n++ )
					{
						comboBox.Items.Add( n );
						if( n == GameEngineApp.ShadowPointLightMaxTextureCount )
							comboBox.SelectedIndex = n;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowPointLightMaxTextureCount = (int)sender.SelectedItem;
					};
				}
			}

			//pageSound
			{
				bool enabled = SoundWorld.Instance.DriverName != "NULL";

				Control pageSound = tabControl.Controls[ "Sound" ];

				//soundVolumeCheckBox
				scrollBar = (ScrollBar)pageSound.Controls[ "SoundVolume" ];
				scrollBar.Value = enabled ? GameEngineApp.SoundVolume : 0;
				scrollBar.Enable = enabled;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.SoundVolume = sender.Value;
				};

				//musicVolumeCheckBox
				scrollBar = (ScrollBar)pageSound.Controls[ "MusicVolume" ];
				scrollBar.Value = enabled ? GameEngineApp.MusicVolume : 0;
				scrollBar.Enable = enabled;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.MusicVolume = sender.Value;
				};
			}

			//pageControls
			{
				Control pageControls = tabControl.Controls[ "Controls" ];

				//MouseHSensitivity
				scrollBar = (ScrollBar)pageControls.Controls[ "MouseHSensitivity" ];
				scrollBar.Value = GameControlsManager.Instance.MouseSensitivity.X;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.X = sender.Value;
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVSensitivity
				scrollBar = (ScrollBar)pageControls.Controls[ "MouseVSensitivity" ];
				scrollBar.Value = Math.Abs( GameControlsManager.Instance.MouseSensitivity.Y );
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					bool invert = ( (CheckBox)pageControls.Controls[ "MouseVInvert" ] ).Checked;
					value.Y = sender.Value * ( invert ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVInvert
				checkBox = (CheckBox)pageControls.Controls[ "MouseVInvert" ];
				checkBox.Checked = GameControlsManager.Instance.MouseSensitivity.Y < 0;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.Y =
						( (ScrollBar)pageControls.Controls[ "MouseVSensitivity" ] ).Value *
						( sender.Checked ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//AlwaysRun
				checkBox = (CheckBox)pageControls.Controls[ "AlwaysRun" ];
				checkBox.Checked = GameControlsManager.Instance.AlwaysRun;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameControlsManager.Instance.AlwaysRun = sender.Checked;
				};

				//Devices
				comboBox = (ComboBox)pageControls.Controls[ "InputDevices" ];
				comboBoxInputDevices = comboBox;
				comboBox.Items.Add( "Keyboard/Mouse" );
				if( InputDeviceManager.Instance != null )
				{
					foreach( InputDevice device in InputDeviceManager.Instance.Devices )
						comboBox.Items.Add( device );
				}
				comboBox.SelectedIndex = 0;

				comboBox.SelectedIndexChange += delegate( ComboBox sender )
				{
					UpdateBindedInputControlsTextBox();
				};

				//Controls
				UpdateBindedInputControlsTextBox();
			}

			//pageLanguage
			{
				Control pageLanguage = tabControl.Controls[ "Language" ];

				//Language
				{
					comboBox = (ComboBox)pageLanguage.Controls[ "Language" ];

					List<string> languages = new List<string>();
					{
						languages.Add( "Autodetect" );
						string[] directories = VirtualDirectory.GetDirectories( LanguageManager.LanguagesDirectory, "*.*",
							SearchOption.TopDirectoryOnly );
						foreach( string directory in directories )
							languages.Add( Path.GetFileNameWithoutExtension( directory ) );
					}

					string language = "Autodetect";
					if( engineConfigBlock != null )
					{
						TextBlock localizationBlock = engineConfigBlock.FindChild( "Localization" );
						if( localizationBlock != null && localizationBlock.IsAttributeExist( "language" ) )
							language = localizationBlock.GetAttribute( "language" );
					}

					foreach( string lang in languages )
					{
						string displayName = lang;
						if( lang == "Autodetect" )
							displayName = Translate( lang );

						comboBox.Items.Add( new ComboBoxItem( lang, displayName ) );
						if( string.Compare( language, lang, true ) == 0 )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					if( comboBox.SelectedIndex == -1 )
						comboBox.SelectedIndex = 0;

					comboBox.SelectedIndexChange += comboBoxLanguage_SelectedIndexChange;
				}

				//LanguageRestart
				{
					Button button = (Button)pageLanguage.Controls[ "LanguageRestart" ];
					button.Click += buttonLanguageRestart_Click;
				}
			}

			tabControl.SelectedIndex = lastPageIndex;
			tabControl.SelectedIndexChange += tabControl_SelectedIndexChange;
			UpdatePageButtonsState();
		}

		void UpdateShadowControlsEnable()
		{
			Control pageVideo = window.Controls[ "TabControl" ].Controls[ "Shadows" ];

			bool textureShadows =
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLow ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMedium ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHigh ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHighPSSM;

			bool pssm = GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHighPSSM;

			bool allowShadowColor = GameEngineApp.ShadowTechnique != ShadowTechniques.None;

			pageVideo.Controls[ "ShadowColor" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && allowShadowColor;

			pageVideo.Controls[ "ShadowPSSMSplitFactor1" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && pssm;

			pageVideo.Controls[ "ShadowPSSMSplitFactor2" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && pssm;

			pageVideo.Controls[ "ShadowFarDistance" ].Enable =
				!GameEngineApp.ShadowUseMapSettings &&
				GameEngineApp.ShadowTechnique != ShadowTechniques.None;

			pageVideo.Controls[ "ShadowDirectionalLightTextureSize" ].Enable = textureShadows;
			//pageVideo.Controls[ "ShadowDirectionalLightMaxTextureCount" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowSpotLightTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowSpotLightMaxTextureCount" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowPointLightTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowPointLightMaxTextureCount" ].Enable = textureShadows;
		}

		void ChangeVideoMode()
		{
			Vec2I size;
			{
				size = EngineApp.Instance.VideoMode;

				if( comboBoxResolution.SelectedIndex != -1 )
				{
					string s = (string)( comboBoxResolution ).SelectedItem;
					s = s.Replace( "x", " " );
					size = Vec2I.Parse( s );
				}
			}

			EngineApp.Instance.VideoMode = size;
		}

		void UpdateBindedInputControlsTextBox()
		{
			Control pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

			InputDevice inputDevice = comboBoxInputDevices.SelectedItem as InputDevice;

			string text = "";

			foreach( GameControlsManager.GameControlItem item in
				GameControlsManager.Instance.Items )
			{
				string valueStr = "";

				//keys and mouse buttons
				if( inputDevice == null )
				{
					foreach( GameControlsManager.SystemKeyboardMouseValue value in
						item.DefaultKeyboardMouseValues )
					{
						if( valueStr != "" )
							valueStr += ", ";

						switch( value.Type )
						{
						case GameControlsManager.SystemKeyboardMouseValue.Types.Key:
							valueStr += string.Format( "\"{0}\" key", value.Key );
							break;

						case GameControlsManager.SystemKeyboardMouseValue.Types.MouseButton:
							valueStr += string.Format( "\"{0}\" mouse button", value.MouseButton );
							break;
						}
					}
				}

				//joystick
				JoystickInputDevice joystickInputDevice = inputDevice as JoystickInputDevice;
				if( joystickInputDevice != null )
				{
					foreach( GameControlsManager.SystemJoystickValue value in
						item.DefaultJoystickValues )
					{
						if( valueStr != "" )
							valueStr += ", ";

						switch( value.Type )
						{
						case GameControlsManager.SystemJoystickValue.Types.Button:
							if( joystickInputDevice.GetButtonByName( value.Button ) != null )
								valueStr += string.Format( "\"{0}\"", value.Button );
							break;

						case GameControlsManager.SystemJoystickValue.Types.Axis:
							if( joystickInputDevice.GetAxisByName( value.Axis ) != null )
								valueStr += string.Format( "axis \"{0} {1}\"", value.Axis, value.AxisFilter );
							break;

						case GameControlsManager.SystemJoystickValue.Types.POV:
							if( joystickInputDevice.GetPOVByName( value.POV ) != null )
								valueStr += string.Format( "\"{0} {1}\"", value.POV, value.POVDirection );
							break;
						}
					}
				}

				if( valueStr != "" )
					text += string.Format( "{0} - {1}\n", item.ControlKey.ToString(), valueStr );
			}

			pageControls.Controls[ "Controls" ].Text = text;
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

		void tabControl_SelectedIndexChange( TabControl sender )
		{
			lastPageIndex = sender.SelectedIndex;
			UpdatePageButtonsState();
		}

		TextBlock LoadEngineConfig()
		{
			string fileName = VirtualFileSystem.GetRealPathByVirtual( "user:Configs/Engine.config" );
			string error;
			return TextBlockUtils.LoadFromRealFile( fileName, out error );
		}

		void SaveEngineConfig( TextBlock engineConfigBlock )
		{
			string fileName = VirtualFileSystem.GetRealPathByVirtual( "user:Configs/Engine.config" );
			try
			{
				string directoryName = Path.GetDirectoryName( fileName );
				if( directoryName != "" && !Directory.Exists( directoryName ) )
					Directory.CreateDirectory( directoryName );
				using( StreamWriter writer = new StreamWriter( fileName ) )
				{
					writer.Write( engineConfigBlock.DumpToString() );
				}
			}
			catch( Exception e )
			{
				Log.Warning( "Unable to save file \"{0}\". {1}", fileName, e.Message );
			}
		}

		void comboBoxRenderTechnique_SelectedIndexChange( ComboBox sender )
		{
			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock rendererBlock = engineConfigBlock.FindChild( "Renderer" );
			if( rendererBlock == null )
				rendererBlock = engineConfigBlock.AddChild( "Renderer" );
			ComboBoxItem item = (ComboBoxItem)sender.SelectedItem;
			rendererBlock.SetAttribute( "renderTechnique", item.Identifier );
			SaveEngineConfig( engineConfigBlock );

			EnableVideoRestartButton();
		}

		void comboBoxFiltering_SelectedIndexChange( ComboBox sender )
		{
			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock rendererBlock = engineConfigBlock.FindChild( "Renderer" );
			if( rendererBlock == null )
				rendererBlock = engineConfigBlock.AddChild( "Renderer" );
			ComboBoxItem item = (ComboBoxItem)sender.SelectedItem;
			rendererBlock.SetAttribute( "filtering", item.Identifier );
			SaveEngineConfig( engineConfigBlock );

			EnableVideoRestartButton();
		}

		void checkBoxDepthBufferAccess_CheckedChange( CheckBox sender )
		{
			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock rendererBlock = engineConfigBlock.FindChild( "Renderer" );
			if( rendererBlock == null )
				rendererBlock = engineConfigBlock.AddChild( "Renderer" );
			rendererBlock.SetAttribute( "depthBufferAccess", sender.Checked.ToString() );
			SaveEngineConfig( engineConfigBlock );

			EnableVideoRestartButton();

			UpdateComboBoxAntialiasing();
		}

		void comboBoxAntialiasing_SelectedIndexChange( ComboBox sender )
		{
			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock rendererBlock = engineConfigBlock.FindChild( "Renderer" );
			if( rendererBlock == null )
				rendererBlock = engineConfigBlock.AddChild( "Renderer" );
			if( comboBoxAntialiasing.SelectedIndex != -1 )
			{
				ComboBoxItem item = (ComboBoxItem)comboBoxAntialiasing.SelectedItem;
				rendererBlock.SetAttribute( "fullSceneAntialiasing", item.Identifier );
			}
			else
				rendererBlock.DeleteAttribute( "fullSceneAntialiasing" );
			SaveEngineConfig( engineConfigBlock );

			EnableVideoRestartButton();
		}

		void UpdateComboBoxAntialiasing()
		{
			int lastSelectedIndex = comboBoxAntialiasing.SelectedIndex;

			comboBoxAntialiasing.Items.Clear();

			comboBoxAntialiasing.Items.Add( new ComboBoxItem( "RecommendedSetting", Translate( "Recommended setting" ) ) );
			comboBoxAntialiasing.Items.Add( new ComboBoxItem( "0", Translate( "No" ) ) );
			if( !checkBoxDepthBufferAccess.Checked )
			{
				comboBoxAntialiasing.Items.Add( new ComboBoxItem( "2", "2" ) );
				comboBoxAntialiasing.Items.Add( new ComboBoxItem( "4", "4" ) );
				comboBoxAntialiasing.Items.Add( new ComboBoxItem( "6", "6" ) );
				comboBoxAntialiasing.Items.Add( new ComboBoxItem( "8", "8" ) );
			}
			comboBoxAntialiasing.Items.Add( new ComboBoxItem( "FXAA", Translate( "Fast Approximate AA (FXAA)" ) ) );

			if( lastSelectedIndex >= 0 && lastSelectedIndex <= 1 )
				comboBoxAntialiasing.SelectedIndex = lastSelectedIndex;
			else
				comboBoxAntialiasing.SelectedIndex = 0;
		}

		void checkBoxVerticalSync_CheckedChange( CheckBox sender )
		{
			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock rendererBlock = engineConfigBlock.FindChild( "Renderer" );
			if( rendererBlock == null )
				rendererBlock = engineConfigBlock.AddChild( "Renderer" );
			rendererBlock.SetAttribute( "verticalSync", sender.Checked.ToString() );
			SaveEngineConfig( engineConfigBlock );

			EnableVideoRestartButton();
		}

		void comboBoxTextureSkipMipLevels_SelectedIndexChange( ComboBox sender )
		{
			int levels = sender.SelectedIndex;
			if( sender.SelectedIndex < 0 )
				levels = 0;

			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock rendererBlock = engineConfigBlock.FindChild( "Renderer" );
			if( rendererBlock == null )
				rendererBlock = engineConfigBlock.AddChild( "Renderer" );
			rendererBlock.SetAttribute( "textureSkipMipLevels", levels.ToString() );
			SaveEngineConfig( engineConfigBlock );

			EnableVideoRestartButton();

			RendererWorld.InitializationOptions.TextureSkipMipLevels = levels;
		}

		void EnableVideoRestartButton()
		{
			Control pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];
			Button button = (Button)pageVideo.Controls[ "VideoRestart" ];
			button.Enable = true;
		}

		void buttonVideoRestart_Click( Button sender )
		{
			Program.needRestartApplication = true;
			EngineApp.Instance.SetNeedExit();
		}

		void comboBoxLanguage_SelectedIndexChange( ComboBox sender )
		{
			//update Engine.config
			TextBlock engineConfigBlock = LoadEngineConfig();
			if( engineConfigBlock == null )
				engineConfigBlock = new TextBlock();
			TextBlock localizationBlock = engineConfigBlock.FindChild( "Localization" );
			if( localizationBlock == null )
				localizationBlock = engineConfigBlock.AddChild( "Localization" );
			ComboBoxItem item = (ComboBoxItem)sender.SelectedItem;
			localizationBlock.SetAttribute( "language", item.Identifier );
			SaveEngineConfig( engineConfigBlock );

			EnableLanguageRestartButton();
		}

		void EnableLanguageRestartButton()
		{
			Control pageLanguage = window.Controls[ "TabControl" ].Controls[ "Language" ];
			Button button = (Button)pageLanguage.Controls[ "LanguageRestart" ];
			button.Enable = true;
		}

		void buttonLanguageRestart_Click( Button sender )
		{
			Program.needRestartApplication = true;
			EngineApp.Instance.SetNeedExit();
		}

		string Translate( string text )
		{
			return LanguageManager.Instance.Translate( "UISystem", text );
		}

		void pageButton_Click( Button sender )
		{
			int index = Array.IndexOf( pageButtons, sender );
			tabControl.SelectedIndex = index;
		}

		void UpdatePageButtonsState()
		{
			for( int n = 0; n < pageButtons.Length; n++ )
			{
				Button button = pageButtons[ n ];
				button.Active = tabControl.SelectedIndex == n;
			}
		}
	}
}
