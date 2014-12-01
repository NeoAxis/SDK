// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for Jigsaw Puzzle Game Example
	/// </summary>
	public class JigsawPuzzleGameWindow : GameWindow
	{
		MapCamera mapCamera;

		//HUD screen
		Control hudControl;
		EditBox chatMessageEditBox;

		//tryingToMovePiece
		JigsawPuzzlePiece tryingToMovePiece;
		Vec2 tryingToMovePieceOffset;

		bool shouldSendMovingPiecePositionToServer;
		Vec2 shouldSendMovingPiecePositionToServer_Position;
		float shouldSendMovingPiecePositionToServer_LastSentTime;

		//screenNessages
		class ScreenMessage
		{
			public string text;
			public float timeRemaining;
		}
		List<ScreenMessage> screenMessages = new List<ScreenMessage>();

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			//load the HUD screen
			hudControl = ControlDeclarationManager.Instance.CreateControl(
				"Maps\\JigsawPuzzleGame\\JigsawPuzzleGameHUD.gui" );
			//attach the HUD screen to the this window
			Controls.Add( hudControl );

			if( EntitySystemWorld.Instance.IsSingle() )
			{
				//hide chat edit box for single mode
				hudControl.Controls[ "ChatText" ].Visible = false;
				hudControl.Controls[ "ChatMessage" ].Visible = false;
			}

			//AnotherMap button
			if( EntitySystemWorld.Instance.IsSingle() )
			{
				( (Button)hudControl.Controls[ "AnotherMap" ] ).Click += delegate( Button sender )
				{
					GameWorld.Instance.NeedChangeMap( "Maps\\MainDemo\\Map.map", "Teleporter_Maps", null );
				};
			}
			else
				hudControl.Controls[ "AnotherMap" ].Visible = false;

			chatMessageEditBox = (EditBox)hudControl.Controls[ "ChatMessage" ];
			chatMessageEditBox.PreKeyDown += ChatMessageEditBox_PreKeyDown;

			//find first map camera
			foreach( Entity entity in Map.Instance.Children )
			{
				mapCamera = entity as MapCamera;
				if( mapCamera != null )
					break;
			}

			//for chat support
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.ChatService.ReceiveText += Server_ChatService_ReceiveText;
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
				client.ChatService.ReceiveText += Client_ChatService_ReceiveText;
		}

		protected override void OnDetach()
		{
			//for chat support
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.ChatService.ReceiveText -= Server_ChatService_ReceiveText;
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
				client.ChatService.ReceiveText -= Client_ChatService_ReceiveText;

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			//change camera type
			if( e.Key == EKeys.F7 )
			{
				FreeCameraEnabled = !FreeCameraEnabled;

				GameEngineApp.Instance.AddScreenMessage(
					string.Format( "Camera type: {0}", FreeCameraEnabled ? "Free" : "Default" ) );

				return true;
			}

			//select another demo map
			if( e.Key == EKeys.F3 )
			{
				GameWorld.Instance.NeedChangeMap( "Maps\\MainDemo\\Map.map", "Teleporter_Maps", null );
				return true;
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			switch( button )
			{
			case EMouseButtons.Left:
				{
					tryingToMovePiece = GetPieceByCursor();
					if( tryingToMovePiece != null )
					{
						if( EntitySystemWorld.Instance.IsServer() )
						{
							//server
							GameNetworkServer server = GameNetworkServer.Instance;
							tryingToMovePiece.Server_MoveBegin( server.UserManagementService.ServerUser );
						}
						else if( EntitySystemWorld.Instance.IsClientOnly() )
						{
							//client
							tryingToMovePiece.Client_MoveTryToBegin();
						}
						else
						{
							//single mode
							tryingToMovePiece.Single_MoveBegin();
						}

						Vec2 cursorPosition;
						GetGameAreaCursorPosition( out cursorPosition );
						tryingToMovePieceOffset = tryingToMovePiece.Position.ToVec2() - cursorPosition;
						return true;
					}
				}
				break;
			}

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			switch( button )
			{
			case EMouseButtons.Left:
				if( tryingToMovePiece != null )
				{
					UpdateShouldSendMovingPiecePositionToServer( true );

					if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
						tryingToMovePiece.ServerOrSingle_MoveFinish();
					else
						tryingToMovePiece.Client_MoveTryToFinish();

					tryingToMovePiece = null;

					return true;
				}
				break;
			}

			return base.OnMouseUp( button );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			//update mouse relative mode
			{
				if( FreeCameraEnabled && FreeCameraMouseRotating )
					EngineApp.Instance.MouseRelativeMode = true;
				else
					EngineApp.Instance.MouseRelativeMode = false;
			}

			//update position of moving place
			if( tryingToMovePiece != null )
			{
				Vec2 cursorPosition;
				if( GetGameAreaCursorPosition( out cursorPosition ) )
				{
					Vec2 newPosition = cursorPosition + tryingToMovePieceOffset;
					//tryingToMovePiece.Client_MoveUpdatePosition( newPosition );

					//should send to server
					shouldSendMovingPiecePositionToServer = true;
					shouldSendMovingPiecePositionToServer_Position = newPosition;
				}
			}

			UpdateShouldSendMovingPiecePositionToServer( false );

			//screenMessages
			{
				for( int n = 0; n < screenMessages.Count; n++ )
				{
					screenMessages[ n ].timeRemaining -= delta;
					if( screenMessages[ n ].timeRemaining <= 0 )
					{
						screenMessages.RemoveAt( n );
						n--;
					}
				}
			}
		}

		void UpdateShouldSendMovingPiecePositionToServer( bool obligatoryToSend )
		{
			if( !shouldSendMovingPiecePositionToServer )
				return;

			float time = EngineApp.Instance.Time;

			float fps = 30.0f;
			if( time > shouldSendMovingPiecePositionToServer_LastSentTime + 1.0f / fps ||
				obligatoryToSend )
			{
				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				{
					//server or single
					tryingToMovePiece.ServerOrSingle_MoveUpdatePosition(
						shouldSendMovingPiecePositionToServer_Position );
				}
				else
				{
					//client
					tryingToMovePiece.Client_MoveUpdatePosition(
						shouldSendMovingPiecePositionToServer_Position );
				}

				shouldSendMovingPiecePositionToServer = false;
				shouldSendMovingPiecePositionToServer_LastSentTime = time;
			}
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward,
			out Vec3 up, ref Degree cameraFov )
		{
			if( mapCamera != null )
			{
				position = mapCamera.Position;
				forward = mapCamera.Rotation.GetForward();
				up = mapCamera.Rotation.GetUp();
			}
			else
			{
				position = Vec3.Zero;
				forward = Vec3.XAxis;
				up = Vec3.ZAxis;
			}
		}

		protected override void OnRender()
		{
			base.OnRender();

			bool thisUserIsMovingPiece = false;

			//render borders for moving pieces by users
			foreach( Entity entity in Map.Instance.Children )
			{
				JigsawPuzzlePiece piece = entity as JigsawPuzzlePiece;

				if( piece != null )
				{
					//server
					if( EntitySystemWorld.Instance.IsServer() )
					{
						if( piece.Server_MovingByUser != null )
						{
							GameNetworkServer server = GameNetworkServer.Instance;
							if( server.UserManagementService.ServerUser == piece.Server_MovingByUser )
								thisUserIsMovingPiece = true;
							RenderPieceSelectionBorder( piece, true );
						}
					}

					//client
					if( EntitySystemWorld.Instance.IsClientOnly() )
					{
						if( piece.Client_MovingByUser != null )
						{
							GameNetworkClient client = GameNetworkClient.Instance;
							if( client.UserManagementService.ThisUser == piece.Client_MovingByUser )
								thisUserIsMovingPiece = true;
							RenderPieceSelectionBorder( piece, true );
						}
					}

					//single mode
					if( EntitySystemWorld.Instance.IsSingle() )
					{
						if( piece.ServerOrSingle_Moving )
						{
							thisUserIsMovingPiece = true;
							RenderPieceSelectionBorder( piece, true );
						}
					}
				}
			}

			//render border for mouse over piece
			if( !thisUserIsMovingPiece )
			{
				JigsawPuzzlePiece piece = GetPieceByCursor();
				if( piece != null )
					RenderPieceSelectionBorder( piece, false );
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			UpdateHUD();

			//render user names for moving pieces by users
			foreach( Entity entity in Map.Instance.Children )
			{
				JigsawPuzzlePiece piece = entity as JigsawPuzzlePiece;
				if( piece != null )
				{
					string userName = null;

					if( EntitySystemWorld.Instance.IsServer() )
					{
						if( piece.Server_MovingByUser != null )
							userName = piece.Server_MovingByUser.Name;
					}
					if( EntitySystemWorld.Instance.IsClientOnly() )
					{
						if( piece.Client_MovingByUser != null )
							userName = piece.Client_MovingByUser.Name;
					}

					if( !string.IsNullOrEmpty( userName ) )
					{
						Vec2 screenPosition;
						if( RendererWorld.Instance.DefaultCamera.ProjectToScreenCoordinates(
							piece.Position, out screenPosition ) )
						{
							renderer.AddText( userName,
								screenPosition, HorizontalAlign.Left, VerticalAlign.Top,
								new ColorValue( 0, 1, 0, .75f ) );
						}
					}
				}
			}

			//show list of users
			if( GameNetworkServer.Instance != null || GameNetworkClient.Instance != null )
			{
				List<string> lines = new List<string>();

				lines.Add( "Players:" );

				if( GameNetworkServer.Instance != null )
				{
					UserManagementServerNetworkService userService =
						GameNetworkServer.Instance.UserManagementService;

					foreach( UserManagementServerNetworkService.UserInfo user in userService.Users )
					{
						string line = "  " + user.Name;
						if( user == userService.ServerUser )
							line += " (you)";
						lines.Add( line );
					}
				}

				if( GameNetworkClient.Instance != null )
				{
					UserManagementClientNetworkService userService =
						GameNetworkClient.Instance.UserManagementService;

					foreach( UserManagementClientNetworkService.UserInfo user in userService.Users )
					{
						string line = "  " + user.Name;
						if( user == userService.ThisUser )
							line += " (you)";
						lines.Add( line );
					}
				}

				renderer.AddTextLines( lines, new Vec2( .01f, .15f ), HorizontalAlign.Left, VerticalAlign.Top,
					0, new ColorValue( 1, 1, 0 ) );
			}

			//screenMessages
			{
				Vec2 pos = new Vec2( .01f, .9f );
				for( int n = screenMessages.Count - 1; n >= 0; n-- )
				{
					ScreenMessage message = screenMessages[ n ];

					ColorValue color = new ColorValue( 1, 1, 1, message.timeRemaining );
					if( color.Alpha > 1 )
						color.Alpha = 1;

					renderer.AddText( message.text, pos, HorizontalAlign.Left, VerticalAlign.Top,
						color );
					pos.Y -= renderer.DefaultFont.Height;
				}
			}

			//Game is paused on server
			if( EntitySystemWorld.Instance.IsClientOnly() && !EntitySystemWorld.Instance.Simulation )
			{
				renderer.AddText( "Game is paused on server", new Vec2( .5f, .5f ),
					HorizontalAlign.Center, VerticalAlign.Center, new ColorValue( 1, 0, 0 ) );
			}
		}

		void RenderPieceSelectionBorder( JigsawPuzzlePiece piece, bool selected )
		{
			Camera camera = RendererWorld.Instance.DefaultCamera;

			if( selected )
				camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
			else
				camera.DebugGeometry.Color = new ColorValue( 1, 1, 0 );

			camera.DebugGeometry.AddBounds( piece.MapBounds );
		}

		void UpdateHUD()
		{
			hudControl.Visible = EngineDebugSettings.DrawGui;
		}

		bool GetGameAreaCursorPosition( out Vec2 position )
		{
			position = Vec2.Zero;

			Plane plane = new Plane( 0, 0, 1, 0 );

			Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
				EngineApp.Instance.MousePosition );
			if( float.IsNaN( ray.Direction.X ) )
				return false;

			float scale;
			if( !plane.RayIntersection( ray, out scale ) )
				return false;

			position = ray.GetPointOnRay( scale ).ToVec2();
			return true;
		}

		JigsawPuzzlePiece GetPieceByCursor()
		{
			if( EngineApp.Instance.MouseRelativeMode )
				return null;

			Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
				EngineApp.Instance.MousePosition );
			if( float.IsNaN( ray.Direction.X ) )
				return null;

			JigsawPuzzlePiece piece = null;

			Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
			{
				piece = obj as JigsawPuzzlePiece;
				if( piece != null )
				{
					//found. stop finding
					return false;
				}

				//continue finding
				return true;
			} );

			return piece;
		}

		void ChatMessageEditBox_PreKeyDown( KeyEvent e, ref bool handled )
		{
			if( e.Key == EKeys.Return && chatMessageEditBox.Focused )
			{
				SayChatMessage();
				handled = true;
				return;
			}
		}

		void SayChatMessage()
		{
			string text = chatMessageEditBox.Text.Trim();
			if( string.IsNullOrEmpty( text ) )
				return;

			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.ChatService.SayToAll( text );
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
				client.ChatService.SayToAll( text );

			chatMessageEditBox.Text = "";
		}

		void AddScreenMessage( string text )
		{
			ScreenMessage message = new ScreenMessage();
			message.text = text;
			message.timeRemaining = 30;
			screenMessages.Add( message );

			while( screenMessages.Count > 20 )
				screenMessages.RemoveAt( 0 );
		}

		void Server_ChatService_ReceiveText( ChatServerNetworkService sender,
			UserManagementServerNetworkService.UserInfo fromUser, string text,
			UserManagementServerNetworkService.UserInfo privateToUser )
		{
			string userName = fromUser != null ? fromUser.Name : "(null)";
			AddScreenMessage( string.Format( "{0}: {1}", userName, text ) );
		}

		void Client_ChatService_ReceiveText( ChatClientNetworkService sender,
			UserManagementClientNetworkService.UserInfo fromUser, string text )
		{
			string userName = fromUser != null ? fromUser.Name : "(null)";
			AddScreenMessage( string.Format( "{0}: {1}", userName, text ) );
		}

		protected override bool OnFreeCameraIsAllowToMove()
		{
			//disable movement of free camera if chat edit box focused
			if( chatMessageEditBox != null && chatMessageEditBox.Focused )
				return false;

			return base.OnFreeCameraIsAllowToMove();
		}

	}
}
