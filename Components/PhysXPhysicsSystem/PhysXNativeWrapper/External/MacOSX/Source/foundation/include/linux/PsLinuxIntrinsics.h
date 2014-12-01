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


#ifndef PX_FOUNDATION_PS_LINUX_INTRINSICS_H
#define PX_FOUNDATION_PS_LINUX_INTRINSICS_H

#include "Ps.h"
#include "foundation/PxAssert.h"

#include <math.h>

#if 0
#include <libkern/OSAtomic.h>
#endif

// this file is for internal intrinsics - that is, intrinsics that are used in
// cross platform code but do not appear in the API

#if !(defined PX_LINUX || defined PX_APPLE || defined PX_ANDROID)
	#error "This file should only be included by linux builds!!"
#endif

namespace physx
{
namespace shdfnd
{

	PX_FORCE_INLINE void memoryBarrier()
	{
#if 0 //!defined (PX_ARM)
		smp_mb();
#endif
	}

	/*!
	Return the index of the highest set bit. Undefined for zero arg.
	*/
	PX_INLINE PxU32 highestSetBitUnsafe(PxU32 v)
	{

		// http://graphics.stanford.edu/~seander/bithacks.html
		static const PxU32 MultiplyDeBruijnBitPosition[32] = 
		{
		  0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
		  8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
		};

		v |= v >> 1; // first round up to one less than a power of 2 
		v |= v >> 2;
		v |= v >> 4;
		v |= v >> 8;
		v |= v >> 16;

		return MultiplyDeBruijnBitPosition[(PxU32)(v * 0x07C4ACDDU) >> 27];
	}

	/*!
	Return the index of the highest set bit. Undefined for zero arg.
	*/
	PX_INLINE PxI32 lowestSetBitUnsafe(PxU32 v)
	{
		// http://graphics.stanford.edu/~seander/bithacks.html
		static const PxU32 MultiplyDeBruijnBitPosition[32] = 
		{
			0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 
			31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
		};
		PxI32 w = v;
		return MultiplyDeBruijnBitPosition[(PxU32)((w & -w) * 0x077CB531U) >> 27];
	}

	/*!
	Returns the index of the highest set bit. Undefined for zero arg.
	*/
	PX_INLINE PxU32 countLeadingZeros(PxU32 v)
	{
		PxI32 result = 0;
		PxU32 testBit = (1<<31);
		while ((v & testBit) == 0 && testBit != 0)
			result ++, testBit >>= 1;
		return result;
	}

	/*!
	Sets \c count bytes starting at \c dst to zero.
	*/
	PX_FORCE_INLINE void* memZero(void* PX_RESTRICT dest, PxU32 count)
	{
		return memset(dest, 0, count);
	}

	/*!
	Sets \c count bytes starting at \c dst to \c c.
	*/
	PX_FORCE_INLINE void* memSet(void* PX_RESTRICT dest, PxI32 c, PxU32 count)
	{
		return memset(dest, c, count);
	}

	/*!
	Copies \c count bytes from \c src to \c dst. User memMove if regions overlap.
	*/
	PX_FORCE_INLINE void* memCopy(void* PX_RESTRICT dest, const void* PX_RESTRICT src, PxU32 count)
	{
		return memcpy(dest, src, count);
	}

	/*!
	Copies \c count bytes from \c src to \c dst. Supports overlapping regions.
	*/
	PX_FORCE_INLINE void* memMove(void* PX_RESTRICT dest, const void* PX_RESTRICT src, PxU32 count)
	{
		return memmove(dest, src, count);
	}

	/*!
	Set 128B to zero starting at \c dst+offset. Must be aligned.
	*/
	PX_FORCE_INLINE void memZero128(void* PX_RESTRICT dest, PxU32 offset = 0)
	{
		PX_ASSERT(((size_t(dest)+offset) & 0x7f) == 0);
		memSet((char* PX_RESTRICT)dest+offset, 0, 128);
	}

	/*!
	Prefetch aligned 128B around \c ptr+offset.
	*/
	PX_FORCE_INLINE void prefetch128(const void*, PxU32 = 0)
	{
	}

	/*!
	Prefetch \c count bytes starting at \c ptr.
	*/
	PX_FORCE_INLINE void prefetch(const void* ptr, PxU32 count = 0)
	{
		for(PxU32 i=0; i<=count; i+=128)
			prefetch128(ptr, i);
	}

	//! \brief platform-specific reciprocal
	PX_FORCE_INLINE float recipFast(float a)
	{
		return 1.0f/a;
	}

	//! \brief platform-specific fast reciprocal square root
	PX_FORCE_INLINE float recipSqrtFast(float a)
	{
		return 1.0f/::sqrtf(a);
	}

	//! \brief platform-specific floor
	PX_FORCE_INLINE float floatFloor(float x)
	{
		return ::floorf(x);
	}

	#define PX_PRINTF printf
	#define PX_EXPECT_TRUE(x) x
	#define PX_EXPECT_FALSE(x) x

} // namespace shdfnd
} // namespace physx

#endif


