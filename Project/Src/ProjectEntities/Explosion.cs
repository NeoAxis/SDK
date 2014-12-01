// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Explosion"/> entity type.
	/// </summary>
	public class ExplosionType : MapObjectType
	{
		[FieldSerialize]
		float latency;
		[FieldSerialize]
		float radius;
		[FieldSerialize]
		float damage;
		[FieldSerialize]
		float impulse;

		[FieldSerialize]
		bool ignoreReasonObject;

		[FieldSerialize]
		InfluenceType influenceType;
		[FieldSerialize]
		float influenceMaxTime;

		//

		[DefaultValue( 0.0f )]
		public float Latency
		{
			get { return latency; }
			set { latency = value; }
		}

		[DefaultValue( 0.0f )]
		public float Radius
		{
			get { return radius; }
			set { radius = value; }
		}

		[DefaultValue( 0.0f )]
		public float Damage
		{
			get { return damage; }
			set { damage = value; }
		}

		[DefaultValue( 0.0f )]
		public float Impulse
		{
			get { return impulse; }
			set { impulse = value; }
		}

		[DefaultValue( false )]
		public bool IgnoreReasonObject
		{
			get { return ignoreReasonObject; }
			set { ignoreReasonObject = value; }
		}

		public InfluenceType InfluenceType
		{
			get { return influenceType; }
			set { influenceType = value; }
		}

		[DefaultValue( 0.0f )]
		public float InfluenceMaxTime
		{
			get { return influenceMaxTime; }
			set { influenceMaxTime = value; }
		}

		public ExplosionType()
		{
			AllowEmptyName = true;
		}
	}

	/// <summary>
	/// An invisible blast wave which damages and increases the impulse of 
	/// physical models within a radius.
	/// </summary>
	public class Explosion : MapObject
	{
		[FieldSerialize]
		float lifeTime;
		[FieldSerialize]
		Dynamic reasonObject;

		[FieldSerialize]
		float damageCoefficient = 1.0f;

		[FieldSerialize]
		Unit sourceUnit;

		//only for optimization
		static ListAllocator<MapObject> mapObjectListAllocator = new ListAllocator<MapObject>();
		static ListAllocator<DamageItem> damageItemListAllocator = new ListAllocator<DamageItem>();

		///////////////////////////////////////////

		struct DamageItem
		{
			public Dynamic dynamic;
			public Vec3 position;
			public float damage;

			public DamageItem( Dynamic dynamic, Vec3 position, float damage )
			{
				this.dynamic = dynamic;
				this.position = position;
				this.damage = damage;
			}
		}

		///////////////////////////////////////////

		ExplosionType _type = null; public new ExplosionType Type { get { return _type; } }

		public Dynamic ReasonObject
		{
			get { return reasonObject; }
			set
			{
				if( reasonObject != null )
					UnsubscribeToDeletionEvent( reasonObject );
				reasonObject = value;
				if( reasonObject != null )
					SubscribeToDeletionEvent( reasonObject );
			}
		}

		[Browsable( false )]
		public Unit SourceUnit
		{
			get { return sourceUnit; }
			set
			{
				if( sourceUnit != null )
					UnsubscribeToDeletionEvent( sourceUnit );
				sourceUnit = value;
				if( sourceUnit != null )
					SubscribeToDeletionEvent( sourceUnit );
			}
		}

		public float DamageCoefficient
		{
			get { return damageCoefficient; }
			set { damageCoefficient = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( sourceUnit == entity )
				sourceUnit = null;
			if( reasonObject == entity )
				reasonObject = null;
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

			lifeTime += TickDelta;
			if( lifeTime >= Type.Latency )
			{
				DoEffect();
				SetForDeletion( true );
			}
		}

		void DoEffect()
		{
			float radius = Type.Radius;
			float radiusInv = 1.0f / radius;

			List<MapObject> objects = mapObjectListAllocator.Alloc();
			List<DamageItem> damageItems = damageItemListAllocator.Alloc();

			//generate objects list
			Map.Instance.GetObjects( new Sphere( Position, radius ), delegate( MapObject obj )
				{
					if( Type.IgnoreReasonObject && obj == ReasonObject )
						return;
					objects.Add( obj );
				} );

			//enumerate objects
			foreach( MapObject obj in objects )
			{
				if( obj.IsSetForDeletion )
					continue;
				if( !obj.Visible )
					continue;

				PhysicsModel objPhysicsModel = obj.PhysicsModel;

				float totalObjImpulse = 0;
				float totalObjDamage = 0;

				if( Type.Impulse != 0 && objPhysicsModel != null )
				{
					float bodyCountInv = 1.0f / objPhysicsModel.Bodies.Length;

					foreach( Body body in objPhysicsModel.Bodies )
					{
						if( body.Static )
							continue;

						Vec3 dir = body.Position - Position;
						float distance = dir.Normalize();

						if( distance < radius )
						{
							float objImpulse = MathFunctions.Cos( ( distance * radiusInv ) *
								( MathFunctions.PI / 2 ) ) * Type.Impulse * DamageCoefficient;
							objImpulse *= bodyCountInv;

							//forcePos for torque
							Vec3 forcePos;
							{
								Vec3 gabarites = body.GetGlobalBounds().GetSize() * .05f;
								EngineRandom random = World.Instance.Random;
								forcePos = new Vec3(
									random.NextFloatCenter() * gabarites.X,
									random.NextFloatCenter() * gabarites.Y,
									random.NextFloatCenter() * gabarites.Z );
							}
							body.AddForce( ForceType.GlobalAtLocalPos, 0, dir * objImpulse, forcePos );

							totalObjImpulse += objImpulse;
						}
					}
				}

				if( Type.Damage != 0 )
				{
					Dynamic dynamic = obj as Dynamic;
					if( dynamic != null )
					{
						float distance = ( dynamic.Position - Position ).Length();
						if( distance < radius )
						{
							float objDamage = MathFunctions.Cos( ( distance * radiusInv ) *
								( MathFunctions.PI / 2 ) ) * Type.Damage * DamageCoefficient;

							//add damages to cache and apply after influences (for correct influences work)
							damageItems.Add( new DamageItem( dynamic, obj.Position, objDamage ) );
							//dynamic.DoDamage( dynamic, obj.Position, objDamage, false );

							totalObjDamage += objDamage;
						}
					}
				}

				//PlayerCharacter contusion
				if( totalObjDamage > 10 && totalObjImpulse > 500 )
				{
					PlayerCharacter playerCharacter = obj as PlayerCharacter;
					if( playerCharacter != null )
						playerCharacter.ContusionTimeRemaining += totalObjDamage * .05f;
				}

				//Influence
				if( Type.InfluenceType != null && ( totalObjDamage != 0 || totalObjImpulse != 0 ) )
				{
					Dynamic dynamic = obj as Dynamic;
					if( dynamic != null )
					{
						float coef = 0;
						if( Type.Damage != 0 )
							coef = totalObjDamage / Type.Damage;
						if( Type.Impulse != 0 )
							coef = Math.Max( coef, totalObjImpulse / Type.Impulse );

						if( coef > 1 )
							coef = 1;
						if( coef != 0 )
							dynamic.AddInfluence( Type.InfluenceType, Type.InfluenceMaxTime * coef, true );
					}
				}
			}

			//Create splash for water
			CreateWaterPlaneSplash();

			//Apply damages
			foreach( DamageItem item in damageItems )
			{
				if( !item.dynamic.IsSetForDeletion )
					item.dynamic.DoDamage( this, item.position, null, item.damage, false );
			}

			mapObjectListAllocator.Free( objects );
			damageItemListAllocator.Free( damageItems );
		}

		void CreateWaterPlaneSplash()
		{
			float influenceRadius = Type.Radius * .75f;

			foreach( WaterPlane waterPlane in WaterPlane.Instances )
			{
				//check by height
				if( Position.Z + influenceRadius < waterPlane.Position.Z )
					continue;
				if( Position.Z - influenceRadius > waterPlane.Position.Z )
					continue;

				//check by bounds
				Rect bounds2 = new Rect( waterPlane.Position.ToVec2() );
				bounds2.Expand( waterPlane.Size * .5f );
				if( !bounds2.IsContainsPoint( Position.ToVec2() ) )
					continue;

				//check by physics
				float height = Position.Z + .01f;
				float waterHeight = waterPlane.Position.Z;
				Ray ray = new Ray( new Vec3( Position.X, Position.Y, height ),
					new Vec3( 0, 0, waterHeight - height ) );
				if( ray.Direction.Z != 0 )
				{
					RayCastResult result = PhysicsWorld.Instance.RayCast(
						ray, (int)ContactGroup.CastOnlyContact );
					if( result.Shape != null )
						continue;
				}

				//create splash
				waterPlane.CreateSplash( WaterPlaneType.SplashTypes.Explosion,
					new Vec3( Position.X, Position.Y, waterPlane.Position.Z ) );
			}
		}
	}
}
