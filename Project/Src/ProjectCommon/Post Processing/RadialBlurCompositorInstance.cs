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
	[CompositorName( "RadialBlur" )]
	public class RadialBlurCompositorParameters : CompositorParameters
	{
		Vec2 center = new Vec2( .5f, .5f );
		float blurFactor = .1f;

		[DefaultValue( typeof( Vec2 ), "0.5 0.5" )]
		[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public Vec2 Center
		{
			get { return center; }
			set { center = value; }
		}

		[DefaultValue( .1f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float BlurFactor
		{
			get { return blurFactor; }
			set
			{
				if( value < 0 )
					value = 0;
				blurFactor = value;
			}
		}
	}

	/// <summary>
	/// Represents work with the RadialBlur post effect.
	/// </summary>
	[CompositorName( "RadialBlur" )]
	public class RadialBlurCompositorInstance : CompositorInstance
	{
		Vec2 center = new Vec2( .5f, .5f );
		float blurFactor = .1f;

		//

		[EditorLimitsRange( 0, 1 )]
		public Vec2 Center
		{
			get { return center; }
			set { center = value; }
		}

		[EditorLimitsRange( 0, 1 )]
		public float BlurFactor
		{
			get { return blurFactor; }
			set
			{
				if( value < 0 )
					value = 0;
				blurFactor = value;
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 123 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				if( parameters != null )
				{
					parameters.SetNamedConstant( "center", new Vec4( center.X, center.Y, 0, 0 ) );
					parameters.SetNamedConstant( "blurFactor", blurFactor );
				}
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			RadialBlurCompositorParameters p = (RadialBlurCompositorParameters)parameters;
			Center = p.Center;
			BlurFactor = p.BlurFactor;
		}

	}
}
