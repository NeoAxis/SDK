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


#ifndef PX_PHYSX_PROFILE_MEMORY_BUFFER_H
#define PX_PHYSX_PROFILE_MEMORY_BUFFER_H
#include "PxProfileBase.h"
#include "PsAllocator.h"
#include "PsIntrinsics.h"

namespace physx { namespace profile {

	template<typename TAllocator = typename AllocatorTraits<PxU8>::Type >
	class MemoryBuffer : public TAllocator
	{
		PxU8* mBegin;
		PxU8* mEnd;
		PxU8* mCapacityEnd;

	public:
		MemoryBuffer( const TAllocator& inAlloc = TAllocator() ) : TAllocator( inAlloc ), mBegin( 0 ), mEnd( 0 ), mCapacityEnd( 0 ) {}
		~MemoryBuffer()
		{
			if ( mBegin ) TAllocator::deallocate( mBegin );
		}
		PxU32 size() const { return static_cast<PxU32>( mEnd - mBegin ); }
		PxU32 capacity() const { return static_cast<PxU32>( mCapacityEnd - mBegin ); }
		PxU8* begin() { return mBegin; }
		PxU8* end() { return mEnd; }
		const PxU8* begin() const { return mBegin; }
		const PxU8* end() const { return mEnd; }
		void clear() { mEnd = mBegin; }
		void write( PxU8 inValue )
		{
			growBuf( 1 );
			*mEnd = inValue;
			++mEnd;
		}

		template<typename TDataType>
		void write( const TDataType& inValue )
		{
			growBuf( sizeof( TDataType ) );
			const PxU8* __restrict readPtr = reinterpret_cast< const PxU8* >( &inValue );
			PxU8* __restrict writePtr = mEnd;
			for ( PxU32 idx = 0; idx < sizeof(TDataType); ++idx ) writePtr[idx] = readPtr[idx];
			mEnd += sizeof(TDataType);
		}
		
		template<typename TDataType>
		void write( const TDataType* inValue, PxU32 inLength )
		{
			if ( inValue && inLength )
			{
				PxU32 writeSize = inLength * sizeof( TDataType );
				growBuf( writeSize );
				memCopy( mBegin + size(), inValue, writeSize );
				mEnd += writeSize;
			}
		}
		
		void writeStrided( const PxU8* __restrict inData, PxU32 inItemSize, PxU32 inLength, PxU32 inStride )
		{
			if ( inStride == 0 || inStride == inItemSize )
				write( inData, inLength * inItemSize );
			else if ( inData && inLength )
			{
				PxU32 writeSize = inLength * inItemSize;
				growBuf( writeSize );
				PxU8* __restrict writePtr = mBegin + size();
				for ( PxU32 idx =0; idx < inLength; ++idx, writePtr += inItemSize, inData += inStride )
					memCopy( writePtr, inData, inItemSize );
				mEnd += writeSize;
			}
		}
		void growBuf( PxU32 inAmount )
		{
			PxU32 newSize = size() + inAmount;
			reserve( newSize );
		}
		void resize( PxU32 inAmount )
		{
			reserve( inAmount );
			mEnd = mBegin + inAmount;
		}
		void reserve( PxU32 newSize )
		{
			PxU32 currentSize = size();
			if ( newSize >= capacity() )
			{
				PxU8* newData = static_cast<PxU8*>( TAllocator::allocate( newSize * 2, __FILE__, __LINE__ ) );
				if ( mBegin )
				{
					memCopy( newData, mBegin, currentSize );
					TAllocator::deallocate( mBegin );
				}
				mBegin = newData;
				mEnd = mBegin + currentSize;
				mCapacityEnd = mBegin + newSize * 2;
			}
		}
	};
}}
#endif
