// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="SimpleMap"/> entity type. 
	/// Used for preview types in the Resource Editor.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class SimpleMapType : MapType
	{
	}

	public class SimpleMap : Map
	{
		SimpleMapType _type = null; public new SimpleMapType Type { get { return _type; } }
	}

}
