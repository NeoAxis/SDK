// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.Utils;
using Engine.MathEx;

namespace ProjectCommon
{
	/// <summary>
	/// Represents work with the HeatVision post effect.
	/// </summary>
	[CompositorName( "HeatVision" )]
	public class HeatVisionCompositorInstance : CompositorInstance
	{
		float start;
		float end;
		float current;
		EngineRandom random = new EngineRandom();

		//

		protected override void OnCreateTexture( string definitionName, ref Vec2I size, ref PixelFormat format )
		{
			base.OnCreateTexture( definitionName, ref size, ref format );

			if( definitionName == "scene" || definitionName == "temp" )
				size = Owner.DimensionsInPixels.Size / 2;
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 123 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].
					Passes[ 0 ].FragmentProgramParameters;

				if( parameters != null )
				{
					// randomFractions
					parameters.SetNamedConstant( "randomFractions",
						new Vec4( random.NextFloat(), random.NextFloat(), 0, 0 ) );

					// depthModulator
					if( ( Math.Abs( current - end ) <= .001f ) )
					{
						// take a new value to reach
						end = .95f + random.NextFloat() * .05f;
						start = current;
					}
					else
					{
						float step = RendererWorld.Instance.FrameRenderTimeStep;
						if( step > .3f )
							step = 0;

						if( current > end )
							current -= step;
						else
							current += step;
					}
					parameters.SetNamedConstant( "depthModulator", current );
				}
			}
		}
	}
}
