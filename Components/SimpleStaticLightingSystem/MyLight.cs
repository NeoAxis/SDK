// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.PhysicsSystem;
using StaticLightingCalculationSystem;

namespace SimpleLightmapSystem
{
	abstract class MyLight
	{
		StaticLightingCalculationWorld.Light data;
		Bounds bounds;

		//

		public Bounds Bounds
		{
			get { return bounds; }
		}

		protected MyLight( StaticLightingCalculationWorld.Light data )
		{
			this.data = data;
		}

		public StaticLightingCalculationWorld.Light Data
		{
			get { return data; }
		}

		public ColorValue DiffuseColor
		{
			get { return data.DiffuseColor; }
		}

		protected abstract Bounds GetBounds();
		public abstract float GetIllumination( Vec3 position );
		public abstract float GetIllumination( Vec3 position, Vec3 normal );
		//public abstract Vec3 GetCheckVisibilityRayDirection( Vec3 position );
		public abstract Ray GetCheckVisibilityRay( Vec3 position );

		public void Initialize()
		{
			bounds = GetBounds();
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	abstract class MyPositioningLight : MyLight
	{
		StaticLightingCalculationWorld.PositioningLight data;

		//

		public MyPositioningLight( StaticLightingCalculationWorld.PositioningLight data )
			: base( data )
		{
			this.data = data;
		}

		protected override Bounds GetBounds()
		{
			Bounds bounds = new Bounds( data.Position );
			bounds.Expand( data.AttenuationFar * 1.1f );
			return bounds;

			//if( !float.IsPositiveInfinity( data.AttenuationRange ) )
			//{
			//   Bounds bounds = new Bounds( data.Position );
			//   bounds.Expand( data.AttenuationRange );
			//   return bounds;
			//}
			//else
			//{
			//   return new Bounds( 
			//      new Vec3( float.MinValue, float.MinValue, float.MinValue ),
			//      new Vec3( float.MaxValue, float.MaxValue, float.MaxValue ) );
			//}
		}

		float GetAttenuationCoefficient( Vec3 position )
		{
			Vec3 diff = position - data.Position;
			Vec3 dir = diff;
			float distance = dir.Normalize();

			//attenuation
			float near = data.AttenuationNear;
			float far = data.AttenuationFar;
			float power = data.AttenuationPower;

			if( near > far - .001f )
				near = far - .001f;

			float attenuationCoef = 
				MathFunctions.Pow( 1.0f - Math.Min( ( distance - near ) / ( far - near ), 1.0f ), power );
			MathFunctions.Saturate( ref attenuationCoef );

			return attenuationCoef;
		}

		public override float GetIllumination( Vec3 position )
		{
			return GetAttenuationCoefficient( position );
		}

		public override float GetIllumination( Vec3 position, Vec3 normal )
		{
			Vec3 diff = position - data.Position;
			Vec3 dir = diff;

			//normal
			float normalCoef = Vec3.Dot( normal, -dir );
			MathFunctions.Saturate( ref normalCoef );

			return GetAttenuationCoefficient( position ) * normalCoef;
		}

		public override Ray GetCheckVisibilityRay( Vec3 position )
		{
			Vec3 diff = position - data.Position;
			Vec3 dir = diff.GetNormalize();
			return new Ray( data.Position, diff - dir * .01f );
		}

		//public override Vec3 GetCheckVisibilityRayDirection( Vec3 position )
		//{
		//   return data.Position - position;
		//}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class MyPointLight : MyPositioningLight
	{
		StaticLightingCalculationWorld.PointLight data;

		//

		public MyPointLight( StaticLightingCalculationWorld.PointLight data )
			: base( data )
		{
			this.data = data;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class MySpotLight : MyPositioningLight
	{
		StaticLightingCalculationWorld.SpotLight data;
		Vec3 lightDirection;
		float cosOuterAngleHalf;
		float cosInnerAngleHalf;

		//

		public MySpotLight( StaticLightingCalculationWorld.SpotLight data )
			: base( data )
		{
			this.data = data;
			lightDirection = data.Rotation * new Vec3( 1, 0, 0 );
			cosOuterAngleHalf = MathFunctions.Cos( data.OuterAngle.InRadians() / 2 );
			cosInnerAngleHalf = MathFunctions.Cos( data.InnerAngle.InRadians() / 2 );
		}

		float GetSpotCoefficient( Vec3 position )
		{
			Vec3 dir = Vec3.Normalize( position - data.Position );

			float spotCoef;

			float rho = Vec3.Dot( lightDirection, Vec3.Normalize( dir ) );
			MathFunctions.Saturate( ref rho );

			float v = rho - cosOuterAngleHalf;
			MathFunctions.Saturate( ref v );

			spotCoef = MathFunctions.Pow( v /
				( cosInnerAngleHalf - cosOuterAngleHalf ), data.Falloff );
			MathFunctions.Saturate( ref spotCoef );

			return spotCoef;
		}

		public override float GetIllumination( Vec3 position )
		{
			return base.GetIllumination( position ) * GetSpotCoefficient( position );
		}

		public override float GetIllumination( Vec3 position, Vec3 normal )
		{
			return base.GetIllumination( position, normal ) * GetSpotCoefficient( position );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class MyDirectionalLight : MyLight
	{
		StaticLightingCalculationWorld.DirectionalLight data;
		Vec3 lightDirection;

		//

		public MyDirectionalLight( StaticLightingCalculationWorld.DirectionalLight data )
			: base( data )
		{
			this.data = data;
			lightDirection = data.Rotation * new Vec3( 1, 0, 0 );
		}

		protected override Bounds GetBounds()
		{
			return new Bounds(
				new Vec3( float.MinValue, float.MinValue, float.MinValue ),
				new Vec3( float.MaxValue, float.MaxValue, float.MaxValue ) );
		}

		public override float GetIllumination( Vec3 position )
		{
			return 1;
		}

		public override float GetIllumination( Vec3 position, Vec3 normal )
		{
			//normal
			float normalCoef = Vec3.Dot( normal, -lightDirection );
			MathFunctions.Saturate( ref normalCoef );
			return normalCoef;
		}

		public override Ray GetCheckVisibilityRay( Vec3 position )
		{
			Vec3 dataPosition = position - lightDirection * 10000.0f;

			Vec3 diff = position - dataPosition;
			Vec3 dir = diff.GetNormalize();
			return new Ray( dataPosition, diff - dir * .01f );
		}

		//public override Vec3 GetCheckVisibilityRayDirection( Vec3 position )
		//{
		//   return -lightDirection * 10000.0f;
		//}
	}

}
