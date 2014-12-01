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

	sealed class ODEBallJoint : BallJoint
	{
		///////////////////////////////////////////

		sealed class BallAxis : RotationAxis
		{
			public ODEBallJoint joint;
			int index;

			//

			public BallAxis( int index )
			{
				this.index = index;
			}

			public void UpdateToLibrary( bool updateDirection )
			{
				if( joint.jointID == IntPtr.Zero )
					return;

				//Существует важное ограничение при использовании euler углов: 
				//угол theta 1 не должен выходить за пределы - pi /2 ... pi /2. 
				//Если это случится то AMotor станет не стабильным (эта особенность +/- pi /2). 
				//Таким образом, вы должны установить подходящие остановки(stops) на оси 1.

				if( updateDirection )
				{
					if( index == 1 )
					{
						Ode.dJointSetAMotorAxis( joint.aMotorID, 0, joint.Body1.Static ? 0 : 1,
							Direction.X, Direction.Y, Direction.Z );
					}
					if( index == 3 )
					{
						Ode.dJointSetAMotorAxis( joint.aMotorID, 2, joint.Body2.Static ? 0 : 2,
							Direction.X, Direction.Y, Direction.Z );
					}
				}

				//limits

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

				Ode.dJointParams p = (Ode.dJointParams)0;

				// Both limits must be set twice because of a ODE bug in
				// the limit setting function.
				for( int z = 0; z < 2; z++ )
				{
					if( !joint.Body1.Static )
					{
						switch( index )
						{
						case 1: p = Ode.dJointParams.dParamLoStop; break;
						case 2: p = Ode.dJointParams.dParamLoStop2; break;
						case 3: p = Ode.dJointParams.dParamLoStop3; break;
						}
					}
					else
					{
						switch( index )
						{
						case 1: p = Ode.dJointParams.dParamLoStop; break;
						case 2: p = Ode.dJointParams.dParamLoStop2; break;
						case 3: p = Ode.dJointParams.dParamLoStop3; break;
						}
					}
					Ode.dJointSetAMotorParam( joint.aMotorID, p, range.Minimum );

					if( !joint.Body1.Static )
					{
						switch( index )
						{
						case 1: p = Ode.dJointParams.dParamHiStop; break;
						case 2: p = Ode.dJointParams.dParamHiStop2; break;
						case 3: p = Ode.dJointParams.dParamHiStop3; break;
						}
					}
					else
					{
						switch( index )
						{
						case 1: p = Ode.dJointParams.dParamHiStop; break;
						case 2: p = Ode.dJointParams.dParamHiStop2; break;
						case 3: p = Ode.dJointParams.dParamHiStop3; break;
						}
					}
					Ode.dJointSetAMotorParam( joint.aMotorID, p, range.Maximum );
				}

				if( !joint.Body1.Static )
				{
					switch( index )
					{
					case 1: p = Ode.dJointParams.dParamStopERP; break;
					case 2: p = Ode.dJointParams.dParamStopERP2; break;
					case 3: p = Ode.dJointParams.dParamStopERP3; break;
					}
				}
				else
				{
					switch( index )
					{
					case 1: p = Ode.dJointParams.dParamStopERP; break;
					case 2: p = Ode.dJointParams.dParamStopERP2; break;
					case 3: p = Ode.dJointParams.dParamStopERP3; break;
					}
				}
				float h = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetAMotorParam( joint.aMotorID, p, h );

				if( !joint.Body1.Static )
				{
					switch( index )
					{
					case 1: p = Ode.dJointParams.dParamBounce; break;
					case 2: p = Ode.dJointParams.dParamBounce2; break;
					case 3: p = Ode.dJointParams.dParamBounce3; break;
					}
				}
				else
				{
					switch( index )
					{
					case 1: p = Ode.dJointParams.dParamBounce; break;
					case 2: p = Ode.dJointParams.dParamBounce2; break;
					case 3: p = Ode.dJointParams.dParamBounce3; break;
					}
				}
				float b = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetAMotorParam( joint.aMotorID, p, b );
			}

			public override Radian GetAngle()
			{
				if( joint.jointID == IntPtr.Zero )
					return 0;

				Radian value = Ode.dJointGetAMotorAngle( joint.aMotorID, index - 1 );
				if( joint.Body1.Static )
					value = -value;
				return value;
			}

			//public override Radian GetVelocity()
			//{
			//   if( joint.jointID == IntPtr.Zero )
			//      return 0;

			//   Log.Warning( "ODEBallJoint: Axis: The method \"GetVelocity()\" are not supported." );
			//   return float.MinValue;

			//   //Radian value = Ode.dJointGetAMotorAngleRate( joint.aMotorID, index - 1 );
			//   //if( joint.Body1.Static )
			//   //   value = -value;
			//   //return value;
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
					Ode.dJointSetAMotorParam( joint.aMotorID, Ode.dJointParams.dParamVel,
						joint.Body1.Static ? -velocity : velocity );
					Ode.dJointSetAMotorParam( joint.aMotorID, Ode.dJointParams.dParamFMax, maxTorque );
				}
				else if( index == 2 )
				{
					Ode.dJointSetAMotorParam( joint.aMotorID, Ode.dJointParams.dParamVel2,
						joint.Body1.Static ? -velocity : velocity );
					Ode.dJointSetAMotorParam( joint.aMotorID, Ode.dJointParams.dParamFMax2, maxTorque );
				}
				else
				{
					Ode.dJointSetAMotorParam( joint.aMotorID, Ode.dJointParams.dParamVel3,
						joint.Body1.Static ? -velocity : velocity );
					Ode.dJointSetAMotorParam( joint.aMotorID, Ode.dJointParams.dParamFMax3, maxTorque );
				}
			}

		}

		///////////////////////////////////////////

		dJointID jointID;
		dJointID aMotorID;
		BallAxis axis1;
		BallAxis axis2;
		BallAxis axis3;

		Vec3 axis1LocalAxis;
		Vec3 axis2LocalAxis;

		//

		public ODEBallJoint( Body body1, Body body2 )
			: base( body1, body2, new BallAxis( 1 ), new BallAxis( 2 ), new BallAxis( 3 ) )
		{
			axis1 = (BallAxis)Axis1;
			axis1.joint = this;
			axis2 = (BallAxis)Axis2;
			axis2.joint = this;
			axis3 = (BallAxis)Axis3;
			axis3.joint = this;
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
					Log.Warning( "BallJoint: Invalid axes." );
					return;
				}
				if( Axis1.LimitsEnabled && Axis1.LimitLow > Axis1.LimitHigh )
				{
					Log.Warning( "BallJoint: Invalid axis1 limits (low > high)." );
					return;
				}
				if( Axis2.LimitsEnabled && Axis2.LimitLow > Axis2.LimitHigh )
				{
					Log.Warning( "BallJoint: Invalid axis2 limits (low > high)." );
					return;
				}
				if( Axis3.LimitsEnabled && Axis3.LimitLow > Axis3.LimitHigh )
				{
					Log.Warning( "BallJoint: Invalid axis3 limits (low > high)." );
					return;
				}

				ODEBody odeBody1 = (ODEBody)Body1;
				ODEBody odeBody2 = (ODEBody)Body2;

				jointID = Ode.dJointCreateBall( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );
				Ode.SetJointContactsEnabled( jointID, ContactsEnabled );

				aMotorID = Ode.dJointCreateAMotor( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );

				Ode.dJointSetAMotorParam( aMotorID, Ode.dJointParams.dParamFudgeFactor,
					Defines.jointFudgeFactor );
				Ode.dJointSetAMotorParam( aMotorID, Ode.dJointParams.dParamFudgeFactor2,
					Defines.jointFudgeFactor );
				Ode.dJointSetAMotorParam( aMotorID, Ode.dJointParams.dParamFudgeFactor3,
					Defines.jointFudgeFactor );

				Ode.dJointAttach( jointID, odeBody1.bodyID, odeBody2.bodyID );
				Ode.dJointAttach( aMotorID, odeBody1.bodyID, odeBody2.bodyID );

				Ode.dJointSetAMotorMode( aMotorID, (int)Ode.dAMotorMode.dAMotorEuler );

				Ode.dJointSetBallAnchor( jointID, Anchor.X, Anchor.Y, Anchor.Z );

				axis1.UpdateToLibrary( true );
				axis2.UpdateToLibrary( true );
				axis3.UpdateToLibrary( true );

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
			if( aMotorID != dJointID.Zero )
			{
				Ode.dJointDestroy( aMotorID );
				aMotorID = dJointID.Zero;
			}

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
			Ode.dJointGetBallAnchor( jointID, ref odeAnchor );
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
			Ode.dJointGetBallAnchor( jointID, ref a1 );
			Ode.dJointGetBallAnchor2( jointID, ref a2 );
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
					( (BallAxis)axis ).UpdateToLibrary( false );
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
