// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="GameMap"/> entity type.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class GameMapType : MapType
	{
	}

	public class GameMap : Map
	{
		static GameMap instance;

		[FieldSerialize]
		[DefaultValue( GameMap.GameTypes.Action )]
		GameTypes gameType = GameTypes.Action;

		[FieldSerialize]
		string gameMusic = "Sounds\\Music\\Game.ogg";

		//Wind settings
		[FieldSerialize]
		Radian windDirection;
		[FieldSerialize]
		float windSpeed = 1;

		[FieldSerialize]
		UnitType playerUnitType;

		[FieldSerialize]
		bool resetYCoordinateForDynamicBodies;

		[FieldSerialize]
		float destroyObjectsBelowHeight = -500;

		///////////////////////////////////////////

		public enum GameTypes
		{
			None,
			Action,
			RTS,
			TPSArcade,
			TurretDemo,
			JigsawPuzzleGame,
			BallGame,
			VillageDemo,
			CatapultGame,
			PlatformerDemo,
			PathfindingDemo,

			//Put here your game type.
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			GameTypeToClient
		}

		///////////////////////////////////////////

		GameMapType _type = null; public new GameMapType Type { get { return _type; } }

		public GameMap()
		{
			instance = this;
		}

		public static new GameMap Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(bool)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			PhysicsWorld.Instance.MainScene.PostStep += MainPhysicsScene_PostStep;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate2(bool)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			GameWorld gameWorld = Parent as GameWorld;
			if( gameWorld != null )
				gameWorld.DoActionsAfterMapCreated();

			UpdateWindSpeedSettings();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			PhysicsWorld.Instance.MainScene.PostStep -= MainPhysicsScene_PostStep;

			base.OnDestroy();
			instance = null;
		}

		/// <summary>
		/// The type of creating map.
		/// </summary>
		/// <remarks>
		/// By default, you can choose several options such as: Action, RTS, TPSArcade, TurrentDemo, VillageDemo and others. Each type differs by common logic and features of the interaction with the player. Developers can add their own types of maps.
		/// </remarks>
		[LocalizedDescription( "The type of creating map. By default, you can choose several options such as: Action, RTS, TPSArcade, TurrentDemo, VillageDemo and others. Each type differs by common logic and features of the interaction with the player. Developers can add their own types of maps.", "GameMap" )]
		[DefaultValue( GameMap.GameTypes.Action )]
		public GameTypes GameType
		{
			get { return gameType; }
			set
			{
				gameType = value;

				//send to clients
				if( EntitySystemWorld.Instance.IsServer() )
					Server_SendGameTypeToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
			}
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//here you can to preload resources for your specific map.
			//
			//example:
			//EntityType entityType = EntityTypes.Instance.GetByName( "MyEntity" );
			//if( entityType != null )
			//entityType.PreloadResources();
		}

		/// <summary>
		/// The file name of the background music.
		/// </summary>
		[LocalizedDescription( "The file name of the background music.", "GameMap" )]
		[DefaultValue( "Sounds\\Music\\Game.ogg" )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string GameMusic
		{
			get { return gameMusic; }
			set { gameMusic = value; }
		}

		void Server_SendGameTypeToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( GameMap ),
				(ushort)NetworkMessages.GameTypeToClient );
			writer.WriteVariableUInt32( (uint)gameType );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.GameTypeToClient )]
		void Client_ReceiveGameType( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			GameTypes value = (GameTypes)reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;
			gameType = value;
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			//send gameType value to the connected world
			Server_SendGameTypeToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		protected override bool OnIsBillboardVisibleByCamera( Camera camera, float cameraVisibleStartOffset,
			Vec3 billboardPosition, object billboardOwner )
		{
			//We can override behaviour of billboard visiblity check when CameraVisibleCheck property is True.
			//MapObjectAttachedBillboard, CameraAttachedObject classes are supported.
			//By default visibility is checking by mean frustum and by mean physics ray cast.

			return base.OnIsBillboardVisibleByCamera( camera, cameraVisibleStartOffset, billboardPosition,
				billboardOwner );
		}

		/// <summary>
		/// The direction of the wind.
		/// </summary>
		/// <remarks>
		/// At the moment this parameter is used only for animating vegetation materials (Vegetation material).
		/// </remarks>
		[LocalizedDescription( "The direction of the wind. At the moment this parameter is used only for animating vegetation materials (Vegetation material).", "GameMap" )]
		[DefaultValue( typeof( Radian ), "0" )]
		[TypeConverter( typeof( RadianAsDegreeConverter ) )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, MathFunctions.PI * 2 )]
		public Radian WindDirection
		{
			get { return windDirection; }
			set
			{
				if( windDirection == value )
					return;
				windDirection = value;
				UpdateWindSpeedSettings();
			}
		}

		/// <summary>
		/// The speed of the wind.
		/// </summary>
		/// <remarks>
		/// At the moment this parameter is used only for animating vegetation materials (Vegetation material).
		/// </remarks>
		[LocalizedDescription( "The speed of the wind. At the moment this parameter is used only for animating vegetation materials (Vegetation material).", "GameMap" )]
		[DefaultValue( 1f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 20 )]
		public float WindSpeed
		{
			get { return windSpeed; }
			set
			{
				if( windSpeed == value )
					return;
				windSpeed = value;
				UpdateWindSpeedSettings();
			}
		}

		void UpdateWindSpeedSettings()
		{
			Vec2 speed = new Vec2(
				MathFunctions.Cos( windDirection ) * windSpeed,
				MathFunctions.Sin( windDirection ) * windSpeed );

			//update Vegetation materials
			foreach( VegetationMaterial material in VegetationMaterial.AllVegetationMaterials )
				material.UpdateGlobalWindSettings( speed );
		}

		/// <summary>
		/// The type of object that will be used when creating a player.
		/// </summary>
		/// <remarks>
		/// Usually if the parameter is not specified, by default a character of a girl is created (Types\Units\Girl\Girl.type).
		/// </remarks>
		[LocalizedDescription( "The type of object that will be used when creating a player. Usually if the parameter is not specified, by default a character of a girl is created (Types\\Units\\Girl\\Girl.type).", "GameMap" )]
		public UnitType PlayerUnitType
		{
			get { return playerUnitType; }
			set { playerUnitType = value; }
		}

		/// <summary>
		/// A value indicating whether the two-dimensional calculation mode physics enabled.
		/// </summary>
		/// <remarks>
		/// After activating this parameter, the position of physical bodies on the Y axis will be reset to zero.
		/// </remarks>
		[LocalizedDescription( "A value indicating whether the two-dimensional calculation mode physics enabled. After activating this parameter, the position of physical bodies on the Y axis will be reset to zero.", "GameMap" )]
		[DefaultValue( false )]
		public bool ResetYCoordinateForDynamicBodies
		{
			get { return resetYCoordinateForDynamicBodies; }
			set { resetYCoordinateForDynamicBodies = value; }
		}

		/// <summary>
		/// Height below which all objects will be removed.
		/// </summary>
		[LocalizedDescription( "Height below which all objects will be removed.", "GameMap" )]
		[DefaultValue( -500 )]
		public float DestroyObjectsBelowHeight
		{
			get { return destroyObjectsBelowHeight; }
			set { destroyObjectsBelowHeight = value; }
		}

		void DoResetYCoordinateForDynamicBodies()
		{
			foreach( Body body in PhysicsWorld.Instance.MainScene.Bodies )
			{
				if( !body.Sleeping )
				{
					if( Math.Abs( body.Position.Y ) > .005f )
						body.Position = new Vec3( body.Position.X, 0, body.Position.Z );

					Vec3 forward = body.Rotation.GetForward();
					if( Math.Abs( forward.Y ) > .05f )
					{
						forward.Y = 0;
						forward.Normalize();
						float angle = MathFunctions.ATan( forward.Z, forward.X );
						body.Rotation = new Angles( 0, MathFunctions.RadToDeg( angle ), 0 ).ToQuat();
					}

					if( Math.Abs( body.LinearVelocity.Y ) > .01f )
						body.LinearVelocity = new Vec3( body.LinearVelocity.X, 0, body.LinearVelocity.Z );

					if( Math.Abs( body.AngularVelocity.X ) > .05f || Math.Abs( body.AngularVelocity.Z ) > .05f )
						body.AngularVelocity = new Vec3( 0, body.AngularVelocity.Y, 0 );
				}
			}
		}

		void MainPhysicsScene_PostStep( PhysicsScene scene )
		{
			if( resetYCoordinateForDynamicBodies )
				DoResetYCoordinateForDynamicBodies();
		}

	}
}
