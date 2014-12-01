// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Engine;
using Engine.Networking;

namespace ProjectCommon
{
	public class GameNetworkServer : NetworkServer
	{
		static GameNetworkServer instance;

		bool allowToConnectNewClients = true;

		//services
		UserManagementServerNetworkService userManagementService;
		CustomMessagesServerNetworkService customMessagesService;
		ChatServerNetworkService chatService;
		EntitySystemServerNetworkService entitySystemService;

		List<ConnectedNode> connectingNodes = new List<ConnectedNode>();

		//

		public GameNetworkServer( string serverName, string serverVersion, int maxConnections,
			bool entitySystemServiceEnabled )
			: base( serverName, serverVersion, maxConnections )
		{
			if( instance != null )
				Log.Fatal( "GameNetworkServer.GameNetworkServer: instance != null." );
			instance = this;

			//register network services

			//register user management service
			userManagementService = new UserManagementServerNetworkService();
			RegisterService( userManagementService );

			//register custom messages service
			customMessagesService = new CustomMessagesServerNetworkService();
			RegisterService( customMessagesService );

			//register chat service
			chatService = new ChatServerNetworkService( userManagementService );
			RegisterService( chatService );

			//register entity system service
			if( entitySystemServiceEnabled )
			{
				entitySystemService = new EntitySystemServerNetworkService( userManagementService );
				RegisterService( entitySystemService );
			}
		}

		public override void Dispose( string reason )
		{
			base.Dispose( reason );

			instance = null;
		}

		public static GameNetworkServer Instance
		{
			get { return instance; }
		}

		protected override bool OnIncomingConnectionApproval( NetworkNode.ConnectedNode connectedNode,
			string clientVersion, string loginName, string password, ref string rejectReason )
		{
			if( !base.OnIncomingConnectionApproval( connectedNode, clientVersion, loginName,
				password, ref rejectReason ) )
				return false;

			if( !AllowToConnectNewClients )
			{
				rejectReason = string.Format(
					"Game is already begun. Game do not support to connect after start." );
				return false;
			}

			//check login and password
			//(use this code for rejection)
			//if(false)
			//{
			//	rejectReason = "Login failed";
			//	return false;
			//}

			return true;
		}

		protected override void OnConnectedNodeConnectionStatusChanged(
			NetworkNode.ConnectedNode connectedNode, NetworkConnectionStatuses status, string message )
		{
			base.OnConnectedNodeConnectionStatusChanged( connectedNode, status, message );

			//connected
			if( status == NetworkConnectionStatuses.Connected )
			{
				//add to user management and send events to all clients
				userManagementService.CreateClientUser( connectedNode );
			}

			//disconnected
			if( status == NetworkConnectionStatuses.Disconnected )
			{
				//remove user
				UserManagementServerNetworkService.UserInfo user = userManagementService.GetUser(
					connectedNode );
				if( user != null )
					userManagementService.RemoveUser( user );
			}
		}

		public UserManagementServerNetworkService UserManagementService
		{
			get { return userManagementService; }
		}

		public CustomMessagesServerNetworkService CustomMessagesService
		{
			get { return customMessagesService; }
		}

		public ChatServerNetworkService ChatService
		{
			get { return chatService; }
		}

		public EntitySystemServerNetworkService EntitySystemService
		{
			get { return entitySystemService; }
		}

		protected override void OnReceiveProtocolError( ConnectedNode sender, string message )
		{
			base.OnReceiveProtocolError( sender, message );

			//get sender ip address
			IPAddress ipAddress = IPAddress.None;
			if( sender.RemoteEndPoint != null )
				ipAddress = sender.RemoteEndPoint.Address;
			string ipAddressText = ipAddress.ToString();

			//use it for debugging
			//Log.Warning( "GameNetworkClient: Protocol error: {0} from \"{1}\".", message, ipAddressText );

			DisconnectConnectedNode( sender, "Protocol error: " + message, 1 );
		}

		public bool AllowToConnectNewClients
		{
			get { return allowToConnectNewClients; }
			set { allowToConnectNewClients = value; }
		}

	}
}
