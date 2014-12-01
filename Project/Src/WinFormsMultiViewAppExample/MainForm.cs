// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using WeifenLuo.WinFormsUI.Docking;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.FileSystem;
using Engine.MapSystem;
using Engine.SoundSystem;
using Engine.Utils;
using WinFormsAppFramework;

namespace WinFormsMultiViewAppExample
{
	partial class MainForm : Form
	{
		const string dockingConfigFileName = "user:Configs/WinFormsMultiViewAppExampleDocking.config";

		static MainForm instance;

		[Config( "MainForm", "showSplashScreenAtStartup" )]
		public static bool showSplashScreenAtStartup = true;
		[Config( "MainForm", "showMaximized" )]
		public static bool showMaximized = true;
		[Config( "MainForm", "startWindowSize" )]
		public static Vec2I startWindowSize;
		[Config( "MainForm", "workAreaViewsConfiguration" )]
		public static ViewsConfigurations workAreaViewsConfiguration = ViewsConfigurations.FourViews;
		[Config( "MainForm", "showStatusBar" )]
		public static bool showStatusBar = true;
		[Config( "Sound", "soundVolume" )]
		static float soundVolume = .5f;

		bool forceCloseForm;

		//docking
		DockPanel dockPanel;
		List<DockContent> dockForms = new List<DockContent>();
		EntityTypesForm entityTypesForm;
		PropertiesForm propertiesForm;
		OutputForm outputForm;
		Example3DViewForm example3DViewForm;

		//work area control
		MultiViewRenderTargetControl workAreaControl;
		ViewsConfigurations initializedWorkAreaViewsConfiguration;
		Engine.Renderer.Font fontMedium;
		Engine.Renderer.Font fontBig;

		//free camera management
		bool freeCameraEnabled = true;
		Vec3 freeCameraPosition;
		SphereDir freeCameraDirection;
		bool freeCameraMouseRotating;
		Vec2 freeCameraRotatingStartPos;

		///////////////////////////////////////////

		public enum ViewsConfigurations
		{
			NoViews,
			OneView,
			FourViews,
		}

		///////////////////////////////////////////

		public static MainForm Instance
		{
			get { return instance; }
		}

		public EntityTypesForm EntityTypesForm
		{
			get { return entityTypesForm; }
		}

		public PropertiesForm PropertiesForm
		{
			get { return propertiesForm; }
		}

		public OutputForm OutputForm
		{
			get { return outputForm; }
		}

		public Example3DViewForm Example3DViewForm
		{
			get { return example3DViewForm; }
		}

		public MainForm()
		{
			instance = this;

			//NeoAxis initialization
			EngineApp.ConfigName = "user:Configs/WinFormsMultiViewAppExample.config";
			if( !WinFormsAppWorld.InitWithoutCreation(
				new MultiViewAppEngineApp( EngineApp.ApplicationTypes.Simulation ),
				"user:Logs/WinFormsMultiViewAppExample.log", true, null, null, null, null ) )
			{
				Close();
				return;
			}

			EngineApp.Instance.Config.RegisterClassParameters( typeof( MainForm ) );

			//print information logs to the Output panel.
			Log.Handlers.InfoHandler += delegate( string text, ref bool dumpToLogFile )
			{
				if( outputForm != null )
					outputForm.Print( text );
			};

			//show splash screen
			if( showSplashScreenAtStartup && !Debugger.IsAttached )
			{
				Image image = WinFormsMultiViewAppExample.Properties.Resources.ApplicationSplash;
				SplashForm splashForm = new SplashForm( image );
				splashForm.Show();
			}

			InitializeComponent();

			//restore window state
			if( showMaximized )
			{
				Screen screen = Screen.FromControl( this );
				if( screen.Primary )
					WindowState = FormWindowState.Maximized;
			}
			else
			{
				Size = new Size( startWindowSize.X, startWindowSize.Y );
			}

			SuspendLayout();

			dockPanel = new DockPanel();
			dockPanel.Visible = false;
			dockPanel.Parent = this;
			dockPanel.Dock = DockStyle.Fill;
			dockPanel.Name = "dockPanel";
			dockPanel.BringToFront();

			//create dock forms
			entityTypesForm = new EntityTypesForm();
			dockForms.Add( entityTypesForm );
			propertiesForm = new PropertiesForm();
			dockForms.Add( propertiesForm );
			outputForm = new OutputForm();
			dockForms.Add( outputForm );
			example3DViewForm = new Example3DViewForm();
			dockForms.Add( example3DViewForm );

			menuStrip.Visible = false;
			toolStripGeneral.Visible = false;

			ResumeLayout();
		}

		private void exitToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void aboutToolStripMenuItem_Click( object sender, EventArgs e )
		{
			MessageBox.Show( "WinForms Multi View Application.", "About" );
		}

		private void entityTypesToolStripMenuItem_Click( object sender, EventArgs e )
		{
			entityTypesForm.Show( dockPanel );
		}

		private void propertiesWindowToolStripMenuItem_Click( object sender, EventArgs e )
		{
			propertiesForm.Show( dockPanel );
		}

		private void outputToolStripMenuItem_Click( object sender, EventArgs e )
		{
			outputForm.Show( dockPanel );
		}

		private void example3DViewToolStripMenuItem_Click( object sender, EventArgs e )
		{
			example3DViewForm.Show( dockPanel );
		}

		IDockContent GetContentFromPersistString( string persistString )
		{
			foreach( DockContent form in dockForms )
			{
				if( persistString == form.GetType().ToString() )
					return form;
			}
			return null;
		}

		void LoadDockingConfig()
		{
			string configFile = VirtualFileSystem.GetRealPathByVirtual( dockingConfigFileName );

			bool loaded = false;

			if( File.Exists( configFile ) )
			{
				try
				{
					dockPanel.LoadFromXml( configFile, GetContentFromPersistString );
					loaded = true;
				}
				catch { }
			}
			else
			{
				try
				{
					string defaultDockingXml = WinFormsMultiViewAppExample.Properties.Resources.DefaultDockingXml;

					byte[] bytes = new byte[ defaultDockingXml.Length * 2 ];
					for( int n = 0; n < defaultDockingXml.Length; n++ )
						bytes[ n * 2 ] = (byte)defaultDockingXml[ n ];

					MemoryStream stream = new MemoryStream( bytes );
					dockPanel.LoadFromXml( stream, GetContentFromPersistString, true );
				}
				catch( Exception ee )
				{
					Log.Error( ee.Message );
				}
			}

			if( !loaded )
			{
				entityTypesForm.Show( dockPanel );
				propertiesForm.Show( dockPanel );
				outputForm.Show( dockPanel );
				example3DViewForm.Show( dockPanel );
			}
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			if( !WinFormsAppWorld.Create( this ) )
			{
				forceCloseForm = true;
				Close();
				return;
			}

			LoadDockingConfig();

			UpdateWorkAreaMenuItems();
			statusBarToolStripMenuItem.Checked = showStatusBar;
			menuStrip.Visible = true;
			toolStripGeneral.Visible = true;
			if( !showStatusBar )
				statusStrip1.Visible = false;
			dockPanel.Visible = true;

			//create work area views
			InitWorkAreaControl();

			//configure sound
			UpdateEngineSoundVolume();

			//update dock forms
			if( entityTypesForm != null )
				entityTypesForm.UpdateTree();

			//show text in Output window
			Log.Info( "Powered by the NeoAxis 3D Engine." );
			Log.Info( "-------------------------------------------------------------" );

			timer1.Start();

			//load map
			MapLoad( "Maps\\MainMenu\\Map.map" );

			if( SplashForm.Instance != null )
				SplashForm.Instance.AllowClose = true;
		}

		private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
		{
			DestroyWorkAreaControl();

			showMaximized = WindowState != FormWindowState.Normal;
			startWindowSize = new Vec2I( Size.Width, Size.Height );

			if( !forceCloseForm )
			{
				string configFile = VirtualFileSystem.GetRealPathByVirtual( dockingConfigFileName );
				if( !Directory.Exists( Path.GetDirectoryName( configFile ) ) )
					Directory.CreateDirectory( Path.GetDirectoryName( configFile ) );
				dockPanel.SaveAsXml( configFile );
			}

			instance = null;
		}

		private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			//NeoAxis shutdown
			WinFormsAppWorld.Shutdown();
		}

		Rectangle GetDockPanelWorkAreaRectangle()
		{
			Rectangle clientRect = dockPanel.DisplayRectangle;

			int left = clientRect.Left;
			int top = clientRect.Top + 1;
			int right = clientRect.Right;
			int bottom = clientRect.Bottom;

			Trace.Assert( dockPanel.DockWindows.Count == 5 );
			IEnumerator enumerator = dockPanel.DockWindows.GetEnumerator();

			Control control;

			//Central
			enumerator.MoveNext();

			//Left
			enumerator.MoveNext();
			control = (Control)enumerator.Current;
			if( control.Visible )
				left += control.Width;

			//Right
			enumerator.MoveNext();
			control = (Control)enumerator.Current;
			if( control.Visible )
				right -= control.Width;

			//Top
			enumerator.MoveNext();
			control = (Control)enumerator.Current;
			if( control.Visible )
				top += control.Height;

			//Bottom
			enumerator.MoveNext();
			control = (Control)enumerator.Current;
			if( control.Visible )
				bottom -= control.Height;

			if( right - left < 1 )
				right = left + 1;
			if( bottom - top < 1 )
				bottom = top + 1;

			return new Rectangle( left, top, right - left, bottom - top );
		}

		public bool IsMainFormModalOnTop()
		{
			Form activeForm = ActiveForm;
			if( activeForm == null )
				return true;

			Form form = activeForm;
			while( form != null )
			{
				if( form == this )
					return true;
				if( form.Modal )
					return false;
				form = form.Owner;
			}

			return true;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			//update menu and toolbar items
			closeToolStripMenuItem.Enabled = Map.Instance != null;

			//update work area render target
			UpdateWorkAreaControl();
		}

		private void openToolStripMenuItem_Click( object sender, EventArgs e )
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = ToolsLocalization.Translate( "Various",
				"Map files (*.map)|*.map|All files (*.*)|*.*" );

			dialog.InitialDirectory = VirtualFileSystem.ResourceDirectoryPath;
			dialog.RestoreDirectory = true;

			if( dialog.ShowDialog() != DialogResult.OK )
				return;

			string virtualFileName = VirtualFileSystem.GetVirtualPathByReal( dialog.FileName );

			if( string.IsNullOrEmpty( virtualFileName ) )
			{
				Log.Warning( ToolsLocalization.Translate( "Various",
					"Unable to load file. You cannot load map outside \"Data\" directory." ) );
				return;
			}

			MapLoad( virtualFileName );
		}

		void ShowOptionsDialog()
		{
			OptionsDialog dialog = new OptionsDialog();
			dialog.AddLeaf( new GeneralOptionsLeaf() );
			dialog.ShowDialog();
		}

		private void optionsToolStripMenuItem_Click( object sender, EventArgs e )
		{
			ShowOptionsDialog();
		}

		public DockPanel DockPanel
		{
			get { return dockPanel; }
		}

		private void closeToolStripMenuItem_Click( object sender, EventArgs e )
		{
			WinFormsAppWorld.WorldDestroy();
		}

		private void statusBarToolStripMenuItem_Click( object sender, EventArgs e )
		{
			showStatusBar = statusBarToolStripMenuItem.Checked;
			statusStrip1.Visible = showStatusBar;
		}

		void InitWorkAreaControl()
		{
			DestroyWorkAreaControl();

			if( workAreaViewsConfiguration != ViewsConfigurations.NoViews )
			{
				initializedWorkAreaViewsConfiguration = workAreaViewsConfiguration;

				workAreaControl = new MultiViewRenderTargetControl();
				workAreaControl.Visible = false;
				dockPanel.Controls.Add( workAreaControl );

				Rect[] rectangles = null;

				switch( workAreaViewsConfiguration )
				{
				case ViewsConfigurations.OneView:
					rectangles = new Rect[] { new Rect( 0, 0, 1, 1 ) };
					break;
				case ViewsConfigurations.FourViews:
					rectangles = new Rect[]
					{
						new Rect( 0, 0, .498f, .495f ),
						new Rect( .502f, 0, 1, .495f ),
						new Rect( 0, .505f, .498f, 1 ),
						new Rect( .502f, .505f, 1, 1 ),
					};
					break;
				}
				workAreaControl.SetViewsConfiguration( rectangles );

				workAreaControl.SetAutomaticUpdateFPSForAllViews( 30 );

				workAreaControl.Render += workAreaControl_Render;
				workAreaControl.RenderUI += workAreaControl_RenderUI;

				//subscribe events to first view
				RenderTargetUserControl control = workAreaControl.Views[ 0 ].Control;
				control.KeyDown += renderTargetUserControl1_KeyDown;
				control.KeyUp += renderTargetUserControl1_KeyUp;
				control.MouseDown += renderTargetUserControl1_MouseDown;
				control.MouseUp += renderTargetUserControl1_MouseUp;
				control.MouseMove += renderTargetUserControl1_MouseMove;
				control.Tick += renderTargetUserControl1_Tick;
			}
		}

		void DestroyWorkAreaControl()
		{
			if( workAreaControl != null )
			{
				workAreaControl.Destroy();
				workAreaControl.Dispose();
				workAreaControl = null;
			}
		}

		void UpdateWorkAreaControl()
		{
			//recreate control when changed views configuration
			if( initializedWorkAreaViewsConfiguration != workAreaViewsConfiguration )
				InitWorkAreaControl();

			if( workAreaControl != null )
			{
				//show control if it still not visible
				workAreaControl.Visible = true;

				//update position of control
				if( !RenderSystem.Instance.IsDeviceLostByTestCooperativeLevel() )
				{
					if( Visible && WindowState != FormWindowState.Minimized )
					{
						Rectangle clientRect = GetDockPanelWorkAreaRectangle();

						if( workAreaControl.Location != clientRect.Location ||
							workAreaControl.Size != clientRect.Size )
						{
							workAreaControl.Location = clientRect.Location;
							workAreaControl.Size = clientRect.Size;
						}
					}
				}
			}
		}

		void renderTargetUserControl1_KeyDown( object sender, KeyEventArgs e )
		{
			//process keys for control here.

			//for checking key state use:
			//renderTargetUserControl1.IsKeyPressed();
		}

		void renderTargetUserControl1_KeyUp( object sender, KeyEventArgs e )
		{
			//process keys for control here.
		}

		void renderTargetUserControl1_MouseDown( object sender, MouseEventArgs e )
		{
			RenderTargetUserControl control = (RenderTargetUserControl)sender;

			//free camera rotating
			if( Map.Instance != null && freeCameraEnabled && e.Button == MouseButtons.Right )
			{
				freeCameraMouseRotating = true;
				freeCameraRotatingStartPos = control.GetFloatMousePosition();
				control.MouseRelativeMode = true;
			}
		}

		void renderTargetUserControl1_MouseUp( object sender, MouseEventArgs e )
		{
			RenderTargetUserControl control = (RenderTargetUserControl)sender;

			//free camera rotating
			if( freeCameraEnabled && e.Button == MouseButtons.Right && freeCameraMouseRotating )
			{
				control.MouseRelativeMode = false;
				freeCameraMouseRotating = false;
			}
		}

		void renderTargetUserControl1_MouseMove( object sender, MouseEventArgs e )
		{
			RenderTargetUserControl control = (RenderTargetUserControl)sender;

			//free camera rotating
			if( Map.Instance != null && freeCameraEnabled && freeCameraMouseRotating )
			{
				Vec2 mouse = control.GetMouseRelativeModeOffset().ToVec2() /
					control.Viewport.DimensionsInPixels.Size.ToVec2();

				SphereDir dir = freeCameraDirection;
				dir.Horizontal -= mouse.X;
				dir.Vertical -= mouse.Y;

				dir.Horizontal = MathFunctions.RadiansNormalize360( dir.Horizontal );

				const float vlimit = MathFunctions.PI / 2 - .01f;
				if( dir.Vertical > vlimit ) dir.Vertical = vlimit;
				if( dir.Vertical < -vlimit ) dir.Vertical = -vlimit;

				freeCameraDirection = dir;
			}
		}

		void renderTargetUserControl1_Tick( RenderTargetUserControl sender, float delta )
		{
			RenderTargetUserControl control = sender;

			//moving free camera by keys
			if( Map.Instance != null && freeCameraEnabled )
			{
				float cameraVelocity = 20;

				Vec3 pos = freeCameraPosition;
				SphereDir dir = freeCameraDirection;

				float step = cameraVelocity * delta;

				if( control.IsKeyPressed( Keys.W ) ||
					control.IsKeyPressed( Keys.Up ) )
				{
					pos += dir.GetVector() * step;
				}
				if( control.IsKeyPressed( Keys.S ) ||
					control.IsKeyPressed( Keys.Down ) )
				{
					pos -= dir.GetVector() * step;
				}
				if( control.IsKeyPressed( Keys.A ) ||
					control.IsKeyPressed( Keys.Left ) )
				{
					pos += new SphereDir(
						dir.Horizontal + MathFunctions.PI / 2, 0 ).GetVector() * step;
				}
				if( control.IsKeyPressed( Keys.D ) ||
					control.IsKeyPressed( Keys.Right ) )
				{
					pos += new SphereDir(
						dir.Horizontal - MathFunctions.PI / 2, 0 ).GetVector() * step;
				}
				if( control.IsKeyPressed( Keys.Q ) )
					pos += new Vec3( 0, 0, step );
				if( control.IsKeyPressed( Keys.E ) )
					pos += new Vec3( 0, 0, -step );

				freeCameraPosition = pos;
			}
		}

		void workAreaControl_Render( MultiViewRenderTargetControl sender, int viewIndex, Camera camera )
		{
			//update camera
			if( Map.Instance != null )
			{
				Vec3 position;
				Vec3 forward;
				Vec3 up;
				Degree fov;

				if( !freeCameraEnabled )
				{
					//usual camera mode

					//add here your special camera management code and set "freeCameraEnabled = false;"

					position = Vec3.Zero;
					forward = new Vec3( 1, 0, 0 );
					up = Vec3.ZAxis;
					fov = Map.Instance.Fov;

					//MapCamera mapCamera = Entities.Instance.GetByName( "MapCamera_0" ) as MapCamera;
					//if( mapCamera != null )
					//{
					//   position = mapCamera.Position;
					//   forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
					//   fov = mapCamera.Fov;
					//}
				}
				else
				{
					//free camera mode
					position = freeCameraPosition;
					forward = freeCameraDirection.GetVector();
					up = Vec3.ZAxis;
					fov = Map.Instance.Fov;
				}

				//update control
				RenderTargetUserControl control = workAreaControl.Views[ viewIndex ].Control;

				if( viewIndex == 0 )
				{
					//first view
					control.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
					control.CameraFixedUp = up;
					control.CameraFov = fov;
					control.CameraPosition = position;
					control.CameraDirection = forward;
				}
				else
				{
					//all views except first view. set orthographic camera projection.
					control.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
					control.CameraProjectionType = ProjectionTypes.Orthographic;
					control.CameraOrthoWindowHeight = 30;

					Vec3 lookAt = position;

					switch( viewIndex )
					{
					case 1:
						control.CameraFixedUp = Vec3.ZAxis;
						control.CameraDirection = -Vec3.XAxis;
						control.CameraPosition = lookAt - control.CameraDirection * 100;
						break;
					case 2:
						control.CameraFixedUp = Vec3.ZAxis;
						control.CameraDirection = -Vec3.YAxis;
						control.CameraPosition = lookAt - control.CameraDirection * 100;
						break;
					case 3:
						control.CameraFixedUp = Vec3.YAxis;
						control.CameraDirection = -Vec3.ZAxis;
						control.CameraPosition = lookAt - control.CameraDirection * 100;
						break;
					}
				}
			}

			//update sound listener
			if( viewIndex == 0 && SoundWorld.Instance != null )
				SoundWorld.Instance.SetListener( camera.Position, Vec3.Zero, camera.Direction, camera.Up );

			//draw 2D graphics as 3D by means DebugGeometry
			if( workAreaViewsConfiguration == ViewsConfigurations.FourViews && viewIndex == 3 )
			{
				const float cameraOffset = 10;
				Vec3 center = camera.Position + camera.Direction * cameraOffset;

				//draw box
				Vec3[] positions;
				int[] indices;
				GeometryGenerator.GenerateBox( new Vec3( 10, 4, 1 ), out positions, out indices );
				Mat4 transform = new Mat4( Mat3.Identity, center );
				camera.DebugGeometry.Color = new ColorValue( .5f, 0, 0 );
				camera.DebugGeometry.AddVertexIndexBuffer( positions, indices, transform, false, true );
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				camera.DebugGeometry.AddVertexIndexBuffer( positions, indices, transform, true, true );

				//draw axes
				camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
				camera.DebugGeometry.AddArrow( center, center + new Vec3( 5, 0, 0 ) );
				camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
				camera.DebugGeometry.AddArrow( center, center + new Vec3( 0, 5, 0 ) );
				camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
				camera.DebugGeometry.AddArrow( center, center + new Vec3( 0, 0, 5 ) );
			}
		}

		void AddTextWithShadow( GuiRenderer renderer, Engine.Renderer.Font font, string text, Vec2 position,
			HorizontalAlign horizontalAlign, VerticalAlign verticalAlign, ColorValue color )
		{
			Vec2 shadowOffset = 2.0f / renderer.ViewportForScreenGuiRenderer.DimensionsInPixels.Size.ToVec2();

			renderer.AddText( font, text, position + shadowOffset, horizontalAlign, verticalAlign,
				new ColorValue( 0, 0, 0, color.Alpha / 2 ) );
			renderer.AddText( font, text, position, horizontalAlign, verticalAlign, color );
		}

		void Draw2DRectangles( GuiRenderer renderer )
		{
			Rect rect = new Rect( .01f, .9f, .25f, .99f );
			renderer.AddQuad( rect, new ColorValue( .5f, .5f, .5f, .5f ) );

			Rect rect2 = rect;
			rect2.Expand( new Vec2( .005f / renderer.AspectRatio, .005f ) );
			renderer.AddRectangle( rect2, new ColorValue( 1, 1, 0 ) );

			AddTextWithShadow( renderer, fontMedium, "2D GUI Drawing", rect.GetCenter(), HorizontalAlign.Center,
				VerticalAlign.Center, new ColorValue( 1, 1, 1 ) );
		}

		void workAreaControl_RenderUI( MultiViewRenderTargetControl sender, int viewIndex, GuiRenderer renderer )
		{
			if( fontMedium == null )
				fontMedium = FontManager.Instance.LoadFont( "Default", .04f );
			if( fontBig == null )
				fontBig = FontManager.Instance.LoadFont( "Default", .07f );

			AddTextWithShadow( renderer, fontBig, string.Format( "View {0}", viewIndex ), new Vec2( .99f, .01f ),
				HorizontalAlign.Right, VerticalAlign.Top, new ColorValue( 1, 1, 1 ) );

			if( viewIndex == 0 )
			{
				AddTextWithShadow( renderer, fontMedium, "Camera control: W A S D, right mouse button",
					new Vec2( .99f, .99f ), HorizontalAlign.Right, VerticalAlign.Bottom, new ColorValue( 1, 1, 1 ) );
			}

			Draw2DRectangles( renderer );
		}

		public static float SoundVolume
		{
			get { return soundVolume; }
			set
			{
				soundVolume = value;
				MathFunctions.Clamp( ref soundVolume, 0, 1 );
				UpdateEngineSoundVolume();
			}
		}

		static void UpdateEngineSoundVolume()
		{
			if( EngineApp.Instance.DefaultSoundChannelGroup != null )
				EngineApp.Instance.DefaultSoundChannelGroup.Volume = soundVolume;
		}

		MapCamera FindFirstMapCamera()
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				MapCamera mapCamera = entity as MapCamera;
				if( mapCamera != null )
					return mapCamera;
			}
			return null;
		}

		bool MapLoad( string virtualFileName )
		{
			if( !WinFormsAppWorld.MapLoad( virtualFileName, true ) )
				return false;

			//set camera position
			MapCamera mapCamera = FindFirstMapCamera();
			if( mapCamera != null )
			{
				freeCameraPosition = mapCamera.Position;
				freeCameraDirection = SphereDir.FromVector( mapCamera.Rotation.GetForward() );
			}
			else
			{
				freeCameraPosition = Map.Instance.EditorCameraPosition;
				freeCameraDirection = Map.Instance.EditorCameraDirection;
			}

			return true;
		}

		private void workAreaWithoutViewsToolStripMenuItem_Click( object sender, EventArgs e )
		{
			workAreaViewsConfiguration = ViewsConfigurations.NoViews;
			UpdateWorkAreaMenuItems();
		}

		private void workAreaWith1ViewToolStripMenuItem_Click( object sender, EventArgs e )
		{
			workAreaViewsConfiguration = ViewsConfigurations.OneView;
			UpdateWorkAreaMenuItems();
		}

		private void workAreaWith4ViewsToolStripMenuItem_Click( object sender, EventArgs e )
		{
			workAreaViewsConfiguration = ViewsConfigurations.FourViews;
			UpdateWorkAreaMenuItems();
		}

		void UpdateWorkAreaMenuItems()
		{
			workAreaWithoutViewsToolStripMenuItem.Checked = workAreaViewsConfiguration == ViewsConfigurations.NoViews;
			workAreaWith1ViewToolStripMenuItem.Checked = workAreaViewsConfiguration == ViewsConfigurations.OneView;
			workAreaWith4ViewsToolStripMenuItem.Checked = workAreaViewsConfiguration == ViewsConfigurations.FourViews;
		}
	}
}
