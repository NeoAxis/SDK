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
	using dBodyID = System.IntPtr;

	//

	sealed class ODEHinge2Joint : Hinge2Joint
	{
		bool suspensionInitialized;
		float suspensionERP;
		float suspensionCFM;

		///////////////////////////////////////////

		sealed class Hinge2Axis : RotationAxis
		{
			public ODEHinge2Joint joint;
			int index;

			//

			public Hinge2Axis( int index )
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
						Ode.dJointSetHinge2Axis1( joint.jointID, Direction.X, Direction.Y, Direction.Z );
					else
						Ode.dJointSetHinge2Axis2( joint.jointID, Direction.X, Direction.Y, Direction.Z );
				}

				//limits

				Range range;
				if( LimitsEnabled )
					range = new Range( LimitLow.InRadians(), LimitHigh.InRadians() );
				else
					range = new Range( -Ode.dInfinity, Ode.dInfinity );

				// Both limits must be set twice because of a ODE bug in
				// the limit setting function.

				Ode.dJointSetHinge2Param( joint.jointID, ( index == 1 ) ?
					Ode.dJointParams.dParamLoStop : Ode.dJointParams.dParamLoStop2, range.Minimum );
				Ode.dJointSetHinge2Param( joint.jointID, ( index == 1 ) ?
					Ode.dJointParams.dParamHiStop : Ode.dJointParams.dParamHiStop2, range.Maximum );
				Ode.dJointSetHinge2Param( joint.jointID, ( index == 1 ) ?
					Ode.dJointParams.dParamLoStop : Ode.dJointParams.dParamLoStop2, range.Minimum );
				Ode.dJointSetHinge2Param( joint.jointID, ( index == 1 ) ?
					Ode.dJointParams.dParamHiStop : Ode.dJointParams.dParamHiStop2, range.Maximum );

				float h = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetHinge2Param( joint.jointID, ( index == 1 ) ?
					Ode.dJointParams.dParamStopERP : Ode.dJointParams.dParamStopERP2, h );

				// ODE's Hinge2 Joint also has a suspension parameter. Use axis 2
				// for this since axis 2 doesn't use limits anyway.
				if( index == 2 )
				{
					Ode.dJointSetHinge2Param( joint.jointID, Ode.dJointParams.dParamSuspensionERP,
						LimitsRestitution );
				}

				float b = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetHinge2Param( joint.jointID, ( index == 1 ) ?
					Ode.dJointParams.dParamBounce : Ode.dJointParams.dParamBounce2, b );
			}

			public override Radian GetAngle()
			{
				if( joint.jointID == IntPtr.Zero )
					return 0;
				if( index == 1 )
				{
					return Ode.dJointGetHinge2Angle1( joint.jointID );
				}
				else
				{
					Log.Warning( "ODEHinge2Joint: The method \"GetAngle()\" is not supported for second axis." );
					return float.MinValue;
				}
			}

			//public override Radian GetVelocity()
			//{
			//   if( joint.jointID == IntPtr.Zero )
			//      return 0;
			//   if( index == 1 )
			//      return Ode.dJointGetHinge2Angle1Rate( joint.jointID );
			//   else
			//      return Ode.dJointGetHinge2Angle2Rate( joint.jointID );
			//}

			public override bool IsDesiredVelocitySupported()
			{
				return true;
			}

			public override void SetDesiredVelocity( Radian velocity, float maxTorque )
			{
				if( joint.jointID == dJointID.Zero )
					return;
				if( index == 1 )
				{
					Ode.dJointSetHinge2Param( joint.jointID, Ode.dJointParams.dParamVel, velocity );
					Ode.dJointSetHinge2Param( joint.jointID, Ode.dJointParams.dParamFMax, maxTorque );
				}
				else
				{
					Ode.dJointSetHinge2Param( joint.jointID, Ode.dJointParams.dParamVel2, velocity );
					Ode.dJointSetHinge2Param( joint.jointID, Ode.dJointParams.dParamFMax2, maxTorque );
				}
			}

		}

		///////////////////////////////////////////

		dJointID jointID;
		Hinge2Axis axis1;
		Hinge2Axis axis2;

		//

		public ODEHinge2Joint( Body body1, Body body2 )
			: base( body1, body2, new Hinge2Axis( 1 ), new Hinge2Axis( 2 ) )
		{
			axis1 = (Hinge2Axis)Axis1;
			axis1.joint = this;
			axis2 = (Hinge2Axis)Axis2;
			axis2.joint = this;
		}

		void UpdateODEJoint()
		{
			bool needCreate = PushedToWorld && !Broken;
			bool created = jointID != dJointID.Zero;

			ODEBody odeBody1 = (ODEBody)Body1;
			ODEBody odeBody2 = (ODEBody)Body2;

			if( needCreate && ( odeBody1.bodyID == dBodyID.Zero || odeBody2.bodyID == dBodyID.Zero ) )
			{
				Log.Warning( "ODEHinge2Joint: It is necessary that both bodies were not static." );
				needCreate = false;
			}

			if( needCreate == created )
				return;

			if( needCreate )
			{
				if( Math.Abs( Vec3.Dot( axis1.Direction, axis2.Direction ) ) > .095f )
				{
					Log.Warning( "Hinge2Joint: Invalid axes." );
					return;
				}
				if( Axis1.LimitsEnabled && Axis1.LimitLow > Axis1.LimitHigh )
				{
					Log.Warning( "Hinge2Joint: Invalid axis1 limits (low > high)." );
					return;
				}
				if( Axis2.LimitsEnabled && Axis2.LimitLow > Axis2.LimitHigh )
				{
					Log.Warning( "Hinge2Joint: Invalid axis2 limits (low > high)." );
					return;
				}

				jointID = Ode.dJointCreateHinge2( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );
				Ode.SetJointContactsEnabled( jointID, ContactsEnabled );

				Ode.dJointSetHinge2Param( jointID, Ode.dJointParams.dParamFudgeFactor,
					Defines.jointFudgeFactor );
				Ode.dJointSetHinge2Param( jointID, Ode.dJointParams.dParamFudgeFactor2,
					Defines.jointFudgeFactor );

				Ode.dJointAttach( jointID, odeBody1.bodyID, odeBody2.bodyID );

				Ode.dJointSetHinge2Anchor( jointID, Anchor.X, Anchor.Y, Anchor.Z );

				axis1.UpdateToLibrary( true );
				axis2.UpdateToLibrary( true );

				UpdateSuspension();

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
			Ode.dVector3 odeAxis1Direction = new Ode.dVector3();
			Ode.dVector3 odeAxis2Direction = new Ode.dVector3();

			Ode.dJointGetHinge2Anchor( jointID, ref odeAnchor );
			Ode.dJointGetHinge2Axis1( jointID, ref odeAxis1Direction );
			Ode.dJointGetHinge2Axis2( jointID, ref odeAxis2Direction );

			Vec3 anchor = Convert.ToNet( odeAnchor );
			Vec3 axis1Direction = Convert.ToNet( odeAxis1Direction );
			Vec3 axis2Direction = Convert.ToNet( odeAxis2Direction );

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
			Ode.dJointGetHinge2Anchor( jointID, ref a1 );
			Ode.dJointGetHinge2Anchor2( jointID, ref a2 );
			const float limit = .01f;// .001f;
			return Math.Abs( a1.X - a2.X ) < limit &&
				Math.Abs( a1.Y - a2.Y ) < limit &&
				Math.Abs( a1.Z - a2.Z ) < limit;
		}

		public override void SetODESuspension( float erp, float cfm )
		{
			suspensionInitialized = true;
			suspensionERP = erp;
			suspensionCFM = cfm;

			if( jointID != dJointID.Zero )
				UpdateSuspension();
		}

		void UpdateSuspension()
		{
			if( suspensionInitialized )
			{
				Ode.dJointSetHinge2Param( jointID, Ode.dJointParams.dParamSuspensionCFM,
					suspensionCFM );
				Ode.dJointSetHinge2Param( jointID, Ode.dJointParams.dParamSuspensionERP,
					suspensionERP );
			}
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
					( (Hinge2Axis)axis ).UpdateToLibrary( false );
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
