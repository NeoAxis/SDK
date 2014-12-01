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


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Include Guard
#ifndef GU_CONVEXHULL_H
#define GU_CONVEXHULL_H

#include "PxPhysXCommon.h"
#include "PxVec3.h"

namespace physx
{
namespace Gu
{
	struct ConvexHullData;

	PX_PHYSX_COMMON_API void	initConvexHullData(ConvexHullData &data);
	PX_PHYSX_COMMON_API bool	convexHullContains(const ConvexHullData &data, const PxVec3& p);
	PX_PHYSX_COMMON_API PxVec3	projectHull_(
									const ConvexHullData &hull, float& minimum, float& maximum, 
									const PxVec3& localDir, const PxMat33& vert2ShapeSkew);
	PX_PHYSX_COMMON_API PxVec3	hullInverseSupportMapping(
									const ConvexHullData& hull, const PxVec3& point, int& featureCode, PxVec3& medianDir,
									const PxMat33& vert2ShapeSkew);
	PX_PHYSX_COMMON_API bool	hullInnerSphere(const ConvexHullData& hull, PxVec3& center, PxReal& radius);
}

}

#endif	// ICECONVEXHULL_H

