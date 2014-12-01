// Thanks to Radius Studios for this post effect
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
	[CompositorName( "Grayscale" )]
	public class GrayscaleCompositorParameters : CompositorParameters
	{
		float intensity = 1;

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float Intensity
		{
			get { return intensity; }
			set
			{
				if( value < 0 )
					value = 0;
				if( value > 1 )
					value = 1;
				intensity = value;
			}
		}
	}

	/// <summary>
	/// Represents work with the Grayscale post effect.
	/// </summary>
	[CompositorName( "Grayscale" )]
	public class GrayscaleCompositorInstance : CompositorInstance
	{
		float intensity = 1;

		//

		[EditorLimitsRange( 0, 1 )]
		public float Intensity
		{
			get { return intensity; }
			set
			{
				if( value < 0 )
					value = 0;
				if( value > 1 )
					value = 1;
				intensity = value;
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 555 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				parameters.SetNamedConstant( "intensity", intensity );
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			GrayscaleCompositorParameters p = (GrayscaleCompositorParameters)parameters;
			Intensity = p.Intensity;
		}

	}
}
