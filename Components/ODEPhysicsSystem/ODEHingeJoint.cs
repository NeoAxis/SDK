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

	sealed class ODEHingeJoint : HingeJoint
	{
		///////////////////////////////////////////

		sealed class HingeAxis : RotationAxis
		{
			public ODEHingeJoint joint;

			//

			public void UpdateToLibrary( bool updateDirection )
			{
				if( joint.jointID == IntPtr.Zero )
					return;

				if( updateDirection )
					Ode.dJointSetHingeAxis( joint.jointID, Direction.X, Direction.Y, Direction.Z );

				//limits

				Range range;
				if( LimitsEnabled )
				{
					if( joint.Body1.Static )
						range = new Range( LimitLow.InRadians(), LimitHigh.InRadians() );
					else
						range = new Range( -LimitHigh.InRadians(), -LimitLow.InRadians() );
				}
				else
					range = new Range( -Ode.dInfinity, Ode.dInfinity );

				// Both limits must be set twice because of a ODE bug in
				// the limit setting function.
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamLoStop, range.Minimum );
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamHiStop, range.Maximum );
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamLoStop, range.Minimum );
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamHiStop, range.Maximum );

				float h = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamStopERP, h );

				float b = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamBounce, b );
			}

			public override Radian GetAngle()
			{
				if( joint.jointID == IntPtr.Zero )
					return 0;
				return -Ode.dJointGetHingeAngle( joint.jointID );
			}

			//public override Radian GetVelocity()
			//{
			//   if( joint.jointID == IntPtr.Zero )
			//      return 0;
			//   new
			//   return -Ode.dJointGetHingeAngleRate( joint.jointID );
			//   //return Ode.dJointGetHingeAngleRate( joint.jointID );
			//}

			public override bool IsDesiredVelocitySupported()
			{
				return true;
			}

			public override void SetDesiredVelocity( Radian velocity, float maxTorque )
			{
				if( joint.jointID == dJointID.Zero )
					return;

				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamVel,
					joint.Body1.Static ? velocity : -velocity );
				Ode.dJointSetHingeParam( joint.jointID, Ode.dJointParams.dParamFMax, maxTorque );
			}

		}

		///////////////////////////////////////////

		dJointID jointID;
		HingeAxis axis;

		//

		public ODEHingeJoint( Body body1, Body body2 )
			: base( body1, body2, new HingeAxis() )
		{
			axis = (HingeAxis)Axis;
			axis.joint = this;
		}

		void UpdateODEJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = jointID != dJointID.Zero;

			if( needCreate == created )
				return;

			if( needCreate )
			{
				if( Axis.LimitsEnabled && Axis.LimitLow > Axis.LimitHigh )
				{
					Log.Warning( "HingeJoint: Invalid axis limits (low > high)." );
					return;
				}

				ODEBody odeBody1 = (ODEBody)Body1;
				ODEBody odeBody2 = (ODEBody)Body2;

				jointID = Ode.dJointCreateHinge( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );
				Ode.SetJointContactsEnabled( jointID, ContactsEnabled );

				Ode.dJointSetHingeParam( jointID, Ode.dJointParams.dParamFudgeFactor,
					Defines.jointFudgeFactor );

				Ode.dJointAttach( jointID, odeBody1.bodyID, odeBody2.bodyID );

				Ode.dJointSetHingeAnchor( jointID, Anchor.X, Anchor.Y, Anchor.Z );

				axis.UpdateToLibrary( true );

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
			Ode.dVector3 odeAxisDirection = new Ode.dVector3();

			Ode.dJointGetHingeAnchor( jointID, ref odeAnchor );
			Ode.dJointGetHingeAxis( jointID, ref odeAxisDirection );

			Vec3 anchor = Convert.ToNet( odeAnchor );
			Vec3 axisDirection = Convert.ToNet( odeAxisDirection );

			UpdateDataFromLibrary( ref anchor, ref axisDirection );
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
			Ode.dJointGetHingeAnchor( jointID, ref a1 );
			Ode.dJointGetHingeAnchor2( jointID, ref a2 );
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
					( (HingeAxis)axis ).UpdateToLibrary( false );
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
