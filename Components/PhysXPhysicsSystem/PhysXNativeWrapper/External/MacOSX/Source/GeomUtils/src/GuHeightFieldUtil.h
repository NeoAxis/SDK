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


#ifndef PX_PHYSICS_GEOMUTILS_PX_HEIGHTFIELDUTIL
#define PX_PHYSICS_GEOMUTILS_PX_HEIGHTFIELDUTIL

#include "PxSimpleTriangleMesh.h"
#include "PxHeightFieldGeometry.h"
#include "GuHeightField.h"
#include "PxTriangle.h"
#include "PsIntrinsics.h"
#include "GuIntersectionRayTriangle.h"
#include "GuIntersectionRayBox.h"

namespace physx
{

/**
\brief Used to control contact queries.
*/
struct PxQueryFlags
{
	enum Enum
	{
		eWORLD_SPACE	= (1<<0),	//!< world-space parameter, else object space
		eFIRST_CONTACT	= (1<<1),	//!< returns first contact only, else returns all contacts
	};
};

namespace Gu
{
	template<class T> class EntityReport;

	class PX_PHYSX_COMMON_API HeightFieldUtil
	{
	private:
	
		PX_CUDA_CALLABLE PX_FORCE_INLINE void initialize()
		{
			const PxReal absRowScale = PxAbs(mHfGeom->rowScale);
			const PxReal absColScale = PxAbs(mHfGeom->columnScale);
			PX_ASSERT(sizeof(PxHeightFieldSample().height) == 2);
			//PxReal minHeightPerSample = PX_MIN_HEIGHTFIELD_Y_SCALE;
			PX_ASSERT(mHfGeom->heightScale >= PX_MIN_HEIGHTFIELD_Y_SCALE);
			PX_ASSERT(absRowScale >= PX_MIN_HEIGHTFIELD_XZ_SCALE);
			PX_ASSERT(absColScale >= PX_MIN_HEIGHTFIELD_XZ_SCALE);
			PX_UNUSED(absRowScale);
			PX_UNUSED(absColScale);
			//using physx::intrinsics::fsel;
			//mOneOverHeightScale	= fsel(mHfGeom->heightScale - minHeightPerSample, 1.0f / mHfGeom->heightScale, 1.0f / minHeightPerSample);
			mOneOverHeightScale	= 1.0f / mHfGeom->heightScale;
			mOneOverRowScale	= 1.0f / mHfGeom->rowScale;
			mOneOverColumnScale	= 1.0f / mHfGeom->columnScale;	
		}
		
		PxReal							mOneOverRowScale;
		PxReal							mOneOverHeightScale;
		PxReal							mOneOverColumnScale;
		const Gu::HeightField*			mHeightField;
		const PxHeightFieldGeometry*	mHfGeom;

	public:
	
		//sschirm: added empty ctor for gpu shared mem allocation
		PX_FORCE_INLINE HeightFieldUtil() {}

		//sschirm: alternative constructor supporting spu pointer fixup. 
		PX_FORCE_INLINE HeightFieldUtil(const PxHeightFieldGeometry& hfGeom, const Gu::HeightField& hf) : mHeightField(&hf), mHfGeom(&hfGeom)
		{
			initialize();
		}
		
		PX_FORCE_INLINE HeightFieldUtil(const PxHeightFieldGeometry& hfGeom) : mHeightField(static_cast<const Gu::HeightField*>(hfGeom.heightField)), mHfGeom(&hfGeom)
		{
			initialize();
		}

		//sschirm: initialize with PxHeightFieldGeometry and Gu::HeightField for gpu shared mem allocation
		PX_CUDA_CALLABLE PX_FORCE_INLINE void initialize(const PxHeightFieldGeometry& hfGeom, const Gu::HeightField& hf)
		{
			mHeightField = &hf;
			mHfGeom = &hfGeom;
			initialize();
		}
		
		PX_CUDA_CALLABLE PX_FORCE_INLINE	const Gu::HeightField&			getHeightField() const			{ return *mHeightField; }
		PX_CUDA_CALLABLE PX_FORCE_INLINE	const PxHeightFieldGeometry&	getHeightFieldGeometry() const	{ return *mHfGeom; }

		PX_FORCE_INLINE	PxReal							getOneOverRowScale() const		{ return mOneOverRowScale; }
		PX_FORCE_INLINE	PxReal							getOneOverHeightScale() const	{ return mOneOverHeightScale; }
		PX_FORCE_INLINE	PxReal							getOneOverColumnScale() const	{ return mOneOverColumnScale; }
	
		void	computeLocalBounds(PxBounds3& bounds) const;
		PX_FORCE_INLINE bool	isCollisionVertex(PxU32 vertexIndex, PxU32 row, PxU32 column) const
		{
			return mHeightField->isCollisionVertex(vertexIndex, row, column, PxHeightFieldMaterial::eHOLE);
		}
		PX_CUDA_CALLABLE bool	isCollisionEdge(PxU32 edgeIndex) const;
		bool	isCollisionEdge(PxU32 edgeIndex, PxU32 count, const PxU32* PX_RESTRICT faceIndices, PxU32 cell, PxU32 row, PxU32 column) const;
		bool	isBoundaryEdge(PxU32 edgeIndex) const;
//		PxReal	getHeightAtShapePoint(PxReal x, PxReal z) const;
		PX_FORCE_INLINE	PxReal	getHeightAtShapePoint(PxReal x, PxReal z) const
		{
			return mHfGeom->heightScale * mHeightField->getHeightInternal(x * mOneOverRowScale, z * mOneOverColumnScale);
		}
		PX_FORCE_INLINE	PxReal	getHeightAtShapePoint2(PxU32 vertexIndex, PxReal fracX, PxReal fracZ) const
		{
			return mHfGeom->heightScale * mHeightField->getHeightInternal2(vertexIndex, fracX, fracZ);
		}

//		PxVec3	getNormalAtShapePoint(PxReal x, PxReal z) const;
		PX_FORCE_INLINE	PxVec3	getNormalAtShapePoint(PxReal x, PxReal z) const
		{
			return mHeightField->getNormal_(x * mOneOverRowScale, z * mOneOverColumnScale, mOneOverRowScale, mOneOverHeightScale, mOneOverColumnScale);
		}
		PX_FORCE_INLINE	PxVec3	getNormalAtShapePoint2(PxU32 vertexIndex, PxReal fracX, PxReal fracZ) const
		{
			return mHeightField->getNormal_2(vertexIndex, fracX, fracZ, mOneOverRowScale, mOneOverHeightScale, mOneOverColumnScale);
		}

		PxU32	getFaceIndexAtShapePoint(PxReal x, PxReal z) const;
		PxU32	getFaceIndexAtShapePointNoTest(PxReal x, PxReal z) const;
		PxU32	getFaceIndexAtShapePointNoTest2(PxU32 cell, PxReal fracX, PxReal fracZ) const;
		PxU32	getFaceIndexAtTriangleIndex(PxU32 triangleIndex) const;

		PxVec3	getSmoothNormalAtShapePoint(PxReal x, PxReal z) const;

		PxVec3	getVertexNormal(PxU32 vertexIndex, PxU32 row, PxU32 column) const;
//		PxVec3	getVertexNormal(PxU32 vertexIndex) const;
		PX_FORCE_INLINE PxVec3	getVertexNormal(PxU32 vertexIndex)	const
		{
			const PxU32 nbColumns = mHeightField->getData().columns;
			const PxU32 row = vertexIndex / nbColumns;
			const PxU32 column = vertexIndex % nbColumns;
			return getVertexNormal(vertexIndex, row, column);
		}

		PxU32	getVertexFaceIndex(PxU32 vertexIndex, PxU32 row, PxU32 column) const;
		void	getEdge(PxU32 edgeIndex, PxU32 cell, PxU32 row, PxU32 column, PxVec3& origin, PxVec3& extent) const;

		PxU32	getEdgeFaceIndex(PxU32 edgeIndex) const;
		PxU32	getEdgeFaceIndex(PxU32 edgeIndex, PxU32 cell, PxU32 row, PxU32 column) const;
		PxU32	getEdgeFaceIndex(PxU32 edgeIndex, PxU32 count, const PxU32* PX_RESTRICT faceIndices) const;

		// possible improvement: face index and edge index can be folded into one incremental feature index (along with vert index)
		// such as, 0: vert index for cell, 1,2: edge indices, 3,4: face indices, then wrap around in multiples of 5 or 8
		// all return arrays must have a size of 11 to accomodate the possible 11 contacts
		PxU32	findClosestPointsOnCell(
			PxU32 row, PxU32 column, PxVec3 point,
			PxVec3* PX_RESTRICT closestPoints, PxU32* PX_RESTRICT faceIndices,
			bool testEdges = true, bool testFaces = true, bool onlyFaces = false) const;

		bool	findProjectionOnTriangle(PxU32 triangleIndex, PxU32 row, PxU32 column, const PxVec3& point, PxVec3& projection) const;

//		PxReal	findClosestPointOnEdge(PxU32 edgeIndex, const PxVec3& point, PxVec3& closestPoint) const;
		PxReal	findClosestPointOnEdge(PxU32 edgeIndex, PxU32 cell, PxU32 row, PxU32 column, const PxVec3& point, PxVec3& closestPoint) const;

		PxU32	getTriangle(const PxTransform&, PxTriangle& worldTri,
			PxU32* vertexIndices, PxTriangleID triangleIndex, bool worldSpaceTranslation=true, bool worldSpaceRotation=true) const;
		bool	overlapAABBTriangles(const PxTransform&, const PxBounds3& bounds, PxU32 flags, EntityReport<PxU32>* callback) const;

		PX_FORCE_INLINE	PxVec3	computePointNormal(PxU32 meshFlags, const PxVec3& d, const PxTransform& transform, PxReal l_sqr, PxReal x, PxReal z, PxReal epsilon, PxReal& l)	const
		{
			PxVec3 n;
			if (l_sqr > epsilon)
			{
				n = transform.rotate(d);
				l = n.normalize();
			}
			else // l == 0
			{
				n = transform.rotate(getNormalAtShapePoint(x, z));
				n = n.getNormalized();
				l = PxSqrt(l_sqr);
			}
			return n;
		}

		PX_INLINE static bool hasSingleMaterial(const Gu::HeightField& hf, const PxHeightFieldGeometry& hfGeom, PxMaterialTableIndex& matIndex)
		{
			matIndex = 0xFFFF;

			if (hf.getCommonMaterialIndex0() != 0xFFFF)
			{
				// the heightfield uses at most two different materials (more than two would result in setting the pattern 0xFFFF).

				if (hf.getCommonMaterialIndex0() == PxHeightFieldMaterial::eHOLE)
					matIndex = hf.getCommonMaterialIndex1();
				if (hf.getCommonMaterialIndex1() == PxHeightFieldMaterial::eHOLE || hf.getCommonMaterialIndex1() == 0xFFFF)
					matIndex = hf.getCommonMaterialIndex0();

				return (matIndex != 0xFFFF);
			}

			return false;
		}

		PX_INLINE bool isShapePointOnHeightField(PxReal x, PxReal z) const
		{
			x *= mOneOverRowScale;
			z *= mOneOverColumnScale;
/*			return ((!(x < 0))
				&&  (!(z < 0))
				&&  (x < (mHeightField.getNbRowsFast()-1)) 
				&&  (z < (mHeightField.getNbColumnsFast()-1)));*/
			return ((x >= 0.0f)
				&&  (z >= 0.0f)
				&&  (x < (mHeightField->getData().rowLimit+1.0f)) 
				&&  (z < (mHeightField->getData().colLimit+1.0f)));
		}

		// floor and ceil don't clamp down exact integers but we want that
		static PX_FORCE_INLINE PxF32 floorDown(PxF32 x) { PxF32 f = PxFloor(x); return (f == x) ? f-1 : f; }
		static PX_FORCE_INLINE PxF32 ceilUp   (PxF32 x) { PxF32 f = PxCeil (x); return (f == x) ? f+1 : f; }

		// If useUnderFaceCalblack is false, traceSegment will report segment/triangle hits via
		//   faceHit(const Gu::HeightFieldUtil& hf, const PxVec3& point, PxU32 triangleIndex)
		// Otherwise traceSegment will report all triangles the segment passes under via
		//   underFaceHit(const Gu::HeightFieldUtil& hf, const PxVec3& triNormal, const PxVec3& crossedEdge,
		//     PxF32 x, PxF32 z, PxF32 rayHeight, PxU32 triangleIndex)
		//   where x,z is the point of previous intercept in hf coords, rayHeight is at that same point
		//   crossedEdge is the edge vector crossed from last call to underFaceHit, undefined for first call
		//   Note that underFaceHit can be called when a line is above a triangle if it's within AABB for that hf cell
		// Note that backfaceCull is ignored if useUnderFaceCallback is true

		template<class T, bool useUnderFaceCallback, bool backfaceCull>
		PX_INLINE void traceSegment(const PxVec3& aP0, const PxVec3& aP1, T* aCallback) const
		{
			PxBounds3 localBounds;
			computeLocalBounds(localBounds);

			PxVec3 rayDir = aP1 - aP0;
			PxReal tnear;
			PxReal tfar;
			if (!Gu::intersectRayAABB2(localBounds.minimum, localBounds.maximum, aP0, rayDir, 1.0f, tnear, tfar)) 
				return;

			// have to make sure we are within bounds to account for numerical inaccuracies
			// this is needed since sometimes p+td produces a point outside of the box even though t is supposed to be on the box
			const PxVec3& mn = localBounds.minimum;
			const PxVec3& mx = localBounds.maximum;
			PxVec3 p0(mx.minimum(mn.maximum(aP0 + rayDir * tnear)));
			PxVec3 p1(mx.minimum(mn.maximum(aP0 + rayDir * tfar)));

			// row = x|u, column = z|v
			PxF32 rowScale = mHfGeom->rowScale, columnScale = mHfGeom->columnScale, heightScale = mHfGeom->heightScale;

			// map p0 from (x, z, y) to (u0, v0, h0)
			const PxReal u0 = p0.x * mOneOverRowScale; // this rescales the u,v grid steps to 1
			const PxReal v0 = p0.z * mOneOverColumnScale;
			const PxReal h0 = p0.y; // we don't scale y
			const PxVec3 uhv0(u0, h0, v0);

			// map p1 from (x, z, y) to (u1, v1, h1)
			const PxReal u1 = p1.x * mOneOverRowScale;
			const PxReal v1 = p1.z * mOneOverColumnScale;
			const PxReal h1 = p1.y; // we don't scale y

			const PxReal dh = h1 - h0;
			PxReal du = u1 - u0, dv = v1 - v0;

			// grid u&v step is always either 1 or -1, we maintain integer and float copies to avoid conversions
			const PxF32 step_uif = PxSign(du), step_vif = PxSign(dv);
			const PxI32 step_ui = PxI32(step_uif), step_vi = PxI32(step_vif);


			// clamp magnitude of du, dv to at least clampEpsilon to avoid special cases when dividing
			const PxF32 clampEpsilon = 1e-10f;
			if (PxAbs(du) < clampEpsilon)
				du = step_ui * clampEpsilon;
			if (PxAbs(dv) < clampEpsilon) 
				dv = step_vi * clampEpsilon;

			const PxVec3 duhv(du, dh, dv);
			PxReal duhvLen = duhv.magnitude();
			PxVec3 duhvNorm = duhv;
			if (duhvLen > PX_NORMALIZATION_EPSILON)
				duhvNorm *= 1.0f/duhvLen;

			// Math derivation:
			// points on 2d segment are parametrized as: [u0,v0] + t [du, dv]. We solve for t_u[n], t for nth u-intercept
			// u0 + t_un du = un
			// t_un = (un-u0) / du
			// t_un1 = (un+1-u0) / du        ;  we use +1 since we rescaled the grid step to 1
			// therefore step_tu = t_un - t_un1 = 1/du

			// seed integer cell coordinates with u0, v0 rounded up or down with standard PxFloor/Ceil behavior
			// to ensure we have the correct first cell between (ui,vi) and (ui+step_ui,vi+step_vi)
			PxI32 numCols = mHeightField->getNbColumnsFast(), numRows = mHeightField->getNbRowsFast();
			// this epsilon is to take care of a situation when we start out with for instance u0=numCols-1 and step_ui=1
			// we artificially clamp the origin to a lower or higher cell to avoid bounds checks inside of the main loop
			const PxF32 clampEpsU = PxAbs(u0) * 1e-7f; // PxFloor(u0+(1-1e-7f*u0)) == u0) holds for u0 up to 100k
			const PxF32 clampEpsV = PxAbs(v0) * 1e-7f; // see computeCellCoordinates for validation code
			PxI32 ui = (du > 0.0f ? PxMax(0, PxI32(PxFloor(u0-clampEpsU))) : PxMin(numRows-1, PxI32(PxCeil(u0+clampEpsU))));
			PxI32 vi = (dv > 0.0f ? PxMax(0, PxI32(PxFloor(v0-clampEpsV))) : PxMin(numCols-1, PxI32(PxCeil(v0+clampEpsV))));

			// find the nearest integer u, v in ray traversal direction and corresponding tu and tv
			PxReal uhit0 = du > 0.0f ? ceilUp(u0) : floorDown(u0);
			PxReal vhit0 = dv > 0.0f ? ceilUp(v0) : floorDown(v0);

			// tu, tv can be > 1 but since the loop is structured as do {} while(tMin < tEnd) we still visit the first cell
			PxF32 last_tu = 0.0f, last_tv = 0.0f;
			PxReal tu = (uhit0-u0) / du;
			PxReal tv = (vhit0-v0) / dv;
			PX_ASSERT(tu >= 0.0f && tv >= 0.0f);

			// compute step_tu and step_tv - t step per grid cell in u and v direction
			PxReal step_tu = 1.0f / PxAbs(du), step_tv = 1.0f / PxAbs(dv);

			// t advances at the same rate for u, v and h therefore we can compute h at u,v grid intercepts
			#define COMPUTE_H_FROM_T(t) (h0 + (t) * dh)

			const PxF32 hEpsilon = 1e-4f;
			PxF32 uif = PxF32(ui), vif = PxF32(vi);

			// these are used to remap h values to correspond to u,v increasing order
			PxI32 uflip = 1-step_ui; /*0 or 2*/
			PxI32 vflip = (1-step_vi)/2; /*0 or 1*/

			// this epsilon is needed to ensure that we include the last [t, t+1] range in the do {} while(t<tEnd) loop
			const PxF32 tEnd = 1.0f - 1e-4f;
			PxF32 tMinUV;

			const Gu::HeightField& hf = *mHeightField;

			// seed hLinePrev as h(0)
			PxReal hLinePrev = COMPUTE_H_FROM_T(0);

			//#define BRUTE_FORCE_TEST
			#ifdef BRUTE_FORCE_TEST
			for (ui = 0, uif = 0.0f; ui < numRows-1; ui++, uif+=1.0f)
				for (vi = 0, vif = 0.0f; vi < numCols-1; vi++, vif+=1.0f)
			#else
			do
			#endif
			{
				tMinUV = PxMin(tu, tv);
				PxF32 hLineNext = COMPUTE_H_FROM_T(tMinUV);

				PX_ASSERT(ui >= 0 && ui < numRows && vi >= 0 && vi < numCols);
				PX_ASSERT(ui+step_ui >= 0 && ui+step_ui < numRows && vi+step_vi >= 0 && vi+step_vi < numCols);


				const PxU32 colIndex0 = numCols * ui + vi;
				const PxU32 colIndex1 = numCols * (ui + step_ui) + vi;
				const PxReal h[4] = { // h[0]=h00, h[1]=h01, h[2]=h10, h[3]=h11 - oriented relative to step_uv
					hf.getHeight(colIndex0) * heightScale, hf.getHeight(colIndex0 + step_vi) * heightScale,
					hf.getHeight(colIndex1) * heightScale, hf.getHeight(colIndex1 + step_vi) * heightScale };

				PxF32 minH = PxMin(PxMin(h[0], h[1]), PxMin(h[2], h[3]));
				PxF32 maxH = PxMax(PxMax(h[0], h[1]), PxMax(h[2], h[3]));

				PxF32 hLineCellRangeMin = PxMin(hLinePrev, hLineNext);
				PxF32 hLineCellRangeMax = PxMax(hLinePrev, hLineNext);

				// do a quick overlap test in h, this should be rejecting the vast majority of tests
				#ifndef BRUTE_FORCE_TEST
				if (
					!(hLineCellRangeMin-hEpsilon > maxH || hLineCellRangeMax+hEpsilon < minH) ||
					(useUnderFaceCallback && hLineCellRangeMax < maxH)
				)
				#endif
				{
					PxF32 triU, triV;

					// arrange h so that h00 corresponds to min(uif, uif+step_uif) h10 to max et c.
					// this is only needed for backface culling to work so we know the proper winding order without branches
					// uflip is 0 or 2, vflip is 0 or 1 (corresponding to positive and negative ui_step and vi_step)
					PxF32 h00 = h[0+uflip+vflip];
					PxF32 h01 = h[1+uflip-vflip];
					PxF32 h10 = h[2-uflip+vflip];
					PxF32 h11 = h[3-uflip-vflip];

					PxF32 minuif = PxMin(uif, uif+step_uif);
					PxF32 maxuif = PxMax(uif, uif+step_uif);
					PxF32 minvif = PxMin(vif, vif+step_vif);
					PxF32 maxvif = PxMax(vif, vif+step_vif);
					PxVec3 p00(minuif, h00, minvif);
					PxVec3 p01(minuif, h01, maxvif);
					PxVec3 p10(maxuif, h10, minvif);
					PxVec3 p11(maxuif, h11, maxvif);

					const PxF32 enlargeEpsilon = 0.0001f;
					const PxVec3* p00a = &p00, *p01a = &p01, *p10a = &p10, *p11a = &p11;
					PxU32 minui = PxMin(ui+step_ui, ui), minvi = PxMin(vi+step_vi, vi);
					const PxU32 vertIndex = numCols * minui + minvi;
					const PxU32 cellIndex = vertIndex - minui;
					bool isZVS = hf.isZerothVertexShared(vertIndex);
					if (!isZVS)
					{
						// rotate the pointers for flipped edge cells
						p10a = &p00;
						p00a = &p01;
						p01a = &p11;
						p11a = &p10;
					}
					// For triangle index computation, see illustration in Gu::HeightField::getTriangleNormal()
					// Since row = u, column = v
					// for zeroth vert shared the 10 index is the corner of the 0-index triangle, and 01 is 1-index
					// if zeroth vertex is not shared, the 00 index is the corner of 0-index triangle
					if (!useUnderFaceCallback)
					{
						#define ISHOLE0 (hf.getMaterialIndex0(vertIndex) == PxHeightFieldMaterial::eHOLE)
						#define ISHOLE1 (hf.getMaterialIndex1(vertIndex) == PxHeightFieldMaterial::eHOLE)
						const PxF32 tEpsilon = 1e-5f;
						PxReal triT0 = PX_MAX_REAL, triT1 = PX_MAX_REAL;
						bool hit0 = false, hit1 = false;
						if (Gu::intersectLineTriangle(uhv0, duhvNorm, *p10a, *p00a, *p11a, triT0, triU, triV, backfaceCull, enlargeEpsilon) &&
							triT0 >= -tEpsilon && triT0 <= duhvLen+tEpsilon && !ISHOLE0)
						{
							hit0 = true;
						} else
							triT0 = PX_MAX_REAL;
						if (Gu::intersectLineTriangle(uhv0, duhvNorm, *p01a, *p11a, *p00a, triT1, triU, triV, backfaceCull, enlargeEpsilon)
							&& triT1 >= -tEpsilon && triT1 <= duhvLen+tEpsilon && !ISHOLE1)
						{
							hit1 = true;
						} else
							triT1 = PX_MAX_REAL;

						if (hit0 && triT0 <= triT1)
						{
							PxVec3 hitPoint((u0 + duhvNorm.x*triT0) * rowScale, h0 + duhvNorm.y * triT0, (v0 + duhvNorm.z*triT0) * columnScale);
							if (!aCallback->faceHit(*this, hitPoint, cellIndex*2))
								return;
						}
						else if (hit1 && triT1 <= triT0)
						{
							PxVec3 hitPoint((u0 + duhvNorm.x*triT1) * rowScale, h0 + duhvNorm.y * triT1, (v0 + duhvNorm.z*triT1) * columnScale);
							if (!aCallback->faceHit(*this, hitPoint, cellIndex*2 + 1))
								return;
						}
						#undef ISHOLE0
						#undef ISHOLE1
					}
					else
					{
						// TODO: quite a few optimizations are possible here. edges can be shared, intersectRayTriangle inlined etc
						// Go to shape space. Height is already in shape space so we only scale x and z
						PxVec3 p00s(p00a->x * rowScale, p00a->y, p00a->z * columnScale);
						PxVec3 p01s(p01a->x * rowScale, p01a->y, p01a->z * columnScale);
						PxVec3 p10s(p10a->x * rowScale, p10a->y, p10a->z * columnScale);
						PxVec3 p11s(p11a->x * rowScale, p11a->y, p11a->z * columnScale);

						PxVec3 triNormals[2] = { (p00s - p10s).cross(p11s - p10s), (p11s - p01s).cross(p00s-p01s) };
						triNormals[0] *= PxRecipSqrt(triNormals[0].magnitudeSquared());
						triNormals[1] *= PxRecipSqrt(triNormals[1].magnitudeSquared());
						// since the heightfield can be mirrored with negative rowScale or columnScale, this assert doesn't hold
						//PX_ASSERT(triNormals[0].y >= 0.0f && triNormals[1].y >= 0.0f);

						// at this point we need to compute the edge direction that we crossed
						// also since we don't DDA the w we need to find u,v for w-intercept (w refers to diagonal adjusted with isZVS)
						PxF32 wnu = isZVS ? -1.0f : 1.0f, wnv = 1.0f; // uv-normal to triangle edge that splits the cell
						PxF32 wpu = uif + 0.5f * step_uif, wpv = vif + 0.5f * step_vif; // a point on triangle edge that splits the cell
						// note that (wpu, wpv) is on both edges (for isZVS and non-ZVS cases) which is nice

						// we clamp tNext to 1 because we still want to issue callbacks even if we stay in one cell
						// note that tNext can potentially be arbitrarily large for a segment contained within a cell
						PxF32 tNext = PxMin(PxMin(tu, tv), 1.0f), tPrev = PxMax(last_tu, last_tv);

						// compute uvs corresponding to tPrev, tNext
						PxF32 unext = u0 + tNext*du, vnext = v0 + tNext*dv;
						PxF32 uprev = u0 + tPrev*du, vprev = v0 + tPrev*dv;

						const PxReal& h00_ = h[0], &h01_ = h[1], &h10_ = h[2]/*, h11_ = h[3]*/; // aliases for step-oriented h

						// (wpu, wpv) is a point on the diagonal
						// we compute a dot of ((unext, vnext) - (wpu, wpv), wn) to see on which side of triangle edge we are
						// if the dot is positive we need to add 1 to triangle index
						PxU32 dotPrevGtz = ((uprev - wpu) * wnu + (vprev - wpv) * wnv) > 0;
						PxU32 dotNextGtz = ((unext - wpu) * wnu + (vnext - wpv) * wnv) > 0;
						PxU32 triIndex0 = cellIndex*2 + dotPrevGtz;
						PxU32 triIndex1 = cellIndex*2 + dotNextGtz;
						PxU32 isHole0 = (hf.getMaterialIndex0(vertIndex) == PxHeightFieldMaterial::eHOLE);
						PxU32 isHole1 = (hf.getMaterialIndex1(vertIndex) == PxHeightFieldMaterial::eHOLE);
						if (triIndex0 > triIndex1)
							shdfnd::swap<PxU32>(isHole0, isHole1);

						// TODO: compute height at u,v inside here, change callback param to PxVec3
						PxVec3 crossedEdge;
						if (last_tu > last_tv) // previous intercept was at u, so we use u=const edge
							crossedEdge = PxVec3(0.0f, h01_-h00_, step_vif * columnScale);
						else // previous intercept at v, use v=const edge
							crossedEdge = PxVec3(step_uif * rowScale, h10_-h00_, 0.0f);

						if (!isHole0 && !aCallback->underFaceHit(*this, triNormals[dotPrevGtz], crossedEdge,
								uprev * rowScale, vprev * columnScale, COMPUTE_H_FROM_T(tPrev), triIndex0))
							return;

						if (triIndex1 != triIndex0 && !isHole1) // if triIndex0 != triIndex1 that means we cross the triangle edge
						{
							// Need to compute tw, the t for ray intersecting the diagonal within the current cell
							// dot((wnu, wnv), (u0+tw*du, v0+tw*dv)-(wpu, wpv)) = 0
							// wnu*(u0+tw*du-wpu) + wnv*(v0+tw*dv-wpv) = 0
							// wnu*u0+wnv*v0-wnu*wpu-wnv*wpv + tw*(du*wnu + dv*wnv) = 0
							PxF32 denom = du*wnu + dv*wnv;
							if (PxAbs(denom) > 1e-6f)
							{
								PxF32 tw = (wnu*(wpu-u0)+wnv*(wpv-v0)) / denom;
								if (!aCallback->underFaceHit(*this, triNormals[dotNextGtz], p10s-p01s,
										(u0+tw*du) * rowScale, (v0+tw*dv) * columnScale, COMPUTE_H_FROM_T(tw), triIndex1))
									return;
							}
						}
					}
				}

				#ifndef BRUTE_FORCE_TEST
				if (tu < tv)
				{
					last_tu = tu;
					ui += step_ui;
					uif += step_uif;
					tu += step_tu;
				}
				else
				{
					last_tv = tv;
					vi += step_vi;
					vif += step_vif;
					tv += step_tv;
				}
				hLinePrev = hLineNext;
				#endif
			}
			#ifndef BRUTE_FORCE_TEST
			// since min(tu,tv) is the END of the active interval we need to check if PREVIOUS min(tu,tv) was past interval end
			// since we update tMinUV in the beginning of the loop, at this point it stores the min(last tu,last tv)
			while (tMinUV < tEnd);
			#endif

			#undef COMPUTE_H
		}

		PX_FORCE_INLINE	PxVec3 hf2shapen(const PxVec3& v) const
		{
			return PxVec3(v.x * mOneOverRowScale, v.y * mOneOverHeightScale, v.z * mOneOverColumnScale);
		}

		PX_CUDA_CALLABLE PX_FORCE_INLINE PxVec3 shape2hfp(const PxVec3& v) const
		{
			return PxVec3(v.x * mOneOverRowScale, v.y * mOneOverHeightScale, v.z * mOneOverColumnScale);
		}

		PX_CUDA_CALLABLE PX_FORCE_INLINE PxVec3 hf2shapep(const PxVec3& v) const
		{
			return PxVec3(v.x * mHfGeom->rowScale, v.y * mHfGeom->heightScale, v.z * mHfGeom->columnScale);
		}

		PX_INLINE PxVec3 hf2worldp(const PxTransform& pose, const PxVec3& v) const
		{
			PxVec3 s = hf2shapep(v);
			return pose.transform(s);
		}

		PX_INLINE PxVec3 hf2worldn(const PxTransform& pose, const PxVec3& v) const
		{
			PxVec3 s = hf2shapen(v);
			return pose.q.rotate(s);
		}

#ifdef REMOVED
bool clipShapeNormalToEdgeVoronoi(PxVec3& normal, PxU32 edgeIndex, PxU32 cell, PxU32 row, PxU32 column) const
{
//	const PxU32 cell = edgeIndex / 3;
	PX_ASSERT(cell == edgeIndex / 3);
//	const PxU32 row = cell / mHeightField.getNbColumnsFast();
//	const PxU32 column = cell % mHeightField.getNbColumnsFast();
	PX_ASSERT(row == cell / mHeightField.getNbColumnsFast());
	PX_ASSERT(column == cell % mHeightField.getNbColumnsFast());

	//PxcHeightFieldFormat format = getFormatFast();
//	PxHeightFieldFormat::Enum format = mHeightField.getFormatFast();

	bool result = false;

//	switch (edgeIndex % 3)
	switch (edgeIndex - cell*3)
	{
	case 0:
		if (row > 0) 
		{
			//const PxcHeightFieldSample& sample = getSample(cell - getNbColumnsFast());
			//if(isZerothVertexShared(cell - getNbColumnsFast())) 
			if(mHeightField.isZerothVertexShared(cell - mHeightField.getNbColumnsFast())) 
			{
				//if (getMaterialIndex0(cell - getNbColumnsFast()) != getHoleMaterial())
				if (mHeightField.getMaterialIndex0(cell - mHeightField.getNbColumnsFast()) != PxHeightFieldMaterial::eHOLE)
				{
					//      <------ COL  
					//       +----+  0  R
					//       |1  /  /^  O
					//       |  /  / #  W
					//       | /  /  #  |
					//       |/  / 0 #  |
					//       +  2<===1  |
					//                  |
					//                  |
					//                  |
					//                  |
					//                  |
					//                  |
					//                  V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell - getNbColumnsFast());
					//PxReal h1 = getHeightScale() * getHeight(cell);
					//PxReal h2 = getHeightScale() * getHeight(cell + 1);
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell - mHeightField.getNbColumnsFast());
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell + 1);
					//PxVec3 eC;
					//eC.set(0, h2-h1, getColumnScale());
					const PxVec3 eC(0, h2-h1, mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(-getRowScale(), h0-h1, 0);
					const PxVec3 eR(-mHfGeom.rowScale, h0-h1, 0);
					const PxVec3 e = eR - eC * eC.dot(eR) / eC.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
			else
			{
				//if (getMaterialIndex1(cell - getNbColumnsFast()) != getHoleMaterial())
				if (mHeightField.getMaterialIndex1(cell - mHeightField.getNbColumnsFast()) != PxHeightFieldMaterial::eHOLE)
				{
					//      <------ COL  
					//       0  +----+  R
					//       ^\  \ 0 |  O
					//       # \  \  |  W
					//       #  \  \ |  |
					//       # 1 \  \|  |
					//       1===>2  +  |
					//                  |
					//                  |
					//                  |
					//                  |
					//                  |
					//                  |
					//                  V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell - getNbColumnsFast() + 1);
					//PxReal h1 = getHeightScale() * getHeight(cell + 1);
					//PxReal h2 = getHeightScale() * getHeight(cell);
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell - mHeightField.getNbColumnsFast() + 1);
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + 1);
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					//PxVec3 eC;
					//eC.set(0, h2-h1, -getColumnScale());
					const PxVec3 eC(0, h2-h1, -mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(-getRowScale(), h0-h1, 0);
					const PxVec3 eR(-mHfGeom.rowScale, h0-h1, 0);
					const PxVec3 e = eR - eC * eC.dot(eR) / eC.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
		}
		//if (row < getNbRowsFast() - 1) 
		if (row < mHeightField.getNbRowsFast() - 1) 
		{
			//const PxcHeightFieldSample& sample = getSample(cell);
			//if(isZerothVertexShared(cell)) 
			if(mHeightField.isZerothVertexShared(cell)) 
			{
				//if (getMaterialIndex1(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex1(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//      <------ COL  
					//                  R
					//                  O
					//                  W
					//                  |
					//                  |
					//                  |
					//       0===>2  0  |
					//       # 1 /  /|  |
					//       #  /  / |  |
					//       # /  /  |  |
					//       V/  / 0 |  |
					//       1  +----+  |
					//                  V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell + 1);
					//PxReal h1 = getHeightScale() * getHeight(cell + getNbColumnsFast() + 1);
					//PxReal h2 = getHeightScale() * getHeight(cell + getNbColumnsFast());
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell + 1);
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast() + 1);
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
					//PxVec3 eC;
					//eC.set(0, h2-h0, -getColumnScale());
					const PxVec3 eC(0, h2-h0, -mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(getRowScale(), h1-h0, 0);
					const PxVec3 eR(mHfGeom.rowScale, h1-h0, 0);
					const PxVec3 e = eR - eC * eC.dot(eR) / eC.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
			else
			{
				//if (getMaterialIndex0(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex0(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//      <------ COL  
					//                  R
					//                  O
					//                  W
					//                  |
					//                  |
					//                  |
					//       +  2<===0  |
					//       |\  \ 0 #  |
					//       | \  \  #  |
					//       |  \  \ #  |
					//       |1  \  \V  |
					//       +----+  1  |
					//                  V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell);
					//PxReal h1 = getHeightScale() * getHeight(cell + getNbColumnsFast());
					//PxReal h2 = getHeightScale() * getHeight(cell + 1);
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell + 1);
					//PxVec3 eC;
					//eC.set(0, h2-h0, getColumnScale());
					const PxVec3 eC(0, h2-h0, mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(getRowScale(), h1-h0, 0);
					const PxVec3 eR(mHfGeom.rowScale, h1-h0, 0);
					const PxVec3 e = eR - eC * eC.dot(eR) / eC.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}							}
			}
		}
		break;
	case 1:
		//if ((row < getNbRowsFast() - 1) && (column < getNbColumnsFast() - 1))
		if ((row < mHeightField.getNbRowsFast() - 1) && (column < mHeightField.getNbColumnsFast() - 1))
		{
			//const PxcHeightFieldSample& sample = getSample(cell);

			//PxReal h0 = getHeightScale() * getHeight(cell);
			//PxReal h1 = getHeightScale() * getHeight(cell + 1);
			//PxReal h2 = getHeightScale() * getHeight(cell + getNbColumnsFast());
			//PxReal h3 = getHeightScale() * getHeight(cell + getNbColumnsFast() + 1);
			const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell);
			const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + 1);
			const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
			const PxReal h3 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast() + 1);

			//if (isZerothVertexShared(cell))
			if (mHeightField.isZerothVertexShared(cell))
			{
				//      <------ COL  
				//          1<---0  R
				//          |1  /|  O
				//          |  / |  W
				//          | /  |  |
				//          |V 0 V  |
				//          3----2  |
				//                  V
				//      
				//PxVec3 eD;
				//eD.set(getRowScale(), h3-h0, getColumnScale());
				const PxVec3 eD(mHfGeom.rowScale, h3-h0, mHfGeom.columnScale);
				const PxReal DD = eD.magnitudeSquared();

				//if (getMaterialIndex0(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex0(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//PxVec3 eR;
					//eR.set(getRowScale(), h2-h0, 0);
					const PxVec3 eR(mHfGeom.rowScale, h2-h0, 0);
					const PxVec3 e = eR - eD * eD.dot(eR) / DD;
					const PxReal proj = e.dot(normal);
					if (proj > 0) {
						normal -= e * proj / e.magnitudeSquared();
						result = true;
					}
				}

				//if (getMaterialIndex1(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex1(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//PxVec3 eC;
					//eC.set(0, h1-h0, getColumnScale());
					const PxVec3 eC(0, h1-h0, mHfGeom.columnScale);
					const PxVec3 e = eC - eD * eD.dot(eC) / DD;
					const PxReal proj = e.dot(normal);
					if (proj > 0) 
					{
						normal -= e * proj / e.magnitudeSquared();
						result = true;
					}
				}
			}
			else 
			{
				//      <------ COL  
				//          1--->0  R
				//          |\ 0 |  O
				//          | \  |  W
				//          |  \ |  |
				//          V 1 V|  |
				//          3----2  |
				//                  V
				//      
				//PxVec3 eD;
				//eD.set(getRowScale(), h2-h1, -getColumnScale());
				const PxVec3 eD(mHfGeom.rowScale, h2-h1, -mHfGeom.columnScale);
				const PxReal DD = eD.magnitudeSquared();

				//if (getMaterialIndex0(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex0(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//PxVec3 eC;
					//eC.set(0, h0-h1, -getColumnScale());
					const PxVec3 eC(0, h0-h1, -mHfGeom.columnScale);
					const PxVec3 e = eC - eD * eD.dot(eC) / DD;
					const PxReal proj = e.dot(normal);
					if (proj > 0) 
					{
						normal -= e * proj / e.magnitudeSquared();
						result = true;
					}
				}

				//if (getMaterialIndex1(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex1(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//PxVec3 eR;
					//eR.set(getRowScale(), h3-h1, 0);
					const PxVec3 eR(mHfGeom.rowScale, h3-h1, 0);
					const PxVec3 e = eR - eD * eD.dot(eR) / DD;
					const PxReal proj = e.dot(normal);
					if (proj > 0) 
					{
						normal -= e * proj / e.magnitudeSquared();
						result = true;
					}
				}
			}
		}
		break;
	case 2:
		if (column > 0)
		{
			//const PxcHeightFieldSample& sample = getSample(cell - 1);

			//if(isZerothVertexShared(cell - 1)) 
			if(mHeightField.isZerothVertexShared(cell - 1)) 
			{
				//if (getMaterialIndex1(cell - 1) != getHoleMaterial())
				if (mHeightField.getMaterialIndex1(cell - 1) != PxHeightFieldMaterial::eHOLE)
				{
					//      <-------------- COL  
					//                1===>0  + R
					//                + 1 /  /| O
					//                +  /  / | W
					//                + /  /  | |
					//                V/  / 0 | |
					//                2  +----+ V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell - 1);
					//PxReal h1 = getHeightScale() * getHeight(cell);
					//PxReal h2 = getHeightScale() * getHeight(cell + getNbColumnsFast());
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell - 1);
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
					//PxVec3 eC;
					//eC.set(0,h0-h1,-getColumnScale());
					const PxVec3 eC(0,h0-h1,-mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(getRowScale(),h2-h1,0);
					const PxVec3 eR(mHfGeom.rowScale,h2-h1,0);
					const PxVec3 e = eC - eR * eR.dot(eC) / eR.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
			else
			{
				//if (getMaterialIndex1(cell - 1) != getHoleMaterial())
				if (mHeightField.getMaterialIndex1(cell - 1) != PxHeightFieldMaterial::eHOLE)
				{
					//      <-------------- COL  
					//                2  +----+ R
					//                ^\  \ 0 | O
					//                + \  \  | W
					//                +  \  \ | |
					//                + 1 \  \| |
					//                1===>0  + V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell - 1 + getNbColumnsFast());
					//PxReal h1 = getHeightScale() * getHeight(cell + getNbColumnsFast());
					//PxReal h2 = getHeightScale() * getHeight(cell);
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell - 1 + mHeightField.getNbColumnsFast());
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					//PxVec3 eC;
					//eC.set(0,h0-h1,-getColumnScale());
					const PxVec3 eC(0,h0-h1,-mHfGeom.columnScale);
					//PxVec3 eR;
					//eC.set(-getRowScale(),h2-h1,0);
					//eC.set(-mHfGeom.rowScale,h2-h1,0);
					const PxVec3 eR(-mHfGeom.rowScale,h2-h1,0);	// PT: I assume this was eR, not eC !!!!!
					const PxVec3 e = eC - eR * eR.dot(eC) / eR.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
		}
		//if (column < getNbColumnsFast() - 1)
		if (column < mHeightField.getNbColumnsFast() - 1)
		{
			//const PxcHeightFieldSample& sample = getSample(cell);

			//if (isZerothVertexShared(cell)) 
			if (mHeightField.isZerothVertexShared(cell)) 
			{
				//if (getMaterialIndex0(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex0(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//      <-------------- COL  
					//       +----+  2          R
					//       | 1 /  /^          O
					//       |  /  / +          W
					//       | /  /  +          |
					//       |/  / 0 +          |
					//       +  1<===0          V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell + getNbColumnsFast());
					//PxReal h1 = getHeightScale() * getHeight(cell + getNbColumnsFast() + 1);
					//PxReal h2 = getHeightScale() * getHeight(cell);
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast() + 1);
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					//PxVec3 eC;
					//eC.set(0,h1-h0,getColumnScale());
					const PxVec3 eC(0,h1-h0,mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(-getRowScale(),h2-h0,0);
					const PxVec3 eR(-mHfGeom.rowScale,h2-h0,0);
					const PxVec3 e = eC - eR * eR.dot(eC) / eR.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) 
					{
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
			else
			{
				//if (getMaterialIndex0(cell) != getHoleMaterial())
				if (mHeightField.getMaterialIndex0(cell) != PxHeightFieldMaterial::eHOLE)
				{
					//      <-------------- COL  
					//       +  1<===0          R
					//       |\  \ 0 +          O
					//       | \  \  +          W
					//       |  \  \ +          |
					//       | 1 \  \V          |
					//       +----+  2          V
					//      
					//PxReal h0 = getHeightScale() * getHeight(cell);
					//PxReal h1 = getHeightScale() * getHeight(cell + 1);
					//PxReal h2 = getHeightScale() * getHeight(cell + getNbColumnsFast());
					const PxReal h0 = mHfGeom.heightScale * mHeightField.getHeight(cell);
					const PxReal h1 = mHfGeom.heightScale * mHeightField.getHeight(cell + 1);
					const PxReal h2 = mHfGeom.heightScale * mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
					//PxVec3 eC;
					//eC.set(0,h1-h0,getColumnScale());
					const PxVec3 eC(0,h1-h0,mHfGeom.columnScale);
					//PxVec3 eR;
					//eR.set(getRowScale(),h2-h0,0);
					const PxVec3 eR(mHfGeom.rowScale,h2-h0,0);
					const PxVec3 e = eC - eR * eR.dot(eC) / eR.magnitudeSquared();
					const PxReal s = normal.dot(e);
					if (s > 0) {
						normal -= e * s / e.magnitudeSquared();
						result = true;
					}
				}
			}
		}
		break;
	}
	return result;
}
#endif

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ptchernev TODO: this is wrong it only clips in x and z
		bool clipShapeNormalToVertexVoronoi(PxVec3& normal, PxU32 vertexIndex, PxU32 row, PxU32 column) const
		{
			//PxU32 row    = vertexIndex / getNbColumnsFast();
			//PxU32 column = vertexIndex % getNbColumnsFast();
//			const PxU32 row    = vertexIndex / mHeightField.getNbColumnsFast();
//			const PxU32 column = vertexIndex % mHeightField.getNbColumnsFast();
			PX_ASSERT(row == vertexIndex / mHeightField->getNbColumnsFast());
			PX_ASSERT(column == vertexIndex % mHeightField->getNbColumnsFast());

			//PxReal h0 = getHeight(vertexIndex);
			const PxReal h0 = mHeightField->getHeight(vertexIndex);

			bool result = false;

			if (row > 0)
			{
				// - row
				//PxVec3 e;
				//e.set(-getRowScale(), getHeightScale() * (getHeight(vertexIndex - getNbColumnsFast()) - h0), 0);
				const PxVec3 e(-mHfGeom->rowScale, mHfGeom->heightScale * (mHeightField->getHeight(vertexIndex - mHeightField->getNbColumnsFast()) - h0), 0);
				const PxReal proj = e.dot(normal);
				if (proj > 0) 
				{
					normal -= e * proj / e.magnitudeSquared();
					result = true;
				}
			}

			//if (row < getNbRowsFast() - 1)
			if (row < mHeightField->getNbRowsFast() - 1)
			{
				// + row
				//PxVec3 e;
				//e.set(getRowScale(), getHeightScale() * (getHeight(vertexIndex + getNbColumnsFast()) - h0), 0);
				const PxVec3 e(mHfGeom->rowScale, mHfGeom->heightScale * (mHeightField->getHeight(vertexIndex + mHeightField->getNbColumnsFast()) - h0), 0);
				const PxReal proj = e.dot(normal);
				if (proj > 0) 
				{
					normal -= e * proj / e.magnitudeSquared();
					result = true;
				}
			}

			if (column > 0)
			{
				// - column
				//PxVec3 e;
				//e.set(0, getHeightScale() * (getHeight(vertexIndex - 1) - h0), -getColumnScale());
				const PxVec3 e(0, mHfGeom->heightScale * (mHeightField->getHeight(vertexIndex - 1) - h0), -mHfGeom->columnScale);
				const PxReal proj = e.dot(normal);
				if (proj > 0) 
				{
					normal -= e * proj / e.magnitudeSquared();
					result = true;
				}
			}

			//if (column < getNbColumnsFast() - 1)
			if (column < mHeightField->getNbColumnsFast() - 1)
			{
				// + column
				//PxVec3 e;
				//e.set(0, getHeightScale() * (getHeight(vertexIndex + 1) - h0), getColumnScale());
				const PxVec3 e(0, mHfGeom->heightScale * (mHeightField->getHeight(vertexIndex + 1) - h0), mHfGeom->columnScale);
				const PxReal proj = e.dot(normal);
				if (proj > 0) 
				{
					normal -= e * proj / e.magnitudeSquared();
					result = true;
				}
			}

			return result;
		}

PxVec3 getEdgeDirection(PxU32 edgeIndex, PxU32 cell) const
{
//	const PxU32 cell = edgeIndex / 3;
	PX_ASSERT(cell == edgeIndex / 3);
//	switch (edgeIndex % 3)
	switch (edgeIndex - cell*3)
	{
	case 0:
		{
//			const PxReal y0 = mHeightField.getHeight(cell);
//			const PxReal y1 = mHeightField.getHeight(cell + 1);
//			return PxVec3(0.0f, mHfGeom.heightScale * (y1 - y0), mHfGeom.columnScale);
			const PxI32 y0 = mHeightField->getSample(cell).height;
			const PxI32 y1 = mHeightField->getSample(cell + 1).height;
			return PxVec3(0.0f, mHfGeom->heightScale * PxReal(y1 - y0), mHfGeom->columnScale);
		}
	case 1:
		if (mHeightField->isZerothVertexShared(cell))
		{
//			const PxReal y0 = mHeightField.getHeight(cell);
//			const PxReal y3 = mHeightField.getHeight(cell + mHeightField.getNbColumnsFast() + 1);
//			return PxVec3(mHfGeom.rowScale, mHfGeom.heightScale * (y3 - y0), mHfGeom.columnScale);
			const PxI32 y0 = mHeightField->getSample(cell).height;
			const PxI32 y3 = mHeightField->getSample(cell + mHeightField->getNbColumnsFast() + 1).height;
			return PxVec3(mHfGeom->rowScale, mHfGeom->heightScale * PxReal(y3 - y0), mHfGeom->columnScale);
		}
		else
		{
//			const PxReal y1 = mHeightField.getHeight(cell + 1);
//			const PxReal y2 = mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
//			return PxVec3(mHfGeom.rowScale, mHfGeom.heightScale * (y2 - y1), -mHfGeom.columnScale);
			const PxI32 y1 = mHeightField->getSample(cell + 1).height;
			const PxI32 y2 = mHeightField->getSample(cell + mHeightField->getNbColumnsFast()).height;
			return PxVec3(mHfGeom->rowScale, mHfGeom->heightScale * PxReal(y2 - y1), -mHfGeom->columnScale);
		}
	case 2:
		{
//			const PxReal y0 = mHeightField.getHeight(cell);
//			const PxReal y2 = mHeightField.getHeight(cell + mHeightField.getNbColumnsFast());
//			return PxVec3(mHfGeom.rowScale, mHfGeom.heightScale * (y2 - y0), 0.0f);
			const PxI32 y0 = mHeightField->getSample(cell).height;
			const PxI32 y2 = mHeightField->getSample(cell + mHeightField->getNbColumnsFast()).height;
			return PxVec3(mHfGeom->rowScale, mHfGeom->heightScale * PxReal(y2 - y0), 0.0f);
		}
	}
	return PxVec3(0);
}

/*PX_FORCE_INLINE PxVec3 getEdgeDirection(PxU32 edgeIndex) const
{
	const PxU32 cell = edgeIndex / 3;
	return getEdgeDirection(edgeIndex, cell);
}*/

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	};


} // namespace Gu

}

#endif
