// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Bullet"/> entity type.
	/// </summary>
	public class BulletType : DynamicType
	{
		[FieldSerialize]
		float velocity;

		[FieldSerialize]
		float maxDistance;

		[FieldSerialize]
		float damage;

		[FieldSerialize]
		float impulse;

		[FieldSerialize]
		float gravity;

		MapObjectCreateObjectCollection hitObjects = new MapObjectCreateObjectCollection();

		//

		public BulletType()
		{
			AllowEmptyName = true;
		}

		[DefaultValue( 0.0f )]
		public float Velocity
		{
			get { return velocity; }
			set { velocity = value; }
		}

		[DefaultValue( 0.0f )]
		public float MaxDistance
		{
			get { return maxDistance; }
			set { maxDistance = value; }
		}

		[DefaultValue( 0.0f )]
		public float Damage
		{
			get { return damage; }
			set { damage = value; }
		}

		[DefaultValue( 0.0f )]
		public float Impulse
		{
			get { return impulse; }
			set { impulse = value; }
		}

		[DefaultValue( 0.0f )]
		public float Gravity
		{
			get { return gravity; }
			set { gravity = value; }
		}

		/// <summary>
		/// The collection of actions to process when bullet hit another body.
		/// </summary>
		/// <remarks>
		/// The developer can configure actions such as creation of a new objects, play sound, etc.
		/// </remarks>
		[Description( "The collection of actions to process when bullet hit another body. The developer can configure actions such as creation of a new objects, play sound, etc." )]
		public MapObjectCreateObjectCollection HitObjects
		{
			get { return hitObjects; }
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			//hitObjects
			TextBlock hitObjectsBlock = block.FindChild( "hitObjects" );
			if( hitObjectsBlock != null )
			{
				if( !hitObjects.Load( hitObjectsBlock ) )
					return false;
			}

			return true;
		}

		protected override bool OnSave( TextBlock block )
		{
			if( !base.OnSave( block ) )
				return false;

			//hitObjects
			if( hitObjects.Count != 0 )
			{
				TextBlock hitObjectsBlock = block.AddChild( "hitObjects" );
				if( !hitObjects.Save( hitObjectsBlock ) )
					return false;
			}

			return true;
		}

		protected override bool OnIsExistsReferenceToObject( object obj )
		{
			if( hitObjects.IsExistsReferenceToObject( obj ) )
				return true;
			return base.OnIsExistsReferenceToObject( obj );
		}

		protected override void OnChangeReferencesToObject( object obj, object newValue )
		{
			base.OnChangeReferencesToObject( obj, newValue );
			hitObjects.ChangeReferencesToObject( obj, newValue );
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//HitObjects
			foreach( MapObjectCreateObject createObject in HitObjects )
				createObject.PreloadResources( this );
		}

		/// <summary>
		/// Calculates the demanded vertical angle to hit the target. Works only for bullet types with enabled Gravity.
		/// </summary>
		/// <param name="horizontalDistance"></param>
		/// <param name="verticalDistance"></param>
		/// <returns></returns>
		public float CalculateDemandedVerticalAngleToHitTarget( float horizontalDistance, float verticalDistance )
		{
			float sh = horizontalDistance;
			float sv = verticalDistance;
			float v0 = velocity;

			float approximationAngle = MathFunctions.DegToRad( 70 );
			float approximationStep = MathFunctions.DegToRad( 29.9f );

			float nearestsh = 0;

			const int iterationCount = 10;
			for( int iteration = 0; iteration < iterationCount; iteration++ )
			{
				double[] sidesh = new double[ 2 ];

				for( int iterside = 0; iterside < 2; iterside++ )
				{
					float angle = approximationAngle;
					if( iterside == 0 )
						angle += approximationStep;
					else
						angle -= approximationStep;

					if( angle > MathFunctions.PI / 2 )
						angle = MathFunctions.PI / 2 - .001f;

					//ignore invalid angle (there is a way more correctly)
					if( angle < MathFunctions.DegToRad( 45 ) )
					{
						sidesh[ iterside ] = 100000.0f;
						continue;
					}

					double vh = Math.Cos( angle ) * v0;
					double vv0 = Math.Sin( angle ) * v0;

					double t;
					{
						double a = ( -gravity );
						double b = 2.0 * vv0;
						double c = -2.0 * sv;

						double d = b * b - 4.0 * a * c;

						if( d < 0 )
							Log.Warning( "BulletType.GetNeedAngleToPosition: d < 0 ({0})", d );

						double dsqrt = Math.Sqrt( d );

						double x1 = ( -b - dsqrt ) / ( 2.0 * a );
						double x2 = ( -b + dsqrt ) / ( 2.0 * a );

						if( x1 < 0 && x2 < 0 )
							Log.Warning( "BulletType.GetNeedAngleToPosition: x1 < 0 && x2 < 0" );

						t = Math.Max( x1, x2 );
					}

					double calcedsh = vh * t;

					sidesh[ iterside ] = calcedsh;
				}

				if( Math.Abs( sidesh[ 0 ] - sh ) < Math.Abs( sidesh[ 1 ] - sh ) )
				{
					approximationAngle += approximationStep;
					nearestsh = (float)sidesh[ 0 ];
				}
				else
				{
					approximationAngle -= approximationStep;
					nearestsh = (float)sidesh[ 1 ];
				}
				approximationStep *= .5f;
			}

			//if( Math.Abs( horizontalDistance - nearestsh ) > 5 )
			//   return -1;

			return approximationAngle;
		}
	}

	/// <summary>
	/// Defines a class for bullet simulation.
	/// </summary>
	public class Bullet : Dynamic
	{
		[FieldSerialize]
		Vec3 velocity;

		[FieldSerialize]
		Unit sourceUnit;

		[FieldSerialize]
		float damageCoefficient = 1.0f;

		[FieldSerialize]
		Vec3 startPosition;

		[FieldSerialize]
		float flyDistance;

		bool firstTick = true;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			HitCallToClient,
		}

		///////////////////////////////////////////

		BulletType _type = null; public new BulletType Type { get { return _type; } }

		public Unit SourceUnit
		{
			get { return sourceUnit; }
			set
			{
				if( sourceUnit != null )
					UnsubscribeToDeletionEvent( sourceUnit );
				sourceUnit = value;
				if( sourceUnit != null )
					SubscribeToDeletionEvent( sourceUnit );
			}
		}

		public Vec3 Velocity
		{
			get { return velocity; }
			set { velocity = value; }
		}

		public float DamageCoefficient
		{
			get { return damageCoefficient; }
			set { damageCoefficient = value; }
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			if( Type.Velocity != 0 )
				velocity = Rotation.GetForward() * Type.Velocity;
			startPosition = Position;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );
			if( sourceUnit == entity )
				sourceUnit = null;
		}

		protected virtual void OnHit( Shape shape, Vec3 normal, MapObject obj )
		{
			if( obj != null )
			{
				//impulse
				float impulse = Type.Impulse * DamageCoefficient;
				if( impulse != 0 && obj.PhysicsModel != null && shape != null )
				{
					shape.Body.AddForce( ForceType.GlobalAtGlobalPos, 0,
						Rotation.GetForward() * impulse, Position );
				}

				//damage
				Dynamic dynamic = obj as Dynamic;
				if( dynamic != null )
				{
					float damage = Type.Damage * DamageCoefficient;
					if( damage != 0 )
						dynamic.DoDamage( this, Position, shape, damage, true );
				}
			}

			HitObjects_Create();
			Die();
		}

		bool Process( float maxDistanceForBulletWithInfiniteSpeed )
		{
			Vec3 offset;
			float distance;
			if( Type.Velocity != 0 )
			{
				offset = velocity * TickDelta;
				distance = offset.Length();
			}
			else
			{
				offset = Rotation.GetForward() * maxDistanceForBulletWithInfiniteSpeed;
				distance = maxDistanceForBulletWithInfiniteSpeed;
			}
			bool deleteIfNoCollisions = false;
			if( Type.MaxDistance != 0 && flyDistance + distance >= Type.MaxDistance )
			{
				distance = Type.MaxDistance - flyDistance;
				if( distance <= 0 )
					distance = .001f;
				offset = offset.GetNormalize() * distance;
				deleteIfNoCollisions = true;
			}

			Vec3 startPosition = Position;
			//back check (that did not fly by through moving towards objects)
			if( !firstTick )
				startPosition -= offset * .1f;

			Ray ray = new Ray( startPosition, offset );

			//find a hit
			Shape hitShape = null;
			Vec3 hitPosition = Vec3.Zero;
			Vec3 hitNormal = Vec3.Zero;
			MapObject hitMapObject = null;//can be null (as example in case when collided with a HeightmapTerrain shape)
			{
				RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing( ray, (int)ContactGroup.CastOnlyContact );
				foreach( RayCastResult result in piercingResult )
				{
					//get associated MapObject with the parent body of the shape
					MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

					//skip bullet creator
					Dynamic dynamic = obj as Dynamic;
					if( dynamic != null && sourceUnit != null && dynamic.GetParentUnitHavingIntellect() == sourceUnit )
						continue;

					//found!
					hitShape = result.Shape;
					hitPosition = result.Position;
					hitNormal = result.Normal;
					hitMapObject = obj;
					break;
				}
			}

			if( hitShape != null )
			{
				//process the hit

				Ray rayToHit = new Ray( ray.Origin, hitPosition - ray.Origin );

				//networking: call OnHit on clients
				if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Body hitShapeBody = null;
					if( hitShape != null )
						hitShapeBody = hitShape.Body;

					Server_SendHitCallToAllClients( hitPosition,
						hitShapeBody != null ? hitShapeBody.Name : "",
						hitShape != null ? hitShape.Name : "",
						hitNormal, hitMapObject );
				}

				Position = hitPosition;
				OnHit( hitShape, hitNormal, hitMapObject );

				CreateWaterPlaneSplash( rayToHit );

				return true;
			}
			else
			{
				//no hit. continue the flying or delete

				Position += offset;
				//update rotation
				if( velocity != Vec3.Zero )
					Rotation = Quat.FromDirectionZAxisUp( velocity.GetNormalize() );
				flyDistance += distance;

				CreateWaterPlaneSplash( ray );

				if( deleteIfNoCollisions )
					SetForDeletion( false );

				return false;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( Type.Velocity != 0 )
			{
				//apply the gravitation effect
				if( Type.Gravity != 0 )
					velocity.Z -= Type.Gravity * TickDelta;

				Process( 0 );
			}
			else
			{
				const float rayCastingStep = 100.0f;

				Bounds checkBounds = Map.Instance.GetSceneGraphObjectsBounds();
				checkBounds.Expand( checkBounds.GetSize() * .05f );

				while( checkBounds.IsContainsPoint( Position ) )
				{
					if( Process( rayCastingStep ) )
						break;
					if( IsSetForDeletion )
						break;
				}

				//outside the world bounds
				if( !IsSetForDeletion )
					SetForDeletion( false );
			}

			firstTick = false;
		}

		public override MapObjectCreateObjectCollection.CreateObjectsResultItem[] DieObjects_Create()
		{
			MapObjectCreateObjectCollection.CreateObjectsResultItem[] result = base.DieObjects_Create();

			//copy DamageCoefficient, SourceUnit properties to created Explosions.
			foreach( MapObjectCreateObjectCollection.CreateObjectsResultItem item in result )
			{
				MapObjectCreateMapObject createMapObject = item.Source as MapObjectCreateMapObject;
				if( createMapObject != null )
				{
					foreach( MapObject mapObject in item.CreatedObjects )
					{
						Explosion explosion = mapObject as Explosion;
						if( explosion != null )
						{
							explosion.DamageCoefficient = DamageCoefficient;
							explosion.SourceUnit = SourceUnit;
						}
					}
				}
			}

			return result;
		}

		protected override void OnLifeTimeIsOver()
		{
			Die();
		}

		void CreateWaterPlaneSplash( Ray ray )
		{
			if( ray.Direction.Z >= 0 )
				return;

			foreach( WaterPlane waterPlane in WaterPlane.Instances )
			{
				//check by plane
				Plane plane = new Plane( Vec3.ZAxis, waterPlane.Position.Z );
				float scale;
				if( !plane.LineIntersection( ray.Origin, ray.Origin + ray.Direction, out scale ) )
					continue;
				Vec3 pos = ray.GetPointOnRay( scale );

				//check by bounds
				Rect bounds2 = new Rect( waterPlane.Position.ToVec2() );
				bounds2.Expand( waterPlane.Size * .5f );
				if( !bounds2.IsContainsPoint( pos.ToVec2() ) )
					continue;

				//create splash
				waterPlane.CreateSplash( WaterPlaneType.SplashTypes.Bullet, pos );
			}
		}

		public virtual MapObjectCreateObjectCollection.CreateObjectsResultItem[] HitObjects_Create()
		{
			//create objects
			MapObjectCreateObjectCollection.CreateObjectsResultItem[] result =
				Type.HitObjects.CreateObjectsOfOneRandomSelectedGroup( this );

			return result;
		}

		void Server_SendHitCallToAllClients( Vec3 hitPosition, string hitShapeBodyName, string hitShapeName, Vec3 hitNormal,
			MapObject hitMapObject )
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Bullet ), (ushort)NetworkMessages.HitCallToClient );
			writer.Write( hitPosition );
			writer.Write( hitShapeBodyName );
			writer.Write( hitShapeName );
			writer.Write( hitNormal );
			writer.WriteVariableUInt32( hitMapObject != null ? hitMapObject.NetworkUIN : 0 );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.HitCallToClient )]
		void Client_ReceiveHitCall( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			Vec3 hitPosition = reader.ReadVec3();
			string hitShapeBodyName = reader.ReadString();
			string hitShapeName = reader.ReadString();
			Vec3 hitNormal = reader.ReadVec3();
			MapObject hitMapObject = Entities.Instance.GetByNetworkUIN( reader.ReadVariableUInt32() ) as MapObject;
			if( !reader.Complete() )
				return;

			Position = hitPosition;
			Shape hitShape = null;
			if( PhysicsModel != null )
			{
				Body body = PhysicsModel.GetBody( hitShapeBodyName );
				if( body != null )
					hitShape = body.GetShape( hitShapeName );
			}
			OnHit( hitShape, hitNormal, hitMapObject );
		}
	}
}
