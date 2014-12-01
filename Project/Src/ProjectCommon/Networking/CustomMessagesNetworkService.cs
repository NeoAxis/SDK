// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Networking;
using Engine.Utils;

namespace ProjectCommon
{
	////////////////////////////////////////////////////////////////////////////////////////////////

	public class CustomMessagesServerNetworkService : ServerNetworkService
	{
		MessageType transferMessageType;

		///////////////////////////////////////////

		public delegate void ReceiveMessageDelegate( CustomMessagesServerNetworkService sender,
			NetworkNode.ConnectedNode source, string message, string data );
		public event ReceiveMessageDelegate ReceiveMessage;

		///////////////////////////////////////////

		public CustomMessagesServerNetworkService()
			: base( "CustomMessages", 2 )
		{
			//register message types
			transferMessageType = RegisterMessageType( "transferMessage", 1,
				ReceiveMessage_TransferMessageToServer );
		}

		bool ReceiveMessage_TransferMessageToServer( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string error )
		{
			string message = reader.ReadString();
			string data = reader.ReadString();
			if( !reader.Complete() )
				return false;

			if( ReceiveMessage != null )
				ReceiveMessage( this, sender, message, data );

			return true;
		}

		public void SendToClient( NetworkNode.ConnectedNode connectedNode, string message,
			string data )
		{
			SendDataWriter writer = BeginMessage( connectedNode, transferMessageType );
			writer.Write( message );
			writer.Write( data );
			EndMessage();
		}

		public void SendToAllClients( string message, string data )
		{
			foreach( NetworkNode.ConnectedNode connectedNode in Owner.ConnectedNodes )
				SendToClient( connectedNode, message, data );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class CustomMessagesClientNetworkService : ClientNetworkService
	{
		MessageType transferMessageType;

		///////////////////////////////////////////

		public delegate void ReceiveMessageDelegate( CustomMessagesClientNetworkService sender,
			string message, string data );
		public event ReceiveMessageDelegate ReceiveMessage;

		///////////////////////////////////////////

		public CustomMessagesClientNetworkService()
			: base( "CustomMessages", 2 )
		{
			//register message types
			transferMessageType = RegisterMessageType( "transferMessage", 1,
				ReceiveMessage_TransferMessageToClient );
		}

		public void SendToServer( string message, string data )
		{
			SendDataWriter writer = BeginMessage( transferMessageType );
			writer.Write( message );
			writer.Write( data );
			EndMessage();
		}

		bool ReceiveMessage_TransferMessageToClient( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			string message = reader.ReadString();
			string data = reader.ReadString();
			if( !reader.Complete() )
				return false;

			if( ReceiveMessage != null )
				ReceiveMessage( this, message, data );

			return true;
		}
	}
}
