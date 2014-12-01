// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine.PhysicsSystem;

namespace ODEPhysicsSystem
{
	using dJointID = System.IntPtr;

	//

	sealed class ODEFixedJoint : FixedJoint
	{
		dJointID jointID;

		//

		public ODEFixedJoint( Body body1, Body body2 )
			: base( body1, body2 ) { }

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

				jointID = Ode.dJointCreateFixed( ( (ODEPhysicsScene)Scene ).worldID, IntPtr.Zero );
				Ode.SetJointContactsEnabled( jointID, false );//ContactsEnabled );

				Ode.dJointAttach( jointID, odeBody1.bodyID, odeBody2.bodyID );

				Ode.dJointSetFixed( jointID );

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

		protected override bool OnUpdateBreakState()
		{
			return Utils.UpdateJointBreakState( this, jointID );
		}
	}
}
