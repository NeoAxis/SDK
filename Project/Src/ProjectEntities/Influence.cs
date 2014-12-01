// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Influence"/> entity type.
	/// </summary>
	public class InfluenceType : EntityType
	{
		[FieldSerialize]
		string defaultParticleName;

		[FieldSerialize]
		Substance allowSubstance;

		//

		[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
		public string DefaultParticleName
		{
			get { return defaultParticleName; }
			set { defaultParticleName = value; }
		}

		[DefaultValue( Substance.None )]
		public Substance AllowSubstance
		{
			get { return allowSubstance; }
			set { allowSubstance = value; }
		}
	}

	/// <summary>
	/// Influences are effects on objects. For example, the ability to burn a monster, 
	/// is implemented through the use of influences.
	/// </summary>
	public class Influence : Entity
	{
		[FieldSerialize]
		float remainingTime;
		MapObjectAttachedParticle defaultAttachedParticle;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			RemainingTimeToClient,
		}

		///////////////////////////////////////////

		InfluenceType _type = null; public new InfluenceType Type { get { return _type; } }

		public float RemainingTime
		{
			get { return remainingTime; }
			set
			{
				if( remainingTime == value )
					return;

				remainingTime = value;

				if( EntitySystemWorld.Instance.IsServer() &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Server_SendRemainingTimeToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			//We has initialization into OnPostCreate2, because we need to intialize after
			//parent entity initialized (Dynamic.OnPostCreate). It's need for world serialization.

			SubscribeToTickEvent();

			Dynamic parent = (Dynamic)Parent;

			bool existsAttachedObjects = false;

			//show attached objects for this influence
			foreach( MapObjectAttachedObject attachedObject in parent.AttachedObjects )
			{
				if( attachedObject.Alias == Type.Name )
				{
					attachedObject.Visible = true;
					existsAttachedObjects = true;
				}
			}

			if( !existsAttachedObjects )
			{
				//create default particle system
				if( !string.IsNullOrEmpty( Type.DefaultParticleName ) )
				{
					defaultAttachedParticle = new MapObjectAttachedParticle();
					defaultAttachedParticle.ParticleName = Type.DefaultParticleName;
					parent.Attach( defaultAttachedParticle );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			Dynamic parent = (Dynamic)Parent;

			//hide attached objects for this influence
			foreach( MapObjectAttachedObject attachedObject in parent.AttachedObjects )
			{
				if( attachedObject.Alias == Type.Name )
					attachedObject.Visible = false;
			}

			//destroy default particle system
			if( defaultAttachedParticle != null )
			{
				parent.Detach( defaultAttachedParticle );
				defaultAttachedParticle = null;
			}

			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			remainingTime -= TickDelta;
			if( remainingTime <= 0 )
			{
				//deletion of object can do only on server
				SetForDeletion( true );
				return;
			}
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();

			remainingTime -= TickDelta;
			if( remainingTime < 0 )
				remainingTime = 0;
		}

		protected override void Server_OnClientConnectedBeforePostCreate( RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendRemainingTimeToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendRemainingTimeToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds,
				typeof( Influence ), (ushort)NetworkMessages.RemainingTimeToClient );
			writer.Write( remainingTime );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.RemainingTimeToClient )]
		void Client_ReceiveRemainingTime( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			RemainingTime = value;
		}

	}
}
