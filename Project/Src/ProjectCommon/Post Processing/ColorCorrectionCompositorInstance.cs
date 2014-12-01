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
	[CompositorName( "ColorCorrection" )]
	public class ColorCorrectionCompositorParameters : CompositorParameters
	{
		float red = 1;
		float green = 1;
		float blue = 1;

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 5 )]
		public float Red
		{
			get { return red; }
			set { red = value; }
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 5 )]
		public float Green
		{
			get { return green; }
			set { green = value; }
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 5 )]
		public float Blue
		{
			get { return blue; }
			set { blue = value; }
		}
	}

	/// <summary>
	/// Represents work with the ColorCorrection post effect.
	/// </summary>
	[CompositorName( "ColorCorrection" )]
	public class ColorCorrectionCompositorInstance : CompositorInstance
	{
		float red = 1;
		float green = 1;
		float blue = 1;

		//

		[EditorLimitsRange( 0, 5 )]
		public float Red
		{
			get { return red; }
			set { red = value; }
		}

		[EditorLimitsRange( 0, 5 )]
		public float Green
		{
			get { return green; }
			set { green = value; }
		}

		[EditorLimitsRange( 0, 5 )]
		public float Blue
		{
			get { return blue; }
			set { blue = value; }
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 500 )
			{
				Vec4 multiplier = new Vec4( Red, Green, Blue, 1 );

				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				parameters.SetNamedConstant( "multiplier", multiplier );
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			ColorCorrectionCompositorParameters p = (ColorCorrectionCompositorParameters)parameters;
			Red = p.Red;
			Green = p.Green;
			Blue = p.Blue;
		}
	}
}
