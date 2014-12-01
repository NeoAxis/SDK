// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Utils;

namespace Game
{
	public class GUISamples2Window : Control
	{
		Control window;
		SceneBox sceneBox;
		int sceneBoxMeshIndex;

		RenderToTexture renderToTexture = new RenderToTexture();

		///////////////////////////////////////////

		class RenderToTexture
		{
			Control outputControl;

			Texture texture;
			RenderTexture renderTexture;
			Camera camera;
			Viewport viewport;
			SceneRenderTargetListener renderTargetListener;

			//draw object example
			MeshObject meshObject;
			SceneNode sceneNode;

			///////////////

			class SceneRenderTargetListener : RenderTargetListener
			{
				RenderToTexture owner;

				public SceneRenderTargetListener( RenderToTexture owner )
				{
					this.owner = owner;
				}

				protected override void OnPreRenderTargetUpdate( RenderTargetEvent evt )
				{
					base.OnPreRenderTargetUpdate( evt );

					Camera camera = owner.camera;

					//update camera settings
					camera.NearClipDistance = .1f;
					camera.FarClipDistance = 1000;
					camera.AspectRatio = 1.3f;
					camera.Fov = 80;
					camera.Position = new Vec3( 3, 1, 2 );
					camera.FixedUp = Vec3.ZAxis;
					camera.LookAt( new Vec3( 0, 0, 0 ) );

					//override visibility (hide main scene objects, show from lists)
					List<SceneNode> sceneNodes = new List<SceneNode>();
					if( owner.sceneNode != null )
						sceneNodes.Add( owner.sceneNode );
					SceneManager.Instance.SetOverrideVisibleObjects( new SceneManager.OverrideVisibleObjectsClass(
						new StaticMeshObject[ 0 ], sceneNodes.ToArray(), new RenderLight[ 0 ] ) );

					//draw box by debug geometry
					camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
					camera.DebugGeometry.AddBounds( new Bounds( new Vec3( -1, -1, -1 ), new Vec3( 1, 1, 1 ) ) );
				}

				protected override void OnPostRenderTargetUpdate( RenderTargetEvent evt )
				{
					SceneManager.Instance.ResetOverrideVisibleObjects();
				}
			}

			///////////////

			bool CreateRenderTexture()
			{
				Vec2I size = new Vec2I( 512, 256 );

				string textureName = TextureManager.Instance.GetUniqueName( "RenderToTextureExample" );
				texture = TextureManager.Instance.Create( textureName, Texture.Type.Type2D, size, 1, 0,
					PixelFormat.R8G8B8, Texture.Usage.RenderTarget );
				if( texture == null )
					return false;

				renderTexture = texture.GetBuffer().GetRenderTarget();
				//you can update render texture manually by means renderTexture.Update() method. For this task set AutoUpdate = false;
				renderTexture.AutoUpdate = true;

				//create camera
				string cameraName = SceneManager.Instance.GetUniqueCameraName( "RenderToTextureExample" );
				camera = SceneManager.Instance.CreateCamera( cameraName );
				camera.Purpose = Camera.Purposes.Special;
				camera.AllowMapCompositorManager = false;

				//add viewport
				viewport = renderTexture.AddViewport( camera );
				viewport.BackgroundColor = new ColorValue( 0, 0, 0, 1 );
				viewport.ShadowsEnabled = false;
				viewport.MaterialScheme = "";

				//add listener
				renderTargetListener = new SceneRenderTargetListener( this );
				renderTexture.AddListener( renderTargetListener );

				return true;
			}

			void DestroyRenderTexture()
			{
				if( renderTargetListener != null )
				{
					renderTexture.RemoveListener( renderTargetListener );
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

				renderTexture = null;

				if( texture != null )
				{
					texture.Dispose();
					texture = null;
				}
			}

			void BindToOutputControl()
			{
				if( outputControl != null )
				{
					outputControl.BackTexture = texture;
					outputControl.BackColor = new ColorValue( 1, 1, 1 );
				}
			}

			void UnbindToOutputControl()
			{
				if( outputControl != null )
				{
					outputControl.BackTexture = null;
					outputControl.BackColor = new ColorValue( .5f, .5f, .5f, .5f );
				}
			}

			public bool Create( Control outputControl )
			{
				Destroy();

				this.outputControl = outputControl;
				if( !CreateRenderTexture() )
					return false;
				BindToOutputControl();
				CreateMeshObject();
				return true;
			}

			public void Destroy()
			{
				DestroyMeshObject();
				UnbindToOutputControl();
				DestroyRenderTexture();
			}

			public Texture Texture
			{
				get { return texture; }
			}

			void CreateMeshObject()
			{
				meshObject = SceneManager.Instance.CreateMeshObject( "Base\\Simple Models\\Box.mesh" );
				if( meshObject != null )
				{
					meshObject.SetMaterialNameForAllSubObjects( "Red" );

					sceneNode = new SceneNode();
					//sceneNode.Position = new Vec3( 0, 0, 1 );
					sceneNode.Attach( meshObject );
				}
			}

			void DestroyMeshObject()
			{
				if( meshObject != null )
				{
					sceneNode.Detach( meshObject );
					sceneNode.Dispose();
					sceneNode = null;
					meshObject.Dispose();
					meshObject = null;
				}
			}

			//public void Update()			
			//{
			//   if( renderTexture != null )
			//      renderTexture.Update();
			//}
		}

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//create window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\GUISamples2Window.gui" );
			Controls.Add( window );

			sceneBox = (SceneBox)window.Controls[ "SceneBox" ];

			( (Button)window.Controls[ "Close" ] ).Click += Close_Click;
			( (Button)window.Controls[ "SceneBoxChangeModel" ] ).Click += SceneBoxChangeModel_Click;
			( (Button)window.Controls[ "SceneBoxCreateParticle" ] ).Click += SceneBoxCreateParticle_Click;
			( (Button)window.Controls[ "SceneBoxFullScreenEffect" ] ).Click += SceneBoxFullScreenEffect_Click;
			//( (Button)window.Controls[ "RenderToTextureTest" ] ).Click += RenderToTextureTest_Click;

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;

			renderToTexture.Create( window.Controls[ "RenderToTextureOutput" ] );
		}

		protected override void OnDetach()
		{
			if( renderToTexture != null )
				renderToTexture.Destroy();

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				Close();
				return true;
			}

			return false;
		}

		protected override void OnRender()
		{
			base.OnRender();

			//if( sceneBox != null )
			//{
			//   Radian angle = RendererWorld.Instance.FrameRenderTime / 8;
			//   sceneBox.CameraPosition = new Vec3(
			//      MathFunctions.Cos( angle ) * 5,
			//      MathFunctions.Sin( angle ) * 5, 3 );
			//}
		}

		void Close()
		{
			SetShouldDetach();
		}

		void Close_Click( Button sender )
		{
			Close();
		}

		void SceneBoxChangeModel_Click( Button sender )
		{
			//update mesh name for object with name "MyMesh".
			SceneBox.SceneBoxMesh sceneMesh = sceneBox.FindObjectByName( "MyMesh" ) as SceneBox.SceneBoxMesh;
			if( sceneMesh != null )
			{
				string[] meshNames = new string[] 
				{
					"Base\\Simple Models\\Box.mesh",
					"Base\\Simple Models\\Cylinder.mesh",
					"Base\\Simple Models\\RoundBox.mesh",
					"Base\\Simple Models\\Sphere.mesh",
				};

				sceneBoxMeshIndex++;
				if( sceneBoxMeshIndex >= meshNames.Length )
					sceneBoxMeshIndex = 0;
				sceneMesh.MeshName = meshNames[ sceneBoxMeshIndex ];

				string[] materials = new string[] { "Red", "Green", "Blue", "Yellow" };
				sceneMesh.OverrideMaterial = materials[ new Random().Next( materials.Length ) ];
			}
		}

		void SceneBoxCreateParticle_Click( Button sender )
		{
			sceneBox.AddParticle( Vec3.Zero, Quat.Identity, Vec3.One, "ExplosionParticle", true );
		}

		void SceneBoxFullScreenEffect_Click( Button sender )
		{
			if( sceneBox.Viewport != null )
			{
				string effectName = "OldTV";
				if( sceneBox.Viewport.GetCompositorInstance( effectName ) == null )
				{
					CompositorInstance instance = sceneBox.Viewport.AddCompositor( effectName );
					if( instance != null )
						instance.Enabled = true;
				}
				else
					sceneBox.Viewport.RemoveCompositor( effectName );
			}
		}

		//void RenderToTextureTest_Click( Button sender )
		//{
		//   if( renderToTexture.Texture == null )
		//      renderToTexture.Create( window.Controls[ "RenderToTextureOutput" ] );
		//   else
		//      renderToTexture.Destroy();
		//}
	}
}
