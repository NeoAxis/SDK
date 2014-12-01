// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.FileSystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.SoundSystem;
using Engine.PhysicsSystem;
using Engine.Utils;
using Engine.Networking;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game application.
	/// </summary>
	public class GameEngineApp : EngineApp
	{
		static GameEngineApp instance;

		//fading out functionality. Using for loading maps and for exit application.
		const float fadingTime = 1;
		float fadingOutTimer;

		//load map or world
		string needMapLoadName;
		bool needRunExampleOfProceduralMapCreation;
		string needWorldLoadName;

		//application exit screen fading
		bool needFadingOutAndExit;

		//fading in
		float fadingInRemainingTime;
		int fadingInSkipFirstFrames;

		//for network client
		bool client_AllowCheckForDisconnection = true;
		Control client_mapLoadingWindow;
		bool client_SubscribedToMapLoadingEvents;

		ScreenControlManager controlManager;

		//

		static float gamma = 1;
		[Config( "Video", "gamma" )]
		public static float _Gamma
		{
			get { return gamma; }
			set
			{
				gamma = value;
				EngineApp.Instance.Gamma = gamma;
			}
		}

		static bool showSystemCursor = true;
		[Config( "Video", "showSystemCursor" )]
		public static bool _ShowSystemCursor
		{
			get { return showSystemCursor; }
			set
			{
				showSystemCursor = value;

				EngineApp.Instance.ShowSystemCursor = value;

				if( EngineApp.Instance.ShowSystemCursor )
				{
					if( Instance != null && Instance.ControlManager != null )
						Instance.ControlManager.DefaultCursor = null;
				}
				else
				{
					string cursorName = "GUI\\Cursors\\Default.png";
					if( !VirtualFile.Exists( cursorName ) )
						cursorName = null;
					if( Instance != null && Instance.ControlManager != null )
						Instance.ControlManager.DefaultCursor = cursorName;
					if( cursorName == null )
						EngineApp.Instance.ShowSystemCursor = true;
				}
			}
		}

		static bool drawFPS;
		[Config( "Video", "drawFPS" )]
		public static bool _DrawFPS
		{
			get { return drawFPS; }
			set
			{
				drawFPS = value;
				EngineApp.Instance.ShowFPS = value;
			}
		}

		static MaterialSchemes materialScheme = MaterialSchemes.Default;
		[Config( "Video", "materialScheme" )]
		public static MaterialSchemes MaterialScheme
		{
			get { return materialScheme; }
			set
			{
				materialScheme = value;
				if( RendererWorld.Instance != null )
					RendererWorld.Instance.DefaultViewport.MaterialScheme = materialScheme.ToString();
			}
		}

		static ShadowTechniques shadowTechnique = ShadowTechniques.ShadowmapHigh;
		[Config( "Video", "shadowTechnique" )]
		public static ShadowTechniques ShadowTechnique
		{
			get { return shadowTechnique; }
			set { shadowTechnique = value; }
		}

		//this options affect to shadowColor and shadowFarDistance
		static bool shadowUseMapSettings = true;
		[Config( "Video", "shadowUseMapSettings" )]
		public static bool ShadowUseMapSettings
		{
			get { return shadowUseMapSettings; }
			set { shadowUseMapSettings = value; }
		}

		static ColorValue shadowColor = new ColorValue( .75f, .75f, .75f );
		[Config( "Video", "shadowColor" )]
		public static ColorValue ShadowColor
		{
			get { return shadowColor; }
			set { shadowColor = value; }
		}

		static float shadowFarDistance = 50;
		[Config( "Video", "shadowFarDistance" )]
		public static float ShadowFarDistance
		{
			get { return shadowFarDistance; }
			set { shadowFarDistance = value; }
		}

		static Vec2 shadowPSSMSplitFactors = new Vec2( .1f, .4f );
		[Config( "Video", "shadowPSSMSplitFactors" )]
		public static Vec2 ShadowPSSMSplitFactors
		{
			get { return shadowPSSMSplitFactors; }
			set { shadowPSSMSplitFactors = value; }
		}

		static int shadowDirectionalLightTextureSize = 2048;
		[Config( "Video", "shadowDirectionalLightTextureSize" )]
		public static int ShadowDirectionalLightTextureSize
		{
			get { return shadowDirectionalLightTextureSize; }
			set { shadowDirectionalLightTextureSize = value; }
		}

		static int shadowDirectionalLightMaxTextureCount = 1;
		[Config( "Video", "shadowDirectionalLightMaxTextureCount" )]
		public static int ShadowDirectionalLightMaxTextureCount
		{
			get { return shadowDirectionalLightMaxTextureCount; }
			set { shadowDirectionalLightMaxTextureCount = value; }
		}

		static int shadowSpotLightTextureSize = 2048;
		[Config( "Video", "shadowSpotLightTextureSize" )]
		public static int ShadowSpotLightTextureSize
		{
			get { return shadowSpotLightTextureSize; }
			set { shadowSpotLightTextureSize = value; }
		}

		static int shadowSpotLightMaxTextureCount = 2;
		[Config( "Video", "shadowSpotLightMaxTextureCount" )]
		public static int ShadowSpotLightMaxTextureCount
		{
			get { return shadowSpotLightMaxTextureCount; }
			set { shadowSpotLightMaxTextureCount = value; }
		}

		static int shadowPointLightTextureSize = 1024;
		[Config( "Video", "shadowPointLightTextureSize" )]
		public static int ShadowPointLightTextureSize
		{
			get { return shadowPointLightTextureSize; }
			set { shadowPointLightTextureSize = value; }
		}

		static int shadowPointLightMaxTextureCount = 2;
		[Config( "Video", "shadowPointLightMaxTextureCount" )]
		public static int ShadowPointLightMaxTextureCount
		{
			get { return shadowPointLightMaxTextureCount; }
			set { shadowPointLightMaxTextureCount = value; }
		}

		static WaterPlane.ReflectionLevels waterReflectionLevel = WaterPlane.ReflectionLevels.OnlyModels;
		[Config( "Video", "waterReflectionLevel" )]
		public static WaterPlane.ReflectionLevels WaterReflectionLevel
		{
			get { return waterReflectionLevel; }
			set { waterReflectionLevel = value; }
		}

		static bool showDecorativeObjects = true;
		[Config( "Video", "showDecorativeObjects" )]
		public static bool ShowDecorativeObjects
		{
			get { return showDecorativeObjects; }
			set { showDecorativeObjects = value; }
		}

		static float soundVolume = .8f;
		[Config( "Sound", "soundVolume" )]
		public static float SoundVolume
		{
			get { return soundVolume; }
			set
			{
				soundVolume = value;
				if( EngineApp.Instance.DefaultSoundChannelGroup != null )
					EngineApp.Instance.DefaultSoundChannelGroup.Volume = soundVolume;
			}
		}

		static float musicVolume = .4f;
		[Config( "Sound", "musicVolume" )]
		public static float MusicVolume
		{
			get { return musicVolume; }
			set
			{
				musicVolume = value;
				if( GameMusic.MusicChannelGroup != null )
					GameMusic.MusicChannelGroup.Volume = musicVolume;
			}
		}

		[Config( "Environment", "autorunMapName" )]
		public static string autorunMapName = "";

		//screenMessages
		class ScreenMessage
		{
			public string text;
			public float timeRemaining;
		}
		List<ScreenMessage> screenMessages = new List<ScreenMessage>();

		//

		public GameEngineApp()
			: base( ApplicationTypes.Simulation )
		{
		}

		public static new GameEngineApp Instance
		{
			get { return instance; }
		}

		void ChangeToBetterDefaultSettings()
		{
			bool shadowTechniqueInitialized = false;
			bool shadowTextureSizeInitialized = false;

			if( !string.IsNullOrEmpty( EngineApp.ConfigName ) )
			{
				string error;
				TextBlock block = TextBlockUtils.LoadFromRealFile(
					VirtualFileSystem.GetRealPathByVirtual( EngineApp.ConfigName ), out error );
				if( block != null )
				{
					TextBlock blockVideo = block.FindChild( "Video" );
					if( blockVideo != null )
					{
						if( blockVideo.IsAttributeExist( "shadowTechnique" ) )
							shadowTechniqueInitialized = true;
						if( blockVideo.IsAttributeExist( "shadowDirectionalLightTextureSize" ) )
							shadowTextureSizeInitialized = true;
					}
				}
			}

			//shadowTechnique
			if( !shadowTechniqueInitialized )
			{
				//configure optimal settings for this computer
				if( RenderSystem.Instance.HasShaderModel3() && RenderSystem.Instance.Capabilities.HardwareRenderToTexture )
				{
					shadowTechnique = ShadowTechniques.ShadowmapHigh;

					//if( RenderSystem.Instance.GPUIsGeForce() )
					//{
					//   if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
					//      RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV40 )
					//   {
					//      shadowTechnique = ShadowTechniques.ShadowmapLow;
					//   }
					//   if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.GeForce_G70 )
					//      shadowTechnique = ShadowTechniques.ShadowmapMedium;
					//}

					//if( RenderSystem.Instance.GPUIsRadeon() )
					//{
					//   if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
					//      RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
					//   {
					//      shadowTechnique = ShadowTechniques.ShadowmapLow;
					//   }
					//   if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Radeon_R500 )
					//      shadowTechnique = ShadowTechniques.ShadowmapMedium;
					//}

					//if( RenderSystem.Instance.GPUIsIntel() )
					//{
					//   if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Intel_HDGraphics )
					//      shadowTechnique = ShadowTechniques.ShadowmapHigh;
					//}
				}
				else
					shadowTechnique = ShadowTechniques.None;
			}

			//shadow texture size
			if( !shadowTextureSizeInitialized )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_G70 )
					{
						shadowDirectionalLightTextureSize = 1024;
						shadowSpotLightTextureSize = 1024;
						shadowPointLightTextureSize = 512;
					}
				}
				else if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R500 )
					{
						shadowDirectionalLightTextureSize = 1024;
						shadowSpotLightTextureSize = 1024;
						shadowPointLightTextureSize = 512;
					}
				}
				else if( RenderSystem.Instance.GPUIsIntel() )
				{
					if( RenderSystem.Instance.GPUCodeName != GPUCodeNames.Intel_HDGraphics )
					{
						shadowDirectionalLightTextureSize = 1024;
						shadowSpotLightTextureSize = 1024;
						shadowPointLightTextureSize = 512;
					}
				}
				else
				{
					shadowDirectionalLightTextureSize = 1024;
					shadowSpotLightTextureSize = 1024;
					shadowPointLightTextureSize = 512;
				}
			}
		}

		float loadingCallbackLastTimeCall;
		void LongOperationCallbackManager_LoadingCallback( string callerInfo, object userData )
		{
			//How to calculate time for progress bar.
			//It is impossible to make universal solution. It's depending to concrete project.
			//By "callerInfo" data you can collect useful info for calculation total loading time.

			//Limit fps.
			const float maxFPSInv = 1.0f / 15.0f;
			float now = Time;
			if( now - loadingCallbackLastTimeCall < maxFPSInv )
				return;
			loadingCallbackLastTimeCall = now;

			//animate "Indicator".
			Control loadingWindow = userData as Control;
			if( loadingWindow != null )
			{
				Control indicator = loadingWindow.Controls[ "Indicator" ];
				if( indicator != null )
				{
					int frame = (int)( ( EngineApp.Instance.Time * 20 ) % 8 );
					indicator.BackTextureCoord =
						new Rect( (float)frame / 8, 0, (float)( frame + 1 ) / 8, 1 );
				}
			}

			//Update frame (2D only).
			SceneManager.Instance.Enable3DSceneRendering = false;
			RenderScene();
			SceneManager.Instance.Enable3DSceneRendering = true;
		}

		protected override void OnBeforeRendererWorldInit()
		{
			base.OnBeforeRendererWorldInit();

			//here you can set default settings like:
			//RendererWorld.InitializationOptions.FullSceneAntialiasing.
		}

		protected override void OnAfterRendererWorldInit()
		{
			base.OnAfterRendererWorldInit();

			//We will load materials later (in OnCreate()), during showing "ProgramLoadingWindow.gui".
			HighLevelMaterialManager.Instance.NeedLoadAllMaterialsAtStartup = false;
		}

		protected override bool OnCreate()
		{
			instance = this;

			ChangeToBetterDefaultSettings();

			if( !base.OnCreate() )
				return false;

			SoundVolume = soundVolume;
			MusicVolume = musicVolume;

			controlManager = new ScreenControlManager( ScreenGuiRenderer );
			if( !ControlsWorld.Init() )
				return false;

			_ShowSystemCursor = _ShowSystemCursor;
			_DrawFPS = _DrawFPS;
			MaterialScheme = materialScheme;

			Log.Handlers.InvisibleInfoHandler += InvisibleLog_Handlers_InfoHandler;
			Log.Handlers.InfoHandler += Log_Handlers_InfoHandler;
			Log.Handlers.WarningHandler += Log_Handlers_WarningHandler;
			Log.Handlers.ErrorHandler += Log_Handlers_ErrorHandler;
			Log.Handlers.FatalHandler += Log_Handlers_FatalHandler;

			//Camera
			Camera camera = RendererWorld.Instance.DefaultCamera;
			camera.NearClipDistance = .1f;
			camera.FarClipDistance = 1000.0f;
			camera.FixedUp = Vec3.ZAxis;
			camera.Fov = 90;
			camera.Position = new Vec3( -10, -10, 10 );
			camera.LookAt( new Vec3( 0, 0, 0 ) );

			Control programLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\ProgramLoadingWindow.gui" );
			if( programLoadingWindow != null )
				controlManager.Controls.Add( programLoadingWindow );

			//Subcribe to callbacks during engine loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
				programLoadingWindow );

			//load materials.
			if( !HighLevelMaterialManager.Instance.NeedLoadAllMaterialsAtStartup )
			{
				//prevent double initialization of materials after startup by means CreateEmptyMaterialsForFasterStartupInitialization = true.
				ShaderBaseMaterial.CreateEmptyMaterialsForFasterStartupInitialization = true;
				if( !HighLevelMaterialManager.Instance.LoadAllMaterials() )
				{
					LongOperationCallbackManager.Unsubscribe();
					return true;
				}
				ShaderBaseMaterial.CreateEmptyMaterialsForFasterStartupInitialization = false;
			}

			RenderScene();

			//Game controls
			GameControlsManager.Init();

			//EntitySystem
			if( !EntitySystemWorld.Init( new EntitySystemWorld() ) )
			{
				LongOperationCallbackManager.Unsubscribe();
				return true;
			}

			//load autorun map
			string mapName = GetAutorunMapName();
			bool mapLoadingFailed = false;
			if( mapName != "" )
			{
				//hide loading window.
				LongOperationCallbackManager.Unsubscribe();
				if( programLoadingWindow != null )
					programLoadingWindow.SetShouldDetach();

				if( !ServerOrSingle_MapLoad( mapName, EntitySystemWorld.Instance.DefaultWorldType, false ) )
					mapLoadingFailed = true;
			}

			//finish initialization of materials and hide loading window.
			ShaderBaseMaterial.FinishInitializationOfEmptyMaterials();
			LongOperationCallbackManager.Unsubscribe();
			if( programLoadingWindow != null )
				programLoadingWindow.SetShouldDetach();

			//if no autorun map play music and go to EngineLogoWindow.
			if( Map.Instance == null && !mapLoadingFailed )
			{
				GameMusic.MusicPlay( "Sounds\\Music\\Game.ogg", true );
				//GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );
				controlManager.Controls.Add( new EngineLogoWindow() );
			}

			//register "showProfilingTool" console command
			if( EngineConsole.Instance != null )
				EngineConsole.Instance.AddCommand( "showProfilingTool", ConsoleCommand_ShowProfilingTool );

			//example of custom input device
			//ExampleCustomInputDevice.InitDevice();

			return true;
		}

		string GetAutorunMapName()
		{
			string mapName = "";

			if( autorunMapName != "" && autorunMapName.Length > 2 )
			{
				mapName = autorunMapName;
				if( !mapName.Contains( "\\" ) && !mapName.Contains( "/" ) )
					mapName = "Maps/" + mapName + "/Map.map";
			}

			if( PlatformInfo.Platform != PlatformInfo.Platforms.Android )
			{
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				if( commandLineArgs.Length > 1 )
				{
					string name = commandLineArgs[ 1 ];
					if( name[ 0 ] == '\"' && name[ name.Length - 1 ] == '\"' )
						name = name.Substring( 1, name.Length - 2 );
					name = name.Replace( '/', '\\' );

					string dataDirectory = VirtualFileSystem.ResourceDirectoryPath;
					dataDirectory = dataDirectory.Replace( '/', '\\' );

					if( name.Length > dataDirectory.Length )
						if( string.Compare( name.Substring( 0, dataDirectory.Length ), dataDirectory, true ) == 0 )
							name = name.Substring( dataDirectory.Length + 1 );

					mapName = name;
				}
			}

			return mapName;
		}

		protected override void OnDestroy()
		{
			MultiViewRenderingManager.Shutdown();

			MapSystemWorld.MapDestroy();
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();

			Server_DestroyServer( "The server has been destroyed" );
			Client_DisconnectFromServer();

			EntitySystemWorld.Shutdown();

			GameControlsManager.Shutdown();

			ControlsWorld.Shutdown();
			controlManager = null;

			EngineConsole.Shutdown();

			instance = null;
			base.OnDestroy();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			//Engine console
			if( EngineConsole.Instance != null )
				if( EngineConsole.Instance.DoKeyDown( e ) )
					return true;

			//Profiling Tool
			if( ( PlatformInfo.Platform == PlatformInfo.Platforms.Windows && e.Key == EKeys.F11 ) ||
				( PlatformInfo.Platform == PlatformInfo.Platforms.MacOSX && e.Key == EKeys.F5 ) )
			{
				bool show = ProfilingToolWindow.Instance == null;
				ShowProfilingTool( show );
				return true;
			}
			if( e.Key == EKeys.Escape )
			{
				if( ProfilingToolWindow.Instance != null && !ProfilingToolWindow.Instance.Background & !MouseRelativeMode )
				{
					ProfilingToolWindow.Instance.SetShouldDetach();
					return true;
				}
			}

			//UI controls
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoKeyDown( e ) )
					return true;

			//make screenshot
			if( ( PlatformInfo.Platform == PlatformInfo.Platforms.Windows && e.Key == EKeys.F12 ) ||
				( PlatformInfo.Platform == PlatformInfo.Platforms.MacOSX && e.Key == EKeys.F6 ) )
			{
				MakeScreenshot();
				return true;
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnKeyPress( KeyPressEvent e )
		{
			if( EngineConsole.Instance != null )
				if( EngineConsole.Instance.DoKeyPress( e ) )
					return true;
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoKeyPress( e ) )
					return true;

			return base.OnKeyPress( e );
		}

		protected override bool OnKeyUp( KeyEvent e )
		{
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoKeyUp( e ) )
					return true;
			return base.OnKeyUp( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoMouseDown( button ) )
					return true;
			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoMouseUp( button ) )
					return true;
			return base.OnMouseUp( button );
		}

		protected override bool OnMouseDoubleClick( EMouseButtons button )
		{
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoMouseDoubleClick( button ) )
					return true;
			return base.OnMouseDoubleClick( button );
		}

		protected override void OnMouseMove( Vec2 mouse )
		{
			base.OnMouseMove( mouse );
			if( controlManager != null && !IsScreenFadingOut() )
				controlManager.DoMouseMove( mouse );
		}

		protected override bool OnMouseWheel( int delta )
		{
			if( EngineConsole.Instance != null )
				if( EngineConsole.Instance.DoMouseWheel( delta ) )
					return true;
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoMouseWheel( delta ) )
					return true;
			return base.OnMouseWheel( delta );
		}

		protected override bool OnJoystickEvent( JoystickInputEvent e )
		{
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoJoystickEvent( e ) )
					return true;
			return base.OnJoystickEvent( e );
		}

		protected override bool OnCustomInputDeviceEvent( InputEvent e )
		{
			if( controlManager != null && !IsScreenFadingOut() )
				if( controlManager.DoCustomInputDeviceEvent( e ) )
					return true;
			return base.OnCustomInputDeviceEvent( e );
		}

		protected override void OnSystemPause( bool pause )
		{
			base.OnSystemPause( pause );

			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.SystemPauseOfSimulation = pause;
		}

		bool IsScreenFadingOut()
		{
			if( needMapLoadName != null || needRunExampleOfProceduralMapCreation || needWorldLoadName != null )
				return true;
			if( needFadingOutAndExit )
				return true;
			return false;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//need load map or world?
			if( needMapLoadName != null || needRunExampleOfProceduralMapCreation || needWorldLoadName != null )
			{
				if( fadingOutTimer > 0 )
				{
					fadingOutTimer -= delta;
					if( fadingOutTimer < 0 )
						fadingOutTimer = 0;
				}

				if( fadingOutTimer == 0 )
				{
					//close all windows
					foreach( Control control in controlManager.Controls )
						control.SetShouldDetach();

					if( needMapLoadName != null )
					{
						string name = needMapLoadName;
						needMapLoadName = null;
						ServerOrSingle_MapLoad( name, EntitySystemWorld.Instance.DefaultWorldType, false );
					}
					else if( needRunExampleOfProceduralMapCreation )
					{
						needRunExampleOfProceduralMapCreation = false;
						ExampleOfProceduralMapCreation.ServerOrSingle_MapCreate();
					}
					else if( needWorldLoadName != null )
					{
						string name = needWorldLoadName;
						needWorldLoadName = null;
						WorldLoad( name );
					}
				}
			}

			//exit application fading out
			if( needFadingOutAndExit )
			{
				if( fadingOutTimer > 0 )
				{
					fadingOutTimer -= delta;
					if( fadingOutTimer < 0 )
						fadingOutTimer = 0;
				}

				if( fadingOutTimer == 0 )
				{
					//Now application must be closed.
					SetNeedExit();

					return;
				}
			}

			if( EngineConsole.Instance != null )
				EngineConsole.Instance.DoTick( delta );
			controlManager.DoTick( delta );

			//update server
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.Update();

			//update client
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
			{
				client.Update();

				//check for disconnection
				if( client_AllowCheckForDisconnection )
				{
					if( client.Status == NetworkConnectionStatuses.Disconnected )
					{
						Client_DisconnectFromServer();

						Log.Error( "Disconnected from server.\n\nReason: \"{0}\"",
							client.DisconnectionReason );
					}
				}
			}

			//screenMessages
			{
				for( int n = 0; n < screenMessages.Count; n++ )
				{
					screenMessages[ n ].timeRemaining -= delta;
					if( screenMessages[ n ].timeRemaining <= 0 )
					{
						screenMessages.RemoveAt( n );
						n--;
					}
				}
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			SystemCursorFileName = "GUI\\Cursors\\DefaultSystem.cur";
			controlManager.DoRender();
		}

		protected override void OnRenderScreenUI( GuiRenderer renderer )
		{
			base.OnRenderScreenUI( renderer );

			if( Map.Instance != null )
				Map.Instance.DoRenderUI( renderer );

			if( MultiViewRenderingManager.Instance != null )
				MultiViewRenderingManager.Instance.RenderScreenUI( renderer );

			controlManager.DoRenderUI( renderer );

			//screenMessages
			{
				Viewport viewport = RendererWorld.Instance.DefaultViewport;
				Vec2 shadowOffset = 2.0f / viewport.DimensionsInPixels.Size.ToVec2();

				Vec2 pos = new Vec2( .03f, .75f );

				for( int n = screenMessages.Count - 1; n >= 0; n-- )
				{
					ScreenMessage message = screenMessages[ n ];

					float alpha = message.timeRemaining;
					if( alpha > 1 )
						alpha = 1;
					renderer.AddText( message.text, pos + shadowOffset, HorizontalAlign.Left,
						VerticalAlign.Bottom, new ColorValue( 0, 0, 0, alpha / 2 ) );
					renderer.AddText( message.text, pos, HorizontalAlign.Left, VerticalAlign.Bottom,
						new ColorValue( 1, 1, 1, alpha ) );

					pos.Y -= renderer.DefaultFont.Height;
				}
			}

			//fading in, out
			RenderFadingOut( renderer );
			RenderFadingIn( renderer );

			if( EngineConsole.Instance != null )
				EngineConsole.Instance.DoRenderUI();
		}

		void RenderFadingOut( GuiRenderer renderer )
		{
			if( IsScreenFadingOut() )
			{
				if( fadingOutTimer != 0 )
				{
					float alpha = 1.0f - fadingOutTimer / fadingTime;
					MathFunctions.Saturate( ref alpha );
					renderer.AddQuad( new Rect( 0, 0, 1, 1 ), new ColorValue( 0, 0, 0, alpha ) );
				}
			}
		}

		void RenderFadingIn( GuiRenderer renderer )
		{
			if( fadingInRemainingTime > 0 )
			{
				//we are skip some amount of frames because resources can be loaded during it.
				if( fadingInSkipFirstFrames == 0 )
				{
					fadingInRemainingTime -= RendererWorld.Instance.FrameRenderTimeStep;
					if( fadingInRemainingTime < 0 )
						fadingInRemainingTime = 0;
				}
				else
					fadingInSkipFirstFrames--;

				float alpha = (float)fadingInRemainingTime / 1;
				MathFunctions.Saturate( ref alpha );
				renderer.AddQuad( new Rect( 0, 0, 1, 1 ), new ColorValue( 0, 0, 0, alpha ) );
			}
		}

		public void CreateGameWindowForMap()
		{
			//close all windows
			foreach( Control control in controlManager.Controls )
				control.SetShouldDetach();

			GameWindow gameWindow = null;

			//Create specific game window
			if( GameMap.Instance != null )
				gameWindow = CreateGameWindowByGameType( GameMap.Instance.GameType );

			if( gameWindow == null )
				gameWindow = new ActionGameWindow();

			controlManager.Controls.Add( gameWindow );
		}

		public void DeleteAllGameWindows()
		{
			ttt:
			foreach( Control control in controlManager.Controls )
			{
				if( control is GameWindow )
				{
					controlManager.Controls.Remove( control );
					goto ttt;
				}
			}
		}

		public bool ServerOrSingle_MapLoad( string fileName, WorldType worldType,
			bool noChangeWindows )
		{
			GameNetworkServer server = GameNetworkServer.Instance;

			Control mapLoadingWindow = null;

			//show map loading window
			if( !noChangeWindows )
			{
				string mapDirectory = Path.GetDirectoryName( fileName );
				string guiPath = Path.Combine( mapDirectory, "Description\\MapLoadingWindow.gui" );
				if( !VirtualFile.Exists( guiPath ) )
					guiPath = "Gui\\MapLoadingWindow.gui";
				mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl( guiPath );
				if( mapLoadingWindow != null )
				{
					mapLoadingWindow.Text = fileName;
					controlManager.Controls.Add( mapLoadingWindow );
				}
			}

			DeleteAllGameWindows();

			bool mapWasDestroyed = Map.Instance != null;

			MapSystemWorld.MapDestroy();

			if( server != null )
			{
				//destroy world for server
				if( EntitySystemWorld.Instance != null )
					EntitySystemWorld.Instance.WorldDestroy();

				if( mapWasDestroyed )
					server.EntitySystemService.WorldWasDestroyed( true );
			}

			//update sound listener
			if( SoundWorld.Instance != null )
				SoundWorld.Instance.SetListener( new Vec3( 10000, 10000, 10000 ), Vec3.Zero, Vec3.XAxis, Vec3.ZAxis );

			if( !noChangeWindows )
				RenderScene();

			//unload all reloadable textures
			TextureManager.Instance.UnloadAll( true, false );

			//create world if need
			if( World.Instance == null || World.Instance.Type != worldType )
			{
				WorldSimulationTypes worldSimulationType;
				EntitySystemWorld.NetworkingInterface networkingInterface = null;

				if( server != null )
				{
					worldSimulationType = WorldSimulationTypes.ServerAndClient;
					networkingInterface = server.EntitySystemService.NetworkingInterface;
				}
				else
					worldSimulationType = WorldSimulationTypes.Single;

				if( !EntitySystemWorld.Instance.WorldCreate( worldSimulationType, worldType,
					networkingInterface ) )
				{
					Log.Fatal( "GameEngineApp: MapLoad: EntitySystemWorld.WorldCreate failed." );
				}
			}

			//Subcribe to callbacks during map loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
				mapLoadingWindow );

			//load map
			if( !MapSystemWorld.MapLoad( fileName ) )
			{
				if( mapLoadingWindow != null )
					mapLoadingWindow.SetShouldDetach();

				LongOperationCallbackManager.Unsubscribe();

				return false;
			}

			//inform clients about world created
			if( server != null )
				server.EntitySystemService.WorldWasCreated();

			//Simulate physics for 5 seconds. That the physics has fallen asleep.
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				SimulatePhysicsForLoadedMap( 5 );

			//Update fog and shadow settings. This operation can be slow because need update all 
			//shaders if fog type or shadow technique changed.
			Map.Instance.UpdateSceneManagerFogAndShadowSettings();

			//Ensure that all materials are fully initialized.
			ShaderBaseMaterial.FinishInitializationOfEmptyMaterials();

			ActivateScreenFadingIn();

			LongOperationCallbackManager.Unsubscribe();

			//Error
			foreach( Control control in controlManager.Controls )
			{
				if( control is MessageBoxWindow && !control.IsShouldDetach() )
					return false;
			}

			if( !noChangeWindows )
				CreateGameWindowForMap();

			//play music
			if( !noChangeWindows )
			{
				if( GameMap.Instance != null )
					GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );
			}

			EntitySystemWorld.Instance.ResetExecutedTime();

			return true;
		}

		void SimulatePhysicsForLoadedMap( float seconds )
		{
			//inform entities about this simulation
			foreach( Entity entity in Map.Instance.Children )
			{
				Dynamic dynamic = entity as Dynamic;
				if( dynamic != null )
					dynamic.SuspendPhysicsDuringMapLoading( true );
			}

			PhysicsWorld.Instance.MainScene.EnableCollisionEvents = false;

			for( float time = 0; time < seconds; time += Entity.TickDelta )
			{
				PhysicsWorld.Instance.MainScene.Simulate( Entity.TickDelta );

				//WaterPlane specific
				foreach( WaterPlane waterPlane in WaterPlane.Instances )
					waterPlane.TickPhysicsInfluence( false );
			}

			PhysicsWorld.Instance.MainScene.EnableCollisionEvents = true;

			//inform entities about this simulation
			foreach( Entity entity in Map.Instance.Children )
			{
				Dynamic dynamic = entity as Dynamic;
				if( dynamic != null )
					dynamic.SuspendPhysicsDuringMapLoading( false );
			}
		}

		public bool WorldLoad( string fileName )
		{
			Control worldLoadingWindow = null;

			//world loading window
			{
				worldLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\WorldLoadingWindow.gui" );
				if( worldLoadingWindow != null )
				{
					worldLoadingWindow.Text = fileName;
					controlManager.Controls.Add( worldLoadingWindow );
				}
			}

			DeleteAllGameWindows();

			//Subcribe to callbacks during engine loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
				worldLoadingWindow );

			MapSystemWorld.MapDestroy();
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();

			RenderScene();

			//unload all reloadable textures
			TextureManager.Instance.UnloadAll( true, false );

			if( !MapSystemWorld.WorldLoad( WorldSimulationTypes.Single, fileName ) )
			{
				if( worldLoadingWindow != null )
					worldLoadingWindow.SetShouldDetach();

				LongOperationCallbackManager.Unsubscribe();

				return false;
			}

			//Update fog and shadow settings. This operation can be slow because need update all 
			//shaders if fog type or shadow technique changed.
			Map.Instance.UpdateSceneManagerFogAndShadowSettings();

			ActivateScreenFadingIn();

			LongOperationCallbackManager.Unsubscribe();

			//Error
			foreach( Control control in controlManager.Controls )
			{
				if( control is MessageBoxWindow && !control.IsShouldDetach() )
					return false;
			}

			//create game window
			CreateGameWindowForMap();

			//play music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			return true;
		}

		public bool WorldSave( string fileName )
		{
			//Control worldSavingWindow = null;
			////world saving window
			//{
			//   worldSavingWindow = ControlDeclarationManager.Instance.CreateControl(
			//      "Gui\\WorldSavingWindow.gui" );
			//   if( worldSavingWindow != null )
			//   {
			//      worldSavingWindow.Text = fileName;
			//      controlManager.Controls.Add( worldSavingWindow );
			//   }
			//   RenderScene();
			//}

			GameWindow gameWindow = null;
			foreach( Control control in controlManager.Controls )
			{
				gameWindow = control as GameWindow;
				if( gameWindow != null )
					break;
			}
			if( gameWindow != null )
				gameWindow.OnBeforeWorldSave();

			bool result = MapSystemWorld.WorldSave( fileName, true );

			//if( worldSavingWindow != null )
			//   worldSavingWindow.SetShouldDetach();

			return result;
		}

		public void SetNeedMapLoad( string fileName )
		{
			needMapLoadName = fileName;
			fadingOutTimer = fadingTime;
		}

		public void SetNeedRunExampleOfProceduralMapCreation()
		{
			needRunExampleOfProceduralMapCreation = true;
			fadingOutTimer = fadingTime;
		}

		public void SetNeedWorldLoad( string fileName )
		{
			needWorldLoadName = fileName;
			fadingOutTimer = fadingTime;
		}

		public void SetFadeOutScreenAndExit()
		{
			needFadingOutAndExit = true;
			fadingOutTimer = fadingTime;
		}

		GameWindow CreateGameWindowByGameType( GameMap.GameTypes gameType )
		{
			switch( gameType )
			{
			case GameMap.GameTypes.Action:
			case GameMap.GameTypes.TPSArcade:
				return new ActionGameWindow();

			case GameMap.GameTypes.RTS:
				return new RTSGameWindow();

			case GameMap.GameTypes.TurretDemo:
				return new TurretDemoGameWindow();

			case GameMap.GameTypes.JigsawPuzzleGame:
				return new JigsawPuzzleGameWindow();

			case GameMap.GameTypes.BallGame:
				return new BallGameWindow();

			case GameMap.GameTypes.VillageDemo:
				return new VillageDemoGameWindow();

			case GameMap.GameTypes.CatapultGame:
				return new CatapultGameWindow();

			case GameMap.GameTypes.PlatformerDemo:
				return new PlatformerDemoGameWindow();

			case GameMap.GameTypes.PathfindingDemo:
				return new PathfindingDemoGameWindow();

			//Here it is necessary to add a your specific game mode.
			//..

			}

			return null;
		}

		public void Client_OnConnectedToServer()
		{
			//add handlers for entity system service events
			GameNetworkClient client = GameNetworkClient.Instance;
			client.EntitySystemService.WorldCreateBegin += Client_EntitySystemService_WorldCreateBegin;
			client.EntitySystemService.WorldCreateEnd += Client_EntitySystemService_WorldCreateEnd;
			client.EntitySystemService.WorldDestroy += Client_EntitySystemService_WorldDestroy;

			if( !client_SubscribedToMapLoadingEvents )
			{
				Map.Client_MapLoadingBegin += Map_Client_MapLoadingBegin;
				Map.Client_MapLoadingEnd += Map_Client_MapLoadingEnd;
				client_SubscribedToMapLoadingEvents = true;
			}

			SuspendWorkingWhenApplicationIsNotActive = false;
		}

		public void Client_DisconnectFromServer()
		{
			GameNetworkClient client = GameNetworkClient.Instance;

			if( client != null )
			{
				//remove handlers for entity system service events
				client.EntitySystemService.WorldCreateBegin -= Client_EntitySystemService_WorldCreateBegin;
				client.EntitySystemService.WorldCreateEnd -= Client_EntitySystemService_WorldCreateEnd;
				client.EntitySystemService.WorldDestroy -= Client_EntitySystemService_WorldDestroy;
				client.Dispose();

				SuspendWorkingWhenApplicationIsNotActive = true;
			}
		}

		void Client_EntitySystemService_WorldCreateBegin( EntitySystemClientNetworkService sender,
			WorldType worldType, string mapVirtualFileName )
		{
			//close all windows
			foreach( Control control in controlManager.Controls )
				control.SetShouldDetach();

			//show map loading window
			if( !string.IsNullOrEmpty( mapVirtualFileName ) )
			{
				string mapDirectory = Path.GetDirectoryName( mapVirtualFileName );
				string guiPath = Path.Combine( mapDirectory, "Description\\MapLoadingWindow.gui" );
				if( !VirtualFile.Exists( guiPath ) )
					guiPath = "Gui\\MapLoadingWindow.gui";
				Control mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl( guiPath );
				if( mapLoadingWindow != null )
				{
					mapLoadingWindow.Text = mapVirtualFileName;
					controlManager.Controls.Add( mapLoadingWindow );
				}

				client_mapLoadingWindow = mapLoadingWindow;
			}

			RenderScene();

			DeleteAllGameWindows();

			MapSystemWorld.MapDestroy();

			//unload all reloadable textures
			TextureManager.Instance.UnloadAll( true, false );

			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.ClientOnly,
				worldType, sender.NetworkingInterface ) )
			{
				Log.Fatal( "GameEngineApp: Client_EntitySystemService_WorldCreateBegin: " +
					"EntitySystemWorld.WorldCreate failed." );
			}
		}

		void Client_EntitySystemService_WorldCreateEnd( EntitySystemClientNetworkService sender )
		{
			//dynamic created map example
			if( string.IsNullOrEmpty( Map.Instance.VirtualFileName ) )
				ExampleOfProceduralMapCreation.Client_CreateEntities();

			//play music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			CreateGameWindowForMap();
		}

		void Map_Client_MapLoadingBegin()
		{
			//Subcribe to callbacks during engine loading. We will render scene from callback.
			if( client_mapLoadingWindow != null )
			{
				LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
					client_mapLoadingWindow );
			}
		}

		void Map_Client_MapLoadingEnd()
		{
			if( client_mapLoadingWindow != null )
				LongOperationCallbackManager.Unsubscribe();
		}

		void Client_EntitySystemService_WorldDestroy( EntitySystemClientNetworkService sender,
			bool newMapWillBeLoaded )
		{
			MapSystemWorld.MapDestroy();
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();

			//close all windows
			foreach( Control control in GameEngineApp.Instance.ControlManager.Controls )
				control.SetShouldDetach();

			if( !newMapWillBeLoaded )
			{
				//create lobby window
				MultiplayerLobbyWindow lobbyWindow = new MultiplayerLobbyWindow();
				GameEngineApp.Instance.ControlManager.Controls.Add( lobbyWindow );
			}
		}

		public void Server_OnCreateServer()
		{
			SuspendWorkingWhenApplicationIsNotActive = false;
		}

		public void Server_DestroyServer( string reason )
		{
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
			{
				server.Dispose( reason );

				SuspendWorkingWhenApplicationIsNotActive = true;
			}
		}

		public bool Client_AllowCheckForDisconnection
		{
			get { return client_AllowCheckForDisconnection; }
			set { client_AllowCheckForDisconnection = value; }
		}

		static readonly string[] skipLogMessages = new string[]{
			"Initializing high level material:",
			"OGRE: Texture: ",
			"OGRE: D3D9 : Loading ",
			"OGRE: Mesh: Loading ",
		};

		void InvisibleLog_Handlers_InfoHandler( string text, ref bool dumpToLogFile )
		{
			//prevent some messages from writing to log file.
			foreach( string filter in skipLogMessages )
			{
				if( text.Contains( filter ) )
					dumpToLogFile = false;
			}

			//if( EngineConsole.Instance != null )
			//   EngineConsole.Instance.Print( text );
		}

		void Log_Handlers_InfoHandler( string text, ref bool dumpToLogFile )
		{
			if( EngineConsole.Instance != null )
				EngineConsole.Instance.Print( text );
		}

		void Log_Handlers_WarningHandler( string text, ref bool handled, ref bool dumpToLogFile )
		{
			if( EngineConsole.Instance != null )
			{
				handled = true;
				EngineConsole.Instance.Print( "Warning: " + text, new ColorValue( 1, 0, 0 ) );
				if( EngineConsole.Instance.AutoOpening )
					EngineConsole.Instance.Active = true;
			}
		}

		void Log_Handlers_ErrorHandler( string text, ref bool handled, ref bool dumpToLogFile )
		{
			if( controlManager != null )
			{
				handled = true;

				//find already created MessageBoxWindow
				foreach( Control control in controlManager.Controls )
				{
					if( control is MessageBoxWindow && !control.IsShouldDetach() )
						return;
				}

				bool insideTheGame = GameWindow.Instance != null;

				if( insideTheGame )
				{
					if( Map.Instance != null )
					{
						if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
							EntitySystemWorld.Instance.Simulation = false;
					}

					EngineApp.Instance.MouseRelativeMode = false;

					DeleteAllGameWindows();

					MapSystemWorld.MapDestroy();
					if( EntitySystemWorld.Instance != null )
						EntitySystemWorld.Instance.WorldDestroy();
				}

				GameEngineApp.Instance.Server_DestroyServer( "Error on the server" );
				GameEngineApp.Instance.Client_DisconnectFromServer();

				//show message box

				MessageBoxWindow messageBoxWindow = new MessageBoxWindow( text, "Error",
					delegate( Button sender )
					{
						if( insideTheGame )
						{
							//close all windows
							foreach( Control control in controlManager.Controls )
								control.SetShouldDetach();
						}
						else
						{
							//destroy Lobby Window
							foreach( Control control in controlManager.Controls )
							{
								if( control is MultiplayerLobbyWindow )
								{
									control.SetShouldDetach();
									break;
								}
							}
						}

						if( EntitySystemWorld.Instance == null )
						{
							EngineApp.Instance.SetNeedExit();
							return;
						}

						//create main menu
						if( MainMenuWindow.Instance == null )
							controlManager.Controls.Add( new MainMenuWindow() );

					} );

				controlManager.Controls.Add( messageBoxWindow );
			}
		}

		void Log_Handlers_FatalHandler( string text, string createdLogFilePath, ref bool handled )
		{
			if( controlManager != null )
			{
				//find already created MessageBoxWindow
				foreach( Control control in controlManager.Controls )
				{
					if( control is MessageBoxWindow && !control.IsShouldDetach() )
					{
						handled = true;
						return;
					}
				}
			}
		}

		void MakeScreenshot()
		{
			string directoryName = VirtualFileSystem.GetRealPathByVirtual( "user:Screenshots" );

			if( !Directory.Exists( directoryName ) )
				Directory.CreateDirectory( directoryName );

			string format = Path.Combine( directoryName, "Screenshot{0}.png" );

			for( int n = 1; n < 1000; n++ )
			{
				string v = n.ToString();
				if( n < 10 )
					v = "0" + v;
				if( n < 100 )
					v = "0" + v;

				string fileName = string.Format( format, v );

				if( !File.Exists( fileName ) )
				{
					RendererWorld.Instance.RenderWindow.WriteContentsToFile( fileName );
					AddScreenMessage( "Screenshot: " + fileName );
					break;
				}
			}
		}

		static void ConsoleCommand_ShowProfilingTool( string arguments )
		{
			bool show = ProfilingToolWindow.Instance == null;

			try
			{
				show = (bool)SimpleTypesUtils.GetSimpleTypeValue( typeof( bool ), arguments );
			}
			catch { }

			ShowProfilingTool( show );
		}

		public static void ShowProfilingTool( bool show )
		{
			if( show )
			{
				if( ProfilingToolWindow.Instance == null )
				{
					ProfilingToolWindow window = new ProfilingToolWindow();
					Instance.ControlManager.Controls.Add( window );
				}
			}
			else
			{
				if( ProfilingToolWindow.Instance != null )
					ProfilingToolWindow.Instance.SetShouldDetach();
			}
		}

		public ScreenControlManager ControlManager
		{
			get { return controlManager; }
		}

		protected override void OnRegisterConfigParameter( Config.Parameter parameter )
		{
			base.OnRegisterConfigParameter( parameter );

			if( EngineConsole.Instance != null )
				EngineConsole.Instance.RegisterConfigParameter( parameter );
		}

		protected override void OnBeforeUpdateShadowSettings()
		{
			base.OnBeforeUpdateShadowSettings();

			//Override map's shadow settings by game options.
			if( Map.Instance != null )
			{
				Map map = Map.Instance;

				map.ShadowTechnique = GameEngineApp.ShadowTechnique;

				if( GameEngineApp.ShadowUseMapSettings )
				{
					GameEngineApp.shadowPSSMSplitFactors = map.InitialShadowPSSMSplitFactors;
					GameEngineApp.ShadowFarDistance = map.InitialShadowFarDistance;
					GameEngineApp.ShadowColor = map.InitialShadowColor;
				}
				map.ShadowColor = GameEngineApp.ShadowColor;
				map.ShadowFarDistance = GameEngineApp.ShadowFarDistance;
				map.ShadowPSSMSplitFactors = GameEngineApp.ShadowPSSMSplitFactors;

				map.ShadowDirectionalLightTextureSize = Map.GetShadowTextureSize(
					GameEngineApp.ShadowDirectionalLightTextureSize );
				map.ShadowDirectionalLightMaxTextureCount = GameEngineApp.ShadowDirectionalLightMaxTextureCount;

				map.ShadowSpotLightTextureSize = Map.GetShadowTextureSize(
					GameEngineApp.ShadowSpotLightTextureSize );
				map.ShadowSpotLightMaxTextureCount = GameEngineApp.ShadowSpotLightMaxTextureCount;

				map.ShadowPointLightTextureSize = Map.GetShadowTextureSize(
					GameEngineApp.ShadowPointLightTextureSize );
				map.ShadowPointLightMaxTextureCount = GameEngineApp.ShadowPointLightMaxTextureCount;
			}
		}

		public void AddScreenMessage( string text )
		{
			ScreenMessage message = new ScreenMessage();
			message.text = text;
			message.timeRemaining = 5;
			screenMessages.Add( message );

			while( screenMessages.Count > 70 )
				screenMessages.RemoveAt( 0 );
		}

		void ActivateScreenFadingIn()
		{
			fadingInRemainingTime = 1;
			fadingInSkipFirstFrames = 5;
		}

	}
}
