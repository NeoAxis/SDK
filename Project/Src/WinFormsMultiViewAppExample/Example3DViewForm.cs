// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Engine;
using Engine.Utils;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using WinFormsAppFramework;

namespace WinFormsMultiViewAppExample
{
	public partial class Example3DViewForm : DockContent
	{
		public Example3DViewForm()
		{
			InitializeComponent();
		}

		public void Init()
		{
			renderTargetUserControl1.Visible = true;
		}

		private void Example3DViewForm_Load( object sender, EventArgs e )
		{
			renderTargetUserControl1.AutomaticUpdateFPS = 15;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
		}

		void renderTargetUserControl1_Render( RenderTargetUserControl sender, Camera camera )
		{
			//update camera
			if( Map.Instance != null )
			{
				Vec3 position;
				Vec3 forward;
				Degree fov;

				//find "MapCamera_1"
				MapCamera mapCamera = Entities.Instance.GetByName( "MapCamera_1" ) as MapCamera;
				if( mapCamera != null )
				{
					position = mapCamera.Position;
					forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
					fov = mapCamera.Fov;
				}
				else
				{
					position = Map.Instance.EditorCameraPosition;
					forward = Map.Instance.EditorCameraDirection.GetVector();
					fov = Map.Instance.Fov;
				}

				if( fov == 0 )
					fov = Map.Instance.Fov;

				renderTargetUserControl1.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
				renderTargetUserControl1.CameraFixedUp = Vec3.ZAxis;
				renderTargetUserControl1.CameraFov = fov;
				renderTargetUserControl1.CameraPosition = position;
				renderTargetUserControl1.CameraDirection = forward;
			}
		}
	}
}
