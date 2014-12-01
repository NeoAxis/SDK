// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine;
using Engine.PhysicsSystem;
using Engine.Utils;
using Engine.FileSystem;
using Engine.MathEx;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXPhysicsWorld : PhysicsWorld
	{
		const float skinWidth = .01f;

		//

		static PhysXPhysicsWorld instance;

		ReportErrorDelegate reportErrorDelegate;
		LogDelegate logDelegate;

		bool supportHeightFields = true;
		bool supportVehicles = true;
		internal bool writeCacheForCookedTriangleMeshes = true;
		int mainSceneMaxThreads = 999;

		static bool preventLogErrors;


		public HACDWrapper.Instance hacdInstance;

		//the dictionary of shapes is used for getting managed Shape object from native PhysXNativeShape.
		internal IntegerKeyDictionary<Shape> shapesDictionary = new IntegerKeyDictionary<Shape>( 64 );

		///////////////////////////////////////////

		public class MeshGeometryPhysXData
		{
			public IntPtr/*PhysXConvexMesh*/[] convexMeshes;
			public IntPtr/*PhysXTriangleMesh*/ triangleMesh;
			public int checkRefCounter;
		}

		///////////////////////////////////////////

		public override string DriverName
		{
			get
			{
				int major, minor, bugfix;
				PhysXNativeWorld.GetSDKVersion( out major, out minor, out bugfix );
				return String.Format( "NVIDIA PhysX {0}.{1}.{2}", major, minor, bugfix );
			}
		}

		unsafe public override float MaxAngularVelocity
		{
			get { return base.MaxAngularVelocity; }
			set
			{
				if( base.MaxAngularVelocity == value )
					return;
				base.MaxAngularVelocity = value;

				foreach( PhysXPhysicsScene scene in Scenes )
				{
					foreach( PhysXBody body in scene.Bodies )
						body.UpdateMaxAngularVelocity();
				}
			}
		}

		public override bool IsHeightFieldShapeSupported
		{
			get { return supportHeightFields; }
		}

		public override bool IsVehicleSupported
		{
			get { return supportVehicles; }
		}

		public override bool IsPerTriangleMaterialsForTriangleMeshSupported
		{
			get { return true; }
		}

		unsafe public override void SetShapePairFlags( Shape shape1, Shape shape2, ShapePairFlags flags )
		{
			base.SetShapePairFlags( shape1, shape2, flags );

			Body body1 = shape1.Body;
			Body body2 = shape2.Body;

			if( body1.PushedToWorld && body2.PushedToWorld )
			{
				PhysXBody physXBody1 = (PhysXBody)body1;
				PhysXBody physXBody2 = (PhysXBody)body2;

				if( physXBody1.nativeBody != IntPtr.Zero && physXBody2.nativeBody != IntPtr.Zero )
				{
					PhysXBody.ShapeData shapeData1 = physXBody1.shapesData[ shape1.BodyIndex ];
					PhysXBody.ShapeData shapeData2 = physXBody2.shapesData[ shape2.BodyIndex ];

					bool disableContacts = ( flags & ShapePairFlags.DisableContacts ) != 0;

					foreach( IntPtr nativeShape1 in shapeData1.nativeShapes )
					{
						foreach( IntPtr nativeShape2 in shapeData2.nativeShapes )
						{
							PhysXNativeScene.SetShapePairFlags( physXBody1.scene.nativeScene, nativeShape1, nativeShape2,
								disableContacts );
						}
					}
				}
			}
		}

		static void ReportError( ErrorCode code, IntPtr pMessage, IntPtr pFile, int line )
		{
			string message = Wrapper.GetOutString( pMessage );
			string file = Wrapper.GetOutString( pFile );
			if( file == null )
				file = "NULL";

			string codeText;
			switch( code )
			{
			case ErrorCode.DEBUG_INFO: codeText = "Info"; break;
			case ErrorCode.DEBUG_WARNING: codeText = "Warning"; break;
			case ErrorCode.INVALID_PARAMETER: codeText = "Invalid parameter"; break;
			case ErrorCode.INVALID_OPERATION: codeText = "Invalid operation"; break;
			case ErrorCode.OUT_OF_MEMORY: codeText = "Out of memory"; break;
			case ErrorCode.INTERNAL_ERROR: codeText = "Internal error"; break;
			case ErrorCode.ABORT: codeText = "Unrecoverable error"; break;
			case ErrorCode.PERF_WARNING: codeText = "Performance warning"; break;
			case ErrorCode.EXCEPTION_ON_STARTUP: codeText = "Exception on startup"; break;
			default: codeText = "Unknown error"; break;
			}

			string text = string.Format( "PhysXPhysicsSystem: {0} : {1} ({2}:{3})", codeText, message, file, line );

			if( code < ErrorCode.DEBUG_INFO )
			{
				if( preventLogErrors )
					Log.Info( text );
				else
					Log.Error( text );
			}

			//bool cannotAllocateCUDA = message.Contains( "cannot allocate CUDA device memory" ) ||
			//   message.Contains( "cannot allocate CUDA host memory" );
			//if( cannotAllocateCUDA )
			//{
			//   Log.Info( "PhysX: Unable to initialize hardware acceleration mode. " +
			//      "Using software mode. Error: \"Cannot allocate CUDA host memory!\"." );

			//text = "PhysX: Unable to initialize hardware acceleration mode. Using software mode.\n" +
			//   "\n" +
			//   "Possible Reason: Another application is accessing the PhysX hardware already.\n" +
			//   "\n" +
			//   "You can disable hardware acceleration into Windows Control Panel -> NVIDIA PhysX.\n" +
			//   "\n" +
			//   "(Internal error: Cannot allocate CUDA device memory).";
			//Log.Warning( text );
			//}
			//else
			//{
			//   if( code < NxErrorCode.NXE_DB_INFO )
			//      Log.Error( text );
			//}
		}

		static void LogMessage( IntPtr pMessage )
		{
			string message = Wrapper.GetOutString( pMessage );
			Log.Info( "PhysX: " + message );
		}

		//static NxAssertResponse ReportAssertViolation( IntPtr pMessage, IntPtr pFile, int line )
		//{
		//   string message = Wrapper.GetOutString( pMessage );
		//   string file = Wrapper.GetOutString( pFile );

		//   if( file == null )
		//      file = "NULL";
		//   string text = string.Format( "PhysXPhysicsSystem: {0} ({1}:{2})", message, file, line );

		//   Log.Fatal( text );

		//   return NxAssertResponse.NX_AR_BREAKPOINT;
		//}

		protected override bool OnInitLibrary( bool allowHardwareAcceleration, bool editor )
		{
			instance = this;

			NativeLibraryManager.PreLoadLibrary( "PhysXNativeWrapper" );

			//change current directory for loading PhysX dlls from specified NativeDlls directory.
			string saveCurrentDirectory = null;
			if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
			{
				saveCurrentDirectory = Directory.GetCurrentDirectory();
				Directory.SetCurrentDirectory( NativeLibraryManager.GetNativeLibrariesDirectory() );
			}

			try
			{
				preventLogErrors = true;

				reportErrorDelegate = ReportError;
				logDelegate = LogMessage;
				IntPtr errorStringPtr;
				if( !PhysXNativeWorld.Init( reportErrorDelegate, out errorStringPtr, logDelegate, skinWidth ) )
				{
					string errorString = Wrapper.GetOutString( errorStringPtr );
					if( string.IsNullOrEmpty( errorString ) )
						errorString = "Unknown error.";
					Log.Fatal( "PhysX: Initialization error: " + errorString );
					return false;
				}

				preventLogErrors = false;
			}
			finally
			{
				//restore current directory
				if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
					Directory.SetCurrentDirectory( saveCurrentDirectory );
			}

			//configs
			if( VirtualFile.Exists( "Base/Constants/PhysicsSystem.config" ) )
			{
				TextBlock block = TextBlockUtils.LoadFromVirtualFile( "Base/Constants/PhysicsSystem.config" );
				if( block != null )
				{
					TextBlock physXBlock = block.FindChild( "physXSpecific" );
					if( physXBlock != null )
					{
						if( physXBlock.IsAttributeExist( "supportHeightFields" ) )
							supportHeightFields = bool.Parse( physXBlock.GetAttribute( "supportHeightFields" ) );

						if( physXBlock.IsAttributeExist( "supportVehicles" ) )
							supportVehicles = bool.Parse( physXBlock.GetAttribute( "supportVehicles" ) );

						if( physXBlock.IsAttributeExist( "writeCacheForCookedTriangleMeshes" ) )
						{
							writeCacheForCookedTriangleMeshes = bool.Parse(
								physXBlock.GetAttribute( "writeCacheForCookedTriangleMeshes" ) );
						}

						if( physXBlock.IsAttributeExist( "mainSceneMaxThreads" ) )
							mainSceneMaxThreads = int.Parse( physXBlock.GetAttribute( "mainSceneMaxThreads" ) );
					}
				}
			}

			return true;
		}

		protected override void OnShutdownLibrary()
		{
			if( shapesDictionary.Count != 0 )
				Log.Warning( "PhysXPhysicsWorld: OnShutdownLibrary: shapesDictionary.Count != 0." );

			PhysXNativeWorld.Destroy();

			if( hacdInstance != null )
			{
				try
				{
					HACDWrapper.Shutdown( hacdInstance );
				}
				catch { }
				hacdInstance = null;
			}

			instance = null;
		}

		unsafe protected override void _OnMeshGeometryDestroy( _MeshGeometry geometry )
		{
			base._OnMeshGeometryDestroy( geometry );

			if( geometry.UserData != null )
			{
				MeshGeometryPhysXData data = (MeshGeometryPhysXData)geometry.UserData;

				if( data.checkRefCounter != 0 )
					Log.Fatal( "PhysXPhysicsWorld: OnMeshGeometryDestroy: data.checkRefCounter != 0." );

				if( data.convexMeshes != null )
				{
					foreach( IntPtr mesh in data.convexMeshes )
					{
						if( mesh != IntPtr.Zero )
							PhysXNativeWorld.ReleaseConvexMesh( mesh );
					}
					data.convexMeshes = null;
				}
				if( data.triangleMesh != IntPtr.Zero )
				{
					PhysXNativeWorld.ReleaseTriangleMesh( data.triangleMesh );
					data.triangleMesh = IntPtr.Zero;
				}
			}
		}

		public static new PhysXPhysicsWorld Instance
		{
			get { return instance; }
		}

		internal void DestroyPhysXJoint( Joint joint )
		{
			switch( joint.JointType )
			{
			case Joint.Type.Hinge:
				( (PhysXHingeJoint)joint ).DestroyPhysXJoint();
				break;

			case Joint.Type.Universal:
				( (PhysXUniversalJoint)joint ).DestroyPhysXJoint();
				break;

			case Joint.Type.Hinge2:
				( (PhysXHinge2Joint)joint ).DestroyPhysXJoint();
				break;

			case Joint.Type.Ball:
				( (PhysXBallJoint)joint ).DestroyPhysXJoint();
				break;

			case Joint.Type.Slider:
				( (PhysXSliderJoint)joint ).DestroyPhysXJoint();
				break;

			case Joint.Type.Fixed:
				( (PhysXFixedJoint)joint ).DestroyPhysXJoint();
				break;

			case Joint.Type._Custom1:
				( (_Custom1Joint)joint ).DestroyPhysXJoint();
				break;

			//implement another custom joints.

			default:
				Log.Fatal( "PhysXPhysicsWorld: DestroyPhysXJoint: Joint type is not implemented." );
				break;
			}
		}

		public override bool IsCustomShapeImplemented( Shape.Type shapeType )
		{
			//if( shapeType == Shape.Type._Custom1 )
			//	return true;

			return false;
		}

		protected override Shape OnCreateCustomShape( Shape.Type shapeType )
		{
			if( shapeType == Shape.Type._Custom1 )
				return new _Custom1Shape();

			return null;
		}

		public override bool IsCustomJointImplemented( Joint.Type jointType )
		{
			//uncomment it to enable _Custom1Joint joint class.
			//if( jointType == Joint.Type._Custom1 )
			//   return true;

			return false;
		}

		protected override unsafe ConvexHullDecompositionDataItem[] OnConvexHullDecomposite(
			Vec3[] vertices, int[] indices, int maxTrianglesInDecimatedMesh, int maxVerticesPerConvexHull )
		{
			if( hacdInstance == null )
				hacdInstance = HACDWrapper.Init();

			double[] points = new double[ vertices.Length * 3 ];
			for( int n = 0; n < vertices.Length; n++ )
			{
				points[ n * 3 + 0 ] = vertices[ n ].X;
				points[ n * 3 + 1 ] = vertices[ n ].Y;
				points[ n * 3 + 2 ] = vertices[ n ].Z;
			}

			bool result;
			fixed( double* pPoints = points )
			{
				fixed( int* pIndices = indices )
				{
					result = HACDWrapper.Compute( hacdInstance, pPoints, vertices.Length, pIndices,
						indices.Length / 3, maxTrianglesInDecimatedMesh, maxVerticesPerConvexHull );
				}
			}

			if( !result )
				return null;

			int clusterCount = HACDWrapper.GetClusterCount( hacdInstance );
			ConvexHullDecompositionDataItem[] items = new ConvexHullDecompositionDataItem[ clusterCount ];

			for( int nCluster = 0; nCluster < clusterCount; nCluster++ )
			{
				int clusterPointCount;
				int clusterTriangleCount;
				HACDWrapper.GetBufferSize( hacdInstance, nCluster, out clusterPointCount,
					out clusterTriangleCount );

				double[] clusterPoints = new double[ clusterPointCount * 3 ];
				int[] clusterIndices = new int[ clusterTriangleCount * 3 ];

				fixed( double* pPoints = clusterPoints )
				{
					fixed( int* pIndices = clusterIndices )
					{
						HACDWrapper.GetBuffer( hacdInstance, nCluster, pPoints, pIndices );
					}
				}

				Vec3[] clusterVertices = new Vec3[ clusterPointCount ];
				for( int n = 0; n < clusterPointCount; n++ )
				{
					clusterVertices[ n ] = new Vec3(
						(float)clusterPoints[ n * 3 + 0 ],
						(float)clusterPoints[ n * 3 + 1 ],
						(float)clusterPoints[ n * 3 + 2 ] );
				}

				items[ nCluster ] = new ConvexHullDecompositionDataItem( clusterVertices, clusterIndices );
			}

			HACDWrapper.ClearComputed( hacdInstance );

			return items;
		}

		protected override PhysicsScene OnCreateScene( string description, int numThreads, uint[] affinityMasks )
		{
			if( MainScene == null )
				numThreads = Math.Min( Environment.ProcessorCount, mainSceneMaxThreads );

			PhysXPhysicsScene scene = new PhysXPhysicsScene( description );
			scene.Create( numThreads, affinityMasks );
			return scene;
		}

		protected override void OnDestroyScene( PhysicsScene scene )
		{
			PhysXPhysicsScene scene2 = (PhysXPhysicsScene)scene;
			scene2.Destroy();
		}

		internal Shape GetShapeByIdentifier( int identifier )
		{
			return shapesDictionary[ identifier ];
		}

		internal Shape GetShapeByNativePointer( IntPtr pShape )
		{
			return shapesDictionary[ PhysXNativeShape.GetIdentifier( pShape ) ];
		}
	}
}
