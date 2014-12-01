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


#ifndef PX_FOUNDATION_PS_WINDOWS_INTRINSICS_H
#define PX_FOUNDATION_PS_WINDOWS_INTRINSICS_H

#include "Ps.h"
#include "foundation/PxAssert.h"

// this file is for internal intrinsics - that is, intrinsics that are used in
// cross platform code but do not appear in the API

#if !(defined PX_WINDOWS || defined PX_WIN8ARM)
	#error "This file should only be included by Windows or WIN8ARM builds!!"
#endif

#include <math.h>
#include <intrin.h>
#include <float.h>
#include <mmintrin.h>

#pragma intrinsic(_BitScanForward)
#pragma intrinsic(_BitScanReverse)

namespace physx
{
namespace shdfnd
{

		/*
	 * Implements a memory barrier
	 */
	PX_FORCE_INLINE void memoryBarrier()
	{
		_ReadWriteBarrier();
		/* long Barrier;
		__asm {
			xchg Barrier, eax
		}*/
	}

	/*!
	Returns the index of the highest set bit. Not valid for zero arg.
	*/
	PX_FORCE_INLINE PxU32 highestSetBitUnsafe(PxU32 v)
	{
		unsigned long retval;
		_BitScanReverse(&retval, v);
		return retval;
	}

	/*!
	Returns the index of the highest set bit. Undefined for zero arg.
	*/
	PX_FORCE_INLINE PxU32 lowestSetBitUnsafe(PxU32 v)
	{
		unsigned long retval;
		_BitScanForward(&retval, v);
		return retval;
	}


	/*!
	Returns the number of leading zeros in v. Returns 32 for v=0.
	*/
	PX_FORCE_INLINE PxU32 countLeadingZeros(PxU32 v)
	{
		if(v)
		{
			unsigned long bsr = (unsigned long)-1;
			_BitScanReverse(&bsr, v);
			return 31 - bsr;
		}
		else
			return 32;
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
#ifndef PX_WIN8ARM
	PX_FORCE_INLINE void prefetch128(const void* ptr, PxU32 offset = 0)
	{
		_mm_prefetch(((const char*)ptr + offset), _MM_HINT_T0);
	}
#else
	PX_FORCE_INLINE void prefetch128(const void* , PxU32 = 0)
	{
	}
#endif

	/*!
	Prefetch \c count bytes starting at \c ptr.
	*/
	PX_FORCE_INLINE void prefetch(const void* ptr, PxU32 count = 0)
	{
		for(PxU32 i=0; i<=count; i+=128)
			prefetch128(ptr, i);
	}

	//! \brief platform-specific reciprocal
	PX_CUDA_CALLABLE PX_FORCE_INLINE float recipFast(float a)				{	return 1.0f/a;			}

	//! \brief platform-specific fast reciprocal square root
	PX_CUDA_CALLABLE PX_FORCE_INLINE float recipSqrtFast(float a)			{   return 1.0f/::sqrtf(a); }

	//! \brief platform-specific floor
	PX_CUDA_CALLABLE PX_FORCE_INLINE float floatFloor(float x)
	{
		return ::floorf(x);
	}

	#define PX_PRINTF printf
	#define PX_EXPECT_TRUE(x) x
	#define PX_EXPECT_FALSE(x) x

} // namespace shdfnd
} // namespace physx

#endif
