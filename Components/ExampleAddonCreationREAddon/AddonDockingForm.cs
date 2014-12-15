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

namespace ExampleAddonCreationREAddon
{
	public partial class AddonDockingForm : DockContent, IDockContentUpdateFonts
	{
		MeshObject meshObject;
		SceneNode sceneNode;

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

		void renderTargetUserControl1_PreUpdate( RenderTargetUserControl sender )
		{
			if( checkBoxOverrideSceneObjects.Checked )
			{
				//create mesh object
				if( sceneNode == null )
					CreateMeshObject();

				//show mesh object on the scene
				if( sceneNode != null )
					sceneNode.Visible = true;

				//override visibility (hide main scene objects, show from lists)
				List<SceneNode> sceneNodes = new List<SceneNode>();
				if( sceneNode != null )
					sceneNodes.Add( sceneNode );
				SceneManager.Instance.SetOverrideVisibleObjects( new SceneManager.OverrideVisibleObjectsClass(
					new StaticMeshObject[ 0 ], sceneNodes.ToArray(), new RenderLight[ 0 ] ) );

				////draw box by debug geometry
				//camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				//camera.DebugGeometry.AddBounds( new Bounds( new Vec3( -1, -1, -1 ), new Vec3( 1, 1, 1 ) ) );
			}
		}

		void renderTargetUserControl1_PostUpdate( RenderTargetUserControl sender )
		{
			if( checkBoxOverrideSceneObjects.Checked )
			{
				//hide mesh object on the scene
				if( sceneNode != null )
					sceneNode.Visible = false;
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

		void CreateMeshObject()
		{
			DestroyMeshObject();

			meshObject = SceneManager.Instance.CreateMeshObject( "Base\\Simple Models\\Cylinder.mesh" );
			if( meshObject != null )
			{
				meshObject.SetMaterialNameForAllSubObjects( "Red" );

				sceneNode = new SceneNode();
				sceneNode.Visible = false;
				sceneNode.Position = new Vec3( 5, 0, 0 );
				sceneNode.Rotation = new Angles( 50, 50, 50 ).ToQuat();
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

		private void AddonDockingForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			DestroyMeshObject();
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
