// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXBallJoint : BallJoint, IPhysXJoint
	{
		IntPtr nativeJoint;
		BallAxis axis1;
		BallAxis axis2;
		BallAxis axis3;

		Vec3 axis1LocalAxis;
		Vec3 axis2LocalAxis;

		///////////////////////////////////////////

		public class BallAxis : RotationAxis
		{
			public PhysXBallJoint joint;
			int index;

			//

			public BallAxis( int index )
			{
				this.index = index;
			}

			public override Radian GetAngle()
			{
				Log.Warning( "PhysXBallJoint: Axis: The method \"GetAngle()\" is not supported." );
				return float.MinValue;
			}
		}

		///////////////////////////////////////////

		public PhysXBallJoint( Body body1, Body body2 )
			: base( body1, body2, new BallAxis( 1 ), new BallAxis( 2 ), new BallAxis( 3 ) )
		{
			axis1 = (BallAxis)Axis1;
			axis1.joint = this;
			axis2 = (BallAxis)Axis2;
			axis2.joint = this;
			axis3 = (BallAxis)Axis3;
			axis3.joint = this;
		}

		void UpdateLimits()
		{
			PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Twist, PhysXD6Motion.Free );
			PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing1, PhysXD6Motion.Free );
			PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing2, PhysXD6Motion.Free );

			if( axis1.LimitsEnabled || axis2.LimitsEnabled )
			{
				float yAngle = 0;
				float zAngle = 0;
				if( axis1.LimitsEnabled )
				{
					if( Math.Abs( axis1.LimitLow + axis1.LimitHigh ) > .001f )
					{
						Log.Warning( "PhysXBallJoint: Different limit values for Ball joint are not supported. " +
							"Equal limits are supported only. Example: LimitLow = -10; LimitHigh = 10." );
					}
					else
					{
						if( axis1.LimitLow == 0 && axis1.LimitHigh == 0 )
							PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing1, PhysXD6Motion.Locked );
						else
						PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing1, PhysXD6Motion.Limited );
						yAngle = Math.Max( Math.Abs( axis1.LimitLow.InRadians() ), Math.Abs( axis1.LimitHigh.InRadians() ) );
					}
				}
				if( axis2.LimitsEnabled )
				{
					if( Math.Abs( axis2.LimitLow + axis2.LimitHigh ) > .001f )
					{
						Log.Warning( "PhysXBallJoint: Different limit values for Ball joint are not supported. " +
							"Equal limits are supported only. Example: LimitLow = -10; LimitHigh = 10." );
					}
					else
					{
						if( axis2.LimitLow == 0 && axis2.LimitHigh == 0 )
							PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing2, PhysXD6Motion.Locked );
						else
						PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing2, PhysXD6Motion.Limited );
						zAngle = Math.Max( Math.Abs( axis2.LimitLow.InRadians() ), Math.Abs( axis2.LimitHigh.InRadians() ) );
					}
				}

				if( axis1.LimitsEnabled && axis2.LimitsEnabled )
				{
					if( Math.Abs( axis1.LimitsRestitution - axis2.LimitsRestitution ) > .001f ||
						Math.Abs( axis1.LimitsSpring - axis2.LimitsSpring ) > .001f ||
						Math.Abs( axis1.LimitsDamping - axis2.LimitsDamping ) > .001f )
					{
						Log.Warning( "PhysXBallJoint: Different limit material properties for axes are not supported. " +
							"Set equal LimitsRestitution, LimitsSpring, LimitsDamping values for first and second axes." );
					}
				}

				float swingRestitution;
				float swingSpring;
				float swingDamping;
				if( axis1.LimitsEnabled )
				{
					swingRestitution = axis1.LimitsRestitution;
					swingSpring = axis1.LimitsSpring;
					swingDamping = axis1.LimitsDamping;
				}
				else
				{
					swingRestitution = axis2.LimitsRestitution;
					swingSpring = axis2.LimitsSpring;
					swingDamping = axis2.LimitsDamping;
				}
				PhysXNativeD6Joint.SetSwingLimit( nativeJoint, yAngle, zAngle, PhysXGeneral.jointLimitContactDistance,
					swingRestitution, swingSpring, swingDamping );
			}

			if( axis3.LimitsEnabled )
			{
				if( axis3.LimitLow == 0 && axis3.LimitHigh == 0 )
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Twist, PhysXD6Motion.Locked );
				else
				PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Twist, PhysXD6Motion.Limited );
				PhysXNativeD6Joint.SetTwistLimit( nativeJoint, axis3.LimitLow.InRadians(), axis3.LimitHigh.InRadians(),
					PhysXGeneral.jointLimitContactDistance, axis3.LimitsRestitution, axis3.LimitsSpring, axis3.LimitsDamping );
			}
		}

		void UpdatePhysXJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = nativeJoint != IntPtr.Zero;

			if( needCreate == created )
				return;

			if( needCreate )
			{
				if( Math.Abs( Vec3.Dot( axis1.Direction, axis2.Direction ) ) > .095f )
				{
					Log.Warning( "BallJoint: Invalid axes." );
					return;
				}
				//if( Axis1.LimitsEnabled && Axis1.LimitLow > Axis1.LimitHigh )
				//{
				//   Log.Warning( "BallJoint: Invalid axis1 limits (low > high)." );
				//   return;
				//}
				//if( Axis2.LimitsEnabled && Axis2.LimitLow > Axis2.LimitHigh )
				//{
				//   Log.Warning( "BallJoint: Invalid axis2 limits (low > high)." );
				//   return;
				//}
				if( Axis3.LimitsEnabled && Axis3.LimitLow > Axis3.LimitHigh )
				{
					Log.Warning( "BallJoint: Invalid axis3 limits (low > high)." );
					return;
				}

				PhysXBody physXBody0 = (PhysXBody)Body1;
				PhysXBody physXBody1 = (PhysXBody)Body2;

				if( ( !physXBody0.Static || !physXBody1.Static ) &&
					( physXBody0.nativeBody != IntPtr.Zero && physXBody1.nativeBody != IntPtr.Zero ) )
				{
					axis1LocalAxis = Body1.Rotation.GetInverse() * axis1.Direction;
					axis2LocalAxis = Body1.Rotation.GetInverse() * axis2.Direction;

					Mat3 axisMatrix = new Mat3( axis1.Direction, -Vec3.Cross( axis1.Direction, axis2.Direction ), axis2.Direction ) *
						Mat3.FromRotateByZ( MathFunctions.PI / 2 );
					Quat globalAxisRotation = axisMatrix.ToQuat().GetNormalize();
					//Quat globalAxisRotation = new Mat3( axis1.Direction, -Vec3.Cross( axis1.Direction, axis2.Direction ),
					//   axis2.Direction ).ToQuat().GetNormalize();

					Vec3 localPosition0 = physXBody0.Rotation.GetInverse() * ( Anchor - physXBody0.Position );
					Quat localRotation0 = ( physXBody0.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();
					Vec3 localPosition1 = physXBody1.Rotation.GetInverse() * ( Anchor - physXBody1.Position );
					Quat localRotation1 = ( physXBody1.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();

					nativeJoint = PhysXNativeWrapper.PhysXNativeD6Joint.Create(
						physXBody0.nativeBody, ref localPosition0, ref localRotation0,
						physXBody1.nativeBody, ref localPosition1, ref localRotation1 );

					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.X,
						PhysX_MotionLockedAxisX ? PhysXD6Motion.Locked : PhysXD6Motion.Free );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Y,
						PhysX_MotionLockedAxisY ? PhysXD6Motion.Locked : PhysXD6Motion.Free );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Z,
						PhysX_MotionLockedAxisZ ? PhysXD6Motion.Locked : PhysXD6Motion.Free );
					//PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.X, PhysXD6Motion.Locked );
					//PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Y, PhysXD6Motion.Locked );
					//PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Z, PhysXD6Motion.Locked );

					UpdateLimits();
					if( ContactsEnabled )
						PhysXNativeWrapper.PhysXJoint.SetCollisionEnable( nativeJoint, true );
					UpdatePhysXBreakData();
					if( Scene._EnableDebugVisualization )
						SetVisualizationEnable( true );

					//PhysXNativeWrapper.PhysXJoint.SetProjectionEnable( nativeJoint, true );
					//desc.projectionMode = NX_JPM_POINT_MINDIST;
					//desc.projectionDistance = 0.1f;
					//desc->projectionAngle = Radian(Degree(3));
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

			Vec3 globalAnchor;
			Quat globalRotation;
			PhysXJoint.GetGlobalPose( nativeJoint, out globalAnchor, out globalRotation );
			Vec3 axis1Direction = Body1.Rotation * axis1LocalAxis;
			Vec3 axis2Direction = Body1.Rotation * axis2LocalAxis;
			UpdateDataFromLibrary( ref globalAnchor, ref axis1Direction, ref axis2Direction );
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

		public void SetVisualizationEnable( bool enable )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXJoint.SetVisualizationEnable( nativeJoint, enable );
		}

		public override void PhysX_SetDrive( PhysX_Drive index, float spring, float damping, float forceLimit, bool acceleration )
		{
			if( nativeJoint != IntPtr.Zero )
			{
				PhysXNativeWrapper.PhysXNativeD6Joint.SetDrive( nativeJoint, (PhysXNativeWrapper.PhysXD6Drive)index, spring,
					damping, forceLimit, acceleration );
			}
		}

		public override void PhysX_GetDrive( PhysX_Drive index, out float spring, out float damping, out float forceLimit,
			out bool acceleration )
		{
			if( nativeJoint == IntPtr.Zero )
			{
				spring = 0;
				damping = 0;
				forceLimit = 0;
				acceleration = false;
				return;
			}
			PhysXNativeWrapper.PhysXNativeD6Joint.GetDrive( nativeJoint, (PhysXNativeWrapper.PhysXD6Drive)index, out spring,
				out damping, out forceLimit, out acceleration );
		}

		public override void PhysX_SetDrivePosition( Vec3 position, Quat rotation )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXNativeWrapper.PhysXNativeD6Joint.SetDrivePosition( nativeJoint, ref position, ref rotation );
		}

		public override void PhysX_GetDrivePosition( out Vec3 position, out Quat rotation )
		{
			if( nativeJoint == IntPtr.Zero )
			{
				position = Vec3.Zero;
				rotation = Quat.Identity;
				return;
			}
			PhysXNativeWrapper.PhysXNativeD6Joint.GetDrivePosition( nativeJoint, out position, out rotation );
		}

		public override void PhysX_SetDriveVelocity( Vec3 linear, Vec3 angular )
		{
			if( nativeJoint != IntPtr.Zero )
				PhysXNativeWrapper.PhysXNativeD6Joint.SetDriveVelocity( nativeJoint, ref linear, ref angular );
		}

		public override void PhysX_GetDriveVelocity( out Vec3 linear, out Vec3 angular )
		{
			if( nativeJoint == IntPtr.Zero )
			{
				linear = Vec3.Zero;
				angular = Vec3.Zero;
				return;
			}
			PhysXNativeWrapper.PhysXNativeD6Joint.GetDriveVelocity( nativeJoint, out linear, out angular );
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

		protected override void OnUpdatePhysXMotionLockedAxisParameters()
		{
			base.OnUpdatePhysXMotionLockedAxisParameters();

			if( nativeJoint != IntPtr.Zero )
			{
				PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.X,
					PhysX_MotionLockedAxisX ? PhysXD6Motion.Locked : PhysXD6Motion.Free );
				PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Y,
					PhysX_MotionLockedAxisY ? PhysXD6Motion.Locked : PhysXD6Motion.Free );
				PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Z,
					PhysX_MotionLockedAxisZ ? PhysXD6Motion.Locked : PhysXD6Motion.Free );
			}
		}
	}
}
