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


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Include Guard
#ifndef PX_PHYSICS_GEOMUTILS_VEC_CONVEXHULL_H
#define PX_PHYSICS_GEOMUTILS_VEC_CONVEXHULL_H

#include "GuVecConvex.h"
#include "GuConvexMeshData.h"
#include "GuBigConvexData.h"
#include "GuConvexSupportTable.h"

namespace physx
{
namespace Gu
{


	PX_FORCE_INLINE Ps::aos::FloatV CalculateConvexMargin(const Gu::ConvexHullData* hullData)
	{
		using namespace Ps::aos;
		const Vec3V minimum = Vec3V_From_PxVec3(hullData->mAABB.minimum);
		const Vec3V maximum = Vec3V_From_PxVec3(hullData->mAABB.maximum);
		const Vec3V extents = V3Scale(V3Sub(maximum, minimum), FHalf());
		
		const FloatV perc = FloatV_From_F32(0.05f);

		const FloatV min = V3ExtractMin(extents);//FMin(V3GetX(extents), FMin(V3GetY(extents), V3GetZ(extents)));
		return FMul(min, perc);
	}



	PX_FORCE_INLINE Ps::aos::Mat33V ConstructSkewMatrix(const Ps::aos::Vec3VArg scale, const Ps::aos::QuatVArg rotation) 
	{
		using namespace Ps::aos;
		const Mat33V rot = QuatGetMat33V(rotation);
		Mat33V trans = M33Trnsps(rot);
		trans.col0 = V3Scale(trans.col0, V3GetX(scale));
		trans.col1 = V3Scale(trans.col1, V3GetY(scale));
		trans.col2 = V3Scale(trans.col2, V3GetZ(scale));
		return M33MulM33(trans, rot);
	}

	PX_FORCE_INLINE void ConstructSkewMatrix(const Ps::aos::Vec3VArg scale, const Ps::aos::QuatVArg rotation, Ps::aos::Vec3V& column0, Ps::aos::Vec3V& column1, Ps::aos::Vec3V& column2) 
	{
		using namespace Ps::aos;
		Vec3V row0, row1, row2;
		QuatGetMat33V(rotation, row0, row1, row2);
		const Vec3V col0 = V3Merge(V3GetX(row0),V3GetX(row1),V3GetX(row2));
		const Vec3V col1 = V3Merge(V3GetY(row0),V3GetY(row1),V3GetY(row2));
		const Vec3V col2 = V3Merge(V3GetZ(row0),V3GetZ(row1),V3GetZ(row2));
		column0 = V3Scale(col0, V3GetX(scale));
		column1 = V3Scale(col1, V3GetY(scale));
		column2 = V3Scale(col2, V3GetZ(scale));
	}

	class ConvexHullV : public ConvexV
	{
		public:
		/**
		\brief Constructor
		*/
		PX_FORCE_INLINE ConvexHullV(): ConvexV(E_CONVEXHULL)
		{
		}

		PX_FORCE_INLINE ConvexHullV(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat) : ConvexV(E_CONVEXHULL)
		{
			using namespace Ps::aos;

			const Vec3V minimum = Vec3V_From_PxVec3(hullData->mAABB.minimum);
			const Vec3V maximum = Vec3V_From_PxVec3(hullData->mAABB.maximum);
			const Vec3V extents = V3Scale(V3Sub(maximum, minimum), FHalf());
			
			const FloatV perc = FloatV_From_F32(0.05f);

			const FloatV min = V3ExtractMin(extents);//FMin(V3GetX(extents), FMin(V3GetY(extents), V3GetZ(extents)));
			margin = FMul(min, perc);
	
			const PxVec3* tempVerts = hullData->getHullVertices();
			verts = tempVerts;
			numVerts = hullData->mNbHullVertices;

			skewInvRot = M33MulM33( _skewMat , M33Trnsps(_rot));
			const Mat33V _rotSkew = M33MulM33(_rot, _skewMat);
			rotSkew = _rotSkew;

			center = _center;
			
			Ps::prefetch128(tempVerts);
			Ps::prefetch128(tempVerts,128);
			Ps::prefetch128(tempVerts,256);
		}

	

		PX_FORCE_INLINE ConvexHullV(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const Ps::aos::FloatVArg _margin, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat) : ConvexV(E_CONVEXHULL)
		{
			using namespace Ps::aos;

			const PxVec3* tempVerts = hullData->getHullVertices();
			margin = _margin;
			verts = tempVerts;
			numVerts = hullData->mNbHullVertices;
			skewInvRot = M33MulM33( _skewMat , M33Trnsps(_rot));
			rotSkew =  M33MulM33(_rot, _skewMat);
			center = _center;
			
			Ps::prefetch128(tempVerts);
			Ps::prefetch128(tempVerts,128);
			Ps::prefetch128(tempVerts,256);
		}

	

		PX_FORCE_INLINE void initialize(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const Ps::aos::FloatVArg _margin, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat)
		{	
			const PxVec3* tempVerts = hullData->getHullVertices();
			margin = _margin;
			verts = tempVerts;
			numVerts = hullData->mNbHullVertices;
			skewInvRot = M33MulM33( _skewMat , M33Trnsps(_rot));
			rotSkew =  M33MulM33(_rot, _skewMat);
			center = _center;
			
			Ps::prefetch128(tempVerts);
			Ps::prefetch128(tempVerts,128);
			Ps::prefetch128(tempVerts,256);
		}

		PX_FORCE_INLINE Support getSupportMapping()const
		{
			return ConvexHullSupport;
		}

		PX_FORCE_INLINE SupportMargin getSupportMarginMapping()const
		{
			return ConvexHullSupportMargin;
		}

		PX_FORCE_INLINE Support getSweepSupportMapping()const
		{
			return ConvexHullSweepSupport;
		}

		Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir)const
		{
			using namespace Ps::aos;

			//convert dir into the local space of convex hull and skew it
			const Vec3V _dir =M33MulV3(skewInvRot, dir);
			//const Vec3V _dir = M33TrnspsMulV3(rot, dir);
			Vec3V p =  Vec3V_From_PxVec3(verts[0]);
			FloatV max = V3Dot(p, _dir);

			//const Vec3V shapeCenter = V3Sub(center,  offSetToCenter);

			const PxU32 numIterations = (numVerts-1)/4;

			PxU32 i = 1;
			for(PxU32 a = 0; a < numIterations; ++a)
			{
				Ps::prefetch128(&verts[i+4]);
				Vec3V p0 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p1 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p2 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p3 = Vec3V_From_PxVec3(verts[i++]);

				const FloatV d0 = V3Dot(p0, _dir);
				const FloatV d1 = V3Dot(p1, _dir);
				const FloatV d2 = V3Dot(p2, _dir);
				const FloatV d3 = V3Dot(p3, _dir);

				const BoolV con0 = FIsGrtr(d0, max);
				max = FSel(con0, d0, max);
				p = V3Sel(con0, p0, p);

				const BoolV con1 = FIsGrtr(d1, max);
				max = FSel(con1, d1, max);
				p = V3Sel(con1, p1, p);

				const BoolV con2 = FIsGrtr(d2, max);
				max = FSel(con2, d2, max);
				p = V3Sel(con2, p2, p);

				const BoolV con3 = FIsGrtr(d3, max);
				max = FSel(con3, d3, max);
				p = V3Sel(con3, p3, p);
			}
			for(; i < numVerts; ++i)
			{
				const Vec3V vertex = Vec3V_From_PxVec3(verts[i]);
				const FloatV dist = V3Dot(vertex, _dir);
				const BoolV con = FIsGrtr(dist, max);
				max = FSel(con, dist, max);
				p = V3Sel(con, vertex, p);
			}


			//translate p back to the world space
			return V3Add(center, M33MulV3(rotSkew, p));
			//return V3Add(center, M33MulV3(rot, p));
		}

		Ps::aos::Vec3V supportSweep(const Ps::aos::Vec3VArg dir)const
		{
			using namespace Ps::aos;

			//convert dir into the local space of convex hull and skew it
			const Vec3V _dir =M33MulV3(skewInvRot, dir);
			//const Vec3V _dir = M33TrnspsMulV3(rot, dir);
			Vec3V p =  Vec3V_From_PxVec3(verts[0]);
			FloatV max = V3Dot(p, _dir);

			//const Vec3V shapeCenter = V3Sub(center,  offSetToCenter);

			const PxU32 numIterations = (numVerts-1)/4;

			PxU32 i = 1;
			for(PxU32 a = 0; a < numIterations; ++a)
			{
				Ps::prefetch128(&verts[i+4]);
				Vec3V p0 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p1 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p2 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p3 = Vec3V_From_PxVec3(verts[i++]);

				const FloatV d0 = V3Dot(p0, _dir);
				const FloatV d1 = V3Dot(p1, _dir);
				const FloatV d2 = V3Dot(p2, _dir);
				const FloatV d3 = V3Dot(p3, _dir);

				const BoolV con0 = FIsGrtr(d0, max);
				max = FSel(con0, d0, max);
				p = V3Sel(con0, p0, p);

				const BoolV con1 = FIsGrtr(d1, max);
				max = FSel(con1, d1, max);
				p = V3Sel(con1, p1, p);

				const BoolV con2 = FIsGrtr(d2, max);
				max = FSel(con2, d2, max);
				p = V3Sel(con2, p2, p);

				const BoolV con3 = FIsGrtr(d3, max);
				max = FSel(con3, d3, max);
				p = V3Sel(con3, p3, p);
			}
			for(; i < numVerts; ++i)
			{
				const Vec3V vertex = Vec3V_From_PxVec3(verts[i]);
				const FloatV dist = V3Dot(vertex, _dir);
				const BoolV con = FIsGrtr(dist, max);
				max = FSel(con, dist, max);
				p = V3Sel(con, vertex, p);
			}


			//translate p back to the world space
			return V3Add(center, M33MulV3(rotSkew, p));
			//return V3Add(center, M33MulV3(rot, p));
		}  


		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin, Ps::aos::Vec3V& support)const
		{
			using namespace Ps::aos;

			//convert dir into the local space of convex hull
			const Vec3V _dir = M33MulV3(skewInvRot, dir);
			//const Vec3V _dir = M33TrnspsMulV3(rot, dir);
			Vec3V p =  Vec3V_From_PxVec3(verts[0]);
			FloatV max = V3Dot(p, _dir);
			//const Vec3V shapeCenter = V3Sub(center, offSetToCenter);

			const PxU32 numIterations = (numVerts-1)/4;

			PxU32 i = 1;
			for(PxU32 a = 0; a < numIterations; ++a)
			{
				Ps::prefetch128(&verts[i],128);
				Vec3V p0 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p1 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p2 = Vec3V_From_PxVec3(verts[i++]);
				Vec3V p3 = Vec3V_From_PxVec3(verts[i++]);

				const FloatV d0 = V3Dot(p0, _dir);
				const FloatV d1 = V3Dot(p1, _dir);
				const FloatV d2 = V3Dot(p2, _dir);
				const FloatV d3 = V3Dot(p3, _dir);

				const BoolV con0 = FIsGrtr(d0, max);
				max = FSel(con0, d0, max);
				p = V3Sel(con0, p0, p);

				const BoolV con1 = FIsGrtr(d1, max);
				max = FSel(con1, d1, max);
				p = V3Sel(con1, p1, p);

				const BoolV con2 = FIsGrtr(d2, max);
				max = FSel(con2, d2, max);
				p = V3Sel(con2, p2, p);

				const BoolV con3 = FIsGrtr(d3, max);
				max = FSel(con3, d3, max);
				p = V3Sel(con3, p3, p);
			}
			for(; i < numVerts; ++i)
			{
				const Vec3V vertex = Vec3V_From_PxVec3(verts[i]);
				const FloatV dist = V3Dot(vertex, _dir);
				const BoolV con = FIsGrtr(dist, max);
				max = FSel(con, dist, max);
				p = V3Sel(con, vertex, p);
			}

			//const Vec3V v = V3Normalise(p);
			//const Vec3V point =  V3Sub(p, V3Mul(v, margin));
			////translate p back to the world space
			//const Vec3V ret = V3Add(shapeCenter, M33MulV3(rotSkew, point));
			//support = ret;
			//return ret;

			const Vec3V x = V3UnitX();
			const Vec3V y = V3UnitY();
			const Vec3V z = V3UnitZ();
			const Vec3V sign = V3Sign(_dir);
			const BoolV bSignTest = V3IsGrtr(sign, V3Zero());
			const Vec3V tempX = V3Sel(BGetX(bSignTest), x, V3Neg(x));
			const Vec3V tempY = V3Sel(BGetY(bSignTest), y, V3Neg(y));
			const Vec3V tempZ = V3Sel(BGetZ(bSignTest), z, V3Neg(z));

			/*const Vec3V  x = V3Scale(tempX, _margin);
			const Vec3V  y = V3Scale(tempY, _margin);
			const Vec3V  z = V3Scale(tempZ, _margin);*/
		
			const Vec3V temp = V3Scale(V3Add(tempY, V3Add(tempX, tempZ)), _margin);
			const Vec3V point = V3Sub(p, temp);
			PX_ASSERT(FAllGrtrOrEq(V3Dot(temp, temp), FMul(_margin, _margin)));
			const Vec3V ret = V3Add(center, M33MulV3(rotSkew, point));
			support = ret;
			return ret;

			//return V3Add(center, M33MulV3(rot, point));
		}


		Ps::aos::Mat33V skewInvRot;
		Ps::aos::Mat33V rotSkew;
		//Ps::aos::Vec3V offSetToCenter;

		const PxVec3* verts;
		PxU32 numVerts;

		
		//Ps::aos::Mat33V rot;
	};


	class BigConvexHullV : public ConvexHullV
	{
		class TinyBitMap
		{
		public:
			PxU32 m[8];
			PX_FORCE_INLINE TinyBitMap()			{ m[0] = m[1] = m[2] = m[3] = m[4] = m[5] = m[6] = m[7] = 0;	}
			PX_FORCE_INLINE void set(PxU8 v)		{ m[v>>5] |= 1<<(v&31);											}
			PX_FORCE_INLINE bool get(PxU8 v) const	{ return (m[v>>5] & 1<<(v&31)) != 0;							}
		};


		public:
		/**
		\brief Constructor
		*/

		PX_FORCE_INLINE BigConvexHullV() :ConvexHullV()
		{
		}

		PX_FORCE_INLINE BigConvexHullV(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat) : 
		ConvexHullV(hullData, _center, _rot, _skewMat)
		{
			searchIndex = 0;
			data = hullData->mBigConvexRawData;
			//margin = Gu::CalculateConvexMargin(hullData);
    
			if(hullData->mBigConvexRawData)
			{
				Ps::prefetch128(hullData->mBigConvexRawData->mValencies);
				Ps::prefetch128(hullData->mBigConvexRawData->mValencies,128);
				Ps::prefetch128(hullData->mBigConvexRawData->mAdjacentVerts);
			}
		}

		
		PX_FORCE_INLINE BigConvexHullV(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const Ps::aos::FloatVArg _margin, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat) : 
		ConvexHullV(hullData, _center, _margin, _rot, _skewMat)
		{
			searchIndex = 0;
			data = hullData->mBigConvexRawData;

			if(hullData->mBigConvexRawData)
			{
				Ps::prefetch128(hullData->mBigConvexRawData->mValencies);
				Ps::prefetch128(hullData->mBigConvexRawData->mValencies,128);
				Ps::prefetch128(hullData->mBigConvexRawData->mAdjacentVerts);
			}
		}

		PX_FORCE_INLINE void initialize(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const Ps::aos::FloatVArg _margin, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat)
		{	
			const PxVec3* tempVerts = hullData->getHullVertices();
			margin = _margin;
			verts = tempVerts;
			numVerts = hullData->mNbHullVertices;
			skewInvRot = M33MulM33( _skewMat , M33Trnsps(_rot));
			rotSkew =  M33MulM33(_skewMat, _rot);
			center = _center;

			searchIndex = 0;
			data = hullData->mBigConvexRawData;

			if(hullData->mBigConvexRawData)
			{
				Ps::prefetch128(hullData->mBigConvexRawData->mValencies);
				Ps::prefetch128(hullData->mBigConvexRawData->mValencies,128);
				Ps::prefetch128(hullData->mBigConvexRawData->mAdjacentVerts);
			}
		}

	/*	PX_INLINE BigConvexHullV(const Gu::ConvexHullData* hullData, const Ps::aos::Vec3VArg _center, const PxF32 _margin, const Ps::aos::Mat33V& _rot, const Ps::aos::Mat33V& _skewMat = Ps::aos::M33Identity()) : ConvexHullV(hullData, _center, _margin, _rot, _skewMat)
		{
			searchIndex = 0;
			data = hullData->mBigConvexRawData;
		}*/

		PX_FORCE_INLINE Support getSupportMapping()const
		{
			return BigConvexHullSupport;
		}

		PX_FORCE_INLINE SupportMargin getSupportMarginMapping()const
		{
			return BigConvexHullSupportMargin;
		}

		PX_FORCE_INLINE Support getSweepSupportMapping()const
		{
			return BigConvexHullSweepSupport;
		}


		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir)const
		{
			using namespace Ps::aos;
		
			const Vec3V _dir = M33MulV3(skewInvRot, dir);
			const Gu::Valency* valency = data->mValencies;
			const PxU8* adjacentVerts = data->mAdjacentVerts;
			
	
			//NotSoTinyBitMap visited;
			PxU32 smallBitMap[8] = {0,0,0,0,0,0,0,0};

			PxU32 index = searchIndex;
			Vec3V maxPoint = Vec3V_From_PxVec3(verts[index]);
			FloatV max = V3Dot(maxPoint, _dir);
	
			PxU32 initialIndex = index;

			//const Vec3V shapeCenter = V3Sub(center, offSetToCenter);

			do
			{
				initialIndex = index;
				const PxU32 numNeighbours = valency[index].mCount;
				const PxU32 offset = valency[index].mOffset;

				for(PxU32 a = 0; a < numNeighbours; ++a)
				{
					const PxU32 neighbourIndex = adjacentVerts[offset + a];
					const PxU32 ind = neighbourIndex>>5;
					const PxU32 mask = 1 << (neighbourIndex & 31);
					if((smallBitMap[ind] & mask) == 0)
					{
						//visited.set(neighbourIndex);
						smallBitMap[ind] |= mask;
						const Vec3V vertex = Vec3V_From_PxVec3(verts[neighbourIndex]);
						const FloatV dist = V3Dot(vertex, _dir);
						if(FAllGrtr(dist, max))
						{
							max = dist;
							maxPoint = vertex;
							index = neighbourIndex;
						}
					}
				}
			}while(index != initialIndex);

			searchIndex = index;
			//translate p back to the world space
			return V3Add(center, M33MulV3(rotSkew, maxPoint));
		
		}

		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin)const
		{

			using namespace Ps::aos;

			const Vec3V _dir = M33MulV3(skewInvRot, dir);
	
			const Gu::Valency* valency = data->mValencies;
			const PxU8* adjacentVerts = data->mAdjacentVerts;
	
			//NotSoTinyBitMap visited;
			PX_ASSERT(numVerts <= 256);

			PxU32 smallBitMap[8] = {0,0,0,0,0,0,0,0};

			PxU32 index = searchIndex;
			Vec3V maxPoint = Vec3V_From_PxVec3(verts[index]);
			FloatV max = V3Dot(maxPoint, _dir);
			
			PxU32 initialIndex = index;

			//const Vec3V shapeCenter = V3Sub(center, offSetToCenter);

			do
			{
				initialIndex = index;
				const PxU32 numNeighbours = valency[index].mCount;
				const PxU32 offset = valency[index].mOffset;

				for(PxU32 a = 0; a < numNeighbours; ++a)
				{
					//TODO - can reduce number of variable bit shifts with 
					//by changing this loop to loop over blocks in same bit-map element.
					//This will also reduce/remove LHS hopefully...
					const PxU32 neighbourIndex = adjacentVerts[offset + a];
					const PxU32 ind = neighbourIndex>>5;
					const PxU32 mask = 1 << (neighbourIndex & 31);
					if((smallBitMap[ind] & mask) == 0)
					{
						//visited.set(neighbourIndex);
						smallBitMap[ind] |= mask;

						const Vec3V vertex = Vec3V_From_PxVec3(verts[neighbourIndex]);
						const FloatV dist = V3Dot(vertex, _dir);
						if(FAllGrtr(dist, max))
						{
							max = dist;
							maxPoint = vertex;
							index = neighbourIndex;
						}
					}
				}
			}while(index != initialIndex);

			//PX_ASSERT(bruceForceIndex == index);
			//searchIndex = index;
			////translate p back to the world space
			//const Vec3V v = V3Normalise(maxPoint);
			//const Vec3V p = V3Sub(maxPoint, V3Mul(v, margin));

			//const Vec3V finalP= V3Add(shapeCenter, M33MulV3(rotSkew, p));
			//return finalP;

			const Vec3V x = V3UnitX();
			const Vec3V y = V3UnitY();
			const Vec3V z = V3UnitZ();
			const Vec3V sign = V3Sign(_dir);
			const BoolV bSignTest = V3IsGrtr(sign, V3Zero());
			const Vec3V tempX = V3Sel(BGetX(bSignTest), x, V3Neg(x));
			const Vec3V tempY = V3Sel(BGetY(bSignTest), y, V3Neg(y));
			const Vec3V tempZ = V3Sel(BGetZ(bSignTest), z, V3Neg(z));

			/*const Vec3V  x = V3Scale(tempX, _margin);
			const Vec3V  y = V3Scale(tempY, _margin);
			const Vec3V  z = V3Scale(tempZ, _margin);*/
		
			const Vec3V temp = V3Scale(V3Add(tempY, V3Add(tempX, tempZ)), _margin);
			const Vec3V point = V3Sub(maxPoint, temp);
			PX_ASSERT(FAllGrtrOrEq(V3Dot(temp, temp), FMul(_margin, _margin)));
			return V3Add(center, M33MulV3(rotSkew, point));
		}

		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin,
			Ps::aos::Vec3V& support)const
		{

			using namespace Ps::aos;

			const Vec3V _dir = M33MulV3(skewInvRot, dir);
	
			const Gu::Valency* valency = data->mValencies;
			const PxU8* adjacentVerts = data->mAdjacentVerts;
	
			//NotSoTinyBitMap visited;
			PX_ASSERT(numVerts <= 256);

			PxU32 smallBitMap[8] = {0,0,0,0,0,0,0,0};

			PxU32 index = searchIndex;
			Vec3V maxPoint = Vec3V_From_PxVec3(verts[index]);
			FloatV max = V3Dot(maxPoint, _dir);
			
			PxU32 initialIndex = index;

			//const Vec3V shapeCenter = V3Sub(center, offSetToCenter);

			do
			{
				initialIndex = index;
				const PxU32 numNeighbours = valency[index].mCount;
				const PxU32 offset = valency[index].mOffset;

				for(PxU32 a = 0; a < numNeighbours; ++a)
				{
					//TODO - can reduce number of variable bit shifts with 
					//by changing this loop to loop over blocks in same bit-map element.
					//This will also reduce/remove LHS hopefully...
					const PxU32 neighbourIndex = adjacentVerts[offset + a];
					const PxU32 ind = neighbourIndex>>5;
					const PxU32 mask = 1 << (neighbourIndex & 31);
					if((smallBitMap[ind] & mask) == 0)
					{
						//visited.set(neighbourIndex);
						smallBitMap[ind] |= mask;

						const Vec3V vertex = Vec3V_From_PxVec3(verts[neighbourIndex]);
						const FloatV dist = V3Dot(vertex, _dir);
						if(FAllGrtr(dist, max))
						{
							max = dist;
							maxPoint = vertex;
							index = neighbourIndex;
						}
					}
				}
			}while(index != initialIndex);

			//PX_ASSERT(bruceForceIndex == index);
			searchIndex = index;
			//translate p back to the world space
			/*const Vec3V v = V3Normalise(maxPoint);
			const Vec3V p = V3Sub(maxPoint, V3Mul(v, margin));

			const Vec3V finalP= V3Add(shapeCenter, M33MulV3(rotSkew, p));
			support = finalP;
			return finalP;*/

			const Vec3V x = V3UnitX();
			const Vec3V y = V3UnitY();
			const Vec3V z = V3UnitZ();
			const Vec3V sign = V3Sign(_dir);
			const BoolV bSignTest = V3IsGrtr(sign, V3Zero());
			const Vec3V tempX = V3Sel(BGetX(bSignTest), x, V3Neg(x));
			const Vec3V tempY = V3Sel(BGetY(bSignTest), y, V3Neg(y));
			const Vec3V tempZ = V3Sel(BGetZ(bSignTest), z, V3Neg(z));

			/*const Vec3V  x = V3Scale(tempX, _margin);
			const Vec3V  y = V3Scale(tempY, _margin);
			const Vec3V  z = V3Scale(tempZ, _margin);*/
		
			const Vec3V temp = V3Scale(V3Add(tempY, V3Add(tempX, tempZ)), _margin);
			const Vec3V point = V3Sub(maxPoint, temp);
			PX_ASSERT(FAllGrtrOrEq(V3Dot(temp, temp), FMul(_margin, _margin)));
			const Vec3V ret = V3Add(center, M33MulV3(rotSkew, point));
			support = ret;
			return ret;
		}

		mutable PxU32 searchIndex;
		const BigConvexRawData* data;  
	};
}

}

#endif	// 
