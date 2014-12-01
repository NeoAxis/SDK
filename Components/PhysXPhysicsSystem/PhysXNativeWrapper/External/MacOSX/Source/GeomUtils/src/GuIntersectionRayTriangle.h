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


#ifndef PX_INTERSECTION_RAY_TRIANGLE_H
#define PX_INTERSECTION_RAY_TRIANGLE_H

#include "PxPhysXCommon.h"  // for PX_PHYSX_COMMON_API
#include "CmPhysXCommon.h"
#include "PsVecMath.h"

namespace physx
{

#define V_LOCAL_EPSILON 0.000001f

namespace Gu
{
	PX_PHYSX_COMMON_API bool intersectLineTriangleCulling(	const PxVec3& orig, 
															const PxVec3& dir, 
															const PxVec3& vert0, 
															const PxVec3& vert1, 
															const PxVec3& vert2, 
															PxReal& t,
															PxReal& u, 
															PxReal& v, 
															float enlarge=0.0f);

	PX_PHYSX_COMMON_API bool intersectLineTriangleNoCulling(const PxVec3& orig, 
															const PxVec3& dir, 
															const PxVec3& vert0, 
															const PxVec3& vert1, 
															const PxVec3& vert2, 
															PxReal& t,
															PxReal& u, 
															PxReal& v, 
															float enlarge=0.0f);

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/**
	*	Computes a ray-triangle intersection test.
	*	From Tomas Moeller's "Fast Minimum Storage Ray-Triangle Intersection"
	*	Could be optimized and cut into 2 methods (culled or not). Should make a batch one too to avoid the call overhead, or make it inline.
	*
	*	\param		orig	[in] ray origin
	*	\param		dir		[in] ray direction
	*	\param		vert0	[in] triangle vertex
	*	\param		vert1	[in] triangle vertex
	*	\param		vert2	[in] triangle vertex
	*	\param		t		[out] distance
	*	\param		u		[out] impact barycentric coordinate
	*	\param		v		[out] impact barycentric coordinate
	*	\param		cull	[in] true to use backface culling
	*	\param		enlarge [in] enlarge triangle by specified epsilon in UV space to avoid false near-edge rejections
	*	\return		true on overlap
	*/
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	PX_FORCE_INLINE bool intersectLineTriangle(const PxVec3& orig, 
								const PxVec3& dir, 
								const PxVec3& vert0, 
								const PxVec3& vert1, 
								const PxVec3& vert2, 
								PxReal& t,
								PxReal& u, 
								PxReal& v, 
								bool cull,
								float enlarge=0.0f)
	{
		return cull ?	intersectLineTriangleCulling(orig, dir, vert0, vert1, vert2, t, u, v, enlarge)
					:	intersectLineTriangleNoCulling(orig, dir, vert0, vert1, vert2, t, u, v, enlarge);
	}


	PX_INLINE bool intersectRayTriangle(	const Ps::aos::Vec3VArg orig, const Ps::aos::Vec3VArg dir,
											const Ps::aos::Vec3VArg vert0, 
											const Ps::aos::Vec3VArg vert1, 
											const Ps::aos::Vec3VArg vert2,
											Ps::aos::FloatV& t, 
											Ps::aos::FloatV& u, 
											Ps::aos::FloatV& v,
											const Ps::aos::BoolVArg backfaceCull, 
											const Ps::aos::FloatV enlarge = Ps::aos::FZero())
	{
		using namespace Ps::aos;

		const FloatV zero = FZero();
		const FloatV one = FOne();
		const FloatV eps = FloatV_From_F32(V_LOCAL_EPSILON);
		// Find vectors for two edges sharing vert0
		const Vec3V edge1 = V3Sub(vert1, vert0);
		const Vec3V edge2 = V3Sub(vert2, vert0);

		// Begin calculating determinant - also used to calculate U parameter
		const Vec3V pvec = V3Cross(dir, edge2);

		// If determinant is near zero, ray lies in plane of triangle
		const FloatV det = V3Dot(edge1, pvec);
		const FloatV detRecip = FRecip(det);
		const FloatV nEnlarge = FNeg(enlarge);

		// Calculate distance from vert0 to ray origin
		const Vec3V tvec = V3Sub(orig, vert0);
		const Vec3V qvec = V3Cross(tvec, edge1);
		const FloatV tmpU = V3Dot(tvec, pvec);
		const FloatV tmpV = V3Dot(dir, qvec);
		const FloatV tmpT = V3Dot(edge2, qvec);

		t = FMul(tmpT, detRecip);// Calculate t, scale parameters, ray intersects triangle
		u = FMul(tmpU, detRecip);// Calculate U parameter
		v = FMul(tmpV, detRecip);//Calculate V parameter

		const FloatV c0 = FSel(backfaceCull, det, FAbs(det));//return false
		const FloatV c1 = FSel(backfaceCull, det, one);
		const FloatV c2 = FSel(backfaceCull, tmpU, u);
		const FloatV c3 = FSel(backfaceCull, tmpV, v);

		const BoolV con0 = FIsGrtr(eps, c0);//return false
		const FloatV enlargeDet =  FAdd(c1, enlarge);
		const BoolV con1 = BOr(FIsGrtr(nEnlarge, c2), FIsGrtr(c2, enlargeDet));//return false
		const BoolV con2 = BOr(FIsGrtr(nEnlarge, c3), FIsGrtr(FAdd(c2, c3), enlargeDet));//return false
		const BoolV con3 = FIsGrtr(zero, tmpT);
		const BoolV bIntersect = BNot(BOr(BOr(con1, con2), BOr(con0, con3)));
		return BAllEq(bIntersect, BTTTT())==1;
	}


	PX_INLINE Ps::aos::BoolV intersectRayTriangle4(	const Ps::aos::Vec3VArg orig, const Ps::aos::Vec3VArg dir,
													const Ps::aos::Vec3VArg a0, 
													const Ps::aos::Vec3VArg b0, 
													const Ps::aos::Vec3VArg c0,
													const Ps::aos::Vec3VArg a1, 
													const Ps::aos::Vec3VArg b1, 
													const Ps::aos::Vec3VArg c1,
													const Ps::aos::Vec3VArg a2, 
													const Ps::aos::Vec3VArg b2, 
													const Ps::aos::Vec3VArg c2,
													const Ps::aos::Vec3VArg a3, 
													const Ps::aos::Vec3VArg b3, 
													const Ps::aos::Vec3VArg c3,
													Ps::aos::Vec4V& t, 
													Ps::aos::Vec4V& u, 
													Ps::aos::Vec4V& v,
													const Ps::aos::BoolVArg backfaceCull, 
													const Ps::aos::FloatV enlarge = Ps::aos::FZero())
	{
		using namespace Ps::aos;

		const Vec4V zero = V4Zero();
		const Vec4V one = V4One();
		const Vec4V eps = Vec4V_From_F32(V_LOCAL_EPSILON);
		// Find vectors for two edges sharing vert0
		const Vec3V ab0 = V3Sub(b0, a0);
		const Vec3V ac0 = V3Sub(c0, a0);

		const Vec3V ab1 = V3Sub(b1, a1);
		const Vec3V ac1 = V3Sub(c1, a1);

		const Vec3V ab2 = V3Sub(b2, a2);
		const Vec3V ac2 = V3Sub(c2, a2);

		const Vec3V ab3 = V3Sub(b3, a3);
		const Vec3V ac3 = V3Sub(c3, a3);

		// Begin calculating determinant - also used to calculate U parameter
		const Vec3V pvec0 = V3Cross(dir, ac0);
		const Vec3V pvec1 = V3Cross(dir, ac1);
		const Vec3V pvec2 = V3Cross(dir, ac2);
		const Vec3V pvec3 = V3Cross(dir, ac3);

		// If determinant is near zero, ray lies in plane of triangle
		FloatV det[4];
		det[0] = V3Dot(ab0, pvec0);
		det[1] = V3Dot(ab1, pvec1);
		det[2] = V3Dot(ab1, pvec2);
		det[3] = V3Dot(ab1, pvec3);
		const Vec4V detV = V4Merge(det);
		const Vec4V detRecipV = V4Recip(detV);
		const Vec4V nEnlargeV = FNeg(enlarge);
		
		// Calculate distance from vert0 to ray origin
		const Vec3V tvec0 = V3Sub(orig, a0);
		const Vec3V qvec0 = V3Cross(tvec0, ab0);

		const Vec3V tvec1 = V3Sub(orig, a1);
		const Vec3V qvec1 = V3Cross(tvec1, ab1);

		const Vec3V tvec2 = V3Sub(orig, a2);
		const Vec3V qvec2 = V3Cross(tvec2, ab2);

		const Vec3V tvec3 = V3Sub(orig, a3);
		const Vec3V qvec3 = V3Cross(tvec3, ab3);

		FloatV tmpUU[4], tmpVV[4], tmpTT[4];

		tmpUU[0] = V3Dot(tvec0, pvec0);
		tmpUU[1] = V3Dot(tvec1, pvec1);
		tmpUU[2] = V3Dot(tvec2, pvec2);
		tmpUU[3] = V3Dot(tvec3, pvec3);

		tmpVV[0] = V3Dot(dir, qvec0);
		tmpVV[1] = V3Dot(dir, qvec1);
		tmpVV[2] = V3Dot(dir, qvec2);
		tmpVV[3] = V3Dot(dir, qvec3);

		tmpTT[0] = V3Dot(ac0, qvec0);
		tmpTT[1] = V3Dot(ac1, qvec1);
		tmpTT[2] = V3Dot(ac2, qvec2);
		tmpTT[3] = V3Dot(ac3, qvec3);

		const Vec4V tmpT = V4Merge(tmpTT);
		const Vec4V tmpU = V4Merge(tmpUU);
		const Vec4V tmpV = V4Merge(tmpVV);

		t = V4Mul(tmpT, detRecipV);// Calculate t, scale parameters, ray intersects triangle
		u = V4Mul(tmpU, detRecipV);// Calculate U parameter
		v = V4Mul(tmpV, detRecipV);//Calculate V parameter

		const Vec4V tmp0 = V4Sel(backfaceCull, detV, V4Abs(detV));//return false
		const Vec4V tmp1 = V4Sel(backfaceCull, detV, one);
		const Vec4V tmp2 = V4Sel(backfaceCull, tmpU, u);
		const Vec4V tmp3 = V4Sel(backfaceCull, tmpV, v);

		const BoolV con0 = V4IsGrtr(eps, tmp0);//return false
		const Vec4V enlargeDet =  V4Add(tmp1, enlarge);
		const BoolV con1 = BOr(V4IsGrtr(nEnlargeV, tmp2), V4IsGrtr(tmp2, enlargeDet));//return false
		const BoolV con2 = BOr(V4IsGrtr(nEnlargeV, tmp3), V4IsGrtr(FAdd(tmp2, tmp3), enlargeDet));//return false
		const BoolV con3 = V4IsGrtr(zero, tmpT);
		return BNot(BOr(BOr(con1, con2), BOr(con0, con3)));
	}

} // namespace Gu

}

#endif
