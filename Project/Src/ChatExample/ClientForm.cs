// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine;
using Engine.Networking;
using ProjectCommon;

namespace ChatExample
{
	public partial class ClientForm : Form
	{
		public static ClientForm instance;

		//

		public ClientForm()
		{
			instance = this;

			InitializeComponent();

			Random random = new Random();
			textBoxUserName.Text = "User" + random.Next( 999 );
		}

		private void buttonConnect_Click( object sender, EventArgs e )
		{
			Connect();
		}

		private void buttonDisconnect_Click( object sender, EventArgs e )
		{
			Disconnect();
		}

		protected override void DestroyHandle()
		{
			Disconnect();

			base.DestroyHandle();

			instance = null;
		}

		void Connect()
		{
			if( GameNetworkClient.Instance != null )
			{
				Log( "Error: Client already connected" );
				return;
			}

			string loginName = textBoxUserName.Text.Trim();
			if( string.IsNullOrEmpty( loginName ) )
			{
				Log( "Error: Empty login name" );
				return;
			}

			GameNetworkClient client = new GameNetworkClient( false );
			client.ConnectionStatusChanged += Client_ConnectionStatusChanged;
			client.UserManagementService.AddUserEvent += UserManagementService_AddUserEvent;
			client.UserManagementService.RemoveUserEvent += UserManagementService_RemoveUserEvent;
			client.ChatService.ReceiveText += ChatService_ReceiveText;

			string password = "";

			int port = 56565;

			string error;
			if( !client.BeginConnect( textBoxAddress.Text, port, EngineVersionInformation.Version,
				loginName, password, out error ) )
			{
				Log( "Error: " + error );
				Disconnect();
				return;
			}

			buttonConnect.Enabled = false;
			buttonDisconnect.Enabled = true;
			textBoxUserName.ReadOnly = true;
			textBoxAddress.ReadOnly = true;
			textBoxEnterText.ReadOnly = true;
			buttonSend.Enabled = true;

			Log( "Trying to connect..." );
		}

		void Disconnect()
		{
			if( GameNetworkClient.Instance != null )
			{
				GameNetworkClient.Instance.Dispose();

				Log( "Disconnected" );

				buttonConnect.Enabled = true;
				buttonDisconnect.Enabled = false;
				textBoxUserName.ReadOnly = false;
				textBoxAddress.ReadOnly = false;
				textBoxEnterText.ReadOnly = true;
				buttonSend.Enabled = false;
				listBoxUsers.Items.Clear();
			}
		}

		void Log( string text, params object[] args )
		{
			int index = listBoxLog.Items.Add( string.Format( text, args ) );
			listBoxLog.SelectedIndex = index;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
				client.Update();
		}

		private void buttonSend_Click( object sender, EventArgs e )
		{
			Send();
		}

		private void textBoxEnterText_KeyDown( object sender, KeyEventArgs e )
		{
			if( e.KeyCode == Keys.Return )
			{
				Send();
				e.Handled = true;
			}
		}

		void Send()
		{
			GameNetworkClient client = GameNetworkClient.Instance;

			if( client == null || client.Status != NetworkConnectionStatuses.Connected )
				return;

			string text = textBoxEnterText.Text.Trim();
			if( string.IsNullOrEmpty( text ) )
				return;

			client.ChatService.SayToAll( text );

			textBoxEnterText.Text = "";
		}

		void Client_ConnectionStatusChanged( NetworkClient sender, NetworkConnectionStatuses status )
		{
			switch( status )
			{
			case NetworkConnectionStatuses.Disconnected:
				{
					string text = string.Format( "Disconnected. Reason: \"{0}\"",
						sender.DisconnectionReason );
					Log( text );
					Disconnect();
				}
				break;

			case NetworkConnectionStatuses.Connecting:
				Log( "Connecting..." );
				break;

			case NetworkConnectionStatuses.Connected:
				Log( "Connected" );
				Log( "Server: \"{0}\"", sender.RemoteServerName );
				foreach( string serviceName in sender.ServerConnectedNode.RemoteServices )
					Log( "Server service: \"{0}\"", serviceName );

				buttonConnect.Enabled = false;
				buttonDisconnect.Enabled = true;
				textBoxUserName.ReadOnly = true;
				textBoxAddress.ReadOnly = true;
				textBoxEnterText.ReadOnly = false;
				buttonSend.Enabled = true;
				break;
			}
		}

		void UserManagementService_AddUserEvent( UserManagementClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo user )
		{
			listBoxUsers.Items.Add( user );
		}

		void UserManagementService_RemoveUserEvent( UserManagementClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo user )
		{
			listBoxUsers.Items.Remove( user );
		}

		void ChatService_ReceiveText( ChatClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo fromUser, string text )
		{
			string userName = fromUser != null ? fromUser.Name : "(null)";
			Log( "{0}: {1}", userName, text );
		}

	}
}