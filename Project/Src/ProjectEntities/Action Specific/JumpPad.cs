// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="JumpPad"/> entity type.
	/// </summary>
	public class JumpPadType : DynamicType
	{
	}

	/// <summary>
	/// Gives an opportunity of creation a jump pads.
	/// </summary>
	public class JumpPad : Dynamic
	{
		[FieldSerialize]
		float force = 1000;

		//

		JumpPadType _type = null; public new JumpPadType Type { get { return _type; } }

		public float Force
		{
			get { return force; }
			set { force = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();

			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
					body.Collision += new Body.CollisionDelegate( body_Collision );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
					body.Collision -= new Body.CollisionDelegate( body_Collision );
			}

			base.OnDestroy();
		}

		void DoForce( Body body )
		{
			float velocity = Force;
			//float velocity = Force / body.Mass;
			body.LinearVelocity = Rotation * new Vec3( velocity, 0, 0 );
		}

		void body_Collision( ref CollisionEvent collisionEvent )
		{
			Body body1 = collisionEvent.ThisShape.Body;
			if( !body1.Static )
				DoForce( body1 );

			Body body2 = collisionEvent.OtherShape.Body;
			if( !body2.Static )
				DoForce( body2 );
		}
	}
}
