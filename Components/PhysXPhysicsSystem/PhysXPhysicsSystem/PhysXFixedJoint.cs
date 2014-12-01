// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXFixedJoint : FixedJoint, IPhysXJoint
	{
		IntPtr nativeJoint;

		//////////////////////////////////////////////////////////

		public PhysXFixedJoint( Body body1, Body body2 )
			: base( body1, body2 )
		{
		}

		public void UpdatePhysXJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = nativeJoint != IntPtr.Zero;

			if( needCreate == created )
				return;

			if( needCreate )
			{
				PhysXBody physXBody0 = (PhysXBody)Body1;
				PhysXBody physXBody1 = (PhysXBody)Body2;

				if( ( !physXBody0.Static || !physXBody1.Static ) &&
					( physXBody0.nativeBody != IntPtr.Zero && physXBody1.nativeBody != IntPtr.Zero ) )
				{
					Vec3 localPosition0 = physXBody0.Rotation.GetInverse() * ( -physXBody0.Position );
					Quat localRotation0 = physXBody0.Rotation.GetInverse();
					Vec3 localPosition1 = physXBody1.Rotation.GetInverse() * ( -physXBody1.Position );
					Quat localRotation1 = physXBody1.Rotation.GetInverse();

					nativeJoint = PhysXNativeWrapper.PhysXNativeFixedJoint.Create( physXBody0.nativeBody, ref localPosition0,
						ref localRotation0, physXBody1.nativeBody, ref localPosition1, ref localRotation1 );

					UpdatePhysXBreakData();
					if( Scene._EnableDebugVisualization )
						SetVisualizationEnable( true );
				}
			}
			else
			{
				DestroyPhysXJoint();
			}
		}

		internal void DestroyPhysXJoint()
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

		protected override void OnSetBroken()
		{
			UpdatePhysXJoint();
		}

		protected override bool OnUpdateBreakState()
		{
			return nativeJoint != IntPtr.Zero && PhysXJoint.IsBroken( nativeJoint );
		}

		protected void UpdatePhysXBreakData()
		{
			if( nativeJoint == IntPtr.Zero )
				return;
			PhysXJoint.SetBreakForce( nativeJoint,
				( BreakMaxForce > 0 ) ? BreakMaxForce * 0.2161234981132075f : PhysXNativeWorld.MAX_REAL,
				( BreakMaxTorque > 0 ) ? BreakMaxTorque * 0.0493827160493827f : PhysXNativeWorld.MAX_REAL );
		}

		protected override void OnSetBreakProperties()
		{
			base.OnSetBreakProperties();
			UpdatePhysXBreakData();
		}

		public void UpdateDataFromLibrary()
		{
		}

		public void SetVisualizationEnable( bool enable )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXJoint.SetVisualizationEnable( nativeJoint, enable );
		}
	}
}
