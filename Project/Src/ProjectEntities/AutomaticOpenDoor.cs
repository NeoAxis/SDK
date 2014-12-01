// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="AutomaticOpenDoor"/> entity type.
	/// </summary>
	public class AutomaticOpenDoorType : DoorType
	{
		const float automaticOpenDistanceDefault = 3.0f;
		[FieldSerialize]
		[DefaultValue( automaticOpenDistanceDefault )]
		float automaticOpenDistance = automaticOpenDistanceDefault;

		const float automaticOpenCloseLatencyDefault = 1;
		[FieldSerialize]
		[DefaultValue( automaticOpenCloseLatencyDefault )]
		float automaticOpenCloseLatency = automaticOpenCloseLatencyDefault;

		//

		[DefaultValue( automaticOpenDistanceDefault )]
		public float AutomaticOpenDistance
		{
			get { return automaticOpenDistance; }
			set { automaticOpenDistance = value; }
		}

		[DefaultValue( automaticOpenCloseLatencyDefault )]
		public float AutomaticOpenCloseLatency
		{
			get { return automaticOpenCloseLatency; }
			set { automaticOpenCloseLatency = value; }
		}
	}

	/// <summary>
	/// Defines automatically opening doors.
	/// </summary>
	public class AutomaticOpenDoor : Door
	{
		[FieldSerialize]
		bool noAutomaticOpen;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float needCloseRemainingTime;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float nextCheckRemainingTime;

		//

		AutomaticOpenDoorType _type = null; public new AutomaticOpenDoorType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			nextCheckRemainingTime = World.Instance.Random.NextFloat();
			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			nextCheckRemainingTime -= TickDelta;
			if( nextCheckRemainingTime <= 0 )
			{
				nextCheckRemainingTime = .2f;
				UpdateState();
			}

			if( needCloseRemainingTime != 0 )
			{
				needCloseRemainingTime -= TickDelta;
				if( needCloseRemainingTime <= 0 )
				{
					needCloseRemainingTime = 0;
					Opened = false;
				}
			}
		}

		void UpdateState()
		{
			bool free = true;

			Sphere sphere = new Sphere( Position, Type.AutomaticOpenDistance );
			Map.Instance.GetObjects( sphere, delegate( MapObject mapObject )
			{
				//ignore this door
				if( mapObject == this )
					return;

				//ignore object without physics
				if( mapObject.PhysicsModel == null )
					return;

				//ignore non Dynamic objects
				if( ( mapObject as Dynamic ) == null )
					return;

				//ignore ingame gui
				if( ( mapObject as GameGuiObject ) != null )
					return;

				//ignore small objects
				Bounds mapBounds = mapObject.MapBounds;
				if( mapBounds.GetSize().X < .8f && mapBounds.GetSize().Y < .8f &&
					mapBounds.GetSize().Y < .8f )
				{
					return;
				}

				free = false;
			} );

			if( free )
			{
				if( needCloseRemainingTime == 0 )
					needCloseRemainingTime = Type.AutomaticOpenCloseLatency;
			}
			else
			{
				if( !noAutomaticOpen )
				{
					Opened = true;
					if( needCloseRemainingTime != 0 )
						needCloseRemainingTime = 0;
				}
			}
		}

		[DefaultValue( false )]
		public bool NoAutomaticOpen
		{
			get { return noAutomaticOpen; }
			set { noAutomaticOpen = value; }
		}

		public override bool Opened
		{
			get { return base.Opened; }
			set
			{
				needCloseRemainingTime = 0;
				base.Opened = value;
			}
		}

	}
}
