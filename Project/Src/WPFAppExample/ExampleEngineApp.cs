// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using Engine;
using Engine.Networking;
using Engine.EntitySystem;
using Engine.MapSystem;
using WPFAppFramework;
using ProjectCommon;

namespace WPFAppExample
{
	//we need this class for networking support.
	public class ExampleEngineApp : WPFAppEngineApp
	{
		static ExampleEngineApp instance;
		bool client_SubscribedToMapLoadingEvents;

		//

		public ExampleEngineApp( ApplicationTypes applicationType )
			: base( applicationType )
		{
			instance = this;
		}

		public static new ExampleEngineApp Instance
		{
			get { return instance; }
		}

		protected override void OnDestroy()
		{
			WPFAppWorld.WorldDestroy();
			Client_DisconnectFromServer();

			base.OnDestroy();
			instance = null;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//update client
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
				client.Update();
		}

		public void TryConnectToServer( string host, int port, string userName, string password )
		{
			GameNetworkClient client = new GameNetworkClient( true );

			client.ConnectionStatusChanged += Client_ConnectionStatusChanged;
			client.ChatService.ReceiveText += Client_ChatService_ReceiveText;
			client.CustomMessagesService.ReceiveMessage += Client_CustomMessagesService_ReceiveMessage;

			//add handlers for entity system service events
			client.EntitySystemService.WorldCreateBegin += Client_EntitySystemService_WorldCreateBegin;
			client.EntitySystemService.WorldCreateEnd += Client_EntitySystemService_WorldCreateEnd;
			client.EntitySystemService.WorldDestroy += Client_EntitySystemService_WorldDestroy;

			if( !client_SubscribedToMapLoadingEvents )
			{
				Map.Client_MapLoadingBegin += Map_Client_MapLoadingBegin;
				Map.Client_MapLoadingEnd += Map_Client_MapLoadingEnd;
				client_SubscribedToMapLoadingEvents = true;
			}

			string error;
			if( !client.BeginConnect( host, port, EngineVersionInformation.Version, userName, password,
				out error ) )
			{
				Log.Error( error );
				Client_DisconnectFromServer();
				return;
			}
		}

		public void Client_DisconnectFromServer()
		{
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
			{
				client.ConnectionStatusChanged -= Client_ConnectionStatusChanged;
				client.ChatService.ReceiveText -= Client_ChatService_ReceiveText;
				client.CustomMessagesService.ReceiveMessage -= Client_CustomMessagesService_ReceiveMessage;

				//remove handlers for entity system service events
				client.EntitySystemService.WorldCreateBegin -= Client_EntitySystemService_WorldCreateBegin;
				client.EntitySystemService.WorldCreateEnd -= Client_EntitySystemService_WorldCreateEnd;
				client.EntitySystemService.WorldDestroy -= Client_EntitySystemService_WorldDestroy;

				client.Dispose();
			}
		}

		void Client_ConnectionStatusChanged( NetworkClient sender, NetworkConnectionStatuses status )
		{
			switch( status )
			{
			case NetworkConnectionStatuses.Disconnected:
				{
					//string text = "Unable to connect";
					//if( sender.DisconnectionReason != "" )
					//   text += ". " + sender.DisconnectionReason;
					//Log.Error( text );

					WPFAppWorld.WorldDestroy();
					Client_DisconnectFromServer();
				}
				break;

			case NetworkConnectionStatuses.Connecting:
				break;

			case NetworkConnectionStatuses.Connected:
				break;
			}
		}

		void Client_EntitySystemService_WorldCreateBegin( EntitySystemClientNetworkService sender,
			WorldType worldType, string mapVirtualFileName )
		{
			MapSystemWorld.MapDestroy();

			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.ClientOnly,
				worldType, sender.NetworkingInterface ) )
			{
				Log.Fatal( "GameEngineApp: Client_EntitySystemService_WorldCreateBegin: " +
					"EntitySystemWorld.WorldCreate failed." );
			}
		}

		void Client_EntitySystemService_WorldCreateEnd( EntitySystemClientNetworkService sender )
		{
		}

		void Map_Client_MapLoadingBegin()
		{
		}

		void Map_Client_MapLoadingEnd()
		{
		}

		void Client_EntitySystemService_WorldDestroy( EntitySystemClientNetworkService sender,
			bool newMapWillBeLoaded )
		{
			WPFAppWorld.WorldDestroy();
		}

		void Client_ChatService_ReceiveText( ChatClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo fromUser, string text )
		{
		}

		void Client_CustomMessagesService_ReceiveMessage( CustomMessagesClientNetworkService sender,
			string message, string data )
		{
			//process custom messages from server

			//if( message == "Lobby_MapName" )
			//{
			//}
		}

	}
}