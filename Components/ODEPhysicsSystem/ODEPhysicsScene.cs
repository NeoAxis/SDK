// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
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
	using dNeoAxisAdditionsID = System.IntPtr;

	//

	class ODEPhysicsScene : PhysicsScene
	{
		internal dWorldID worldID;
		dNeoAxisAdditionsID neoAxisAdditionsID;

		/// The root of the ODE collision detection hierarchy.
		internal dSpaceID rootSpaceID;

		/// The ODE joint constraint group.
		dJointGroupID contactJointGroupID;

		//RayCast
		dGeomID rayCastGeomID;
		RayCastResult[] emptyPiercingRayCastResult = new RayCastResult[ 0 ];
		Comparison<RayCastResult> rayCastResultDistanceComparer;

		//VolumeCast
		Set<Body> volumeCastResult = new Set<Body>();
		Body[] emptyVolumeCastResult = new Body[ 0 ];

		internal IntegerKeyDictionary<ODEBody.GeomData> shapesDictionary =
			new IntegerKeyDictionary<ODEBody.GeomData>( 64 );

		///////////////////////////////////////////

		public ODEPhysicsScene( string description )
			: base( description )
		{
		}

		public void Create()
		{
			MaxIterationCount = ODEPhysicsWorld.Instance.defaultMaxIterationCount;

			worldID = Ode.dWorldCreate();

			//Ode.dVector3 center = new Ode.dVector3( 0, 0, 0 );
			//Ode.dVector3 extents = new Ode.dVector3( 1000, 1000, 1000 );
			//rootSpaceID = Ode.dQuadTreeSpaceCreate( dSpaceID.Zero, ref center, ref extents, 10 );
			rootSpaceID = Ode.dHashSpaceCreate( dSpaceID.Zero );
			Ode.dHashSpaceSetLevels( rootSpaceID, ODEPhysicsWorld.Instance.hashSpaceMinLevel,
				ODEPhysicsWorld.Instance.hashSpaceMaxLevel );

			// Create the ODE contact joint group.
			contactJointGroupID = Ode.dJointGroupCreate( 0 );

			// Set the ODE global CFM value that will be used by all Joints
			// (including contacts).  This affects normal Joint constraint
			// operation and Joint limits.  The user cannot adjust CFM, but
			// they can adjust ERP (a.k.a. bounciness/restitution) for materials
			// (i.e. contact settings) and Joint limits.
			Ode.dWorldSetCFM( worldID, Defines.globalCFM );

			// Set the ODE global ERP value.  This will only be used for Joints
			// under normal conditions, not at their limits.  Also, it will not
			// affect contacts at all since they use material properties to
			// calculate ERP.
			Ode.dWorldSetERP( worldID, 0.5f * ( Defines.maxERP + Defines.minERP ) );

			Ode.dWorldSetContactSurfaceLayer( worldID, Defines.surfaceLayer );

			//MaxIterationCount = maxIterationCount;

			//ray for RayCast
			rayCastGeomID = Ode.dCreateRay( rootSpaceID, 1 );
			Ode.dGeomSetData( rayCastGeomID, IntPtr.Zero );

			rayCastResultDistanceComparer = new Comparison<RayCastResult>( SortRayCastResultsMethod );

			unsafe
			{
				Ode.CheckEnumAndStructuresSizes( sizeof( Ode.CollisionEventData ),
					sizeof( Ode.RayCastResult ) );
			}

			neoAxisAdditionsID = Ode.NeoAxisAdditions_Init( Defines.maxContacts, Defines.minERP,
				Defines.maxERP, Defines.maxFriction, Defines.bounceThreshold, worldID, rootSpaceID,
				rayCastGeomID, contactJointGroupID );

			UpdateMaxIterationCount();
			UpdateGravity();
			UpdateMaxAngularSpeed();

			for( int group0 = 0; group0 < 32; group0++ )
				for( int group1 = 0; group1 < 32; group1++ )
					OnUpdateContactGroups( group0, group1, IsContactGroupsContactable( group0, group1 ) );
		}

		public void Destroy()
		{
			if( shapesDictionary.Count != 0 )
				Log.Warning( "ODEPhysicsWorld: OnShutdownLibrary: shapesDictionary.Count != 0." );

			if( neoAxisAdditionsID != IntPtr.Zero )
			{
				Ode.NeoAxisAdditions_Shutdown( neoAxisAdditionsID );
				neoAxisAdditionsID = IntPtr.Zero;
			}

			if( rayCastGeomID != dGeomID.Zero )
			{
				Ode.dGeomDestroy( rayCastGeomID );
				rayCastGeomID = dGeomID.Zero;
			}

			if( rootSpaceID != dSpaceID.Zero )
			{
				Ode.dSpaceDestroy( rootSpaceID );
				rootSpaceID = dSpaceID.Zero;
			}

			if( worldID != dWorldID.Zero )
			{
				Ode.dWorldDestroy( worldID );
				worldID = dWorldID.Zero;
			}

			if( contactJointGroupID != dJointGroupID.Zero )
			{
				Ode.dJointGroupDestroy( contactJointGroupID );
				contactJointGroupID = dJointGroupID.Zero;
			}
		}

		protected override RayCastResult OnRayCast( Ray ray, int contactGroup )
		{
			if( float.IsNaN( ray.Origin.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Origin.X)" );
			if( float.IsNaN( ray.Direction.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Direction.X)" );
			if( ray.Direction == Vec3.Zero )
				return new RayCastResult();

			Vec3 dirNormal = ray.Direction;
			float length = dirNormal.Normalize();
			Ode.dGeomRaySet( rayCastGeomID, ray.Origin.X, ray.Origin.Y, ray.Origin.Z,
				dirNormal.X, dirNormal.Y, dirNormal.Z );
			Ode.dGeomRaySetLength( rayCastGeomID, length );

			int count;
			IntPtr data;
			Ode.DoRayCast( neoAxisAdditionsID, contactGroup, out count, out data );

			RayCastResult result = new RayCastResult();

			if( count != 0 )
			{
				unsafe
				{
					Ode.RayCastResult* pointer = (Ode.RayCastResult*)data;

					result.Shape = shapesDictionary[ pointer->shapeDictionaryIndex ].shape;
					result.Position = Convert.ToNet( pointer->position );
					result.Normal = Convert.ToNet( pointer->normal );
					result.Distance = pointer->distance;
					result.TriangleID = pointer->triangleID;
				}
			}

			return result;
		}

		static int SortRayCastResultsMethod( RayCastResult r1, RayCastResult r2 )
		{
			if( r1.Distance < r2.Distance )
				return -1;
			if( r1.Distance > r2.Distance )
				return 1;
			return 0;
		}

		protected override RayCastResult[] OnRayCastPiercing( Ray ray, int contactGroup )
		{
			if( float.IsNaN( ray.Origin.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Origin.X)" );
			if( float.IsNaN( ray.Direction.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Direction.X)" );
			if( ray.Direction == Vec3.Zero )
				return emptyPiercingRayCastResult;

			Vec3 dirNormal = ray.Direction;
			float length = dirNormal.Normalize();
			Ode.dGeomRaySet( rayCastGeomID, ray.Origin.X, ray.Origin.Y, ray.Origin.Z,
				dirNormal.X, dirNormal.Y, dirNormal.Z );
			Ode.dGeomRaySetLength( rayCastGeomID, length );

			int count;
			IntPtr data;
			Ode.DoRayCastPiercing( neoAxisAdditionsID, contactGroup, out count, out data );

			if( count == 0 )
				return emptyPiercingRayCastResult;

			RayCastResult[] array = new RayCastResult[ count ];

			unsafe
			{
				Ode.RayCastResult* pointer = (Ode.RayCastResult*)data;
				for( int n = 0; n < count; n++ )
				{
					RayCastResult result = new RayCastResult();

					result.Shape = shapesDictionary[ pointer->shapeDictionaryIndex ].shape;
					result.Position = Convert.ToNet( pointer->position );
					result.Normal = Convert.ToNet( pointer->normal );
					result.Distance = pointer->distance;
					result.TriangleID = pointer->triangleID;

					array[ n ] = result;

					pointer++;
				}
			}

			//sort by distance
			ArrayUtils.SelectionSort( array, rayCastResultDistanceComparer );

			return array;
		}

		Body[] DoVolumeCastGeneral( dGeomID volumeCastGeomID, int contactGroup )
		{
			int count;
			IntPtr data;
			Ode.DoVolumeCast( neoAxisAdditionsID, volumeCastGeomID, contactGroup, out count,
				out data );

			if( count == 0 )
				return emptyVolumeCastResult;

			unsafe
			{
				int* pointer = (int*)data;
				for( int n = 0; n < count; n++ )
				{
					int shapeDictionaryIndex = *pointer;
					ODEBody.GeomData geomData = shapesDictionary[ shapeDictionaryIndex ];

					volumeCastResult.AddWithCheckAlreadyContained( geomData.shape.Body );

					pointer++;
				}
			}

			Body[] result = new Body[ volumeCastResult.Count ];
			volumeCastResult.CopyTo( result, 0 );
			volumeCastResult.Clear();

			return result;
		}

		protected override Body[] OnVolumeCast( Bounds bounds, int contactGroup )
		{
			Vec3 size;
			bounds.GetSize( out size );
			Vec3 center;
			bounds.GetCenter( out center );

			dGeomID volumeCastGeomID = Ode.dCreateBox( rootSpaceID, size.X, size.Y, size.Z );
			Ode.dGeomSetPosition( volumeCastGeomID, center.X, center.Y, center.Z );

			Body[] result = DoVolumeCastGeneral( volumeCastGeomID, contactGroup );

			Ode.dGeomDestroy( volumeCastGeomID );

			return result;
		}

		protected override Body[] OnVolumeCast( Box box, int contactGroup )
		{
			dGeomID volumeCastGeomID = Ode.dCreateBox( rootSpaceID,
				box.Extents.X * 2, box.Extents.Y * 2, box.Extents.Z * 2 );

			Mat3 mat3 = box.Axis;
			Ode.dMatrix3 odeMat3;
			Convert.ToODE( ref mat3, out odeMat3 );
			Ode.dGeomSetRotation( volumeCastGeomID, ref odeMat3 );
			Ode.dGeomSetPosition( volumeCastGeomID, box.Center.X, box.Center.Y, box.Center.Z );

			Body[] result = DoVolumeCastGeneral( volumeCastGeomID, contactGroup );

			Ode.dGeomDestroy( volumeCastGeomID );

			return result;
		}

		protected override Body[] OnVolumeCast( Sphere sphere, int contactGroup )
		{
			dGeomID volumeCastGeomID = Ode.dCreateSphere( rootSpaceID, sphere.Radius );
			Ode.dGeomSetPosition( volumeCastGeomID, sphere.Origin.X, sphere.Origin.Y, sphere.Origin.Z );

			Body[] result = DoVolumeCastGeneral( volumeCastGeomID, contactGroup );

			Ode.dGeomDestroy( volumeCastGeomID );

			return result;
		}

		protected override Body[] OnVolumeCast( Capsule capsule, int contactGroup )
		{
			Vec3 center;
			capsule.GetCenter( out center );
			float length = capsule.GetLength();
			Vec3 direction;
			capsule.GetDirection( out direction );

			dGeomID volumeCastGeomID = Ode.dCreateCapsule( rootSpaceID, capsule.Radius, length );

			Quat rotation = Quat.FromDirectionZAxisUp( direction );

			Mat3 rot;
			rotation.ToMat3( out rot );

			Mat3 rotationMat;
			Mat3.FromRotateByY( MathFunctions.PI / 2, out rotationMat );
			Mat3 mat3;
			Mat3.Multiply( ref rot, ref rotationMat, out mat3 );

			Ode.dMatrix3 odeMat3;
			Convert.ToODE( ref mat3, out odeMat3 );
			Ode.dGeomSetRotation( volumeCastGeomID, ref odeMat3 );
			Ode.dGeomSetPosition( volumeCastGeomID, center.X, center.Y, center.Z );

			Body[] result = DoVolumeCastGeneral( volumeCastGeomID, contactGroup );

			Ode.dGeomDestroy( volumeCastGeomID );

			return result;
		}

		protected override void OnSimulationStep()
		{
			foreach( Body body in Bodies )
			{
				if( !body.Static && !body.Sleeping )
				{
					ODEBody odeBody = (ODEBody)body;

					// Apply linear and angular damping; if using the "add opposing
					// forces" method, be sure to do this before calling ODE step
					// function.
					odeBody.DoDamping();

					if( body.CCD )
						odeBody.ccdLastPosition = odeBody.Position;
				}
			}

			int collisionEventCount;
			IntPtr collisionEvents;
			Ode.DoSimulationStep( neoAxisAdditionsID, out collisionEventCount, out collisionEvents );

			//process collision events
			{
				unsafe
				{
					Ode.CollisionEventData* pointer = (Ode.CollisionEventData*)collisionEvents;
					for( int n = 0; n < collisionEventCount; n++ )
					{
						ODEBody.GeomData geomData1 = shapesDictionary[ pointer->shapeDictionaryIndex1 ];
						ODEBody.GeomData geomData2 = shapesDictionary[ pointer->shapeDictionaryIndex2 ];

						//// Invalidate the "freely-spinning" parameters.
						//geomData1.odeBody.freelySpinning = false;
						//geomData2.odeBody.freelySpinning = false;

						if( EnableCollisionEvents )
						{
							Shape shape1 = geomData1.shape;
							Shape shape2 = geomData2.shape;

							if( IsBodyCollisionEventHandled( shape1.Body ) ||
								IsBodyCollisionEventHandled( shape2.Body ) )
							{
								Vec3 pos;
								Vec3 normal;
								Convert.ToNet( ref pointer->position, out pos );
								Convert.ToNet( ref pointer->normal, out normal );
								normal = -normal;
								AddCollisionEvent( shape1, shape2, ref pos, ref normal, pointer->depth );
							}
						}

						pointer++;
					}
				}
			}

			// Take a simulation step.
			Ode.dWorldQuickStep( worldID, StepSize );

			// Remove all joints from the contact group.
			Ode.dJointGroupEmpty( contactJointGroupID );

			//update from ODE
			foreach( Body body in Bodies )
			{
				if( !body.Static )
				{
					ODEBody odeBody = (ODEBody)body;

					odeBody.UpdateDataFromLibrary();

					if( !odeBody.Sleeping )
					{
						//ODE bug fix
						//need still?
						if( float.IsNaN( odeBody.Position.X ) )
							odeBody.Position = odeBody.OldPosition;
						if( float.IsNaN( odeBody.Rotation.X ) )
							odeBody.Rotation = odeBody.OldRotation;

						//// Fix angular velocities for freely-spinning bodies that have
						//// gained angular velocity through explicit integrator inaccuracy.
						//odeBody.DoAngularVelocityFix();

						if( odeBody.CCD )
							odeBody.CCDStep();
					}
				}
			}

			foreach( Joint joint in Joints )
			{
				switch( joint.JointType )
				{
				case Joint.Type.Hinge:
					( (ODEHingeJoint)joint ).UpdateDataFromLibrary();
					break;

				case Joint.Type.Universal:
					( (ODEUniversalJoint)joint ).UpdateDataFromLibrary();
					break;

				case Joint.Type.Hinge2:
					( (ODEHinge2Joint)joint ).UpdateDataFromLibrary();
					break;

				case Joint.Type.Ball:
					( (ODEBallJoint)joint ).UpdateDataFromLibrary();
					break;

				case Joint.Type.Slider:
					( (ODESliderJoint)joint ).UpdateDataFromLibrary();
					break;

				case Joint.Type.Fixed:
					break;

				default:
					Trace.Assert( false );
					break;
				}
			}
		}

		public bool CCDCast( ODEBody ccdBody, Vec3 start, Vec3 end, int contactGroup,
			out float minDistance )
		{
			Vec3 direction = end - start;

			if( Math.Abs( direction.X ) < .0001f && Math.Abs( direction.Y ) < .0001f &&
				Math.Abs( direction.Z ) < .0001f )
			{
				minDistance = 0;
				return false;
			}

			Ray ray = new Ray( start, direction );

			Vec3 dirNormal = ray.Direction;
			float length = dirNormal.Normalize();
			Ode.dGeomRaySet( rayCastGeomID, ray.Origin.X, ray.Origin.Y, ray.Origin.Z,
				dirNormal.X, dirNormal.Y, dirNormal.Z );
			Ode.dGeomRaySetLength( rayCastGeomID, length );

			return Ode.DoCCDCast( neoAxisAdditionsID, ccdBody.bodyID, contactGroup, out minDistance );
		}

		protected override void OnSetStepSize()
		{
		}

		void UpdateMaxIterationCount()
		{
			if( worldID != IntPtr.Zero )
				Ode.dWorldSetQuickStepNumIterations( worldID, MaxIterationCount );
		}

		protected override void OnSetMaxIterationCount()
		{
			UpdateMaxIterationCount();
		}

		void UpdateGravity()
		{
			if( worldID != IntPtr.Zero )
				Ode.dWorldSetGravity( worldID, Gravity.X, Gravity.Y, Gravity.Z );
		}

		protected override void OnSetGravity()
		{
			UpdateGravity();
		}

		protected override void OnUpdateContactGroups( int group0, int group1, bool makeContacts )
		{
			if( neoAxisAdditionsID != IntPtr.Zero )
				Ode.SetupContactGroups( neoAxisAdditionsID, group0, group1, makeContacts );
		}

		public void UpdateMaxAngularSpeed()
		{
			if( worldID != IntPtr.Zero )
				Ode.dWorldSetMaxAngularSpeed( worldID, ODEPhysicsWorld.Instance.MaxAngularVelocity );
		}

		protected override Body OnCreateBody()
		{
			return new ODEBody( this );
		}

		protected override Joint OnCreateJointByType( Joint.Type jointType, Body body1, Body body2 )
		{
			switch( jointType )
			{
			case Joint.Type.Hinge: return new ODEHingeJoint( body1, body2 );
			case Joint.Type.Universal: return new ODEUniversalJoint( body1, body2 );
			case Joint.Type.Hinge2: return new ODEHinge2Joint( body1, body2 );
			case Joint.Type.Ball: return new ODEBallJoint( body1, body2 );
			case Joint.Type.Slider: return new ODESliderJoint( body1, body2 );
			case Joint.Type.Fixed: return new ODEFixedJoint( body1, body2 );

			//custom joint creation
			//case Joint.Type._Custom1: return new _Custom1Joint( body1, body2 );
			}

			Log.Fatal( "ODEPhysicsScene: OnCreateJointByType: Joint type \"{0}\" is not supported.",
				jointType );
			return null;
		}
	}
}
