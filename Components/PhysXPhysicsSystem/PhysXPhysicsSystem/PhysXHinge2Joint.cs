// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXHinge2Joint : Hinge2Joint, IPhysXJoint
	{
		IntPtr nativeJoint;
		Hinge2Axis axis1;
		Hinge2Axis axis2;

		Vec3 axis1LocalAxis = Vec3.Zero;
		Vec3 axis2LocalAxis = Vec3.Zero;

		///////////////////////////////////////////

		public class Hinge2Axis : RotationAxis
		{
			public PhysXHinge2Joint joint;
			int index;

			//

			public Hinge2Axis( int index )
			{
				this.index = index;
			}

			public override Radian GetAngle()
			{
				Log.Warning( "PhysXHinge2Joint: Axis: The method \"GetAngle()\" is not supported." );
				return float.MinValue;
			}
		}

		///////////////////////////////////////////

		public PhysXHinge2Joint( Body body1, Body body2 )
			: base( body1, body2, new Hinge2Axis( 1 ), new Hinge2Axis( 2 ) )
		{
			axis1 = (Hinge2Axis)Axis1;
			axis1.joint = this;
			axis2 = (Hinge2Axis)Axis2;
			axis2.joint = this;
		}

		unsafe void UpdatePhysXJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = nativeJoint != IntPtr.Zero;

			if( needCreate == created )
				return;

			if( needCreate )
			{
				Log.Warning( "PhysXHinge2Joint: Hinge2 joint is not supported by the PhysX Physics System." );
			}
			else
			{
				DestroyPhysXJoint();
			}
		}

		unsafe internal void DestroyPhysXJoint()
		{
			if( nativeJoint != IntPtr.Zero )
			{
				PhysXJoint.Destroy( nativeJoint );
				nativeJoint = IntPtr.Zero;
			}
		}

		protected override void OnUpdatePushedToWorld()
		{
			UpdatePhysXJoint();
		}

		unsafe public void UpdateDataFromLibrary()
		{
			if( nativeJoint == IntPtr.Zero )
				return;

			//Vec3 globalAnchor;
			//NxJoint.getGlobalAnchor( pxJoint, out globalAnchor );
			//Vec3 axis1Direction = Body1.Rotation * axis1LocalAxis;
			//Vec3 axis2Direction = Body1.Rotation * axis2LocalAxis;
			//UpdateDataFromLibrary( ref globalAnchor, ref axis1Direction, ref axis2Direction );
		}

		unsafe public override bool IsStability()
		{
			if( nativeJoint == IntPtr.Zero )
				return true;
			return true;
		}

		protected override void OnSetBroken()
		{
			UpdatePhysXJoint();
		}

		unsafe protected override bool OnUpdateBreakState()
		{
			return ( nativeJoint != IntPtr.Zero );// &&
			//( ( NxJoint.getState( nxJoint ) & NxJointState.NX_JS_BROKEN ) != 0 );
		}

		unsafe protected void UpdatePhysXBreakData()
		{
			if( nativeJoint == IntPtr.Zero )
				return;
			//NxJoint.setBreakable( nxJoint,
			//   ( BreakMaxForce > 0 ) ? BreakMaxForce / 540 : Nx.NX_MAX_REAL,
			//   ( BreakMaxTorque > 0 ) ? BreakMaxTorque / 2160 : Nx.NX_MAX_REAL );
		}

		protected override void OnSetBreakProperties()
		{
			base.OnSetBreakProperties();
			UpdatePhysXBreakData();
		}

		public void SetVisualizationEnable( bool enable )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXJoint.SetVisualizationEnable( nativeJoint, enable );
		}
	}
}
