// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="RTSMine"/> entity type.
	/// </summary>
	public class RTSMineType : RTSBuildingType
	{
		[FieldSerialize]
		float moneyPerSecond = 1.0f;

		[DefaultValue( 1.0f )]
		public float MoneyPerSecond
		{
			get { return moneyPerSecond; }
			set { moneyPerSecond = value; }
		}
	}

	public class RTSMine : RTSBuilding
	{
		RTSMineType _type = null; public new RTSMineType Type { get { return _type; } }

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

			//Add money to faction
			if( BuildedProgress == 1 )
			{
				if( RTSFactionManager.Instance != null )
				{
					if( Intellect != null && Intellect.Faction != null )
					{
						RTSFactionManager.FactionItem factionItem = RTSFactionManager.Instance.
							GetFactionItemByType( Intellect.Faction );

						if( factionItem != null )
							factionItem.Money += Type.MoneyPerSecond * TickDelta;
					}
				}
			}

			//Rotation propeller
			if( BuildedProgress == 1 )
			{
				float angle = -Entities.Instance.TickTime * 500;
				AttachedObjects[ 3 ].RotationOffset = new Angles( 0, 0, angle ).ToQuat();
			}
		}
	}
}
