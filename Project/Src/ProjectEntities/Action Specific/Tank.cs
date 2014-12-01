// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.Utils;
using Engine.FileSystem;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Tank"/> entity type.
	/// </summary>
	public class TankType : UnitType
	{
		[FieldSerialize]
		float maxForwardSpeed = 20;

		[FieldSerialize]
		float maxBackwardSpeed = 10;

		[FieldSerialize]
		float driveForwardForce = 80000;

		[FieldSerialize]
		float driveBackwardForce = 40000;

		[FieldSerialize]
		float brakeForce = 200000;

		[FieldSerialize]
		Range gunRotationAngleRange = new Range( -8, 40 );

		[FieldSerialize]
		Range optimalAttackDistanceRange;

		[FieldSerialize]
		List<Gear> gears = new List<Gear>();

		[FieldSerialize]
		Degree towerTurnSpeed = 60;

		[FieldSerialize]
		string soundOn;

		[FieldSerialize]
		string soundOff;

		[FieldSerialize]
		string soundGearUp;

		[FieldSerialize]
		string soundGearDown;

		[FieldSerialize]
		string soundTowerTurn;

		[FieldSerialize]
		[DefaultValue( typeof( Vec2 ), "1 0" )]
		Vec2 tracksAnimationMultiplier = new Vec2( 1, 0 );

		[FieldSerialize]
		float mainGunRecoilForce;

		[FieldSerialize]
		List<Wheel> wheels = new List<Wheel>();

		///////////////////////////////////////////

		public class Gear
		{
			[FieldSerialize]
			int number;

			[FieldSerialize]
			Range speedRange;

			[FieldSerialize]
			string soundMotor;

			[FieldSerialize]
			[DefaultValue( typeof( Range ), "1 1.2" )]
			Range soundMotorPitchRange = new Range( 1, 1.2f );

			//

			[DefaultValue( 0 )]
			public int Number
			{
				get { return number; }
				set { number = value; }
			}

			[DefaultValue( typeof( Range ), "0 0" )]
			public Range SpeedRange
			{
				get { return speedRange; }
				set { speedRange = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			[SupportRelativePath]
			public string SoundMotor
			{
				get { return soundMotor; }
				set { soundMotor = value; }
			}

			[DefaultValue( typeof( Range ), "1 1.2" )]
			public Range SoundMotorPitchRange
			{
				get { return soundMotorPitchRange; }
				set { soundMotorPitchRange = value; }
			}

			public override string ToString()
			{
				return string.Format( "Gear {0}", number );
			}
		}

		///////////////////////////////////////////

		public class Wheel
		{
			[FieldSerialize]
			string wheelBoneName = "";
			[FieldSerialize]
			string trackBoneName = "";
			[FieldSerialize]
			float groundHeight = .3f;
			[FieldSerialize]
			float boneMaxHeightOffset = .2f;
			[FieldSerialize]
			float wheelAnimationMultiplier = 1;

			//

			public string WheelBoneName
			{
				get { return wheelBoneName; }
				set { wheelBoneName = value; }
			}

			public string TrackBoneName
			{
				get { return trackBoneName; }
				set { trackBoneName = value; }
			}

			[DefaultValue( .3f )]
			public float GroundHeight
			{
				get { return groundHeight; }
				set { groundHeight = value; }
			}

			[DefaultValue( .2f )]
			public float BoneMaxHeightOffset
			{
				get { return boneMaxHeightOffset; }
				set { boneMaxHeightOffset = value; }
			}

			[DefaultValue( 1.0f )]
			public float WheelAnimationMultiplier
			{
				get { return wheelAnimationMultiplier; }
				set { wheelAnimationMultiplier = value; }
			}

			public override string ToString()
			{
				string result = "";
				if( !string.IsNullOrEmpty( wheelBoneName ) )
					result += string.Format( "Wheel bone: {0}", wheelBoneName );
				if( !string.IsNullOrEmpty( trackBoneName ) )
				{
					if( result != "" )
						result += ", ";
					result += string.Format( "Track bone: {0}", trackBoneName );
				}

				if( result == "" )
					result = "(not initialized)";

				return result;
			}
		}

		///////////////////////////////////////////

		[DefaultValue( 20.0f )]
		public float MaxForwardSpeed
		{
			get { return maxForwardSpeed; }
			set { maxForwardSpeed = value; }
		}

		[DefaultValue( 10.0f )]
		public float MaxBackwardSpeed
		{
			get { return maxBackwardSpeed; }
			set { maxBackwardSpeed = value; }
		}

		[DefaultValue( 80000.0f )]
		public float DriveForwardForce
		{
			get { return driveForwardForce; }
			set { driveForwardForce = value; }
		}

		[DefaultValue( 40000.0f )]
		public float DriveBackwardForce
		{
			get { return driveBackwardForce; }
			set { driveBackwardForce = value; }
		}

		[DefaultValue( 200000.0f )]
		public float BrakeForce
		{
			get { return brakeForce; }
			set { brakeForce = value; }
		}

		[Description( "In degrees." )]
		[DefaultValue( typeof( Range ), "-8 40" )]
		public Range GunRotationAngleRange
		{
			get { return gunRotationAngleRange; }
			set { gunRotationAngleRange = value; }
		}

		[DefaultValue( typeof( Range ), "0 0" )]
		public Range OptimalAttackDistanceRange
		{
			get { return optimalAttackDistanceRange; }
			set { optimalAttackDistanceRange = value; }
		}

		public List<Gear> Gears
		{
			get { return gears; }
		}

		[Description( "Degrees per second." )]
		[DefaultValue( typeof( Degree ), "60" )]
		public Degree TowerTurnSpeed
		{
			get { return towerTurnSpeed; }
			set { towerTurnSpeed = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundOn
		{
			get { return soundOn; }
			set { soundOn = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundOff
		{
			get { return soundOff; }
			set { soundOff = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundGearUp
		{
			get { return soundGearUp; }
			set { soundGearUp = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundGearDown
		{
			get { return soundGearDown; }
			set { soundGearDown = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundTowerTurn
		{
			get { return soundTowerTurn; }
			set { soundTowerTurn = value; }
		}

		[DefaultValue( typeof( Vec2 ), "1 0" )]
		public Vec2 TracksAnimationMultiplier
		{
			get { return tracksAnimationMultiplier; }
			set { tracksAnimationMultiplier = value; }
		}

		[DefaultValue( 0.0f )]
		public float MainGunRecoilForce
		{
			get { return mainGunRecoilForce; }
			set { mainGunRecoilForce = value; }
		}

		public List<Wheel> Wheels
		{
			get { return wheels; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			PreloadSound( SoundOn, SoundMode.Mode3D );
			PreloadSound( SoundOff, SoundMode.Mode3D );
			PreloadSound( SoundGearUp, SoundMode.Mode3D );
			PreloadSound( SoundGearDown, SoundMode.Mode3D );
			PreloadSound( SoundTowerTurn, SoundMode.Mode3D | SoundMode.Loop );
			foreach( Gear gear in gears )
				PreloadSound( gear.SoundMotor, SoundMode.Mode3D | SoundMode.Loop );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class Tank : Unit
	{
		Track leftTrack = new Track();
		Track rightTrack = new Track();
		float tracksPositionYOffset;

		Body chassisBody;
		float chassisSleepTimer;
		Body towerBody;
		Vec3 towerBodyLocalPosition;

		MapObjectAttachedMapObject mainGunAttachedObject;
		Gun mainGun;
		Vec3 mainGunOffsetPosition;

		SphereDir towerLocalDirection;
		SphereDir needTowerLocalDirection;
		SphereDir server_sentTowerLocalDirection;

		//currently gears used only for sounds
		TankType.Gear currentGear;

		bool motorOn;
		string currentMotorSoundName;
		VirtualChannel motorSoundChannel;
		VirtualChannel towerTurnChannel;

		bool firstTick = true;

		//Minefield specific
		float minefieldUpdateTimer;

		//tracks animation
		float tracksTextureAnimationRenderTime;

		//wheels
		List<Wheel> wheels = new List<Wheel>();
		MeshObject wheelsSkeletonAnimationMeshObject;
		float wheelsSkeletonAnimationRenderTime;
		bool wheelsSkeletonAnimationNeedUpdate = true;

		///////////////////////////////////////////

		class Track
		{
			public List<MapObjectAttachedHelper> trackHelpers = new List<MapObjectAttachedHelper>();
			public bool onGround = true;

			//animation
			public MeshObject.SubObject meshSubObject;
			public Vec2 materialScrollValue;
			public ShaderBaseMaterial clonedMaterialForFixedPipeline;

			public float speed;
			public float server_sentSpeed;
		}

		///////////////////////////////////////////

		class Wheel
		{
			public TankType.Wheel type;
			public Track track;

			public float rotationAngle;
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			TowerLocalDirectionToClient,
			TracksSpeedToClient,
		}

		///////////////////////////////////////////

		TankType _type = null; public new TankType Type { get { return _type; } }

		public Tank()
		{
			//Minefield specific
			minefieldUpdateTimer = World.Instance.Random.NextFloat();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPreCreate()"/>.</summary>
		protected override void OnPreCreate()
		{
			base.OnPreCreate();

			//initialize wheels list
			wheels = new List<Wheel>();
			foreach( TankType.Wheel wheelType in Type.Wheels )
			{
				Wheel wheel = new Wheel();

				wheel.type = wheelType;

				//detect track
				bool left = wheel.type.WheelBoneName.ToLower().Contains( "left" );
				wheel.track = left ? leftTrack : rightTrack;

				wheel.rotationAngle = World.Instance.Random.NextFloat() * MathFunctions.PI * 2;

				wheels.Add( wheel );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();

			if( EngineApp.Instance.ApplicationType != EngineApp.ApplicationTypes.ResourceEditor )
			{
				if( PhysicsModel == null )
				{
					Log.Error( "Tank: Physics model not exists." );
					return;
				}

				chassisBody = PhysicsModel.GetBody( "chassis" );
				if( chassisBody == null )
				{
					Log.Error( "Tank: \"chassis\" body not exists." );
					return;
				}
				towerBody = PhysicsModel.GetBody( "tower" );

				//chassisBody.Collision += chassisBody_Collision;

				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				{
					if( attachedObject.Alias == "leftTrack" )
						leftTrack.trackHelpers.Add( (MapObjectAttachedHelper)attachedObject );
					if( attachedObject.Alias == "rightTrack" )
						rightTrack.trackHelpers.Add( (MapObjectAttachedHelper)attachedObject );
				}

				if( leftTrack.trackHelpers.Count != 0 )
					tracksPositionYOffset = Math.Abs( leftTrack.trackHelpers[ 0 ].PositionOffset.Y );
			}

			//mainGun
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject == null )
					continue;

				mainGun = attachedMapObject.MapObject as Gun;
				if( mainGun != null )
				{
					mainGunAttachedObject = attachedMapObject;
					mainGunOffsetPosition = attachedMapObject.PositionOffset;
					break;
				}
			}

			//towerBodyLocalPosition
			if( towerBody != null )
				towerBodyLocalPosition = PhysicsModel.ModelDeclaration.GetBody( towerBody.Name ).Position;

			//initialize currentGear
			currentGear = Type.Gears.Find( delegate( TankType.Gear gear )
			{
				return gear.Number == 0;
			} );

			if( EngineApp.Instance.ApplicationType != EngineApp.ApplicationTypes.ResourceEditor )
			{
				InitTracksTextureAnimation();
				InitWheelsSkeletonAnimation();
			}

			//disable contacts between chassisBody and towerBody
			if( chassisBody != null && towerBody != null )
			{
				foreach( Shape shape1 in chassisBody.Shapes )
				{
					foreach( Shape shape2 in towerBody.Shapes )
					{
						PhysicsWorld.Instance.SetShapePairFlags( shape1, shape2,
							ShapePairFlags.DisableContacts );
					}
				}
			}
		}

		protected override void OnDestroy()
		{
			if( motorSoundChannel != null )
			{
				motorSoundChannel.Stop();
				motorSoundChannel = null;
			}

			if( towerTurnChannel != null )
			{
				towerTurnChannel.Stop();
				towerTurnChannel = null;
			}

			ShutdownTracksTextureAnimation();
			ShutdownWheelsSkeletonAnimation();

			base.OnDestroy();
		}

		protected override void OnSuspendPhysicsDuringMapLoading( bool suspend )
		{
			base.OnSuspendPhysicsDuringMapLoading( suspend );

			//After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
			//During this time we will disable physics for this entity.
			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
					body.Static = suspend;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			CalculateTracksSpeed();
			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendTracksSpeedToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );

			TickChassisGround();
			TickChassis();

			if( Intellect != null )
			{
				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire1 ) )
				{
					if( GunsTryFire( false ) )
						AddMainGunRecoilForce();
				}

				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire2 ) )
					GunsTryFire( true );
			}

			TickCurrentGear();
			TickMotorSound();

			TickTowerTurn();

			TickTurnOver();

			//Minefield specific
			TickMinefields();

			//send tower local direction to clients
			if( EntitySystemWorld.Instance.IsServer() )
				Server_TickSendTowerLocalDirection();

			if( RenderSystem.Instance.IsNULLRenderingSystem() )
				UpdateTowerTransform();

			firstTick = false;
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();

			TickCurrentGear();
			TickMotorSound();

			firstTick = false;
		}

		void AddMainGunRecoilForce()
		{
			if( mainGun != null && chassisBody != null && Type.MainGunRecoilForce != 0 )
			{
				chassisBody.AddForce( ForceType.GlobalAtGlobalPos, 0,
					mainGun.Rotation * new Vec3( -Type.MainGunRecoilForce, 0, 0 ), mainGun.Position );
			}
		}

		void TickMotorSound()
		{
			bool lastMotorOn = motorOn;
			motorOn = Intellect != null && Intellect.IsActive();

			//sound on, off
			if( motorOn != lastMotorOn )
			{
				if( !firstTick && Health != 0 )
				{
					if( motorOn )
						SoundPlay3D( Type.SoundOn, .7f, true );
					else
						SoundPlay3D( Type.SoundOff, .7f, true );
				}
			}

			string needSoundName = null;
			if( motorOn && currentGear != null )
				needSoundName = currentGear.SoundMotor;

			if( needSoundName != currentMotorSoundName )
			{
				//change motor sound

				if( motorSoundChannel != null )
				{
					motorSoundChannel.Stop();
					motorSoundChannel = null;
				}

				currentMotorSoundName = needSoundName;

				if( !string.IsNullOrEmpty( needSoundName ) )
				{
					Sound sound = SoundWorld.Instance.SoundCreate(
						RelativePathUtils.ConvertToFullPath( Path.GetDirectoryName( Type.FilePath ), needSoundName ),
						SoundMode.Mode3D | SoundMode.Loop );

					if( sound != null )
					{
						motorSoundChannel = SoundWorld.Instance.SoundPlay(
							sound, EngineApp.Instance.DefaultSoundChannelGroup, .3f, true );
						motorSoundChannel.Position = Position;
						switch( Type.SoundRolloffMode )
						{
						case DynamicType.SoundRolloffModes.Logarithmic:
							motorSoundChannel.SetLogarithmicRolloff( Type.SoundMinDistance, Type.SoundMaxDistance,
								Type.SoundRolloffLogarithmicFactor );
							break;
						case DynamicType.SoundRolloffModes.Linear:
							motorSoundChannel.SetLinearRolloff( Type.SoundMinDistance, Type.SoundMaxDistance );
							break;
						}
						motorSoundChannel.Pause = false;
					}
				}
			}

			//update motor channel position and pitch
			if( motorSoundChannel != null )
			{
				Range speedRangeAbs = currentGear.SpeedRange;
				if( speedRangeAbs.Minimum < 0 && speedRangeAbs.Maximum < 0 )
					speedRangeAbs = new Range( -speedRangeAbs.Maximum, -speedRangeAbs.Minimum );
				Range pitchRange = currentGear.SoundMotorPitchRange;

				float speedAbs = Math.Max( Math.Abs( leftTrack.speed ), Math.Abs( rightTrack.speed ) );

				float speedCoef = 0;
				if( speedRangeAbs.Size() != 0 )
					speedCoef = ( speedAbs - speedRangeAbs.Minimum ) / speedRangeAbs.Size();
				MathFunctions.Clamp( ref speedCoef, 0, 1 );

				//update channel
				motorSoundChannel.Pitch = pitchRange.Minimum + speedCoef * pitchRange.Size();
				motorSoundChannel.Position = Position;
			}
		}

		protected override void OnIntellectCommand( Intellect.Command command )
		{
			base.OnIntellectCommand( command );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( command.KeyPressed )
				{
					if( command.Key == GameControlKeys.Fire1 )
					{
						if( GunsTryFire( false ) )
							AddMainGunRecoilForce();
					}
					if( command.Key == GameControlKeys.Fire2 )
						GunsTryFire( true );
				}
			}
		}

		bool GunsTryFire( bool alternative )
		{
			bool fire = false;

			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject == null )
					continue;

				Gun gun = attachedMapObject.MapObject as Gun;

				if( gun != null )
				{
					if( gun.TryFire( alternative ) )
						fire = true;
				}
			}

			return fire;
		}

		void TickChassisGround()
		{
			if( chassisBody == null )
				return;

			if( chassisBody.Sleeping )
				return;

			float rayLength = .7f;

			leftTrack.onGround = false;
			rightTrack.onGround = false;

			float mass = 0;
			foreach( Body body in PhysicsModel.Bodies )
				mass += body.Mass;

			int helperCount = leftTrack.trackHelpers.Count + rightTrack.trackHelpers.Count;

			float verticalVelocity =
				( chassisBody.Rotation.GetInverse() * chassisBody.LinearVelocity ).Z;

			for( int side = 0; side < 2; side++ )
			{
				Track track = side == 0 ? leftTrack : rightTrack;

				foreach( MapObjectAttachedHelper trackHelper in track.trackHelpers )
				{
					Vec3 pos;
					Quat rot;
					Vec3 scl;
					trackHelper.GetGlobalTransform( out pos, out rot, out scl );

					Vec3 downDirection = chassisBody.Rotation * new Vec3( 0, 0, -rayLength );

					Vec3 start = pos - downDirection;

					Ray ray = new Ray( start, downDirection );
					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
						ray, (int)ContactGroup.CastOnlyContact );

					bool collision = false;
					Vec3 collisionPos = Vec3.Zero;

					foreach( RayCastResult result in piercingResult )
					{
						if( Array.IndexOf( PhysicsModel.Bodies, result.Shape.Body ) != -1 )
							continue;
						collision = true;
						collisionPos = result.Position;
						break;
					}

					if( collision )
					{
						track.onGround = true;

						float distance = ( collisionPos - start ).Length();

						if( distance < rayLength )
						{
							float needCoef = ( rayLength - distance ) / rayLength;

							float force = 0;
							//anti gravity
							force += ( -PhysicsWorld.Instance.MainScene.Gravity.Z * mass ) / (float)helperCount;
							//anti vertical velocity
							force += ( -verticalVelocity * mass ) / (float)helperCount;

							force *= ( needCoef + .45f );

							chassisBody.AddForce( ForceType.GlobalAtGlobalPos,
								TickDelta, new Vec3( 0, 0, force ), pos );
						}
					}
				}
			}
		}

		void TickChassis()
		{
			bool onGround = leftTrack.onGround || rightTrack.onGround;

			float leftTrackThrottle = 0;
			float rightTrackThrottle = 0;
			if( Intellect != null )
			{
				float forward = Intellect.GetControlKeyStrength( GameControlKeys.Forward );
				leftTrackThrottle += forward;
				rightTrackThrottle += forward;

				float backward = Intellect.GetControlKeyStrength( GameControlKeys.Backward );
				leftTrackThrottle -= backward;
				rightTrackThrottle -= backward;

				float left = Intellect.GetControlKeyStrength( GameControlKeys.Left );
				leftTrackThrottle -= left * 2;
				rightTrackThrottle += left * 2;

				float right = Intellect.GetControlKeyStrength( GameControlKeys.Right );
				leftTrackThrottle += right * 2;
				rightTrackThrottle -= right * 2;

				MathFunctions.Clamp( ref leftTrackThrottle, -1, 1 );
				MathFunctions.Clamp( ref rightTrackThrottle, -1, 1 );
			}

			//return if no throttle and sleeping
			if( chassisBody.Sleeping && rightTrackThrottle == 0 && leftTrackThrottle == 0 )
				return;

			Vec3 localLinearVelocity = chassisBody.LinearVelocity * chassisBody.Rotation.GetInverse();

			//add drive force

			float slopeForwardForceCoeffient;
			float slopeBackwardForceCoeffient;
			float slopeLinearDampingAddition;
			{
				Vec3 dir = chassisBody.Rotation.GetForward();
				Radian slopeAngle = MathFunctions.ATan( dir.Z, dir.ToVec2().Length() );

				Radian maxAngle = MathFunctions.PI / 4;//new Degree(45)

				slopeForwardForceCoeffient = 1;
				if( slopeAngle > maxAngle )
					slopeForwardForceCoeffient = 0;

				slopeBackwardForceCoeffient = 1;
				if( slopeAngle < -maxAngle )
					slopeBackwardForceCoeffient = 0;

				MathFunctions.Clamp( ref slopeForwardForceCoeffient, 0, 1 );
				MathFunctions.Clamp( ref slopeBackwardForceCoeffient, 0, 1 );

				slopeLinearDampingAddition = localLinearVelocity.X > 0 ? slopeAngle : -slopeAngle;
				//slopeLinearDampingAddition *= 1;
				if( slopeLinearDampingAddition < 0 )
					slopeLinearDampingAddition = 0;
			}

			if( leftTrack.onGround )
			{
				if( leftTrackThrottle > 0 && localLinearVelocity.X < Type.MaxForwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.DriveForwardForce : Type.BrakeForce;
					force *= leftTrackThrottle;
					force *= slopeForwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, tracksPositionYOffset, 0 ) );
				}

				if( leftTrackThrottle < 0 && ( -localLinearVelocity.X ) < Type.MaxBackwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.BrakeForce : Type.DriveBackwardForce;
					force *= leftTrackThrottle;
					force *= slopeBackwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, tracksPositionYOffset, 0 ) );
				}
			}

			if( rightTrack.onGround )
			{
				if( rightTrackThrottle > 0 && localLinearVelocity.X < Type.MaxForwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.DriveForwardForce : Type.BrakeForce;
					force *= rightTrackThrottle;
					force *= slopeForwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, -tracksPositionYOffset, 0 ) );
				}

				if( rightTrackThrottle < 0 && ( -localLinearVelocity.X ) < Type.MaxBackwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.BrakeForce : Type.DriveBackwardForce;
					force *= rightTrackThrottle;
					force *= slopeBackwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, -tracksPositionYOffset, 0 ) );
				}
			}

			//LinearVelocity
			if( onGround && localLinearVelocity != Vec3.Zero )
			{
				Vec3 velocity = localLinearVelocity;
				velocity.Y = 0;
				chassisBody.LinearVelocity = chassisBody.Rotation * velocity;
			}

			bool stop = onGround && leftTrackThrottle == 0 && rightTrackThrottle == 0;

			bool noLinearVelocity = chassisBody.LinearVelocity.Equals( Vec3.Zero, .2f );
			bool noAngularVelocity = chassisBody.AngularVelocity.Equals( Vec3.Zero, .2f );

			//LinearDamping
			float linearDamping;
			if( stop )
				linearDamping = noLinearVelocity ? 5 : 1;
			else
				linearDamping = .15f;
			chassisBody.LinearDamping = linearDamping + slopeLinearDampingAddition;

			//AngularDamping
			if( onGround )
			{
				if( stop && noAngularVelocity )
					chassisBody.AngularDamping = 5;
				else
					chassisBody.AngularDamping = 1;
			}
			else
				chassisBody.AngularDamping = .15f;

			//sleeping
			if( !chassisBody.Sleeping && stop && noLinearVelocity && noAngularVelocity )
			{
				chassisSleepTimer += TickDelta;
				if( chassisSleepTimer > 1 )
					chassisBody.Sleeping = true;
			}
			else
				chassisSleepTimer = 0;
		}

		[Browsable( false )]
		public Gun MainGun
		{
			get { return mainGun; }
		}

		public void SetMomentaryTurnToPosition( Vec3 pos )
		{
			if( towerBody == null )
				return;

			Vec3 direction = pos - towerBody.Position;
			towerLocalDirection = SphereDir.FromVector( Rotation.GetInverse() * direction );
			needTowerLocalDirection = towerLocalDirection;
		}

		public void SetNeedTurnToPosition( Vec3 pos )
		{
			if( towerBody == null )
				return;

			if( Type.TowerTurnSpeed != 0 )
			{
				Vec3 direction = pos - towerBody.Position;
				needTowerLocalDirection = SphereDir.FromVector( Rotation.GetInverse() * direction );
			}
			else
				SetMomentaryTurnToPosition( pos );
		}

		protected override void OnCalculateMapBounds( ref Bounds bounds )
		{
			base.OnCalculateMapBounds( ref bounds );

			//add gun bounds to the tank
			if( MainGun != null )
				bounds.Add( MainGun.MapBounds );
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( chassisBody != null &&
				( !chassisBody.Sleeping || EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationTypes.Editor ) )
			{
				wheelsSkeletonAnimationNeedUpdate = true;
			}
		}

		protected override void OnRender( Camera camera )
		{
			//not very true update in the OnRender.
			//it is here because need update after all Ticks and before update attached objects.
			UpdateTowerTransform();

			base.OnRender( camera );
		}

		void UpdateTowerTransform()
		{
			if( towerBody == null || chassisBody == null || mainGunAttachedObject == null )
				return;

			Radian horizontalAngle = towerLocalDirection.Horizontal;
			Radian verticalAngle = towerLocalDirection.Vertical;

			Range gunRotationRange = Type.GunRotationAngleRange * MathFunctions.PI / 180.0f;
			if( verticalAngle < gunRotationRange.Minimum )
				verticalAngle = gunRotationRange.Minimum;
			if( verticalAngle > gunRotationRange.Maximum )
				verticalAngle = gunRotationRange.Maximum;

			//update tower body
			towerBody.Position = GetInterpolatedPosition() + GetInterpolatedRotation() * towerBodyLocalPosition;
			towerBody.Rotation = GetInterpolatedRotation() * new Angles( 0, 0, -horizontalAngle.InDegrees() ).ToQuat();
			towerBody.Sleeping = true;

			//update gun vertical rotation
			Quat verticalRotation = new Angles( 0, verticalAngle.InDegrees(), 0 ).ToQuat();
			mainGunAttachedObject.RotationOffset = verticalRotation;
			RecalculateMapBounds();
		}

		void CalculateTracksSpeed()
		{
			leftTrack.speed = 0;
			rightTrack.speed = 0;

			if( chassisBody == null )
				return;

			if( chassisBody.Sleeping )
				return;

			Vec3 linearVelocity = chassisBody.LinearVelocity;
			Vec3 angularVelocity = chassisBody.AngularVelocity;

			//optimization
			if( linearVelocity.Equals( Vec3.Zero, .1f ) && angularVelocity.Equals( Vec3.Zero, .1f ) )
				return;

			Vec3 localLinearVelocity = linearVelocity * chassisBody.Rotation.GetInverse();
			leftTrack.speed = localLinearVelocity.X - angularVelocity.Z * 2;
			rightTrack.speed = localLinearVelocity.X + angularVelocity.Z * 2;
		}

		void TickCurrentGear()
		{
			//currently gears used only for sounds

			if( currentGear == null )
				return;

			if( motorOn )
			{
				float speed = Math.Max( leftTrack.speed, rightTrack.speed );

				TankType.Gear newGear = null;

				if( speed < currentGear.SpeedRange.Minimum || speed > currentGear.SpeedRange.Maximum )
				{
					//find new gear
					newGear = Type.Gears.Find( delegate( TankType.Gear gear )
					{
						return speed >= gear.SpeedRange.Minimum && speed <= gear.SpeedRange.Maximum;
					} );
				}

				if( newGear != null && currentGear != newGear )
				{
					//change gear
					TankType.Gear oldGear = currentGear;
					OnGearChange( oldGear, newGear );
					currentGear = newGear;
				}
			}
			else
			{
				if( currentGear.Number != 0 )
				{
					currentGear = Type.Gears.Find( delegate( TankType.Gear gear )
					{
						return gear.Number == 0;
					} );
				}
			}
		}

		void OnGearChange( TankType.Gear oldGear, TankType.Gear newGear )
		{
			if( !firstTick && Health != 0 )
			{
				bool up = Math.Abs( newGear.Number ) > Math.Abs( oldGear.Number );
				string soundName = up ? Type.SoundGearUp : Type.SoundGearDown;
				SoundPlay3D( soundName, .7f, true );
			}
		}

		void TickTowerTurn()
		{
			//update direction
			if( towerLocalDirection != needTowerLocalDirection )
			{
				Radian turnSpeed = Type.TowerTurnSpeed;

				SphereDir needDirection = needTowerLocalDirection;
				SphereDir direction = towerLocalDirection;

				//update horizontal direction
				float diffHorizontalAngle = needDirection.Horizontal - direction.Horizontal;
				while( diffHorizontalAngle < -MathFunctions.PI )
					diffHorizontalAngle += MathFunctions.PI * 2;
				while( diffHorizontalAngle > MathFunctions.PI )
					diffHorizontalAngle -= MathFunctions.PI * 2;

				if( diffHorizontalAngle > 0 )
				{
					if( direction.Horizontal > needDirection.Horizontal )
						direction.Horizontal -= MathFunctions.PI * 2;
					direction.Horizontal += turnSpeed * TickDelta;
					if( direction.Horizontal > needDirection.Horizontal )
						direction.Horizontal = needDirection.Horizontal;
				}
				else
				{
					if( direction.Horizontal < needDirection.Horizontal )
						direction.Horizontal += MathFunctions.PI * 2;
					direction.Horizontal -= turnSpeed * TickDelta;
					if( direction.Horizontal < needDirection.Horizontal )
						direction.Horizontal = needDirection.Horizontal;
				}

				//update vertical direction
				if( direction.Vertical < needDirection.Vertical )
				{
					direction.Vertical += turnSpeed * TickDelta;
					if( direction.Vertical > needDirection.Vertical )
						direction.Vertical = needDirection.Vertical;
				}
				else
				{
					direction.Vertical -= turnSpeed * TickDelta;
					if( direction.Vertical < needDirection.Vertical )
						direction.Vertical = needDirection.Vertical;
				}

				if( direction.Equals( needTowerLocalDirection, .001f ) )
					towerLocalDirection = direction;

				towerLocalDirection = direction;
			}

			//update tower turn sound
			{
				bool needSound = !towerLocalDirection.Equals( needTowerLocalDirection,
					new Degree( 2 ).InRadians() );

				if( needSound )
				{
					if( towerTurnChannel == null && !string.IsNullOrEmpty( Type.SoundTowerTurn ) )
					{
						Sound sound = SoundWorld.Instance.SoundCreate(
							RelativePathUtils.ConvertToFullPath( Path.GetDirectoryName( Type.FilePath ), Type.SoundTowerTurn ),
							SoundMode.Mode3D | SoundMode.Loop );

						if( sound != null )
						{
							towerTurnChannel = SoundWorld.Instance.SoundPlay(
								sound, EngineApp.Instance.DefaultSoundChannelGroup, .3f, true );
							towerTurnChannel.Position = Position;
							switch( Type.SoundRolloffMode )
							{
							case DynamicType.SoundRolloffModes.Logarithmic:
								towerTurnChannel.SetLogarithmicRolloff( Type.SoundMinDistance, Type.SoundMaxDistance,
									Type.SoundRolloffLogarithmicFactor );
								break;
							case DynamicType.SoundRolloffModes.Linear:
								towerTurnChannel.SetLinearRolloff( Type.SoundMinDistance, Type.SoundMaxDistance );
								break;
							}
							towerTurnChannel.Pause = false;
						}
					}

					if( towerTurnChannel != null )
						towerTurnChannel.Position = Position;
				}
				else
				{
					if( towerTurnChannel != null )
					{
						towerTurnChannel.Stop();
						towerTurnChannel = null;
					}
				}
			}

		}

		void InitTracksTextureAnimation()
		{
			for( int nTrack = 0; nTrack < 2; nTrack++ )
			{
				Track track = nTrack == 0 ? leftTrack : rightTrack;

				//find by alias
				{
					string alias = nTrack == 0 ? "leftTrackMesh" : "rightTrackMesh";

					MapObjectAttachedMesh attachedMesh = GetFirstAttachedObjectByAlias( alias ) as MapObjectAttachedMesh;
					if( attachedMesh != null && attachedMesh.MeshObject != null )
						track.meshSubObject = attachedMesh.MeshObject.SubObjects[ 0 ];
				}

				//find by material name
				{
					string mark = nTrack == 0 ? "lefttrack" : "righttrack";

					foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
					{
						MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
						if( attachedMesh != null )
						{
							MeshObject meshObject = attachedMesh.MeshObject;

							if( meshObject != null )
							{
								foreach( MeshObject.SubObject subObject in meshObject.SubObjects )
								{
									if( subObject.MaterialName.ToLower().Contains( mark ) )
									{
										track.meshSubObject = subObject;
										break;
									}
								}
							}
						}
					}
				}

				if( track.meshSubObject != null )
				{
					track.meshSubObject.Parent.AddToRenderQueue += TrackMeshObject_AddToRenderQueue;

					//for fixed pipeline
					if( !RenderSystem.Instance.HasShaderModel3() )
					{
						ShaderBaseMaterial sourceMaterial = HighLevelMaterialManager.Instance.GetMaterialByName(
							track.meshSubObject.MaterialName ) as ShaderBaseMaterial;

						if( sourceMaterial != null )
						{
							string materialName = MaterialManager.Instance.GetUniqueName( sourceMaterial.Name + "_Cloned" );
							track.clonedMaterialForFixedPipeline = (ShaderBaseMaterial)
								HighLevelMaterialManager.Instance.Clone( sourceMaterial, materialName );

							track.clonedMaterialForFixedPipeline.UpdateBaseMaterial();

							//change material
							track.meshSubObject.MaterialName = materialName;
						}
					}
				}
			}
		}

		void ShutdownTracksTextureAnimation()
		{
			leftTrack.meshSubObject = null;
			rightTrack.meshSubObject = null;

			if( leftTrack.clonedMaterialForFixedPipeline != null )
			{
				leftTrack.clonedMaterialForFixedPipeline.Dispose();
				leftTrack.clonedMaterialForFixedPipeline = null;
			}
			if( rightTrack.clonedMaterialForFixedPipeline != null )
			{
				rightTrack.clonedMaterialForFixedPipeline.Dispose();
				rightTrack.clonedMaterialForFixedPipeline = null;
			}
		}

		void TrackMeshObject_AddToRenderQueue( MovableObject sender, Camera camera,
			bool onlyShadowCasters, ref bool allowRender )
		{
			float renderTime = RendererWorld.Instance.FrameRenderTime;
			if( tracksTextureAnimationRenderTime != renderTime )
			{
				tracksTextureAnimationRenderTime = renderTime;
				UpdateTracksTextureAnimation();
			}
		}

		void UpdateTracksTextureAnimation()
		{
			for( int nTrack = 0; nTrack < 2; nTrack++ )
			{
				Track track = nTrack == 0 ? leftTrack : rightTrack;

				if( track.meshSubObject == null )
					continue;

				//update value
				if( EntitySystemWorld.Instance.Simulation &&
					!EntitySystemWorld.Instance.SystemPauseOfSimulation )
				{
					Vec2 value = track.materialScrollValue + Type.TracksAnimationMultiplier *
						( track.speed * RendererWorld.Instance.FrameRenderTimeStep );

					while( value.X < 0 ) value.X++;
					while( value.X >= 1 ) value.X--;
					while( value.Y < 0 ) value.Y++;
					while( value.Y >= 1 ) value.Y--;

					track.materialScrollValue = value;
				}

				//update gpu program parameters
				{
					Vec4 value = new Vec4( track.materialScrollValue.X, track.materialScrollValue.Y,
						0, 0 );

					track.meshSubObject.SetCustomGpuParameter(
						(int)ShaderBaseMaterial.GpuParameters.diffuse1MapTransformAdd, value );
					track.meshSubObject.SetCustomGpuParameter(
						(int)ShaderBaseMaterial.GpuParameters.specularMapTransformAdd, value );
					track.meshSubObject.SetCustomGpuParameter(
						(int)ShaderBaseMaterial.GpuParameters.normalMapTransformAdd, value );
				}

				//update parameters for fixed pipeline material
				if( track.clonedMaterialForFixedPipeline != null )
				{
					track.clonedMaterialForFixedPipeline.Diffuse1Map.Transform.Scroll = track.materialScrollValue;
					track.clonedMaterialForFixedPipeline.SpecularMap.Transform.Scroll = track.materialScrollValue;
				}
			}
		}

		void InitWheelsSkeletonAnimation()
		{
			MapObjectAttachedMesh attachedMesh = null;

			//find first attached mesh
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				attachedMesh = attachedObject as MapObjectAttachedMesh;
				if( attachedMesh != null )
					break;
			}

			if( attachedMesh == null )
				return;
			if( attachedMesh.MeshObject == null )
				return;
			if( attachedMesh.MeshObject.Skeleton == null )
				return;

			wheelsSkeletonAnimationMeshObject = attachedMesh.MeshObject;
			wheelsSkeletonAnimationMeshObject.AddToRenderQueue +=
				WheelsSkeletonAnimationMeshObject_AddToRenderQueue;
		}

		void ShutdownWheelsSkeletonAnimation()
		{
			wheelsSkeletonAnimationMeshObject = null;
		}

		void WheelsSkeletonAnimationMeshObject_AddToRenderQueue( MovableObject sender,
			Camera camera, bool onlyShadowCasters, ref bool allowRender )
		{
			float renderTime = RendererWorld.Instance.FrameRenderTime;
			if( wheelsSkeletonAnimationRenderTime != renderTime )
			{
				wheelsSkeletonAnimationRenderTime = renderTime;

				if( wheelsSkeletonAnimationNeedUpdate )
				{
					UpdateWheelsSkeletonAnimation();
					wheelsSkeletonAnimationNeedUpdate = false;
				}
			}
		}

		void UpdateWheelsSkeletonAnimation()
		{
			MeshObject meshObject = wheelsSkeletonAnimationMeshObject;
			if( meshObject == null )
				return;

			if( meshObject.Skeleton == null )
				return;

			if( chassisBody == null )
				return;

			foreach( Wheel wheel in wheels )
			{
				//update rotation angle
				if( EntitySystemWorld.Instance.Simulation &&
					!EntitySystemWorld.Instance.SystemPauseOfSimulation )
				{
					wheel.rotationAngle += wheel.track.speed * wheel.type.WheelAnimationMultiplier *
						RendererWorld.Instance.FrameRenderTimeStep;
					wheel.rotationAngle = MathFunctions.RadiansNormalize360( wheel.rotationAngle );
				}

				//update bones
				{
					Bone wheelBone = meshObject.Skeleton.GetBone( wheel.type.WheelBoneName );
					Bone trackBone = meshObject.Skeleton.GetBone( wheel.type.TrackBoneName );
					Bone sourceWheelBone = meshObject.Mesh.Skeleton.GetBone( wheel.type.WheelBoneName );
					Bone sourceTrackBone = meshObject.Mesh.Skeleton.GetBone( wheel.type.TrackBoneName );

					float heightOffset = 0;

					//calculate height offset
					if( sourceWheelBone != null && wheel.type.BoneMaxHeightOffset != 0 )
					{
						Vec3 boneWorldPosition;
						{
							SceneNode sceneNode = meshObject.ParentSceneNode;

							Vec3 boneDerivedPosition = sourceWheelBone.GetDerivedPosition();
							Quat boneDerivedRotation = sourceWheelBone.GetDerivedRotation();

							boneWorldPosition = sceneNode.Position +
								( sceneNode.Rotation * boneDerivedPosition ) * sceneNode.Scale;
						}

						float maxHeightOffset = wheel.type.BoneMaxHeightOffset;
						float typeGroundHeight = wheel.type.GroundHeight;

						Vec3 rayDirection = chassisBody.Rotation *
							new Vec3( 0, 0, -( maxHeightOffset + typeGroundHeight ) );

						Ray ray = new Ray( boneWorldPosition, rayDirection );
						RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
							ray, (int)ContactGroup.CastOnlyContact );

						float length = rayDirection.Length();

						foreach( RayCastResult result in piercingResult )
						{
							if( Array.IndexOf( PhysicsModel.Bodies, result.Shape.Body ) != -1 )
								continue;
							length = result.Distance;
							break;
						}

						heightOffset = length - typeGroundHeight;

						//correct by min max range
						if( heightOffset < 0 )
							heightOffset = 0;
						if( heightOffset > maxHeightOffset )
							heightOffset = maxHeightOffset;
					}

					Quat rotation = Mat3.FromRotateByY( wheel.rotationAngle ).ToQuat();

					if( wheelBone != null )
					{
						wheelBone.ManuallyControlled = true;
						wheelBone.Position = sourceWheelBone.Position - new Vec3( 0, 0, heightOffset );
						wheelBone.Rotation = rotation * sourceWheelBone.Rotation;
					}

					if( trackBone != null )
					{
						trackBone.ManuallyControlled = true;
						trackBone.Position = sourceTrackBone.Position - new Vec3( 0, 0, heightOffset );
					}
				}
			}
		}

		void TickTurnOver()
		{
			if( Rotation.GetUp().Z < .2f )
				Die();
		}

		//Minefield specific
		void TickMinefields()
		{
			minefieldUpdateTimer -= TickDelta;
			if( minefieldUpdateTimer > 0 )
				return;
			minefieldUpdateTimer += 1;

			if( chassisBody != null && chassisBody.LinearVelocity != Vec3.Zero )
			{
				Minefield minefield = Minefield.GetMinefieldByPosition( Position );
				if( minefield != null )
				{
					Die();
				}
			}
		}

		protected override void Server_OnClientConnectedAfterPostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedAfterPostCreate( remoteEntityWorld );

			RemoteEntityWorld[] worlds = new RemoteEntityWorld[] { remoteEntityWorld };
			Server_SendTowerLocalDirectionToClients( worlds );
			Server_SendTracksSpeedToClients( worlds );
		}

		void Server_TickSendTowerLocalDirection()
		{
			float epsilon = new Degree( .5f ).InRadians();
			if( !towerLocalDirection.Equals( server_sentTowerLocalDirection, epsilon ) )
			{
				Server_SendTowerLocalDirectionToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				server_sentTowerLocalDirection = towerLocalDirection;
			}
		}

		void Server_SendTowerLocalDirectionToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Tank ),
				(ushort)NetworkMessages.TowerLocalDirectionToClient );
			writer.Write( towerLocalDirection );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.TowerLocalDirectionToClient )]
		void Client_ReceiveTowerLocalDirection( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			SphereDir value = reader.ReadSphereDir();
			if( !reader.Complete() )
				return;
			towerLocalDirection = value;
		}

		void Server_SendTracksSpeedToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			const float epsilon = .25f;

			bool leftUpdate = Math.Abs( leftTrack.speed - leftTrack.server_sentSpeed ) > epsilon ||
				( leftTrack.speed == 0 && leftTrack.server_sentSpeed != 0 );
			bool rightUpdate = Math.Abs( rightTrack.speed - rightTrack.server_sentSpeed ) > epsilon ||
				( rightTrack.speed == 0 && rightTrack.server_sentSpeed != 0 );

			if( leftUpdate || rightUpdate )
			{
				SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Tank ),
					(ushort)NetworkMessages.TracksSpeedToClient );
				writer.Write( leftTrack.speed );
				writer.Write( rightTrack.speed );
				EndNetworkMessage();

				leftTrack.server_sentSpeed = leftTrack.speed;
				rightTrack.server_sentSpeed = rightTrack.speed;
			}
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.TracksSpeedToClient )]
		void Client_ReceiveTracksSpeed( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value1 = reader.ReadSingle();
			float value2 = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			leftTrack.speed = value1;
			rightTrack.speed = value2;
		}

		public override void GetFirstPersonCameraPosition( out Vec3 position, out Vec3 forward, out Vec3 up )
		{
			position = mainGun.GetInterpolatedPosition() + Type.FPSCameraOffset * mainGun.GetInterpolatedRotation();
			if( PlayerIntellect.Instance != null )
				forward = PlayerIntellect.Instance.LookDirection.GetVector();
			else
				forward = Vec3.XAxis;
			up = Vec3.ZAxis;
		}
	}
}
