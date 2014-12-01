// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="HealthItem"/> entity type.
	/// </summary>
	public class HealthItemType : ItemType
	{
		[FieldSerialize]
		float health;

		[DefaultValue( 0.0f )]
		public float Health
		{
			get { return health; }
			set { health = value; }
		}
	}

	/// <summary>
	/// Defines a class for item with ability to heal the unit. When the player takes the item his <see cref="Dynamic.Health"/> increased.
	/// </summary>
	public class HealthItem : Item
	{
		HealthItemType _type = null; public new HealthItemType Type { get { return _type; } }

		protected override bool OnTake( Unit unit )
		{
			bool take = base.OnTake( unit );

			float healthMax = unit.Type.HealthMax;

			if( unit.Health < healthMax )
			{
				float health = unit.Health + Type.Health;
				if( health > healthMax )
					health = healthMax;

				unit.Health = health;

				take = true;
			}

			return take;
		}
	}
}
