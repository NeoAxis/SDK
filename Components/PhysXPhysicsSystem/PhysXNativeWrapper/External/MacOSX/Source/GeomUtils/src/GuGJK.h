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


#ifndef PX_GJK_H
#define PX_GJK_H


#include "GuConvexSupportTable.h"
#include "GuVecBox.h"
#include "GuGJKWrapper.h"
#include "GuEPA.h"
#include "GuGJKSimplex.h"

namespace physx
{
namespace Gu
{

	class ConvexV;


#ifndef	__SPU__
	/*
		
	*/
	//template<class ConvexA, class ConvexB>
	//PxGJKStatus _GJK(const ConvexA& a, const ConvexB& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist)
	//{
	//	using namespace Ps::aos;
	//	const Vec3V zeroV = V3Zero();
	//	const FloatV zero = FZero();
	//	const BoolV bTrue = BTTTT();
	//	PxU32 size=1;
	//	const Vec3V _initialSearchDir(V3Sub(a.getCenter(), b.getCenter()));

	//	const Vec3V initialSearchDir = V3Sel(FIsGrtr(V3Dot(_initialSearchDir, _initialSearchDir), zero), _initialSearchDir, V3UnitX());

	//	const Vec3V initialSupportA(a.support(V3Neg(initialSearchDir)));
	//	const Vec3V initialSupportB(b.support(initialSearchDir));
	//	 
	//	Vec3V Q[4] = {V3Sub(initialSupportA, initialSupportB), zeroV, zeroV, zeroV}; //simplex set
	//	Vec3V A[4] = {initialSupportA, zeroV, zeroV, zeroV}; //ConvexHull a simplex set
	//	Vec3V B[4] = {initialSupportB, zeroV, zeroV, zeroV}; //ConvexHull b simplex set
	//	 

	//	Vec3V v = Q[0];
	//	Vec3V supportA = initialSupportA;
	//	Vec3V supportB = initialSupportB;
	//	const FloatV eps1 = FloatV_From_F32(0.0001f);
	//	const FloatV eps2 = FMul(eps1, eps1);
	//	Vec3V closA(initialSupportA), closB(initialSupportB);
	//	FloatV sDist = V3Dot(v, v);
	//	FloatV minDist = sDist;
	//	Vec3V closAA = initialSupportA;
	//	Vec3V closBB = initialSupportB;
	//	
	//	BoolV bNotTerminated = FIsGrtr(sDist, eps2);
	//	BoolV bCon = bTrue;
	//	
	//	while(BAllEq(bNotTerminated, bTrue))
	//	{
	//		minDist = sDist;
	//		closAA = closA;
	//		closBB = closB;

	//		supportA=a.support(V3Neg(v));
	//		supportB=b.support(v);
	//		
	//		//calculate the support point
	//		const Vec3V support = V3Sub(supportA, supportB);
	//		const FloatV signDist = V3Dot(v, support);
	//		const FloatV tmp0 = FSub(sDist, signDist);

	//		PX_ASSERT(size < 4);
	//		A[size]=supportA;
	//		B[size]=supportB;
	//		Q[size++]=support;
	//
	//		if(FAllGrtr(eps1, tmp0))
	//		{
	//			closestA = closA;
	//			closestB = closB;
	//			sqDist = sDist;
	//			normal = V3Normalize(V3Sub(closB, closA));
	//			return GJK_NON_INTERSECT;
	//		}

	//		//calculate the closest point between two convex hull
	//		const Vec3V tempV = GJKCPairDoSimplex(Q, A, B, support, supportA, supportB, size, closA, closB);
	//		v = tempV;
	//		sDist = V3Dot(v, v);
	//		bCon = FIsGrtr(minDist, sDist);
	//		bNotTerminated = BAnd(FIsGrtr(sDist, eps2), bCon);
	//	}		

	//	closA = V3Sel(bCon, closA, closAA);
	//	closB = V3Sel(bCon, closB, closBB);
	//	closestA = closA;
	//	closestB = closB;
	//	normal = V3Normalize(V3Sub(closB, closA));
	//	sqDist = FSel(bCon, sDist, minDist);
	//	return BAllEq(bCon, bTrue) == 1 ? GJK_CONTACT : GJK_DEGENERATE;
	//}

	template<class ConvexA, class ConvexB>
	PxGJKStatus _GJK(const ConvexA& a, const ConvexB& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist)
	{
		using namespace Ps::aos;
		Vec3V Q[4];
		Vec3V A[4];
		Vec3V B[4];

		const Vec3V zeroV = V3Zero();
		const FloatV zero = FZero();
		const BoolV bTrue = BTTTT();
		PxU32 size=0;

		const Vec3V _initialSearchDir = V3Sub(a.getCenter(), b.getCenter());
		Vec3V v = V3Sel(FIsGrtr(V3Dot(_initialSearchDir, _initialSearchDir), zero), _initialSearchDir, V3UnitX());

		const FloatV marginA = a.getMargin();
		const FloatV marginB = b.getMargin();
		const FloatV minMargin = FMin(marginA, marginB);
		const FloatV eps2 = FMul(minMargin, FloatV_From_F32(0.001f));
		const FloatV epsRel = FMul(eps2, eps2);

		Vec3V closA(zeroV), closB(zeroV);
		FloatV sDist = FMax();
		FloatV minDist = sDist;
		Vec3V closAA;
		Vec3V closBB;

		
		BoolV bNotTerminated = bTrue;
		BoolV bCon = bTrue;
		
		do
		{
			minDist = sDist;
			closAA = closA;
			closBB = closB;

			const Vec3V supportA=a.support(V3Neg(v));
			const Vec3V supportB=b.support(v);
			
			//calculate the support point
			const Vec3V support = V3Sub(supportA, supportB);
			const FloatV signDist = V3Dot(v, support);
			const FloatV tmp0 = FSub(sDist, signDist);

			PX_ASSERT(size < 4);
			A[size]=supportA;
			B[size]=supportB;
			Q[size++]=support;
	
			if(FAllGrtr(FMul(epsRel, sDist), tmp0))
			{
				const Vec3V n = V3Normalize(V3Sub(closB, closA));
			/*	closestA = V3Sel(aQuadratic, V3ScaleAdd(n, marginA, closA), closA);
				closestB = V3Sel(bQuadratic, V3NegScaleSub(n, marginB, closB), closB);*/
				closestA = closA;
				closestB = closB;
				sqDist = sDist;
				normal = n;
				return GJK_NON_INTERSECT;
			}

			//calculate the closest point between two convex hull
			const Vec3V tempV = GJKCPairDoSimplex(Q, A, B, support, supportA, supportB, size, closA, closB);
			v = tempV;
			sDist = V3Dot(v, v);
			bCon = FIsGrtr(minDist, sDist);
			bNotTerminated = BAnd(FIsGrtr(sDist, eps2), bCon);
		}while(BAllEq(bNotTerminated, bTrue));		

		closA = V3Sel(bCon, closA, closAA);
		closB = V3Sel(bCon, closB, closBB);
		/*const Vec3V n = V3Normalize(V3Sub(closB, closA));
		closestA = V3Sel(aQuadratic, V3ScaleAdd(n, marginA, closA), closA);
		closestB = V3Sel(bQuadratic, V3NegScaleSub(n, marginB, closB), closB);*/
		closestA = closA;
		closestB = closB;
		normal = V3Normalize(V3Sub(closB, closA));
		sqDist = FSel(bCon, sDist, minDist);
		return BAllEq(bCon, bTrue) == 1 ? GJK_CONTACT : GJK_DEGENERATE;
	}

	template<class ConvexA, class ConvexB>
	PxGJKStatus _GJKSeparatingAxis(const ConvexA& a, const ConvexB& b, const Ps::aos::FloatVArg contactOffSet)
	{
		using namespace Ps::aos;
		const FloatV zero = FZero();
		const Vec3V zeroV = V3Zero();
		const BoolV bTrue = BTTTT();
		const BoolV bFalse = BFFFF();
		PxU32 size=1;
		const Vec3V _initialSearchDir =V3Sub(a.getCenter(), b.getCenter());
		const Vec3V initialSearchDir = V3Sel(FIsGrtr(V3Dot(_initialSearchDir, _initialSearchDir), zero), _initialSearchDir, V3UnitX());
		Vec3V supportA = a.support(V3Neg(initialSearchDir));
		Vec3V supportB = b.support(initialSearchDir);
		Vec3V Q[4] = {V3Sub(supportA, supportB), zeroV, zeroV, zeroV}; //simplex set
		Vec3V A[4] = {supportA, zeroV, zeroV, zeroV}; //ConvexHull a simplex set
		Vec3V B[4] = {supportB, zeroV, zeroV, zeroV}; //ConvexHull b simplex set
		
		const FloatV sqContactOffSet = FMul(contactOffSet, contactOffSet);
		
		Vec3V closA(supportA), closB(supportB);
		Vec3V v(Q[0]);
		const FloatV eps1 = FloatV_From_F32(0.001f);
		const FloatV eps2 = FMul(eps1, eps1);
		FloatV sDist = V3Dot(v, v);
		FloatV minDist = sDist;
		BoolV bNotTerminated = FIsGrtr(sDist, eps2);
		BoolV bCon = bTrue;
		
		while(BAllEq(bNotTerminated, bTrue))
		{
			minDist = sDist;
			//const Vec3V dir = V3Normalise(V3Neg(v));
			supportA=a.support(V3Neg(v));
			supportB=b.support(v);
			//calculate the support point
			Vec3V support = V3Sub(supportA, supportB);
			const FloatV signDist = V3Dot(v, support);
			if(FAllGrtr(signDist, zero))
				return GJK_NON_INTERSECT;

			PX_ASSERT(size < 4);
			A[size]=supportA;
			B[size]=supportB;
			Q[size++]=support;
			//calculate the closest point between two convex hull
			v = GJKCPairDoSimplex(Q, A, B, support, supportA, supportB, size, closA, closB);

			sDist = V3Dot(v, v);

			bCon = FIsGrtr(minDist, sDist);
			bNotTerminated = BAnd(FIsGrtr(sDist, eps2), bCon);
		}

		if(BAllEq(bCon, bFalse) && FAllGrtrOrEq(sDist, sqContactOffSet))
		{
			return GJK_DEGENERATE;
		}
		return GJK_CONTACT;

		//return BAllEq(bCon, bTrue) == 1 ? GJK_CONTACT : GJK_DEGENERATE;
	}


	template<class ConvexA, class ConvexB>
	PxGJKStatus _GJKPenetration(const ConvexA& a, const ConvexB& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth)
	{
		//PIX_PROFILE_ZONE(GJKPenetration);
		using namespace Ps::aos;

		Vec3V A[4]; 
		Vec3V B[4];
		Vec3V Q[4];
		Vec3V D[4]; //store the direction
	
		const FloatV zero = FZero();

		const FloatV _marginA = a.getMargin();
		const FloatV _marginB = b.getMargin();
		const BoolV aQuadratic = a.isMarginEqRadiusV();
		const BoolV bQuadratic = b.isMarginEqRadiusV();

		const BoolV bHasMarginEqRadius = BOr(aQuadratic, bQuadratic);

		//const Vec3V centerAToCenterB =  V3Sub(b.getCenter(), a.getCenter());
		const Vec3V _initialSearchDir = V3Sub(a.getCenter(), b.getCenter());//V3Neg(centerAToCenterB);
		Vec3V v = V3Sel(FIsGrtr(V3Dot(_initialSearchDir, _initialSearchDir), zero), _initialSearchDir, V3UnitX());

		const FloatV minMargin = FMin(_marginA, _marginB);
		const FloatV marginA = FSel(bHasMarginEqRadius, _marginA, minMargin);
		const FloatV marginB = FSel(bHasMarginEqRadius, _marginB, minMargin);
		

		const FloatV eps1 = FloatV_From_F32(0.0001f);
		const FloatV eps2 = FMul(minMargin, FloatV_From_F32(0.001f));
		//const FloatV ratio = FloatV_From_F32(0.05f);
		const Vec3V zeroV = V3Zero();
		const BoolV bTrue = BTTTT();
		const BoolV bFalse = BFFFF();
		PxU32 size=0;

		//const FloatV tenthMargin = FMul(minMargin, ratio);
	
		//const FloatV sumMargin = FAdd(FAdd(marginA, marginB), tenthMargin);
		const FloatV sumMargin0 = FAdd(marginA, marginB);
		const FloatV sumMargin = FAdd(sumMargin0, contactDist);
		//const FloatV sumMargin = FAdd(marginA, marginB);
		const FloatV sqMargin = FMul(sumMargin, sumMargin);

		Vec3V closA = zeroV;
		Vec3V closB = zeroV;
		FloatV sDist = FMax();
		FloatV minDist;
		Vec3V tempClosA;
		Vec3V tempClosB;

		BoolV bNotTerminated = bTrue;
		BoolV bCon = bTrue;

		do
		{
			minDist = sDist;
			tempClosA = closA;
			tempClosB = closB;

			const Vec3V nv = V3Neg(v);

			D[size] = v;
			const Vec3V supportA=a.supportMargin(nv, marginA, A[size]);
			const Vec3V supportB=b.supportMargin(v, marginB, B[size]);
			
			//calculate the support point
			const Vec3V support = V3Sub(supportA, supportB);
			Q[size++]=support;

		

			PX_ASSERT(size <= 4);

			const FloatV tmp = FMul(sDist, sqMargin);//FMulAdd(sDist, sqMargin, eps3);
			const FloatV vw = V3Dot(v, support);
			const FloatV sqVW = FMul(vw, vw);
			
			const BoolV bTmp1 = FIsGrtr(vw, zero);
			const BoolV bTmp2 = FIsGrtr(sqVW, tmp);
			BoolV con = BAnd(bTmp1, bTmp2);

			const FloatV tmp1 = FSub(sDist, vw);
			const BoolV conGrtr = FIsGrtrOrEq(FMul(eps1, sDist), tmp1);

			const BoolV conOrconGrtr(BOr(con, conGrtr));

			if(BAllEq(conOrconGrtr, bTrue))
			{
				//size--; if you want to get the correct size, this line need to be on
				if(BAllEq(con, bFalse)) //must be true otherwise we wouldn't be in here...
				{
					const FloatV recipDist = FRsqrt(sDist);
					const FloatV dist = FRecip(recipDist);//FSqrt(sDist);
					PX_ASSERT(FAllGrtr(dist, FEps()));
					const Vec3V n = V3Scale(v, recipDist);//normalise
					/*contactA = V3Sub(closA, V3Scale(n, marginA));
					contactB = V3Add(closB, V3Scale(n, marginB));*/
					contactA = V3NegScaleSub(n, marginA, closA);
					contactB = V3ScaleAdd(n, marginB, closB);
					penetrationDepth = FSub(dist, sumMargin0);
					normal = n;
					PX_ASSERT(isFiniteVec3V(normal));
					return GJK_CONTACT;
					
				}
				else
				{
					return GJK_NON_INTERSECT;
				}
			}

			//calculate the closest point between two convex hull

			//v = GJKCPairDoSimplex(Q, A, B, size, closA, closB);
			v = GJKCPairDoSimplex(Q, A, B, D, support, supportA, supportB, size, closA, closB);

			sDist = V3Dot(v, v);

			bCon = FIsGrtr(minDist, sDist);
			bNotTerminated = BAnd(FIsGrtr(sDist, eps2), bCon);
		}
		while(BAllEq(bNotTerminated, bTrue));

		
		if(BAllEq(bCon, bFalse))
		{
			if(FAllGrtrOrEq(sqMargin, sDist))
			{
				//Reset back to older closest point
				closA = tempClosA;
				closB = tempClosB;
				sDist = minDist;
				v = V3Sub(closA, closB);

				const FloatV recipDist = FRsqrt(sDist);
				const FloatV dist = FRecip(recipDist);//FSqrt(sDist);
				PX_ASSERT(FAllGrtr(dist, FEps()));
				const Vec3V n = V3Scale(v, recipDist);//normalise
				contactA = V3NegScaleSub(n, marginA, closA);
				contactB = V3ScaleAdd(n, marginB, closB);
				penetrationDepth = FSub(dist, sumMargin0);
				normal = n;
				PX_ASSERT(isFiniteVec3V(normal));

				return GJK_CONTACT;
			}
			return GJK_DEGENERATE;
		}
		else
		{
			return RecalculateSimplex(a, b, a.getSupportMapping(), b.getSupportMapping(), D, size, contactA, contactB, normal, penetrationDepth, aQuadratic, bQuadratic);
		}
	}


#else
	

	
	PxGJKStatus _GJKPenetration(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport, SupportMargin aSupportMargin, SupportMargin bSupportMargin, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	template<typename ConvexA, typename ConvexB>
	PxGJKStatus _GJKPenetration(const ConvexA& a, const ConvexB& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth)
	{
		return _GJKPenetration(a, b, a.getSupportMapping(), b.getSupportMapping(), a.getSupportMarginMapping(), b.getSupportMarginMapping(), contactDist, contactA, contactB, normal, penetrationDepth);
	}
   

	PxGJKStatus _GJK(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport,  Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);

	template<typename ConvexA, typename ConvexB>
	PxGJKStatus _GJK(const ConvexA& a, const ConvexB& b,  Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist)
	{
		return _GJK(a, b, a.getSupportMapping(), b.getSupportMapping(), contactA, contactB,  normal, sqDist);
	}

	PxGJKStatus _GJKSeparatingAxis(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport, const Ps::aos::FloatVArg contactOffSet );

	template<typename ConvexA, typename ConvexB>
	PxGJKStatus _GJKSeparatingAxis(const ConvexA& a, const ConvexB& b, const Ps::aos::FloatVArg contactOffSet)
	{
		return _GJKSeparatingAxis(a, b, a.getSupportMapping(), b.getSupportMapping(), contactOffSet);
	}

	

#endif

}

}

#endif
