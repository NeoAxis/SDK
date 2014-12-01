// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Utils;

namespace ODEPhysicsSystem
{
	using dReal = System.Single;
	using dWorldID = System.IntPtr;
	using dBodyID = System.IntPtr;
	using dJointID = System.IntPtr;
	using dJointGroupID = System.IntPtr;
	using dGeomID = System.IntPtr;
	using dSpaceID = System.IntPtr;
	using dTriMeshDataID = System.IntPtr;

	//

	sealed class ODEBody : Body
	{
		static List<GeomData> tempGeomDatasAsList = new List<GeomData>();
		static bool notSupportedMeshesLogInformed;

		///////////////////////////////////////////

		internal ODEPhysicsScene scene;

		internal IntPtr bodyData;
		internal dBodyID bodyID;
		GeomData[] geomDatas;

		//// True if the ODEBody has a non-symmetric inertia tensor.
		//bool nonSymmetricInertia;

		//// Used to improve ODE's angular velocity calculations for objects 
		//// with non-symmetric inertia tensors.
		//internal bool freelySpinning = true;

		//// Used to improve ODE's angular velocity calculations for objects 
		//// with non-symmetric inertia tensors.
		//float prevAngVelMagSquared;

		internal float ccdRadius = -1;
		internal Vec3 ccdLastPosition;

		///////////////////////////////////////////

		internal class GeomData
		{
			public Shape shape;
			public ODEBody odeBody;//optimization
			public dGeomID geomID;
			public dSpaceID spaceID;
			public dGeomID transformID;
			public ODEPhysicsWorld.MeshGeometryODEData meshGeometryODEData;
			public int shapeDictionaryIndex;
		}

		///////////////////////////////////////////

		public ODEBody( PhysicsScene scene )
			: base( scene )
		{
			this.scene = (ODEPhysicsScene)scene;
		}

		protected override void OnSetSleepiness()
		{
			if( !Static )
				UpdateSleepiness();
		}

		void UpdateSleepiness()
		{
			if( bodyID == IntPtr.Zero )
				return;

			Ode.dBodySetAutoDisableFlag( bodyID, ( Sleepiness == 0 ) ? 0 : 1 );

			// As value goes from 0.0 to 1.0:
			// AutoDisableLinearThreshold goes from min to max,
			// AutoDisableAngularThreshold goes from min to max,
			// AutoDisableSteps goes from max to min,
			// AutoDisableTime goes from max to min.

			float range = Defines.autoDisableLinearMax - Defines.autoDisableLinearMin;
			Ode.dBodySetAutoDisableLinearThreshold( bodyID,
				Sleepiness * range + Defines.autoDisableLinearMin );

			range = Defines.autoDisableAngularMax - Defines.autoDisableAngularMin;
			Ode.dBodySetAutoDisableAngularThreshold( bodyID,
				Sleepiness * range + Defines.autoDisableAngularMin );

			range = ( Defines.autoDisableStepsMax - Defines.autoDisableStepsMin );
			Ode.dBodySetAutoDisableSteps( bodyID,
				(int)( Defines.autoDisableStepsMax - Sleepiness * range ) );

			range = Defines.autoDisableTimeMax - Defines.autoDisableTimeMin;
			Ode.dBodySetAutoDisableTime( bodyID, Defines.autoDisableTimeMax - Sleepiness * range );
		}

		protected override void OnSetTransform( bool updatePosition, bool updateRotation )
		{
			base.OnSetTransform( updatePosition, updateRotation );

			if( PushedToWorld )
			{
				if( !Static )
				{
					if( bodyID != IntPtr.Zero )
					{
						if( updatePosition )
							Ode.dBodySetPosition( bodyID, Position.X, Position.Y, Position.Z );
						if( updateRotation )
						{
							Ode.dQuaternion odeQuat;
							Convert.ToODE( Rotation, out odeQuat );
							Ode.dBodySetQuaternion( bodyID, ref odeQuat );
						}
					}
				}
				else
					UpdateStaticBodyGeomsTransform();
			}
		}

		protected override void OnSetLinearVelocity()
		{
			if( PushedToWorld && bodyID != IntPtr.Zero )
			{
				Ode.dBodySetLinearVel( bodyID, LinearVelocity.X, LinearVelocity.Y, LinearVelocity.Z );
				//freelySpinning = false;
			}
		}

		protected override void OnSetAngularVelocity()
		{
			if( PushedToWorld && bodyID != IntPtr.Zero )
			{
				Ode.dBodySetAngularVel( bodyID, AngularVelocity.X, AngularVelocity.Y, AngularVelocity.Z );
				//freelySpinning = false;
			}
		}

		protected override void OnSetSleeping()
		{
			if( PushedToWorld && bodyID != IntPtr.Zero )
			{
				if( Sleeping )
				{
					ClearForces();
					Ode.dBodyDisable( bodyID );
					LinearVelocity = Vec3.Zero;
					AngularVelocity = Vec3.Zero;
				}
				else
					Ode.dBodyEnable( bodyID );
			}
		}

		void PushToWorld()
		{
			if( !Static )
			{
				bodyID = Ode.dBodyCreate( scene.worldID );

				UpdateSleepiness();

				Ode.dBodySetPosition( bodyID, Position.X, Position.Y, Position.Z );

				Ode.dQuaternion odeQuat;
				Convert.ToODE( Rotation, out odeQuat );
				Ode.dBodySetQuaternion( bodyID, ref odeQuat );

				Ode.dBodySetLinearVel( bodyID, LinearVelocity.X, LinearVelocity.Y, LinearVelocity.Z );
				Ode.dBodySetAngularVel( bodyID, AngularVelocity.X, AngularVelocity.Y, AngularVelocity.Z );

				if( Sleeping )
					Ode.dBodyDisable( bodyID );
				else
					Ode.dBodyEnable( bodyID );

				if( !EnableGravity )
					Ode.dBodySetGravityMode( bodyID, 0 );
			}

			bodyData = Ode.CreateBodyData( bodyID );

			CreateGeomDatas();

			//no shapes
			if( geomDatas == null || geomDatas.Length == 0 )
			{
				PopFromWorld();
				return;
			}

			if( !Static )
				CalculateBodyMass();

			RecreateAttachedJoints();
		}

		void PopFromWorld()
		{
			//destroy ODE joints
			if( RelatedJoints != null )
			{
				for( int n = 0; n < RelatedJoints.Count; n++ )
					ODEPhysicsWorld.Instance.DestroyODEJoint( RelatedJoints[ n ] );
			}

			DestroyGeomDatas();

			if( bodyID != IntPtr.Zero )
			{
				Ode.dBodyDestroy( bodyID );
				bodyID = IntPtr.Zero;
			}

			if( bodyData != IntPtr.Zero )
			{
				Ode.DestroyBodyData( bodyData );
				bodyData = IntPtr.Zero;
			}
		}

		bool AreEqual( float x, float y )
		{
			float maxVal = 1;
			if( Math.Abs( x ) > maxVal )
				maxVal = Math.Abs( x );
			if( Math.Abs( y ) > maxVal )
				maxVal = Math.Abs( y );
			return Math.Abs( x - y ) <= 0.000001f * maxVal;
		}

		void CalculateBodyMass()
		{
			float totalVolume = 0;
			if( MassMethod == MassMethods.Manually )
			{
				for( int nShape = 0; nShape < Shapes.Length; nShape++ )
					totalVolume += Shapes[ nShape ].Volume;
				if( totalVolume == 0 )
					totalVolume = .001f;
			}

			Ode.dMass bodyMass = new Ode.dMass();
			bool bodyMassInitialized = false;

			for( int nShape = 0; nShape < Shapes.Length; nShape++ )
			{
				Shape shape = Shapes[ nShape ];

				Ode.dMass shapeMass = new Ode.dMass();

				float shapeDensity;
				if( MassMethod == MassMethods.Manually )
					shapeDensity = Mass / totalVolume;
				else
					shapeDensity = shape.Density;

				if( shapeDensity <= 0 )
					shapeDensity = .0001f;

				switch( shape.ShapeType )
				{
				case Shape.Type.Box:
					{
						BoxShape boxShape = (BoxShape)shape;
						Ode.dMassSetBox( ref shapeMass, shapeDensity, boxShape.Dimensions.X,
							boxShape.Dimensions.Y, boxShape.Dimensions.Z );
					}
					break;

				case Shape.Type.Sphere:
					{
						SphereShape sphereShape = (SphereShape)shape;
						Ode.dMassSetSphere( ref shapeMass, shapeDensity, sphereShape.Radius );
					}
					break;

				case Shape.Type.Capsule:
					{
						CapsuleShape capsuleShape = (CapsuleShape)shape;
						Ode.dMassSetCapsule( ref shapeMass, shapeDensity, 3, capsuleShape.Radius,
							capsuleShape.Length );
					}
					break;

				case Shape.Type.Cylinder:
					{
						CylinderShape cylinderShape = (CylinderShape)shape;
						Ode.dMassSetCylinder( ref shapeMass, shapeDensity, 3, cylinderShape.Radius,
							cylinderShape.Length );
					}
					break;

				case Shape.Type.Mesh:
					{
						GeomData geomData = geomDatas[ nShape ];

						//ignore this shape
						if( geomData == null )
							continue;

						IntPtr geomID;

						if( geomData.transformID == dGeomID.Zero )
							geomID = geomData.geomID;
						else
							geomID = geomData.transformID;

						//ignore this shape
						if( geomID == dGeomID.Zero )
							continue;

						Ode.Aabb aabb = new Ode.Aabb();
						Ode.dGeomGetAABB( geomID, ref aabb );
						Ode.dMassSetBox( ref shapeMass, shapeDensity, aabb.maxx - aabb.minx,
							aabb.maxy - aabb.miny, aabb.maxz - aabb.minz );

						//correct
						shapeMass.mass = shape.Volume * shapeDensity;
						if( shapeMass.mass <= 0 )
							shapeMass.mass = .001f;
					}
					break;

				default:
					Trace.Assert( false );
					break;
				}

				if( shape.Rotation != Quat.Identity )
				{
					Mat3 mat3;
					shape.Rotation.ToMat3( out mat3 );
					Ode.dMatrix3 odeMat3;
					Convert.ToODE( ref mat3, out odeMat3 );
					Ode.dMassRotate( ref shapeMass, ref odeMat3 );
				}

				if( shape.Position != Vec3.Zero )
				{
					Ode.dMassTranslate( ref shapeMass, shape.Position.X,
						shape.Position.Y, shape.Position.Z );
				}

				if( !bodyMassInitialized )
				{
					bodyMass = shapeMass;
					bodyMassInitialized = true;
				}
				else
					Ode.dMassAdd( ref bodyMass, ref shapeMass );
			}

			if( MassMethod == MassMethods.Manually )
			{
				bodyMass.mass = Mass;
				if( bodyMass.mass <= 0 )
					bodyMass.mass = .0001f;
			}

			if( bodyMass.mass != 0 )
			{
				//if( CenterOfMassAuto )
				//   Log.Warning( "ODEBody: CenterOfMassAuto is not supported on ODE physics." );

				//!!!!!!тут вручную введенное положение цента масс

				Ode.dMassTranslate( ref bodyMass, -bodyMass.c.X, -bodyMass.c.Y, -bodyMass.c.Z );
				Ode.dBodySetMass( bodyID, ref bodyMass );
			}

			////calculate mNonSymmetricInertia
			//nonSymmetricInertia =
			//   !AreEqual( bodyMass.I.M00, bodyMass.I.M11 ) ||
			//   !AreEqual( bodyMass.I.M11, bodyMass.I.M22 );
		}

		void CreateGeomDatas()
		{
			tempGeomDatasAsList.Clear();
			for( int n = 0; n < Shapes.Length; n++ )
				tempGeomDatasAsList.Add( null );

			dSpaceID bodySpaceID = scene.rootSpaceID;

			for( int nShape = 0; nShape < Shapes.Length; nShape++ )
			{
				Shape shape = Shapes[ nShape ];

				GeomData geomData = new GeomData();
				geomData.shape = shape;
				geomData.odeBody = (ODEBody)shape.Body;

				bool identityTransform = shape.Position == Vec3.Zero && shape.Rotation == Quat.Identity;

				// No offset transform.
				if( identityTransform )
					geomData.spaceID = bodySpaceID;

				//create geom

				switch( shape.ShapeType )
				{
				case Shape.Type.Box:
					{
						BoxShape boxShape = (BoxShape)shape;
						geomData.geomID = Ode.dCreateBox( geomData.spaceID, boxShape.Dimensions.X,
							boxShape.Dimensions.Y, boxShape.Dimensions.Z );
					}
					break;

				case Shape.Type.Sphere:
					{
						SphereShape sphereShape = (SphereShape)shape;
						geomData.geomID = Ode.dCreateSphere( geomData.spaceID, sphereShape.Radius );
					}
					break;

				case Shape.Type.Capsule:
					{
						CapsuleShape capsuleShape = (CapsuleShape)shape;
						geomData.geomID = Ode.dCreateCapsule( geomData.spaceID, capsuleShape.Radius,
							capsuleShape.Length );
					}
					break;

				case Shape.Type.Cylinder:
					{
						CylinderShape cylinderShape = (CylinderShape)shape;
						geomData.geomID = Ode.dCreateCylinder( geomData.spaceID, cylinderShape.Radius,
							cylinderShape.Length );
					}
					break;

				case Shape.Type.Mesh:
					{
						MeshShape meshShape = (MeshShape)shape;

						if( !Static )
						{
							if( !notSupportedMeshesLogInformed )
							{
								notSupportedMeshesLogInformed = true;
								Log.Warning( "ODEBody: Dynamic convex and triangle meshes are not " +
									"supported by ODE." );
							}
							Log.Info( "ODEBody: Dynamic convex and triangle meshes are not " +
								"supported by ODE." );

							//ignore shape
							continue;
						}

						//get mesh geometry from cache
						PhysicsWorld._MeshGeometry geometry = meshShape._GetMeshGeometry();

						//ignore shape
						if( geometry == null )
						{
							Log.Info( "ODEBody: Mesh is not initialized. ({0}).", meshShape.MeshName );
							continue;
						}

						ODEPhysicsWorld.MeshGeometryODEData data;

						if( geometry.UserData == null )
						{
							data = new ODEPhysicsWorld.MeshGeometryODEData();

							//generate MeshGeometryODEData data
							data.triMeshDataID = Ode.dGeomTriMeshDataCreate();

							data.verticesCount = geometry.Vertices.Length;
							data.indicesCount = geometry.Indices.Length;

							data.vertices = (IntPtr)Ode.dAlloc( (uint)
								( Marshal.SizeOf( typeof( float ) ) * 3 * data.verticesCount ) );
							data.indices = (IntPtr)Ode.dAlloc( (uint)
								( Marshal.SizeOf( typeof( int ) ) * data.indicesCount ) );

							unsafe
							{
								fixed( Vec3* source = geometry.Vertices )
								{
									NativeUtils.CopyMemory( data.vertices, (IntPtr)source,
										data.verticesCount * sizeof( Vec3 ) );
								}
								fixed( int* source = geometry.Indices )
								{
									NativeUtils.CopyMemory( data.indices, (IntPtr)source,
										data.indicesCount * sizeof( int ) );
								}
							}

							//build ode tri mesh data
							Ode.dGeomTriMeshDataBuildSingleAsIntPtr(
								data.triMeshDataID,
								data.vertices,
								Marshal.SizeOf( typeof( float ) ) * 3,
								data.verticesCount,
								data.indices,
								data.indicesCount,
								Marshal.SizeOf( typeof( int ) ) * 3 );

							geometry.UserData = data;
						}
						else
							data = (ODEPhysicsWorld.MeshGeometryODEData)geometry.UserData;

						data.checkRefCounter++;

						geomData.meshGeometryODEData = data;

						geomData.geomID = Ode.dCreateTriMesh( geomData.spaceID,
							data.triMeshDataID, null, null, null );

						Ode.SetGeomTriMeshSetRayCallback( geomData.geomID );

						//unsafe
						//{

						//   float[] planes = new float[]
						//      {
						//         1.0f ,0.0f ,0.0f ,0.25f,
						//         0.0f ,1.0f ,0.0f ,0.25f,
						//         0.0f ,0.0f ,1.0f ,0.25f,
						//         -1.0f,0.0f ,0.0f ,0.25f,
						//         0.0f ,-1.0f,0.0f ,0.25f,
						//         0.0f ,0.0f ,-1.0f,0.25f
						//      };

						//   float[] points = new float[]
						//      {
						//         0.25f,0.25f,0.25f,  
						//         -0.25f,0.25f,0.25f, 

						//         0.25f,-0.25f,0.25f, 
						//         -0.25f,-0.25f,0.25f,

						//         0.25f,0.25f,-0.25f, 
						//         -0.25f,0.25f,-0.25f,

						//         0.25f,-0.25f,-0.25f,
						//         -0.25f,-0.25f,-0.25f,
						//      };

						//   uint[] polygons = new uint[]
						//      {
						//         4,0,2,6,4, 
						//         4,1,0,4,5, 
						//         4,0,1,3,2, 
						//         4,3,1,5,7, 
						//         4,2,3,7,6, 
						//         4,5,4,6,7, 
						//      };

						//   float* nativePlanes = (float*)Ode.dAlloc( (uint)( sizeof( float ) * planes.Length ) );
						//   for( int n = 0; n < planes.Length; n++ )
						//      nativePlanes[ n ] = planes[ n ];

						//   uint planeCount = 6;

						//   float* nativePoints = (float*)Ode.dAlloc( (uint)( sizeof( float ) * points.Length ) );
						//   for( int n = 0; n < points.Length; n++ )
						//      nativePoints[ n ] = points[ n ];

						//   uint pointCount = 8;

						//   uint* nativePolygons = (uint*)Ode.dAlloc( (uint)( sizeof( uint ) * polygons.Length ) );
						//   for( int n = 0; n < polygons.Length; n++ )
						//      nativePolygons[ n ] = polygons[ n ];

						//   //ODEPhysicsWorld.MeshGeometryODEData data;

						//   //if( geometry.UserData == null )
						//   //{
						//   //   data = new ODEPhysicsWorld.MeshGeometryODEData();
						//   //}

						//   geomData.geomID = Ode.dCreateConvex( geomData.spaceID, nativePlanes,
						//      planeCount, nativePoints, pointCount, nativePolygons );
						//}
					}
					break;
				}

				//add geom data to list
				tempGeomDatasAsList[ nShape ] = geomData;

				geomData.shape = shape;
				geomData.odeBody = (ODEBody)shape.Body;

				// Use ODE's geom transform object.
				if( !identityTransform )
					geomData.transformID = Ode.dCreateGeomTransform( bodySpaceID );

				//set geom to body
				if( !Static )
				{
					if( geomData.transformID == dGeomID.Zero )
						Ode.dGeomSetBody( geomData.geomID, bodyID );
					else
						Ode.dGeomSetBody( geomData.transformID, bodyID );
				}

				if( geomData.transformID != dGeomID.Zero )
				{
					// Setup geom transform.

					Ode.dGeomTransformSetGeom( geomData.transformID, geomData.geomID );

					Ode.dQuaternion odeQuat;
					Convert.ToODE( shape.Rotation, out odeQuat );
					Ode.dGeomSetQuaternion( geomData.geomID, ref odeQuat );

					Ode.dGeomSetPosition( geomData.geomID, shape.Position.X,
						shape.Position.Y, shape.Position.Z );
				}

				// Set the GeomData reference for later use (e.g. in collision handling).
				geomData.shapeDictionaryIndex = scene.shapesDictionary.Add( geomData );

				dGeomID geomID = geomData.transformID != dGeomID.Zero ?
					geomData.transformID : geomData.geomID;
				Ode.CreateShapeData( geomID, bodyData, geomData.shapeDictionaryIndex,
					shape.ShapeType == Shape.Type.Mesh, shape.ContactGroup,
					shape.Hardness, shape.Restitution, shape.DynamicFriction, shape.StaticFriction );

				//shape pair flags
				Dictionary<Shape, ShapePairFlags> list = shape._GetShapePairFlags();
				if( list != null )
				{
					foreach( KeyValuePair<Shape, ShapePairFlags> pair in list )
					{
						Shape otherShape = pair.Key;
						ShapePairFlags flags = pair.Value;

						if( ( flags & ShapePairFlags.DisableContacts ) != 0 )
						{
							ODEBody otherBody = (ODEBody)otherShape.Body;

							GeomData otherGeomData = otherBody.GetGeomDataByShape( otherShape );
							if( otherGeomData != null )
							{
								dGeomID otherGeomID = ( otherGeomData.transformID != dGeomID.Zero ) ?
									otherGeomData.transformID : otherGeomData.geomID;
								Ode.SetShapePairDisableContacts( geomID, otherGeomID, true );
							}
						}
					}
				}
			}

			geomDatas = tempGeomDatasAsList.ToArray();
			tempGeomDatasAsList.Clear();

			if( Static )
				UpdateStaticBodyGeomsTransform();
		}

		void UpdateStaticBodyGeomsTransform()
		{
			if( geomDatas != null )
			{
				foreach( GeomData geomData in geomDatas )
				{
					if( geomData == null )
						continue;

					Ode.dQuaternion odeQuat;
					Convert.ToODE( Rotation, out odeQuat );

					dGeomID geom = ( geomData.transformID != dGeomID.Zero ) ?
						geomData.transformID : geomData.geomID;

					Ode.dGeomSetQuaternion( geom, ref odeQuat );
					Ode.dGeomSetPosition( geom, Position.X, Position.Y, Position.Z );
				}
			}
		}

		void DestroyGeomDatas()
		{
			if( geomDatas != null )
			{
				foreach( GeomData geomData in geomDatas )
				{
					if( geomData == null )
						continue;

					dGeomID geomID = geomData.transformID != dGeomID.Zero ?
						geomData.transformID : geomData.geomID;
					Ode.DestroyShapeData( geomID );

					scene.shapesDictionary.Remove( geomData.shapeDictionaryIndex );

					if( geomData.transformID != dGeomID.Zero )
						Ode.dGeomDestroy( geomData.transformID );

					if( geomData.meshGeometryODEData != null )
					{
						geomData.meshGeometryODEData.checkRefCounter--;
						if( geomData.meshGeometryODEData.checkRefCounter < 0 )
							Log.Fatal( "ODEBody: DestroyGeomDatas: Mesh geometry counter < 0." );
					}

					Ode.dGeomDestroy( geomData.geomID );
				}
			}
			geomDatas = null;
		}

		protected override void OnUpdatePushedToWorld()
		{
			if( PushedToWorld )
				PushToWorld();
			else
				PopFromWorld();
			ccdRadius = -1;
		}

		unsafe internal void UpdateDataFromLibrary()
		{
			if( bodyID == IntPtr.Zero )
				return;

			bool sleeping = Ode.dBodyIsEnabled( bodyID ) == 0;
			if( !sleeping || !Sleeping )
			{
				Vec3 pos;
				Convert.ToNet( Ode.dBodyGetPosition_( bodyID ), out pos );
				Quat rot;
				Convert.ToNet( Ode.dBodyGetQuaternion_( bodyID ), out rot );
				Vec3 linearVel;
				Convert.ToNet( Ode.dBodyGetLinearVel_( bodyID ), out linearVel );
				Vec3 angularVel;
				Convert.ToNet( Ode.dBodyGetAngularVel_( bodyID ), out angularVel );

				UpdateDataFromLibrary( ref pos, ref rot, ref linearVel, ref angularVel, sleeping );
			}
		}

		//public override Mat3 GetInertiaTensor()
		//{
		//   if( Static )
		//      return Mat3.Identity;
		//   if( bodyID == dBodyID.Zero )
		//      return Mat3.Zero;

		//   Ode.dMass mass = new Ode.dMass();
		//   Ode.dBodyGetMass( bodyID, ref mass );

		//   return new Mat3(
		//      mass.I.M00, mass.I.M01, mass.I.M02,
		//      mass.I.M10, mass.I.M11, mass.I.M12,
		//      mass.I.M20, mass.I.M21, mass.I.M22 );
		//}

		protected override void ApplyForce( ForceType type, ref Vec3 vector, ref Vec3 pos )
		{
			if( bodyID == dBodyID.Zero )
				return;

			Ode.dBodyEnable( bodyID );

			switch( type )
			{
			case ForceType.Local:
				Ode.dBodyAddRelForce( bodyID, vector.X, vector.Y, vector.Z );
				break;
			case ForceType.Global:
				Ode.dBodyAddForce( bodyID, vector.X, vector.Y, vector.Z );
				break;
			case ForceType.LocalTorque:
				Ode.dBodyAddRelTorque( bodyID, vector.X, vector.Y, vector.Z );
				break;
			case ForceType.GlobalTorque:
				Ode.dBodyAddTorque( bodyID, vector.X, vector.Y, vector.Z );
				break;
			case ForceType.LocalAtLocalPos:
				Ode.dBodyAddRelForceAtRelPos( bodyID, vector.X, vector.Y, vector.Z, pos.X, pos.Y, pos.Z );
				break;
			case ForceType.LocalAtGlobalPos:
				Ode.dBodyAddRelForceAtPos( bodyID, vector.X, vector.Y, vector.Z, pos.X, pos.Y, pos.Z );
				break;
			case ForceType.GlobalAtLocalPos:
				Ode.dBodyAddForceAtRelPos( bodyID, vector.X, vector.Y, vector.Z, pos.X, pos.Y, pos.Z );
				break;
			case ForceType.GlobalAtGlobalPos:
				Ode.dBodyAddForceAtPos( bodyID, vector.X, vector.Y, vector.Z, pos.X, pos.Y, pos.Z );
				break;
			}

			//// Invalidate the "freely-spinning" parameter.
			//freelySpinning = false;
		}

		internal void DoDamping()
		{
			if( bodyID == dBodyID.Zero )
				return;

			Ode.dMass mass = new Ode.dMass();
			Ode.dBodyGetMass( bodyID, ref mass );

			// Linear damping
			if( LinearDamping != 0 )
			{
				// The damping force depends on the damping amount, mass, and velocity 
				// (i.e. damping amount and momentum).
				float factor = -LinearDamping * mass.mass;
				Vec3 force = LinearVelocity * factor;

				// Add a global force opposite to the global linear velocity.
				Ode.dBodyAddForce( bodyID, force.X, force.Y, force.Z );
			}

			// Angular damping
			if( AngularDamping != 0 )
			{
				Vec3 localVelocity;
				{
					Ode.dVector3 aVelLocal = new Ode.dVector3();
					Ode.dBodyVectorFromWorld( bodyID, AngularVelocity.X, AngularVelocity.Y,
						AngularVelocity.Z, ref aVelLocal );
					localVelocity = Convert.ToNet( aVelLocal );
				}

				// The damping force depends on the damping amount, mass, and velocity 
				// (i.e. damping amount and momentum).
				float factor = -AngularDamping;

				Vec3 momentum = new Vec3(
					Vec3.Dot( new Vec3( mass.I.M00, mass.I.M01, mass.I.M02 ), localVelocity ),
					Vec3.Dot( new Vec3( mass.I.M10, mass.I.M11, mass.I.M12 ), localVelocity ),
					Vec3.Dot( new Vec3( mass.I.M20, mass.I.M21, mass.I.M22 ), localVelocity ) );

				Vec3 torque = momentum * factor;

				// Add a local torque opposite to the local angular velocity.
				Ode.dBodyAddRelTorque( bodyID, torque.X, torque.Y, torque.Z );
			}
		}

		//internal void DoAngularVelocityFix()
		//{
		//   if( nonSymmetricInertia )
		//   {
		//      Vec3 velocity = AngularVelocity;
		//      float currentAngVelMagSquared = velocity.LengthSqr();

		//      if( freelySpinning )
		//      {
		//         // If the current angular velocity magnitude is greater than
		//         // that of the previous step, scale it by that of the previous
		//         // step; otherwise, update the previous value to that of the
		//         // current step.  This ensures that angular velocity never
		//         // increases for freely-spinning objects.

		//         if( currentAngVelMagSquared > prevAngVelMagSquared )
		//         {
		//            float currentAngVelMag = MathFunctions.Sqrt16( currentAngVelMagSquared );
		//            velocity = velocity / currentAngVelMag;
		//            // Vel is now a unit vector.  Next, scale this vector
		//            // by the previous angular velocity magnitude.
		//            float prevAngVelMag = MathFunctions.Sqrt16( prevAngVelMagSquared );
		//            AngularVelocity = velocity * prevAngVelMag;
		//         }
		//      }

		//      prevAngVelMagSquared = currentAngVelMagSquared;
		//   }

		//   // Reset the "freely-spinning" parameter for the next time step.
		//   freelySpinning = true;

		//   //fix MaxAngularVelocity
		//   {
		//      float max = ODEPhysicsWorld.Instance.MaxAngularVelocity;

		//      Vec3 vel = AngularVelocity;
		//      bool changed = false;

		//      if( vel.X < -max ) { vel.X = -max; changed = true; }
		//      else if( vel.X > max ) { vel.X = max; changed = true; }
		//      if( vel.Y < -max ) { vel.Y = -max; changed = true; }
		//      else if( vel.Y > max ) { vel.Y = max; changed = true; }
		//      if( vel.Z < -max ) { vel.Z = -max; changed = true; }
		//      else if( vel.Z > max ) { vel.Z = max; changed = true; }

		//      if( changed )
		//         AngularVelocity = vel;
		//   }

		//}

		public override void ClearForces()
		{
			base.ClearForces();

			if( bodyID != dBodyID.Zero )
			{
				Ode.dBodySetForce( bodyID, 0, 0, 0 );
				Ode.dBodySetTorque( bodyID, 0, 0, 0 );
			}
		}

		public void CCDStep()
		{
			//calculate ccd radius
			if( ccdRadius == -1 )
			{
				ccdRadius = float.MaxValue;

				foreach( Shape shape in Shapes )
				{
					float radius = float.MaxValue;

					switch( shape.ShapeType )
					{
					case Shape.Type.Box:
						{
							Vec3 half = ( (BoxShape)shape ).Dimensions * .5f * .9f;
							radius = Math.Min( half.X, Math.Min( half.Y, half.Z ) );
						}
						break;

					case Shape.Type.Sphere:
						radius = ( (SphereShape)shape ).Radius * .9f;
						break;

					case Shape.Type.Capsule:
						radius = ( (CapsuleShape)shape ).Radius * .9f;
						break;

					case Shape.Type.Cylinder:
						radius = ( (CylinderShape)shape ).Radius * .9f;
						break;

					case Shape.Type.Mesh:
						//not implemented
						radius = .1f;
						break;
					}

					if( radius < ccdRadius )
						ccdRadius = radius;
				}
			}
			if( ccdRadius == float.MaxValue )
				return;

			//ccd action

			Vec3 dir = Position - ccdLastPosition;
			if( Math.Abs( dir.X ) < .0001f && Math.Abs( dir.Y ) < .0001f && Math.Abs( dir.Z ) < .0001f )
				return;
			dir.Normalize();

			//simple ray cast
			{
				foreach( Shape shape in Shapes )
				{
					if( float.IsNaN( ccdLastPosition.X ) )
						Log.Fatal( "ODEBody.CCDStep: float.IsNaN( ccdLastPosition.X )." );

					Vec3 start = ccdLastPosition;
					Vec3 end = Position + dir * ccdRadius;

					float d;
					if( scene.CCDCast( this, start, end, shape.ContactGroup, out d ) )
					{
						if( d >= ccdRadius )
							d -= ccdRadius;

						Vec3 pos = start + dir * d;

						if( float.IsNaN( pos.X ) )
							Log.Fatal( "ODEBody.CCDStep: float.IsNaN( pos.X )." );

						Vec3 oldPosition = OldPosition;
						Position = pos;
						OldPosition = oldPosition;
					}
				}
			}
		}

		protected override void OnSetLinearDamping()
		{
		}

		protected override void OnSetAngularDamping()
		{
		}

		protected override void OnSetEnableGravity()
		{
			if( bodyID != dBodyID.Zero )
				Ode.dBodySetGravityMode( bodyID, EnableGravity ? 1 : 0 );
		}

		internal GeomData GetGeomDataByShape( Shape shape )
		{
			if( geomDatas != null )
			{
				foreach( GeomData data in geomDatas )
				{
					if( data != null && data.shape == shape )
						return data;
				}
			}
			return null;
		}

		protected override void OnShapeSetContactGroup( Shape shape )
		{
			base.OnShapeSetContactGroup( shape );

			GeomData geomData = GetGeomDataByShape( shape );
			if( geomData != null )
			{
				dGeomID geomID = ( geomData.transformID != dGeomID.Zero ) ?
					geomData.transformID : geomData.geomID;
				Ode.SetShapeContractGroup( geomID, shape.ContactGroup );
			}
		}

		protected override void OnShapeSetMaterialProperty( Shape shape )
		{
			GeomData geomData = GetGeomDataByShape( shape );
			if( geomData != null )
			{
				dGeomID geomID = ( geomData.transformID != dGeomID.Zero ) ? geomData.transformID : geomData.geomID;
				Ode.SetShapeMaterialProperties( geomID, shape.Hardness, shape.Restitution, shape.DynamicFriction,
					shape.StaticFriction );
			}
		}
	}
}
