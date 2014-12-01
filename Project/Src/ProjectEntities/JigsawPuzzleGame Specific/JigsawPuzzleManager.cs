// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.UISystem;
using Engine.Renderer;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="JigsawPuzzleManager"/> entity type.
	/// </summary>
	public class JigsawPuzzleManagerType : MapGeneralObjectType
	{
		public JigsawPuzzleManagerType()
		{
			UniqueEntityInstance = true;
			AllowEmptyName = true;
		}
	}

	/// <summary>
	/// The pieces manager for puzzle game example.
	/// </summary>
	public class JigsawPuzzleManager : MapGeneralObject
	{
		static JigsawPuzzleManager instance;

		Vec2I pieceCount;//network synchronized

		JigsawPuzzleManagerType _type = null; public new JigsawPuzzleManagerType Type { get { return _type; } }

		MeshObject backgroundImageMeshObject;
		SceneNode backgroundImageSceneNode;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			PieceCountToClient,
		}

		///////////////////////////////////////////

		public JigsawPuzzleManager()
		{
			if( instance != null )
				Log.Fatal( "JigsawPuzzleManager: instance already created." );
			instance = this;
		}

		public static JigsawPuzzleManager Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			instance = this;//for undo support

			base.OnPostCreate( loaded );

			SubscribeToTickEvent();

			//generate puzzles
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				ServerOrSingle_GeneratePuzzles( new Vec2I( 8, 6 ) );

			CreateBackgroundImageMeshObject();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();

			DestroyBackgroundImageMeshObject();

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				ServerOrSingle_DestroyPuzzles();

			if( instance == this )//for undo support
				instance = null;
		}

		[Browsable( false )]
		public Vec2I PieceCount
		{
			get { return pieceCount; }
		}

		void ServerOrSingle_SetPieceCount( Vec2I pieceCount )
		{
			this.pieceCount = pieceCount;

			//send pieceCount to clients
			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendPieceCountToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
		}

		void ServerOrSingle_GeneratePuzzles( Vec2I pieceCount )
		{
			ServerOrSingle_DestroyPuzzles();

			ServerOrSingle_SetPieceCount( pieceCount );

			for( int y = 0; y < pieceCount.Y; y++ )
				for( int x = 0; x < pieceCount.X; x++ )
					ServerOrSingle_CreatePiece( new Vec2I( x, y ) );
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			//send PieceCount to the connected remote world
			Server_SendPieceCountToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendPieceCountToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds,
				typeof( JigsawPuzzleManager ), (ushort)NetworkMessages.PieceCountToClient );
			writer.Write( pieceCount );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.PieceCountToClient )]
		void Client_ReceivePieceCount( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec2I value = reader.ReadVec2I();
			if( !reader.Complete() )
				return;
			pieceCount = value;
		}

		void ServerOrSingle_CreatePiece( Vec2I index )
		{
			JigsawPuzzlePiece piece = (JigsawPuzzlePiece)Entities.Instance.Create(
				"JigsawPuzzlePiece", Map.Instance );

			piece.ServerOrSingle_SetIndex( index );

			//calculate position
			{
				Rect area = GetGameArea();
				area.Expand( -.5f );
				Rect exceptArea = GetDestinationArea();
				exceptArea.Expand( 1 );

				float x = 0;
				float y = 0;

				bool free = false;
				do
				{
					free = true;

					EngineRandom random = World.Instance.Random;
					x = area.Minimum.X + random.NextFloat() * area.Size.X;
					y = area.Minimum.Y + random.NextFloat() * area.Size.Y;

					if( exceptArea.IsContainsPoint( new Vec2( x, y ) ) )
						free = false;

					Bounds checkBounds = new Bounds(
						new Vec3( x - .5f, y - .5f, -100 ),
						new Vec3( x + .5f, y + .5f, 100 ) );

					Map.Instance.GetObjects( checkBounds, delegate( MapObject mapObject )
						{
							JigsawPuzzlePiece p = mapObject as JigsawPuzzlePiece;
							if( p != null )
								free = false;
						} );

				} while( !free );

				piece.Position = new Vec3( x, y, .1f );
			}

			piece.PostCreate();
		}

		public Vec2 GetPieceDestinationPosition( Vec2I index )
		{
			//piece size is always 1,1
			return new Vec2( .5f, .5f ) + index.ToVec2();
		}

		void ServerOrSingle_DestroyPuzzles()
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				JigsawPuzzlePiece piece = entity as JigsawPuzzlePiece;
				if( piece != null )
					piece.SetForDeletion( true );
			}
		}

		public Rect GetDestinationArea()
		{
			return new Rect(
				GetPieceDestinationPosition( Vec2I.Zero ) - new Vec2( .5f, .5f ),
				GetPieceDestinationPosition( PieceCount - new Vec2I( 1, 1 ) ) + new Vec2( .5f, .5f ) );
		}

		public Rect GetGameArea()
		{
			Rect area = GetDestinationArea();
			area.Expand( area.Size );
			return area;
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			//update background area image
			if( backgroundImageMeshObject != null )
			{
				Rect area = GetDestinationArea();
				backgroundImageSceneNode.Position = new Vec3(
					( area.Maximum.X + area.Minimum.X ) / 2,
					( area.Maximum.Y + area.Minimum.Y ) / 2, -.04f );
				backgroundImageSceneNode.Scale = new Vec3( area.Size.X, area.Size.Y, 1 );
			}

			if( camera.Purpose == Camera.Purposes.MainCamera )
			{
				//render destination area
				{
					Rect area = GetDestinationArea();
					area.Expand( .1f );
					Bounds b = new Bounds(
						area.Minimum.X, area.Minimum.Y, 0,
						area.Maximum.X, area.Maximum.Y, 0 );
					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
					camera.DebugGeometry.AddBounds( b );
				}

				//render game area
				{
					Rect area = GetGameArea();
					area.Expand( .1f );
					Bounds b = new Bounds(
						area.Minimum.X, area.Minimum.Y, 0,
						area.Maximum.X, area.Maximum.Y, 0 );
					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0 );
					camera.DebugGeometry.AddBounds( b );
				}
			}
		}

		void CreateBackgroundImageMeshObject()
		{
			backgroundImageMeshObject = SceneManager.Instance.CreateMeshObject(
				"Maps\\JigsawPuzzleGame\\BackgroundImage.mesh" );

			if( backgroundImageMeshObject != null )
			{
				backgroundImageSceneNode = new SceneNode();
				backgroundImageSceneNode.Attach( backgroundImageMeshObject );
			}
		}

		void DestroyBackgroundImageMeshObject()
		{
			if( backgroundImageMeshObject != null )
			{
				backgroundImageMeshObject.Dispose();
				backgroundImageMeshObject = null;
				backgroundImageSceneNode.Dispose();
				backgroundImageSceneNode = null;
			}
		}

	}
}
