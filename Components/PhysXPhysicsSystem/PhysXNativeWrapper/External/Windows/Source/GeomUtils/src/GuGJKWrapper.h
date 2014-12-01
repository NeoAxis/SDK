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


#ifndef PX_GJKWRAPPER_H
#define PX_GJKWRAPPER_H

#include "PxPhysXCommon.h"  // for PX_PHYSX_COMMON_API
#include "CmPhysXCommon.h"
#include "PsVecMath.h"

//#include "GuGJK.h"
//#include "GuGJKRaycast.h"

#define	GJK_CONTACT_OFFSET	0.04f	
/*
	This file is used to avoid the inner loop cross DLL calls
*/
namespace physx
{
namespace Gu
{

#define PxGJKStatus PxU32
#define GJK_NON_INTERSECT 0 
#define GJK_CONTACT 1
#define GJK_UNDEFINED 2
#define	GJK_DEGENERATE 3

	class SegmentV;
	class TriangleV;
	//class SphereV;
	class CapsuleV;
	class BoxV;
	class ConvexHullV;
	class BigConvexHullV;

	


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//
	//													gjk
	//			
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	PX_PHYSX_COMMON_API PxGJKStatus GJK(const TriangleV& a, const ConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const TriangleV& a, const BigConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const CapsuleV& a, const BigConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const CapsuleV& a, const ConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const BoxV& a, const BigConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const BoxV& a, const ConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const ConvexHullV& a, const BigConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const ConvexHullV& a, const ConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const BigConvexHullV& a, const BigConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const BigConvexHullV& a, const ConvexHullV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	PX_PHYSX_COMMON_API PxGJKStatus GJK(const TriangleV& a, const BoxV& b, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& sqDist);
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//
	//													gjk seprating axis
	//			
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const TriangleV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);
	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const TriangleV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);

	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const BoxV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);
	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const BoxV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);  

	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const ConvexHullV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);
	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const ConvexHullV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);
	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const BigConvexHullV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);
	PX_PHYSX_COMMON_API PxGJKStatus GJKSeparatingAxis(const BigConvexHullV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactOffSet);


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//
	//													gjk/epa with contact dist
	//			
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	//PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const TriangleV& a, const SphereV& b, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const TriangleV& a, const BoxV& b, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);
	
	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const TriangleV& a, const CapsuleV& b, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);
	
	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const TriangleV& a, const ConvexHullV& b, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);
	
	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const TriangleV& a, const BigConvexHullV& b, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);
	
	///*	
	//	sphere vs others
	//*/

	//PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const SphereV& a, const SphereV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	//PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const SphereV& a, const CapsuleV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	//PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const SphereV& a, const BoxV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	//PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const SphereV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	//PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const SphereV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);


	
	/*	
		capsule vs others
	*/
	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const CapsuleV& a, const CapsuleV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const CapsuleV& a, const BoxV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const CapsuleV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const CapsuleV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);


	/*	
		box vs others
	*/
	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const BoxV& a, const BoxV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const BoxV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const BoxV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);


	/*	
		convexhull vs others
	*/
	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const BigConvexHullV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const BigConvexHullV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const ConvexHullV& a, const ConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);

	PX_PHYSX_COMMON_API PxGJKStatus GJKPenetration(const ConvexHullV& a, const BigConvexHullV& b, const Ps::aos::FloatVArg contactDist, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth);



	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//
	//													gjk raycast
	//			
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	
	//PX_PHYSX_COMMON_API bool GJKRayCast(TriangleV& a, SphereV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(TriangleV& a, BoxV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(TriangleV& a, CapsuleV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(TriangleV& a, ConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(TriangleV& a, BigConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);

	
	//PX_PHYSX_COMMON_API bool GJKRayCast(SphereV& a, SphereV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	//PX_PHYSX_COMMON_API bool GJKRayCast(SphereV& a, CapsuleV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	//PX_PHYSX_COMMON_API bool GJKRayCast(SphereV& a, BoxV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	//PX_PHYSX_COMMON_API bool GJKRayCast(SphereV& a, ConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	//PX_PHYSX_COMMON_API bool GJKRayCast(SphereV& a, BigConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);

	PX_PHYSX_COMMON_API bool GJKRayCast(CapsuleV& a, CapsuleV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(CapsuleV& a, BoxV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(CapsuleV& a, ConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(CapsuleV& a, BigConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);

	PX_PHYSX_COMMON_API bool GJKRayCast(BoxV& a, BoxV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(BoxV& a, ConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(BoxV& a, BigConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	

	PX_PHYSX_COMMON_API bool GJKRayCast(BigConvexHullV& a, BigConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(BigConvexHullV& a, ConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(ConvexHullV& a, BigConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);
	PX_PHYSX_COMMON_API bool GJKRayCast(ConvexHullV& a, ConvexHullV& b, const Ps::aos::FloatVArg initialLambda, const Ps::aos::Vec3VArg s, const Ps::aos::Vec3VArg r, Ps::aos::FloatV& lambda, Ps::aos::Vec3V& normal, Ps::aos::Vec3V& closestA);

}
}

#endif