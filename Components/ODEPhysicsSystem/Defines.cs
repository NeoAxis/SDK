// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;

namespace ODEPhysicsSystem
{
	static class Defines
	{
		public const int maxContacts = 24;
		public const float bounceThreshold = 1.0f;

		public const float autoDisableLinearMin = 0;
		public const float autoDisableLinearMax = 0.4f;
		public const float autoDisableAngularMin = 0;
		public const float autoDisableAngularMax = 0.2f;

		public const int autoDisableStepsMin = 4;
		public const int autoDisableStepsMax = 60;
		public const float autoDisableTimeMin = .05f;//0;
		public const float autoDisableTimeMax = 0.4f;
		//note: max and min mass ratios must be the inverse of each other
		//public const float minMassRatio = 0.001f;
		//const float maxMassRatio=(float)1000.0;
		public const float minERP = 0.1f;
		public const float maxERP = 0.9f;
		public const float globalCFM = 1e-5f;
		public const float jointFudgeFactor = 0.1f;
		public const float maxFriction = 100.0f;
		public const float surfaceLayer = 0.001f;

		//public const float ccdEpsilon = .001f;
	}
}
