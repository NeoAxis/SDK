// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="PlayerManager"/> entity type.
	/// </summary>
	public class PlayerManagerType : EntityType
	{
	}

	public class PlayerManager : Entity
	{
		static PlayerManager instance;

		//server side or single mode
		[FieldSerialize]
		uint serverOrSingle_playerIdentifierCounter;

		[FieldSerialize]
		List<ServerOrSingle_Player> serverOrSingle_players;
		ReadOnlyCollection<ServerOrSingle_Player> serverOrSingle_playersAsReadOnly;

		bool server_shouldUpdateDataToClients;
		float server_updateDataToClientsLastTime;

		//client side
		List<Client_Player> client_players;
		ReadOnlyCollection<Client_Player> client_playersAsReadOnly;

		///////////////////////////////////////////

		public class ServerOrSingle_Player
		{
			[FieldSerialize]
			uint identifier;//used only for network synchronization
			[FieldSerialize]
			string name;
			[FieldSerialize]
			bool bot;
			UserManagementServerNetworkService.UserInfo user;

			[FieldSerialize]
			int frags;

			float ping;

			[FieldSerialize]
			Intellect intellect;

			//for serialization
			public ServerOrSingle_Player()
			{
			}

			public ServerOrSingle_Player( uint identifier, string name, bool bot,
				UserManagementServerNetworkService.UserInfo user )
			{
				this.identifier = identifier;
				this.name = name;
				this.bot = bot;
				this.user = user;
			}

			/// <summary>
			/// used only for network synchronization
			/// </summary>
			public uint Identifier
			{
				get { return identifier; }
			}

			public string Name
			{
				get { return name; }
			}

			public bool Bot
			{
				get { return bot; }
			}

			public UserManagementServerNetworkService.UserInfo User
			{
				get { return user; }
			}

			public int Frags
			{
				get { return frags; }
				set
				{
					frags = value;
					PlayerManager.Instance.server_shouldUpdateDataToClients = true;
				}
			}

			public float Ping
			{
				get { return ping; }
				set
				{
					ping = value;
					PlayerManager.Instance.server_shouldUpdateDataToClients = true;
				}
			}

			public Intellect Intellect
			{
				get { return intellect; }
				set
				{
					if( intellect != null )
						PlayerManager.Instance.UnsubscribeToDeletionEvent( intellect );
					intellect = value;
					if( intellect != null )
						PlayerManager.Instance.SubscribeToDeletionEvent( intellect );
				}
			}

		}

		///////////////////////////////////////////

		public class Client_Player
		{
			uint identifier;
			string name;
			bool bot;
			UserManagementClientNetworkService.UserInfo user;

			int frags;
			float ping;

			public Client_Player( uint identifier, string name, bool bot,
				UserManagementClientNetworkService.UserInfo user )
			{
				this.identifier = identifier;
				this.name = name;
				this.bot = bot;
				this.user = user;
			}

			public uint Identifier
			{
				get { return identifier; }
			}

			public string Name
			{
				get { return name; }
			}

			public bool Bot
			{
				get { return bot; }
			}

			public UserManagementClientNetworkService.UserInfo User
			{
				get { return user; }
			}

			public int Frags
			{
				get { return frags; }
				set { frags = value; }
			}

			public float Ping
			{
				get { return ping; }
				set { ping = value; }
			}
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			AddUserToClient,
			RemoveUserToClient,
			UpdateDataToClient,
		}

		///////////////////////////////////////////

		PlayerManagerType _type = null; public new PlayerManagerType Type { get { return _type; } }

		public PlayerManager()
		{
			if( instance != null )
				Log.Fatal( "PlayerManager: PlayerManager is already created." );
			instance = this;
		}

		public static PlayerManager Instance
		{
			get { return instance; }
		}

		protected override void OnPreCreate()
		{
			base.OnPreCreate();

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				serverOrSingle_playerIdentifierCounter = 1;
				serverOrSingle_players = new List<ServerOrSingle_Player>();
				serverOrSingle_playersAsReadOnly = new ReadOnlyCollection<ServerOrSingle_Player>(
					serverOrSingle_players );
			}

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				client_players = new List<Client_Player>();
				client_playersAsReadOnly = new ReadOnlyCollection<Client_Player>( client_players );
			}
		}

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();

			instance = null;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( EntitySystemWorld.Instance.IsServer() )
			{
				Server_UpdatePing();
				Server_TickUpdateDataToClients();
			}
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( serverOrSingle_players != null )
			{
				foreach( ServerOrSingle_Player player in serverOrSingle_players )
				{
					if( player.Intellect == entity )
						player.Intellect = null;
				}
			}
		}

		///////////////////////////////////////////
		// Server side
		///////////////////////////////////////////

		public IList<ServerOrSingle_Player> ServerOrSingle_Players
		{
			get { return serverOrSingle_playersAsReadOnly; }
		}

		public ServerOrSingle_Player Server_AddClientPlayer(
			UserManagementServerNetworkService.UserInfo user )
		{
			uint identifier = serverOrSingle_playerIdentifierCounter;
			serverOrSingle_playerIdentifierCounter++;

			ServerOrSingle_Player player = new ServerOrSingle_Player( identifier, user.Name, false,
				user );
			serverOrSingle_players.Add( player );

			Server_SendAddPlayerToClients( EntitySystemWorld.Instance.RemoteEntityWorlds, player );

			return player;
		}

		public ServerOrSingle_Player Single_AddSinglePlayer( string name )
		{
			uint identifier = serverOrSingle_playerIdentifierCounter;
			serverOrSingle_playerIdentifierCounter++;

			ServerOrSingle_Player player = new ServerOrSingle_Player( identifier, name, false, null );
			serverOrSingle_players.Add( player );
			return player;
		}

		public ServerOrSingle_Player ServerOrSingle_AddBotPlayer( string name )
		{
			uint identifier = serverOrSingle_playerIdentifierCounter;
			serverOrSingle_playerIdentifierCounter++;

			ServerOrSingle_Player player = new ServerOrSingle_Player( identifier, name, true, null );
			serverOrSingle_players.Add( player );

			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendAddPlayerToClients( EntitySystemWorld.Instance.RemoteEntityWorlds, player );

			return player;
		}

		public void ServerOrSingle_RemovePlayer( ServerOrSingle_Player player )
		{
			if( !serverOrSingle_players.Contains( player ) )
				Log.Fatal( "PlayerManager: ServerOrSingle_RemovePlayer: player is not exists." );

			if( EntitySystemWorld.Instance.IsServer() )
			{
				Server_SendRemovePlayerToClients( EntitySystemWorld.Instance.RemoteEntityWorlds,
					player );
			}

			serverOrSingle_players.Remove( player );
		}

		public ServerOrSingle_Player ServerOrSingle_GetPlayer( string name )
		{
			//it is can be slowly. need to use Dictionary.
			foreach( ServerOrSingle_Player player in serverOrSingle_players )
			{
				if( player.Name == name )
					return player;
			}
			return null;
		}

		public ServerOrSingle_Player ServerOrSingle_GetPlayer(
			UserManagementServerNetworkService.UserInfo user )
		{
			if( user == null )
				Log.Fatal( "PlayerManager: ServerOrSingle_GetPlayerByIntellect: user == null." );

			//it is can be slowly. need to use Dictionary.
			foreach( ServerOrSingle_Player player in serverOrSingle_players )
			{
				if( player.User == user )
					return player;
			}
			return null;
		}

		public ServerOrSingle_Player ServerOrSingle_GetPlayer( Intellect intellect )
		{
			if( intellect == null )
				Log.Fatal( "PlayerManager: ServerOrSingle_GetPlayerByIntellect: intellect == null." );

			//it is can be slowly. need to use Dictionary.
			foreach( ServerOrSingle_Player player in serverOrSingle_players )
			{
				if( player.Intellect == intellect )
					return player;
			}
			return null;
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			RemoteEntityWorld[] worlds = new RemoteEntityWorld[] { remoteEntityWorld };

			//send player information to client
			foreach( ServerOrSingle_Player player in serverOrSingle_players )
				Server_SendAddPlayerToClients( worlds, player );

			Server_SendUpdateDataToClients( worlds );
		}

		void Server_SendAddPlayerToClients( IList<RemoteEntityWorld> remoteEntityWorlds,
			ServerOrSingle_Player player )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PlayerManager ),
				(ushort)NetworkMessages.AddUserToClient );
			writer.WriteVariableUInt32( player.Identifier );
			writer.Write( player.Name );
			writer.Write( player.Bot );
			writer.WriteVariableUInt32( player.User != null ? player.User.Identifier : (uint)0 );
			EndNetworkMessage();
		}

		void Server_SendRemovePlayerToClients( IList<RemoteEntityWorld> remoteEntityWorlds,
			ServerOrSingle_Player player )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PlayerManager ),
				(ushort)NetworkMessages.RemoveUserToClient );
			writer.WriteVariableUInt32( player.Identifier );
			EndNetworkMessage();
		}

		void Server_UpdatePing()
		{
			foreach( ServerOrSingle_Player player in ServerOrSingle_Players )
			{
				if( player.User != null && player.User.ConnectedNode != null )
					player.Ping = player.User.ConnectedNode.LastRoundtripTime;
			}
		}

		void Server_TickUpdateDataToClients()
		{
			if( server_shouldUpdateDataToClients )
			{
				const float timeInterval = .25f;

				float time = EngineApp.Instance.Time;
				if( time >= server_updateDataToClientsLastTime + timeInterval )
				{
					Server_SendUpdateDataToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );

					server_shouldUpdateDataToClients = false;
					server_updateDataToClientsLastTime = time;
				}
			}
		}

		void Server_SendUpdateDataToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PlayerManager ),
				(ushort)NetworkMessages.UpdateDataToClient );

			foreach( ServerOrSingle_Player player in serverOrSingle_players )
			{
				writer.WriteVariableUInt32( player.Identifier );
				writer.WriteVariableInt32( player.Frags );
				writer.Write( player.Ping );
			}

			EndNetworkMessage();
		}

		///////////////////////////////////////////
		// Client side
		///////////////////////////////////////////

		public IList<Client_Player> Client_Players
		{
			get { return client_playersAsReadOnly; }
		}

		public Client_Player Client_GetPlayer( string name )
		{
			//slowly. need Dictionary.
			foreach( Client_Player player in client_players )
			{
				if( player.Name == name )
					return player;
			}
			return null;
		}

		public Client_Player Client_GetPlayer(
			UserManagementClientNetworkService.UserInfo user )
		{
			//slowly. need Dictionary.
			foreach( Client_Player player in client_players )
			{
				if( player.User == user )
					return player;
			}
			return null;
		}

		Client_Player Client_GetPlayer( uint identifier )
		{
			//slowly. need Dictionary.
			foreach( Client_Player player in client_players )
			{
				if( player.Identifier == identifier )
					return player;
			}
			return null;
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.AddUserToClient )]
		void Client_ReceiveAddUser( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint identifier = reader.ReadVariableUInt32();
			string name = reader.ReadString();
			bool bot = reader.ReadBoolean();
			uint userIdentifier = reader.ReadVariableUInt32();

			if( !reader.Complete() )
				return;

			//check for already exists
			{
				Client_Player playerForCheck = Client_GetPlayer( identifier );

				if( playerForCheck != null )
				{
					Log.Fatal( "PlayerManager: Client_ReceiveAddUserToClient: Player " +
						"with identifier \"{0}\" is already exists.", identifier );
				}
			}

			UserManagementClientNetworkService.UserInfo user = null;
			if( userIdentifier != 0 )
				user = GameNetworkClient.Instance.UserManagementService.GetUser( userIdentifier );

			Client_Player player = new Client_Player( identifier, name, bot, user );
			client_players.Add( player );
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.RemoveUserToClient )]
		void Client_ReceiveRemoveUser( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint identifier = reader.ReadVariableUInt32();

			if( !reader.Complete() )
				return;

			Client_Player player = Client_GetPlayer( identifier );
			if( player == null )
				return;

			client_players.Remove( player );
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.UpdateDataToClient )]
		void Client_ReceiveUpdateData( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			while( reader.BitPosition < reader.EndBitPosition )
			{
				uint identifier = reader.ReadVariableUInt32();
				int frags = reader.ReadVariableInt32();
				float ping = reader.ReadSingle();

				Client_Player player = Client_GetPlayer( identifier );

				if( player != null )
				{
					player.Frags = frags;
					player.Ping = ping;
				}
			}
		}
	}
}
