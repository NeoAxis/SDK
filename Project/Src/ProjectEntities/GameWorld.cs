// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.Networking;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="GameWorld"/> entity type.
	/// </summary>
	public class GameWorldType : WorldType
	{
	}

	public class GameWorld : World
	{
		static GameWorld instance;

		//for moving player character between maps
		string needChangeMapName;
		string needChangeMapSpawnPointName;
		PlayerCharacter.ChangeMapInformation needChangeMapPlayerCharacterInformation;
		string needChangeMapPreviousMapName;

		bool needWorldDestroy;

		//

		GameWorldType _type = null; public new GameWorldType Type { get { return _type; } }

		public GameWorld()
		{
			instance = this;
		}

		public static new GameWorld Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();

			//create PlayerManager
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( PlayerManager.Instance == null )
				{
					PlayerManager manager = (PlayerManager)Entities.Instance.Create(
						"PlayerManager", this );
					manager.PostCreate();
				}
			}
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

			//single mode. recreate player units if need
			if( EntitySystemWorld.Instance.IsSingle() )
			{
				if( GameMap.Instance.GameType == GameMap.GameTypes.Action ||
					GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade ||
					GameMap.Instance.GameType == GameMap.GameTypes.TurretDemo ||
					GameMap.Instance.GameType == GameMap.GameTypes.VillageDemo ||
					GameMap.Instance.GameType == GameMap.GameTypes.PlatformerDemo )
				{
					if( PlayerManager.Instance != null )
					{
						foreach( PlayerManager.ServerOrSingle_Player player in
							PlayerManager.Instance.ServerOrSingle_Players )
						{
							if( player.Intellect == null || player.Intellect.ControlledObject == null )
							{
								ServerOrSingle_CreatePlayerUnit( player );
							}
						}
					}
				}
			}

			//networking mode
			if( EntitySystemWorld.Instance.IsServer() )
			{
				if( GameMap.Instance.GameType == GameMap.GameTypes.Action ||
					GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade ||
					GameMap.Instance.GameType == GameMap.GameTypes.TurretDemo ||
					GameMap.Instance.GameType == GameMap.GameTypes.VillageDemo ||
					GameMap.Instance.GameType == GameMap.GameTypes.PlatformerDemo )
				{
					if( PlayerManager.Instance != null )
					{
						UserManagementServerNetworkService userManagementService =
							GameNetworkServer.Instance.UserManagementService;

						//remove users
						again:
						foreach( PlayerManager.ServerOrSingle_Player player in
							PlayerManager.Instance.ServerOrSingle_Players )
						{
							if( player.User != null && player.User != userManagementService.ServerUser )
							{
								NetworkNode.ConnectedNode connectedNode = player.User.ConnectedNode;
								if( connectedNode == null ||
									connectedNode.Status != NetworkConnectionStatuses.Connected )
								{
									if( player.Intellect != null )
									{
										PlayerIntellect playerIntellect = player.Intellect as PlayerIntellect;
										if( playerIntellect != null )
											playerIntellect.TryToRestoreMainControlledUnit();

										if( player.Intellect.ControlledObject != null )
											player.Intellect.ControlledObject.Die();
										player.Intellect.SetForDeletion( true );
										player.Intellect = null;
									}

									PlayerManager.Instance.ServerOrSingle_RemovePlayer( player );

									goto again;
								}
							}
						}

						//add users
						foreach( UserManagementServerNetworkService.UserInfo user in
							userManagementService.Users )
						{
							//check whether "EntitySystem" service on the client
							if( user.ConnectedNode != null )
							{
								if( !user.ConnectedNode.RemoteServices.Contains( "EntitySystem" ) )
									continue;
							}

							PlayerManager.ServerOrSingle_Player player = PlayerManager.Instance.
								ServerOrSingle_GetPlayer( user );

							if( player == null )
							{
								player = PlayerManager.Instance.Server_AddClientPlayer( user );

								PlayerIntellect intellect = (PlayerIntellect)Entities.Instance.
									Create( "PlayerIntellect", World.Instance );
								intellect.PostCreate();

								player.Intellect = intellect;

								if( GameNetworkServer.Instance.UserManagementService.ServerUser != user )
								{
									//player on client
									RemoteEntityWorld remoteEntityWorld = GameNetworkServer.Instance.
										EntitySystemService.GetRemoteEntityWorld( user );
									intellect.Server_SendSetInstanceToClient( remoteEntityWorld );
								}
								else
								{
									//player on this server
									PlayerIntellect.SetInstance( intellect );
								}

							}
						}

						//create units
						foreach( PlayerManager.ServerOrSingle_Player player in
							PlayerManager.Instance.ServerOrSingle_Players )
						{
							if( player.Intellect != null && player.Intellect.ControlledObject == null )
							{
								ServerOrSingle_CreatePlayerUnit( player );
							}
						}
					}
				}
			}
		}

		internal void DoActionsAfterMapCreated()
		{
			if( EntitySystemWorld.Instance.IsSingle() )
			{
				if( GameMap.Instance.GameType == GameMap.GameTypes.Action ||
					GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade ||
					GameMap.Instance.GameType == GameMap.GameTypes.TurretDemo ||
					GameMap.Instance.GameType == GameMap.GameTypes.VillageDemo ||
					GameMap.Instance.GameType == GameMap.GameTypes.PlatformerDemo )
				{
					string playerName = "__SinglePlayer__";

					//create Player
					PlayerManager.ServerOrSingle_Player player = PlayerManager.Instance.
						ServerOrSingle_GetPlayer( playerName );
					if( player == null )
						player = PlayerManager.Instance.Single_AddSinglePlayer( playerName );

					//create PlayerIntellect
					PlayerIntellect intellect = null;
					{
						//find already created PlayerIntellect
						foreach( Entity entity in World.Instance.Children )
						{
							intellect = entity as PlayerIntellect;
							if( intellect != null )
								break;
						}

						if( intellect == null )
						{
							intellect = (PlayerIntellect)Entities.Instance.Create( "PlayerIntellect",
								World.Instance );
							intellect.PostCreate();

							player.Intellect = intellect;
						}

						//set instance
						if( PlayerIntellect.Instance == null )
							PlayerIntellect.SetInstance( intellect );
					}

					//create unit
					if( intellect.ControlledObject == null )
					{
						MapObject spawnPoint = null;
						if( !string.IsNullOrEmpty( needChangeMapSpawnPointName ) )
						{
							spawnPoint = Entities.Instance.GetByName( needChangeMapSpawnPointName ) as MapObject;
							if( spawnPoint == null )
							{
								if( GameMap.Instance.GameType != GameMap.GameTypes.TPSArcade )
									Log.Warning( "GameWorld: Object with name \"{0}\" is not exists.", needChangeMapSpawnPointName );
							}
						}

						Unit unit;
						if( spawnPoint != null )
							unit = ServerOrSingle_CreatePlayerUnit( player, spawnPoint );
						else
							unit = ServerOrSingle_CreatePlayerUnit( player );

						if( needChangeMapPlayerCharacterInformation != null )
						{
							PlayerCharacter playerCharacter = (PlayerCharacter)unit;
							playerCharacter.ApplyChangeMapInformation(
								needChangeMapPlayerCharacterInformation, spawnPoint );
						}
						else
						{
							if( unit != null )
							{
								intellect.LookDirection = SphereDir.FromVector(
									unit.Rotation.GetForward() );
							}
						}
					}
				}
			}

			needChangeMapName = null;
			needChangeMapSpawnPointName = null;
			needChangeMapPlayerCharacterInformation = null;
		}

		Unit ServerOrSingle_CreatePlayerUnit( PlayerManager.ServerOrSingle_Player player,
			MapObject spawnPoint )
		{
			string unitTypeName;
			if( !player.Bot )
			{
				if( GameMap.Instance.PlayerUnitType != null )
					unitTypeName = GameMap.Instance.PlayerUnitType.Name;
				else
					unitTypeName = "Girl";//"Rabbit";
			}
			else
				unitTypeName = player.Name;

			Unit unit = (Unit)Entities.Instance.Create( unitTypeName, Map.Instance );

			Vec3 posOffset = new Vec3( 0, 0, 1.5f );
			unit.Position = spawnPoint.Position + posOffset;
			unit.Rotation = spawnPoint.Rotation;
			unit.PostCreate();

			if( player.Intellect != null )
			{
				player.Intellect.ControlledObject = unit;
				unit.SetIntellect( player.Intellect, false );
			}

			Teleporter teleporter = spawnPoint as Teleporter;
			if( teleporter != null )
				teleporter.ReceiveObject( unit, null );

			return unit;
		}

		Unit ServerOrSingle_CreatePlayerUnit( PlayerManager.ServerOrSingle_Player player )
		{
			SpawnPoint spawnPoint = SpawnPoint.GetDefaultSpawnPoint();

			if( spawnPoint == null )
				spawnPoint = SpawnPoint.GetFreeRandomSpawnPoint();

			if( spawnPoint == null )
				return null;
			return ServerOrSingle_CreatePlayerUnit( player, spawnPoint );
		}

		public string NeedChangeMapName
		{
			get { return needChangeMapName; }
		}

		public string NeedChangeMapSpawnPointName
		{
			get { return needChangeMapSpawnPointName; }
		}

		public string NeedChangeMapPreviousMapName
		{
			get { return needChangeMapPreviousMapName; }
		}

		public void NeedChangeMap( string mapName, string spawnPointName,
			PlayerCharacter.ChangeMapInformation playerCharacterInformation )
		{
			if( needChangeMapName != null )
				return;
			needChangeMapName = mapName;
			needChangeMapSpawnPointName = spawnPointName;
			needChangeMapPlayerCharacterInformation = playerCharacterInformation;
			needChangeMapPreviousMapName = Map.Instance.VirtualFileName;
		}

		[Browsable( false )]
		public bool NeedWorldDestroy
		{
			get { return needWorldDestroy; }
			set { needWorldDestroy = value; }
		}
	}
}
