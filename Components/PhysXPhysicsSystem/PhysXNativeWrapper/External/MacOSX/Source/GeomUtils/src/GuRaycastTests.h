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


#ifndef PX_PHYSICS_GEOMUTILS_PX_RAYCASTTESTS
#define PX_PHYSICS_GEOMUTILS_PX_RAYCASTTESTS

#include "CmPhysXCommon.h"
#include "PxPhysXCommon.h"  // needed for dll export
#include "PxSimpleTypes.h"
#include "PxSceneQueryReport.h"

namespace physx
{

class PxGeometry;
class PxBoxGeometry;
class PxCapsuleGeometry;
class PxSphereGeometry;
class PxPlaneGeometry;
class PxConvexMeshGeometry;
class PxTriangleMeshGeometry;
class PxHeightFieldGeometry;
struct PxRaycastHit;

namespace Gu
{
	PX_PHYSX_COMMON_API PxU32 raycast_box(
		const PxGeometry& boxGeom, const PxTransform& pose,
		const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist,
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);
	PX_PHYSX_COMMON_API PxU32 raycast_sphere(
		const PxGeometry& sphereGeom,
		const PxTransform& pose, const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist,
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);
	PX_PHYSX_COMMON_API PxU32 raycast_capsule(
		const PxGeometry& capsuleGeom, const PxTransform& pose,
		const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist,
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);
	PX_PHYSX_COMMON_API PxU32 raycast_plane(
		const PxGeometry& planeGeom, const PxTransform& pose,
		const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist,
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);
	PX_PHYSX_COMMON_API PxU32 raycast_convexMesh(
		const PxGeometry& convexMeshGeom, const PxTransform& pose,
		const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist,
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);
	PX_PHYSX_COMMON_API PxU32 raycast_triangleMesh(
		const PxGeometry& triangleMeshGeom,	const PxTransform& pose,
		const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist,
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);
	PX_PHYSX_COMMON_API PxU32 raycast_heightField(
		const PxGeometry& heightFieldGeom, const PxTransform& pose,
		const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist, // rayDir will be normalized, so length doesn't matter
		PxSceneQueryFlags hintFlags, PxU32 maxHits, PxRaycastHit* PX_RESTRICT hits, bool firstHit = false);

	typedef PxU32 (*RaycastFunc) (
		const PxGeometry&, const PxTransform&,
		const PxVec3&, const PxVec3&, PxReal,
		PxSceneQueryFlags, PxU32, PxRaycastHit* PX_RESTRICT, bool);

	PX_PHYSX_COMMON_API RaycastFunc GetRaycastFunc(PxU16 type);

	extern const RaycastFunc gRaycastMap[7];

}  // namespace Gu

}

#endif
