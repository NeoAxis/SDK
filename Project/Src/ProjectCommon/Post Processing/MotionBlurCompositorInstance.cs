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
	[CompositorName( "MotionBlur" )]
	public class MotionBlurCompositorParameters : CompositorParameters
	{
		float blur = 1;

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2 )]
		public float Blur
		{
			get { return blur; }
			set
			{
				if( value < 0 )
					value = 0;
				blur = value;
			}
		}
	}

	/// <summary>
	/// Represents work with the MotionBlur post effect.
	/// </summary>
	[CompositorName( "MotionBlur" )]
	public class MotionBlurCompositorInstance : CompositorInstance
	{
		float blur = 1;

		[EditorLimitsRange( 0, 2 )]
		public float Blur
		{
			get { return blur; }
			set
			{
				if( value < 0 )
					value = 0;
				blur = value;
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 666 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].
					Passes[ 0 ].FragmentProgramParameters;
				if( parameters != null )
				{
					parameters.SetNamedConstant( "blur", Blur );
				}
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			MotionBlurCompositorParameters p = (MotionBlurCompositorParameters)parameters;
			Blur = p.Blur;
		}
	}
}
