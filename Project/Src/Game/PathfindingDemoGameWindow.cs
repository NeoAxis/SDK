// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System.Collections.Generic;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for PathfindingDemo.
	/// </summary>
	public class PathfindingDemoGameWindow : GameWindow
	{
		//HUD screen
		Control hudControl;

		//path test
		bool pathTest;
		Vec3 startPosition;
		Vec3 endPosition;
		Vec3[] path;
		bool found;
		float time;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			//To load the HUD screen
			hudControl = ControlDeclarationManager.Instance.CreateControl( "Maps\\PathfindingDemo\\Gui\\HUD.gui" );
			//Attach the HUD screen to the this window
			Controls.Add( hudControl );
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

			if( button == EMouseButtons.Left )
			{
				if( GetPositionByCursor( out startPosition ) )
					pathTest = true;
			}

			if( button == EMouseButtons.Right )
			{
				MapObject mapObject = GetMapObjectByCursor();
				if( mapObject != null )
				{
					UnitAttack( mapObject );
				}
				else
				{
					Vec3 position;
					if( GetPositionByCursor( out position ) )
						UnitMove( position );
				}
			}

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			if( button == EMouseButtons.Left && pathTest )
				pathTest = false;

			return base.OnMouseUp( button );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			if( pathTest )
			{
				found = false;

				if( GetPositionByCursor( out endPosition ) )
				{
					float startTime = EngineApp.Instance.Time;

					found = GetNavigationSystem().FindPath( startPosition, endPosition, 1,
						new Vec3( 2, 2, 2 ), 512, 4096, 16, out path );

					float endTime = EngineApp.Instance.Time;
					time = endTime - startTime;
				}
			}
		}

		static Vec3[] addSpherePositions;
		static int[] addSphereIndices;
		static void AddSphere( Camera camera, Sphere sphere )
		{
			if( addSpherePositions == null )
				GeometryGenerator.GenerateSphere( sphere.Radius, 8, 8, false, out addSpherePositions, out addSphereIndices );

			Mat4 transform = new Mat4( Mat3.Identity, sphere.Origin );
			camera.DebugGeometry.AddVertexIndexBuffer( addSpherePositions, addSphereIndices, transform, false, true );
		}

		static Vec3[] addThicknessLinePositions;
		static int[] addThicknessLineIndices;
		static void AddThicknessLine( Camera camera, Vec3 start, Vec3 end, float thickness )
		{
			Vec3 diff = end - start;
			Vec3 direction = diff.GetNormalize();
			Quat rotation = Quat.FromDirectionZAxisUp( direction );
			float length = diff.Length();
			float thickness2 = thickness;

			Mat4 t = new Mat4( rotation.ToMat3() * Mat3.FromScale( new Vec3( length, thickness2, thickness2 ) ),
				( start + end ) * .5f );

			if( addThicknessLinePositions == null )
				GeometryGenerator.GenerateBox( new Vec3( 1, 1, 1 ), out addThicknessLinePositions, out addThicknessLineIndices );
			camera.DebugGeometry.AddVertexIndexBuffer( addThicknessLinePositions, addThicknessLineIndices, t, false, true );
		}

		static void AddCone( Camera camera, Vec3 from, Vec3 to, float radius )
		{
			Vec3 direction = to - from;
			float length = direction.Normalize();

			Vec3[] positions;
			int[] indices;
			GeometryGenerator.GenerateCone( radius, length, 32, true, true, out positions, out indices );

			Quat rotation = Quat.FromDirectionZAxisUp( direction );
			Mat4 transform = new Mat4( rotation.ToMat3(), from );
			camera.DebugGeometry.AddVertexIndexBuffer( positions, indices, transform, false, true );
		}

		static void AddArrow( Camera camera, Vec3 from, Vec3 to, float thickness, float arrowSize )
		{
			Vec3 dir = ( to - from ).GetNormalize();
			float size = ( to - from ).Length();

			AddThicknessLine( camera, from, from + dir * ( size - arrowSize ), thickness );
			camera.DebugGeometry.AddLine( from, from + dir * ( size - arrowSize ) );

			AddCone( camera, from + dir * ( size - arrowSize ), to, arrowSize / 6 );
		}

		protected override void OnRender()
		{
			base.OnRender();

			Camera camera = RendererWorld.Instance.DefaultCamera;

			GetNavigationSystem().DebugDrawNavMesh( camera );

			//path test
			{
				Vec3 offset = new Vec3( 0, 0, .2f );

				if( pathTest )
				{
					camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
					AddArrow( camera, startPosition + new Vec3( 0, 0, 4 ), startPosition + offset, .07f, 1.2f );

					List<string> lines = new List<string>();

					if( time < 0.001f )
						lines.Add( string.Format( "Time: <0.001 seconds" ) );
					else
						lines.Add( string.Format( "Time: {0} seconds", time.ToString( "F3" ) ) );

					//SodanKerjuu: we check if the path will lead us close enough to where we wanted
					bool futile = false;
					if( found )
					{
						if( ( path[ path.Length - 1 ] - endPosition ).Length() > 1f )
						{
							futile = true;
							lines.Add( "Path found, but didn't reach close enough to end point." );
						}
						else
							lines.Add( "Path found" );
					}
					else
						lines.Add( "Path not found" );

					//write out information
					if( found )
					{
						lines.Add( string.Format( "Points: {0}", path.Length ) );

						camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
						for( int n = 1; n < path.Length; n++ )
						{
							Vec3 from = path[ n - 1 ] + offset;
							Vec3 to = path[ n ] + offset;
							AddThicknessLine( camera, from, to, .07f );
							camera.DebugGeometry.AddLine( from, to );
						}

						camera.DebugGeometry.Color = futile ? new ColorValue( 1, 0, 0 ) : new ColorValue( 1, 1, 0 );
						foreach( Vec3 point in path )
							AddSphere( camera, new Sphere( point + offset, .15f ) );
					}

					//show end position and arrow between start and end
					if( GetPositionByCursor( out endPosition ) )
					{
						camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
						AddArrow( camera, endPosition + new Vec3( 0, 0, 4 ), endPosition + offset, .07f, 1.2f );
					}

					EngineApp.Instance.ScreenGuiRenderer.AddTextLines( lines, new Vec2( .05f, .075f ),
						HorizontalAlign.Left, VerticalAlign.Top, 0, new ColorValue( 1, 1, 1 ) );
				}
				else
				{
					//show end position and arrow between start and end
					Vec3 pos;
					if( GetPositionByCursor( out pos ) )
					{
						camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
						AddArrow( camera, pos + new Vec3( 0, 0, 4 ), pos + offset, .07f, 1.2f );
					}
				}
			}

			//highlight cursor on object
			{
				MapObject mapObject = GetMapObjectByCursor();
				if( mapObject != null )
				{
					camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
					Box box = mapObject.GetBox();
					box.Expand( .1f );
					camera.DebugGeometry.AddBox( box );
				}
			}

			//draw paths of units
			foreach( Entity entity in Map.Instance.Children )
			{
				Character character = entity as Character;
				if( character != null )
				{
					GameCharacterAI ai = character.Intellect as GameCharacterAI;
					if( ai != null )
					{
						ai.DebugDrawTasks( camera );
						ai.DebugDrawPath( camera );
					}
				}
			}

			UpdateHUD();
		}

		/// <summary>
		/// Updates HUD screen
		/// </summary>
		void UpdateHUD()
		{
			hudControl.Visible = EngineDebugSettings.DrawGui;

			//Game
			//hudControl.Controls[ "Game" ].Visible = !FreeCameraEnabled && !IsCutSceneEnabled();
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			List<string> lines = new List<string>();

			lines.Add( "Unit tasks:" );
			foreach( Entity entity in Map.Instance.Children )
			{
				Character character = entity as Character;
				if( character != null )
				{
					GameCharacterAI ai = character.Intellect as GameCharacterAI;
					if( ai != null )
					{
						if( lines.Count > 1 )
							lines.Add( "" );
						lines.Add( "  " + ai.CurrentTask.ToString() );
						foreach( GameCharacterAI.Task task in ai.Tasks )
							lines.Add( "  " + task.ToString() );
					}
				}
			}

			EngineApp.Instance.ScreenGuiRenderer.AddTextLines( lines, new Vec2( .95f, .075f ),
				HorizontalAlign.Right, VerticalAlign.Top, 0, new ColorValue( 1, 1, 1 ) );
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward,
			out Vec3 up, ref Degree cameraFov )
		{
			position = Vec3.Zero;
			forward = Vec3.XAxis;
			up = Vec3.ZAxis;

			MapCamera mapCamera = Entities.Instance.GetByName( "MapCamera_0" ) as MapCamera;
			if( mapCamera != null )
			{
				position = mapCamera.Position;
				forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
				up = mapCamera.Rotation * new Vec3( 0, 0, 1 );
				if( mapCamera.Fov != 0 )
					cameraFov = mapCamera.Fov;
			}
		}

		public Control HUDControl
		{
			get { return hudControl; }
		}

		RecastNavigationSystem GetNavigationSystem()
		{
			//use first instance on the map
			return RecastNavigationSystem.Instances[ 0 ];
		}

		MapObject GetMapObjectByCursor()
		{
			if( !EngineApp.Instance.MouseRelativeMode )
			{
				Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
					EngineApp.Instance.MousePosition );

				MapObject result = null;

				Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
				{
					//check by sphere
					Sphere sphere = new Sphere( obj.Position, .5f );
					if( sphere.RayIntersection( ray ) )
					{
						//find entities with Dynamic class only
						Dynamic dynamic = obj as Dynamic;
						if( dynamic != null )
						{
							result = obj;
							//stop GetObjects
							return false;
						}
					}
					//find next object
					return true;
				} );

				return result;
			}

			return null;
		}

		bool GetPositionByCursor( out Vec3 pos )
		{
			pos = Vec3.Zero;

			if( !EngineApp.Instance.MouseRelativeMode )
			{
				Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
					EngineApp.Instance.MousePosition );

				RayCastResult[] results = PhysicsWorld.Instance.RayCastPiercing( ray,
					(int)ContactGroup.CastOnlyCollision );
				foreach( RayCastResult result in results )
				{
					Radian angle = MathUtils.GetVectorsAngle( result.Normal, ray.Direction );
					if( angle > MathFunctions.PI / 2 )
					{
						pos = result.Position;
						return true;
					}
				}
			}

			return false;
		}

		void UnitMove( Vec3 position )
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				Character character = entity as Character;
				if( character != null )
				{
					GameCharacterAI ai = character.Intellect as GameCharacterAI;
					if( ai != null )
					{
						string text = string.Format( "Unit move to \"{0}\"", position.ToString( 1 ) );
						GameEngineApp.Instance.AddScreenMessage( text );

						bool toQueue = EngineApp.Instance.IsKeyPressed( EKeys.Shift );
						ai.AutomaticTasks = GameCharacterAI.AutomaticTasksEnum.Disabled;
						ai.DoTask( new GameCharacterAI.MoveTask( ai, position, .5f ), toQueue );
					}
				}
			}
		}

		void UnitAttack( MapObject attackedObject )
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				Character character = entity as Character;
				if( character != null && attackedObject != character )
				{
					GameCharacterAI ai = character.Intellect as GameCharacterAI;
					if( ai != null )
					{
						string text = string.Format( "Unit attack \"{0}\"", entity.ToString() );
						GameEngineApp.Instance.AddScreenMessage( text );

						bool toQueue = EngineApp.Instance.IsKeyPressed( EKeys.Shift );
						ai.AutomaticTasks = GameCharacterAI.AutomaticTasksEnum.Disabled;
						ai.DoTask( new GameCharacterAI.AttackTask( ai, attackedObject ), toQueue );
					}
				}
			}
		}

	}
}
