// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="FloatSwitch"/> entity type.
	/// </summary>
	public class FloatSwitchType : SwitchType
	{
		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float changeVelocity;

		[DefaultValue( 1.0f )]
		public float ChangeVelocity
		{
			get { return changeVelocity; }
			set { changeVelocity = value; }
		}
	}

	/// <summary>
	/// Defines the user quantitative switches.
	/// </summary>
	public class FloatSwitch : Switch
	{
		[FieldSerialize]
		float value;

		bool use;
		bool useChangeIncrease;

		FloatSwitchType _type = null; public new FloatSwitchType Type { get { return _type; } }

		///////////////////////////////////////////

		enum NetworkMessages
		{
			ValueToClient,
			UseStartToServer,
			UseEndToServer,
		}

		///////////////////////////////////////////

		[DefaultValue( 0.0f )]
		[LogicSystemBrowsable( true )]
		public float Value
		{
			get { return this.value; }
			set
			{
				if( this.value == value )
					return;

				if( value < 0 || value > 1 )
					throw new Exception( "Invalid Value Range" );

				this.value = value;

				OnValueChange();

				if( EntitySystemWorld.Instance.IsServer() )
				{
					if( Type.NetworkType == EntityNetworkTypes.Synchronized )
						Server_SendValueToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
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

			if( use )
			{
				float step = Type.ChangeVelocity * TickDelta;

				if( useChangeIncrease )
				{
					float newValue = value + step;
					if( newValue > 1 )
						newValue = 1;
					Value = newValue;

					if( value == 1 )
						use = false;
				}
				else
				{
					float newValue = value - step;
					if( newValue < 0 )
						newValue = 0;
					Value = newValue;

					if( value == 0 )
						use = false;
				}
			}
		}

		public void UseStart()
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( use )
					return;

				use = true;
				useChangeIncrease = !useChangeIncrease;
				if( useChangeIncrease && value == 1 )
					useChangeIncrease = false;
				if( !useChangeIncrease && value == 0 )
					useChangeIncrease = true;
			}
			else
			{
				//client. send message to server.
				SendDataWriter writer = BeginNetworkMessage( typeof( FloatSwitch ),
					(ushort)NetworkMessages.UseStartToServer );
				EndNetworkMessage();
			}
		}

		public void UseEnd()
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				use = false;
			}
			else
			{
				//client. send message to server.
				SendDataWriter writer = BeginNetworkMessage( typeof( FloatSwitch ),
					(ushort)NetworkMessages.UseEndToServer );
				EndNetworkMessage();
			}
		}

		void Server_SendValueToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( FloatSwitch ),
				(ushort)NetworkMessages.ValueToClient );
			writer.Write( Value );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ValueToClient )]
		void Client_ReceiveValue( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			float value = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			Value = value;
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendValueToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.UseStartToServer )]
		void Server_ReceiveUseStart( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//not safe. every player from any place can to send this message.
			if( !reader.Complete() )
				return;
			UseStart();
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.UseEndToServer )]
		void Server_ReceiveUseEnd( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//not safe. every player from any place can to send this message.
			if( !reader.Complete() )
				return;
			UseEnd();
		}

	}
}
