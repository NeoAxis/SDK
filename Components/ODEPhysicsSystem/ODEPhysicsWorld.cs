// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine;
using Engine.FileSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;
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

	sealed class ODEPhysicsWorld : PhysicsWorld
	{
		static ODEPhysicsWorld instance;

		internal int defaultMaxIterationCount = 20;
		internal int hashSpaceMinLevel = 2;// 2^2 = 4 minimum cell size
		internal int hashSpaceMaxLevel = 8;// 2^8 = 256 maximum cell size

		///////////////////////////////////////////

		public class MeshGeometryODEData
		{
			public dTriMeshDataID triMeshDataID;
			public IntPtr vertices;//3 floats per vertex
			public int verticesCount;
			public IntPtr indices;
			public int indicesCount;

			public int checkRefCounter;
		}

		///////////////////////////////////////////

		//public class ConvexGeometryODEData
		//{
		//   leaks

		//   public IntPtr planes;
		//   public int planeCount;
		//   public IntPtr points;
		//   public int pointCount;
		//   public IntPtr polygons;

		//   public int checkRefCounter;
		//}

		///////////////////////////////////////////

		internal new static ODEPhysicsWorld Instance
		{
			get { return instance; }
		}

		protected override bool OnInitLibrary( bool allowHardwareAcceleration, bool editor )
		{
			instance = this;

			NativeLibraryManager.PreLoadLibrary( "ode" );

			//int maxIterationCount = 20;
			//int hashSpaceMinLevel = 2;// 2^2 = 4 minimum cell size
			//int hashSpaceMaxLevel = 8;// 2^8 = 256 maximum cell size

			if( VirtualFile.Exists( "Base/Constants/PhysicsSystem.config" ) )
			{
				TextBlock block = TextBlockUtils.LoadFromVirtualFile( "Base/Constants/PhysicsSystem.config" );
				if( block != null )
				{
					TextBlock odeBlock = block.FindChild( "odeSpecific" );
					if( odeBlock != null )
					{
						if( odeBlock.IsAttributeExist( "maxIterationCount" ) )
							defaultMaxIterationCount = int.Parse( odeBlock.GetAttribute( "maxIterationCount" ) );
						if( odeBlock.IsAttributeExist( "hashSpaceMinLevel" ) )
							hashSpaceMinLevel = int.Parse( odeBlock.GetAttribute( "hashSpaceMinLevel" ) );
						if( odeBlock.IsAttributeExist( "hashSpaceMaxLevel" ) )
							hashSpaceMaxLevel = int.Parse( odeBlock.GetAttribute( "hashSpaceMaxLevel" ) );
					}
				}
			}

			Ode.dInitODE2( 0 );

			return true;
		}

		protected override void OnShutdownLibrary()
		{
			Ode.dCloseODE();

			instance = null;
		}

		public override string DriverName
		{
			get { return "ODE 0.11.1"; }
		}

		public override float MaxAngularVelocity
		{
			get { return base.MaxAngularVelocity; }
			set
			{
				base.MaxAngularVelocity = value;

				foreach( ODEPhysicsScene scene in Scenes )
					scene.UpdateMaxAngularSpeed();
			}
		}

		protected override void _OnMeshGeometryDestroy( _MeshGeometry geometry )
		{
			base._OnMeshGeometryDestroy( geometry );

			if( geometry.UserData != null )
			{
				MeshGeometryODEData data = (MeshGeometryODEData)geometry.UserData;

				if( data.checkRefCounter != 0 )
					Log.Fatal( "ODEPhysicsWorld: OnMeshGeometryDestroy: No destroyed mesh geometry." );

				Ode.dFree( data.vertices,
					(uint)( data.verticesCount * Marshal.SizeOf( typeof( float ) ) * 3 ) );
				Ode.dFree( data.indices,
					(uint)( data.indicesCount * Marshal.SizeOf( typeof( int ) ) ) );

				Ode.dGeomTriMeshDataDestroy( data.triMeshDataID );
			}
		}

		public override void SetShapePairFlags( Shape shape1, Shape shape2, ShapePairFlags flags )
		{
			base.SetShapePairFlags( shape1, shape2, flags );

			ODEBody body1 = (ODEBody)shape1.Body;
			ODEBody body2 = (ODEBody)shape2.Body;

			ODEBody.GeomData geomData1 = body1.GetGeomDataByShape( shape1 );
			ODEBody.GeomData geomData2 = body2.GetGeomDataByShape( shape2 );

			if( geomData1 != null && geomData2 != null )
			{
				dGeomID geomID1 = ( geomData1.transformID != dGeomID.Zero ) ?
					geomData1.transformID : geomData1.geomID;
				dGeomID geomID2 = ( geomData2.transformID != dGeomID.Zero ) ?
					geomData2.transformID : geomData2.geomID;

				bool value = ( flags & ShapePairFlags.DisableContacts ) != 0;
				Ode.SetShapePairDisableContacts( geomID1, geomID2, value );
			}
		}

		internal void DestroyODEJoint( Joint joint )
		{
			switch( joint.JointType )
			{
			case Joint.Type.Hinge:
				( (ODEHingeJoint)joint ).DestroyODEJoint();
				break;

			case Joint.Type.Universal:
				( (ODEUniversalJoint)joint ).DestroyODEJoint();
				break;

			case Joint.Type.Hinge2:
				( (ODEHinge2Joint)joint ).DestroyODEJoint();
				break;

			case Joint.Type.Ball:
				( (ODEBallJoint)joint ).DestroyODEJoint();
				break;

			case Joint.Type.Slider:
				( (ODESliderJoint)joint ).DestroyODEJoint();
				break;

			case Joint.Type.Fixed:
				( (ODEFixedJoint)joint ).DestroyODEJoint();
				break;

			default:
				Trace.Assert( false );
				break;
			}
		}

		public override bool IsCustomShapeImplemented( Shape.Type shapeType )
		{
			return false;
		}

		protected override Shape OnCreateCustomShape( Shape.Type shapeType )
		{
			return null;
		}

		public override bool IsCustomJointImplemented( Joint.Type jointType )
		{
			return false;
		}

		protected override PhysicsScene OnCreateScene( string description, int numThreads, uint[] affinityMasks )
		{
			ODEPhysicsScene scene = new ODEPhysicsScene( description );
			scene.Create();
			return scene;
		}

		protected override void OnDestroyScene( PhysicsScene scene )
		{
			ODEPhysicsScene scene2 = (ODEPhysicsScene)scene;
			scene2.Destroy();
		}
	}
}
