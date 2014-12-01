// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.MathEx;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="InfluenceRegion"/> entity type.
	/// </summary>
	public class InfluenceRegionType : RegionType
	{
	}

	public class InfluenceRegion : Region
	{
		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float impulsePerSecond;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float damagePerSecond;

		[FieldSerialize]
		InfluenceType influenceType;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float influenceTimePerSecond;

		public enum DistanceFunctionType
		{
			One,
			NormalFadeAxisX,
		}

		[FieldSerialize]
		[DefaultValue( DistanceFunctionType.One )]
		DistanceFunctionType distanceFunction;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float force;

		InfluenceRegionType _type = null; public new InfluenceRegionType Type { get { return _type; } }

		[DefaultValue( 0.0f )]
		public float ImpulsePerSecond
		{
			get { return impulsePerSecond; }
			set { impulsePerSecond = value; }
		}

		[DefaultValue( 0.0f )]
		public float DamagePerSecond
		{
			get { return damagePerSecond; }
			set { damagePerSecond = value; }
		}

		public InfluenceType InfluenceType
		{
			get { return influenceType; }
			set { influenceType = value; }
		}

		[DefaultValue( 0.0f )]
		public float InfluenceTimePerSecond
		{
			get { return influenceTimePerSecond; }
			set { influenceTimePerSecond = value; }
		}

		[DefaultValue( DistanceFunctionType.One )]
		public DistanceFunctionType DistanceFunction
		{
			get { return distanceFunction; }
			set { distanceFunction = value; }
		}

		[DefaultValue( 0.0f )]
		public float Force
		{
			get { return force; }
			set
			{
				if( value < -1 || value > 1 )
					throw new InvalidOperationException( "Throttle need in [-1,1] interval" );

				force = value;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();
		}

		float GetDistanceCoefficient( Vec3 pos )
		{
			switch( distanceFunction )
			{
			case DistanceFunctionType.One:
				return 1;

			case DistanceFunctionType.NormalFadeAxisX:
				float distance = ( Rotation.GetInverse() * ( pos - Position ) ).X;
				return 1.0f - Math.Abs( distance ) / ( Scale.X * .5f );

			default:
				throw new Exception( "GetDistanceCoefficient no code" );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( force != 0 )
			{
				foreach( MapObject obj in ObjectsInRegion )
				{
					if( obj.IsSetForDeletion )
						continue;

					//impulse
					if( impulsePerSecond != 0 )
					{
						PhysicsModel objPhysicsModel = obj.PhysicsModel;
						if( objPhysicsModel != null )
						{
							foreach( Body body in objPhysicsModel.Bodies )
							{
								if( body.Static )
									continue;

								EngineRandom random = World.Instance.Random;

								float distanceCoef = GetDistanceCoefficient( body.Position );

								float randomCoef = 1.0f + random.NextFloatCenter() * .1f;

								float v = impulsePerSecond * force * distanceCoef * randomCoef * TickDelta;

								Vec3 point = new Vec3(
									random.NextFloat() * .1f,
									random.NextFloat() * .1f,
									random.NextFloat() * .1f );

								body.AddForce( ForceType.GlobalAtLocalPos, TickDelta,
									Rotation * new Vec3( v, 0, 0 ), point );
							}
						}
					}

					if( InfluenceType != null || DamagePerSecond != 0 )
					{
						Dynamic dynamic = obj as Dynamic;
						if( dynamic != null )
						{
							float distanceCoef = GetDistanceCoefficient( obj.Position );

							if( InfluenceType != null )
							{
								dynamic.AddInfluence( InfluenceType,
									InfluenceTimePerSecond * distanceCoef * TickDelta, true );
							}
							if( DamagePerSecond != 0 )
							{
								dynamic.DoDamage( null, obj.Position, null,
									DamagePerSecond * distanceCoef * TickDelta, false );
							}
						}
					}
				}
			}


		}

	}
}
