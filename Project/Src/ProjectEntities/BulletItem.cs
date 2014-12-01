// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="BulletItem"/> entity type.
	/// </summary>
	public class BulletItemType : ItemType
	{
		[FieldSerialize]
		BulletType bulletType;
		[FieldSerialize]
		int bulletCount;

		[FieldSerialize]
		BulletType bulletType2;
		[FieldSerialize]
		int bulletCount2;

		/// <summary>
		/// Gets or sets the bullets type.
		/// </summary>
		[Description( "The bullets type." )]
		public BulletType BulletType
		{
			get { return bulletType; }
			set { bulletType = value; }
		}

		/// <summary>
		/// Gets or sets the bullets count.
		/// </summary>
		[Description( "The bullets count." )]
		[DefaultValue( 0 )]
		public int BulletCount
		{
			get { return bulletCount; }
			set { bulletCount = value; }
		}

		/// <summary>
		/// Gets or sets the bullets type.
		/// </summary>
		[Description( "The bullets type." )]
		public BulletType BulletType2
		{
			get { return bulletType2; }
			set { bulletType2 = value; }
		}

		/// <summary>
		/// Gets or sets the bullets count.
		/// </summary>
		[Description( "The bullets count." )]
		[DefaultValue( 0 )]
		public int BulletCount2
		{
			get { return bulletCount2; }
			set { bulletCount2 = value; }
		}

	}

	/// <summary>
	/// Represents a item of the weapon bullets. When the player take this item it 
	/// takes a specified bullets.
	/// </summary>
	public class BulletItem : Item
	{
		BulletItemType _type = null; public new BulletItemType Type { get { return _type; } }

		protected override bool OnTake( Unit unit )
		{
			bool take = base.OnTake( unit );

			PlayerCharacter character = unit as PlayerCharacter;
			if( character != null )
			{
				bool taked = false;
				bool taked2 = false;
								
				if( Type.BulletType != null )
					taked = character.TakeBullets( Type.BulletType, Type.BulletCount );
				if( Type.BulletType2 != null )
					taked2 = character.TakeBullets( Type.BulletType2, Type.BulletCount2 );

				if( taked || taked2 )
					take = true;
			}

			return take;
		}
	}
}
