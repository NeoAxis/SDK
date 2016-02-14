// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Runtime.InteropServices;
using System.Security;
using Engine.MathEx;

namespace PhysXNativeWrapper
{
	struct Wrapper
	{
		public const string library = "PhysXNativeWrapper";
		public const CallingConvention convention = CallingConvention.Cdecl;

		[DllImport( Wrapper.library, EntryPoint = "PhysXNativeWrapper_FreeOutString", CallingConvention = Wrapper.convention )]
		public unsafe static extern void FreeOutString( IntPtr pointer );

		public static string GetOutString( IntPtr pointer )
		{
			if( pointer != IntPtr.Zero )
			{
				string result = Marshal.PtrToStringUni( pointer );
				FreeOutString( pointer );
				return result;
			}
			else
				return null;
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct PhysXNativeWorld
	{
		public const float MAX_REAL = 3.402823466e+38F;
		//public const float SLEEP_INTERVAL = 20.0f * 0.02f;

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_Init", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool Init( ReportErrorDelegate reportErrorDelegate, out IntPtr errorString,
			LogDelegate logDelegate, float cookingParamsSkinWidth );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_Destroy", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Destroy();

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_GetSDKVersion", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetSDKVersion( out int major, out int minor, out int bugfix );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_CreateScene", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXScene*/ CreateScene( int numThreads, IntPtr affinityMasks );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_DestroyScene", CallingConvention = Wrapper.convention )]
		public unsafe static extern void DestroyScene( IntPtr/*PhysXScene*/ scene );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_CreateBody", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXBody*/ CreateBody( IntPtr/*PhysXScene*/ pScene,
			[MarshalAs( UnmanagedType.U1 )] bool isStatic, ref Vec3 globalPosition, ref Quat globalRotation );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_DestroyBody", CallingConvention = Wrapper.convention )]
		public unsafe static extern void DestroyBody( IntPtr/*PhysXBody*/ body );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_CreateConvexMesh", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PxConvexMesh*/ CreateConvexMesh( IntPtr/*PxVec3**/ vertices, int vertexCount,
			IntPtr/*void**/ indices, int indexCount, [MarshalAs( UnmanagedType.U1 )] bool use16bitIndices );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_ReleaseConvexMesh", CallingConvention = Wrapper.convention )]
		public unsafe static extern void ReleaseConvexMesh( IntPtr/*PxConvexMesh*/ mesh );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_CookTriangleMesh", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool CookTriangleMesh( IntPtr/*PxVec3**/ vertices, int vertexCount, IntPtr/*void**/ indices,
			int indexCount, [MarshalAs( UnmanagedType.U1 )] bool use16bitIndices, IntPtr/*short*/ materialIndices,
			out IntPtr data, out int size );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_CreateTriangleMesh", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PxTriangleMesh*/ CreateTriangleMesh( IntPtr data, int size );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_ReleaseTriangleMesh", CallingConvention = Wrapper.convention )]
		public unsafe static extern void ReleaseTriangleMesh( IntPtr/*PxTriangleMesh*/ mesh );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_Alloc", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*void**/ Alloc( int size );

		[DllImport( Wrapper.library, EntryPoint = "PhysXWorld_Free", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Free( IntPtr pointer );
	}

	struct PhysXNativeScene
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_SetGravity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetGravity( IntPtr/*PhysXScene*/ scene, ref Vec3 vec );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_Simulate", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Simulate( IntPtr/*PhysXScene*/ scene, float elapsedTime );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_FetchResults", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool FetchResults( IntPtr/*PhysXScene*/ scene,
			[MarshalAs( UnmanagedType.U1 )] bool block );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_AddBody", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddBody( IntPtr/*PhysXScene*/ scene, IntPtr/*PhysXBody*/ body );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_RemoveBody", CallingConvention = Wrapper.convention )]
		public unsafe static extern void RemoveBody( IntPtr/*PhysXScene*/ scene, IntPtr/*PhysXBody*/ body );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_RayCast", CallingConvention = Wrapper.convention )]
		public unsafe static extern int RayCast( IntPtr/*PhysXScene*/ scene, ref Vec3 origin, ref Vec3 unitDir,
			float distance, uint contactGroupMask, [MarshalAs( UnmanagedType.U1 )] bool piercing );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_GetLastRayCastResults", CallingConvention = Wrapper.convention )]
		public unsafe static extern NativeRayCastResult* GetLastRayCastResults( IntPtr/*PhysXScene*/ scene );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_OverlapOBBShapes", CallingConvention = Wrapper.convention )]
		public unsafe static extern int OverlapOBBShapes( IntPtr/*PhysXScene*/ scene, ref Vec3 origin, ref Vec3 halfExtents,
			ref Quat rotation, uint contactGroupMask );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_OverlapSphereShapes", CallingConvention = Wrapper.convention )]
		public unsafe static extern int OverlapSphereShapes( IntPtr/*PhysXScene*/ scene, ref Vec3 origin, float radius,
			uint contactGroupMask );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_OverlapCapsuleShapes", CallingConvention = Wrapper.convention )]
		public unsafe static extern int OverlapCapsuleShapes( IntPtr/*PhysXScene*/ scene, ref Vec3 origin, ref Quat rotation,
			float radius, float halfHeight, uint contactGroupMask );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_GetLastVolumeCastShapeIdentifiers", CallingConvention = Wrapper.convention )]
		public unsafe static extern int* GetLastVolumeCastShapeIdentifiers( IntPtr/*PhysXScene*/ scene );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_SetGroupCollisionFlag", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetGroupCollisionFlag( IntPtr/*PhysXScene*/ scene, int group1,
			int group2, [MarshalAs( UnmanagedType.U1 )] bool enable );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_SetShapePairFlags", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetShapePairFlags( IntPtr/*PhysXScene*/ scene, IntPtr/*PhysXShape*/ pShape1,
			IntPtr/*PhysXShape*/ nativeShape2, [MarshalAs( UnmanagedType.U1 )] bool disableContacts );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_GetContactReportList", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetContactReportList( IntPtr/*PhysXScene*/ scene,
			out IntPtr/*Contact*/ contactList, out int contactCount );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_SetEnableDebugVisualization", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetEnableDebugVisualization( IntPtr/*PhysXScene*/ scene,
			[MarshalAs( UnmanagedType.U1 )] bool enable );

		[DllImport( Wrapper.library, EntryPoint = "PhysXScene_GetDebugVisualizationData", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetDebugVisualizationData( IntPtr/*PhysXScene*/ scene, out int lineCount, IntPtr lines );
	}

	struct PhysXNativeBody
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_CreateBoxShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ CreateBoxShape( IntPtr/*PhysXBody*/ body, ref Vec3 position,
			ref Quat rotation, ref Vec3 halfDimension, int materialCount, IntPtr* materials, float mass, int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_CreateSphereShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ CreateSphereShape( IntPtr/*PhysXBody*/ body, ref Vec3 position,
			ref Quat rotation, float radius, int materialCount, IntPtr* materials, float mass, int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_CreateCapsuleShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ CreateCapsuleShape( IntPtr/*PhysXBody*/ body, ref Vec3 position,
			ref Quat rotation, float radius, float halfHeight, int materialCount, IntPtr* materials, float mass, int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_CreateConvexMeshShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ CreateConvexMeshShape( IntPtr/*PhysXBody*/ body, ref Vec3 position,
			ref Quat rotation, IntPtr/*PxTriangleMesh*/ pxConvexMesh, int materialCount, IntPtr* materials, float mass,
			int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_CreateTriangleMeshShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ CreateTriangleMeshShape( IntPtr/*PhysXBody*/ body, ref Vec3 position,
			ref Quat rotation, IntPtr/*PxTriangleMesh*/ pxTriangleMesh, int materialCount, IntPtr* materials, float mass,
			int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_CreateHeightFieldShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ CreateHeightFieldShape( IntPtr/*PhysXBody*/ body, ref Vec3 position,
			ref Quat rotation, int sampleCountX, int sampleCountY, IntPtr samples, ref Vec3 samplesScale, float thickness,
			int materialCount, IntPtr* materials, int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_IsSleeping", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool IsSleeping( IntPtr/*PhysXBody*/ body );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_GetGlobalPoseLinearAngularVelocities", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetGlobalPoseLinearAngularVelocities( IntPtr/*PhysXBody*/ body,
			out Vec3 globalPosition, out Quat globalRotation, out Vec3 linearVelocity,
			out Vec3 angularVelocity );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetGlobalPose", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetGlobalPose( IntPtr/*PhysXBody*/ body, ref Vec3 globalPosition,
			ref Quat globalRotation );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetLinearVelocity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetLinearVelocity( IntPtr/*PhysXBody*/ body, ref Vec3 linearVelocity );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetAngularVelocity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetAngularVelocity( IntPtr/*PhysXBody*/ body, ref Vec3 angularVelocity );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_PutToSleep", CallingConvention = Wrapper.convention )]
		public unsafe static extern void PutToSleep( IntPtr/*PhysXBody*/ body );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetMassAndInertia", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMassAndInertia( IntPtr/*PhysXBody*/ body,
			[MarshalAs( UnmanagedType.U1 )] bool autoCenterOfMass, ref Vec3 manualMassLocalPosition,
			ref Quat manualMassLocalRotation, ref Vec3 inertiaTensorFactor );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_GetShapeCount", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetShapeCount( IntPtr/*PhysXBody*/ body );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_GetShape", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXShape*/ GetShape( IntPtr/*PhysXBody*/ body, int index );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_WakeUp", CallingConvention = Wrapper.convention )]
		public unsafe static extern void WakeUp( IntPtr/*PhysXBody*/ body );
		//[DllImport( Wrapper.library, EntryPoint = "PhysXBody_WakeUp", CallingConvention = Wrapper.convention )]
		//public unsafe static extern void WakeUp( IntPtr/*PhysXBody*/ body, float wakeCounterValue );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddLocalForce", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddLocalForce( IntPtr/*PhysXBody*/ body, ref Vec3 force );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddForce", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddForce( IntPtr/*PhysXBody*/ body, ref Vec3 force );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddLocalTorque", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddLocalTorque( IntPtr/*PhysXBody*/ body, ref Vec3 torque );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddTorque", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddTorque( IntPtr/*PhysXBody*/ body, ref Vec3 torque );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddLocalForceAtLocalPos", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddLocalForceAtLocalPos( IntPtr/*PhysXBody*/ body, ref Vec3 force, ref Vec3 pos );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddLocalForceAtPos", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddLocalForceAtPos( IntPtr/*PhysXBody*/ body, ref Vec3 force, ref Vec3 pos );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddForceAtLocalPos", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddForceAtLocalPos( IntPtr/*PhysXBody*/ body, ref Vec3 force, ref Vec3 pos );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_AddForceAtPos", CallingConvention = Wrapper.convention )]
		public unsafe static extern void AddForceAtPos( IntPtr/*PhysXBody*/ body, ref Vec3 force, ref Vec3 pos );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetLinearDamping", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetLinearDamping( IntPtr/*PhysXBody*/ pBody, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetAngularDamping", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetAngularDamping( IntPtr/*PhysXBody*/ pBody, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetSleepThreshold", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetSleepThreshold( IntPtr/*PhysXBody*/ pBody, float threshold );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetGravity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetGravity( IntPtr/*PhysXBody*/ pBody,
			[MarshalAs( UnmanagedType.U1 )] bool enabled );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetMaxAngularVelocity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMaxAngularVelocity( IntPtr/*PhysXBody*/ pBody, float value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_EnableCCD", CallingConvention = Wrapper.convention )]
		public unsafe static extern void EnableCCD( IntPtr/*PhysXBody*/ pBody,
			[MarshalAs( UnmanagedType.U1 )] bool enabled );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetSolverIterationCounts", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetSolverIterationCounts( IntPtr/*PhysXBody*/ pBody, int positionIterations,
			int velocityIterations );

		//[DllImport( Wrapper.library, EntryPoint = "PhysXBody_GetInertiaTensor", CallingConvention = Wrapper.convention )]
		//public unsafe static extern void GetInertiaTensor( IntPtr/*PhysXBody*/ body, out Mat3 value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetKinematicFlag", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetKinematicFlag( IntPtr/*PhysXBody*/ pBody, [MarshalAs( UnmanagedType.U1 )] bool value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXBody_SetKinematicTarget", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetKinematicTarget( IntPtr/*PhysXBody*/ pBody, ref Vec3 pos, ref Quat rot );
	}

	struct PhysXNativeShape
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXShape_SetIdentifier", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetIdentifier( IntPtr/*PhysXShape*/ pShape, int value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXShape_GetIdentifier", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetIdentifier( IntPtr/*PhysXShape*/ pShape );

		[DllImport( Wrapper.library, EntryPoint = "PhysXShape_SetContactGroup", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetContactGroup( IntPtr/*PhysXShape*/ pShape, int contactGroup );

		[DllImport( Wrapper.library, EntryPoint = "PhysXShape_SetMaterialFreeOldMaterial", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMaterialFreeOldMaterial( IntPtr/*PhysXShape*/ pShape, int materialIndex,
			IntPtr/*PhysXMaterial*/ material );

		[DllImport( Wrapper.library, EntryPoint = "PhysXShape_UpdatePhysXShapeMaterials", CallingConvention = Wrapper.convention )]
		public unsafe static extern void UpdatePhysXShapeMaterials( IntPtr/*PhysXShape*/ pShape );

		//[DllImport( Wrapper.library, EntryPoint = "PhysXShape_Raycast", CallingConvention = Wrapper.convention )]
		//public unsafe static extern int Raycast( IntPtr/*PhysXShape*/ pShape, ref Vec3 origin, ref Vec3 unitDir,
		//   float distance, out IntPtr/*RayCastHit[]*/ hitResult );
	}

	struct PhysXJoint
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_Destroy", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Destroy( IntPtr/*PhysXJoint*/ joint );

		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_GetGlobalPose", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetGlobalPose( IntPtr/*PhysXJoint*/ joint, out Vec3 globalPosition,
			out Quat globalRotation );

		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_IsBroken", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool IsBroken( IntPtr/*PhysXJoint*/ joint );

		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_SetBreakForce", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool SetBreakForce( IntPtr/*PhysXJoint*/ joint, float force, float torque );

		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_SetCollisionEnable", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetCollisionEnable( IntPtr/*PhysXJoint*/ joint,
			[MarshalAs( UnmanagedType.U1 )] bool value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_SetProjectionEnable", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetProjectionEnable( IntPtr/*PhysXJoint*/ joint,
			[MarshalAs( UnmanagedType.U1 )] bool value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXJoint_SetVisualizationEnable", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetVisualizationEnable( IntPtr/*PhysXJoint*/ joint,
			[MarshalAs( UnmanagedType.U1 )] bool value );
	}

	struct PhysXNativeHingeJoint
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXHingeJoint_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXJoint*/ Create( IntPtr/*PhysXBody*/ body0, ref Vec3 localPosition0,
			ref Quat localRotation0, IntPtr/*PhysXBody*/ body1, ref Vec3 localPosition1, ref Quat localRotation1 );

		[DllImport( Wrapper.library, EntryPoint = "PhysXHingeJoint_SetLimit", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetLimit( IntPtr/*PhysXHingeJoint*/ joint,
			[MarshalAs( UnmanagedType.U1 )] bool enabled, float low, float high, float limitContactDistance,
			float restitution, float spring, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXHingeJoint_SetDrive", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetDrive( IntPtr/*PhysXHingeJoint*/ joint, float velocity, float maxForce,
			[MarshalAs( UnmanagedType.U1 )] bool freeSpin, float driveGearRatio );

		[DllImport( Wrapper.library, EntryPoint = "PhysXHingeJoint_GetDrive", CallingConvention = Wrapper.convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		public unsafe static extern bool GetDrive( IntPtr/*PhysXHingeJoint*/ joint, out float velocity, out float maxForce,
			[MarshalAs( UnmanagedType.U1 )] out bool freeSpin, out float driveGearRatio );

		[DllImport( Wrapper.library, EntryPoint = "PhysXHingeJoint_SetProjectionTolerances", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetProjectionTolerances( IntPtr/*PhysXHingeJoint*/ joint, float linear, float angular );
	}

	struct PhysXNativeFixedJoint
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXFixedJoint_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXJoint*/ Create( IntPtr/*PhysXBody*/ body0, ref Vec3 localPosition0,
			ref Quat localRotation0, IntPtr/*PhysXBody*/ body1, ref Vec3 localPosition1, ref Quat localRotation1 );

		[DllImport( Wrapper.library, EntryPoint = "PhysXFixedJoint_SetProjectionTolerances", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetProjectionTolerances( IntPtr/*PhysXFixedJoint*/ joint, float linear, float angular );
	}

	struct PhysXNativeSliderJoint
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXSliderJoint_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXJoint*/ Create( IntPtr/*PhysXBody*/ body0, ref Vec3 localPosition0,
			ref Quat localRotation0, IntPtr/*PhysXBody*/ body1, ref Vec3 localPosition1, ref Quat localRotation1 );

		[DllImport( Wrapper.library, EntryPoint = "PhysXSliderJoint_SetLimit", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetLimit( IntPtr/*PhysXSliderJoint*/ joint, [MarshalAs( UnmanagedType.U1 )] bool enabled,
			float low, float high, float limitContactDistance, float restitution, float spring, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXSliderJoint_SetProjectionTolerances", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetProjectionTolerances( IntPtr/*PhysXSliderJoint*/ joint, float linear, float angular );
	}

	enum PhysXD6Axis
	{
		X,
		Y,
		Z,
		Twist,
		Swing1,
		Swing2,
	}

	enum PhysXD6Motion
	{
		Locked,
		Limited,
		Free
	}

	enum PhysXD6Drive
	{
		X,
		Y,
		Z,
		Swing,
		Twist,
		Slerp,
	}

	struct PhysXNativeD6Joint
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXJoint*/ Create( IntPtr/*PhysXBody*/ body0, ref Vec3 localPosition0,
			ref Quat localRotation0, IntPtr/*PhysXBody*/ body1, ref Vec3 localPosition1, ref Quat localRotation1 );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetMotion", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMotion( IntPtr/*PhysXD6Joint*/ joint, PhysXD6Axis axis, PhysXD6Motion type );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetLinearLimit", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetLinearLimit( IntPtr/*PhysXD6Joint*/ joint, float value,
			float limitContactDistance, float restitution, float spring, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetTwistLimit", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetTwistLimit( IntPtr/*PhysXD6Joint*/ joint, float low, float high,
			float limitContactDistance, float restitution, float spring, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetSwingLimit", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetSwingLimit( IntPtr/*PhysXD6Joint*/ joint, float yAngle, float zAngle,
			float limitContactDistance, float restitution, float spring, float damping );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetDrive", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetDrive( IntPtr/*PhysXD6Joint*/ joint, PhysXD6Drive index, float spring,
			float damping, float forceLimit, [MarshalAs( UnmanagedType.U1 )] bool acceleration );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_GetDrive", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetDrive( IntPtr/*PhysXD6Joint*/ joint, PhysXD6Drive index, out float spring,
			out float damping, out float forceLimit, [MarshalAs( UnmanagedType.U1 )] out bool acceleration );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetDrivePosition", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetDrivePosition( IntPtr/*PhysXD6Joint*/ joint, ref Vec3 position, ref Quat rotation );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_GetDrivePosition", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetDrivePosition( IntPtr/*PhysXD6Joint*/ joint, out Vec3 position, out Quat rotation );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetDriveVelocity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetDriveVelocity( IntPtr/*PhysXD6Joint*/ joint, ref Vec3 linear, ref Vec3 angular );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_GetDriveVelocity", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetDriveVelocity( IntPtr/*PhysXD6Joint*/ joint, out Vec3 linear, out Vec3 angular );

		[DllImport( Wrapper.library, EntryPoint = "PhysXD6Joint_SetProjectionTolerances", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetProjectionTolerances( IntPtr/*PhysXD6Joint*/ joint, float linear, float angular );
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct PhysXNativeDistanceJoint
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXJoint*/ Create( IntPtr/*PhysXBody*/ body0, ref Vec3 localPosition0,
			ref Quat localRotation0, IntPtr/*PhysXBody*/ body1, ref Vec3 localPosition1, ref Quat localRotation1 );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetMinDistanceEnabled", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMinDistanceEnabled( IntPtr/*PhysXDistanceJoint*/ joint, [MarshalAs( UnmanagedType.U1 )] bool value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetMinDistance", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMinDistance( IntPtr/*PhysXDistanceJoint*/ joint, float value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetMaxDistanceEnabled", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMaxDistanceEnabled( IntPtr/*PhysXDistanceJoint*/ joint, [MarshalAs( UnmanagedType.U1 )] bool value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetMaxDistance", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetMaxDistance( IntPtr/*PhysXDistanceJoint*/ joint, float value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetSpringEnabled", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetSpringEnabled( IntPtr/*PhysXDistanceJoint*/ joint, [MarshalAs( UnmanagedType.U1 )] bool value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetSpring", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetSpring( IntPtr/*PhysXDistanceJoint*/ joint, float value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetTolerance", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetTolerance( IntPtr/*PhysXDistanceJoint*/ joint, float value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXDistanceJoint_SetDamping", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetDamping( IntPtr/*PhysXDistanceJoint*/ joint, float value );
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct PhysXNativeMaterial
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXMaterial_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXMaterial*/ Create( float staticFriction, float dynamicFriction, float restitution,
			[MarshalAs( UnmanagedType.LPWStr )] string materialName, [MarshalAs( UnmanagedType.U1 )] bool vehicleDrivableSurface );
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct NativeRayCastResult
	{
		public IntPtr/*PhysXShape*/ shape;//public int shapeIdentifier;
		public Vec3 worldImpact;
		public Vec3 worldNormal;
		public int faceID;
		public float distance;
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct ContactReport
	{
		public int shapeIndex1;
		public int shapeIndex2;
		public Vec3 contactPoint;
		public Vec3 normal;
		public float separation;
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	enum ErrorCode
	{
		NO_ERROR = 0,

		//! \brief An informational message.
		DEBUG_INFO = 1,

		//! \brief a warning message for the user to help with debugging
		DEBUG_WARNING = 2,

		//! \brief method called with invalid parameter(s)
		INVALID_PARAMETER = 4,

		//! \brief method was called at a time when an operation is not possible
		INVALID_OPERATION = 8,

		//! \brief method failed to allocate some memory
		OUT_OF_MEMORY = 16,

		/** \brief The library failed for some reason.
		Possibly you have passed invalid values like NaNs, which are not checked for.
		*/
		INTERNAL_ERROR = 32,

		//! \brief An unrecoverable error, execution should be halted and log output flushed 
		ABORT = 64,

		//! \brief The SDK has determined that an operation may result in poor performance. 
		PERF_WARNING = 128,

		//! \brief The update loader failed to load PhysX3Gpu_xx.dll.
		EXCEPTION_ON_STARTUP = 256
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[UnmanagedFunctionPointer( Wrapper.convention )]
	delegate void ReportErrorDelegate( ErrorCode code, IntPtr message, IntPtr file, int line );

	[UnmanagedFunctionPointer( Wrapper.convention )]
	delegate void LogDelegate( IntPtr message );
}
