// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.PhysicsSystem;

namespace Engine
{
	public class GridBasedNavigationSystemType : MapGeneralObjectType
	{
		public GridBasedNavigationSystemType()
		{
			AllowEmptyName = true;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[ExtendedFunctionalityDescriptor( "Engine.Editor.GridBasedNavigationSystemExtendedFunctionalityDescriptor, GridBasedNavigationSystem.Editor" )]
	public class GridBasedNavigationSystem : MapGeneralObject
	{
		static List<GridBasedNavigationSystem> instances = new List<GridBasedNavigationSystem>();
		static ReadOnlyCollection<GridBasedNavigationSystem> instancesReadOnly;

		[FieldSerialize( "gridBounds" )]
		Rect gridBounds = new Rect( -100, -100, 100, 100 );

		[FieldSerialize( "gridCellSize" )]
		float gridCellSize = 1.0f;

		[FieldSerialize( "agentHeight" )]
		float agentHeight = 2.0f;

		[FieldSerialize( "agentMaxSlope" )]
		Degree agentMaxSlope = 30;

		[FieldSerialize( "heightByTerrainOnly" )]
		bool heightByTerrainOnly = false;

		[FieldSerialize( "alwaysDrawGrid" )]
		bool alwaysDrawGrid;

		[FieldSerialize( "drawGridDistance" )]
		float drawGridDistance = 100;

		bool initialized;
		float gridCellSizeInv;

		//for method DoFindA()
		int onClosedList;
		int[] openList;//1 dimensional array holding ID# of open list items
		int[ , ] whichList;  //2 dimensional array used to record 
		int[] openX; //1d array stores the x location of an item on the open list
		int[] openY; //1d array stores the y location of an item on the open list
		int[ , ] parentX; //2d array to store parent of each cell (x)
		int[ , ] parentY; //2d array to store parent of each cell (y)
		int[] Fcost;	//1d array to store F cost of a cell on the open list
		int[ , ] Gcost; 	//2d array to store G cost for each cell.
		int[] Hcost;	//1d array to store H cost of a cell on the open list
		int pathLength;//stores length of the found path for critter

		//for FindPath()
		Vec2I[] pathArray;

		Vec2I mapSize;
		Vec2I mapMaxIndex;

		Vec2 mapMotionPosition;
		byte[ , ] mapMotion;
		Dictionary<MapObject, RectI[]> mapMotionRectangles = new Dictionary<MapObject, RectI[]>();

		//for render
		List<Vec3> renderVertices = new List<Vec3>();
		List<int> renderFreeIndices = new List<int>();
		List<int> renderBusyIndices = new List<int>();

		//Add/RemoveObjectToMotionMap (only for optimization)
		List<Rect> tempRectangles = new List<Rect>();

		List<TempClearMotionMapDataItem> tempClearMotionMapData = new List<TempClearMotionMapDataItem>();

		///////////////////////////////////////////

		public interface IOverrideObjectBehavior
		{
			void GetMotionMapRectanglesForObject( GridBasedNavigationSystem navigationSystem, MapObject obj, List<Rect> rectangles );
		}

		///////////////////////////////////////////

		class TempClearMotionMapDataItem
		{
			public RectI mapRectangle;
			public byte[ , ] saveMapData;
			//public byte[ , ] saveMapData = new byte[ 32, 32 ];
		}

		///////////////////////////////////////////

		[TypeField]
		GridBasedNavigationSystemType __type = null;
		/// <summary>
		/// Gets the entity type.
		/// </summary>
		public new GridBasedNavigationSystemType Type { get { return __type; } }

		public static IList<GridBasedNavigationSystem> Instances
		{
			get
			{
				if( instancesReadOnly == null )
					instancesReadOnly = new ReadOnlyCollection<GridBasedNavigationSystem>( instances );
				return instancesReadOnly;
			}
		}

		public GridBasedNavigationSystem()
		{
			instances.Add( this );
		}

		/// <summary>
		/// The bounds of the grid.
		/// </summary>
		[LocalizedDescription( "The bounds of the grid.", "GridBasedNavigationSystem" )]
		[DefaultValue( typeof( Rect ), "-100 -100 100 100" )]
		public Rect GridBounds
		{
			get { return gridBounds; }
			set { gridBounds = value; }
		}

		/// <summary>
		/// The size of the cell of the grid.
		/// </summary>
		[LocalizedDescription( "The size of the cell of the grid.", "GridBasedNavigationSystem" )]
		[DefaultValue( 1.0f )]
		public float GridCellSize
		{
			get { return gridCellSize; }
			set
			{
				if( gridCellSize == value )
					return;
				gridCellSize = value;

				ClearMotionMap();
			}
		}

		/// <summary>
		/// Maximum agent's height.
		/// </summary>
		[LocalizedDescription( "Maximum agent's height.", "GridBasedNavigationSystem" )]
		[DefaultValue( 2.0f )]
		public float AgentHeight
		{
			get { return agentHeight; }
			set { agentHeight = value; }
		}

		/// <summary>
		/// The maximum angle which the agent can reach.
		/// </summary>
		[LocalizedDescription( "The maximum angle which the agent can reach.", "GridBasedNavigationSystem" )]
		[DefaultValue( typeof( Degree ), "30" )]
		public Degree AgentMaxSlope
		{
			get { return agentMaxSlope; }
			set { agentMaxSlope = value; }
		}

		public bool IsInitialized()
		{
			return initialized;
		}

		void InitializeMotionMap()
		{
			initialized = true;
			if( gridCellSize != 0 )
				gridCellSizeInv = 1.0f / gridCellSize;

			Rect bounds = gridBounds;
			if( bounds == Rect.Zero )
				bounds = new Rect( Vec2.Zero, new Vec2( 10, 10 ) );

			Vec2 v = bounds.GetSize() * gridCellSizeInv;
			mapSize = new Vec2I( (int)v.X, (int)v.Y );
			mapMotionPosition = new Vec2( bounds.Minimum.X, bounds.Minimum.Y );

			mapMaxIndex = mapSize - new Vec2I( mapSize.X > 0 ? 1 : 0, mapSize.Y > 0 ? 1 : 0 );

			openList = new int[ mapSize.X * mapSize.Y + 2 ];
			whichList = new int[ mapSize.X, mapSize.Y ];
			openX = new int[ mapSize.X * mapSize.Y + 2 ];
			openY = new int[ mapSize.X * mapSize.Y + 2 ];
			parentX = new int[ mapSize.X, mapSize.Y ];
			parentY = new int[ mapSize.X, mapSize.Y ];
			Fcost = new int[ mapSize.X * mapSize.Y + 2 ];
			Gcost = new int[ mapSize.X, mapSize.Y ];
			Hcost = new int[ mapSize.X * mapSize.Y + 2 ];

			pathArray = new Vec2I[ mapSize.X * mapSize.Y ];

			renderVertices.Clear();
			renderFreeIndices.Clear();
			renderBusyIndices.Clear();

			mapMotionRectangles.Clear();

			mapMotion = new byte[ mapSize.X, mapSize.Y ];
			FillMotionMap();
		}

		public void ClearMotionMap()
		{
			initialized = false;
			gridCellSizeInv = 0;

			onClosedList = 0;
			openList = null;
			whichList = null;
			openX = null;
			openY = null;
			parentX = null;
			parentY = null;
			Fcost = null;
			Gcost = null;
			Hcost = null;
			pathLength = 0;

			pathArray = null;

			mapSize = Vec2I.Zero;
			mapMaxIndex = Vec2I.Zero;

			mapMotionPosition = Vec2.Zero;
			mapMotion = null;
			mapMotionRectangles = new Dictionary<MapObject, RectI[]>();

			renderVertices.Clear();
			renderFreeIndices.Clear();
			renderBusyIndices.Clear();
		}

		public void UpdateMotionMap()
		{
			InitializeMotionMap();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			if( !instances.Contains( this ) )
				instances.Add( this );

			base.OnPostCreate( loaded );

			Map.Instance.AddObjectToNodesEvent += Map_AddObjectToNodesEvent;
			Map.Instance.RemoveObjectFromNodesEvent += Map_RemoveObjectFromNodesEvent;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			if( !initialized || EntitySystemWorld.Instance.IsEditor() )
				InitializeMotionMap();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnDestroy()
		{
			Map.Instance.AddObjectToNodesEvent -= Map_AddObjectToNodesEvent;
			Map.Instance.RemoveObjectFromNodesEvent -= Map_RemoveObjectFromNodesEvent;

			base.OnDestroy();

			instances.Remove( this );
		}

		bool IsFreeInMapMotion( Vec2I pos )
		{
			return mapMotion[ pos.X, pos.Y ] == 0;
		}

		bool IsFreeInMapMotion( Vec2I pos, int unitSize )
		{
			if( unitSize != 1 )
			{
				if( pos.X + unitSize - 1 >= mapSize.X )
					return false;
				if( pos.Y + unitSize - 1 >= mapSize.Y )
					return false;

				for( int y = 0; y < unitSize; y++ )
					for( int x = 0; x < unitSize; x++ )
						if( !IsFreeInMapMotion( pos + new Vec2I( x, y ) ) )
							return false;
				return true;
			}
			else
				return IsFreeInMapMotion( pos );
		}

		public bool IsFreeInMapMotion( Rect rect )
		{
			if( initialized )
			{
				RectI mapMotionRect = GetMapMotionRectangle( rect );
				for( int y = mapMotionRect.Top; y <= mapMotionRect.Bottom; y++ )
					for( int x = mapMotionRect.Left; x <= mapMotionRect.Right; x++ )
						if( !IsFreeInMapMotion( new Vec2I( x, y ) ) )
							return false;
			}
			return true;
		}

		protected virtual bool OnInitializeMotionMapCheckForFreeVolume( Bounds bounds )
		{
			return PhysicsWorld.Instance.VolumeCast( bounds, (int)ContactGroup.CastOnlyCollision ).Length == 0;
		}

		void FillMotionMap()
		{
			LongOperationCallbackManager.CallCallback( "GridBasedNavigationSystem: FillMotionMap" );

			//Fill collision
			{
				float[ , ] heightMap = new float[ mapSize.X + 1, mapSize.Y + 1 ];

				for( int y = 0; y <= mapSize.Y; y++ )
				{
					LongOperationCallbackManager.CallCallback( "GridBasedNavigationSystem: FillMotionMap: Fill collision" );

					for( int x = 0; x <= mapSize.X; x++ )
					{
						Vec2 p2 = mapMotionPosition + new Vec2( x, y ) * GridCellSize;
						heightMap[ x, y ] = GetMotionMapHeight( p2 );
					}
				}

				float downStep = GridCellSize / 8;

				for( int y = 0; y < mapSize.Y; y++ )
				{
					LongOperationCallbackManager.CallCallback( "GridBasedNavigationSystem: FillMotionMap: Fill collision" );

					for( int x = 0; x < mapSize.X; x++ )
					{
						float maxHeight = heightMap[ x, y ];
						float minHeight = heightMap[ x, y ];

						maxHeight = Math.Max( maxHeight, heightMap[ x + 1, y ] );
						minHeight = Math.Min( minHeight, heightMap[ x + 1, y ] );
						maxHeight = Math.Max( maxHeight, heightMap[ x, y + 1 ] );
						minHeight = Math.Min( minHeight, heightMap[ x, y + 1 ] );
						maxHeight = Math.Max( maxHeight, heightMap[ x + 1, y + 1 ] );
						minHeight = Math.Min( minHeight, heightMap[ x + 1, y + 1 ] );

						//slope check
						{
							float overfall = maxHeight - minHeight;
							Degree angle = new Radian( MathFunctions.ATan( overfall, GridCellSize * 1.41f ) ).InDegrees();
							if( angle > AgentMaxSlope )
							{
								mapMotion[ x, y ]++;
								continue;
							}
						}

						Vec2 p2 = mapMotionPosition + new Vec2( x, y ) * GridCellSize;

						Bounds volumeBounds = new Bounds(
							new Vec3( p2.X + .0001f, p2.Y + .0001f, maxHeight + downStep ),
							new Vec3( p2.X + GridCellSize - .0001f, p2.Y + GridCellSize - .0001f,
							maxHeight + AgentHeight ) );

						if( !OnInitializeMotionMapCheckForFreeVolume( volumeBounds ) )
							mapMotion[ x, y ]++;
					}
				}
			}

			LongOperationCallbackManager.CallCallback( "GridBasedNavigationSystem: FillMotionMap: Add objects" );

			//Fill map objects
			foreach( Entity entity in Map.Instance.Children )
			{
				MapObject mapObject = entity as MapObject;
				if( mapObject != null )
					Map_AddObjectToNodesEvent( mapObject );
			}
		}

		RectI GetMapMotionRectangle( Rect rect )
		{
			Vec2 minf = ( rect.Minimum - mapMotionPosition ) * gridCellSizeInv;
			Vec2 maxf = ( rect.Maximum - mapMotionPosition ) * gridCellSizeInv;
			Vec2I min = new Vec2I( (int)minf.X, (int)minf.Y );
			Vec2I max = new Vec2I( (int)maxf.X, (int)maxf.Y );
			min.Clamp( new Vec2I( 0, 0 ), mapMaxIndex );
			max.Clamp( new Vec2I( 0, 0 ), mapMaxIndex );
			return new RectI( min, max );
		}

		Vec2I GetMapMotionPosition( Vec2 pos )
		{
			Vec2 pf = ( pos - mapMotionPosition ) * gridCellSizeInv;
			Vec2I p = new Vec2I( (int)pf.X, (int)pf.Y );
			p.Clamp( new Vec2I( 0, 0 ), mapMaxIndex );
			return p;
		}

		public void AddTempClearMotionMap( Rect rectangle )
		{
			if( !initialized )
				return;

			RectI mapRectangle = GetMapMotionRectangle( rectangle );

			TempClearMotionMapDataItem item = new TempClearMotionMapDataItem();
			item.mapRectangle = mapRectangle;
			item.saveMapData = new byte[ mapRectangle.Size.X + 1, mapRectangle.Size.Y + 1 ];

			byte[ , ] data = item.saveMapData;
			for( int y = mapRectangle.Top; y <= mapRectangle.Bottom; y++ )
			{
				for( int x = mapRectangle.Left; x <= mapRectangle.Right; x++ )
				{
					data[ x - mapRectangle.Left, y - mapRectangle.Top ] = mapMotion[ x, y ];
					mapMotion[ x, y ] = 0;
				}
			}

			tempClearMotionMapData.Add( item );
		}

		public void DeleteAllTempClearedMotionMap()
		{
			if( !initialized )
				return;

			for( int nItem = 0; nItem < tempClearMotionMapData.Count; nItem++ )
			{
				TempClearMotionMapDataItem item = tempClearMotionMapData[ nItem ];
				RectI mapRectangle = item.mapRectangle;
				byte[ , ] data = item.saveMapData;

				for( int y = mapRectangle.Top; y <= mapRectangle.Bottom; y++ )
					for( int x = mapRectangle.Left; x <= mapRectangle.Right; x++ )
						mapMotion[ x, y ] = data[ x - mapRectangle.Left, y - mapRectangle.Top ];
			}
			tempClearMotionMapData.Clear();
		}

		public void GetMotionMapRectanglesForObjectDefaultImplementation( MapObject obj, List<Rect> rectangles )
		{
			//TO DO: better to check not by the bounds of shapes. better to make motion map grid for the object.

			if( obj.PhysicsModel != null )
			{
				foreach( Body body in obj.PhysicsModel.Bodies )
				{
					foreach( Shape shape in body.Shapes )
					{
						if( shape.ContactGroup != (int)ContactGroup.NoContact )
						{
							Bounds bounds = shape.GetGlobalBounds();
							rectangles.Add( new Rect( bounds.Minimum.ToVec2(), bounds.Maximum.ToVec2() ) );
						}
					}
				}
			}
		}

		protected virtual void OnGetMotionMapRectanglesForObject( MapObject obj, List<Rect> rectangles )
		{
			IOverrideObjectBehavior overrideObjectBehavior = obj as IOverrideObjectBehavior;
			if( overrideObjectBehavior != null )
				overrideObjectBehavior.GetMotionMapRectanglesForObject( this, obj, rectangles );
			else
				GetMotionMapRectanglesForObjectDefaultImplementation( obj, rectangles );
		}

		public void AddObjectToMotionMap( MapObject obj )
		{
			if( !initialized )
				return;

			OnGetMotionMapRectanglesForObject( obj, tempRectangles );

			if( tempRectangles.Count != 0 )
			{
				RectI[] rectangles = new RectI[ tempRectangles.Count ];

				for( int nRectangle = 0; nRectangle < tempRectangles.Count; nRectangle++ )
				{
					Rect rectangle = tempRectangles[ nRectangle ];
					RectI mapMotionRectangle = GetMapMotionRectangle( rectangle );

					for( int y = mapMotionRectangle.Top; y <= mapMotionRectangle.Bottom; y++ )
						for( int x = mapMotionRectangle.Left; x <= mapMotionRectangle.Right; x++ )
							mapMotion[ x, y ]++;

					rectangles[ nRectangle ] = mapMotionRectangle;
				}

				mapMotionRectangles.Add( obj, rectangles );

				tempRectangles.Clear();
			}
		}

		public void RemoveObjectFromMotionMap( MapObject obj )
		{
			if( !initialized )
				return;

			RectI[] rectangles;
			if( mapMotionRectangles.TryGetValue( obj, out rectangles ) )
			{
				mapMotionRectangles.Remove( obj );

				foreach( RectI mapMotionRectangle in rectangles )
				{
					for( int y = mapMotionRectangle.Top; y <= mapMotionRectangle.Bottom; y++ )
						for( int x = mapMotionRectangle.Left; x <= mapMotionRectangle.Right; x++ )
							mapMotion[ x, y ]--;
				}
			}
		}

		void Map_AddObjectToNodesEvent( MapObject obj )
		{
			if( !initialized )
				return;
			AddObjectToMotionMap( obj );
		}

		void Map_RemoveObjectFromNodesEvent( MapObject obj )
		{
			if( !initialized )
				return;
			RemoveObjectFromMotionMap( obj );
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( camera.Purpose == Camera.Purposes.MainCamera )
			{
				bool drawMapEditor = false;
				if( MapEditorInterface.Instance != null && MapEditorInterface.Instance.IsEntitySelected( this ) )
					drawMapEditor = true;
				if( alwaysDrawGrid || drawMapEditor )
					DrawGrid( camera );
			}
		}

		public void DrawGrid( Camera camera )
		{
			//Update renderVertices buffer
			if( renderVertices.Count == 0 )
			{
				renderVertices.Capacity = ( mapSize.X + 1 ) * ( mapSize.Y + 1 );

				if( renderFreeIndices.Capacity < renderVertices.Capacity * 6 )
					renderFreeIndices.Capacity = renderVertices.Capacity * 6;
				if( renderBusyIndices.Capacity < renderVertices.Capacity * 6 )
					renderBusyIndices.Capacity = renderVertices.Capacity * 6;

				for( int y = 0; y < mapSize.Y + 1; y++ )
				{
					for( int x = 0; x < mapSize.X + 1; x++ )
					{
						Vec2 p = mapMotionPosition + new Vec2( x, y ) * GridCellSize;
						renderVertices.Add( new Vec3( p.X, p.Y, GetMotionMapHeight( p ) ) );
					}
				}
			}

			renderFreeIndices.Clear();
			renderBusyIndices.Clear();

			{
				const int tileSize = 10;

				//set color for lines
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0, .7f );

				//tiles loop
				for( int tileY = 0; tileY < mapSize.Y; tileY += tileSize )
				{
					for( int tileX = 0; tileX < mapSize.X; tileX += tileSize )
					{
						//get tile bounds
						Rect bounds2 = mapMotionPosition + new Rect(
							new Vec2( tileX * GridCellSize, tileY * GridCellSize ),
							new Vec2( ( tileX + tileSize ) * GridCellSize, ( tileY + tileSize ) * GridCellSize ) );

						Bounds worldBounds = Map.Instance.SceneGraph.GetOctreeBoundsWithBoundsOfObjectsOutsideOctree();
						Bounds bounds = new Bounds(
							bounds2.Minimum.X, bounds2.Minimum.Y, worldBounds.Minimum.Z,
							bounds2.Maximum.X, bounds2.Maximum.Y, worldBounds.Maximum.Z );

						//check tile visibility
						if( !camera.IsIntersectsFast( bounds ) )
							continue;

						//check by distance
						{
							float distance = bounds.GetPointDistance( camera.Position );
							if( distance > drawGridDistance )
								continue;
						}

						//loop in tile
						for( int y = tileY; y < tileY + tileSize && y < mapSize.Y; y++ )
						{
							for( int x = tileX; x < tileX + tileSize && x < mapSize.X; x++ )
							{
								int p0 = ( mapSize.X + 1 ) * y + x;
								int p1 = p0 + 1;
								int p2 = p0 + mapSize.X + 1;
								int p3 = p2 + 1;

								//draw lines
								Vec3 offset = new Vec3( 0, 0, .4f );

								//camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
								camera.DebugGeometry.AddLine( renderVertices[ p0 ] + offset, renderVertices[ p1 ] + offset );
								camera.DebugGeometry.AddLine( renderVertices[ p0 ] + offset, renderVertices[ p2 ] + offset );

								//add grid buffers
								List<int> list = IsFreeInMapMotion( new Vec2I( x, y ) ) ? renderFreeIndices : renderBusyIndices;
								list.Add( p0 );
								list.Add( p1 );
								list.Add( p2 );

								list.Add( p2 );
								list.Add( p1 );
								list.Add( p3 );
							}
						}
					}
				}
			}

			//draw grids
			camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .3f );
			camera.DebugGeometry.AddVertexIndexBuffer( renderVertices, renderFreeIndices,
				Mat4.FromTranslate( new Vec3( 0, 0, .2f ) ), false, true );

			camera.DebugGeometry.Color = new ColorValue( 1, 0, 0, .3f );
			camera.DebugGeometry.AddVertexIndexBuffer( renderVertices, renderBusyIndices,
				Mat4.FromTranslate( new Vec3( 0, 0, .2f ) ), false, true );
		}

		/// <summary>
		/// A value indicating whether a grid drawing enabled.
		/// </summary>
		[LocalizedDescription( "A value indicating whether a grid drawing enabled.", "GridBasedNavigationSystem" )]
		[DefaultValue( false )]
		public bool AlwaysDrawGrid
		{
			get { return alwaysDrawGrid; }
			set { alwaysDrawGrid = value; }
		}

		/// <summary>
		/// A value indicates calculation of the height only for terrain (HeightmapTerrain).
		/// </summary>
		[LocalizedDescription( "A value indicates calculation of the height only for terrain (HeightmapTerrain).", "GridBasedNavigationSystem" )]
		[DefaultValue( false )]
		public bool HeightByTerrainOnly
		{
			get { return heightByTerrainOnly; }
			set { heightByTerrainOnly = value; }
		}

		/// <summary>
		/// The distance of drawing the grid.
		/// </summary>
		[LocalizedDescription( "The distance of drawing the grid.", "GridBasedNavigationSystem" )]
		[DefaultValue( 100.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 10, 1000 )]
		public float DrawGridDistance
		{
			get { return drawGridDistance; }
			set { drawGridDistance = value; }
		}

		int DoFindA( Vec2I start, Vec2I end, int unitSize, int maxFieldsDistance,
			int maxFieldsToCheck, bool visualizeForDebugging )
		{
			const int found = 1, nonexistent = 2;
			const int notStarted = 0;// path-related constants
			const int walkable = 0;// walkability array constants

			int onOpenList = 0, parentXval = 0, parentYval = 0,
			a = 0, b = 0, m = 0, u = 0, v = 0, temp = 0, corner = 0, numberOfOpenListItems = 0,
			addedGCost = 0, tempGcost = 0, path = 0,
			tempx, pathX, pathY, //cellPosition,
			newOpenListItemID = 0;

			//1. Convert location data (in pixels) to coordinates in the walkability array.
			int startX = start.X;
			int startY = start.Y;
			int targetX = end.X;
			int targetY = end.Y;

			//2.Quick Path Checks: Under the some circumstances no path needs to
			//	be generated ...

			//	If starting location and target are in the same location...
			if( start == end )
				return 0;

			//	if (startX == targetX && startY == targetY && pathLocation[pathfinderID] > 0)
			//		return found;
			//	if (startX == targetX && startY == targetY && pathLocation[pathfinderID] == 0)
			//		return nonexistent;

			//	If target square is unwalkable, return that it's a nonexistent path.

			if( !IsFreeInMapMotion( new Vec2I( targetX, targetY ), unitSize ) )
				return 0;

			//3.Reset some variables that need to be cleared
			if( onClosedList > 1000000 ) //reset whichList occasionally
			{
				for( int y = 0; y < mapSize.Y; y++ )
					for( int x = 0; x < mapSize.X; x++ )
						whichList[ x, y ] = 0;
				//memset( whichList, 0, sizeof( whichList ) );
				onClosedList = 10;
			}
			onClosedList = onClosedList + 2; //changing the values of onOpenList and onClosed list is faster than redimming whichList() array
			onOpenList = onClosedList - 1;
			pathLength = notStarted;//i.e, = 0
			//pathLocation = notStarted;//i.e, = 0
			Gcost[ startX, startY ] = 0; //reset starting square's G value to 0

			//4.Add the starting location to the open list of squares to be checked.
			numberOfOpenListItems = 1;
			openList[ 1 ] = 1;//assign it as the top (and currently only) item in the open list, which is maintained as a binary heap (explained below)
			openX[ 1 ] = startX; openY[ 1 ] = startY;

			int fieldsCheckedCount = 0;

			//5.Do the following until a path is found or deemed nonexistent.
			do
			{
				//6.If the open list is not empty, take the first cell off of the list.
				//	This is the lowest F cost cell on the open list.
				if( numberOfOpenListItems != 0 )
				{

					//7. Pop the first item off the open list.
					parentXval = openX[ openList[ 1 ] ];
					parentYval = openY[ openList[ 1 ] ]; //record cell coordinates of the item
					whichList[ parentXval, parentYval ] = onClosedList;//add the item to the closed list

					//	Open List = Binary Heap: Delete this item from the open list, which
					//  is maintained as a binary heap. For more information on binary heaps, see:
					//	http://www.policyalmanac.org/games/binaryHeaps.htm
					numberOfOpenListItems = numberOfOpenListItems - 1;//reduce number of open list items by 1	

					//	Delete the top item in binary heap and reorder the heap, with the lowest F cost item rising to the top.
					openList[ 1 ] = openList[ numberOfOpenListItems + 1 ];//move the last item in the heap up to slot #1
					v = 1;

					//	Repeat the following until the new item in slot #1 sinks to its proper spot in the heap.
					do
					{
						u = v;
						if( 2 * u + 1 <= numberOfOpenListItems ) //if both children exist
						{
							//Check if the F cost of the parent is greater than each child.
							//Select the lowest of the two children.
							if( Fcost[ openList[ u ] ] >= Fcost[ openList[ 2 * u ] ] )
								v = 2 * u;
							if( Fcost[ openList[ v ] ] >= Fcost[ openList[ 2 * u + 1 ] ] )
								v = 2 * u + 1;
						}
						else
						{
							if( 2 * u <= numberOfOpenListItems ) //if only child #1 exists
							{
								//Check if the F cost of the parent is greater than child #1	
								if( Fcost[ openList[ u ] ] >= Fcost[ openList[ 2 * u ] ] )
									v = 2 * u;
							}
						}

						if( u != v ) //if parent's F is > one of its children, swap them
						{
							temp = openList[ u ];
							openList[ u ] = openList[ v ];
							openList[ v ] = temp;
						}
						else
							break; //otherwise, exit loop

					}
					while( true );


					//7.Check the adjacent squares. (Its "children" -- these path children
					//	are similar, conceptually, to the binary heap children mentioned
					//	above, but don't confuse them. They are different. Path children
					//	are portrayed in Demo 1 with grey pointers pointing toward
					//	their parents.) Add these adjacent child squares to the open list
					//	for later consideration if appropriate (see various if statements
					//	below).
					for( b = parentYval - 1; b <= parentYval + 1; b++ )
					{
						for( a = parentXval - 1; a <= parentXval + 1; a++ )
						{

							//	If not off the map (do this first to avoid array out-of-bounds errors)
							if( a != -1 && b != -1 && a != mapSize.X && b != mapSize.Y )
							{

								//	If not already on the closed list (items on the closed list have
								//	already been considered and can now be ignored).			
								if( whichList[ a, b ] != onClosedList )
								{

									//	If not a wall/obstacle square.
									if( Math.Abs( a - start.X ) <= maxFieldsDistance &&
										Math.Abs( b - start.Y ) <= maxFieldsDistance )
									{
										if( IsFreeInMapMotion( new Vec2I( a, b ), unitSize ) )
										{

											//	Don't cut across corners
											corner = walkable;
											if( a == parentXval - 1 )
											{
												if( b == parentYval - 1 )
												{
													if( !IsFreeInMapMotion( new Vec2I( parentXval - 1, parentYval ), unitSize )
														|| !IsFreeInMapMotion( new Vec2I( parentXval, parentYval - 1 ), unitSize ) )
														corner = 1;
												}
												else if( b == parentYval + 1 )
												{
													if( !IsFreeInMapMotion( new Vec2I( parentXval, parentYval + 1 ), unitSize )
														|| !IsFreeInMapMotion( new Vec2I( parentXval - 1, parentYval ), unitSize ) )
														corner = 1;
												}
											}
											else if( a == parentXval + 1 )
											{
												if( b == parentYval - 1 )
												{
													if( !IsFreeInMapMotion( new Vec2I( parentXval, parentYval - 1 ), unitSize )
														|| !IsFreeInMapMotion( new Vec2I( parentXval + 1, parentYval ), unitSize ) )
														corner = 1;
												}
												else if( b == parentYval + 1 )
												{
													if( !IsFreeInMapMotion( new Vec2I( parentXval + 1, parentYval ), unitSize )
														|| !IsFreeInMapMotion( new Vec2I( parentXval, parentYval + 1 ), unitSize ) )
														corner = 1;
												}
											}
											if( corner == walkable )
											{
												//	If not already on the open list, add it to the open list.			
												if( whichList[ a, b ] != onOpenList )
												{
													//check for max fields
													fieldsCheckedCount++;
													if( fieldsCheckedCount == maxFieldsToCheck )
													{
														goto notFound;
													}

													//visualize
													if( visualizeForDebugging )
													{
														Vec2 startPoint2 = mapMotionPosition +
															new Vec2( (float)a + .5f, (float)b + .5f ) * GridCellSize;
														Vec3 startPoint = new Vec3( startPoint2.X, startPoint2.Y,
															GetMotionMapHeight( startPoint2 ) );

														Vec2 endPoint2 = mapMotionPosition +
															new Vec2( (float)parentXval + .5f, (float)parentYval + .5f ) * GridCellSize;
														Vec3 endPoint = new Vec3( endPoint2.X, endPoint2.Y, GetMotionMapHeight( endPoint2 ) );

														Camera camera = RendererWorld.Instance.DefaultCamera;
														camera.DebugGeometry.Color = new ColorValue( 1, 1, 1, .5f );
														camera.DebugGeometry.AddArrow(
															startPoint + new Vec3( 0, 0, .1f ),
															endPoint + new Vec3( 0, 0, .1f ) );
													}

													//Create a new open list item in the binary heap.
													newOpenListItemID = newOpenListItemID + 1; //each new item has a unique ID #
													m = numberOfOpenListItems + 1;
													openList[ m ] = newOpenListItemID;//place the new open list item (actually, its ID#) at the bottom of the heap
													openX[ newOpenListItemID ] = a;
													openY[ newOpenListItemID ] = b;//record the x and y coordinates of the new item

													//Figure out its G cost
													if( Math.Abs( a - parentXval ) == 1 && Math.Abs( b - parentYval ) == 1 )
														addedGCost = 14;//cost of going to diagonal squares	
													else
														addedGCost = 10;//cost of going to non-diagonal squares				
													Gcost[ a, b ] = Gcost[ parentXval, parentYval ] + addedGCost;

													//Figure out its H and F costs and parent
													Hcost[ openList[ m ] ] = 10 * ( Math.Abs( a - targetX ) + Math.Abs( b - targetY ) );
													Fcost[ openList[ m ] ] = Gcost[ a, b ] + Hcost[ openList[ m ] ];
													parentX[ a, b ] = parentXval; parentY[ a, b ] = parentYval;

													//Move the new open list item to the proper place in the binary heap.
													//Starting at the bottom, successively compare to parent items,
													//swapping as needed until the item finds its place in the heap
													//or bubbles all the way to the top (if it has the lowest F cost).
													while( m != 1 ) //While item hasn't bubbled to the top (m=1)	
													{
														//Check if child's F cost is < parent's F cost. If so, swap them.	
														if( Fcost[ openList[ m ] ] <= Fcost[ openList[ m / 2 ] ] )
														{
															temp = openList[ m / 2 ];
															openList[ m / 2 ] = openList[ m ];
															openList[ m ] = temp;
															m = m / 2;
														}
														else
															break;
													}
													numberOfOpenListItems = numberOfOpenListItems + 1;//add one to the number of items in the heap

													//Change whichList to show that the new item is on the open list.
													whichList[ a, b ] = onOpenList;
												}

												//8.If adjacent cell is already on the open list, check to see if this 
												//	path to that cell from the starting location is a better one. 
												//	If so, change the parent of the cell and its G and F costs.	
												else //If whichList(a,b) = onOpenList
												{
													//Figure out the G cost of this possible new path
													if( Math.Abs( a - parentXval ) == 1 && Math.Abs( b - parentYval ) == 1 )
														addedGCost = 14;//cost of going to diagonal tiles	
													else
														addedGCost = 10;//cost of going to non-diagonal tiles				
													tempGcost = Gcost[ parentXval, parentYval ] + addedGCost;

													//If this path is shorter (G cost is lower) then change
													//the parent cell, G cost and F cost. 		
													if( tempGcost < Gcost[ a, b ] ) //if G cost is less,
													{
														parentX[ a, b ] = parentXval; //change the square's parent
														parentY[ a, b ] = parentYval;
														Gcost[ a, b ] = tempGcost;//change the G cost			

														//Because changing the G cost also changes the F cost, if
														//the item is on the open list we need to change the item's
														//recorded F cost and its position on the open list to make
														//sure that we maintain a properly ordered open list.
														for( int x = 1; x <= numberOfOpenListItems; x++ ) //look for the item in the heap
														{
															if( openX[ openList[ x ] ] == a && openY[ openList[ x ] ] == b ) //item found
															{
																Fcost[ openList[ x ] ] = Gcost[ a, b ] + Hcost[ openList[ x ] ];//change the F cost

																//See if changing the F score bubbles the item up from it's current location in the heap
																m = x;
																while( m != 1 ) //While item hasn't bubbled to the top (m=1)	
																{
																	//Check if child is < parent. If so, swap them.	
																	if( Fcost[ openList[ m ] ] < Fcost[ openList[ m / 2 ] ] )
																	{
																		temp = openList[ m / 2 ];
																		openList[ m / 2 ] = openList[ m ];
																		openList[ m ] = temp;
																		m = m / 2;
																	}
																	else
																		break;
																}
																break; //exit for x = loop
															} //If openX(openList(x)) = a
														} //For x = 1 To numberOfOpenListItems
													}//If tempGcost < Gcost(a,b)

												}//else If whichList(a,b) = onOpenList	
											}//If not cutting a corner
										}//If not a wall/obstacle square.
									}
								}//If not already on the closed list 
							}//If not off the map
						}//for (a = parentXval-1; a <= parentXval+1; a++){
					}//for (b = parentYval-1; b <= parentYval+1; b++){

				}//if (numberOfOpenListItems != 0)

				//9.If open list is empty then there is no path.	
				else
				{
					path = nonexistent;
					break;
				}

				//If target is added to open list then path has been found.
				if( whichList[ targetX, targetY ] == onOpenList )
				{
					path = found;
					break;
				}

			}
			while( true );//Do until path is found or deemed nonexistent

			//10.Save the path if it exists.
			if( path == found )
			{
				//a.Working backwards from the target to the starting location by checking
				//	each cell's parent, figure out the length of the path.
				pathX = targetX; pathY = targetY;
				do
				{
					//Look up the parent of the current cell.	
					tempx = parentX[ pathX, pathY ];
					pathY = parentY[ pathX, pathY ];
					pathX = tempx;

					//Figure out the path length
					pathLength = pathLength + 1;
				}
				while( pathX != startX || pathY != startY );

				//construct patharray
				{
					int pos = pathLength - 1;
					pathX = targetX; pathY = targetY;
					do
					{
						/*patharray[pos]*/
						pathArray[ pos + 1 ] = new Vec2I( pathX, pathY );
						pos--;
						tempx = parentX[ pathX, pathY ];
						pathY = parentY[ pathX, pathY ];
						pathX = tempx;
					}
					while( pathX != startX || pathY != startY );
				}
				return pathLength;
			}

			notFound:

			return 0;
		}

		int RemoveFictitiousPoints( Vec2I start, int pathSize )
		{
			int size = 0;
			Vec2I ptold = start;
			Vec2I dirLine = pathArray/*path*/[ 0 ] - start;//текущее устойчивое направление
			for( int inpos = 0; inpos < pathSize; inpos++ )
			{
				Vec2I pt = pathArray[ inpos + 1 ];//Vec2I pt = path[ inpos ];
				Vec2I dir = pt - ptold;
				if( dir != dirLine )
				{
					pathArray[ size++ + 1 ] = ptold;//path[ size++ ] = ptold;
					dirLine = dir;
				}
				ptold = pt;
			}
			pathArray[ size++ + 1 ] = pathArray[ pathSize + 1 - 1 ];//path[ size++  ] = path[ pathSize  - 1 ];
			return size;
		}

		Vec2[] tempLineOffsets = new Vec2[ 3 ];
		bool LineIntersect( Vec2 start, Vec2 end, float unitSize )
		{
			float smoothFindStep = .1f;//*Type.SmoothFindStep*/ * gridCellSizeInv;
			Vec2 dirVec = end - start;
			float length = dirVec.Normalize();
			Vec2 posStep = dirVec * smoothFindStep;

			//calculate lines
			int linesCount;
			{
				float unitSizeHalf = unitSize * .5f;

				if( unitSize <= 1.0f )
				{
					linesCount = 2;
					tempLineOffsets[ 0 ] = new Vec2( -dirVec.Y, dirVec.X ) * ( unitSizeHalf * 1.1f );
					tempLineOffsets[ 1 ] = new Vec2( -dirVec.Y, dirVec.X ) * ( -unitSizeHalf * 1.1f );
				}
				else if( unitSize <= 2.0f )
				{
					linesCount = 3;
					tempLineOffsets[ 0 ] = new Vec2( -dirVec.Y, dirVec.X ) * ( unitSizeHalf * 1.1f );
					tempLineOffsets[ 1 ] = new Vec2( -dirVec.Y, dirVec.X ) * ( -unitSizeHalf * 1.1f );
					tempLineOffsets[ 2 ] = new Vec2( 0, 0 );
				}
				else
				{
					Log.Fatal( "GridBasedNavigationSystem: LineIntersect: not implemented: internal unit size > 2." );
					return true;
				}
			}

			float distance = 0;
			Vec2 pos = start;
			while( distance < length )
			{
				for( int nLine = 0; nLine < linesCount; nLine++ )
				{
					Vec2I index = new Vec2I( (int)( pos.X + tempLineOffsets[ nLine ].X ),
						(int)( pos.Y + tempLineOffsets[ nLine ].Y ) );

					if( index.X < 0 || index.Y < 0 || index.X >= mapSize.X || index.Y >= mapSize.Y )
						return true;

					if( !IsFreeInMapMotion( index ) )
						return true;
				}
				pos += posStep;
				distance += smoothFindStep;
			}

			return false;
		}

		int Smooth( Vec2 start, int pathSize, float unitSize )
		{
			int size = 0;

			Vec2 ptBase = start;

			Vec2I nPointOld = pathArray[ 0 ];
			Vec2 pointOld = new Vec2( (float)nPointOld.X + .5f, (float)nPointOld.Y + .5f );

			for( int inpos = 1; inpos < pathSize; inpos++ )
			{
				Vec2I npt = pathArray[ inpos ];
				Vec2 pt = new Vec2( (float)npt.X + .5f, (float)npt.Y + .5f );
				if( LineIntersect( ptBase, pt, unitSize ) )
				{
					pathArray[ size++ ] = nPointOld;
					ptBase = pointOld;
				}
				nPointOld = npt;
				pointOld = pt;
			}
			pathArray[ size++ ] = nPointOld;
			return size;
		}

		public bool FindPath( float unitSize, Vec2 start, Vec2 end, int maxFieldsDistance, int maxFieldsToCheck, bool smooth,
			bool visualizeForDebugging, List<Vec2> path )
		{
			if( !initialized )
				return false;

			path.Clear();

			float internalUnitSize = unitSize * gridCellSizeInv;
			int nInternalUnitSize = (int)( internalUnitSize + .99999f );

			if( nInternalUnitSize == 0 )
				Log.Fatal( "GridBasedNavigationSystem: FindPath: nInternalUnitSize == 0." );

			Vec2 internalStart = ( start - mapMotionPosition ) * gridCellSizeInv;
			Vec2 internalEnd = ( end - mapMotionPosition ) * gridCellSizeInv;

			Vec2I nInternalStart = new Vec2I( (int)internalStart.X, (int)internalStart.Y );
			Vec2I nInternalEnd = new Vec2I( (int)internalEnd.X, (int)internalEnd.Y );

			if( nInternalStart.X < 0 || nInternalStart.X >= mapSize.X ||
				nInternalStart.Y < 0 || nInternalStart.Y >= mapSize.Y )
				return false;
			if( nInternalEnd.X < 0 || nInternalEnd.X >= mapSize.X ||
				nInternalEnd.Y < 0 || nInternalEnd.Y >= mapSize.Y )
				return false;

			int nPathSize = DoFindA( nInternalStart, nInternalEnd, nInternalUnitSize,
				maxFieldsDistance, maxFieldsToCheck, visualizeForDebugging );
			if( nPathSize == 0 )
				return false;

			if( smooth )
			{
				//remove fictitious points
				nPathSize = RemoveFictitiousPoints( nInternalStart, nPathSize );
				if( nPathSize == 0 )
					return false;
			}

			pathArray[ 0 ] = nInternalStart;

			if( smooth )
			{
				//smooth path
				nPathSize = Smooth( internalStart, nPathSize + 1, internalUnitSize );
				if( nPathSize == 0 )
					return false;
			}

			for( int n = 0; n < nPathSize; n++ )
			{
				Vec2 internalPoint = new Vec2( pathArray[ n ].X, pathArray[ n ].Y ) + new Vec2( .5f, .5f );
				path.Add( internalPoint * GridCellSize + mapMotionPosition );
			}

			if( !smooth )
				path.Add( end );

			return true;
		}

		protected virtual bool OnGetMotionMapHeight( Vec2 pos, out float height )
		{
			Bounds worldBounds = Map.Instance.SceneGraph.GetOctreeBoundsWithBoundsOfObjectsOutsideOctree();

			RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing( new Ray(
				new Vec3( pos.X, pos.Y, worldBounds.Maximum.Z ), new Vec3( 0, 0, worldBounds.Minimum.Z - worldBounds.Maximum.Z ) ),
				(int)ContactGroup.CastOnlyCollision );
			if( piercingResult.Length != 0 )
			{
				bool downFound = false;
				float downHeight = worldBounds.Minimum.Z;

				for( int n = piercingResult.Length - 1; n >= 0; n-- )
				{
					RayCastResult result = piercingResult[ n ];

					if( heightByTerrainOnly )
					{
						if( HeightmapTerrain.GetTerrainByBody( result.Shape.Body ) == null )
							continue;
					}

					if( downFound )
					{
						if( result.Position.Z - downHeight > AgentHeight )
						{
							height = downHeight;
							return true;
						}
						else
							downFound = false;
					}

					if( !downFound )
					{
						downFound = true;
						downHeight = result.Position.Z;
					}
				}

				if( downFound )
				{
					height = downHeight;
					return true;
				}
				height = piercingResult[ piercingResult.Length - 1 ].Position.Z;
				return true;
			}
			else
			{
				height = 0;
				return false;
			}
		}

		public bool GetMotionMapHeight( Vec2 pos, out float height )
		{
			return OnGetMotionMapHeight( pos, out height );
		}

		public float GetMotionMapHeight( Vec2 pos )
		{
			float height;
			OnGetMotionMapHeight( pos, out height );
			return height;
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

		void DrawDebugLineStepOnCollision( Camera camera, Vec2 start, Vec2 end )
		{
			float startZ = GetMotionMapHeight( start );
			float endZ = GetMotionMapHeight( end );

			const float offsetZ = .2f;
			AddThicknessLine( camera, new Vec3( start, startZ + offsetZ ), new Vec3( end, endZ + offsetZ ), .07f );
			camera.DebugGeometry.AddLine( new Vec3( start, startZ + offsetZ ), new Vec3( end, endZ + offsetZ ) );
		}

		void DrawDebugLineOnCollision( Camera camera, Vec2 start, Vec2 end )
		{
			const float step = 1.0f;

			Vec2 dirVec = end - start;
			float length = dirVec.Normalize();
			Vec2 posStep = dirVec * step;

			float distance = 0;
			Vec2 pos = start;
			while( distance < length )
			{
				distance += step;

				Vec2 newPos = pos + posStep;
				if( distance > length )
					newPos = end;

				DrawDebugLineStepOnCollision( camera, pos, newPos );

				pos += posStep;
			}
		}

		public void DebugDrawPath( Camera camera, Vec2 startPosition, List<Vec2> path )
		{
			Vec3 offset = new Vec3( 0, 0, .2f );

			Vec3 p = new Vec3( startPosition.X, startPosition.Y, GetMotionMapHeight( startPosition ) );
			foreach( Vec2 pathPoint in path )
			{
				Vec3 start = p;
				Vec3 end = new Vec3( pathPoint.X, pathPoint.Y, GetMotionMapHeight( pathPoint ) );

				camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
				DrawDebugLineOnCollision( camera, start.ToVec2(), end.ToVec2() );

				p = end;
			}

			foreach( Vec2 pathPoint in path )
			{
				Vec3 end = new Vec3( pathPoint.X, pathPoint.Y, GetMotionMapHeight( pathPoint ) );

				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				AddSphere( camera, new Sphere( end + offset, .15f ) );
			}
		}

		Vec2I GetNearestFreeMotionMap( Vec2I pos, int unitSize )
		{
			if( IsFreeInMapMotion( pos, unitSize ) )
				return pos;

			for( int r = 1; ; r++ )
			{
				for( int n = 0; n <= r; n++ )
				{
					Vec2I p;

					p = pos + new Vec2I( n, -r );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( n, r );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( -n, -r );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( -n, r );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( -r, n );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( r, n );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( -r, -n );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;

					p = pos + new Vec2I( -r, n );
					if( IsFreeInMapMotion( p, unitSize ) )
						return p;
				}
			}
		}

		public Vec2 GetNearestFreePosition( Vec2 pos, float unitSize )
		{
			if( !initialized )
				return pos;

			int nInternalUnitSize = (int)( unitSize * gridCellSizeInv + .99999f );
			Vec2I motionMapPos = GetNearestFreeMotionMap( GetMapMotionPosition( pos ), nInternalUnitSize );

			Vec2 nearestPos = motionMapPos.ToVec2() * GridCellSize + mapMotionPosition;

			//not absolutely true
			nearestPos.X += GridCellSize * .5f;
			nearestPos.Y += GridCellSize * .5f;

			return nearestPos;
		}
	}
}
