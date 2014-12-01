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
	public class CarType_SteerVsForwardSpeedTableCollectionEditor : ProjectEntitiesGeneralListCollectionEditor
	{
		public CarType_SteerVsForwardSpeedTableCollectionEditor()
			: base( typeof( List<CarType.SteerVsForwardSpeedTableItem> ) )
		{ }
	}
}
