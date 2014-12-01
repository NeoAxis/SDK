// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="TankGameUnitAI"/> entity type.
	/// </summary>
	public class TankGameUnitAIType : AIType
	{
		[FieldSerialize]
		float notifyAlliesRadius = 50;

		[DefaultValue( 50.0f )]
		public float NotifyAlliesRadius
		{
			get { return notifyAlliesRadius; }
			set { notifyAlliesRadius = value; }
		}
	}

	public class TankGameUnitAI : AI
	{
		//initial data
		Region activationRegion;

		//general task
		[FieldSerialize]
		GeneralTaskTypes generalTaskType;
		[FieldSerialize]
		MapCurve generalTaskWay;
		[FieldSerialize]
		MapCurvePoint generalTaskCurrentWayPoint;
		float generalTaskUpdateTimer;

		//move task
		[FieldSerialize]
		bool moveTaskEnabled;
		[FieldSerialize]
		Vec3 moveTaskPosition;

		//attack tasks
		//we not serialize AttackTack because Weapon in the class cannot be serialized
		//(weapon will recreated after map loading)
		List<AttackTask> attackTasks = new List<AttackTask>();
		float attackTasksUpdateTimer;

		List<Weapon> unitWeapons;

		///////////////////////////////////////////

		public enum GeneralTaskTypes
		{
			None,
			WayMove, //by MapCurve (taken from TankGameExtendedProperties.Way)
			Battle,
		}

		///////////////////////////////////////////

		public class AttackTask
		{
			Weapon weapon;
			Vec3 targetPosition;
			Dynamic targetEntity;

			//

			public AttackTask( Weapon weapon, Vec3 target )
			{
				this.weapon = weapon;
				this.targetPosition = target;
				this.targetEntity = null;
			}

			public AttackTask( Weapon weapon, Dynamic target )
			{
				this.weapon = weapon;
				this.targetPosition = new Vec3( float.NaN, float.NaN, float.NaN );
				this.targetEntity = target;
			}

			public Weapon Weapon
			{
				get { return weapon; }
			}

			public Vec3 TargetPosition
			{
				get { return targetPosition; }
			}

			public Dynamic TargetEntity
			{
				get { return targetEntity; }
			}
		}

		///////////////////////////////////////////

		TankGameUnitAIType _type = null; public new TankGameUnitAIType Type { get { return _type; } }

		public TankGameUnitAI()
		{
			generalTaskUpdateTimer = World.Instance.Random.NextFloat() * 2;
			attackTasksUpdateTimer = World.Instance.Random.NextFloat() * 1;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();

			//get activationRegion
			TankGameExtendedProperties extendedProperties =
				ControlledObject.ExtendedProperties as TankGameExtendedProperties;
			if( extendedProperties != null )
				activationRegion = extendedProperties.ActivationRegion;

			//listen activationRegion
			if( activationRegion != null )
				activationRegion.ObjectIn += ActivationRegion_ObjectIn;
		}

		protected override void OnDestroy()
		{
			//stop listen activationRegion
			if( activationRegion != null )
			{
				activationRegion.ObjectIn -= ActivationRegion_ObjectIn;
				activationRegion = null;
			}
			base.OnDestroy();
		}

		protected override void OnTick()
		{
			base.OnTick();

			//tick general task
			generalTaskUpdateTimer -= TickDelta;
			if( generalTaskUpdateTimer <= 0 )
			{
				UpdateGeneralTask();
				generalTaskUpdateTimer += 1;
			}

			//tick attack tasks
			attackTasksUpdateTimer -= TickDelta;
			if( attackTasksUpdateTimer <= 0 )
			{
				TickAttackTasks();
				attackTasksUpdateTimer += .5f;
			}

			UpdateMoveTaskControlKeys();
			UpdateAttackTasksControlKeys();
		}

		protected override void OnControlledObjectRender( Camera camera )
		{
			base.OnControlledObjectRender( camera );

			//debug geometry
			if( EngineDebugSettings.DrawGameSpecificDebugGeometry &&
				camera.Purpose == Camera.Purposes.MainCamera )
			{
				//way
				if( generalTaskCurrentWayPoint != null )
				{
					ReadOnlyCollection<MapCurvePoint> points = generalTaskWay.Points;

					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .5f );
					int index = points.IndexOf( generalTaskCurrentWayPoint );
					for( ; index < points.Count - 1; index++ )
					{
						camera.DebugGeometry.AddArrow(
							points[ index ].Position, points[ index + 1 ].Position, 1 );
					}
				}

				//view radius
				if( ControlledObject.ViewRadius != 0 )
				{
					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .5f );
					Vec3 lastPos = Vec3.Zero;
					for( float angle = 0; angle <= MathFunctions.PI * 2 + .001f;
						angle += MathFunctions.PI / 16 )
					{
						Vec3 pos = ControlledObject.Position + new Vec3( MathFunctions.Cos( angle ),
							MathFunctions.Sin( angle ), 0 ) * ControlledObject.ViewRadius;

						if( angle != 0 )
							camera.DebugGeometry.AddLine( lastPos, pos );

						lastPos = pos;
					}
				}

				if( unitWeapons == null )
					FindUnitWeapons();

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

				//move task
				if( moveTaskEnabled )
				{
					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
					camera.DebugGeometry.AddArrow( ControlledObject.Position, moveTaskPosition, 1 );
				}

				//attack tasks
				foreach( AttackTask attackTask in attackTasks )
				{
					Vec3 targetPos = ( attackTask.TargetEntity != null ) ?
						attackTask.TargetEntity.Position : attackTask.TargetPosition;

					camera.DebugGeometry.Color = IsWeaponDirectedToTarget( attackTask ) ?
						new ColorValue( 1, 1, 0 ) : new ColorValue( 1, 0, 0 );
					camera.DebugGeometry.AddArrow( attackTask.Weapon.Position, targetPos, 1 );
					camera.DebugGeometry.AddSphere( new Sphere( targetPos, 3 ), 10 );
				}
			}
		}

		void ActivationRegion_ObjectIn( Entity entity, MapObject obj )
		{
			if( activationRegion == null )
				return;

			bool isPlayer = false;

			Unit unit = obj as Unit;
			if( unit != null && unit.Intellect as PlayerIntellect != null )
				isPlayer = true;

			if( isPlayer )
				ActivateRegion();
		}

		void ActivateRegion()
		{
			//stop listen activationRegion
			if( activationRegion != null )
			{
				activationRegion.ObjectIn -= ActivationRegion_ObjectIn;
				activationRegion = null;
			}

			ActivateWayMovement();
		}

		public void ActivateWayMovement()
		{
			MapCurve way = null;
			{
				TankGameExtendedProperties extendedProperties =
					ControlledObject.ExtendedProperties as TankGameExtendedProperties;
				if( extendedProperties != null )
					way = extendedProperties.Way;
			}

			if( way != null )
				DoGeneralTask( GeneralTaskTypes.WayMove, way );
		}

		void DoGeneralTask( GeneralTaskTypes type, MapCurve way )
		{
			generalTaskType = type;
			generalTaskWay = way;
			generalTaskCurrentWayPoint = null;

			if( generalTaskWay != null )
				generalTaskCurrentWayPoint = generalTaskWay;

			//if( generalTaskType == GeneralTaskTypes.None )
			ResetMoveTask();
		}

		void UpdateGeneralTask()
		{
			switch( generalTaskType )
			{
			case GeneralTaskTypes.WayMove:
				{
					const float wayPointCheckDistance = 10;

					float wayPointDistance = ( ControlledObject.Position -
						generalTaskCurrentWayPoint.Position ).Length();

					if( wayPointDistance < wayPointCheckDistance )
					{
						//next way point or stop

						int index = generalTaskWay.Points.IndexOf( generalTaskCurrentWayPoint );
						index++;

						if( index < generalTaskWay.Points.Count )
						{
							//next way point
							generalTaskCurrentWayPoint = generalTaskWay.Points[ index ];
						}
						else
						{
							//task completed
							DoGeneralTask( GeneralTaskTypes.None, null );
						}
					}

					if( generalTaskCurrentWayPoint != null )
						DoMoveTask( generalTaskCurrentWayPoint.Position );
				}
				break;

			case GeneralTaskTypes.Battle:
				{
					Dynamic enemy = FindEnemy( ControlledObject.ViewRadius );
					if( enemy != null )
					{
						//notify allies
						NotifyAlliesOnEnemy( enemy.Position );

						//Tank specific
						Tank tank = ControlledObject as Tank;
						if( tank != null )
						{
							Range range = tank.Type.OptimalAttackDistanceRange;
							float distance = ( enemy.Position - ControlledObject.Position ).Length();

							bool needMove = false;

							if( distance > range.Maximum )
								needMove = true;

							if( !needMove && attackTasks.Count != 0 )
							{
								//to check up a line of fire
								bool existsDirectedWeapons = false;
								foreach( AttackTask attackTask in attackTasks )
								{
									if( IsWeaponDirectedToTarget( attackTask ) )
									{
										existsDirectedWeapons = true;
										break;
									}
								}
								if( !existsDirectedWeapons )
									needMove = true;
							}

							if( needMove )
								DoMoveTask( enemy.Position );
							else
								ResetMoveTask();
						}

					}
					else
					{
						if( moveTaskEnabled )
						{
							const float needDistance = 10;
							float distance = ( moveTaskPosition - ControlledObject.Position ).Length();
							if( distance < needDistance )
								ResetMoveTask();
						}

						if( !moveTaskEnabled )
							DoGeneralTask( GeneralTaskTypes.None, null );
					}
				}
				break;
			}

			//find enemies
			{
				if( generalTaskType != GeneralTaskTypes.Battle )
				{
					Dynamic enemy = FindEnemy( ControlledObject.ViewRadius );
					if( enemy != null )
						DoGeneralTask( GeneralTaskTypes.Battle, null );
				}
			}
		}

		void TickAttackTasks()
		{
			if( unitWeapons == null )
				FindUnitWeapons();

			foreach( Weapon weapon in unitWeapons )
			{
				float radius = 0;
				if( weapon.Type.WeaponNormalMode.IsInitialized )
					radius = Math.Max( radius, weapon.Type.WeaponNormalMode.UseDistanceRange.Maximum );
				if( weapon.Type.WeaponAlternativeMode.IsInitialized )
					radius = Math.Max( radius, weapon.Type.WeaponAlternativeMode.UseDistanceRange.Maximum );

				Dynamic enemy = FindEnemy( radius );


				AttackTask task = attackTasks.Find( delegate( AttackTask t )
				{
					return t.Weapon == weapon;
				} );

				if( task != null )
				{
					if( task.TargetEntity != enemy )
					{
						if( enemy != null )
							DoAttackTask( weapon, enemy );
						else
							ResetAttackTask( task );
					}
				}
				else
				{
					if( enemy != null )
						DoAttackTask( weapon, enemy );
				}
			}
		}

		Vec3 CalculateTargetPosition( AttackTask attackTask )
		{
			Dynamic target = attackTask.TargetEntity;

			Vec3 targetPos = target != null ? target.Position : attackTask.TargetPosition;

			//to consider speed of the target
			if( target != null )
			{
				Gun gun = attackTask.Weapon as Gun;
				if( gun != null )
				{
					BulletType bulletType = gun.Type.NormalMode.BulletType;
					if( bulletType.Velocity != 0 )
					{
						float flyTime = ( targetPos - ControlledObject.Position ).Length() /
							bulletType.Velocity;

						if( target.PhysicsModel != null && target.PhysicsModel.Bodies.Length != 0 )
						{
							Body targetBody = target.PhysicsModel.Bodies[ 0 ];
							targetPos += targetBody.LinearVelocity * flyTime;
						}
					}
				}
			}

			return targetPos;
		}

		void UpdateAttackTasksControlKeys()
		{
			ControlKeyRelease( GameControlKeys.Fire1 );
			ControlKeyRelease( GameControlKeys.Fire2 );

			foreach( AttackTask attackTask in attackTasks )
			{
				//Tank specific
				Tank tank = ControlledObject as Tank;
				if( tank != null )
				{
					if( attackTask.Weapon == tank.MainGun )
					{
						Vec3 targetPos = CalculateTargetPosition( attackTask );

						//turn turret
						tank.SetNeedTurnToPosition( targetPos );

						//fire
						if( IsWeaponDirectedToTarget( attackTask ) )
							ControlKeyPress( GameControlKeys.Fire1, 1 );
					}
				}
			}
		}

		bool IsWeaponDirectedToTarget( AttackTask attackTask )
		{
			Vec3 targetPos = CalculateTargetPosition( attackTask );

			Weapon weapon = attackTask.Weapon;

			//to check up a weapon angle
			{
				Vec3 needDirection = ( targetPos - weapon.Position ).GetNormalize();
				Vec3 weaponDirection = weapon.Rotation.GetForward();

				Radian angle = Math.Abs( MathFunctions.ACos( Vec3.Dot( needDirection, weaponDirection ) ) );
				Radian minimalDifferenceAngle = new Degree( 2 ).InRadians();

				if( angle > minimalDifferenceAngle )
					return false;
			}

			//to check up a line of fire
			{
				Ray ray = new Ray( weapon.Position, targetPos - weapon.Position );

				RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
					ray, (int)ContactGroup.CastOnlyContact );

				foreach( RayCastResult result in piercingResult )
				{
					Dynamic dynamic = MapSystemWorld.GetMapObjectByBody( result.Shape.Body ) as Dynamic;
					if( dynamic != null )
					{
						Unit parentUnit = dynamic.GetParentUnit();
						if( parentUnit != null )
						{
							if( parentUnit == attackTask.TargetEntity )
								continue;
							if( parentUnit == ControlledObject )
								continue;
						}
					}

					return false;
				}
			}

			return true;
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

		Dynamic FindEnemy( float radius )
		{
			if( Faction == null )
				return null;

			Unit controlledObject = ControlledObject;

			Unit enemy = null;
			float enemyPriority = 0;

			Map.Instance.GetObjects( new Sphere( controlledObject.Position, radius ),
				 MapObjectSceneGraphGroups.UnitGroupMask, delegate( MapObject mapObject )
			{
				Unit obj = (Unit)mapObject;

				//check by distance
				Vec3 diff = obj.Position - controlledObject.Position;
				float objDistance = diff.Length();
				if( objDistance > radius )
					return;

				float priority = GetAttackObjectPriority( obj );
				if( priority != 0 && priority > enemyPriority )
				{
					enemy = obj;
					enemyPriority = priority;
				}
			} );

			return enemy;
		}

		void DoMoveTask( Vec3 pos )
		{
			moveTaskEnabled = true;
			moveTaskPosition = pos;
		}

		void ResetMoveTask()
		{
			moveTaskEnabled = false;
		}

		void UpdateMoveTaskControlKeys()
		{
			bool forward = false;
			//bool backward = false;
			bool left = false;
			bool right = false;

			if( moveTaskEnabled )
			{
				//Vehicle specific

				Vec3 unitPos = ControlledObject.Position;
				Vec3 unitDir = ControlledObject.Rotation.GetForward();
				Vec3 needDir = moveTaskPosition - unitPos;

				Radian unitAngle = MathFunctions.ATan( unitDir.Y, unitDir.X );
				Radian needAngle = MathFunctions.ATan( needDir.Y, needDir.X );

				Radian diffAngle = needAngle - unitAngle;
				while( diffAngle < -MathFunctions.PI )
					diffAngle += MathFunctions.PI * 2;
				while( diffAngle > MathFunctions.PI )
					diffAngle -= MathFunctions.PI * 2;

				//!!!!!!! 10.0f
				if( Math.Abs( diffAngle ) > new Degree( 10.0f ).InRadians() )
				{
					if( diffAngle > 0 )
						left = true;
					else
						right = true;
				}

				if( diffAngle > -MathFunctions.PI / 2 && diffAngle < MathFunctions.PI / 2 )
					forward = true;
			}

			if( forward )
				ControlKeyPress( GameControlKeys.Forward, 1 );
			else
				ControlKeyRelease( GameControlKeys.Forward );

			//if( backward )
			//   ControlKeyPress( ControlKey.Backward );
			//else
			//   ControlKeyRelease( ControlKey.Backward );

			if( left )
				ControlKeyPress( GameControlKeys.Left, 1 );
			else
				ControlKeyRelease( GameControlKeys.Left );

			if( right )
				ControlKeyPress( GameControlKeys.Right, 1 );
			else
				ControlKeyRelease( GameControlKeys.Right );
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			//reset related task
			again:
			foreach( AttackTask task in attackTasks )
			{
				if( task.Weapon == entity || task.TargetEntity == entity )
				{
					ResetAttackTask( task );
					goto again;
				}
			}

			//remove deleted weapon
			if( unitWeapons != null )
			{
				Weapon weapon = entity as Weapon;
				if( weapon != null )
					unitWeapons.Remove( weapon );
			}
		}

		void DoAttackTask( Weapon weapon, Vec3 target )
		{
			AttackTask task = attackTasks.Find( delegate( AttackTask t )
			{
				return t.Weapon == weapon;
			} );

			if( task != null && task.TargetPosition == target )
				return;

			ResetAttackTask( task );

			task = new AttackTask( weapon, target );
			attackTasks.Add( task );
		}

		void DoAttackTask( Weapon weapon, Dynamic target )
		{
			AttackTask task = attackTasks.Find( delegate( AttackTask t )
			{
				return t.Weapon == weapon;
			} );

			if( task != null && task.TargetEntity == target )
				return;

			if( task != null )
				ResetAttackTask( task );

			task = new AttackTask( weapon, target );
			SubscribeToDeletionEvent( target );
			attackTasks.Add( task );
		}

		void ResetAttackTask( AttackTask task )
		{
			if( task.TargetEntity != null )
				UnsubscribeToDeletionEvent( task.TargetEntity );
			attackTasks.Remove( task );
		}

		void ResetAllAttackTasks()
		{
			while( attackTasks.Count != 0 )
				ResetAttackTask( attackTasks[ attackTasks.Count - 1 ] );
		}

		void FindUnitWeapons()
		{
			unitWeapons = new List<Weapon>();

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

		public override bool IsActive()
		{
			return generalTaskType != GeneralTaskTypes.None;
		}

		protected override void OnControlledObjectChange( Unit oldObject )
		{
			base.OnControlledObjectChange( oldObject );

			if( oldObject != null )
				oldObject.Damage -= ControlledObject_Damage;
			if( ControlledObject != null )
				ControlledObject.Damage += ControlledObject_Damage;
		}

		void ControlledObject_Damage( Dynamic entity, MapObject prejudicial, Vec3 pos, float damage )
		{
			if( generalTaskType != GeneralTaskTypes.Battle && prejudicial != null )
			{
				Unit sourceUnit = null;

				Bullet bullet = prejudicial as Bullet;
				if( bullet != null )
					sourceUnit = bullet.SourceUnit;
				Explosion explosion = prejudicial as Explosion;
				if( explosion != null )
					sourceUnit = explosion.SourceUnit;

				if( sourceUnit != null )
				{
					Intellect unitIntellect = sourceUnit.Intellect as Intellect;
					if( unitIntellect != null && unitIntellect.Faction != Faction )
					{
						//do battle task
						DoGeneralTask( GeneralTaskTypes.Battle, null );

						//move to enemy
						DoMoveTask( sourceUnit.Position );

						//notify allies
						NotifyAlliesOnEnemy( sourceUnit.Position );
					}
				}
			}
		}

		void NotifyAlliesOnEnemy( Vec3 enemyPos )
		{
			if( Type.NotifyAlliesRadius <= 0 )
				return;

			Map.Instance.GetObjects( new Sphere( ControlledObject.Position, Type.NotifyAlliesRadius ),
				MapObjectSceneGraphGroups.UnitGroupMask, delegate( MapObject mapObject )
			{
				Unit unit = (Unit)mapObject;

				if( unit == ControlledObject )
					return;

				TankGameUnitAI unitAI = unit.Intellect as TankGameUnitAI;
				if( unitAI != null && unitAI.Faction == Faction )
				{
					unitAI.OnNotifyFromAllyOnEnemy( enemyPos );
				}
			} );
		}

		public void OnNotifyFromAllyOnEnemy( Vec3 enemyPos )
		{
			if( generalTaskType != GeneralTaskTypes.Battle )
			{
				//do battle task
				DoGeneralTask( GeneralTaskTypes.Battle, null );

				DoMoveTask( enemyPos );
			}
		}

	}
}
