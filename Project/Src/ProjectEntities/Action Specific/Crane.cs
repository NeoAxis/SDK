// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Crane"/> entity type.
	/// </summary>
	public class CraneType : UnitType
	{
	}

	/// <summary>
	/// Gives an opportunity of creation of the crane and his control via <see cref="Intellect"/>.
	/// </summary>
	public class Crane : Unit
	{
		CraneType _type = null; public new CraneType Type { get { return _type; } }

		//threads
		List<ThreadItem> threads = new List<ThreadItem>();

		//magnet
		Body magnetBody;
		Dictionary<Body, int> lastMagnetContactsCount = new Dictionary<Body, int>();

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		List<MagnetObjectItem> magnetAttachedObjects = new List<MagnetObjectItem>();

		///////////////////////////////////////////

		class ThreadItem
		{
			public MapObjectAttachedObject startObject;
			public MapObjectAttachedObject endObject;
			public MeshObject meshObject;
			public SceneNode sceneNode;
		}

		///////////////////////////////////////////

		class MagnetObjectItem
		{
			[FieldSerialize]
			public MapObject mapObject;

			public Body body;
			[FieldSerialize]
			public int bodyIndex;//for world serialization

			public FixedJoint fixedJoint;
		}

		///////////////////////////////////////////

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();
			CreateThreads();

			magnetBody = PhysicsModel.GetBody( "magnet" );
			if( magnetBody != null )
				magnetBody.Collision += magnetBody_Collision;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate2(Boolean)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			//world serialization
			if( loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				foreach( MagnetObjectItem item in magnetAttachedObjects )
				{
					//restore item.body
					item.body = item.mapObject.PhysicsModel.Bodies[ item.bodyIndex ];

					//recreate fixed joint
					CreateFixedJointForAttachedObject( item );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( magnetBody != null )
				magnetBody.Collision -= magnetBody_Collision;

			DestroyThreads();
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			again:
			foreach( MagnetObjectItem item in magnetAttachedObjects )
			{
				if( item.mapObject == entity )
				{
					MagnetDetachObject( item );
					goto again;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( Intellect != null )
				TickIntellect();

			TickMagnet();
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );
			UpdateThreads();
		}

		void TickIntellect()
		{
			//horizontalMotor
			{
				float throttle = 0;
				throttle += Intellect.GetControlKeyStrength( GameControlKeys.Left );
				throttle -= Intellect.GetControlKeyStrength( GameControlKeys.Right );

				GearedMotor motor = PhysicsModel.GetMotor( "horizontalMotor" ) as GearedMotor;
				if( motor != null )
					motor.Throttle = throttle;
			}

			//gibbetMotor
			{
				ServoMotor motor = PhysicsModel.GetMotor( "gibbetMotor" ) as ServoMotor;
				if( motor != null )
				{
					Radian needAngle = motor.DesiredAngle;

					needAngle += Intellect.GetControlKeyStrength( GameControlKeys.Forward ) * .004f;
					needAngle -= Intellect.GetControlKeyStrength( GameControlKeys.Backward ) * .004f;

					MathFunctions.Clamp( ref needAngle,
						new Degree( -20.0f ).InRadians(), new Degree( 40.0f ).InRadians() );

					motor.DesiredAngle = needAngle;
				}
			}

			//Change player LookDirection at rotation
			PlayerIntellect intellect = Intellect as PlayerIntellect;
			if( intellect != null )
			{
				Vec3 lookVector = intellect.LookDirection.GetVector();
				lookVector *= OldRotation.GetInverse();
				lookVector *= Rotation;
				intellect.LookDirection = SphereDir.FromVector( lookVector );
			}
		}

		void CreateThreads()
		{
			for( int n = 1; ; n++ )
			{
				MapObjectAttachedObject startObject = GetFirstAttachedObjectByAlias( string.Format( "thread{0}Start", n ) );
				MapObjectAttachedObject endObject = GetFirstAttachedObjectByAlias( string.Format( "thread{0}End", n ) );
				if( startObject == null || endObject == null )
					break;

				MeshObject meshObject = SceneManager.Instance.CreateMeshObject( "Base\\Simple Models\\Box.mesh" );
				if( meshObject == null )
					break;

				meshObject.SetMaterialNameForAllSubObjects( "Black" );
				meshObject.CastShadows = true;

				ThreadItem item = new ThreadItem();
				item.startObject = startObject;
				item.endObject = endObject;
				item.meshObject = meshObject;
				item.sceneNode = new SceneNode();
				item.sceneNode.Attach( item.meshObject );

				MapObject.AssociateSceneNodeWithMapObject( item.sceneNode, this );

				threads.Add( item );
			}

			UpdateThreads();
		}

		void DestroyThreads()
		{
			foreach( ThreadItem item in threads )
			{
				item.sceneNode.Dispose();
				item.meshObject.Dispose();
			}
			threads.Clear();
		}

		void UpdateThreads()
		{
			const float threadThickness = .07f;

			foreach( ThreadItem item in threads )
			{
				Vec3 start;
				Vec3 end;
				{
					Quat r;
					Vec3 s;
					item.startObject.GetGlobalInterpolatedTransform( out start, out r, out s );
					item.endObject.GetGlobalInterpolatedTransform( out end, out r, out s );
				}
				Vec3 dir = end - start;
				float length = dir.Normalize();

				//update scene node transform
				item.sceneNode.Position = ( start + end ) * .5f;
				item.sceneNode.Rotation = Quat.FromDirectionZAxisUp( dir );
				item.sceneNode.Scale = new Vec3( length, threadThickness, threadThickness );
				item.sceneNode.Visible = Visible;
			}
		}

		void magnetBody_Collision( ref CollisionEvent collisionEvent )
		{
			Body mapObjectBody = collisionEvent.OtherShape.Body;

			MapObject mapObject = MapSystemWorld.GetMapObjectByBody( mapObjectBody );
			if( mapObject == null )
				return;

			if( mapObject == this )
				return;

			if( IsMagnetBodyAttached( mapObjectBody ) )
				return;

			int count;
			if( lastMagnetContactsCount.TryGetValue( mapObjectBody, out count ) )
				lastMagnetContactsCount.Remove( mapObjectBody );
			else
				count = 0;
			lastMagnetContactsCount.Add( mapObjectBody, count + 1 );
		}

		bool IsMagnetBodyAttached( Body body )
		{
			for( int n = 0; n < magnetAttachedObjects.Count; n++ )
			{
				if( magnetAttachedObjects[ n ].body == body )
					return true;
			}
			return false;
		}

		void MagnetAttachObject( Body mapObjectBody )
		{
			if( mapObjectBody.IsDisposed )
				return;

			if( IsMagnetBodyAttached( mapObjectBody ) )
				Log.Fatal( "Crane: MagnetAttachObject: IsMagnetBodyAttached( mapObjectBody )." );

			MapObject mapObject = MapSystemWorld.GetMapObjectByBody( mapObjectBody );
			if( mapObject != null && mapObject.PhysicsModel != null )
			{
				MagnetObjectItem item = new MagnetObjectItem();
				item.mapObject = mapObject;
				item.body = mapObjectBody;
				item.bodyIndex = Array.IndexOf<Body>( mapObject.PhysicsModel.Bodies, mapObjectBody );

				SubscribeToDeletionEvent( mapObject );

				CreateFixedJointForAttachedObject( item );

				magnetAttachedObjects.Add( item );
			}
		}

		void CreateFixedJointForAttachedObject( MagnetObjectItem item )
		{
			FixedJoint fixedJoint = PhysicsWorld.Instance.CreateFixedJoint( magnetBody, item.body );
			fixedJoint.PushedToWorld = true;
			item.fixedJoint = fixedJoint;
		}

		void MagnetDetachObject( MagnetObjectItem item )
		{
			item.fixedJoint.Dispose();
			UnsubscribeToDeletionEvent( item.mapObject );

			magnetAttachedObjects.Remove( item );
		}

		void MagnetDetachAllObjects()
		{
			again:
			foreach( MagnetObjectItem item in magnetAttachedObjects )
			{
				MagnetDetachObject( item );
				goto again;
			}
		}

		void TickMagnet()
		{
			bool userNeedDetach = Intellect != null &&
				( Intellect.IsControlKeyPressed( GameControlKeys.Fire1 ) ||
				Intellect.IsControlKeyPressed( GameControlKeys.Fire2 ) );

			//attach new bodies
			if( !userNeedDetach )
			{
				const int needContacts = 3;

				foreach( KeyValuePair<Body, int> pair in lastMagnetContactsCount )
				{
					Body mapObjectBody = pair.Key;
					int contactsCount = pair.Value;

					if( contactsCount >= needContacts )
						MagnetAttachObject( mapObjectBody );
				}
			}
			lastMagnetContactsCount.Clear();

			//detach by user
			if( userNeedDetach )
				MagnetDetachAllObjects();

			//detach if joint disposed
			again:
			foreach( MagnetObjectItem item in magnetAttachedObjects )
			{
				if( item.fixedJoint.IsDisposed )
				{
					MagnetDetachObject( item );
					goto again;
				}
			}

		}

		public override void GetFirstPersonCameraPosition( out Vec3 position, out Vec3 forward, out Vec3 up )
		{
			position = GetInterpolatedPosition() + Type.FPSCameraOffset * GetInterpolatedRotation();
			if( PlayerIntellect.Instance != null )
				forward = PlayerIntellect.Instance.LookDirection.GetVector();
			else
				forward = Vec3.XAxis;
			up = Vec3.ZAxis;
		}
	}
}
