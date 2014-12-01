// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.Utils;
using ProjectCommon;

namespace ProjectCommon
{
	/// <summary>
	/// Class for execute actions after initialization of the engine.
	/// </summary>
	/// <remarks>
	/// It is class works in simulation application and editors (Resource Editor, Map Editor).
	/// </remarks>
	public class GameEngineInitialization : EngineInitialization
	{
		public override RenderingLowLevelMethods CreateRenderingLowLevelMethodsImpl()
		{
			return new RenderingLowLevelMethodsImpl();
		}

		public override DynamicMeshManager CreateDynamicMeshManagerImpl( float maxLifeTimeNotUsedDataInCache )
		{
			return new DynamicMeshManagerImpl( maxLifeTimeNotUsedDataInCache );
		}

		protected override bool OnPostInitialize()
		{
			ConfigureRenderTechnique();

			//configure MRT rendering
			if( RendererWorld.InitializationOptions.AllowSceneMRTRendering )
				ConfigureMRTRendering();

			//Initialize HDR compositor for HDR render technique
			if( EngineApp.RenderTechnique == "HDR" )
				InitializeHDRCompositor();

			//Initialize Fast Approximate Antialiasing (FXAA)
			string fsaa = RendererWorld.InitializationOptions.FullSceneAntialiasing;
			if( ( ( fsaa == "" || fsaa == "RecommendedSetting" ) && IsActivateFXAAByDefault() ) || fsaa == "FXAA" )
			{
				if( RenderSystem.Instance.HasShaderModel3() )
					InitializeFXAACompositor();
			}

			return true;
		}

		bool IsHDRSupported()
		{
			Compositor compositor = CompositorManager.Instance.GetByName( "HDR" );
			if( compositor == null || !compositor.IsSupported() )
				return false;

			bool floatTexturesSupported = TextureManager.Instance.IsEquivalentFormatSupported(
				Texture.Type.Type2D, PixelFormat.Float16RGB, Texture.Usage.RenderTarget );
			if( !floatTexturesSupported )
				return false;

			if( RenderSystem.Instance.GPUIsGeForce() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV30 )
				return false;
			if( RenderSystem.Instance.GPUIsRadeon() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
				return false;

			return true;
		}

		bool IsActivateHDRByDefault()
		{
			if( IsHDRSupported() )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_G80 ||
						RenderSystem.Instance.GPUCodeName == GPUCodeNames.GeForce_Unknown )
					{
						return true;
					}
				}
				if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R600 ||
						RenderSystem.Instance.GPUCodeName == GPUCodeNames.Radeon_Unknown )
					{
						return true;
					}
				}
			}

			return false;
		}

		void ConfigureRenderTechnique()
		{
			//HDR choose by default
			if( ( EngineApp.RenderTechnique == "" || EngineApp.RenderTechnique == "RecommendedSetting" ) )
				EngineApp.RenderTechnique = IsActivateHDRByDefault() ? "HDR" : "Standard";

			//HDR render technique support check
			if( EngineApp.RenderTechnique == "HDR" && !IsHDRSupported() )
			{
				//bool nullRenderSystem = RenderSystem.Instance.Name.ToLower().Contains( "null" );

				//if( !nullRenderSystem )//no warning for null render system
				//{
				//   Log.Warning( "HDR render technique is not supported. " +
				//      "Using \"Standard\" render technique." );
				//}
				EngineApp.RenderTechnique = "Standard";
			}

			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) )
				EngineApp.RenderTechnique = "Standard";
		}

		public static void ConfigureMRTRendering()
		{
			//!!!!At this time OpenGL is not supported.
			if( RendererWorld.InitializationOptions.AllowSceneMRTRendering &&
				RenderSystem.Instance.HasShaderModel3() &&
				RenderSystem.Instance.Capabilities.NumMultiRenderTargets > 1 &&
				RenderSystem.Instance.IsDirect3D() )
			{
				//enable MRT rendering

				PixelFormat[] formats = null;

				if( RenderSystem.Instance.Capabilities.MRTDifferentBitDepths )
				{
					formats = new PixelFormat[] { PixelFormat.Float32R }; // PixelFormat.Float16R
				}
				else
				{
					if( EngineApp.RenderTechnique == "HDR" )
						formats = new PixelFormat[] { PixelFormat.Float16RGB };
					else
						formats = new PixelFormat[] { PixelFormat.Float16GR };
				}

				RenderSystem.Instance.SetAdditionalMRTFormats( formats );
			}
			else
			{
				//disable MRT rendering
				RendererWorld.InitializationOptions.AllowSceneMRTRendering = false;
			}
		}

		void InitializeHDRCompositor()
		{
			//Add HDR compositor
			HDRCompositorInstance instance = (HDRCompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor( "HDR" );

			if( instance != null )
				instance.Enabled = true;
		}

		bool IsActivateFXAAByDefault()
		{
			if( RenderSystem.Instance.HasShaderModel3() )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_G80 ||
						RenderSystem.Instance.GPUCodeName == GPUCodeNames.GeForce_Unknown )
					{
						return true;
					}
				}
				if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R600 ||
						RenderSystem.Instance.GPUCodeName == GPUCodeNames.Radeon_Unknown )
					{
						return true;
					}
				}
				if( RenderSystem.Instance.GPUIsIntel() )
				{
					if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Intel_HDGraphics )
						return true;
				}
			}

			return false;
		}

		void InitializeFXAACompositor()
		{
			FXAACompositorInstance instance = (FXAACompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor( "FXAA" );

			if( instance != null )
				instance.Enabled = true;
		}
	}
}
