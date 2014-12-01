// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MathEx;
using Engine.PhysicsSystem;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	public interface IPhysXJoint
	{
		void UpdateDataFromLibrary();
		void SetVisualizationEnable( bool enable );
	}
}
