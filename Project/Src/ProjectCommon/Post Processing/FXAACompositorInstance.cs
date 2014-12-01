// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;

namespace ProjectCommon
{
	/// <summary>
	/// Represents work with the ShowDepth post effect.
	/// </summary>
	[CompositorName( "FXAA" )]
	public class FXAACompositorInstance : CompositorInstance
	{
		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 100 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				if( parameters != null )
				{
					parameters.SetNamedAutoConstant( "viewportSize", 
						GpuProgramParameters.AutoConstantType.ViewportSize );
				}
			}
		}
	}
}
