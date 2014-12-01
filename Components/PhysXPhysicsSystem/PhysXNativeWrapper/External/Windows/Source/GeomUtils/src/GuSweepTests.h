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


#ifndef PX_PHYSICS_GEOMUTILS_PX_SWEEPTESTS
#define PX_PHYSICS_GEOMUTILS_PX_SWEEPTESTS

#include "CmPhysXCommon.h"
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
class PxMeshScale;
class PxTriangle;
struct PxSweepHit;

namespace Gu
{
	class Capsule;
	class Box;

	// PT: TODO: those useless functions shouldn't be exposed
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxSphereGeometry& sphereGeom,			const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxPlaneGeometry& planeGeom,				const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxCapsuleGeometry& capsuleGeom,			const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxBoxGeometry& boxGeom,					const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxConvexMeshGeometry& convexGeom,		const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxTriangleMeshGeometry& triMeshGeom,	const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepCapsule(const PxHeightFieldGeometry& heightFieldGeom,	const PxTransform& pose, const Gu::Capsule& lss, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);

	PX_PHYSX_COMMON_API bool sweepBox(const PxSphereGeometry& sphereGeom,			const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepBox(const PxPlaneGeometry& planeGeom,				const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepBox(const PxCapsuleGeometry& capsuleGeom,			const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepBox(const PxBoxGeometry& capsuleGeom,				const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepBox(const PxConvexMeshGeometry& convexGeom,		const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepBox(const PxTriangleMeshGeometry& triMeshGeom,	const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API bool sweepBox(const PxHeightFieldGeometry& heightFieldGeom,	const PxTransform& pose, const Gu::Box& box, const PxVec3& unitDir, const PxReal distance, PxSweepHit& sweepHit, PxSceneQueryFlags hintFlags);

	typedef bool (*SweepCapsuleFunc) (const PxGeometry&, const PxTransform&, const Gu::Capsule&, const PxVec3& unitDir, const PxReal distance, PxSweepHit&, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API const SweepCapsuleFunc* GetSweepCapsuleMap();
	extern const SweepCapsuleFunc gSweepCapsuleMap[7];

	typedef bool (*SweepBoxFunc) (const PxGeometry&, const PxTransform&, const Gu::Box&, const PxVec3& unitDir, const PxReal distance, PxSweepHit&, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API const SweepBoxFunc* GetSweepBoxMap();
	extern const SweepBoxFunc gSweepBoxMap[7];

	typedef bool (*SweepConvexFunc) (const PxGeometry&, const PxTransform&, const PxConvexMeshGeometry&, const PxTransform&, const PxVec3& unitDir, const PxReal distance, PxSweepHit&, PxSceneQueryFlags hintFlags);
	PX_PHYSX_COMMON_API const SweepConvexFunc* GetSweepConvexMap();
	extern const SweepConvexFunc gSweepConvexMap[7];

	// For sweep vs. triangle list: PxGeometryQuery::sweep()
	PX_PHYSX_COMMON_API bool SweepCapsuleTriangles(PxU32 nb_tris, const PxTriangle* triangles,
								const PxCapsuleGeometry& capsuleGeom, const PxTransform& capsulePose,
								const PxVec3& unitDir, const PxReal distance, const PxU32* cachedIndex, PxVec3& hit,
								PxVec3& normal, PxReal& d, PxU32& index);

	// For sweep vs. triangle list: PxGeometryQuery::sweep()
	PX_PHYSX_COMMON_API bool SweepBoxTriangles(PxU32 nb_tris, const PxTriangle* triangles,
							const PxBoxGeometry& boxGeom, const PxTransform& boxPose, const PxVec3& unitDir, const PxReal distance, 
							PxVec3& _hit, PxVec3& _normal, float& _d, PxU32& _index, const PxU32* cachedIndex);

}  // namespace Gu

}

#endif
