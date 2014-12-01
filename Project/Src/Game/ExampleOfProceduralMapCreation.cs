// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.MathEx;
using Engine.UISystem;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	static class ExampleOfProceduralMapCreation
	{
		public static bool ServerOrSingle_MapCreate()
		{
			GameNetworkServer server = GameNetworkServer.Instance;

			Control mapLoadingWindow = null;

			//show map loading window
			mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl( "Gui\\MapLoadingWindow.gui" );
			if( mapLoadingWindow != null )
			{
				mapLoadingWindow.Text = "Procedural map creation";
				GameEngineApp.Instance.ControlManager.Controls.Add( mapLoadingWindow );
			}

			//delete all GameWindow's
			GameEngineApp.Instance.DeleteAllGameWindows();

			MapSystemWorld.MapDestroy();

			EngineApp.Instance.RenderScene();

			//create world if need
			WorldType worldType = EntitySystemWorld.Instance.DefaultWorldType;

			if( World.Instance == null || World.Instance.Type != worldType )
			{
				WorldSimulationTypes worldSimulationType;
				EntitySystemWorld.NetworkingInterface networkingInterface = null;

				if( server != null )
				{
					worldSimulationType = WorldSimulationTypes.ServerAndClient;
					networkingInterface = server.EntitySystemService.NetworkingInterface;
				}
				else
					worldSimulationType = WorldSimulationTypes.Single;

				if( !EntitySystemWorld.Instance.WorldCreate( worldSimulationType, worldType, networkingInterface ) )
					Log.Fatal( "ExampleOfProceduralMapCreation: ServerOrSingle_MapCreate: EntitySystemWorld.Instance.WorldCreate failed." );
			}

			//create map
			GameMapType gameMapType = EntityTypes.Instance.GetByName( "GameMap" ) as GameMapType;
			if( gameMapType == null )
				Log.Fatal( "ExampleOfProceduralMapCreation: ServerOrSingle_MapCreate: \"GameMap\" type is not defined." );
			GameMap gameMap = (GameMap)Entities.Instance.Create( gameMapType, World.Instance );
			gameMap.ShadowFarDistance = 60;
			gameMap.ShadowColor = new ColorValue( .5f, .5f, .5f );

			//create MapObjects
			ServerOrSingle_CreateEntities();

			//post create map
			gameMap.PostCreate();

			//inform clients about world created
			if( server != null )
				server.EntitySystemService.WorldWasCreated();

			//Error
			foreach( Control control in GameEngineApp.Instance.ControlManager.Controls )
			{
				if( control is MessageBoxWindow && !control.IsShouldDetach() )
					return false;
			}

			GameEngineApp.Instance.CreateGameWindowForMap();

			//play music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			return true;
		}

		static void CreateEntitiesWhichNotSynchronizedViaNetwork()
		{
			//ground
			{
				//create materials from the code
				ShaderBaseMaterial[] materials = new ShaderBaseMaterial[ 7 ];
				{
					for( int n = 0; n < materials.Length; n++ )
					{
						string materialName = HighLevelMaterialManager.Instance.GetUniqueMaterialName(
							"ExampleOfProceduralMapCreation_Ground" );
						ShaderBaseMaterial material = (ShaderBaseMaterial)HighLevelMaterialManager.Instance.CreateMaterial(
							materialName, "ShaderBaseMaterial" );
						material.Diffuse1Map.Texture = string.Format( "Types\\Vegetation\\Trees\\Textures\\Bark{0}A.dds", n + 1 );
						material.NormalMap.Texture = string.Format( "Types\\Vegetation\\Trees\\Textures\\Bark{0}A_N.dds", n + 1 );
						material.SpecularColor = new ColorValue( 1, 1, 1 );
						material.SpecularMap.Texture = string.Format( "Types\\Vegetation\\Trees\\Textures\\Bark{0}A_S.dds", n + 1 );
						material.PostCreate();

						materials[ n ] = material;
					}
				}

				//create objects with collision body
				EngineRandom random = new EngineRandom( 0 );
				for( float y = -35; y < 35; y += 5 )
				{
					for( float x = -35; x < 35; x += 2.5f )
					{
						StaticMesh staticMesh = (StaticMesh)Entities.Instance.Create( "StaticMesh", Map.Instance );
						staticMesh.MeshName = "Base\\Simple Models\\Box.mesh";

						ShaderBaseMaterial material = materials[ random.Next( 0, 6 ) ];
						staticMesh.ForceMaterial = material.Name;//"DarkGray";

						staticMesh.Position = new Vec3( x, y, -1.0f );
						staticMesh.Scale = new Vec3( 2.5f, 5, 2 );
						staticMesh.CastDynamicShadows = false;
						staticMesh.PostCreate();
					}
				}
			}

			//SkyBox
			{
				Entity skyBox = Entities.Instance.Create( "SkyBox", Map.Instance );
				skyBox.PostCreate();
			}

			//Light
			{
				Light light = (Light)Entities.Instance.Create( "Light", Map.Instance );
				light.LightType = RenderLightType.Directional;
				light.SpecularColor = new ColorValue( 1, 1, 1 );
				light.Position = new Vec3( 0, 0, 10 );
				light.Rotation = new Angles( 120, 50, 330 ).ToQuat();
				light.PostCreate();
			}
		}

		static void ServerOrSingle_CreateEntities()
		{
			CreateEntitiesWhichNotSynchronizedViaNetwork();

			//SpawnPoint (server or single only)
			{
				SpawnPoint obj = (SpawnPoint)Entities.Instance.Create( "SpawnPoint", Map.Instance );
				obj.Position = new Vec3( -29, 0, 1 );
				obj.PostCreate();
			}

			//weapon
			if( EntityTypes.Instance.GetByName( "SubmachineGunItem" ) != null )
			{
				MapObject obj = (MapObject)Entities.Instance.Create( "SubmachineGunItem", Map.Instance );
				obj.Position = new Vec3( -26, 0, .3f );
				obj.PostCreate();
			}

			//Boxes (synchronized via network)
			for( float y = -20; y <= 20; y += 5 )
			{
				for( float x = -20; x <= 20; x += 5 )
				{
					for( float z = .5f; z < 10; z += 1 )
					{
						MapObject mapObject = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
						mapObject.Position = new Vec3( x, y, z );
						mapObject.Rotation = new Angles( 0, 0, World.Instance.Random.NextFloat() * 360 ).ToQuat();
						mapObject.PostCreate();
					}
				}
			}
		}

		public static void Client_CreateEntities()
		{
			CreateEntitiesWhichNotSynchronizedViaNetwork();
		}
	}
}
