// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.PhysicsSystem;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	public class CatapultGameWindow : GameWindow
	{
		//HUD screen
		Control hudControl;

		//catapult controlling
		MapObject catapult;
		bool catapultFiring;
		Vec2 catapultFiringMouseStartPosition;

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//To load the HUD screen
			hudControl = ControlDeclarationManager.Instance.CreateControl( "Maps\\CatapultGame\\Gui\\HUD.gui" );
			//Attach the HUD screen to the this window
			Controls.Add( hudControl );

			//find Catapult
			foreach( Entity entity in Map.Instance.Children )
			{
				if( entity.Type.Name == "CatapultGame_Catapult" )
				{
					catapult = (MapObject)entity;
					break;
				}
			}
		}

		protected override void OnDetach()
		{
			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			//change camera type
			if( e.Key == EKeys.F7 )
			{
				FreeCameraEnabled = !FreeCameraEnabled;
				GameEngineApp.Instance.AddScreenMessage( "Camera type: " +
					( FreeCameraEnabled ? "Free" : "Game" ) );
				return true;
			}

			//select another demo map
			if( e.Key == EKeys.F3 )
			{
				GameWorld.Instance.NeedChangeMap( "Maps\\MainDemo\\Map.map", "Teleporter_Maps", null );
				return true;
			}

			return base.OnKeyDown( e );
		}

		//MapObject GetObjectOverCursor()
		//{
		//   Camera camera = RendererWorld.Instance.DefaultCamera;
		//   Ray ray = camera.GetCameraToViewportRay( MousePosition );
		//   MapObject mapObject = null;
		//   Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
		//   {
		//      //skip StaticMesh'es
		//      if( obj is StaticMesh )
		//         return true;
		//      mapObject = obj;
		//      return false;
		//   } );
		//   return mapObject;
		//}

		bool IsCursorOverCatapult()
		{
			if( catapult != null )
			{
				Camera camera = RendererWorld.Instance.DefaultCamera;
				Ray ray = camera.GetCameraToViewportRay( MousePosition );
				if( catapult.MapBounds.RayIntersection( ray ) )
					return true;
			}
			return false;
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			if( !FreeCameraEnabled && button == EMouseButtons.Left )
			{
				if( IsCursorOverCatapult() )
				{
					catapultFiring = true;
					catapultFiringMouseStartPosition = MousePosition;
					return true;
				}
			}

			if( button == EMouseButtons.Right )
			{
				//remove all CatapultBullet

				List<Entity> list = new List<Entity>();
				foreach( Entity entity in Map.Instance.Children )
				{
					if( entity.Type.Name == "CatapultGame_CatapultBullet" )
						list.Add( entity );
				}

				foreach( Entity entity in list )
					entity.SetForDeletion( true );

			}

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			if( catapultFiring && button == EMouseButtons.Left )
			{
				Fire();
				catapultFiring = false;
				return true;
			}

			return base.OnMouseUp( button );
		}

		MapCamera FindFirstMapCamera()
		{
			foreach( Entity entity in Entities.Instance.EntitiesCollection )
			{
				MapCamera mapCamera = entity as MapCamera;
				if( mapCamera != null )
					return mapCamera;
			}
			return null;
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward, out Vec3 up,
			ref Degree cameraFov )
		{
			MapCamera mapCamera = FindFirstMapCamera();

			if( mapCamera != null )
			{
				position = mapCamera.Position;
				forward = mapCamera.Rotation.GetForward();
				up = mapCamera.Rotation.GetUp();
				cameraFov = mapCamera.Fov;
			}
			else
			{
				position = Vec3.Zero;
				forward = new Vec3( 1, 0, 0 );
				up = new Vec3( 0, 0, 1 );
				//fov = 90;
			}
		}

		void UpdateHUD()
		{
			hudControl.Visible = EngineDebugSettings.DrawGui;

			hudControl.Controls[ "Game" ].Visible = true;// !FreeCameraEnabled;// && !IsCutSceneEnabled();
			hudControl.Controls[ "CutScene" ].Visible = false;// IsCutSceneEnabled();
		}

		void GetFireParameters( out Vec3 pos, out Quat rot, out float speed )
		{
			Camera camera = RendererWorld.Instance.DefaultCamera;

			pos = catapult.Position + new Vec3( 0, 0, .7f );

			Radian verticalAngle = new Degree( 30 ).InRadians();

			rot = Quat.Identity;
			speed = 0;

			if( catapultFiring )
			{
				Ray startRay = camera.GetCameraToViewportRay( catapultFiringMouseStartPosition );
				Ray ray = camera.GetCameraToViewportRay( MousePosition );

				Plane plane = Plane.FromPointAndNormal( pos, Vec3.ZAxis );

				Vec3 startRayPos;
				if( !plane.RayIntersection( startRay, out startRayPos ) )
				{
					//must never happen
				}

				Vec3 rayPos;
				if( !plane.RayIntersection( ray, out rayPos ) )
				{
					//must never happen
				}

				Vec2 diff = rayPos.ToVec2() - startRayPos.ToVec2();

				Radian horizonalAngle = MathFunctions.ATan( diff.Y, diff.X ) + MathFunctions.PI;

				SphereDir dir = new SphereDir( horizonalAngle, verticalAngle );
				rot = Quat.FromDirectionZAxisUp( dir.GetVector() );

				float distance = diff.Length();

				//3 meters clamp
				MathFunctions.Clamp( ref distance, .001f, 3 );

				speed = distance * 10;
			}
		}

		protected override void OnRender()
		{
			base.OnRender();

			UpdateHUD();

			//draw debug geometry
			Camera camera = RendererWorld.Instance.DefaultCamera;
			if( !FreeCameraEnabled )
			{
				//over catapult
				if( IsCursorOverCatapult() || catapultFiring )
				{
					//draw bounding box
					camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
					camera.DebugGeometry.AddBounds( catapult.MapBounds );
				}

				//fire position
				{
					Vec3 pos;
					Quat rot;
					float speed;
					GetFireParameters( out pos, out rot, out speed );

					camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
					camera.DebugGeometry.AddSphere( new Sphere( pos, .01f ) );
					camera.DebugGeometry.AddSphere( new Sphere( pos, .02f ) );

					if( catapultFiring )
					{
						camera.DebugGeometry.Color = new ColorValue( 1, 0, 0, .5f );
						camera.DebugGeometry.AddArrow( pos, pos - rot.GetForward() * ( speed / 10 ) );

						//draw potential path
						{
							Vec3 currentPos = pos;
							Vec3 currentVelocity = rot.GetForward() * speed;

							float step = .1f;
							for( float time = 0; time <= 5; time += step )
							{
								Vec3 lastPos = currentPos;

								currentVelocity += PhysicsWorld.Instance.MainScene.Gravity * step;
								currentPos += currentVelocity * step;

								camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .5f );
								camera.DebugGeometry.AddLine( currentPos, lastPos );
							}

						}
					}
				}

			}
		}

		void Fire()
		{
			Vec3 pos;
			Quat rot;
			float speed;
			GetFireParameters( out pos, out rot, out speed );

			//create entity
			MapObject mapObject = (MapObject)Entities.Instance.Create( "CatapultGame_CatapultBullet",
				Map.Instance );
			mapObject.Position = pos;
			mapObject.Rotation = rot;
			mapObject.PostCreate();

			//apply force
			Body body = mapObject.PhysicsModel.Bodies[ 0 ];
			body.LinearVelocity = mapObject.Rotation.GetForward() * speed;
			//Vec3 vector = mapObject.Rotation.GetForward() * power * body.Mass;
			//body.AddForce( ForceType.GlobalAtLocalPos, 0, vector, Vec3.Zero );
		}

	}
}
