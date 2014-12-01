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


#ifndef PX_INTERSECTION_BOX_BOX_H
#define PX_INTERSECTION_BOX_BOX_H

#include "PxMat33.h"
#include "GuBox.h"
#include "GuVecBox.h"
#include "GuBounds3.h"
#include "PxBounds3.h"

namespace physx
{
namespace Gu
{

	class PxBounds3V;
	class PxBoxV;
	PX_PHYSX_COMMON_API bool intersectOBBOBB(const PxVec3& e0, const PxVec3& c0, const PxMat33& r0, const PxVec3& e1, const PxVec3& c1, const PxMat33& r1, bool full_test);

	PX_PHYSX_COMMON_API bool intersectOBBOBB(const Ps::aos::Vec3VArg e0, const Ps::aos::Vec3VArg c0, const Ps::aos::Mat33V& r0, const Ps::aos::Vec3VArg e1, const Ps::aos::Vec3VArg c1, const Ps::aos::Mat33V& r1, bool full_test);

	PX_INLINE bool intersectOBBAABB(const Gu::Box& obb, const PxBounds3& aabb)
	{
		PxVec3 center = aabb.getCenter();
		PxVec3 extents = aabb.getExtents();
		return intersectOBBOBB(obb.extents, obb.center, obb.rot, extents, center, PxMat33::createIdentity(), true);
	}

	PX_INLINE bool intersectOBBAABB(const Gu::BoxV& obb, const Gu::PxBounds3V& aabb)
	{
		using namespace Ps::aos;
		Vec3V center = aabb.getCenter();
		Vec3V extents = aabb.getExtents();
		return intersectOBBOBB(obb.extents, obb.getCenter(), obb.rot, extents, center, M33Identity(), true);
	}

} // namespace Gu

}

#endif