// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ODEPhysicsSystem
{
	using dJointID = System.IntPtr;

	//

	sealed class ODESliderJoint : SliderJoint
	{
		///////////////////////////////////////////

		sealed class SliderAxis : TranslationAxis
		{
			public ODESliderJoint joint;

			//

			public void UpdateToLibrary( bool updateDirection )
			{
				if( joint.jointID == IntPtr.Zero )
					return;

				if( updateDirection )
					Ode.dJointSetSliderAxis( joint.jointID, Direction.X, Direction.Y, Direction.Z );

				//limits

				Range range;
				if( LimitsEnabled )
				{
					if( !joint.Body1.Static )
						range = new Range( LimitLow, LimitHigh );
					else
						range = new Range( -LimitHigh, -LimitLow );
				}
				else
					range = new Range( -Ode.dInfinity, Ode.dInfinity );

				// Both limits must be set twice because of a ODE bug in
				// the limit setting function.
				Ode.dJointSetSliderParam( joint.jointID, Ode.dJointParams.dParamLoStop, range.Minimum );
				Ode.dJointSetSliderParam( joint.jointID, Ode.dJointParams.dParamHiStop, range.Maximum );
				Ode.dJointSetSliderParam( joint.jointID, Ode.dJointParams.dParamLoStop, range.Minimum );
				Ode.dJointSetSliderParam( joint.jointID, Ode.dJointParams.dParamHiStop, range.Maximum );

				float h = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetSliderParam( joint.jointID, Ode.dJointParams.dParamStopERP, h );

				float b = LimitsRestitution * ( Defines.maxERP - Defines.minERP ) + Defines.minERP;
				Ode.dJointSetSliderParam( joint.jointID, Ode.dJointParams.dParamBounce, b );
			}

			//public override float Distance
			//{
			//   get { return Ode.dJointGetSliderPosition( joint.jointID ); }
			//}

			//public override float Velocity
			//{
			//   get { return Ode.dJointGetSliderPositionRate( joint.jointID ); }
			//}
		}

		///////////////////////////////////////////

		dJointID jointID;
		SliderAxis axis;

		//

		public ODESliderJoint( Body body1, Body body2 )
			: base( body1, body2, new SliderAxis() )
		{
			axis = (SliderAxis)Axis;
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
				ODEBody odeBody1 = (ODEBody)Body1;
				ODEBody odeBody2 = (ODEBody)Body2;

				jointID = Ode.dJointCreateSlider( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );
				Ode.SetJointContactsEnabled( jointID, ContactsEnabled );

				Ode.dJointSetSliderParam( jointID, Ode.dJointParams.dParamFudgeFactor,
					Defines.jointFudgeFactor );

				Ode.dJointAttach( jointID, odeBody1.bodyID, odeBody2.bodyID );

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

			Ode.dVector3 odeAxisDirection = new Ode.dVector3();
			Ode.dJointGetSliderAxis( jointID, ref odeAxisDirection );

			Vec3 axisDirection = Convert.ToNet( odeAxisDirection );

			UpdateDataFromLibrary( ref axisDirection );
		}

		protected override bool OnUpdateBreakState()
		{
			return Utils.UpdateJointBreakState( this, jointID );
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
					( (SliderAxis)axis ).UpdateToLibrary( false );
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
