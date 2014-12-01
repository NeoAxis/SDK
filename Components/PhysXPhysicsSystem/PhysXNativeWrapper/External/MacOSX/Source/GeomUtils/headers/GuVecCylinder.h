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


#ifndef PX_PHYSICS_GEOMUTILS_CYLINER_H
#define PX_PHYSICS_GEOMUTILS_CYLINER_H

#include "GuVecConvex.h"

namespace physx
{
namespace Gu
{
	class CylinderV : public ConvexV
	{
	public:
       
		PX_INLINE CylinderV() : ConvexV(E_CYLINDER)     
		{
			u = Ps::aos::V3UnitY();
			halfHeight = Ps::aos::FOne();
			radius = Ps::aos::FOne();    
		}
	/**
		\brief Constructor
		*/
		PX_INLINE CylinderV(const Ps::aos::Vec3VArg _center,  const Ps::aos::Vec3VArg _u,  const Ps::aos::FloatVArg _halfHeight, const Ps::aos::FloatVArg _radius) : ConvexV(E_CYLINDER, _center)
		{
			PX_ASSERT(Ps::aos::FAllEq(Ps::aos::V3Length(_u), Ps::aos::FOne()));
			u = _u;
			halfHeight = _halfHeight;
			radius = _radius;
		}

		PX_INLINE CylinderV(const Ps::aos::Vec3VArg _center, const Ps::aos::Vec3VArg _u, const Ps::aos::FloatVArg _halfHeight, const Ps::aos::FloatVArg _radius, const Ps::aos::FloatVArg _margin) : ConvexV(E_CYLINDER, _center, _margin)
		{
			PX_ASSERT(Ps::aos::FAllEq(Ps::aos::V3Length(_u), Ps::aos::FOne()));
			u = _u;
			halfHeight = _halfHeight;
			radius = _radius;
		}


		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3V dir) const
		{
			using namespace Ps::aos;
			const FloatV zero = FZero();
			const FloatV uDir = V3Dot(u, dir);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V v = V3Sel(FIsGrtr(wLength, zero), V3Scale(_w, radius), V3Zero());
			const Vec3V q = V3Scale(u, halfHeight);
			const Vec3V p = V3Sel(FIsGrtr(uDir, zero), q, V3Neg(q));
			return V3Add(V3Add(center, p), v);
		}


		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3V dir, const Ps::aos::FloatV _margin) const
		{
			using namespace Ps::aos;
			const FloatV zero = FZero();
			const FloatV uDir = V3Dot(u, dir);
			const FloatV _halfHeight = FAdd(halfHeight, _margin);
			const FloatV _radius = FAdd(radius, _margin);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V v = V3Sel(FIsGrtr(wLength, zero), V3Scale(_w, _radius), V3Zero());
			const Vec3V q = V3Scale(u, _halfHeight);
			const Vec3V p = V3Sel(FIsGrtr(uDir, zero), q, V3Neg(q));
			return V3Add(V3Add(center, p), v);
		}


		Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3V dir, const Ps::aos::FloatV _margin) const
		{
			using namespace Ps::aos;
			const FloatV zero = FZero();
			const FloatV uDir = V3Dot(u, dir);
			const FloatV _halfHeight = FSub(halfHeight, _margin);
			const FloatV _radius = FSub(radius, _margin);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V v = V3Sel(FIsGrtr(wLength, zero), V3Scale(_w, _radius), V3Zero());
			const Vec3V q = V3Scale(u, _halfHeight);
			const Vec3V p = V3Sel(FIsGrtr(uDir, zero), q, V3Neg(q));
			return V3Add(V3Add(center, p), v);
		}

		Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3V dir, const Ps::aos::FloatV _margin, Ps::aos::Vec3V& support) const
		{
			using namespace Ps::aos;
			const FloatV zero = FZero();
			const FloatV uDir = V3Dot(u, dir);
			const FloatV _halfHeight = FSub(halfHeight, _margin);
			const FloatV _radius = FSub(radius, _margin);
			const Vec3V w = V3Sub(dir, V3Scale(u, uDir));
			const FloatV wLength = V3Length(w);
			const Vec3V _w = V3Scale(w, FRecip(wLength));
			const Vec3V v = V3Sel(FIsGrtr(wLength, zero), V3Scale(_w, _radius), V3Zero());
			const Vec3V q = V3Scale(u, _halfHeight);
			const Vec3V p = V3Sel(FIsGrtr(uDir, zero), q, V3Neg(q));
			const Vec3V ret = V3Add(V3Add(center, p), v);
			support = ret;
			return ret;
		}

		Ps::aos::Vec3V u; // center axis unit vectpr 
		Ps::aos::FloatV halfHeight;
		Ps::aos::FloatV radius;
	};
}

}

#endif