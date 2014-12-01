// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectCommon
{
	public enum MaterialSchemes
	{
		//no normal mapping, no receiving shadows, no specular.
		//Game: HeightmapTerrain will use SimpleRendering mode for this scheme.
		//Game: used for generation WaterPlane reflection.
		Low,

		//High. Maximum quality.
		//Resource Editor and Map Editor uses "Default" scheme by default.
		//Note! Need save "Default" scheme in this enumeration. Without that scheme possible to get side effects.
		Default
	}
}
