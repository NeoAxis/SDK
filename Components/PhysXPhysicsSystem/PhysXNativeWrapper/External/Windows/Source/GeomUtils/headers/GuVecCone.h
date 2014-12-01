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


#ifndef PX_PHYSICS_GEOMUTILS_CONE_H
#define PX_PHYSICS_GEOMUTILS_CONE_H

#include "GuVecConvex.h"

namespace physx
{
namespace Gu
{
	class ConeV : public ConvexV
	{
		public:
		/**
		\brief Constructor
		*/

		PX_INLINE ConeV() : ConvexV(E_CONE)
		{
			using namespace Ps::aos;
			u = V3UnitY();
			halfHeight = FOne();
			radius = FOne();
			margin = FloatV_From_F32(0.1f);
		}

		PX_INLINE ConeV(const Ps::aos::Vec3VArg _center, const Ps::aos::Vec3VArg _u, const Ps::aos::FloatVArg _halfHeight, const Ps::aos::FloatVArg _radius): ConvexV(E_CONE, _center)
		{
			using namespace Ps::aos;
			const FloatV perc = FloatV_From_F32(0.1f);
			PX_ASSERT(Ps::aos::FAllEq(Ps::aos::V3Length(_u), Ps::aos::FOne())); 
			u =_u;
			halfHeight = _halfHeight;
			radius = _radius;
			const FloatV min = FMin(_halfHeight, _radius);
			margin = FMul(min, perc);
		}

		PX_INLINE ConeV(const Ps::aos::Vec3VArg _center, const Ps::aos::Vec3VArg _u, const Ps::aos::FloatVArg _halfHeight, const Ps::aos::FloatVArg _radius, const Ps::aos::FloatVArg _margin): ConvexV(E_CONE, _center, _margin)
		{
			using namespace Ps::aos;
			PX_ASSERT(Ps::aos::FAllEq(Ps::aos::V3Length(_u), Ps::aos::FOne()));
			u = _u;
			halfHeight = _halfHeight;
			radius = _radius;
			margin = _margin;
			//eps = _margin * 0.1f;
		}

		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir) const
		{
			using namespace Ps::aos;
			/*const FloatV height = FAdd(halfHeight, halfHeight);
			const FloatV dLength = V3Length(dir);
			const FloatV sqRadius = FMul(radius, radius);
			const FloatV sqHeight = FMul(height, height);
			const FloatV sinTheta = FDiv(radius, FSqrt(FAdd(sqRadius, sqHeight))); 
			const FloatV uDir = V3Dot(u, dir);
			const FloatV right = FMul(sinTheta, dLength); 
			const Vec3V v = V3Scale(u, halfHeight);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V nv = V3Neg(v);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V v1 = V3Scale(w, FMul(radius, FRecip(wLength)));
			const Vec3V h = V3Sel(FIsEq(wLength, FZero()), nv, V3Add(nv, v1));
			const Vec3V g = V3Sel(FIsGrtrOrEq(uDir, right), v, h);
			return V3Add(center, g);*/

			using namespace Ps::aos;
			const FloatV uDir = V3Dot(u, dir);
			const Vec3V v =V3Scale(u, halfHeight); // tip
			const Vec3V nv = V3Neg(v);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V tmp =  V3Add(nv, V3Scale(_w, radius));
			const Vec3V h = V3Sel(FIsGrtr(wLength, FZero()),tmp, nv);//bottom
			const Vec3V g = V3Sel(FIsGrtr(uDir, FZero()), v, h);
			return V3Add(center, g);
		}

		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin) const
		{
			using namespace Ps::aos;
			const FloatV _halfHeight = FSub(halfHeight, _margin);
			const FloatV _radius = FSub(radius, _margin);
			const FloatV uDir = V3Dot(u, dir);
			const Vec3V v =V3Scale(u, _halfHeight); // tip
			const Vec3V nv = V3Neg(v);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V tmp =  V3Add(nv, V3Scale(_w, _radius));
			const Vec3V h = V3Sel(FIsGrtr(wLength, FZero()),tmp, nv);//bottom
			const Vec3V g = V3Sel(FIsGrtr(uDir, FZero()), v, h);
			return V3Add(center, g);
		}

		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin,
			Ps::aos::Vec3V& support) const
		{
			using namespace Ps::aos;
			const FloatV _halfHeight = FSub(halfHeight, _margin);
			const FloatV _radius = FSub(radius, _margin);
			const FloatV uDir = V3Dot(u, dir);
			const Vec3V v =V3Scale(u, _halfHeight); // tip
			const Vec3V nv = V3Neg(v);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V tmp =  V3Add(nv, V3Scale(_w, _radius));
			const Vec3V h = V3Sel(FIsGrtr(wLength, FZero()),tmp, nv);//bottom
			const Vec3V g = V3Sel(FIsGrtr(uDir, FZero()), v, h);
			const Vec3V ret = V3Add(center, g);
			support = ret;
			return ret;
		}

		Ps::aos::Vec3V u; //center axis
		Ps::aos::FloatV halfHeight;
		Ps::aos::FloatV radius;
	
	};
}

}

#endif