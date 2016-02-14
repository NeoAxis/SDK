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
	[CompositorName( "ColorCorrectionLUT" )]
	public class ColorCorrectionLUTCompositorParameters : CompositorParameters
	{
		string textureName = "Base\\FullScreenEffects\\ColorCorrectionLUT\\Textures\\Sepia.png";
		float multiply = 1;
		float add;

		[DefaultValue( "Base\\FullScreenEffects\\ColorCorrectionLUT\\Textures\\Sepia.png" )]
		[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		public string TextureName
		{
			get { return textureName; }
			set { textureName = value; }
		}

		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 2 )]
		public float Multiply
		{
			get { return multiply; }
			set { multiply = value; }
		}

		[DefaultValue( 0.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( -1, 1 )]
		public float Add
		{
			get { return add; }
			set { add = value; }
		}
	}

	/// <summary>
	/// Represents work with the ColorCorrectionLUT post effect.
	/// </summary>
	[CompositorName( "ColorCorrectionLUT" )]
	public class ColorCorrectionLUTCompositorInstance : CompositorInstance
	{
		string textureName = "Base\\FullScreenEffects\\ColorCorrectionLUT\\Textures\\Sepia.png";
		float multiply = 1;
		float add;

		//

		[DefaultValue( "Base\\FullScreenEffects\\ColorCorrectionLUT\\Textures\\Sepia.png" )]
		[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		public string TextureName
		{
			get { return textureName; }
			set { textureName = value; }
		}

		[EditorLimitsRange( .1f, 2 )]
		public float Multiply
		{
			get { return multiply; }
			set { multiply = value; }
		}

		[EditorLimitsRange( -1, 1 )]
		public float Add
		{
			get { return add; }
			set { add = value; }
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			//update texture name
			if( passId == 100 )
			{
				TextureUnitState textureUnit = material.Techniques[ 0 ].Passes[ 0 ].TextureUnitStates[ 1 ];

				//we can't change texture by means call SetTextureName() for compositor materials. use _Internal_SetTexture
				Texture texture = null;
				if( !string.IsNullOrEmpty( TextureName ) )
					texture = TextureManager.Instance.Load( TextureName, Texture.Type.Type2D );
				if( texture == null )
					texture = TextureManager.Instance.Load( "Base\\FullScreenEffects\\ColorCorrectionLUT\\Textures\\NoEffect.png" );
				textureUnit._Internal_SetTexture( texture );
				//if( textureUnit.TextureName != TextureName )
				//   textureUnit.SetTextureName( TextureName );

				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				parameters.SetNamedConstant( "multiply", multiply );
				parameters.SetNamedConstant( "add", add );
			}
		}

		protected override void OnUpdateParameters( CompositorParameters parameters )
		{
			base.OnUpdateParameters( parameters );

			ColorCorrectionLUTCompositorParameters p = (ColorCorrectionLUTCompositorParameters)parameters;
			TextureName = p.TextureName;
			Multiply = p.Multiply;
			Add = p.Add;
		}
	}
}
