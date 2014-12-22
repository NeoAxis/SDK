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
	[CompositorName( "Sharpen" )]
	public class SharpenCompositorParameters : CompositorParameters
	{
		float sharpStrength = 1;
		float sharpClamp = .035f;
		float offsetBias = 1;

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2.0f )]
		public float SharpStrength
		{
			get { return sharpStrength; }
			set
			{
				if( value < 0 )
					value = 0;
				sharpStrength = value;
			}
		}

		[DefaultValue( .035f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float SharpClamp
		{
			get { return sharpClamp; }
			set
			{
				if( value < 0 )
					value = 0;
				sharpClamp = value;
			}
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 10.0f )]
		public float OffsetBias
		{
			get { return offsetBias; }
			set
			{
				if( value < 0 )
					value = 0;
				offsetBias = value;
			}
		}
	}

	[CompositorName( "Sharpen" )]
	public class SharpenCompositorInstance : CompositorInstance
	{
		float sharpStrength = 1;
		float sharpClamp = .035f;
		float offsetBias = 1;

		[EditorLimitsRange( 0, 2.0f )]
		public float SharpStrength
		{
			get { return sharpStrength; }
			set
			{
				if( value < 0 )
					value = 0;
				sharpStrength = value;
			}
		}

		[EditorLimitsRange( 0, 1 )]
		public float SharpClamp
		{
			get { return sharpClamp; }
			set
			{
				if( value < 0 )
					value = 0;
				sharpClamp = value;
			}
		}

		[EditorLimitsRange( .1f, 10.0f )]
		public float OffsetBias
		{
			get { return offsetBias; }
			set
			{
				if( value < 0 )
					value = 0;
				offsetBias = value;
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
					parameters.SetNamedAutoConstant( "viewportSize", GpuProgramParameters.AutoConstantType.ViewportSize );
					parameters.SetNamedConstant( "sharp_strength", sharpStrength );
					parameters.SetNamedConstant( "sharp_clamp", sharpClamp );
					parameters.SetNamedConstant( "offset_bias", offsetBias );
				}
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			SharpenCompositorParameters p = (SharpenCompositorParameters)parameters;
			SharpStrength = p.SharpStrength;
			SharpClamp = p.SharpClamp;
			OffsetBias = p.OffsetBias;
		}
	}
}
