// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.FileSystem;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a base window of game.
	/// </summary>
	public abstract class GameWindow : Control
	{
		static GameWindow instance;

		bool simulationAfterCloseMenuWindow;

		[Config( "Camera", "fov" )]
		public static Degree fov = 0;
		[Config( "Camera", "freeCameraSpeedNormal" )]
		public static float freeCameraSpeedNormal = 20;
		[Config( "Camera", "freeCameraSpeedFast" )]
		public static float freeCameraSpeedFast = 100;

		//free camera
		bool freeCameraEnabled;

		Vec3 freeCameraPosition;
		SphereDir freeCameraDirection;
		bool freeCameraMouseRotating;
		Vec2 freeCameraRotatingStartPos;

		//

		public static GameWindow Instance
		{
			get { return instance; }
		}

		protected override void OnAttach()
		{
			base.OnAttach();
			instance = this;

			freeCameraPosition = Map.Instance.EditorCameraPosition;
			freeCameraDirection = Map.Instance.EditorCameraDirection;

			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				EntitySystemWorld.Instance.Simulation = true;
		}

		protected override void OnDetach()
		{
			EngineApp.Instance.MouseRelativeMode = false;

			instance = null;
			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			if( e.Key == EKeys.Escape )
			{
				EngineApp.Instance.KeysAndMouseButtonUpAll();
				Controls.Add( new MenuWindow() );
				return true;
			}

			//simulation pause 
			if( e.Key == EKeys.F8 )
			{
				if( EntitySystemWorld.Instance.IsClientOnly() )
				{
					Log.Warning( "You cannot suspend or continue simulation on the client." );
					return true;
				}

				EntitySystemWorld.Instance.Simulation = !EntitySystemWorld.Instance.Simulation;

				if( EntitySystemWorld.Instance.Simulation )
					GameEngineApp.Instance.AddScreenMessage( "Resume Game" );
				else
					GameEngineApp.Instance.AddScreenMessage( "Game Paused" );

				return true;
			}

			return base.OnKeyDown( e );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			if( Controls.Count != 1 )
				return;

			EntitySystemWorld.Instance.Tick();

			//Shound change map
			if( GameWorld.Instance != null && GameWorld.Instance.NeedChangeMapName != null )
			{
				GameEngineApp.Instance.ServerOrSingle_MapLoad( GameWorld.Instance.NeedChangeMapName,
					EntitySystemWorld.Instance.DefaultWorldType, false );
			}

			//moving free camera by keys
			if( FreeCameraEnabled && OnFreeCameraIsAllowToMove() )
			{
				float cameraVelocity;
				if( EngineApp.Instance.IsKeyPressed( EKeys.Shift ) )
					cameraVelocity = freeCameraSpeedFast;
				else
					cameraVelocity = freeCameraSpeedNormal;

				Vec3 pos = freeCameraPosition;
				SphereDir dir = freeCameraDirection;

				float step = cameraVelocity * delta;

				bool activeConsole = EngineConsole.Instance != null && EngineConsole.Instance.Active;

				if( !activeConsole )
				{
					if( EngineApp.Instance.IsKeyPressed( EKeys.W ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Up ) )
					{
						pos += dir.GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.S ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Down ) )
					{
						pos -= dir.GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.A ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Left ) )
					{
						pos += new SphereDir(
							dir.Horizontal + MathFunctions.PI / 2, 0 ).GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.D ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Right ) )
					{
						pos += new SphereDir(
							dir.Horizontal - MathFunctions.PI / 2, 0 ).GetVector() * step;
					}

					if( EngineApp.Instance.IsKeyPressed( EKeys.Q ) )
						pos += new SphereDir( dir.Horizontal, dir.Vertical + MathFunctions.PI / 2 ).GetVector() * step;
					if( EngineApp.Instance.IsKeyPressed( EKeys.E ) )
						pos += new SphereDir( dir.Horizontal, dir.Vertical - MathFunctions.PI / 2 ).GetVector() * step;
				}

				freeCameraPosition = pos;
			}

			if( freeCameraMouseRotating && !FreeCameraEnabled )
				freeCameraMouseRotating = false;
		}

		protected virtual bool OnFreeCameraIsAllowToMove()
		{
			//disable movement of free camera if console activated
			if( EngineConsole.Instance != null && EngineConsole.Instance.Active )
				return false;

			return true;
		}

		protected override void OnRender()
		{
			base.OnRender();

			//update camera orientation
			{
				Vec3 position, forward, up;
				Degree cameraFov = ( fov != 0 ) ? fov : Map.Instance.Fov;

				if( !FreeCameraEnabled )
				{
					OnGetCameraTransform( out position, out forward, out up, ref cameraFov );
				}
				else
				{
					position = freeCameraPosition;
					forward = freeCameraDirection.GetVector();
					up = Vec3.ZAxis;
				}

				if( cameraFov == 0 )
					cameraFov = ( fov != 0 ) ? fov : Map.Instance.Fov;

				Camera camera = RendererWorld.Instance.DefaultCamera;
				camera.NearClipDistance = Map.Instance.NearFarClipDistance.Minimum;
				camera.FarClipDistance = Map.Instance.NearFarClipDistance.Maximum;

				camera.FixedUp = up;
				camera.Fov = cameraFov;
				camera.Position = position;
				camera.Direction = forward;
			}

			//update game specific options
			{
				//water reflection level
				foreach( WaterPlane waterPlane in WaterPlane.Instances )
					waterPlane.ReflectionLevel = GameEngineApp.WaterReflectionLevel;

				//decorative objects
				if( DecorativeObjectManager.Instance != null )
					DecorativeObjectManager.Instance.Visible = GameEngineApp.ShowDecorativeObjects;

				//HeightmapTerrain
				//enable simple rendering for Low material scheme.
				foreach( HeightmapTerrain terrain in HeightmapTerrain.Instances )
					terrain.SimpleRendering = GameEngineApp.MaterialScheme == MaterialSchemes.Low;
			}

			//update sound listener
			if( SoundWorld.Instance != null )
			{
				Vec3 position, velocity, forward, up;
				OnGetSoundListenerTransform( out position, out velocity, out forward, out up );
				SoundWorld.Instance.SetListener( position, velocity, forward, up );
			}
		}

		protected override void OnControlAttach( Control control )
		{
			base.OnControlAttach( control );
			if( control as MenuWindow != null )
			{
				simulationAfterCloseMenuWindow = EntitySystemWorld.Instance.Simulation;

				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
					EntitySystemWorld.Instance.Simulation = false;

				EngineApp.Instance.MouseRelativeMode = false;
			}
		}

		protected override void OnControlDetach( Control control )
		{
			if( control as MenuWindow != null )
			{
				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
					EntitySystemWorld.Instance.Simulation = simulationAfterCloseMenuWindow;
			}

			base.OnControlDetach( control );
		}

		protected abstract void OnGetCameraTransform( out Vec3 position, out Vec3 forward, out Vec3 up,
			ref Degree cameraFov );

		protected virtual void OnGetSoundListenerTransform( out Vec3 position, out Vec3 velocity,
			out Vec3 forward, out Vec3 up )
		{
			Camera camera = RendererWorld.Instance.DefaultCamera;

			position = camera.Position;
			velocity = Vec3.Zero;
			forward = camera.Direction;
			up = camera.Up;
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			if( base.OnMouseDown( button ) )
				return true;

			//free camera rotating
			if( FreeCameraEnabled )
			{
				if( button == EMouseButtons.Right )
				{
					freeCameraMouseRotating = true;
					freeCameraRotatingStartPos = EngineApp.Instance.MousePosition;
				}
			}

			return true;
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			//free camera rotating
			if( button == EMouseButtons.Right && freeCameraMouseRotating )
			{
				EngineApp.Instance.MouseRelativeMode = false;
				freeCameraMouseRotating = false;
			}

			return base.OnMouseUp( button );
		}

		protected override void OnMouseMove()
		{
			base.OnMouseMove();

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			//free camera rotating
			if( FreeCameraEnabled && freeCameraMouseRotating )
			{
				if( !EngineApp.Instance.MouseRelativeMode )
				{
					Vec2 diffPixels = ( MousePosition - freeCameraRotatingStartPos ) *
						new Vec2( EngineApp.Instance.VideoMode.X, EngineApp.Instance.VideoMode.Y );
					if( Math.Abs( diffPixels.X ) >= 3 || Math.Abs( diffPixels.Y ) >= 3 )
					{
						EngineApp.Instance.MouseRelativeMode = true;
					}
				}
				else
				{
					SphereDir dir = freeCameraDirection;
					dir.Horizontal -= MousePosition.X;// *cameraRotateSensitivity;
					dir.Vertical -= MousePosition.Y;// *cameraRotateSensitivity;

					dir.Horizontal = MathFunctions.RadiansNormalize360( dir.Horizontal );

					const float vlimit = MathFunctions.PI / 2 - .01f;
					if( dir.Vertical > vlimit ) dir.Vertical = vlimit;
					if( dir.Vertical < -vlimit ) dir.Vertical = -vlimit;

					freeCameraDirection = dir;
				}
			}
		}

		public bool FreeCameraEnabled
		{
			get { return freeCameraEnabled; }
			set { freeCameraEnabled = value; }
		}

		public bool FreeCameraMouseRotating
		{
			get { return freeCameraMouseRotating; }
		}

		public virtual void OnBeforeWorldSave() { }

	}
}
