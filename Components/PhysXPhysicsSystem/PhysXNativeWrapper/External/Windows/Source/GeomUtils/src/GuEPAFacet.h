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


#ifndef	PX_EPA_FACET_H
#define	PX_EPA_FACET_H

#include "CmPhysXCommon.h"
#include "PsVecMath.h"
#include "PsFPU.h"

namespace physx
{

#define MaxEdges 64

namespace Gu
{
	const PxU32 lookUp[3] = {1, 2, 0};
	
	PX_FORCE_INLINE PxU32 incMod3(PxU32 i) { return lookUp[i]; } 
	
	class EdgeBuffer;
	class Edge;

	class Facet
	{
	public:
		//Facet(): m_closest( Ps::aos::V3Zero() ),/* m_dir(Ps::aos::V3Zero()),*/ m_dist(Ps::aos::FZero()), m_obsolete(false)
		//{
		//	m_adjFacets[0] = m_adjFacets[1] = m_adjFacets[2] = NULL;
		//	m_adjEdges[0] = m_adjEdges[1] = m_adjEdges[2] = -1;
		//}

		Facet()
		{
		}

		PX_FORCE_INLINE Facet(const PxU32 _i0, const PxU32 _i1, const PxU32 _i2)
			: m_closest( Ps::aos::V3Zero() ),/* m_dir(Ps::aos::V3Zero()),*/ m_dist(Ps::aos::FZero()), m_obsolete(false)
		{
			m_indices[0]=_i0;
			m_indices[1]=_i1;
			m_indices[2]=_i2;
			 
			m_adjFacets[0] = m_adjFacets[1] = m_adjFacets[2] = NULL;
			m_adjEdges[0] = m_adjEdges[1] = m_adjEdges[2] = -1;
		}

		PX_FORCE_INLINE bool Valid()
		{
			return (m_adjFacets[0] != NULL) & (m_adjFacets[1] != NULL) & (m_adjFacets[2] != NULL);
		}

		PX_FORCE_INLINE PxU32 operator[](const PxU32 i) const 
		{ 
			return m_indices[i]; 
		} 

		bool link(const PxU32 edge0, Facet* PX_RESTRICT  facet, const PxU32 edge1);

		PX_FORCE_INLINE bool isObsolete() const { return m_obsolete; }

		/*bool computeClosest(const Ps::aos::Vec3V *verts);
		bool computeClosest(const Ps::aos::Vec3V* a_verts, const Ps::aos::Vec3V* b_verts);
		bool computeClosest(const PxU32 _i0, const PxU32 _i1, const PxU32 _i2, const Ps::aos::Vec3V* a_verts, const Ps::aos::Vec3V* b_verts);*/

		PX_FORCE_INLINE const Ps::aos::Vec3V getClosest() const 
		{ 
			return m_closest; 
		}

		PX_FORCE_INLINE void setClosest(const Ps::aos::Vec3VArg closest)
		{ 
			m_closest = closest;
		}


		PX_FORCE_INLINE Ps::aos::BoolV isValid(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::Vec3V* PX_RESTRICT a_verts, 
			const Ps::aos::Vec3V* PX_RESTRICT b_verts, PxI32& b1);
		PX_FORCE_INLINE Ps::aos::BoolV isValid2(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::Vec3V* PX_RESTRICT aBuf, const Ps::aos::Vec3V* PX_RESTRICT bBuf, 
		const Ps::aos::FloatVArg lower, const Ps::aos::FloatVArg upper, PxI32& b1);

		PX_FORCE_INLINE Ps::aos::FloatV getDist() const 
		{ 
			return m_dist;
		}

		void getClosestPoint(const Ps::aos::Vec3V* PX_RESTRICT aBuf, const Ps::aos::Vec3V* PX_RESTRICT bBuf, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB) const;
		void silhouette(const Ps::aos::Vec3VArg w, EdgeBuffer& edgeBuffer, Edge* const stack);
		
		bool operator <(const Facet& b) const
		{
			return m_UDist < b.m_UDist;
		}
		bool operator > (const Facet& b) const
		{
			return m_UDist > b.m_UDist;
		}
		bool operator <=(const Facet& b) const
		{
			return m_UDist <= b.m_UDist;
		}
		bool operator >=(const Facet& b) const
		{
			return m_UDist >= b.m_UDist;
		}
	 
	//private:
		PX_FORCE_INLINE void silhouette(const PxU32 index, const Ps::aos::Vec3VArg w, EdgeBuffer& edgeBuffer, Edge* PX_RESTRICT  const stack);

		Ps::aos::Vec3V m_closest; //closest point to the origin
		Ps::aos::Vec3V m_closestA;
		Ps::aos::Vec3V m_closestB;
		Ps::aos::FloatV m_dist;

		//PxF32 m_FDist;
		PxU32 m_UDist;
		Facet* PX_RESTRICT m_adjFacets[3]; //the triangle adjacent to edge i in this triangle
		PxI32 m_adjEdges[3]; //the edge connected with the corresponding triangle
		PxI32 m_indices[3]; //the index of vertices of the triangle
		bool m_obsolete; //a flag to denote whether the triangle is visible from the new support point

	};

	class Edge 
	{
	public:
		PX_FORCE_INLINE Edge() {}
		PX_FORCE_INLINE Edge(Facet * PX_RESTRICT facet, const PxU32 index) : m_facet(facet), m_index(index) {}
		PX_FORCE_INLINE Edge(const Edge& other) : m_facet(other.m_facet), m_index(other.m_index){}

		PX_FORCE_INLINE Edge& operator = (const Edge& other)
		{
			m_facet = other.m_facet;
			m_index = other.m_index;
			return *this;
		}

		PX_FORCE_INLINE Facet *getFacet() const { return m_facet; }
		PX_FORCE_INLINE PxU32 getIndex() const { return m_index; }

		PX_FORCE_INLINE PxU32 getSource() const
		{
			PX_ASSERT(m_index < 3);
			return (*m_facet)[m_index];
		}

		PX_FORCE_INLINE PxU32 getTarget() const
		{
			PX_ASSERT(m_index < 3);
			return (*m_facet)[incMod3(m_index)];
		}

		Facet* PX_RESTRICT m_facet;
		PxU32 m_index;
	};


	class EdgeBuffer
	{
	public:
		EdgeBuffer() : m_Size(0)
		{
		}

		Edge* Insert(const Edge& edge) PX_RESTRICT
		{
			PX_ASSERT(m_Size < MaxEdges);
			Edge* PX_RESTRICT pEdge = &m_pEdges[m_Size++];
			*pEdge = edge;
			return pEdge;
		}

		Edge* Insert(Facet* PX_RESTRICT  facet, const PxU32 index)  PX_RESTRICT
		{
			//const Edge edge(facet, index);
			PX_ASSERT(m_Size < MaxEdges);
			Edge* pEdge = &m_pEdges[m_Size++];
			pEdge->m_facet=facet;
			pEdge->m_index=index;
			return pEdge;
		}

		Edge* Get(const PxU32 index)  PX_RESTRICT
		{
			PX_ASSERT(index < m_Size);
			return &m_pEdges[index];
		}

		PxU32 Size()
		{
			return m_Size;
		}

		bool IsEmpty()
		{
			return m_Size == 0;
		}

		void MakeEmpty()
		{
			m_Size = 0;
		}

		Edge m_pEdges[MaxEdges];
		PxU32 m_Size;
	};

	
	PX_FORCE_INLINE void Facet::getClosestPoint(const Ps::aos::Vec3V* PX_RESTRICT aBuf, const Ps::aos::Vec3V* PX_RESTRICT bBuf, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB) const 
	{
		using namespace Ps::aos;
		const Vec3V pa0(aBuf[m_indices[0]]);
		const Vec3V pa1(aBuf[m_indices[1]]);
		const Vec3V pa2(aBuf[m_indices[2]]);
		const Vec3V pb0(bBuf[m_indices[0]]);
		const Vec3V pb1(bBuf[m_indices[1]]);
		const Vec3V pb2(bBuf[m_indices[2]]);
		const Vec3V p0 = V3Sub(pa0, pb0);
		const Vec3V v1 = V3Sub(V3Sub(pa1, pb1), p0);
		const Vec3V v2 = V3Sub(V3Sub(pa2, pb2), p0);
		const FloatV v1dv1 = V3Dot(v1, v1);
		const FloatV v1dv2 = V3Dot(v1, v2);
		const FloatV v2dv2 = V3Dot(v2, v2);
		const FloatV p0dv1 = V3Dot(p0, v1); 
		const FloatV p0dv2 = V3Dot(p0, v2);

		//const FloatV det = FSub(FMul(v1dv1, v2dv2), FMul(v1dv2, v1dv2) ); // non-negative
		const FloatV det = FNegMulSub(v1dv2, v1dv2, FMul(v1dv1, v2dv2)); // non-negative
		const FloatV recipDet = FRecip(det);//(Maths::UnsafeRcp24(Maths::VF32)det));
		PX_ASSERT(FAllGrtrOrEq(det, FZero()));
		/*const FloatV lambda1 = FSub(FMul(p0dv2, v1dv2), FMul(p0dv1, v2dv2));
		const FloatV lambda2 = FSub(FMul(p0dv1, v1dv2), FMul(p0dv2, v1dv1));*/
		const FloatV lambda1 = FNegMulSub(p0dv1, v2dv2, FMul(p0dv2, v1dv2));
		const FloatV lambda2 = FNegMulSub(p0dv2, v1dv1, FMul(p0dv1, v1dv2));

		const Vec3V a0 = V3Scale(V3Sub(pa1, pa0), lambda1);
		const Vec3V a1 = V3Scale(V3Sub(pa2, pa0), lambda2);
		const Vec3V b0 = V3Scale(V3Sub(pb1, pb0), lambda1);
		const Vec3V b1 = V3Scale(V3Sub(pb2, pb0), lambda2);
		closestA = V3MulAdd(V3Add(a0, a1), recipDet, pa0);
		closestB = V3MulAdd(V3Add(b0, b1), recipDet, pb0);

		
	}

	/*inline void Facet::getClosestPoint(const Ps::aos::Vec3V* PX_RESTRICT aBuf, const Ps::aos::Vec3V* PX_RESTRICT bBuf, Ps::aos::Vec3V& closestA, Ps::aos::Vec3V& closestB) const 
	{
		using namespace Ps::aos;
		const Vec3V pa0(aBuf[m_indices[0]]);
		const Vec3V pa1(aBuf[m_indices[1]]);
		const Vec3V pa2(aBuf[m_indices[2]]);
		const Vec3V pb0(bBuf[m_indices[0]]);
		const Vec3V pb1(bBuf[m_indices[1]]);
		const Vec3V pb2(bBuf[m_indices[2]]);
		
		const Vec3V a0 = V3Scale(V3Sub(pa1, pa0), m_lambda1);
		const Vec3V a1 = V3Scale(V3Sub(pa2, pa0), m_lambda2);
		const Vec3V b0 = V3Scale(V3Sub(pb1, pb0), m_lambda1);
		const Vec3V b1 = V3Scale(V3Sub(pb2, pb0), m_lambda2);
		closestA = V3MulAdd(V3Add(a0, a1), m_recipDet, pa0);
		closestB = V3MulAdd(V3Add(b0, b1), m_recipDet, pb0);
	}*/


	PX_FORCE_INLINE Ps::aos::BoolV Facet::isValid(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::Vec3V* PX_RESTRICT aBuf, const Ps::aos::Vec3V* PX_RESTRICT bBuf, PxI32& b1)
	{
		using namespace Ps::aos;
		const FloatV zero = FNeg(FEps());
		//const FloatV zero = FEps();

		/*const Vec3V pa0(aBuf[m_indices[0]]);
		const Vec3V pa1(aBuf[m_indices[1]]);
		const Vec3V pa2(aBuf[m_indices[2]]);

		const Vec3V pb0(bBuf[m_indices[0]]);
		const Vec3V pb1(bBuf[m_indices[1]]);
		const Vec3V pb2(bBuf[m_indices[2]]);*/

		const Vec3V pa0(aBuf[i0]);
		const Vec3V pa1(aBuf[i1]);
		const Vec3V pa2(aBuf[i2]);

		const Vec3V pb0(bBuf[i0]);
		const Vec3V pb1(bBuf[i1]);
		const Vec3V pb2(bBuf[i2]);

		const Vec3V p0 = V3Sub(pa0, pb0);
		const Vec3V p1 = V3Sub(pa1, pb1);
		const Vec3V p2 = V3Sub(pa2, pb2);

		const Vec3V v1 = V3Sub(p1, p0);
		const Vec3V v2 = V3Sub(p2, p0);

		const Vec3V _a1 = V3Sub(pa1, pa0);
		const Vec3V _a2 = V3Sub(pa2, pa0);

		const Vec3V _b1 = V3Sub(pb1, pb0);
		const Vec3V _b2 = V3Sub(pb2, pb0);

		const FloatV v1dv1 = V3Dot(v1, v1);
		const FloatV v1dv2 = V3Dot(v1, v2);
		const FloatV v2dv2 = V3Dot(v2, v2);
		const FloatV p0dv1 = V3Dot(p0, v1); 
		const FloatV p0dv2 = V3Dot(p0, v2);

		const FloatV det = FNegMulSub(v1dv2, v1dv2, FMul(v1dv1, v2dv2));//FSub( FMul(v1dv1, v2dv2), FMul(v1dv2, v1dv2) ); // non-negative
		b1= FAllGrtrOrEq(det, zero);

		const FloatV recip = FRecip(det);

		const FloatV lambda1 = FNegMulSub(p0dv1, v2dv2, FMul(p0dv2, v1dv2));
		const FloatV lambda2 = FNegMulSub(p0dv2, v1dv1, FMul(p0dv1, v1dv2));
		const FloatV sumLambda = FAdd(lambda1, lambda2);
		const Vec3V a = V3ScaleAdd(v1, lambda1, V3Scale(v2, lambda2));
		const Vec3V closest = V3ScaleAdd(a, recip, p0);

		const Vec3V tempA = V3ScaleAdd(_a1, lambda1, V3Scale(_a2, lambda2));
		const Vec3V tempB = V3ScaleAdd(_b1, lambda1, V3Scale(_b2, lambda2));
		const Vec3V closestA = V3MulAdd(tempA, recip, pa0);
		const Vec3V closestB = V3MulAdd(tempB, recip, pb0);

		m_closest=closest;
		m_closestA = closestA;
		m_closestB = closestB;

		const FloatV dist = V3Dot(closest,closest);
		PxF32_From_FloatV(dist, PX_FPTR(&m_UDist));
		m_dist = dist;    
		
		return BAnd(FIsGrtr(lambda1,zero), BAnd(FIsGrtr(lambda2, zero), FIsGrtr(det, sumLambda)));
	}

	PX_FORCE_INLINE Ps::aos::BoolV Facet::isValid2(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::Vec3V* PX_RESTRICT aBuf, const Ps::aos::Vec3V* PX_RESTRICT bBuf, 
		const Ps::aos::FloatVArg lower, const Ps::aos::FloatVArg upper, PxI32& b1)
	{
		using namespace Ps::aos;
		const FloatV zero = FNeg(FEps());
		//const FloatV zero = FEps();

		/*const Vec3V pa0(aBuf[m_indices[0]]);
		const Vec3V pa1(aBuf[m_indices[1]]);
		const Vec3V pa2(aBuf[m_indices[2]]);

		const Vec3V pb0(bBuf[m_indices[0]]);
		const Vec3V pb1(bBuf[m_indices[1]]);
		const Vec3V pb2(bBuf[m_indices[2]]);*/

		const Vec3V pa0(aBuf[i0]);
		const Vec3V pa1(aBuf[i1]);
		const Vec3V pa2(aBuf[i2]);

		const Vec3V pb0(bBuf[i0]);
		const Vec3V pb1(bBuf[i1]);
		const Vec3V pb2(bBuf[i2]);

		const Vec3V p0 = V3Sub(pa0, pb0);
		const Vec3V p1 = V3Sub(pa1, pb1);
		const Vec3V p2 = V3Sub(pa2, pb2);

		const Vec3V v1 = V3Sub(p1, p0);
		const Vec3V v2 = V3Sub(p2, p0);

		const FloatV v1dv1 = V3Dot(v1, v1);
		const FloatV v1dv2 = V3Dot(v1, v2);
		const FloatV v2dv2 = V3Dot(v2, v2);
		const FloatV p0dv1 = V3Dot(p0, v1); 
		const FloatV p0dv2 = V3Dot(p0, v2);

		const FloatV det = FNegMulSub(v1dv2, v1dv2, FMul(v1dv1, v2dv2));//FSub( FMul(v1dv1, v2dv2), FMul(v1dv2, v1dv2) ); // non-negative
		b1= FAllGrtrOrEq(det, zero);
		
		const FloatV recip = FRecip(det);

		const FloatV lambda1 = FNegMulSub(p0dv1, v2dv2, FMul(p0dv2, v1dv2));
		const FloatV lambda2 = FNegMulSub(p0dv2, v1dv1, FMul(p0dv1, v1dv2));
		const FloatV sumLambda = FAdd(lambda1, lambda2);
		
		//calculate the closest point and sqdist
		const Vec3V a = V3ScaleAdd(v1, lambda1, V3Scale(v2, lambda2));
		const Vec3V closest = V3ScaleAdd(a, recip, p0);
		const FloatV dist = V3Dot(closest,closest);
		PxF32_From_FloatV(dist, PX_FPTR(&m_UDist));

		const BoolV b2 = BAnd(FIsGrtr(lambda1,zero), BAnd(FIsGrtr(lambda2, zero), FIsGrtr(det, sumLambda)));

		const Vec3V _a1 = V3Sub(pa1, pa0);
		const Vec3V _a2 = V3Sub(pa2, pa0);

		const Vec3V _b1 = V3Sub(pb1, pb0);
		const Vec3V _b2 = V3Sub(pb2, pb0);

		const Vec3V tempA = V3ScaleAdd(_a1, lambda1, V3Scale(_a2, lambda2));
		const Vec3V tempB = V3ScaleAdd(_b1, lambda1, V3Scale(_b2, lambda2));
		const Vec3V closestA = V3MulAdd(tempA, recip, pa0);
		const Vec3V closestB = V3MulAdd(tempB, recip, pb0);

		m_closest=closest;
		m_closestA = closestA;
		m_closestB = closestB;

		m_dist = dist;    

		
		return BAnd(b2, BAnd(FIsGrtrOrEq(dist, lower), FIsGrtrOrEq(upper, dist)));
		
		//return BAnd(FIsGrtr(lambda1,zero), BAnd(FIsGrtr(lambda2, zero), FIsGrtr(det, sumLambda)));
	}

	//inline Ps::aos::BoolV Facet::isValid(const PxU32 i0, const PxU32 i1, const PxU32 i2, const Ps::aos::Vec3V* PX_RESTRICT a_verts, const Ps::aos::Vec3V* PX_RESTRICT b_verts, PxI32& b1)
	//{
	//	using namespace Ps::aos;
	//	const FloatV zero = FNeg(FEps());
	//	const Vec3V p0 = V3Sub(a_verts[i0], b_verts[i0]);
	//	const Vec3V p1 = V3Sub(a_verts[i1], b_verts[i1]);
	//	const Vec3V p2 = V3Sub(a_verts[i2], b_verts[i2]);
	//	const Vec3V v1 = V3Sub(p1, p0);
	//	const Vec3V v2 = V3Sub(p2, p0);
	//	const FloatV v1dv1 = V3Dot(v1, v1);
	//	const FloatV v1dv2 = V3Dot(v1, v2);
	//	const FloatV v2dv2 = V3Dot(v2, v2);
	//	const FloatV p0dv1 = V3Dot(p0, v1); 
	//	const FloatV p0dv2 = V3Dot(p0, v2);

	//	const FloatV detA = FMul(v1dv1, v2dv2);
	//	const FloatV detB = FMul(v1dv2, v1dv2);
	//	const FloatV lambda1A = FMul(p0dv2, v1dv2);
	//	//const FloatV lambda1B = FMul(p0dv1, v2dv2);
	//	const FloatV lambda2A = FMul(p0dv1, v1dv2);
	//	//const FloatV lambda2B = FMul(p0dv2, v1dv1);


	//	const FloatV det = FSub( detA, detB ); // non-negative
	//	//const FloatV lambda1 = FSub(lambda1A, lambda1B );
	//	const FloatV lambda1 = FNegMulSub(p0dv1, v2dv2, lambda1A );
	//	//const FloatV lambda2 = FSub(lambda2A, lambda2B );
	//	const FloatV lambda2 = FNegMulSub(p0dv2, v1dv1, lambda2A );

	//	const FloatV recip = FRecip(det);

	//	//const Vec3V a1 = V3Scale(v1, lambda1);
	//	const Vec3V a2 = V3Scale(v2, lambda2);
	//
	//	const FloatV sumLambda = FAdd(lambda1, lambda2);

	//	const BoolV bCon1 = FIsGrtr(lambda1,zero);
	//	const BoolV bCon2 = FIsGrtr(lambda2, zero);
	//	
	//	const Vec3V a = V3ScaleAdd(v1, lambda1, a2);

	//	const BoolV bCon3 = FIsGrtr(det, sumLambda);
	//	const Vec3V closest = V3MulAdd(a, recip, p0);
	//	m_closest=closest;
	//	//m_dir = closest;
	//	//sDist=closest | closest;
	//	const FloatV dist = V3Dot(closest,closest);
	//	

	//	b1= FAllGrtrOrEq(det, zero);

	//	const BoolV bCon1AndCon2 = BAnd(bCon1, bCon2 );

	//	PxF32_From_FloatV(dist, &m_FDist);
	//	m_dist = dist;

	//	return BAnd(bCon1AndCon2, bCon3 );
	//}


	PX_FORCE_INLINE void Facet::silhouette(const PxU32 _index, const Ps::aos::Vec3VArg w, EdgeBuffer& edgeBuffer, Edge* const PX_RESTRICT stack) 
	{
		using namespace Ps::aos;
		PxI32 size = 1;
		Facet* next_facet = this;
		PxI32 next_index = _index;
		while(size--)
		{
			Facet* const PX_RESTRICT f = next_facet;
			const PxU32 index( next_index );
			if(f->m_obsolete)
			{ 
				const Edge& next_e = stack[ PxMax(size-1,0) ];
				next_facet = next_e.m_facet;
				next_index = next_e.m_index;
			}
			else
			{
				const FloatV vw = V3Dot(f->m_closest, w);
				//if (V3Dot(f->m_closest, w) < f->m_dist) //the facet is not visible from w
				if(FAllGrtr(f->m_dist, vw))
				{
					edgeBuffer.Insert(f, index);
					const Edge& next_e = stack[ PxMax(size-1,0) ];
					next_facet = next_e.m_facet;
					next_index = next_e.m_index;
				} 
				else 
				{
					f->m_obsolete = true; // Facet is visible from w
					const PxU32 next(incMod3(index));
					const PxU32 next2(incMod3(next));
					stack[size].m_facet = f->m_adjFacets[next2];
					stack[size].m_index = f->m_adjEdges[next2];
					size += 2;
					next_facet = f->m_adjFacets[next];
					next_index = f->m_adjEdges[next];
				}
			}
		}
	}

	inline void Facet::silhouette(const Ps::aos::Vec3VArg w, EdgeBuffer& edgeBuffer, Edge* const PX_RESTRICT stack)
	{
		m_obsolete = true;
		for(PxU32 a = 0; a < 3; ++a)
		{
			m_adjFacets[a]->silhouette(m_adjEdges[a], w, edgeBuffer, stack);
		}
	}


	PX_FORCE_INLINE bool Facet::link(const PxU32 edge0, Facet * PX_RESTRICT facet, const PxU32 edge1) 
	{
		m_adjFacets[edge0] = facet;
		m_adjEdges[edge0] = edge1;
		facet->m_adjFacets[edge1] = this;
		facet->m_adjEdges[edge1] = edge0;

	/*	PX_ASSERT(m_indices[edge0] == facet->m_indices[incMod3(edge1)]);
		PX_ASSERT(m_indices[incMod3(edge0)] == facet->m_indices[edge1]);*/

		////insure the index of the end points of the edge is the same
		return ((m_indices[edge0] == facet->m_indices[incMod3(edge1)]) &
		(m_indices[incMod3(edge0)] == facet->m_indices[edge1]));

	}      

}

}

#endif
