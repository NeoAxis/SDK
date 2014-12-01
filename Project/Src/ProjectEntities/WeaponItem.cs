// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="WeaponItem"/> entity type.
	/// </summary>
	public class WeaponItemType : BulletItemType
	{
		[FieldSerialize]
		WeaponType weaponType;

		/// <summary>
		/// Gets or sets the item weapon type.
		/// </summary>
		[Description( "The item weapon type." )]
		public WeaponType WeaponType
		{
			get { return weaponType; }
			set { weaponType = value; }
		}
	}

	/// <summary>
	/// Represents a item of the weapon. When the player take this item it 
	/// takes a specified weapon.
	/// </summary>
	public class WeaponItem : BulletItem
	{
		WeaponItemType _type = null; public new WeaponItemType Type { get { return _type; } }

		protected override bool OnTake( Unit unit )
		{
			bool take = base.OnTake( unit );

			if( Type.WeaponType != null )
			{
				PlayerCharacter character = unit as PlayerCharacter;
				if( character != null && character.TakeWeapon( Type.WeaponType ) )
					take = true;
			}
			else
				Log.Warning( "WeaponItem.OnTake: Type.WeaponType == null" );

			return take;
		}
	}
}
