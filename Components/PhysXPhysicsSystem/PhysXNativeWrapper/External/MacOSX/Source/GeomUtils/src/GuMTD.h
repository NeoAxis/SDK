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

#ifndef PX_PHYSICS_GEOMUTILS_GU_MTD
#define PX_PHYSICS_GEOMUTILS_GU_MTD

#include "PxPhysXCommon.h"

namespace physx
{
	class PxConvexMeshGeometry;
	class PxTriangleMeshGeometry;
	class PxPlane;

namespace Gu
{
	class Box;
	class Sphere;
	class Capsule;

	PX_PHYSX_COMMON_API	bool	computeMTD_SphereSphere		(PxVec3& mtd, PxF32& depth, const Sphere& sphere0, const Sphere& sphere1);
	PX_PHYSX_COMMON_API	bool	computeMTD_SphereCapsule	(PxVec3& mtd, PxF32& depth, const Sphere& sphere, const Capsule& capsule);
	PX_PHYSX_COMMON_API	bool	computeMTD_SphereBox		(PxVec3& mtd, PxF32& depth, const Sphere& sphere, const Box& box);
	PX_PHYSX_COMMON_API	bool	computeMTD_CapsuleCapsule	(PxVec3& mtd, PxF32& depth, const Capsule& capsule0, const Capsule& capsule1);
	PX_PHYSX_COMMON_API	bool	computeMTD_BoxCapsule		(PxVec3& mtd, PxF32& depth, const Box& box, const Capsule& capsule);
	PX_PHYSX_COMMON_API	bool	computeMTD_BoxBox			(PxVec3& mtd, PxF32& depth, const Box& box0, const Box& box1);

	PX_PHYSX_COMMON_API	bool	computeMTD_SphereConvex		(PxVec3& mtd, PxF32& depth, const Sphere& sphere, const PxConvexMeshGeometry& convexGeom, const PxTransform& convexPose);
	PX_PHYSX_COMMON_API	bool	computeMTD_BoxConvex		(PxVec3& mtd, PxF32& depth, const Box& box, const PxConvexMeshGeometry& convexGeom, const PxTransform& convexPose);
	PX_PHYSX_COMMON_API	bool	computeMTD_CapsuleConvex	(PxVec3& mtd, PxF32& depth, const Capsule& capsule, const PxConvexMeshGeometry& convexGeom, const PxTransform& convexPose);
	PX_PHYSX_COMMON_API	bool	computeMTD_ConvexConvex		(PxVec3& mtd, PxF32& depth, const PxConvexMeshGeometry& convexGeom0, const PxTransform& convexPose0, const PxConvexMeshGeometry& convexGeom1, const PxTransform& convexPose1);

	PX_PHYSX_COMMON_API	bool	computeMTD_PlaneSphere		(PxVec3& mtd, PxF32& depth, const PxPlane& plane, const Sphere& sphere);
	PX_PHYSX_COMMON_API	bool	computeMTD_PlaneBox			(PxVec3& mtd, PxF32& depth, const PxPlane& plane, const Box& box);
	PX_PHYSX_COMMON_API	bool	computeMTD_PlaneCapsule		(PxVec3& mtd, PxF32& depth, const PxPlane& plane, const Capsule& capsule);
	PX_PHYSX_COMMON_API	bool	computeMTD_PlaneConvex		(PxVec3& mtd, PxF32& depth, const PxPlane& plane, const PxConvexMeshGeometry& convexGeom, const PxTransform& convexPose);

	PX_PHYSX_COMMON_API	bool	computeMTD_SphereMesh		(PxVec3& mtd, PxF32& depth, const Sphere& sphere, const PxTriangleMeshGeometry& meshGeom, const PxTransform& meshPose);
	PX_PHYSX_COMMON_API	bool	computeMTD_BoxMesh			(PxVec3& mtd, PxF32& depth, const Box& box, const PxTriangleMeshGeometry& meshGeom, const PxTransform& meshPose);
	PX_PHYSX_COMMON_API	bool	computeMTD_CapsuleMesh		(PxVec3& mtd, PxF32& depth, const Capsule& capsule, const PxTriangleMeshGeometry& meshGeom, const PxTransform& meshPose);
	PX_PHYSX_COMMON_API	bool	computeMTD_ConvexMesh		(PxVec3& mtd, PxF32& depth, const PxConvexMeshGeometry& convexGeom, const PxTransform& convexPose, const PxTriangleMeshGeometry& meshGeom, const PxTransform& meshPose);
}
}

#endif
