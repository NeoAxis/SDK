// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.Renderer;
using Engine.Utils;
using Engine.FileSystem;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the type class of entity class <see cref="Dynamic"/>.
	/// </summary>
	public class DynamicType : MapObjectType
	{
		[FieldSerialize]
		float healthMax;
		[FieldSerialize]
		float healthMin;

		[FieldSerialize]
		float impulseDamageCoefficient;
		[FieldSerialize]
		float impulseMinimalDamage;

		[FieldSerialize]
		float targetPriority;

		[FieldSerialize]
		string soundCollision;
		[FieldSerialize]
		float soundCollisionMinVelocity = 1.0f;

		[FieldSerialize]
		Substance substance;

		[FieldSerialize]
		float dieLatency;

		[FieldSerialize]
		float lifeTime;

		[FieldSerialize]
		List<AutomaticInfluenceItem> automaticInfluences = new List<AutomaticInfluenceItem>();

		public enum SoundRolloffModes
		{
			Logarithmic,
			Linear,
			Manually,
		}
		[FieldSerialize]
		SoundRolloffModes soundRolloffMode = SoundRolloffModes.Logarithmic;
		[FieldSerialize]
		float soundMinDistance = 5;
		[FieldSerialize]
		float soundMaxDistance = 50;
		[FieldSerialize]
		float soundRolloffLogarithmicFactor = 1;

		MapObjectCreateObjectCollection dieObjects = new MapObjectCreateObjectCollection();

		///////////////////////////////////////////

		public class AutomaticInfluenceItem
		{
			[FieldSerialize]
			InfluenceType influence;
			[FieldSerialize]
			[DefaultValue( typeof( Range ), "0 0.5" )]
			Range lifeCoefficientRange = new Range( 0, .5f );

			public InfluenceType Influence
			{
				get { return influence; }
				set { influence = value; }
			}

			[DefaultValue( typeof( Range ), "0 0.5" )]
			public Range LifeCoefficientRange
			{
				get { return lifeCoefficientRange; }
				set { lifeCoefficientRange = value; }
			}

			public override string ToString()
			{
				if( influence == null )
					return "(not initialized)";
				return influence.Name;
			}
		}

		///////////////////////////////////////////

		[DefaultValue( 0.0f )]
		public float HealthMax
		{
			get { return healthMax; }
			set { healthMax = value; }
		}

		[DefaultValue( 0.0f )]
		public float HealthMin
		{
			get { return healthMin; }
			set { healthMin = value; }
		}

		[DefaultValue( 0.0f )]
		public float ImpulseDamageCoefficient
		{
			get { return impulseDamageCoefficient; }
			set { impulseDamageCoefficient = value; }
		}

		[DefaultValue( 0.0f )]
		public float ImpulseMinimalDamage
		{
			get { return impulseMinimalDamage; }
			set { impulseMinimalDamage = value; }
		}

		[DefaultValue( 0.0f )]
		public float TargetPriority
		{
			get { return targetPriority; }
			set { targetPriority = value; }
		}

		[DefaultValue( "" )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundCollision
		{
			get { return soundCollision; }
			set { soundCollision = value; }
		}

		[DefaultValue( 1.0f )]
		public float SoundCollisionMinVelocity
		{
			get { return soundCollisionMinVelocity; }
			set { soundCollisionMinVelocity = value; }
		}

		[DefaultValue( Substance.None )]
		public Substance Substance
		{
			get { return substance; }
			set { substance = value; }
		}

		[DefaultValue( 0.0f )]
		public float DieLatency
		{
			get { return dieLatency; }
			set { dieLatency = value; }
		}

		[DefaultValue( 0.0f )]
		public float LifeTime
		{
			get { return lifeTime; }
			set { lifeTime = value; }
		}

		[TypeConverter( typeof( CollectionTypeConverter ) )]
		[Editor( "ProjectEntities.Editor.DynamicType_AutomaticInfluencesCollectionEditor, ProjectEntities.Editor", typeof( UITypeEditor ) )]
		public List<AutomaticInfluenceItem> AutomaticInfluences
		{
			get { return automaticInfluences; }
		}

		[DefaultValue( SoundRolloffModes.Logarithmic )]
		public SoundRolloffModes SoundRolloffMode
		{
			get { return soundRolloffMode; }
			set { soundRolloffMode = value; }
		}

		[DefaultValue( 5.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 100.0f )]
		public float SoundMinDistance
		{
			get { return soundMinDistance; }
			set { soundMinDistance = value; }
		}

		[DefaultValue( 50.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 1000 )]
		public float SoundMaxDistance
		{
			get { return soundMaxDistance; }
			set { soundMaxDistance = value; }
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 10.0f )]
		public float SoundRolloffLogarithmicFactor
		{
			get { return soundRolloffLogarithmicFactor; }
			set { soundRolloffLogarithmicFactor = value; }
		}

		/// <summary>
		/// The collection of actions to process when Die() method is called.
		/// </summary>
		/// <remarks>
		/// Usually Die() method is used to destroy the object with ability to do actions such as creation of a new objects, play sound, etc.
		/// </remarks>
		[Description( "The collection of actions to process when Die() method is called. Usually Die() method is used to destroy the object with ability to do actions such as creation of a new objects, play sound, etc." )]
		public MapObjectCreateObjectCollection DieObjects
		{
			get { return dieObjects; }
		}

		///////////////////////////////////////////

		public void PreloadSound( string soundName, SoundMode mode )
		{
			if( !string.IsNullOrEmpty( soundName ) )
			{
				SoundWorld.Instance.SoundCreate(
					RelativePathUtils.ConvertToFullPath( Path.GetDirectoryName( FilePath ), soundName ), mode );
			}
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			PreloadSound( SoundCollision, SoundMode.Mode3D );

			//DieObjects
			foreach( MapObjectCreateObject createObject in DieObjects )
				createObject.PreloadResources( this );
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			//dieObjects
			TextBlock dieObjectsBlock = block.FindChild( "dieObjects" );
			if( dieObjectsBlock != null )
			{
				if( !dieObjects.Load( dieObjectsBlock ) )
					return false;
			}

			//old version compatibility
			if( block.IsAttributeExist( "lifeMax" ) )
				healthMax = float.Parse( block.GetAttribute( "lifeMax" ) );
			if( block.IsAttributeExist( "lifeMin" ) )
				healthMin = float.Parse( block.GetAttribute( "lifeMin" ) );

			return true;
		}

		protected override bool OnSave( TextBlock block )
		{
			if( !base.OnSave( block ) )
				return false;

			//dieObjects
			if( dieObjects.Count != 0 )
			{
				TextBlock dieObjectsBlock = block.AddChild( "dieObjects" );
				if( !dieObjects.Save( dieObjectsBlock ) )
					return false;
			}

			return true;
		}

		protected override bool OnIsExistsReferenceToObject( object obj )
		{
			if( dieObjects.IsExistsReferenceToObject( obj ) )
				return true;
			return base.OnIsExistsReferenceToObject( obj );
		}

		protected override void OnChangeReferencesToObject( object obj, object newValue )
		{
			base.OnChangeReferencesToObject( obj, newValue );
			dieObjects.ChangeReferencesToObject( obj, newValue );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Defines a class based on MapObject with additional functionality which usually useful to use in the project. 
	/// </summary>
	/// <remarks>
	/// Features:
	/// - Health system. The object can lose all health and die.
	/// - Ability to configure set of actions to execute at object destroying. As example: Create particle system, new object, play sound.
	/// - Playing sound at physical collision with another object.
	/// - Managing of incluences. The object can have set of attached influences. As example the object can take fire.
	/// - Networking support: Synchronization position, rotation of physical bodies.
	/// </remarks>
	public class Dynamic : MapObject
	{
		[FieldSerialize( FieldSerializeSerializationTypes.Map )]
		float healthFactorAtBeginning = 1;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float health;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float dieLatencyRamainingTime;

		bool died;

		float soundCollisionTimeRemaining;
		bool soundCollisionTimeRemainingTimerAdded;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float lifeTime;

		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float receiveDamageCoefficient = 1;

		[FieldSerialize]
		Influence[] automaticInfluences;

		bool server_enableSynchronizationPositionsToClients = true;

		//for NetworkMessages.PositionToClient (PhysicsModel == null)
		Vec3 server_sentPositionToClients = new Vec3( float.MaxValue, 0, 0 );
		Quat server_sentRotationToClients = Quat.Identity;
		Vec3 server_sentScaleToClients = new Vec3( 1, 1, 1 );

		//for NetworkMessages.BodiesPositionsToClient (PhysicsModel != null)
		Server_SentBodiesPositionsToClientsItem[] server_sentBodiesPositionsToClients;

		//we are using cache of positions for interpolation
		List<Client_ReceivePositionsSnapshot> client_receivePositionsSnapshots = new List<Client_ReceivePositionsSnapshot>();

		//Die objects
		bool dieObjectsCreated;

		///////////////////////////////////////////

		//for NetworkMessages.BodiesPositionsToClient (PhysicsModel != null)
		struct Server_SentBodiesPositionsToClientsItem
		{
			public Vec3 position;
			public Quat rotation;
		}

		///////////////////////////////////////////

		class Client_ReceivePositionsSnapshot
		{
			public int networkTickNumber;

			//for PhysicsModel == null
			public Vec3 position;
			public Quat rotation;
			public Vec3 scale;

			//for PhysicsModel != null
			public struct BodyItem
			{
				public Vec3 position;
				public Quat rotation;
			}
			public BodyItem[] bodies;
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			PositionsToClient,//used for object which have not a physics model
			BodiesPositionsToClient,//used for object which have a physics model
			HealthToClient,
			SoundPlayCollisionToClient,
			DieCallToClient,
		}

		///////////////////////////////////////////

		DynamicType _type = null; public new DynamicType Type { get { return _type; } }

		[DefaultValue( 1.0f )]
		[LogicSystemBrowsable( true )]
		public float HealthFactorAtBeginning
		{
			get { return healthFactorAtBeginning; }
			set { healthFactorAtBeginning = value; }
		}

		[Browsable( false )]
		[LogicSystemBrowsable( true )]
		public float Health
		{
			get { return health; }
			set
			{
				if( health == value )
					return;

				health = value;

				if( health < Type.HealthMin )
					health = Type.HealthMin;
				else if( health > Type.HealthMax )
					health = Type.HealthMax;

				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				{
					if( Type.AutomaticInfluences.Count != 0 )
						UpdateAutomaticInfluences();
				}

				//send to clients
				if( EntitySystemWorld.Instance.IsServer() &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Server_SendHealthToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		//need Shape shape too. But currently logic system not support physics classes.
		public delegate void DamageDelegate( Dynamic entity, MapObject prejudicial,
			Vec3 pos, float damage );
		[LogicSystemBrowsable( true )]
		public event DamageDelegate Damage;

		protected virtual void OnDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			float realDamage = damage * ReceiveDamageCoefficient;

			if( Type.HealthMax != 0 )
			{
				float newHealth = Health - realDamage;
				MathFunctions.Clamp( ref newHealth, Type.HealthMin, Type.HealthMax );
				Health = newHealth;

				if( Health == 0 )
					Die( prejudicial );
			}
			else
			{
				if( allowMoveDamageToParent )
				{
					//damage to parent
					Dynamic dynamic = AttachedMapObjectParent as Dynamic;
					if( dynamic != null )
						dynamic.OnDamage( prejudicial, pos, shape, realDamage, true );
				}
			}

			if( Damage != null )
				Damage( this, prejudicial, pos, realDamage );
		}

		public void DoDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			OnDamage( prejudicial, pos, shape, damage, allowMoveDamageToParent );
		}

		public void Die( MapObject prejudicial, bool allowLatencyTime )
		{
			if( died )
				return;
			if( dieLatencyRamainingTime != 0 )
				return;
			if( allowLatencyTime )
			{
				dieLatencyRamainingTime = Type.DieLatency;
				if( dieLatencyRamainingTime > 0 )
				{
					SubscribeToTickEvent();
					return;
				}
			}

			died = true;

			//create die objects
			if( !dieObjectsCreated )
			{
				DieObjects_Create();
				dieObjectsCreated = true;
			}

			OnDie( prejudicial );

			//networking: send Die call to clients
			if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
				Server_SendDieCallToAllClients( prejudicial, allowLatencyTime );

			SetForDeletion( true );
		}

		public void Die( MapObject prejudicial )
		{
			Die( prejudicial, true );
		}

		[LogicSystemBrowsable( true )]
		public void Die()
		{
			Die( null );
		}

		[Browsable( false )]
		public bool Died
		{
			get { return died; }
		}

		protected virtual void OnCreateInfluence( Influence influence )
		{
			//AutomaticInfluences
			for( int n = 0; n < Type.AutomaticInfluences.Count; n++ )
			{
				DynamicType.AutomaticInfluenceItem typeItem = Type.AutomaticInfluences[ n ];
				if( typeItem.Influence == influence.Type )
				{
					if( automaticInfluences == null )
						automaticInfluences = new Influence[ Type.AutomaticInfluences.Count ];
					automaticInfluences[ n ] = influence;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnLoad(TextBlock)"/>.</summary>
		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			if( EntitySystemWorld.Instance.SerializationMode == SerializationModes.Map ||
				EntitySystemWorld.Instance.SerializationMode == SerializationModes.MapSceneFile )
			{
				Health = Type.HealthMax * HealthFactorAtBeginning;
			}

			return true;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnSave(TextBlock)"/>.</summary>
		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			//World serialization: save animation tree.
			if( EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				AnimationTree tree = GetFirstAnimationTree();
				if( tree != null )
				{
					TextBlock treeBlock = block.AddChild( "animationTree" );
					tree.WorldSave( treeBlock );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnCreate()"/>.</summary>
		protected override void OnCreate()
		{
			base.OnCreate();

			Health = Type.HealthMax;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( PhysicsModel != null )
				{
					if( Type.ImpulseDamageCoefficient != 0 || !string.IsNullOrEmpty( Type.SoundCollision ) )
					{
						foreach( Body body in PhysicsModel.Bodies )
							body.Collision += Body_Collision;
					}
				}
			}

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( Type.AutomaticInfluences.Count != 0 )
					UpdateAutomaticInfluences();
			}

			if( Type.LifeTime != 0 )
				SubscribeToTickEvent();

			//World serialization: load animation tree.
			if( loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				AnimationTree tree = GetFirstAnimationTree();
				if( tree != null )
				{
					TextBlock treeBlock = LoadingTextBlock.FindChild( "animationTree" );
					if( treeBlock != null )
					{
						string error;
						tree.WorldLoad( treeBlock, out error );
					}
				}
			}

			//server side. send positions to clients
			if( EntitySystemWorld.Instance.IsServer() )
			{
				if( Server_EnableSynchronizationPositionsToClients &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					if( AttachedMapObjectParent == null )//no update for attached MapObjects
					{
						if( PhysicsModel != null )
							Server_SendBodiesPositionsToAllClients( true );
						else
							Server_SendPositionsToAllClients( true );
					}
				}
			}

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				if( Type.NetworkType == EntityNetworkTypes.Synchronized )
					Client_UpdatePositionsBySnapshots( false );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( PhysicsModel != null )
				{
					if( Type.ImpulseDamageCoefficient != 0 || !string.IsNullOrEmpty( Type.SoundCollision ) )
					{
						foreach( Body body in PhysicsModel.Bodies )
							body.Collision -= Body_Collision;
					}
				}
			}

			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRemoveChild(Entity)"/></summary>
		protected override void OnRemoveChild( Entity entity )
		{
			base.OnRemoveChild( entity );

			//automaticInfluences
			Influence influence = entity as Influence;
			if( influence != null && automaticInfluences != null )
			{
				for( int n = 0; n < automaticInfluences.Length; n++ )
				{
					if( automaticInfluences[ n ] == influence )
						automaticInfluences[ n ] = null;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( dieLatencyRamainingTime != 0 )
			{
				dieLatencyRamainingTime -= TickDelta;
				if( dieLatencyRamainingTime <= 0 )
				{
					dieLatencyRamainingTime = 0;
					Die( null, false );
				}
			}

			//destroy objects below specified height
			if( GameMap.Instance != null && Position.Z < GameMap.Instance.DestroyObjectsBelowHeight )
				Die();

			if( !Died && soundCollisionTimeRemaining != 0 )
			{
				soundCollisionTimeRemaining -= TickDelta;
				if( soundCollisionTimeRemaining < 0 )
				{
					soundCollisionTimeRemaining = 0;

					if( soundCollisionTimeRemainingTimerAdded )
					{
						UnsubscribeToTickEvent();
						soundCollisionTimeRemainingTimerAdded = false;
					}
				}
			}

			if( !Died && Type.LifeTime != 0 )
			{
				lifeTime += TickDelta;
				if( lifeTime >= Type.LifeTime )
					OnLifeTimeIsOver();
			}
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();
		}

		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );

			//server side
			if( IsPostCreated )
			{
				if( EntitySystemWorld.Instance.IsServer() )
				{
					if( Server_EnableSynchronizationPositionsToClients &&
						Type.NetworkType == EntityNetworkTypes.Synchronized )
					{
						if( AttachedMapObjectParent == null )//no update for attached MapObjects
						{
							if( PhysicsModel != null )
								Server_SendBodiesPositionsToAllClients( false );
							else
								Server_SendPositionsToAllClients( false );
						}
					}
				}
			}
		}

		protected virtual void OnDie( MapObject prejudicial )
		{
			//disable contacts
			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
				{
					foreach( Shape shape in body.Shapes )
						shape.ContactGroup = (int)ContactGroup.NoContact;
				}
			}
		}

		public void AddInfluence( InfluenceType influenceType, float time, bool checkSubstance )
		{
			Trace.Assert( time > 0 );

			if( checkSubstance )
				if( influenceType.AllowSubstance != Substance.None )
					if( ( influenceType.AllowSubstance & Type.Substance ) == 0 )
						return;

			foreach( Entity child in Children )
			{
				if( child.Type != influenceType )
					continue;
				if( child.IsSetForDeletion )
					continue;

				Influence i = (Influence)child;
				i.RemainingTime += time;
				return;
			}

			Influence influence = (Influence)Entities.Instance.Create( influenceType, this );
			influence.RemainingTime = time;
			influence.PostCreate();

			OnCreateInfluence( influence );
		}

		void CopyInfluencesToObject( Dynamic destination )
		{
			foreach( Entity child in Children )
			{
				if( child.IsSetForDeletion )
					continue;

				Influence influence = child as Influence;
				if( influence == null )
					continue;

				Influence i = (Influence)Entities.Instance.Create( influence.Type, destination );
				i.RemainingTime = influence.RemainingTime;
				i.PostCreate();
			}
		}

		void Body_Collision( ref CollisionEvent collisionEvent )
		{
			//Type.SoundCollision
			if( soundCollisionTimeRemaining == 0 && !string.IsNullOrEmpty( Type.SoundCollision ) )
			{
				Body thisBody = collisionEvent.ThisShape.Body;
				Body otherBody = collisionEvent.OtherShape.Body;

				Vec3 velocityDifference = thisBody.LastStepLinearVelocity;
				if( !otherBody.Static )
					velocityDifference -= otherBody.LastStepLinearVelocity;
				else
					velocityDifference -= thisBody.LinearVelocity;

				float minVelocity = Type.SoundCollisionMinVelocity;

				bool allowPlay = velocityDifference.LengthSqr() > minVelocity * minVelocity;

				if( allowPlay )
				{
					SoundPlay3D( Type.SoundCollision, .5f, false );
					soundCollisionTimeRemaining = .25f;

					if( EntitySystemWorld.Instance.IsServer() &&
						Type.NetworkType == EntityNetworkTypes.Synchronized )
					{
						Server_SendSoundPlayCollisionToAllClients();
					}

					if( !soundCollisionTimeRemainingTimerAdded )
					{
						SubscribeToTickEvent();
						soundCollisionTimeRemainingTimerAdded = true;
					}
				}
			}

			//Type.ImpulseDamageCoefficient
			if( Type.ImpulseDamageCoefficient != 0 && Health > Type.HealthMin )
			{
				Body thisBody = collisionEvent.ThisShape.Body;
				Body otherBody = collisionEvent.OtherShape.Body;
				float otherMass = otherBody.Mass;

				float impulse = 0;
				impulse += thisBody.LastStepLinearVelocity.Length() * thisBody.Mass;
				if( otherMass != 0 )
					impulse += otherBody.LastStepLinearVelocity.Length() * otherMass;

				float damage = impulse * Type.ImpulseDamageCoefficient;
				if( damage >= Type.ImpulseMinimalDamage )
					OnDamage( null, collisionEvent.Position, collisionEvent.ThisShape, damage, true );
			}
		}

		public Unit GetParentUnitHavingIntellect()
		{
			MapObject mapObject = this;
			while( mapObject != null )
			{
				Unit unit = mapObject as Unit;
				if( unit != null && unit.Intellect != null )
					return unit;
				mapObject = mapObject.AttachedMapObjectParent;
			}
			return null;
		}

		public Unit GetParentUnit()
		{
			MapObject mapObject = this;
			while( mapObject != null )
			{
				Unit unit = mapObject as Unit;
				if( unit != null )
					return unit;
				mapObject = mapObject.AttachedMapObjectParent;
			}
			return null;
		}

		public void SoundPlay3D( string name, float priority, bool needAttach )
		{
			if( string.IsNullOrEmpty( name ) )
				return;

			if( EngineApp.Instance.DefaultSoundChannelGroup != null &&
				EngineApp.Instance.DefaultSoundChannelGroup.Volume == 0 )
				return;

			string nameFullPath = RelativePathUtils.ConvertToFullPath(
				Path.GetDirectoryName( Type.FilePath ), name );

			//2d sound mode for FPS Camera Player
			PlayerIntellect playerIntellect = PlayerIntellect.Instance;

			if( playerIntellect != null && playerIntellect.FPSCamera &&
				 playerIntellect.ControlledObject != null &&
				 playerIntellect.ControlledObject == GetParentUnit() )
			{
				Sound sound = SoundWorld.Instance.SoundCreate( nameFullPath, 0 );
				if( sound != null )
				{
					SoundWorld.Instance.SoundPlay( sound, EngineApp.Instance.DefaultSoundChannelGroup,
						priority );
				}
				return;
			}

			//Default 3d mode
			{
				if( !needAttach )
				{
					Sound sound = SoundWorld.Instance.SoundCreate( nameFullPath, SoundMode.Mode3D );
					if( sound == null )
						return;

					VirtualChannel channel = SoundWorld.Instance.SoundPlay( sound,
						EngineApp.Instance.DefaultSoundChannelGroup, priority, true );
					if( channel != null )
					{
						channel.Position = Position;
						switch( Type.SoundRolloffMode )
						{
						case DynamicType.SoundRolloffModes.Logarithmic:
							channel.SetLogarithmicRolloff( Type.SoundMinDistance, Type.SoundMaxDistance,
								Type.SoundRolloffLogarithmicFactor );
							break;
						case DynamicType.SoundRolloffModes.Linear:
							channel.SetLinearRolloff( Type.SoundMinDistance, Type.SoundMaxDistance );
							break;
						}
						channel.Pause = false;
					}
				}
				else
				{
					MapObjectAttachedSound attachedSound = new MapObjectAttachedSound();
					attachedSound.RolloffMode = (MapObjectAttachedSound.RolloffModes)Type.SoundRolloffMode;
					attachedSound.MinDistance = Type.SoundMinDistance;
					attachedSound.MaxDistance = Type.SoundMaxDistance;
					attachedSound.RolloffLogarithmicFactor = Type.SoundRolloffLogarithmicFactor;
					attachedSound.SetSoundName( nameFullPath, false );
					Attach( attachedSound );
				}
			}
		}

		/// <summary>
		/// Calls Die().
		/// </summary>
		protected virtual void OnLifeTimeIsOver()
		{
			Die();
		}

		[Browsable( false )]
		public float ReceiveDamageCoefficient
		{
			get { return receiveDamageCoefficient; }
			set { receiveDamageCoefficient = value; }
		}

		void UpdateAutomaticInfluences()
		{
			for( int n = 0; n < Type.AutomaticInfluences.Count; n++ )
			{
				DynamicType.AutomaticInfluenceItem typeItem = Type.AutomaticInfluences[ n ];

				bool need =
					health >= typeItem.LifeCoefficientRange.Minimum * Type.HealthMax &&
					health <= typeItem.LifeCoefficientRange.Maximum * Type.HealthMax;

				if( need )
				{
					if( automaticInfluences == null || automaticInfluences[ n ] == null )
						AddInfluence( typeItem.Influence, 1000000.0f, false );
				}
				else
				{
					if( automaticInfluences != null && automaticInfluences[ n ] != null )
						automaticInfluences[ n ].SetForDeletion( true );
				}
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				if( Type.NetworkType == EntityNetworkTypes.Synchronized )
					Client_UpdatePositionsBySnapshots( true );
			}
		}

		protected virtual void OnSuspendPhysicsDuringMapLoading( bool suspend )
		{
		}

		public void SuspendPhysicsDuringMapLoading( bool suspend )
		{
			OnSuspendPhysicsDuringMapLoading( suspend );
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			//send entity position, rotation and scale
			if( Server_EnableSynchronizationPositionsToClients )
			{
				if( AttachedMapObjectParent == null )//no update for attached MapObjects
				{
					if( PhysicsModel != null )
						Server_SendBodiesPositionsToNewClient( remoteEntityWorld );
					else
						Server_SendPositionsToNewClient( remoteEntityWorld );
				}
			}
		}

		protected override void Server_OnClientConnectedAfterPostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedAfterPostCreate( remoteEntityWorld );

			if( Health != Type.HealthMax )
				Server_SendHealthToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		[Browsable( false )]
		public bool Server_EnableSynchronizationPositionsToClients
		{
			get { return server_enableSynchronizationPositionsToClients; }
			set
			{
				server_enableSynchronizationPositionsToClients = value;

				if( !server_enableSynchronizationPositionsToClients )
				{
					server_sentPositionToClients = new Vec3( float.MaxValue, 0, 0 );
					server_sentRotationToClients = new Quat( 0, 0, 0, 0 );
					server_sentScaleToClients = Vec3.Zero;
					server_sentBodiesPositionsToClients = null;
				}
			}
		}

		void Server_SendPositionsToAllClients( bool updateAll )
		{
			const float positionEpsilon = .005f;
			const float rotationEpsilon = .001f;
			const float scaleEpsilon = .001f;

			bool positionUpdated = updateAll ||
				!Position.Equals( ref server_sentPositionToClients, positionEpsilon );
			bool rotationUpdated = updateAll ||
				!Rotation.Equals( ref server_sentRotationToClients, rotationEpsilon );
			bool scaleUpdated = updateAll ||
				!Scale.Equals( ref server_sentScaleToClients, scaleEpsilon );

			if( positionUpdated || rotationUpdated || scaleUpdated )
			{
				SendDataWriter writer = BeginNetworkMessage( typeof( Dynamic ),
					(ushort)NetworkMessages.PositionsToClient );

				writer.Write( positionUpdated );
				if( positionUpdated )
				{
					writer.Write( Position );
					server_sentPositionToClients = Position;
				}

				writer.Write( rotationUpdated );
				if( rotationUpdated )
				{
					writer.Write( Rotation, 16 );
					server_sentRotationToClients = Rotation;
				}

				writer.Write( scaleUpdated );
				if( scaleUpdated )
				{
					writer.Write( Scale );
					server_sentScaleToClients = Scale;
				}

				EndNetworkMessage();
			}
		}

		void Server_SendPositionsToNewClient( RemoteEntityWorld remoteEntityWorld )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorld, typeof( Dynamic ),
				(ushort)NetworkMessages.PositionsToClient );

			writer.Write( true );
			writer.Write( Position );
			writer.Write( true );
			writer.Write( Rotation, 16 );
			writer.Write( true );
			writer.Write( Scale );

			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.PositionsToClient )]
		void Client_ReceivePositions( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//clear snanshots cache if entity is not created
			if( !IsPostCreated )
				client_receivePositionsSnapshots.Clear();

			//check for invalid snapshot cache
			if( client_receivePositionsSnapshots.Count != 0 )
			{
				Client_ReceivePositionsSnapshot lastSnapshot = client_receivePositionsSnapshots[
					client_receivePositionsSnapshots.Count - 1 ];
				if( lastSnapshot.bodies != null )
				{
					//remove snapshot cache
					client_receivePositionsSnapshots.Clear();
				}
			}

			Client_ReceivePositionsSnapshot snapshot = new Client_ReceivePositionsSnapshot();
			snapshot.networkTickNumber = EntitySystemWorld.Instance.NetworkTickCounter;

			//read position
			if( reader.ReadBoolean() )
			{
				snapshot.position = reader.ReadVec3();
			}
			else
			{
				//get position from previous snapshot
				if( client_receivePositionsSnapshots.Count != 0 )
				{
					snapshot.position = client_receivePositionsSnapshots[
						client_receivePositionsSnapshots.Count - 1 ].position;
				}
			}

			//read rotation
			if( reader.ReadBoolean() )
			{
				snapshot.rotation = reader.ReadQuat( 16 );
			}
			else
			{
				//get rotation from previous snapshot
				if( client_receivePositionsSnapshots.Count != 0 )
				{
					snapshot.rotation = client_receivePositionsSnapshots[
						client_receivePositionsSnapshots.Count - 1 ].rotation;
				}
			}

			//read scale
			if( reader.ReadBoolean() )
			{
				snapshot.scale = reader.ReadVec3();
			}
			else
			{
				//get position from previous snapshot
				if( client_receivePositionsSnapshots.Count != 0 )
				{
					snapshot.scale = client_receivePositionsSnapshots[
						client_receivePositionsSnapshots.Count - 1 ].scale;
				}
			}

			if( !reader.Complete() )
				return;

			client_receivePositionsSnapshots.Add( snapshot );

			Client_UpdatePositionsBySnapshots( false );
		}

		protected void Server_SendBodiesPositionsToAllClients( bool updateAll )
		{
			const float positionEpsilon = .005f;
			const float rotationEpsilon = .001f;

			Body[] bodies = PhysicsModel.Bodies;

			if( server_sentBodiesPositionsToClients == null ||
				server_sentBodiesPositionsToClients.Length != bodies.Length )
			{
				server_sentBodiesPositionsToClients = new Server_SentBodiesPositionsToClientsItem[
					bodies.Length ];

				//reset items
				Server_SentBodiesPositionsToClientsItem item =
					new Server_SentBodiesPositionsToClientsItem();
				item.position = new Vec3( float.MaxValue, 0, 0 );
				for( int n = 0; n < bodies.Length; n++ )
					server_sentBodiesPositionsToClients[ n ] = item;
			}

			bool existsUpdates = false;
			{
				for( int n = 0; n < bodies.Length; n++ )
				{
					Body body = bodies[ n ];

					Server_SentBodiesPositionsToClientsItem item =
						server_sentBodiesPositionsToClients[ n ];

					bool positionUpdated = !body.Position.Equals( ref item.position, positionEpsilon );
					bool rotationUpdated = !body.Rotation.Equals( ref item.rotation, rotationEpsilon );

					if( positionUpdated || rotationUpdated )
					{
						existsUpdates = true;
						break;
					}
				}
			}
			if( updateAll )
				existsUpdates = true;

			if( existsUpdates )
			{
				SendDataWriter writer = BeginNetworkMessage( typeof( Dynamic ),
					(ushort)NetworkMessages.BodiesPositionsToClient );

				//write body count
				writer.WriteVariableUInt32( (uint)bodies.Length );

				//write bodies positions and rotations
				for( int n = 0; n < bodies.Length; n++ )
				{
					Body body = bodies[ n ];

					Server_SentBodiesPositionsToClientsItem item =
						server_sentBodiesPositionsToClients[ n ];

					bool positionUpdated = updateAll ||
						!body.Position.Equals( ref item.position, positionEpsilon );
					bool rotationUpdated = updateAll ||
						!body.Rotation.Equals( ref item.rotation, rotationEpsilon );

					writer.Write( positionUpdated );
					if( positionUpdated )
					{
						writer.Write( body.Position );
						item.position = body.Position;
					}

					writer.Write( rotationUpdated );
					if( rotationUpdated )
					{
						writer.Write( body.Rotation, 16 );
						item.rotation = body.Rotation;
					}

					//update server_sentBodiesPositionsToClients[ n ]
					if( positionUpdated || rotationUpdated )
						server_sentBodiesPositionsToClients[ n ] = item;
				}

				EndNetworkMessage();
			}
		}

		void Server_SendBodiesPositionsToNewClient( RemoteEntityWorld remoteEntityWorld )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorld, typeof( Dynamic ),
				(ushort)NetworkMessages.BodiesPositionsToClient );

			//write body count
			writer.WriteVariableUInt32( (uint)PhysicsModel.Bodies.Length );

			//write bodies positions and rotations
			foreach( Body body in PhysicsModel.Bodies )
			{
				writer.Write( true );
				writer.Write( body.Position );
				writer.Write( true );
				writer.Write( body.Rotation, 16 );
			}

			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.BodiesPositionsToClient )]
		void Client_ReceiveBodiesPositions( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//clear snapshots cache if entity is not created
			if( !IsPostCreated )
				client_receivePositionsSnapshots.Clear();

			//check for invalid snapshot cache
			if( client_receivePositionsSnapshots.Count != 0 )
			{
				Client_ReceivePositionsSnapshot lastSnapshot = client_receivePositionsSnapshots[
					client_receivePositionsSnapshots.Count - 1 ];
				if( lastSnapshot.bodies == null )
				{
					//remove snapshot cache
					client_receivePositionsSnapshots.Clear();
				}
			}

			int count = (int)reader.ReadVariableUInt32();

			Client_ReceivePositionsSnapshot snapshot = new Client_ReceivePositionsSnapshot();
			snapshot.networkTickNumber = EntitySystemWorld.Instance.NetworkTickCounter;

			snapshot.bodies = new Client_ReceivePositionsSnapshot.BodyItem[ count ];

			//receive bodies positions and rotations
			for( int n = 0; n < count; n++ )
			{
				Client_ReceivePositionsSnapshot.BodyItem bodyItem = new
					Client_ReceivePositionsSnapshot.BodyItem();

				//read position
				if( reader.ReadBoolean() )
				{
					bodyItem.position = reader.ReadVec3();
				}
				else
				{
					//get position from previous snapshot
					if( client_receivePositionsSnapshots.Count != 0 )
					{
						bodyItem.position = client_receivePositionsSnapshots[
							client_receivePositionsSnapshots.Count - 1 ].bodies[ n ].position;
					}
				}

				//read rotation
				if( reader.ReadBoolean() )
				{
					bodyItem.rotation = reader.ReadQuat( 16 );
				}
				else
				{
					//get rotation from previous snapshot
					if( client_receivePositionsSnapshots.Count != 0 )
					{
						bodyItem.rotation = client_receivePositionsSnapshots[
							client_receivePositionsSnapshots.Count - 1 ].bodies[ n ].rotation;
					}
				}

				snapshot.bodies[ n ] = bodyItem;
			}

			if( !reader.Complete() )
				return;

			client_receivePositionsSnapshots.Add( snapshot );

			if( IsPostCreated )
				Client_UpdatePositionsBySnapshots( false );
		}

		void Server_SendHealthToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Dynamic ),
				(ushort)NetworkMessages.HealthToClient );
			writer.Write( Health );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.HealthToClient )]
		void Client_ReceiveHealth( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			Health = value;
		}

		void Server_SendSoundPlayCollisionToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Dynamic ),
				(ushort)NetworkMessages.SoundPlayCollisionToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.SoundPlayCollisionToClient )]
		void Client_ReceiveSoundPlayCollision( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			SoundPlay3D( Type.SoundCollision, .5f, false );
		}

		void Client_GetReceivePositionsSnapshots( int networkTickNumber,
			out Client_ReceivePositionsSnapshot snapshot1,
			out Client_ReceivePositionsSnapshot snapshot2 )
		{
			for( int n = client_receivePositionsSnapshots.Count - 1; n >= 0; n-- )
			{
				Client_ReceivePositionsSnapshot snapshot = client_receivePositionsSnapshots[ n ];

				if( networkTickNumber >= snapshot.networkTickNumber )
				{
					snapshot1 = snapshot;
					snapshot2 = null;
					if( n + 1 < client_receivePositionsSnapshots.Count )
					{
						Client_ReceivePositionsSnapshot nextSnapshot =
							client_receivePositionsSnapshots[ n + 1 ];
						if( networkTickNumber + 1 == nextSnapshot.networkTickNumber )
							snapshot2 = nextSnapshot;
					}
					return;
				}
			}

			snapshot1 = client_receivePositionsSnapshots[ 0 ];
			snapshot2 = null;
		}

		public void Client_UpdatePositionsBySnapshots( bool rendering )
		{
			if( client_receivePositionsSnapshots.Count == 0 )
				return;

			float time;
			{
				float clientTime = EntitySystemWorld.Instance.Client_TickTime;

				if( rendering )
				{
					float timeFromLastTick = RendererWorld.Instance.FrameRenderTime -
						EntitySystemWorld.Instance.Client_TimeWhenTickTimeWasUpdated;
					clientTime += timeFromLastTick;
				}

				//interpolationTime should be greater than sending interval (currently TickDelta)
				float interpolationTime = TickDelta * 1.5f;

				time = clientTime - interpolationTime;
			}

			int networkTickNumber = (int)( (float)time * EntitySystemWorld.Instance.GameFPS );// time / TickDelta 

			//remove not necessary snapshots
			{
				again:
				if( client_receivePositionsSnapshots.Count > 1 )
				{
					if( client_receivePositionsSnapshots[ 1 ].networkTickNumber <= networkTickNumber )
					{
						client_receivePositionsSnapshots.RemoveAt( 0 );
						goto again;
					}
				}
			}

			Client_ReceivePositionsSnapshot snapshot1;
			Client_ReceivePositionsSnapshot snapshot2;
			if( rendering )
			{
				Client_GetReceivePositionsSnapshots( networkTickNumber, out snapshot1, out snapshot2 );
			}
			else
			{
				//use last snapshot
				snapshot1 = client_receivePositionsSnapshots[
					client_receivePositionsSnapshots.Count - 1 ];
				snapshot2 = null;
			}

			if( snapshot2 != null )
			{
				//with interpolation

				float timeCoef;
				{
					float time1 = ( (float)networkTickNumber ) * TickDelta;
					//float time2 = time1 + TickDelta;
					float timeDiff = TickDelta;//time2 - time1
					timeCoef = 1.0f - ( time - time1 ) / timeDiff;
					MathFunctions.Saturate( ref timeCoef );
				}

				if( PhysicsModel != null )
				{
					for( int n = 0; n < PhysicsModel.Bodies.Length; n++ )
					{
						Body body = PhysicsModel.Bodies[ n ];

						if( snapshot1.bodies == null || n >= snapshot1.bodies.Length )
							continue;
						if( snapshot2.bodies == null || n >= snapshot2.bodies.Length )
							continue;

						Client_ReceivePositionsSnapshot.BodyItem bodyItem1 = snapshot1.bodies[ n ];
						Client_ReceivePositionsSnapshot.BodyItem bodyItem2 = snapshot2.bodies[ n ];

						Vec3 pos;
						Quat rot;
						Vec3.Lerp( ref bodyItem2.position, ref bodyItem1.position, timeCoef, out pos );
						Quat.Slerp( ref bodyItem2.rotation, ref bodyItem1.rotation, timeCoef, out rot );

						body.SetTransform( pos, rot );
						body.SetOldTransform( pos, rot );
					}

					UpdatePositionAndRotationByPhysics( true );
					OldPosition = Position;
					OldRotation = Rotation;
				}
				else
				{
					Vec3 pos;
					Quat rot;
					Vec3 scl;
					Vec3.Lerp( ref snapshot2.position, ref snapshot1.position, timeCoef, out pos );
					Quat.Slerp( ref snapshot2.rotation, ref snapshot1.rotation, timeCoef, out rot );
					Vec3.Lerp( ref snapshot2.scale, ref snapshot1.scale, timeCoef, out scl );

					SetTransform( pos, rot, scl );
					SetOldTransform( pos, rot, scl );
				}
			}
			else
			{
				//without interpolation

				if( PhysicsModel != null )
				{
					for( int n = 0; n < PhysicsModel.Bodies.Length; n++ )
					{
						Body body = PhysicsModel.Bodies[ n ];

						if( snapshot1.bodies == null || n >= snapshot1.bodies.Length )
							continue;

						Client_ReceivePositionsSnapshot.BodyItem bodyItem = snapshot1.bodies[ n ];
						body.SetTransform( bodyItem.position, bodyItem.rotation );
						body.SetOldTransform( bodyItem.position, bodyItem.rotation );
					}

					UpdatePositionAndRotationByPhysics( true );
					OldPosition = Position;
					OldRotation = Rotation;
				}
				else
				{
					SetTransform( snapshot1.position, snapshot1.rotation, snapshot1.scale );
					SetOldTransform( snapshot1.position, snapshot1.rotation, snapshot1.scale );
				}
			}
		}

		public AnimationTree GetFirstAnimationTree()
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
				if( attachedMesh != null )
				{
					if( attachedMesh.AnimationTree != null )
						return attachedMesh.AnimationTree;
				}
			}
			return null;
		}

		public AnimationTree[] GetAllAnimationTrees()
		{
			List<AnimationTree> list = new List<AnimationTree>();
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
				if( attachedMesh != null )
				{
					if( attachedMesh.AnimationTree != null )
						list.Add( attachedMesh.AnimationTree );
				}
			}
			return list.ToArray();
		}

		public virtual MapObjectCreateObjectCollection.CreateObjectsResultItem[] DieObjects_Create()
		{
			//create objects
			MapObjectCreateObjectCollection.CreateObjectsResultItem[] result =
				Type.DieObjects.CreateObjectsOfOneRandomSelectedGroup( this );

			//modify created objects:
			//- Random rotate objects with Alias = "randomRotation"
			//- Apply CopyVelocitiesFromParent property
			foreach( MapObjectCreateObjectCollection.CreateObjectsResultItem item in result )
			{
				MapObjectCreateMapObject createMapObject = item.Source as MapObjectCreateMapObject;
				if( createMapObject != null )
				{
					foreach( MapObject mapObject in item.CreatedObjects )
					{
						//Copy information to dead object
						if( createMapObject.CopyVelocitiesFromParent )
						{
							Dynamic dynamic = mapObject as Dynamic;
							if( dynamic != null )
								CopyInfluencesToObject( dynamic );
						}

						//random rotation
						if( createMapObject.Alias == "randomRotation" )
						{
							Bullet bullet = mapObject as Bullet;
							if( bullet != null )
							{
								bullet.Rotation = new Angles(
									World.Instance.Random.NextFloat() * 180.0f,
									World.Instance.Random.NextFloat() * 180.0f,
									World.Instance.Random.NextFloat() * 180.0f ).ToQuat();
								bullet.Velocity = bullet.Rotation.GetForward() * bullet.Type.Velocity;
							}
						}
					}
				}
			}

			return result;
		}

		void Server_SendDieCallToAllClients( MapObject prejudicial, bool allowLatencyTime )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Dynamic ), (ushort)NetworkMessages.DieCallToClient );
			writer.WriteVariableUInt32( prejudicial != null ? prejudicial.NetworkUIN : 0 );
			writer.Write( allowLatencyTime );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.DieCallToClient )]
		void Client_ReceiveDieCall( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			MapObject prejudicial = Entities.Instance.GetByNetworkUIN( reader.ReadVariableUInt32() ) as MapObject;
			bool allowLatencyTime = reader.ReadBoolean();
			if( !reader.Complete() )
				return;
			Die( prejudicial, allowLatencyTime );
		}
	}
}
