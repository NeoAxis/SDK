// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXHingeJoint : HingeJoint, IPhysXJoint
	{
		IntPtr nativeJoint;
		HingeAxis axis;
		Quat initialRelativeRotationInverse;

		//////////////////////////////////////////////////////////

		public class HingeAxis : RotationAxis
		{
			public PhysXHingeJoint joint;

			//

			public override Radian GetAngle()
			{
				Quat relativeRotation = ( (PhysXHingeJoint)Joint ).initialRelativeRotationInverse *
					( Joint.Body2.Rotation * Joint.Body1.Rotation.GetInverse() );

				float cost2 = relativeRotation.W;
				float sint2 = MathFunctions.Sqrt(
					relativeRotation.X * relativeRotation.X +
					relativeRotation.Y * relativeRotation.Y +
					relativeRotation.Z * relativeRotation.Z );
				float dot = relativeRotation.X * Direction.X + relativeRotation.Y * Direction.Y + relativeRotation.Z * Direction.Z;
				float theta = ( dot >= 0 ) ?
					( 2.0f * MathFunctions.ATan( sint2, cost2 ) ) :  // if u points in direction of axis
					( 2.0f * MathFunctions.ATan( sint2, -cost2 ) );  // if u points in opposite direction

				// the angle we get will be between 0..2*pi, but we want to return angles between -pi..pi
				if( theta > MathFunctions.PI )
					theta -= 2.0f * MathFunctions.PI;

				//// the angle we've just extracted has the wrong sign
				//theta = -theta;

				return theta;
			}

			public override void SetDesiredVelocity( Radian velocity, float maxTorque )
			{
				joint.PhysX_SetDrive( velocity, maxTorque, false, 1 );
			}

			public override bool IsDesiredVelocitySupported()
			{
				return true;
			}
		}

		///////////////////////////////////////////

		public PhysXHingeJoint( Body body1, Body body2 )
			: base( body1, body2, new HingeAxis() )
		{
			axis = (HingeAxis)Axis;
			axis.joint = this;
		}

		void UpdateLimits()
		{
			if( axis.LimitsEnabled )
			{
				PhysXNativeWrapper.PhysXNativeHingeJoint.SetLimit( nativeJoint, true, axis.LimitLow.InRadians(),
					axis.LimitHigh.InRadians(), PhysXGeneral.jointLimitContactDistance, axis.LimitsRestitution,
					axis.LimitsSpring, axis.LimitsDamping );
			}
			else
				PhysXNativeWrapper.PhysXNativeHingeJoint.SetLimit( nativeJoint, false, 0, 0, 0, 0, 0, 0 );
		}

		public void UpdatePhysXJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = nativeJoint != IntPtr.Zero;

			if( needCreate == created )
				return;

			if( needCreate )
			{
				if( Axis.LimitsEnabled && Axis.LimitLow > Axis.LimitHigh )
				{
					Log.Warning( "HingeJoint: Invalid axis limits (low > high)." );
					return;
				}

				PhysXBody physXBody0 = (PhysXBody)Body1;
				PhysXBody physXBody1 = (PhysXBody)Body2;

				if( ( !physXBody0.Static || !physXBody1.Static ) &&
					( physXBody0.nativeBody != IntPtr.Zero && physXBody1.nativeBody != IntPtr.Zero ) )
				{
					Quat globalAxisRotation = Quat.FromDirectionZAxisUp( Axis.Direction );
					Vec3 localPosition0 = physXBody0.Rotation.GetInverse() * ( Anchor - physXBody0.Position );
					Quat localRotation0 = ( physXBody0.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();
					Vec3 localPosition1 = physXBody1.Rotation.GetInverse() * ( Anchor - physXBody1.Position );
					Quat localRotation1 = ( physXBody1.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();

					nativeJoint = PhysXNativeWrapper.PhysXNativeHingeJoint.Create(
						physXBody0.nativeBody, ref localPosition0, ref localRotation0,
						physXBody1.nativeBody, ref localPosition1, ref localRotation1 );

					UpdateLimits();

					if( ContactsEnabled )
						PhysXNativeWrapper.PhysXJoint.SetCollisionEnable( nativeJoint, true );
					UpdatePhysXBreakData();
					if( Scene._EnableDebugVisualization )
						SetVisualizationEnable( true );

					//PhysXNativeWrapper.PhysXJoint.SetProjectionEnable( nativeJoint, true );
					//desc->projectionMode = NxJointProjectionMode.NX_JPM_POINT_MINDIST;
					//desc->projectionDistance = 0.1f;
					//desc->projectionAngle = new Radian( new Degree( 3 ) );

					initialRelativeRotationInverse = ( physXBody1.Rotation * physXBody0.Rotation.GetInverse() ).GetInverse();
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

		public void UpdateDataFromLibrary()
		{
			if( nativeJoint == IntPtr.Zero )
				return;

			Vec3 position;
			Quat rotation;
			PhysXJoint.GetGlobalPose( nativeJoint, out position, out rotation );
			Vec3 axis = Vec3.XAxis * rotation;
			UpdateDataFromLibrary( ref position, ref axis );
		}

		public override bool IsStability()
		{
			if( nativeJoint == IntPtr.Zero )
				return true;
			return true;
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

		public override void PhysX_SetDrive( float targetVelocity, float maxForce, bool freeSpin, float driveGearRatio )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXNativeHingeJoint.SetDrive( nativeJoint, targetVelocity, maxForce, freeSpin, driveGearRatio );
		}

		public override bool PhysX_GetDrive( out float targetVelocity, out float maxForce, out bool freeSpin,
			out float driveGearRatio )
		{
			if( nativeJoint == IntPtr.Zero )
			{
				targetVelocity = 0;
				maxForce = 0;
				freeSpin = false;
				driveGearRatio = 0;
				return false;
			}
			return PhysXNativeHingeJoint.GetDrive( nativeJoint, out targetVelocity, out maxForce, out freeSpin, out driveGearRatio );
		}

		public void SetVisualizationEnable( bool enable )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXJoint.SetVisualizationEnable( nativeJoint, enable );
		}

		protected override void OnUpdateAxisParameter( BaseAxis axis, UpdateAxisParameters parameter )
		{
			base.OnUpdateAxisParameter( axis, parameter );

			if( nativeJoint != IntPtr.Zero )
			{
				if( parameter == UpdateAxisParameters.LimitsEnabled || parameter == UpdateAxisParameters.LimitsRestitution ||
					parameter == UpdateAxisParameters.LimitsSpring || parameter == UpdateAxisParameters.LimitsDamping ||
					parameter == UpdateAxisParameters.LimitLow || parameter == UpdateAxisParameters.LimitHigh )
				{
					UpdateLimits();
				}
			}
		}

		protected override void OnUpdateContactsEnabled()
		{
			base.OnUpdateContactsEnabled();
			if( nativeJoint != IntPtr.Zero )
				PhysXNativeWrapper.PhysXJoint.SetCollisionEnable( nativeJoint, ContactsEnabled );
		}
	}
}
