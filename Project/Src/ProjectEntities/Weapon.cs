// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.MathEx;
using Engine.Utils;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.SoundSystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Weapon"/> entity type.
	/// </summary>
	public abstract class WeaponType : DynamicType
	{
		protected WeaponMode weaponNormalMode;
		protected WeaponMode weaponAlternativeMode;

		[FieldSerialize]
		string characterBoneSlot = "";
		[FieldSerialize]
		string characterBoneSlotFirstPersonCamera = "";

		[FieldSerialize]
		Vec3 fpsCameraAttachPosition;

		///////////////////////////////////////////

		public abstract class WeaponMode
		{
			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float betweenFireTime = 0;

			[FieldSerialize]
			string soundFire;

			[FieldSerialize]
			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			Vec3 startOffsetPosition;

			[FieldSerialize]
			[DefaultValue( typeof( Quat ), "0 0 0" )]
			Quat startOffsetRotation = Quat.Identity;

			[FieldSerialize]
			[DefaultValue( typeof( Vec2 ), "0 0" )]
			Range useDistanceRange;

			[FieldSerialize]
			List<float> fireTimes = new List<float>();

			[FieldSerialize]
			string fireAnimationTrigger = "fire";

			[DefaultValue( 0.0f )]
			public float BetweenFireTime
			{
				get { return betweenFireTime; }
				set { betweenFireTime = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			[SupportRelativePath]
			public string SoundFire
			{
				get { return soundFire; }
				set { soundFire = value; }
			}

			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			public Vec3 StartOffsetPosition
			{
				get { return startOffsetPosition; }
				set { startOffsetPosition = value; }
			}

			[DefaultValue( typeof( Quat ), "0 0 0" )]
			public Quat StartOffsetRotation
			{
				get { return startOffsetRotation; }
				set { startOffsetRotation = value; }
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			public Range UseDistanceRange
			{
				get { return useDistanceRange; }
				set { useDistanceRange = value; }
			}

			public List<float> FireTimes
			{
				get { return fireTimes; }
			}

			[DefaultValue( "fire" )]
			public string FireAnimationTrigger
			{
				get { return fireAnimationTrigger; }
				set { fireAnimationTrigger = value; }
			}

			[Browsable( false )]
			public abstract bool IsInitialized
			{
				get;
			}
		}

		///////////////////////////////////////////

		[Browsable( false )]
		public WeaponMode WeaponNormalMode
		{
			get { return weaponNormalMode; }
		}

		[Browsable( false )]
		public WeaponMode WeaponAlternativeMode
		{
			get { return weaponAlternativeMode; }
		}

		public string CharacterBoneSlot
		{
			get { return characterBoneSlot; }
			set { characterBoneSlot = value; }
		}

		public string CharacterBoneSlotFirstPersonCamera
		{
			get { return characterBoneSlotFirstPersonCamera; }
			set { characterBoneSlotFirstPersonCamera = value; }
		}

		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public Vec3 FPSCameraAttachPosition
		{
			get { return fpsCameraAttachPosition; }
			set { fpsCameraAttachPosition = value; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//it is not known how will be used this sound (2D or 3D?).
			//Sound will preloaded as 3D only here.
			if( weaponNormalMode != null )
				PreloadSound( weaponNormalMode.SoundFire, SoundMode.Mode3D );
			if( weaponAlternativeMode != null )
				PreloadSound( weaponAlternativeMode.SoundFire, SoundMode.Mode3D );
		}
	}

	/// <summary>
	/// Defines the weapons. Both hand-held by characters or guns established on turret are weapons.
	/// </summary>
	public abstract class Weapon : Dynamic
	{
		MapObjectAttachedMesh mainMeshObject;

		bool setForceFireRotation;
		Quat forceFireRotation;

		//

		WeaponType _type = null; public new WeaponType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			//get mainMeshObject
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMesh attachedMeshObject = attachedObject as MapObjectAttachedMesh;
				if( attachedMeshObject != null )
				{
					if( mainMeshObject == null )
						mainMeshObject = attachedMeshObject;
				}
			}

			if( EntitySystemWorld.Instance.WorldSimulationType != WorldSimulationTypes.Editor )
				UpdateTPSFPSCameraAttachedObjectsVisibility( false );

			SubscribeToTickEvent();
		}

		[Browsable( false )]
		abstract public bool Ready
		{
			get;
		}

		public delegate void PreFireDelegate( Weapon entity, bool alternative );
		public event PreFireDelegate PreFire;

		protected void DoPreFireEvent( bool alternative )
		{
			if( PreFire != null )
				PreFire( this, alternative );
		}

		public abstract bool TryFire( bool alternative );

		public void SetForceFireRotationLookTo( Vec3 lookTo )
		{
			setForceFireRotation = true;

			Vec3 diff = lookTo - Position;
			//Vec3 diff = lookTo - GetFirePosition( false );

			forceFireRotation = Quat.FromDirectionZAxisUp( diff );
		}

		public void ResetForceFireRotationLookTo()
		{
			setForceFireRotation = false;
		}

		public abstract Quat GetFireRotation( bool alternative );

		protected Quat GetFireRotation( WeaponType.WeaponMode typeMode )
		{
			Quat rot = setForceFireRotation ? forceFireRotation : Rotation;
			return rot * typeMode.StartOffsetRotation;
		}

		public abstract Vec3 GetFirePosition( bool alternative );

		protected Vec3 GetFirePosition( WeaponType.WeaponMode typeMode )
		{
			Quat rot = setForceFireRotation ? forceFireRotation : Rotation;
			return Position + rot * typeMode.StartOffsetPosition;
		}

		public void UpdateTPSFPSCameraAttachedObjectsVisibility( bool enableFPS )
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				bool tps = string.Compare( attachedObject.Alias, "tpsCamera", true ) == 0;
				bool fps = string.Compare( attachedObject.Alias, "fpsCamera", true ) == 0;

				if( tps )
					attachedObject.Visible = !enableFPS;
				if( fps )
					attachedObject.Visible = enableFPS;
			}
		}

	}
}
