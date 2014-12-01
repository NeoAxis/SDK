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


#ifndef PX_PHYSICS_GEOMUTILS_GEOMETRY_QUERY
#define PX_PHYSICS_GEOMUTILS_GEOMETRY_QUERY

/** \addtogroup geomutils
  @{
*/

#include "common/PxPhysXCommon.h"
#include "PxSceneQueryReport.h"

namespace physx
{

class PxGeometry;
struct PxSweepHit;

class PxTriangle;


namespace Gu
{

class GeometryQuery
{
public:

	/**
	\brief Sweep a specified geometry object in space and test for collision with a set of given triangles.

	\note Only the following geometry types are supported: PxSphereGeometry, PxCapsuleGeometry, PxBoxGeometry
	\note If a shape from the scene is already overlapping with the query shape in its starting position, behavior is controlled by the PxSceneQueryFlag::eINITIAL_OVERLAP flag.

	\param[in] unitDir Normalized direction of the sweep.
	\param[in] distance Sweep distance. Needs to be larger than 0.
	\param[in] geom The geometry object to sweep. Supported geometries are #PxSphereGeometry, #PxCapsuleGeometry and #PxBoxGeometry
	\param[in] pose Pose of the geometry object to sweep.
	\param[in] triangleCount Number of specified triangles
	\param[in] triangles Array of triangles to sweep against
	\param[out] sweepHit The sweep hit information. On hit, both faceID parameters will hold the index of the hit triangle. Only valid if this method returns true.
	\param[in] hintFlags Specification of the kind of information to retrieve on hit. Combination of #PxSceneQueryFlag flags
	\param[in] cachedIndex Cached triangle index for subsequent calls. Cached triangle is tested first. Optional parameter.
	\return True if the swept geometry object hits the specified triangles

	@see Triangle PxSweepHit PxGeometry PxTransform
	*/
	PX_PHYSX_COMMON_API static bool sweep(const PxVec3& unitDir,
							const PxReal distance,
							const PxGeometry& geom,
							const PxTransform& pose,
							PxU32 triangleCount,
							const PxTriangle* triangles,
							PxSweepHit& sweepHit,
							PxSceneQueryFlags hintFlags=(PxSceneQueryFlags)0xffffffff,
							const PxU32* cachedIndex = NULL);
};


}  // Gu

}  // physx

/** @} */
#endif
