// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.FileSystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Gun"/> entity type.
	/// </summary>
	public class GunType : WeaponType
	{
		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class GunMode : WeaponMode
		{
			internal GunType owner;

			[FieldSerialize]
			BulletType bulletType;

			[FieldSerialize]
			[DefaultValue( 0 )]
			int bulletCapacity;

			[FieldSerialize]
			[DefaultValue( 0 )]
			int magazineCapacity;

			[FieldSerialize]
			[DefaultValue( 1 )]
			int bulletExpense = 1;

			[FieldSerialize]
			[DefaultValue( 1 )]
			int fireCount = 1;

			[FieldSerialize]
			[DefaultValue( typeof( Degree ), "0" )]
			Degree dispersionAngle;

			MapObjectCreateObjectCollection fireObjects = new MapObjectCreateObjectCollection();

			//

			public BulletType BulletType
			{
				get { return bulletType; }
				set { bulletType = value; }
			}

			[DefaultValue( 0 )]
			public int BulletCapacity
			{
				get { return bulletCapacity; }
				set { bulletCapacity = value; }
			}

			[DefaultValue( 0 )]
			public int MagazineCapacity
			{
				get { return magazineCapacity; }
				set { magazineCapacity = value; }
			}

			[DefaultValue( 1 )]
			public int BulletExpense
			{
				get { return bulletExpense; }
				set { bulletExpense = value; }
			}

			[DefaultValue( 1 )]
			public int FireCount
			{
				get { return fireCount; }
				set { fireCount = value; }
			}

			[DefaultValue( typeof( Degree ), "0" )]
			public Degree DispersionAngle
			{
				get { return dispersionAngle; }
				set { dispersionAngle = value; }
			}

			public override string ToString()
			{
				if( bulletType != null )
					return bulletType.ToString();
				else
					return "(not initialized)";
			}

			[Browsable( false )]
			public override bool IsInitialized
			{
				get { return bulletType != null; }
			}

			/// <summary>
			/// Gets the fire objects collection.
			/// </summary>
			/// <remarks>
			/// <para>
			/// These objects will be created after fire.
			/// </para>
			/// </remarks>
			[Description( "The fire objects collection. These objects will be created after fire." )]
			public MapObjectCreateObjectCollection FireObjects
			{
				get { return fireObjects; }
			}

			public bool Load( TextBlock block )
			{
				//fireObjects
				TextBlock fireObjectsBlock = block.FindChild( "fireObjects" );
				if( fireObjectsBlock != null )
				{
					if( !fireObjects.Load( fireObjectsBlock ) )
						return false;
				}

				return true;
			}

			public bool Save( TextBlock block )
			{
				//fireObjects
				if( fireObjects.Count != 0 )
				{
					TextBlock fireObjectsBlock = block.AddChild( "fireObjects" );
					if( !fireObjects.Save( fireObjectsBlock ) )
						return false;
				}

				return true;
			}
		}

		[FieldSerialize]
		GunMode normalMode = new GunMode();
		[FieldSerialize]
		GunMode alternativeMode = new GunMode();

		[FieldSerialize]
		float reloadTime;

		[FieldSerialize]
		string soundReload;
		[FieldSerialize]
		string soundEmpty;

		[FieldSerialize]
		string reloadAnimationTrigger = "reload";

		//

		public GunType()
		{
			weaponNormalMode = normalMode;
			weaponAlternativeMode = alternativeMode;
			normalMode.owner = this;
			alternativeMode.owner = this;
		}

		public GunMode NormalMode
		{
			get { return normalMode; }
		}

		public GunMode AlternativeMode
		{
			get { return alternativeMode; }
		}

		[DefaultValue( 0.0f )]
		public float ReloadTime
		{
			get { return reloadTime; }
			set { reloadTime = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundReload
		{
			get { return soundReload; }
			set { soundReload = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundEmpty
		{
			get { return soundEmpty; }
			set { soundEmpty = value; }
		}

		[DefaultValue( "reload" )]
		public string ReloadAnimationTrigger
		{
			get { return reloadAnimationTrigger; }
			set { reloadAnimationTrigger = value; }
		}

		protected override bool OnLoad( TextBlock block )
		{
			TextBlock normalModeBlock = block.FindChild( "normalMode" );
			if( normalModeBlock != null )
				if( !normalMode.Load( normalModeBlock ) )
					return false;

			TextBlock alternativeModeBlock = block.FindChild( "alternativeMode" );
			if( alternativeModeBlock != null )
				if( !alternativeMode.Load( alternativeModeBlock ) )
					return false;

			return base.OnLoad( block );
		}

		protected override bool OnSave( TextBlock block )
		{
			TextBlock normalModeBlock = block.FindChild( "normalMode" );
			if( normalModeBlock == null )
				Log.Fatal( "{0} : normalMode Block not exists", Name );
			if( !normalMode.Save( normalModeBlock ) )
				return false;

			TextBlock alternativeModeBlock = block.FindChild( "alternativeMode" );
			if( alternativeModeBlock == null )
				Log.Fatal( "{0} : alternativeMode Block not exists", Name );
			if( !alternativeMode.Save( alternativeModeBlock ) )
				return false;

			return base.OnSave( block );
		}

		protected override bool OnIsExistsReferenceToObject( object obj )
		{
			if( NormalMode.FireObjects.IsExistsReferenceToObject( obj ) )
				return true;
			if( AlternativeMode.FireObjects.IsExistsReferenceToObject( obj ) )
				return true;
			return base.OnIsExistsReferenceToObject( obj );
		}

		protected override void OnChangeReferencesToObject( object obj, object newValue )
		{
			base.OnChangeReferencesToObject( obj, newValue );
			NormalMode.FireObjects.ChangeReferencesToObject( obj, newValue );
			AlternativeMode.FireObjects.ChangeReferencesToObject( obj, newValue );
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//it is not known how will be used this sound (2D or 3D?).
			//Sound will preloaded as 3D only here.
			PreloadSound( SoundReload, SoundMode.Mode3D );
			PreloadSound( SoundEmpty, SoundMode.Mode3D );
		}

	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class Gun : Weapon
	{
		[FieldSerialize]
		Mode normalMode = new Mode();
		[FieldSerialize]
		Mode alternativeMode = new Mode();

		[FieldSerialize]
		bool needReload;

		[FieldSerialize]
		float readyTimeRemaining;

		//for FireTimes
		[FieldSerialize]
		float currentFireTime;
		//serialized in OnLoad/OnSave
		Mode currentFireMode;
		[FieldSerialize]
		int fireTimesExecuted;

		bool server_shouldSendBulletCountToClients;

		///////////////////////////////////////////

		public class Mode
		{
			internal Gun owner;
			internal GunType.GunMode typeMode;

			[FieldSerialize]
			int bulletCount;
			[FieldSerialize]
			int bulletMagazineCount;

			//

			public int BulletCount
			{
				get { return bulletCount; }
				set
				{
					if( bulletCount == value )
						return;

					bulletCount = value;
					if( EntitySystemWorld.Instance.IsServer() )
						owner.server_shouldSendBulletCountToClients = true;
				}
			}

			public int BulletMagazineCount
			{
				get { return bulletMagazineCount; }
				set
				{
					if( bulletMagazineCount == value )
						return;

					bulletMagazineCount = value;
					if( EntitySystemWorld.Instance.IsServer() )
						owner.server_shouldSendBulletCountToClients = true;
				}
			}

			public override string ToString()
			{
				if( typeMode.BulletType == null )
					return "(not initialized)";
				string text = string.Format( "{0} ({1})", bulletCount.ToString(),
					typeMode.BulletCapacity.ToString() );
				if( typeMode.MagazineCapacity != 0 )
					text += string.Format( ", {0} ({1})", BulletMagazineCount.ToString(),
						typeMode.MagazineCapacity.ToString() );
				return text;
			}
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			UpdateBulletCountToClient,
			CreateBulletEventToClient,
			ReloadEventToClient,
			NoAmmoEventToClient,
			FireEventToClient,
		}

		///////////////////////////////////////////

		GunType _type = null; public new GunType Type { get { return _type; } }

		protected override void OnPreCreate()
		{
			base.OnPreCreate();

			normalMode.owner = this;
			normalMode.typeMode = Type.NormalMode;
			alternativeMode.owner = this;
			alternativeMode.typeMode = Type.AlternativeMode;
		}

		[Browsable( false )]
		public Mode NormalMode
		{
			get { return normalMode; }
		}

		[Browsable( false )]
		public Mode AlternativeMode
		{
			get { return alternativeMode; }
		}

		[Browsable( false )]
		public override bool Ready
		{
			get { return readyTimeRemaining == 0; }
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			if( block.IsAttributeExist( "currentFireMode" ) )
			{
				if( block.GetAttribute( "currentFireMode" ) == "normal" )
					currentFireMode = normalMode;
				else
					currentFireMode = alternativeMode;
			}

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( currentFireMode != null )
			{
				if( currentFireMode == normalMode )
					block.SetAttribute( "currentFireMode", "normal" );
				else
					block.SetAttribute( "currentFireMode", "alternative" );
			}
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

			//fireTimes
			if( currentFireMode != null )
			{
				currentFireTime += TickDelta;
				int fireTimesCount = currentFireMode.typeMode.FireTimes.Count;
				if( fireTimesExecuted < fireTimesCount )
				{
					again:
					if( fireTimesExecuted < fireTimesCount )
					{
						float nextTime = currentFireMode.typeMode.FireTimes[ fireTimesExecuted ];
						if( currentFireTime >= nextTime )
						{
							fireTimesExecuted++;
							for( int n = 0; n < currentFireMode.typeMode.FireCount; n++ )
								CreateBullet( currentFireMode );

							SoundPlay3D( currentFireMode.typeMode.SoundFire, .5f, true );
							currentFireMode.typeMode.FireObjects.CreateObjectsOfOneRandomSelectedGroup( this );

							if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
								Server_SendCreateBulletEventToAllClients( currentFireMode );

							goto again;
						}
					}
				}
				else
					currentFireMode = null;
			}

			if( readyTimeRemaining != 0 )
			{
				float coef = 1.0f;

				Unit unit = GetParentUnitHavingIntellect();
				if( unit != null && unit.FastAttackInfluence != null )
					coef *= unit.FastAttackInfluence.Type.Coefficient;

				readyTimeRemaining -= TickDelta * coef;
				if( readyTimeRemaining < 0 )
					readyTimeRemaining = 0;
			}

			if( needReload && readyTimeRemaining == 0 )
			{
				for( int nMode = 0; nMode < 2; nMode++ )
				{
					Mode mode = nMode == 0 ? NormalMode : AlternativeMode;

					if( mode.typeMode.MagazineCapacity != 0 && mode.BulletCount != 0 )
					{
						if( mode.BulletMagazineCount != mode.typeMode.MagazineCapacity &&
							mode.BulletMagazineCount < mode.BulletCount )
						{
							mode.BulletMagazineCount = Math.Min(
								mode.typeMode.MagazineCapacity, mode.BulletCount );

							readyTimeRemaining = Type.ReloadTime;

							OnReload();

							if( EntitySystemWorld.Instance.IsServer() &&
								Type.NetworkType == EntityNetworkTypes.Synchronized )
							{
								Server_SendReloadEventToAllClients();
							}
						}
					}
				}

				needReload = false;
			}

			//server. send bullet count to clients
			if( EntitySystemWorld.Instance.IsServer() &&
				Type.NetworkType == EntityNetworkTypes.Synchronized )
			{
				if( server_shouldSendBulletCountToClients )
				{
					Server_SendUpdateBulletCountToClients(
						EntitySystemWorld.Instance.RemoteEntityWorlds );
					server_shouldSendBulletCountToClients = false;
				}
			}
		}

		void OnReload()
		{
			//sound
			SoundPlay3D( Type.SoundReload, .5f, true );

			//update animation tree
			if( !string.IsNullOrEmpty( Type.ReloadAnimationTrigger ) )
			{
				foreach( AnimationTree tree in GetAllAnimationTrees() )
					tree.ActivateTrigger( Type.ReloadAnimationTrigger );

				Unit parentUnit = GetParentUnit();
				if( parentUnit != null )
				{
					foreach( AnimationTree parentTree in parentUnit.GetAllAnimationTrees() )
						parentTree.ActivateTrigger( Type.ReloadAnimationTrigger );
				}
			}
		}

		public bool GetAdvanceAttackTargetPosition( bool alternative, MapObject obj, bool useGravity,
			out Vec3 pos )
		{
			Mode mode = alternative ? alternativeMode : normalMode;

			if( mode.typeMode.BulletType == null )
				Log.Fatal( "Gun: GetAdvanceAttackTargetPosition: BulletType = null" );

			PhysicsModel objPhysicsModel = obj.PhysicsModel;
			if( objPhysicsModel == null )
			{
				pos = obj.Position;
				return true;
			}

			Vec3 objPos = obj.Position;

			float bulletVelocity = mode.typeMode.BulletType.Velocity;
			if( bulletVelocity == 0 )
			{
				pos = objPos;
				return true;
			}

			Vec3 diff = objPos - Position;
			float len = diff.Length();

			float flyTime;

			if( useGravity && mode.typeMode.BulletType.Gravity != 0 )
			{
				float sh = diff.ToVec2().Length();
				float angle = mode.typeMode.BulletType.CalculateDemandedVerticalAngleToHitTarget( sh, diff.Z );

				if( angle != -1 )
				{
					float vh = bulletVelocity * MathFunctions.Cos( angle );
					flyTime = sh / vh;
				}
				else
				{
					pos = objPos;
					return false;
				}
			}
			else
				flyTime = len / bulletVelocity;

			Vec3 objVelocity = objPhysicsModel.Bodies[ 0 ].LinearVelocity;
			pos = objPos + objVelocity * flyTime;
			return true;
		}

		public override bool TryFire( bool alternative )
		{
			if( !Ready )
				return false;

			Mode mode = alternative ? alternativeMode : normalMode;

			if( mode.typeMode.BulletType == null )
				return false;

			bool permit = mode.BulletCount >= mode.typeMode.BulletExpense;

			if( mode.typeMode.MagazineCapacity != 0 )
				if( mode.BulletMagazineCount < mode.typeMode.BulletExpense )
					permit = false;

			if( !permit )
			{
				//no ammo
				SoundPlay3D( Type.SoundEmpty, .5f, true );
				readyTimeRemaining = mode.typeMode.BetweenFireTime;
				TryReload();

				if( EntitySystemWorld.Instance.IsServer() &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Server_SendNoAmmoEventToAllClients();
				}

				return false;
			}
			Fire( mode );
			return true;
		}

		public void TryReload()
		{
			if( Type.NormalMode.MagazineCapacity != 0 && normalMode.BulletCount != 0 )
			{
				if( normalMode.BulletMagazineCount != Type.NormalMode.MagazineCapacity
					&& normalMode.BulletMagazineCount < normalMode.BulletCount )
				{
					needReload = true;
					return;
				}
			}
			if( Type.AlternativeMode.MagazineCapacity != 0 && alternativeMode.BulletCount != 0 )
			{
				if( alternativeMode.BulletMagazineCount != Type.AlternativeMode.MagazineCapacity
					&& alternativeMode.BulletMagazineCount < alternativeMode.BulletCount )
				{
					needReload = true;
					return;
				}
			}
		}

		protected virtual void Fire( Mode mode )
		{
			bool alternative = mode == AlternativeMode;

			DoPreFireEvent( alternative );

			if( Type.NormalMode.BulletType == Type.AlternativeMode.BulletType )
			{
				if( normalMode.typeMode.MagazineCapacity != 0 )
				{
					normalMode.BulletMagazineCount -= normalMode.typeMode.BulletExpense;
					if( normalMode.BulletMagazineCount < 0 )
						Log.Fatal( "Gun: NormalMode: BulletMagazineCount < 0" );
				}
				normalMode.BulletCount -= normalMode.typeMode.BulletExpense;
				if( normalMode.BulletCount < 0 )
					Log.Fatal( "Gun: NormalMode: BulletCount < 0" );

				if( alternativeMode.typeMode.MagazineCapacity != 0 )
				{
					alternativeMode.BulletMagazineCount -= alternativeMode.typeMode.BulletExpense;
					if( normalMode.BulletMagazineCount < 0 )
						Log.Fatal( "Gun: AlternativeMode: BulletMagazineCount < 0" );
				}
				alternativeMode.BulletCount -= alternativeMode.typeMode.BulletExpense;
				if( normalMode.BulletCount < 0 )
					Log.Fatal( "Gun: AlternativeMode: BulletCount < 0" );
			}
			else
			{
				if( mode.typeMode.MagazineCapacity != 0 )
				{
					mode.BulletMagazineCount -= mode.typeMode.BulletExpense;
					if( mode.BulletMagazineCount < 0 )
						Log.Fatal( "Gun: {0}Mode: BulletMagazineCount < 0", mode == normalMode ?
							"Normal" : "Alternative" );
				}
				mode.BulletCount -= mode.typeMode.BulletExpense;
				if( mode.BulletCount < 0 )
					Log.Fatal( "Gun: {0}Mode: BulletCount < 0", mode == normalMode ?
						"Normal" : "Alternative" );
			}

			readyTimeRemaining = mode.typeMode.BetweenFireTime;
			currentFireMode = mode;
			currentFireTime = 0;
			fireTimesExecuted = 0;

			//Create fireObjects
			if( mode.typeMode.FireTimes.Count == 0 )
			{
				for( int n = 0; n < mode.typeMode.FireCount; n++ )
					CreateBullet( mode );

				SoundPlay3D( mode.typeMode.SoundFire, .5f, true );
				mode.typeMode.FireObjects.CreateObjectsOfOneRandomSelectedGroup( this );

				if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
					Server_SendCreateBulletEventToAllClients( mode );
			}

			OnFire( mode.typeMode );

			if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
				Server_SendFireEventToAllClients( alternative );
		}

		void OnFire( GunType.GunMode typeMode )
		{
			//update animation tree
			if( !string.IsNullOrEmpty( typeMode.FireAnimationTrigger ) )
			{
				foreach( AnimationTree tree in GetAllAnimationTrees() )
					tree.ActivateTrigger( typeMode.FireAnimationTrigger );

				Unit parentUnit = GetParentUnit();
				if( parentUnit != null )
				{
					foreach( AnimationTree parentTree in parentUnit.GetAllAnimationTrees() )
						parentTree.ActivateTrigger( typeMode.FireAnimationTrigger );
				}
			}
		}

		protected virtual void CreateBullet( Mode mode )
		{
			Bullet obj = (Bullet)Entities.Instance.Create( mode.typeMode.BulletType, Parent );
			obj.SourceUnit = GetParentUnitHavingIntellect();
			obj.Position = GetFirePosition( mode.typeMode );

			//Correcting position at a shot in very near object (when the point of a shot inside object).
			{
				Vec3 startPos = Position;
				if( AttachedMapObjectParent != null )
					startPos = AttachedMapObjectParent.Position;

				Ray ray = new Ray( startPos, obj.Position - startPos );
				if( ray.Direction != Vec3.Zero )
				{
					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
						ray, (int)ContactGroup.CastOnlyContact );

					foreach( RayCastResult result in piercingResult )
					{
						MapObject mapObject = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

						if( mapObject != null )
						{
							if( mapObject == this )
								continue;
							if( mapObject == this.AttachedMapObjectParent )
								continue;
						}

						obj.Position = result.Position - ray.Direction * .01f;
						break;
					}
				}
			}

			Quat rot = GetFireRotation( mode.typeMode );
			Radian dispersionAngle = mode.typeMode.DispersionAngle;
			if( dispersionAngle != 0 )
			{
				EngineRandom random = World.Instance.Random;

				Mat3 matrix;
				matrix = Mat3.FromRotateByX( random.NextFloat() * MathFunctions.PI * 2 );
				matrix *= Mat3.FromRotateByZ( random.NextFloat() * dispersionAngle );

				rot *= matrix.ToQuat();
			}
			obj.Rotation = rot;

			obj.PostCreate();

			//set damage coefficient
			float coef = obj.DamageCoefficient;
			Unit unit = GetParentUnitHavingIntellect();
			if( unit != null && unit.BigDamageInfluence != null )
				coef *= unit.BigDamageInfluence.Type.Coefficient;
			obj.DamageCoefficient = coef;
		}

		public void AddBullets( BulletType bulletType, int count )
		{
			if( Type.NormalMode.BulletType == bulletType )
			{
				bool reload = normalMode.BulletCount == 0;

				normalMode.BulletCount += count;
				if( normalMode.BulletCount > Type.NormalMode.BulletCapacity )
					normalMode.BulletCount = Type.NormalMode.BulletCapacity;

				if( reload )
					TryReload();
			}
			if( Type.AlternativeMode.BulletType == bulletType )
			{
				alternativeMode.BulletCount += count;
				if( alternativeMode.BulletCount > Type.AlternativeMode.BulletCapacity )
					alternativeMode.BulletCount = Type.AlternativeMode.BulletCapacity;
			}
		}

		public override Quat GetFireRotation( bool alternative )
		{
			Mode mode = alternative ? alternativeMode : normalMode;
			return GetFireRotation( mode.typeMode );
		}

		public override Vec3 GetFirePosition( bool alternative )
		{
			Mode mode = alternative ? alternativeMode : normalMode;
			return GetFirePosition( mode.typeMode );
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendUpdateBulletCountToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendUpdateBulletCountToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Gun ),
				(ushort)NetworkMessages.UpdateBulletCountToClient );
			writer.WriteVariableInt32( normalMode.BulletCount );
			writer.WriteVariableInt32( normalMode.BulletMagazineCount );
			writer.WriteVariableInt32( alternativeMode.BulletCount );
			writer.WriteVariableInt32( alternativeMode.BulletMagazineCount );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.UpdateBulletCountToClient )]
		void Client_ReceiveUpdateBulletCount( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			normalMode.BulletCount = reader.ReadVariableInt32();
			normalMode.BulletMagazineCount = reader.ReadVariableInt32();
			alternativeMode.BulletCount = reader.ReadVariableInt32();
			alternativeMode.BulletMagazineCount = reader.ReadVariableInt32();
		}

		void Server_SendCreateBulletEventToAllClients( Mode mode )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Gun ),
				(ushort)NetworkMessages.CreateBulletEventToClient );
			writer.Write( mode == alternativeMode );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.CreateBulletEventToClient )]
		void Client_ReceiveCreateBulletEvent( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool alternativeModeFlag = reader.ReadBoolean();

			if( !reader.Complete() )
				return;

			Mode mode = alternativeModeFlag ? alternativeMode : normalMode;

			SoundPlay3D( mode.typeMode.SoundFire, .5f, true );
			mode.typeMode.FireObjects.CreateObjectsOfOneRandomSelectedGroup( this );
		}

		void Server_SendReloadEventToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Gun ),
				(ushort)NetworkMessages.ReloadEventToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ReloadEventToClient )]
		void Client_ReceiveReloadEvent( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			OnReload();
		}

		void Server_SendNoAmmoEventToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Gun ),
				(ushort)NetworkMessages.NoAmmoEventToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.NoAmmoEventToClient )]
		void Client_ReceiveNoAmmoEvent( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;

			SoundPlay3D( Type.SoundEmpty, .5f, true );
		}

		void Server_SendFireEventToAllClients( bool alternative )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Gun ),
				(ushort)NetworkMessages.FireEventToClient );
			writer.Write( alternative );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.FireEventToClient )]
		void Client_ReceiveFireEvent( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool alternative = reader.ReadBoolean();
			if( !reader.Complete() )
				return;

			GunType.GunMode typeMode = alternative ? Type.AlternativeMode : Type.NormalMode;
			OnFire( typeMode );
		}

	}
}
