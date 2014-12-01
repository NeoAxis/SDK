// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace ProjectCommon
{
	[CompositorName( "HDR" )]
	public class HDRCompositorParameters : CompositorParameters
	{
		bool adaptation;
		float adaptationVelocity = 1;
		float adaptationMiddleBrightness = .5f;
		float adaptationMinimum = .2f;
		float adaptationMaximum = 2;
		float bloomBrightThreshold = 1.2f;
		float bloomScale = 1.25f;

		[DefaultValue( false )]
		public bool Adaptation
		{
			get { return adaptation; }
			set { adaptation = value; }
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 10 )]
		public float AdaptationVelocity
		{
			get { return adaptationVelocity; }
			set
			{
				if( value < 0 )
					value = 0;
				adaptationVelocity = value;
			}
		}

		[DefaultValue( .5f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 2 )]
		public float AdaptationMiddleBrightness
		{
			get { return adaptationMiddleBrightness; }
			set { adaptationMiddleBrightness = value; }
		}

		[DefaultValue( .2f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 2 )]
		public float AdaptationMinimum
		{
			get { return adaptationMinimum; }
			set { adaptationMinimum = value; }
		}

		[DefaultValue( 2.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 2 )]
		public float AdaptationMaximum
		{
			get { return adaptationMaximum; }
			set { adaptationMaximum = value; }
		}

		[DefaultValue( 1.2f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 2 )]
		public float BloomBrightThreshold
		{
			get { return bloomBrightThreshold; }
			set
			{
				if( value < 0 )
					value = 0;
				bloomBrightThreshold = value;
			}
		}

		[DefaultValue( 1.25f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 5 )]
		public float BloomScale
		{
			get { return bloomScale; }
			set
			{
				if( value < 0 )
					value = 0;
				bloomScale = value;
			}
		}
	}

	/// <summary>
	/// HDR scene post processing compositor instance.
	/// </summary>
	[CompositorName( "HDR" )]
	public class HDRCompositorInstance : CompositorInstance
	{
		bool adaptation;
		float adaptationVelocity = 1;
		float adaptationMiddleBrightness = .5f;
		float adaptationMinimum = .2f;
		float adaptationMaximum = 2;
		float bloomBrightThreshold = 1.2f;
		float bloomScale = 1.25f;

		float lastLuminanceCalculationTime = -1;
		Vec2I brightPassTextureSize;
		Vec2I bloomTextureSize;

		//

		protected override void OnCreate()
		{
			base.OnCreate();
			RenderSystem.Instance.RenderSystemEvent += RenderSystemEvent;
		}

		protected override void OnDestroy()
		{
			RenderSystem.Instance.RenderSystemEvent -= RenderSystemEvent;
			base.OnDestroy();
		}

		public bool Adaptation
		{
			get { return adaptation; }
			set { adaptation = value; }
		}

		[EditorLimitsRange( .1f, 10 )]
		public float AdaptationVelocity
		{
			get { return adaptationVelocity; }
			set
			{
				if( value < 0 )
					value = 0;
				adaptationVelocity = value;
			}
		}

		[EditorLimitsRange( .1f, 2 )]
		public float AdaptationMiddleBrightness
		{
			get { return adaptationMiddleBrightness; }
			set { adaptationMiddleBrightness = value; }
		}

		[EditorLimitsRange( .1f, 2 )]
		public float AdaptationMinimum
		{
			get { return adaptationMinimum; }
			set { adaptationMinimum = value; }
		}

		[EditorLimitsRange( .1f, 2 )]
		public float AdaptationMaximum
		{
			get { return adaptationMaximum; }
			set { adaptationMaximum = value; }
		}

		[EditorLimitsRange( .1f, 2 )]
		public float BloomBrightThreshold
		{
			get { return bloomBrightThreshold; }
			set
			{
				if( value < 0 )
					value = 0;
				bloomBrightThreshold = value;
			}
		}

		[EditorLimitsRange( 0, 5 )]
		public float BloomScale
		{
			get { return bloomScale; }
			set
			{
				if( value < 0 )
					value = 0;
				bloomScale = value;
			}
		}

		protected override void OnCreateTexture( string definitionName, ref Vec2I size, ref PixelFormat format )
		{
			base.OnCreateTexture( definitionName, ref size, ref format );

			if( definitionName == "rt_brightPass" )
			{
				size = Owner.DimensionsInPixels.Size / 2;
				brightPassTextureSize = size;
			}
			else if( definitionName == "rt_bloomBlur" ||
				definitionName == "rt_bloomHorizontal" || definitionName == "rt_bloomVertical" )
			{
				size = Owner.DimensionsInPixels.Size / 4;
				bloomTextureSize = size;
			}

			lastLuminanceCalculationTime = -1;
		}

		static void CalculateDownScale4x4SampleOffsets( Vec2I sourceTextureSize, Vec2[] sampleOffsets )
		{
			// Sample from the 16 surrounding points. Since the center point will be in
			// the exact center of 16 texels, a 0.5f offset is needed to specify a texel
			// center.
			Vec2 invSize = 1.0f / sourceTextureSize.ToVec2();
			int index = 0;
			for( int y = 0; y < 4; y++ )
			{
				for( int x = 0; x < 4; x++ )
				{
					sampleOffsets[ index ] = new Vec2( ( (float)x - 1.5f ), ( (float)y - 1.5f ) ) * invSize;
					index++;
				}
			}
		}

		static float GaussianDistribution( float x, float y, float rho )
		{
			float g = 1.0f / MathFunctions.Sqrt( 2.0f * MathFunctions.PI * rho * rho );
			g *= MathFunctions.Exp( -( x * x + y * y ) / ( 2.0f * rho * rho ) );
			return g;
		}

		static void CalculateGaussianBlur5x5SampleOffsets( Vec2I textureSize,
			Vec2[] sampleOffsets, Vec4[] sampleWeights, float multiplier )
		{
			float tu = 1.0f / (float)textureSize.X;
			float tv = 1.0f / (float)textureSize.Y;

			Vec4 white = new Vec4( 1, 1, 1, 1 );

			float totalWeight = 0.0f;
			int index = 0;
			for( int x = -2; x <= 2; x++ )
			{
				for( int y = -2; y <= 2; y++ )
				{
					// Exclude pixels with a block distance greater than 2. This will
					// create a kernel which approximates a 5x5 kernel using only 13
					// sample points instead of 25; this is necessary since 2.0 shaders
					// only support 16 texture grabs.
					if( Math.Abs( x ) + Math.Abs( y ) > 2 )
						continue;

					// Get the unscaled Gaussian intensity for this offset
					sampleOffsets[ index ] = new Vec2( x * tu, y * tv );
					sampleWeights[ index ] = white * GaussianDistribution( (float)x, (float)y, 1 );
					totalWeight += sampleWeights[ index ].X;

					index++;
				}
			}

			// Divide the current weight by the total weight of all the samples; Gaussian
			// blur kernels add to 1.0f to ensure that the intensity of the image isn't
			// changed when the blur occurs. An optional multiplier variable is used to
			// add or remove image intensity during the blur.
			for( int i = 0; i < index; i++ )
			{
				sampleWeights[ i ] /= totalWeight;
				sampleWeights[ i ] *= multiplier;
			}
		}

		static void CalculateBloomSampleOffsets( int textureSize, float[] sampleOffsets,
			Vec4[] sampleWeights, float deviation, float multiplier )
		{
			int n;
			float weight;
			float tu = 1.0f / (float)textureSize;

			// Fill the center texel
			weight = multiplier * GaussianDistribution( 0, 0, deviation );
			sampleOffsets[ 0 ] = 0.0f;
			sampleWeights[ 0 ] = new Vec4( weight, weight, weight, 1.0f );

			// Fill the first half
			for( n = 1; n < 8; n++ )
			{
				// Get the Gaussian intensity for this offset
				weight = multiplier * GaussianDistribution( n, 0, deviation );
				sampleOffsets[ n ] = n * tu;
				sampleWeights[ n ] = new Vec4( weight, weight, weight, 1.0f );
			}

			// Mirror to the second half
			for( n = 8; n < 15; n++ )
			{
				sampleOffsets[ n ] = -sampleOffsets[ n - 7 ];
				sampleWeights[ n ] = sampleWeights[ n - 7 ];
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			//update material scheme
			{
				string materialScheme = RendererWorld.Instance.DefaultViewport.MaterialScheme;

				foreach( CompositionTechnique technique in Compositor.Techniques )
				{
					foreach( CompositionTargetPass pass in technique.TargetPasses )
						pass.MaterialScheme = materialScheme;
					if( technique.OutputTargetPass != null )
						technique.OutputTargetPass.MaterialScheme = materialScheme;
				}
			}

			const int rt_luminance0 = 994;
			const int rt_luminance1 = 993;
			const int rt_luminance2 = 992;
			const int rt_luminance3 = 991;
			const int rt_luminance4 = 990;
			const int rt_brightPass = 800;
			const int rt_bloomBlur = 700;
			const int rt_bloomHorizontal = 701;
			const int rt_bloomVertical = 702;
			const int rt_adaptedLuminance = 500;
			const int rt_targetOutput = 600;

			//Skip adaptation passes if adaptation switched off.
			if( !Adaptation )
			{
				if( passId == rt_luminance0 || passId == rt_luminance1 || passId == rt_luminance2
					|| passId == rt_luminance3 || passId == rt_luminance4 )
				{
					skipPass = true;
					return;
				}
			}

			//Skip bloom passes if bloom switched off
			if( BloomScale == 0 )
			{
				if( passId == rt_brightPass || passId == rt_bloomBlur || passId == rt_bloomHorizontal ||
					passId == rt_bloomVertical )
				{
					skipPass = true;
					return;
				}
			}

			// Prepare the fragment params offsets
			switch( passId )
			{
			case rt_luminance0:
				{
					Vec2[] sampleOffsets = new Vec2[ 9 ];

					// Initialize the sample offsets for the initial luminance pass.
					int textureSize = Technique.GetTextureDefinition( "rt_luminance0" ).Size.X;

					float tu = 1.0f / ( 3.0f * textureSize );

					int index = 0;
					for( int x = -1; x <= 1; x++ )
					{
						for( int y = -1; y <= 1; y++ )
						{
							sampleOffsets[ index ] = new Vec2( x, y ) * tu;
							index++;
						}
					}

					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;

					//convert to Vec4 array
					Vec4[] vec4Offsets = new Vec4[ 9 ];
					for( int n = 0; n < 9; n++ )
					{
						Vec2 offset = sampleOffsets[ n ];
						vec4Offsets[ n ] = new Vec4( offset[ 0 ], offset[ 1 ], 0, 0 );
					}

					parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
				}
				break;

			case rt_luminance1:
			case rt_luminance2:
			case rt_luminance3:
			case rt_luminance4:
				{
					Vec2[] sampleOffsets = new Vec2[ 16 ];

					string textureSizeFrom = null;
					switch( passId )
					{
					case rt_luminance1: textureSizeFrom = "rt_luminance0"; break;
					case rt_luminance2: textureSizeFrom = "rt_luminance1"; break;
					case rt_luminance3: textureSizeFrom = "rt_luminance2"; break;
					case rt_luminance4: textureSizeFrom = "rt_luminance3"; break;
					default: Trace.Assert( false ); break;
					}
					Vec2I textureSize = Technique.GetTextureDefinition( textureSizeFrom ).Size;
					CalculateDownScale4x4SampleOffsets( textureSize, sampleOffsets );

					//convert to Vec4 array
					Vec4[] vec4Offsets = new Vec4[ 16 ];
					for( int n = 0; n < 16; n++ )
					{
						Vec2 offset = sampleOffsets[ n ];
						vec4Offsets[ n ] = new Vec4( offset[ 0 ], offset[ 1 ], 0, 0 );
					}

					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
				}
				break;

			//BrightPass
			case rt_brightPass:
				{
					Vec2[] sampleOffsets = new Vec2[ 16 ];

					Vec2I textureSize = Owner.DimensionsInPixels.Size;
					CalculateDownScale4x4SampleOffsets( textureSize, sampleOffsets );

					//convert to Vec4 array
					Vec4[] vec4Offsets = new Vec4[ 16 ];
					for( int n = 0; n < 16; n++ )
					{
						Vec2 offset = sampleOffsets[ n ];
						vec4Offsets[ n ] = new Vec4( offset[ 0 ], offset[ 1 ], 0, 0 );
					}

					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "brightThreshold", BloomBrightThreshold );
					parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
				}
				break;

			case rt_bloomBlur:
				{
					Vec2[] sampleOffsets = new Vec2[ 13 ];
					Vec4[] sampleWeights = new Vec4[ 13 ];

					Vec2I textureSize = brightPassTextureSize;
					CalculateGaussianBlur5x5SampleOffsets( textureSize, sampleOffsets, sampleWeights, 1 );

					//convert to Vec4 array
					Vec4[] vec4Offsets = new Vec4[ 13 ];
					for( int n = 0; n < 13; n++ )
					{
						Vec2 offset = sampleOffsets[ n ];
						vec4Offsets[ n ] = new Vec4( offset.X, offset.Y, 0, 0 );
					}

					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
					parameters.SetNamedConstant( "sampleWeights", sampleWeights );
				}
				break;

			case rt_bloomHorizontal:
			case rt_bloomVertical:
				{
					// horizontal and vertical bloom
					bool horizontal = passId == rt_bloomHorizontal;

					float[] sampleOffsets = new float[ 15 ];
					Vec4[] sampleWeights = new Vec4[ 15 ];

					Vec2I textureSize = bloomTextureSize;
					CalculateBloomSampleOffsets( horizontal ? textureSize.X : textureSize.Y,
						sampleOffsets, sampleWeights, 3, 2 );

					//convert to Vec4 array
					Vec4[] vec4Offsets = new Vec4[ 15 ];
					for( int n = 0; n < 15; n++ )
					{
						float offset = sampleOffsets[ n ];

						if( horizontal )
							vec4Offsets[ n ] = new Vec4( offset, 0, 0, 0 );
						else
							vec4Offsets[ n ] = new Vec4( 0, offset, 0, 0 );
					}

					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
					parameters.SetNamedConstant( "sampleWeights", sampleWeights );
				}
				break;

			case rt_adaptedLuminance:
				{
					float elapsedTime;
					{
						float currentTime = RendererWorld.Instance.FrameRenderTime;
						if( lastLuminanceCalculationTime != -1 )
							elapsedTime = currentTime - lastLuminanceCalculationTime;
						else
							elapsedTime = 1000;
						lastLuminanceCalculationTime = currentTime;
					}

					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "adaptationMinimum", Adaptation ? AdaptationMinimum : 1 );
					parameters.SetNamedConstant( "adaptationMaximum", Adaptation ? AdaptationMaximum : 1 );
					parameters.SetNamedConstant( "adaptationVelocity", AdaptationVelocity );
					parameters.SetNamedConstant( "elapsedTime", elapsedTime );
				}
				break;

			//Final pass
			case rt_targetOutput:
				{
					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "adaptationMiddleBrightness",
						Adaptation ? AdaptationMiddleBrightness : 1 );
					parameters.SetNamedConstant( "bloomScale", BloomScale );
				}
				break;
			}
		}

		void RenderSystemEvent( RenderSystemEvents name )
		{
			if( name == RenderSystemEvents.DeviceLost || name == RenderSystemEvents.DeviceRestored )
				lastLuminanceCalculationTime = -1;
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			HDRCompositorParameters p = (HDRCompositorParameters)parameters;
			Adaptation = p.Adaptation;
			AdaptationVelocity = p.AdaptationVelocity;
			AdaptationMiddleBrightness = p.AdaptationMiddleBrightness;
			AdaptationMinimum = p.AdaptationMinimum;
			AdaptationMaximum = p.AdaptationMaximum;
			BloomBrightThreshold = p.BloomBrightThreshold;
			BloomScale = p.BloomScale;
		}

	}
}
