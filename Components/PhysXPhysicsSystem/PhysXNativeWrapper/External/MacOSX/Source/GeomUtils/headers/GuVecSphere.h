// This code contains NVIDIA Confidential Information and is disclosed to you 
// under a form of NVIDIA software license agreement provided separately to you.
//
// Notice
// NVIDIA Corporation and its licensors retain all intellectual property and
// proprietary rights in and to this software and related documentation and 
// any modifications thereto. Any use, reproduction, disclosure, or 
// distribution of this software and related documentation without an express 
// license agreement from NVIDIA Corporation is strictly prohibited.
// 
// ALL NVIDIA DESIGN SPECIFICATIONS, CODE ARE PROVIDED "AS IS.". NVIDIA MAKES
// NO WARRANTIES, EXPRESSED, IMPLIED, STATUTORY, OR OTHERWISE WITH RESPECT TO
// THE MATERIALS, AND EXPRESSLY DISCLAIMS ALL IMPLIED WARRANTIES OF NONINFRINGEMENT,
// MERCHANTABILITY, AND FITNESS FOR A PARTICULAR PURPOSE.
//
// Information and code furnished is believed to be accurate and reliable.
// However, NVIDIA Corporation assumes no responsibility for the consequences of use of such
// information or for any infringement of patents or other rights of third parties that may
// result from its use. No license is granted by implication or otherwise under any patent
// or patent rights of NVIDIA Corporation. Details are subject to change without notice.
// This code supersedes and replaces all information previously supplied.
// NVIDIA Corporation products are not authorized for use as critical
// components in life support devices or systems without express written approval of
// NVIDIA Corporation.
//
// Copyright (c) 2008-2012 NVIDIA Corporation. All rights reserved.
// Copyright (c) 2004-2008 AGEIA Technologies, Inc. All rights reserved.
// Copyright (c) 2001-2004 NovodeX AG. All rights reserved.  


#ifndef PX_PHYSICS_GEOMUTILS_VEC_SPHERE_H
#define PX_PHYSICS_GEOMUTILS_VEC_SPHERE_H
/** \addtogroup geomutils
@{
*/

#include "GuVecConvex.h"
#include "GuConvexSupportTable.h"

/**
\brief Represents a sphere defined by its center point and radius.
*/
namespace physx
{
namespace Gu
{
	class SphereV : public ConvexV
	{
	public:
		/**
		\brief Constructor
		*/
		PX_INLINE SphereV(): ConvexV(E_SPHERE)
		{
			radius = Ps::aos::FZero();
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}

		PX_INLINE SphereV(const Ps::aos::Vec3VArg _center, const Ps::aos::FloatV _radius): ConvexV(E_SPHERE, _center)
		{
			radius = _radius;
			margin = _radius;
			//margin = Ps::aos::PxF32_From_FloatV(_radius);
			//eps = margin * 0.1f;
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}


		/*
			Margin should be the same as radius
		*/
		PX_INLINE SphereV(const Ps::aos::Vec3VArg _center, const Ps::aos::FloatVArg _radius, const Ps::aos::FloatVArg _margin): ConvexV(E_SPHERE, _center, _margin)
		{
			radius = _radius;
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}

		/**
		\brief Copy constructor
		*/
		PX_INLINE SphereV(const SphereV& sphere) : ConvexV(E_SPHERE, sphere.center, sphere.margin), radius(sphere.radius)
		{
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}

	/*	PX_INLINE SphereV(const Sphere& sphere)
		{
			using namespace Ps::aos;
			const Vec3V center = Vec3V_From_PxVec3(sphere.center);
			const FloatV radius = FloatV_From_F32(sphere.radius);
			centerRadius = V4SetW(center, radius);
		}*/
		/**
		\brief Destructor
		*/
		PX_INLINE ~SphereV()
		{
		}

		PX_INLINE	void	setV(const Ps::aos::Vec3VArg _center, const Ps::aos::FloatVArg _radius)		
		{ 
			center = _center;
			radius = _radius;
		}

		/**
		\brief Checks the sphere is valid.

		\return		true if the sphere is valid
		*/
		PX_INLINE bool isValid() const
		{
			// Consistency condition for spheres: Radius >= 0.0f
			using namespace Ps::aos;
			return BAllEq(FIsGrtrOrEq(radius, FZero()), BTTTT()) != 0;
		}

		/**
		\brief Tests if a point is contained within the sphere.

		\param[in] p the point to test
		\return	true if inside the sphere
		*/
		PX_INLINE bool contains(const Ps::aos::Vec3VArg p) const
		{
			using namespace Ps::aos;
			const FloatV rr = FMul(radius, radius);
			const FloatV cc =  V3LengthSq(V3Sub(center, p));
			//return (center-p).magnitudeSquared() <= radius*radius;
			return FAllGrtrOrEq(rr, cc) != 0;
		}

		/**
		\brief Tests if a sphere is contained within the sphere.

		\param		sphere	[in] the sphere to test
		\return		true if inside the sphere
		*/
		PX_INLINE bool contains(const SphereV& sphere)	const
		{
			using namespace Ps::aos;
			
			const Vec3V centerDif= V3Sub(center, sphere.center);
			const FloatV radiusDif = FSub(radius, sphere.radius);
			const FloatV cc = V3Dot(centerDif, centerDif);
			const FloatV rr = FMul(radiusDif, radiusDif); 

			const BoolV con0 = FIsGrtrOrEq(radiusDif, FZero());//might contain
			const BoolV con1 = FIsGrtr(rr, cc);//return true
			return BAllEq(BAnd(con0, con1), BTTTT())==1;
			
			//// If our radius is the smallest, we can't possibly contain the other sphere
			//if(radius < sphere.radius)	return false;
			//// So r is always positive or null now
			//const PxF32 r = radius - sphere.radius;
			//const PxF32 rr = r*r;
			//const FloatV rrV = FloatV_From_F32(rr);
			////return (center - sphere.center).magnitudeSquared() <= r*r;
			//return FAllGrtrOrEq(rrV, V3LengthSq(V3Sub(center, sphere.center))) == 1;


		}

		/**
		\brief Tests if a box is contained within the sphere.

		\param		minimum		[in] minimum value of the box
		\param		maximum		[in] maximum value of the box
		\return		true if inside the sphere
		*/
		PX_INLINE bool contains(const Ps::aos::Vec3VArg minimum, const Ps::aos::Vec3VArg maximum) const
		{
		
			//compute the sphere which wrap around the box
			using namespace Ps::aos;
			const FloatV zero = FZero();
			const FloatV half = FHalf();

			const Vec3V boxSphereCenter = V3Scale(V3Add(maximum, minimum), half);
			const Vec3V v = V3Scale(V3Sub(maximum, minimum), half);
			const FloatV boxSphereR = V3Length(v);

			const Vec3V w = V3Sub(center, boxSphereCenter);
			const FloatV wLength = V3Length(w);
			const FloatV dif = FSub(FSub(radius, wLength), boxSphereR); 

			return FAllGrtrOrEq(dif, zero) != 0;
		}           
                  
		/**
		\brief Tests if the sphere intersects another sphere

		\param		sphere	[in] the other sphere
		\return		true if spheres overlap
		*/
		PX_INLINE bool intersect(const SphereV& sphere) const
		{
			using namespace Ps::aos;
			const Vec3V centerDif = V3Sub(center, sphere.center);
			const FloatV cc = V3Dot(centerDif, centerDif);
			const FloatV r = FAdd(radius, sphere.radius);
			const FloatV rr = FMul(r, r);
			return FAllGrtrOrEq(rr, cc) != 0;
		}

		/*PX_FORCE_INLINE Support getSupportMapping()const
		{
			return SphereSupport;
		}

		PX_FORCE_INLINE SupportMargin getSupportMarginMapping()const
		{
			return SphereSupportMargin;
		}*/

		/*
			the direction need to be normalized  
		*/
		//PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir)const
		//{
		//	using namespace Ps::aos;
		//	const Vec3V _dir = V3Normalize(dir);
		//	return V3ScaleAdd(_dir, radius, center);  
		///*	const Vec3V mulRadius = V3Mul(dir, radius);
		//	const FloatV recipLength = V3RecipLengthFast(dir);
		//	return V3ScaleAdd(mulRadius, recipLength, center);*/
		//}
  //  
		//sweep code need to have full version
		PX_FORCE_INLINE Ps::aos::Vec3V supportSweep(const Ps::aos::Vec3VArg dir)const
		{
			using namespace Ps::aos;
			const Vec3V _dir = V3Normalize(dir);
			return V3ScaleAdd(_dir, radius, center);  
		}

		//make the support function the same as support margin
		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir)const
		{
			return center;//_margin is the same as radius
		}
  

		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin, Ps::aos::Vec3V& support)const
		{
			support = center;
			return center;//_margin is the same as radius
		}

		PX_FORCE_INLINE bool isMarginEqRadius()const
		{
			return true;
		}

#ifndef __SPU__
		PX_FORCE_INLINE Ps::aos::BoolV isMarginEqRadiusV()const
		{
			return Ps::aos::BTTTT();
		}
#endif


		Ps::aos::FloatV radius;		//!< Sphere's center, w component is radius

	};
}

}

#endif