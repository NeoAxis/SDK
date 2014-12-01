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

#ifndef PX_COLLISION_RTREE
#define PX_COLLISION_RTREE

#include "PxSimpleTypes.h"
#include "PxVec4.h"
#include "PxBounds3.h"
#include "PsUserAllocated.h" // for PxSerialStream
#include "CmMetaData.h"

#define RTREE_MINIMUM_BOUNDS_EPSILON	1e-4F

namespace physx
{

class PxInputStream;
class PxOutputStream;

using namespace physx::shdfnd;

namespace Gu {
	
	class Box;
	struct RTreePage;

	#define RTREE_AABB_UNSIGNED 1

	#if RTREE_AABB_UNSIGNED
	typedef PxU16 PxR16;
	#else
	typedef PxI16 PxR16;
	#endif

	/////////////////////////////////////////////////////////////////////////
	// quantized untransposed RTree node - used for offline build and dynamic insertion
	struct RTreeNodeQ
	{
		PxR16 minx, miny, minz, maxx, maxy, maxz;
		PxU32 ptr;

		PX_FORCE_INLINE void setEmpty();
		PX_FORCE_INLINE void grow(const RTreePage& page, int nodeIndex);
		PX_FORCE_INLINE void grow(const RTreeNodeQ& node);
	};

	/////////////////////////////////////////////////////////////////////////
	// RTreePage data structure, holds RTreePage::SIZE transposed nodes
	#define RTREE_PAGE_SIZE 8 // changing this will affect the mesh format
	PX_COMPILE_TIME_ASSERT(RTREE_PAGE_SIZE <= 128); // using the low 5 bits for storage of index(childPtr) for dynamic rtree

	// RTreePage data structure, holds 8 transposed nodes
	PX_ALIGN_PREFIX(16)
	struct RTreePage {
		enum { SIZE = RTREE_PAGE_SIZE };
		#if RTREE_AABB_UNSIGNED
		enum {MN = 0, MX = 0xFFFF};
		#else
		enum {MN = -32768, MX = 32767};
		#endif
		PxR16 minx[SIZE]; // [min=MX, max=MN] is used as a sentinel range for empty bounds
		PxR16 miny[SIZE];
		PxR16 minz[SIZE];
		PxR16 maxx[SIZE];
		PxR16 maxy[SIZE];
		PxR16 maxz[SIZE];
		PxU32 ptrs[SIZE]; // for static rtree this is an offset relative to the first page divided by 16, for dynamics it's an absolute pointer divided by 16

		PX_FORCE_INLINE PxU32	nodeCount() const; // returns the number of occupied nodes in this page
		PX_FORCE_INLINE void	setEmpty(PxU32 startIndex = 0);
		PX_FORCE_INLINE void	copyNode(PxU32 targetIndex, const RTreePage& sourcePage, PxU32 sourceIndex);
		PX_FORCE_INLINE void	setNode(PxU32 targetIndex, const RTreeNodeQ& node);
		PX_FORCE_INLINE void	clearNode(PxU32 nodeIndex);
		PX_FORCE_INLINE void	getNode(PxU32 nodeIndex, RTreeNodeQ& result) const;
		PX_FORCE_INLINE void	computeBounds(RTreeNodeQ& bounds);
		PX_FORCE_INLINE void	adjustChildBounds(PxU32 index, const RTreeNodeQ& adjustedChildBounds);
		PX_FORCE_INLINE void	growChildBounds(PxU32 index, const RTreeNodeQ& adjustedChildBounds);
		PX_FORCE_INLINE PxU32	getNodeHandle(PxU32 index) const;
	} PX_ALIGN_SUFFIX(16);

	/////////////////////////////////////////////////////////////////////////
	// RTree root data structure
	PX_ALIGN_PREFIX(16)
	struct PX_PHYSX_COMMON_API RTree
	{
		// PX_SERIALIZATION
		RTree(PxRefResolver& v);
		void	exportExtraData(PxSerialStream&);
		char*	importExtraData(char* address, PxU32& totalPadding);
		static	void	getMetaData(PxSerialStream& stream);
		//~PX_SERIALIZATION

		RTree(); // offline static rtree constructor used with cooking

		~RTree() { release(); }

		void release();
		bool save(PxOutputStream& stream) const; // always saves as big endian
		bool load(PxInputStream& stream, PxU32 meshVersion); // converts to proper endian at load time

		////////////////////////////////////////////////////////////////////////////
		// QUERIES
		struct Callback
		{
			// result buffer should have room for at least RTreePage::SIZE items
			// should return true to continue traversal. If false is returned, traversal is aborted
			virtual bool processResults(PxU32 count, PxU32* buf) = 0;
			virtual ~Callback() {};
		};

		struct CallbackRaycast
		{
			// result buffer should have room for at least RTreePage::SIZE items
			// should return true to continue traversal. If false is returned, traversal is aborted
			// newMaxT serves as both input and output, as input it's the maxT so far
			// set it to a new value (which should be smaller) and it will become the new far clip t
			virtual bool processResults(PxU32 count, PxU32* buf, PxF32& newMaxT) = 0;
			virtual ~CallbackRaycast() {};
		};

		// callback will be issued as soon as the buffer overflows maxResultsPerBlock-RTreePage:SIZE entries
		// use maxResults = RTreePage:SIZE and return false from callback for "first hit" early out
		void		traverseAABB(
						const PxVec3& boxMin, const PxVec3& boxMax,
						const PxU32 maxResultsPerBlock, PxU32* resultsBlockBuf, Callback* processResultsBlockCallback) const;
		void		traverseOBB(
						const Gu::Box& obb,
						const PxU32 maxResultsPerBlock, PxU32* resultsBlockBuf, Callback* processResultsBlockCallback) const;
		template <int useRadius>
		void		traverseRay(
						const PxVec3& rayOrigin, const PxVec3& rayDir, // dir doesn't have to be normalized and is B-A for raySegment
						const PxU32 maxResults, PxU32* resultsPtr,
						Gu::RTree::CallbackRaycast* callback,
						const PxVec3& inflateAABBs, // inflate tree's AABBs by this amount. This function turns into AABB sweep.
						PxF32 maxT = PX_MAX_REAL // maximum ray t parameter, p(t)=origin+t*dir; use 1.0f for ray segment
						) const;

		////////////////////////////////////////////////////////////////////////////
		// DEBUG HELPER FUNCTIONS
		void		validate(); // verify that all children are indeed included in parent bounds
		void		openTextDump();
		void		closeTextDump();
		void		textDump(const char* prefix);
		void		maxscriptExport();
		PxU32		computeBottomLevelCount(PxU32 storedToMemMultiplier) const;

		////////////////////////////////////////////////////////////////////////////
		// DATA
		// remember to update save() and load() when adding or removing data
		PxVec4			mBoundsMin, mBoundsMax, mInvDiagonal, mDiagonalScaler;
		PxU32			mPageSize;
		PxU32			mNumRootPages;
		PxU32			mNumLevels;
		PxU32			mTotalNodes;
		PxU32			mTotalPages;
		PxU32			mFlags; enum { USER_ALLOCATED = 0x1, IS_DYNAMIC = 0x2 };
		PxU32			mUnused;
		RTreePage*		mPages;

		static PxU32	mVersion;

	protected:
		typedef PxU32 NodeHandle;
		void		validateRecursive(PxU32 level, RTreeNodeQ parentBounds, RTreePage* page);
		void		maxscriptExportRecursive(void* f, RTreePage* page, PxU32 level, PxU32 numLevels);
		void		dequantizeNode(const RTreePage* page, PxU32 index, PxVec3& mn, PxVec3& mx) const;
		bool		findObjectBackTrack(PxU32 objectHandle, const PxVec3& mn, const PxVec3& mx, NodeHandle* backtrackBuf, PxU32 bufsize) const;

				// has to be aligned to page size
		PX_FORCE_INLINE
		RTreePage*	getPageFromNodeHandle(NodeHandle nh) const { return pagePtrFrom32Bits(nh&~(sizeof(RTreePage)-1)); }

		PX_FORCE_INLINE
		PxU32		getNodeIdxFromNodeHandle(NodeHandle nh) const { return nh & (RTreePage::SIZE-1); }

		static PX_FORCE_INLINE
		PxU32		pagePtrTo32Bits(const RTreePage* page);

		static PX_FORCE_INLINE
		RTreePage*	pagePtrFrom32Bits(PxU32 page);
		PX_FORCE_INLINE

		RTreePage*	get64BitBasePage() const;
		#ifdef PX_X64
		static RTreePage* sFirstPoolPage;
		#endif // PX_X64

		friend struct RTreePage;
	} PX_ALIGN_SUFFIX(16);

	/////////////////////////////////////////////////////////////////////////
	PX_FORCE_INLINE PxU32 RTree::pagePtrTo32Bits(const RTreePage* page)
	{
		#ifdef PX_X64
			PX_ASSERT(PxU64(page) >= PxU64(sFirstPoolPage));
			PxU64 delta = PxU64(page)-PxU64(sFirstPoolPage);
			PX_ASSERT(delta <= 0xFFFFffff);
			return PxU32(delta);
		#else
			return PxU32(page);
		#endif //PX_X64
	}

	/////////////////////////////////////////////////////////////////////////
	PX_FORCE_INLINE RTreePage* RTree::pagePtrFrom32Bits(PxU32 page)
	{
		#ifdef PX_X64
			return reinterpret_cast<RTreePage*>(PxU64(sFirstPoolPage)+page);
		#else
			return reinterpret_cast<RTreePage*>(page);
		#endif //PX_X64
	}

	/////////////////////////////////////////////////////////////////////////
	PX_FORCE_INLINE RTreePage* RTree::get64BitBasePage() const
	{
		#ifdef PX_X64
			if (mFlags & IS_DYNAMIC)
				return sFirstPoolPage;
			else
				return mPages;
		#else
			return mPages;
		#endif
	}

	// explicit instantiations for traverseRay
	// XXX: dima: g++ 4.4 won't compile this => skipping by PX_LINUX
#if (defined(PX_X86) || defined(PX_X360)) && !(defined(PX_LINUX) || defined(PX_APPLE))
	template void RTree::traverseRay<0>(
		const PxVec3&, const PxVec3&, const PxU32, PxU32*, Gu::RTree::CallbackRaycast*, const PxVec3&, PxF32 maxT) const;
	template void RTree::traverseRay<1>(
		const PxVec3&, const PxVec3&, const PxU32, PxU32*, Gu::RTree::CallbackRaycast*, const PxVec3&, PxF32 maxT) const;
#endif

	/////////////////////////////////////////////////////////////////////////
	// Dynamic RTree class

	typedef physx::shdfnd::HashMap<PxU32, PxU32> RTreeObjectMap;
	typedef physx::shdfnd::HashMap<PxU32, PxU32> RTreePageMap;

	struct PX_PHYSX_COMMON_API DynamicRTree : public RTree
	{
		struct NodeMarker { PxR16 x, y, z; }; // 6 bytes

		DynamicRTree(const PxVec3& worldMinBound, const PxVec3& worldMaxBound, bool useBacktrackHash); // dynamic rtree constructor
		~DynamicRTree() { release(); }

		// returns a handle that can be used later for update, remove and to retrive conservatively de-quantized bounds
		NodeMarker	addObject(PxU32 objectHandle, const PxVec3& minExtent, const PxVec3& maxExtent);

		// specified bounds have to overlap with objectHandle's bounds
		// returns false if objectHandle wasn't found at specified bounds
		bool		removeObject(PxU32 objectHandle, const PxVec3& minBounds, const PxVec3& maxBounds);
		// locate using compressed NodeMarker returned from addObject
		bool		removeObject(PxU32 objectHandle, const NodeMarker& nodeMarker);

		// note that updateObject functions do not restructure the tree but simply adjust the bounds up the tree
		// without altering it's structure
		// the tree still remains valid but the perf will definitely deteriorate over time
		// one strategy to combat perf deterioration is to reinsert a fixed number of moved objects per frame in an LRU fashion
		// returns false if objectHandle wasn't found at oldBounds
		bool		updateObject(PxU32 objectHandle, const PxVec3& oldMin, const PxVec3& oldMax, const PxVec3& minBounds, const PxVec3& maxBounds);
		// update using NodeMarker returned from addObject
		bool		updateObject(PxU32 objectHandle, const NodeMarker& nodeMarker, const PxVec3& minBounds, const PxVec3& maxBounds);

		// returns conservatively dequantized bounds and stored object handle for a given node handle
		void		getDequantizedBounds(PxU32 objectHandle, const NodeMarker& marker, PxVec3& mn, PxVec3& mx);

		void		removeAllObjects();
		PxU32		countObjects() const; // current implementation is recursive and has linear perf in the number of objects

		// debug helper functions
		void		validate(); // verify that all children are indeed included in parent bounds plus check hash consistency

	private:
		bool			mUseBacktrackHash;
		RTreePageMap	mPageMap; // maps from "stored form" pointer to parent node handle (see comments for RTREE_STOREDMEM_MUL)
		RTreeObjectMap	mObjectMap;

		void		release();

		void		addQuantizedNodeAtLevel(const RTreeNodeQ& qNode, PxU32 insertionLevel); // mNumLevels-1 for insertion into terminal pages
		void		removeAllObjectsRecursive(PxU32 level, RTreePage* page);
		PxU32		countObjectsRecursive(PxU32 level, RTreePage* page) const;
		PxVec3		dequantizeNodeMarker(const NodeMarker& marker); // TODO: there is some unnecessary quantization/dequantization going on right now, need to optimize

		// returns true if objectHandle was found. buf will be filled with pointers to backtrack nodes, with level0 in buf[0] (unused)
		// down to buf[mNumLevels-1] for the bottom level, which will be the address of the page/16+node index inside of the page
		// page pointer and node index can be reconstructed from backtrackBuf which contains addresses to child page nodes.
		// the page is always 128 byte aligned and node pointer is 16 byte aligned, so the page pointer from node pointer is x&~127
		// Since the returned pointer is also divided by 16, the node index is backtrackBuf[j]&7 and page pointer is
		// (backtrackBuf[j]*16)&~127
		// If objectHandle is found (match by id and overlap) then there will be mNumLevel-1 results in the backtrackBuf array
		// and the function will return true, otherwise the function will return false.
		bool		findObject(PxU32 objectHandle, NodeHandle* backtrackBuf, PxU32 bufsize) const;

		PX_FORCE_INLINE void relocateObjectHash(PxU32 objOrNodeHandle, PxU32 parentHandle, bool isBottomLevel, bool newItem);
	};

	/////////////////////////////////////////////////////////////////////////
	// quantizer helper class
	struct RTreeNodeQuantizer
	{
		static PxR16 cvtPxF32ToPxR16(PxF32 f)
		{
			PX_ASSERT(f >= PxF32(RTreePage::MN) && f <= PxF32(RTreePage::MX));
			return PxR16(f);
		}

		static PxVec3 computeInvDiagUpdateBounds(PxBounds3& treeBounds)
		{
			PxVec3 boundsEpsilon(RTREE_MINIMUM_BOUNDS_EPSILON);
			treeBounds.maximum += boundsEpsilon; // adjust bounds after PX_EPS_F32 expansion for dims
			treeBounds.minimum -= boundsEpsilon;
			// in addition, inflate the bounds so that we have at least 1 quantization step of empty space on each side
			// this is done so we can clamp a quantized query to [MN+1,MX-1] range without excluding any objects
			// and have the [MX,MN] inverted sentinel range used for empty nodes
			// to be able to always return no intersection without additional runtime checks
			PxVec3 boundsExp = treeBounds.getDimensions() * 1.5f / 65536.0f;
			treeBounds.minimum -= boundsExp;
			treeBounds.maximum += boundsExp;
			PxVec3 treeDiag = treeBounds.getDimensions() + boundsEpsilon;
			treeDiag.x = 1.0f / treeDiag.x;
			treeDiag.y = 1.0f / treeDiag.y;
			treeDiag.z = 1.0f / treeDiag.z;
			return treeDiag;
		}

		static RTreeNodeQ quantize(
			const PxVec4& nqMin, const PxVec4& nqMax, const PxVec4& treeMin, const PxVec4& invDiagonal
		)
		{
			// since we quantize conservatively, we perform queryMax<=treeMin for rejection (not queryMax<treeMin)
			// (consider floor(treeMin=1.02) quantizing to 1 on min-size, and ceil(queryMax=1.01) quantizing to 2 on max-side)
			// offline we scale min & max to full 16 bit range-2 plus unused 1 on each side to leave room for out-of-bound compares
			// then at runtime we clamp "wide" so that bounds that are out of range of quantization still produce correct results
			// note: there is a simd version of this routine in RTreeQuery.cpp

			// Quantization logic derivation for Q=query, T=tree
			// NO OVERLAP IFF (exists maxQ <= minT || exists minQ>=maxT)
			// IFF (exists maxQ < minT+1 || exists minQ > maxT-1)
			PxF32 hiClamp = PxF32(RTreePage::MX);
			PxF32 loClamp = PxF32(RTreePage::MN);
			RTreeNodeQ q;
			PxF32 range = PxF32(0xFFFF); // using narrow clamped quantization range for both narrow and wide clamp
			PxVec4 scaledMin = (nqMin - treeMin).multiply(invDiagonal) * range;
			PxVec4 scaledMax = (nqMax - treeMin).multiply(invDiagonal) * range;
			q.minx = cvtPxF32ToPxR16(PxClamp(PxFloor(scaledMin.x), loClamp, hiClamp));
			q.maxx = cvtPxF32ToPxR16(PxClamp(PxCeil(scaledMax.x), loClamp, hiClamp));
			q.miny = cvtPxF32ToPxR16(PxClamp(PxFloor(scaledMin.y), loClamp, hiClamp));
			q.maxy = cvtPxF32ToPxR16(PxClamp(PxCeil(scaledMax.y), loClamp, hiClamp));
			q.minz = cvtPxF32ToPxR16(PxClamp(PxFloor(scaledMin.z), loClamp, hiClamp));
			q.maxz = cvtPxF32ToPxR16(PxClamp(PxCeil(scaledMax.z), loClamp, hiClamp));

			// see derivation above - we shrink the tree node by 2 and quantized query by 1
			q.minx += (q.minx != RTreePage::MX) ? 1 : 0;
			q.miny += (q.miny != RTreePage::MX) ? 1 : 0;
			q.minz += (q.minz != RTreePage::MX) ? 1 : 0;
			q.maxx -= (q.maxx != RTreePage::MN) ? 1 : 0;
			q.maxy -= (q.maxy != RTreePage::MN) ? 1 : 0;
			q.maxz -= (q.maxz != RTreePage::MN) ? 1 : 0;

			q.ptr = 0;

			return q;
		}
	}; // struct RTreeNodeQuantizer

	/////////////////////////////////////////////////////////////////////////
	PX_FORCE_INLINE void RTreeNodeQ::setEmpty()
	{
		minx = miny = minz = RTreePage::MX;
		maxx = maxy = maxz = RTreePage::MN;
	}
} // namespace Gu

}

#endif // #ifdef PX_COLLISION_RTREE
