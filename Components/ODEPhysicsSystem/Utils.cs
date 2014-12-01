// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;

namespace ODEPhysicsSystem
{
	using dJointID = System.IntPtr;

	static class Utils
	{
		unsafe internal static void CalculateJointStress( dJointID jointID,
			out float force, out float torque )
		{
			IntPtr jointFeedback = Ode.dJointGetFeedbackAsIntPtr( jointID );

			if( jointFeedback == IntPtr.Zero )
			{
				//create jointFeedback and begin use on next simulation step

				jointFeedback = Ode.dAlloc( (uint)Marshal.SizeOf( typeof( Ode.dJointFeedback ) ) );

				//zero memory
				unsafe
				{
					Ode.dJointFeedback* p = (Ode.dJointFeedback*)jointFeedback;
					p->f1 = new Ode.dVector3();
					p->t1 = new Ode.dVector3();
					p->f2 = new Ode.dVector3();
					p->t2 = new Ode.dVector3();
				}
				Ode.dJointSetFeedbackAsIntPtr( jointID, jointFeedback );

				force = 0;
				torque = 0;
				return;
			}

			Ode.dJointFeedback* ptr = (Ode.dJointFeedback*)jointFeedback;
			Vec3 f1 = Convert.ToNet( ptr->f1 );
			Vec3 t1 = Convert.ToNet( ptr->t1 );
			Vec3 f2 = Convert.ToNet( ptr->f2 );
			Vec3 t2 = Convert.ToNet( ptr->t2 );

			// This is a simplification, but it should still work.
			force = ( f1 - f2 ).Length();
			torque = ( t1 - t2 ).Length();
		}

		internal static void FreeJointFeedback( dJointID jointID )
		{
			IntPtr jointFeedback = Ode.dJointGetFeedbackAsIntPtr( jointID );
			if( jointFeedback != IntPtr.Zero )
			{
				Ode.dFree( jointFeedback, (uint)Marshal.SizeOf( typeof( Ode.dJointFeedback ) ) );
				jointFeedback = IntPtr.Zero;
			}
		}

		internal static bool UpdateJointBreakState( Joint joint, dJointID jointID )
		{
			if( jointID == dJointID.Zero )
				return false;

			float force;
			float torque;
			CalculateJointStress( jointID, out force, out torque );

			if( joint.BreakMaxForce != 0 && force >= joint.BreakMaxForce )
				return true;
			if( joint.BreakMaxTorque != 0 && torque >= joint.BreakMaxTorque )
				return true;
			return false;
		}

	}
}
