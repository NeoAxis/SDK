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


#ifndef PX_GJKRAYCAST_H
#define PX_GJKRAYCAST_H

#include "GuGJKWrapper.h"
#include "GuGJKSimplex.h"
#include "GuConvexSupportTable.h"

namespace physx
{


namespace Gu
{

#ifndef	__SPU__

	template<class ConvexA, class ConvexB>
	bool _GJKRayCast(ConvexA& a, ConvexB& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA)
	{
		using namespace Ps::aos;
		const Vec3V zeroV = V3Zero();
		const FloatV zero = FZero();
		const FloatV one = FOne();
		const BoolV bTrue = BTTTT();

		const FloatV maxDist = FloatV_From_F32(PX_MAX_REAL);
	
		FloatV _lambda = zero;//initialLambda;
		Vec3V x = V3ScaleAdd(r, _lambda, s);
		PxU32 size=1;

		const Vec3V bOriginalCenter = b.getCenter(); 
		b.setCenter(x);
		const Vec3V _initialSearchDir(V3Sub(a.getCenter(), b.getCenter()));
		const Vec3V initialSearchDir = V3Sel(FIsGrtr(V3Dot(_initialSearchDir, _initialSearchDir), zero), _initialSearchDir, V3UnitX());

		const Vec3V initialSupportA(a.supportSweep(V3Neg(initialSearchDir)));
		const Vec3V initialSupportB(b.supportSweep(initialSearchDir));
		 
		Vec3V Q[4] = {V3Sub(initialSupportA, initialSupportB), zeroV, zeroV, zeroV}; //simplex set
		Vec3V A[4] = {initialSupportA, zeroV, zeroV, zeroV}; //ConvexHull a simplex set
		Vec3V B[4] = {initialSupportB, zeroV, zeroV, zeroV}; //ConvexHull b simplex set
		 

		Vec3V v = V3Neg(Q[0]);
		Vec3V supportA = initialSupportA;
		Vec3V supportB = initialSupportB;
		Vec3V support = Q[0];
		const FloatV eps1 = FloatV_From_F32(0.0001f);
		const FloatV eps2 = FMul(eps1, eps1);
		Vec3V closA(initialSupportA), closB(initialSupportB);
		FloatV sDist = V3Dot(v, v);
		FloatV minDist = sDist;
		Vec3V closAA = initialSupportA;
		Vec3V closBB = initialSupportB;
		
		BoolV bNotTerminated = FIsGrtr(sDist, eps2);
		BoolV bCon = bTrue;

		Vec3V nor = v;
		
		while(BAllEq(bNotTerminated, bTrue))
		{
			
			minDist = sDist;
			closAA = closA;
			closBB = closB;

			supportA=a.supportSweep(v);
			supportB=b.supportSweep(V3Neg(v));
			
			//calculate the support point
			support = V3Sub(supportA, supportB);
			const Vec3V w = V3Neg(support);
			const FloatV vw = V3Dot(v, w);
			const FloatV vr = V3Dot(v, r);
			if(FAllGrtr(vw, zero))
			{
	
				if(FAllGrtrOrEq(vr, zero))
				{
					b.setCenter(bOriginalCenter);
					return false;
				}
				else
				{
					const FloatV _oldLambda = _lambda;
					_lambda = FSub(_lambda, FDiv(vw, vr));
					if(FAllGrtr(_lambda, _oldLambda))
					{
						if(FAllGrtr(_lambda, one))
						{
							b.setCenter(bOriginalCenter);
							return false;
						}
						const Vec3V bPreCenter = b.getCenter();
						x = V3ScaleAdd(r, _lambda, s);
						b.setCenter(x);
						const Vec3V offSet = V3Sub(x, bPreCenter);
						const Vec3V b0 = V3Add(B[0], offSet);
						const Vec3V b1 = V3Add(B[1], offSet);
						const Vec3V b2 = V3Add(B[2], offSet);

						B[0] = b0;
						B[1] = b1;
						B[2] = b2;

						Q[0]=V3Sub(A[0], b0);
						Q[1]=V3Sub(A[1], b1);
						Q[2]=V3Sub(A[2], b2);

						supportB = b.supportSweep(V3Neg(v));
						support = V3Sub(supportA, supportB);
						minDist = maxDist;
						nor = v;
						//size=0;
					}
				}
			}

			PX_ASSERT(size < 4);
			A[size]=supportA;
			B[size]=supportB;
			Q[size++]=support;
	
			//calculate the closest point between two convex hull
			const Vec3V tempV = GJKCPairDoSimplex(Q, A, B, support, supportA, supportB, size, closA, closB);
			v = V3Neg(tempV);
			sDist = V3Dot(tempV, tempV);
			bCon = FIsGrtr(minDist, sDist);
			bNotTerminated = BAnd(FIsGrtr(sDist, eps2), bCon);
		}

		lambda = _lambda;
		if(FAllEq(_lambda, zero))
		{
			//time of impact is zero, the sweep shape is intesect, use epa to get the normal and contact point
			b.setCenter(bOriginalCenter);
			const FloatV contactDist = getContactEps(a.getMargin(), b.getMargin());

			closestA = closAA;
			normal= V3Normalize(V3Sub(closAA, closBB));
			//hitPoint = x;
			if(GJKPenetration(a, b, contactDist, closA, closB, normal, sDist))
			{
				closestA = closA;
			}
		}
		else
		{
			//const FloatV stepBackRatio = FDiv(offset, V3Length(r));
			//lambda = FMax(FSub(lambda, stepBackRatio), zero);
			b.setCenter(bOriginalCenter);
			closA = V3Sel(bCon, closA, closAA);
			closestA = closA;
			normal = V3Neg(V3Normalize(nor));
		}
		return true;
	}


#else

	bool _GJKRayCast(ConvexV& a, ConvexV& b, Support aSupportSweep, Support bSupportSweep, Support aSupport, Support bSupport, SupportMargin aSupportMargin, SupportMargin bSupportMargin,  const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);


	template<class ConvexA, class ConvexB>
	bool _GJKRayCast(ConvexA& a, ConvexB& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA)
	{
		return _GJKRayCast(a, b, a.getSweepSupportMapping(), b.getSweepSupportMapping(), a.getSupportMapping(), b.getSupportMapping(), a.getSupportMarginMapping(), b.getSupportMarginMapping(), initialLambda, s, r, lambda, normal, closestA);
	}

	
#endif

}
}

#endif
