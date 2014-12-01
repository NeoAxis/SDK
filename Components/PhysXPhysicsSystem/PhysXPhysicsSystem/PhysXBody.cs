// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Engine;
using Engine.FileSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;
using Engine.Utils;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXBody : Body
	{
		static double cookTriangleMeshTotalTime;

		Vec3 lastLinearVelocity;
		Vec3 lastLinearVelocity2;

		//

		internal PhysXPhysicsScene scene;

		internal unsafe IntPtr nativeBody;

		internal class ShapeData
		{
			public unsafe IntPtr/*PhysXShape*/[] nativeShapes;
			public PhysXPhysicsWorld.MeshGeometryPhysXData shapesMeshData;
		}
		internal ShapeData[] shapesData;

		//

		public PhysXBody( PhysXPhysicsScene scene )
			: base( scene )
		{
			this.scene = scene;
		}

		protected override void OnSetSleepiness()
		{
			if( !Static )
				UpdateSleepiness();
		}

		public void UpdateSleepiness()
		{
			if( nativeBody != IntPtr.Zero )
			{
				float threshold = MathFunctions.Pow( Sleepiness, 8 ) * 2;
				PhysXNativeBody.SetSleepThreshold( nativeBody, threshold );
			}
		}

		public void UpdateDataFromLibrary()
		{
			if( Static )
				Log.Fatal( "PhysXBody: UpdateDataFromLibrary: Static == true." );

			if( nativeBody != IntPtr.Zero )
			{
				bool sleeping = PhysXNativeBody.IsSleeping( nativeBody );

				if( !sleeping || !Sleeping )
				{
					Vec3 pos;
					Quat rot;
					Vec3 linearVel;
					Vec3 angularVel;
					PhysXNativeBody.GetGlobalPoseLinearAngularVelocities( nativeBody, out pos, out rot, out linearVel,
						out angularVel );

					UpdateDataFromLibrary( ref pos, ref rot, ref linearVel, ref angularVel, sleeping );

					//fix the strange behaviour. The body can not sleep but stay on the one position.
					//fixes car physics sleep bug.
					if( !sleeping && lastLinearVelocity == linearVel && lastLinearVelocity2 == linearVel && linearVel != Vec3.Zero )
					{
						PhysXNativeBody.PutToSleep( nativeBody );
						PhysXNativeBody.WakeUp( nativeBody, PhysXNativeWorld.SLEEP_INTERVAL );
					}
					lastLinearVelocity2 = lastLinearVelocity;
					lastLinearVelocity = linearVel;
				}
			}
		}

		//public override Mat3 GetInertiaTensor()
		//{
		//   if( Static )
		//      return Mat3.Identity;
		//   if( nativeBody == IntPtr.Zero )
		//      return Mat3.Zero;

		//   Mat3 value;
		//   PhysXNativeBody.GetInertiaTensor( nativeBody, out value );

		//   return value;
		//}

		public override void ClearForces()
		{
			base.ClearForces();
		}

		protected override void OnSetLinearDamping()
		{
			if( nativeBody != IntPtr.Zero )
				PhysXNativeBody.SetLinearDamping( nativeBody, LinearDamping );
		}

		protected override void OnSetAngularDamping()
		{
			if( nativeBody != IntPtr.Zero )
				PhysXNativeBody.SetAngularDamping( nativeBody, AngularDamping );
		}

		protected override bool IsNeedRecreateInWorldToUpdateTransform()
		{
			return Static;
		}

		protected override void OnSetTransform( bool updatePosition, bool updateRotation )
		{
			base.OnSetTransform( updatePosition, updateRotation );

			if( nativeBody != IntPtr.Zero )
			{
				Vec3 position = Position;
				Quat rotation = Rotation;
				PhysXNativeBody.SetGlobalPose( nativeBody, ref position, ref rotation );
				//if( !Static && Sleeping )
				//   PhysXNativeBody.PutToSleep( nativeBody );
			}
		}

		protected override void OnSetLinearVelocity()
		{
			if( !Static && nativeBody != IntPtr.Zero )
			{
				Vec3 linearVelocity = LinearVelocity;
				PhysXNativeBody.SetLinearVelocity( nativeBody, ref linearVelocity );
				//if( Sleeping )
				//   PhysXNativeBody.PutToSleep( nativeBody );
			}
		}

		protected override void OnSetAngularVelocity()
		{
			if( !Static && nativeBody != IntPtr.Zero )
			{
				Vec3 angularVelocity = AngularVelocity;
				PhysXNativeBody.SetAngularVelocity( nativeBody, ref angularVelocity );
				//if( Sleeping )
				//   PhysXNativeBody.PutToSleep( nativeBody );
			}
		}

		protected override void OnSetSleeping()
		{
			if( !Static && nativeBody != IntPtr.Zero )
			{
				if( Sleeping )
				{
					ClearForces();
					LinearVelocity = Vec3.Zero;
					AngularVelocity = Vec3.Zero;
					PhysXNativeBody.PutToSleep( nativeBody );
				}
				else
				{
					PhysXNativeBody.WakeUp( nativeBody, PhysXNativeWorld.SLEEP_INTERVAL );
				}
			}
		}

		float GetShapeMass( Shape shape, float totalVolume )
		{
			float mass = 0;
			if( !Static )
			{
				if( MassMethod == MassMethods.Manually )
					mass = Mass * ( shape.Volume / totalVolume );
				else
					mass = shape.Volume * shape.Density;
				if( mass <= 0 )
					mass = .001f;
			}
			return mass;
		}

		IntPtr CreateMaterial( Shape shape )
		{
			return PhysXNativeMaterial.Create( shape.StaticFriction, shape.DynamicFriction, shape.Restitution, shape.MaterialName,
				shape.VehicleDrivableSurface );
		}

		unsafe void CreateBoxShape( BoxShape shape, float totalVolume )
		{
			Vec3 halfDimension = shape.Dimensions * 0.5f;
			Vec3 position = shape.Position;
			Quat rotation = shape.Rotation;
			float mass = GetShapeMass( shape, totalVolume );
			IntPtr material = CreateMaterial( shape );
			PhysXNativeBody.CreateBoxShape( nativeBody, ref position, ref rotation, ref halfDimension, 1, (IntPtr*)&material,
				mass, shape.ContactGroup );
		}

		unsafe void CreateSphereShape( SphereShape shape, float totalVolume )
		{
			Vec3 position = shape.Position;
			Quat rotation = shape.Rotation;
			float mass = GetShapeMass( shape, totalVolume );
			IntPtr material = CreateMaterial( shape );
			PhysXNativeBody.CreateSphereShape( nativeBody, ref position, ref rotation, shape.Radius, 1, (IntPtr*)&material,
				mass, shape.ContactGroup );
		}

		unsafe void CreateCapsuleShape( CapsuleShape shape, float totalVolume )
		{
			Vec3 position = shape.Position;
			Mat3 rotationMatrix = shape.Rotation.ToMat3() * Mat3.FromRotateByY( MathFunctions.PI / 2 );
			Quat rotation = rotationMatrix.ToQuat();
			float mass = GetShapeMass( shape, totalVolume );
			IntPtr material = CreateMaterial( shape );
			PhysXNativeBody.CreateCapsuleShape( nativeBody, ref position, ref rotation, shape.Radius, shape.Length * .5f,
				1, (IntPtr*)&material, mass, shape.ContactGroup );
		}

		unsafe IntPtr CreateConvexMesh( PhysicsWorld.ConvexHullDecompositionDataItem item )
		{
			IntPtr vertices;
			int vertexCount;
			//set vertices
			{
				vertexCount = item.Vertices.Length;
				Vec3* vertices2 = (Vec3*)PhysXNativeWorld.Alloc( item.Vertices.Length * sizeof( Vec3 ) );
				for( int n = 0; n < item.Vertices.Length; n++ )
					vertices2[ n ] = item.Vertices[ n ];
				vertices = (IntPtr)vertices2;
			}

			//set indices
			IntPtr indices;
			int indexCount;
			bool indices16Bits = item.Indices.Length < 65535;
			{
				indexCount = item.Indices.Length;

				if( indices16Bits )
				{
					ushort* indices2 = (ushort*)PhysXNativeWorld.Alloc(
						item.Indices.Length * sizeof( ushort ) );
					for( int n = 0; n < item.Indices.Length; n++ )
						indices2[ n ] = (ushort)item.Indices[ n ];
					indices = (IntPtr)indices2;
				}
				else
				{
					uint* indices2 = (uint*)PhysXNativeWorld.Alloc( item.Indices.Length * sizeof( uint ) );
					for( int n = 0; n < item.Indices.Length; n++ )
						indices2[ n ] = (uint)item.Indices[ n ];
					indices = (IntPtr)indices2;
				}
			}

			IntPtr pxConvexMesh = PhysXNativeWorld.CreateConvexMesh( vertices, vertexCount, indices, indexCount, indices16Bits );

			PhysXNativeWorld.Free( vertices );
			PhysXNativeWorld.Free( indices );

			return pxConvexMesh;
		}

		unsafe void CreateConvexMeshShape( Shape shape, IntPtr pxConvexMesh, float totalVolume )
		{
			Vec3 position = shape.Position;
			Quat rotation = shape.Rotation;
			float mass = GetShapeMass( shape, totalVolume );
			IntPtr material = CreateMaterial( shape );
			PhysXNativeBody.CreateConvexMeshShape( nativeBody, ref position, ref rotation, pxConvexMesh, 1, (IntPtr*)&material,
				mass, shape.ContactGroup );
		}

		unsafe IntPtr CreateTriangleMesh( PhysicsWorld._MeshGeometry geometry )
		{
			byte[] cookedData = null;
			//try get cooked triangle mesh from cache
			if( !geometry.CreatedAsCustomMeshGeometry )
				cookedData = GetCookedTriangleMeshFromCache( geometry.Vertices, geometry.Indices, geometry.MaterialIndices );

			if( cookedData == null )
			{
				//cook triangle mesh

				//set vertices
				IntPtr vertices;
				int vertexCount;
				{
					vertexCount = geometry.Vertices.Length;
					Vec3* vertices2 = (Vec3*)PhysXNativeWorld.Alloc( geometry.Vertices.Length * sizeof( Vec3 ) );
					for( int n = 0; n < geometry.Vertices.Length; n++ )
						vertices2[ n ] = geometry.Vertices[ n ];
					vertices = (IntPtr)vertices2;
				}

				//set indices
				IntPtr indices;
				int indexCount;
				bool indices16Bits = geometry.Indices.Length < 65535;
				{
					indexCount = geometry.Indices.Length;

					if( indices16Bits )
					{
						ushort* indices2 = (ushort*)PhysXNativeWorld.Alloc( geometry.Indices.Length * sizeof( ushort ) );
						for( int n = 0; n < geometry.Indices.Length; n++ )
							indices2[ n ] = (ushort)geometry.Indices[ n ];
						indices = (IntPtr)indices2;
					}
					else
					{
						uint* indices2 = (uint*)PhysXNativeWorld.Alloc( geometry.Indices.Length * sizeof( uint ) );
						for( int n = 0; n < geometry.Indices.Length; n++ )
							indices2[ n ] = (uint)geometry.Indices[ n ];
						indices = (IntPtr)indices2;
					}
				}

				//material indices
				IntPtr materialIndices = IntPtr.Zero;
				if( geometry.MaterialIndices != null )
				{
					short* materialIndices2 = (short*)PhysXNativeWorld.Alloc( geometry.MaterialIndices.Length * sizeof( short ) );
					for( int n = 0; n < geometry.MaterialIndices.Length; n++ )
						materialIndices2[ n ] = geometry.MaterialIndices[ n ];
					materialIndices = (IntPtr)materialIndices2;
				}

				IntPtr cookedNativeData;
				int cookedLength;
				bool cookResult = PhysXNativeWorld.CookTriangleMesh( vertices, vertexCount, indices, indexCount, indices16Bits,
					materialIndices, out cookedNativeData, out cookedLength );

				PhysXNativeWorld.Free( vertices );
				PhysXNativeWorld.Free( indices );
				if( materialIndices != IntPtr.Zero )
					PhysXNativeWorld.Free( materialIndices );

				if( !cookResult )
					return IntPtr.Zero;

				cookedData = new byte[ cookedLength ];
				Marshal.Copy( cookedNativeData, cookedData, 0, cookedLength );
				PhysXNativeWorld.Free( cookedNativeData );

				//write to cache
				if( PhysXPhysicsWorld.Instance.writeCacheForCookedTriangleMeshes && !geometry.CreatedAsCustomMeshGeometry )
					WriteCookedTriangleMeshToCache( geometry.Vertices, geometry.Indices, geometry.MaterialIndices, cookedData );
			}

			//create triangle mesh
			IntPtr triangleMesh;
			fixed( byte* pCookedData = cookedData )
			{
				triangleMesh = PhysXNativeWorld.CreateTriangleMesh( (IntPtr)pCookedData, cookedData.Length );
			}

			return triangleMesh;
		}

		unsafe void CreateTriangleMeshShape( MeshShape shape, IntPtr pxTriangleMesh, float totalVolume )
		{
			//if( shape.PerTriangleMaterials != null && shape.PerTriangleMaterials.Length > 127 )
			//   Log.Fatal( "PhysXBody: CreateTriangleMeshShape: The amount of per triangle materials can't be more than 127." );

			Vec3 position = shape.Position;
			Quat rotation = shape.Rotation;
			float mass = GetShapeMass( shape, totalVolume );

			IntPtr[] materials;
			if( shape.PerTriangleMaterials != null && shape.PerTriangleMaterials.Length > 0 )
			{
				materials = new IntPtr[ shape.PerTriangleMaterials.Length ];
				for( int n = 0; n < materials.Length; n++ )
				{
					MeshShape.PerTriangleMaterial item = shape.PerTriangleMaterials[ n ];
					materials[ n ] = PhysXNativeMaterial.Create( item.StaticFriction, item.DynamicFriction, item.Restitution,
						item.MaterialName, item.VehicleDrivableSurface );
				}
			}
			else
				materials = new IntPtr[] { CreateMaterial( shape ) };
			//IntPtr material = CreateMaterial( shape );

			fixed( IntPtr* pMaterials = materials )
			{
				PhysXNativeBody.CreateTriangleMeshShape( nativeBody, ref position, ref rotation, pxTriangleMesh,
					materials.Length, pMaterials, mass, shape.ContactGroup );
			}
		}

		unsafe void CreateHeightFieldShape( HeightFieldShape shape, float totalVolume )
		{
			if( sizeof( HeightFieldShape.Sample ) != 4 )
				Log.Fatal( "PhysXBody: CreateHeightFieldShape: sizeof( HeightFieldShape.Sample ) != 4." );
			if( shape.PerTriangleMaterials != null && shape.PerTriangleMaterials.Length > 127 )
				Log.Fatal( "PhysXBody: CreateHeightFieldShape: The amount of per triangle materials can't be more than 127." );

			Vec3 position = shape.Position;
			Quat rotation = shape.Rotation;

			//rotate by axis.
			rotation *= new Quat( .5f, .5f, .5f, .5f );
			//Mat3 m = rotation.ToMat3();
			//m *= Mat3.FromRotateByX( -MathFunctions.PI / 2 );
			//m *= Mat3.FromRotateByY( -MathFunctions.PI / 2 );
			//rotation = m.ToQuat();

			Vec3 samplesScale = shape.SamplesScale;

			fixed( HeightFieldShape.Sample* pSamples = shape.Samples )
			{
				IntPtr[] materials;
				if( shape.PerTriangleMaterials != null && shape.PerTriangleMaterials.Length > 0 )
				{
					materials = new IntPtr[ shape.PerTriangleMaterials.Length ];
					for( int n = 0; n < materials.Length; n++ )
					{
						HeightFieldShape.PerTriangleMaterial item = shape.PerTriangleMaterials[ n ];
						materials[ n ] = PhysXNativeMaterial.Create( item.StaticFriction, item.DynamicFriction, item.Restitution,
							item.MaterialName, item.VehicleDrivableSurface );
					}
				}
				else
					materials = new IntPtr[] { CreateMaterial( shape ) };

				fixed( IntPtr* pMaterials = materials )
				{
					PhysXNativeBody.CreateHeightFieldShape( nativeBody, ref position, ref rotation, shape.SampleCount.X,
						shape.SampleCount.Y, (IntPtr)pSamples, ref samplesScale, shape.Thickness, materials.Length, pMaterials,
						shape.ContactGroup );
				}
			}
		}

		unsafe void CreateCustomShape( _Custom1Shape shape, float totalVolume )
		{
			Vec3 halfDimension = shape.Dimensions * 0.5f;
			Vec3 position = shape.Position;
			Quat rotation = shape.Rotation;
			float mass = GetShapeMass( shape, totalVolume );
			IntPtr material = CreateMaterial( shape );
			PhysXNativeBody.CreateBoxShape( nativeBody, ref position, ref rotation, ref halfDimension, 1, (IntPtr*)&material,
				mass, shape.ContactGroup );
		}

		//static bool IsExistsVerticesOnBothSidesOfPlane( Plane plane, Vec3[] vertices, int skipIndex0, int skipIndex1, int skipIndex2 )
		//{
		//   bool negative = false;
		//   bool positive = false;

		//   for( int nVertex = 0; nVertex < vertices.Length; nVertex++ )
		//   {
		//      if( nVertex != skipIndex0 && nVertex != skipIndex1 && nVertex != skipIndex2 )
		//      {
		//         Plane.Side side = plane.GetSide( vertices[ nVertex ] );
		//         if( side == Plane.Side.Negative )
		//            negative = true;
		//         if( side == Plane.Side.Positive )
		//            positive = true;
		//      }
		//   }
		//   return negative && positive;
		//}

		//static bool FindNonConvexVertices( Vec3[] vertices, int[] indices )
		//{
		//   for( int nTriangle = 0; nTriangle < indices.Length / 3; nTriangle++ )
		//   {
		//      int index0 = indices[ nTriangle * 3 + 0 ];
		//      int index1 = indices[ nTriangle * 3 + 1 ];
		//      int index2 = indices[ nTriangle * 3 + 2 ];
		//      Vec3 vertex0 = vertices[ index0 ];
		//      Vec3 vertex1 = vertices[ index1 ];
		//      Vec3 vertex2 = vertices[ index2 ];

		//      Plane plane = Plane.FromPoints( vertex0, vertex1, vertex2 );
		//      if( IsExistsVerticesOnBothSidesOfPlane( plane, vertices, index0, index1, index2 ) )
		//         return true;
		//   }

		//   return false;
		//}

		static int FindVertexIndex( List<Vec3> vertices, Vec3 vertex, float epsilon )
		{
			for( int n = 0; n < vertices.Count; n++ )
			{
				if( vertices[ n ].Equals( vertex, epsilon ) )
					return n;
			}
			return -1;
		}

		static void MergeCloseVertices( Vec3[] vertices, int[] indices, float epsilon, out Vec3[] outVertices,
			out int[] outIndices )
		{
			List<Vec3> newVertices = new List<Vec3>( vertices.Length );
			List<int> newIndices = new List<int>( indices.Length );

			for( int index = 0; index < indices.Length; index++ )
			{
				Vec3 vertex = vertices[ indices[ index ] ];

				int newIndex = FindVertexIndex( newVertices, vertex, epsilon );
				if( newIndex != -1 )
				{
					newIndices.Add( newIndex );
				}
				else
				{
					newVertices.Add( vertex );
					newIndices.Add( newVertices.Count - 1 );
				}
			}

			outVertices = newVertices.ToArray();
			outIndices = newIndices.ToArray();
		}

		void PushToWorld()
		{
			if( Shapes.Length == 0 )
				return;

			shapesData = new ShapeData[ Shapes.Length ];

			//position, rotation
			Vec3 position = Position;
			Quat rotation = Rotation;
			nativeBody = PhysXNativeWorld.CreateBody( scene.nativeScene, Static, ref position, ref rotation );

			if( !Static )
			{
				PhysXNativeBody.SetLinearDamping( nativeBody, LinearDamping );
				PhysXNativeBody.SetAngularDamping( nativeBody, AngularDamping );

				Vec3 linearVelocity = LinearVelocity;
				PhysXNativeBody.SetLinearVelocity( nativeBody, ref linearVelocity );

				Vec3 angularVelocity = AngularVelocity;
				PhysXNativeBody.SetAngularVelocity( nativeBody, ref angularVelocity );

				if( !EnableGravity )
					PhysXNativeBody.SetGravity( nativeBody, false );

				UpdateMaxAngularVelocity();

				PhysXNativeBody.SetSolverIterationCounts( nativeBody, PhysX_SolverPositionIterations,
					PhysX_SolverVelocityIterations );

				if( PhysX_Kinematic )
					PhysXNativeBody.SetKinematicFlag( nativeBody, true );
			}

			int[] nativeShapesCount = new int[ Shapes.Length ];
			List<ushort> materialIndexes = new List<ushort>( Shapes.Length );

			//shapes

			float totalVolume = 0;
			if( MassMethod == MassMethods.Manually && !Static )
			{
				for( int nShape = 0; nShape < Shapes.Length; nShape++ )
					totalVolume += Shapes[ nShape ].Volume;
				if( totalVolume == 0 )
					totalVolume = .001f;
			}

			for( int nShape = 0; nShape < Shapes.Length; nShape++ )
			{
				Shape shape = Shapes[ nShape ];

				ShapeData shapeData = new ShapeData();
				shapesData[ nShape ] = shapeData;

				switch( shape.ShapeType )
				{
				case Shape.Type.Box:
					{
						BoxShape boxShape = (BoxShape)shape;
						CreateBoxShape( boxShape, totalVolume );
						nativeShapesCount[ nShape ] = 1;
					}
					break;

				case Shape.Type.Sphere:
					{
						SphereShape sphereShape = (SphereShape)shape;
						CreateSphereShape( sphereShape, totalVolume );
						nativeShapesCount[ nShape ] = 1;
					}
					break;

				case Shape.Type.Capsule:
					{
						CapsuleShape capsuleShape = (CapsuleShape)shape;
						CreateCapsuleShape( capsuleShape, totalVolume );
						nativeShapesCount[ nShape ] = 1;
					}
					break;

				case Shape.Type.Cylinder:
					Log.Warning( "PhysXBody: Cylinders are not supported by PhysX." );
					//skip creation of this shape
					continue;

				case Shape.Type.Mesh:
					{
						MeshShape meshShape = (MeshShape)shape;

						if( !Static && meshShape.MeshType == MeshShape.MeshTypes.TriangleMesh )
						{
							Log.Warning( "PhysXBody: Dynamic triangle meshes are not supported by PhysX. " +
								"You can consider using of convex hulls. See MeshShape.MeshType property." );
							//skip creation of this shape
							continue;
						}

						//get mesh geometry from cache
						PhysicsWorld._MeshGeometry geometry = meshShape._GetMeshGeometry();

						//skip creation of this shape
						if( geometry == null )
						{
							Log.Info( "PhysXBody: Mesh is not initialized. ({0}).", meshShape.MeshName );
							continue;
						}

						PhysXPhysicsWorld.MeshGeometryPhysXData data;

						if( geometry.UserData == null )
						{
							//generate MeshGeometryPhysXData data
							data = new PhysXPhysicsWorld.MeshGeometryPhysXData();

							if( meshShape.MeshType == MeshShape.MeshTypes.ConvexHullDecomposition ||
								meshShape.MeshType == MeshShape.MeshTypes.ConvexHull )
							{
								//convex mesh, convex mesh decomposition

								PhysicsWorld.ConvexHullDecompositionDataItem[] items;
								if( meshShape.MeshType == MeshShape.MeshTypes.ConvexHullDecomposition )
								{
									items = geometry.GetConvexHullDecompositionData();
									if( items == null )
									{
										Log.Warning( "PhysXBody: Unable to do convex hull decomposite for \"{0}\".",
											meshShape.MeshName );
										//skip creation of this shape
										continue;
									}
								}
								else
								{
									//Log.Warning( "START:" );
									//Log.Warning( "Before, vertices: {0}, triangles: {1}", geometry.Vertices.Length,
									//   geometry.Indices.Length / 3 );
									//if( FindNonConvexVertices( geometry.Vertices, geometry.Indices ) )
									//{
									//   Log.Warning( "FindNonConvexVertices found: " + meshShape.MeshName );
									//}

									Vec3[] vertices2;
									int[] indices2;
									float epsilon;
									{
										Bounds bounds = Bounds.Cleared;
										foreach( Vec3 vertex in geometry.Vertices )
											bounds.Add( vertex );
										Vec3 size = bounds.GetSize();
										float maxSide = Math.Max( Math.Max( size.X, size.Y ), size.Z );
										epsilon = maxSide / 10000;
									}
									MergeCloseVertices( geometry.Vertices, geometry.Indices, epsilon, out vertices2, out indices2 );

									//Log.Warning( "After, vertices: {0}, triangles: {1}", vertices2.Length, indices2.Length / 3 );

									items = new PhysicsWorld.ConvexHullDecompositionDataItem[] { 
										new PhysicsWorld.ConvexHullDecompositionDataItem( vertices2, indices2 ) };
								}

								data.convexMeshes = new IntPtr[ items.Length ];

								bool error = false;

								for( int nItem = 0; nItem < items.Length; nItem++ )
								{
									PhysicsWorld.ConvexHullDecompositionDataItem item = items[ nItem ];

									//create PxConvexMesh
									IntPtr pxConvexMesh = CreateConvexMesh( item );

									if( pxConvexMesh == IntPtr.Zero )
									{
										Log.Warning( "PhysXBody: Unable to cook a convex mesh for \"{0}\".",
											meshShape.MeshName );
										//skip creation of this shape
										error = true;
										break;
									}

									data.convexMeshes[ nItem ] = pxConvexMesh;
								}

								if( error )
								{
									//skip creation of this shape
									continue;
								}
							}
							else
							{
								//triangle mesh

								DateTime startTime = DateTime.Now;

								data.triangleMesh = CreateTriangleMesh( geometry );
								if( data.triangleMesh == IntPtr.Zero )
								{
									Log.Warning( "PhysXBody: Unable to cook a triangle mesh. ({0})", meshShape.MeshName );

									//skip creation of this shape
									continue;
								}

								cookTriangleMeshTotalTime += ( DateTime.Now - startTime ).TotalSeconds;
								//Log.Info( "total time: " + cookTriangleMeshTotalTime.ToString() );
							}

							geometry.UserData = data;
						}
						else
							data = (PhysXPhysicsWorld.MeshGeometryPhysXData)geometry.UserData;

						//add PhysX shapes
						if( data.convexMeshes != null )
						{
							//convex meshes
							for( int n = 0; n < data.convexMeshes.Length; n++ )
							{
								CreateConvexMeshShape( shape, data.convexMeshes[ n ], totalVolume );
							}
							nativeShapesCount[ nShape ] = data.convexMeshes.Length;
						}
						else
						{
							//triangle mesh
							CreateTriangleMeshShape( meshShape, data.triangleMesh, totalVolume );
							nativeShapesCount[ nShape ] = 1;
						}

						data.checkRefCounter++;

						shapeData.shapesMeshData = data;
					}
					break;

				case Shape.Type.HeightField:
					{
						HeightFieldShape heightFieldShape = (HeightFieldShape)shape;
						CreateHeightFieldShape( heightFieldShape, totalVolume );
						nativeShapesCount[ nShape ] = 1;
					}
					break;

				case Shape.Type._Custom1:
					{
						//create box as example
						_Custom1Shape boxShape = (_Custom1Shape)shape;
						CreateCustomShape( boxShape, totalVolume );
						nativeShapesCount[ nShape ] = 1;
					}
					break;
				}
			}

			if( !Static )
			{
				Vec3 centerOfMassPosition = CenterOfMassPosition;
				Quat centerOfMassRotation = CenterOfMassRotation;
				Vec3 inertiaTensorFactor = InertiaTensorFactor;
				PhysXNativeBody.SetMassAndInertia( nativeBody, CenterOfMassAuto, ref centerOfMassPosition,
					ref centerOfMassRotation, ref inertiaTensorFactor );
			}

			int totalPxShapes = 0;
			foreach( int count in nativeShapesCount )
				totalPxShapes += count;

			//no shapes
			if( totalPxShapes == 0 )
			{
				PhysXNativeWorld.DestroyBody( nativeBody );
				nativeBody = IntPtr.Zero;
				return;
			}

			//generate nativeShapes
			{
				int physXShapeCount = PhysXNativeBody.GetShapeCount( nativeBody );
				int currentShape = 0;
				for( int nShape = 0; nShape < Shapes.Length; nShape++ )
				{
					ShapeData shapeData = shapesData[ nShape ];
					int count = nativeShapesCount[ nShape ];
					shapeData.nativeShapes = new IntPtr[ count ];
					for( int n = 0; n < count; n++ )
					{
						shapeData.nativeShapes[ n ] = (IntPtr)PhysXNativeBody.GetShape( nativeBody, currentShape );
						currentShape++;
					}
				}
				if( physXShapeCount != currentShape )
					Log.Fatal( "PhysXBody: PushToWorld: physXShapeCount != currentShape." );
			}

			//configure shapes dictionary
			for( int n = 0; n < Shapes.Length; n++ )
			{
				Shape shape = Shapes[ n ];
				ShapeData shapeData = shapesData[ n ];

				int dictionaryIdentifier = PhysXPhysicsWorld.Instance.shapesDictionary.Add( shape );
				foreach( IntPtr pxShape in shapeData.nativeShapes )
					PhysXNativeShape.SetIdentifier( pxShape, dictionaryIdentifier );
			}

			//set contact pairs
			for( int n = 0; n < Shapes.Length; n++ )
			{
				Shape shape = Shapes[ n ];
				Dictionary<Shape, ShapePairFlags> list = shape._GetShapePairFlags();
				if( list != null )
				{
					ShapeData shapeData = shapesData[ n ];

					foreach( KeyValuePair<Shape, ShapePairFlags> pair in list )
					{
						Shape otherShape = pair.Key;
						ShapePairFlags flags = pair.Value;

						if( otherShape.Body.PushedToWorld && ( flags & ShapePairFlags.DisableContacts ) != 0 )
						{
							PhysXBody otherBody = (PhysXBody)otherShape.Body;

							if( otherBody.nativeBody != null )
							{
								ShapeData otherShapeData = otherBody.shapesData[ otherShape.BodyIndex ];
								foreach( IntPtr nativeShape in shapeData.nativeShapes )
								{
									foreach( IntPtr nativeOtherShape in otherShapeData.nativeShapes )
										PhysXNativeScene.SetShapePairFlags( scene.nativeScene, nativeShape, nativeOtherShape, true );
								}
							}
						}
					}
				}
			}

			PhysXNativeWrapper.PhysXNativeScene.AddBody( scene.nativeScene, nativeBody );

			if( !Static )
			{
				UpdateSleepiness();

				if( Sleeping )
					PhysXNativeBody.PutToSleep( nativeBody );
				else
					PhysXNativeBody.WakeUp( nativeBody, PhysXNativeWorld.SLEEP_INTERVAL );
			}

			if( CCD && !Static )
				PhysXNativeBody.EnableCCD( nativeBody, true );

			RecreateAttachedJoints();
		}

		void PopFromWorld()
		{
			//destroy PhysX joints
			if( RelatedJoints != null )
			{
				for( int n = 0; n < RelatedJoints.Count; n++ )
					PhysXPhysicsWorld.Instance.DestroyPhysXJoint( RelatedJoints[ n ] );
			}

			if( nativeBody != IntPtr.Zero )
			{
				foreach( ShapeData shapeData in shapesData )
				{
					if( shapeData.shapesMeshData != null )
					{
						shapeData.shapesMeshData.checkRefCounter--;
						if( shapeData.shapesMeshData.checkRefCounter < 0 )
							Log.Fatal( "PhysXBody: DestroyGeomDatas: Mesh geometry counter < 0" );
						shapeData.shapesMeshData = null;
					}
				}

				foreach( ShapeData shapeData in shapesData )
				{
					IntPtr nativeShape = shapeData.nativeShapes[ 0 ];
					int shapesDictionaryIndex = PhysXNativeShape.GetIdentifier( nativeShape );
					PhysXPhysicsWorld.Instance.shapesDictionary.Remove( shapesDictionaryIndex );
				}

				PhysXNativeScene.RemoveBody( scene.nativeScene, nativeBody );
				PhysXNativeWorld.DestroyBody( nativeBody );
				nativeBody = IntPtr.Zero;

				shapesData = null;
			}
		}

		protected override void OnUpdatePushedToWorld()
		{
			if( PushedToWorld )
				PushToWorld();
			else
				PopFromWorld();
		}

		protected override void ApplyForce( ForceType type, ref Vec3 vector, ref Vec3 pos )
		{
			if( nativeBody != IntPtr.Zero && !Static )
			{
				switch( type )
				{
				case ForceType.Local:
					PhysXNativeBody.AddLocalForce( nativeBody, ref vector );
					break;
				case ForceType.Global:
					PhysXNativeBody.AddForce( nativeBody, ref vector );
					break;
				case ForceType.LocalTorque:
					PhysXNativeBody.AddLocalTorque( nativeBody, ref vector );
					break;
				case ForceType.GlobalTorque:
					PhysXNativeBody.AddTorque( nativeBody, ref vector );
					break;
				case ForceType.LocalAtLocalPos:
					PhysXNativeBody.AddLocalForceAtLocalPos( nativeBody, ref vector, ref pos );
					break;
				case ForceType.LocalAtGlobalPos:
					PhysXNativeBody.AddLocalForceAtPos( nativeBody, ref vector, ref pos );
					break;
				case ForceType.GlobalAtLocalPos:
					PhysXNativeBody.AddForceAtLocalPos( nativeBody, ref vector, ref pos );
					break;
				case ForceType.GlobalAtGlobalPos:
					PhysXNativeBody.AddForceAtPos( nativeBody, ref vector, ref pos );
					break;
				}
			}
		}

		protected override void OnShapeSetContactGroup( Shape shape )
		{
			base.OnShapeSetContactGroup( shape );

			if( nativeBody != IntPtr.Zero )
			{
				ShapeData shapeData = shapesData[ shape.BodyIndex ];
				foreach( IntPtr nativeShape in shapeData.nativeShapes )
					PhysXNativeShape.SetContactGroup( nativeShape, shape.ContactGroup );
			}
		}

		protected override void OnSetEnableGravity()
		{
			if( nativeBody != IntPtr.Zero && !Static )
				PhysXNativeBody.SetGravity( nativeBody, EnableGravity );
		}

		protected override void OnShapeSetMaterialProperty( Shape shape )
		{
			if( nativeBody != IntPtr.Zero )
			{
				ShapeData shapeData = shapesData[ shape.BodyIndex ];
				foreach( IntPtr nativeShape in shapeData.nativeShapes )
				{
					IntPtr material = CreateMaterial( shape );
					PhysXNativeShape.SetMaterialFreeOldMaterial( nativeShape, 0, material );
					PhysXNativeShape.UpdatePhysXShapeMaterials( nativeShape );
				}
			}
		}

		internal void UpdateMaxAngularVelocity()
		{
			if( !Static && nativeBody != IntPtr.Zero )
				PhysXNativeBody.SetMaxAngularVelocity( nativeBody, PhysXPhysicsWorld.Instance.MaxAngularVelocity );
		}

		void GetCookTriangleMeshSourceBuffer( Vec3[] vertices, int[] indices, short[] materialIndices,
			out byte[] sourceBuffer, out string baseName )
		{
			int verticesSize = vertices.Length * 12/*sizeof( Vec3 )*/;
			int indicesSize = indices.Length * sizeof( int );
			int materialIndicesSize = 0;
			if( materialIndices != null )
				materialIndicesSize = materialIndices.Length * sizeof( short );

			sourceBuffer = new byte[ verticesSize + indicesSize + materialIndicesSize ];
			unsafe
			{
				fixed( byte* pBuffer = sourceBuffer )
				{
					fixed( Vec3* pVertices = vertices )
						NativeUtils.CopyMemory( (IntPtr)pBuffer, (IntPtr)pVertices, verticesSize );
					fixed( int* pIndices = indices )
						NativeUtils.CopyMemory( (IntPtr)( pBuffer + verticesSize ), (IntPtr)pIndices, indicesSize );
					if( materialIndices != null )
					{
						fixed( short* pMaterialIndices = materialIndices )
						{
							NativeUtils.CopyMemory( (IntPtr)( pBuffer + verticesSize + indicesSize ),
								(IntPtr)pMaterialIndices, materialIndicesSize );
						}
					}
				}
			}

			string md5HashString;
			using( MD5 md5 = new MD5CryptoServiceProvider() )
			{
				byte[] md5Hash = md5.ComputeHash( sourceBuffer );
				StringBuilder builder = new StringBuilder( md5Hash.Length * 2 );
				foreach( byte c in md5Hash )
					builder.Append( c.ToString( "x2" ) );
				md5HashString = builder.ToString();
			}
			baseName = md5HashString;
		}

		byte[] GetCookedTriangleMeshFromCache( Vec3[] vertices, int[] indices, short[] materialIndices )
		{
			try
			{
				string cacheDirectory = "user:Caches\\PhysXCookedTriangleMeshes";
				string realCacheDirectory = VirtualFileSystem.GetRealPathByVirtual( cacheDirectory );

				if( Directory.Exists( realCacheDirectory ) )
				{
					byte[] sourceBuffer;
					string baseName;
					GetCookTriangleMeshSourceBuffer( vertices, indices, materialIndices, out sourceBuffer, out baseName );
					string realFileName = Path.Combine( realCacheDirectory, baseName + ".cache" );

					if( File.Exists( realFileName ) )
					{
						int major, minor, bugfix;
						PhysXNativeWorld.GetSDKVersion( out major, out minor, out bugfix );
						int additional = 0;
						int version = ( ( ( ( ( major << 8 ) + minor ) << 8 ) + bugfix ) << 8 ) + additional;

						//File format:
						//1. PhysX version (4 bytes)
						//2. source buffer length (4 bytes)
						//3. source buffer
						//4. cooked data length (4 bytes)
						//5. cooked data

						//no archive
						byte[] fileData = File.ReadAllBytes( realFileName );

						//archive
						//byte[] fileData;
						////Mono runtime specific
						//if( RuntimeFramework.Runtime == RuntimeFramework.RuntimeType.Mono )
						//   ZipConstants.DefaultCodePage = 0;
						//using( ZipFile zipFile = new ZipFile( realFileName ) )
						//{
						//   ZipEntry entry = zipFile.GetEntry( "data" );
						//   Stream zipStream = zipFile.GetInputStream( entry );
						//   fileData = new byte[ entry.Size ];
						//   if( zipStream.Read( fileData, 0, (int)entry.Size ) != fileData.Length )
						//      return null;
						//}

						ReceiveDataReader reader = new ReceiveDataReader();
						reader.Init( fileData, 0, fileData.Length * 8 );

						//check for old version
						int fileVersion = reader.ReadInt32();
						if( version != fileVersion )
							return null;

						int fileBufferSize = reader.ReadInt32();
						if( sourceBuffer.Length != fileBufferSize )
							return null;

						byte[] fileSourceBuffer = new byte[ fileBufferSize ];
						reader.ReadBuffer( fileSourceBuffer );
						unsafe
						{
							fixed( byte* pSource = sourceBuffer, pFileBuffer = fileSourceBuffer )
							{
								if( NativeUtils.CompareMemory( (IntPtr)pSource, (IntPtr)pFileBuffer,
									sourceBuffer.Length ) != 0 )
								{
									return null;
								}
							}
						}

						int cookedSize = reader.ReadInt32();
						byte[] cookedData = new byte[ cookedSize ];
						reader.ReadBuffer( cookedData );

						return cookedData;
					}
				}
			}
			catch { }

			return null;
		}

		void WriteCookedTriangleMeshToCache( Vec3[] vertices, int[] indices, short[] materialIndices, byte[] cookedData )
		{
			try
			{
				string cacheDirectory = "user:Caches\\PhysXCookedTriangleMeshes";
				string realCacheDirectory = VirtualFileSystem.GetRealPathByVirtual( cacheDirectory );

				if( !Directory.Exists( realCacheDirectory ) )
					Directory.CreateDirectory( realCacheDirectory );

				byte[] sourceBuffer;
				string baseName;
				GetCookTriangleMeshSourceBuffer( vertices, indices, materialIndices, out sourceBuffer, out baseName );
				string realFileName = Path.Combine( realCacheDirectory, baseName + ".cache" );

				int major, minor, bugfix;
				PhysXNativeWorld.GetSDKVersion( out major, out minor, out bugfix );
				int additional = 0;
				int version = ( ( ( ( ( major << 8 ) + minor ) << 8 ) + bugfix ) << 8 ) + additional;

				SendDataWriter writer = new SendDataWriter();
				writer.Write( version );
				writer.Write( sourceBuffer.Length );
				writer.Write( sourceBuffer );
				writer.Write( cookedData.Length );
				writer.Write( cookedData );

				//no archive
				using( FileStream stream = new FileStream( realFileName, FileMode.Create ) )
				{
					stream.Write( writer.Data, 0, writer.BitLength / 8 );
				}

				//archive
				//FileStream outFileStream = File.Create( realFileName );
				//using( ZipOutputStream zipStream = new ZipOutputStream( outFileStream ) )
				//{
				//   zipStream.SetLevel( 9 ); // 0 - store only to 9 - means best compression

				//   ZipEntry entry = new ZipEntry( "data" );
				//   entry.Flags |= (int)GeneralBitFlags.UnicodeText;
				//   entry.Size = writer.BitLength / 8;
				//   entry.DateTime = DateTime.Now;

				//   zipStream.PutNextEntry( entry );
				//   zipStream.Write( writer.Data, 0, writer.BitLength / 8 );
				//   zipStream.Finish();
				//   zipStream.Close();
				//}
			}
			catch { }
		}

		protected override void OnUpdatePhysXSolverInterationCounts()
		{
			base.OnUpdatePhysXSolverInterationCounts();
			if( !Static )
			{
				PhysXNativeBody.SetSolverIterationCounts( nativeBody, PhysX_SolverPositionIterations,
					PhysX_SolverVelocityIterations );
			}
		}

		public override object CallCustomMethod( string message, object param )
		{
			return base.CallCustomMethod( message, param );
		}

		protected override void OnUpdatePhysXKinematic()
		{
			base.OnUpdatePhysXKinematic();

			if( nativeBody != IntPtr.Zero )
				PhysXNativeBody.SetKinematicFlag( nativeBody, PhysX_Kinematic );
		}

		protected override void OnPhysXSetKinematicTarget( Vec3 pos, Quat rot )
		{
			base.OnPhysXSetKinematicTarget( pos, rot );

			if( nativeBody != IntPtr.Zero )
				PhysXNativeBody.SetKinematicTarget( nativeBody, ref pos, ref rot );
		}
	}
}
