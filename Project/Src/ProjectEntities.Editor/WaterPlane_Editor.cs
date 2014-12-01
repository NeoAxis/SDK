// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.Design;
using Engine;
using Engine.EntitySystem;
using Engine.Utils;

namespace ProjectEntities.Editor
{
	public class WaterPlaneType_SplashesCollectionEditor : ProjectEntitiesGeneralListCollectionEditor 
	{
		public WaterPlaneType_SplashesCollectionEditor()
			: base( typeof( List<WaterPlaneType.SplashItem> ) )
		{ }
	}

	public class WaterPlaneTypeSplashItem_ParticlesCollectionEditor: ProjectEntitiesGeneralListCollectionEditor
	{
		public WaterPlaneTypeSplashItem_ParticlesCollectionEditor()
			: base( typeof( List<WaterPlaneType.SplashItem.ParticleItem> ) )
		{ }
	}
}
