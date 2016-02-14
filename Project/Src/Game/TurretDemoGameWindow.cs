// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.PhysicsSystem;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for TurretDemo.
	/// </summary>
	public class TurretDemoGameWindow : ActionGameWindow
	{
		float remainingTimeForCreateEnemy;
		float gameTime;
		int level;
		int remainingCount;
		int remainingCreateCount;
		float createInterval;

		Vec3 turretCreatePosition;
		Quat turretCreateRotation;

		Font screenFont;

		List<MapObject> enemySpawnPoints = new List<MapObject>();

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			//World serialization. Need to add the support of load/save feature in Game.exe.
			{
				if( World.Instance.GetCustomSerializationValue( "remainingTimeForCreateEnemy" ) != null )
				{
					remainingTimeForCreateEnemy = (float)World.Instance.GetCustomSerializationValue(
						"remainingTimeForCreateEnemy" );
				}

				if( World.Instance.GetCustomSerializationValue( "gameTime" ) != null )
					gameTime = (float)World.Instance.GetCustomSerializationValue( "gameTime" );

				if( World.Instance.GetCustomSerializationValue( "level" ) != null )
					level = (int)World.Instance.GetCustomSerializationValue( "level" );

				if( World.Instance.GetCustomSerializationValue( "remainingCount" ) != null )
					remainingCount = (int)World.Instance.GetCustomSerializationValue( "remainingCount" );

				if( World.Instance.GetCustomSerializationValue( "remainingCreateCount" ) != null )
					remainingCreateCount = (int)World.Instance.GetCustomSerializationValue( "remainingCreateCount" );

				if( World.Instance.GetCustomSerializationValue( "createInterval" ) != null )
					createInterval = (float)World.Instance.GetCustomSerializationValue( "createInterval" );
			}

			GameGuiObject billboard = Entities.Instance.GetByName( "HangingBillboard_Game" ) as GameGuiObject;
			billboard.Damage += GameBillboard_Damage;

			//find enemy spawn points
			foreach( Entity entity in Map.Instance.Children )
			{
				MapObject point = entity as MapObject;

				if( !string.IsNullOrEmpty( entity.GetTag( "TextUserData" ) ) )
				{
					if( point != null && point.Type.Name == "HelperPoint" )
						enemySpawnPoints.Add( point );
				}
			}

			screenFont = FontManager.Instance.LoadFont( "Default", .05f );

			if( level == 0 )//for world serialization
				level = 1;

			//get turret start position
			Turret turret = (Turret)Entities.Instance.GetByName( "Turret_Game" );
			if( turret != null )
			{
				turretCreatePosition = turret.Position;
				turretCreateRotation = turret.Rotation;
			}

			UpdateVictoryObjects( false );

			//World serialization. Need to add the support of load/save feature in Game.exe.
			foreach( Entity entity in Map.Instance.Children )
			{
				if( entity.IsSetForDeletion )
					continue;

				Unit unit = entity as Unit;
				if( unit == null )
					continue;

				if( unit is PlayerCharacter )
					continue;

				if( ( unit.Intellect as AI ) != null )
				{
					unit.ViewRadius = 300;
					unit.Destroying += EnemyUnitDestroying;
					unit.Tick += EnemyUnitTick;
				}
			}

			//for world serialization
			if( gameTime != 0 )
				MainPlayerUnitSubscribeToDestroying();

			//for world serialization
			if( gameTime >= 8 )
				GameMusic.MusicPlay( "Sounds/Music/Action.ogg", true );

			Map.Instance.Tick += Map_Tick;
		}

		protected override void OnDetach()
		{
			if( Map.Instance != null )
				Map.Instance.Tick += Map_Tick;

			GameStop( false );

			base.OnDetach();
		}

		public override void OnBeforeWorldSave()
		{
			base.OnBeforeWorldSave();

			//World serialized data
			World.Instance.ClearAllCustomSerializationValues();
			World.Instance.SetCustomSerializationValue( "remainingTimeForCreateEnemy", remainingTimeForCreateEnemy );
			World.Instance.SetCustomSerializationValue( "gameTime", gameTime );
			World.Instance.SetCustomSerializationValue( "level", level );
			World.Instance.SetCustomSerializationValue( "remainingCount", remainingCount );
			World.Instance.SetCustomSerializationValue( "remainingCreateCount", remainingCreateCount );
			World.Instance.SetCustomSerializationValue( "createInterval", createInterval );
		}

		protected override void OnRender()
		{
			base.OnRender();

			if( EntitySystemWorld.Instance.Simulation )
			{
				string text = "";
				float alpha = 0;

				if( gameTime >= 2 && gameTime < 4 )
					text = ">     3     <";
				else if( gameTime >= 4 && gameTime < 6 )
					text = ">   2   <";
				else if( gameTime >= 6 && gameTime < 8 )
				{
					text = "> 1 <";
					alpha = ( gameTime - 6 ) / 2;
				}
				else if( gameTime >= 8 && gameTime < 10 )
					text = "FIGHT";

				if( alpha != 0 )
					EngineApp.Instance.ScreenGuiRenderer.AddQuad( new Rect( 0, 0, 1, 1 ), new ColorValue( 0, 0, 0, alpha ) );

				if( text != "" )
				{
					EngineApp.Instance.ScreenGuiRenderer.AddText( screenFont, text, new Vec2( .5f, .4f ),
						HorizontalAlign.Center, VerticalAlign.Center, new ColorValue( 1, 0, 0 ) );
				}
			}
		}

		void Map_Tick( Entity entity )
		{
			//Game tick
			if( gameTime != 0 )
			{
				gameTime += Entity.TickDelta;

				//Pre start
				if( gameTime >= 2 && gameTime < 2 + Entity.TickDelta * 1.5 )
					GameEngineApp.Instance.ControlManager.PlaySound( "Sounds/Feedback/Three.ogg" );
				if( gameTime >= 4 && gameTime < 4 + Entity.TickDelta * 1.5 )
					GameEngineApp.Instance.ControlManager.PlaySound( "Sounds/Feedback/Two.ogg" );
				if( gameTime >= 6 && gameTime < 6 + Entity.TickDelta * 1.5 )
					GameEngineApp.Instance.ControlManager.PlaySound( "Sounds/Feedback/One.ogg" );
				if( gameTime >= 8 && gameTime < 8 + Entity.TickDelta * 1.5 )
					GameEngineApp.Instance.ControlManager.PlaySound( "Sounds/Feedback/Fight.ogg" );

				if( gameTime >= 8 )
					GameMusic.MusicPlay( "Sounds/Music/Action.ogg", true );

				//Create enemies
				if( gameTime > 10 && remainingCreateCount != 0 )
				{
					remainingTimeForCreateEnemy -= Entity.TickDelta;

					if( remainingTimeForCreateEnemy <= 0 )
					{
						remainingTimeForCreateEnemy = createInterval;
						if( CreateEnemy() )
							remainingCreateCount--;
					}
				}
			}

			//Update billboard text
			{
				string mainText = "";
				string downText = "";

				if( gameTime < 8 )
				{
					if( gameTime == 0 )
					{
						if( level <= 4 )
						{
							mainText = string.Format( "Level {0}", level );
							downText = "Fire here to start";
						}
						else
						{
							mainText = "Victory";
							downText = "Congratulations :)";
						}
					}
					else
					{
						mainText = "Prepare";
						downText = "for battle";
					}
				}
				else
				{
					mainText = remainingCount.ToString();
					downText = string.Format( "Level {0}", level );
				}

				GameGuiObject billboard = (GameGuiObject)Entities.Instance.GetByName( "HangingBillboard_Game" );
				billboard.MainControl.Controls[ "MainText" ].Text = mainText;
				billboard.MainControl.Controls[ "DownText" ].Text = downText;
			}

			//Recreate Turret
			if( Entities.Instance.GetByName( "Turret_Game" ) == null && gameTime == 0 )
			{
				Turret turret = (Turret)Entities.Instance.Create( "Turret", Map.Instance );
				turret.Name = "Turret_Game";
				turret.Position = turretCreatePosition;
				turret.Rotation = turretCreateRotation;
				turret.PostCreate();
			}
		}

		bool CreateEnemy()
		{
			//Get need enemy type
			string typeName = GetCreateEnemyTypeName();

			//Get point
			MapObject point = null;
			int safeCounter = 1;
			while( point == null )
			{
				point = enemySpawnPoints[ World.Instance.Random.Next( enemySpawnPoints.Count ) ];
				safeCounter++;
				if( safeCounter > 1000 )
					break;
			}

			if( point == null )
				return false;

			//check for free position
			bool freePoint;
			{
				Bounds volume = new Bounds( point.Position );
				volume.Expand( new Vec3( 2, 2, 2 ) );
				Body[] bodies = PhysicsWorld.Instance.VolumeCast( volume,
					(int)ContactGroup.CastOnlyDynamic );

				freePoint = bodies.Length == 0;
			}
			if( !freePoint )
				return false;

			//Create object
			MapObject newObj = (MapObject)Entities.Instance.Create( typeName, Map.Instance );
			newObj.Position = point.Position + new Vec3( 0, 0, 1 );
			if( typeName == "Robot" )
				newObj.Position += new Vec3( 0, 0, 1 );
			newObj.PostCreate();

			if( newObj is Unit )
				( (Unit)newObj ).ViewRadius = 300;
			newObj.Destroying += EnemyUnitDestroying;
			newObj.Tick += EnemyUnitTick;

			return true;
		}

		void GameStart()
		{
			if( level > 4 )
				level = 1;

			remainingCount = 20 + level * 3;
			createInterval = 20.0f / ( level + 15 );

			gameTime = .001f;
			remainingCreateCount = remainingCount;
			GameEngineApp.Instance.ControlManager.PlaySound( "Sounds/Feedback/Accept.ogg" );

			MainPlayerUnitSubscribeToDestroying();

			Turret turret = Entities.Instance.GetByName( "Turret_Game" ) as Turret;
			if( turret != null )
				turret.Health = turret.Type.HealthMax;

			UpdateVictoryObjects( false );
		}

		void GameStop( bool complete )
		{
			if( gameTime == 0 )
				return;

			gameTime = 0;

			if( complete )
			{
				GameEngineApp.Instance.ControlManager.PlaySound( "Sounds/Feedback/Complete.ogg" );
				level++;
			}

			//Destroy all alive enemies
			ttt: ;
			foreach( Entity entity in Map.Instance.Children )
			{
				if( entity.IsSetForDeletion )
					continue;

				Unit unit = entity as Unit;
				if( unit == null )
					continue;

				if( ( unit.Intellect as AI ) != null )
				{
					unit.Die();
					goto ttt;
				}
			}

			//restore default music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			if( complete && level > 4 )
				UpdateVictoryObjects( true );
		}

		void UpdateVictoryObjects( bool visible )
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				if( entity.GetTag( "TextUserData" ) != "Victory" )
					continue;

				MapObject obj = entity as MapObject;
				if( obj != null )
					obj.Visible = visible;
			}
		}

		string GetCreateEnemyTypeName()
		{
			float bug = 0;
			float zombie = 0;
			float robot = 0;

			switch( level )
			{
			case 1:
				bug = .5f;
				zombie = .5f;
				break;

			case 2:
				zombie = .4f;
				bug = .5f;
				robot = .1f;
				break;

			case 3:
				zombie = .1f;
				bug = .7f;
				robot = .2f;
				break;

			case 4:
				bug = .7f;
				robot = .3f;
				break;
			}

			float value = World.Instance.Random.NextFloat();
			if( value >= 0 && value < bug )
				return "Bug";
			if( value >= bug && value < bug + zombie )
				return "Zombie";
			if( value >= bug + zombie && value < bug + zombie + robot )
				return "Robot";
			return "Robot";
		}

		void EnemyUnitDestroying( Entity entity )
		{
			if( gameTime != 0 )
			{
				remainingCount--;
				if( remainingCount == 0 )
					GameStop( true );
			}
		}

		void EnemyUnitTick( Entity entity )
		{
			Dynamic obj = (Dynamic)entity;

			Unit playerUnit = null;
			if( PlayerIntellect.Instance != null )
				playerUnit = PlayerIntellect.Instance.ControlledObject;

			if( playerUnit != null )
			{
				float playerDistance = ( obj.Position - playerUnit.Position ).Length();

				Vec3 diff = playerUnit.Position - obj.Position;
				float angle = MathFunctions.ATan( diff.Y, diff.X );

				//Suicide
				bool allowSuicide = false;
				float suicideDistance = 3;

				if( playerUnit is Turret )
					allowSuicide = true;

				if( allowSuicide && playerDistance < suicideDistance )
				{
					obj.Die();

					Dynamic dieObject = (Dynamic)Entities.Instance.Create( "TurretDemo_MonsterSuicideObject", Map.Instance );
					dieObject.Position = obj.Position;
					dieObject.PostCreate();

					dieObject.Die();

					return;
				}
			}

			//Check outside map
			Bounds bounds = Map.Instance.InitialCollisionBounds;
			bounds.Expand( new Vec3( 50, 50, 300 ) );
			if( !bounds.IsContainsPoint( ( (MapObject)entity ).Position ) )
				entity.SetForDeletion( false );
		}

		void MainPlayerUnitSubscribeToDestroying()
		{
			PlayerCharacter mainPlayerUnit = null;
			foreach( Entity entity in Map.Instance.Children )
			{
				mainPlayerUnit = entity as PlayerCharacter;
				if( mainPlayerUnit != null )
					break;
			}
			if( mainPlayerUnit != null )
			{
				mainPlayerUnit.Destroying += delegate( Entity entity )
				{
					GameStop( false );
				};
			}
		}

		void GameBillboard_Damage( Dynamic entity, MapObject prejudicial, Vec3 pos, float damage )
		{
			if( gameTime == 0 )
				GameStart();
		}
	}
}
