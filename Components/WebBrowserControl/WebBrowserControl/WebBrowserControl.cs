// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Drawing.Design;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.UISystem;
using Engine.FileSystem;
using Engine.Utils;
using WebBrowserControl.Properties;
using Xilium.CefGlue;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.UISystem
{
	public class WebBrowserControl : Control
	{
		string startUrl = "http://www.neoaxis.com";
		string startFile = "";
		int inGame3DGuiHeightInPixels = 800;
		bool inGame3DGuiMipmaps;
		float zoom = 0;

		CefBrowser browser;
		CefBrowserHost browserHost;

		Vec2I viewSize;

		Texture texture;
		Vec2I textureSize;
		bool needUpdateTexture;
		bool needInvalidate = true;
		bool needRecreateTexture;

		string title;

		object renderBufferLock = new object();
		byte[] renderBuffer;
		Vec2I renderBufferForSize;

		//Thread mainThread;

		static bool cefRuntimeInitialized;
		static bool cefRuntimeUnableToLoad;
		static string cefRuntimeUnableToLoadError;
		//!!!!new
		static bool subscribedToEngineAppDestroy;


		[Category( "WebBrowser" )]
		[DefaultValue( "http://www.neoaxis.com" )]
		[Serialize]
		public string StartURL
		{
			get { return startUrl; }
			set
			{
				if( startUrl == value )
					return;
				startUrl = value;

				if( string.IsNullOrEmpty( startFile ) )
					LoadURL( startUrl );
			}
		}

		[Category( "WebBrowser" )]
		[DefaultValue( "" )]
		[Serialize]
		public string StartFile
		{
			get { return startFile; }
			set
			{
				if( startFile == value )
					return;
				startFile = value;

				if( !string.IsNullOrEmpty( startFile ) )
					LoadFileByVirtualFileName( startFile );
				else
					LoadURL( startUrl );
			}
		}

		[Category( "WebBrowser" )]
		[DefaultValue( 800 )]
		[Serialize]
		public int InGame3DGuiHeightInPixels
		{
			get { return inGame3DGuiHeightInPixels; }
			set
			{
				if( inGame3DGuiHeightInPixels == value )
					return;
				if( value < 1 )
					value = 1;

				inGame3DGuiHeightInPixels = value;
			}
		}

		[Category( "WebBrowser" )]
		[DefaultValue( false )]
		[Serialize]
		public bool InGame3DGuiMipmaps
		{
			get { return inGame3DGuiMipmaps; }
			set
			{
				if( inGame3DGuiMipmaps == value )
					return;
				inGame3DGuiMipmaps = value;
				needRecreateTexture = true;
			}
		}

		[Category( "WebBrowser" )]
		[DefaultValue( 0.0f )]
		[Serialize]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( -10.0f, 10.0f )]
		public float Zoom
		{
			get { return zoom; }
			set
			{
				if( zoom == value )
					return;
				zoom = value;

				if( browserHost != null )
				{
					browserHost.SetZoomLevel( zoom );
					needInvalidate = true;
				}
			}
		}

		[Browsable( false )]
		public override bool CanFocus
		{
			get { return IsEnabledInHierarchy(); }
		}

		[Browsable( false )]
		public CefBrowser Browser { get { return browser; } }

		#region Initialization

		protected override void OnAttach()
		{
			base.OnAttach();

			//mainThread = Thread.CurrentThread;
			RenderSystem.Instance.RenderSystemEvent += RenderSystem_RenderSystemEvent;
		}

		protected override void OnDetach()
		{
			if( RenderSystem.Instance != null )
				RenderSystem.Instance.RenderSystemEvent -= RenderSystem_RenderSystemEvent;

			DestroyBrowser();

			//never called
			//WebCore.Shutdown();

			if( texture != null )
			{
				texture.Dispose();
				texture = null;
			}

			base.OnDetach();
		}

		void RenderSystem_RenderSystemEvent( RenderSystemEvents name )
		{
			if( name == RenderSystemEvents.DeviceRestored )
				needUpdateTexture = true;
		}

		static void InitializeCefRuntime()
		{
			if( cefRuntimeUnableToLoad )
				return;
			if( !IsSupportedByThisPlatform() )
				return;

			if( cefRuntimeInitialized )
				throw new InvalidOperationException( "The CefRuntime is already initialized. Call ShutdownCefRuntime() before initializing it again." );

			//load native libraries
			{
				if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
				{
					try
					{
						string error = NativeLibraryManager.PreLoadLibrary( Path.Combine( "CefGlue", "libcef" ), true );
						if( !string.IsNullOrEmpty( error ) )
						{
							cefRuntimeUnableToLoad = true;
							cefRuntimeUnableToLoadError = error;
						}
					}
					catch
					{
						cefRuntimeUnableToLoad = true;
						cefRuntimeUnableToLoadError = "Crash inside NativeLibraryManager.PreLoadLibrary.";
					}
				}
			}

			//delete log file
			string realLogFileName = VirtualFileSystem.GetRealPathByVirtual( "user:Logs\\WebBrowserControl_CefGlue.log" );
			try
			{
				if( File.Exists( realLogFileName ) )
					File.Delete( realLogFileName );
			}
			catch { }

			if( !cefRuntimeUnableToLoad )
			{
				try
				{
					CefRuntime.Load();
				}
				catch( DllNotFoundException ex )
				{
					Log.Error( "WebBrowserControl: InitializeCefRuntime: CefRuntime.Load(): " + ex.Message );
					return;
				}
				catch( CefRuntimeException ex )
				{
					Log.Error( "WebBrowserControl: InitializeCefRuntime: CefRuntime.Load(): " + ex.Message );
					return;
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: InitializeCefRuntime: CefRuntime.Load(): " + ex.Message );
					return;
				}

				var mainArgs = new CefMainArgs( null );
				var cefApp = new SimpleApp();

				var exitCode = CefRuntime.ExecuteProcess( mainArgs, cefApp, IntPtr.Zero );
				if( exitCode != -1 )
				{
					Log.Error( "WebBrowserControl: InitializeCefRuntime: CefRuntime.ExecuteProcess: Exit code: {0}", exitCode );
					return;
				}

				var cefSettings = new CefSettings
				{
					SingleProcess = true,
					WindowlessRenderingEnabled = true,
					MultiThreadedMessageLoop = true,
					LogSeverity = CefLogSeverity.Verbose,
					LogFile = realLogFileName,
					BrowserSubprocessPath = "",
					CachePath = "",
				};

				///// <summary>
				///// Set to <c>true</c> to disable configuration of browser process features using
				///// standard CEF and Chromium command-line arguments. Configuration can still
				///// be specified using CEF data structures or via the
				///// CefApp::OnBeforeCommandLineProcessing() method.
				///// </summary>
				//public bool CommandLineArgsDisabled { get; set; }

				///// <summary>
				///// The fully qualified path for the resources directory. If this value is
				///// empty the cef.pak and/or devtools_resources.pak files must be located in
				///// the module directory on Windows/Linux or the app bundle Resources directory
				///// on Mac OS X. Also configurable using the "resources-dir-path" command-line
				///// switch.
				///// </summary>
				//public string ResourcesDirPath { get; set; }

				try
				{
					CefRuntime.Initialize( mainArgs, cefSettings, cefApp, IntPtr.Zero );
				}
				catch( CefRuntimeException ex )
				{
					Log.Error( "WebBrowserControl: InitializeCefRuntime: CefRuntime.Initialize: " + ex.Message );
					return;
				}

				cefRuntimeInitialized = true;

				if( !subscribedToEngineAppDestroy )
				{
					subscribedToEngineAppDestroy = true;
					EngineApp.DestroyEvent += EngineApp_DestroyEvent;
				}
			}
		}

		static void EngineApp_DestroyEvent()
		{
			ShutdownCefRuntime();
		}

		static void ShutdownCefRuntime()
		{
			// shutdown CEF
			try
			{
				CefRuntime.Shutdown();
			}
			catch { }
			cefRuntimeInitialized = false;
		}

		void CreateBrowser()
		{
			if( !cefRuntimeInitialized && !cefRuntimeUnableToLoad )
				InitializeCefRuntime();

			if( cefRuntimeInitialized )
			{
				this.viewSize = GetNeededSize();

				var windowInfo = CefWindowInfo.Create();
				windowInfo.SetAsWindowless( IntPtr.Zero, false );

				var client = new WebClient( this );

				var settings = new CefBrowserSettings
				{
					// AuthorAndUserStylesDisabled = false,
				};

				//string r = GetURLFromVirtualFileName( "Maps\\Engine Features Demo\\Resources\\GUI\\FileTest.html" );
				//r = VirtualFileSystem.GetRealPathByVirtual( "Maps\\Engine Features Demo\\Resources\\GUI\\FileTest.html" );
				//CefBrowserHost.CreateBrowser( windowInfo, client, settings, r );//"about:blank" );
				//if( !string.IsNullOrEmpty( startUrl ) )
				//   LoadURL( startUrl );
				//LoadFileByVirtualFileName( "Maps\\Engine Features Demo\\Resources\\GUI\\FileTest.html" );

				string url = "about:blank";
				if( !string.IsNullOrEmpty( startFile ) )
					url = GetURLFromVirtualFileName( startFile );
				else if( !string.IsNullOrEmpty( startUrl ) )
					url = startUrl;
				CefBrowserHost.CreateBrowser( windowInfo, client, settings, url );

				//CefBrowserHost.CreateBrowser( windowInfo, client, settings, "about:blank" );
				//if( !string.IsNullOrEmpty( startFile ) )
				//   LoadFileByVirtualFileName( startFile );
				//else if( !string.IsNullOrEmpty( startUrl ) )
				//   LoadURL( startUrl );

				//CefBrowserHost.CreateBrowser( windowInfo, client, settings, !string.IsNullOrEmpty( StartURL ) ? StartURL : "about:blank" );
				//if( !string.IsNullOrEmpty( startUrl ) )
				//   LoadURL( startUrl );
			}
		}

		void DestroyBrowser()
		{
			if( browser != null )
			{
				// TODO: What's the right way of disposing the browser instance?
				if( browserHost != null )
				{
					browserHost.CloseBrowser();
					browserHost.Dispose();
					browserHost = null;
				}

				if( browser != null )
				{
					browser.Dispose();
					browser = null;
				}
			}
		}

		#endregion

		#region Events

		public event EventHandler Created;

		internal void HandleAfterCreated( CefBrowser browser )
		{
			this.browser = browser;
			this.browserHost = browser.GetHost();

			needInvalidate = true;
			browserHost.SetZoomLevel( zoom );

			var handler = Created;
			if( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		public event EventHandler<BeforePopupEventArgs> BeforePopup;

		internal void OnBeforePopup( BeforePopupEventArgs e )
		{
			if( BeforePopup != null )
			{
				BeforePopup( this, e );
			}
			else
			{
				LoadURL( e.TargetUrl );
				e.Handled = true;
			}
		}

		public event EventHandler<TitleChangedEventArgs> TitleChanged;

		internal void OnTitleChanged( string title )
		{
			this.title = title;

			var handler = TitleChanged;
			if( handler != null )
			{
				handler( this, new TitleChangedEventArgs( title ) );
			}
		}

		public event EventHandler<AddressChangedEventArgs> AddressChanged;

		internal void OnAddressChanged( string address )
		{
			var handler = AddressChanged;
			if( handler != null )
			{
				handler( this, new AddressChangedEventArgs( address ) );
			}
		}

		public event EventHandler<TargetUrlChangedEventArgs> TargetUrlChanged;

		internal void OnTargetUrlChanged( string targetUrl )
		{
			var handler = TargetUrlChanged;
			if( handler != null )
			{
				handler( this, new TargetUrlChangedEventArgs( targetUrl ) );
			}
		}

		internal bool OnTooltip( string text )
		{
			//Console.WriteLine( "OnTooltip: {0}", text );
			return false;
		}

		public event EventHandler<LoadingStateChangeEventArgs> LoadingStateChange;

		internal void OnLoadingStateChange( bool isLoading, bool canGoBack, bool canGoForward )
		{
			if( this.LoadingStateChange != null )
			{
				var e = new LoadingStateChangeEventArgs( isLoading, canGoBack, canGoForward );
				this.LoadingStateChange( this, e );
			}
		}

		public event EventHandler<LoadStartEventArgs> LoadStart;

		internal void OnLoadStart( CefFrame frame )
		{
			if( this.LoadStart != null )
			{
				var e = new LoadStartEventArgs( frame );
				this.LoadStart( this, e );
			}
		}

		public event EventHandler<LoadEndEventArgs> LoadEnd;

		internal void OnLoadEnd( CefFrame frame, int httpStatusCode )
		{
			if( this.LoadEnd != null )
			{
				var e = new LoadEndEventArgs( frame, httpStatusCode );
				this.LoadEnd( this, e );
			}

			needInvalidate = true;
		}

		public event EventHandler<LoadErrorEventArgs> LoadError;

		internal void OnLoadError( CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl )
		{
			if( this.LoadError != null )
			{
				var e = new LoadErrorEventArgs( frame, errorCode, errorText, failedUrl );
				this.LoadError( this, e );
			}
		}

		#endregion

		#region Rendering

		internal bool GetViewRect( ref CefRectangle rect )
		{
			bool rectProvided = false;
			CefRectangle browserRect = new CefRectangle();

			// TODO: simplify this
			//_mainUiDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
			//{
			try
			{
				// The simulated screen and view rectangle are the same. This is necessary
				// for popup menus to be located and sized inside the view.
				browserRect.X = browserRect.Y = 0;
				browserRect.Width = ViewSize.X;
				browserRect.Height = ViewSize.Y;

				rectProvided = true;
			}
			catch( Exception ex )
			{
				Log.Error( "WebBrowserControl: Caught exception in GetViewRect(): " + ex.Message );
				rectProvided = false;
			}
			//}));

			if( rectProvided )
			{
				rect = browserRect;
			}

			//_logger.Debug("GetViewRect result provided:{0} Rect: X{1} Y{2} H{3} W{4}", rectProvided, browserRect.X, browserRect.Y, browserRect.Height, browserRect.Width);

			return rectProvided;
		}

		internal void GetScreenPoint( int viewX, int viewY, ref int screenX, ref int screenY )
		{
			//if (mainThread != Thread.CurrentThread)
			//    Log.Fatal("WebBrowserControl: GetScreenPoint event: mainThread != Thread.CurrentThread.");

			Point ptScreen = new Point();

			//_mainUiDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
			//{
			try
			{
				//Point ptView = new Point(viewX, viewY);
				//ptScreen = PointToScreen(ptView);

				ptScreen.X = viewSize.X * viewX;
				ptScreen.Y = viewSize.Y * viewY;
			}
			catch( Exception ex )
			{
				Log.Error( "WebBrowserControl: Caught exception in GetScreenPoint(): " + ex.Message );
			}
			//}));

			screenX = (int)ptScreen.X;
			screenY = (int)ptScreen.Y;
		}

		internal void HandlePaint( CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height )
		{
			if( type == CefPaintElementType.View )
			{
				if( texture != null && width != 0 && height != 0 && width == viewSize.X && height == viewSize.Y )
				{
					//TO DO: dirtyRects

					try
					{
						int stride = width * 4;
						int sourceBufferSize = stride * height;

						lock( renderBufferLock )
						{
							Vec2I newSize = new Vec2I( width, height );
							if( renderBuffer == null || sourceBufferSize != renderBuffer.Length || renderBufferForSize != newSize )
							{
								renderBuffer = new byte[ sourceBufferSize ];
								renderBufferForSize = newSize;
							}
							Marshal.Copy( buffer, renderBuffer, 0, sourceBufferSize );
						}

						needUpdateTexture = true;
					}
					catch( Exception ex )
					{
						Log.Error( "WebBrowserControl: Caught exception in HandleViewPaint(): " + ex.Message );
					}
				}
			}
			if( type == CefPaintElementType.Popup )
			{
				//TO DO?
			}
		}

		void UpdateTexture()
		{
			lock( renderBufferLock )
			{
				if( renderBuffer != null && renderBufferForSize == ViewSize && renderBuffer.Length == ViewSize.X * ViewSize.Y * 4 )
				{
					try
					{
						HardwarePixelBuffer pixelBuffer = texture.GetBuffer();
						pixelBuffer.Lock( HardwareBuffer.LockOptions.Discard );
						PixelBox pixelBox = pixelBuffer.GetCurrentLock();
						unsafe
						{
							fixed( byte* ptr = renderBuffer )
								pixelBox.WriteDataUnmanaged( (IntPtr)ptr, ViewSize.X, ViewSize.Y );
						}
						pixelBuffer.Unlock();
					}
					catch( Exception ex )
					{
						Log.Error( "WebBrowserControl: Caught exception in UpdateTexture(): " + ex.Message );
					}
				}
			}
		}

		Vec2I GetNeededSize()
		{
			Vec2I result;

			ScreenControlManager screenControlManager = GetControlManager() as ScreenControlManager;
			if( screenControlManager != null )
			{
				//screen gui

				Vec2I viewportSize = screenControlManager.GuiRenderer.ViewportForScreenGuiRenderer.DimensionsInPixels.Size;

				Vec2 size = viewportSize.ToVec2() * GetScreenSize();
				if( screenControlManager.GuiRenderer._OutGeometryTransformEnabled )
					size *= screenControlManager.GuiRenderer._OutGeometryTransformScale;

				result = new Vec2I( (int)( size.X + .9999f ), (int)( size.Y + .9999f ) );
			}
			else
			{
				//in-game gui

				int height = inGame3DGuiHeightInPixels;
				Vec2 screenSize = GetScreenSize();
				float width = (float)height * ( screenSize.X / screenSize.Y ) * GetControlManager().AspectRatio;
				result = new Vec2I( (int)( width + .9999f ), height );

				//int height = inGame3DGuiHeightInPixels;
				//if( height > RenderSystem.Instance.Capabilities.MaxTextureSize )
				//   height = RenderSystem.Instance.Capabilities.MaxTextureSize;

				//Vec2 screenSize = GetScreenSize();
				//float width = (float)height * ( screenSize.X / screenSize.Y ) * GetControlManager().AspectRatio;
				//result = new Vec2I( (int)( width + .9999f ), height );
			}

			if( result.X < 1 )
				result.X = 1;
			if( result.Y < 1 )
				result.Y = 1;

			//fix max texture size
			if( result.X > RenderSystem.Instance.Capabilities.MaxTextureSize || result.Y > RenderSystem.Instance.Capabilities.MaxTextureSize )
			{
				float divideX = (float)result.X / (float)RenderSystem.Instance.Capabilities.MaxTextureSize;
				float divideY = (float)result.Y / (float)RenderSystem.Instance.Capabilities.MaxTextureSize;
				float divide = Math.Max( Math.Max( divideX, divideY ), 1 );
				if( divide != 1 )
				{
					result = ( result.ToVec2() / divide ).ToVec2I();
					if( result.X > RenderSystem.Instance.Capabilities.MaxTextureSize )
						result.X = RenderSystem.Instance.Capabilities.MaxTextureSize;
					if( result.Y > RenderSystem.Instance.Capabilities.MaxTextureSize )
						result.Y = RenderSystem.Instance.Capabilities.MaxTextureSize;
				}
			}

			return result;
		}

		protected virtual void OnResized( Vec2I oldSize, Vec2I newSize )
		{
			//_logger.Debug("BrowserResize. Old H{0}xW{1}; New H{2}xW{3}.", _browserHeight, _browserWidth, newHeight, newWidth);

			if( newSize.X > 0 && newSize.Y > 0 )
			{
				// If the window has already been created, just resize it
				if( browserHost != null )
				{
					//_logger.Trace("CefBrowserHost::WasResized to {0}x{1}.", newWidth, newHeight);
					browserHost.WasResized();
				}
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			Vec2I size = GetNeededSize();

			if( browser == null )
				CreateBrowser();

			//update brower engine and texture
			if( browser != null )
			{
				if( viewSize != size /*&& !browser.IsResizing */)
				{
					var oldSize = viewSize;
					viewSize = size;
					OnResized( oldSize, viewSize );
				}

				//create texture
				if( texture == null || textureSize != size || needRecreateTexture )
				{
					if( texture != null )
					{
						texture.Dispose();
						texture = null;
					}

					textureSize = size;

					string textureName = TextureManager.Instance.GetUniqueName( "WebBrowserControl" );

					bool mipmaps = false;
					if( GetControlManager() != null && GetControlManager() is In3dControlManager )
						mipmaps = inGame3DGuiMipmaps;

					if( mipmaps )
					{
						texture = TextureManager.Instance.Create( textureName, Texture.Type.Type2D, textureSize,
							1, -1, PixelFormat.A8R8G8B8, Texture.Usage.DynamicWriteOnlyDiscardable | Texture.Usage.AutoMipmap );
					}
					else
					{
						texture = TextureManager.Instance.Create( textureName, Texture.Type.Type2D, textureSize,
							1, 0, PixelFormat.A8R8G8B8, Texture.Usage.DynamicWriteOnlyDiscardable );
					}

					needUpdateTexture = true;
					needRecreateTexture = false;
				}

				if( needInvalidate )
				{
					browserHost.SetZoomLevel( zoom );
					browserHost.Invalidate( CefPaintElementType.View );
					//browserHost.Invalidate( new CefRectangle( 0, 0, 100000, 100000 ), CefPaintElementType.View );
					needInvalidate = false;
				}

				//update texture
				if( /*browser.IsDirty ||*/ needUpdateTexture )
				{
					if( texture != null )
						UpdateTexture();
					needUpdateTexture = false;
				}
			}

			//draw texture
			if( browser != null )
			{
				bool backColorZero = BackColor == new ColorValue( 0, 0, 0, 0 );

				ColorValue color = backColorZero ? new ColorValue( 1, 1, 1 ) : BackColor;
				if( texture == null )
					color = new ColorValue( 0, 0, 0, color.Alpha );
				color *= GetTotalColorMultiplier();

				color.Clamp( new ColorValue( 0, 0, 0, 0 ), new ColorValue( 1, 1, 1, 1 ) );

				Rect rect;
				GetScreenRectangle( out rect );

				if( renderer.IsScreen && !renderer._OutGeometryTransformEnabled )
				{
					//screen per pixel accuracy

					Vec2 viewportSize = renderer.ViewportForScreenGuiRenderer.DimensionsInPixels.Size.ToVec2();

					Vec2 leftTop = rect.LeftTop;
					leftTop *= viewportSize;
					leftTop = new Vec2( (int)( leftTop.X + .9999f ), (int)( leftTop.Y + .9999f ) );
					if( RenderSystem.Instance.IsDirect3D() )
						leftTop -= new Vec2( .5f, .5f );
					leftTop /= viewportSize;

					Vec2 rightBottom = rect.RightBottom;
					rightBottom *= viewportSize;
					rightBottom = new Vec2( (int)( rightBottom.X + .9999f ), (int)( rightBottom.Y + .9999f ) );
					if( RenderSystem.Instance.IsDirect3D() )
						rightBottom -= new Vec2( .5f, .5f );
					rightBottom /= viewportSize;

					Rect fixedRect = new Rect( leftTop, rightBottom );

					renderer.AddQuad( fixedRect, new Rect( 0, 0, 1, 1 ), texture, color, true );
				}
				else
				{
					renderer.AddQuad( rect, new Rect( 0, 0, 1, 1 ), texture, color, true );
				}
			}

			if( cefRuntimeUnableToLoad )
			{
				renderer.AddTextWordWrap( 
					string.Format( "WebBrowserControl: Unable to initialize web browser control. Error: {0}", cefRuntimeUnableToLoadError ),
					GetScreenRectangle(), Renderer.HorizontalAlign.Center, false, Renderer.VerticalAlign.Center, 0, new ColorValue( 1, 0, 0 ) );
			}
			else if( !IsSupportedByThisPlatform() )
			{
				renderer.AddText( string.Format( "WebBrowserControl: {0} is not supported.", PlatformInfo.Platform ),
					new Vec2( .5f, .5f ), Renderer.HorizontalAlign.Center, Renderer.VerticalAlign.Center, new ColorValue( 1, 0, 0 ) );
			}
		}

		#endregion

		#region Input

		static bool IsSupportedMouseButton( EMouseButtons button )
		{
			return button == EMouseButtons.Left ||
				button == EMouseButtons.Middle ||
				button == EMouseButtons.Right;
		}

		static CefMouseButtonType ToCefMouseButton( EMouseButtons button )
		{
			switch( button )
			{
			case EMouseButtons.Left: return CefMouseButtonType.Left;
			case EMouseButtons.Middle: return CefMouseButtonType.Middle;
			case EMouseButtons.Right: return CefMouseButtonType.Right;
			}
			return CefMouseButtonType.Left;
		}

		static CefEventFlags GetCurrentKeyboardModifiers()
		{
			CefEventFlags result = new CefEventFlags();

			if( EngineApp.Instance.IsKeyPressed( EKeys.Alt ) )
				result |= CefEventFlags.AltDown;
			if( EngineApp.Instance.IsKeyPressed( EKeys.Shift ) )
				result |= CefEventFlags.ShiftDown;
			if( EngineApp.Instance.IsKeyPressed( EKeys.Control ) )
				result |= CefEventFlags.ControlDown;
			if( EngineApp.Instance.IsKeyPressed( EKeys.LWin ) ||
				EngineApp.Instance.IsKeyPressed( EKeys.RWin ) ||
				EngineApp.Instance.IsKeyPressed( EKeys.Command ) )
			{
				result |= CefEventFlags.CommandDown;
			}

			return result;
		}

		CefMouseEvent GetCurrentMouseEvent()
		{
			Vec2 pos = viewSize.ToVec2() * MousePosition;
			var mouseEvent = new CefMouseEvent( (int)pos.X, (int)pos.Y, GetCurrentKeyboardModifiers() );
			return mouseEvent;
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( IsEnabledInHierarchy() && browserHost != null && new Rect( 0, 0, 1, 1 ).IsContainsPoint( MousePosition ) )
			{
				try
				{
					Focus();

					if( IsSupportedMouseButton( button ) )
						browserHost.SendMouseClickEvent( GetCurrentMouseEvent(), ToCefMouseButton( button ), false, 1 );

					//_logger.Debug(string.Format("Browser_MouseDown: ({0},{1})", cursorPos.X, cursorPos.Y));
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnMouseDown(): " + ex.Message );
				}

				return true;
			}
			else
				Unfocus();

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			bool result = base.OnMouseUp( button );

			if( IsEnabledInHierarchy() && browserHost != null && IsSupportedMouseButton( button ) )
			{
				try
				{
					//Focus();

					if( IsSupportedMouseButton( button ) )
						browserHost.SendMouseClickEvent( GetCurrentMouseEvent(), ToCefMouseButton( button ), true, 1 );

					//_logger.Debug(string.Format("Browser_MouseDown: ({0},{1})", cursorPos.X, cursorPos.Y));
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnMouseUp(): " + ex.Message );
				}
			}

			return result;
		}

		protected override bool OnMouseDoubleClick( EMouseButtons button )
		{
			if( IsEnabledInHierarchy() && browserHost != null && new Rect( 0, 0, 1, 1 ).IsContainsPoint( MousePosition ) )
			{
				try
				{
					Focus();

					if( IsSupportedMouseButton( button ) )
						browserHost.SendMouseClickEvent( GetCurrentMouseEvent(), ToCefMouseButton( button ), false, 2 );

					//_logger.Debug(string.Format("Browser_MouseDown: ({0},{1})", cursorPos.X, cursorPos.Y));
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnMouseDoubleClick(): " + ex.Message );
				}

				return true;
			}

			return base.OnMouseDoubleClick( button );
		}

		protected override bool OnMouseWheel( int delta )
		{
			bool result = base.OnMouseWheel( delta );

			if( IsEnabledInHierarchy() && browserHost != null && new Rect( 0, 0, 1, 1 ).IsContainsPoint( MousePosition ) )
			{
				try
				{
					browserHost.SendMouseWheelEvent( GetCurrentMouseEvent(), 0, delta );
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnMouseWheel(): " + ex.Message );
				}
			}

			return result;
		}

		protected override void OnMouseMove()
		{
			base.OnMouseMove();

			if( IsEnabledInHierarchy() && browserHost != null )
			{
				try
				{
					browserHost.SendMouseMoveEvent( GetCurrentMouseEvent(), false );
					//_logger.Debug(string.Format("Browser_MouseMove: ({0},{1})", cursorPos.X, cursorPos.Y));
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnMouseMove(): " + ex.Message );
				}
			}
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( Focused && IsEnabledInHierarchy() && browserHost != null )
			{
				browserHost.SendFocusEvent( true );

				try
				{
					//_logger.Debug(string.Format("KeyDown: system key {0}, key {1}", arg.SystemKey, arg.Key));
					CefKeyEvent keyEvent = new CefKeyEvent()
					{
						EventType = CefKeyEventType.RawKeyDown,
						WindowsKeyCode = (int)e.Key /*KeyInterop.VirtualKeyFromKey(arg.Key == Key.System ? arg.SystemKey : arg.Key)*/,
						NativeKeyCode = (int)e.Key,/*0*/
						/*IsSystemKey = e.Key == EKeys.System*/
					};

					keyEvent.Modifiers = GetCurrentKeyboardModifiers();

					browserHost.SendKeyEvent( keyEvent );
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnKeyDown(): " + ex.Message );
				}

				//arg.Handled = HandledKeys.Contains(arg.Key);

				return true;
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnKeyPress( KeyPressEvent e )
		{
			if( Focused && IsEnabledInHierarchy() && browserHost != null )
			{
				browserHost.SendFocusEvent( true );

				try
				{
					//_logger.Debug(string.Format("KeyDown: system key {0}, key {1}", arg.SystemKey, arg.Key));
					CefKeyEvent keyEvent = new CefKeyEvent()
					{
						EventType = CefKeyEventType.Char,
						WindowsKeyCode = (int)e.KeyChar
					};

					keyEvent.Modifiers = GetCurrentKeyboardModifiers();

					browserHost.SendKeyEvent( keyEvent );
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnKeyDown(): " + ex.Message );
				}

				//arg.Handled = true;

				return true;
			}

			return base.OnKeyPress( e );
		}

		protected override bool OnKeyUp( KeyEvent e )
		{
			if( IsEnabledInHierarchy() && browserHost != null )
			{
				browserHost.SendFocusEvent( true );

				try
				{
					//_logger.Debug(string.Format("KeyDown: system key {0}, key {1}", arg.SystemKey, arg.Key));
					CefKeyEvent keyEvent = new CefKeyEvent()
					{
						EventType = CefKeyEventType.KeyUp,
						WindowsKeyCode = (int)e.Key /*KeyInterop.VirtualKeyFromKey(arg.Key == Key.System ? arg.SystemKey : arg.Key)*/,
						NativeKeyCode = (int)e.Key,/*0*/
						/*IsSystemKey = e.Key == EKeys.System*/
					};

					keyEvent.Modifiers = GetCurrentKeyboardModifiers();

					browserHost.SendKeyEvent( keyEvent );
				}
				catch( Exception ex )
				{
					Log.Error( "WebBrowserControl: Caught exception in OnKeyDown(): " + ex.Message );
				}

				//arg.Handled = true;
			}

			return base.OnKeyUp( e );
		}

		#endregion

		#region Browser

		public void LoadURL( string url )
		{
			// Remove leading whitespace from the URL
			string url2 = url.TrimStart();

			if( string.IsNullOrEmpty( url2 ) )
				url2 = "about:blank";

			if( browser != null )
				browser.GetMainFrame().LoadUrl( url2 );
		}

		static string GetURLFromVirtualFileName( string virtualFileName )
		{
			return "file:///" + VirtualFileSystem.GetRealPathByVirtual( virtualFileName );
		}

		public void LoadFileByVirtualFileName( string virtualFileName )
		{
			if( browser == null )
				return;

			if( VirtualFile.IsInArchive( virtualFileName ) )
			{
				Log.Warning( "WebBrowserControl: LoadFileByVirtualFileName: Loading from archive is not supported." );
				return;
			}

			string url = GetURLFromVirtualFileName( virtualFileName );
			LoadURL( url );
		}

		public void LoadFileByRealFileName( string realFileName )
		{
			if( browser == null )
				return;
			string url = "file:///" + realFileName;
			LoadURL( url );
		}

		public void LoadString( string content, string url )
		{
			// Remove leading whitespace from the URL
			string url2 = url.TrimStart();

			if( string.IsNullOrEmpty( url2 ) )
				url2 = "about:blank";

			if( browser != null )
				browser.GetMainFrame().LoadString( content, url2 );
		}

		//public void LoadHTML( string html, string frameName )
		//{
		//    if( browser != null )
		//        browser.GetMainFrame().LoadRequest(html, frameName);
		//}

		//public void LoadHTML( string html )
		//{
		//    LoadHTML( html, "" );
		//}

		public void ExecuteJavaScript( string code, string url, int line )
		{
			if( browser != null )
				this.browser.GetMainFrame().ExecuteJavaScript( code, url, line );
		}

		public bool CanGoBack()
		{
			if( browser != null )
				return browser.CanGoBack;
			else
				return false;
		}

		public void GoBack()
		{
			if( browser != null )
				browser.GoBack();
		}

		public bool CanGoForward()
		{
			if( browser != null )
				return browser.CanGoForward;
			else
				return false;
		}

		public void GoForward()
		{
			if( browser != null )
				browser.GoForward();
		}

		public void Stop()
		{
			if( browser != null )
				browser.StopLoad();
		}

		public void Reload()
		{
			if( browser != null )
				browser.Reload();
		}

		//[Browsable( false )]
		//public string Source
		//{
		//   get
		//   {
		//      if( browser != null )
		//      {
		//         //return browser.GetMainFrame().GetSource();
		//      }
		//      return "";
		//   }
		//}

		[Browsable( false )]
		public string Title
		{
			get
			{
				return this.title;
			}
		}

		[Browsable( false )]
		public string TargetURL
		{
			get
			{
				if( browser != null )
					return browser.GetMainFrame().Url;
				return "";
			}
		}

		#endregion

		protected override System.Drawing.Image OnGetEditorIcon()
		{
			return Resources.WebBrowserIcon;
		}

		public static bool IsSupportedByThisPlatform()
		{
			if( PlatformInfo.Platform == PlatformInfo.Platforms.MacOSX )
				return false;
			return true;
		}

		[Browsable( false )]
		public Vec2I ViewSize
		{
			get { return viewSize; }
		}

		[Browsable( false )]
		public bool CefRuntimeUnableToLoad
		{
			get { return cefRuntimeUnableToLoad; }
		}

		[Browsable( false )]
		public string CefRuntimeUnableToLoadError
		{
			get { return cefRuntimeUnableToLoadError; }
		}
	}
}
