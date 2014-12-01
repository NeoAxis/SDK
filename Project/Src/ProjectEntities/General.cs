// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MapSystem;
using Engine.SoundSystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines possible substances of <see cref="Dynamic"/> objects.
	/// Substances are necessary for work of influences (<see cref="Influence"/>). 
	/// The certain influences operate only on the set substances.
	/// </summary>
	[Flags]
	public enum Substance
	{
		None = 0,
		Flesh = 2,
		Metal = 4,
		Wood = 8,
	}

	/// <summary>
	/// User defined scene graph groups for MapObjects.
	/// </summary>
	public class MapObjectSceneGraphGroups
	{
		//all Units will have group with number 1.
		public const int UnitGroup = 1;

		//use this value for faster getting objects by means GetObjects.
		//Map.Instance.GetObjects( bounds, GameSceneGraphGroups.UnitGroupMask );
		public const uint UnitGroupMask = 1 << UnitGroup;

		public const uint AllObjectsGroupMask = 0xFFFFFFFF;
	}
}
