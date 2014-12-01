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


#ifndef PX_INTERSECTION_CAPSULE_BOX_H
#define PX_INTERSECTION_CAPSULE_BOX_H

#include "PxBounds3.h"
#include "GuCapsule.h"
#include "GuDistanceSegmentBox.h"

namespace physx
{
namespace Gu
{
	PX_INLINE bool intersectCapsuleAABB(const Gu::Capsule& capsule, const PxBounds3& box)
	{
		const float r2 = capsule.radius*capsule.radius;

		PxVec3 center = box.getCenter();
		PxVec3 extents = box.getExtents();

		return distanceSegmentBoxSquared(capsule.p0, capsule.p1, center, extents, PxMat33::createIdentity()) < r2;
	}


	PX_INLINE bool intersectCapsuleAABB(const Gu::Capsule& capsule, const PxVec3& boxMin, const PxVec3& boxMax)
	{
		PxVec3 center = (boxMin + boxMax) * 0.5f, extents = (boxMax - boxMin) * 0.5f;
		return distanceSegmentBoxSquared(capsule.p0, capsule.p1, center, extents, PxMat33::createIdentity()) < capsule.radius * capsule.radius;
	}
} // namespace Gu

}

#endif
