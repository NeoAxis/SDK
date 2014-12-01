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


#ifndef PX_EPA_H
#define PX_EPA_H

#include "CmPhysXCommon.h"
#include "GuEPAFacet.h"
#include "PsAllocator.h"
#include "GuConvexSupportTable.h"
#include "GuGJKWrapper.h"


namespace physx
{

#define MaxFacets 64
#define MaxSupportPoints 128

namespace Gu
{

	PxGJKStatus RecalculateSimplex(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport, Ps::aos::Vec3V* PX_RESTRICT D, PxU32 _size, Ps::aos::Vec3V& contactA, Ps::aos::Vec3V& contactB, Ps::aos::Vec3V& normal, Ps::aos::FloatV& penetrationDepth,
		const Ps::aos::BoolVArg aQuadratic, const Ps::aos::BoolVArg bQuadratic);

	class ConvexV;

	template<class Element, PxU32 Size>
	class BinaryHeap 
	{
	public:
		BinaryHeap() 
		{
			heapSize = 0;
		}
		
		~BinaryHeap() 
		{
		}
		
		inline Element* getTop() 
		{
			//return heapTop;//data[0];
			return data[0];
		}
		
		inline bool isEmpty()
		{
			return (heapSize == 0);
		}
		
		inline void makeEmpty()
		{
			heapSize = 0;
		}

		PX_FORCE_INLINE void insert(Element* value)
		{
			PX_ASSERT((PxU32)heapSize < Size);
			PxU32 newIndex;
			PxI32 parentIndex = parent(heapSize);
			for (newIndex = heapSize; newIndex > 0 && (*data[parentIndex]) > (*value); newIndex = parentIndex, parentIndex= parent(newIndex)) 
			{
				data[ newIndex ] = data[parentIndex];
			}
			data[newIndex] = value; 
			heapSize++;
			PX_ASSERT(isValid());
		}


		PX_FORCE_INLINE Element* deleteTop() PX_RESTRICT
		{
			PX_ASSERT(heapSize > 0);
			PxI32 i, child;
			Element* PX_RESTRICT min = data[0];
			Element* PX_RESTRICT last = data[--heapSize];
			PX_ASSERT(heapSize != -1);
			
			for (i = 0; (child = left(i)) < heapSize; i = child) 
			{
				/* Find smaller child */
				const PxI32 rightChild = child + 1;
				/*if((rightChild < heapSize) && (*data[rightChild]) < (*data[child]))
					child++;*/
				child += ((rightChild < heapSize) & (*data[rightChild]) < (*data[child])) ? 1 : 0;

				if((*data[child]) >= (*last))
					break;

				PX_ASSERT(i >= 0 && i < Size);
				data[i] = data[child];
			}
			PX_ASSERT(i >= 0 && i < Size);
			data[ i ] = last;
			/*heapTop = min;*/
			PX_ASSERT(isValid());
			return min;
		} 

		bool isValid()
		{
			Element* min = data[0];
			for(PxI32 i=1; i<heapSize; ++i)
			{
				if((*min) > (*data[i]))
					return false;
			}

			return true;
		}


		PxI32 heapSize;
//	private:
		Element* PX_RESTRICT data[Size];
		
		inline PxI32 left(PxI32 nodeIndex) 
		{
			return (nodeIndex << 1) + 1;
		}
		
		PxI32 parent(PxI32 nodeIndex) 
		{
			return (nodeIndex - 1) >> 1;
		}
	};

	class EPA
	{
	
	public:

		EPA(PxU32 index);
		EPA(): freeFacet(0)
		{
		}
		

//#ifdef	__SPU__
		bool PenetrationDepth(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport, const Ps::aos::Vec3V* PX_RESTRICT Q, const Ps::aos::Vec3V* PX_RESTRICT A, const Ps::aos::Vec3V* PX_RESTRICT B, const PxI32 size, Ps::aos::Vec3V& pa, Ps::aos::Vec3V& pb);
		bool expandSegment(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport);
		bool expandTriangle(const ConvexV& a, const ConvexV& b, Support aSupport, Support bSupport);
//#else
//
//		template <class ConvexA, class ConvexB>
//		bool PenetrationDepth(const ConvexA& a, const ConvexB& b, const Ps::aos::Vec3V* PX_RESTRICT Q, const Ps::aos::Vec3V* PX_RESTRICT A, const Ps::aos::Vec3V* PX_RESTRICT B, const PxI32 size, Ps::aos::Vec3V& pa, Ps::aos::Vec3V& pb);
//	
//		template <class ConvexA, class ConvexB>
//		bool expandSegment(const ConvexA& a, const ConvexB& b);
//		
//		template <class ConvexA, class ConvexB>
//		bool expandTriangle(const ConvexA& a, const ConvexB& b);
//#endif

		Facet* addFacet(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::FloatVArg lower2, const Ps::aos::FloatVArg upper2);
	
		bool originInTetrahedron(const Ps::aos::Vec3VArg p1, const Ps::aos::Vec3VArg p2, const Ps::aos::Vec3VArg p3, const Ps::aos::Vec3VArg p4);
		
		//void DebugRender(DbUtil::I_DebugRender* pRender, const Maths::HVec3 PCREF a, const Maths::HVec3 PCREF b, const Maths::HVec3 PCREF c, const Maths::HVec3 PCREF d);
		
		BinaryHeap<Facet, MaxFacets> heap;
		Ps::aos::Vec3V aBuf[MaxSupportPoints];
		Ps::aos::Vec3V bBuf[MaxSupportPoints];
		Facet facetBuf[MaxFacets];
		Edge stack[MaxFacets * 2];
		EdgeBuffer edgeBuffer;
		//PxI32 num_verts;
		//PxI32 num_facets;
		PxI32 freeFacet;
	};

	inline bool EPA::originInTetrahedron(const Ps::aos::Vec3VArg p1, const Ps::aos::Vec3VArg p2, const Ps::aos::Vec3VArg p3, const Ps::aos::Vec3VArg p4)
	{
		/*using namespace Ps::aos;
		const BoolV bFalse = BFFFF();
		return BAllEq(PointOutsideOfPlane4(p1, p2, p3, p4), bFalse) == 1;*/

		using namespace Ps::aos;
		const Vec4V zero = V4Zero();
		const BoolV bFalse = BFFFF();

		const Vec3V ab = V3Sub(p2, p1);
		const Vec3V ac = V3Sub(p3, p1);
		const Vec3V ad = V3Sub(p4, p1);
		const Vec3V bd = V3Sub(p4, p2);
		const Vec3V bc = V3Sub(p3, p2);

		const Vec3V v0 = V3Cross(ab, ac);
		const Vec3V v1 = V3Cross(ac, ad);
		const Vec3V v2 = V3Cross(ad, ab);
		const Vec3V v3 = V3Cross(bd, bc);

		const FloatV signa0 = V3Dot(v0, p1);
		const FloatV signa1 = V3Dot(v1, p1);
		const FloatV signa2 = V3Dot(v2, p1);
		const FloatV signa3 = V3Dot(v3, p2);
		const FloatV signd0 = V3Dot(v0, p4);
		const FloatV signd1 = V3Dot(v1, p2);
		const FloatV signd2 = V3Dot(v2, p3);
		const FloatV signd3 = V3Dot(v3, p1);
		const Vec4V signa = V4Merge(signa0, signa1, signa2, signa3);
		const Vec4V signd = V4Merge(signd0, signd1, signd2, signd3);
		return BAllEq(V4IsGrtrOrEq(V4Mul(signa, signd), zero), bFalse) == 1;//same side, outside of the plane
	}


	PX_FORCE_INLINE Facet* EPA::addFacet(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::FloatVArg lower2, const Ps::aos::FloatVArg upper2)
	{
		using namespace Ps::aos;
		const BoolV bTrue = BTTTT();
		PX_ASSERT(i0 != i1 && i0 != i2 && i1 != i2);
		if (freeFacet < MaxFacets)
		{
			Ps::prefetch128(&facetBuf[freeFacet], 128);

			Facet * PX_RESTRICT facet = PX_PLACEMENT_NEW(&facetBuf[freeFacet],Facet(i0, i1, i2));
			PxI32 b1;
			/*const BoolV b2 = facet->isValid(i0, i1, i2, aBuf, bBuf, b1);
			const BoolV con = BAnd(b2, BAnd(FIsGrtrOrEq(facet->m_dist, lower2), FIsGrtrOrEq(upper2, facet->m_dist)));*/

			const BoolV con = facet->isValid2(i0, i1, i2, aBuf, bBuf, lower2, upper2, b1);

			if(BAllEq(con, bTrue))
			{
				heap.insert(facet);
			}
			freeFacet+=b1;
			return b1!=0 ? facet: NULL;
		}

		return NULL;

	}

	
	
}

}

#endif
