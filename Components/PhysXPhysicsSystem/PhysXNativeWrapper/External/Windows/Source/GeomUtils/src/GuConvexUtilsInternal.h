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


#ifndef PX_PHYSICS_GEOMUTILS_PX_CONVEX_UTILITIES_INTERNAL
#define PX_PHYSICS_GEOMUTILS_PX_CONVEX_UTILITIES_INTERNAL

#include "PxPhysXCommon.h"  // for PX_PHYSX_COMMON_API
#include "CmPhysXCommon.h"

namespace physx
{
class PxMeshScale;

namespace Cm
{
	class Matrix34;
	class FastVertex2ShapeScaling;
}

namespace Gu
{
	class Box;
	PX_PHYSX_COMMON_API void computeHullOBB(Gu::Box& hullOBB, const PxBounds3& hullAABB, float offset, const PxTransform& transform0, const Cm::Matrix34& world0, const Cm::Matrix34& world1, const Cm::FastVertex2ShapeScaling& meshScaling, bool idtScaleMesh);
	PX_PHYSX_COMMON_API void computeVertexSpaceOBB(Gu::Box& dst, const Gu::Box& src, const PxTransform& meshPose, const PxMeshScale& meshScale);

}  // namespace Gu

}

#endif
