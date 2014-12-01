// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Door"/> entity type.
	/// </summary>
	public class DoorType : DynamicType
	{
		[FieldSerialize]
		Vec3 openDoorBodyOffset = new Vec3( 0, 0, 1 );
		[FieldSerialize]
		Vec3 openDoor2BodyOffset = new Vec3( 0, 0, 1 );

		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float openTime = 1.0f;

		[FieldSerialize]
		string soundOpen;
		[FieldSerialize]
		string soundClose;

		//

		/// <summary>
		/// Gets or sets the displacement a position of a body "door" when the door is open.
		/// </summary>
		[Description( "The displacement a position of a body \"door\" when the door is open." )]
		[DefaultValue( typeof( Vec3 ), "0 0 1" )]
		public Vec3 OpenDoorBodyOffset
		{
			get { return openDoorBodyOffset; }
			set { openDoorBodyOffset = value; }
		}

		/// <summary>
		/// Gets or sets the displacement a position of a body "door2" when the door is open.
		/// </summary>
		[Description( "The displacement a position of a body \"door2\" when the door is open." )]
		[DefaultValue( typeof( Vec3 ), "0 0 1" )]
		public Vec3 OpenDoor2BodyOffset
		{
			get { return openDoor2BodyOffset; }
			set { openDoor2BodyOffset = value; }
		}

		/// <summary>
		/// Gets or set the time of opening/closing of a door.
		/// </summary>
		[Description( "The time of opening/closing of a door." )]
		[DefaultValue( 1.0f )]
		public float OpenTime
		{
			get { return openTime; }
			set { openTime = value; }
		}

		/// <summary>
		/// Gets or sets the sound at opening a door.
		/// </summary>
		[Description( "The sound at opening a door." )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundOpen
		{
			get { return soundOpen; }
			set { soundOpen = value; }
		}

		/// <summary>
		/// Gets or sets the sound at closing a door.
		/// </summary>
		[Description( "The sound at closing a door." )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundClose
		{
			get { return soundClose; }
			set { soundClose = value; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			PreloadSound( SoundOpen, SoundMode.Mode3D );
			PreloadSound( SoundClose, SoundMode.Mode3D );
		}
	}

	/// <summary>
	/// Defines the doors. That doors worked, it is necessary that the physical 
	/// model had a body with a name "door". This body will move at change of a status of a door.
	/// </summary>
	public class Door : Dynamic
	{
		[FieldSerialize]
		bool opened;
		[FieldSerialize]
		bool needOpen;
		[FieldSerialize]
		float openDoorOffsetCoefficient;

		Vec3 doorBody1InitPosition;
		Vec3 doorBody2InitPosition;
		Body doorBody1;
		Body doorBody2;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			OpenSettingsToClient,
			SoundOpenToClient,
			SoundCloseToClient,
		}

		///////////////////////////////////////////

		DoorType _type = null; public new DoorType Type { get { return _type; } }

		protected override void OnCreate()
		{
			base.OnCreate();
			needOpen = opened;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();

			//init doorBodyInitPosition and doorBody
			if( PhysicsModel != null )
			{
				for( int n = 0; n < PhysicsModel.Bodies.Length; n++ )
				{
					if( PhysicsModel.Bodies[ n ].Name == "door" || PhysicsModel.Bodies[ n ].Name == "door1" )
					{
						Mat4 transform = PhysicsModel.ModelDeclaration.Bodies[ n ].GetTransform();
						doorBody1InitPosition = transform.Item3.ToVec3();
					}
					else if( PhysicsModel.Bodies[ n ].Name == "door2" )
					{
						Mat4 transform = PhysicsModel.ModelDeclaration.Bodies[ n ].GetTransform();
						doorBody2InitPosition = transform.Item3.ToVec3();
					}
				}

				doorBody1 = PhysicsModel.GetBody( "door1" );
				if( doorBody1 == null )
					doorBody1 = PhysicsModel.GetBody( "door" );
				doorBody2 = PhysicsModel.GetBody( "door2" );
			}

			UpdateDoorBodies();
			UpdateAttachedObjects();
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );

			UpdateAttachedObjects();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( needOpen != opened || openDoorOffsetCoefficient != 0 )
			{
				float offset = TickDelta / Type.OpenTime;

				bool oldOpened = opened;
				float oldOpenDoorOffsetCoefficient = openDoorOffsetCoefficient;

				if( needOpen )
				{
					openDoorOffsetCoefficient += offset;
					if( openDoorOffsetCoefficient >= 1 )
					{
						openDoorOffsetCoefficient = 1;
						opened = needOpen;
					}
				}
				else
				{
					openDoorOffsetCoefficient -= offset;
					if( openDoorOffsetCoefficient <= 0 )
					{
						openDoorOffsetCoefficient = 0;
						opened = needOpen;
					}
				}

				if( oldOpened != opened || oldOpenDoorOffsetCoefficient != openDoorOffsetCoefficient )
				{
					if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
						Server_SendOpenSettingsToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
				}

				UpdateAttachedObjects();
			}

			UpdateDoorBodies();
		}

		void UpdateDoorBodies()
		{
			if( doorBody1 != null )
			{
				Vec3 pos = Position +
					( doorBody1InitPosition + Type.OpenDoorBodyOffset * openDoorOffsetCoefficient ) * Rotation;
				Vec3 oldPosition = doorBody1.Position;
				doorBody1.Position = pos;
				doorBody1.OldPosition = oldPosition;
			}
			if( doorBody2 != null )
			{
				Vec3 pos = Position +
					( doorBody2InitPosition + Type.OpenDoor2BodyOffset * openDoorOffsetCoefficient ) * Rotation;
				Vec3 oldPosition = doorBody2.Position;
				doorBody2.Position = pos;
				doorBody2.OldPosition = oldPosition;
			}

			//send event to clients in networking mode
			if( EntitySystemWorld.Instance.IsServer() &&
				Type.NetworkType == EntityNetworkTypes.Synchronized )
			{
				Server_SendBodiesPositionsToAllClients( false );
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the door is currently open.
		/// </summary>
		[DefaultValue( false )]
		[LogicSystemBrowsable( true )]
		virtual public bool Opened
		{
			get { return opened; }
			set
			{
				if( needOpen == value )
					return;

				needOpen = value;

				if( EntitySystemWorld.Instance.IsEditor() )
				{
					opened = value;
					openDoorOffsetCoefficient = opened ? 1 : 0;
					UpdateDoorBodies();
				}
				else
				{
					if( needOpen )
					{
						SoundPlay3D( Type.SoundOpen, .5f, false );

						//send message to client
						if( EntitySystemWorld.Instance.IsServer() )
						{
							if( Type.NetworkType == EntityNetworkTypes.Synchronized )
								Server_SendSoundOpenToAllClients();
						}
					}
					else
					{
						SoundPlay3D( Type.SoundClose, .5f, false );

						//send message to client
						if( EntitySystemWorld.Instance.IsServer() )
						{
							if( Type.NetworkType == EntityNetworkTypes.Synchronized )
								Server_SendSoundCloseToAllClients();
						}
					}
				}

				UpdateAttachedObjects();
			}
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendOpenSettingsToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendOpenSettingsToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Door ),
				(ushort)NetworkMessages.OpenSettingsToClient );
			writer.Write( opened );
			writer.Write( openDoorOffsetCoefficient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.OpenSettingsToClient )]
		void Client_ReceiveOpenSettings( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool value = reader.ReadBoolean();
			float value2 = reader.ReadSingle();
			if( !reader.Complete() )
				return;
			opened = value;
			openDoorOffsetCoefficient = value2;
		}

		void Server_SendSoundOpenToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Door ),
				(ushort)NetworkMessages.SoundOpenToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.SoundOpenToClient )]
		void Client_ReceiveSoundOpen( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			SoundPlay3D( Type.SoundOpen, .5f, false );
		}

		void Server_SendSoundCloseToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Door ),
				(ushort)NetworkMessages.SoundCloseToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.SoundCloseToClient )]
		void Client_ReceiveSoundClose( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			SoundPlay3D( Type.SoundClose, .5f, false );
		}

		void UpdateAttachedObjects()
		{
			bool visible = !opened && openDoorOffsetCoefficient == 0;

			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				if( attachedObject.Alias == "visibleWhenClosed" )
				{
					attachedObject.Visible = visible;

					MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
					if( attachedMapObject != null )
					{
						Occluder occluder = attachedMapObject.MapObject as Occluder;
						if( occluder != null )
							occluder.Enabled = visible;
					}
				}
			}
		}
	}
}
