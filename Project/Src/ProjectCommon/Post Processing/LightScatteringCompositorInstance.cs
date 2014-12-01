// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace ProjectCommon
{
	[CompositorName( "LightScattering" )]
	public class LightScatteringCompositorParameters : CompositorParameters
	{
		ColorValue color = new ColorValue( 1, 1, .6f );
		float intensity = .5f;
		float decay = .9f;
		float density = 1;
		float blurFactor = .2f;
		float resolution = 5;

		//

		[DefaultValue( typeof( ColorValue ), "255 255 153" )]
		[ColorValueNoAlphaChannel]
		public ColorValue Color
		{
			get { return color; }
			set { color = value; }
		}

		[DefaultValue( .5f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2 )]
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

		[DefaultValue( .9f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float Decay
		{
			get { return decay; }
			set
			{
				if( value < 0 )
					value = 0;
				decay = value;
			}
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		public float Density
		{
			get { return density; }
			set
			{
				if( value < 0 )
					value = 0;
				density = value;
			}
		}

		[DefaultValue( .2f )]
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

		[DefaultValue( 5.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 8 )]
		public float Resolution
		{
			get { return resolution; }
			set
			{
				if( value < 1 )
					value = 1;
				if( value > 8 )
					value = 8;
				resolution = value;
			}
		}
	}

	/// <summary>
	/// LightScattering scene post processing compositor instance.
	/// </summary>
	[CompositorName( "LightScattering" )]
	public class LightScatteringCompositorInstance : CompositorInstance
	{
		ColorValue color = new ColorValue( 1, 1, .6f );
		float intensity = .5f;
		float decay = .9f;
		float density = 1;
		float blurFactor = .2f;
		float resolution = 5;

		float smoothIntensityFactorLastUpdate;
		float smoothIntensityFactor;

		//

		[ColorValueNoAlphaChannel]
		public ColorValue Color
		{
			get { return color; }
			set { color = value; }
		}

		[EditorLimitsRange( 0, 2 )]
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

		[EditorLimitsRange( 0, 1 )]
		public float Decay
		{
			get { return decay; }
			set
			{
				if( value < 0 )
					value = 0;
				decay = value;
			}
		}

		[EditorLimitsRange( 0, 1 )]
		public float Density
		{
			get { return density; }
			set
			{
				if( value < 0 )
					value = 0;
				density = value;
			}
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

		[EditorLimitsRange( 1, 8 )]
		public float Resolution
		{
			get { return resolution; }
			set
			{
				if( value < 1 )
					value = 1;
				if( value > 8 )
					value = 8;
				if( resolution == value )
					return;
				resolution = value;

				//recreate
				if( Enabled )
				{
					Enabled = false;
					Enabled = true;
				}
			}
		}

		protected override void OnCreateTexture( string definitionName, ref Vec2I size, ref PixelFormat format )
		{
			base.OnCreateTexture( definitionName, ref size, ref format );

			if( definitionName == "rt_scattering" || definitionName == "rt_blur" )
			{
				float divisor = 9.0f - resolution;
				if( divisor <= 1 )
					divisor = 1;
				Vec2 sizeFloat = Owner.DimensionsInPixels.Size.ToVec2() / divisor;
				size = new Vec2I( (int)sizeFloat.X, (int)sizeFloat.Y );
			}
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			Camera camera = Owner.ViewportCamera;

			Sun sun = null;
			Vec2 screenLightPosition = Vec2.Zero;
			if( Map.Instance != null && Map.Instance.IsPostCreated && Sun.Instances.Count > 0 )
			{
				//get first sun entity on the map.
				sun = Sun.Instances[ 0 ];

				Vec3 direction;
				if( sun.BillboardOverridePosition != Vec3.Zero )
					direction = sun.BillboardOverridePosition.GetNormalize();
				else
					direction = -sun.Rotation.GetForward();

				Vec3 sunPosition = camera.Position + direction * 100000;

				if( !camera.ProjectToScreenCoordinates( sunPosition, out screenLightPosition ) )
				{
					//don't see the sun.
					sun = null;
				}
			}

			//calculate intensity factor by the sun position on the screen.
			float needIntensityFactor = 0;
			if( sun != null )
			{
				const float screenFadingBorder = .1f;

				float minDistance = 1;

				for( int axis = 0; axis < 2; axis++ )
				{
					if( screenLightPosition[ axis ] < screenFadingBorder )
					{
						float d = screenLightPosition[ axis ];
						if( d < minDistance )
							minDistance = d;
					}
					else if( screenLightPosition[ axis ] > 1.0f - screenFadingBorder )
					{
						float d = 1.0f - screenLightPosition[ axis ];
						if( d < minDistance )
							minDistance = d;
					}
				}
				needIntensityFactor = minDistance / screenFadingBorder;
				MathFunctions.Saturate( ref needIntensityFactor );

				//clamp screenLightPosition
				if( !new Rect( 0, 0, 1, 1 ).IsContainsPoint( screenLightPosition ) )
				{
					Vec2 intersectPoint1;
					Vec2 intersectPoint2;
					if( MathUtils.IntersectRectangleLine( new Rect( .0001f, .0001f, .9999f, .9999f ),
						new Vec2( .5f, .5f ), screenLightPosition, out intersectPoint1, out intersectPoint2 ) != 0 )
					{
						screenLightPosition = intersectPoint1;
					}
				}
			}

			//update smooth intensity factor
			if( sun != null )
			{
				if( smoothIntensityFactorLastUpdate != RendererWorld.Instance.FrameRenderTime )
				{
					smoothIntensityFactorLastUpdate = RendererWorld.Instance.FrameRenderTime;

					const float smoothSpeed = 3;
					float step = RendererWorld.Instance.FrameRenderTimeStep * smoothSpeed;

					if( needIntensityFactor > smoothIntensityFactor )
					{
						smoothIntensityFactor += step;
						if( smoothIntensityFactor > needIntensityFactor )
							smoothIntensityFactor = needIntensityFactor;
					}
					else
					{
						smoothIntensityFactor -= step;
						if( smoothIntensityFactor < needIntensityFactor )
							smoothIntensityFactor = needIntensityFactor;
					}
				}
			}
			else
				smoothIntensityFactor = 0;

			//get result intensity
			float resultIntensity = intensity * smoothIntensityFactor;


			const int rt_scattering = 100;
			const int rt_blur = 200;
			const int rt_final = 300;

			//skip passes for disabled effect
			if( resultIntensity == 0 )
			{
				if( passId == rt_scattering || passId == rt_blur )
				{
					skipPass = true;
					return;
				}
			}

			//set gpu parameters

			switch( passId )
			{
			case rt_scattering:
				{
					GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].
						FragmentProgramParameters;
					parameters.SetNamedConstant( "color", new Vec4( color.Red, color.Green, color.Blue, 0 ) );
					parameters.SetNamedConstant( "screenLightPosition",
						new Vec4( screenLightPosition.X, screenLightPosition.Y, 0, 0 ) );
					parameters.SetNamedConstant( "decay", decay );
					parameters.SetNamedConstant( "density", density );
				}
				break;

			case rt_blur:
				{
					GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].
						FragmentProgramParameters;
					parameters.SetNamedConstant( "color", new Vec4( color.Red, color.Green, color.Blue, 0 ) );
					parameters.SetNamedConstant( "screenLightPosition",
						new Vec4( screenLightPosition.X, screenLightPosition.Y, 0, 0 ) );
					parameters.SetNamedConstant( "intensity", resultIntensity );
					parameters.SetNamedConstant( "blurFactor", blurFactor );
				}
				break;

			case rt_final:
				{
					GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].
						FragmentProgramParameters;
					parameters.SetNamedConstant( "intensity", resultIntensity );
				}
				break;
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			LightScatteringCompositorParameters p = (LightScatteringCompositorParameters)parameters;
			Color = p.Color;
			Decay = p.Decay;
			Density = p.Density;
			Intensity = p.Intensity;
			BlurFactor = p.BlurFactor;
			Resolution = p.Resolution;
		}
	}
}
