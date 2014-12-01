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


#ifndef PX_PHYSICS_GEOMUTILS_PX_UTILITIES_INTERNAL
#define PX_PHYSICS_GEOMUTILS_PX_UTILITIES_INTERNAL

#include "CmPhysXCommon.h"
#include "PsFPU.h"
#include "GuSphere.h"
#include "PxTriangle.h"
#include "PxBounds3.h"

#include "PxPhysXGeomUtils.h"
#include "PsVecMath.h"

namespace physx
{

class PxCapsuleGeometry;
class PxTriangleMeshGeometry;

namespace Cm
{
	class Matrix34;
}

namespace Gu
{
	class Plane;
	class PlaneV;
	class Capsule;
	class CapsuleV;
	class Box;
	class BoxV;
	class Segment;
	class SegmentV;      

	PX_PHYSX_COMMON_API const PxF32*	getBoxVertexNormals();
	PX_PHYSX_COMMON_API const PxU8*		getBoxTriangles();
	PX_PHYSX_COMMON_API const PxVec3*	getBoxLocalEdgeNormals();
	PX_PHYSX_COMMON_API const PxU8*		getBoxEdges();

	PX_PHYSX_COMMON_API void			computeBoxPoints(const PxBounds3& bounds, PxVec3* PX_RESTRICT pts);
	PX_PHYSX_COMMON_API void			computeBoundsAroundVertices(PxBounds3& bounds, PxU32 nbVerts, const PxVec3* PX_RESTRICT verts);

	PX_PHYSX_COMMON_API void			computeBoxWorldEdgeNormal(const Box& box, PxU32 edge_index, PxVec3& world_normal);
	PX_PHYSX_COMMON_API void			computeBoxAroundCapsule(const Capsule& capsule, Box& box);  //TODO: Refactor this one out in the future
	PX_PHYSX_COMMON_API void			computeBoxAroundCapsule(const CapsuleV& capsule, BoxV& box);  //TODO: Refactor this one out in the future
	PX_PHYSX_COMMON_API void			computeBoxAroundCapsule(const PxCapsuleGeometry& capsuleGeom, const PxTransform& capsulePose, Box& box);

	// PT: please don't use names like "worldCapsule" - nothing tells us the capsule is in world space
	PX_PHYSX_COMMON_API void			getSegment(Gu::Segment& segment, const PxCapsuleGeometry& capsuleGeom, const PxTransform& pose);
	PX_PHYSX_COMMON_API void			getSegment(Gu::SegmentV& segment, const PxCapsuleGeometry& capsuleGeom, const PxTransform& pose);
	PX_PHYSX_COMMON_API void			getCapsule(Gu::Capsule& capsule, const PxCapsuleGeometry& capsuleGeom, const PxTransform& pose);
	PX_PHYSX_COMMON_API PxPlane		getPlane(const PxTransform& pose);
	PX_PHYSX_COMMON_API Gu::PlaneV      getPlaneV(const PxTransform& pose);
	PX_PHYSX_COMMON_API PxTransform		getWorldTransform(const Gu::Capsule& worldCapsule, PxReal& halfHeight);

	PX_INLINE void computeBasis(const PxVec3& dir, PxVec3& right, PxVec3& up)
	{
		// Derive two remaining vectors
		if(dir.y>0.9999f)
		{
			right = PxVec3(1.0f, 0.0f, 0.0f);
		}
		else
		{
			right = (PxVec3(0.0f, 1.0f, 0.0f).cross(dir));
			right.normalize();
		}

		up = dir.cross(right);
	}

	PX_INLINE void computeBasis(const PxVec3& p0, const PxVec3& p1, PxVec3& dir, PxVec3& right, PxVec3& up)
	{
		// Compute the new direction vector
		dir = p1 - p0;
		dir.normalize();

		// Derive two remaining vectors
		computeBasis(dir, right, up);
	}

	PX_FORCE_INLINE void basisExtent(PxBounds3& dest, const PxVec3& center, const PxMat33& basis, const PxVec3& extent)
	{
		// extended basis vectors
		const PxVec3 c0 = basis.column0 * extent.x;
		const PxVec3 c1 = basis.column1 * extent.y;
		const PxVec3 c2 = basis.column2 * extent.z;

		// find combination of base vectors that produces max. distance for each component = sum of abs()
		const PxVec3 w(
			PxAbs(c0.x) + PxAbs(c1.x) + PxAbs(c2.x),
			PxAbs(c0.y) + PxAbs(c1.y) + PxAbs(c2.y),
			PxAbs(c0.z) + PxAbs(c1.z) + PxAbs(c2.z));

		dest = PxBounds3(center - w, center + w);
	}

	PX_FORCE_INLINE void transformNoEmptyTest(PxBounds3& dest, const PxMat33& matrix, const PxBounds3& bounds)
	{
		Gu::basisExtent(dest, matrix * bounds.getCenter(), matrix, bounds.getExtents());
	}

	PX_INLINE void computeBasis(const Ps::aos::Vec3VArg p0, const Ps::aos::Vec3VArg p1, Ps::aos::Vec3V& dir, Ps::aos::Vec3V& right, Ps::aos::Vec3V& up)
	{
		//Need to test
		using namespace Ps::aos;
		// Compute the new direction vector
		const FloatV eps = FloatV_From_F32(0.9999f);
		const Vec3V v = V3Sub(p1, p0);
		dir = V3Normalize(v);
		const BoolV con = FIsGrtr(V3GetY(dir), eps);
		const Vec3V w = V3Normalize(V3Cross(V3UnitY(), dir));
		right = V3Sel(con, V3UnitX(), w);
		up = V3Cross(dir, right);
	}


}  // namespace Gu

}

#endif
