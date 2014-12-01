// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ODEPhysicsSystem
{
	using dJointID = System.IntPtr;

	//

	sealed class ODEUniversalJoint : UniversalJoint
	{
		///////////////////////////////////////////

		sealed class UniversalAxis : RotationAxis
		{
			public ODEUniversalJoint joint;
			int index;

			//

			public UniversalAxis( int index )
			{
				this.index = index;
			}

			public void UpdateToLibrary( bool updateDirection )
			{
				if( joint.jointID == IntPtr.Zero )
					return;

				if( updateDirection )
				{
					if( index == 1 )
						Ode.dJointSetUniversalAxis1( joint.jointID, Direction.X, Direction.Y, Direction.Z );
					else
						Ode.dJointSetUniversalAxis2( joint.jointID, Direction.X, Direction.Y, Direction.Z );
				}

				//limits

				int useIndex;
				if( !joint.Body1.Static )
					useIndex = index == 1 ? 1 : 2;
				else
					useIndex = index == 1 ? 2 : 1;

				Range range;
				if( LimitsEnabled )
				{
					if( !joint.Body1.Static )
						range = new Range( LimitLow.InRadians(), LimitHigh.InRadians() );
					else
						range = new Range( -LimitHigh.InRadians(), -LimitLow.InRadians() );
				}
				else
					range = new Range( -Ode.dInfinity, Ode.dInfinity );

				// Both limits must be set twice because of a ODE bug in
				// the limit setting function.

				Ode.dJointSetUniversalParam( joint.jointID, ( useIndex == 1 ) ?
					Ode.dJointParams.dParamLoStop : Ode.dJointParams.dParamLoStop2, range.Minimum );
				Ode.dJointSetUniversalParam( joint.jointID, ( useIndex == 1 ) ?
					Ode.dJointParams.dParamHiStop : Ode.dJointParams.dParamHiStop2, range.Maximum );
				Ode.dJointSetUniversalParam( joint.jointID, ( useIndex == 1 ) ?
					Ode.dJointParams.dParamLoStop : Ode.dJointParams.dParamLoStop2, range.Minimum );
				Ode.dJointSetUniversalParam( joint.jointID, ( useIndex == 1 ) ?
					Ode.dJointParams.dParamHiStop : Ode.dJointParams.dParamHiStop2, range.Maximum );

				float h = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetUniversalParam( joint.jointID, ( useIndex == 1 ) ?
					Ode.dJointParams.dParamStopERP : Ode.dJointParams.dParamStopERP2, h );

				float b = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetUniversalParam( joint.jointID, ( useIndex == 1 ) ?
					Ode.dJointParams.dParamBounce : Ode.dJointParams.dParamBounce2, b );
			}

			public override Radian GetAngle()
			{
				if( joint.jointID == IntPtr.Zero )
					return 0;

				float value;
				if( index == 1 )
					value = Ode.dJointGetUniversalAngle1( joint.jointID );
				else
					value = Ode.dJointGetUniversalAngle2( joint.jointID );
				if( joint.Body1.Static )
					value = -value;
				return value;
			}

			//public override Radian GetVelocity()
			//{
			//   if( joint.jointID == IntPtr.Zero )
			//      return 0;

			//   float value;
			//   if( index == 1 )
			//      value = Ode.dJointGetUniversalAngle1Rate( joint.jointID );
			//   else
			//      value = Ode.dJointGetUniversalAngle2Rate( joint.jointID );
			//   if( joint.Body1.Static )
			//      value = -value;
			//   return value;
			//}

			public override bool IsDesiredVelocitySupported()
			{
				return true;
			}

			public override void SetDesiredVelocity( Radian velocity, float maxTorque )
			{
				if( joint.jointID == dJointID.Zero )
					return;

				int useIndex;
				if( !joint.Body1.Static )
					useIndex = index == 1 ? 1 : 2;
				else
					useIndex = index == 1 ? 2 : 1;

				if( useIndex == 1 )
				{
					Ode.dJointSetUniversalParam( joint.jointID, Ode.dJointParams.dParamVel,
						joint.Body1.Static ? -velocity : velocity );
					Ode.dJointSetUniversalParam( joint.jointID, Ode.dJointParams.dParamFMax, maxTorque );
				}
				else
				{
					Ode.dJointSetUniversalParam( joint.jointID, Ode.dJointParams.dParamVel2,
						joint.Body1.Static ? -velocity : velocity );
					Ode.dJointSetUniversalParam( joint.jointID, Ode.dJointParams.dParamFMax2, maxTorque );
				}
			}

		}

		///////////////////////////////////////////

		dJointID jointID;
		UniversalAxis axis1;
		UniversalAxis axis2;

		Vec3 axis1LocalAxis;
		Vec3 axis2LocalAxis;

		//

		public ODEUniversalJoint( Body body1, Body body2 )
			: base( body1, body2, new UniversalAxis( 1 ), new UniversalAxis( 2 ) )
		{
			axis1 = (UniversalAxis)Axis1;
			axis1.joint = this;
			axis2 = (UniversalAxis)Axis2;
			axis2.joint = this;
		}

		void UpdateODEJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = jointID != dJointID.Zero;

			if( needCreate == created )
				return;

			if( needCreate )
			{
				if( Math.Abs( Vec3.Dot( axis1.Direction, axis2.Direction ) ) > .095f )
				{
					Log.Warning( "UniversalJoint: Invalid axes." );
					return;
				}
				if( Axis1.LimitsEnabled && Axis1.LimitLow > Axis1.LimitHigh )
				{
					Log.Warning( "UniversalJoint: Invalid axis1 limits (low > high)." );
					return;
				}
				if( Axis2.LimitsEnabled && Axis2.LimitLow > Axis2.LimitHigh )
				{
					Log.Warning( "UniversalJoint: Invalid axis2 limits (low > high)." );
					return;
				}

				ODEBody odeBody1 = (ODEBody)Body1;
				ODEBody odeBody2 = (ODEBody)Body2;

				jointID = Ode.dJointCreateUniversal( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );
				Ode.SetJointContactsEnabled( jointID, ContactsEnabled );

				Ode.dJointSetUniversalParam( jointID, Ode.dJointParams.dParamFudgeFactor,
					Defines.jointFudgeFactor );
				Ode.dJointSetUniversalParam( jointID, Ode.dJointParams.dParamFudgeFactor2,
					Defines.jointFudgeFactor );

				Ode.dJointAttach( jointID, odeBody1.bodyID, odeBody2.bodyID );

				Ode.dJointSetUniversalAnchor( jointID, Anchor.X, Anchor.Y, Anchor.Z );

				axis1.UpdateToLibrary( true );
				axis2.UpdateToLibrary( true );

				axis1LocalAxis = Body1.Rotation.GetInverse() * axis1.Direction;
				axis2LocalAxis = Body1.Rotation.GetInverse() * axis2.Direction;

				Ode.BodyDataAddJoint( odeBody1.bodyData, jointID );
				Ode.BodyDataAddJoint( odeBody2.bodyData, jointID );
			}
			else
			{
				DestroyODEJoint();
			}
		}

		internal void DestroyODEJoint()
		{
			if( jointID != dJointID.Zero )
			{
				ODEBody odeBody1 = (ODEBody)Body1;
				ODEBody odeBody2 = (ODEBody)Body2;

				Ode.BodyDataRemoveJoint( odeBody1.bodyData, jointID );
				Ode.BodyDataRemoveJoint( odeBody2.bodyData, jointID );

				Utils.FreeJointFeedback( jointID );
				Ode.dJointDestroy( jointID );
				jointID = dJointID.Zero;
			}
		}

		protected override void OnUpdatePushedToWorld()
		{
			UpdateODEJoint();
		}

		protected override void OnSetBroken()
		{
			UpdateODEJoint();
		}

		internal void UpdateDataFromLibrary()
		{
			if( jointID == dJointID.Zero )
				return;

			Ode.dVector3 odeAnchor = new Ode.dVector3();
			Ode.dJointGetUniversalAnchor( jointID, ref odeAnchor );
			Vec3 anchor = Convert.ToNet( odeAnchor );
			Vec3 axis1Direction = Body1.Rotation * axis1LocalAxis;
			Vec3 axis2Direction = Body1.Rotation * axis2LocalAxis;
			UpdateDataFromLibrary( ref anchor, ref axis1Direction, ref axis2Direction );
		}

		protected override bool OnUpdateBreakState()
		{
			return Utils.UpdateJointBreakState( this, jointID );
		}

		public override bool IsStability()
		{
			if( jointID == dJointID.Zero )
				return true;
			Ode.dVector3 a1 = new Ode.dVector3();
			Ode.dVector3 a2 = new Ode.dVector3();
			Ode.dJointGetUniversalAnchor( jointID, ref a1 );
			Ode.dJointGetUniversalAnchor2( jointID, ref a2 );
			const float limit = .01f;// .001f;
			return Math.Abs( a1.X - a2.X ) < limit &&
				Math.Abs( a1.Y - a2.Y ) < limit &&
				Math.Abs( a1.Z - a2.Z ) < limit;
		}

		protected override void OnUpdateAxisParameter( BaseAxis axis, UpdateAxisParameters parameter )
		{
			base.OnUpdateAxisParameter( axis, parameter );

			if( jointID != dJointID.Zero )
			{
				if( parameter == UpdateAxisParameters.LimitsEnabled || parameter == UpdateAxisParameters.LimitsRestitution ||
					parameter == UpdateAxisParameters.LimitsSpring || parameter == UpdateAxisParameters.LimitsDamping ||
					parameter == UpdateAxisParameters.LimitLow || parameter == UpdateAxisParameters.LimitHigh )
				{
					( (UniversalAxis)axis ).UpdateToLibrary( false );
				}
			}
		}

		protected override void OnUpdateContactsEnabled()
		{
			base.OnUpdateContactsEnabled();

			if( jointID != dJointID.Zero )
				Ode.SetJointContactsEnabled( jointID, ContactsEnabled );
		}
	}
}
