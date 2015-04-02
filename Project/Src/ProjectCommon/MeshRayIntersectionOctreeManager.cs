// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace ProjectCommon
{
	/// <summary>
	/// The class for finding ray intersection with the mesh.
	/// </summary>
	public class MeshRayIntersectionOctreeManager
	{
		//TO DO: Use one octree for all sub meshes.
		//TO DO: Less memory usage.

		Mesh mesh;
		Item[] items;

		///////////////

		class Item
		{
			SubMesh subMesh;
			Vec3[] positions;
			int[] indices;
			OctreeContainer octreeContainer;

			//

			public Item( SubMesh subMesh )
			{
				this.subMesh = subMesh;

				subMesh.GetSomeGeometry( out positions, out indices );

				Bounds bounds = Bounds.Cleared;
				foreach( Vec3 pos in positions )
					bounds.Add( pos );
				bounds.Expand( bounds.GetSize() * .001f );

				OctreeContainer.InitSettings initSettings = new OctreeContainer.InitSettings();
				initSettings.InitialOctreeBounds = bounds;
				initSettings.OctreeBoundsRebuildExpand = Vec3.Zero;
				initSettings.MinNodeSize = bounds.GetSize() / 50;
				octreeContainer = new OctreeContainer( initSettings );

				for( int nTriangle = 0; nTriangle < indices.Length / 3; nTriangle++ )
				{
					Vec3 vertex0 = positions[ indices[ nTriangle * 3 + 0 ] ];
					Vec3 vertex1 = positions[ indices[ nTriangle * 3 + 1 ] ];
					Vec3 vertex2 = positions[ indices[ nTriangle * 3 + 2 ] ];

					Bounds triangleBounds = new Bounds( vertex0 );
					triangleBounds.Add( vertex1 );
					triangleBounds.Add( vertex2 );

					int octreeIndex = octreeContainer.AddObject( triangleBounds, 1 );
				}
			}

			public bool CheckIntersection( Ray ray, out float rayIntersectionScale )
			{
				bool foundIntersection = false;
				float nearestScale = 0;

				foreach( OctreeContainer.GetObjectsRayOutputData data in octreeContainer.GetObjects( ray, 0xFFFFFFFF ) )
				{
					int nTriangle = data.ObjectIndex;
					Vec3 vertex0 = positions[ indices[ nTriangle * 3 + 0 ] ];
					Vec3 vertex1 = positions[ indices[ nTriangle * 3 + 1 ] ];
					Vec3 vertex2 = positions[ indices[ nTriangle * 3 + 2 ] ];

					float scale;
					bool found = MathUtils.IntersectTriangleRay( vertex0, vertex1, vertex2, ray, out scale );
					if( found )
					{
						if( !foundIntersection || scale < nearestScale )
						{
							foundIntersection = true;
							nearestScale = scale;
						}
					}
				}

				rayIntersectionScale = nearestScale;
				return foundIntersection;
			}

			public void Dispose()
			{
				if( octreeContainer != null )
				{
					octreeContainer.Dispose();
					octreeContainer = null;
				}
			}
		}

		///////////////

		public MeshRayIntersectionOctreeManager( Mesh mesh )
		{
			this.mesh = mesh;
			items = new Item[ mesh.SubMeshes.Length ];

			for( int n = 0; n < items.Length; n++ )
			{
				Item item = new Item( mesh.SubMeshes[ n ] );
				items[ n ] = item;
			}
		}

		public void Dispose()
		{
			foreach( Item item in items )
				item.Dispose();
		}

		public bool CheckIntersection( Ray ray, out int subMeshIndex, out float scale )
		{
			int nearestIndex = -1;
			float nearestScale = 0;

			for( int n = 0; n < mesh.SubMeshes.Length; n++ )
			{
				Item item = items[ n ];

				float s;
				if( item.CheckIntersection( ray, out s ) )
				{
					if( nearestIndex == -1 || s < nearestScale )
					{
						nearestIndex = n;
						nearestScale = s;
					}
				}
			}

			subMeshIndex = nearestIndex;
			scale = nearestScale;
			return nearestIndex != -1;
		}
	}
}
