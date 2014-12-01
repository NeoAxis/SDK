// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Renderer;

namespace ProjectCommon
{
	/// <summary>
	/// Material for correct rendering of weapons in a FPS mode.
	/// </summary>
	[Description( "Based on ShaderBaseMaterial class in intended for correct rendering of 3D models in the first person mode. The class fixes issues with rendering on close distances to the camera." )]
	public class FPSWeaponMaterial : ShaderBaseMaterial
	{
		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );
		}

		protected override string OnGetExtensionFileName()
		{
			return "FPSWeapon.shaderBaseExtension";
		}
	}
}
