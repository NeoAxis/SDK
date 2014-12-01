// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.UISystem;
using Engine.Renderer;
using Engine.Utils;
using Engine.SoundSystem;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="JigsawPuzzlePiece"/> entity type.
	/// </summary>
	public class JigsawPuzzlePieceType : MapObjectType
	{
	}

	/// <summary>
	/// The piece for puzzle game example.
	/// </summary>
	public class JigsawPuzzlePiece : MapObject
	{
		Vec2I index;//network synchronized

		Mesh mesh;
		MapObjectAttachedMesh attachedMesh;

		//server side
		bool serverOrSingle_Moving;
		const float maxMovingTimeSeconds = 30;
		float serverOrSingle_movingTime;
		UserManagementServerNetworkService.UserInfo server_movingByUser;

		//client side
		UserManagementClientNetworkService.UserInfo client_movingByUser;

		//

		JigsawPuzzlePieceType _type = null; public new JigsawPuzzlePieceType Type { get { return _type; } }

		///////////////////////////////////////////

		[StructLayout( LayoutKind.Sequential )]
		struct Vertex
		{
			public Vec3 position;
			public Vec3 normal;
			public Vec2 texCoord;
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			IndexToClient,
			PositionToClient,
			MoveBeginToClient,
			MoveFinishToClient,

			MoveTryToBeginToServer,
			MoveUpdatePositionToServer,
			MoveTryToFinishToServer,
		}

		///////////////////////////////////////////

		[Browsable( false )]
		public Vec2I Index
		{
			get { return index; }
		}

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			CreateMesh();
			CreateAttachedMesh();

			SubscribeToTickEvent();
		}

		protected override void OnDestroy()
		{
			DestroyAttachedMesh();
			DestroyMesh();

			base.OnDestroy();
		}

		protected override void OnCalculateMapBounds( ref Bounds bounds )
		{
			base.OnCalculateMapBounds( ref bounds );

			Bounds b = new Bounds( Position );
			b.Expand( new Vec3( .5f, .5f, .1f ) );
			bounds.Add( b );
		}

		protected override void OnTick()
		{
			base.OnTick();

			//server side
			if( serverOrSingle_Moving )
			{
				serverOrSingle_movingTime += TickDelta;

				if( serverOrSingle_movingTime > maxMovingTimeSeconds )
				{
					ServerOrSingle_MoveFinish();
				}
			}
		}

		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );

			//server side
			if( EntitySystemWorld.Instance.IsServer() )
			{
				//send new position to clients
				Server_SendPositionToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
			}
		}

		void CreateMesh()
		{
			Vec3 size = new Vec3( .97f, .97f, .1f );

			Vec3[] positions;
			Vec3[] normals;
			int[] indices;
			GeometryGenerator.GenerateBox( size, out positions, out normals, out indices );

			string meshName = MeshManager.Instance.GetUniqueName(
				string.Format( "JigsawPuzzlePiece[{0},{1}]", index.X, index.Y ) );
			mesh = MeshManager.Instance.CreateManual( meshName );

			//create submesh
			SubMesh subMesh = mesh.CreateSubMesh();
			subMesh.UseSharedVertices = false;

			//init VertexData
			VertexDeclaration declaration = subMesh.VertexData.VertexDeclaration;
			declaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			declaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
			declaration.AddElement( 0, 24, VertexElementType.Float2,
				VertexElementSemantic.TextureCoordinates, 0 );

			VertexBufferBinding bufferBinding = subMesh.VertexData.VertexBufferBinding;
			HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				32, positions.Length, HardwareBuffer.Usage.StaticWriteOnly );
			bufferBinding.SetBinding( 0, vertexBuffer, true );

			subMesh.VertexData.VertexCount = positions.Length;

			unsafe
			{
				Vertex* buffer = (Vertex*)vertexBuffer.Lock( HardwareBuffer.LockOptions.Normal );

				for( int n = 0; n < positions.Length; n++ )
				{
					Vertex vertex = new Vertex();
					vertex.position = positions[ n ];
					vertex.normal = normals[ n ];

					if( JigsawPuzzleManager.Instance != null )
					{
						Vec2I pieceCount = JigsawPuzzleManager.Instance.PieceCount;

						Vec2I i = index;
						if( vertex.position.X > 0 )
							i.X++;
						if( vertex.position.Y > 0 )
							i.Y++;

						vertex.texCoord = new Vec2(
							(float)i.X / (float)pieceCount.X,
							1.0f - (float)i.Y / (float)pieceCount.Y );
					}

					*buffer = vertex;
					buffer++;
				}

				vertexBuffer.Unlock();
			}

			//calculate mesh bounds
			Bounds bounds = Bounds.Cleared;
			float radius = 0;
			foreach( Vec3 position in positions )
			{
				bounds.Add( position );
				float r = position.Length();
				if( r > radius )
					radius = r;
			}
			mesh.SetBoundsAndRadius( bounds, radius );

			//init IndexData
			subMesh.IndexData = IndexData.CreateFromArray( indices, 0, indices.Length, false );

			//init material
			subMesh.MaterialName = "JigsawPuzzleImage";
		}

		void DestroyMesh()
		{
			if( mesh != null )
			{
				mesh.Dispose();
				mesh = null;
			}
		}

		void CreateAttachedMesh()
		{
			attachedMesh = new MapObjectAttachedMesh();
			attachedMesh.MeshName = mesh.Name;
			Attach( attachedMesh );
		}

		void DestroyAttachedMesh()
		{
			if( attachedMesh != null )
			{
				Detach( attachedMesh );
				attachedMesh = null;
			}
		}

		///////////////////////////////////////////
		// Server side
		///////////////////////////////////////////

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			RemoteEntityWorld[] array = new RemoteEntityWorld[] { remoteEntityWorld };
			//send Index to the connected remote world
			Server_SendIndexToClients( array );
			//send Position to the connected remote world
			Server_SendPositionToClients( array );
		}

		protected override void Server_OnClientDisconnected( RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientDisconnected( remoteEntityWorld );

			//finish moving when user disconnected
			if( serverOrSingle_Moving )
			{
				EntitySystemServerNetworkService.ClientRemoteEntityWorld clientRemoteEntityWorld =
					(EntitySystemServerNetworkService.ClientRemoteEntityWorld)remoteEntityWorld;
				UserManagementServerNetworkService.UserInfo user = clientRemoteEntityWorld.User;

				if( user == server_movingByUser )
				{
					ServerOrSingle_MoveFinish();
				}
			}
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.MoveTryToBeginToServer )]
		void Server_ReceiveMoveTryToBegin( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;

			//already moving
			if( serverOrSingle_Moving )
				return;

			//get network user by remote entity world
			EntitySystemServerNetworkService.ClientRemoteEntityWorld clientRemoteEntityWorld =
				(EntitySystemServerNetworkService.ClientRemoteEntityWorld)sender;
			UserManagementServerNetworkService.UserInfo user = clientRemoteEntityWorld.User;

			Server_MoveBegin( user );
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.MoveUpdatePositionToServer )]
		void Server_ReceiveMoveUpdatePosition( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec2 newPosition = reader.ReadVec2();
			if( !reader.Complete() )
				return;

			//get network user by remote entity world
			EntitySystemServerNetworkService.ClientRemoteEntityWorld clientRemoteEntityWorld =
				(EntitySystemServerNetworkService.ClientRemoteEntityWorld)sender;
			UserManagementServerNetworkService.UserInfo user = clientRemoteEntityWorld.User;

			if( user == server_movingByUser )
			{
				ServerOrSingle_MoveUpdatePosition( newPosition );
			}
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.MoveTryToFinishToServer )]
		void Server_ReceiveMoveTryToFinish( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;

			//get network user by remote entity world
			EntitySystemServerNetworkService.ClientRemoteEntityWorld clientRemoteEntityWorld =
				(EntitySystemServerNetworkService.ClientRemoteEntityWorld)sender;
			UserManagementServerNetworkService.UserInfo user = clientRemoteEntityWorld.User;

			if( user == server_movingByUser )
			{
				ServerOrSingle_MoveFinish();
			}
		}

		public void ServerOrSingle_SetIndex( Vec2I index )
		{
			this.index = index;

			//send Index to clients
			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendIndexToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
		}

		void Server_SendIndexToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds,
				typeof( JigsawPuzzlePiece ), (ushort)NetworkMessages.IndexToClient );
			writer.Write( Index );
			EndNetworkMessage();
		}

		void Server_SendPositionToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds,
				typeof( JigsawPuzzlePiece ), (ushort)NetworkMessages.PositionToClient );
			writer.Write( Position );
			EndNetworkMessage();
		}

		public void Server_MoveBegin( UserManagementServerNetworkService.UserInfo user )
		{
			if( serverOrSingle_Moving )
				Log.Fatal( "JigsawPuzzlePiece: Server_BeginMoving: serverOrSingle_Moving == true." );

			serverOrSingle_Moving = true;
			serverOrSingle_movingTime = 0;
			server_movingByUser = user;

			//inform clients (send to the clients MoveBeginToClient message)
			SendDataWriter writer = BeginNetworkMessage( typeof( JigsawPuzzlePiece ),
				(ushort)NetworkMessages.MoveBeginToClient );
			writer.WriteVariableUInt32( server_movingByUser.Identifier );
			EndNetworkMessage();
		}

		public void Single_MoveBegin()
		{
			if( serverOrSingle_Moving )
				Log.Fatal( "JigsawPuzzlePiece: Server_BeginMoving: serverOrSingle_Moving == true." );

			serverOrSingle_Moving = true;
			serverOrSingle_movingTime = 0;
		}

		public void ServerOrSingle_MoveUpdatePosition( Vec2 newPosition )
		{
			if( !serverOrSingle_Moving )
				return;

			//clamp position
			Vec2 clampedPosition = newPosition;
			Rect gameArea = JigsawPuzzleManager.Instance.GetGameArea();
			gameArea.Expand( -.5f );
			clampedPosition.Clamp( gameArea.Minimum, gameArea.Maximum );

			//"Position" will be send to clients from JigsawPuzzlePiece.OnSetTransform() method
			Position = new Vec3( clampedPosition.X, clampedPosition.Y, .1f );
		}

		public void ServerOrSingle_MoveFinish()
		{
			if( !serverOrSingle_Moving )
				return;

			serverOrSingle_Moving = false;
			server_movingByUser = null;
			serverOrSingle_movingTime = 0;

			bool putToDestinationPlace = false;

			//check for destination place
			{
				Vec2 destination = JigsawPuzzleManager.Instance.GetPieceDestinationPosition( Index );
				if( Math.Abs( Position.X - destination.X ) < .2f &&
					Math.Abs( Position.Y - destination.Y ) < .2f )
				{
					//move to destination place

					//"Position" will be send to clients from JigsawPuzzlePiece.OnSetTransform() method
					Position = new Vec3( destination.X, destination.Y, 0 );

					putToDestinationPlace = true;
				}
			}

			//check for complete puzzle
			bool completePuzzle = true;
			{
				foreach( Entity entity in Map.Instance.Children )
				{
					JigsawPuzzlePiece piece = entity as JigsawPuzzlePiece;
					if( piece != null )
					{
						Vec2 destination = JigsawPuzzleManager.Instance.GetPieceDestinationPosition(
							piece.Index );
						if( Math.Abs( piece.Position.X - destination.X ) > .01f ||
							Math.Abs( piece.Position.Y - destination.Y ) > .01f )
						{
							completePuzzle = false;
							break;
						}
					}
				}
			}

			if( EntitySystemWorld.Instance.IsServer() )
			{
				SendDataWriter writer = BeginNetworkMessage( typeof( JigsawPuzzlePiece ),
					(ushort)NetworkMessages.MoveFinishToClient );
				writer.Write( putToDestinationPlace );
				writer.Write( completePuzzle );
				EndNetworkMessage();
			}

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				//play sounds
				if( putToDestinationPlace )
					ClientOrSingle_SoundPlay( "Maps\\JigsawPuzzleGame\\PutToDestinationPlace.ogg" );
				if( completePuzzle )
					ClientOrSingle_SoundPlay( "Maps\\JigsawPuzzleGame\\CompletePuzzle.ogg" );
			}
		}

		///////////////////////////////////////////
		// Client side
		///////////////////////////////////////////

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.IndexToClient )]
		void Client_ReceiveIndex( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec2I value = reader.ReadVec2I();
			if( !reader.Complete() )
				return;
			index = value;
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.PositionToClient )]
		void Client_ReceivePosition( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec3 value = reader.ReadVec3();
			if( !reader.Complete() )
				return;
			Position = value;
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.MoveBeginToClient )]
		void Client_ReceiveMoveBegin( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint userId = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			UserManagementClientNetworkService userService = GameNetworkClient.Instance.
				UserManagementService;

			client_movingByUser = userService.GetUser( userId );
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.MoveFinishToClient )]
		void Client_ReceiveMoveFinish( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool putToDestinationPlace = reader.ReadBoolean();
			bool completePuzzle = reader.ReadBoolean();
			if( !reader.Complete() )
				return;

			client_movingByUser = null;

			//play sounds
			if( putToDestinationPlace )
				ClientOrSingle_SoundPlay( "Maps\\JigsawPuzzleGame\\PutToDestinationPlace.ogg" );
			if( completePuzzle )
				ClientOrSingle_SoundPlay( "Maps\\JigsawPuzzleGame\\CompletePuzzle.ogg" );
		}

		void ClientOrSingle_SoundPlay( string soundName )
		{
			Sound sound = SoundWorld.Instance.SoundCreate( soundName, 0 );
			if( sound != null )
			{
				SoundWorld.Instance.SoundPlay( sound, EngineApp.Instance.DefaultSoundChannelGroup,
					.5f );
			}
		}

		[Browsable( false )]
		public bool ServerOrSingle_Moving
		{
			get { return serverOrSingle_Moving; }
		}

		[Browsable( false )]
		public UserManagementServerNetworkService.UserInfo Server_MovingByUser
		{
			get { return server_movingByUser; }
		}

		[Browsable( false )]
		public UserManagementClientNetworkService.UserInfo Client_MovingByUser
		{
			get { return client_movingByUser; }
		}

		public void Client_MoveTryToBegin()
		{
			//send message to server
			SendDataWriter writer = BeginNetworkMessage( typeof( JigsawPuzzlePiece ),
				(ushort)NetworkMessages.MoveTryToBeginToServer );
			EndNetworkMessage();
		}

		public void Client_MoveTryToFinish()
		{
			//send message to server
			SendDataWriter writer = BeginNetworkMessage( typeof( JigsawPuzzlePiece ),
				(ushort)NetworkMessages.MoveTryToFinishToServer );
			EndNetworkMessage();
		}

		public void Client_MoveUpdatePosition( Vec2 newPosition )
		{
			//send network message to server
			SendDataWriter writer = BeginNetworkMessage( typeof( JigsawPuzzlePiece ),
				(ushort)NetworkMessages.MoveUpdatePositionToServer );
			writer.Write( newPosition );
			EndNetworkMessage();
		}

	}
}
