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


#ifndef PX_GJKSWEEP_H
#define PX_GJKSWEEP_H

#include "PxVec3.h"
#include "PxTransform.h"
#include "PxPhysXCommon.h"
#include "CmSpatialVector.h"

namespace physx
{

class GJKConvexInterfaceCache
{
	//empty
};

class GJKConvexInterface
{
public:
	virtual			~GJKConvexInterface()	{}
	virtual void	getBounds(PxBounds3& bounds) const = 0;
	virtual PxVec3	projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const = 0;
	// featureCode=1 for face or smooth surface, 2 for edge, 3 for vertex. This is a count of how many "faces" the point is on.
	virtual PxVec3	inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const = 0;
	virtual bool	getInnerSphere(PxVec3& center, PxReal& radius) const = 0;
};


// destDistance result is always positive.
// If convexes intersect this functino returns false with all other return values undefined
PX_PHYSX_COMMON_API bool convexConvexDistance(
		const GJKConvexInterface& convexA, const GJKConvexInterface& convexB,
		const PxTransform& shape2worldA, const PxTransform& shape2worldB,
		PxVec3& sepAxisGuessInOut,
		PxVec3& destWorldNormalOnB, PxVec3& destWorldPointA, PxVec3& destWorldPointB, PxReal& destDistance, GJKConvexInterfaceCache& cache,
		PxU32* nbIter = NULL);

PX_PHYSX_COMMON_API bool convexConvexLinearSweep(
		const GJKConvexInterface& convexA, const GJKConvexInterface& convexB,
		const PxTransform& shape2worldA, const PxVec3& worldDestPosA,
		const PxTransform& shape2worldB, const PxVec3& worldDestPosB,
		PxReal distTreshold, PxVec3& destNormal, PxVec3& destWorldPointA,
		PxReal& toi);

#ifndef __SPU__
PX_PHYSX_COMMON_API bool convexConvexFullMotionSweep(
		const GJKConvexInterface& convexA, const GJKConvexInterface& convexB,
		const PxTransform& shape2worldA, const Cm::SpatialVector& motionA,
		const PxTransform& shape2worldB, const Cm::SpatialVector& motionB,
		PxReal distTreshold, PxVec3& destNormal, PxVec3& destPoint, PxReal& toi, GJKConvexInterfaceCache& cache);
#endif

}

#endif