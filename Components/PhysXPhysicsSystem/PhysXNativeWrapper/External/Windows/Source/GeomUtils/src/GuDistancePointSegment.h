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


#ifndef PX_DISTANCE_POINT_SEGMENT_H
#define PX_DISTANCE_POINT_SEGMENT_H

#include "PxPhysXCommon.h"
#include "GuSegment.h"
#include "PsVecMath.h"

namespace physx
{
namespace Gu
{

	PX_PHYSX_COMMON_API PxReal distancePointSegmentSquared(const PxVec3& p0, const PxVec3& p1, const PxVec3& point, PxReal* param=NULL);

	PX_PHYSX_COMMON_API Ps::aos::FloatV distancePointSegmentSquared(const Ps::aos::Vec3VArg p0, const Ps::aos::Vec3VArg p1, const Ps::aos::Vec3VArg point, Ps::aos::FloatV& param);

	//Ps::aos::FloatV distancePointSegmentTValue(const Ps::aos::Vec3VArg p0, const Ps::aos::Vec3VArg p1, const Ps::aos::Vec3VArg point);

	PX_INLINE PxReal distancePointSegmentSquared(const Gu::Segment& segment, const PxVec3& point, PxReal* param=NULL)
	{
		return  distancePointSegmentSquared(segment.p0, segment.p1, point, param);

	}

	/*PX_INLINE Ps::aos::FloatV distancePointSegmentSquared(const Gu::SegmentV& segment, const Ps::aos::Vec3V& point, Ps::aos::FloatV& t)
	{
		return distancePointSegmentSquared(segment.p0, segment.p1, point, t);
	}*/      

	PX_FORCE_INLINE Ps::aos::FloatV distancePointSegmentTValue(const Ps::aos::Vec3VArg a, const Ps::aos::Vec3VArg b, const Ps::aos::Vec3VArg p)
	{
		using namespace Ps::aos;
		const FloatV zero = FZero();
		const Vec3V ap = V3Sub(p, a);
		const Vec3V ab = V3Sub(b, a);
		const FloatV nom = V3Dot(ap, ab);
		
		const FloatV denom = V3Dot(ab, ab);
		const FloatV tValue = FMul(nom, FRecip(denom));
		return FSel(FIsEq(denom, zero), zero, tValue);
	}



	//Calculates the t value (a0,b0) -> p0, (a0,b0) -> p1, (a1,b1) ->p2, (a1,b1) -> p3 and returns as 
	//elements x,y,z,w in return result respectively
	PX_FORCE_INLINE Ps::aos::Vec4V distancePointSegmentTValue22(const Ps::aos::Vec3VArg a0, const Ps::aos::Vec3VArg b0, 
																	const Ps::aos::Vec3VArg a1, const Ps::aos::Vec3VArg b1,
																	const Ps::aos::Vec3VArg p0, const Ps::aos::Vec3VArg p1,
																	const Ps::aos::Vec3VArg p2, const Ps::aos::Vec3VArg p3)
	{
		using namespace Ps::aos;
		const Vec4V zero = V4Zero();
		const Vec3V ap00 = V3Sub(p0, a0);
		const Vec3V ap10 = V3Sub(p1, a0);
		const Vec3V ap01 = V3Sub(p2, a1);
		const Vec3V ap11 = V3Sub(p3, a1);

		const Vec3V ab0 = V3Sub(b0, a0);
		const Vec3V ab1 = V3Sub(b1, a1);

		const FloatV nom00 = V3Dot(ap00, ab0);
		const FloatV nom10 = V3Dot(ap10, ab0);
		const FloatV nom01 = V3Dot(ap01, ab1);
		const FloatV nom11 = V3Dot(ap11, ab1);
		
		const FloatV denom0 = V3Dot(ab0, ab0);
		const FloatV denom1 = V3Dot(ab1, ab1);

		const Vec4V nom = V4Merge(nom00, nom10, nom01, nom11);
		const Vec4V denom = V4Merge(denom0, denom0, denom1, denom1);

		const Vec4V recip = V4Recip(denom);
		const Vec4V tValue = V4Mul(nom, recip);
		return V4Sel(V4IsEq(denom, zero), zero, tValue);
	}

	PX_PHYSX_COMMON_API Ps::aos::Vec4V distancePointSegmentSquared(	const Ps::aos::Vec3VArg a, const Ps::aos::Vec3VArg b, 
																	const Ps::aos::Vec3VArg p0, const Ps::aos::Vec3VArg p1,
																	const Ps::aos::Vec3VArg p2, const Ps::aos::Vec3VArg p3,
																	Ps::aos::Vec4V& param);

} // namespace Gu

}

#endif
