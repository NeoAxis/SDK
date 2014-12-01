// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Engine;
using Engine.Networking;
using Engine.Utils;

namespace ProjectCommon
{
	////////////////////////////////////////////////////////////////////////////////////////////////

	public class UserManagementServerNetworkService : ServerNetworkService
	{
		Dictionary<uint, UserInfo> usersByIdentifier = new Dictionary<uint, UserInfo>();
		Dictionary<NetworkNode.ConnectedNode, UserInfo> usersByConnectedNode =
			new Dictionary<NetworkNode.ConnectedNode, UserInfo>();
		UserInfo serverUser;

		///////////////////////////////////////////

		public class UserInfo
		{
			uint identifier;
			string name;
			NetworkNode.ConnectedNode connectedNode;

			//

			internal UserInfo( uint identifier, string name, NetworkNode.ConnectedNode connectedNode )
			{
				this.identifier = identifier;
				this.name = name;
				this.connectedNode = connectedNode;
			}

			public uint Identifier
			{
				get { return identifier; }
			}

			public string Name
			{
				get { return name; }
			}

			public NetworkNode.ConnectedNode ConnectedNode
			{
				get { return connectedNode; }
			}

			public override string ToString()
			{
				string ipAddressText;
				if( connectedNode != null )
				{
					IPAddress ipAddress = IPAddress.None;
					if( connectedNode.RemoteEndPoint != null )
						ipAddress = connectedNode.RemoteEndPoint.Address;
					ipAddressText = ipAddress.ToString();
				}
				else
					ipAddressText = "Local";
				return string.Format( "{0} ({1})", name, ipAddressText );
			}
		}

		///////////////////////////////////////////

		public delegate void AddRemoveUserDelegate( UserManagementServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo user );
		public event AddRemoveUserDelegate AddUserEvent;
		public event AddRemoveUserDelegate RemoveUserEvent;

		///////////////////////////////////////////

		public UserManagementServerNetworkService()
			: base( "UserManagement", 1 )
		{
			//register message types
			RegisterMessageType( "addUserToClient", 1 );
			RegisterMessageType( "removeUserToClient", 2 );
		}

		protected override void Dispose()
		{
			while( usersByIdentifier.Count != 0 )
			{
				Dictionary<uint, UserInfo>.Enumerator enumerator = usersByIdentifier.GetEnumerator();
				enumerator.MoveNext();
				RemoveUser( enumerator.Current.Value );
			}

			base.Dispose();
		}

		public ICollection<UserInfo> Users
		{
			get { return usersByIdentifier.Values; }
		}

		public UserInfo GetUser( uint identifier )
		{
			UserInfo user;
			if( !usersByIdentifier.TryGetValue( identifier, out user ) )
				return null;
			return user;
		}

		public UserInfo ServerUser
		{
			get { return serverUser; }
		}

		public UserInfo GetUser( NetworkNode.ConnectedNode connectedNode )
		{
			UserInfo user;
			if( !usersByConnectedNode.TryGetValue( connectedNode, out user ) )
				return null;
			return user;
		}

		uint GetFreeUserIdentifier()
		{
			uint identifier = 1;
			while( usersByIdentifier.ContainsKey( identifier ) )
				identifier++;
			return identifier;
		}

		UserInfo CreateUser( string name, NetworkNode.ConnectedNode connectedNode )
		{
			uint identifier = GetFreeUserIdentifier();
			UserInfo newUser = new UserInfo( identifier, name, connectedNode );
			usersByIdentifier.Add( identifier, newUser );
			if( newUser.ConnectedNode != null )
				usersByConnectedNode.Add( newUser.ConnectedNode, newUser );

			{
				MessageType messageType = GetMessageType( "addUserToClient" );

				//send event about new user to the all users
				foreach( UserInfo user in Users )
				{
					if( user.ConnectedNode != null )
					{
						bool thisUserFlag = user == newUser;

						SendDataWriter writer = BeginMessage( user.ConnectedNode, messageType );
						writer.WriteVariableUInt32( newUser.Identifier );
						writer.Write( newUser.Name );
						writer.Write( thisUserFlag );
						EndMessage();
					}
				}

				if( newUser.ConnectedNode != null )
				{
					//send list of users to new user
					foreach( UserInfo user in Users )
					{
						if( user == newUser )
							continue;
						SendDataWriter writer = BeginMessage( newUser.ConnectedNode, messageType );
						writer.WriteVariableUInt32( user.Identifier );
						writer.Write( user.Name );
						writer.Write( false );//this user flag
						EndMessage();
					}
				}
			}

			if( AddUserEvent != null )
				AddUserEvent( this, newUser );

			return newUser;
		}

		public UserInfo CreateClientUser( NetworkNode.ConnectedNode connectedNode )
		{
			return CreateUser( connectedNode.LoginName, connectedNode );
		}

		public UserInfo CreateServerUser( string name )
		{
			if( serverUser != null )
			{
				Log.Fatal( "UserManagementServerNetworkService: CreateServerUser: " +
					"Server user is already created." );
			}

			serverUser = CreateUser( name, null );
			return serverUser;
		}

		public void RemoveUser( UserInfo user )
		{
			//already removed?
			if( !usersByIdentifier.ContainsValue( user ) )
				return;

			if( RemoveUserEvent != null )
				RemoveUserEvent( this, user );

			//remove user
			usersByIdentifier.Remove( user.Identifier );
			if( user.ConnectedNode != null )
				usersByConnectedNode.Remove( user.ConnectedNode );
			if( serverUser == user )
				serverUser = null;

			//send event to the all users
			{
				MessageType messageType = GetMessageType( "removeUserToClient" );
				foreach( UserInfo toUser in Users )
				{
					if( toUser.ConnectedNode != null )
					{
						SendDataWriter writer = BeginMessage( toUser.ConnectedNode, messageType );
						writer.WriteVariableUInt32( user.Identifier );
						EndMessage();
					}
				}
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class UserManagementClientNetworkService : ClientNetworkService
	{
		//key: user identifier
		Dictionary<uint, UserInfo> users = new Dictionary<uint, UserInfo>();
		UserInfo thisUser;

		///////////////////////////////////////////

		public class UserInfo
		{
			uint identifier;
			string name;

			//

			internal UserInfo( uint identifier, string name )
			{
				this.identifier = identifier;
				this.name = name;
			}

			public uint Identifier
			{
				get { return identifier; }
			}

			public string Name
			{
				get { return name; }
			}

			public override string ToString()
			{
				return name;
			}
		}

		///////////////////////////////////////////

		public delegate void AddRemoveUserDelegate( UserManagementClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo user );
		public event AddRemoveUserDelegate AddUserEvent;
		public event AddRemoveUserDelegate RemoveUserEvent;

		///////////////////////////////////////////

		public UserManagementClientNetworkService()
			: base( "UserManagement", 1 )
		{
			//register message types
			RegisterMessageType( "addUserToClient", 1, ReceiveMessage_AddUserToClient );
			RegisterMessageType( "removeUserToClient", 2, ReceiveMessage_RemoveUserToClient );
		}

		protected override void Dispose()
		{
			while( users.Count != 0 )
			{
				Dictionary<uint, UserInfo>.Enumerator enumerator = users.GetEnumerator();
				enumerator.MoveNext();
				RemoveUser( enumerator.Current.Value );
			}

			base.Dispose();
		}

		public ICollection<UserInfo> Users
		{
			get { return users.Values; }
		}

		public UserInfo GetUser( uint identifier )
		{
			UserInfo user;
			if( !users.TryGetValue( identifier, out user ) )
				return null;
			return user;
		}

		bool ReceiveMessage_AddUserToClient( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			//get data from message
			uint identifier = reader.ReadVariableUInt32();
			string name = reader.ReadString();
			bool thisUserFlag = reader.ReadBoolean();
			if( !reader.Complete() )
				return false;

			AddUser( identifier, name, thisUserFlag );

			return true;
		}

		bool ReceiveMessage_RemoveUserToClient( NetworkNode.ConnectedNode sender,
			MessageType messageType, ReceiveDataReader reader, ref string additionalErrorMessage )
		{
			//get data from message
			uint identifier = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return false;

			UserInfo user;
			users.TryGetValue( identifier, out user );

			if( user != null )
				RemoveUser( user );

			return true;
		}

		void AddUser( uint identifier, string name, bool thisUserFlag )
		{
			UserInfo user = new UserInfo( identifier, name );
			users.Add( identifier, user );

			if( thisUserFlag )
				thisUser = user;

			if( AddUserEvent != null )
				AddUserEvent( this, user );
		}

		void RemoveUser( UserInfo user )
		{
			if( RemoveUserEvent != null )
				RemoveUserEvent( this, user );

			users.Remove( user.Identifier );

			if( thisUser == user )
				thisUser = null;
		}

		public UserInfo ThisUser
		{
			get { return thisUser; }
		}

	}

}
