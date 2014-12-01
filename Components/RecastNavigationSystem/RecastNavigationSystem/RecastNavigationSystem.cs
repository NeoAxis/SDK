// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Design;
using Engine.FileSystem;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;

namespace Engine
{
	public class RecastNavigationSystemType : MapGeneralObjectType
	{
		public RecastNavigationSystemType()
		{
			AllowEmptyName = true;
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct Wrapper
	{
		public const string library = "Recast";
		public const CallingConvention convention = CallingConvention.Cdecl;

		[DllImport( Wrapper.library, EntryPoint = "Recast_Initialize", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr Initialize(
			 ref Vec3 bmin, ref Vec3 bmax,
			 float tileSize, float cellSize, float cellHeight,
			 int minRegionSize, int mergeRegionSize, [MarshalAs( UnmanagedType.U1 )] bool monotonePartitioning,
			 float maxEgdeLength, float maxEdgeError,
			 int vertsPerPoly, float detailSampleDistance, float detailMaxSampleError,
			 float agentHeight, float agentRadius, float agentMaxClimb, float agentMaxSlope );

		[DllImport( Wrapper.library, EntryPoint = "Recast_NavQueryInit", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool NavQueryInit( IntPtr world, int maxNodes );

		[DllImport( Wrapper.library, EntryPoint = "Recast_GetSizes", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetSizes( IntPtr world, out int maxTiles, out int maxPolysPerTile );

		[DllImport( Wrapper.library, EntryPoint = "Recast_BuildAllTiles", CallingConvention = Wrapper.convention )]
		public unsafe static extern void BuildAllTiles( IntPtr world );

		[DllImport( Wrapper.library, EntryPoint = "Recast_DestroyAllTiles", CallingConvention = Wrapper.convention )]
		public unsafe static extern void DestroyAllTiles( IntPtr world );

		[DllImport( Wrapper.library, EntryPoint = "Recast_SetGeometry", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetGeometry( IntPtr world, IntPtr vertices, int vertexCount,
			IntPtr indices, int indexCount, int trianglesPerChunk );

		[DllImport( Wrapper.library, EntryPoint = "Recast_Destroy", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Destroy( IntPtr world );

		[DllImport( Wrapper.library, EntryPoint = "Recast_GetNavigationMesh", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool GetNavigationMesh( IntPtr world, out Vec3* vertices,
			out int vertexCount );

		[DllImport( Wrapper.library, EntryPoint = "Recast_FindPath", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool FindPath( IntPtr world, ref Vec3 start, ref Vec3 end, float stepSize,
			ref Vec3 polygonPickExtents, int maxPolygonPath, int maxSmoothPath, int maxSteerPoints,
			out Vec3* outPath, out int outPathCount );

		[DllImport( Wrapper.library, EntryPoint = "Recast_FreeMemory", CallingConvention = Wrapper.convention )]
		public unsafe static extern void FreeMemory( IntPtr pointer );

		[DllImport( Wrapper.library, EntryPoint = "Recast_LoadNavMesh", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool LoadNavMesh( IntPtr world, IntPtr data, int dataSize );

		[DllImport( Wrapper.library, EntryPoint = "Recast_SaveNavMesh", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SaveNavMesh( IntPtr world, out IntPtr data, out int dataSize );

		[DllImport( Wrapper.library, EntryPoint = "Recast_BuildTile", CallingConvention = Wrapper.convention )]
		public unsafe static extern void BuildTile( IntPtr world, ref Vec3 position );

		[DllImport( Wrapper.library, EntryPoint = "Recast_RemoveTile", CallingConvention = Wrapper.convention )]
		public unsafe static extern void RemoveTile( IntPtr world, ref Vec3 position );
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////

	[ExtendedFunctionalityDescriptor( "Engine.Editor.RecastNavigationSystemExtendedFunctionalityDescriptor, RecastNavigationSystem.Editor" )]
	public class RecastNavigationSystem : MapGeneralObject
	{
		static List<RecastNavigationSystem> instances = new List<RecastNavigationSystem>();
		static ReadOnlyCollection<RecastNavigationSystem> instancesReadOnly;

		[FieldSerialize( "boundsMin" )]
		Vec3 boundsMin = new Vec3( -100, -100, -100 );

		[FieldSerialize( "boundsMax" )]
		Vec3 boundsMax = new Vec3( 100, 100, 100 );

		[FieldSerialize( "tileSize" )]
		int tileSize = 32;

		[FieldSerialize( "cellSize" )]
		float cellSize = .3f;

		[FieldSerialize( "cellHeight" )]
		float cellHeight = .3f;

		[FieldSerialize( "trianglesPerChunk" )]
		int trianglesPerChunk = 512;

		[FieldSerialize( "minRegionSize" )]
		int minRegionSize = 8;

		[FieldSerialize( "mergeRegionSize" )]
		int mergeRegionSize = 20;

		[FieldSerialize( "monotonePartitioning" )]
		bool monotonePartitioning = false;

		[FieldSerialize( "maxEdgeLength" )]
		float maxEdgeLength = 12;

		[FieldSerialize( "maxEdgeError" )]
		float maxEdgeError = 1.3f;

		[FieldSerialize( "maxVerticesPerPolygon" )]
		int maxVerticesPerPolygon = 6;

		[FieldSerialize( "detailSampleDistance" )]
		float detailSampleDistance = 6;

		[FieldSerialize( "detailMaxSampleError" )]
		float detailMaxSampleError = 1;

		[FieldSerialize( "agentHeight" )]
		float agentHeight = 2.0f;

		[FieldSerialize( "agentRadius" )]
		float agentRadius = .6f;

		[FieldSerialize( "agentMaxClimb" )]
		float agentMaxClimb = .9f;

		[FieldSerialize( "agentMaxSlope" )]
		Degree agentMaxSlope = 45;

		[FieldSerialize( "geometries" )]
		List<Entity> geometries = new List<Entity>();
		ReadOnlyCollection<Entity> geometriesReadOnly;

		[FieldSerialize( "alwaysDrawNavMesh" )]
		bool alwaysDrawNavMesh;

		[FieldSerialize( "pathfindingMaxNodes" )]
		int pathfindingMaxNodes = 8192;

		[FieldSerialize( "dataDirectory" )]
		string dataDirectory = "RecastNavigationSystem";

		//[FieldSerialize( "gridHeight" )]
		//float gridHeight = .5f;

		IntPtr recastWorld;

		Vec3[] debugNavigationMeshVertices;
		int[] debugNavigationMeshIndices;

		//Vec3[] tileGridMeshVertices;
		//int[] tileGridMeshIndices;
		//Vec3[] cellGridMeshVertices;
		//int[] cellGridMeshIndices;

		//bool drawTileGrid;

		///////////////////////////////////////////

		[TypeField]
		RecastNavigationSystemType __type = null;
		/// <summary>
		/// Gets the entity type.
		/// </summary>
		public new RecastNavigationSystemType Type { get { return __type; } }

		///////////////////////////////////////////

		class IndexVertexBufferCollector
		{
			//!!!!!!need instancing
			public Vec3[] resultVertices = new Vec3[ 4096 ];
			public int[] resultIndices = new int[ 4096 ];
			public int resultVertexCount;
			public int resultIndexCount;

			public void Add( Vec3[] vertices, int vertexCount, int[] indices, int indexCount )
			{
				int newVertexCount = resultVertexCount + vertexCount;
				int newIndexCount = resultIndexCount + indexCount;

				if( newVertexCount > resultVertices.Length )
				{
					int s = resultVertices.Length;
					while( newVertexCount > s )
						s *= 2;
					Vec3[] old = resultVertices;
					resultVertices = new Vec3[ s ];
					Array.Copy( old, resultVertices, old.Length );
				}

				if( newIndexCount > resultIndices.Length )
				{
					int s = resultIndices.Length;
					while( newIndexCount > s )
						s *= 2;
					int[] old = resultIndices;
					resultIndices = new int[ s ];
					Array.Copy( old, resultIndices, old.Length );
				}

				Array.Copy( vertices, 0, resultVertices, resultVertexCount, vertexCount );
				for( int n = 0; n < indexCount; n++ )
					resultIndices[ resultIndexCount + n ] = resultVertexCount + indices[ n ];
				resultVertexCount = newVertexCount;
				resultIndexCount = newIndexCount;
			}

		}

		///////////////////////////////////////////

		public RecastNavigationSystem()
		{
			instances.Add( this );

			geometriesReadOnly = new ReadOnlyCollection<Entity>( geometries );

			NativeLibraryManager.PreLoadLibrary( "Recast" );
		}

		public static IList<RecastNavigationSystem> Instances
		{
			get
			{
				if( instancesReadOnly == null )
					instancesReadOnly = new ReadOnlyCollection<RecastNavigationSystem>( instances );
				return instancesReadOnly;
			}
		}

		[Category( "Debug" )]
		[DefaultValue( false )]
		public bool AlwaysDrawNavMesh
		{
			get { return alwaysDrawNavMesh; }
			set { alwaysDrawNavMesh = value; }
		}

		//[Browsable( false )] //SodanKerjuu: controlled by the initialize toolbox form
		//public bool DrawTileGrid
		//{
		//   get { return drawTileGrid; }
		//   set { drawTileGrid = value; }
		//}

		//[DefaultValue( .5f )]
		//public float GridHeight
		//{
		//   get { return gridHeight; }
		//   set
		//   {
		//      gridHeight = value;
		//      MathFunctions.Saturate( ref gridHeight );
		//      ClearDebugGrids();
		//   }
		//}

		[Category( "Grid" )]
		public Vec3 BoundsMin
		{
			get { return boundsMin; }
			set
			{
				if( boundsMin == value )
					return;
				boundsMin = value;

				//ClearDebugGrids();
			}
		}

		[Category( "Grid" )]
		public Vec3 BoundsMax
		{
			get { return boundsMax; }
			set
			{
				if( boundsMax == value )
					return;
				boundsMax = value;

				//ClearDebugGrids();
			}
		}

		[Category( "Grid" )]
		[DefaultValue( 32 )]
		[LocalizedDescription( "The size of a tile.", "RecastNavigationSystem" )]
		public int TileSize
		{
			get { return tileSize; }
			set
			{
				if( value < 16 )
					value = 16;
				tileSize = value;
			}
		}

		[Category( "Grid" )]
		[DefaultValue( .3f )]
		[LocalizedDescription( "The width and depth resolution used when sampling the source geometry. The width and depth of the voxels in voxel fields. The width and depth of the cell columns that make up voxel fields. A lower value allows for the generated meshes to more closely match the source geometry, but at a higher processing and memory cost.", "RecastNavigationSystem" )]
		public float CellSize
		{
			get { return cellSize; }
			set
			{
				if( value < .01f )
					value = .01f;
				cellSize = value;
			}
		}

		[Category( "Grid" )]
		[DefaultValue( .3f )]
		[LocalizedDescription( "The height resolution used when sampling the source geometry. The height of the voxels in voxel fields.", "RecastNavigationSystem" )]
		public float CellHeight
		{
			get { return cellHeight; }
			set
			{
				if( value < .01f )
					value = .01f;
				cellHeight = value;
			}
		}

		[Category( "Grid" )]
		[DefaultValue( 512 )]
		[LocalizedDescription( "Max amount of triangles for each chunk in the internal AABB tree.", "RecastNavigationSystem" )]
		public int TrianglesPerChunk
		{
			get { return trianglesPerChunk; }
			set
			{
				if( value < 128 )
					value = 128;
				trianglesPerChunk = value;
			}
		}

		[Category( "Regions" )]
		[DefaultValue( 8 )]
		[LocalizedDescription( "The minimum region size for unconnected (island) regions. The value is in voxels. Regions that are not connected to any other region and are smaller than this size will be culled before mesh generation. I.e. They will no longer be considered traversable.", "RecastNavigationSystem" )]
		public int MinRegionSize
		{
			get { return minRegionSize; }
			set
			{
				if( value < 1 )
					value = 1;
				minRegionSize = value;
			}
		}

		[Category( "Regions" )]
		[DefaultValue( 20 )]
		[LocalizedDescription( "Any regions smaller than this size will, if possible, be merged with larger regions. Value is in voxels. Helps reduce the number of small regions. This is especially an issue in diagonal path regions where inherent faults in the region generation algorithm can result in unnecessarily small regions.", "RecastNavigationSystem" )]
		public int MergeRegionSize
		{
			get { return mergeRegionSize; }
			set
			{
				if( value < 0 )
					value = 0;
				mergeRegionSize = value;
			}
		}

		[Category( "Regions" )]
		[DefaultValue( false )]
		[LocalizedDescription( "Partition the walkable surface into simple regions without holes.", "RecastNavigationSystem" )]
		public bool MonotonePartitioning
		{
			get { return monotonePartitioning; }
			set { monotonePartitioning = value; }
		}

		[Category( "Polygonization" )]
		[DefaultValue( 12.0f )]
		[LocalizedDescription( "The maximum length of polygon edges that represent the border of meshes. More vertices will be added to border edges if this value is exceeded for a particular edge. A value of zero will disable this feature.", "RecastNavigationSystem" )]
		public float MaxEdgeLength
		{
			get { return maxEdgeLength; }
			set
			{
				if( value < 0 )
					value = 0;
				maxEdgeLength = value;
			}
		}

		[Category( "Polygonization" )]
		[DefaultValue( 1.3f )]
		[LocalizedDescription( "The maximum distance the edges of meshes may deviate from the source geometry. A lower value will result in mesh edges following the xz-plane geometry contour more accurately at the expense of an increased triangle count.", "RecastNavigationSystem" )]
		public float MaxEdgeError
		{
			get { return maxEdgeError; }
			set
			{
				if( value < .1f )
					value = .1f;
				maxEdgeError = value;
			}
		}

		[Category( "Polygonization" )]
		[DefaultValue( 6 )]
		public int MaxVerticesPerPolygon
		{
			get { return maxVerticesPerPolygon; }
			set
			{
				if( value < 3 )
					value = 3;
				maxVerticesPerPolygon = value;

				//!!!!!!
				//need change debug drawing NavMesh. at this time navmesh draws as triangle list.
			}
		}

		[Category( "Detail Mesh" )]
		[DefaultValue( 6.0f )]
		[LocalizedDescription( "Sets the sampling distance to use when matching the detail mesh to the surface of the original geometry. Impacts how well the final detail mesh conforms to the surface contour of the original geometry. Higher values result in a detail mesh which conforms more closely to the original geometry's surface at the cost of a higher final triangle count and higher processing cost.", "RecastNavigationSystem" )]
		public float DetailSampleDistance
		{
			get { return detailSampleDistance; }
			set
			{
				if( value < 0 )
					value = 0;
				detailSampleDistance = value;
			}
		}

		[Category( "Detail Mesh" )]
		[DefaultValue( 1.0f )]
		[LocalizedDescription( "The maximum distance the surface of the detail mesh may deviate from the surface of the original geometry.", "RecastNavigationSystem" )]
		public float DetailMaxSampleError
		{
			get { return detailMaxSampleError; }
			set
			{
				if( value < 0 )
					value = 0;
				detailMaxSampleError = value;
			}
		}

		[Category( "Agent" )]
		[DefaultValue( 2.0f )]
		[LocalizedDescription( "Minimum height where the agent can still walk.", "RecastNavigationSystem" )]
		public float AgentHeight
		{
			get { return agentHeight; }
			set
			{
				if( value < .1f )
					value = .1f;
				agentHeight = value;
			}
		}

		[Category( "Agent" )]
		[DefaultValue( .6f )]
		[LocalizedDescription( "Radius of the agent.", "RecastNavigationSystem" )]
		public float AgentRadius
		{
			get { return agentRadius; }
			set
			{
				if( value < 0 )
					value = 0;
				agentRadius = value;
			}
		}

		[Category( "Agent" )]
		[DefaultValue( .9f )]
		[LocalizedDescription( "Maximum height between grid cells the agent can climb.", "RecastNavigationSystem" )]
		public float AgentMaxClimb
		{
			get { return agentMaxClimb; }
			set
			{
				if( value < .001f )
					value = .001f;
				agentMaxClimb = value;
			}
		}

		[Category( "Agent" )]
		[DefaultValue( typeof( Degree ), "45" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 89 )]
		[LocalizedDescription( "Maximum walkable slope angle in degrees.", "RecastNavigationSystem" )]
		public Degree AgentMaxSlope
		{
			get { return agentMaxSlope; }
			set
			{
				if( value < 1 )
					value = 1;
				if( value > 89 )
					value = 89;
				agentMaxSlope = value;
			}
		}

		[Browsable( false )]
		public IList<Entity> Geometries
		{
			get { return geometriesReadOnly; }
		}

		[Category( "Pathfinding" )]
		[DefaultValue( 8192 )]
		[LocalizedDescription( "Maximum number of search nodes to use (max 65536).", "RecastNavigationSystem" )]
		public int PathfindingMaxNodes
		{
			get { return pathfindingMaxNodes; }
			set
			{
				if( value < 4 )
					value = 4;
				if( value > 65536 )
					value = 65536;
				pathfindingMaxNodes = value;

				if( recastWorld != IntPtr.Zero )
					Wrapper.NavQueryInit( recastWorld, pathfindingMaxNodes );
			}
		}

		//void ClearDebugGrids()
		//{
		//   tileGridMeshVertices = null;
		//   tileGridMeshIndices = null;
		//   cellGridMeshVertices = null;
		//   cellGridMeshIndices = null;
		//}

		static Vec3 ToRecastVec3( Vec3 v )
		{
			return new Vec3( v.X, v.Z, -v.Y );
		}

		static Vec3 ToEngineVec3( Vec3 v )
		{
			return new Vec3( v.X, -v.Z, v.Y );
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			InitRecastWorld();

			string virtualDataDirectory = Path.Combine( Map.Instance.GetVirtualFileDirectory(), dataDirectory );
			string virtualFilePath = Path.Combine( virtualDataDirectory, "NavMesh.dat" );

			if( VirtualFile.Exists( virtualFilePath ) )
			{
				byte[] data = VirtualFile.ReadAllBytes( virtualFilePath );
				unsafe
				{
					fixed( byte* pData = data )
					{
						if( Wrapper.LoadNavMesh( recastWorld, (IntPtr)pData, data.Length ) )
						{
							Wrapper.NavQueryInit( recastWorld, pathfindingMaxNodes );
						}
					}
				}
			}

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( EntitySystemWorld.Instance.SerializationMode == SerializationModes.MapSceneFile )
			{
				Log.Warning( "Scene export: RecastNavigationSystem is not supported." );
				return;
			}

			string mapRealFileDirectory = VirtualFileSystem.GetRealPathByVirtual(
				Map.Instance.GetVirtualFileDirectory() );
			string realFullDataDirectory = Path.Combine( mapRealFileDirectory, dataDirectory );

			string realFilePath = Path.Combine( realFullDataDirectory, "NavMesh.dat" );

			if( recastWorld != IntPtr.Zero )
			{
				if( !Directory.Exists( realFullDataDirectory ) )
					Directory.CreateDirectory( realFullDataDirectory );

				IntPtr data;
				int size;
				Wrapper.SaveNavMesh( recastWorld, out data, out size );

				if( data != IntPtr.Zero )
				{
					byte[] buffer = new byte[ size ];
					Marshal.Copy( data, buffer, 0, size );

					Wrapper.FreeMemory( data );

					File.WriteAllBytes( realFilePath, buffer );
				}
			}
			else
			{
				if( File.Exists( realFilePath ) )
					File.Delete( realFilePath );
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			string mapRealFileDirectory = VirtualFileSystem.GetRealPathByVirtual(
				Map.Instance.GetVirtualFileDirectory() );

			//dataDirectory
			for( int counter = 1; ; counter++ )
			{
				dataDirectory = "RecastNavigationSystem";
				if( counter != 1 )
					dataDirectory += counter.ToString();

				bool busy = false;
				foreach( RecastNavigationSystem system in Instances )
				{
					if( system != this && dataDirectory == system.dataDirectory )
						busy = true;
				}
				if( !busy )
				{
					string realPath = Path.Combine( mapRealFileDirectory, dataDirectory );
					if( !Directory.Exists( realPath ) )
						break;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			if( !instances.Contains( this ) )
				instances.Add( this );

			base.OnPostCreate( loaded );

			//remove null geometry entries
			if( loaded )
			{
				again:
				for( int n = 0; n < geometries.Count; n++ )
				{
					if( geometries[ n ] == null )
					{
						geometries.RemoveAt( n );
						goto again;
					}
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnDestroy()
		{
			DestroyRecastWorld();

			base.OnDestroy();

			instances.Remove( this );
		}

		public void InitRecastWorld()
		{
			unsafe
			{
				DestroyRecastWorld();

				Vec3 min = boundsMin;
				Vec3 max = boundsMax;
				for( int n = 0; n < 3; n++ )
					if( max[ n ] - min[ n ] < 1 )
						max[ n ] = min[ n ] + 1;

				Vec3 recastBoundsMin = ToRecastVec3( min );
				Vec3 recastBoundsMax = ToRecastVec3( max );

				recastWorld = Wrapper.Initialize(
					ref recastBoundsMin, ref recastBoundsMax,
					tileSize, cellSize, cellHeight,
					minRegionSize, mergeRegionSize, monotonePartitioning,
					maxEdgeLength, maxEdgeError,
					maxVerticesPerPolygon, detailSampleDistance, detailMaxSampleError,
					agentHeight, agentRadius, agentMaxClimb, (float)agentMaxSlope );

				if( recastWorld != IntPtr.Zero )
				{
					Wrapper.NavQueryInit( recastWorld, pathfindingMaxNodes );
				}

				//if( recastWorld != IntPtr.Zero )
				//{
				//Log.Info( "RecastWorld Initialized." );

				//int mT = 0, mPPT = 0;
				//Wrapper.GetSizes( recastWorld, out mT, out mPPT );
				//Log.Info( "MaxTiles: " + mT.ToString() + "  MaxPolygonsPerTile: " + mPPT.ToString() );
				//}
			}
		}

		[Browsable( false )]
		public bool IsInitialized
		{
			get { return recastWorld != IntPtr.Zero; }
		}

		public void DestroyRecastWorld()
		{
			debugNavigationMeshVertices = null;
			debugNavigationMeshIndices = null;

			if( recastWorld != IntPtr.Zero )
			{
				Wrapper.Destroy( recastWorld );
				recastWorld = IntPtr.Zero;
			}
		}

		public void DestroyAllTiles()
		{
			if( recastWorld == IntPtr.Zero )
				return;

			Wrapper.DestroyAllTiles( recastWorld );

			//refresh debug mesh
			debugNavigationMeshVertices = null;
			debugNavigationMeshIndices = null;
		}

		public bool BuildAllTiles( out string error )
		{
			error = null;

			if( recastWorld == IntPtr.Zero )
			{
				error = "Need to initialize the Recast World.";
				return false;
			}

			if( geometries.Count == 0 )
			{
				error = "No collision objects are selected.";
				return false;
			}

			IndexVertexBufferCollector collector = GetAllGeometriesForNavigationMesh();
			{
				Vec3[] vertices = collector.resultVertices;
				int[] indices = collector.resultIndices;
				int vertexCount = collector.resultVertexCount;
				int indexCount = collector.resultIndexCount;

				if( vertexCount == 0 )
				{
					error = "No vertices were gathered from collision objects.";
					return false;
				}

				//convert to Recast space
				for( int n = 0; n < vertexCount; n++ )
					vertices[ n ] = ToRecastVec3( vertices[ n ] );

				unsafe
				{
					fixed( Vec3* pVertices = vertices )
					{
						fixed( int* pIndices = indices )
						{
							Wrapper.SetGeometry( recastWorld, (IntPtr)pVertices, vertexCount, (IntPtr)pIndices,
								indexCount, trianglesPerChunk );

							Wrapper.BuildAllTiles( recastWorld );
						}
					}
				}
			}

			//refresh debug mesh
			debugNavigationMeshVertices = null;
			debugNavigationMeshIndices = null;

			return true;
		}

		static bool IsAllowSaveRecursive( Entity entity )
		{
			while( entity != null )
			{
				if( !entity.AllowSave )
					return false;
				entity = entity.Parent;
			}
			return true;
		}

		public bool IsSupportedGeometry( Entity entity )
		{
			if( !IsAllowSaveRecursive( entity ) )
				return false;

			HeightmapTerrain terrain = entity as HeightmapTerrain;
			if( terrain != null )
				return true;

			StaticMesh staticMesh = entity as StaticMesh;
			if( staticMesh != null )
			{
				if( staticMesh.Collision )
					return true;
				return false;
			}

			MapObject mapObject = entity as MapObject;
			if( mapObject != null )
			{
				foreach( MapObjectAttachedObject attachedObject in mapObject.AttachedObjects )
				{
					MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
					if( attachedMesh != null && attachedMesh.Collision )
						return true;
				}
			}

			return false;
		}

		//IndexVertexBufferCollector GetGeometriesForNavigationMesh( Bounds bounds )
		//{
		//   IndexVertexBufferCollector collector = new IndexVertexBufferCollector();

		//   странный способ
		//   Map.Instance.GetObjects( new Box( bounds ), delegate( MapObject obj )
		//      {
		//         if( geometries.Contains( obj ) )
		//            AddEntityToCollector( collector, obj );
		//      } );

		//   //heightmapterrain is not added in volume selections, this is actually good thing because now we can only add the required part
		//   {
		//      //raycast does not hit terrain?!?
		//      /*
		//      Vec3 top = bounds.GetCenter();
		//      top.Z = boundsMax.Z;

		//      Vec3 bottom = bounds.GetCenter();
		//      bottom.Z = boundsMin.Z;

		//      RayCastResult[] results = PhysicsWorld.Instance.RayCastPiercing(new Ray(top, bottom), (int)ContactGroup.CastOnlyCollision);

		//      foreach (RayCastResult result in results)
		//      {
		//          HeightmapTerrain terrain = HeightmapTerrain.GetTerrainByBody( result.Shape.Body );
		//          if( terrain != null)
		//              if( geometries.Contains(terrain) )
		//                  AddHeightmapTerrainPartToCollector(collector, terrain, bounds); 
		//      }
		//      */

		//      //SodanKerjuu: Lo-Tek style, better hope you don't have more than one terrain
		//      foreach( HeightmapTerrain terrain in HeightmapTerrain.Instances )
		//         if( geometries.Contains( terrain ) )
		//            AddHeightmapTerrainPartToCollector( collector, terrain, bounds );
		//   }

		//   return collector;
		//}

		//void AddHeightmapTerrainPartToCollector( IndexVertexBufferCollector collector, HeightmapTerrain terrain, Bounds bounds )
		//{
		//   int size = terrain.GetHeightmapSizeAsInteger();

		//   Vec3[] vertices = new Vec3[ ( size + 1 ) * ( size + 1 ) ];
		//   int[] indices = new int[ size * size * 6 ];

		//   int vertexPosition = 0;
		//   int indexPosition = 0;

		//   for( int y = 0; y < size + 1; y++ )
		//   {
		//      for( int x = 0; x < size + 1; x++ )
		//      {
		//         если очень большое число дыр, то будет излишне?
		//         //SodanKerjuu: no need, you will have unused vertices, but they won't matter for Recast if they are not indexed
		//         //if( !terrain.GetHoleFlag( new Vec2i( x, y ) ) )
		//         //{
		//         Vec2 pos2 = terrain.GetPositionXY( new Vec2i( x, y ) );
		//         Vec3 pos = new Vec3( pos2.X, pos2.Y, terrain.GetHeight( new Vec2i( x, y ) ) );
		//         vertices[ vertexPosition ] = pos;
		//         vertexPosition++;
		//         //}
		//      }
		//   }

		//   for( int y = 0; y < size; y++ )
		//   {
		//      for( int x = 0; x < size; x++ )
		//      {
		//         if( !terrain.GetHoleFlag( new Vec2i( x, y ) ) )
		//         {
		//            indices[ indexPosition + 0 ] = ( size + 1 ) * y + x;
		//            indices[ indexPosition + 1 ] = ( size + 1 ) * y + x + 1;
		//            indices[ indexPosition + 2 ] = ( size + 1 ) * ( y + 1 ) + x + 1;
		//            indices[ indexPosition + 3 ] = ( size + 1 ) * ( y + 1 ) + x + 1;
		//            indices[ indexPosition + 4 ] = ( size + 1 ) * ( y + 1 ) + x;
		//            indices[ indexPosition + 5 ] = ( size + 1 ) * y + x;
		//            indexPosition += 6;
		//         }
		//      }
		//   }

		//   collector.Add( vertices, vertexPosition, indices, indexPosition );

		//}

		bool AddEntityToCollector( IndexVertexBufferCollector collector, Entity entity )
		{
			//Static meshes
			StaticMesh staticMesh = entity as StaticMesh;
			if( staticMesh != null )
			{
				if( staticMesh.Collision == false )
					return false;

				Mesh mesh;
				if( !string.IsNullOrEmpty( staticMesh.CollisionSpecialMeshName ) )
					mesh = MeshManager.Instance.Load( staticMesh.CollisionSpecialMeshName );
				else
					mesh = MeshManager.Instance.Load( staticMesh.MeshName );

				if( mesh == null )
					return false;

				Mat4 transform = staticMesh.GetTransform();
				foreach( SubMesh subMesh in mesh.SubMeshes )
				{
					if( subMesh.AllowCollision )
					{
						Vec3[] vertices;
						int[] indices;
						subMesh.GetSomeGeometry( out vertices, out indices );
						if( vertices != null )
						{
							for( int n = 0; n < vertices.Length; n++ )
								vertices[ n ] = transform * vertices[ n ];
							collector.Add( vertices, vertices.Length, indices, indices.Length );
						}
					}
				}
				return true;
			}

			//MapObjectAttachedMesh
			MapObject mapObject = entity as MapObject;
			if( mapObject != null && !( entity is StaticMesh ) ) //SodanKerjuu: static meshes qualify as both
			{
				bool added = false;

				foreach( MapObjectAttachedObject attachedObject in mapObject.AttachedObjects )
				{
					MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
					if( attachedMesh != null && attachedMesh.Collision )
					{
						Mesh mesh;
						if( !string.IsNullOrEmpty( attachedMesh.CollisionSpecialMeshName ) )
							mesh = MeshManager.Instance.Load( attachedMesh.CollisionSpecialMeshName );
						else
							mesh = MeshManager.Instance.Load( attachedMesh.MeshName );

						if( mesh != null )
						{
							Vec3 pos;
							Quat rot;
							Vec3 scl;
							attachedMesh.GetGlobalTransform( out pos, out rot, out scl );
							Mat4 transform = new Mat4( rot.ToMat3() * Mat3.FromScale( scl ), pos );

							foreach( SubMesh subMesh in mesh.SubMeshes )
							{
								if( subMesh.AllowCollision )
								{
									Vec3[] vertices;
									int[] indices;
									subMesh.GetSomeGeometry( out vertices, out indices );
									if( vertices != null )
									{
										for( int n = 0; n < vertices.Length; n++ )
											vertices[ n ] = transform * vertices[ n ];
										collector.Add( vertices, vertices.Length, indices, indices.Length );
										added = true;
									}
								}
							}
						}
					}
				}

				return added;
			}

			//HeightmapTerrain
			HeightmapTerrain terrain = entity as HeightmapTerrain;
			if( terrain != null )
			{
				int size = terrain.GetHeightmapSizeAsInteger();

				Vec3[] vertices = new Vec3[ ( size + 1 ) * ( size + 1 ) ];
				int[] indices = new int[ size * size * 6 ];

				int vertexPosition = 0;
				int indexPosition = 0;

				for( int y = 0; y < size + 1; y++ )
				{
					for( int x = 0; x < size + 1; x++ )
					{
						//SodanKerjuu: no need, you will have unused vertices, but they won't matter for Recast if they are not indexed
						//if( !terrain.GetHoleFlag( new Vec2i( x, y ) ) )
						//{
						Vec2 pos2 = terrain.GetPositionXY( new Vec2I( x, y ) );
						Vec3 pos = new Vec3( pos2.X, pos2.Y, terrain.GetHeight( new Vec2I( x, y ) ) );
						vertices[ vertexPosition ] = pos;
						vertexPosition++;
						//}
					}
				}

				for( int y = 0; y < size; y++ )
				{
					for( int x = 0; x < size; x++ )
					{
						if( !terrain.GetHoleFlag( new Vec2I( x, y ) ) )
						{
							indices[ indexPosition + 0 ] = ( size + 1 ) * y + x;
							indices[ indexPosition + 1 ] = ( size + 1 ) * y + x + 1;
							indices[ indexPosition + 2 ] = ( size + 1 ) * ( y + 1 ) + x + 1;
							indices[ indexPosition + 3 ] = ( size + 1 ) * ( y + 1 ) + x + 1;
							indices[ indexPosition + 4 ] = ( size + 1 ) * ( y + 1 ) + x;
							indices[ indexPosition + 5 ] = ( size + 1 ) * y + x;
							indexPosition += 6;
						}
					}
				}

				collector.Add( vertices, vertexPosition, indices, indexPosition );
				return true;
			}

			return false;
		}

		IndexVertexBufferCollector GetAllGeometriesForNavigationMesh()
		{
			IndexVertexBufferCollector collector = new IndexVertexBufferCollector();

			foreach( Entity geometry in geometries )
			{
				if( !geometry.Editor_IsExcludedFromWorld() )
					AddEntityToCollector( collector, geometry );
			}

			return collector;
		}

		public void AddAllGeometriesOnMap()
		{
			foreach( Entity entity in Entities.Instance.EntitiesCollection )
			{
				if( IsSupportedGeometry( entity ) )
				{
					if( !Geometries.Contains( entity ) )
						AddGeometry( entity );
				}
			}
		}

		Bounds GetGeometryBounds( Entity entity )
		{
			HeightmapTerrain terrain = entity as HeightmapTerrain;
			if( terrain != null && terrain.Enabled )
				return terrain.CalculateBounds();

			MapObject mapObject = entity as MapObject;
			if( mapObject != null )
				return mapObject.MapBounds;

			return Engine.MathEx.Bounds.Cleared;
		}

		public void RecalculateBounds()
		{
			Bounds bounds = Bounds.Cleared;
			foreach( Entity entity in Geometries )
			{
				if( entity.Editor_IsExcludedFromWorld() )
					continue;
				bounds.Add( GetGeometryBounds( entity ) );
			}

			if( bounds.IsCleared() )
				bounds = new Bounds( -10, -10, -10, 10, 10, 10 );

			//needs to be whole number because decimals are rejected on the initialize toolbox
			Vec3 padding = new Vec3( 1, 1, 1 );

			BoundsMin = bounds.Minimum - padding;
			BoundsMax = bounds.Maximum + padding;
		}

		public bool FindPath( Vec3 start, Vec3 end, float stepSize, Vec3 polygonPickExtents, int maxPolygonPath,
			int maxSmoothPath, int maxSteerPoints, out Vec3[] outPath )
		{
			unsafe
			{
				outPath = null;

				if( recastWorld == IntPtr.Zero )
					return false;

				//convert to Recast space
				Vec3 recastStart = ToRecastVec3( start );
				Vec3 recastEnd = ToRecastVec3( end );
				Vec3 recastPolygonPickExtents =
					new Vec3( polygonPickExtents.X, polygonPickExtents.Z, polygonPickExtents.Y );

				Vec3* pathPointer;
				int pathCount;
				bool result = Wrapper.FindPath( recastWorld, ref recastStart, ref recastEnd, stepSize,
					ref recastPolygonPickExtents, maxPolygonPath, maxSmoothPath, maxSteerPoints,
					out pathPointer, out pathCount );

				if( result )
				{
					if( pathCount > 0 )
					{
						outPath = new Vec3[ pathCount ];
						for( int n = 0; n < pathCount; n++ )
							outPath[ n ] = ToEngineVec3( pathPointer[ n ] );
					}
					else
					{
						outPath = new Vec3[ 1 ];
						outPath[ 0 ] = start;
					}

					Wrapper.FreeMemory( (IntPtr)pathPointer );
				}

				return result;
			}
		}

		//public Bounds GetCellBounds( Vec3 pos )
		//{
		//   float gridSize = tileSize * cellSize;
		//   float minX = boundsMin.X + ( (int)Math.Floor( ( pos.X - boundsMin.X ) / gridSize ) * gridSize );
		//   float minY = boundsMax.Y + ( (int)Math.Floor( ( pos.Y - boundsMax.Y ) / gridSize ) * gridSize );
		//   xx;
		//   //SodanKerjuu: mirrored Y because of Recast coordinate system

		//   return new Bounds( minX, minY, boundsMin.Z, minX + gridSize, minY + gridSize, boundsMax.Z );
		//}

		////use this to manually set the geometry, used with BuildTileCached to speed things up
		//public void AssignGeometry( Bounds bounds )
		//{
		//   if( recastWorld == IntPtr.Zero )
		//      return;

		//   IndexVertexBufferCollector collector = GetGeometriesForNavigationMesh( bounds );

		//   if( collector.resultVertexCount > 0 )
		//   {
		//      Vec3[] vertices = collector.resultVertices;
		//      int[] indices = collector.resultIndices;
		//      int vertexCount = collector.resultVertexCount;
		//      int indexCount = collector.resultIndexCount;

		//      //convert to Recast space
		//      for( int n = 0; n < vertexCount; n++ )
		//         vertices[ n ] = ToRecastVec3( vertices[ n ] );

		//      unsafe
		//      {
		//         fixed( Vec3* pVertices = vertices )
		//         fixed( int* pIndices = indices )
		//            Wrapper.SetGeometry( recastWorld, (IntPtr)pVertices, vertexCount, (IntPtr)pIndices, indexCount );
		//      }
		//   }
		//}

		////this will use the previously assigned geometry instead of getting new one
		//public void BuildTileCached( Vec3 position )
		//{
		//   if( recastWorld == IntPtr.Zero )
		//      return;

		//   Vec3 targetPosition = ToRecastVec3( position );
		//   Wrapper.BuildTile( recastWorld, ref targetPosition );

		//   //redraw debug mesh
		//   debugNavigationMeshVertices = null;
		//   debugNavigationMeshIndices = null;
		//}

		//public void BuildTile( Vec3 position )
		//{
		//   if( recastWorld == IntPtr.Zero )
		//      return;

		//   Bounds selectionBounds = GetCellBounds( position );

		//   IndexVertexBufferCollector collector = GetGeometriesForNavigationMesh( selectionBounds );
		//   if( collector.resultVertexCount > 0 )
		//   {
		//      Vec3[] vertices = collector.resultVertices;
		//      int[] indices = collector.resultIndices;
		//      int vertexCount = collector.resultVertexCount;
		//      int indexCount = collector.resultIndexCount;

		//      //convert to Recast space
		//      for( int n = 0; n < vertexCount; n++ )
		//         vertices[ n ] = ToRecastVec3( vertices[ n ] );

		//      unsafe
		//      {
		//         Log.Info( "SetGeometry: Vertices: {0}, Indices {1}", vertexCount, indexCount );

		//         fixed( Vec3* pVertices = vertices )
		//         fixed( int* pIndices = indices )
		//         {
		//            Wrapper.SetGeometry( recastWorld, (IntPtr)pVertices, vertexCount, (IntPtr)pIndices, indexCount );
		//            Vec3 targetPosition = ToRecastVec3( position );
		//            Wrapper.BuildTile( recastWorld, ref targetPosition );

		//            //redraw debug mesh
		//            debugNavigationMeshVertices = null;
		//            debugNavigationMeshIndices = null;
		//         }
		//      }
		//   }
		//}

		//public void RemoveTile( Vec3 position )
		//{
		//   if( recastWorld == IntPtr.Zero )
		//      return;

		//   Vec3 targetPosition = ToRecastVec3( position );
		//   Wrapper.RemoveTile( recastWorld, ref targetPosition );

		//   debugNavigationMeshVertices = null;
		//   debugNavigationMeshIndices = null;
		//}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( Geometries.Contains( entity ) )
				RemoveGeometry( entity );
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( camera.Purpose == Camera.Purposes.MainCamera )
			{
				//if( drawTileGrid )
				//   DebugRenderTileGrid( camera );

				bool drawMapEditor = false;
				if( MapEditorInterface.Instance != null )
				{
					//bool allow
					try
					{
						bool v = (bool)MapEditorInterface.Instance.SendCustomMessage( this, "IsAllowToRenderNavigationMesh", null );
						if( v )
							drawMapEditor = true;
					}
					catch { }
				}

				if( alwaysDrawNavMesh || drawMapEditor )
					DebugDrawNavMesh( camera );

				//draw global bounds
				bool mapEditorIsSelected = MapEditorInterface.Instance != null &&
					MapEditorInterface.Instance.IsEntitySelected( this );
				if( mapEditorIsSelected )
				{
					camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
					Bounds bounds = new Bounds( boundsMin, boundsMax );
					camera.DebugGeometry.AddBounds( bounds );
				}



				//bool mapEditorIsSelected = false;
				//if( MapEditorInterface.Instance != null && MapEditorInterface.Instance.IsEntitySelected( this ) )
				//   mapEditorIsSelected = true;

				//if( alwaysDrawNavMesh || mapEditorIsSelected )
				//   DebugDrawNavMesh( camera );

				////draw global bounds
				//if( mapEditorIsSelected )
				//{
				//   camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );

				//   Bounds bounds = new Bounds( boundsMin, boundsMax );
				//   camera.DebugGeometry.AddBounds( bounds );
				//}
			}
		}

		public void DebugDrawNavMesh( Camera camera )
		{
			if( recastWorld == IntPtr.Zero )
				return;

			if( debugNavigationMeshVertices == null )
			{
				if( !GetDebugNavigationMeshGeometry( out debugNavigationMeshVertices ) )
					return;

				debugNavigationMeshIndices = new int[ debugNavigationMeshVertices.Length ];
				for( int n = 0; n < debugNavigationMeshIndices.Length; n++ )
					debugNavigationMeshIndices[ n ] = n;
			}

			//Render NavMesh
			{
				Mat4 transform = new Mat4( Mat3.Identity, new Vec3( 0, 0, .1f ) );

				//draw without depth test
				{
					camera.DebugGeometry.SetSpecialDepthSettings( false, false );

					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .1f );
					camera.DebugGeometry.AddVertexIndexBuffer( debugNavigationMeshVertices,
						debugNavigationMeshIndices, transform, false, true );

					camera.DebugGeometry.Color = new ColorValue( 1, 1, 0, .1f );
					camera.DebugGeometry.AddVertexIndexBuffer( debugNavigationMeshVertices,
						debugNavigationMeshIndices, transform, true, true );

					camera.DebugGeometry.RestoreDefaultDepthSettings();
				}

				//draw with depth test
				{
					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .3f );
					camera.DebugGeometry.AddVertexIndexBuffer( debugNavigationMeshVertices,
						debugNavigationMeshIndices, transform, false, true );
					camera.DebugGeometry.Color = new ColorValue( 1, 1, 0, .3f );
					camera.DebugGeometry.AddVertexIndexBuffer( debugNavigationMeshVertices,
						debugNavigationMeshIndices, transform, true, true );
				}
			}
		}

		//void DebugRenderTileGrid( Camera camera )
		//{
		//   //make a tile grid
		//   {
		//      if( tileGridMeshVertices == null )
		//         CreateTileGridMesh( out tileGridMeshVertices, out tileGridMeshIndices, false );

		//      camera.DebugGeometry.Color = new ColorValue( 0f, 1f, 0f, .3f );
		//      camera.DebugGeometry.AddVertexIndexBuffer( tileGridMeshVertices, tileGridMeshIndices,
		//         Mat4.Identity, true, false );
		//   }

		//   //make a cell grid
		//   {
		//      if( cellGridMeshVertices == null )
		//         CreateTileGridMesh( out cellGridMeshVertices, out cellGridMeshIndices, true );

		//      camera.DebugGeometry.Color = new ColorValue( 1f, 0f, 0f, .3f );
		//      camera.DebugGeometry.AddVertexIndexBuffer( cellGridMeshVertices, cellGridMeshIndices,
		//         Mat4.Identity, true, false );
		//   }

		//   //add the bounds
		//   {
		//      camera.DebugGeometry.Color = new ColorValue( 0f, 1f, 1f, .6f );
		//      camera.DebugGeometry.AddBounds( new Bounds( boundsMin, boundsMax ) );
		//   }
		//}

		//void CreateTileGridMesh( out Vec3[] vertices, out int[] indices, bool cellSplit )
		//{
		//   int size;
		//   if( cellSplit )
		//      size = 128;
		//   else
		//      size = 64;

		//   vertices = new Vec3[ ( size + 1 ) * ( size + 1 ) ];
		//   {
		//      int vertexPosition = 0;
		//      for( int y = 0; y < size + 1; y++ )
		//      {
		//         for( int x = 0; x < size + 1; x++ )
		//         {
		//            xx;
		//            //SodanKerjuu: mirrored Y because of Recast coordinate system
		//            if( cellSplit )
		//            {
		//               vertices[ vertexPosition ] = new Vec3(
		//                  boundsMin.X + x * tileSize * cellSize,
		//                  boundsMax.Y - y * tileSize * cellSize,
		//                  boundsMin.Z + gridHeight * ( boundsMax.Z - boundsMin.Z ) );
		//            }
		//            else
		//            {
		//               vertices[ vertexPosition ] = new Vec3(
		//                  boundsMin.X + x * tileSize,
		//                  boundsMax.Y - y * tileSize,
		//                  boundsMin.Z + gridHeight * ( boundsMax.Z - boundsMin.Z ) );
		//            }
		//            vertexPosition++;
		//         }
		//      }
		//   }

		//   indices = new int[ size * size * 6 ];
		//   {
		//      int indexPosition = 0;
		//      for( int y = 0; y < size; y++ )
		//      {
		//         for( int x = 0; x < size; x++ )
		//         {
		//            indices[ indexPosition + 0 ] = ( size + 1 ) * y + x;
		//            indices[ indexPosition + 1 ] = ( size + 1 ) * y + x + 1;
		//            indices[ indexPosition + 2 ] = ( size + 1 ) * ( y + 1 ) + x + 1;
		//            indices[ indexPosition + 3 ] = ( size + 1 ) * ( y + 1 ) + x + 1;
		//            indices[ indexPosition + 4 ] = ( size + 1 ) * ( y + 1 ) + x;
		//            indices[ indexPosition + 5 ] = ( size + 1 ) * y + x;
		//            indexPosition += 6;
		//         }
		//      }
		//   }
		//}

		public bool GetDebugNavigationMeshGeometry( out Vec3[] vertices )
		{
			vertices = null;

			if( recastWorld == IntPtr.Zero )
				return false;

			unsafe
			{
				Vec3* nativeVertices;
				int vertexCount;
				if( !Wrapper.GetNavigationMesh( recastWorld, out nativeVertices, out vertexCount ) )
					return false;

				vertices = new Vec3[ vertexCount ];
				for( int n = 0; n < vertices.Length; n++ )
					vertices[ n ] = ToEngineVec3( nativeVertices[ n ] );

				Wrapper.FreeMemory( (IntPtr)nativeVertices );
			}

			return true;
		}

		public void AddGeometry( Entity entity )
		{
			if( geometries.Contains( entity ) )
				Log.Fatal( "RecastNavigationSystem: AddGeometry: This entity is already added." );

			geometries.Add( entity );
			SubscribeToDeletionEvent( entity );
		}

		public void RemoveGeometry( Entity entity )
		{
			if( !geometries.Contains( entity ) )
				return;

			UnsubscribeToDeletionEvent( entity );
			geometries.Remove( entity );
		}

		public void RemoveAllGeometries()
		{
			while( geometries.Count != 0 )
				RemoveGeometry( geometries[ geometries.Count - 1 ] );
		}
	}
}
