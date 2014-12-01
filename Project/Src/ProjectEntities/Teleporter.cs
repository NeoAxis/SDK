// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using Engine;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.FileSystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Teleporter"/> entity type.
	/// </summary>
	public class TeleporterType : DynamicType
	{
		[FieldSerialize]
		float teleportTransitTime = .3f;
		[FieldSerialize]
		float activeAreaHeight = 3;
		[FieldSerialize]
		float activeAreaRadius = 1;
		[FieldSerialize]
		string sendParticleName = "";
		[FieldSerialize]
		string receiveParticleName = "";
		[FieldSerialize]
		string soundTeleportation = "";

		[DefaultValue( .3f )]
		public float TeleportTransitTime
		{
			get { return teleportTransitTime; }
			set { teleportTransitTime = value; }
		}

		[DefaultValue( 3.0f )]
		public float ActiveAreaHeight
		{
			get { return activeAreaHeight; }
			set { activeAreaHeight = value; }
		}

		[DefaultValue( 1.0f )]
		public float ActiveAreaRadius
		{
			get { return activeAreaRadius; }
			set { activeAreaRadius = value; }
		}

		[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
		public string SendParticleName
		{
			get { return sendParticleName; }
			set { sendParticleName = value; }
		}

		[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
		public string ReceiveParticleName
		{
			get { return receiveParticleName; }
			set { receiveParticleName = value; }
		}

		[DefaultValue( "" )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		public string SoundTeleportation
		{
			get { return soundTeleportation; }
			set { soundTeleportation = value; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			PreloadSound( SoundTeleportation, 0 );
			PreloadSound( SoundTeleportation, SoundMode.Mode3D );
		}
	}

	/// <summary>
	/// Defines the teleporter for transfering objects.
	/// </summary>
	public class Teleporter : Dynamic
	{
		[FieldSerialize]
		bool active = true;

		[FieldSerialize]
		Teleporter destination;
		[FieldSerialize]
		string changeMapName = "";
		[FieldSerialize]
		string changeMapSpawnObjectName = "";

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float teleportTransitProgress;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		MapObject objectWhichActivatesTransition;

		Set<MapObject> processedObjectsInActiveArea = new Set<MapObject>();
		int skipTicks;

		TeleporterType _type = null; public new TeleporterType Type { get { return _type; } }

		/// <summary>
		/// Gets or sets a value indicating whether the teleporter is currently active. 
		/// </summary>
		[Description( "A value indicating whether the teleporter is currently active." )]
		[DefaultValue( true )]
		public bool Active
		{
			get { return active; }
			set
			{
				if( active == value )
					return;
				active = value;
				if( IsPostCreated )
					UpdateAttachedObjects();
			}
		}

		/// <summary>
		/// Gets or sets the destination teleporter.
		/// </summary>
		[Description( "The destination teleporter." )]
		public Teleporter Destination
		{
			get { return destination; }
			set
			{
				if( value == this )
					throw new Exception( "To itself to refer it is impossible." );

				if( destination != null )
					UnsubscribeToDeletionEvent( destination );
				destination = value;
				if( destination != null )
					SubscribeToDeletionEvent( destination );
			}
		}

		public string ChangeMapName
		{
			get { return changeMapName; }
			set { changeMapName = value; }
		}

		public string ChangeMapSpawnObjectName
		{
			get { return changeMapSpawnObjectName; }
			set { changeMapSpawnObjectName = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			SubscribeToTickEvent();
			UpdateAttachedObjects();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( entity == destination )
				destination = null;
		}

		protected override void OnTick()
		{
			base.OnTick();

			if( skipTicks == 0 )
			{
				if( teleportTransitProgress != 0 )
				{
					//teleportation progress
					teleportTransitProgress += TickDelta;
					if( teleportTransitProgress >= Type.TeleportTransitTime )
						DoTransfer();
				}
				else
				{
					bool enable = false;
					if( Active )
					{
						if( !string.IsNullOrEmpty( changeMapName ) &&
							EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationTypes.Single )
						{
							enable = true;
						}
						if( destination != null )
							enable = true;
					}

					if( enable )
					{
						Set<MapObject> objects = GetObjectsInActiveArea();

						//remove objects from list of processed objects
						reply:
						foreach( MapObject obj in processedObjectsInActiveArea )
						{
							if( !objects.Contains( obj ) )
							{
								processedObjectsInActiveArea.Remove( obj );
								goto reply;
							}
						}

						//find object for activation teleportation
						foreach( MapObject obj in objects )
						{
							if( !processedObjectsInActiveArea.Contains( obj ) )
							{
								//object comes to active zone in this tick.
								BeginTeleportation( obj );
								break;
							}
						}
					}
					else
					{
						processedObjectsInActiveArea.Clear();
					}
				}
			}
			else
				skipTicks--;
		}

		protected virtual bool IsAllowToTeleport( MapObject obj )
		{
			//allow to teleport only for objects with physics model and only with dynamic bodies.
			if( obj.PhysicsModel != null )
			{
				bool allDynamic = true;
				foreach( Body body in obj.PhysicsModel.Bodies )
				{
					if( body.Static )
					{
						allDynamic = false;
						break;
					}
				}
				if( allDynamic )
					return true;
			}

			return false;
		}

		bool CheckPositionInActiveArea( Vec3 pos )
		{
			if( ( pos.ToVec2() - Position.ToVec2() ).Length() <= Type.ActiveAreaRadius &&
				pos.Z > Position.Z && pos.Z < Position.Z + Type.ActiveAreaHeight )
			{
				return true;
			}
			return false;
		}

		Set<MapObject> GetObjectsInActiveArea()
		{
			Set<MapObject> result = new Set<MapObject>();

			float height = Type.ActiveAreaHeight;
			float radius = Type.ActiveAreaRadius;

			Bounds bounds = new Bounds(
				Position - new Vec3( radius, radius, 0 ),
				Position + new Vec3( radius, radius, height ) );
			Body[] bodies = PhysicsWorld.Instance.VolumeCast( bounds, (int)ContactGroup.CastOnlyDynamic );

			foreach( Body body in bodies )
			{
				if( !body.Static )
				{
					MapObject obj = MapSystemWorld.GetMapObjectByBody( body );
					if( obj != null && obj != this && IsAllowToTeleport( obj ) &&
						CheckPositionInActiveArea( obj.Position ) )
					{
						result.AddWithCheckAlreadyContained( obj );
					}
				}
			}

			return result;
		}

		public bool BeginTeleportation( MapObject objectWhichActivatesTransition )
		{
			if( teleportTransitProgress != 0 )
				return false;
			if( !Active )
				return false;

			teleportTransitProgress = .0001f;
			this.objectWhichActivatesTransition = objectWhichActivatesTransition;

			if( !string.IsNullOrEmpty( Type.SendParticleName ) )
				Map.Instance.CreateAutoDeleteParticleSystem( Type.SendParticleName, Position );

			//play teleportation sound
			Dynamic dynamic = objectWhichActivatesTransition as Dynamic;
			if( dynamic != null )
			{
				string soundTeleporationFullPath = RelativePathUtils.ConvertToFullPath(
					Path.GetDirectoryName( Type.FilePath ), Type.SoundTeleportation );
				dynamic.SoundPlay3D( soundTeleporationFullPath, .5f, false );
			}
			else
				SoundPlay3D( Type.SoundTeleportation, .5f, false );

			return true;
		}

		void TransferToAnotherMap( MapObject obj )
		{
			if( PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj )
			{
				//change the map and transfer player information to the new map.

				if( EntitySystemWorld.Instance.IsServer() )
				{
					Log.Warning( "Teleporter: ChangeMapForPlayer: Networking mode is not supported." );
					return;
				}

				PlayerCharacter character = (PlayerCharacter)PlayerIntellect.Instance.ControlledObject;
				PlayerCharacter.ChangeMapInformation playerCharacterInformation =
					character.GetChangeMapInformation( this );
				GameWorld.Instance.NeedChangeMap( changeMapName, changeMapSpawnObjectName,
					playerCharacterInformation );
			}
			else
			{
				//delete object if it is not contolled by the player.
				obj.SetForDeletion( false );
			}
		}

		void DoTransfer()
		{
			Set<MapObject> objects = GetObjectsInActiveArea();
			if( objectWhichActivatesTransition != null && !objectWhichActivatesTransition.IsSetForDeletion )
				objects.AddWithCheckAlreadyContained( objectWhichActivatesTransition );

			foreach( MapObject obj in objects )
			{
				if( !string.IsNullOrEmpty( changeMapName ) )
					TransferToAnotherMap( obj );
				else if( destination != null )
					destination.ReceiveObject( obj, this );
			}

			teleportTransitProgress = 0;
			objectWhichActivatesTransition = null;
		}

		[Browsable( false )]
		public float TeleportTransitProgress
		{
			get { return teleportTransitProgress; }
		}

		void UpdateAttachedObjects()
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				if( attachedObject.Alias == "active" )
					attachedObject.Visible = Active;
			}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( ( EngineDebugSettings.DrawGameSpecificDebugGeometry ||
				EngineApp.Instance.ApplicationType == EngineApp.ApplicationTypes.ResourceEditor ) &&
				camera.Purpose == Camera.Purposes.MainCamera )
			{
				if( Active )
				{
					float height = Type.ActiveAreaHeight;
					float radius = Type.ActiveAreaRadius;
					int segments = 32;

					camera.DebugGeometry.Color = new ColorValue( 0, 1, 0, .5f );

					float angleStep = MathFunctions.PI * 2 / (float)segments;
					for( float angle = 0; angle < MathFunctions.PI * 2 - angleStep * .5f; angle += angleStep )
					{
						float sin1 = MathFunctions.Sin( angle );
						float cos1 = MathFunctions.Cos( angle );
						float sin2 = MathFunctions.Sin( angle + angleStep );
						float cos2 = MathFunctions.Cos( angle + angleStep );
						Vec3 bottom1 = Position + new Vec3( cos1 * radius, sin1 * radius, 0 );
						Vec3 top1 = Position + new Vec3( cos1 * radius, sin1 * radius, height );
						Vec3 bottom2 = Position + new Vec3( cos2 * radius, sin2 * radius, 0 );
						Vec3 top2 = Position + new Vec3( cos2 * radius, sin2 * radius, height );

						camera.DebugGeometry.AddLine( bottom1, top1 );
						camera.DebugGeometry.AddLine( top1, top2 );
						camera.DebugGeometry.AddLine( bottom1, bottom2 );
					}
				}
			}
		}

		public void ReceiveObject( MapObject obj, Teleporter source )
		{
			if( !string.IsNullOrEmpty( Type.ReceiveParticleName ) )
				Map.Instance.CreateAutoDeleteParticleSystem( Type.ReceiveParticleName, Position );

			if( source == null )
			{
				float offset = obj.Position.Z - obj.PhysicsModel.GetGlobalBounds().Minimum.Z;
				obj.Position = Position + new Vec3( 0, 0, offset );
				obj.Rotation = Rotation;
				obj.SetOldTransform( obj.Position, obj.Rotation, obj.Scale );
			}
			else
			{
				Quat destRotation = Rotation * Mat3.FromRotateByZ( new Degree( 180 ).InRadians() ).ToQuat();

				foreach( Body body in obj.PhysicsModel.Bodies )
				{
					body.Rotation = body.Rotation * source.Rotation.GetInverse() * destRotation;
					Vec3 localPosOffset = ( body.Position - source.Position ) * source.Rotation.GetInverse();
					body.Position = Position + localPosOffset * destRotation;
					body.OldPosition = body.Position;
					body.OldRotation = body.Rotation;

					body.LinearVelocity = body.LinearVelocity * source.Rotation.GetInverse() * destRotation;
					body.AngularVelocity = body.AngularVelocity * source.Rotation.GetInverse() * destRotation;
				}

				obj.UpdatePositionAndRotationByPhysics( true );
				obj.SetOldTransform( obj.Position, obj.Rotation, obj.Scale );

				Unit unit = obj as Unit;
				if( unit != null )
				{
					PlayerIntellect playerIntellect = unit.Intellect as PlayerIntellect;
					if( playerIntellect != null )
					{
						Vec3 vec = playerIntellect.LookDirection.GetVector();
						Vec3 v = vec * source.Rotation.GetInverse() * destRotation;
						playerIntellect.LookDirection = SphereDir.FromVector( v );
					}
				}
			}

			//add object to the list of processed objects. object can't activate teleportation.
			processedObjectsInActiveArea.AddWithCheckAlreadyContained( obj );

			//skip ticks to wait for update physics body of transfered object after teleportation.
			skipTicks += 2;
		}

	}
}
