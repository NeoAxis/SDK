// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;
using Engine.MapSystem;

namespace ProjectCommon
{
	/// <summary>
	/// This class is intended only for advanced users. By means this class the developer can change behavior 
	/// of getting the list of visible objects in the camera, getting the list of objects for shadow generation.
	/// </summary>
	public class RenderingLowLevelMethodsImpl : RenderingLowLevelMethods
	{
		const float shadowMapFarClipDistance = 2000;
		const float directionalLightExtrusionDistance = 1000;

		RenderLight[] tempLightArray = new RenderLight[ 0 ];

		//clip volumes for occlusion culling
		static Stack<Box[]> clipVolumes = new Stack<Box[]>();
		static Box[] totalClipVolumes = new Box[ 0 ];

		///////////////////////////////////////////

		public override void OnSceneManagementUpdateShadowSettings()
		{
			ShadowTechniques needShadowTechnique = Map.Instance.ShadowTechnique;
			PixelFormat textureFormatForDirectionalLight = PixelFormat.L8;
			PixelFormat textureFormatForSpotLight = PixelFormat.L8;
			PixelFormat textureFormatForPointLight = PixelFormat.L8;
			HighLevelMaterial[] defaultCasterMaterials = new HighLevelMaterial[ 3 ];
			bool textureSelfShadow = false;
			bool renderBackFaces = false;
			bool pssm = false;
			float[] pssmSplitDistances = new float[ 0 ];
			int directionalLightMaxTextureCount = Map.Instance.ShadowDirectionalLightMaxTextureCount;
			int spotLightMaxTextureCount = Map.Instance.ShadowSpotLightMaxTextureCount;
			int pointLightMaxTextureCount = Map.Instance.ShadowPointLightMaxTextureCount;
			int maxTextureSize = RenderSystem.Instance.Capabilities.MaxTextureSize;
			Vec2[] shadowLightBiasDirectionalLight = new Vec2[ 3 ];
			Vec2 shadowLightBiasPointLight = Vec2.Zero;
			Vec2 shadowLightBiasSpotLight = Vec2.Zero;

			//no shadows for OpenGL ES.
			if( RenderSystem.Instance.IsOpenGLES() )
				needShadowTechnique = ShadowTechniques.None;

			bool shadowMap =
				needShadowTechnique == ShadowTechniques.ShadowmapLow ||
				needShadowTechnique == ShadowTechniques.ShadowmapMedium ||
				needShadowTechnique == ShadowTechniques.ShadowmapHigh ||
				needShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
				needShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
				needShadowTechnique == ShadowTechniques.ShadowmapHighPSSM;

			if( shadowMap )
			{
				bool atiHardwareShadows = false;
				bool nvidiaHardwareShadows = false;
				{
					if( ( RenderSystem.Instance.IsDirect3D() || RenderSystem.Instance.IsOpenGL() ) &&
						RenderSystem.Instance.IsDepthShadowmapSupported() &&
						RenderSystem.Instance.HasShaderModel3() &&
						TextureManager.Instance.IsFormatSupported( Texture.Type.Type2D,
						PixelFormat.Depth24, Texture.Usage.RenderTarget ) )
					{
						if( RenderSystem.Instance.Capabilities.Vendor == GPUVendors.ATI )
							atiHardwareShadows = true;
						if( RenderSystem.Instance.Capabilities.Vendor == GPUVendors.NVidia )
							nvidiaHardwareShadows = true;
					}
				}

				if( RenderSystem.Instance.IsDepthShadowmapSupported() )
				{
					//set texture formats
					textureFormatForDirectionalLight = PixelFormat.Float32R;
					textureFormatForSpotLight = PixelFormat.Float32R;
					textureFormatForPointLight = PixelFormat.Float32R;
					if( nvidiaHardwareShadows )
					{
						textureFormatForDirectionalLight = PixelFormat.Depth24;
						textureFormatForSpotLight = PixelFormat.Depth24;
					}

					//create material for each light type
					for( int nLightType = 0; nLightType < 3; nLightType++ )
					{
						RenderLightType lightType = (RenderLightType)nLightType;

						string materialName = string.Format( "__(system)DefaultShadowCaster_{0}Light", lightType );
						if( lightType == RenderLightType.Directional || lightType == RenderLightType.Spot )
						{
							if( atiHardwareShadows )
								materialName += "_AtiHardwareShadows";
							if( nvidiaHardwareShadows )
								materialName += "_NvidiaHardwareShadows";
						}

						DefaultShadowCasterMaterial material = (DefaultShadowCasterMaterial)
							HighLevelMaterialManager.Instance.GetMaterialByName( materialName );
						if( material == null )
						{
							material = (DefaultShadowCasterMaterial)HighLevelMaterialManager.Instance.
								CreateMaterial( materialName, typeof( DefaultShadowCasterMaterial ).Name );

							material.LightType = lightType;
							if( lightType == RenderLightType.Directional || lightType == RenderLightType.Spot )
							{
								material.AtiHardwareShadows = atiHardwareShadows;
								material.NvidiaHardwareShadows = nvidiaHardwareShadows;
							}
						}

						//set default caster material for scene manager
						defaultCasterMaterials[ (int)lightType ] = material;
					}

					textureSelfShadow = true;

					if( RenderSystem.Instance.HasShaderModel3() )
					{
						if( needShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
							needShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
							needShadowTechnique == ShadowTechniques.ShadowmapHighPSSM )
						{
							pssm = true;
							pssmSplitDistances = new float[ 4 ];
							pssmSplitDistances[ 0 ] = Map.Instance.NearFarClipDistance.Minimum;
							pssmSplitDistances[ 1 ] = Map.Instance.ShadowPSSMSplitFactors[ 0 ] *
								Map.Instance.ShadowFarDistance;
							pssmSplitDistances[ 2 ] = Map.Instance.ShadowPSSMSplitFactors[ 1 ] *
								Map.Instance.ShadowFarDistance;
							pssmSplitDistances[ 3 ] = Map.Instance.ShadowFarDistance;

							if( pssmSplitDistances[ 1 ] > pssmSplitDistances[ 2 ] )
								pssmSplitDistances[ 1 ] = pssmSplitDistances[ 2 ] - .001f;
						}
					}

					if( !RenderSystem.Instance.HasShaderModel3() )
						needShadowTechnique = ShadowTechniques.ShadowmapLow;

					//PSSM is not supported for OpenGL at this time.
					if( RenderSystem.Instance.IsOpenGL() )
					{
						if( needShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
							needShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
							needShadowTechnique == ShadowTechniques.ShadowmapHighPSSM )
						{
							Log.Warning( "At this time Parallel-Split Shadow Map is not supported for OpenGL." );
							needShadowTechnique = ShadowTechniques.ShadowmapLow;
						}
					}
				}
				else if( RenderSystem.Instance.Capabilities.HardwareRenderToTexture )
				{
					needShadowTechnique = ShadowTechniques.ShadowmapLow;

					ColorValue shadowColor = Map.Instance.ShadowColor;
					if( shadowColor.Red == shadowColor.Green && shadowColor.Red == shadowColor.Blue )
					{
						textureFormatForDirectionalLight = PixelFormat.L8;
						textureFormatForSpotLight = PixelFormat.L8;
						textureFormatForPointLight = PixelFormat.L8;
					}
					else
					{
						textureFormatForDirectionalLight = PixelFormat.X8R8G8B8;
						textureFormatForSpotLight = PixelFormat.X8R8G8B8;
						textureFormatForPointLight = PixelFormat.X8R8G8B8;
					}

					//no point light shadows for ffp
					pointLightMaxTextureCount = 0;

					//for old card compatibility
					if( !RenderSystem.Instance.Capabilities.FragmentProgram )
						maxTextureSize = 1024;
					//if( !RenderSystem.Instance.Capabilities.HardwareRenderToTexture )
					//   maxTextureSize = 512;

					renderBackFaces = true;
				}
				else
				{
					needShadowTechnique = ShadowTechniques.None;
				}

				//bias
				{
					//good idea to adapt bias for each project.

					float qualityFactor;
					{
						if( needShadowTechnique == ShadowTechniques.ShadowmapHigh ||
							needShadowTechnique == ShadowTechniques.ShadowmapHighPSSM ||
							needShadowTechnique == ShadowTechniques.ShadowmapMedium ||
							needShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM )
						{
							qualityFactor = 1.5f;
						}
						else
						{
							qualityFactor = 1;
						}
					}

					if( RenderSystem.Instance.HasShaderModel3() && RenderSystem.Instance.IsDirect3D() )
					{
						//directional light
						{
							//NVIDIA: Depth24 texture format
							//ATI: Float32 texture format

							float[] factors = null;

							switch( needShadowTechnique )
							{
							case ShadowTechniques.ShadowmapLow:
								factors = new float[] { 1.0f };
								break;
							case ShadowTechniques.ShadowmapMedium:
								factors = new float[] { 1.5f };
								break;
							case ShadowTechniques.ShadowmapHigh:
								factors = new float[] { 1.5f };
								break;
							case ShadowTechniques.ShadowmapLowPSSM:
								factors = new float[] { 1.0f, 1.0f, 1.0f };
								break;
							case ShadowTechniques.ShadowmapMediumPSSM:
								factors = new float[] { 1.5f, 1.0f, 1.0f };
								break;
							case ShadowTechniques.ShadowmapHighPSSM:
								factors = new float[] { 1.5f, 1.5f, 1.0f };
								break;
							}

							float iterationCount = pssm ? 3 : 1;
							for( int index = 0; index < iterationCount; index++ )
							{
								if( nvidiaHardwareShadows )
								{
									//Depth24 texture format
									shadowLightBiasDirectionalLight[ index ] =
										new Vec2( .0001f + .00005f * (float)index, factors[ index ] );
								}
								else
								{
									//Float32 texture format
									shadowLightBiasDirectionalLight[ index ] =
										new Vec2( .0001f + .00005f * (float)index, factors[ index ] );
								}
							}
						}

						//point light
						{
							//Float32 texture format (both for NVIDIA and ATI)
							shadowLightBiasPointLight = new Vec2( .2f * qualityFactor, .5f * qualityFactor );
						}

						//spot light
						{
							if( nvidiaHardwareShadows )
							{
								//Depth24 texture format
								float textureSize = Map.Instance.GetShadowSpotLightTextureSizeAsInteger();
								float textureSizeFactor = 1024.0f / textureSize;
								shadowLightBiasSpotLight =
									new Vec2( .001f * qualityFactor * textureSizeFactor, .001f * qualityFactor );
							}
							else
							{
								//Float32 texture format
								shadowLightBiasSpotLight = new Vec2( .1f * qualityFactor, 1.0f * qualityFactor );
							}
						}
					}
					else
					{
						shadowLightBiasDirectionalLight[ 0 ] = new Vec2( .0003f * qualityFactor, 0 );
						shadowLightBiasPointLight = new Vec2( .15f * qualityFactor, 0 );
						shadowLightBiasSpotLight = new Vec2( .15f * qualityFactor, 0 );
					}
				}

			}

			//apply settings to scene manager

			SceneManager.Instance.ShadowColor = Map.Instance.ShadowColor;
			SceneManager.Instance.ShadowFarDistance = Map.Instance.ShadowFarDistance;
			SceneManager.Instance.ShadowCasterRenderBackFaces = renderBackFaces;
			SceneManager.Instance.ShadowTextureFadeStart = .9f;

			if( shadowMap )
			{
				SceneManager.Instance.SetShadowTextureSettings(
					textureFormatForDirectionalLight,
					Math.Min( Map.Instance.GetShadowDirectionalLightTextureSizeAsInteger(), maxTextureSize ),
					directionalLightMaxTextureCount,
					pssm ? 3 : 1,
					pssmSplitDistances,
					textureFormatForSpotLight,
					Math.Min( Map.Instance.GetShadowSpotLightTextureSizeAsInteger(), maxTextureSize ),
					spotLightMaxTextureCount,
					textureFormatForPointLight,
					Math.Min( Map.Instance.GetShadowPointLightTextureSizeAsInteger(), maxTextureSize ),
					pointLightMaxTextureCount );

				SceneManager.Instance.ShadowTextureSelfShadow = textureSelfShadow;

				for( int n = 0; n < shadowLightBiasDirectionalLight.Length; n++ )
				{
					Vec2 value = shadowLightBiasDirectionalLight[ n ];
					SceneManager.Instance.SetShadowLightBias( RenderLightType.Directional, n, value.X, value.Y );
				}
				SceneManager.Instance.SetShadowLightBias( RenderLightType.Point, 0,
					shadowLightBiasPointLight.X, shadowLightBiasPointLight.Y );
				SceneManager.Instance.SetShadowLightBias( RenderLightType.Spot, 0,
					shadowLightBiasSpotLight.X, shadowLightBiasSpotLight.Y );
			}
			else
			{
				SceneManager.Instance.SetShadowTextureSettings( PixelFormat.X8R8G8B8,
					8, 0, 0, new float[] { }, PixelFormat.X8R8G8B8, 8, 0, PixelFormat.X8R8G8B8, 8, 0 );
			}

			SceneManager.Instance.ShadowTechnique = needShadowTechnique;

			//update default caster materials
			{
				foreach( HighLevelMaterial material in defaultCasterMaterials )
				{
					if( material != null )
						material.UpdateBaseMaterial();
				}

				for( int n = 0; n < 3; n++ )
				{
					HighLevelMaterial material = defaultCasterMaterials[ n ];
					SceneManager.Instance.SetShadowTextureDefaultCasterMaterialName(
						(RenderLightType)n, material != null ? material.Name : "" );
				}
			}
		}

		void GetDirectionalLightCameraCornerPoints( Camera mainCamera, float initialNearDistance,
			float initialFarDistance, bool clipByShadowFarDistance, out Vec3[] cornerPoints )
		{
			float nearDistance = initialNearDistance;
			float farDistance = initialFarDistance;

			Frustum frustum = FrustumUtils.GetFrustumByCamera( mainCamera );

			//clip by shadow far distance sphere (only for perspective camera)
			if( mainCamera.ProjectionType == ProjectionTypes.Perspective && clipByShadowFarDistance )
			{
				Vec3[] points = null;
				frustum.ToPoints( ref points );

				Sphere sphere = new Sphere( mainCamera.DerivedPosition, farDistance );

				Vec3[] intersections = new Vec3[ 3 ];

				for( int n = 0; n < 3; n++ )
				{
					Vec3 pointEnd = points[ n + 4 ];

					float scale1;
					float scale2;
					Ray ray = new Ray( mainCamera.DerivedPosition, pointEnd - mainCamera.DerivedPosition );
					sphere.RayIntersection( ray, out scale1, out scale2 );
					float scale = Math.Max( scale1, scale2 );

					intersections[ n ] = ray.GetPointOnRay( scale );
				}

				Plane farPlane = Plane.FromPoints( intersections[ 0 ], intersections[ 1 ], intersections[ 2 ] );

				Ray cameraDirectionAsRay = new Ray( mainCamera.DerivedPosition, mainCamera.DerivedDirection );

				Vec3 pointByDirection;
				farPlane.RayIntersection( cameraDirectionAsRay, out pointByDirection );

				farDistance = ( pointByDirection - mainCamera.DerivedPosition ).Length();
				if( nearDistance + 5 > farDistance )
					farDistance = nearDistance + 5;
			}

			if( nearDistance < .0001f )
				nearDistance = .0001f;
			if( farDistance < nearDistance + .01f )
				farDistance = nearDistance + .01f;

			frustum.NearDistance = nearDistance;
			frustum.MoveFarDistance( farDistance );
			cornerPoints = null;
			frustum.ToPoints( ref cornerPoints );
		}

		Vec3 GetDirectionalLightCameraDestinationPoint( Camera mainCamera, Vec3[] cornerPoints )
		{
			if( mainCamera.ProjectionType == ProjectionTypes.Perspective )
			{
				//perspective camera

				Ray cameraDirectionAsRay = new Ray( mainCamera.DerivedPosition, mainCamera.DerivedDirection );

				Vec3 nearPoint = cornerPoints[ 0 ];
				Vec3 farPoint = cornerPoints[ 4 ];

				Vec3 projectedPoint = MathUtils.ProjectPointToLine( cameraDirectionAsRay.Origin,
					cameraDirectionAsRay.Origin + cameraDirectionAsRay.Direction, farPoint );

				if( ( projectedPoint - farPoint ).Length() >= ( projectedPoint - nearPoint ).Length() )
				{
					return projectedPoint;
				}
				else
				{
					Vec3 centerBetweenPoints = ( nearPoint + farPoint ) / 2;

					Vec3 normal = ( farPoint - centerBetweenPoints ).GetNormalize();
					Plane plane = Plane.FromPointAndNormal( centerBetweenPoints, normal );

					float scale;
					plane.RayIntersection( cameraDirectionAsRay, out scale );

					return cameraDirectionAsRay.GetPointOnRay( scale );
				}
			}
			else
			{
				//orthographic camera

				Vec3 destinationPoint = Vec3.Zero;
				foreach( Vec3 point in cornerPoints )
					destinationPoint += point;
				destinationPoint /= (float)cornerPoints.Length;

				return destinationPoint;
			}
		}

		public override void OnGetShadowmapCameraSetup( Camera mainCamera, RenderLight light,
			int directionalLightPSSMTextureIndex, int pointLightFaceIndex, Camera lightCamera,
			ref bool skipThisShadowmap )
		{
			if( light.Type == RenderLightType.Directional )
			{
				//Directional light

				float orthoWindowSize;
				Vec3 lightCameraPosition;
				Vec3 lightCameraUp;

				Vec3[] cornerPoints;
				{
					if( SceneManager.Instance.IsShadowTechniquePSSM() )
					{
						float[] splitDistances = SceneManager.Instance.ShadowDirectionalLightSplitDistances;

						float nearSplitDistance = splitDistances[ directionalLightPSSMTextureIndex ];
						float farSplitDistance = splitDistances[ directionalLightPSSMTextureIndex + 1 ];
						bool lastTextureIndex = directionalLightPSSMTextureIndex ==
							SceneManager.Instance.ShadowDirectionalLightSplitTextureCount - 1;

						GetDirectionalLightCameraCornerPoints( mainCamera, nearSplitDistance, farSplitDistance,
							lastTextureIndex, out cornerPoints );
					}
					else
					{
						GetDirectionalLightCameraCornerPoints( mainCamera, mainCamera.NearClipDistance,
							SceneManager.Instance.ShadowFarDistance, true, out cornerPoints );
					}
				}

				Vec3 destinationPoint = GetDirectionalLightCameraDestinationPoint( mainCamera, cornerPoints );

				lightCameraPosition = destinationPoint - light.Direction * directionalLightExtrusionDistance;

				lightCameraUp = Vec3.ZAxis;
				if( Math.Abs( Vec3.Dot( lightCameraUp, light.Direction ) ) >= .99f )
					lightCameraUp = Vec3.YAxis;

				float maxDistance = 0;
				{
					foreach( Vec3 point in cornerPoints )
					{
						float distance = ( point - destinationPoint ).Length();
						if( distance > maxDistance )
							maxDistance = distance;
					}
				}

				orthoWindowSize = maxDistance * 2 * 1.05f;
				//fix epsilon error
				orthoWindowSize = ( (int)( orthoWindowSize / 5 ) ) * 5 + 5;

				//fix jittering
				{
					Quat lightRotation = Quat.FromDirectionZAxisUp( light.Direction );

					//convert world space camera position into light space
					Vec3 lightSpacePos = lightRotation.GetInverse() * lightCameraPosition;

					//snap to nearest texel
					float textureSize = SceneManager.Instance.ShadowDirectionalLightTextureSize;
					float worldTexelSize = orthoWindowSize / textureSize;
					lightSpacePos.Y -= (float)Math.IEEERemainder( lightSpacePos.Y, worldTexelSize );
					lightSpacePos.Z -= (float)Math.IEEERemainder( lightSpacePos.Z, worldTexelSize );

					//convert back to world space
					lightCameraPosition = lightRotation * lightSpacePos;
				}

				lightCamera.ProjectionType = ProjectionTypes.Orthographic;
				lightCamera.AspectRatio = 1;
				lightCamera.OrthoWindowHeight = orthoWindowSize;
				lightCamera.NearClipDistance = mainCamera.NearClipDistance;
				lightCamera.FarClipDistance = shadowMapFarClipDistance;
				lightCamera.Position = lightCameraPosition;
				lightCamera.FixedUp = lightCameraUp;
				lightCamera.Direction = light.Direction;
			}
			else if( light.Type == RenderLightType.Spot )
			{
				//Spot light

				Degree fov = new Radian( light.SpotlightOuterAngle * 1.05f ).InDegrees();
				if( fov > 179 )
					fov = 179;

				Vec3 up = Vec3.ZAxis;
				if( Math.Abs( Vec3.Dot( up, light.Direction ) ) >= 1.0f )
					up = Vec3.YAxis;

				lightCamera.ProjectionType = ProjectionTypes.Perspective;
				lightCamera.AspectRatio = 1;
				lightCamera.Fov = fov;
				lightCamera.NearClipDistance = mainCamera.NearClipDistance;
				lightCamera.FarClipDistance = shadowMapFarClipDistance;// light.AttenuationFar * 1.05f;
				lightCamera.Position = light.Position;
				lightCamera.FixedUp = up;
				lightCamera.Direction = light.Direction;
			}
			else
			{
				//Point light

				//you can completely skip specified faces for better performance.
				//use "skipThisShadowmap" parameter for this.
				//example:
				//if( pointLightFaceIndex == 3 )
				//   skipThisShadowmap = true;

				Vec3 dir = Vec3.Zero;
				Vec3 up = Vec3.Zero;

				switch( pointLightFaceIndex )
				{
				case 0: dir = -Vec3.YAxis; up = Vec3.ZAxis; break;
				case 1: dir = Vec3.YAxis; up = Vec3.ZAxis; break;
				case 2: dir = Vec3.ZAxis; up = -Vec3.XAxis; break;
				case 3: dir = -Vec3.ZAxis; up = Vec3.XAxis; break;
				case 4: dir = Vec3.XAxis; up = Vec3.ZAxis; break;
				case 5: dir = -Vec3.XAxis; up = Vec3.ZAxis; break;
				}

				lightCamera.ProjectionType = ProjectionTypes.Perspective;
				lightCamera.AspectRatio = 1;
				lightCamera.Fov = 90;
				lightCamera.NearClipDistance = mainCamera.NearClipDistance;
				lightCamera.FarClipDistance = shadowMapFarClipDistance;// light.AttenuationFar * 1.05f;
				lightCamera.Position = light.Position;
				lightCamera.FixedUp = up;
				lightCamera.Direction = dir;
			}
		}

		ConvexPolyhedron GetConvexPolyhedronFromFrustum( ref Frustum frustum )
		{
			Vec3[] points = null;
			frustum.ToPoints( ref points );

			ConvexPolyhedron.Face[] faces = new ConvexPolyhedron.Face[ 12 ];

			faces[ 0 ] = new ConvexPolyhedron.Face( 5, 4, 7 );
			faces[ 1 ] = new ConvexPolyhedron.Face( 7, 6, 5 );
			faces[ 2 ] = new ConvexPolyhedron.Face( 0, 1, 2 );
			faces[ 3 ] = new ConvexPolyhedron.Face( 2, 3, 0 );

			faces[ 4 ] = new ConvexPolyhedron.Face( 4, 0, 3 );
			faces[ 5 ] = new ConvexPolyhedron.Face( 3, 7, 4 );
			faces[ 6 ] = new ConvexPolyhedron.Face( 1, 5, 6 );
			faces[ 7 ] = new ConvexPolyhedron.Face( 6, 2, 1 );

			faces[ 8 ] = new ConvexPolyhedron.Face( 6, 7, 3 );
			faces[ 9 ] = new ConvexPolyhedron.Face( 3, 2, 6 );
			faces[ 10 ] = new ConvexPolyhedron.Face( 4, 5, 1 );
			faces[ 11 ] = new ConvexPolyhedron.Face( 1, 0, 4 );

			return new ConvexPolyhedron( points, faces, .0001f );
		}

		static ConvexPolyhedron MakeConvexPolyhedronForPointLight( RenderLight light )
		{
			float radius = light.AttenuationFar;
			radius /= MathFunctions.Cos( MathFunctions.PI * 2.0f / 32.0f );

			Vec3[] vertices;
			int[] indices;
			GeometryGenerator.GenerateSphere( radius, 8, 8, false, out vertices, out indices );

			for( int n = 0; n < vertices.Length; n++ )
				vertices[ n ] = light.Position + vertices[ n ];

			return new ConvexPolyhedron( vertices, indices, .0001f );
		}

		static ConvexPolyhedron MakeConvexPolyhedronForSpotLight( RenderLight light )
		{
			float outerAngle = light.SpotlightOuterAngle;
			if( outerAngle < new Degree( 1 ).InRadians() )
				outerAngle = new Degree( 1 ).InRadians();
			if( outerAngle > new Degree( 179 ).InRadians() )
				outerAngle = new Degree( 179 ).InRadians();

			List<Vec3> vertices = new List<Vec3>( 10 );
			List<ConvexPolyhedron.Face> faces = new List<ConvexPolyhedron.Face>( 16 );

			Mat3 worldRotation = Quat.FromDirectionZAxisUp( light.Direction ).ToMat3();

			float sideAngle;
			{
				float radius = MathFunctions.Sin( outerAngle / 2 ) * light.AttenuationFar;

				float l = MathFunctions.Sqrt( light.AttenuationFar * light.AttenuationFar - radius * radius );
				radius /= MathFunctions.Cos( MathFunctions.PI * 2 / 16 );

				sideAngle = MathFunctions.ATan( radius / l );
			}

			Vec3 farPoint;
			{
				Mat3 pointRotation = worldRotation * Mat3.FromRotateByY( outerAngle / 4 );
				Vec3 direction = pointRotation * Vec3.XAxis;
				Vec3 point = light.Position + direction * light.AttenuationFar;

				Plane plane = Plane.FromPointAndNormal( point, direction );
				Ray ray = new Ray( light.Position, light.Direction * light.AttenuationFar );

				float scale;
				plane.RayIntersection( ray, out scale );
				farPoint = ray.GetPointOnRay( scale * 1.05f );
			}

			vertices.Add( light.Position );
			vertices.Add( farPoint );

			for( int nAxisAngle = 0; nAxisAngle < 8; nAxisAngle++ )
			{
				float axisAngle = ( MathFunctions.PI * 2 ) * ( (float)nAxisAngle / 8 );

				Mat3 worldAxisRotation = worldRotation * Mat3.FromRotateByX( axisAngle );

				Plane sidePlane;
				{
					Mat3 sideAngleRotation = Mat3.FromRotateByY( sideAngle + MathFunctions.PI / 2 );
					Mat3 pointRotation = worldAxisRotation * sideAngleRotation;
					sidePlane = Plane.FromPointAndNormal( light.Position, pointRotation * Vec3.XAxis );
				}

				{
					Mat3 pointRotation = worldAxisRotation * Mat3.FromRotateByY( outerAngle / 4 );
					Vec3 direction = pointRotation * Vec3.XAxis;
					Vec3 point = light.Position + direction * ( light.AttenuationFar * 1.05f );

					Ray ray = new Ray( farPoint, point - farPoint );

					float scale;
					sidePlane.RayIntersection( ray, out scale );
					Vec3 p = ray.GetPointOnRay( scale );

					vertices.Add( p );
				}
			}

			for( int n = 0; n < 8; n++ )
			{
				faces.Add( new ConvexPolyhedron.Face( 0, n + 2, ( n + 1 ) % 8 + 2 ) );
				faces.Add( new ConvexPolyhedron.Face( 1, ( n + 1 ) % 8 + 2, n + 2 ) );
			}

			//foreach( ConvexPolyhedron.Face face in faces )
			//{
			//   camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
			//   Vec3 p0 = vertices[ face.Vertex0 ];
			//   Vec3 p1 = vertices[ face.Vertex1 ];
			//   Vec3 p2 = vertices[ face.Vertex2 ];
			//   camera.DebugGeometry.AddLine( p0, p1 );
			//   camera.DebugGeometry.AddLine( p1, p2 );
			//   camera.DebugGeometry.AddLine( p2, p0 );

			//   Vec3[] v = new Vec3[ 3 ] { p0, p1, p2 };
			//   int[] i = new int[] { 0, 1, 2 };
			//   camera.DebugGeometry.Color = new ColorValue( 0, 0, 1, .5f );
			//   camera.DebugGeometry.AddVertexIndexBuffer( v, i, Mat4.Identity, false, true );
			//}
			//camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
			//foreach( Vec3 vertex in vertices )
			//   camera.DebugGeometry.AddSphere( new Sphere( vertex, .1f ) );

			return new ConvexPolyhedron( vertices.ToArray(), faces.ToArray(), .0001f );
		}

		public override void OnSceneManagementGetLightsForCamera( Camera camera,
			List<RenderingLowLevelMethods.LightItem> outLights )
		{
			Frustum frustum = FrustumUtils.GetFrustumByCamera( camera );
			if( EngineDebugSettings.FrustumTest && camera.AllowFrustumTestMode )
			{
				frustum.HalfWidth *= .5f;
				frustum.HalfHeight *= .5f;
			}

			ConvexPolyhedron frustumPolyhedron = GetConvexPolyhedronFromFrustum( ref frustum );

			ICollection<RenderLight> list;
			if( SceneManager.Instance.OverrideVisibleObjects != null )
				list = SceneManager.Instance.OverrideVisibleObjects.Lights;
			else
				list = SceneManager.Instance.RenderLights;

			foreach( RenderLight light in list )
			{
				if( !light.Visible )
					continue;

				bool allowCastShadows = true;

				if( light.Type == RenderLightType.Point || light.Type == RenderLightType.Spot )
				{
					if( light.AttenuationFar <= .01f )
						continue;

					Sphere lightSphere = new Sphere( light.Position, light.AttenuationFar );

					//fast culling. not cull everything.
					if( !frustum.IsIntersectsFast( lightSphere ) )
						continue;

					//generate convex polyhedron for light volume 
					//and check intersection with camera frustum.
					ConvexPolyhedron lightPolyhedron = null;
					if( light.Type == RenderLightType.Point )
						lightPolyhedron = MakeConvexPolyhedronForPointLight( light );
					else if( light.Type == RenderLightType.Spot )
						lightPolyhedron = MakeConvexPolyhedronForSpotLight( light );

					if( !ConvexPolyhedron.IsIntersects( frustumPolyhedron, lightPolyhedron ) )
						continue;

					//allowCastShadows
					if( light.CastShadows )
					{
						Sphere frustumShadowSphere = new Sphere( camera.DerivedPosition,
							SceneManager.Instance.ShadowFarDistance );

						if( frustumShadowSphere.IsIntersectsSphere( lightSphere ) )
						{
							if( light.Type == RenderLightType.Spot )
							{
								Cone cone = new Cone( light.Position, light.Direction,
									light.SpotlightOuterAngle / 2 );

								if( !cone.IsIntersectsSphere( frustumShadowSphere ) )
									allowCastShadows = false;
							}
						}
						else
							allowCastShadows = false;
					}
				}

				outLights.Add( new RenderingLowLevelMethods.LightItem( light, allowCastShadows ) );
			}
		}

		static Plane[] GetClipPlanesOfConvexHullAroundMainCameraAndLightPosition(
			Camera mainCamera, RenderLight light )
		{
			Frustum mainFrustum = FrustumUtils.GetFrustumByCamera( mainCamera );
			if( EngineDebugSettings.FrustumTest && mainCamera.AllowFrustumTestMode )
			{
				mainFrustum.HalfWidth *= .5f;
				mainFrustum.HalfHeight *= .5f;
			}

			if( SceneManager.Instance.ShadowFarDistance > mainFrustum.NearDistance )
				mainFrustum.MoveFarDistance( SceneManager.Instance.ShadowFarDistance );

			Vec3[] frustumPoints = null;
			mainFrustum.ToPoints( ref frustumPoints );

			//create convex hull from all camera corner points and light position.
			List<Vec3> convexHullVertices = new List<Vec3>();
			convexHullVertices.Add( mainFrustum.Origin );
			for( int n = 4; n < 8; n++ )
				convexHullVertices.Add( frustumPoints[ n ] );
			convexHullVertices.Add( light.Position );

			Plane[] hullPlanes = ConvexPolyhedron.GetConvexPolyhedronPlanesFromVertices(
				convexHullVertices, .0001f );
			return hullPlanes;
		}

		static void WalkSpotLightShadowGeneration( Camera mainCamera, RenderLight light,
			List<SceneNode> outSceneNodes, List<StaticMeshObject> outStaticMeshObjects )
		{
			Sphere lightSphere = new Sphere( light.Position, light.AttenuationFar );

			int[] sceneGraphIndexes;
			if( SceneManager.Instance.OverrideVisibleObjects != null )
			{
				sceneGraphIndexes = GetOverrideVisibleObjectsSceneGraphIndexes();
			}
			else
			{
				Plane[] clipPlanes;
				{
					//add spot light clip planes
					Plane[] array1 = light.SpotLightClipPlanes;
					//add main frustum clip planes and light position
					Plane[] array2 = GetClipPlanesOfConvexHullAroundMainCameraAndLightPosition( mainCamera, light );

					clipPlanes = new Plane[ array1.Length + array2.Length ];
					Array.Copy( array1, 0, clipPlanes, 0, array1.Length );
					Array.Copy( array2, 0, clipPlanes, array1.Length, array2.Length );
				}
				Bounds clipBounds = lightSphere.ToBounds();
				sceneGraphIndexes = SceneManager.Instance.SceneGraph.GetObjects( clipPlanes, clipBounds, 0xFFFFFFFF );
			}

			foreach( int sceneGraphIndex in sceneGraphIndexes )
			{
				SceneManager.SceneGraphObjectData data = SceneManager.Instance.SceneGraphObjects[ sceneGraphIndex ];

				//SceneNode
				SceneNode sceneNode = data.SceneNode;
				if( sceneNode != null && sceneNode.Visible && sceneNode.IsContainsObjectWithCastsShadowsEnabled() )
				{
					Bounds sceneNodeBounds = sceneNode.GetWorldBounds();
					//clip by sphere
					if( lightSphere.IsIntersectsBounds( sceneNodeBounds ) )
					{
						//clip volumes
						if( !IsTotalClipVolumesContainsBounds( sceneNodeBounds ) )
							outSceneNodes.Add( sceneNode );
					}
				}

				//StaticMeshObject
				StaticMeshObject staticMeshObject = data.StaticMeshObject;
				if( staticMeshObject != null && staticMeshObject.Visible && staticMeshObject.CastShadows )
				{
					//clip by sphere
					if( lightSphere.IsIntersectsBounds( staticMeshObject.Bounds ) )
					{
						//clip volumes
						if( !IsTotalClipVolumesContainsBounds( staticMeshObject.Bounds ) )
							outStaticMeshObjects.Add( staticMeshObject );
					}
				}
			}
		}

		static Plane[] GetClipPlanesForPointLightShadowGeneration( Camera mainCamera,
			RenderLight light, Vec3 pointLightFaceDirection )
		{
			Plane[] clipPlanes = new Plane[ 5 ];

			Vec3 direction = pointLightFaceDirection;

			float attenuationRange = light.AttenuationFar;

			Vec3[] points = new Vec3[ 4 ];

			if( direction.Equals( Vec3.XAxis, .01f ) )
			{
				points[ 0 ] = new Vec3( 1, -1, -1 );
				points[ 1 ] = new Vec3( 1, -1, 1 );
				points[ 2 ] = new Vec3( 1, 1, 1 );
				points[ 3 ] = new Vec3( 1, 1, -1 );
				clipPlanes[ 4 ] = new Plane( direction, light.Position.X + attenuationRange );
			}
			else if( direction.Equals( -Vec3.XAxis, .01f ) )
			{
				points[ 0 ] = new Vec3( -1, 1, -1 );
				points[ 1 ] = new Vec3( -1, 1, 1 );
				points[ 2 ] = new Vec3( -1, -1, 1 );
				points[ 3 ] = new Vec3( -1, -1, -1 );
				clipPlanes[ 4 ] = new Plane( direction, -( light.Position.X - attenuationRange ) );
			}
			else if( direction.Equals( Vec3.YAxis, .01f ) )
			{
				points[ 0 ] = new Vec3( 1, 1, -1 );
				points[ 1 ] = new Vec3( 1, 1, 1 );
				points[ 2 ] = new Vec3( -1, 1, 1 );
				points[ 3 ] = new Vec3( -1, 1, -1 );
				clipPlanes[ 4 ] = new Plane( direction, light.Position.Y + attenuationRange );
			}
			else if( direction.Equals( -Vec3.YAxis, .01f ) )
			{
				points[ 0 ] = new Vec3( -1, -1, -1 );
				points[ 1 ] = new Vec3( -1, -1, 1 );
				points[ 2 ] = new Vec3( 1, -1, 1 );
				points[ 3 ] = new Vec3( 1, -1, -1 );
				clipPlanes[ 4 ] = new Plane( direction, -( light.Position.Y - attenuationRange ) );
			}
			else if( direction.Equals( Vec3.ZAxis, .01f ) )
			{
				points[ 0 ] = new Vec3( -1, 1, 1 );
				points[ 1 ] = new Vec3( 1, 1, 1 );
				points[ 2 ] = new Vec3( 1, -1, 1 );
				points[ 3 ] = new Vec3( -1, -1, 1 );
				clipPlanes[ 4 ] = new Plane( direction, light.Position.Z + attenuationRange );
			}
			else if( direction.Equals( -Vec3.ZAxis, .01f ) )
			{
				points[ 0 ] = new Vec3( -1, -1, -1 );
				points[ 1 ] = new Vec3( 1, -1, -1 );
				points[ 2 ] = new Vec3( 1, 1, -1 );
				points[ 3 ] = new Vec3( -1, 1, -1 );
				clipPlanes[ 4 ] = new Plane( direction, -( light.Position.Z - attenuationRange ) );
			}
			else
			{
				Log.Fatal( "MyRenderingLowLevelMethods: GetClipPlanesForPointLightShadowGeneration: Internal error." );
			}

			for( int n = 0; n < 4; n++ )
			{
				clipPlanes[ n ] = Plane.FromPoints( light.Position,
					light.Position + points[ n ], light.Position + points[ ( n + 1 ) % 4 ] );
			}

			return clipPlanes;
		}

		static void WalkPointLightShadowGeneration( Camera mainCamera, RenderLight light,
			Vec3 pointLightFaceDirection, List<SceneNode> outSceneNodes,
			List<StaticMeshObject> outStaticMeshObjects )
		{
			Sphere lightSphere = new Sphere( light.Position, light.AttenuationFar );

			if( !pointLightFaceDirection.Equals( Vec3.Zero, .001f ) )
			{
				//shadowmap. 6 render targets.

				int[] sceneGraphIndexes;
				if( SceneManager.Instance.OverrideVisibleObjects != null )
				{
					sceneGraphIndexes = GetOverrideVisibleObjectsSceneGraphIndexes();
				}
				else
				{
					Plane[] clipPlanes;
					{
						//add point light clip planes
						Plane[] array1 = GetClipPlanesForPointLightShadowGeneration( mainCamera, light, pointLightFaceDirection );
						//add main frustum clip planes and light position
						Plane[] array2 = GetClipPlanesOfConvexHullAroundMainCameraAndLightPosition( mainCamera, light );

						clipPlanes = new Plane[ array1.Length + array2.Length ];
						Array.Copy( array1, 0, clipPlanes, 0, array1.Length );
						Array.Copy( array2, 0, clipPlanes, array1.Length, array2.Length );
					}
					Bounds clipBounds = lightSphere.ToBounds();
					sceneGraphIndexes = SceneManager.Instance.SceneGraph.GetObjects( clipPlanes, clipBounds, 0xFFFFFFFF );
				}

				foreach( int sceneGraphIndex in sceneGraphIndexes )
				{
					SceneManager.SceneGraphObjectData data = SceneManager.Instance.SceneGraphObjects[ sceneGraphIndex ];

					//SceneNode
					SceneNode sceneNode = data.SceneNode;
					if( sceneNode != null && sceneNode.Visible && sceneNode.IsContainsObjectWithCastsShadowsEnabled() )
					{
						Bounds sceneNodeBounds = sceneNode.GetWorldBounds();
						//clip by sphere
						if( lightSphere.IsIntersectsBounds( sceneNodeBounds ) )
						{
							//clip volumes
							if( !IsTotalClipVolumesContainsBounds( sceneNodeBounds ) )
								outSceneNodes.Add( sceneNode );
						}
					}

					//StaticMeshObject
					StaticMeshObject staticMeshObject = data.StaticMeshObject;
					if( staticMeshObject != null && staticMeshObject.Visible && staticMeshObject.CastShadows )
					{
						//clip by sphere
						if( lightSphere.IsIntersectsBounds( staticMeshObject.Bounds ) )
						{
							//clip volumes
							if( !IsTotalClipVolumesContainsBounds( staticMeshObject.Bounds ) )
								outStaticMeshObjects.Add( staticMeshObject );
						}
					}
				}
			}
			else
			{
				//stencil shadows.
				//check by sphere volume.

				int[] sceneGraphIndexes;
				if( SceneManager.Instance.OverrideVisibleObjects != null )
				{
					sceneGraphIndexes = GetOverrideVisibleObjectsSceneGraphIndexes();
				}
				else
				{
					Plane[] clipPlanes;
					{
						//add main frustum clip planes and light position
						Plane[] array1 = GetClipPlanesOfConvexHullAroundMainCameraAndLightPosition( mainCamera, light );

						clipPlanes = new Plane[ 6 + array1.Length ];

						//add 6 light clip planes.
						clipPlanes[ 0 ] = new Plane( Vec3.XAxis, lightSphere.Origin.X + lightSphere.Radius );
						clipPlanes[ 1 ] = new Plane( -Vec3.XAxis, -( lightSphere.Origin.X - lightSphere.Radius ) );
						clipPlanes[ 2 ] = new Plane( Vec3.YAxis, lightSphere.Origin.Y + lightSphere.Radius );
						clipPlanes[ 3 ] = new Plane( -Vec3.YAxis, -( lightSphere.Origin.Y - lightSphere.Radius ) );
						clipPlanes[ 4 ] = new Plane( Vec3.ZAxis, lightSphere.Origin.Z + lightSphere.Radius );
						clipPlanes[ 5 ] = new Plane( -Vec3.ZAxis, -( lightSphere.Origin.Z - lightSphere.Radius ) );

						Array.Copy( array1, 0, clipPlanes, 6, array1.Length );
					}
					Bounds clipBounds = lightSphere.ToBounds();
					sceneGraphIndexes = SceneManager.Instance.SceneGraph.GetObjects( clipPlanes, clipBounds, 0xFFFFFFFF );
				}

				foreach( int sceneGraphIndex in sceneGraphIndexes )
				{
					SceneManager.SceneGraphObjectData data = SceneManager.Instance.SceneGraphObjects[ sceneGraphIndex ];

					//SceneNode
					SceneNode sceneNode = data.SceneNode;
					if( sceneNode != null && sceneNode.Visible && sceneNode.IsContainsObjectWithCastsShadowsEnabled() )
					{
						Bounds sceneNodeBounds = sceneNode.GetWorldBounds();
						//clip by sphere
						if( lightSphere.IsIntersectsBounds( sceneNodeBounds ) )
						{
							//clip volumes
							if( !IsTotalClipVolumesContainsBounds( sceneNodeBounds ) )
								outSceneNodes.Add( sceneNode );
						}
					}

					//StaticMeshObject
					StaticMeshObject staticMeshObject = data.StaticMeshObject;
					if( staticMeshObject != null && staticMeshObject.Visible && staticMeshObject.CastShadows )
					{
						//clip by sphere
						if( lightSphere.IsIntersectsBounds( staticMeshObject.Bounds ) )
						{
							//clip volumes
							if( !IsTotalClipVolumesContainsBounds( staticMeshObject.Bounds ) )
								outStaticMeshObjects.Add( staticMeshObject );
						}
					}
				}
			}
		}

		static Plane[] GetClipPlanesForDirectionalLightShadowGeneration( Camera mainCamera,
			RenderLight light, int pssmTextureIndex )
		{
			Frustum mainFrustum = FrustumUtils.GetFrustumByCamera( mainCamera );
			if( EngineDebugSettings.FrustumTest && mainCamera.AllowFrustumTestMode )
			{
				mainFrustum.HalfWidth *= .5f;
				mainFrustum.HalfHeight *= .5f;
			}

			if( SceneManager.Instance.IsShadowTechniquePSSM() )
			{
				float[] splitDistances = SceneManager.Instance.ShadowDirectionalLightSplitDistances;

				float nearSplitDistance = splitDistances[ pssmTextureIndex ];
				float farSplitDistance = splitDistances[ pssmTextureIndex + 1 ];

				const float splitPadding = 1;

				float splitCount = splitDistances.Length - 1;
				if( pssmTextureIndex > 0 )
					nearSplitDistance -= splitPadding;
				if( pssmTextureIndex < splitCount - 1 )
					farSplitDistance += splitPadding;

				if( nearSplitDistance < mainCamera.NearClipDistance )
					nearSplitDistance = mainCamera.NearClipDistance;
				if( farSplitDistance <= nearSplitDistance + .001f )
					farSplitDistance = nearSplitDistance + .001f;

				mainFrustum.NearDistance = nearSplitDistance;
				mainFrustum.MoveFarDistance( farSplitDistance );
			}
			else
			{
				if( SceneManager.Instance.ShadowFarDistance > mainFrustum.NearDistance )
					mainFrustum.MoveFarDistance( SceneManager.Instance.ShadowFarDistance );
			}

			List<Plane> clipPlanes = new List<Plane>( 64 );

			{
				Quat lightRotation = Quat.FromDirectionZAxisUp( light.Direction );

				Vec3[] frustumPoints = null;
				mainFrustum.ToPoints( ref frustumPoints );

				Vec3 frustumCenterPoint;
				{
					frustumCenterPoint = Vec3.Zero;
					foreach( Vec3 point in frustumPoints )
						frustumCenterPoint += point;
					frustumCenterPoint /= frustumPoints.Length;
				}

				//calculate frustum points projected to 2d from light direction.
				Vec2[] projectedFrustumPoints = new Vec2[ frustumPoints.Length ];
				{
					//Quat invertFrustumAxis = lightCameraFrustum.Rotation.GetInverse();
					Quat lightRotationInv = lightRotation.GetInverse();
					Vec3 translate = frustumCenterPoint - light.Direction * 1000;

					for( int n = 0; n < frustumPoints.Length; n++ )
					{
						Vec3 point = frustumPoints[ n ] - translate;
						Vec3 localPoint = lightRotationInv * point;
						projectedFrustumPoints[ n ] = new Vec2( localPoint.Y, localPoint.Z );
					}
				}

				int[] edges = ConvexPolygon.GetFromPoints( projectedFrustumPoints, .001f );

				for( int n = 0; n < edges.Length; n++ )
				{
					Vec3 point1 = frustumPoints[ edges[ n ] ];
					Vec3 point2 = frustumPoints[ edges[ ( n + 1 ) % edges.Length ] ];

					Plane plane = Plane.FromVectors( light.Direction,
						( point2 - point1 ).GetNormalize(), point1 );

					clipPlanes.Add( plane );
				}
			}

			//add main frustum clip planes
			{
				foreach( Plane plane in mainFrustum.Planes )
				{
					if( Vec3.Dot( plane.Normal, light.Direction ) < 0 )
						continue;

					clipPlanes.Add( plane );
				}
			}

			//add directionalLightExtrusionDistance clip plane
			{
				Quat rot = Quat.FromDirectionZAxisUp( light.Direction );
				Vec3 p = mainCamera.Position - light.Direction * directionalLightExtrusionDistance;
				Plane plane = Plane.FromVectors( rot * Vec3.ZAxis, rot * Vec3.YAxis, p );
				clipPlanes.Add( plane );
			}

			return clipPlanes.ToArray();
		}

		static void WalkDirectionalLightShadowGeneration( Camera mainCamera, RenderLight light,
			int pssmTextureIndex, List<SceneNode> outSceneNodes, List<StaticMeshObject> outStaticMeshObjects )
		{
			int[] sceneGraphIndexes;
			if( SceneManager.Instance.OverrideVisibleObjects != null )
			{
				sceneGraphIndexes = GetOverrideVisibleObjectsSceneGraphIndexes();
			}
			else
			{
				Plane[] clipPlanes = GetClipPlanesForDirectionalLightShadowGeneration( mainCamera, light, pssmTextureIndex );
				sceneGraphIndexes = SceneManager.Instance.SceneGraph.GetObjects( clipPlanes, 0xFFFFFFFF );
			}

			foreach( int sceneGraphIndex in sceneGraphIndexes )
			{
				SceneManager.SceneGraphObjectData data = SceneManager.Instance.SceneGraphObjects[ sceneGraphIndex ];

				//SceneNode
				SceneNode sceneNode = data.SceneNode;
				if( sceneNode != null && sceneNode.Visible && sceneNode.IsContainsObjectWithCastsShadowsEnabled() )
				{
					//clip volumes
					if( !IsTotalClipVolumesContainsBounds( sceneNode.GetWorldBounds() ) )
						outSceneNodes.Add( sceneNode );
				}

				//StaticMeshObject
				StaticMeshObject staticMeshObject = data.StaticMeshObject;
				if( staticMeshObject != null && staticMeshObject.Visible && staticMeshObject.CastShadows )
				{
					//clip volumes
					if( !IsTotalClipVolumesContainsBounds( staticMeshObject.Bounds ) )
						outStaticMeshObjects.Add( staticMeshObject );
				}
			}
		}

		//you can use this method for debugging purposes.
		static void WalkAll( List<SceneNode> outSceneNodes, List<StaticMeshObject> outStaticMeshObjects )
		{
			int[] sceneGraphIndexes;
			if( SceneManager.Instance.OverrideVisibleObjects != null )
			{
				sceneGraphIndexes = GetOverrideVisibleObjectsSceneGraphIndexes();
			}
			else
			{
				List<int> list = new List<int>( SceneManager.Instance.SceneGraphObjects.Count );
				foreach( SceneManager.SceneGraphObjectData data in SceneManager.Instance.SceneGraphObjects )
				{
					if( data.SceneNode != null )
						list.Add( data.SceneNode.SceneGraphIndex );
					else if( data.StaticMeshObject != null )
						list.Add( data.StaticMeshObject.SceneGraphIndex );
				}
				sceneGraphIndexes = list.ToArray();
			}

			foreach( int sceneGraphIndex in sceneGraphIndexes )
			{
				SceneManager.SceneGraphObjectData data = SceneManager.Instance.SceneGraphObjects[ sceneGraphIndex ];

				//SceneNode
				SceneNode sceneNode = data.SceneNode;
				if( sceneNode != null && sceneNode.Visible )
					outSceneNodes.Add( sceneNode );

				//StaticMeshObject
				StaticMeshObject staticMeshObject = data.StaticMeshObject;
				if( staticMeshObject != null && staticMeshObject.Visible )
					outStaticMeshObjects.Add( staticMeshObject );
			}
		}

		public override void OnSceneManagementGetShadowCastersForLight( Camera mainCamera,
			RenderLight light, int directionalLightPSSMTextureIndex, Vec3 pointLightFaceDirection,
			List<SceneNode> outSceneNodes, List<StaticMeshObject> outStaticMeshObjects )
		{
			switch( light.Type )
			{
			case RenderLightType.Spot:
				WalkSpotLightShadowGeneration( mainCamera, light, outSceneNodes, outStaticMeshObjects );
				break;

			case RenderLightType.Point:
				WalkPointLightShadowGeneration( mainCamera, light, pointLightFaceDirection,
					outSceneNodes, outStaticMeshObjects );
				break;

			case RenderLightType.Directional:
				WalkDirectionalLightShadowGeneration( mainCamera, light,
					directionalLightPSSMTextureIndex, outSceneNodes, outStaticMeshObjects );
				break;
			}
		}

		public override bool OnSceneManagementIsAllowPortalSystem( Camera camera )
		{
			return true;
		}

		public override void OnSceneManagementGetObjectsForCamera( Camera camera,
			List<SceneNode> outSceneNodes, List<StaticMeshObject> outStaticMeshObjects )
		{
			Frustum frustum = FrustumUtils.GetFrustumByCamera( camera );
			if( EngineDebugSettings.FrustumTest && camera.AllowFrustumTestMode )
			{
				frustum.HalfWidth *= .5f;
				frustum.HalfHeight *= .5f;
			}

			int[] sceneGraphIndexes;
			if( SceneManager.Instance.OverrideVisibleObjects != null )
				sceneGraphIndexes = GetOverrideVisibleObjectsSceneGraphIndexes();
			else
				sceneGraphIndexes = SceneManager.Instance.SceneGraph.GetObjects( frustum, 0xFFFFFFFF );

			foreach( int sceneGraphIndex in sceneGraphIndexes )
			{
				SceneManager.SceneGraphObjectData data = SceneManager.Instance.SceneGraphObjects[ sceneGraphIndex ];

				//SceneNode
				SceneNode sceneNode = data.SceneNode;
				if( sceneNode != null && sceneNode.Visible )
				{
					//clip volumes
					if( !IsTotalClipVolumesContainsBounds( sceneNode.GetWorldBounds() ) )
						outSceneNodes.Add( sceneNode );
				}

				//StaticMeshObject
				StaticMeshObject staticMeshObject = data.StaticMeshObject;
				if( staticMeshObject != null && staticMeshObject.Visible )
				{
					//clip volumes
					if( !IsTotalClipVolumesContainsBounds( staticMeshObject.Bounds ) )
						outStaticMeshObjects.Add( staticMeshObject );
				}
			}
		}

		static bool IsIntersects( RenderLight light, Bounds bounds )
		{
			if( light.Type == RenderLightType.Point || light.Type == RenderLightType.Spot )
			{
				//check by bounding sphere
				{
					Sphere lightSphere = new Sphere( light.Position, light.AttenuationFar );
					if( !lightSphere.IsIntersectsBounds( ref bounds ) )
						return false;
				}

				//check by spot light clip planes
				if( light.Type == RenderLightType.Spot )
				{
					Vec3 boundsCenter = bounds.GetCenter();
					Vec3 boundsHalfSize = boundsCenter - bounds.Minimum;

					foreach( Plane plane in light.SpotLightClipPlanes )
					{
						if( plane.GetSide( ref boundsCenter, ref boundsHalfSize ) == Plane.Side.Positive )
							return false;
					}
				}
			}

			return true;
		}

		//static bool IsIntersects( RenderLight light, Sphere boundingSphere )
		//{
		//   if( light.Type == RenderLightType.Point || light.Type == RenderLightType.Spot )
		//   {
		//      //check by bounding sphere
		//      {
		//         float range = light.AttenuationFar;
		//         Sphere lightSphere = new Sphere( light.Position, range );

		//         if( !lightSphere.IsIntersectsSphere( ref boundingSphere ) )
		//            return false;
		//      }

		//      //check by spot light clip planes
		//      if( light.Type == RenderLightType.Spot )
		//      {
		//         foreach( Plane plane in light.SpotLightClipPlanes )
		//         {
		//            if( plane.GetDistance( boundingSphere.Origin ) > boundingSphere.Radius )
		//               return false;
		//         }
		//      }
		//   }

		//   return true;
		//}

		public override void OnSceneManagementGetAffectingLightsForObject( Camera camera,
			RenderLight[] affectingLights, StaticMeshObject staticMeshObject )
		{
			if( tempLightArray.Length != affectingLights.Length )
				tempLightArray = new RenderLight[ affectingLights.Length ];

			int count = 0;

			foreach( RenderLight light in affectingLights )
			{
				if( light.Type == RenderLightType.Point || light.Type == RenderLightType.Spot )
				{
					if( !IsIntersects( light, staticMeshObject.Bounds ) )
						continue;
				}

				tempLightArray[ count ] = light;
				count++;
			}

			staticMeshObject.SetAffectingLights( tempLightArray, count );
		}

		public override void OnSceneManagementGetAffectingLightsForObject( Camera camera,
			RenderLight[] affectingLights, SceneNode sceneNode )
		{
			if( tempLightArray.Length != affectingLights.Length )
				tempLightArray = new RenderLight[ affectingLights.Length ];

			int count = 0;

			foreach( RenderLight light in affectingLights )
			{
				//Sphere boundingSphere = new Sphere( sceneNode.Position, 0 );
				//{
				//   for( int n = 0; n < sceneNode.MovableObjects.Count; n++ )
				//   {
				//      MovableObject movableObject = sceneNode.MovableObjects[ n ];

				//      float radius = movableObject._GetBoundingRadius();
				//      if( radius > boundingSphere.Radius )
				//         boundingSphere.Radius = radius;
				//   }
				//}

				if( light.Type == RenderLightType.Point || light.Type == RenderLightType.Spot )
				{
					Bounds bounds;
					sceneNode.GetWorldBounds( out bounds );
					if( !IsIntersects( light, bounds ) )
						continue;
				}

				tempLightArray[ count ] = light;
				count++;
			}

			sceneNode.SetAffectingLights( tempLightArray, count );
		}

		public static void PushClipVolumes( Box[] volumes )
		{
			clipVolumes.Push( (Box[])volumes.Clone() );
			UpdateTotalClipVolumes();
		}

		public static void PopClipVolumes()
		{
			if( clipVolumes.Count != 0 )
				clipVolumes.Pop();
			UpdateTotalClipVolumes();
		}

		static void UpdateTotalClipVolumes()
		{
			List<Box> list = new List<Box>();
			foreach( Box[] array in clipVolumes )
			{
				foreach( Box volume in array )
					list.Add( volume );
			}
			totalClipVolumes = list.ToArray();
		}

		static bool IsTotalClipVolumesContainsBounds( Bounds bounds )
		{
			foreach( Box volume in totalClipVolumes )
			{
				if( volume.IsContainsBounds( bounds ) )
					return true;
			}
			return false;
		}

		static int[] GetOverrideVisibleObjectsSceneGraphIndexes()
		{
			SceneNode[] sceneNodes = SceneManager.Instance.OverrideVisibleObjects.SceneNodes;
			StaticMeshObject[] staticMeshObjects = SceneManager.Instance.OverrideVisibleObjects.StaticMeshObjects;

			int[] sceneGraphIndexes = new int[ sceneNodes.Length + staticMeshObjects.Length ];
			int index = 0;
			for( int n = 0; n < sceneNodes.Length; n++, index++ )
				sceneGraphIndexes[ index ] = sceneNodes[ n ].SceneGraphIndex;
			for( int n = 0; n < staticMeshObjects.Length; n++, index++ )
				sceneGraphIndexes[ index ] = staticMeshObjects[ n ].SceneGraphIndex;
			return sceneGraphIndexes;
		}
	}
}
