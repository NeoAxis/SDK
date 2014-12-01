// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing.Design;
using System.ComponentModel;
using Engine.MathEx;
using Engine.Renderer;

namespace ProjectCommon
{
	[CompositorName( "Blur" )]
	public class BlurCompositorParameters : CompositorParameters
	{
		float fuzziness = 1;

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 15 )]
		public float Fuzziness
		{
			get { return fuzziness; }
			set 
			{
				if( value < 0 )
					value = 0;
				fuzziness = value; 
			}
		}
	}

	/// <summary>
	/// GaussianBlur scene post processing compositor instance.
	/// </summary>
	[CompositorName( "Blur" )]
	public class BlurCompositorInstance : CompositorInstance
	{
		float fuzziness = 1;

		[EditorLimitsRange( 0, 15 )]
		public float Fuzziness
		{
			get { return fuzziness; }
			set
			{
				if( value < 0 )
					value = 0;
				fuzziness = value;
			}
		}

		//

		static float GaussianDistribution( float x, float y, float rho )
		{
			float g = 1.0f / MathFunctions.Sqrt( 2.0f * MathFunctions.PI * rho * rho );
			g *= MathFunctions.Exp( -( x * x + y * y ) / ( 2.0f * rho * rho ) );
			return g;
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 700 || passId == 701 )
			{
				bool horizontal = passId == 700;

				Vec2[] sampleOffsets = new Vec2[ 15 ];
				Vec4[] sampleWeights = new Vec4[ 15 ];

				// calculate gaussian texture offsets & weights
				Vec2I textureSize = Owner.DimensionsInPixels.Size;
				float texelSize = 1.0f / (float)( horizontal ? textureSize.X : textureSize.Y );

				texelSize *= fuzziness;

				// central sample, no offset
				sampleOffsets[ 0 ] = Vec2.Zero;
				{
					float distribution = GaussianDistribution( 0, 0, 3 );
					sampleWeights[ 0 ] = new Vec4( distribution, distribution, distribution, 0 );
				}

				// 'pre' samples
				for( int n = 1; n < 8; n++ )
				{
					float distribution = GaussianDistribution( n, 0, 3 );
					sampleWeights[ n ] = new Vec4( distribution, distribution, distribution, 1 );

					if( horizontal )
						sampleOffsets[ n ] = new Vec2( (float)n * texelSize, 0 );
					else
						sampleOffsets[ n ] = new Vec2( 0, (float)n * texelSize );
				}
				// 'post' samples
				for( int n = 8; n < 15; n++ )
				{
					sampleWeights[ n ] = sampleWeights[ n - 7 ];
					sampleOffsets[ n ] = -sampleOffsets[ n - 7 ];
				}

				//convert to Vec4 array
				Vec4[] vec4Offsets = new Vec4[ 15 ];
				for( int n = 0; n < 15; n++ )
				{
					Vec2 offset = sampleOffsets[ n ];
					vec4Offsets[ n ] = new Vec4( offset.X, offset.Y, 0, 0 );
				}

				GpuProgramParameters parameters = material.GetBestTechnique().
					Passes[ 0 ].FragmentProgramParameters;
				parameters.SetNamedConstant( "sampleOffsets", vec4Offsets );
				parameters.SetNamedConstant( "sampleWeights", sampleWeights );
			}
		}


		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			BlurCompositorParameters p = (BlurCompositorParameters)parameters;
			Fuzziness = p.Fuzziness;
		}

	}
}
