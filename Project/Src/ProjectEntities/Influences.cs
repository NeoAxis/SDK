// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace ProjectEntities
{
	////////////////////////////////////////////////////////////////////////////////////////////////

	public class BigDamageInfluenceType : InfluenceType
	{
		[FieldSerialize]
		float coefficient;

		public float Coefficient
		{
			get { return coefficient; }
			set { coefficient = value; }
		}
	}

	public class BigDamageInfluence : Influence
	{
		BigDamageInfluenceType _type = null; public new BigDamageInfluenceType Type { get { return _type; } }
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class FastAttackInfluenceType : InfluenceType
	{
		[FieldSerialize]
		float coefficient;

		public float Coefficient
		{
			get { return coefficient; }
			set { coefficient = value; }
		}
	}

	public class FastAttackInfluence : Influence
	{
		FastAttackInfluenceType _type = null; public new FastAttackInfluenceType Type { get { return _type; } }
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class FastMoveInfluenceType : InfluenceType
	{
		[FieldSerialize]
		float coefficient;

		public float Coefficient
		{
			get { return coefficient; }
			set { coefficient = value; }
		}
	}

	public class FastMoveInfluence : Influence
	{
		FastMoveInfluenceType _type = null; public new FastMoveInfluenceType Type { get { return _type; } }
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class FireInfluenceType : InfluenceType
	{
		[FieldSerialize]
		float damagePerSecond;

		public float DamagePerSecond
		{
			get { return damagePerSecond; }
			set { damagePerSecond = value; }
		}
	}

	public class FireInfluence : Influence
	{
		FireInfluenceType _type = null; public new FireInfluenceType Type { get { return _type; } }

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

			Trace.Assert( Parent is Dynamic );
			Dynamic obj = (Dynamic)Parent;
			if( !obj.IsSetForDeletion )
				obj.DoDamage( obj, obj.Position, null, Type.DamagePerSecond * TickDelta, true );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class SmokeInfluenceType : InfluenceType
	{
	}

	public class SmokeInfluence : Influence
	{
		SmokeInfluenceType _type = null; public new SmokeInfluenceType Type { get { return _type; } }
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

}
