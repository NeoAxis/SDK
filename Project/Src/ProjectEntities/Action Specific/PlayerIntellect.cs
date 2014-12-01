// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="PlayerIntellect"/> entity type.
	/// </summary>
	public class PlayerIntellectType : IntellectType
	{
	}

	/// <summary>
	/// Represents intellect of the player.
	/// </summary>
	public class PlayerIntellect : Intellect
	{
		//in networking mode each client will have different instance. Reference to the his intellect.
		static PlayerIntellect instance;

		[FieldSerialize]
		SphereDir lookDirection;

		bool fpsCamera;//for hiding player unit for the fps camera
		float tpsCameraCenterOffset;

		//data for an opportunity of the player to control other units. (for Example: Turret control)
		[FieldSerialize]
		Unit mainNotActiveUnit;
		Dictionary<Shape, int> mainNotActiveUnitShapeContactGroups;
		[FieldSerialize]
		Vec3 mainNotActiveUnitRestorePosition;

		Vec3 client_sentTurnToPosition = new Vec3( float.NaN, 0, 0 );

		///////////////////////////////////////////

		enum NetworkMessages
		{
			SetInstanceToClient,
			MainNotActiveUnitToClient,

			ControlKeyPressToServer,
			ControlKeyReleaseToServer,
			TurnToPositionToServer,

			ChangeMainControlledUnitToServer,
			RestoreMainControlledUnitToServer,
		}

		///////////////////////////////////////////

		PlayerIntellectType _type = null; public new PlayerIntellectType Type { get { return _type; } }

		/// <summary>
		/// In networking mode each client will have different instance. Reference to the his intellect.
		/// </summary>
		public static PlayerIntellect Instance
		{
			get { return instance; }
		}

		public static void SetInstance( PlayerIntellect instance )
		{
			if( PlayerIntellect.instance != null && instance != null )
				Log.Fatal( "PlayerIntellect: SetInstance: Instance already initialized." );

			if( PlayerIntellect.instance != null )
			{
				//This entity will accept commands of the player
				if( GameControlsManager.Instance != null )
				{
					GameControlsManager.Instance.GameControlsEvent -=
						PlayerIntellect.instance.GameControlsManager_GameControlsEvent;
				}
			}

			PlayerIntellect.instance = instance;

			if( PlayerIntellect.instance != null )
			{
				//This entity will accept commands of the player
				if( GameControlsManager.Instance != null )
				{
					GameControlsManager.Instance.GameControlsEvent +=
						PlayerIntellect.instance.GameControlsManager_GameControlsEvent;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			Faction = (FactionType)EntityTypes.Instance.GetByName( "GoodFaction" );
			if( Faction == null )
				Log.Fatal( "PlayerIntellect: OnPostCreate: Faction == null." );

			AllowTakeItems = true;

			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate2(bool)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			if( loaded )
				UpdateMainControlledUnitAfterLoading();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();

			if( instance == this )
				SetInstance( null );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			TickCurrentUnitAllowPlayerControl();

			if( Instance == this )
			{
				Vec3 turnToPosition;
				if( GetTurnToPosition( out turnToPosition ) )
					UpdateTurnToPositionForUnits( turnToPosition );
			}

			if( mainNotActiveUnit != null )
				mainNotActiveUnit.Visible = false;
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();

			TickCurrentUnitAllowPlayerControl();

			if( Instance == this )
			{
				Vec3 turnToPosition;
				if( GetTurnToPosition( out turnToPosition ) )
				{
					UpdateTurnToPositionForUnits( turnToPosition );
					Client_SendTurnToPositionToServer( turnToPosition );
				}
			}
		}

		bool GetTurnToPosition( out Vec3 turnToPosition )
		{
			if( Instance != this )
				Log.Fatal( "PlayerIntellect: GetTurnToPosition: Instance != this." );

			turnToPosition = Vec3.Zero;

			if( ControlledObject == null )
				return false;

			//CutSceneManager specific
			if( CutSceneManager.Instance != null && CutSceneManager.Instance.CutSceneEnable )
				return false;

			Vec3 from;
			Vec3 dir;

			if( !fpsCamera )
			{
				from = ControlledObject.Position + new Vec3( 0, 0, tpsCameraCenterOffset );
				dir = lookDirection.GetVector();
			}
			else
			{
				from = ControlledObject.Position +
					ControlledObject.Type.FPSCameraOffset * ControlledObject.Rotation;
				dir = lookDirection.GetVector();
			}

			//invalid ray
			if( dir == Vec3.Zero || float.IsNaN( from.X ) || float.IsNaN( dir.X ) )
				return false;

			float distance = 1000.0f;

			turnToPosition = from + dir * distance;

			RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
				new Ray( from, dir * distance ), (int)ContactGroup.CastAll );
			foreach( RayCastResult result in piercingResult )
			{
				WaterPlane waterPlane = WaterPlane.GetWaterPlaneByBody( result.Shape.Body );

				if( waterPlane == null && result.Shape.ContactGroup == (int)ContactGroup.NoContact )
					continue;

				MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

				if( obj == ControlledObject )
					continue;

				if( waterPlane != null )
				{
					//ignore water from inside
					if( result.Shape.Body.GetGlobalBounds().IsContainsPoint( from ) )
						continue;
				}

				Dynamic dynamic = obj as Dynamic;
				if( dynamic != null )
				{
					if( dynamic.GetParentUnitHavingIntellect() == ControlledObject )
						continue;
				}

				turnToPosition = result.Position;
				break;
			}

			return true;
		}

		void UpdateTurnToPositionForUnits( Vec3 turnToPosition )
		{
			//Character
			Character character = ControlledObject as Character;
			if( character != null )
				character.SetTurnToPosition( turnToPosition );

			//Turret
			Turret turret = ControlledObject as Turret;
			if( turret != null )
				turret.SetMomentaryTurnToPosition( turnToPosition );

			//Tank
			Tank tank = ControlledObject as Tank;
			if( tank != null )
				tank.SetNeedTurnToPosition( turnToPosition );
		}

		void GameControlsManager_GameControlsEvent( GameControlsEventData e )
		{
			//GameControlsKeyDownEventData
			{
				GameControlsKeyDownEventData evt = e as GameControlsKeyDownEventData;
				if( evt != null )
				{
					if( GetControlKeyStrength( evt.ControlKey ) != evt.Strength )
					{
						ControlKeyPress( evt.ControlKey, evt.Strength );

						//send to server
						if( EntitySystemWorld.Instance.IsClientOnly() )
						{
							//send turnToPosition before fire
							if( evt.ControlKey == GameControlKeys.Fire1 ||
								evt.ControlKey == GameControlKeys.Fire2 )
							{
								if( Instance == this )//send message only to this player
								{
									Vec3 turnToPosition;
									if( GetTurnToPosition( out turnToPosition ) )
										Client_SendTurnToPositionToServer( turnToPosition );
								}
							}

							Client_SendControlKeyPressToServer( evt.ControlKey, evt.Strength );
						}
					}

					return;
				}
			}

			//GameControlsKeyUpEventData
			{
				GameControlsKeyUpEventData evt = e as GameControlsKeyUpEventData;
				if( evt != null )
				{
					if( IsControlKeyPressed( evt.ControlKey ) )
					{
						ControlKeyRelease( evt.ControlKey );

						//send to server
						if( EntitySystemWorld.Instance.IsClientOnly() )
							Client_SendControlKeyReleaseToServer( evt.ControlKey );
					}

					return;
				}
			}

			//TPS arcade, PlatformerDemo specific (camera observe)
			bool tpsArcade = GameMap.Instance != null &&
				GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade;
			bool platformerDemo = GameMap.Instance != null &&
				GameMap.Instance.GameType == GameMap.GameTypes.PlatformerDemo;

			//GameControlsMouseMoveEventData
			if( !tpsArcade && !platformerDemo )
			{
				GameControlsMouseMoveEventData evt = e as GameControlsMouseMoveEventData;
				if( evt != null )
				{
					Vec2 sens = GameControlsManager.Instance.MouseSensitivity * 2;

					lookDirection.Horizontal -= evt.MouseOffset.X * sens.X;
					lookDirection.Vertical -= evt.MouseOffset.Y * sens.Y;

					float limit = fpsCamera ? .1f : MathFunctions.PI / 8;
					if( lookDirection.Vertical < -( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = -( MathFunctions.PI / 2 - limit );
					if( lookDirection.Vertical > ( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = ( MathFunctions.PI / 2 - limit );

					return;
				}
			}

			//GameControlsTickEventData
			if( !tpsArcade && !platformerDemo )
			{
				GameControlsTickEventData evt = e as GameControlsTickEventData;
				if( evt != null )
				{
					Vec2 sensitivity = GameControlsManager.Instance.JoystickAxesSensitivity * 2;

					Vec2 offset = Vec2.Zero;
					offset.X -= GetControlKeyStrength( GameControlKeys.LookLeft );
					offset.X += GetControlKeyStrength( GameControlKeys.LookRight );
					offset.Y += GetControlKeyStrength( GameControlKeys.LookUp );
					offset.Y -= GetControlKeyStrength( GameControlKeys.LookDown );

					//Turret specific
					if( ControlledObject != null && ControlledObject is Turret )
					{
						offset.X -= GetControlKeyStrength( GameControlKeys.Left );
						offset.X += GetControlKeyStrength( GameControlKeys.Right );
						offset.Y += GetControlKeyStrength( GameControlKeys.Forward );
						offset.Y -= GetControlKeyStrength( GameControlKeys.Backward );
					}

					offset *= evt.Delta * sensitivity;

					lookDirection.Horizontal -= offset.X;
					lookDirection.Vertical += offset.Y;

					float limit = fpsCamera ? .1f : MathFunctions.PI / 8;
					if( lookDirection.Vertical < -( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = -( MathFunctions.PI / 2 - limit );
					if( lookDirection.Vertical > ( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = ( MathFunctions.PI / 2 - limit );

					return;
				}
			}

			//..
		}

		[Browsable( false )]
		public SphereDir LookDirection
		{
			get { return lookDirection; }
			set { lookDirection = value; }
		}

		[Browsable( false )]
		public bool FPSCamera
		{
			get { return fpsCamera; }
			set { fpsCamera = value; }
		}

		[Browsable( false )]
		public float TPSCameraCenterOffset
		{
			get { return tpsCameraCenterOffset; }
			set { tpsCameraCenterOffset = value; }
		}

		protected override void OnControlledObjectChange( Unit oldObject )
		{
			base.OnControlledObjectChange( oldObject );

			//update look direction
			if( ControlledObject != null )
				lookDirection = SphereDir.FromVector( ControlledObject.Rotation * new Vec3( 1, 0, 0 ) );

			//TankGame specific
			{
				//set small damage for player tank
				Tank oldTank = oldObject as Tank;
				if( oldTank != null )
					oldTank.ReceiveDamageCoefficient = 1;
				Tank tank = ControlledObject as Tank;
				if( tank != null )
					tank.ReceiveDamageCoefficient = .1f;
			}
		}

		public override bool IsActive()
		{
			return true;
		}

		[Browsable( false )]
		public Unit MainNotActiveUnit
		{
			get { return mainNotActiveUnit; }
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			//mainNotActiveUnit destroyed
			if( mainNotActiveUnit == entity )
			{
				if( !IsSetForDeletion )
				{
					if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
						ServerOrSingle_RestoreMainControlledUnit();
					else
						mainNotActiveUnit = null;
				}
				else
					mainNotActiveUnit = null;
			}
		}

		void ServerOrSingle_ChangeMainControlledUnit( Unit unit )
		{
			//Change player controlled unit
			mainNotActiveUnit = ControlledObject;

			//send mainNotActiveUnit to clients
			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendMainNotActiveUnitToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );

			if( mainNotActiveUnit != null )
			{
				SubscribeToDeletionEvent( mainNotActiveUnit );

				mainNotActiveUnit.SetIntellect( null, false );
				mainNotActiveUnitRestorePosition = mainNotActiveUnit.Position;

				//disable collision for shapes and save contact groups
				mainNotActiveUnitShapeContactGroups = new Dictionary<Shape, int>();
				foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
				{
					foreach( Shape shape in body.Shapes )
					{
						mainNotActiveUnitShapeContactGroups.Add( shape, shape.ContactGroup );
						shape.ContactGroup = (int)ContactGroup.NoContact;
					}
				}

				mainNotActiveUnit.Server_EnableSynchronizationPositionsToClients = false;

				ControlledObject = unit;
				unit.SetIntellect( this, false );
				unit.Destroying += AlternativeUnitAllowPlayerControl_Destroying;
			}
		}

		public void TryToChangeMainControlledUnit( Unit unit )
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				ServerOrSingle_ChangeMainControlledUnit( unit );
			}

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				SendDataWriter writer = BeginNetworkMessage( typeof( PlayerIntellect ),
					(ushort)NetworkMessages.ChangeMainControlledUnitToServer );
				writer.WriteVariableUInt32( unit.NetworkUIN );
				EndNetworkMessage();
			}
		}

		void UpdateMainControlledUnitAfterLoading()
		{
			if( mainNotActiveUnit == null )
				return;

			if( ControlledObject != null )
				ControlledObject.Destroying += AlternativeUnitAllowPlayerControl_Destroying;

			//disable collision for shapes and save contact groups
			mainNotActiveUnitShapeContactGroups = new Dictionary<Shape, int>();
			foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
			{
				foreach( Shape shape in body.Shapes )
				{
					mainNotActiveUnitShapeContactGroups.Add( shape, shape.ContactGroup );
					shape.ContactGroup = (int)ContactGroup.NoContact;
				}
			}
		}

		void AlternativeUnitAllowPlayerControl_Destroying( Entity entity )
		{
			if( ControlledObject == entity && mainNotActiveUnit != null )
			{
				//restore main player controlled unit
				ServerOrSingle_RestoreMainControlledUnit();
			}
		}

		Vec3 FindFreePositionForUnit( Unit unit, Vec3 center )
		{
			Vec3 volumeSize = unit.MapBounds.GetSize() + new Vec3( 2, 2, 0 );

			for( float zOffset = 0; ; zOffset += .3f )
			{
				for( float radius = 3; radius < 8; radius += .6f )
				{
					for( float angle = 0; angle < MathFunctions.PI * 2; angle += MathFunctions.PI / 32 )
					{
						Vec3 pos = center + new Vec3( MathFunctions.Cos( angle ),
							MathFunctions.Sin( angle ), 0 ) * radius + new Vec3( 0, 0, zOffset );

						Bounds volume = new Bounds( pos );
						volume.Expand( volumeSize * .5f );

						Body[] bodies = PhysicsWorld.Instance.VolumeCast(
							volume, (int)ContactGroup.CastOnlyContact );

						if( bodies.Length == 0 )
							return pos;
					}
				}
			}
		}

		void ServerOrSingle_RestoreMainControlledUnit()
		{
			if( mainNotActiveUnit == null )
				return;

			if( ControlledObject != null )
			{
				ControlledObject.SetIntellect( null, false );
				ControlledObject.Destroying -= AlternativeUnitAllowPlayerControl_Destroying;
			}

			if( !mainNotActiveUnit.IsSetForDeletion )
			{
				mainNotActiveUnit.Server_EnableSynchronizationPositionsToClients = true;

				mainNotActiveUnit.Position = mainNotActiveUnitRestorePosition;
				//find free position for movable player controlled units
				if( ControlledObject != null )
				{
					//Tank, Car specific
					if( ControlledObject is Tank || ControlledObject is Car )
					{
						mainNotActiveUnit.Position = FindFreePositionForUnit(
							mainNotActiveUnit, ControlledObject.Position );
					}
				}

				mainNotActiveUnit.OldPosition = mainNotActiveUnit.Position;

				mainNotActiveUnit.Visible = true;

				UnsubscribeToDeletionEvent( mainNotActiveUnit );

				//restore contact groups for shapes
				if( mainNotActiveUnitShapeContactGroups != null )
				{
					foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
					{
						foreach( Shape shape in body.Shapes )
						{
							int group;
							if( mainNotActiveUnitShapeContactGroups.TryGetValue( shape, out group ) )
								shape.ContactGroup = group;
						}
					}
					mainNotActiveUnitShapeContactGroups.Clear();
					mainNotActiveUnitShapeContactGroups = null;
				}

				mainNotActiveUnit.SetIntellect( this, false );

				ControlledObject = mainNotActiveUnit;
			}
			else
				ControlledObject = null;

			mainNotActiveUnit = null;

			//send mainNotActiveUnit to clients
			if( EntitySystemWorld.Instance.IsServer() )
				Server_SendMainNotActiveUnitToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
		}

		public void TryToRestoreMainControlledUnit()
		{
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				ServerOrSingle_RestoreMainControlledUnit();
			}

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				SendDataWriter writer = BeginNetworkMessage( typeof( PlayerIntellect ),
					(ushort)NetworkMessages.RestoreMainControlledUnitToServer );
				EndNetworkMessage();
			}
		}

		void TickCurrentUnitAllowPlayerControl()
		{
			if( mainNotActiveUnit == null )
				return;

			mainNotActiveUnit.Position = ControlledObject.Position;
			mainNotActiveUnit.OldPosition = mainNotActiveUnit.Position;

			//reset velocities
			foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
			{
				body.LinearVelocity = Vec3.Zero;
				body.AngularVelocity = Vec3.Zero;
			}
		}

		protected override void OnControlledObjectRenderFrame()
		{
			base.OnControlledObjectRenderFrame();

			if( Instance == this )
			{
				Vec3 turnToPosition;
				if( GetTurnToPosition( out turnToPosition ) )
					UpdateTurnToPositionForUnits( turnToPosition );
			}

			if( mainNotActiveUnit != null )
				mainNotActiveUnit.Visible = false;
		}

		public void UpdateTransformBeforeCameraPositionCalculation()
		{
			if( Instance == this )
			{
				if( EntitySystemWorld.Instance.IsClientOnly() )
				{
					if( ControlledObject != null )
						ControlledObject.Client_UpdatePositionsBySnapshots( true );
				}

				Vec3 turnToPosition;
				if( GetTurnToPosition( out turnToPosition ) )
					UpdateTurnToPositionForUnits( turnToPosition );
			}
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendMainNotActiveUnitToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		public void Server_SendSetInstanceToClient( RemoteEntityWorld remoteEntityWorld )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorld,
				typeof( PlayerIntellect ), (ushort)NetworkMessages.SetInstanceToClient );
			writer.WriteVariableUInt32( NetworkUIN );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.SetInstanceToClient )]
		void Client_ReceiveSetInstance( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint networkUIN = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			Entity entity = Entities.Instance.GetByNetworkUIN( networkUIN );
			SetInstance( (PlayerIntellect)entity );
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.ChangeMainControlledUnitToServer )]
		void Server_ReceiveChangeMainControlledUnit( RemoteEntityWorld sender,
			ReceiveDataReader reader )
		{
			//not safe. client can send networkUIN of any unit from any place.

			//check to ensure that other players can not send messages to another player
			if( !Server_CheckRemoteEntityWorldAssociatedWithThisIntellect( sender ) )
				return;

			uint unitNetworkUIN = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			Unit unit = Entities.Instance.GetByNetworkUIN( unitNetworkUIN ) as Unit;
			//unit is not exists
			if( unit == null )
				return;

			ServerOrSingle_ChangeMainControlledUnit( unit );
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.RestoreMainControlledUnitToServer )]
		void Server_ReceiveRestoreMainControlledUnit( RemoteEntityWorld sender,
			ReceiveDataReader reader )
		{
			//check to ensure that other players can not send messages to another player
			if( !Server_CheckRemoteEntityWorldAssociatedWithThisIntellect( sender ) )
				return;

			if( !reader.Complete() )
				return;

			ServerOrSingle_RestoreMainControlledUnit();
		}

		void Client_SendTurnToPositionToServer( Vec3 turnToPosition )
		{
			float epsilon = .01f;
			bool updated = !client_sentTurnToPosition.Equals( turnToPosition, epsilon );

			if( updated )
			{
				SendDataWriter writer = BeginNetworkMessage( typeof( PlayerIntellect ),
					(ushort)NetworkMessages.TurnToPositionToServer );
				writer.Write( turnToPosition );
				EndNetworkMessage();

				client_sentTurnToPosition = turnToPosition;
			}
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.TurnToPositionToServer )]
		void Server_ReceiveTurnToPosition( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//check to ensure that other players can not send messages to another player
			if( !Server_CheckRemoteEntityWorldAssociatedWithThisIntellect( sender ) )
				return;

			Vec3 value = reader.ReadVec3();
			if( !reader.Complete() )
				return;

			UpdateTurnToPositionForUnits( value );
		}

		void Server_SendMainNotActiveUnitToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( PlayerIntellect ),
				(ushort)NetworkMessages.MainNotActiveUnitToClient );
			writer.WriteVariableUInt32( mainNotActiveUnit != null ?
				mainNotActiveUnit.NetworkUIN : (uint)0 );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.MainNotActiveUnitToClient )]
		void Client_ReceiveMainNotActiveUnit( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint networkUIN = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			if( mainNotActiveUnit != null )
				mainNotActiveUnit.Visible = true;

			mainNotActiveUnit = Entities.Instance.GetByNetworkUIN( networkUIN ) as Unit;
		}

		void Client_SendControlKeyPressToServer( GameControlKeys controlKey, float strength )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( PlayerIntellect ),
				(ushort)NetworkMessages.ControlKeyPressToServer );
			writer.WriteVariableUInt32( (uint)controlKey );
			writer.Write( strength );
			EndNetworkMessage();
		}

		void Client_SendControlKeyReleaseToServer( GameControlKeys controlKey )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( PlayerIntellect ),
				(ushort)NetworkMessages.ControlKeyReleaseToServer );
			writer.WriteVariableUInt32( (uint)controlKey );
			EndNetworkMessage();
		}

		bool Server_CheckRemoteEntityWorldAssociatedWithThisIntellect( RemoteEntityWorld sender )
		{
			UserManagementServerNetworkService.UserInfo senderUser =
				( (EntitySystemServerNetworkService.ClientRemoteEntityWorld)sender ).User;

			if( PlayerManager.Instance != null )
			{
				PlayerManager.ServerOrSingle_Player player = PlayerManager.Instance.
					ServerOrSingle_GetPlayer( this );

				if( senderUser != player.User )
					return false;
			}

			return true;
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.ControlKeyPressToServer )]
		void Server_ReceiveControlKeyPress( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//check to ensure that other players can not send messages to another player
			if( !Server_CheckRemoteEntityWorldAssociatedWithThisIntellect( sender ) )
				return;

			GameControlKeys controlKey = (GameControlKeys)reader.ReadVariableUInt32();
			float strength = reader.ReadSingle();
			if( !reader.Complete() )
				return;

			//check for invalid value
			if( !Enum.IsDefined( typeof( GameControlKeys ), (int)controlKey ) )
				return;
			if( strength <= 0 )
				return;

			ControlKeyPress( controlKey, strength );
		}

		[NetworkReceive( NetworkDirections.ToServer, (ushort)NetworkMessages.ControlKeyReleaseToServer )]
		void Server_ReceiveControlKeyRelease( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			//check to ensure that other players can not send messages to another player
			if( !Server_CheckRemoteEntityWorldAssociatedWithThisIntellect( sender ) )
				return;

			GameControlKeys controlKey = (GameControlKeys)reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			//check for invalid value
			if( !Enum.IsDefined( typeof( GameControlKeys ), (int)controlKey ) )
				return;

			ControlKeyRelease( controlKey );
		}

		public override bool IsAlwaysRun()
		{
			//is not supported in networking mode.
			if( GameControlsManager.Instance != null )
				return GameControlsManager.Instance.AlwaysRun;
			else
				return true;
			//return base.IsAlwaysRun();
		}

	}
}
