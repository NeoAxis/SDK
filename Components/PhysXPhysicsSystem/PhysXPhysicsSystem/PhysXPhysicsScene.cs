// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Engine;
using Engine.FileSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXPhysicsScene : PhysicsScene
	{
		public IntPtr nativeScene;

		RayCastResult[] emptyPiercingRayCastResult = new RayCastResult[ 0 ];
		Body[] emptyVolumeCastResult = new Body[ 0 ];

		Set<Body> tempSetOfBodies = new Set<Body>( 64 );
		int tempSetOfBodiesMaxSize;
		int tempSetOfBodiesShrinkCounter;

		///////////////////////////////////////////

		public PhysXPhysicsScene( string description )
			: base( description )
		{
		}

		public unsafe void Create( int numThreads, uint[] affinityMasks )
		{
			//create native scene
			if( affinityMasks != null )
			{
				fixed( uint* pAffinityMasks = affinityMasks )
				{
					nativeScene = PhysXNativeWorld.CreateScene( numThreads, (IntPtr)pAffinityMasks );
				}
			}
			else
				nativeScene = PhysXNativeWorld.CreateScene( numThreads, IntPtr.Zero );

			UpdateGravity();

			for( int group0 = 0; group0 < 32; group0++ )
				for( int group1 = 0; group1 < 32; group1++ )
					OnUpdateContactGroups( group0, group1, IsContactGroupsContactable( group0, group1 ) );
		}

		public void Destroy()
		{
			PhysXNativeWorld.DestroyScene( nativeScene );
			nativeScene = IntPtr.Zero;
		}

		unsafe RayCastResult GetRayCastResultFromNative( NativeRayCastResult* native )
		{
			RayCastResult result = new RayCastResult();
			if( native->shape != IntPtr.Zero )
			{
				result.Shape = PhysXPhysicsWorld.Instance.GetShapeByNativePointer( native->shape );
				result.Position = native->worldImpact;
				result.Normal = native->worldNormal;
				result.Distance = native->distance;
				if( result.Shape.ShapeType == Shape.Type.Mesh )
				{
					MeshShape meshShape = (MeshShape)result.Shape;
					if( meshShape.MeshType != MeshShape.MeshTypes.ConvexHullDecomposition )
						result.TriangleID = native->faceID;
				}
				else if( result.Shape.ShapeType == Shape.Type.HeightField )
					result.TriangleID = native->faceID;
				else
					result.TriangleID = 0;
			}
			return result;
		}

		protected override RayCastResult OnRayCast( Ray ray, int contactGroup )
		{
			if( float.IsNaN( ray.Origin.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Origin.X)" );
			if( float.IsNaN( ray.Direction.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Direction.X)" );
			if( ray.Direction == Vec3.Zero )
				return new RayCastResult();

			Vec3 normalDir = ray.Direction;
			float length = normalDir.Normalize();
			Vec3 origin = ray.Origin;
			int count = PhysXNativeWrapper.PhysXNativeScene.RayCast( nativeScene, ref origin, ref normalDir, length,
				GetContactGroupMask( contactGroup ), false );
			if( count == 0 )
				return new RayCastResult();

			unsafe
			{
				NativeRayCastResult* results = PhysXNativeScene.GetLastRayCastResults( nativeScene );
				NativeRayCastResult* nativeResult = results;
				return GetRayCastResultFromNative( nativeResult );
			}
		}

		unsafe protected override RayCastResult[] OnRayCastPiercing( Ray ray, int contactGroup )
		{
			if( float.IsNaN( ray.Origin.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Origin.X)" );
			if( float.IsNaN( ray.Direction.X ) )
				Log.Fatal( "PhysicsWorld.RayCast: Single.IsNaN(ray.Direction.X)" );
			if( ray.Direction == Vec3.Zero )
				return emptyPiercingRayCastResult;

			Vec3 normalDir = ray.Direction;
			float length = normalDir.Normalize();
			Vec3 origin = ray.Origin;
			int count = PhysXNativeWrapper.PhysXNativeScene.RayCast( nativeScene, ref origin, ref normalDir,
				length, GetContactGroupMask( contactGroup ), true );
			if( count == 0 )
				return emptyPiercingRayCastResult;

			NativeRayCastResult* results = PhysXNativeScene.GetLastRayCastResults( nativeScene );

			RayCastResult[] items = new RayCastResult[ count ];
			NativeRayCastResult* nativeResult = results;
			for( int n = 0; n < count; n++ )
			{
				items[ n ] = GetRayCastResultFromNative( nativeResult );
				nativeResult++;
			}

			return items;
		}

		unsafe Body[] GetBodiesFromVolumeCastResult( int shapeCount )
		{
			int* shapeIdentifiers = PhysXNativeScene.GetLastVolumeCastShapeIdentifiers( nativeScene );

			for( int n = 0; n < shapeCount; n++ )
			{
				Shape shape = PhysXPhysicsWorld.Instance.GetShapeByIdentifier( shapeIdentifiers[ n ] );
				tempSetOfBodies.AddWithCheckAlreadyContained( shape.Body );
			}
			Body[] result = new Body[ tempSetOfBodies.Count ];
			tempSetOfBodies.CopyTo( result, 0 );

			//clear and shrink tempSetOfBodies
			{
				if( tempSetOfBodies.Count > tempSetOfBodiesMaxSize )
					tempSetOfBodiesMaxSize = tempSetOfBodies.Count;
				if( tempSetOfBodies.Count * 2 < tempSetOfBodiesMaxSize )
				{
					tempSetOfBodiesShrinkCounter++;
					if( tempSetOfBodiesShrinkCounter == 100 )
					{
						tempSetOfBodies = new Set<Body>( 64 );
						tempSetOfBodiesMaxSize = 0;
					}
				}
				else
					tempSetOfBodiesShrinkCounter = 0;
				tempSetOfBodies.Clear();
			}

			return result;
		}

		protected override Body[] OnVolumeCast( Bounds bounds, int contactGroup )
		{
			Vec3 origin = bounds.GetCenter();
			Vec3 halfExtents = bounds.GetSize() * .5f;
			Quat rotation = Quat.Identity;

			int shapeCount = PhysXNativeScene.OverlapOBBShapes( nativeScene, ref origin, ref halfExtents, ref rotation,
				GetContactGroupMask( contactGroup ) );
			if( shapeCount == 0 )
				return emptyVolumeCastResult;
			return GetBodiesFromVolumeCastResult( shapeCount );
		}

		protected override Body[] OnVolumeCast( Box box, int contactGroup )
		{
			Vec3 origin = box.Center;
			Vec3 halfExtents = box.Extents;
			Quat rotation = box.Axis.ToQuat();

			int shapeCount = PhysXNativeScene.OverlapOBBShapes( nativeScene, ref origin, ref halfExtents, ref rotation,
				GetContactGroupMask( contactGroup ) );
			if( shapeCount == 0 )
				return emptyVolumeCastResult;
			return GetBodiesFromVolumeCastResult( shapeCount );
		}

		protected override Body[] OnVolumeCast( Sphere sphere, int contactGroup )
		{
			Vec3 origin = sphere.Origin;
			int shapeCount = PhysXNativeScene.OverlapSphereShapes( nativeScene, ref origin, sphere.Radius,
				GetContactGroupMask( contactGroup ) );
			if( shapeCount == 0 )
				return emptyVolumeCastResult;
			return GetBodiesFromVolumeCastResult( shapeCount );
		}

		protected override Body[] OnVolumeCast( Capsule capsule, int contactGroup )
		{
			Vec3 origin = capsule.GetCenter();
			Quat rotation = Quat.FromDirectionZAxisUp( capsule.GetDirection() );
			int shapeCount = PhysXNativeScene.OverlapCapsuleShapes( nativeScene, ref origin, ref rotation, capsule.Radius,
				capsule.GetLength() * .5f, GetContactGroupMask( contactGroup ) );
			if( shapeCount == 0 )
				return emptyVolumeCastResult;
			return GetBodiesFromVolumeCastResult( shapeCount );
		}

		protected override void OnSimulationStep()
		{
			//Vehicles

			// Take a simulation step.
			PhysXNativeScene.Simulate( nativeScene, StepSize );

			// Fetch simulation results
			if( !PhysXNativeScene.FetchResults( nativeScene, true ) )
				Log.Fatal( "PhysXPhysicsScene: OnSimulationStep: Error while PhysX fetching results." );

			//we can get list of not sleeping bodies from PhysX.
			//joints too?
			//update from PhysX
			foreach( Body body in Bodies )
			{
				PhysXBody physXBody = (PhysXBody)body;
				if( !body.Static )
					physXBody.UpdateDataFromLibrary();
			}

			foreach( IPhysXJoint joint in Joints )
				joint.UpdateDataFromLibrary();

			if( EnableCollisionEvents )
			{
				IntPtr contactList;
				int contactCount;
				PhysXNativeScene.GetContactReportList( nativeScene, out contactList, out contactCount );
				unsafe
				{
					ContactReport* pContact = (ContactReport*)contactList;
					for( int n = 0; n < contactCount; n++ )
					{
						Shape shape1 = PhysXPhysicsWorld.Instance.GetShapeByIdentifier( pContact->shapeIndex1 );
						Shape shape2 = PhysXPhysicsWorld.Instance.GetShapeByIdentifier( pContact->shapeIndex2 );
						Vec3 contactPoint = pContact->contactPoint;
						Vec3 normal = pContact->normal;
						AddCollisionEvent( shape1, shape2, ref contactPoint, ref normal, -pContact->separation );
						pContact++;
					}
				}
			}
		}

		unsafe protected override void OnSetStepSize()
		{
		}

		protected override void OnSetMaxIterationCount()
		{
		}

		public new bool IsBodyCollisionEventHandled( Body body )
		{
			return base.IsBodyCollisionEventHandled( body );
		}

		unsafe void UpdateGravity()
		{
			Vec3 value = Gravity;
			PhysXNativeScene.SetGravity( nativeScene, ref value );
		}

		unsafe protected override void OnSetGravity()
		{
			UpdateGravity();
		}

		protected override void OnUpdateContactGroups( int group0, int group1, bool makeContacts )
		{
			if( nativeScene != IntPtr.Zero )
				PhysXNativeScene.SetGroupCollisionFlag( nativeScene, group0, group1, makeContacts );
		}

		protected override void OnUpdateEnableDebugVisualization()
		{
			base.OnUpdateEnableDebugVisualization();

			PhysXNativeScene.SetEnableDebugVisualization( nativeScene, _EnableDebugVisualization );
			foreach( IPhysXJoint joint in Joints )
				joint.SetVisualizationEnable( _EnableDebugVisualization );
		}

		unsafe protected override Line[] OnGetDebugVisualizationData()
		{
			int lineCount;
			PhysXNativeScene.GetDebugVisualizationData( nativeScene, out lineCount, IntPtr.Zero );
			if( lineCount == 0 )
				return null;
			Line[] lines = new Line[ lineCount ];
			fixed( Line* pLines = lines )
			{
				PhysXNativeScene.GetDebugVisualizationData( nativeScene, out lineCount, (IntPtr)pLines );
			}
			return lines;
		}

		protected override Body OnCreateBody()
		{
			return new PhysXBody( this );
		}

		protected override PhysicsVehicle OnCreateVehicle( Body baseBody )
		{
			return new PhysXPhysicsVehicle( this, (PhysXBody)baseBody );
		}

		protected override Joint OnCreateJointByType( Joint.Type jointType, Body body1, Body body2 )
		{
			switch( jointType )
			{
			case Joint.Type.Hinge: return new PhysXHingeJoint( body1, body2 );
			case Joint.Type.Universal: return new PhysXUniversalJoint( body1, body2 );
			case Joint.Type.Hinge2: return new PhysXHinge2Joint( body1, body2 );
			case Joint.Type.Ball: return new PhysXBallJoint( body1, body2 );
			case Joint.Type.Slider: return new PhysXSliderJoint( body1, body2 );
			case Joint.Type.Fixed: return new PhysXFixedJoint( body1, body2 );
			//case Joint.Type.Distance: return new PhysXDistanceJoint( body1, body2 );

			//custom joint creation
			case Joint.Type._Custom1: return new _Custom1Joint( body1, body2 );
			}

			Log.Fatal( "PhysXPhysicsScene: OnCreateJointByType: Joint type \"{0}\" is not supported.", jointType );
			return null;
		}
	}
}
