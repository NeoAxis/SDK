// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="SpawnPoint"/> entity type.
	/// </summary>
	public class SpawnPointType : MapObjectType
	{
	}

	public class SpawnPoint : MapObject
	{
		static List<SpawnPoint> instances = new List<SpawnPoint>();

		[FieldSerialize]
		FactionType faction;

		[FieldSerialize]
		bool defaultPoint;

		static bool noSpawnPointLogInformed;

		//

		SpawnPointType _type = null; public new SpawnPointType Type { get { return _type; } }

		public FactionType Faction
		{
			get { return faction; }
			set { faction = value; }
		}

		[DefaultValue( false )]
		public bool DefaultPoint
		{
			get { return defaultPoint; }
			set { defaultPoint = value; }
		}

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			instances.Add( this );
		}

		protected override void OnDestroy()
		{
			instances.Remove( this );
			base.OnDestroy();
		}

		public static SpawnPoint GetRandomSpawnPoint()
		{
			if( instances.Count == 0 )
				return null;
			return instances[ World.Instance.Random.Next( instances.Count ) ];
		}

		public static SpawnPoint GetDefaultSpawnPoint()
		{
			foreach( SpawnPoint spawnPoint in instances )
				if( spawnPoint.DefaultPoint )
					return spawnPoint;
			return null;
		}

		public static SpawnPoint GetFreeRandomSpawnPoint()
		{
			for( int n = 0; n < 10; n++ )
			{
				SpawnPoint spawnPoint = GetRandomSpawnPoint();

				if( spawnPoint == null )
				{
					if( !noSpawnPointLogInformed )
					{
						Log.Warning( "No spawn points." );
						noSpawnPointLogInformed = true;
					}
					return null;
				}

				bool busy = false;
				{
					Bounds volume = new Bounds( spawnPoint.Position );
					volume.Expand( new Vec3( 1, 1, 2 ) );

					Body[] result = PhysicsWorld.Instance.VolumeCast( volume,
						(int)ContactGroup.CastOnlyContact );

					foreach( Body body in result )
					{
						if( body.Static )
							continue;

						foreach( Shape shape in body.Shapes )
						{
							if( PhysicsWorld.Instance.MainScene.IsContactGroupsContactable( shape.ContactGroup,
								(int)ContactGroup.Dynamic ) )
							{
								busy = true;
								break;
							}
						}
						if( busy )
							break;
					}
				}

				if( !busy )
					return spawnPoint;
			}
			return null;
		}
	}
}
