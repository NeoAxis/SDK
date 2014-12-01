// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.PhysicsSystem;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Turret"/> entity type.
	/// </summary>
	public class TurretType : UnitType
	{
	}

	/// <summary>
	/// Gives an opportunity of creation of the turrets.
	/// A turret can be rotated. Guns are attached on the tower and player can 
	/// control the aiming and shooting of the turret.
	/// </summary>
	public class Turret : Unit
	{
		Body turretBody;
		Body baseBody;

		MapObjectAttachedMapObject mainGunAttachedObject;
		Gun mainGun;
		Vec3 mainGunOffsetPosition;

		Vec3 server_shouldSendTurnToPosition = new Vec3( 10000, 0, 0 );
		Vec3 server_sentTurnToPosition = new Vec3( 10000, 0, 0 );

		//

		///////////////////////////////////////////

		enum NetworkMessages
		{
			TurnToPositionToClient,
		}

		///////////////////////////////////////////

		TurretType _type = null; public new TurretType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( PhysicsModel != null )
			{
				turretBody = PhysicsModel.GetBody( "turret" );
				baseBody = PhysicsModel.GetBody( "base" );
			}

			if( !EntitySystemWorld.Instance.IsEditor() )
			{
				if( turretBody == null )
					Log.Error( "Turret: \"turret\" body not exists." );
				if( baseBody == null )
					Log.Error( "Turret: \"base\" body not exists." );
			}

			//mainGun
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject == null )
					continue;

				mainGun = attachedMapObject.MapObject as Gun;
				if( mainGun != null )
				{
					mainGunAttachedObject = attachedMapObject;
					mainGunOffsetPosition = attachedMapObject.PositionOffset;
					break;
				}
			}

			SubscribeToTickEvent();
		}

		public void SetMomentaryTurnToPosition( Vec3 pos )
		{
			if( ( pos - Position ).Length() < 4.0f )
			{
				Vec3 dir = ( pos - Position ).GetNormalize();
				pos += dir * 10;
			}

			MomentaryTurnToPositionUpdate( pos );
		}

		void MomentaryTurnToPositionUpdate( Vec3 turnToPosition )
		{
			if( turretBody == null || baseBody == null || mainGunAttachedObject == null )
				return;

			Vec3 diff = turnToPosition - Position;

			Radian horizontalAngle = MathFunctions.ATan( diff.Y, diff.X );
			Radian verticalAngle = MathFunctions.ATan( diff.Z, diff.ToVec2().Length() );

			turretBody.Rotation = new Angles( 0, 0, -horizontalAngle.InDegrees() ).ToQuat();

			Quat rot = baseBody.Rotation.GetInverse() * turretBody.Rotation;

			Quat verticalRot = new Angles( 0, verticalAngle.InDegrees(), 0 ).ToQuat();

			mainGunAttachedObject.PositionOffset = rot * mainGunOffsetPosition;
			mainGunAttachedObject.RotationOffset = rot * verticalRot;
			RecalculateMapBounds();

			if( EntitySystemWorld.Instance.IsServer() )
				server_shouldSendTurnToPosition = turnToPosition;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( Intellect != null )
			{
				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire1 ) )
					GunsTryFire( false );
				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire2 ) )
					GunsTryFire( true );
			}

			//send turn to position to clients
			if( EntitySystemWorld.Instance.IsServer() )
			{
				float epsilon = .05f;
				if( !server_shouldSendTurnToPosition.Equals( server_sentTurnToPosition, epsilon ) )
				{
					Server_SendTurnToPositionToClients( EntitySystemWorld.Instance.RemoteEntityWorlds,
						server_shouldSendTurnToPosition );
					server_sentTurnToPosition = server_shouldSendTurnToPosition;
				}
			}
		}

		protected override void OnIntellectCommand( Intellect.Command command )
		{
			base.OnIntellectCommand( command );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
			{
				if( command.KeyPressed )
				{
					if( command.Key == GameControlKeys.Fire1 )
						GunsTryFire( false );
					if( command.Key == GameControlKeys.Fire2 )
						GunsTryFire( true );
				}
			}
		}

		void GunsTryFire( bool alternative )
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject == null )
					continue;

				Gun gun = attachedMapObject.MapObject as Gun;

				if( gun != null )
					gun.TryFire( alternative );
			}
		}

		[Browsable( false )]
		public Gun MainGun
		{
			get { return mainGun; }
		}

		protected override void Server_OnClientConnectedAfterPostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedAfterPostCreate( remoteEntityWorld );

			Server_SendTurnToPositionToClients( new RemoteEntityWorld[] { remoteEntityWorld },
				server_sentTurnToPosition );
		}

		void Server_SendTurnToPositionToClients( IList<RemoteEntityWorld> remoteEntityWorlds,
			Vec3 turnToPosition )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Turret ),
				(ushort)NetworkMessages.TurnToPositionToClient );
			writer.Write( turnToPosition );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.TurnToPositionToClient )]
		void Client_ReceiveTurnToPosition( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec3 turnToPosition = reader.ReadVec3();
			if( !reader.Complete() )
				return;
			SetMomentaryTurnToPosition( turnToPosition );
		}

		public override void GetFirstPersonCameraPosition( out Vec3 position, out Vec3 forward, out Vec3 up )
		{
			position = mainGun.GetInterpolatedPosition() + Type.FPSCameraOffset * mainGun.GetInterpolatedRotation();
			if( PlayerIntellect.Instance != null )
				forward = PlayerIntellect.Instance.LookDirection.GetVector();
			else
				forward = Vec3.XAxis;
			up = Vec3.ZAxis;
		}
	}
}
