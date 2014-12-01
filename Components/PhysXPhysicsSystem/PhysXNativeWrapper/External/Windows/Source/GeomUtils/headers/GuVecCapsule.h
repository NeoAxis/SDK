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


#ifndef PX_PHYSICS_GEOMUTILS_VEC_CAPSULE_H
#define PX_PHYSICS_GEOMUTILS_VEC_CAPSULE_H

/** \addtogroup geomutils
@{
*/

#include "GuVecConvex.h"   
#include "GuConvexSupportTable.h"

namespace physx
{
namespace Gu
{
	class CapsuleV : public ConvexV  
	{
	public:
		/**
		\brief Constructor
		*/

		PX_INLINE CapsuleV():ConvexV(E_CAPSULE)
		{
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}

		PX_INLINE CapsuleV(const Ps::aos::Vec3VArg p, const Ps::aos::FloatVArg _radius) : ConvexV(E_CAPSULE)
		{
			using namespace Ps::aos;
			center = p;
			radius = _radius;
			p0 = p;
			p1 = p;
			margin = _radius;
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}

		PX_INLINE CapsuleV(const Ps::aos::Vec3VArg _center, const Ps::aos::Vec3VArg _v, const Ps::aos::FloatVArg _radius) : ConvexV(E_CAPSULE)
		{
			using namespace Ps::aos;
			center = _center;
			radius = _radius;
			p0 = V3Add(_center, _v);
			p1 = V3Sub(_center, _v);
			margin = _radius;
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}


		PX_INLINE CapsuleV(const Ps::aos::Vec3VArg _center, const Ps::aos::Vec3VArg _v, const Ps::aos::FloatVArg _radius, const PxF32 _margin) : ConvexV(E_CAPSULE)
		{
			using namespace Ps::aos;
			center = _center;
			radius = _radius;
			p0 = V3Add(_center, _v);
			p1 = V3Sub(_center, _v);
			margin = _radius;
			
#ifdef __SPU__
			bMarginIsRadius = true;
#endif
		}

		/**
		\brief Constructor

		\param _radius Radius of the capsule.
		*/

		/**
		\brief Destructor
		*/
		PX_INLINE ~CapsuleV()
		{
		}

		PX_INLINE Ps::aos::Vec3V computeDirection() const
		{
			return Ps::aos::V3Sub(p1, p0);
		}

		
		PX_FORCE_INLINE	Ps::aos::FloatV	getRadius()	const
		{
			return radius;
		}


		PX_FORCE_INLINE Support getSupportMapping()const
		{
			return CapsuleSupport;
		}

		PX_FORCE_INLINE SupportMargin getSupportMarginMapping()const
		{
			return CapsuleSupportMargin;
		}

		PX_FORCE_INLINE Support getSweepSupportMapping()const
		{
			return CapsuleSweepSupport;
		}


		PX_FORCE_INLINE void setCenter(const Ps::aos::Vec3VArg _center)
		{
			using namespace Ps::aos;
			Vec3V offset = V3Sub(_center, center);
			center = _center;

			p0 = V3Add(p0, offset);
			p1 = V3Add(p1, offset);

		}

		PX_FORCE_INLINE Ps::aos::Vec3V supportSweep(const Ps::aos::Vec3VArg dir)const
		{
			using namespace Ps::aos;
			const Vec3V _dir = V3Normalize(dir);
			const FloatV dist0 = V3Dot(p0, dir);
			const FloatV dist1 = V3Dot(p1, dir);
			const Vec3V p = V3Sel(FIsGrtr(dist0, dist1), p0, p1);
			return V3ScaleAdd(_dir, radius, p);
		}


		//treat the support function and the support margin as the same
		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir)const
		{
			using namespace Ps::aos;
			const FloatV dist0 = V3Dot(p0, dir);
			const FloatV dist1 = V3Dot(p1, dir);
			return V3Sel(FIsGrtr(dist0, dist1), p0, p1);
		} 


		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin, Ps::aos::Vec3V& support)const 
		{
			using namespace Ps::aos;

			const FloatV dist0 = V3Dot(p0, dir);
			const FloatV dist1 = V3Dot(p1, dir);
			const Vec3V ret = V3Sel(FIsGrtr(dist0, dist1), p0, p1);
			support = ret;
			return ret;
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

		
		Ps::aos::Vec3V	p0;		//!< Start of segment
		Ps::aos::Vec3V	p1;		//!< End of segment
		Ps::aos::FloatV	radius;
	};
}

}

#endif