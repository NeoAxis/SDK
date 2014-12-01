// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="PhysicalStream"/> entity type.
	/// </summary>
	public class PhysicalStreamType : DynamicType
	{
		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float length;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float thickness;

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class Mode
		{
			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float damagePerSecond;

			[FieldSerialize]
			InfluenceType influenceType;

			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float influenceTimePerSecond;

			[FieldSerialize]
			[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
			string particleName;

			//

			[DefaultValue( 0.0f )]
			public float DamagePerSecond
			{
				get { return damagePerSecond; }
				set { damagePerSecond = value; }
			}

			[RefreshProperties( RefreshProperties.Repaint )]
			public InfluenceType InfluenceType
			{
				get { return influenceType; }
				set { influenceType = value; }
			}

			[DefaultValue( 0.0f )]
			public float InfluenceTimePerSecond
			{
				get { return influenceTimePerSecond; }
				set { influenceTimePerSecond = value; }
			}

			[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public string ParticleName
			{
				get { return particleName; }
				set { particleName = value; }
			}

			public override string ToString()
			{
				string text = "";

				if( influenceType != null )
				{
					if( text != "" )
						text += ", ";
					text += "Influence: " + influenceType.Name;
				}

				if( !string.IsNullOrEmpty( particleName ) )
				{
					if( text != "" )
						text += ", ";
					text += "Particle: " + particleName;
				}

				return text;
			}
		}

		[FieldSerialize]
		Mode normalMode = new Mode();
		[FieldSerialize]
		Mode alternativeMode = new Mode();

		[DefaultValue( 0.0f )]
		public float Length
		{
			get { return length; }
			set { length = value; }
		}

		[DefaultValue( 0.0f )]
		public float Thickness
		{
			get { return thickness; }
			set { thickness = value; }
		}

		public Mode NormalMode
		{
			get { return normalMode; }
		}

		public Mode AlternativeMode
		{
			get { return alternativeMode; }
		}
	}

	/// <summary>
	/// Defines the physics streams. You can create steam, fiery streams, etc.
	/// </summary>
	public class PhysicalStream : Dynamic
	{
		[FieldSerialize]
		bool alternativeMode;

		[FieldSerialize]
		float throttle;

		InfluenceRegion region;

		PhysicalStreamType _type = null; public new PhysicalStreamType Type { get { return _type; } }

		MapObjectAttachedParticle modeAttachedParticle;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			ThrottleToClient,
			AlternativeModeToClient,
		}

		///////////////////////////////////////////

		[DefaultValue( 0.0f )]
		[LogicSystemBrowsable( true )]
		public float Throttle
		{
			get { return throttle; }
			set
			{
				if( throttle == value )
					return;

				if( value < 0 || value > 1 )
					throw new InvalidOperationException( "Throttle need in [0,1] interval" );
				throttle = value;

				UpdateTransform();

				if( EntitySystemWorld.Instance.IsServer() &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Server_SendThrottleToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				//region
				const string regionTypeName = "ManualInfluenceRegion";

				InfluenceRegionType regionType = (InfluenceRegionType)EntityTypes.Instance.GetByName(
					regionTypeName );
				if( regionType == null )
				{
					regionType = (InfluenceRegionType)EntityTypes.Instance.ManualCreateType(
						regionTypeName,
						EntityTypes.Instance.GetClassInfoByEntityClassName( "InfluenceRegion" ) );
					regionType.NetworkType = EntityNetworkTypes.ServerOnly;
				}

				if( Type.Thickness != 0 && Type.Length != 0 )
				{
					region = (InfluenceRegion)Entities.Instance.Create( regionType, Map.Instance );
					region.ShapeType = Region.ShapeTypes.Box;
					UpdateTransform();
					region.PostCreate();
					region.AllowSave = false;
					region.EditorSelectable = false;
				}
			}

			UpdateMode();

			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( region != null )
			{
				region.SetForDeletion( true );
				region = null;
			}
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );
			if( region != null )
				UpdateTransform();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( region != null )
				region.Force = throttle;
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			UpdateAttachedParticleSystem();
		}

		void UpdateAttachedParticleSystem()
		{
			float factor = Throttle;

			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedParticle attachedParticleObject = attachedObject as MapObjectAttachedParticle;
				if( attachedParticleObject != null )
				{
					ParticleSystem particleSystem = attachedParticleObject.ParticleSystem;
					ParticleSystem sourceParticleSystem = attachedParticleObject.SourceParticleSystem;

					if( particleSystem != null && sourceParticleSystem != null )
					{
						for( int n = 0; n < particleSystem.Emitters.Length; n++ )
						{
							ParticleEmitter emitter = particleSystem.Emitters[ n ];
							ParticleEmitter sourceEmitter = sourceParticleSystem.Emitters[ n ];

							emitter.ParticleVelocity = factor * sourceEmitter.ParticleVelocity;

							if( factor > 0.05f )
								emitter.EmissionRate = factor * sourceEmitter.EmissionRate;
							else
								emitter.EmissionRate = 0;
						}
					}
				}
			}
		}

		void UpdateTransform()
		{
			if( region == null )
				return;

			float len = Type.Length * throttle;

			Vec3 size = new Vec3( len, Type.Thickness, Type.Thickness );
			region.SetTransform( Position + Rotation * new Vec3( size.X * .5f, 0, 0 ), Rotation, size );
		}

		void UpdateMode()
		{
			PhysicalStreamType.Mode mode = alternativeMode ? Type.AlternativeMode : Type.NormalMode;

			if( region != null )
			{
				region.DamagePerSecond = mode.DamagePerSecond;
				region.InfluenceType = mode.InfluenceType;
				region.InfluenceTimePerSecond = mode.InfluenceTimePerSecond;
			}

			if( modeAttachedParticle != null )
			{
				Detach( modeAttachedParticle );
				modeAttachedParticle = null;
			}

			if( !string.IsNullOrEmpty( mode.ParticleName ) )
			{
				modeAttachedParticle = new MapObjectAttachedParticle();
				modeAttachedParticle.ParticleName = mode.ParticleName;
				modeAttachedParticle.OwnerRotation = true;
				Attach( modeAttachedParticle );
			}
		}

		[DefaultValue( false )]
		[LogicSystemBrowsable( true )]
		public bool AlternativeMode
		{
			get { return alternativeMode; }
			set
			{
				if( alternativeMode == value )
					return;
				alternativeMode = value;
				UpdateMode();

				if( EntitySystemWorld.Instance.IsServer() &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Server_SendAlternativeModeToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		protected override void Server_OnClientConnectedAfterPostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedAfterPostCreate( remoteEntityWorld );

			RemoteEntityWorld[] worlds = new RemoteEntityWorld[] { remoteEntityWorld };
			Server_SendThrottleToClients( worlds );
			Server_SendAlternativeModeToClients( worlds );
		}

		void Server_SendThrottleToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PhysicalStream ),
				(ushort)NetworkMessages.ThrottleToClient );
			writer.Write( Throttle );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ThrottleToClient )]
		void Client_ReceiveThrottle( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			Throttle = value;
		}

		void Server_SendAlternativeModeToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PhysicalStream ),
				(ushort)NetworkMessages.AlternativeModeToClient );
			writer.Write( AlternativeMode );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.AlternativeModeToClient )]
		void Client_ReceiveAlternativeMode( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool value = reader.ReadBoolean();
			if( !reader.Complete() )
				return;
			AlternativeMode = value;
		}

	}
}
