// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="GameCharacterAI"/> entity type.
	/// </summary>
	public class GameCharacterAIType : AIType
	{
	}

	public class GameCharacterAI : AI
	{
		//cached list of weapons for better performance
		Weapon[] weapons;

		[FieldSerialize]
		AutomaticTasksEnum automaticTasks = AutomaticTasksEnum.Enabled;
		[FieldSerialize]
		float findAutomaticTaskRemainingTime;

		Task currentTask;
		Queue<Task> tasks = new Queue<Task>();
		ReadOnlyQueue<Task> tasksReadOnly;

		PathController pathController = new PathController();

		[FieldSerialize]
		bool alwaysRun;

		///////////////////////////////////////////

		public enum AutomaticTasksEnum
		{
			Disabled,
			EnabledOnlyWhenNoTasks,
			Enabled,
		}

		///////////////////////////////////////////

		public abstract class Task
		{
			GameCharacterAI owner;

			Vec3 taskPosition;
			MapObject taskEntity;

			//

			protected Task( GameCharacterAI owner, Vec3 position, MapObject entity )
			{
				this.owner = owner;
				this.taskPosition = position;
				this.taskEntity = entity;
			}

			public GameCharacterAI Owner
			{
				get { return owner; }
			}

			public Vec3 TaskPosition
			{
				get { return taskPosition; }
			}

			public MapObject TaskEntity
			{
				get { return taskEntity; }
			}

			protected virtual bool OnLoad( TextBlock block )
			{
				if( block.IsAttributeExist( "taskPosition" ) )
					taskPosition = Vec3.Parse( block.GetAttribute( "taskPosition" ) );
				if( block.IsAttributeExist( "taskEntity" ) )
				{
					taskEntity = Entities.Instance.GetLoadingEntityBySerializedUIN(
						uint.Parse( block.GetAttribute( "taskEntity" ) ) ) as MapObject;
					if( taskEntity == null )
						return false;
				}
				return true;
			}
			internal bool _Load( TextBlock block ) { return OnLoad( block ); }

			protected virtual void OnSave( TextBlock block )
			{
				if( taskPosition != Vec3.Zero )
					block.SetAttribute( "taskPosition", taskPosition.ToString() );
				if( taskEntity != null )
					block.SetAttribute( "taskEntity", taskEntity.UIN.ToString() );
			}
			internal void _Save( TextBlock block ) { OnSave( block ); }

			protected virtual void OnBegin() { }
			internal void _Begin() { OnBegin(); }

			protected virtual void OnTick() { }
			internal void _Tick() { OnTick(); }

			protected abstract bool OnIsFinished();
			public bool IsFinished() { return OnIsFinished(); }

			public virtual Vec3 GetTargetPosition()
			{
				if( TaskEntity != null )
					return TaskEntity.Position;
				else
					return TaskPosition;
			}
		}

		///////////////////////////////////////////

		public class IdleTask : Task
		{
			public IdleTask( GameCharacterAI owner )
				: base( owner, Vec3.Zero, null ) { }

			public override string ToString()
			{
				return "Idle";
			}

			protected override void OnBegin()
			{
				base.OnBegin();

				//stop unit
				Owner.ControlledObject.SetForceMoveVector( Vec2.Zero );

				//reset old path
				Owner.pathController.Reset();
			}

			protected override bool OnIsFinished()
			{
				return false;
			}
		}

		///////////////////////////////////////////

		public class MoveTask : Task
		{
			float reachDistance;

			//

			public MoveTask( GameCharacterAI owner, Vec3 position, float reachDistance )
				: base( owner, position, null )
			{
				this.reachDistance = reachDistance;
			}

			public MoveTask( GameCharacterAI owner, MapObject entity, float reachDistance )
				: base( owner, Vec3.Zero, entity )
			{
				this.reachDistance = reachDistance;
			}

			public float ReachDistance
			{
				get { return reachDistance; }
			}

			public override string ToString()
			{
				return "Move: " + ( TaskEntity != null ? TaskEntity.ToString() : TaskPosition.ToString( 1 ) );
			}

			protected override bool OnLoad( TextBlock block )
			{
				if( !base.OnLoad( block ) )
					return false;
				if( block.IsAttributeExist( "reachDistance" ) )
					reachDistance = float.Parse( block.GetAttribute( "reachDistance" ) );
				return true;
			}

			protected override void OnSave( TextBlock block )
			{
				base.OnSave( block );
				block.SetAttribute( "reachDistance", reachDistance.ToString() );
			}

			protected override void OnBegin()
			{
				base.OnBegin();

				Owner.pathController.Reset();
			}

			bool IsAllowUpdateControlledObject()
			{
				//bad for system with disabled renderer, because here game logic depends animation.
				AnimationTree tree = Owner.ControlledObject.GetFirstAnimationTree();
				if( tree != null && tree.GetActiveTriggers().Count != 0 )
					return false;
				return true;
			}

			protected override void OnTick()
			{
				base.OnTick();

				GameCharacter controlledObj = Owner.ControlledObject;

				if( IsAllowUpdateControlledObject() && controlledObj.GetElapsedTimeSinceLastGroundContact() < .3f )//IsOnGround() )
				{
					//update path controller
					Owner.pathController.Update( Entity.TickDelta, controlledObj.Position,
						GetTargetPosition(), false );

					//update character
					Vec3 nextPointPosition;
					if( Owner.pathController.GetNextPointPosition( out nextPointPosition ) )
					{
						Vec2 vector = nextPointPosition.ToVec2() - controlledObj.Position.ToVec2();
						if( vector != Vec2.Zero )
							vector.Normalize();
						controlledObj.SetTurnToPosition( nextPointPosition );
						controlledObj.SetForceMoveVector( vector );
					}
					else
						controlledObj.SetForceMoveVector( Vec2.Zero );
				}
				else
					controlledObj.SetForceMoveVector( Vec2.Zero );
			}

			protected override bool OnIsFinished()
			{
				Vec3 targetPosition = GetTargetPosition();
				Vec3 objectPosition = Owner.ControlledObject.Position;
				if( ( GetTargetPosition().ToVec2() - objectPosition.ToVec2() ).Length() < reachDistance &&
					Math.Abs( objectPosition.Z - targetPosition.Z ) < 1.5f )
				{
					return true;
				}
				return false;
			}
		}

		///////////////////////////////////////////

		public class AttackTask : Task
		{
			public AttackTask( GameCharacterAI owner, Vec3 position )
				: base( owner, position, null ) { }
			public AttackTask( GameCharacterAI owner, MapObject entity )
				: base( owner, Vec3.Zero, entity ) { }

			public override string ToString()
			{
				return "Attack: " + ( TaskEntity != null ? TaskEntity.ToString() : TaskPosition.ToString( 1 ) );
			}

			protected override void OnBegin()
			{
				base.OnBegin();

				Owner.pathController.Reset();
			}

			bool IsAllowUpdateControlledObject()
			{
				//bad for system with disabled renderer, because here game logic depends animation.
				AnimationTree tree = Owner.ControlledObject.GetFirstAnimationTree();
				if( tree != null && tree.GetActiveTriggers().Count != 0 )
					return false;
				return true;
			}

			bool CheckDirectVisibilityByRayCast( Vec3 from, Vec3 targetPosition, MapObject targetObject )
			{
				GameCharacter controlledObj = Owner.ControlledObject;

				Ray ray = new Ray( from, targetPosition - from );

				RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
					ray, (int)ContactGroup.CastOnlyContact );
				foreach( RayCastResult result in piercingResult )
				{
					MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );
					if( obj != null )
					{
						//skip target object
						if( targetObject != null && obj == targetObject )
							continue;
						//skip controlled object
						if( obj == controlledObj )
							continue;
					}

					//found body which breaks visibility
					return false;
				}

				return true;
			}

			protected override void OnTick()
			{
				base.OnTick();

				GameCharacter controlledObj = Owner.ControlledObject;

				if( IsAllowUpdateControlledObject() && controlledObj.GetElapsedTimeSinceLastGroundContact() < .3f )//IsOnGround() )
				{
					float targetDistance = ( GetTargetPosition() - controlledObj.Position ).Length();

					//movement
					{
						Vec3 rayCastFrom;
						if( Owner.weapons.Length != 0 )
							rayCastFrom = Owner.weapons[ 0 ].Position;
						else
							rayCastFrom = controlledObj.Position;

						Range optimalAttackDistanceRange = controlledObj.Type.OptimalAttackDistanceRange;
						if( targetDistance < optimalAttackDistanceRange.Maximum &&
							CheckDirectVisibilityByRayCast( rayCastFrom, GetTargetPosition(), TaskEntity ) )
						{
							//destination target is visible
							controlledObj.SetTurnToPosition( GetTargetPosition() );

							if( targetDistance < optimalAttackDistanceRange.Minimum )
							{
								//move backward
								Vec2 direction = GetTargetPosition().ToVec2() - controlledObj.Position.ToVec2();
								if( direction != Vec2.Zero )
									direction = -direction.GetNormalize();
								controlledObj.SetForceMoveVector( direction );
							}
							else
							{
								//sees the target and stay on optimal attack distance
								controlledObj.SetForceMoveVector( Vec2.Zero );
							}
						}
						else
						{
							//need move

							//update path controller
							Owner.pathController.Update( Entity.TickDelta, controlledObj.Position,
								GetTargetPosition(), false );

							//update character
							Vec3 nextPointPosition;
							if( Owner.pathController.GetNextPointPosition( out nextPointPosition ) )
							{
								Vec2 vector = nextPointPosition.ToVec2() - controlledObj.Position.ToVec2();
								if( vector != Vec2.Zero )
									vector.Normalize();

								controlledObj.SetTurnToPosition( nextPointPosition );
								controlledObj.SetForceMoveVector( vector );
							}
							else
							{
								controlledObj.SetForceMoveVector( Vec2.Zero );
							}
						}
					}

					//shot
					foreach( Weapon weapon in Owner.weapons )
					{
						if( weapon.Ready )
						{
							Range normalRange = weapon.Type.WeaponNormalMode.UseDistanceRange;
							bool normalInRange = targetDistance >= normalRange.Minimum &&
								targetDistance <= normalRange.Maximum;

							Range alternativeRange = weapon.Type.WeaponAlternativeMode.UseDistanceRange;
							bool alternativeInRange = targetDistance >= alternativeRange.Minimum &&
								targetDistance <= alternativeRange.Maximum;

							if( ( normalInRange || alternativeInRange ) &&
								CheckDirectVisibilityByRayCast( weapon.Position, GetTargetPosition(), TaskEntity ) )
							{
								//update weapon fire orientation
								{
									Vec3 pos = GetTargetPosition();
									Gun gun = weapon as Gun;
									if( gun != null )
										gun.GetAdvanceAttackTargetPosition( false, TaskEntity, false, out pos );
									weapon.SetForceFireRotationLookTo( pos );
								}

								controlledObj.SetTurnToPosition( GetTargetPosition() );
								if( normalInRange )
									weapon.TryFire( false );
								if( alternativeInRange )
									weapon.TryFire( true );
							}
						}
					}
				}
				else
				{
					controlledObj.SetForceMoveVector( Vec2.Zero );
				}
			}

			protected override bool OnIsFinished()
			{
				if( TaskEntity != null && TaskEntity.IsSetForDeletion )
					return true;
				return false;
			}
		}

		///////////////////////////////////////////

		class PathController
		{
			readonly float reachDestinationPointDistance = .5f;
			readonly float reachDestinationPointZDifference = 1.5f;
			readonly float maxAllowableDeviationFromPath = .5f;
			readonly float updatePathWhenTargetPositionHasChangedMoreThanDistance = 2;
			readonly float stepSize = 1;
			readonly Vec3 polygonPickExtents = new Vec3( 2, 2, 2 );
			readonly int maxPolygonPath = 512;
			readonly int maxSmoothPath = 4096;
			readonly int maxSteerPoints = 16;

			Vec3 foundPathForTargetPosition = new Vec3( float.NaN, float.NaN, float.NaN );
			Vec3[] path;
			float pathFindWaitTime;
			int currentIndex;

			//

			RecastNavigationSystem GetNavigationSystem()
			{
				//use first instance on the map
				if( RecastNavigationSystem.Instances.Count != 0 )
					return RecastNavigationSystem.Instances[ 0 ];
				return null;
			}

			public void DropPath()
			{
				foundPathForTargetPosition = new Vec3( float.NaN, float.NaN, float.NaN );
				path = null;
				currentIndex = 0;
			}

			public void Reset()
			{
				DropPath();
			}

			public void Update( float delta, Vec3 unitPosition, Vec3 targetPosition, bool dropPath )
			{
				if( dropPath )
					DropPath();

				//wait before last path find
				if( pathFindWaitTime > 0 )
				{
					pathFindWaitTime -= delta;
					if( pathFindWaitTime < 0 )
						pathFindWaitTime = 0;
				}

				//already on target position?
				if( ( unitPosition.ToVec2() - targetPosition.ToVec2() ).LengthSqr() <
					reachDestinationPointDistance * reachDestinationPointDistance &&
					Math.Abs( unitPosition.Z - targetPosition.Z ) < reachDestinationPointZDifference )
				{
					DropPath();
					return;
				}

				//drop path when target position was updated
				if( path != null && ( foundPathForTargetPosition - targetPosition ).Length() >
					updatePathWhenTargetPositionHasChangedMoreThanDistance )
				{
					DropPath();
				}

				//drop path when unit goaway from path
				if( path != null && currentIndex > 0 )
				{
					Vec3 previous = path[ currentIndex - 1 ];
					Vec3 next = path[ currentIndex ];

					float min = Math.Min( previous.Z, next.Z );
					float max = Math.Max( previous.Z, next.Z );

					Vec2 projectedPoint = MathUtils.ProjectPointToLine(
						previous.ToVec2(), next.ToVec2(), unitPosition.ToVec2() );
					float distance2D = ( unitPosition.ToVec2() - projectedPoint ).Length();

					if( distance2D > maxAllowableDeviationFromPath ||
						unitPosition.Z + reachDestinationPointZDifference < min ||
						unitPosition.Z - reachDestinationPointZDifference > max )
					{
						DropPath();
					}
				}

				//check if need update path
				if( path == null && pathFindWaitTime == 0 )
				{
					bool found;

					RecastNavigationSystem system = GetNavigationSystem();
					if( system != null )
					{
						found = system.FindPath( unitPosition, targetPosition, stepSize, polygonPickExtents,
							maxPolygonPath, maxSmoothPath, maxSteerPoints, out path );
					}
					else
					{
						found = true;
						path = new Vec3[] { targetPosition };
					}

					currentIndex = 0;

					if( found )
					{
						foundPathForTargetPosition = targetPosition;
						//can't find new path during specified time.
						pathFindWaitTime = .3f;
					}
					else
					{
						foundPathForTargetPosition = new Vec3( float.NaN, float.NaN, float.NaN );
						//can't find new path during specified time.
						pathFindWaitTime = 1.0f;
					}
				}

				//progress
				if( path != null )
				{
					Vec3 point;
					while( true )
					{
						point = path[ currentIndex ];

						if( ( unitPosition.ToVec2() - point.ToVec2() ).LengthSqr() <
							reachDestinationPointDistance * reachDestinationPointDistance &&
							Math.Abs( unitPosition.Z - point.Z ) < reachDestinationPointZDifference )
						{
							//reach point
							currentIndex++;
							if( currentIndex == path.Length )
							{
								//path is ended
								DropPath();
								break;
							}
						}
						else
							break;
					}
				}
			}

			public bool GetNextPointPosition( out Vec3 position )
			{
				if( path != null )
				{
					position = path[ currentIndex ];
					return true;
				}
				position = Vec3.Zero;
				return false;
			}

			public void DebugDrawPath( Camera camera )
			{
				if( path != null )
				{
					Vec3 offset = new Vec3( 0, 0, .2f );

					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
					for( int n = 1; n < path.Length; n++ )
					{
						Vec3 from = path[ n - 1 ] + offset;
						Vec3 to = path[ n ] + offset;
						AddThicknessLine( camera, from, to, .07f );
						camera.DebugGeometry.AddLine( from, to );
					}

					camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
					foreach( Vec3 point in path )
						AddSphere( camera, new Sphere( point + offset, .15f ) );
				}
			}
		}

		///////////////////////////////////////////

		GameCharacterAIType _type = null; public new GameCharacterAIType Type { get { return _type; } }

		public GameCharacterAI()
		{
			currentTask = new IdleTask( this );
			tasksReadOnly = new ReadOnlyQueue<Task>( tasks );
		}

		Task CreateTaskByClassName( string className )
		{
			if( className == "IdleTask" )
				return new IdleTask( this );
			if( className == "MoveTask" )
				return new MoveTask( this, Vec3.Zero, 0 );
			if( className == "AttackTask" )
				return new AttackTask( this, Vec3.Zero );

			Log.Fatal( "GameCharacterAI: CreateTaskByClassName: Unknown task class name {0}.", className );
			return null;
		}

		string GetTaskClassName( Task task )
		{
			if( task is IdleTask )
				return "IdleTask";
			if( task is MoveTask )
				return "MoveTask";
			if( task is AttackTask )
				return "AttackTask";

			Log.Fatal( "GameCharacterAI: GetTaskClassName: Unknown task class." );
			return "";
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			//currentTask
			{
				TextBlock taskBlock = block.FindChild( "currentTask" );
				if( taskBlock != null )
				{
					Task task = CreateTaskByClassName( taskBlock.GetAttribute( "class" ) );
					if( task._Load( taskBlock ) )
						currentTask = task;
				}
			}

			//tasks
			TextBlock tasksBlock = block.FindChild( "tasks" );
			if( tasksBlock != null )
			{
				foreach( TextBlock taskBlock in tasksBlock.Children )
				{
					if( taskBlock.Name == "item" )
					{
						Task task = CreateTaskByClassName( taskBlock.GetAttribute( "class" ) );
						if( task._Load( taskBlock ) )
							tasks.Enqueue( task );
					}
				}
			}

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( !( currentTask is IdleTask ) )
			{
				TextBlock taskBlock = block.AddChild( "currentTask" );
				taskBlock.SetAttribute( "class", GetTaskClassName( currentTask ) );
				currentTask._Save( taskBlock );
			}

			if( tasks.Count != 0 )
			{
				TextBlock tasksBlock = block.AddChild( "tasks" );
				foreach( Task task in tasks )
				{
					TextBlock taskBlock = tasksBlock.AddChild( "item" );
					taskBlock.SetAttribute( "class", GetTaskClassName( task ) );
					task._Save( taskBlock );
				}
			}
		}

		public AutomaticTasksEnum AutomaticTasks
		{
			get { return automaticTasks; }
			set { automaticTasks = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();
		}

		protected override void OnDestroy()
		{
			ClearTaskQueue();

			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			bool contains = false;
			foreach( Task task in tasks )
			{
				if( task.TaskEntity == entity )
				{
					contains = true;
					break;
				}
			}
			if( contains )
			{
				Queue<Task> newTasks = new Queue<Task>( tasks.Count );
				foreach( Task task in tasks )
				{
					if( task.TaskEntity != entity )
						newTasks.Enqueue( task );
				}
				tasks = newTasks;
			}

			if( currentTask.TaskEntity == entity )
				DoNextTask();
		}

		[Browsable( false )]
		public new GameCharacter ControlledObject
		{
			get { return (GameCharacter)base.ControlledObject; }
		}

		void UpdateWeaponsList()
		{
			List<Weapon> list = new List<Weapon>();
			foreach( MapObjectAttachedObject attachedObject in ControlledObject.AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject != null )
				{
					Weapon weapon = attachedMapObject.MapObject as Weapon;
					if( weapon != null )
						list.Add( weapon );
				}
			}
			weapons = list.ToArray();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( ControlledObject != null )
			{
				//update weapons list
				if( weapons == null )
					UpdateWeaponsList();

				//tick task
				currentTask._Tick();
				if( currentTask.IsFinished() )
					DoNextTask();

				//find task automatically
				if( automaticTasks == AutomaticTasksEnum.Enabled ||
					automaticTasks == AutomaticTasksEnum.EnabledOnlyWhenNoTasks && currentTask is IdleTask )
				{
					findAutomaticTaskRemainingTime -= TickDelta;
					if( findAutomaticTaskRemainingTime <= 0 )
					{
						if( FindAutomaticTask() )
							findAutomaticTaskRemainingTime = .2f + World.Instance.Random.NextFloat() * .1f;
						else
							findAutomaticTaskRemainingTime = 1.5f + World.Instance.Random.NextFloat() * .1f;
					}
				}
			}
		}

		protected override void OnControlledObjectRender( Camera camera )
		{
			base.OnControlledObjectRender( camera );

			if( EngineDebugSettings.DrawGameSpecificDebugGeometry &&
				camera.Purpose == Camera.Purposes.MainCamera && ControlledObject != null )
			{
				DebugDrawTasks( camera );
				DebugDrawPath( camera );
			}
		}

		void ClearTaskQueue()
		{
			foreach( Task task in tasks )
			{
				if( task.TaskEntity != null )
					UnsubscribeToDeletionEvent( task.TaskEntity );
			}
			tasks.Clear();
		}

		void DoTaskInternal( Task task )
		{
			if( currentTask.TaskEntity != null )
				UnsubscribeToDeletionEvent( currentTask.TaskEntity );

			currentTask = task;

			if( currentTask.TaskEntity != null )
				SubscribeToDeletionEvent( currentTask.TaskEntity );
			currentTask._Begin();
		}

		public void DoTask( Task task, bool toQueue )
		{
			if( ControlledObject == null )
				return;

			if( toQueue && tasks.Count == 0 && currentTask is IdleTask )
				toQueue = false;

			if( !toQueue )
			{
				ClearTaskQueue();
				DoTaskInternal( task );
			}
			else
			{
				//add task to queue
				if( task.TaskEntity != null )
					SubscribeToDeletionEvent( task.TaskEntity );
				tasks.Enqueue( task );
			}
		}

		public void DoNextTask()
		{
			if( ControlledObject == null )
				return;

			if( tasks.Count != 0 )
			{
				//remove task from queue
				Task task = tasks.Dequeue();
				if( task.TaskEntity != null )
					UnsubscribeToDeletionEvent( task.TaskEntity );

				DoTaskInternal( task );
			}
			else
			{
				DoTask( new IdleTask( this ), false );
			}
		}

		public Task CurrentTask
		{
			get { return currentTask; }
		}

		public ReadOnlyQueue<Task> Tasks
		{
			get { return tasksReadOnly; }
		}

		public void DebugDrawTasks( Camera camera )
		{
			if( ControlledObject == null )
				return;
			if( currentTask is IdleTask )
				return;

			Vec3 offset = new Vec3( 0, 0, .2f );

			//current task
			{
				Vec3 pos = currentTask.GetTargetPosition() + offset;
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				AddArrow( camera, pos + new Vec3( 0, 0, 4 ), pos, .07f, 1.2f );
			}

			//task queue
			foreach( Task task in tasks )
			{
				Vec3 pos = task.GetTargetPosition() + offset;
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );
				AddArrow( camera, pos + new Vec3( 0, 0, 4 ), pos, .07f, 1.2f );
			}
		}

		public void DebugDrawPath( Camera camera )
		{
			pathController.DebugDrawPath( camera );
		}

		protected float GetTaskAttackObjectPriority( Unit obj )
		{
			if( ControlledObject != obj )
			{
				if( obj.Intellect != null && Faction != obj.Intellect.Faction )
				{
					Vec3 distance = obj.Position - ControlledObject.Position;
					float len = distance.Length();
					if( len == 0 )
						len = .01f;
					return 1.0f / len + 1.0f;
				}
			}
			return 0;
		}

		bool FindAutomaticTask()
		{
			GameCharacter controlledObj = ControlledObject;

			float maximalPriority = 0;
			MapObject needAttackObject = null;

			//find object. units only.
			Map.Instance.GetObjects( new Sphere( controlledObj.Position, controlledObj.ViewRadius ),
				MapObjectSceneGraphGroups.UnitGroupMask, delegate( MapObject mapObject )
			{
				Unit unit = (Unit)mapObject;

				//check distance
				Vec3 diff = unit.Position - controlledObj.Position;
				float objDistanceSqr = diff.LengthSqr();
				if( objDistanceSqr <= controlledObj.ViewRadius * controlledObj.ViewRadius )
				{
					float priority;

					//Attack task
					if( weapons.Length != 0 )
					{
						priority = GetTaskAttackObjectPriority( unit );
						if( priority != 0 && priority > maximalPriority )
						{
							maximalPriority = priority;
							needAttackObject = unit;
						}
					}
				}

			} );

			//do task
			if( needAttackObject != null )
			{
				MapObject currentAttackedUnit = null;
				{
					AttackTask currentAttackTask = currentTask as AttackTask;
					if( currentAttackTask != null )
						currentAttackedUnit = currentAttackTask.TaskEntity;
				}
				if( needAttackObject != currentAttackedUnit )
					DoTask( new AttackTask( this, needAttackObject ), false );

				return true;
			}

			return false;
		}

		public bool AlwaysRun
		{
			get { return alwaysRun; }
			set { alwaysRun = value; }
		}

		public override bool IsAlwaysRun()
		{
			if( !( currentTask is IdleTask ) )
				return alwaysRun;
			else
				return base.IsAlwaysRun();
		}

		static Vec3[] addSpherePositions;
		static int[] addSphereIndices;
		static void AddSphere( Camera camera, Sphere sphere )
		{
			if( addSpherePositions == null )
				GeometryGenerator.GenerateSphere( sphere.Radius, 8, 8, false, out addSpherePositions, out addSphereIndices );

			Mat4 transform = new Mat4( Mat3.Identity, sphere.Origin );
			camera.DebugGeometry.AddVertexIndexBuffer( addSpherePositions, addSphereIndices, transform, false, true );
		}

		static Vec3[] addThicknessLinePositions;
		static int[] addThicknessLineIndices;
		static void AddThicknessLine( Camera camera, Vec3 start, Vec3 end, float thickness )
		{
			Vec3 diff = end - start;
			Vec3 direction = diff.GetNormalize();
			Quat rotation = Quat.FromDirectionZAxisUp( direction );
			float length = diff.Length();
			float thickness2 = thickness;

			Mat4 t = new Mat4( rotation.ToMat3() * Mat3.FromScale( new Vec3( length, thickness2, thickness2 ) ),
				( start + end ) * .5f );

			if( addThicknessLinePositions == null )
				GeometryGenerator.GenerateBox( new Vec3( 1, 1, 1 ), out addThicknessLinePositions, out addThicknessLineIndices );
			camera.DebugGeometry.AddVertexIndexBuffer( addThicknessLinePositions, addThicknessLineIndices, t, false, true );
		}

		static void AddCone( Camera camera, Vec3 from, Vec3 to, float radius )
		{
			Vec3 direction = to - from;
			float length = direction.Normalize();

			Vec3[] positions;
			int[] indices;
			GeometryGenerator.GenerateCone( radius, length, 32, true, true, out positions, out indices );

			Quat rotation = Quat.FromDirectionZAxisUp( direction );
			Mat4 transform = new Mat4( rotation.ToMat3(), from );
			camera.DebugGeometry.AddVertexIndexBuffer( positions, indices, transform, false, true );
		}

		static void AddArrow( Camera camera, Vec3 from, Vec3 to, float thickness, float arrowSize )
		{
			Vec3 dir = ( to - from ).GetNormalize();
			float size = ( to - from ).Length();

			AddThicknessLine( camera, from, from + dir * ( size - arrowSize ), thickness );
			camera.DebugGeometry.AddLine( from, from + dir * ( size - arrowSize ) );

			AddCone( camera, from + dir * ( size - arrowSize ), to, arrowSize / 6 );
		}
	}
}
