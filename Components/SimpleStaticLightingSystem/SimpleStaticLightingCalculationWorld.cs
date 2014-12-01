// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using StaticLightingCalculationSystem;

namespace SimpleLightmapSystem
{
	sealed class SimpleStaticLightingCalculationWorld : StaticLightingCalculationWorld
	{
		//scene
		const int contactGroup = 1;
		PhysicsScene physicsScene;
		//!!!!slowly. need grid for optimization
		MyLight[] lights;
		ColorValue shadowColor;
		bool calculateShadows;

		//lightmap rendering
		const int bucketSize = 32;
		MeshObject lightmapMeshObject;
		LightmapImage lightmapRenderingImage;
		Vec2I lightmapBucketIndex;
		int[][] lightmapTriangleMap;

		//

		protected override bool OnInitLibrary()
		{
			return true;
		}

		protected override void OnShutdownLibrary()
		{
		}

		public override string DriverName
		{
			get { return "Simple static lighting calculation system"; }
		}

		protected override void OnCreateScene( Mesh[] meshes, MeshObject[] meshObjects,
			Light[] lights, ColorValue shadowColor, bool calculateShadows )
		{
			//create separated physics scene
			physicsScene = PhysicsWorld.Instance.CreateScene( "Static Lighting" );

			//initialize contact group
			physicsScene.SpecialContactGroupsEnabled = true;
			physicsScene.SetupSpecialContactGroups( 1, 1, true );

			Dictionary<Mesh, string> meshPhysicsMeshNames = new Dictionary<Mesh, string>();

			//register physics custom mesh names
			foreach( Mesh mesh in meshes )
			{
				string customMeshName = PhysicsWorld.Instance.AddCustomMeshGeometry(
					mesh.Positions, mesh.Indices, null, MeshShape.MeshTypes.TriangleMesh, 0, 0 );

				meshPhysicsMeshNames.Add( mesh, customMeshName );
			}

			//create bodies
			foreach( MeshObject meshObject in meshObjects )
			{
				Body body = physicsScene.CreateBody();
				body.Static = true;
				body.Position = meshObject.Position;
				body.Rotation = meshObject.Rotation;
				body.UserData = meshObject;

				MeshShape shape = body.CreateMeshShape();
				shape.ContactGroup = contactGroup;
				shape.MeshName = meshPhysicsMeshNames[ meshObject.Mesh ];
				shape.MeshScale = meshObject.Scale;

				body.PushedToWorld = true;
			}

			//lights
			{
				this.lights = new MyLight[ lights.Length ];
				for( int n = 0; n < lights.Length; n++ )
				{
					Light light = lights[ n ];

					MyLight myLight = null;

					PointLight pointLight = light as PointLight;
					if( pointLight != null )
						myLight = new MyPointLight( pointLight );

					SpotLight spotLight = light as SpotLight;
					if( spotLight != null )
						myLight = new MySpotLight( spotLight );

					DirectionalLight directionalLight = light as DirectionalLight;
					if( directionalLight != null )
						myLight = new MyDirectionalLight( directionalLight );

					if( myLight == null )
						Log.Fatal( "SimpleStaticLightingCalculationWorld.OnCreateScene: not implemented light type." );

					this.lights[ n ] = myLight;
					this.lights[ n ].Initialize();
				}
			}

			this.shadowColor = shadowColor;
			this.calculateShadows = calculateShadows;
		}

		protected override void OnDestroyScene()
		{
			if( physicsScene != null )
			{
				List<Body> bodies = new List<Body>( physicsScene.Bodies );
				foreach( Body body in bodies )
					body.Dispose();

				PhysicsWorld.Instance.DestroyNotUsedMeshGeometries();

				physicsScene.Dispose();
				physicsScene = null;
			}

			lights = null;
		}

		protected override void OnBeginRenderLightmap(
			StaticLightingCalculationWorld.MeshObject meshObject, LightmapImage renderingImage )
		{
			lightmapMeshObject = meshObject;
			lightmapRenderingImage = renderingImage;
			lightmapBucketIndex = Vec2I.Zero;

			Vec2I textureSize = lightmapRenderingImage.Size;

			//generate lightmapTriangleMap
			{
				lightmapTriangleMap = new int[ textureSize.Y ][];
				for( int y = 0; y < textureSize.Y; y++ )
				{
					lightmapTriangleMap[ y ] = new int[ textureSize.X ];
					for( int x = 0; x < textureSize.X; x++ )
						lightmapTriangleMap[ y ][ x ] = -1;
				}

				Mesh mesh = meshObject.Mesh;

				int triangleCount = mesh.Indices.Length / 3;
				for( int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++ )
				{
					int index0 = mesh.Indices[ triangleIndex * 3 + 0 ];
					int index1 = mesh.Indices[ triangleIndex * 3 + 1 ];
					int index2 = mesh.Indices[ triangleIndex * 3 + 2 ];

					Vec3 position0 = mesh.Positions[ index0 ];
					Vec3 position1 = mesh.Positions[ index1 ];
					Vec3 position2 = mesh.Positions[ index2 ];
					if( MathUtils.IsDegenerateTriangle( position0, position1, position2 ) )
						continue;

					Vec2 texCoord0 = mesh.LightmapTexCoords[ index0 ];
					Vec2 texCoord1 = mesh.LightmapTexCoords[ index1 ];
					Vec2 texCoord2 = mesh.LightmapTexCoords[ index2 ];

					Vec2I pixelIndex0 = GetPixelIndexByTexCoord( texCoord0 );
					Vec2I pixelIndex1 = GetPixelIndexByTexCoord( texCoord1 );
					Vec2I pixelIndex2 = GetPixelIndexByTexCoord( texCoord2 );

					Geometry2D.FillTriangle( pixelIndex0, pixelIndex1, pixelIndex2,
						new RectI( Vec2I.Zero, textureSize ), delegate( Vec2I point )
						{
							lightmapTriangleMap[ point.Y ][ point.X ] = triangleIndex;
						} );
				}
			}
		}

		protected override void OnUpdateRenderLightmap( out bool finished )
		{
			finished = false;

			//render bucket
			{
				//DoLogEvent( string.Format( "Render bucket {0},{1}", lightmapBucketIndex.X,
				//   lightmapBucketIndex.Y ) );

				RectI bucketRectangle = new RectI( lightmapBucketIndex * bucketSize,
					( lightmapBucketIndex + new Vec2I( 1, 1 ) ) * bucketSize );

				if( bucketRectangle.Right > lightmapRenderingImage.Size.X )
					bucketRectangle.Right = lightmapRenderingImage.Size.X;
				if( bucketRectangle.Bottom > lightmapRenderingImage.Size.Y )
					bucketRectangle.Bottom = lightmapRenderingImage.Size.Y;

				lightmapRenderingImage.Fill( bucketRectangle, new ColorValue( 1, 1, 0 ) );

				ColorValue[] colors = new ColorValue[ bucketRectangle.Size.X * bucketRectangle.Size.Y ];

				for( int y = bucketRectangle.Minimum.Y; y < bucketRectangle.Maximum.Y; y++ )
				{
					for( int x = bucketRectangle.Minimum.X; x < bucketRectangle.Maximum.X; x++ )
					{
						Vec2I pixelIndex = new Vec2I( x, y );

						ColorValue color = RenderPixel( pixelIndex );

						Vec2I colorIndex = pixelIndex - bucketRectangle.Minimum;
						colors[ colorIndex.Y * bucketRectangle.Size.X + colorIndex.X ] = color;
					}
				}

				lightmapRenderingImage.Fill( bucketRectangle, colors );
			}

			//change bucket
			{
				Vec2I bucketCount = lightmapRenderingImage.Size / bucketSize;
				if( lightmapRenderingImage.Size.X % bucketSize != 0 )
					bucketCount.X++;
				if( lightmapRenderingImage.Size.Y % bucketSize != 0 )
					bucketCount.Y++;

				lightmapBucketIndex.X++;
				if( lightmapBucketIndex.X >= bucketCount.X )
				{
					lightmapBucketIndex.X = 0;
					lightmapBucketIndex.Y++;
					if( lightmapBucketIndex.Y >= bucketCount.Y )
						finished = true;
				}
			}

			//image finished. do final operations.
			if( finished )
			{
				lightmapRenderingImage.FillHoles( 5 );
				lightmapRenderingImage.Finish();
			}
		}

		Vec2I GetPixelIndexByTexCoord( Vec2 texCoord )
		{
			return new Vec2I(
				(int)( texCoord.X * (float)lightmapRenderingImage.Size.X + .5f ),
				(int)( texCoord.Y * (float)lightmapRenderingImage.Size.Y + .5f ) );
		}

		Vec2 GetTexCoordByPixelIndex( Vec2I pixelIndex )
		{
			return new Vec2(
				( (float)pixelIndex.X / (float)lightmapRenderingImage.Size.X ),
				( (float)pixelIndex.Y / (float)lightmapRenderingImage.Size.Y ) );
		}

		static void GetTriangePositionCoefficients( ref Vec2 p0, ref Vec2 p1, ref Vec2 p2,
			ref Vec2 point, out float coef0, out float coef1 )
		{
			coef0 = 0;
			coef1 = 0;

			//!!!!!it is necessary to check up if point outside of triangle?

			Vec2 p01;
			if( !MathUtils.IntersectRayRay( p0, p1, point, point + ( p2 - p1 ), out p01 ) )
				return;

			Vec2 p02;
			if( !MathUtils.IntersectRayRay( p0, p2, point, point + ( p2 - p1 ), out p02 ) )
				return;

			float p01Length = ( p1 - p0 ).Length();
			float p0_p01Length = ( p01 - p0 ).Length();
			if( p01Length != 0 )
				coef0 = p0_p01Length / p01Length;
			MathFunctions.Saturate( ref coef0 );

			float p01_p02Length = ( p02 - p01 ).Length();
			float p01_sourcePointLength = ( point - p01 ).Length();
			if( p01_p02Length != 0 )
				coef1 = p01_sourcePointLength / p01_p02Length;
			MathFunctions.Saturate( ref coef1 );
		}

		ColorValue RenderPixel( Vec2I pixelIndex )
		{
			int triangleIndex = lightmapTriangleMap[ pixelIndex.Y ][ pixelIndex.X ];
			if( triangleIndex == -1 )
				return new ColorValue( -1, -1, -1, -1 );

			Mesh mesh = lightmapMeshObject.Mesh;

			Mat4 transform = new Mat4( lightmapMeshObject.Rotation.ToMat3() *
				Mat3.FromScale( lightmapMeshObject.Scale ), lightmapMeshObject.Position );

			Vec3 position;
			Vec3 normal;
			{
				Vec2 texCoord = GetTexCoordByPixelIndex( pixelIndex );

				int index0 = mesh.Indices[ triangleIndex * 3 + 0 ];
				int index1 = mesh.Indices[ triangleIndex * 3 + 1 ];
				int index2 = mesh.Indices[ triangleIndex * 3 + 2 ];

				Vec3 position0 = transform * mesh.Positions[ index0 ];
				Vec3 position1 = transform * mesh.Positions[ index1 ];
				Vec3 position2 = transform * mesh.Positions[ index2 ];

				Vec3 normal0 = lightmapMeshObject.Rotation * mesh.Normals[ index0 ];
				Vec3 normal1 = lightmapMeshObject.Rotation * mesh.Normals[ index1 ];
				Vec3 normal2 = lightmapMeshObject.Rotation * mesh.Normals[ index2 ];

				Vec2 texCoord0 = mesh.LightmapTexCoords[ index0 ];
				Vec2 texCoord1 = mesh.LightmapTexCoords[ index1 ];
				Vec2 texCoord2 = mesh.LightmapTexCoords[ index2 ];

				float coef0;
				float coef1;
				GetTriangePositionCoefficients( ref texCoord0, ref texCoord1, ref texCoord2,
					ref texCoord, out coef0, out coef1 );

				//calculate position
				{
					Vec3 p01 = Vec3.Lerp( position0, position1, coef0 );
					Vec3 p02 = Vec3.Lerp( position0, position2, coef0 );
					position = Vec3.Lerp( p01, p02, coef1 );
				}

				//calculate normal
				{
					Vec3 p01 = Vec3.Lerp( normal0, normal1, coef0 );
					Vec3 p02 = Vec3.Lerp( normal0, normal2, coef0 );
					normal = Vec3.Lerp( p01, p02, coef1 );
					normal.Normalize();
				}
			}

			//calculate pixel color
			ColorValue resultColor = new ColorValue( 0, 0, 0 );

			foreach( MyLight light in lights )
			{
				//simple culling method. need use grid
				if( !light.Bounds.IsContainsPoint( position ) )
					continue;

				//calculate illumination
				float illumination = light.GetIllumination( position, normal );
				if( illumination <= .00001f )
					continue;

				//check for direct visibility
				bool shadowed = false;

				if( calculateShadows )
				{
					Ray ray = light.GetCheckVisibilityRay( position );

					RayCastResult result = physicsScene.RayCast( ray, contactGroup );
					if( result.Shape != null )
						shadowed = true;
				}

				ColorValue color = light.DiffuseColor * illumination;

				if( shadowed )
					color *= new ColorValue( 1, 1, 1 ) - shadowColor;

				resultColor += color;
			}

			resultColor.Alpha = 1;
			return resultColor;
		}

		protected override void OnEndRenderLightmap()
		{
			lightmapRenderingImage = null;
			lightmapMeshObject = null;
			lightmapBucketIndex = Vec2I.Zero;
		}

		List<StaticLightingCalculationWorld.IrradianceVolumeCellLightItem> tempCellLightItemList =
			new List<IrradianceVolumeCellLightItem>();

		protected override StaticLightingCalculationWorld.IrradianceVolumeCellLightItem[]
			OnCalculateIrradianceVolumeCell( Vec3 position )
		{
			tempCellLightItemList.Clear();

			foreach( MyLight light in lights )
			{
				//simple culling method. need use grid
				if( !light.Bounds.IsContainsPoint( position ) )
					continue;

				//calculate illumination
				float illumination = light.GetIllumination( position );
				if( illumination <= .00001f )
					continue;

				//check for direct visibility
				bool shadowed = false;

				if( calculateShadows )
				{
					Ray ray = light.GetCheckVisibilityRay( position );

					RayCastResult result = physicsScene.RayCast( ray, contactGroup );
					if( result.Shape != null )
						shadowed = true;
				}

				float power = 1;

				if( shadowed )
				{
					float coef = 1.0f - ( shadowColor.Red + shadowColor.Green + shadowColor.Blue ) / 3.0f;
					power *= coef;
				}

				if( power <= .005f )
					continue;

				tempCellLightItemList.Add( new IrradianceVolumeCellLightItem( light.Data, power ) );
			}


			return tempCellLightItemList.ToArray();
		}
	}
}
