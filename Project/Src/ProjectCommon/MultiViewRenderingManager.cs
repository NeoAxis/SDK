// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.MapSystem;

namespace ProjectCommon
{
	public class MultiViewRenderingManager
	{
		static MultiViewRenderingManager instance;

		List<View> views = new List<View>();
		ReadOnlyCollection<View> viewsReadOnly;
		bool mainCameraDraw3DScene;
		bool drawDebugInfo;

		int preventTextureCreationRemainingFrames;

		Font debugFont;

		///////////////////////////////////////////

		public class View
		{
			Rect rectangle;
			float opacity = 1;

			Texture texture;
			Vec2I initializedTextureSize;
			Camera camera;
			Viewport viewport;
			ViewRenderTargetListener renderTargetListener;

			///////////////

			public delegate void RenderDelegate( View view, Camera camera );
			public event RenderDelegate Render;

			///////////////

			class ViewRenderTargetListener : RenderTargetListener
			{
				View owner;

				public ViewRenderTargetListener( View owner )
				{
					this.owner = owner;
				}

				protected override void OnPreRenderTargetUpdate( RenderTargetEvent evt )
				{
					base.OnPreRenderTargetUpdate( evt );

					Camera defaultCamera = RendererWorld.Instance.DefaultCamera;
					Camera camera = owner.camera;

					//set camera settings to default state
					camera.ProjectionType = defaultCamera.ProjectionType;
					camera.OrthoWindowHeight = defaultCamera.OrthoWindowHeight;
					camera.NearClipDistance = defaultCamera.NearClipDistance;
					camera.FarClipDistance = defaultCamera.FarClipDistance;

					Vec2I sizeInPixels = owner.Viewport.DimensionsInPixels.Size;
					camera.AspectRatio = (float)sizeInPixels.X / (float)sizeInPixels.Y;

					camera.Fov = defaultCamera.Fov;
					camera.FixedUp = defaultCamera.FixedUp;
					camera.Direction = defaultCamera.Direction;
					camera.Position = defaultCamera.Position;

					////override visibility (hide main scene objects, show from lists)
					//List<SceneNode> sceneNodes = new List<SceneNode>();
					//if( owner.sceneNode != null )
					//   sceneNodes.Add( owner.sceneNode );
					//SceneManager.Instance.SetOverrideVisibleObjects( new SceneManager.OverrideVisibleObjectsClass(
					//   new StaticMeshObject[ 0 ], sceneNodes.ToArray(), new RenderLight[ 0 ] ) );

					if( owner.Render != null )
						owner.Render( owner, camera );
				}

				protected override void OnPostRenderTargetUpdate( RenderTargetEvent evt )
				{
					//SceneManager.Instance.ResetOverrideVisibleObjects();
				}
			}

			///////////////

			internal View()
			{
			}

			public Rect Rectangle
			{
				get { return rectangle; }
				set { rectangle = value; }
			}

			public float Opacity
			{
				get { return opacity; }
				set { opacity = value; }
			}

			public Texture Texture
			{
				get { return texture; }
			}

			public Camera Camera
			{
				get { return camera; }
			}

			public Viewport Viewport
			{
				get { return viewport; }
			}

			void CreateViewport()
			{
				int index = instance.views.IndexOf( this );

				DestroyViewport();

				Vec2I textureSize = GetNeededTextureSize();

				string textureName = TextureManager.Instance.GetUniqueName(
					string.Format( "MultiViewRendering{0}", index ) );
				PixelFormat format = PixelFormat.R8G8B8;

				int fsaa;
				if( !int.TryParse( RendererWorld.InitializationOptions.FullSceneAntialiasing, out fsaa ) )
					fsaa = 0;

				texture = TextureManager.Instance.Create( textureName, Texture.Type.Type2D,
					textureSize, 1, 0, format, Texture.Usage.RenderTarget, false, fsaa );
				if( texture == null )
				{
					Log.Fatal( "MultiViewRenderingManager: Unable to create texture." );
					return;
				}

				RenderTarget renderTarget = texture.GetBuffer().GetRenderTarget();
				renderTarget.AutoUpdate = true;
				renderTarget.AllowAdditionalMRTs = true;

				//create camera
				camera = SceneManager.Instance.CreateCamera(
					SceneManager.Instance.GetUniqueCameraName( string.Format( "MultiViewRendering{0}", index ) ) );
				camera.Purpose = Camera.Purposes.MainCamera;

				//add viewport
				viewport = renderTarget.AddViewport( camera, 0 );
				viewport.ShadowsEnabled = true;

				//Create compositor for HDR render technique
				bool hdrCompositor =
					RendererWorld.Instance.DefaultViewport.GetCompositorInstance( "HDR" ) != null;
				if( hdrCompositor )
				{
					viewport.AddCompositor( "HDR" );
					viewport.SetCompositorEnabled( "HDR", true );
				}

				//FXAA antialiasing post effect
				bool fxaaCompositor =
					RendererWorld.Instance.DefaultViewport.GetCompositorInstance( "FXAA" ) != null;
				if( fxaaCompositor )
				{
					viewport.AddCompositor( "FXAA" );
					viewport.SetCompositorEnabled( "FXAA", true );
				}

				//add listener
				renderTargetListener = new ViewRenderTargetListener( this );
				renderTarget.AddListener( renderTargetListener );

				initializedTextureSize = textureSize;
			}

			Vec2I GetNeededTextureSize()
			{
				Vec2I result = Vec2I.Zero;

				Vec2I screenSize = RendererWorld.Instance.DefaultViewport.DimensionsInPixels.Size;
				if( screenSize.X > 0 && screenSize.Y > 0 )
					result = ( rectangle.GetSize() * screenSize.ToVec2() ).ToVec2I();

				if( result.X < 1 )
					result.X = 1;
				if( result.Y < 1 )
					result.Y = 1;

				return result;
			}

			internal void UpdateViewport()
			{
				if( initializedTextureSize != GetNeededTextureSize() )
				{
					DestroyViewport();
					if( instance.preventTextureCreationRemainingFrames <= 0 )
						CreateViewport();
				}
			}

			internal void DestroyViewport()
			{
				if( renderTargetListener != null )
				{
					RenderTarget renderTarget = texture.GetBuffer().GetRenderTarget();
					renderTarget.RemoveListener( renderTargetListener );
					renderTargetListener.Dispose();
					renderTargetListener = null;
				}
				if( viewport != null )
				{
					viewport.Dispose();
					viewport = null;
				}
				if( camera != null )
				{
					camera.Dispose();
					camera = null;
				}
				if( texture != null )
				{
					texture.Dispose();
					texture = null;
					instance.preventTextureCreationRemainingFrames = 2;
				}

				initializedTextureSize = Vec2I.Zero;
			}
		}

		///////////////////////////////////////////

		MultiViewRenderingManager()
		{
			viewsReadOnly = new ReadOnlyCollection<View>( views );
		}

		public static MultiViewRenderingManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			if( instance != null )
				Log.Fatal( "MultiDisplayRenderingManager: Init: is already initialized." );

			instance = new MultiViewRenderingManager();
			instance.InitInternal();
		}

		public static void Shutdown()
		{
			if( instance != null )
			{
				instance.ShutdownInternal();
				instance = null;
			}
		}

		void InitInternal()
		{
			RenderSystem.Instance.RenderSystemEvent += RenderSystem_RenderSystemEvent;
			RendererWorld.Instance.BeginRenderFrame += RendererWorld_BeginRenderFrame;
		}

		void ShutdownInternal()
		{
			RemoveAllViews();

			if( RendererWorld.Instance != null )
			{
				RenderSystem.Instance.RenderSystemEvent -= RenderSystem_RenderSystemEvent;
				RendererWorld.Instance.BeginRenderFrame -= RendererWorld_BeginRenderFrame;

				//restore main camera
				if( !mainCameraDraw3DScene )
				{
					RendererWorld.Instance.DefaultCamera.Enable3DSceneRendering = true;
					MapCompositorManager.ApplyToMainCamera = true;
				}
			}
		}

		public IList<View> Views
		{
			get { return viewsReadOnly; }
		}

		public bool MainCameraDraw3DScene
		{
			get { return mainCameraDraw3DScene; }
			set { mainCameraDraw3DScene = value; }
		}

		public bool DrawDebugInfo
		{
			get { return drawDebugInfo; }
			set { drawDebugInfo = value; }
		}

		void DestroyViewports()
		{
			foreach( View view in views )
				view.DestroyViewport();
		}

		void RendererWorld_BeginRenderFrame()
		{
			RendererWorld.Instance.DefaultCamera.Enable3DSceneRendering = mainCameraDraw3DScene;
			MapCompositorManager.ApplyToMainCamera = mainCameraDraw3DScene;

			if( preventTextureCreationRemainingFrames > 0 )
				preventTextureCreationRemainingFrames--;
			foreach( View view in views )
				view.UpdateViewport();
		}

		void AddTextWithShadow( GuiRenderer renderer, Font font, string text, Vec2 position,
			HorizontalAlign horizontalAlign, VerticalAlign verticalAlign, ColorValue color )
		{
			Vec2 shadowOffset = 1.0f / renderer.ViewportForScreenGuiRenderer.DimensionsInPixels.Size.ToVec2();

			renderer.AddText( font, text, position + shadowOffset, horizontalAlign, verticalAlign,
				new ColorValue( 0, 0, 0, color.Alpha / 2 ) );
			renderer.AddText( font, text, position, horizontalAlign, verticalAlign, color );
		}

		public void RenderScreenUI( GuiRenderer renderer )
		{
			for( int viewIndex = 0; viewIndex < views.Count; viewIndex++ )
			{
				View view = views[ viewIndex ];

				//draw view on screen
				if( view.Opacity > 0 )
				{
					renderer.PushTextureFilteringMode( GuiRenderer.TextureFilteringModes.Point );
					renderer.AddQuad( view.Rectangle, new Rect( 0, 0, 1, 1 ), view.Texture,
						new ColorValue( 1, 1, 1, view.Opacity ), true );
					renderer.PopTextureFilteringMode();
				}

				//draw debug info
				if( drawDebugInfo )
				{
					Viewport screenViewport = renderer.ViewportForScreenGuiRenderer;
					Vec2 pixelOffset = 1.0f / screenViewport.DimensionsInPixels.Size.ToVec2();
					ColorValue color = new ColorValue( 1, 1, 0 );
					renderer.AddRectangle( new Rect(
						view.Rectangle.LeftTop + pixelOffset,
						view.Rectangle.RightBottom - pixelOffset * 2 ),
						color );
					renderer.AddLine( view.Rectangle.LeftTop, view.Rectangle.RightBottom, color );
					renderer.AddLine( view.Rectangle.RightTop, view.Rectangle.LeftBottom, color );

					if( debugFont == null )
						debugFont = FontManager.Instance.LoadFont( "Default", .03f );

					string sizeString = "";
					if( view.Texture != null )
						sizeString = string.Format( "{0}x{1}", view.Texture.Size.X, view.Texture.Size.Y );
					string text = string.Format( "View {0}, {1}", viewIndex, sizeString );
					Vec2 position = new Vec2( view.Rectangle.Right - pixelOffset.X * 5, view.Rectangle.Top );
					AddTextWithShadow( renderer, debugFont, text, position, HorizontalAlign.Right,
						VerticalAlign.Top, new ColorValue( 1, 1, 1 ) );
				}
			}
		}

		void RenderSystem_RenderSystemEvent( RenderSystemEvents name )
		{
			if( name == RenderSystemEvents.DeviceLost )
			{
				foreach( View view in views )
					view.DestroyViewport();
			}
		}

		public View AddView( Rect rectangle )
		{
			View view = new View();
			view.Rectangle = rectangle;
			views.Add( view );
			return view;
		}

		public void RemoveView( View view )
		{
			view.DestroyViewport();
			views.Remove( view );
		}

		public void RemoveAllViews()
		{
			while( views.Count > 0 )
				RemoveView( views[ views.Count - 1 ] );
		}
	}
}
