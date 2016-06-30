// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="FreezeObjectsManager"/> entity type.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class FreezeObjectsManagerType : MapGeneralObjectType
	{
		public FreezeObjectsManagerType()
		{
			UniqueEntityInstance = true;
			AllowEmptyName = true;
		}
	}

	/// <summary>
	/// Defines the manager to control of freezing the objects on a map.
	/// </summary>
	public class FreezeObjectsManager : MapGeneralObject
	{
		static FreezeObjectsManager instance;

		[FieldSerialize( "enabled" )]
		bool enabled = true;
		[FieldSerialize( "enabledInEditor" )]
		bool enabledInEditor;

		[FieldSerialize( "unfreezeByCameraDistance" )]
		float unfreezeByCameraDistance = 50;
		[FieldSerialize( "unfreezeByCameraZDistance" )]
		float unfreezeByCameraZDistance; //if 0, sphere shape, else Z check ( Cylinder Shape )

		[FieldSerialize( "cellCount" )]
		Vec3I cellCount = new Vec3I( 50, 50, 4 );
		[FieldSerialize( "cellMinSize" )]
		Vec3 cellMinSize = new Vec3( 4, 4, 4 );
		[FieldSerialize( "debugDrawGrid" )]
		bool debugDrawGrid;

		//

		struct GridItem
		{
			public int startIndex;
			public int count;
		}
		Vec3 gridStartPosition;
		Vec3 gridCellSize;
		Vec3 gridCellSizeInv;
		GridItem[ , , ] grid;
		FreezeObjectsArea[] gridAreas;

		bool needRegenerateGrid;

		//

		[TypeField]
		FreezeObjectsManagerType __type = null;
		/// <summary>
		/// Gets the entity type.
		/// </summary>
		public new FreezeObjectsManagerType Type { get { return __type; } }

		///////////////////////////////////////////

		class GridAreasIndexByListOfAreasKey : IEqualityComparer<FreezeObjectsArea[]>
		{
			public bool Equals( FreezeObjectsArea[] x, FreezeObjectsArea[] y )
			{
				if( x.Length != y.Length )
					return false;
				for( int n = 0; n < x.Length; n++ )
				{
					if( x[ n ] != y[ n ] )
						return false;
				}
				return true;
			}

			public int GetHashCode( FreezeObjectsArea[] obj )
			{
				int hash = 0;
				foreach( FreezeObjectsArea area in obj )
					hash ^= area.GetHashCode();
				return hash;
			}
		}

		///////////////////////////////////////////

		public FreezeObjectsManager()
		{
			instance = this;
		}

		public static FreezeObjectsManager Instance
		{
			get { return FreezeObjectsManager.instance; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the manager enabled.
		/// </summary>
		[DefaultValue( true )]
		[LocalizedDisplayName( "Enabled", "FreezeObjectsManager" )]
		[LocalizedDescription( "A value indicating whether the manager enabled.", "FreezeObjectsManager" )]
		public bool Enabled
		{
			get { return enabled; }
			set
			{
				if( enabled == value )
					return;
				enabled = value;

				ClearGrid();
				if( IsReallyEnabled() )
					needRegenerateGrid = true;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the manager enabled in Map Editor.
		/// </summary>
		[DefaultValue( false )]
		[LocalizedDisplayName( "Enabled In Editor", "FreezeObjectsManager" )]
		[LocalizedDescription( "A value indicating whether the manager enabled in Map Editor.", "FreezeObjectsManager" )]
		public bool EnabledInEditor
		{
			get { return enabledInEditor; }
			set
			{
				if( enabledInEditor == value )
					return;
				enabledInEditor = value;

				ClearGrid();
				if( IsReallyEnabled() )
					needRegenerateGrid = true;
			}
		}

		/// <summary>
		/// Specifies the value for unfreezing by camera distance.
		/// </summary>
		[DefaultValue( 50.0f )]
		[LocalizedDisplayName( "Unfreeze By Camera Distance", "FreezeObjectsManager" )]
		[LocalizedDescription( "Specifies the value for unfreezing by camera distance.", "FreezeObjectsManager" )]
		public float UnfreezeByCameraDistance
		{
			get { return unfreezeByCameraDistance; }
			set
			{
				if( value < 0 )
					value = 0;
				unfreezeByCameraDistance = value;
			}
		}

		/// <summary>
		/// Defines to shape for unfreezing by camera distance. Set \"0\" for sphere shape, else special Z distance is enabled (cylinder shape).
		/// </summary>
		[DefaultValue( 0.0f )]
		[LocalizedDisplayName( "Unfreeze By Camera Z Distance", "FreezeObjectsManager" )]
		[LocalizedDescription( "Defines to shape for unfreezing by camera distance. Set \"0\" for sphere shape, else special Z distance is enabled (cylinder shape).", "FreezeObjectsManager" )]
		public float UnfreezeByCameraZDistance
		{
			get { return unfreezeByCameraZDistance; }
			set
			{
				if( value < 0 )
					value = 0;
				unfreezeByCameraZDistance = value;
			}
		}

		[DefaultValue( typeof( Vec3 ), "50 50 4" )]
		[LocalizedDisplayName( "Cell Count", "FreezeObjectsManager" )]
		[LocalizedDescription( "The amount of cells of the 3D grid. The grid is used for fast search areas by the position.", "FreezeObjectsManager" )]
		public Vec3I CellCount
		{
			get { return cellCount; }
			set
			{
				if( value.X < 1 )
					value.X = 1;
				if( value.Y < 1 )
					value.Y = 1;
				if( value.Z < 1 )
					value.Z = 1;
				if( cellCount == value )
					return;
				cellCount = value;

				ClearGrid();
				needRegenerateGrid = true;
			}
		}

		[DefaultValue( typeof( Vec3 ), "4 4 4" )]
		[LocalizedDisplayName( "Cell Min Size", "FreezeObjectsManager" )]
		[LocalizedDescription( "Minimal cell size of the 3D grid. The grid is used for fast search areas by the position.", "FreezeObjectsManager" )]
		public Vec3 CellMinSize
		{
			get { return cellMinSize; }
			set
			{
				if( value.X < .1f )
					value.X = .1f;
				if( value.Y < .1f )
					value.Y = .1f;
				if( value.Z < .1f )
					value.Z = .1f;
				if( cellMinSize == value )
					return;
				cellMinSize = value;

				ClearGrid();
				needRegenerateGrid = true;
			}
		}

		[DefaultValue( false )]
		[LocalizedDisplayName( "Debug Draw Grid", "FreezeObjectsManager" )]
		[LocalizedDescription( "Enables drawing the structure of the 3D grid. The grid is used for fast search areas by the position.", "FreezeObjectsManager" )]
		public bool DebugDrawGrid
		{
			get { return debugDrawGrid; }
			set { debugDrawGrid = value; }
		}

		/// <summary>
		/// Called after the entity is added into the world.
		/// </summary>
		/// <param name="loaded"><b>true</b> if the entity has been loaded; otherwise, <b>false</b>.</param>
		/// <seealso cref="Engine.EntitySystem.Entity.PostCreate()"/>
		/// <seealso cref="Engine.EntitySystem.Entity.OnPostCreate2(bool)"/>
		protected override void OnPostCreate( bool loaded )
		{
			if( instance == null )//for undo support
				instance = this;

			base.OnPostCreate( loaded );

			Entities.Instance.BeforeEntityTick += Entities_BeforeEntityTick;
			Map.Instance.BeforeMapGeneralObjectOnRenderFrame += Map_BeforeMapGeneralObjectOnRenderFrame;
			Map.Instance.BeforeMapGeneralObjectOnRender += Map_BeforeMapGeneralObjectOnRender;

			needRegenerateGrid = true;
		}

		/// <summary>
		/// Called when the entity is removed from the world.
		/// </summary>
		protected override void OnDestroy()
		{
			ClearGrid();

			if( Entities.Instance != null )
				Entities.Instance.BeforeEntityTick -= Entities_BeforeEntityTick;
			if( Map.Instance != null )
			{
				Map.Instance.BeforeMapGeneralObjectOnRenderFrame -= Map_BeforeMapGeneralObjectOnRenderFrame;
				Map.Instance.BeforeMapGeneralObjectOnRender -= Map_BeforeMapGeneralObjectOnRender;
			}

			base.OnDestroy();

			if( instance == this )//for undo support
				instance = null;
		}

		void Entities_BeforeEntityTick( Entity entity, ref bool skipTick )
		{
			if( !skipTick )
			{
				MapObject mapObject = entity as MapObject;
				if( mapObject != null && GetObjectLastFrozenState( mapObject ) )
					skipTick = true;
			}
		}

		void Map_BeforeMapGeneralObjectOnRenderFrame( MapGeneralObject entity, ref bool skipCallOnRenderFrame )
		{
			if( !skipCallOnRenderFrame )
			{
				MapObject mapObject = entity as MapObject;
				if( mapObject != null )
				{
					//TO DO: don't calculate each render frame event.

					bool frozen = CalculateFrozenState( mapObject );
					mapObject._FreezeObjectsManagerData = new MapObject._FreezeObjectsManagerDataStruct( frozen );
					if( frozen )
						skipCallOnRenderFrame = true;
				}
			}
		}

		void Map_BeforeMapGeneralObjectOnRender( MapGeneralObject entity, Camera camera, ref bool skipCallOnRender )
		{
			if( !skipCallOnRender )
			{
				MapObject mapObject = entity as MapObject;
				if( mapObject != null && GetObjectLastFrozenState( mapObject ) )
					skipCallOnRender = true;
			}
		}

		public bool IsReallyEnabled()
		{
			if( enabled )
			{
				if( EngineApp.Instance.ApplicationType == EngineApp.ApplicationTypes.Simulation )
					return true;
				else
					return EnabledInEditor;
			}
			return false;
		}

		bool CalculateFrozenState( MapObject obj )
		{
			if( !IsReallyEnabled() )
				return false;
			if( obj._FreezeObjectsManagerNeverFreeze )
				return false;

			//check by camera distance
			{
				Camera camera = RendererWorld.Instance.DefaultCamera;

				if( unfreezeByCameraZDistance != 0 )
				{
					float length2 = ( camera.Position.ToVec2() - obj.Position.ToVec2() ).Length();
					if( length2 < unfreezeByCameraDistance )
						return false;
					float lengthZ = Math.Abs( camera.Position.Z - obj.Position.Z );
					if( lengthZ < unfreezeByCameraZDistance )
						return false;
				}
				else
				{
					float length = ( camera.Position - obj.Position ).Length();
					if( length < unfreezeByCameraDistance )
						return false;
				}
			}

			//check by areas
			if( grid != null )
			{
				Vec3 indexF = ( obj.Position - gridStartPosition ) * gridCellSizeInv;
				Vec3I index = indexF.ToVec3I();
				if( index.X >= 0 && index.Y >= 0 && index.Z >= 0 && index.X < cellCount.X && index.Y < cellCount.Y && index.Z < cellCount.Z )
				{
					GridItem item = grid[ index.X, index.Y, index.Z ];

					for( int n = 0; n < item.count; n++ )
					{
						FreezeObjectsArea area = gridAreas[ n + item.startIndex ];

						bool unfreezing;
						area._UpdateLastCheckState( out unfreezing );
						if( unfreezing )
							return false;
					}
				}
			}

			return true;
		}

		public bool GetObjectLastFrozenState( MapObject obj )
		{
			return obj._FreezeObjectsManagerData.Frozen;
		}

		Bounds GetCellBounds( Vec3I cellIndex )
		{
			Vec3 min = gridStartPosition + gridCellSize * cellIndex.ToVec3();
			return new Bounds( min, min + gridCellSize );
		}

		Vec3I GetCellIndexWithoutClamp( Vec3 position )
		{
			return ( ( position - gridStartPosition ) * gridCellSizeInv ).ToVec3I();
		}

		void ClampCellIndex( ref Vec3I cellIndex )
		{
			if( cellIndex.X < 0 )
				cellIndex.X = 0;
			if( cellIndex.Y < 0 )
				cellIndex.Y = 0;
			if( cellIndex.Z < 0 )
				cellIndex.Z = 0;
			if( cellIndex.X >= cellCount.X )
				cellIndex.X = cellCount.X - 1;
			if( cellIndex.Y >= cellCount.Y )
				cellIndex.Y = cellCount.Y - 1;
			if( cellIndex.Z >= cellCount.Z )
				cellIndex.Z = cellCount.Z - 1;
		}

		Bounds GetGridBounds()
		{
			return new Bounds(
				gridStartPosition,
				gridStartPosition + gridCellSize * cellCount.ToVec3() );
		}

		void GenerateGrid()
		{
			ClearGrid();

			if( FreezeObjectsArea.Instances.Count != 0 )
			{
				//init grid
				{
					Bounds bounds = Bounds.Cleared;
					foreach( FreezeObjectsArea area in FreezeObjectsArea.Instances )
						bounds.Add( area.MapBounds );
					if( bounds.GetSize().X < 1 )
						bounds.Expand( new Vec3( 1, 0, 0 ) );
					if( bounds.GetSize().Y < 1 )
						bounds.Expand( new Vec3( 0, 1, 0 ) );
					if( bounds.GetSize().Z < 1 )
						bounds.Expand( new Vec3( 0, 0, 1 ) );

					gridStartPosition = bounds.Minimum;

					gridCellSize = bounds.GetSize() / cellCount.ToVec3();
					for( int n = 0; n < 3; n++ )
					{
						if( gridCellSize[ n ] < cellMinSize[ n ] )
							gridCellSize[ n ] = cellMinSize[ n ];
					}
					gridCellSizeInv = 1.0f / gridCellSize;
					grid = new GridItem[ cellCount.X, cellCount.Y, cellCount.Z ];
				}

				//fill areas

				GridAreasIndexByListOfAreasKey comparer = new GridAreasIndexByListOfAreasKey();
				Dictionary<FreezeObjectsArea[], int> gridAreasIndexByListOfAreas = new Dictionary<FreezeObjectsArea[], int>( comparer );
				List<FreezeObjectsArea> gridAreasList = new List<FreezeObjectsArea>();

				foreach( FreezeObjectsArea area in FreezeObjectsArea.Instances )
				{
					Box box = area.GetBox();
					Bounds bounds = box.ToBounds();

					Vec3I startIndex = GetCellIndexWithoutClamp( bounds.Minimum );
					Vec3I endIndex = GetCellIndexWithoutClamp( bounds.Maximum );
					ClampCellIndex( ref startIndex );
					ClampCellIndex( ref endIndex );

					for( int z = startIndex.Z; z <= endIndex.Z; z++ )
					{
						for( int y = startIndex.Y; y <= endIndex.Y; y++ )
						{
							for( int x = startIndex.X; x <= endIndex.X; x++ )
							{
								Vec3I cellIndex = new Vec3I( x, y, z );
								Bounds cellBounds = GetCellBounds( cellIndex );
								if( box.IsIntersectsBounds( cellBounds ) )
								{
									GridItem item = grid[ cellIndex.X, cellIndex.Y, cellIndex.Z ];

									FreezeObjectsArea[] array = new FreezeObjectsArea[ item.count + 1 ];
									for( int n = 0; n < item.count; n++ )
										array[ n ] = gridAreas[ item.startIndex + n ];
									array[ array.Length - 1 ] = area;

									int gridAreasIndex;
									if( !gridAreasIndexByListOfAreas.TryGetValue( array, out gridAreasIndex ) )
									{
										gridAreasIndex = gridAreasList.Count;

										gridAreasIndexByListOfAreas.Add( array, gridAreasIndex );
										gridAreasList.AddRange( array );
									}

									item.startIndex = gridAreasIndex;
									item.count = array.Length;
									grid[ cellIndex.X, cellIndex.Y, cellIndex.Z ] = item;
								}
							}
						}
					}

					gridAreas = gridAreasList.ToArray();
				}
			}
		}

		void ClearGrid()
		{
			gridStartPosition = Vec3.Zero;
			gridCellSize = Vec3.Zero;
			gridCellSizeInv = Vec3.Zero;
			grid = null;
			gridAreas = null;
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( needRegenerateGrid )
			{
				GenerateGrid();
				needRegenerateGrid = false;
			}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( DebugDrawGrid && grid != null && camera == RendererWorld.Instance.DefaultCamera )
			//if( DebugDrawGrid && grid != null && camera.Purpose == Camera.Purposes.MainCamera )
			{
				camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
				camera.DebugGeometry.AddBounds( GetGridBounds() );
			}
		}

		public void SetNeedRegenerateGrid()
		{
			needRegenerateGrid = true;
		}

		public bool GridIsCreated()
		{
			return grid != null;
		}
	}
}
