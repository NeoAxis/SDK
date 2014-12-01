// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="TurnFloatSwitch"/> entity type.
	/// </summary>
	public class TurnFloatSwitchType : FloatSwitchType
	{
		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float turnCoefficient = 1;

		[FieldSerialize]
		[DefaultValue( false )]
		bool clockwiseRotation;

		[DefaultValue( 1.0f )]
		public float TurnCoefficient
		{
			get { return turnCoefficient; }
			set { turnCoefficient = value; }
		}

		[DefaultValue( false )]
		public bool ClockwiseRotation
		{
			get { return clockwiseRotation; }
			set { clockwiseRotation = value; }
		}

	}

	/// <summary>
	/// Defines the parametrical switch which turns the <see cref="Switch.UseAttachedMesh"/>.
	/// </summary>
	public class TurnFloatSwitch : FloatSwitch
	{
		TurnFloatSwitchType _type = null; public new TurnFloatSwitchType Type { get { return _type; } }

		protected override void OnValueChange()
		{
			base.OnValueChange();

			if( UseAttachedMesh != null )
			{
				float angle = Value * Type.TurnCoefficient;
				angle = MathFunctions.RadiansNormalize360( angle );
				if( !Type.ClockwiseRotation )
					angle = -angle;

				UseAttachedMesh.RotationOffset = new Angles( 0, 0, MathFunctions.RadToDeg( angle ) ).ToQuat();
				RecalculateMapBounds();
			}
		}
	}
}
