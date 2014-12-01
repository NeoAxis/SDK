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
using Engine.PhysicsSystem;
using ProjectCommon;

namespace ProjectCommon
{
	[CompositorName( "DepthOfField" )]
	public class DepthOfFieldCompositorParameters : CompositorParameters
	{
		float focalDistance = 30;
		bool autoFocus = true;
		float focalSize = 20;
		float blurSpread = .3f;
		float blurTextureResolution = 5;
		float backgroundTransitionLength = 40;
		bool blurForeground = true;
		float foregroundTransitionLength = 40;
		Range autoFocusRange = new Range( 1, 70 );
		float autoFocusTransitionSpeed = 100;

		//

		[DefaultValue( 30.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 300 )]
		public float FocalDistance
		{
			get { return focalDistance; }
			set
			{
				if( value < 0 )
					value = 0;
				focalDistance = value;
			}
		}

		[DefaultValue( true )]
		public bool AutoFocus
		{
			get { return autoFocus; }
			set { autoFocus = value; }
		}

		[DefaultValue( 20.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 100 )]
		public float FocalSize
		{
			get { return focalSize; }
			set
			{
				if( value < 0 )
					value = 0;
				focalSize = value;
			}
		}

		[DefaultValue( .3f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float BlurSpread
		{
			get { return blurSpread; }
			set
			{
				if( value < 0 )
					value = 0;
				blurSpread = value;
			}
		}

		[DefaultValue( 5.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 7 )]
		public float BlurTextureResolution
		{
			get { return blurTextureResolution; }
			set
			{
				if( value < 1 )
					value = 1;
				if( value > 7 )
					value = 7;
				blurTextureResolution = value;
			}
		}

		[DefaultValue( 40.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 100 )]
		public float BackgroundTransitionLength
		{
			get { return backgroundTransitionLength; }
			set
			{
				if( value < 0 )
					value = 0;
				backgroundTransitionLength = value;
			}
		}

		[DefaultValue( true )]
		public bool BlurForeground
		{
			get { return blurForeground; }
			set { blurForeground = value; }
		}

		[DefaultValue( 40.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 100 )]
		public float ForegroundTransitionLength
		{
			get { return foregroundTransitionLength; }
			set
			{
				if( value < 0 )
					value = 0;
				foregroundTransitionLength = value;
			}
		}

		[DefaultValue( typeof( Range ), "1 70" )]
		[Editor( typeof( RangeValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 200 )]
		public Range AutoFocusRange
		{
			get { return autoFocusRange; }
			set { autoFocusRange = value; }
		}

		[DefaultValue( 100.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 500 )]
		public float AutoFocusTransitionSpeed
		{
			get { return autoFocusTransitionSpeed; }
			set
			{
				if( value < 0 )
					value = 0;
				autoFocusTransitionSpeed = value;
			}
		}
	}

	[CompositorName( "DepthOfField" )]
	public class DepthOfFieldCompositorInstance : CompositorInstance
	{
		float focalDistance = 30;
		bool autoFocus = true;
		float focalSize = 20;
		float blurSpread = .3f;
		float blurTextureResolution = 5;
		float backgroundTransitionLength = 40;
		bool blurForeground = true;
		float foregroundTransitionLength = 40;
		Range autoFocusRange = new Range( 1, 70 );
		float autoFocusTransitionSpeed = 100;

		Vec2I downscaleTextureSize;

		//

		[EditorLimitsRange( 1, 300 )]
		public float FocalDistance
		{
			get { return focalDistance; }
			set
			{
				if( value < 0 )
					value = 0;
				focalDistance = value;
			}
		}

		public bool AutoFocus
		{
			get { return autoFocus; }
			set { autoFocus = value; }
		}

		[EditorLimitsRange( 1, 100 )]
		public float FocalSize
		{
			get { return focalSize; }
			set
			{
				if( value < 0 )
					value = 0;
				focalSize = value;
			}
		}

		[EditorLimitsRange( 0, 1 )]
		public float BlurSpread
		{
			get { return blurSpread; }
			set
			{
				if( value < 0 )
					value = 0;
				blurSpread = value;
			}
		}

		[EditorLimitsRange( 1, 7 )]
		public float BlurTextureResolution
		{
			get { return blurTextureResolution; }
			set
			{
				if( value < 1 )
					value = 1;
				if( value > 7 )
					value = 7;
				if( blurTextureResolution == value )
					return;
				blurTextureResolution = value;

				//recreate
				if( Enabled )
				{
					Enabled = false;
					Enabled = true;
				}
			}
		}

		[EditorLimitsRange( 0, 100 )]
		public float BackgroundTransitionLength
		{
			get { return backgroundTransitionLength; }
			set
			{
				if( value < 0 )
					value = 0;
				backgroundTransitionLength = value;
			}
		}

		public bool BlurForeground
		{
			get { return blurForeground; }
			set { blurForeground = value; }
		}

		[EditorLimitsRange( 0, 100 )]
		public float ForegroundTransitionLength
		{
			get { return foregroundTransitionLength; }
			set
			{
				if( value < 0 )
					value = 0;
				foregroundTransitionLength = value;
			}
		}

		[EditorLimitsRange( 1, 200 )]
		public Range AutoFocusRange
		{
			get { return autoFocusRange; }
			set { autoFocusRange = value; }
		}

		[EditorLimitsRange( 1, 500 )]
		public float AutoFocusTransitionSpeed
		{
			get { return autoFocusTransitionSpeed; }
			set
			{
				if( value < 0 )
					value = 0;
				autoFocusTransitionSpeed = value;
			}
		}

		protected override void OnCreateTexture( string definitionName, ref Vec2I size, ref PixelFormat format )
		{
			base.OnCreateTexture( definitionName, ref size, ref format );

			if( definitionName == "rt_downscale" || definitionName == "rt_blurHorizontal" ||
				definitionName == "rt_blurVertical" )
			{
				float divisor = 8.0f - blurTextureResolution;
				if( divisor <= 1 )
					divisor = 1;
				Vec2 sizeFloat = Owner.DimensionsInPixels.Size.ToVec2() / divisor;
				size = new Vec2I( (int)sizeFloat.X, (int)sizeFloat.Y );
				if( size.X < 1 )
					size.X = 1;
				if( size.Y < 1 )
					size.Y = 1;

				downscaleTextureSize = size;
			}
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

		static void CalculateBlurSampleOffsets( int textureSize, float[] sampleOffsets,
			Vec4[] sampleWeights, float deviation, float multiplier )
		{
			float tu = 1.0f / (float)textureSize;

			// Fill the center texel
			{
				float weight = multiplier * GaussianDistribution( 0, 0, deviation );
				sampleOffsets[ 0 ] = 0.0f;
				sampleWeights[ 0 ] = new Vec4( weight, weight, weight, 1.0f );
			}

			// Fill the first half
			for( int n = 1; n < 8; n++ )
			{
				// Get the Gaussian intensity for this offset
				float weight = multiplier * GaussianDistribution( n, 0, deviation );
				sampleOffsets[ n ] = n * tu;
				sampleWeights[ n ] = new Vec4( weight, weight, weight, 1.0f );
			}

			// Mirror to the second half
			for( int n = 8; n < 15; n++ )
			{
				sampleOffsets[ n ] = -sampleOffsets[ n - 7 ];
				sampleWeights[ n ] = sampleWeights[ n - 7 ];
			}

			//normalize weights (fix epsilon errors)
			{
				Vec4 total = Vec4.Zero;
				for( int n = 0; n < sampleWeights.Length; n++ )
					total += sampleWeights[ n ];
				for( int n = 0; n < sampleWeights.Length; n++ )
					sampleWeights[ n ] = ( sampleWeights[ n ] / total ) * multiplier;
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			const int rt_downscale = 100;
			const int rt_blurHorizontal = 200;
			const int rt_blurVertical = 300;
			const int rt_autoFocus1 = 400;
			const int rt_autoFocus2 = 401;
			const int rt_autoFocus3 = 402;
			const int rt_autoFocusFinal = 403;
			const int rt_autoFocusCurrent = 404;
			//const int rt_blurFactors = 500;
			const int rt_targetOutput = 600;

			//Skip auto focus passes if no auto focus is enabled
			if( !autoFocus )
			{
				if( passId == rt_autoFocus1 || passId == rt_autoFocus2 || passId == rt_autoFocus3 ||
					passId == rt_autoFocusFinal || passId == rt_autoFocusCurrent )
				{
					skipPass = true;
					return;
				}
			}

			// Prepare the fragment params offsets
			switch( passId )
			{
			case rt_downscale:
				{
					Vec2[] sampleOffsets = new Vec2[ 16 ];

					CalculateDownScale4x4SampleOffsets( Owner.DimensionsInPixels.Size, sampleOffsets );

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

			case rt_blurHorizontal:
			case rt_blurVertical:
				{
					// horizontal and vertical blur
					bool horizontal = passId == rt_blurHorizontal;

					float[] sampleOffsets = new float[ 15 ];
					Vec4[] sampleWeights = new Vec4[ 15 ];

					CalculateBlurSampleOffsets( horizontal ? downscaleTextureSize.X : downscaleTextureSize.Y,
						sampleOffsets, sampleWeights, 3, 1 );

					//convert to Vec4 array
					Vec4[] vec4Offsets = new Vec4[ 15 ];
					for( int n = 0; n < 15; n++ )
					{
						float offset = sampleOffsets[ n ] * blurSpread;

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

			case rt_autoFocus1:
				{
					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedAutoConstant( "farClipDistance",
						GpuProgramParameters.AutoConstantType.FarClipDistance );
				}
				break;

			case rt_autoFocus2:
			case rt_autoFocus3:
				{
					Vec2[] sampleOffsets = new Vec2[ 16 ];

					string textureSizeFrom = null;
					switch( passId )
					{
					case rt_autoFocus2: textureSizeFrom = "rt_autoFocus1"; break;
					case rt_autoFocus3: textureSizeFrom = "rt_autoFocus2"; break;
					default: Trace.Assert( false ); break;
					}
					Vec2I sourceTextureSize = Technique.GetTextureDefinition( textureSizeFrom ).Size;
					CalculateDownScale4x4SampleOffsets( sourceTextureSize, sampleOffsets );

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

			case rt_autoFocusFinal:
				{
					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;

					Vec4 properties = Vec4.Zero;
					properties.X = autoFocusRange.Minimum;
					properties.Y = autoFocusRange.Maximum;
					properties.Z = RendererWorld.Instance.FrameRenderTimeStep * autoFocusTransitionSpeed;
					parameters.SetNamedConstant( "properties", properties );
				}
				break;

			case rt_autoFocusCurrent:
				break;

			//case rt_blurFactors:
			//   {
			//      GpuProgramParameters parameters = material.GetBestTechnique().
			//         Passes[ 0 ].FragmentProgramParameters;
			//      parameters.SetNamedAutoConstant( "farClipDistance",
			//         GpuProgramParameters.AutoConstantType.FarClipDistance );

			//      Vec4 properties = Vec4.Zero;
			//      properties.X = autoFocus ? -1.0f : focalDistance;
			//      properties.Y = focalSize;
			//      properties.Z = backgroundTransitionLength;
			//      properties.W = blurForeground ? foregroundTransitionLength : -1;
			//      parameters.SetNamedConstant( "properties", properties );
			//   }
			//   break;

			//Final pass
			case rt_targetOutput:
				{
					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;

					parameters.SetNamedAutoConstant( "farClipDistance",
						GpuProgramParameters.AutoConstantType.FarClipDistance );

					Vec4 properties = Vec4.Zero;
					properties.X = autoFocus ? -1.0f : focalDistance;
					properties.Y = focalSize;
					properties.Z = backgroundTransitionLength;
					properties.W = blurForeground ? foregroundTransitionLength : -1;
					parameters.SetNamedConstant( "properties", properties );

					//Vec2[] sampleOffsets = new Vec2[ 49 ];
					//Vec2 textureSize = Owner.DimensionsInPixels.Size.ToVec2();
					//for( int y = -3; y <= 3; y++ )
					//   for( int x = -3; x <= 3; x++ )
					//      sampleOffsets[ ( y + 3 ) * 7 + ( x + 3 ) ] = new Vec2( x, y ) / textureSize;

					////convert to Vec4 array
					//Vec4[] vec4Offsets = new Vec4[ 49 ];
					//for( int n = 0; n < 49; n++ )
					//{
					//   Vec2 offset = sampleOffsets[ n ];
					//   vec4Offsets[ n ] = new Vec4( offset[ 0 ], offset[ 1 ], 0, 0 );
					//}
					//parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
				}
				break;
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			DepthOfFieldCompositorParameters p = (DepthOfFieldCompositorParameters)parameters;
			FocalDistance = p.FocalDistance;
			FocalSize = p.FocalSize;
			BlurSpread = p.BlurSpread;
			BlurTextureResolution = p.BlurTextureResolution;
			BackgroundTransitionLength = p.BackgroundTransitionLength;
			BlurForeground = p.BlurForeground;
			ForegroundTransitionLength = p.ForegroundTransitionLength;
			AutoFocus = p.AutoFocus;
			AutoFocusRange = p.AutoFocusRange;
			AutoFocusTransitionSpeed = p.AutoFocusTransitionSpeed;
		}
	}
}
