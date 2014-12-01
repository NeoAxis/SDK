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
	[CompositorName( "_ShowDepth" )]
	public class ShowDepthCompositorParameters : CompositorParameters
	{
		float multiplier = 1;

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 100 )]
		public float Multiplier
		{
			get { return multiplier; }
			set
			{
				if( value < 0 )
					value = 0;
				multiplier = value;
			}
		}
	}

	/// <summary>
	/// Represents work with the ShowDepth post effect.
	/// </summary>
	[CompositorName( "_ShowDepth" )]
	public class ShowDepthCompositorInstance : CompositorInstance
	{
		float multiplier = 1;

		[EditorLimitsRange( 1, 100 )]
		public float Multiplier
		{
			get { return multiplier; }
			set
			{
				if( value < 0 )
					value = 0;
				multiplier = value;
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 100 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				if( parameters != null )
				{
					parameters.SetNamedAutoConstant( "farClipDistance",
						GpuProgramParameters.AutoConstantType.FarClipDistance );
					parameters.SetNamedAutoConstant( "viewportSize",
						GpuProgramParameters.AutoConstantType.ViewportSize );

					parameters.SetNamedConstant( "multiplier", Multiplier );
				}
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			ShowDepthCompositorParameters p = (ShowDepthCompositorParameters)parameters;
			Multiplier = p.Multiplier;
		}

	}
}
