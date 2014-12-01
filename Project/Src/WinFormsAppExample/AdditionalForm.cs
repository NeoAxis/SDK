using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using WinFormsAppFramework;

namespace WinFormsAppExample
{
	public partial class AdditionalForm : Form
	{
		public AdditionalForm()
		{
			InitializeComponent();
		}

		private void buttonClose_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void AdditionalForm_Load( object sender, EventArgs e )
		{
			renderTargetUserControl1.AutomaticUpdateFPS = 60;
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