// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;

namespace Engine.Editor
{
	// -----------------------
	#region Path Find Tester

	class RecastNavigationSystemPathFindFunctionalityArea : FunctionalityArea
	{
		RecastNavigationSystemExtendedFunctionalityDescriptor extendedFunctionalityDescriptor;

		bool pathTest;
		Vec3 startPosition;
		Vec3 endPosition;
		Vec3[] path;
		bool found;
		float time;

		//

		public RecastNavigationSystemPathFindFunctionalityArea( RecastNavigationSystemExtendedFunctionalityDescriptor
		 extendedFunctionalityDescriptor )
		{
			this.extendedFunctionalityDescriptor = extendedFunctionalityDescriptor;
		}

		public override string ToString()
		{
			return "Path finding: Test mode";
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( e.Key == EKeys.Escape )
			{
				MapEditorInterface.Instance.FunctionalityArea = null;
				return true;
			}
			return base.OnKeyDown( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( button == EMouseButtons.Left )
			{
				if( GetPositionByCursor( out startPosition ) )
					pathTest = true;
			}
			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			if( button == EMouseButtons.Left && pathTest )
				pathTest = false;
			return base.OnMouseUp( button );
		}

		bool GetPositionByCursor( out Vec3 pos )
		{
			pos = Vec3.Zero;

			if( EngineApp.Instance.MouseRelativeMode )
				return false;

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

			return false;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			if( pathTest )
			{
				found = false;

				if( GetPositionByCursor( out endPosition ) )
				{
					float startTime = EngineApp.Instance.Time;

					const float stepSize = 1.0f;
					found = RecastNavigationSystem.Instance.FindPath( startPosition, endPosition, stepSize, out path );

					float endTime = EngineApp.Instance.Time;
					time = endTime - startTime;
				}
			}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( pathTest )
			{
				Vec3 offset = new Vec3( 0, 0, .2f );

				camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
				camera.DebugGeometry.AddArrow( startPosition + new Vec3( 0, 0, 3 ), startPosition + offset );

				List<string> lines = new List<string>();

				if( time < 0.001f )
					lines.Add( string.Format( "Time: <0.001 seconds" ) );
				else
					lines.Add( string.Format( "Time: " + time.ToString( "F3" ) + " seconds" ) );

				//SodanKerjuu: we check if the path will lead us close enough to where we wanted
				bool futile = false;
				if( found )
				{
					if( ( path[ path.Length - 1 ] - endPosition ).LengthFast() > 5f )
					{ futile = true; lines.Add( "Detour: Path found, but didn't reach close enough to end point." ); }
					else
						lines.Add( "Detour: Path found" );
				}
				else
					lines.Add( "Detour: Path not found" );

				//write out information
				if( found )
				{
					lines.Add( string.Format( "Points: {0}", path.Length ) );

					camera.DebugGeometry.Color = futile ? new ColorValue( 1, 0, 0 ) : new ColorValue( 0, 1, 0 );
					for( int n = 0; n < path.Length; n++ )
					{
						Vec3 point = path[ n ];
						camera.DebugGeometry.AddSphere( new Sphere( point, .15f ), 8 );
					}
				}

				//show end position and arrow between start and end
				if( GetPositionByCursor( out endPosition ) )
				{
					camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
					camera.DebugGeometry.AddArrow( endPosition + new Vec3( 0, 0, 3 ), endPosition + offset );

					camera.DebugGeometry.SetSpecialDepthSettings( false, true );
					camera.DebugGeometry.Color = ( !found || futile ) ? new ColorValue( 1, 0, 0, .5f ) : new ColorValue( 1, 1, 0, .5f );
					camera.DebugGeometry.AddArrow( startPosition, endPosition );
					camera.DebugGeometry.RestoreDefaultDepthSettings();
				}

				EngineApp.Instance.ScreenGuiRenderer.AddTextLines( lines, new Vec2( .05f, .075f ),
					HorizontalAlign.Left, VerticalAlign.Top, 0, new ColorValue( 1, 1, 1 ) );
			}
		}
	}

	#endregion
	//------------------------

	//------------------------
	#region Select / Desect entities

	class RecastNavigationSystemGeometryFunctionalityArea : FunctionalityArea
	{
		Entity targetedEntity;

		RecastNavigationSystemExtendedFunctionalityDescriptor extendedFunctionalityDescriptor;

		//

		public RecastNavigationSystemGeometryFunctionalityArea(
			 RecastNavigationSystemExtendedFunctionalityDescriptor extendedFunctionalityDescriptor )
		{
			this.extendedFunctionalityDescriptor = extendedFunctionalityDescriptor;
		}

		public override string ToString()
		{
			return "Select / Deselect geometry";
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( e.Key == EKeys.Escape )
			{
				MapEditorInterface.Instance.FunctionalityArea = null;
				return true;
			}
			return base.OnKeyDown( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( button == EMouseButtons.Left )
				ToggleEntity();

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			return base.OnMouseUp( button );
		}

		void ToggleEntity()
		{
			if( EngineApp.Instance.MouseRelativeMode )
				return;

			MapEditorInterface.Instance.SetMapModified();

			Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay( EngineApp.Instance.MousePosition );
			RayCastResult[] results = PhysicsWorld.Instance.RayCastPiercing( ray, (int)ContactGroup.CastOnlyCollision );

			foreach( RayCastResult result in results )
			{
				//heightmapterrain
				HeightmapTerrain terrain = HeightmapTerrain.GetTerrainByBody( result.Shape.Body );
				if( terrain != null )
				{
					//deselect
					if( RecastNavigationSystem.Instance.Geometries.Contains( terrain ) )
					{
						//Log.Info("Removed terrain " + terrain.Name + " from geometries.");
						RecastNavigationSystem.Instance.Geometries.Remove( terrain );
						return;
					}

					//select
					if( RecastNavigationSystem.Instance.GeometryVerifier( terrain, true ) )
					{
						//Log.Info("Added terrain " + terrain.Name + " to geometries.");
						RecastNavigationSystem.Instance.Geometries.Add( terrain );
						return;
					}
				}

				MapObject mapObject = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );
				if( mapObject != null )
				{
					//deselect
					if( RecastNavigationSystem.Instance.Geometries.Contains( mapObject ) )
					{
						RecastNavigationSystem.Instance.Geometries.Remove( mapObject );
						return;
					}

					//select
					if( RecastNavigationSystem.Instance.GeometryVerifier( mapObject, true ) )
					{
						RecastNavigationSystem.Instance.Geometries.Add( mapObject );
						return;
					}
				}

				Log.Error( "Unknown entity with collisions, what shenanigans!" );
			}

			return;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );
		}

		protected void UpdateTargetObject()
		{
			//redraw
			targetedEntity = null;

			if( EngineApp.Instance.MouseRelativeMode )
				return;

			Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay( EngineApp.Instance.MousePosition );

			RayCastResult[] results = PhysicsWorld.Instance.RayCastPiercing( ray, (int)ContactGroup.CastOnlyCollision );

			foreach( RayCastResult result in results )
			{
				//terrain
				HeightmapTerrain terrain = HeightmapTerrain.GetTerrainByBody( result.Shape.Body );
				if( terrain != null )
				{
					targetedEntity = terrain;
					return;
				}

				//other
				targetedEntity = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );
				return;
			}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			//SodanKerjuu: this was OnTick before but it caused null references some times when destroying / deselecting 
			UpdateTargetObject();

			if( targetedEntity != null )
			{
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );

				if( targetedEntity is MapObject )
					( targetedEntity as MapObject ).DoEditorSelectionDebugRender( camera, true, false );
			}

			foreach( Entity entity in RecastNavigationSystem.Instance.Geometries )
			{
				camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );

				if( entity is MapObject )
					( entity as MapObject ).DoEditorSelectionDebugRender( camera, false, false );
				else if( entity is HeightmapTerrain )
					( entity as HeightmapTerrain ).DoEditorSelectionDebugRender( camera, false );
			}
		}
	}

	#endregion
	//------------------------
}
