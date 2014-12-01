// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MathEx;

namespace ODEPhysicsSystem
{
	static class Convert
	{
		public static Vec3 ToNet( Ode.dVector3 value )
		{
			return new Vec3( value.X, value.Y, value.Z );
		}

		public static void ToNet( ref Ode.dVector3 value, out Vec3 result )
		{
			result = new Vec3( value.X, value.Y, value.Z );
		}

		public unsafe static void ToNet( float* value, out Vec3 result )
		{
			result = new Vec3( value[ 0 ], value[ 1 ], value[ 2 ] );
		}

		public unsafe static void ToNet( float* value, out Quat result )
		{
			result = new Quat( value[ 1 ], value[ 2 ], value[ 3 ], value[ 0 ] );
		}

		public static void ToODE( Quat value, out Ode.dQuaternion result )
		{
			result.X = value.X;
			result.Y = value.Y;
			result.Z = value.Z;
			result.W = value.W;
		}

		public static void ToODE( ref Mat3 value, out Ode.dMatrix3 result )
		{
			result.M00 = value.Item0.X;
			result.M01 = value.Item1.X;
			result.M02 = value.Item2.X;
			result.M03 = 0;
			result.M10 = value.Item0.Y;
			result.M11 = value.Item1.Y;
			result.M12 = value.Item2.Y;
			result.M13 = 0;
			result.M20 = value.Item0.Z;
			result.M21 = value.Item1.Z;
			result.M22 = value.Item2.Z;
			result.M23 = 0;
		}

	}
}
