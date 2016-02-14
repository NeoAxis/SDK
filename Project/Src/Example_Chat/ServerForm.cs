// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine;
using ProjectCommon;

namespace ChatExample
{
	public partial class ServerForm : Form
	{
		public static ServerForm instance;

		//

		public ServerForm()
		{
			instance = this;

			InitializeComponent();
		}

		private void buttonCreate_Click( object sender, EventArgs e )
		{
			Create();
		}

		private void buttonDestroy_Click( object sender, EventArgs e )
		{
			Destroy();
		}

		protected override void DestroyHandle()
		{
			Destroy();

			base.DestroyHandle();

			instance = null;
		}

		void Create()
		{
			if( GameNetworkServer.Instance != null )
			{
				Log( "Error: Already created" );
				return;
			}

			GameNetworkServer server = new GameNetworkServer( "NeoAxis Chat Server",
				EngineVersionInformation.Version, 128, false );

			server.UserManagementService.AddUserEvent += UserManagementService_AddUserEvent;
			server.UserManagementService.RemoveUserEvent += UserManagementService_RemoveUserEvent;
			server.ChatService.ReceiveText += ChatService_ReceiveText;

			int port = 56565;

			string error;
			if( !server.BeginListen( port, out error ) )
			{
				Log( "Error: " + error );
				Destroy();
				return;
			}

			Log( "Server has been created" );
			Log( "Listening port {0}...", port );

			buttonCreate.Enabled = false;
			buttonDestroy.Enabled = true;
		}

		void Destroy()
		{
			if( GameNetworkServer.Instance != null )
			{
				GameNetworkServer.Instance.Dispose( "The server has been destroyed" );

				buttonCreate.Enabled = true;
				buttonDestroy.Enabled = false;
				listBoxUsers.Items.Clear();

				Log( "Destroyed" );
			}
		}

		void Log( string text, params object[] args )
		{
			int index = listBoxLog.Items.Add( string.Format( text, args ) );
			listBoxLog.SelectedIndex = index;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.Update();
		}

		void UserManagementService_AddUserEvent( UserManagementServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo user )
		{
			Log( "User connected: " + user.ToString() );
			listBoxUsers.Items.Add( user );
		}

		void UserManagementService_RemoveUserEvent( UserManagementServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo user )
		{
			listBoxUsers.Items.Remove( user );
			Log( "User disconnected: " + user.ToString() );
		}

		void ChatService_ReceiveText( ChatServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo fromUser,
			string text, UserManagementServerNetworkService.UserInfo privateToUser )
		{
			string userName = fromUser != null ? fromUser.Name : "(null)";
			string toUserName = privateToUser != null ? privateToUser.Name : "All";
			Log( "Chat: {0} -> {1}: {2}", userName, toUserName, text );
		}

	}
}