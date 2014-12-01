// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="BooleanSwitch"/> entity type.
	/// </summary>
	public class BooleanSwitchType : SwitchType
	{
		[FieldSerialize]
		[DefaultValue( "True" )]
		string trueValueAttachedAlias = "True";

		[FieldSerialize]
		[DefaultValue( "False" )]
		string falseValueAttachedAlias = "False";

		[DefaultValue( "True" )]
		public string TrueValueAttachedAlias
		{
			get { return trueValueAttachedAlias; }
			set { trueValueAttachedAlias = value; }
		}

		[DefaultValue( "False" )]
		public string FalseValueAttachedAlias
		{
			get { return falseValueAttachedAlias; }
			set { falseValueAttachedAlias = value; }
		}

	}

	/// <summary>
	/// Defines the user boolean switches.
	/// </summary>
	public class BooleanSwitch : Switch
	{
		[FieldSerialize]
		bool value;

		BooleanSwitchType _type = null; public new BooleanSwitchType Type { get { return _type; } }

		///////////////////////////////////////////

		enum NetworkMessages
		{
			ValueToClient,
			PressToServer
		}

		///////////////////////////////////////////

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			UpdateAttachedObjects();
		}

		[DefaultValue( false )]
		[LogicSystemBrowsable( true )]
		public bool Value
		{
			get { return this.value; }
			set
			{
				if( this.value == value )
					return;

				this.value = value;

				OnValueChange();
				UpdateAttachedObjects();

				if( EntitySystemWorld.Instance.IsServer() )
				{
					if( Type.NetworkType == EntityNetworkTypes.Synchronized )
						Server_SendValueToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		void UpdateAttachedObjects()
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				if( attachedObject.Alias == Type.TrueValueAttachedAlias )
					attachedObject.Visible = value;
				else if( attachedObject.Alias == Type.FalseValueAttachedAlias )
					attachedObject.Visible = !value;
			}
		}

		public void Press()
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				Value = !Value;
			}
			else
			{
				//client. send message to server.
				SendDataWriter writer = BeginNetworkMessage( typeof( BooleanSwitch ),
					(ushort)NetworkMessages.PressToServer );
				EndNetworkMessage();
			}
		}

		void Server_SendValueToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( BooleanSwitch ),
				(ushort)NetworkMessages.ValueToClient );
			writer.Write( Value );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ValueToClient )]
		void Client_ReceiveValue( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool value = reader.ReadBoolean();
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

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.PressToServer )]
		void Server_ReceivePress( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//not safe. every player from any place can to send this message.
			if( !reader.Complete() )
				return;
			Press();
		}


	}
}
