// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for ball game demo
	/// </summary>
	public class BallGameWindow : GameWindow
	{
		//HUD screen
		Control hudControl;

		SphereDir cameraDirection = new SphereDir( MathFunctions.PI / 2, -MathFunctions.PI / 4 );
		float cameraDistance = 8;

		MapObject ball;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			//load the HUD screen
			hudControl = ControlDeclarationManager.Instance.CreateControl(
				"Maps\\BallGame\\Gui\\BallGameHUD.gui" );
			//attach the HUD screen to the this window
			Controls.Add( hudControl );

			if( EntitySystemWorld.Instance.IsSingle() || EntitySystemWorld.Instance.IsServer() )
				RecreateBallIfNeed();
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

				GameEngineApp.Instance.AddScreenMessage(
					string.Format( "Camera type: {0}", FreeCameraEnabled ? "Free" : "Default" ) );

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

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			return base.OnMouseUp( button );
		}

		protected override bool OnMouseWheel( int delta )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseWheel( delta );

			cameraDistance -= (float)delta * .003f;
			if( cameraDistance < 1 )
				cameraDistance = 1;

			return base.OnMouseWheel( delta );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			//update mouse relative mode
			{
				if( FreeCameraEnabled && FreeCameraMouseRotating )
					EngineApp.Instance.MouseRelativeMode = true;
				else
					EngineApp.Instance.MouseRelativeMode = false;
			}

			if( EntitySystemWorld.Instance.IsSingle() || EntitySystemWorld.Instance.IsServer() )
				RecreateBallIfNeed();

			bool consoleActive = EngineConsole.Instance != null && EngineConsole.Instance.Active;
			if( !FreeCameraEnabled && EntitySystemWorld.Instance.Simulation && ball != null && !consoleActive )
				TickUserForceToBall( delta );
		}

		void TickUserForceToBall( float delta )
		{
			const float forceMultiplier = 700;

			Vec2 vec = Vec2.Zero;
			if( EngineApp.Instance.IsKeyPressed( EKeys.Left ) || EngineApp.Instance.IsKeyPressed( EKeys.A ) )
				vec.Y++;
			if( EngineApp.Instance.IsKeyPressed( EKeys.Right ) || EngineApp.Instance.IsKeyPressed( EKeys.D ) )
				vec.Y--;
			if( EngineApp.Instance.IsKeyPressed( EKeys.Up ) || EngineApp.Instance.IsKeyPressed( EKeys.W ) )
				vec.X++;
			if( EngineApp.Instance.IsKeyPressed( EKeys.Down ) || EngineApp.Instance.IsKeyPressed( EKeys.S ) )
				vec.X--;

			if( vec != Vec2.Zero )
			{
				vec.Normalize();

				//calculate force vector with considering camera orientation
				Vec2 diff = ball.Position.ToVec2() - RendererWorld.Instance.DefaultCamera.Position.ToVec2();
				Degree angle = new Radian( MathFunctions.ATan( diff.Y, diff.X ) );
				Degree vecAngle = new Radian( MathFunctions.ATan( -vec.Y, vec.X ) );
				Quat rot = new Angles( 0, 0, vecAngle - angle ).ToQuat();
				Vec2 forceVector2 = ( rot * new Vec3( 1, 0, 0 ) ).ToVec2();
				Vec3 forceVector = new Vec3( forceVector2.X, forceVector2.Y, 0 );

				Body body = ball.PhysicsModel.Bodies[ 0 ];
				body.AddForce( ForceType.GlobalAtLocalPos, delta, forceVector * forceMultiplier, Vec3.Zero );
			}
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward,
			out Vec3 up, ref Degree cameraFov )
		{
			if( ball != null && !ball.IsSetForDeletion )
			{
				Vec3 lookAt = ball.GetInterpolatedPosition();
				Vec3 cameraPos = lookAt - cameraDirection.GetVector() * cameraDistance;

				position = cameraPos;
				forward = ( lookAt - position ).GetNormalize();
				up = Vec3.ZAxis;
			}
			else
			{
				position = Vec3.Zero;
				forward = Vec3.XAxis;
				up = Vec3.ZAxis;
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( hudControl != null )
				hudControl.Visible = EngineDebugSettings.DrawGui;
		}

		protected override void OnRender()
		{
			base.OnRender();
		}

		SpawnPoint FindSpawnPoint()
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				SpawnPoint point = entity as SpawnPoint;
				if( point != null )
					return point;
			}
			return null;
		}

		void RecreateBallIfNeed()
		{
			const float minHeight = -50;

			if( ball != null && ball.IsSetForDeletion )
				ball = null;

			if( ball != null && ball.Position.Z < minHeight )
			{
				ball.SetForDeletion( true );
				return;
			}

			if( ball == null )
			{
				ball = (MapObject)Entities.Instance.Create( "BallGame_Ball", Map.Instance );
				SpawnPoint spawnPoint = FindSpawnPoint();
				if( spawnPoint != null )
					ball.Position = spawnPoint.Position;
				ball.PostCreate();
			}

		}

	}
}
