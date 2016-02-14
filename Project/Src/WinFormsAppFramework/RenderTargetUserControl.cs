// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.Utils;

namespace WinFormsAppFramework
{
	public partial class RenderTargetUserControl : UserControl
	{
		//camera settings
		Range cameraNearFarClipDistance = new Range( .1f, 1000 );
		Vec3 cameraPosition;
		Vec3 cameraFixedUp = Vec3.ZAxis;
		Vec3 cameraDirection = Vec3.XAxis;
		Degree cameraFov = 75;
		ProjectionTypes cameraProjectionType = ProjectionTypes.Perspective;
		float cameraOrthoWindowHeight = 100;

		//render window and camera
		RenderWindow renderWindow;
		Viewport viewport;
		Camera camera;
		bool needCreateRenderWindow;
		bool allowCreateRenderWindow = true;

		//update render target
		float automaticUpdateFPS = 30;
		Timer updateTimer;
		float lastRenderTime;

		//keys
		Set<Keys> keys = new Set<Keys>();

		//MouseRelativeMode
		bool mouseRelativeMode;
		Vec2I mouseRelativeModeStartPosition;
		bool mouseRelativeModeIgnoreFirst;

		GuiRenderer guiRenderer;
		ScreenControlManager controlManager;

		ViewRenderTargetListener renderTargetListener;

		///////////////////////////////////////////

		class ViewRenderTargetListener : RenderTargetListener
		{
			RenderTargetUserControl owner;

			public ViewRenderTargetListener( RenderTargetUserControl owner )
			{
				this.owner = owner;
			}

			protected override void OnPreViewportUpdate( RenderTargetViewportEvent evt )
			{
				base.OnPreViewportUpdate( evt );

				if( evt.Source == owner.Viewport )
				{
					owner.OnRender( owner.Camera );
					owner.OnRenderUI( owner.guiRenderer );
				}
			}

			protected override void OnPostViewportUpdate( RenderTargetViewportEvent evt )
			{
				base.OnPostViewportUpdate( evt );

				if( evt.Source == owner.Viewport )
					owner.OnPostRender();
			}
		}

		///////////////////////////////////////////

		public RenderTargetUserControl()
		{
			InitializeComponent();
		}

		[Browsable( false )]
		[DefaultValue( typeof( Range ), "0.1 1000" )]
		public virtual Range CameraNearFarClipDistance
		{
			get { return cameraNearFarClipDistance; }
			set
			{
				cameraNearFarClipDistance = value;
				if( camera != null )
				{
					camera.NearClipDistance = cameraNearFarClipDistance.Minimum;
					camera.FarClipDistance = cameraNearFarClipDistance.Maximum;
				}
			}
		}

		[Browsable( false )]
		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public virtual Vec3 CameraPosition
		{
			get { return cameraPosition; }
			set
			{
				cameraPosition = value;
				if( camera != null )
					camera.Position = cameraPosition;
			}
		}

		[Browsable( false )]
		[DefaultValue( typeof( Vec3 ), "0 0 1" )]
		public virtual Vec3 CameraFixedUp
		{
			get { return cameraFixedUp; }
			set
			{
				cameraFixedUp = value;
				if( camera != null )
					camera.FixedUp = cameraFixedUp;
			}
		}

		[Browsable( false )]
		[DefaultValue( typeof( Vec3 ), "1 0 0" )]
		public virtual Vec3 CameraDirection
		{
			get { return cameraDirection; }
			set
			{
				cameraDirection = value;
				if( camera != null )
					camera.Direction = cameraDirection;
			}
		}

		[Browsable( false )]
		[DefaultValue( typeof( Degree ), "75" )]
		public virtual Degree CameraFov
		{
			get { return cameraFov; }
			set
			{
				cameraFov = value;
				if( camera != null )
					camera.Fov = cameraFov;
			}
		}

		[Browsable( false )]
		[DefaultValue( ProjectionTypes.Perspective )]
		public virtual ProjectionTypes CameraProjectionType
		{
			get { return cameraProjectionType; }
			set
			{
				cameraProjectionType = value;
				if( camera != null )
					camera.ProjectionType = cameraProjectionType;
			}
		}

		[Browsable( false )]
		[DefaultValue( 100.0f )]
		public virtual float CameraOrthoWindowHeight
		{
			get { return cameraOrthoWindowHeight; }
			set
			{
				cameraOrthoWindowHeight = value;
				if( camera != null )
					camera.OrthoWindowHeight = cameraOrthoWindowHeight;
			}
		}

		[Browsable( false )]
		public bool AllowCreateRenderWindow
		{
			get { return allowCreateRenderWindow; }
			set { allowCreateRenderWindow = value; }
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			needCreateRenderWindow = true;

			float interval = ( automaticUpdateFPS != 0 ) ?
				( ( 1.0f / automaticUpdateFPS ) * 1000.0f ) : 100;
			updateTimer = new Timer();
			updateTimer.Interval = (int)interval;
			updateTimer.Tick += updateTimer_Tick;
			updateTimer.Enabled = true;

			WinFormsAppWorld.renderTargetUserControls.Add( this );
		}

		protected virtual void OnDestroy()
		{
			if( updateTimer != null )
			{
				updateTimer.Dispose();
				updateTimer = null;
			}

			if( guiRenderer != null )
			{
				guiRenderer.Dispose();
				guiRenderer = null;
			}
			controlManager = null;

			DestroyRenderTarget();

			WinFormsAppWorld.renderTargetUserControls.Remove( this );
		}

		public void Destroy()
		{
			OnDestroy();
		}

		protected override void WndProc( ref Message m )
		{
			const int WM_DESTROY = 0x0002;

			switch( m.Msg )
			{
			case WM_DESTROY:
				Destroy();
				break;
			}

			base.WndProc( ref m );
		}

		void updateTimer_Tick( object sender, EventArgs e )
		{
			if( WinFormsAppWorld.DuringWarningOrErrorMessageBox )
				return;

			if( automaticUpdateFPS != 0 )
				Invalidate();
		}

		bool IsActivateFXAAByDefault()
		{
			if( RenderSystem.Instance.HasShaderModel3() )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_G80 ||
						RenderSystem.Instance.GPUCodeName == GPUCodeNames.GeForce_Unknown )
					{
						return true;
					}
				}
				if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R600 ||
						RenderSystem.Instance.GPUCodeName == GPUCodeNames.Radeon_Unknown )
					{
						return true;
					}
				}
				if( RenderSystem.Instance.GPUIsIntel() )
				{
					if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Intel_HDGraphics )
						return true;
				}
			}

			return false;
		}

		void InitializeFXAACompositor()
		{
			CompositorInstance instance = viewport.AddCompositor( "FXAA" );
			if( instance != null )
				instance.Enabled = true;
		}

		bool CreateRenderTarget()
		{
			DestroyRenderTarget();

			if( RendererWorld.Instance == null )
				return false;

			Vec2I size = new Vec2I( ClientRectangle.Size.Width, ClientRectangle.Size.Height );
			if( size.X < 1 || size.Y < 1 )
				return false;

			renderWindow = RendererWorld.Instance.CreateRenderWindow( Handle, size );
			if( renderWindow == null )
				return false;

			renderWindow.AutoUpdate = false;
			renderWindow.AllowAdditionalMRTs = true;

			camera = SceneManager.Instance.CreateCamera(
				SceneManager.Instance.GetUniqueCameraName( "UserControl" ) );
			camera.Purpose = Camera.Purposes.MainCamera;

			//update camera settings
			camera.NearClipDistance = cameraNearFarClipDistance.Minimum;
			camera.FarClipDistance = cameraNearFarClipDistance.Maximum;
			camera.AspectRatio = (float)renderWindow.Size.X / (float)renderWindow.Size.Y;
			camera.FixedUp = cameraFixedUp;
			camera.Position = cameraPosition;
			camera.Direction = cameraDirection;
			camera.Fov = cameraFov;
			camera.ProjectionType = cameraProjectionType;
			camera.OrthoWindowHeight = cameraOrthoWindowHeight;

			viewport = renderWindow.AddViewport( camera );

			//Initialize HDR compositor for HDR render technique
			if( EngineApp.RenderTechnique == "HDR" )
			{
				viewport.AddCompositor( "HDR", 0 );
				viewport.SetCompositorEnabled( "HDR", true );
			}

			//Initialize Fast Approximate Antialiasing (FXAA)
			{
				bool useMRT = RendererWorld.InitializationOptions.AllowSceneMRTRendering;
				string fsaa = RendererWorld.InitializationOptions.FullSceneAntialiasing;
				if( ( useMRT && ( fsaa == "" || fsaa == "RecommendedSetting" ) && IsActivateFXAAByDefault() ) ||
					fsaa == "FXAA" )
				{
					if( RenderSystem.Instance.HasShaderModel3() )
						InitializeFXAACompositor();
				}
			}

			//add listener
			renderTargetListener = new ViewRenderTargetListener( this );
			renderWindow.AddListener( renderTargetListener );

			if( guiRenderer == null )
				guiRenderer = new GuiRenderer( viewport );
			else
				guiRenderer.ChangeViewport( viewport );

			if( controlManager == null )
				controlManager = new ScreenControlManager( guiRenderer );

			return true;
		}

		public void DestroyRenderTarget()
		{
			if( renderWindow != null )
			{
				if( renderTargetListener != null )
				{
					renderWindow.RemoveListener( renderTargetListener );
					renderTargetListener.Dispose();
					renderTargetListener = null;
				}

				viewport.Dispose();
				viewport = null;

				camera.Dispose();
				camera = null;

				renderWindow.Dispose();
				renderWindow = null;
			}
		}

		protected override void OnResize( EventArgs e )
		{
			base.OnResize( e );

			needCreateRenderWindow = true;
			//if( renderWindow != null )
			//{
			//   //update render window
			//   Vec2I size = new Vec2I( ClientRectangle.Size.Width, ClientRectangle.Size.Height );
			//   if( size.X >= 1 && size.Y >= 1 )
			//   {
			//      renderWindow.WindowMovedOrResized( false, size );
			//      camera.AspectRatio = (float)renderWindow.Size.X / (float)renderWindow.Size.Y;
			//   }
			//}

			Invalidate();
		}

		protected override void OnPaintBackground( PaintEventArgs e )
		{
			if( renderWindow != null && !WinFormsAppWorld.DuringWarningOrErrorMessageBox )
				return;

			base.OnPaintBackground( e );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint( e );

			if( RenderSystem.Instance == null )
				return;
			if( WinFormsAppWorld.DuringWarningOrErrorMessageBox )
				return;
			if( RenderSystem.Instance.IsDeviceLostByTestCooperativeLevel() )
				return;

			//create render window
			if( needCreateRenderWindow && allowCreateRenderWindow && EngineApp.Instance != null && EngineApp.Instance.IsCreated )
			{
				if( CreateRenderTarget() )
					needCreateRenderWindow = false;
			}

			if( renderWindow != null )
			{
				float time = EngineApp.Instance.Time;
				if( lastRenderTime == 0 )
					lastRenderTime = time;

				float step = time - lastRenderTime;
				lastRenderTime = time;

				OnTick( step );

				//tick and entity world tick
				if( WinFormsAppEngineApp.Instance != null )
					WinFormsAppEngineApp.Instance.DoTick();

				if( renderWindow.Size.X != 0 && renderWindow.Size.Y != 0 )
					camera.AspectRatio = (float)renderWindow.Size.X / (float)renderWindow.Size.Y;
				if( renderWindow.Size.X != 0 && renderWindow.Size.Y != 0 )
					guiRenderer.AspectRatio = (float)renderWindow.Size.X / (float)renderWindow.Size.Y;

				//update
				renderWindow.Update( true );
			}
		}

		public delegate void TickDelegate( RenderTargetUserControl sender, float delta );
		public event TickDelegate Tick;

		protected virtual void OnTick( float delta )
		{
			//reset MousePosition for relative mode
			if( mouseRelativeMode )
			{
				Cursor.Position = PointToScreen( new Point(
					mouseRelativeModeStartPosition.X, mouseRelativeModeStartPosition.Y ) );
			}

			if( Tick != null )
				Tick( this, delta );

			if( controlManager != null )
				controlManager.DoTick( delta );
		}

		public delegate void RenderDelegate( RenderTargetUserControl sender, Camera camera );
		public event RenderDelegate Render;

		public delegate void PostRenderDelegate( RenderTargetUserControl sender );
		public event PostRenderDelegate PostRender;

		public delegate void RenderUIDelegate( RenderTargetUserControl sender, GuiRenderer renderer );
		public event RenderUIDelegate RenderUI;

		protected virtual void OnRender( Camera camera )
		{
			if( Render != null )
				Render( this, camera );

			if( controlManager != null )
				controlManager.DoRender();
		}

		protected virtual void OnRenderUI( GuiRenderer renderer )
		{
			if( RenderUI != null )
				RenderUI( this, renderer );

			if( Map.Instance != null )
				Map.Instance.DoRenderUI( renderer );

			if( controlManager != null )
				controlManager.DoRenderUI( guiRenderer );
		}

		protected virtual void OnPostRender()
		{
			if( PostRender != null )
				PostRender( this );
		}

		bool GetEKeyByKeyCode( Keys keyCode, out EKeys eKey )
		{
			if( Enum.IsDefined( typeof( EKeys ), (int)keyCode ) )
			{
				eKey = (EKeys)(int)keyCode;
				return true;
			}
			else
			{
				eKey = EKeys.Cancel;
				return false;
			}
		}

		protected override void OnKeyDown( KeyEventArgs e )
		{
			base.OnKeyDown( e );

			DoKeyDown( e );
		}

		protected override void OnKeyPress( KeyPressEventArgs e )
		{
			base.OnKeyPress( e );

			if( controlManager != null )
			{
				KeyPressEvent keyEvent = new KeyPressEvent( e.KeyChar );
				if( controlManager.DoKeyPress( keyEvent ) )
					e.Handled = true;
			}
		}

		protected override void OnKeyUp( KeyEventArgs e )
		{
			DoKeyUp( e );
			base.OnKeyUp( e );
		}

		EMouseButtons GetEMouseButtonByMouseButton( MouseButtons button )
		{
			if( button == MouseButtons.Left )
				return EMouseButtons.Left;
			else if( button == MouseButtons.Right )
				return EMouseButtons.Right;
			else if( button == MouseButtons.Middle )
				return EMouseButtons.Middle;
			else if( button == MouseButtons.XButton1 )
				return EMouseButtons.XButton1;
			else
				return EMouseButtons.XButton2;
		}

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );

			if( controlManager != null )
				controlManager.DoMouseDown( GetEMouseButtonByMouseButton( e.Button ) );
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );

			if( controlManager != null )
				controlManager.DoMouseUp( GetEMouseButtonByMouseButton( e.Button ) );
		}

		protected override void OnMouseDoubleClick( MouseEventArgs e )
		{
			base.OnMouseDoubleClick( e );

			if( controlManager != null )
				controlManager.DoMouseDoubleClick( GetEMouseButtonByMouseButton( e.Button ) );
		}

		protected override void OnMouseWheel( MouseEventArgs e )
		{
			base.OnMouseWheel( e );

			if( controlManager != null )
				controlManager.DoMouseWheel( e.Delta );
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			base.OnMouseMove( e );

			if( controlManager != null )
				controlManager.DoMouseMove( GetFloatMousePosition() );
		}

		void DoKeyDown( KeyEventArgs e )
		{
			Keys key = e.KeyCode;
			if( !keys.Contains( key ) )
				keys.Add( key );

			if( controlManager != null )
			{
				EKeys eKey;
				if( GetEKeyByKeyCode( e.KeyCode, out eKey ) )
				{
					KeyEvent keyEvent = new KeyEvent( eKey );
					if( controlManager.DoKeyDown( keyEvent ) )
						e.Handled = true;
					if( keyEvent.SuppressKeyPress )
						e.SuppressKeyPress = true;
				}
			}
		}

		void DoKeyUp( KeyEventArgs e )
		{
			Keys key = e.KeyCode;
			if( !keys.Contains( key ) )
				return;
			keys.Remove( key );

			if( controlManager != null )
			{
				EKeys eKey;
				if( GetEKeyByKeyCode( e.KeyCode, out eKey ) )
				{
					KeyEvent keyEvent = new KeyEvent( eKey );
					if( controlManager.DoKeyUp( keyEvent ) )
						e.Handled = true;
					if( keyEvent.SuppressKeyPress )
						e.SuppressKeyPress = true;
				}
			}
		}

		void DoKeyUpAll()
		{
			List<Keys> tempKeys = new List<Keys>();
			foreach( Keys key in keys )
				tempKeys.Add( key );

			foreach( Keys key in tempKeys )
				DoKeyUp( new KeyEventArgs( (Keys)key ) );
		}

		protected override void OnLeave( EventArgs e )
		{
			DoKeyUpAll();
			lastRenderTime = 0;

			base.OnLeave( e );
		}

		public bool IsKeyPressed( Keys key )
		{
			if( !Focused )
				DoKeyUpAll();

			return keys.Contains( key );
		}

		Vec2I GetLocalMousePosition()
		{
			Point localPosition = PointToClient( MousePosition );
			return new Vec2I( localPosition.X, localPosition.Y );
		}

		public Vec2I GetMouseRelativeModeOffset()
		{
			if( !mouseRelativeMode )
				return Vec2I.Zero;
			if( mouseRelativeModeIgnoreFirst )
			{
				mouseRelativeModeIgnoreFirst = false;
				return Vec2I.Zero;
			}
			return GetLocalMousePosition() - mouseRelativeModeStartPosition;
		}

		[Browsable( false )]
		public bool MouseRelativeMode
		{
			get { return mouseRelativeMode; }
			set
			{
				if( mouseRelativeMode == value )
					return;

				mouseRelativeMode = value;

				if( EngineApp.Instance != null )
				{
					if( mouseRelativeMode )
					{
						Cursor.Position = PointToScreen(
							new Point( ClientRectangle.Size.Width / 2, ClientRectangle.Size.Height / 2 ) );
						mouseRelativeModeStartPosition = GetLocalMousePosition();
						Cursor.Hide();
						EngineApp.Instance.ShowSystemCursor = false;
						Capture = true;
						mouseRelativeModeIgnoreFirst = true;
					}
					else
					{
						Cursor.Position = PointToScreen( new Point(
							mouseRelativeModeStartPosition.X, mouseRelativeModeStartPosition.Y ) );
						Cursor.Show();
						EngineApp.Instance.ShowSystemCursor = true;
						Capture = false;
					}
				}
			}
		}

		/// <summary>
		/// If zero, then no automatic updates.
		/// </summary>
		[Browsable( false )]
		public float AutomaticUpdateFPS
		{
			get { return automaticUpdateFPS; }
			set
			{
				automaticUpdateFPS = value;

				if( updateTimer != null )
				{
					float interval = ( automaticUpdateFPS != 0 ) ?
						( ( 1.0f / automaticUpdateFPS ) * 1000.0f ) : 100;
					updateTimer.Interval = (int)interval;
				}
			}
		}

		public Vec2 GetFloatMousePosition()
		{
			Point localPosition = PointToClient( MousePosition );
			Size size = ClientRectangle.Size;
			if( size.Width == 0 || size.Height == 0 )
				return Vec2.Zero;
			return new Vec2(
				(float)localPosition.X / (float)size.Width,
				(float)localPosition.Y / (float)size.Height );
		}

		public Ray GetWorldRayByMousePosition()
		{
			if( camera == null )
				return new Ray( Vec3.Zero, Vec3.Zero );

			return camera.GetCameraToViewportRay( GetFloatMousePosition() );
		}

		[Browsable( false )]
		public ScreenControlManager ControlManager
		{
			get { return controlManager; }
		}

		[Browsable( false )]
		public RenderWindow RenderWindow
		{
			get { return renderWindow; }
		}

		[Browsable( false )]
		public Viewport Viewport
		{
			get { return viewport; }
		}

		[Browsable( false )]
		public Camera Camera
		{
			get { return camera; }
		}
	}
}
