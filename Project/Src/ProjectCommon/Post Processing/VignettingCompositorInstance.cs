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
	[CompositorName( "Vignetting" )]
	public class VignettingCompositorParameters : CompositorParameters
	{
		float radius = 3;
		float intensity = 1;

		[DefaultValue( 3.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float Radius
		{
			get { return radius; }
			set
			{
				if( value < 0 )
					value = 0;
				radius = value;
			}
		}

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
				intensity = value;
			}
		}
	}

	/// <summary>
	/// Represents work with the Vignetting post effect.
	/// </summary>
	[CompositorName( "Vignetting" )]
	public class VignettingCompositorInstance : CompositorInstance
	{
		float radius = 3;
		float intensity = 1;

		//

		[EditorLimitsRange( 0, 10 )]
		public float Radius
		{
			get { return radius; }
			set
			{
				if( value < 0 )
					value = 0;
				radius = value;
			}
		}

		[EditorLimitsRange( 0, 1 )]
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

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 555 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				parameters.SetNamedConstant( "intensity", intensity );
				parameters.SetNamedConstant( "radius", radius );
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			VignettingCompositorParameters p = (VignettingCompositorParameters)parameters;
			Radius = p.Radius;
			Intensity = p.Intensity;
		}

	}
}
