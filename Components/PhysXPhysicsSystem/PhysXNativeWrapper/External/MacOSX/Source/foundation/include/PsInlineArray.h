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


#ifndef PX_FOUNDATION_PSINLINEARRAY_H
#define PX_FOUNDATION_PSINLINEARRAY_H

#include "PsArray.h"
#include "PsInlineAllocator.h"

namespace physx
{
namespace shdfnd
{

// needs some work from binary serialization
//#define INHERIT_INLINEARRAY_FROM_ARRAY 

#ifdef INHERIT_INLINEARRAY_FROM_ARRAY
	// array that pre-allocates for N elements
	template <typename T, PxU32 N, typename Alloc = typename AllocatorTraits<T>::Type>
	class InlineArray : public Array<T, Alloc> 
	{
	public:

		InlineArray(const PxEmpty& v) : Array<T, Alloc>(v) {}

		InlineArray(const Alloc& alloc = Alloc()): 
			Array<T,Alloc>(reinterpret_cast<T*>(mInlineSpace), 0, N, alloc) {}

		PX_INLINE bool isInlined()	const
		{
			return mData == reinterpret_cast<T*>(mInlineSpace);
		}

		template<class Serializer>
		void exportExtraData(Serializer& stream)
		{
			if(!isInlined())
				Array<T, Alloc>::exportArray(stream, false);
		}

		char* importExtraData(char* address, PxU32& totalPadding)
		{
			PX_UNUSED(totalPadding);
			if(isInlined())
				this->mData = reinterpret_cast<T*>(mInlineSpace);
			else
				address = Array<T, Alloc>::importArray(address);
			return address;
		}
	protected:
		// T inlineSpace[N] requires T to have a default constructor
		PxU8 mInlineSpace[N*sizeof(T)];
	};

#else
	// array that pre-allocates for N elements
	template <typename T, PxU32 N, typename Alloc = typename AllocatorTraits<T>::Type>
	class InlineArray : public Array<T, InlineAllocator<N * sizeof(T), Alloc> >
	{
		typedef InlineAllocator<N * sizeof(T), Alloc> Allocator;
	public:

		InlineArray(const PxEmpty& v) : Array<T, Allocator>(v) {}

		PX_INLINE bool isInlined()	const
		{
			return Allocator::isBufferUsed();
		}

		template<class Serializer>

		void exportExtraData(Serializer& stream)
		{
			if(!isInlined())
				Array<T, Allocator>::exportArray(stream, false);
		}

		char* importExtraData(char* address, PxU32& totalPadding)
		{
			PX_UNUSED(totalPadding);
			if(isInlined())
				this->mData = reinterpret_cast<T*>(Array<T, Allocator>::getInlineBuffer());
			else
				address = Array<T, Allocator>::importArray(address);
			return address;
		}

		PX_INLINE explicit InlineArray(const Alloc& alloc = Alloc()) 
			: Array<T, Allocator>(alloc)
		{
			this->mData = this->allocate(N);
			this->mCapacity = N; 
		}
	};
#endif

} // namespace shdfnd
} // namespace physx

#endif
