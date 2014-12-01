// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;

namespace ProjectEntities
{
	//!!!!!!this class not implemented
	//!!!!! this is a fake

	/// <summary>
	/// Defines the <see cref="Aircraft"/> entity type.
	/// </summary>
	public class AircraftType : UnitType
	{
		[FieldSerialize]
		float velocity;

		public float Velocity
		{
			get { return velocity; }
			set { velocity = value; }
		}
	}

	public class Aircraft : Unit
	{
		AircraftType _type = null; public new AircraftType Type { get { return _type; } }

		[FieldSerialize]
		float flyHeight = 10;

		public float FlyHeight
		{
			get { return flyHeight; }
			set { flyHeight = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			//!!!!!!this class not implemented
			//!!!!! this is a fake

			float mass = 0;
			foreach( Body b in PhysicsModel.Bodies )
				mass += b.Mass;

			Body body = PhysicsModel.Bodies[ 0 ];

			body.AngularVelocity = Vec3.Zero;
			body.LinearVelocity = Rotation * new Vec3( Type.Velocity, 0, body.LinearVelocity.Z );

			//anti gravity
			body.AddForce( ForceType.GlobalAtLocalPos, TickDelta,
				-PhysicsWorld.Instance.MainScene.Gravity * mass, Vec3.Zero );

			float diff = Position.Z - flyHeight;

			float force = -diff - body.LinearVelocity.Z;
			MathFunctions.Clamp( ref force, -10, 10 );

			body.AddForce( ForceType.GlobalAtLocalPos, TickDelta,
				new Vec3( 0, 0, force ) * mass, Vec3.Zero );

			//check outside Map position
			Bounds checkBounds = Map.Instance.InitialCollisionBounds;
			checkBounds.Expand( new Vec3( 300, 300, 10000 ) );
			if( !checkBounds.IsContainsPoint( Position ) )
				SetForDeletion( false );
		}
	}
}
