// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="DecalCreator"/> entity type.
	/// </summary>
	public class DecalCreatorType : MapObjectType
	{
		[FieldSerialize]
		float size = 1;

		[FieldSerialize]
		SpreadTypes spreadType;

		[FieldSerialize]
		float omniMaxDistance = 1;

		[FieldSerialize]
		List<MaterialItem> materials = new List<MaterialItem>();

		[FieldSerialize]
		int maxCount;

		[FieldSerialize]
		float lifeTime;

		[FieldSerialize]
		float depthRenderOffset = .01f;

		[FieldSerialize]
		float fadeTime;

		[FieldSerialize]
		bool applyToMapObjects;

		///////////////////////////////////////////

		public enum SpreadTypes
		{
			Directional,
			Omni,
		}

		///////////////////////////////////////////

		public class MaterialItem
		{
			[FieldSerialize]
			string physicsMaterialName = "";
			[FieldSerialize]
			string materialName = "";

			//

			[Editor( typeof( PhysicsWorld.MaterialNameEditor ), typeof( UITypeEditor ) )]
			public string PhysicsMaterialName
			{
				get { return physicsMaterialName; }
				set { physicsMaterialName = value; }
			}

			[Editor( typeof( EditorMaterialUITypeEditor ), typeof( UITypeEditor ) )]
			public string MaterialName
			{
				get { return materialName; }
				set { materialName = value; }
			}

			public override string ToString()
			{
				if( string.IsNullOrEmpty( MaterialName ) )
					return "(not initialized)";

				return MaterialName;
			}
		}

		///////////////////////////////////////////

		public DecalCreatorType()
		{
			AllowEmptyName = true;
		}

		[DefaultValue( 1.0f )]
		public float Size
		{
			get { return size; }
			set { size = value; }
		}

		[DefaultValue( SpreadTypes.Directional )]
		public SpreadTypes SpreadType
		{
			get { return spreadType; }
			set { spreadType = value; }
		}

		[DefaultValue( 1.0f )]
		public float OmniMaxDistance
		{
			get { return omniMaxDistance; }
			set { omniMaxDistance = value; }
		}

		[TypeConverter( typeof( CollectionTypeConverter ) )]
		[Editor( "ProjectEntities.Editor.DecalCreatorType_MaterialsCollectionEditor, ProjectEntities.Editor", typeof( UITypeEditor ) )]
		public List<MaterialItem> Materials
		{
			get { return materials; }
		}

		[DefaultValue( 0 )]
		public int MaxCount
		{
			get { return maxCount; }
			set { maxCount = value; }
		}

		[Description( "Using for fixed pipeline only. For shader based rendering use material property DepthOffset of ShaderBase material." )]
		[DefaultValue( .01f )]
		public float DepthRenderOffset
		{
			get { return depthRenderOffset; }
			set { depthRenderOffset = value; }
		}

		[DefaultValue( 0.0f )]
		public float LifeTime
		{
			get { return lifeTime; }
			set { lifeTime = value; }
		}

		[DefaultValue( 0.0f )]
		public float FadeTime
		{
			get { return fadeTime; }
			set { fadeTime = value; }
		}

		[DefaultValue( false )]
		public bool ApplyToMapObjects
		{
			get { return applyToMapObjects; }
			set { applyToMapObjects = value; }
		}


		//for DecalCreator instances
		LinkedList<Decal> createdDecals = new LinkedList<Decal>();
		float checkMaxCountTime;

		internal void AddDecalToCreatedList( Decal decal )
		{
			createdDecals.AddLast( decal );
		}

		internal void RemoveDecalFromCreatedList( Decal decal )
		{
			createdDecals.Remove( decal );
		}

		internal void CheckMaxCount()
		{
			if( MaxCount == 0 )
				return;

			float time = EngineApp.Instance.Time;
			if( time == checkMaxCountTime )
				return;
			checkMaxCountTime = time;

			while( createdDecals.Count > MaxCount )
			{
				Decal decal = createdDecals.First.Value;
				createdDecals.Remove( decal );
				decal.SetForDeletion( true );
			}
		}
	}

	public class DecalCreator : MapObject
	{
		bool decalsCreated;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float lifeTime;

		List<Decal> decals = new List<Decal>();

		///////////////////////////////////////////

		struct ShapeTriangleID : IEquatable<ShapeTriangleID>
		{
			public Shape shape;
			public int triangleID;

			public ShapeTriangleID( Shape shape, int triangleID )
			{
				this.shape = shape;
				this.triangleID = triangleID;
			}
			public override int GetHashCode()
			{
				return shape.GetHashCode() ^ triangleID.GetHashCode();
			}
			public override bool Equals( object obj )
			{
				return ( obj is ShapeTriangleID && this == (ShapeTriangleID)obj );
			}
			public static bool operator ==( ShapeTriangleID a, ShapeTriangleID b )
			{
				return ( a.shape == b.shape && a.triangleID == b.triangleID );
			}
			public static bool operator !=( ShapeTriangleID a, ShapeTriangleID b )
			{
				return ( a.shape != b.shape || a.triangleID != b.triangleID );
			}
			public bool Equals( ShapeTriangleID other )
			{
				return ( shape == other.shape && triangleID == other.triangleID );
			}
		}

		///////////////////////////////////////////

		DecalCreatorType _type = null; public new DecalCreatorType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();

			if( !loaded && !EntitySystemWorld.Instance.IsEditor() )
			{
				if( !decalsCreated )
					CreateDecals();
			}
		}

		void DoTick()
		{
			if( !decalsCreated )
				CreateDecals();

			//tick lifeTime
			if( Type.LifeTime != 0 && lifeTime >= 0 )
			{
				lifeTime += TickDelta;
				if( lifeTime >= Type.LifeTime )
				{
					Decal[] list = decals.ToArray();
					foreach( Decal decal in list )
						decal.SetForDeletion( true );

					lifeTime = -1;
				}
			}

			//delete if no decals
			if( decals.Count == 0 )
				SetForDeletion( true );

			Type.CheckMaxCount();
		}

		protected override void OnTick()
		{
			base.OnTick();
			DoTick();
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();
			DoTick();
		}

		internal void RemoveDecal( Decal decal )
		{
			Type.RemoveDecalFromCreatedList( decal );
			decals.Remove( decal );
		}

		void CreateDecals()
		{
			decalsCreated = true;

			switch( Type.SpreadType )
			{
			case DecalCreatorType.SpreadTypes.Directional:
				{
					Vec3 dir = Rotation.GetForward();

					RayCastResult result = PhysicsWorld.Instance.RayCast(
						new Ray( Position - dir * .01f, dir * .02f ), (int)ContactGroup.CastOnlyCollision );

					if( result.Shape != null )
					{
						CreateDirectionalDecal( result.Shape, result.Position,
							result.Normal, result.TriangleID );
					}
				}
				break;

			case DecalCreatorType.SpreadTypes.Omni:
				{
					//ray cast for 6 directions

					Vec3 pos = Position + Rotation * new Vec3( -.001f, 0, 0 );

					for( int nDirection = 0; nDirection < 6; nDirection++ )
					{
						Vec3 dir = Vec3.Zero;

						switch( nDirection )
						{
						case 0: dir = new Vec3( 1, 0, 0 ); break;
						case 1: dir = new Vec3( -1, 0, 0 ); break;
						case 2: dir = new Vec3( 0, 1, 0 ); break;
						case 3: dir = new Vec3( 0, -1, 0 ); break;
						case 4: dir = new Vec3( 0, 0, 1 ); break;
						case 5: dir = new Vec3( 0, 0, -1 ); break;
						}

						RayCastResult result = PhysicsWorld.Instance.RayCast(
							new Ray( pos, dir * Type.OmniMaxDistance ), (int)ContactGroup.CastOnlyCollision );

						if( result.Shape != null )
						{
							Radian angle = MathFunctions.ACos( Vec3.Dot( -dir, result.Normal ) );
							if( angle <= new Degree( 45.0f ).InRadians() )
							{
								CreateDirectionalDecal( result.Shape, result.Position, -dir,
									result.TriangleID );
							}
						}
					}
				}
				break;
			}
		}

		void CreateDirectionalDecal( Shape shape, Vec3 pos, Vec3 normal, int triangleID )
		{
			bool smallDecal = Type.Size < .3f;

			Body body = shape.Body;

			//static objects
			if( shape.ContactGroup == (int)ContactGroup.Collision )
			{
				ShapeTriangleID triangle = new ShapeTriangleID( shape, triangleID );

				//StaticMesh
				{
					StaticMesh staticMesh =
						MapSystemWorld.GetMapObjectByShapeWithStaticBatchingSupport( shape, triangleID ) as StaticMesh;
					//StaticMesh staticMesh = StaticMesh.GetStaticMeshByBody( body );
					if( staticMesh != null )
					{
						if( staticMesh.AllowDecals == StaticMesh.DecalTypes.OnlySmall && smallDecal ||
							staticMesh.AllowDecals == StaticMesh.DecalTypes.All )
						{
							CreateDecalForStaticObject( triangle, pos, normal, null );
							return;
						}
					}
				}

				//HeightmapTerrain
				if( HeightmapTerrain.GetTerrainByBody( body ) != null )
				{
					CreateDecalForStaticObject( triangle, pos, normal, null );
					return;
				}

				if( Type.ApplyToMapObjects )
				{
					//MapObject: Attached mesh
					MapObject mapObject = MapSystemWorld.GetMapObjectByBody( body );
					if( mapObject != null )
					{
						bool isCollision = false;

						foreach( MapObjectAttachedObject attachedObject in mapObject.AttachedObjects )
						{
							MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
							if( attachedMesh != null && attachedMesh.CollisionBody == body )
							{
								isCollision = true;
								break;
							}
						}

						if( isCollision )
						{
							CreateDecalForStaticObject( triangle, pos, normal, mapObject );
							return;
						}
					}
				}
			}

			//dynamic objects
			{
				//not implemented
			}
		}

		List<Vec3> CutConvexPlanePolygonByPlane( List<Vec3> vertices, Plane plane )
		{
			List<Vec3> outList = new List<Vec3>( vertices.Count );

			for( int n = 0; n < vertices.Count; n++ )
			{
				Vec3 p1 = vertices[ n ];
				Vec3 p2 = vertices[ ( n + 1 ) % vertices.Count ];

				if( plane.GetDistance( p1 ) <= 0 )
					outList.Add( p1 );

				float scale;
				if( plane.LineIntersection( p1, p2, out scale ) )
					outList.Add( p1 * ( 1.0f - scale ) + p2 * scale );
			}

			if( outList.Count >= 3 )
				return outList;
			else
				return null;
		}

		//optimization
		static Set<ShapeTriangleID> triangleIDs = new Set<ShapeTriangleID>();
		static Set<ShapeTriangleID> checkedTriangles = new Set<ShapeTriangleID>();
		static Stack<ShapeTriangleID> trianglesForCheck = new Stack<ShapeTriangleID>( 32 );
		static List<Decal.Vertex> tempVertices = new List<Decal.Vertex>( 64 );
		static List<int> tempIndices = new List<int>( 128 );

		void CreateDecalForStaticObject( ShapeTriangleID startTriangle, Vec3 pos, Vec3 normal,
			MapObject parentMapObject )
		{
			bool existsNormalsMore45Degrees = false;

			//find near triangles
			//Set<ShapeTriangleID> triangleIDs = new Set<ShapeTriangleID>();
			{
				Sphere checkSphere = new Sphere( pos, Type.Size * .5f * 1.41f );//Sqrt(2)

				//Set<ShapeTriangleID> checkedTriangles = new Set<ShapeTriangleID>();

				//Stack<ShapeTriangleID> trianglesForCheck = new Stack<ShapeTriangleID>( 16 );
				trianglesForCheck.Push( startTriangle );

				while( trianglesForCheck.Count != 0 )
				{
					ShapeTriangleID triangle = trianglesForCheck.Pop();

					//add to checked triangles
					if( !checkedTriangles.AddWithCheckAlreadyContained( triangle ) )
					{
						//ignore already checked triangles
						continue;
					}

					//get triangle points
					Vec3 p0, p1, p2;
					{
						switch( triangle.shape.ShapeType )
						{
						case Shape.Type.Mesh:
							MeshShape meshShape = (MeshShape)triangle.shape;
							meshShape.GetTriangle( triangle.triangleID, true, out p0, out p1, out p2 );
							break;
						case Shape.Type.HeightField:
							HeightFieldShape heightFieldShape = (HeightFieldShape)triangle.shape;
							heightFieldShape.GetTriangle( triangle.triangleID, true, out p0, out p1, out p2 );
							break;
						default:
							Log.Fatal( "DecalCreator: CreateDecalForStaticObject: Not supported shape type ({0}).",
								triangle.shape.ShapeType );
							return;
						}
					}

					//cull by checkBounds
					if( !checkSphere.TriangleIntersection( p0, p1, p2 ) )
						continue;

					//check normal
					bool correctNormal = false;

					if( Type.SpreadType != DecalCreatorType.SpreadTypes.Directional )
					{
						Plane plane = Plane.FromPoints( p0, p1, p2 );
						if( plane.GetSide( pos + normal ) == Plane.Side.Positive )
						{
							Radian angle = MathFunctions.ACos( Vec3.Dot( normal, plane.Normal ) );

							if( angle <= new Degree( 70.0f ).InRadians() )
							{
								if( !existsNormalsMore45Degrees && angle >= new Degree( 45.0f ).InRadians() )
									existsNormalsMore45Degrees = true;

								correctNormal = true;
							}
						}
					}
					else
						correctNormal = true;

					if( correctNormal )
					{
						//add triangle to result list
						triangleIDs.Add( triangle );
					}

					//add near triangles to check list
					{
						//expand vertices
						const float border = .001f;
						Vec3 center = ( p0 + p1 + p2 ) * ( 1.0f / 3.0f );

						Vec3 diff0 = p0 - center;
						Vec3 diff1 = p1 - center;
						Vec3 diff2 = p2 - center;

						if( diff0 != Vec3.Zero && diff1 != Vec3.Zero && diff2 != Vec3.Zero )
						{
							p0 += diff0.GetNormalize() * border;
							p1 += diff1.GetNormalize() * border;
							p2 += diff2.GetNormalize() * border;

							Vec3 p01 = ( p0 + p1 ) * .5f;
							Vec3 p12 = ( p1 + p2 ) * .5f;
							Vec3 p20 = ( p2 + p0 ) * .5f;

							//find triangles
							for( int n = 0; n < 3; n++ )
							{
								Vec3 p = Vec3.Zero;
								switch( n )
								{
								case 0: p = p01; break;
								case 1: p = p12; break;
								case 2: p = p20; break;
								}

								RayCastResult[] piercingResult =
									PhysicsWorld.Instance.RayCastPiercing( new Ray(
									p + normal * .025f, -normal * .05f ), (int)ContactGroup.CastOnlyCollision );
								foreach( RayCastResult result in piercingResult )
								{
									if( result.Shape != null )
									{
										trianglesForCheck.Push( new ShapeTriangleID(
											result.Shape, result.TriangleID ) );
									}
								}
							}
						}
					}
				}

				checkedTriangles.Clear();
			}

			if( triangleIDs.Count == 0 )
				return;

			//calculate perpendiculars to normal
			Vec3 side1Normal;
			Vec3 side2Normal;
			{
				if( Math.Abs( normal.X ) > .001f || Math.Abs( normal.Y ) > .001f )
				{
					side1Normal = Mat3.FromRotateByZ( MathFunctions.PI / 2 ) *
						new Vec3( normal.X, normal.Y, 0 );
					side1Normal.Normalize();
				}
				else
					side1Normal = new Vec3( 1, 0, 0 );

				side2Normal = Vec3.Cross( normal, side1Normal );
			}

			//generate clip planes
			Plane[] clipPlanes = new Plane[ 6 ];
			{
				float halfSize = Type.Size * .5f;

				if( existsNormalsMore45Degrees )
					halfSize *= 1.41f;

				Plane p;
				p = Plane.FromVectors( normal, -side2Normal, Position );
				clipPlanes[ 0 ] = new Plane( p.Normal, p.Distance + halfSize );
				p = Plane.FromVectors( normal, side2Normal, Position );
				clipPlanes[ 1 ] = new Plane( p.Normal, p.Distance + halfSize );
				p = Plane.FromVectors( normal, -side1Normal, Position );
				clipPlanes[ 2 ] = new Plane( p.Normal, p.Distance + halfSize );
				p = Plane.FromVectors( normal, side1Normal, Position );
				clipPlanes[ 3 ] = new Plane( p.Normal, p.Distance + halfSize );
				p = Plane.FromVectors( side1Normal, side2Normal, Position );
				clipPlanes[ 4 ] = new Plane( p.Normal, p.Distance + halfSize );
				//clipPlanes[ 4 ] = new Plane( p.Normal, p.Distance + halfSize * .5f );
				p = Plane.FromVectors( side1Normal, -side2Normal, Position );
				clipPlanes[ 5 ] = new Plane( p.Normal, p.Distance + halfSize );
				//clipPlanes[ 5 ] = new Plane( p.Normal, p.Distance + halfSize * .5f );
			}

			//generate vertices and indices by triangles
			//List<Decal.Vertex> vertices = new List<Decal.Vertex>( triangleIDs.Count * 3 );
			//List<int> indices = new List<int>( triangleIDs.Count * 3 );
			List<Decal.Vertex> vertices = tempVertices;
			List<int> indices = tempIndices;
			vertices.Clear();
			indices.Clear();
			{
				foreach( ShapeTriangleID triangle in triangleIDs )
				{
					Vec3 p0, p1, p2;
					{
						switch( triangle.shape.ShapeType )
						{
						case Shape.Type.Mesh:
							MeshShape meshShape = (MeshShape)triangle.shape;
							meshShape.GetTriangle( triangle.triangleID, true, out p0, out p1, out p2 );
							break;
						case Shape.Type.HeightField:
							HeightFieldShape heightFieldShape = (HeightFieldShape)triangle.shape;
							heightFieldShape.GetTriangle( triangle.triangleID, true, out p0, out p1, out p2 );
							break;
						default:
							Log.Fatal( "DecalCreator: CreateDecalForStaticObject: Not supported shape type ({0}).",
								triangle.shape.ShapeType );
							return;
						}
					}

					List<Vec3> list = new List<Vec3>();
					list.Add( p0 );
					list.Add( p1 );
					list.Add( p2 );

					//clip by planes
					foreach( Plane plane in clipPlanes )
					{
						list = CutConvexPlanePolygonByPlane( list, plane );
						if( list == null )
							break;
					}

					//add to vertices and indices lists
					if( list != null )
					{
						int vertexCount = vertices.Count;

						Vec3 norm = Plane.FromPoints( p0, p1, p2 ).Normal;
						foreach( Vec3 p in list )
							vertices.Add( new Decal.Vertex( p, norm, Vec2.Zero, Vec3.Zero ) );

						for( int n = 1; n < list.Count - 1; n++ )
						{
							indices.Add( vertexCount );
							indices.Add( vertexCount + n );
							indices.Add( vertexCount + n + 1 );
						}
					}
				}
			}

			triangleIDs.Clear();

			if( indices.Count == 0 )
				return;

			//calculate texCoord and Type.DepthRenderOffset
			{
				Plane planeSide1 = Plane.FromVectors( normal, side1Normal, Position );
				Plane planeSide2 = Plane.FromVectors( normal, side2Normal, Position );
				float invSize = 1.0f / Type.Size;

				for( int n = 0; n < vertices.Count; n++ )
				{
					Decal.Vertex vertex = vertices[ n ];

					//calculate texCoord
					float distance1 = planeSide1.GetDistance( vertex.position );
					float distance2 = planeSide2.GetDistance( vertex.position );
					vertex.texCoord = new Vec2( distance1 * invSize + .5f, distance2 * invSize + .5f );

					//Add perpendicular to normal offset.
					//Alternative way: for shader based rendering use DepthOffset property of decal material.
					//if( !RenderSystem.Instance.HasShaderModel3() )
					{
						//add Type.DepthRenderOffset
						vertex.position = vertex.position + normal * Type.DepthRenderOffset;
					}

					vertices[ n ] = vertex;
				}
			}

			//calculate tangent vectors
			{
				int triangleCount = indices.Count / 3;
				for( int nTriangle = 0; nTriangle < triangleCount; nTriangle++ )
				{
					int index0 = indices[ nTriangle * 3 + 0 ];
					int index1 = indices[ nTriangle * 3 + 1 ];
					int index2 = indices[ nTriangle * 3 + 2 ];

					Decal.Vertex vertex0 = vertices[ index0 ];
					Decal.Vertex vertex1 = vertices[ index1 ];
					Decal.Vertex vertex2 = vertices[ index2 ];

					Vec3 tangent = MathUtils.CalculateTangentSpaceVector(
						vertex0.position, vertex0.texCoord,
						vertex1.position, vertex1.texCoord,
						vertex2.position, vertex2.texCoord );

					vertex0.tangent += tangent;
					vertex1.tangent += tangent;
					vertex2.tangent += tangent;

					vertices[ index0 ] = vertex0;
					vertices[ index1 ] = vertex1;
					vertices[ index2 ] = vertex2;
				}

				for( int n = 0; n < vertices.Count; n++ )
				{
					Decal.Vertex vertex = vertices[ n ];
					if( vertex.tangent != Vec3.Zero )
						vertex.tangent.Normalize();
					vertices[ n ] = vertex;
				}
			}

			//subtract decal position (make local vertices coordinates)
			{
				for( int n = 0; n < vertices.Count; n++ )
				{
					Decal.Vertex vertex = vertices[ n ];
					vertex.position -= Position;
					vertices[ n ] = vertex;
				}
			}

			//get material
			string materialName = null;
			{
				string physicsMaterialName = startTriangle.shape.MaterialName;
				string defaultMaterialName = "";

				foreach( DecalCreatorType.MaterialItem item in Type.Materials )
				{
					if( item.PhysicsMaterialName == physicsMaterialName )
						materialName = item.MaterialName;

					if( string.IsNullOrEmpty( item.PhysicsMaterialName ) )
						defaultMaterialName = item.MaterialName;
				}

				if( materialName == null )
					materialName = defaultMaterialName;
			}

			//create Decal
			Decal decal = (Decal)Entities.Instance.Create( "Decal", Map.Instance );
			decal.Position = Position;
			decal.Init( this, vertices.ToArray(), indices.ToArray(), materialName, parentMapObject );
			decal.PostCreate();
			Type.AddDecalToCreatedList( decal );
			decals.Add( decal );
		}

	}
}
