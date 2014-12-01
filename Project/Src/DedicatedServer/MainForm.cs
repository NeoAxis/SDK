// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using ProjectCommon;
using WinFormsAppFramework;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.Networking;
using ProjectEntities;

namespace DedicatedServer
{
	public partial class MainForm : Form
	{
		[Config( "DedicatedServer", "lastMapName" )]
		static string lastMapName = "Maps\\JigsawPuzzleGame\\Map\\Map.map";//Jigsaw puzzle by default

		[Config( "DedicatedServer", "loadMapAtStartup" )]
		static bool loadMapAtStartup;

		[Config( "DedicatedServer", "allowCustomClientCommands" )]
		static bool allowCustomClientCommands = true;

		//

		public MainForm()
		{
			InitializeComponent();
		}

		private void buttonClose_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buttonCreate_Click( object sender, EventArgs e )
		{
			Create();
		}

		private void buttonDestroy_Click( object sender, EventArgs e )
		{
			Destroy();
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			//NeoAxis initialization
			EngineApp.ConfigName = "user:Configs/DedicatedServer.config";
			EngineApp.ReplaceRenderingSystemComponentName = "RenderingSystem_NULL";
			EngineApp.ReplaceSoundSystemComponentName = "SoundSystem_NULL";
			if( !WinFormsAppWorld.Init( new WinFormsAppEngineApp( EngineApp.ApplicationTypes.Simulation ), this,
				"user:Logs/DedicatedServer.log", true, null, null, null, null ) )
			{
				Close();
				return;
			}
			WinFormsAppEngineApp.Instance.AutomaticTicks = false;

			Engine.Log.Handlers.InfoHandler += delegate( string text, ref bool dumpToLogFile )
			{
				Log( "Log: " + text );
			};

			Engine.Log.Handlers.ErrorHandler += delegate( string text, ref bool handled, ref bool dumpToLogFile )
			{
				handled = true;
				timer1.Stop();
				MessageBox.Show( text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				timer1.Start();
			};

			Engine.Log.Handlers.FatalHandler += delegate( string text, string createdLogFilePath,
				ref bool handled )
			{
				handled = true;
				timer1.Stop();
				MessageBox.Show( text, "Fatal", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			};

			//register config fields of this class
			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			//generate map list
			{
				string[] mapList = VirtualDirectory.GetFiles( "", "*.map", SearchOption.AllDirectories );
				foreach( string mapName in mapList )
				{
					//check for network support
					if( VirtualFile.Exists( string.Format( "{0}\\NoNetworkSupport.txt",
						Path.GetDirectoryName( mapName ) ) ) )
					{
						continue;
					}

					comboBoxMaps.Items.Add( mapName );
					if( mapName == lastMapName )
						comboBoxMaps.SelectedIndex = comboBoxMaps.Items.Count - 1;
				}

				comboBoxMaps.SelectedIndexChanged += comboBoxMaps_SelectedIndexChanged;
			}

			checkBoxLoadMapAtStartup.Checked = loadMapAtStartup;
			checkBoxAllowCustomClientCommands.Checked = allowCustomClientCommands;

			//load map at startup
			if( loadMapAtStartup && comboBoxMaps.SelectedItem != null )
			{
				Create();
				string mapName = comboBoxMaps.SelectedItem as string;
				if( !MapLoad( mapName ) )
					return;
			}
		}

		private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			Destroy();

			//NeoAxis shutdown
			WinFormsAppWorld.Shutdown();
		}

		void Create()
		{
			if( GameNetworkServer.Instance != null )
			{
				Log( "Error: Server already created" );
				return;
			}

			GameNetworkServer server = new GameNetworkServer( "NeoAxis Game Server",
				EngineVersionInformation.Version, 128, true );

			server.UserManagementService.AddUserEvent += UserManagementService_AddUserEvent;
			server.UserManagementService.RemoveUserEvent += UserManagementService_RemoveUserEvent;
			server.ChatService.ReceiveText += ChatService_ReceiveText;
			server.CustomMessagesService.ReceiveMessage += CustomMessagesService_ReceiveMessage;

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
			buttonMapLoad.Enabled = true;
		}

		bool MapLoad( string fileName )
		{
			MapDestroy( false );

			Log( "Loading map \"{0}\"...", fileName );

			WorldType worldType = EntitySystemWorld.Instance.DefaultWorldType;

			GameNetworkServer server = GameNetworkServer.Instance;
			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.DedicatedServer,
				worldType, server.EntitySystemService.NetworkingInterface ) )
			{
				Log( "Error: EntitySystemWorld.Instance.WorldCreate failed." );
				return false;
			}

			if( !MapSystemWorld.MapLoad( fileName ) )
			{
				MapDestroy( false );
				return false;
			}

			//run simulation
			EntitySystemWorld.Instance.Simulation = true;

			GameNetworkServer.Instance.EntitySystemService.WorldWasCreated();

			Log( "Map loaded" );

			buttonMapLoad.Enabled = false;
			buttonMapUnload.Enabled = true;
			buttonMapChange.Enabled = true;

			return true;
		}

		void MapDestroy( bool newMapWillBeLoaded )
		{
			bool mapWasDestroyed = Map.Instance != null;

			MapSystemWorld.MapDestroy();

			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();

			if( mapWasDestroyed )
				GameNetworkServer.Instance.EntitySystemService.WorldWasDestroyed( newMapWillBeLoaded );

			if( mapWasDestroyed )
				Log( "Map destroyed" );

			buttonMapLoad.Enabled = true;
			buttonMapUnload.Enabled = false;
			buttonMapChange.Enabled = false;
		}

		void Destroy()
		{
			MapDestroy( false );

			if( GameNetworkServer.Instance != null )
			{
				GameNetworkServer.Instance.Dispose( "The server has been destroyed" );

				buttonCreate.Enabled = true;
				buttonDestroy.Enabled = false;
				buttonMapLoad.Enabled = false;
				buttonMapChange.Enabled = false;
				buttonMapUnload.Enabled = false;
				listBoxUsers.Items.Clear();

				Log( "Server destroyed" );
			}
		}

		void Log( string text, params object[] args )
		{
			while( listBoxLog.Items.Count > 300 )
				listBoxLog.Items.RemoveAt( 0 );
			int index = listBoxLog.Items.Add( string.Format( text, args ) );
			listBoxLog.SelectedIndex = index;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.Update();

			if( WinFormsAppEngineApp.Instance != null )
				WinFormsAppEngineApp.Instance.DoTick();
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
			UserManagementServerNetworkService.UserInfo fromUser, string text,
			UserManagementServerNetworkService.UserInfo privateToUser )
		{
			string userName = fromUser != null ? fromUser.Name : "(null)";
			string toUserName = privateToUser != null ? privateToUser.Name : "All";
			Log( "Chat: {0} -> {1}: {2}", userName, toUserName, text );
		}

		void comboBoxMaps_SelectedIndexChanged( object sender, EventArgs e )
		{
			lastMapName = comboBoxMaps.SelectedItem as string;
		}

		private void buttonDoSomething_Click( object sender, EventArgs e )
		{
			MessageBox.Show( "You can write code for testing here.", "Warning" );

			//example
			//MapObject mapObject = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
			//mapObject.Position = new Vec3( 0, 0, 30 );
			//mapObject.PostCreate();
		}

		private void checkBoxLoadMapAtStartup_CheckedChanged( object sender, EventArgs e )
		{
			loadMapAtStartup = checkBoxLoadMapAtStartup.Checked;
		}

		private void buttonMapLoad_Click( object sender, EventArgs e )
		{
			string mapName = comboBoxMaps.SelectedItem as string;
			if( string.IsNullOrEmpty( mapName ) )
			{
				Log( "Error: No map selected" );
				return;
			}

			if( !MapLoad( mapName ) )
				return;
		}

		private void buttonMapUnload_Click( object sender, EventArgs e )
		{
			MapDestroy( false );
		}

		private void buttonMapChange_Click( object sender, EventArgs e )
		{
			MapDestroy( true );

			string mapName = comboBoxMaps.SelectedItem as string;
			if( string.IsNullOrEmpty( mapName ) )
			{
				Log( "Error: No map selected" );
				return;
			}

			if( !MapLoad( mapName ) )
				return;
		}

		void CustomMessagesService_ReceiveMessage( CustomMessagesServerNetworkService sender,
			NetworkNode.ConnectedNode source, string message, string data )
		{
			//Warning! Messages must be checked by security reasons.
			//Modified client application can send any message with any data.

			if( allowCustomClientCommands )
			{
				//load map
				if( message == "Example_MapLoad" )
				{
					string mapName = data;
					MapDestroy( true );
					if( !MapLoad( mapName ) )
						return;
					return;
				}

				//create map object
				if( message == "Example_CreateMapObject" )
				{
					string[] parameters = data.Split( ';' );
					string typeName = parameters[ 0 ];
					Vec3 position = Vec3.Parse( parameters[ 1 ] );
					Quat rotation = Quat.Parse( parameters[ 2 ] );

					if( Map.Instance != null )
					{
						MapObject entity = (MapObject)Entities.Instance.Create( typeName, Map.Instance );
						entity.Position = position;
						entity.Rotation = rotation;
						entity.PostCreate();
					}

					return;
				}
			}

		}

		private void checkBoxAllowCustomClientCommands_CheckedChanged( object sender, EventArgs e )
		{
			allowCustomClientCommands = checkBoxAllowCustomClientCommands.Checked;
		}

	}
}
