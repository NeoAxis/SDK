// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using Engine;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.Networking;
using WPFAppFramework;
using ProjectCommon;

namespace WPFAppExample
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		bool initialized;

		bool freeCameraEnabled = true;
		Vec3 freeCameraPosition;
		SphereDir freeCameraDirection;
		bool freeCameraMouseRotating;
		Vec2 freeCameraRotatingStartPos;

		//

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded( object sender, RoutedEventArgs e )
		{
			//D3DImage support
			//D3DImage feature is a new level of interoperability between WPF and DirectX by allowing a custom Direct3D (D3D) 
			//surface to be blended with WPF's native D3D surface.
			{
				string[] args = Environment.GetCommandLineArgs();
				for( int n = 1; n < args.Length; n++ )
				{
					string arg = args[ n ];

					if( arg.ToLower() == "/d3dimage" )
						RendererWorld.InitializationOptions.AllowDirectX9Ex = true;
				}
			}

			//NeoAxis initialization
			if( !WPFAppWorld.Init( new ExampleEngineApp( EngineApp.ApplicationTypes.Simulation ), this,
				"user:Logs/WPFAppExample.log", true, null, null, null, null ) )
			{
				Close();
				return;
			}

			UpdateVolume();

			renderTargetUserControl1.AutomaticUpdateFPS = 60;
			renderTargetUserControl1.KeyDown += renderTargetUserControl1_KeyDown;
			renderTargetUserControl1.KeyUp += renderTargetUserControl1_KeyUp;
			renderTargetUserControl1.MouseDown += renderTargetUserControl1_MouseDown;
			renderTargetUserControl1.MouseUp += renderTargetUserControl1_MouseUp;
			renderTargetUserControl1.MouseMove += renderTargetUserControl1_MouseMove;
			renderTargetUserControl1.Tick += renderTargetUserControl1_Tick;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
			renderTargetUserControl1.RenderUI += renderTargetUserControl1_RenderUI;

			const string startMapName = "Maps\\Village Demo\\Map\\Map.map";

			//generate map list
			{
				string[] mapList = VirtualDirectory.GetFiles( "", "*.map", SearchOption.AllDirectories );
				foreach( string mapName in mapList )
				{
					comboBoxMaps.Items.Add( mapName );
					if( mapName == startMapName )
						comboBoxMaps.SelectedIndex = comboBoxMaps.Items.Count - 1;
				}
			}

			//load map
			WPFAppWorld.MapLoad( startMapName, true );

			//set camera position
			if( Map.Instance != null )
			{
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
			}

			if( !RendererWorld.InitializationOptions.AllowDirectX9Ex )
			{
				//checkBoxUseD3DImage.IsEnabled = false;
				checkBoxUseD3DImage.IsChecked = false;
			}

			initialized = true;
		}

		private void Window_Closed( object sender, EventArgs e )
		{
			//NeoAxis shutdown
			WPFAppWorld.Shutdown();
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

		void renderTargetUserControl1_MouseDown( object sender, MouseButtonEventArgs e )
		{
			//free camera rotating
			if( freeCameraEnabled && e.ChangedButton == MouseButton.Right )
			{
				freeCameraMouseRotating = true;
				freeCameraRotatingStartPos = renderTargetUserControl1.GetFloatMousePosition();
				renderTargetUserControl1.MouseRelativeMode = true;
			}
		}

		void renderTargetUserControl1_MouseUp( object sender, MouseButtonEventArgs e )
		{
			//free camera rotating
			if( freeCameraEnabled && e.ChangedButton == MouseButton.Right && freeCameraMouseRotating )
			{
				renderTargetUserControl1.MouseRelativeMode = false;
				freeCameraMouseRotating = false;
			}
		}

		void renderTargetUserControl1_MouseMove( object sender, MouseEventArgs e )
		{
			//free camera rotating
			if( Map.Instance != null && freeCameraEnabled && freeCameraMouseRotating )
			{
				Vec2 mouse = renderTargetUserControl1.GetMouseRelativeModeOffset().ToVec2() /
					renderTargetUserControl1.Viewport.DimensionsInPixels.Size.ToVec2();

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
			//update network status
			NetworkConnectionStatuses status = NetworkConnectionStatuses.Disconnected;
			if( GameNetworkClient.Instance != null )
				status = GameNetworkClient.Instance.Status;
			labelNetworkStatus.Content = status.ToString();

			//moving free camera by keys
			if( Map.Instance != null && freeCameraEnabled )
			{
				float cameraVelocity = 20;

				Vec3 pos = freeCameraPosition;
				SphereDir dir = freeCameraDirection;

				float step = cameraVelocity * delta;

				if( renderTargetUserControl1.IsKeyPressed( Key.W ) ||
					renderTargetUserControl1.IsKeyPressed( Key.Up ) )
				{
					pos += dir.GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Key.S ) ||
					renderTargetUserControl1.IsKeyPressed( Key.Down ) )
				{
					pos -= dir.GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Key.A ) ||
					renderTargetUserControl1.IsKeyPressed( Key.Left ) )
				{
					pos += new SphereDir(
						dir.Horizontal + MathFunctions.PI / 2, 0 ).GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Key.D ) ||
					renderTargetUserControl1.IsKeyPressed( Key.Right ) )
				{
					pos += new SphereDir(
						dir.Horizontal - MathFunctions.PI / 2, 0 ).GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Key.Q ) )
					pos += new Vec3( 0, 0, step );
				if( renderTargetUserControl1.IsKeyPressed( Key.E ) )
					pos += new Vec3( 0, 0, -step );

				freeCameraPosition = pos;
			}
		}

		void renderTargetUserControl1_Render( RenderTargetUserControl sender, Camera camera )
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
					//usual camera

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
					//free camera

					position = freeCameraPosition;
					forward = freeCameraDirection.GetVector();
					up = Vec3.ZAxis;
					fov = Map.Instance.Fov;
				}

				renderTargetUserControl1.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
				renderTargetUserControl1.CameraFixedUp = up;
				renderTargetUserControl1.CameraFov = fov;
				renderTargetUserControl1.CameraPosition = position;
				renderTargetUserControl1.CameraDirection = forward;
			}

			//update sound listener
			if( SoundWorld.Instance != null )
				SoundWorld.Instance.SetListener( camera.Position, Vec3.Zero, camera.Direction, camera.Up );

			//if( Map.Instance != null && !renderTargetUserControl1.MouseRelativeMode )
			//	RenderEntityOverCursor( camera );
		}

		void RenderEntityOverCursor( Camera camera )
		{
			Vec2 mouse = renderTargetUserControl1.GetFloatMousePosition();

			Ray ray = camera.GetCameraToViewportRay( mouse );

			MapObject mapObject = null;

			Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
			{
				if( obj is StaticMesh )
					return true;
				mapObject = obj;
				return false;
			} );

			if( mapObject != null )
			{
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				camera.DebugGeometry.AddBounds( mapObject.MapBounds );
			}
		}

		void renderTargetUserControl1_RenderUI( RenderTargetUserControl sender, GuiRenderer renderer )
		{
			string text = "NeoAxis 3D Engine " + EngineVersionInformation.Version;
			renderer.AddText( text, new Vec2( .01f, .01f ), HorizontalAlign.Left,
				 VerticalAlign.Top, new ColorValue( 1, 1, 1 ) );

			renderer.AddText( "Camera control: W A S D, right mouse", new Vec2( .99f, .99f ),
				HorizontalAlign.Right, VerticalAlign.Bottom, new ColorValue( 1, 1, 1 ) );
		}

		private void slider1_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			UpdateVolume();
		}

		void UpdateVolume()
		{
			if( SoundWorld.Instance == null )
				return;

			float volume = (float)slider1.Value / (float)slider1.Maximum;
			SoundWorld.Instance.MasterChannelGroup.Volume = volume;
		}

		private void buttonShowUI_Click( object sender, EventArgs e )
		{
			//close if already created
			foreach( Engine.UISystem.Control control in renderTargetUserControl1.ControlManager.Controls )
			{
				if( control is WindowsAppExampleHUD )
				{
					control.SetShouldDetach();
					return;
				}
			}

			//create
			WindowsAppExampleHUD window = new WindowsAppExampleHUD();
			renderTargetUserControl1.ControlManager.Controls.Add( window );
		}

		private void buttonClose_Click( object sender, RoutedEventArgs e )
		{
			Close();
		}

		private void buttonCreateBox_Click( object sender, RoutedEventArgs e )
		{
			if( Map.Instance == null )
				return;

			Vec3 position = new Vec3( -10.04362f, 3.380724f, 53.08361f );

			if( GameNetworkClient.Instance != null )
			{
				//network mode.
				//send request message "Example_CreateMapObject" to the server.
				string data = string.Format( "{0};{1};{2}", "Box", position, Quat.Identity );
				GameNetworkClient.Instance.CustomMessagesService.SendToServer(
					"Example_CreateMapObject", data );
			}
			else
			{
				//single mode.
				MapObject box = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
				box.Position = position;
				box.PostCreate();
			}
		}

		private void buttonShowUI_Click( object sender, RoutedEventArgs e )
		{
			//close if already created
			foreach( Engine.UISystem.Control control in renderTargetUserControl1.ControlManager.Controls )
			{
				if( control is WindowsAppExampleHUD )
				{
					control.SetShouldDetach();
					return;
				}
			}

			//create
			WindowsAppExampleHUD window = new WindowsAppExampleHUD();
			renderTargetUserControl1.ControlManager.Controls.Add( window );
		}

		private void buttonAddForm_Click( object sender, RoutedEventArgs e )
		{
			AdditionalWindow window = new AdditionalWindow();
			window.ShowDialog();
		}

		private void buttonLoadMap_Click( object sender, RoutedEventArgs e )
		{
			if( comboBoxMaps.SelectedIndex == -1 )
				return;

			string mapName = (string)comboBoxMaps.SelectedItem;

			if( GameNetworkClient.Instance != null )
			{
				//network mode.
				//send request message "Example_MapLoad" to the server.
				GameNetworkClient.Instance.CustomMessagesService.SendToServer( "Example_MapLoad", mapName );
			}
			else
			{
				//load map in single mode.
				WPFAppWorld.MapLoad( mapName, true );

				//set camera position
				if( Map.Instance != null )
				{
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
				}

			}
		}

		private void buttonDestroy_Click( object sender, RoutedEventArgs e )
		{
			WPFAppWorld.WorldDestroy();
			if( ExampleEngineApp.Instance != null )
				ExampleEngineApp.Instance.Client_DisconnectFromServer();
		}

		private void buttonConnect_Click( object sender, RoutedEventArgs e )
		{
			WPFAppWorld.WorldDestroy();
			if( ExampleEngineApp.Instance != null )
				ExampleEngineApp.Instance.Client_DisconnectFromServer();

			string host = textBoxServerAddress.Text;
			int port = 56565;
			string userName = textBoxUserName.Text;
			string password = "";
			ExampleEngineApp.Instance.TryConnectToServer( host, port, userName, password );
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

		void RestartApplication( bool d3dImageSupport )
		{
			try
			{
				string fileName = Assembly.GetExecutingAssembly().Location;

				string arguments = "";
				if( d3dImageSupport )
					arguments += "/D3DImage";
				Process process = Process.Start( fileName, arguments );

				Application.Current.Shutdown();
			}
			catch( Exception e )
			{
				MessageBox.Show( e.Message );
			}
		}

		private void checkBoxUseD3DImage_Checked( object sender, RoutedEventArgs e )
		{
			//!!!!
			if( initialized )
			{
				if( MessageBox.Show( "Restart application to enable D3DImage support?\n\nD3DImage feature is a new level of " +
					"interoperability between WPF and DirectX by allowing a custom Direct3D (D3D) surface to be blended with " +
					"WPF's native D3D surface.", "WPF App Example", MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes )
				{
					RestartApplication( true );
				}
				//renderTargetUserControl1.AllowUsingD3DImage = true;
			}
		}

		private void checkBoxUseD3DImage_Unchecked( object sender, RoutedEventArgs e )
		{
			//!!!!
			if( initialized )
			{
				if( MessageBox.Show( "Restart application to disable D3DImage support?\n\nD3DImage feature is a new level of " +
					"interoperability between WPF and DirectX by allowing a custom Direct3D (D3D) surface to be blended with " +
					"WPF's native D3D surface.", "WPF App Example", MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes )
				{
					RestartApplication( false );
				}
				//renderTargetUserControl1.AllowUsingD3DImage = false;
			}
		}
	}
}
