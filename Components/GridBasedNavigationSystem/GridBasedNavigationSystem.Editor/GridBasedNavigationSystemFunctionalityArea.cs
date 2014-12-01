// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Utils;

namespace Engine.Editor
{
	class GridBasedNavigationSystemFunctionalityArea : FunctionalityArea
	{
		GridBasedNavigationSystem system;
		GridBasedNavigationSystemExtendedFunctionalityDescriptor extendedFunctionalityDescriptor;

		bool pathTest;
		Vec3 startPosition;
		bool found;
		float time;
		int pathCount;

		//

		public GridBasedNavigationSystemFunctionalityArea( GridBasedNavigationSystem system,
			GridBasedNavigationSystemExtendedFunctionalityDescriptor extendedFunctionalityDescriptor )
		{
			this.system = system;
			this.extendedFunctionalityDescriptor = extendedFunctionalityDescriptor;
		}

		public override string ToString()
		{
			return Translate( "Pathfinding Test" );
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( e.Key == EKeys.Escape || e.Key == EKeys.Space )
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
			RayCastResult result = PhysicsWorld.Instance.RayCast( ray,
				(int)ContactGroup.CastOnlyCollision );
			if( result.Shape != null )
			{
				pos = result.Position;
				return true;
			}
			return false;
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

		static void AddArrow( Camera camera, Vec3 from, Vec3 to, float thickness, float arrowSize )
		{
			Vec3 dir = ( to - from ).GetNormalize();
			float size = ( to - from ).Length();

			AddThicknessLine( camera, from, from + dir * ( size - arrowSize ), thickness );
			camera.DebugGeometry.AddLine( from, from + dir * ( size - arrowSize ) );

			AddCone( camera, from + dir * ( size - arrowSize ), to, arrowSize / 6 );
		}

		static void AddSphere( Camera camera, Sphere sphere )
		{
			Vec3[] positions;
			int[] indices;
			GeometryGenerator.GenerateSphere( sphere.Radius, 4, 4, false, out positions, out indices );

			Mat4 transform = new Mat4( Mat3.Identity, sphere.Origin );
			camera.DebugGeometry.AddVertexIndexBuffer( positions, indices, transform, false, true );
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			Vec3 offset = new Vec3( 0, 0, .2f );

			if( pathTest )
			{
				camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
				AddArrow( camera, startPosition + new Vec3( 0, 0, 4 ), startPosition + offset, .07f, 1.2f );
			}

			Vec3 endPosition;
			if( GetPositionByCursor( out endPosition ) )
			{
				camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
				AddArrow( camera, endPosition + new Vec3( 0, 0, 4 ), endPosition + offset, .07f, 1.2f );

				found = false;

				if( pathTest )
				{
					//find the path

					List<Vec2> path = new List<Vec2>();

					float startTime = EngineApp.Instance.Time;

					float unitSize = system.GridCellSize * .8f;
					int maxFieldsDistance = 1000000;// extendedFunctionalityDescriptor.MaxFieldsDistance;
					int maxFieldsToCheck = 1000000;// extendedFunctionalityDescriptor.MaxFieldsToCheck;
					bool smooth = extendedFunctionalityDescriptor.Smooth;
					bool visualize = extendedFunctionalityDescriptor.Visualize;

					found = system.FindPath( unitSize, startPosition.ToVec2(), endPosition.ToVec2(), maxFieldsDistance,
						maxFieldsToCheck, smooth, visualize, path );
					time = EngineApp.Instance.Time - startTime;
					pathCount = path.Count;

					if( found )
					{
						system.DebugDrawPath( camera, startPosition.ToVec2(), path );
					}
					//else
					//{
					//   List<Vec2> list = new List<Vec2>();
					//   list.Add( endPosition.ToVec2() );
					//   system.DebugDrawPath( camera, startPosition.ToVec2(), list, new ColorValue( 1, 0, 0 ) );
					//}
				}
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			Vec3 endPosition;
			if( GetPositionByCursor( out endPosition ) )
			{
				if( pathTest )
				{
					List<string> lines = new List<string>();
					lines.Add( found ? "Path found" : "Path not found" );
					if( time < 0.001f )
						lines.Add( string.Format( Translate( "Time: <0.001 seconds" ) ) );
					else
						lines.Add( string.Format( Translate( "Time: {0} seconds" ), time.ToString( "F3" ) ) );
					if( found )
						lines.Add( string.Format( "Points: {0}", pathCount ) );

					AddTextLinesWithShadow( renderer, lines, new Rect( .05f, .075f, 1, 1 ), HorizontalAlign.Left, VerticalAlign.Top,
						new ColorValue( 1, 1, 0 ) );
				}
				else
				{
					string text = Translate( "Specify start and end points with the mouse" );
					AddTextWithShadow( renderer, text, new Vec2( .5f, .5f ), HorizontalAlign.Center, VerticalAlign.Center,
						new ColorValue( 1, 1, 0 ) );
				}
			}
		}

		string Translate( string text )
		{
			return ToolsLocalization.Translate( "GridBasedNavigationSystemFunctionalityArea", text );
		}
	}
}
