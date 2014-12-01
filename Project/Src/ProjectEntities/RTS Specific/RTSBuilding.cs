// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.PhysicsSystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="RTSBuilding"/> entity type.
	/// </summary>
	public class RTSBuildingType : RTSUnitType
	{

	}

	public class RTSBuilding : RTSUnit
	{
		[FieldSerialize]
		RTSUnitType productUnitType;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float productUnitProgress;

		MapObjectAttachedMesh productUnitAttachedMesh;

		[DefaultValue( 1.0f )]
		[FieldSerialize]
		float buildedProgress = 1;

		//

		RTSBuildingType _type = null; public new RTSBuildingType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();

			//for world load/save
			if( productUnitType != null )
				CreateProductUnitAttachedMesh();

			UpdateAttachedObjectsVisibility();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();
			TickProductUnit();
		}

		void TickProductUnit()
		{
			if( productUnitType == null )
				return;

			productUnitProgress += TickDelta / productUnitType.BuildTime;

			Degree angleDelta = TickDelta * 20;

			if( productUnitAttachedMesh != null )
				productUnitAttachedMesh.RotationOffset *= new Angles( 0, 0, angleDelta ).ToQuat();

			if( BuildUnitProgress >= 1 )
			{
				CreateProductedUnit();
				StopProductUnit();
			}

			MapObjectAttachedObject buildPlatformMesh = GetFirstAttachedObjectByAlias( "buildPlatform" );
			if( buildPlatformMesh != null )
				buildPlatformMesh.RotationOffset *= new Angles( 0, 0, angleDelta ).ToQuat();
		}

		public void StartProductUnit( RTSUnitType unitType )
		{
			StopProductUnit();

			//check cost
			RTSFactionManager.FactionItem factionItem = RTSFactionManager.Instance.
				GetFactionItemByType( Intellect.Faction );
			if( factionItem != null )
			{
				float cost = unitType.BuildCost;

				if( factionItem.Money - cost < 0 )
					return;

				factionItem.Money -= cost;
			}

			productUnitType = unitType;
			productUnitProgress = 0;

			CreateProductUnitAttachedMesh();

			UpdateAttachedObjectsVisibility();
		}

		public void StopProductUnit()
		{
			DestroyProductUnitAttachedMesh();

			productUnitType = null;
			productUnitProgress = 0;

			UpdateAttachedObjectsVisibility();
		}

		void CreateProductUnitAttachedMesh()
		{
			productUnitAttachedMesh = new MapObjectAttachedMesh();
			Attach( productUnitAttachedMesh );

			string meshName = null;
			Vec3 meshOffset = Vec3.Zero;
			Vec3 meshScale = new Vec3( 1, 1, 1 );
			{
				foreach( MapObjectTypeAttachedObject typeAttachedObject in
					productUnitType.AttachedObjects )
				{
					MapObjectTypeAttachedMesh typeAttachedMesh =
						typeAttachedObject as MapObjectTypeAttachedMesh;
					if( typeAttachedMesh == null )
						continue;

					meshName = typeAttachedMesh.GetMeshNameFullPath();
					meshOffset = typeAttachedMesh.Position;
					meshScale = typeAttachedMesh.Scale;
					break;
				}
			}

			productUnitAttachedMesh.MeshName = meshName;

			Vec3 pos = meshOffset;
			{
				MapObjectAttachedObject buildPointAttachedHelper = GetFirstAttachedObjectByAlias( "productUnitPoint" );
				if( buildPointAttachedHelper != null )
					pos += buildPointAttachedHelper.PositionOffset;
			}
			productUnitAttachedMesh.PositionOffset = pos;

			productUnitAttachedMesh.ScaleOffset = meshScale;

			if( Type.Name == "RTSHeadquaters" )
			{
				foreach( MeshObject.SubObject subMesh in productUnitAttachedMesh.MeshObject.SubObjects )
					subMesh.MaterialName = "RTSBuildMaterial";
			}
		}

		void DestroyProductUnitAttachedMesh()
		{
			if( productUnitAttachedMesh != null )
			{
				Detach( productUnitAttachedMesh );
				productUnitAttachedMesh = null;
			}
		}

		[Browsable( false )]
		public RTSUnitType BuildUnitType
		{
			get { return productUnitType; }
		}

		[Browsable( false )]
		public float BuildUnitProgress
		{
			get { return productUnitProgress; }
		}

		void CreateProductedUnit()
		{
			RTSUnit unit = (RTSUnit)Entities.Instance.Create( productUnitType, Map.Instance );

			RTSCharacter character = unit as RTSCharacter;
			if( character == null )
				Log.Fatal( "RTSBuilding: CreateProductedUnit: character == null" );

			GridBasedNavigationSystem navigationSystem = GridBasedNavigationSystem.Instances[ 0 ];
			Vec2 p = navigationSystem.GetNearestFreePosition( Position.ToVec2(), character.Type.Radius * 2 );
			unit.Position = new Vec3( p.X, p.Y, navigationSystem.GetMotionMapHeight( p ) + character.Type.Height * .5f );

			if( Intellect != null )
				unit.InitialFaction = Intellect.Faction;

			unit.PostCreate();
		}

		[DefaultValue( 1.0f )]
		public float BuildedProgress
		{
			get { return buildedProgress; }
			set
			{
				buildedProgress = value;

				UpdateAttachedObjectsVisibility();
			}
		}

		protected override void OnDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			float oldLife = Health;

			base.OnDamage( prejudicial, pos, shape, damage, allowMoveDamageToParent );

			if( damage < 0 && BuildedProgress != 1 )
			{
				BuildedProgress += ( -damage ) / Type.HealthMax;
				if( BuildedProgress > 1 )
					BuildedProgress = 1;

				if( BuildedProgress != 1 && Health == Type.HealthMax )
					Health = Type.HealthMax - .01f;
			}

			float halfLife = Type.HealthMax * .5f;
			if( Health > halfLife && oldLife <= halfLife )
				UpdateAttachedObjectsVisibility();
			else if( Health < halfLife && oldLife >= halfLife )
				UpdateAttachedObjectsVisibility();

			float quarterLife = Type.HealthMax * .25f;
			if( Health > quarterLife && oldLife <= quarterLife )
				UpdateAttachedObjectsVisibility();
			else if( Health < quarterLife && oldLife >= quarterLife )
				UpdateAttachedObjectsVisibility();
		}

		void UpdateAttachedObjectsVisibility()
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				//lessHalfLife
				if( attachedObject.Alias == "lessHalfLife" )
				{
					attachedObject.Visible = ( Health < Type.HealthMax * .5f && buildedProgress == 1 );
					continue;
				}

				//lessQuarterLife
				if( attachedObject.Alias == "lessQuarterLife" )
				{
					attachedObject.Visible = ( Health < Type.HealthMax * .25f && buildedProgress == 1 );
					continue;
				}

				//productUnit
				if( attachedObject.Alias == "productUnit" )
				{
					attachedObject.Visible = productUnitType != null;
					continue;
				}

				//building
				{
					string showAlias = null;

					if( buildedProgress < .25f )
						showAlias = "building0";
					else if( buildedProgress < .5f )
						showAlias = "building1";
					else if( buildedProgress < 1 )
						showAlias = "building2";

					if( showAlias != null )
						attachedObject.Visible = ( attachedObject.Alias == showAlias );
					else
						attachedObject.Visible = !attachedObject.Alias.Contains( "building" );
				}

			}
		}

	}
}
