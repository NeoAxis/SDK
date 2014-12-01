// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.Renderer;
using Engine.MathEx;

namespace ProjectCommon
{
	[CompositorName( "SSAO" )]
	public class SSAOCompositorParameters : CompositorParameters
	{
		float intensity = 1.5f;
		float downsampling = 2;
		//int iterations = 24;
		float sampleLength = 2;
		float offsetScale = .1f;
		float defaultAccessibility = .6f;
		float maxDistance = 50;
		float blurSpread = 1;
		//bool fixEdges = true;
		bool showAO;

		//

		[DefaultValue( 1.5f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 4 )]
		public float Intensity
		{
			get { return intensity; }
			set
			{
				if( value < 0 )
					value = 0;
				intensity = value;
			}
		}

		[DefaultValue( 2.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 3 )]
		public float Downsampling
		{
			get { return downsampling; }
			set
			{
				int n = (int)( value + .4999f );
				MathFunctions.Clamp( ref n, 1, 3 );
				downsampling = n;

				//if( value < 1 )
				//   value = 1;
				//downsampling = value;
			}
		}

		//[DefaultValue( 24 )]
		//[Editor( typeof( IntegerValueEditor ), typeof( UITypeEditor ) )]
		//[EditorLimitsRangeI( 8, 64 )]
		//public int Iterations
		//{
		//   get { return iterations; }
		//   set
		//   {
		//      int v = (int)( value / 8 );
		//      v *= 8;
		//      if( v < 8 )
		//         v = 8;
		//      if( v > 64 )
		//         v = 64;
		//      iterations = v;
		//   }
		//}

		[DefaultValue( 2.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float SampleLength
		{
			get { return sampleLength; }
			set
			{
				if( value < 0 )
					value = 0;
				sampleLength = value;
			}
		}

		[DefaultValue( .1f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float OffsetScale
		{
			get { return offsetScale; }
			set
			{
				if( value < 0 )
					value = 0;
				if( value > 1 )
					value = 1;
				offsetScale = value;
			}
		}

		[DefaultValue( .6f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float DefaultAccessibility
		{
			get { return defaultAccessibility; }
			set
			{
				if( value < 0 )
					value = 0;
				if( value > 1 )
					value = 1;
				defaultAccessibility = value;
			}
		}

		[DefaultValue( 50.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 300 )]
		public float MaxDistance
		{
			get { return maxDistance; }
			set
			{
				if( value < 1 )
					value = 1;
				maxDistance = value;
			}
		}

		[DefaultValue( 1.0f )]
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

		//[DefaultValue( true )]
		//public bool FixEdges
		//{
		//   get { return fixEdges; }
		//   set { fixEdges = value; }
		//}

		[DefaultValue( false )]
		public bool ShowAO
		{
			get { return showAO; }
			set { showAO = value; }
		}
	}

	/// <summary>
	/// Represents work with the SSAO post effect.
	/// </summary>
	[CompositorName( "SSAO" )]
	public class SSAOCompositorInstance : CompositorInstance
	{
		float intensity = 1.5f;
		float downsampling = 2;
		//float iterations = 24;
		float sampleLength = 2;
		float offsetScale = .1f;
		float defaultAccessibility = .6f;
		float maxDistance = 50;
		float blurSpread = 1;
		//bool fixEdges = true;
		bool showAO;

		Vec2I downscaleTextureSize;

		//

		[EditorLimitsRange( 0, 4 )]
		public float Intensity
		{
			get { return intensity; }
			set
			{
				if( value < 0 )
					value = 0;
				intensity = value;
			}
		}

		[EditorLimitsRange( 1, 3 )]
		public float Downsampling
		{
			get { return downsampling; }
			set
			{
				int n = (int)( value + .4999f );
				MathFunctions.Clamp( ref n, 1, 3 );
				float newValue = n;

				if( downsampling == newValue )
					return;
				downsampling = newValue;

				//if( value < 1 )
				//   value = 1;
				//if( downsampling == value )
				//   return;
				//downsampling = value;

				//recreate
				if( Enabled )
				{
					Enabled = false;
					Enabled = true;
				}
			}
		}

		//[EditorLimitsRangeI( 8, 64 )]
		//public int Iterations
		//{
		//   get { return iterations; }
		//   set
		//   {
		//      int v = (int)( value / 8 );
		//      v *= 8;
		//      if( v < 8 )
		//         v = 8;
		//      if( v > 64 )
		//         v = 64;

		//      if( iterations == v )
		//         return;
		//      iterations = v;

		//      //recreate
		//      if( Enabled )
		//      {
		//         Enabled = false;
		//         Enabled = true;
		//      }
		//   }
		//}

		[EditorLimitsRange( 0, 10 )]
		public float SampleLength
		{
			get { return sampleLength; }
			set
			{
				if( value < 0 )
					value = 0;
				sampleLength = value;
			}
		}

		[EditorLimitsRange( 0, 1 )]
		public float OffsetScale
		{
			get { return offsetScale; }
			set
			{
				if( value < 0 )
					value = 0;
				if( value > 1 )
					value = 1;
				offsetScale = value;
			}
		}

		[EditorLimitsRange( 0, 1 )]
		public float DefaultAccessibility
		{
			get { return defaultAccessibility; }
			set
			{
				if( value < 0 )
					value = 0;
				if( value > 1 )
					value = 1;
				defaultAccessibility = value;
			}
		}

		[EditorLimitsRange( 1, 500 )]
		public float MaxDistance
		{
			get { return maxDistance; }
			set
			{
				if( value < 1 )
					value = 1;
				maxDistance = value;
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

		//[DefaultValue( true )]
		//public bool FixEdges
		//{
		//   get { return fixEdges; }
		//   set
		//   {
		//      if( fixEdges == value )
		//         return;
		//      fixEdges = value;
		//   }
		//}

		[DefaultValue( false )]
		public bool ShowAO
		{
			get { return showAO; }
			set { showAO = value; }
		}

		protected override void OnCreateTexture( string definitionName, ref Vec2I size, ref PixelFormat format )
		{
			base.OnCreateTexture( definitionName, ref size, ref format );

			//change format of rt_scene and rt_final textures if HDR compositor is enabled.
			if( definitionName == "rt_scene" || definitionName == "rt_final" )
			{
				CompositorInstance hdrInstance = Owner.GetCompositorInstance( "HDR" );
				if( hdrInstance != null && hdrInstance.Enabled )
					format = PixelFormat.Float16RGB;
				else
					format = PixelFormat.R8G8B8;
			}

			if( definitionName == "rt_depth" || definitionName == "rt_occlusion" )
			{
				float divisor = downsampling;
				if( divisor < 1 )
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

		static float GaussianDistribution( float x, float y, float rho )
		{
			float g = 1.0f / MathFunctions.Sqrt( 2.0f * MathFunctions.PI * rho * rho );
			g *= MathFunctions.Exp( -( x * x + y * y ) / ( 2.0f * rho * rho ) );
			return g;
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

			const int rt_depthDownsample1x1 = 100;
			const int rt_depthDownsample2x2 = 101;
			const int rt_depthDownsample3x3 = 102;
			const int rt_occlusion = 200;
			const int rt_blurHorizontal = 300;
			const int rt_blurVertical = 400;
			const int rt_targetOutput = 500;

			switch( passId )
			{
			case rt_depthDownsample1x1:
			case rt_depthDownsample2x2:
			case rt_depthDownsample3x3:
				{
					if( downsampling == 1 && passId != rt_depthDownsample1x1 )
					{
						skipPass = true;
						return;
					}
					if( downsampling == 2 && passId != rt_depthDownsample2x2 )
					{
						skipPass = true;
						return;
					}
					if( downsampling == 3 && passId != rt_depthDownsample3x3 )
					{
						skipPass = true;
						return;
					}

					GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
					if( parameters != null )
					{
						parameters.SetNamedAutoConstant( "viewportSize", GpuProgramParameters.AutoConstantType.ViewportSize );

						Vec4 v = new Vec4( EngineApp.Instance.IsKeyPressed( EKeys.Z ) ? 1 : -1, 0, 0, 0 );
						parameters.SetNamedConstant( "temp", v );
					}
				}
				break;

			case rt_occlusion:
				{
					GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
					if( parameters != null )
					{
						parameters.SetNamedAutoConstant( "farClipDistance",
							GpuProgramParameters.AutoConstantType.FarClipDistance );
						parameters.SetNamedAutoConstant( "viewportSize",
							GpuProgramParameters.AutoConstantType.ViewportSize );
						parameters.SetNamedAutoConstant( "fov",
							GpuProgramParameters.AutoConstantType.FOV );

						parameters.SetNamedConstant( "downscaleTextureSize",
							new Vec4( downscaleTextureSize.X, downscaleTextureSize.Y,
							1.0f / (float)downscaleTextureSize.X, 1.0f / (float)downscaleTextureSize.Y ) );

						parameters.SetNamedConstant( "sampleLength", sampleLength );
						parameters.SetNamedConstant( "offsetScale", offsetScale );
						parameters.SetNamedConstant( "defaultAccessibility", defaultAccessibility );

						//parameters.SetNamedConstant( "parameters",
						//   new Vec4( sampleLength, offsetScale, defaultAccessibility, 0 ) );

						Range range = new Range( maxDistance, maxDistance * 1.2f );
						parameters.SetNamedConstant( "fadingByDistanceRange",
							new Vec4( range.Minimum, 1.0f / ( range.Maximum - range.Minimum ), 0, 0 ) );
					}
				}
				break;

			case rt_blurHorizontal:
			case rt_blurVertical:
				{
					// horizontal and vertical blur
					bool horizontal = passId == rt_blurHorizontal;

					float[] sampleOffsets = new float[ 15 ];
					Vec4[] sampleWeights = new Vec4[ 15 ];

					Vec2I textureSize = Owner.DimensionsInPixels.Size;
					CalculateBlurSampleOffsets( horizontal ? textureSize.X : textureSize.Y, sampleOffsets, sampleWeights, 3, 1 );

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
					parameters.SetNamedAutoConstant( "farClipDistance",
						GpuProgramParameters.AutoConstantType.FarClipDistance );
					parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
					parameters.SetNamedConstant( "sampleWeights", sampleWeights );

					//parameters.SetNamedConstant( "horizontal", passId == rt_blurHorizontal ? 1.0f : -1.0f );
				}
				break;

			case rt_targetOutput:
				{
					GpuProgramParameters parameters = material.GetBestTechnique().
						Passes[ 0 ].FragmentProgramParameters;
					parameters.SetNamedConstant( "intensity", intensity );

					//parameters.SetNamedConstant( "fixEdges", fixEdges ? 1.0f : -1.0f );
					parameters.SetNamedConstant( "showAO", showAO ? 1.0f : -1.0f );

					parameters.SetNamedConstant( "downscaleTextureSize",
						new Vec4( downscaleTextureSize.X, downscaleTextureSize.Y,
							1.0f / (float)downscaleTextureSize.X, 1.0f / (float)downscaleTextureSize.Y ) );
				}
				break;
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			SSAOCompositorParameters p = (SSAOCompositorParameters)parameters;
			Intensity = p.Intensity;
			Downsampling = p.Downsampling;
			//Iterations = p.Iterations;
			SampleLength = p.SampleLength;
			OffsetScale = p.OffsetScale;
			DefaultAccessibility = p.DefaultAccessibility;
			MaxDistance = p.MaxDistance;
			BlurSpread = p.BlurSpread;
			//FixEdges = p.FixEdges;
			ShowAO = p.ShowAO;
		}
	}
}
