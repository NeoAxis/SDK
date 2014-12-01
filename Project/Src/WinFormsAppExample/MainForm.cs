// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Engine;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.Networking;
using WinFormsAppFramework;
using ProjectCommon;

namespace WinFormsAppExample
{
	public partial class MainForm : Form
	{
		bool freeCameraEnabled = true;
		Vec3 freeCameraPosition;
		SphereDir freeCameraDirection;
		bool freeCameraMouseRotating;
		Vec2 freeCameraRotatingStartPos;

		//

		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			//NeoAxis initialization
			if( !WinFormsAppWorld.Init( new ExampleEngineApp( EngineApp.ApplicationTypes.Simulation ), this,
				"user:Logs/WinFormsAppExample.log", true, null, null, null, null ) )
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

			const string startMapName = "Maps\\MainMenu\\Map.map";

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
			WinFormsAppWorld.MapLoad( startMapName, true );

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

		private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			//NeoAxis shutdown
			WinFormsAppWorld.Shutdown();
		}

		private void buttonExit_Click( object sender, EventArgs e )
		{
			Close();
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
			//free camera rotating
			if( freeCameraEnabled && e.Button == MouseButtons.Right )
			{
				freeCameraMouseRotating = true;
				freeCameraRotatingStartPos = renderTargetUserControl1.GetFloatMousePosition();
				renderTargetUserControl1.MouseRelativeMode = true;
			}
		}

		void renderTargetUserControl1_MouseUp( object sender, MouseEventArgs e )
		{
			//free camera rotating
			if( freeCameraEnabled && e.Button == MouseButtons.Right && freeCameraMouseRotating )
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
			labelNetworkStatus.Text = status.ToString();

			//moving free camera by keys
			if( Map.Instance != null && freeCameraEnabled )
			{
				float cameraVelocity = 20;

				Vec3 pos = freeCameraPosition;
				SphereDir dir = freeCameraDirection;

				float step = cameraVelocity * delta;

				if( renderTargetUserControl1.IsKeyPressed( Keys.W ) ||
					renderTargetUserControl1.IsKeyPressed( Keys.Up ) )
				{
					pos += dir.GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Keys.S ) ||
					renderTargetUserControl1.IsKeyPressed( Keys.Down ) )
				{
					pos -= dir.GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Keys.A ) ||
					renderTargetUserControl1.IsKeyPressed( Keys.Left ) )
				{
					pos += new SphereDir(
						dir.Horizontal + MathFunctions.PI / 2, 0 ).GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Keys.D ) ||
					renderTargetUserControl1.IsKeyPressed( Keys.Right ) )
				{
					pos += new SphereDir(
						dir.Horizontal - MathFunctions.PI / 2, 0 ).GetVector() * step;
				}
				if( renderTargetUserControl1.IsKeyPressed( Keys.Q ) )
					pos += new Vec3( 0, 0, step );
				if( renderTargetUserControl1.IsKeyPressed( Keys.E ) )
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
			//   RenderEntityOverCursor( camera );
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

		private void buttonAdditionalForm_Click( object sender, EventArgs e )
		{
			AdditionalForm form = new AdditionalForm();
			form.ShowDialog();
		}

		private void buttonCreateBox_Click( object sender, EventArgs e )
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

		private void trackBarVolume_Scroll( object sender, EventArgs e )
		{
			UpdateVolume();
		}

		void UpdateVolume()
		{
			float volume = (float)trackBarVolume.Value / (float)trackBarVolume.Maximum;
			SoundWorld.Instance.MasterChannelGroup.Volume = volume;
		}

		private void buttonShowUI_Click( object sender, EventArgs e )
		{
			//close if already created
			foreach( Engine.UISystem.Control control in renderTargetUserControl1.ControlManager.Controls )
			{
				if( control is WinFormsAppExampleHUD )
				{
					control.SetShouldDetach();
					return;
				}
			}

			//create
			WinFormsAppExampleHUD window = new WinFormsAppExampleHUD();
			renderTargetUserControl1.ControlManager.Controls.Add( window );
		}

		private void buttonConnect_Click( object sender, EventArgs e )
		{
			WinFormsAppWorld.WorldDestroy();
			if( ExampleEngineApp.Instance != null )
				ExampleEngineApp.Instance.Client_DisconnectFromServer();

			string host = textBoxServerAddress.Text;
			int port = 56565;
			string userName = textBoxUserName.Text;
			string password = "";
			ExampleEngineApp.Instance.TryConnectToServer( host, port, userName, password );
		}

		private void buttonLoadMap_Click( object sender, EventArgs e )
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

				WinFormsAppWorld.MapLoad( mapName, true );

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

		private void buttonDestroy_Click( object sender, EventArgs e )
		{
			WinFormsAppWorld.WorldDestroy();
			if( ExampleEngineApp.Instance != null )
				ExampleEngineApp.Instance.Client_DisconnectFromServer();
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

	}
}