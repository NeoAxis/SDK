// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.SoundSystem;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a main menu.
	/// </summary>
	public class MainMenuWindow : Control
	{
		static MainMenuWindow instance;

		Control window;
		//TextBox versionTextBox;

		Map mapInstance;

		[Config( "MainMenu", "showBackgroundMap" )]
		static bool showBackgroundMap = true;

		///////////////////////////////////////////

		public static MainMenuWindow Instance
		{
			get { return instance; }
		}

		/// <summary>
		/// Creates a window of the main menu and creates the background world.
		/// </summary>
		protected override void OnAttach()
		{
			instance = this;
			base.OnAttach();

			//for showBackgroundMap field.
			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			//create main menu window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MainMenuWindow.gui" );

			window.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );
			Controls.Add( window );

			//no shader model 3 warning
			if( window.Controls[ "NoShaderModel3" ] != null )
				window.Controls[ "NoShaderModel3" ].Visible = !RenderSystem.Instance.HasShaderModel3();

			//button handlers
			if( window.Controls[ "Run" ] != null )
				( (Button)window.Controls[ "Run" ] ).Click += Run_Click;
			if( window.Controls[ "Multiplayer" ] != null )
				( (Button)window.Controls[ "Multiplayer" ] ).Click += Multiplayer_Click;
			if( window.Controls[ "Maps" ] != null )
				( (Button)window.Controls[ "Maps" ] ).Click += Maps_Click;
			if( window.Controls[ "LoadSave" ] != null )
				( (Button)window.Controls[ "LoadSave" ] ).Click += LoadSave_Click;
			if( window.Controls[ "Options" ] != null )
				( (Button)window.Controls[ "Options" ] ).Click += Options_Click;
			if( window.Controls[ "MultiView" ] != null )
				( (Button)window.Controls[ "MultiView" ] ).Click += MultiView_Click;
			if( window.Controls[ "ProfilingTool" ] != null )
				( (Button)window.Controls[ "ProfilingTool" ] ).Click += ProfilingTool_Click;
			if( window.Controls[ "GUISamples1" ] != null )
				( (Button)window.Controls[ "GUISamples1" ] ).Click += GUISamples1_Click;
			if( window.Controls[ "GUISamples2" ] != null )
				( (Button)window.Controls[ "GUISamples2" ] ).Click += GUISamples2_Click;
			if( window.Controls[ "About" ] != null )
				( (Button)window.Controls[ "About" ] ).Click += About_Click;
			if( window.Controls[ "Exit" ] != null )
				( (Button)window.Controls[ "Exit" ] ).Click += Exit_Click;

			////add version info control
			//versionTextBox = new TextBox();
			//versionTextBox.TextHorizontalAlign = HorizontalAlign.Left;
			//versionTextBox.TextVerticalAlign = VerticalAlign.Bottom;
			//versionTextBox.Text = "Version " + EngineVersionInformation.Version;
			//versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );
			//Controls.Add( versionTextBox );

			//showBackgroundMap check box
			CheckBox checkBox = (CheckBox)window.Controls[ "ShowBackgroundMap" ];
			if( checkBox != null )
			{
				checkBox.Checked = showBackgroundMap;
				checkBox.Click += checkBoxShowBackgroundMap_Click;
			}

			//play background music
			GameMusic.MusicPlay( "Sounds\\Music\\Game.ogg", true );
			//GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//create the background world
			if( showBackgroundMap )
				CreateMap();

			ResetTime();
		}

		void checkBoxShowBackgroundMap_Click( CheckBox sender )
		{
			showBackgroundMap = sender.Checked;

			if( showBackgroundMap )
				CreateMap();
			else
				DestroyMap();
		}

		void Run_Click( Button sender )
		{
			GameEngineApp.Instance.SetNeedMapLoad( "Maps\\MainDemo\\Map.map" );
		}

		void RunVillageDemo_Click( Button sender )
		{
			GameEngineApp.Instance.SetNeedMapLoad( "Maps\\VillageDemo\\Map\\Map.map" );
		}

		void Multiplayer_Click( Button sender )
		{
			Controls.Add( new MultiplayerLoginWindow() );
		}

		void Maps_Click( Button sender )
		{
			Controls.Add( new MapsWindow() );
		}

		void LoadSave_Click( Button sender )
		{
			Controls.Add( new WorldLoadSaveWindow() );
		}

		void Options_Click( Button sender )
		{
			Controls.Add( new OptionsWindow() );
		}

		void ProfilingTool_Click( Button sender )
		{
			if( ProfilingToolWindow.Instance == null )
				Controls.Add( new ProfilingToolWindow() );
		}

		void GUISamples1_Click( Button sender )
		{
			GameEngineApp.Instance.ControlManager.Controls.Add( new GUISamples1Window() );
		}

		void GUISamples2_Click( Button sender )
		{
			GameEngineApp.Instance.ControlManager.Controls.Add( new GUISamples2Window() );
		}

		void MultiView_Click( Button sender )
		{
			Controls.Add( new MultiViewConfigurationWindow() );
		}

		void About_Click( Button sender )
		{
			GameEngineApp.Instance.ControlManager.Controls.Add( new AboutWindow() );
		}

		void Exit_Click( Button sender )
		{
			GameEngineApp.Instance.SetFadeOutScreenAndExit();
		}

		/// <summary>
		/// Destroys the background world at closing the main menu.
		/// </summary>
		protected override void OnDetach()
		{
			//destroy the background world
			DestroyMap();

			base.OnDetach();
			instance = null;
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			//if( e.Key == EKeys.Escape )
			//{
			//   Controls.Add( new MenuWindow() );
			//   return true;
			//}

			return false;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//Change window transparency
			{
				float alpha = 0;

				if( Time > 3 && Time <= 5 )
					alpha = ( Time - 3 ) / 2;
				else if( Time > 4 )
					alpha = 1;

				window.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
				//versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
			}

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//Tick a background world
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.Tick();
		}

		protected override void OnRender()
		{
			base.OnRender();

			//Update camera orientation
			if( Map.Instance != null )
			{
				Vec3 from = Vec3.Zero;
				Vec3 to = new Vec3( 1, 0, 0 );
				float fov = 80;

				MapCamera mapCamera = Entities.Instance.GetByName( "MapCamera_MainMenu" ) as MapCamera;
				if( mapCamera != null )
				{
					from = mapCamera.Position;
					to = from + mapCamera.Rotation.GetForward();
					if( mapCamera.Fov != 0 )
						fov = mapCamera.Fov;
				}

				Camera camera = RendererWorld.Instance.DefaultCamera;
				camera.NearClipDistance = Map.Instance.NearFarClipDistance.Minimum;
				camera.FarClipDistance = Map.Instance.NearFarClipDistance.Maximum;
				camera.FixedUp = Vec3.ZAxis;
				camera.Fov = fov;
				camera.Position = from;
				camera.LookAt( to );

				//update game specific options
				{
					//water reflection level
					foreach( WaterPlane waterPlane in WaterPlane.Instances )
						waterPlane.ReflectionLevel = GameEngineApp.WaterReflectionLevel;

					//decorative objects
					if( DecorativeObjectManager.Instance != null )
						DecorativeObjectManager.Instance.Visible = GameEngineApp.ShowDecorativeObjects;

					//HeightmapTerrain
					//enable simple rendering for Low material scheme.
					foreach( HeightmapTerrain terrain in HeightmapTerrain.Instances )
						terrain.SimpleRendering = GameEngineApp.MaterialScheme == MaterialSchemes.Low;
				}

			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( Map.Instance == null )
			{
				renderer.AddQuad( new Rect( 0, 0, 1, 1 ),
					new ColorValue( .2f, .2f, .2f ) * window.ColorMultiplier );
			}
		}

		/// <summary>
		/// Creates the background world.
		/// </summary>
		void CreateMap()
		{
			DestroyMap();

			string mapName = "Maps\\MainMenu\\Map.map";

			if( VirtualFile.Exists( mapName ) )
			{
				WorldType worldType = EntityTypes.Instance.GetByName( "SimpleWorld" ) as WorldType;
				if( worldType == null )
					Log.Fatal( "MainMenuWindow: CreateMap: \"SimpleWorld\" type is not exists." );

				if( GameEngineApp.Instance.ServerOrSingle_MapLoad( mapName, worldType, true ) )
				{
					mapInstance = Map.Instance;
					EntitySystemWorld.Instance.Simulation = true;
				}
			}
		}

		/// <summary>
		/// Destroys the background world.
		/// </summary>
		void DestroyMap()
		{
			if( mapInstance == Map.Instance )
			{
				MapSystemWorld.MapDestroy();
				EntitySystemWorld.Instance.WorldDestroy();
			}
		}
	}
}
