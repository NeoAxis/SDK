// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="RTSUnit"/> entity type.
	/// </summary>
	public class RTSUnitType : UnitType
	{
		[FieldSerialize]
		[DefaultValue( typeof( Range ), "0 0" )]
		Range optimalAttackDistanceRange;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float buildCost;

		[FieldSerialize]
		[DefaultValue( 10.0f )]
		float buildTime = 10;

		//

		[DefaultValue( typeof( Range ), "0 0" )]
		public Range OptimalAttackDistanceRange
		{
			get { return optimalAttackDistanceRange; }
			set { optimalAttackDistanceRange = value; }
		}

		[DefaultValue( 0.0f )]
		public float BuildCost
		{
			get { return buildCost; }
			set { buildCost = value; }
		}

		[DefaultValue( 10.0f )]
		public float BuildTime
		{
			get { return buildTime; }
			set { buildTime = value; }
		}
	}

	public class RTSUnit : Unit
	{
		[FieldSerialize]
		[DefaultValue( false )]
		bool moveEnabled;

		[FieldSerialize]
		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		Vec3 movePosition;

		//

		RTSUnitType _type = null; public new RTSUnitType Type { get { return _type; } }

		public void Stop()
		{
			moveEnabled = false;
			movePosition = Vec3.Zero;
		}

		public void Move( Vec3 pos )
		{
			moveEnabled = true;
			movePosition = pos;
		}

		[Browsable( false )]
		protected bool MoveEnabled
		{
			get { return moveEnabled; }
		}

		[Browsable( false )]
		protected Vec3 MovePosition
		{
			get { return movePosition; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			UpdateSkin();
		}

		public override MapObjectCreateObjectCollection.CreateObjectsResultItem[] DieObjects_Create()
		{
			MapObjectCreateObjectCollection.CreateObjectsResultItem[] result = base.DieObjects_Create();

			//paint created corpses of units to faction color
			foreach( MapObjectCreateObjectCollection.CreateObjectsResultItem item in result )
			{
				MapObjectCreateMapObject createMapObject = item.Source as MapObjectCreateMapObject;
				if( createMapObject != null )
				{
					foreach( MapObject mapObject in item.CreatedObjects )
					{
						//Corpse copy forceMaterial to meshes
						if( mapObject is Corpse && InitialFaction != null )
						{
							bool badFaction = InitialFaction.Name == "BadFaction";

							if( Type.Name == "RTSRobot" )
							{
								( mapObject.AttachedObjects[ 0 ] as MapObjectAttachedMesh ).MeshObject.
									SubObjects[ 0 ].MaterialName = badFaction ? "Robot2" : "Robot";
							}
							else if( Type.Name == "RTSConstructor" )
							{
								( mapObject.AttachedObjects[ 0 ] as MapObjectAttachedMesh ).MeshObject.
									SubObjects[ 0 ].MaterialName = badFaction ? "Red" : "Blue";
							}
						}
					}
				}
			}

			return result;
		}

		public override FactionType InitialFaction
		{
			get { return base.InitialFaction; }
			set
			{
				base.InitialFaction = value;
				if( IsPostCreated )
					UpdateSkin();
			}
		}

		void UpdateSkin()
		{
			if( InitialFaction == null )
				return;

			//!!!!!!temp. not universal

			bool badFaction = InitialFaction.Name == "BadFaction";

			if( Type.Name == "RTSRobot" )
			{
				( AttachedObjects[ 0 ] as MapObjectAttachedMesh ).MeshObject.
					SubObjects[ 0 ].MaterialName = badFaction ? "Robot2" : "Robot";
			}
			else if( Type.Name == "RTSConstructor" )
			{
				( AttachedObjects[ 0 ] as MapObjectAttachedMesh ).MeshObject.
					SubObjects[ 0 ].MaterialName = badFaction ? "Red" : "Blue";
			}
			else if( Type.Name == "RTSMine" || Type.Name == "RTSHeadquaters" )
			{
				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				{
					MapObjectAttachedMesh meshAttachedObject = attachedObject as MapObjectAttachedMesh;
					if( meshAttachedObject != null )
					{
						MapObjectTypeAttachedMesh typeObject = (MapObjectTypeAttachedMesh)meshAttachedObject.TypeObject;

						if( typeObject.ForceMaterial == "" )
						{
							meshAttachedObject.MeshObject.SetMaterialNameForAllSubObjects(
								badFaction ? ( Type.Name + "2" ) : Type.Name );
						}
					}
				}
			}
			else if( Type.Name == "RTSFactory" )
			{
				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				{
					MapObjectAttachedMesh meshAttachedObject = attachedObject as MapObjectAttachedMesh;
					if( meshAttachedObject != null )
					{
						MapObjectTypeAttachedMesh typeObject = (MapObjectTypeAttachedMesh)meshAttachedObject.TypeObject;

						if( typeObject.ForceMaterial == "" )
						{
							meshAttachedObject.MeshObject.SetMaterialNameForAllSubObjects(
								badFaction ? ( Type.Name + "2" ) : Type.Name );
							break;
						}
					}
				}
			}
		}

	}
}
