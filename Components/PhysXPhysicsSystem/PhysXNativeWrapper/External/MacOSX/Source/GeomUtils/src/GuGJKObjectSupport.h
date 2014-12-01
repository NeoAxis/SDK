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


#ifndef PX_PHYSICS_GEOMUTILS_PX_GJK_OBJECT_SUPPORT
#define PX_PHYSICS_GEOMUTILS_PX_GJK_OBJECT_SUPPORT

#include "CmPhysXCommon.h"

#include "GuConvexMesh.h"
#include "PxMeshScale.h"

#include "GuGJKSweep.h"
#include "GuBoxProjection.h"
#include "PsMathUtils.h"

namespace physx
{
namespace Gu
{
	class GJKSphereSupport : public GJKConvexInterface
	{
	public:
		GJKSphereSupport(PxReal r) : mRadius(r)
		{
		}
		virtual void getBounds(PxBounds3& bounds) const {}		//not needed
		virtual PxVec3 projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const
		{
			return localDir.getNormalized() * mRadius;	//localdir is really not normalized, at least when called from the GJK code!
		}
		virtual PxVec3 inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const
		{
			featureCode = 1;
			medianDir = pointOnSurface.getNormalized();
			return medianDir;
		}
		virtual bool getInnerSphere(PxVec3& center, PxReal& radius) const
		{
			center = PxVec3(0.0f);
			radius = mRadius;
			return true;
		}

		PxReal	mRadius;
	};

	class GJKSegmentSupport : public GJKConvexInterface
	{
	public:
		GJKSegmentSupport(PxReal hH) : mHalfHeight(hH)
		{
		}
		virtual void getBounds(PxBounds3& bounds) const {}		//not needed
		virtual PxVec3 projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const
		{
			// PT: dir doesn't need to be normalized for this one

/*			const PxVec3 p0(mHalfHeight, 0.0f, 0.0f);
			const PxVec3 p1(-mHalfHeight, 0.0f, 0.0f);
			PxVec3 p;
			PxReal maximum = -PX_MAX_F32;
			const float dp0 = p0|localDir;
			if(dp0 > maximum)	{ maximum = dp0; p = p0; }
			const float dp1 = p1|localDir;
			if(dp1 > maximum)	{ maximum = dp1; p = p1; }
			return p;*/

			return PxVec3(Ps::sign(localDir.x)*mHalfHeight, 0.0f, 0.0f);
		}

		// AP scaffold todo: merge with Michelle's implementation
		PxReal closestOnSegment(const PxVec3& p, const PxVec3& a, const PxVec3& b, PxVec3& pab) const
		{
			PxVec3 ab = b-a;
			PxReal ab2 = ab.dot(ab);
			PxReal invDenom = (ab2 < 1e-6f) ? 0.0f : 1.0f / ab2; // AP scaffold newccd epsilon
			PxReal tp = ab.dot(p-a) * invDenom;
			tp = PxMax(PxMin(tp, 1.0f), 0.0f); // clamp to [0,1]
			pab = a+ab*tp;
			return tp;
		}

		virtual PxVec3 inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const
		{
			featureCode = 1;
			PxVec3 p1(mHalfHeight,0,0);
			PxVec3 p0(-mHalfHeight,0,0);
			PxVec3 pClosest;
			closestOnSegment(pointOnSurface, p0, p1, pClosest);
			medianDir = (pointOnSurface - pClosest).getNormalized();
			return medianDir;
		}

		virtual bool getInnerSphere(PxVec3& center, PxReal& radius) const
		{
			center = PxVec3(0.0f);
			radius = 0.0f;
			return true;
		}

		PxReal	mHalfHeight;
	};


	class GJKCapsuleSupport : public GJKConvexInterface
	{
	public:
		GJKCapsuleSupport(PxReal hH, PxReal r) : mHalfHeight(hH), mRadius(r)
		{
		}
		virtual void getBounds(PxBounds3& bounds) const {}		//not needed

		virtual PxVec3 projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const
		{
			const PxVec3 radialDir = localDir.getNormalized() * mRadius;	//localdir is really not normalized, at least when called from the GJK code!
			return PxVec3(Ps::sign(localDir.x)*mHalfHeight + radialDir.x, radialDir.y, radialDir.z);
		}

		// AP scaffold todo: merge with Michelle's implementation
		PxReal closestOnSegment(const PxVec3& p, const PxVec3& a, const PxVec3& b, PxVec3& pab) const
		{
			PxVec3 ab = b-a;
			PxReal ab2 = ab.dot(ab);
			PxReal invDenom = (ab2 < 1e-6f) ? 0.0f : 1.0f / ab2; // AP scaffold newccd epsilon
			PxReal tp = ab.dot(p-a) * invDenom;
			tp = PxMax(PxMin(tp, 1.0f), 0.0f); // clamp to [0,1]
			pab = a+ab*tp;
			return tp;
		}

		virtual PxVec3 inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const
		{
			featureCode = 0;
			PxVec3 p1(mHalfHeight,0,0);
			PxVec3 p0(-mHalfHeight,0,0);
			PxVec3 pClosest;
			closestOnSegment(pointOnSurface, p0, p1, pClosest);
			medianDir = (pointOnSurface - pClosest).getNormalized();
			return medianDir;
		}

		virtual bool getInnerSphere(PxVec3& center, PxReal& radius) const
		{
			center = PxVec3(0.0f);
			radius = mRadius;
			return true;
		}

		PxReal	mHalfHeight;
		PxReal	mRadius;
	};


	class GJKBoxSupport : public GJKConvexInterface
	{
	public:
		GJKBoxSupport(const PxVec3& extents) : mHalfSide(extents)
		{
		}
		virtual void getBounds(PxBounds3& bounds) const {}		//not needed

		virtual PxVec3 projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const
		{
			// PT: dir doesn't need to be normalized for this one
			PxVec3 p;
			projectBox(p, localDir, mHalfSide);
			return p;
		}

		// AP scaffold TODO: duplicate of PxcConvexBox?? merge with Michelle's code
		PxVec3 inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const
		{
			PxVec3 absP = PxVec3(PxAbs(pointOnSurface.x), PxAbs(pointOnSurface.y), PxAbs(pointOnSurface.z));
			absP -= this->mHalfSide;
			absP = PxVec3(PxAbs(absP.x), PxAbs(absP.y), PxAbs(absP.z));
			PxF32 cntzEps = 1e-2f;
			PxU32 nearX = (absP.x < cntzEps);
			PxU32 nearY = (absP.y < cntzEps);
			PxU32 nearZ = (absP.z < cntzEps);
			PxU32 cntz =  nearX + nearY + nearZ;
			featureCode = cntz;
			medianDir = (PxVec3(nearX * PxSign(pointOnSurface.x), 0, 0) +
						PxVec3(0, nearY * PxSign(pointOnSurface.y), 0) +
						PxVec3(0, 0, nearZ * PxSign(pointOnSurface.z))).getNormalized();
			if (absP.x < absP.y && absP.x < absP.z)
				return PxVec3(1.0f*PxSign(pointOnSurface.x), 0.0f, 0.0f);
			else if (absP.y < absP.z)
				return PxVec3(0.0f, 1.0f*PxSign(pointOnSurface.y), 0.0f);
			else
				return PxVec3(0.0f, 0.0f, 1.0f*PxSign(pointOnSurface.z));
		}

		virtual bool getInnerSphere(PxVec3& center, PxReal& radius) const
		{
			center = PxVec3(0.0f);
			radius = PxMin(PxMin(mHalfSide.x, mHalfSide.y), mHalfSide.z);
			return true;
		}

		PxVec3	mHalfSide;
	};


	class GJKConvexSupport : public GJKConvexInterface
	{
	public:
		GJKConvexSupport(const Gu::ConvexHullData& hull, const PxMeshScale& scale) : mConvexHull(hull), mVertex2ShapeSkew(scale.toMat33())
		{
		}
		virtual void getBounds(PxBounds3& bounds) const {}		//not needed
		virtual PxVec3 projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const
		{
			PxReal min, max;
			return projectHull_(mConvexHull, min, max, localDir, mVertex2ShapeSkew);
		}
		virtual PxVec3 inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const
		{
			return hullInverseSupportMapping(mConvexHull, pointOnSurface, featureCode, medianDir, mVertex2ShapeSkew);
		}
		virtual bool getInnerSphere(PxVec3& center, PxReal& radius) const
		{
			return hullInnerSphere(mConvexHull, center, radius);
		}

//	private:
		const Gu::ConvexHullData&	mConvexHull;
		PxMat33					mVertex2ShapeSkew;
	};

	class GJKTriangleSupport : public GJKConvexInterface
	{
		public:
		GJKTriangleSupport(const PxVec3& p0, const PxVec3& p1, const PxVec3& p2)
		{
			mP[0] = p0;
			mP[1] = p1;
			mP[2] = p2;
		}
		virtual void	getBounds(PxBounds3& bounds) const
		{
			PX_ASSERT(0);
		}

		virtual PxVec3	projectHullMax(const PxVec3& localDir, GJKConvexInterfaceCache&) const
		{
			PX_ASSERT(localDir.isNormalized());

			PxReal maximum = -FLT_MAX;
			PxU32 candidate=0;
			for(PxU32 i=0;i<3;i++)
			{
				const PxReal dp = localDir.dot(mP[i]);
				if(dp>maximum)
				{
					maximum = dp;
					candidate = i;
				}
			}
			return mP[candidate];
		}

		virtual PxVec3 inverseSupportMapping(const PxVec3& pointOnSurface, int& featureCode, PxVec3& medianDir) const
		{
			featureCode = 1;
			medianDir = (mP[1]-mP[0]).cross(mP[2]-mP[0]);
			return medianDir;
		}

		virtual bool getInnerSphere(PxVec3& center, PxReal& radius) const
		{
			center = (mP[0] + mP[1] + mP[2]) * 1.0f / 3.0f;
			radius = 0.0f;

			return true;
		}

		PxVec3 mP[3];
	};

} // namespace Gu

}

#endif
