// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="TurretAI"/> entity type.
	/// </summary>
	public class TurretAIType : AIType
	{
	}

	public class TurretAI : AI
	{
		List<Weapon> unitWeapons = new List<Weapon>();

		float updateTaskTimer;

		[FieldSerialize]
		Dynamic targetTask;

		///////////////////////////////////////////

		TurretAIType _type = null; public new TurretAIType Type { get { return _type; } }

		public TurretAI()
		{
			updateTaskTimer = World.Instance.Random.NextFloat() * 3;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();

			FindUnitWeapons();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( targetTask == entity )
				targetTask = null;

			//remove deleted weapon
			Weapon weapon = entity as Weapon;
			if( weapon != null )
				unitWeapons.Remove( weapon );
		}

		float GetAttackObjectPriority( Unit obj )
		{
			if( ControlledObject == obj )
				return 0;
			if( obj.Intellect == null )
				return 0;
			if( obj.Intellect.Faction == null )
				return 0;
			if( Faction == obj.Intellect.Faction )
				return 0;

			Vec3 distance = obj.Position - ControlledObject.Position;
			float len = distance.Length();
			return 1.0f / len + 1.0f;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			updateTaskTimer -= TickDelta;
			if( updateTaskTimer <= 0 )
			{
				updateTaskTimer += .5f;
				UpdateTask();
				if( targetTask == null )
					updateTaskTimer += 1;
			}

			TickTask();
		}

		void UpdateTask()
		{
			Unit newTarget = null;
			float newTargetPriority = 0;

			//find target
			{
				float radius = ControlledObject.ViewRadius;

				Map.Instance.GetObjects( new Sphere( ControlledObject.Position, radius ),
					MapObjectSceneGraphGroups.UnitGroupMask, delegate( MapObject mapObject )
				{
					Unit obj = (Unit)mapObject;

					//check by distance
					Vec3 diff = obj.Position - ControlledObject.Position;
					float objDistance = diff.Length();
					if( objDistance > radius )
						return;

					float priority = GetAttackObjectPriority( obj );
					if( priority != 0 && priority > newTargetPriority )
					{
						newTarget = obj;
						newTargetPriority = priority;
					}
				} );
			}

			//update targetTask
			if( newTarget != targetTask )
			{
				if( targetTask != null )
					UnsubscribeToDeletionEvent( targetTask );
				targetTask = newTarget;
				if( targetTask != null )
					SubscribeToDeletionEvent( targetTask );
			}
		}

		void TickTask()
		{
			ControlKeyRelease( GameControlKeys.Fire1 );
			ControlKeyRelease( GameControlKeys.Fire2 );

			if( targetTask != null )
			{
				Vec3 targetPos = targetTask.Position;

				Turret turret = ControlledObject as Turret;
				if( turret != null )
				{
					//to consider speed of the target
					if( turret.MainGun != null )
					{
						BulletType bulletType = turret.MainGun.Type.NormalMode.BulletType;
						if( bulletType.Velocity != 0 )
						{
							float flyTime = ( targetPos - ControlledObject.Position ).Length() /
								bulletType.Velocity;

							if( targetTask.PhysicsModel != null && targetTask.PhysicsModel.Bodies.Length != 0 )
							{
								Body targetBody = targetTask.PhysicsModel.Bodies[ 0 ];
								targetPos += targetBody.LinearVelocity * flyTime;
							}
						}
					}

					turret.SetMomentaryTurnToPosition( targetPos );
				}

				ControlKeyPress( GameControlKeys.Fire1, 1 );
				ControlKeyPress( GameControlKeys.Fire2, 1 );
			}
		}

		protected override void OnControlledObjectRender( Camera camera )
		{
			base.OnControlledObjectRender( camera );

			//debug geometry
			if( EngineDebugSettings.DrawGameSpecificDebugGeometry && 
				camera.Purpose == Camera.Purposes.MainCamera )
			{
				//view radius
				if( ControlledObject.ViewRadius != 0 )
				{
					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .5f );
					Vec3 lastPos = Vec3.Zero;
					for( float angle = 0; angle <= MathFunctions.PI * 2 + .001f;
						angle += MathFunctions.PI / 16 )
					{
						Vec3 pos = ControlledObject.Position +
							new Vec3( MathFunctions.Cos( angle ), MathFunctions.Sin( angle ), 0 ) *
							ControlledObject.ViewRadius;

						if( angle != 0 )
							camera.DebugGeometry.AddLine( lastPos, pos );

						lastPos = pos;
					}
				}

				//weapons
				foreach( Weapon weapon in unitWeapons )
				{
					float radius = 0;

					if( weapon.Type.WeaponNormalMode.IsInitialized )
						radius = Math.Max( radius, weapon.Type.WeaponNormalMode.UseDistanceRange.Maximum );
					if( weapon.Type.WeaponAlternativeMode.IsInitialized )
						radius = Math.Max( radius, weapon.Type.WeaponAlternativeMode.UseDistanceRange.Maximum );

					camera.DebugGeometry.Color = new ColorValue( 1, 0, 0, .5f );
					Vec3 lastPos = Vec3.Zero;
					for( float angle = 0; angle <= MathFunctions.PI * 2 + .001f;
						angle += MathFunctions.PI / 16 )
					{
						Vec3 pos = weapon.Position +
							new Vec3( MathFunctions.Cos( angle ), MathFunctions.Sin( angle ), 0 ) * radius;

						if( angle != 0 )
							camera.DebugGeometry.AddLine( lastPos, pos );

						lastPos = pos;
					}
				}

				//target task
				if( targetTask != null )
				{
					Vec3 targetPos = targetTask.Position;
					camera.DebugGeometry.AddArrow( ControlledObject.Position, targetPos, 1 );
					camera.DebugGeometry.AddSphere( new Sphere( targetPos, 3 ), 10 );
				}
			}
		}

		void FindUnitWeapons()
		{
			foreach( MapObjectAttachedObject attachedObject in ControlledObject.AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject =
					attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject != null )
				{
					Weapon weapon = attachedMapObject.MapObject as Weapon;
					if( weapon != null )
						unitWeapons.Add( weapon );
				}
			}

			foreach( Weapon weapon in unitWeapons )
				SubscribeToDeletionEvent( weapon );
		}
	}
}
