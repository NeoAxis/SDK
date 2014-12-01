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

	public class ChatServerNetworkService : ServerNetworkService
	{
		UserManagementServerNetworkService userManagementService;

		///////////////////////////////////////////

		public delegate void ReceiveTextDelegate( ChatServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo fromUser, string text, 
			UserManagementServerNetworkService.UserInfo privateToUser );
		public event ReceiveTextDelegate ReceiveText;

		///////////////////////////////////////////

		public ChatServerNetworkService( UserManagementServerNetworkService userManagementService )
			: base( "Chat", 3 )
		{
			this.userManagementService = userManagementService;

			//register message types
			RegisterMessageType( "textToServer", 1, ReceiveMessage_TextToServer );
			RegisterMessageType( "textToClient", 2 );
		}

		bool ReceiveMessage_TextToServer( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			//get source user
			UserManagementServerNetworkService.UserInfo fromUser = userManagementService.
				GetUser( sender );

			//get data of message
			string text = reader.ReadString();
			uint privateToUserIdentifier = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return false;

			//send text to the clients
			if( privateToUserIdentifier != 0 )
			{
				//send text to the specific user

				UserManagementServerNetworkService.UserInfo privateToUser = userManagementService.
					GetUser( privateToUserIdentifier );
				if( privateToUser != null )
				{
					SendText( fromUser, text, privateToUser );
				}
				else
				{
					//no user anymore
				}
			}
			else
			{
				SendText( fromUser, text, null );
			}

			return true;
		}

		public void SayToAll( string text )
		{
			UserManagementServerNetworkService.UserInfo fromUser = userManagementService.ServerUser;
			if( fromUser == null )
				Log.Fatal( "ChatServerNetworkService: Say: Server user is not created." );
			SendText( fromUser, text, null );
		}

		public void SayPrivate( string text, UserManagementServerNetworkService.UserInfo toUser )
		{
			UserManagementServerNetworkService.UserInfo fromUser = userManagementService.ServerUser;
			if( fromUser == null )
				Log.Fatal( "ChatServerNetworkService: Say: Server user is not created." );
			SendText( fromUser, text, toUser );
		}

		void SendText( UserManagementServerNetworkService.UserInfo fromUser,
			string text, UserManagementServerNetworkService.UserInfo privateToUser )
		{
			if( ReceiveText != null )
				ReceiveText( this, fromUser, text, null );

			if( privateToUser != null )
			{
				if( privateToUser.ConnectedNode != null )
					SendTextToClient( privateToUser, fromUser, text );
			}
			else
			{
				foreach( UserManagementServerNetworkService.UserInfo toUser in
					userManagementService.Users )
				{
					if( toUser.ConnectedNode != null )
						SendTextToClient( toUser, fromUser, text );
				}
			}
		}

		void SendTextToClient( UserManagementServerNetworkService.UserInfo toUser,
			UserManagementServerNetworkService.UserInfo fromUser, string text )
		{
			MessageType messageType = GetMessageType( "textToClient" );
			SendDataWriter writer = BeginMessage( toUser.ConnectedNode, messageType );
			writer.WriteVariableUInt32( fromUser.Identifier );
			writer.Write( text );
			EndMessage();
		}

	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class ChatClientNetworkService : ClientNetworkService
	{
		UserManagementClientNetworkService userManagementService;

		///////////////////////////////////////////

		public delegate void ReceiveTextDelegate( ChatClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo fromUser, string text );
		public event ReceiveTextDelegate ReceiveText;

		///////////////////////////////////////////

		public ChatClientNetworkService( UserManagementClientNetworkService userManagementService )
			: base( "Chat", 3 )
		{
			this.userManagementService = userManagementService;

			//register message types
			RegisterMessageType( "textToServer", 1 );
			RegisterMessageType( "textToClient", 2, ReceiveMessage_TextToClient );
		}

		public void SayToAll( string text )
		{
			MessageType messageType = GetMessageType( "textToServer" );
			SendDataWriter writer = BeginMessage( messageType );
			writer.Write( text );
			writer.WriteVariableUInt32( 0 );
			EndMessage();
		}

		public void SayPrivate( string text, UserManagementClientNetworkService.UserInfo toUser )
		{
			MessageType messageType = GetMessageType( "textToServer" );
			SendDataWriter writer = BeginMessage( messageType );
			writer.Write( text );
			writer.WriteVariableUInt32( toUser.Identifier );
			EndMessage();
		}

		bool ReceiveMessage_TextToClient( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			//get data from message
			uint fromUserIdentifier = reader.ReadVariableUInt32();
			string text = reader.ReadString();
			if( !reader.Complete() )
				return false;

			//get user by identifier
			UserManagementClientNetworkService.UserInfo fromUser = userManagementService.GetUser(
				fromUserIdentifier );
			if( fromUser == null )
			{
				//error. no such user.
				return true;
			}

			if( ReceiveText != null )
				ReceiveText( this, fromUser, text );

			return true;
		}
	}
}
