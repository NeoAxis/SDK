// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="PlayerCharacter"/> entity type.
	/// </summary>
	public class PlayerCharacterType : GameCharacterType
	{
		[FieldSerialize]
		Vec3 notAnimatedWeaponAttachPosition;

		[FieldSerialize]
		List<WeaponItem> weapons = new List<WeaponItem>();

		///////////////////////////////////////////

		public class WeaponItem
		{
			[FieldSerialize]
			WeaponType weaponType;

			public WeaponType WeaponType
			{
				get { return weaponType; }
				set { weaponType = value; }
			}

			public override string ToString()
			{
				if( weaponType == null )
					return "(not initialized)";
				return weaponType.Name;
			}
		}

		///////////////////////////////////////////

		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public Vec3 NotAnimatedWeaponAttachPosition
		{
			get { return notAnimatedWeaponAttachPosition; }
			set { notAnimatedWeaponAttachPosition = value; }
		}

		public List<WeaponItem> Weapons
		{
			get { return weapons; }
		}
	}

	public class PlayerCharacter : GameCharacter
	{
		//not synchronized via network
		[FieldSerialize]
		List<WeaponItem> weapons = new List<WeaponItem>();

		[FieldSerialize]
		Weapon activeWeapon;
		MapObjectAttachedMapObject activeWeaponAttachedObject;

		[FieldSerialize]
		float contusionTimeRemaining;

		///////////////////////////////////////////

		public class WeaponItem
		{
			[FieldSerialize]
			internal bool exists;

			[FieldSerialize]
			internal int normalBulletCount;
			[FieldSerialize]
			internal int normalMagazineCount;

			[FieldSerialize]
			internal int alternativeBulletCount;
			[FieldSerialize]
			internal int alternativeMagazineCount;

			public bool Exists
			{
				get { return exists; }
			}

			public int NormalBulletCount
			{
				get { return normalBulletCount; }
			}

			public int NormalMagazineCount
			{
				get { return normalMagazineCount; }
			}

			public int AlternativeBulletCount
			{
				get { return alternativeBulletCount; }
			}

			public int AlternativeMagazineCount
			{
				get { return alternativeMagazineCount; }
			}
		}

		///////////////////////////////////////////

		public class ChangeMapInformation
		{
			public Vec3 position;
			public SphereDir lookDirection;
			public Vec3 velocity;

			public float health;

			public List<WeaponItem> weapons;
			public int activeWeaponIndex;
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			ActiveWeaponToClient,
			ContusionTimeRemainingToClient,
		}

		///////////////////////////////////////////

		PlayerCharacterType _type = null; public new PlayerCharacterType Type { get { return _type; } }

		[Browsable( false )]
		public List<WeaponItem> Weapons
		{
			get { return weapons; }
			set { weapons = value; }
		}

		public int GetWeaponIndex( WeaponType weaponType )
		{
			for( int n = 0; n < Type.Weapons.Count; n++ )
				if( Type.Weapons[ n ].WeaponType == weaponType )
					return n;
			return -1;
		}

		public bool TakeWeapon( WeaponType weaponType )
		{
			int index = GetWeaponIndex( weaponType );
			if( index == -1 )
				return false;

			if( weapons[ index ].exists )
				return true;

			weapons[ index ].exists = true;

			SetActiveWeapon( index );

			return true;
		}

		public bool TakeBullets( BulletType bulletType, int count )
		{
			bool taked = false;

			for( int n = 0; n < weapons.Count; n++ )
			{
				GunType gunType = Type.Weapons[ n ].WeaponType as GunType;
				if( gunType == null )
					continue;

				if( gunType.NormalMode.BulletType == bulletType )
				{
					if( weapons[ n ].normalBulletCount < gunType.NormalMode.BulletCapacity )
					{
						taked = true;
						weapons[ n ].normalBulletCount += count;
						if( weapons[ n ].normalBulletCount > gunType.NormalMode.BulletCapacity )
							weapons[ n ].normalBulletCount = gunType.NormalMode.BulletCapacity;
					}
				}
				if( gunType.AlternativeMode.BulletType == bulletType )
				{
					if( weapons[ n ].alternativeBulletCount < gunType.AlternativeMode.BulletCapacity )
					{
						taked = true;
						weapons[ n ].alternativeBulletCount += count;
						if( weapons[ n ].alternativeBulletCount > gunType.AlternativeMode.BulletCapacity )
							weapons[ n ].alternativeBulletCount = gunType.AlternativeMode.BulletCapacity;
					}
				}
			}

			if( ActiveWeapon != null )
			{
				Gun activeGun = activeWeapon as Gun;
				if( activeGun != null )
					activeGun.AddBullets( bulletType, count );
			}

			return taked;
		}

		public bool SetActiveWeapon( int index )
		{
			if( index < -1 || index >= weapons.Count )
				return false;

			if( index != -1 )
				if( !weapons[ index ].exists )
					return false;

			if( activeWeapon != null )
			{
				if( index != -1 && Type.Weapons[ index ].WeaponType == activeWeapon.Type )
					return true;

				activeWeapon.PreFire -= activeWeapon_PreFire;

				if( activeWeaponAttachedObject != null )
				{
					Detach( activeWeaponAttachedObject );
					activeWeaponAttachedObject = null;
				}

				Gun activeGun = activeWeapon as Gun;
				if( activeGun != null )
				{
					int activeIndex = GetWeaponIndex( activeWeapon.Type );
					weapons[ activeIndex ].normalMagazineCount =
						activeGun.NormalMode.BulletMagazineCount;
					weapons[ activeIndex ].alternativeMagazineCount =
						activeGun.AlternativeMode.BulletMagazineCount;
				}

				activeWeapon.Die();
				activeWeapon = null;

				if( EntitySystemWorld.Instance.IsServer() )
					Server_SendSetActiveWeaponToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
			}

			if( index != -1 )
			{
				activeWeapon = (Weapon)Entities.Instance.Create(
					Type.Weapons[ index ].WeaponType, Parent );
				activeWeapon.Server_EnableSynchronizationPositionsToClients = false;

				Gun activeGun = activeWeapon as Gun;

				if( activeGun != null )
				{
					activeGun.NormalMode.BulletCount = weapons[ index ].NormalBulletCount;
					activeGun.NormalMode.BulletMagazineCount = weapons[ index ].NormalMagazineCount;

					activeGun.AlternativeMode.BulletCount = weapons[ index ].AlternativeBulletCount;
					activeGun.AlternativeMode.BulletMagazineCount =
						weapons[ index ].AlternativeMagazineCount;
				}

				activeWeapon.PostCreate();

				CreateActiveWeaponAttachedObject();

				activeWeapon.PreFire += activeWeapon_PreFire;

				if( EntitySystemWorld.Instance.IsServer() )
					Server_SendSetActiveWeaponToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
			}

			return true;
		}

		void CreateActiveWeaponAttachedObject()
		{
			activeWeaponAttachedObject = new MapObjectAttachedMapObject();
			activeWeaponAttachedObject.MapObject = activeWeapon;
			activeWeaponAttachedObject.BoneSlot = GetBoneSlotFromAttachedMeshes(
				activeWeapon.Type.CharacterBoneSlot );
			if( activeWeaponAttachedObject.BoneSlot == null )
				activeWeaponAttachedObject.PositionOffset = Type.NotAnimatedWeaponAttachPosition;
			Attach( activeWeaponAttachedObject );
		}

		int GetActiveWeapon()
		{
			if( activeWeapon == null )
				return -1;
			for( int n = 0; n < Weapons.Count; n++ )
			{
				if( weapons[ n ].Exists )
				{
					if( Type.Weapons[ n ].WeaponType == activeWeapon.Type )
						return n;
				}
			}
			return -1;
		}

		void SetActiveNextWeapon()
		{
			if( Weapons.Count == 0 )
				return;

			int index = GetActiveWeapon();
			int counter = Weapons.Count;
			while( counter != 0 )
			{
				counter--;
				index++;
				if( index >= Weapons.Count )
					index = 0;
				if( !Weapons[ index ].Exists )
					continue;

				SetActiveWeapon( index );
				break;
			}
		}

		void SetActivePreviousWeapon()
		{
			if( Weapons.Count == 0 )
				return;

			int index = GetActiveWeapon();
			int counter = Weapons.Count;
			while( counter != 0 )
			{
				counter--;
				index--;
				if( index < 0 )
					index = Weapons.Count - 1;
				if( !Weapons[ index ].Exists )
					continue;

				SetActiveWeapon( index );
				break;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( !loaded )
			{
				for( int n = 0; n < Type.Weapons.Count; n++ )
					weapons.Add( new WeaponItem() );
			}

			SubscribeToTickEvent();

			if( loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				if( activeWeapon != null )
				{
					activeWeapon.PreFire += activeWeapon_PreFire;

					if( activeWeaponAttachedObject == null )
						CreateActiveWeaponAttachedObject();
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( activeWeapon == entity )
			{
				activeWeapon.PreFire -= activeWeapon_PreFire;
				activeWeapon = null;
				activeWeaponAttachedObject = null;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( Intellect != null )
			{
				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire1 ) )
					WeaponTryFire( false );

				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire2 ) )
					WeaponTryFire( true );
			}

			TickContusionTime();

			if( activeWeapon == null || activeWeapon.Ready )
				UpdatePlatformerDemoLookDirection();
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();

			TickContusionTime();
		}

		void activeWeapon_PreFire( Weapon entity, bool alternative )
		{
			UpdatePlatformerDemoLookDirection();
		}

		void UpdatePlatformerDemoLookDirection()
		{
			if( GameMap.Instance != null && GameMap.Instance.GameType == GameMap.GameTypes.PlatformerDemo &&
				LastTickForceVector != Vec2.Zero )
			{
				if( Intellect != null && PlayerIntellect.Instance == Intellect )
				{
					float angle = MathFunctions.ATan( LastTickForceVector.Y, LastTickForceVector.X );
					PlayerIntellect.Instance.LookDirection = new SphereDir( angle, 0 );
				}
			}
		}

		void TickContusionTime()
		{
			if( contusionTimeRemaining != 0 )
			{
				contusionTimeRemaining -= TickDelta;
				if( contusionTimeRemaining < 0 )
					contusionTimeRemaining = 0;
			}
		}

		protected override void OnRenderFrame()
		{
			UpdateAnimationTree();

			base.OnRenderFrame();
		}

		void UpdateAnimationTree()
		{
			if( EntitySystemWorld.Instance.Simulation && !EntitySystemWorld.Instance.SystemPauseOfSimulation )
			{
				AnimationTree tree = GetFirstAnimationTree();
				if( tree != null )
				{
					tree.SetParameterValue( "weapon", activeWeapon != null ? 1 : 0 );

					Radian horizontalAngle = 0;
					Radian verticalAngle = 0;

					PlayerIntellect playerIntellect = Intellect as PlayerIntellect;
					if( playerIntellect != null )
					{
						Vec2 vec = Vec2.Zero;
						vec.X += playerIntellect.GetControlKeyStrength( GameControlKeys.Forward );
						vec.X -= playerIntellect.GetControlKeyStrength( GameControlKeys.Backward );
						vec.Y += playerIntellect.GetControlKeyStrength( GameControlKeys.Left );
						vec.Y -= playerIntellect.GetControlKeyStrength( GameControlKeys.Right );
						if( vec.X < 0 )
							vec = -vec;
						horizontalAngle = MathFunctions.ATan( vec.Y, vec.X );

						verticalAngle = playerIntellect.LookDirection.Vertical;
					}

					tree.SetParameterValue( "weaponHorizontalAngle", horizontalAngle.InDegrees() );
					tree.SetParameterValue( "weaponVerticalAngle", verticalAngle.InDegrees() );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			//get camera state
			bool playerIntellectFPSCamera = false;
			{
				PlayerIntellect playerIntellect = Intellect as PlayerIntellect;
				if( playerIntellect != null )
					playerIntellectFPSCamera = playerIntellect.FPSCamera;
			}
			bool fpsCamera = playerIntellectFPSCamera && camera.Purpose == Camera.Purposes.MainCamera;
			bool mainCameraUpdating = SceneManager.Instance.CurrentUpdatingCamera.Purpose == Camera.Purposes.MainCamera;

			//update first person arms
			MapObjectAttachedMesh firstPersonArmsAttachedMesh =
				GetFirstAttachedObjectByAlias( "firstPersonArms" ) as MapObjectAttachedMesh;
			if( firstPersonArmsAttachedMesh != null )
			{
				if( EntitySystemWorld.Instance.WorldSimulationType != WorldSimulationTypes.Editor && fpsCamera )
					UpdateFirstPersonArmsAttachedMesh( firstPersonArmsAttachedMesh, camera );
			}

			//update weapon
			if( activeWeapon != null && GameMap.Instance.GameType != GameMap.GameTypes.PlatformerDemo )
			{
				//update weapon attached objects visibility depending camera type
				activeWeapon.UpdateTPSFPSCameraAttachedObjectsVisibility( fpsCamera );

				//update weapon position. Guns only.
				if( activeWeaponAttachedObject.MapObject is Gun )
				{
					//guns
					if( fpsCamera )
					{
						if( firstPersonArmsAttachedMesh != null )
						{
							Vec3 p;
							Quat r;
							Vec3 s;

							string slotName = activeWeapon.Type.CharacterBoneSlotFirstPersonCamera;
							if( !string.IsNullOrEmpty( slotName ) )
							{
								MapObjectAttachedMesh.MeshBoneSlot slot = GetBoneSlotFromAttachedMeshes( slotName );
								if( slot != null )
								{
									slot.GetGlobalTransform( out p, out r, out s );
								}
								else
								{
									p = camera.Position;
									r = camera.Rotation;
									s = new Vec3( 1, 1, 1 );
								}
							}
							else
							{
								p = camera.Position + camera.Rotation * activeWeapon.Type.FPSCameraAttachPosition;
								r = camera.Rotation;
								s = new Vec3( 1, 1, 1 );
							}

							activeWeaponAttachedObject.MapObject.SetTransform( p, r, s );
							activeWeaponAttachedObject.MapObject.SetOldTransform( p, r, s );
						}
					}
					else
					{
						//change active weapon position for non animated characters
						if( activeWeaponAttachedObject.BoneSlot == null )
						{
							Vec3 diff = TurnToPosition - Position;
							float dirV = -MathFunctions.ATan( diff.Z, diff.ToVec2().Length() );
							float halfDirV = dirV * .5f;
							Quat rot = new Quat( 0, MathFunctions.Sin( halfDirV ), 0,
								MathFunctions.Cos( halfDirV ) );

							activeWeaponAttachedObject.RotationOffset = rot;
							activeWeaponAttachedObject.PositionOffset = Type.NotAnimatedWeaponAttachPosition;
						}
					}
				}

				//no cast shadows from active weapon in the FPS mode
				foreach( MapObjectAttachedObject weaponAttachedObject in activeWeapon.AttachedObjects )
				{
					MapObjectAttachedMesh weaponAttachedMesh = weaponAttachedObject as MapObjectAttachedMesh;
					if( weaponAttachedMesh != null && weaponAttachedMesh.MeshObject != null )
					{
						if( weaponAttachedMesh.RemainingTime == 0 )
							weaponAttachedMesh.MeshObject.CastShadows = !fpsCamera;
					}
				}
			}

			//change visibility of attached objects
			if( !Map.Instance.CubemapGenerationMode ||
				( Map.Instance.CubemapGenerationMode_CubemapZone != null &&
				Map.Instance.CubemapGenerationMode_CubemapZone.DrawDynamicObjects ) )
			{
				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				{
					if( attachedObject == activeWeaponAttachedObject )
					{
						//attached weapon is always visible
						attachedObject.Visible = true;
					}
					else if( attachedObject == firstPersonArmsAttachedMesh )
					{
						//first person arms is visible only for first person camera. The weapon is also must be activated.
						attachedObject.Visible = fpsCamera && activeWeaponAttachedObject != null;
					}
					else
					{
						//hide attached objects for first person camera
						attachedObject.Visible = !fpsCamera;
					}
				}

				//hide shadows for first person camera mode
				if( playerIntellectFPSCamera && mainCameraUpdating && camera.Purpose == Camera.Purposes.ShadowMap )
				{
					foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
						attachedObject.Visible = false;
				}
			}
		}

		protected override void OnIntellectCommand( Intellect.Command command )
		{
			base.OnIntellectCommand( command );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( command.KeyPressed )
				{
					if( command.Key >= GameControlKeys.Weapon1 && command.Key <= GameControlKeys.Weapon9 )
					{
						int index = (int)command.Key - (int)GameControlKeys.Weapon1;
						if( GetActiveWeapon() != index )
							SetActiveWeapon( index );
						else
							SetActiveWeapon( -1 );
					}

					if( command.Key == GameControlKeys.PreviousWeapon )
						SetActivePreviousWeapon();
					if( command.Key == GameControlKeys.NextWeapon )
						SetActiveNextWeapon();
					if( command.Key == GameControlKeys.Fire1 )
						WeaponTryFire( false );
					if( command.Key == GameControlKeys.Fire2 )
						WeaponTryFire( true );
					if( command.Key == GameControlKeys.Reload )
						WeaponTryReload();
				}
			}
		}

		[Browsable( false )]
		public Weapon ActiveWeapon
		{
			get { return activeWeapon; }
		}

		void WeaponTryFire( bool alternative )
		{
			if( activeWeapon == null )
				return;

			//set real weapon fire direction
			{
				Vec3 seeDir = ( TurnToPosition - Position ).GetNormalize();
				Vec3 lookTo = TurnToPosition;

				for( int iter = 0; iter < 100; iter++ )
				{
					activeWeapon.SetForceFireRotationLookTo( lookTo );
					Vec3 fireDir = activeWeapon.GetFireRotation( alternative ).GetForward();
					Degree angle = MathUtils.GetVectorsAngle( seeDir, fireDir );
					if( angle < 80 )
						break;
					const float step = .3f;
					lookTo += seeDir * step;
				}

				activeWeapon.SetForceFireRotationLookTo( lookTo );
			}

			bool fired = activeWeapon.TryFire( alternative );

			Gun activeGun = activeWeapon as Gun;
			if( activeGun != null )
			{
				if( fired )
				{
					int index = GetWeaponIndex( activeWeapon.Type );
					weapons[ index ].normalBulletCount = activeGun.NormalMode.BulletCount;
					weapons[ index ].normalMagazineCount = activeGun.NormalMode.BulletMagazineCount;
					weapons[ index ].alternativeBulletCount = activeGun.AlternativeMode.BulletCount;
					weapons[ index ].alternativeMagazineCount =
						activeGun.AlternativeMode.BulletMagazineCount;
				}
			}
		}

		void WeaponTryReload()
		{
			if( activeWeapon == null )
				return;

			Gun activeGun = activeWeapon as Gun;
			if( activeGun != null )
				activeGun.TryReload();
		}

		public ChangeMapInformation GetChangeMapInformation( MapChangeRegion region )
		{
			ChangeMapInformation information = new ChangeMapInformation();

			information.position = ( OldPosition - region.Position ) * region.Rotation.GetInverse();
			information.lookDirection = ( (PlayerIntellect)Intellect ).LookDirection *
				region.Rotation.GetInverse();
			information.velocity = MainBody.LinearVelocity * region.Rotation.GetInverse();
			information.health = Health;
			if( region.SaveWeapons )
			{
				information.weapons = weapons;
				information.activeWeaponIndex = GetWeaponIndex( ( activeWeapon != null ) ? activeWeapon.Type : null );
			}

			return information;
		}

		public ChangeMapInformation GetChangeMapInformation( Teleporter teleporter )
		{
			ChangeMapInformation information = new ChangeMapInformation();

			information.position = new Vec3( 0, 0, OldPosition.Z - teleporter.Position.Z );
			information.health = Health;
			information.weapons = weapons;
			information.activeWeaponIndex = GetWeaponIndex(
				( activeWeapon != null ) ? activeWeapon.Type : null );

			return information;
		}

		public void ApplyChangeMapInformation( ChangeMapInformation information, MapObject spawnPoint )
		{
			if( spawnPoint == null )
				return;

			Position = spawnPoint.Position + information.position * spawnPoint.Rotation;
			( (PlayerIntellect)Intellect ).LookDirection =
				information.lookDirection * spawnPoint.Rotation;
			MainBody.LinearVelocity = information.velocity * spawnPoint.Rotation;

			OldPosition = Position;
			OldRotation = Rotation;

			if( information.weapons != null )
			{
				weapons = information.weapons;
				SetActiveWeapon( information.activeWeaponIndex );
			}

			Health = information.health;
		}

		[Browsable( false )]
		public float ContusionTimeRemaining
		{
			get { return contusionTimeRemaining; }
			set
			{
				if( contusionTimeRemaining == value )
					return;

				contusionTimeRemaining = value;

				if( EntitySystemWorld.Instance.IsServer() )
				{
					Server_SendContusionTimeRemainingToClients(
						EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		protected override void OnCopyTransform( MapObject from )
		{
			base.OnCopyTransform( from );
			SetTurnToPosition( from.Position + from.Rotation * new Vec3( 100, 0, 0 ) );
		}

		protected override void Server_OnClientConnectedAfterPostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedAfterPostCreate( remoteEntityWorld );

			IList<RemoteEntityWorld> worlds = new RemoteEntityWorld[] { remoteEntityWorld };
			Server_SendSetActiveWeaponToClients( worlds );
			Server_SendContusionTimeRemainingToClients( worlds );
		}

		void Server_SendSetActiveWeaponToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds,
				typeof( PlayerCharacter ), (ushort)NetworkMessages.ActiveWeaponToClient );
			writer.WriteVariableUInt32( activeWeapon != null ? activeWeapon.NetworkUIN : 0 );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ActiveWeaponToClient )]
		void Client_ReceiveActiveWeapon( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint networkUIN = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			Weapon value = (Weapon)Entities.Instance.GetByNetworkUIN( networkUIN );

			if( activeWeaponAttachedObject != null )
			{
				Detach( activeWeaponAttachedObject );
				activeWeaponAttachedObject = null;
			}

			activeWeapon = value;

			if( activeWeapon != null )
				CreateActiveWeaponAttachedObject();
		}

		void Server_SendContusionTimeRemainingToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PlayerCharacter ),
				(ushort)NetworkMessages.ContusionTimeRemainingToClient );
			writer.Write( contusionTimeRemaining );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ContusionTimeRemainingToClient )]
		void Client_ReceiveContusionTimeRemaining( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			ContusionTimeRemaining = value;
		}

		void UpdateFirstPersonArmsAttachedMesh( MapObjectAttachedMesh armsAttachedMesh, Camera camera )
		{
			//update animation tree
			if( EntitySystemWorld.Instance.Simulation && !EntitySystemWorld.Instance.SystemPauseOfSimulation )
			{
				AnimationTree tree = armsAttachedMesh.AnimationTree;
				if( tree != null )
				{
					bool onGround = GetElapsedTimeSinceLastGroundContact() < .2f;//IsOnGround();

					bool move = false;
					float moveSpeed = 0;
					if( onGround && GroundRelativeVelocitySmooth.ToVec2().Length() > .05f )
					{
						move = true;
						moveSpeed = GroundRelativeVelocitySmooth.ToVec2().Length();
					}

					tree.SetParameterValue( "move", move ? 1 : 0 );
					tree.SetParameterValue( "run", move && IsNeedRun() ? 1 : 0 );
					tree.SetParameterValue( "moveSpeed", moveSpeed );
					float moveSpeedFactor = moveSpeed / Type.RunForwardMaxSpeed;
					if( moveSpeedFactor > 1 )
						moveSpeedFactor = 1;
					tree.SetParameterValue( "moveSpeedFactor", moveSpeedFactor );
				}
			}

			//update scene node
			armsAttachedMesh.SceneNode.Position = camera.Position + camera.Rotation * armsAttachedMesh.TypeObject.Position;
			armsAttachedMesh.SceneNode.Rotation = camera.Rotation * armsAttachedMesh.TypeObject.Rotation;

			//use this code to look arms from another point of view. Useful for calibration position of attached weapon.
			//armsAttachedMesh.SceneNode.Position = new Vec3( 0, 0, 2 ) + Quat.Identity * armsAttachedMesh.TypeObject.Position;
			//armsAttachedMesh.SceneNode.Rotation = Quat.Identity * armsAttachedMesh.TypeObject.Rotation;
		}
	}
}
