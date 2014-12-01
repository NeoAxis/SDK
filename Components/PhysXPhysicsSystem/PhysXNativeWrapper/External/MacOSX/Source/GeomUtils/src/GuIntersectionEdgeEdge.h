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


#ifndef PX_INTERSECTION_EDGE_EDGE_H
#define PX_INTERSECTION_EDGE_EDGE_H

#include "PxPhysXCommon.h"  // for PX_PHYSX_COMMON_API
#include "CmPhysXCommon.h"
#include "PsVecMath.h"

namespace physx
{
namespace Gu
{

	// collide edge (p1,p2) moving in direction (dir) colliding
	// width edge (p3,p4). Return true on a collision with
	// collision distance (dist) and intersection point (ip)
	// note: dist and ip are invalid if function returns false.
	// note: ip is on (p1,p2), not (p1+dist*dir,p2+dist*dir)
	PX_PHYSX_COMMON_API bool intersectEdgeEdge(const PxVec3& p1, const PxVec3& p2, const PxVec3& dir, const PxVec3& p3, const PxVec3& p4, PxReal& dist, PxVec3& ip);

	// PT: the new code doesn't return the same "ip" as before. Disabled from now.
	// csigg: had a bug, fixed now. Does return the same "ip" as above, but faster (please verify) and without precision issues.
	// please also apply this change in other places where the implementation above has been copied to.
	PX_PHYSX_COMMON_API bool intersectEdgeEdgeNEW(const PxVec3& p1, const PxVec3& p2, const PxVec3& dir, const PxVec3& p3, const PxVec3& p4, PxReal& dist, PxVec3& ip);

	PX_PHYSX_COMMON_API bool intersectEdgeEdge(const Ps::aos::Vec3VArg p1, const Ps::aos::Vec3VArg p2, const Ps::aos::Vec3VArg dir, const Ps::aos::Vec3VArg p3, const Ps::aos::Vec3VArg p4, Ps::aos::FloatV& dist, Ps::aos::Vec3V& ip);

	PX_PHYSX_COMMON_API Ps::aos::BoolV intersectEdgeEdge4(	const Ps::aos::Vec3VArg p1, const Ps::aos::Vec3VArg p2, const Ps::aos::Vec3VArg dir, 
															const Ps::aos::Vec3VArg a0, const Ps::aos::Vec3VArg b0, 
															const Ps::aos::Vec3VArg a1, const Ps::aos::Vec3VArg b1,
															const Ps::aos::Vec3VArg a2, const Ps::aos::Vec3VArg b2,
															const Ps::aos::Vec3VArg a3, const Ps::aos::Vec3VArg b3,
															Ps::aos::Vec4V& dist, Ps::aos::Vec3V* ip);
} // namespace Gu

}

#endif