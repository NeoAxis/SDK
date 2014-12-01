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


#ifndef PX_CONVEXHULL_H
#define PX_CONVEXHULL_H

#include "CmPhysXCommon.h"
#include "PxBounds3.h"
#include "PsIntrinsics.h"
#include "PxPlane.h"

// Data definition

namespace physx
{
namespace Gu
{
	struct BigConvexRawData;

	struct HullPolygonData
	{
		//Note: this structure can not be used with PX_NEW because Plane has a constructor but isn't a sub-class of PxAllocatable.
		//		please do not assume that any data types added to this class will automatically call their constructor in cooking. 
		PxPlane			mPlane;			//!< Plane equation for this polygon	//Could drop 4th elem as it can be computed from any vertex as: d = - p.dot(n);
		PxU16			mVRef8;			//!< Offset of vertex references in hull vertex data (CS: can we assume indices are tightly packed and offsets are ascending?? DrawObjects makes and uses this assumption)
		PxU8			mNbVerts;		//!< Number of vertices/edges in the polygon
		PxU8			mMinIndex;		//!< Index of the polygon vertex that has minimal projection along this plane's normal.

		PX_FORCE_INLINE	PxReal getMin(const PxVec3* PX_RESTRICT hullVertices) const	//minimum of projection of the hull along this plane normal
		{ 
			return mPlane.n.dot(hullVertices[mMinIndex]);
		}
		PX_FORCE_INLINE	PxReal getMax() const		{ return -mPlane.d; }	//maximum of projection of the hull along this plane normal

		PX_FORCE_INLINE	void negatePlane()
		{
			mPlane.n=-mPlane.n;
			mPlane.d=-mPlane.d;
		}
	};
	PX_COMPILE_TIME_ASSERT(sizeof(Gu::HullPolygonData) == 20);

// TEST_INTERNAL_OBJECTS
	struct InternalObjectsData
	{
		PxReal					mRadius;
		PxReal					mExtents[3];
	};
	PX_COMPILE_TIME_ASSERT(sizeof(Gu::InternalObjectsData) == 16);
//~TEST_INTERNAL_OBJECTS

	struct ConvexHullData
	{
		PxBounds3				mAABB;				//!< bounds TODO: compute this on the fly from first 6 vertices in the vertex array.  We'll of course need to sort the most extreme ones to the front.
		PxVec3					mCenterOfMass;		//in local space of mesh!

		// PT: WARNING: mNbHullVertices *must* appear before mBigConvexRawData for ConvX to be able to do "big raw data" surgery
		PxU16					mNbEdges;
		PxU8					mNbHullVertices;	//!< Number of vertices in the convex hull
		PxU8					mNbPolygons;		//!< Number of planar polygons composing the hull

		HullPolygonData*		mPolygons;			//!< Array of mNbPolygons structures
		BigConvexRawData*		mBigConvexRawData;	//!< Hill climbing data, only for large convexes! else NULL.

// TEST_INTERNAL_OBJECTS
		InternalObjectsData		mInternal;
//~TEST_INTERNAL_OBJECTS

		PX_FORCE_INLINE	const PxVec3* PX_RESTRICT_RETVAL getHullVertices()	const	//!< Convex hull vertices
		{
			const char* tmp = reinterpret_cast<const char*>(mPolygons);
			tmp += sizeof(Gu::HullPolygonData) * mNbPolygons;
			return reinterpret_cast<const PxVec3* PX_RESTRICT>(tmp);
		}

		PX_FORCE_INLINE	const PxU8* PX_RESTRICT_RETVAL getFacesByEdges8()	const	//!< for each edge, gives 2 adjacent polygons; used by convex-convex code to come up with all the convex' edge normals.  
		{
			const char* PX_RESTRICT tmp = reinterpret_cast<const char* PX_RESTRICT>(mPolygons);
			tmp += sizeof(Gu::HullPolygonData) * mNbPolygons;
			tmp += sizeof(PxVec3) * mNbHullVertices;
			return reinterpret_cast<const PxU8* PX_RESTRICT>(tmp);
		}

		PX_FORCE_INLINE	const PxU8*	PX_RESTRICT_RETVAL getVertexData8()	const	//!< Vertex indices indexed by hull polygons
		{
			const char* PX_RESTRICT tmp = reinterpret_cast<const char* PX_RESTRICT>(mPolygons);
			tmp += sizeof(Gu::HullPolygonData) * mNbPolygons;
			tmp += sizeof(PxVec3) * mNbHullVertices;
			tmp += sizeof(PxU8) * mNbEdges * 2;
			return reinterpret_cast<const PxU8* PX_RESTRICT>(tmp);
		}
	};
	#if defined(PX_X64)
	PX_COMPILE_TIME_ASSERT(sizeof(Gu::ConvexHullData) == 72);
	#else
	PX_COMPILE_TIME_ASSERT(sizeof(Gu::ConvexHullData) == 64);
	#endif
} // namespace Gu

}

//#pragma PX_POP_PACK

#endif
