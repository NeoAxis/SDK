// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	class PhysXSliderJoint : SliderJoint, IPhysXJoint
	{
		IntPtr nativeJoint;
		SliderAxis axis;

		//////////////////////////////////////////////////////////

		public class SliderAxis : TranslationAxis
		{
			internal PhysXSliderJoint joint;
		}

		//////////////////////////////////////////////////////////

		public PhysXSliderJoint( Body body1, Body body2 )
			: base( body1, body2, new SliderAxis() )
		{
			axis = (SliderAxis)Axis;
			axis.joint = this;
		}

		void UpdateLimits()
		{
			PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.X, PhysXD6Motion.Free );

			if( axis.LimitsEnabled )
			{
				if( Math.Abs( axis.LimitLow + axis.LimitHigh ) > .001f )
				{
					Log.Warning( "PhysXSliderJoint: Different limit values for Slider joint are not supported. " +
						"Equal limits are supported only. Example: LimitLow = -3; LimitHigh = 3." );
				}
				else
				{
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.X, PhysXD6Motion.Limited );
					PhysXNativeD6Joint.SetLinearLimit( nativeJoint, axis.LimitHigh, PhysXGeneral.jointLimitContactDistance, 
						axis.LimitsRestitution, axis.LimitsSpring, axis.LimitsDamping );
				}
				//PhysXNativeD6Joint.SetLinearLimit( nativeJoint, ( globalAnchor - low ).Length(),
				//   PhysXGeneral.jointLimitContactDistance, axis.LimitsBounciness, axis.LimitsHardness );
				//PhysXNativeD6Joint.SetLinearLimit( nativeJoint, diff / 2, PhysXGeneral.jointLimitContactDistance,
				//   axis.LimitsBounciness, axis.LimitsHardness );
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
				PhysXBody physXBody0 = (PhysXBody)Body1;
				PhysXBody physXBody1 = (PhysXBody)Body2;

				if( ( !physXBody0.Static || !physXBody1.Static ) &&
					( physXBody0.nativeBody != IntPtr.Zero && physXBody1.nativeBody != IntPtr.Zero ) )
				{
					Vec3 globalAnchor = ( Body1.Position + Body2.Position ) * .5f;

					//float diff = axis.LimitHigh - axis.LimitLow;
					//Vec3 low = globalAnchor + Axis.Direction * axis.LimitLow;
					//Vec3 high = globalAnchor + Axis.Direction * axis.LimitHigh;
					//if( axis.LimitsEnabled )
					//{
					//   globalAnchor = ( low + high ) / 2;
					//   //float d = axis.LimitHigh + axis.LimitLow;
					//   //если low положительный?
					//   //globalAnchor += ( d / 2 ) * Axis.Direction;
					//}

					Quat globalAxisRotation = Quat.FromDirectionZAxisUp( Axis.Direction );
					Vec3 localPosition0 = physXBody0.Rotation.GetInverse() * ( globalAnchor - physXBody0.Position );
					Quat localRotation0 = ( physXBody0.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();
					Vec3 localPosition1 = physXBody1.Rotation.GetInverse() * ( globalAnchor - physXBody1.Position );
					Quat localRotation1 = ( physXBody1.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();

					nativeJoint = PhysXNativeWrapper.PhysXNativeD6Joint.Create(
						physXBody0.nativeBody, ref localPosition0, ref localRotation0,
						physXBody1.nativeBody, ref localPosition1, ref localRotation1 );

					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.X, PhysXD6Motion.Free );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Y, PhysXD6Motion.Locked );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Z, PhysXD6Motion.Locked );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Twist, PhysXD6Motion.Locked );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing1, PhysXD6Motion.Locked );
					PhysXNativeD6Joint.SetMotion( nativeJoint, PhysXD6Axis.Swing2, PhysXD6Motion.Locked );

					UpdateLimits();

					//Vec3 globalAnchor = ( Body1.Position + Body2.Position ) * .5f;
					//Quat globalAxisRotation = Quat.FromDirectionZAxisUp( Axis.Direction );
					//Vec3 localPosition0 = physXBody0.Rotation.GetInverse() * ( globalAnchor - physXBody0.Position );
					//Quat localRotation0 = ( physXBody0.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();
					//Vec3 localPosition1 = physXBody1.Rotation.GetInverse() * ( globalAnchor - physXBody1.Position );
					//Quat localRotation1 = ( physXBody1.Rotation.GetInverse() * globalAxisRotation ).GetNormalize();

					//nativeJoint = PhysXNativeWrapper.PhysXNativeSliderJoint.Create(
					//   physXBody0.nativeBody, ref localPosition0, ref localRotation0,
					//   physXBody1.nativeBody, ref localPosition1, ref localRotation1 );

					//if( axis.LimitsEnabled )
					//{
					//   Log.Warning( "PhysXSliderJoint: Limits for Slider joint are not supported by the PhysX Physics System." );
					//   //PhysXNativeWrapper.PhysXNativeSliderJoint.SetLimit( nativeJoint, true, axis.LimitLow, axis.LimitHigh,
					//   //   PhysXGeneral.jointLimitContactDistance, axis.LimitsBounciness, axis.LimitsHardness );
					//}

					if( ContactsEnabled )
						PhysXNativeWrapper.PhysXJoint.SetCollisionEnable( nativeJoint, true );
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

		public void UpdateDataFromLibrary()
		{
			if( nativeJoint == IntPtr.Zero )
				return;

			Vec3 position;
			Quat rotation;
			PhysXJoint.GetGlobalPose( nativeJoint, out position, out rotation );
			Vec3 axis = Vec3.XAxis * rotation;
			UpdateDataFromLibrary( ref axis );
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
