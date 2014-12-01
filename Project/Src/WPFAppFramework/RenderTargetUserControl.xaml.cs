// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Engine;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.Utils;
using Engine.Renderer;

namespace WPFAppFramework
{
	/// <summary>
	/// Interaction logic for RenderTargetUserControl.xaml
	/// </summary>
	public partial class RenderTargetUserControl : UserControl
	{
		//camera settings
		Range cameraNearFarClipDistance = new Range( .1f, 1000 );
		Vec3 cameraPosition;
		Vec3 cameraFixedUp = Vec3.ZAxis;
		Vec3 cameraDirection = Vec3.XAxis;
		Degree cameraFov = 80;
		ProjectionTypes cameraProjectionType = ProjectionTypes.Perspective;
		float cameraOrthoWindowHeight = 100;

		//render texture and camera
		Viewport viewport;
		Camera camera;
		Texture texture;
		RenderTexture renderTexture;
		bool needCreateRenderTarget;
		Vec2I currentTextureSize;
		byte[] tempRenderTextureArray;

		//update render target
		float automaticUpdateFPS = 30;
		System.Windows.Forms.Timer updateTimer;
		float lastRenderTime;

		//keys
		Set<Key> keys = new Set<Key>();

		//MouseRelativeMode
		bool mouseRelativeMode;
		Vec2I mouseRelativeModeStartPosition;
		bool mouseRelativeModeIgnoreFirst;

		GuiRenderer guiRenderer;
		ScreenControlManager controlManager;

		ViewRenderTargetListener renderTargetListener;

		bool allowUsingD3DImage;
		bool d3dImageIsSupported;
		D3DImage d3dImage;

		///////////////////////////////////////////

		public delegate void TickDelegate( RenderTargetUserControl sender, float delta );
		public event TickDelegate Tick;

		public delegate void RenderDelegate( RenderTargetUserControl sender, Camera camera );
		public event RenderDelegate Render;

		public delegate void RenderUIDelegate( RenderTargetUserControl sender, GuiRenderer renderer );
		public event RenderUIDelegate RenderUI;

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
		}

		///////////////////////////////////////////

		[StructLayout( LayoutKind.Sequential )]
		struct GetD3D9HardwarePixelBufferData
		{
			public IntPtr hardwareBuffer;
			public IntPtr outPointer;
		}

		///////////////////////////////////////////

		public RenderTargetUserControl()
		{
			InitializeComponent();
		}

		private void RenderTarget_Loaded( object sender, RoutedEventArgs e )
		{
			if( RenderSystem.Instance != null )
			{
				d3dImageIsSupported = false;
				if( RenderSystem.Instance.IsDirect3D() && RendererWorld.InitializationOptions.AllowDirectX9Ex &&
					System.Environment.OSVersion.Version.Major >= 6 )
				{
					d3dImageIsSupported = _Direct3D9Utils.CheckD3DEx();
				}

				RenderSystem.Instance.RenderSystemEvent += RenderSystem_RenderSystemEvent;
			}
		}

		void RenderSystem_RenderSystemEvent( RenderSystemEvents name )
		{
			if( name == RenderSystemEvents.DeviceLost || name == RenderSystemEvents.DeviceRestored )
			{
				DestroyRenderTarget();
				needCreateRenderTarget = true;
			}
		}

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

		protected override void OnInitialized( EventArgs e )
		{
			base.OnInitialized( e );

			needCreateRenderTarget = true;

			float interval = ( automaticUpdateFPS != 0 ) ?
				( ( 1.0f / automaticUpdateFPS ) * 1000.0f ) : 100;
			updateTimer = new System.Windows.Forms.Timer();
			updateTimer.Interval = (int)interval;
			updateTimer.Tick += updateTimer_Tick;
			updateTimer.Enabled = true;

			WPFAppWorld.renderTargetUserControls.Add( this );
		}

		protected virtual void OnDestroy()
		{
			if( updateTimer != null )
			{
				updateTimer.Stop();
				updateTimer = null;
			}

			if( guiRenderer != null )
			{
				guiRenderer.Dispose();
				guiRenderer = null;
			}
			controlManager = null;

			DestroyRenderTarget();

			WPFAppWorld.renderTargetUserControls.Remove( this );
		}

		public void Destroy()
		{
			OnDestroy();
		}

		void updateTimer_Tick( object sender, EventArgs e )
		{
			if( WPFAppWorld.DuringWarningOrErrorMessageBox )
				return;

			if( automaticUpdateFPS != 0 )
				InvalidateVisual();
		}

		double GetDPIFactor()
		{
			Matrix m = PresentationSource.FromVisual( this ).CompositionTarget.TransformToDevice;
			return 1.0 / m.M11;
		}

		Vec2I GetDemandTextureSize()
		{
			double dpiFactor = GetDPIFactor();

			Vec2I size = new Vec2I(
				(int)( ActualWidth / dpiFactor + .999 ),
				(int)( ActualHeight / dpiFactor + .999 ) );

			return size;
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

			Vec2I textureSize = GetDemandTextureSize();
			if( textureSize.X < 1 || textureSize.Y < 1 )
				return false;

			string textureName = TextureManager.Instance.GetUniqueName( "WPFRenderTexture" );

			int hardwareFSAA = 0;
			if( !RendererWorld.InitializationOptions.AllowSceneMRTRendering )
			{
				if( !int.TryParse( RendererWorld.InitializationOptions.FullSceneAntialiasing, out hardwareFSAA ) )
					hardwareFSAA = 0;
			}

			texture = TextureManager.Instance.Create( textureName, Texture.Type.Type2D, textureSize,
				1, 0, Engine.Renderer.PixelFormat.R8G8B8, Texture.Usage.RenderTarget, false, hardwareFSAA );

			if( texture == null )
				return false;

			currentTextureSize = textureSize;

			renderTexture = texture.GetBuffer().GetRenderTarget();
			renderTexture.AutoUpdate = false;
			renderTexture.AllowAdditionalMRTs = true;

			camera = SceneManager.Instance.CreateCamera(
				SceneManager.Instance.GetUniqueCameraName( "UserControl" ) );
			camera.Purpose = Camera.Purposes.MainCamera;

			//update camera settings
			camera.NearClipDistance = cameraNearFarClipDistance.Minimum;
			camera.FarClipDistance = cameraNearFarClipDistance.Maximum;
			camera.AspectRatio = (float)texture.Size.X / (float)texture.Size.Y;
			camera.FixedUp = cameraFixedUp;
			camera.Position = cameraPosition;
			camera.Direction = cameraDirection;
			camera.Fov = cameraFov;
			camera.ProjectionType = cameraProjectionType;
			camera.OrthoWindowHeight = cameraOrthoWindowHeight;

			viewport = renderTexture.AddViewport( camera );

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
			renderTexture.AddListener( renderTargetListener );

			if( guiRenderer == null )
				guiRenderer = new GuiRenderer( viewport );
			else
				guiRenderer.ChangeViewport( viewport );

			if( controlManager == null )
				controlManager = new ScreenControlManager( guiRenderer );

			//initialize D3DImage output
			if( d3dImageIsSupported && allowUsingD3DImage )
			{
				// create a D3DImage to host the scene and monitor it for changes in front buffer availability
				if( d3dImage == null )
				{
					d3dImage = new D3DImage();
					d3dImage.IsFrontBufferAvailableChanged += D3DImage_IsFrontBufferAvailableChanged;
					CompositionTarget.Rendering += D3DImage_OnRendering;
				}

				// set output to background image
				Background = new ImageBrush( d3dImage );

				// set the back buffer using the new scene pointer
				HardwarePixelBuffer buffer = texture.GetBuffer( 0, 0 );
				GetD3D9HardwarePixelBufferData data = new GetD3D9HardwarePixelBufferData();
				data.hardwareBuffer = buffer._GetRealObject();
				data.outPointer = IntPtr.Zero;
				unsafe
				{
					GetD3D9HardwarePixelBufferData* pData = &data;
					if( !RenderSystem.Instance.CallCustomMethod( "Get D3D9HardwarePixelBuffer getSurface", (IntPtr)pData ) )
						Log.Fatal( "Get D3D9HardwarePixelBuffer getSurface failed." );
				}
				d3dImage.Lock();
				d3dImage.SetBackBuffer( D3DResourceType.IDirect3DSurface9, data.outPointer );
				d3dImage.Unlock();
			}

			return true;
		}

		void DestroyRenderTarget()
		{
			if( renderTexture != null )
			{
				// set output to background image
				Background = null;

				if( d3dImage != null )
				{
					d3dImage.Lock();
					d3dImage.SetBackBuffer( D3DResourceType.IDirect3DSurface9, IntPtr.Zero );
					d3dImage.Unlock();
				}

				if( renderTargetListener != null )
				{
					renderTexture.RemoveListener( renderTargetListener );
					renderTargetListener.Dispose();
					renderTargetListener = null;
				}

				viewport.Dispose();
				viewport = null;

				camera.Dispose();
				camera = null;

				texture.Dispose();
				texture = null;

				renderTexture = null;
			}
		}

		void RenderScene( DrawingContext drawingContext )
		{
			if( WPFAppWorld.DuringWarningOrErrorMessageBox )
				return;

			WPFAppWorld.duringOnRender = true;

			try
			{
				//create render target
				if( renderTexture != null && !needCreateRenderTarget )
				{
					if( currentTextureSize != GetDemandTextureSize() )
						needCreateRenderTarget = true;
				}
				//create render window
				if( needCreateRenderTarget )
				{
					if( CreateRenderTarget() )
						needCreateRenderTarget = false;
				}

				//paint
				if( renderTexture != null )
				{
					float time = EngineApp.Instance.Time;
					if( lastRenderTime == 0 )
						lastRenderTime = time;

					float step = time - lastRenderTime;
					lastRenderTime = time;

					OnTick( step );

					//tick and entity world tick
					if( WPFAppEngineApp.Instance != null )
						WPFAppEngineApp.Instance.DoTick();

					//update

					if( renderTexture.Size.X != 0 && renderTexture.Size.Y != 0 )
						camera.AspectRatio = (float)renderTexture.Size.X / (float)renderTexture.Size.Y;
					if( renderTexture.Size.X != 0 && renderTexture.Size.Y != 0 )
						guiRenderer.AspectRatio = (float)renderTexture.Size.X / (float)renderTexture.Size.Y;

					renderTexture.Update( true );

					//render to control
					if( drawingContext != null )
					{
						if( !RenderSystem.Instance.IsDeviceLost() )
						{
							int width = texture.Size.X;
							int height = texture.Size.Y;

							if( tempRenderTextureArray == null || tempRenderTextureArray.Length != width * height * 4 )
								tempRenderTextureArray = new byte[ width * height * 4 ];

							renderTexture.WriteContentsToMemory( tempRenderTextureArray,
								Engine.Renderer.PixelFormat.X8R8G8B8 );

							BitmapSource bitmapSource = BitmapSource.Create( width, height, 1, 1, PixelFormats.Bgr32,
								null, tempRenderTextureArray, width * 4 );

							drawingContext.DrawImage( bitmapSource, new System.Windows.Rect( 0, 0,
								(double)renderTexture.Size.X * GetDPIFactor(),
								(double)renderTexture.Size.Y * GetDPIFactor() ) );
						}
					}
				}

				if( drawingContext != null )
					base.OnRender( drawingContext );

			}
			finally
			{
				WPFAppWorld.duringOnRender = false;
			}
		}

		protected override void OnRender( DrawingContext drawingContext )
		{
			//create render target
			if( renderTexture != null && !needCreateRenderTarget )
			{
				if( currentTextureSize != GetDemandTextureSize() )
					needCreateRenderTarget = true;
			}
			if( needCreateRenderTarget )
			{
				if( CreateRenderTarget() )
					needCreateRenderTarget = false;
			}

			if( !d3dImageIsSupported || !allowUsingD3DImage )
				RenderScene( drawingContext );
		}

		protected virtual void OnTick( float delta )
		{
			//reset MousePosition for relative mode
			if( mouseRelativeMode )
			{
				Point point = PointToScreen(
					new Point( mouseRelativeModeStartPosition.X, mouseRelativeModeStartPosition.Y ) );
				System.Windows.Forms.Cursor.Position = new System.Drawing.Point( (int)point.X, (int)point.Y );
			}

			if( Tick != null )
				Tick( this, delta );

			if( controlManager != null )
				controlManager.DoTick( delta );
		}

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

		bool GetEKeyByKeyCode( Key keyCode, out EKeys eKey )
		{
			int virtualKey = (int)KeyInterop.VirtualKeyFromKey( keyCode );
			if( Enum.IsDefined( typeof( EKeys ), virtualKey ) )
			{
				eKey = (EKeys)virtualKey;
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

		protected override void OnTextInput( TextCompositionEventArgs e )
		{
			base.OnTextInput( e );

			if( controlManager != null )
			{
				if( e.Text.Length > 0 )
				{
					char keyChar = e.Text[ 0 ];

					KeyPressEvent keyEvent = new KeyPressEvent( keyChar );

					controlManager.DoKeyPress( keyEvent );

					//if (controlManager.DoKeyPress(keyEvent))
					//    e.Handled = true;
				}
			}
		}

		protected override void OnKeyUp( KeyEventArgs e )
		{
			DoKeyUp( e );

			base.OnKeyUp( e );
		}

		EMouseButtons GetEMouseButtonByMouseButton( MouseButton button )
		{
			if( button == MouseButton.Left )
				return EMouseButtons.Left;
			else if( button == MouseButton.Right )
				return EMouseButtons.Right;
			else if( button == MouseButton.Middle )
				return EMouseButtons.Middle;
			else if( button == MouseButton.XButton1 )
				return EMouseButtons.XButton1;
			else
				return EMouseButtons.XButton2;
		}

		protected override void OnMouseDown( MouseButtonEventArgs e )
		{
			Focus();

			base.OnMouseDown( e );

			if( controlManager != null )
				controlManager.DoMouseDown( GetEMouseButtonByMouseButton( e.ChangedButton ) );
		}

		protected override void OnMouseUp( MouseButtonEventArgs e )
		{
			base.OnMouseUp( e );

			if( controlManager != null )
				controlManager.DoMouseUp( GetEMouseButtonByMouseButton( e.ChangedButton ) );
		}

		protected override void OnMouseDoubleClick( MouseButtonEventArgs e )
		{
			base.OnMouseDoubleClick( e );

			if( controlManager != null )
				controlManager.DoMouseDoubleClick( GetEMouseButtonByMouseButton( e.ChangedButton ) );
		}

		protected override void OnMouseWheel( MouseWheelEventArgs e )
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
			Key key = e.Key;

			if( !keys.Contains( key ) )
				keys.Add( key );

			if( controlManager != null )
			{
				EKeys eKey;
				if( GetEKeyByKeyCode( e.Key, out eKey ) )
				{
					KeyEvent keyEvent = new KeyEvent( eKey );

					controlManager.DoKeyDown( keyEvent );

					//if (controlManager.DoKeyDown(keyEvent))
					//    e.Handled = true;
				}
			}
		}

		void DoKeyUp( KeyEventArgs e )
		{
			Key key = e.Key;
			if( !keys.Contains( key ) )
				return;
			keys.Remove( key );

			if( controlManager != null )
			{
				EKeys eKey;
				if( GetEKeyByKeyCode( e.Key, out eKey ) )
				{
					KeyEvent keyEvent = new KeyEvent( eKey );

					controlManager.DoKeyUp( keyEvent );

					//if (controlManager.DoKeyUp(keyEvent))
					//    e.Handled = true;
				}
			}
		}

		void DoKeyUpAll()
		{
			List<Key> tempKeys = new List<Key>();
			foreach( Key key in keys )
				tempKeys.Add( key );

			foreach( Key key in tempKeys )
			{
				DoKeyUp( new KeyEventArgs( Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0,
					key ) );
			}
		}

		protected override void OnLostFocus( RoutedEventArgs e )
		{
			DoKeyUpAll();
			lastRenderTime = 0;

			base.OnLostFocus( e );
		}

		public bool IsKeyPressed( Key key )
		{
			if( !IsFocused )
				DoKeyUpAll();

			return keys.Contains( key );
		}

		Vec2I GetLocalMousePosition()
		{
			Point localPosition = Mouse.GetPosition( this );
			return new Vec2I( (int)localPosition.X, (int)localPosition.Y );
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

		public bool MouseRelativeMode
		{
			get { return mouseRelativeMode; }
			set
			{
				if( mouseRelativeMode == value )
					return;

				mouseRelativeMode = value;

				if( mouseRelativeMode )
				{
					Vec2I centerPos = new Vec2I( (int)( ActualWidth / 2 ), (int)( ActualHeight / 2 ) );
					Point point = PointToScreen( new Point( centerPos.X, centerPos.Y ) );
					System.Windows.Forms.Cursor.Position = new System.Drawing.Point( (int)point.X,
						(int)point.Y );
					mouseRelativeModeStartPosition = centerPos;
					System.Windows.Forms.Cursor.Hide();
					EngineApp.Instance.ShowSystemCursor = false;
					Mouse.Capture( this );
					mouseRelativeModeIgnoreFirst = true;
				}
				else
				{
					Point point = PointToScreen( new Point(
						 mouseRelativeModeStartPosition.X, mouseRelativeModeStartPosition.Y ) );
					System.Windows.Forms.Cursor.Position = new System.Drawing.Point( (int)point.X,
						 (int)point.Y );
					System.Windows.Forms.Cursor.Show();
					EngineApp.Instance.ShowSystemCursor = true;
					Mouse.Capture( null );
				}
			}
		}

		/// <summary>
		/// If zero, then no automatic updates.
		/// </summary>
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
			Point localPosition = Mouse.GetPosition( this );
			Size size = new Size( ActualWidth, ActualHeight );
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

		public ScreenControlManager ControlManager
		{
			get { return controlManager; }
		}

		public Texture Texture
		{
			get { return texture; }
		}

		public RenderTexture RenderTexture
		{
			get { return renderTexture; }
		}

		public Viewport Viewport
		{
			get { return viewport; }
		}

		public Camera Camera
		{
			get { return camera; }
		}

		void D3DImage_OnRendering( object sender, EventArgs e )
		{
			if( renderTexture != null && !needCreateRenderTarget )
			{
				if( currentTextureSize != GetDemandTextureSize() )
					needCreateRenderTarget = true;
			}

			//render scene
			if( d3dImage.IsFrontBufferAvailable && renderTexture != null && !needCreateRenderTarget )
			{
				// lock the D3DImage
				d3dImage.Lock();

				// when WPF's composition target is about to render, we update our 
				// custom render target so that it can be blended with the WPF target
				RenderScene( null );

				// invalidate the updated region of the D3DImage (in this case, the whole image)
				if( texture != null && d3dImage.Height != 0 )
					d3dImage.AddDirtyRect( new Int32Rect( 0, 0, texture.Size.X, texture.Size.Y ) );

				// unlock the D3DImage
				d3dImage.Unlock();
			}
		}

		void D3DImage_IsFrontBufferAvailableChanged( object sender, DependencyPropertyChangedEventArgs e )
		{
			DestroyRenderTarget();
			needCreateRenderTarget = true;
		}

		public bool AllowUsingD3DImage
		{
			get { return allowUsingD3DImage; }
			set
			{
				if( allowUsingD3DImage == value )
					return;
				allowUsingD3DImage = value;

				needCreateRenderTarget = true;
			}
		}
	}
}
