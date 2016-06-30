// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using WeifenLuo.WinFormsUI.Docking;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;
using Engine.Renderer;
using Engine.MathEx;
using EditorBase;

namespace ExampleAddonCreationMEAddon
{
	public partial class AddonDockingForm : EditorBase.Theme.EditorDockContent, IDockContentUpdateFonts
	{
		bool sceneCreated;
		List<RenderLight> lights = new List<RenderLight>();
		List<SceneNode> sceneNodes = new List<SceneNode>();

		Engine.UISystem.Control guiExampleControl;

		//

		public AddonDockingForm()
		{
			InitializeComponent();

			renderTargetUserControl1.PreUpdate += renderTargetUserControl1_PreUpdate;
			renderTargetUserControl1.PostUpdate += renderTargetUserControl1_PostUpdate;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
			renderTargetUserControl1.RenderUI += renderTargetUserControl1_RenderUI;
		}

		public void UpdateFonts()
		{
		}

		void SetObjectsVisible( bool visible )
		{
			foreach( var light in lights )
				light.Visible = visible;
			foreach( var node in sceneNodes )
				node.Visible = visible;
		}

		void renderTargetUserControl1_PreUpdate( RenderTargetUserControl sender )
		{
			if( checkBoxOverrideSceneObjects.Checked )
			{
				//update camera position
				renderTargetUserControl1.CameraNearFarClipDistance = new Range( .1f, 10000 );
				renderTargetUserControl1.CameraProjectionType = ProjectionTypes.Perspective;
				renderTargetUserControl1.CameraOrthoWindowHeight = 100;
				renderTargetUserControl1.CameraFixedUp = new Vec3( 0, 0, 1 );
				renderTargetUserControl1.CameraFov = 80;
				renderTargetUserControl1.CameraPosition = new Vec3( 2, 20, 17 );
				renderTargetUserControl1.CameraDirection = ( new Vec3( 0, 0, 0 ) - renderTargetUserControl1.CameraPosition ).GetNormalize();

				//create scene
				if( !sceneCreated )
					CreateScene();

				//show scene
				SetObjectsVisible( true );

				SceneManager.Instance.SetOverrideVisibleObjects( new SceneManager.OverrideVisibleObjectsClass(
					new StaticMeshObject[ 0 ], sceneNodes.ToArray(), lights.ToArray() ) );

				////draw box by debug geometry
				//camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				//camera.DebugGeometry.AddBounds( new Bounds( new Vec3( -1, -1, -1 ), new Vec3( 1, 1, 1 ) ) );
			}
			else
			{
				//update camera position
				Camera defaultCamera = RendererWorld.Instance.DefaultCamera;
				renderTargetUserControl1.CameraNearFarClipDistance = new Range( defaultCamera.NearClipDistance, defaultCamera.FarClipDistance );
				renderTargetUserControl1.CameraProjectionType = defaultCamera.ProjectionType;
				renderTargetUserControl1.CameraOrthoWindowHeight = defaultCamera.OrthoWindowHeight;
				renderTargetUserControl1.CameraFixedUp = defaultCamera.FixedUp;
				renderTargetUserControl1.CameraFov = defaultCamera.Fov;
				renderTargetUserControl1.CameraPosition = defaultCamera.Position;
				renderTargetUserControl1.CameraDirection = defaultCamera.Direction;
			}
		}

		void renderTargetUserControl1_PostUpdate( RenderTargetUserControl sender )
		{
			if( checkBoxOverrideSceneObjects.Checked )
			{
				//hide scene
				SetObjectsVisible( false );

				//reset overriding scene objects
				SceneManager.Instance.ResetOverrideVisibleObjects();
			}
		}

		void renderTargetUserControl1_Render( RenderTargetUserControl sender, Camera camera )
		{
		}

		void renderTargetUserControl1_RenderUI( RenderTargetUserControl sender, GuiRenderer renderer )
		{
			//example of text rendering
			//renderer.AddText( "NeoAxis 3D Engine " + EngineVersionInformation.Version, new Vec2( 0, 0 ), HorizontalAlign.Left,
			//   VerticalAlign.Top, new ColorValue( 1, 0, 0 ) );
		}

		void CreateScene()
		{
			DestroyScene();

			//create light
			var light = SceneManager.Instance.CreateLight();
			light.Type = RenderLightType.Directional;
			light.Direction = new Vec3( 0, 0, -1 );
			lights.Add( light );

			//create meshes

			string[] colorNames = new string[] { "Red", "Blue", "Yellow", "Green" };
			int colorCounter = 0;

			for( float z = -5; z <= 5; z++ )
			{
				for( float y = -5; y <= 5; y++ )
				{
					for( float x = -5; x <= 5; x++ )
					{
						MeshObject meshObject = SceneManager.Instance.CreateMeshObject( "Base\\Simple Models\\Cylinder.mesh" );
						if( meshObject != null )
						{
							meshObject.SetMaterialNameForAllSubObjects( colorNames[ colorCounter % colorNames.Length ] );
							colorCounter++;

							SceneNode sceneNode = new SceneNode();
							sceneNode.Position = new Vec3( x * 2, y * 2, z * 2 );
							sceneNode.Rotation = new Angles( 0, 0, 0 ).ToQuat();
							sceneNode.Scale = new Vec3( 1, 1, 1 );
							sceneNode.Attach( meshObject );

							sceneNodes.Add( sceneNode );
						}
					}
				}
			}

			//Hide by default. Show during drawing time.
			SetObjectsVisible( false );

			sceneCreated = true;
		}

		void DestroyScene()
		{
			foreach( var light in lights )
				light.Dispose();
			lights.Clear();

			foreach( var node in sceneNodes )
			{
				while( node.MovableObjects.Count != 0 )
				{
					MovableObject obj = node.MovableObjects[ node.MovableObjects.Count - 1 ];
					node.Detach( obj );
					obj.Dispose();
				}
				node.Dispose();
			}
			sceneNodes.Clear();

			sceneCreated = false;
		}

		private void AddonDockingForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			DestroyScene();
		}

		private void checkBoxInteractive2DGUI_CheckedChanged( object sender, EventArgs e )
		{
			if( checkBoxInteractive2DGUI.Checked )
				CreateGUI();
			else
				DestroyGUI();
		}

		void CreateGUI()
		{
			DestroyGUI();

			guiExampleControl = new GUIExampleControl();
			renderTargetUserControl1.ControlManager.Controls.Add( guiExampleControl );
		}

		void DestroyGUI()
		{
			if( guiExampleControl != null )
			{
				guiExampleControl.SetShouldDetach();
				guiExampleControl = null;
			}
		}
	}
}
