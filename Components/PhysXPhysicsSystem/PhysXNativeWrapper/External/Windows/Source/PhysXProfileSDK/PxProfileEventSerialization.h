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


#ifndef PX_PHYSX_PROFILE_EVENT_SERIALIZATION_H
#define PX_PHYSX_PROFILE_EVENT_SERIALIZATION_H
#include "PxProfileBase.h"
#include "PxProfileDataParsing.h"
#include "PxProfileMemoryBuffer.h"
#include "PxProfileEvents.h"
#include "PsString.h"

namespace physx { namespace profile {

	/**
	 *	Array type must be a pxu8 container.  Templated so that this object can write
	 *	to different collections.
	 */
	
	template<typename TArrayType>
	struct EventSerializer
	{
		TArrayType* mArray;
		EventSerializer( TArrayType* inA ) : mArray( inA ) {}

		template<typename TDataType>
		void streamify( const char*, const TDataType& inType )
		{
			mArray->write( inType );
		}

		void streamify( const char*, const char*& inType )
		{
			PX_ASSERT( inType != NULL );
			PxU32 len( static_cast<PxU32>( strlen( inType ) ) );
			++len; //include the null terminator
			mArray->write( len );
			mArray->write( inType, len );
		}
		
		void streamify( const char*, const PxU8* inData, PxU32 len )
		{
			mArray->write( len );
			if ( len )
				mArray->write( inData, len );
		}

		void streamify( const char* nm, const PxU64& inType, EventStreamCompressionFlags::Enum inFlags )
		{
			switch( inFlags )
			{
			case EventStreamCompressionFlags::U8:
					streamify( nm, static_cast<PxU8>( inType ) );
					break;
			case EventStreamCompressionFlags::U16:
					streamify( nm, static_cast<PxU16>( inType ) );
					break;
			case EventStreamCompressionFlags::U32:
					streamify( nm, static_cast<PxU32>( inType ) );
					break;
			default:
				streamify( nm, inType );
				break;
			}
		}
		
		void streamify( const char* nm, const PxU32& inType, EventStreamCompressionFlags::Enum inFlags )
		{
			switch( inFlags )
			{
			case EventStreamCompressionFlags::U8:
					streamify( nm, static_cast<PxU8>( inType ) );
					break;
			case EventStreamCompressionFlags::U16:
					streamify( nm, static_cast<PxU16>( inType ) );
					break;
			default:
				streamify( nm, inType );
				break;
			}
		}
	};

	/**
	 *	The event deserializes takes a buffer implements the streamify functions
	 *	by setting the passed in data to the data in the buffer.
	 */	
	template<bool TSwapBytes>
	struct EventDeserializer
	{
		const PxU8* mData;
		PxU32		mLength;
		bool		mFail;

		EventDeserializer( const PxU8* inData,  PxU32 inLength )
			: mData( inData )
			, mLength( inLength )
			, mFail( false )
		{
			if ( mData == NULL )
				mLength = 0;
		}

		bool val() { return TSwapBytes; }

		void streamify( const char* , PxU8& inType )
		{
			PxU8* theData = reinterpret_cast<PxU8*>( &inType ); //type punned pointer...
			if ( mFail || sizeof( inType ) > mLength )
			{
				PX_ASSERT( false );
				mFail = true;
			}
			else
			{
				for( PxU32 idx = 0; idx < sizeof( PxU8 ); ++idx, ++mData, --mLength )
					theData[idx] = *mData;
			}
		}

		//default streamify reads things natively as bytes.
		template<typename TDataType>
		void streamify( const char* , TDataType& inType )
		{
			PxU8* theData = reinterpret_cast<PxU8*>( &inType ); //type punned pointer...
			if ( mFail || sizeof( inType ) > mLength )
			{
				PX_ASSERT( false );
				mFail = true;
			}
			else
			{
				for( PxU32 idx = 0; idx < sizeof( TDataType ); ++idx, ++mData, --mLength )
					theData[idx] = *mData;
				bool temp = val();
				if ( temp ) 
					BlockParseFunctions::swapBytes<sizeof(TDataType)>( theData );
			}
		}

		void streamify( const char*, const char*& inType )
		{
			PxU32 theLen;
			streamify( "", theLen );
			theLen = PxMin( theLen, mLength );
			inType = reinterpret_cast<const char*>( mData );
			mData += theLen;
			mLength -= theLen;
		}
		
		void streamify( const char*, const PxU8*& inData, PxU32& len )
		{
			PxU32 theLen;
			streamify( "", theLen );
			theLen = PxMin( theLen, mLength );
			len = theLen;
			inData = reinterpret_cast<const PxU8*>( mData );
			mData += theLen;
			mLength -= theLen;
		}

		void streamify( const char* nm, PxU64& inType, EventStreamCompressionFlags::Enum inFlags )
		{
			switch( inFlags )
			{
			case EventStreamCompressionFlags::U8:
				{
					PxU8 val=0;
					streamify( nm, val );
					inType = val;
				}
					break;
			case EventStreamCompressionFlags::U16:
				{
					PxU16 val;
					streamify( nm, val );
					inType = val;
				}
					break;
			case EventStreamCompressionFlags::U32:
				{
					PxU32 val;
					streamify( nm, val );
					inType = val;
				}
					break;
			default:
				streamify( nm, inType );
				break;
			}
		}
		
		void streamify( const char* nm, PxU32& inType, EventStreamCompressionFlags::Enum inFlags )
		{
			switch( inFlags )
			{
			case EventStreamCompressionFlags::U8:
				{
					PxU8 val=0;
					streamify( nm, val );
					inType = val;
				}
					break;
			case EventStreamCompressionFlags::U16:
				{
					PxU16 val=0;
					streamify( nm, val );
					inType = val;
				}
					break;
			default:
				streamify( nm, inType );
				break;
			}
		}
	};
}}
#endif