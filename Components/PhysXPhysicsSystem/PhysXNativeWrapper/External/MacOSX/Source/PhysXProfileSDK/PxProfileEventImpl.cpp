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

#include "PxProfileEvents.h"
#include "PxProfileEventSerialization.h"
#include "PxProfileEventBuffer.h"
#include "PxProfileEventParser.h"
#include "PxProfileEventHandler.h"
#include "PxProfileScopedMutexLock.h"
#include "PxProfileEventFilter.h"
#include "PxProfileContextProvider.h"
#include "PxProfileEventMutex.h"
#include "PsFoundation.h"
#include "PsUserAllocated.h"
#include "PxProfileZoneManagerImpl.h"
#include "PxProfileZoneImpl.h"
#include "PxErrorCallback.h"
#include "PxAllocatorCallback.h"
#include "PxProfileMemoryEventTypes.h"
#include "PxProfileMemoryEventRecorder.h"
#include "PxProfileMemoryEventBuffer.h"
#include "PxProfileMemoryEventParser.h"
#include "PxProfileContextProviderImpl.h"
#include <stdio.h>

namespace physx { 
	using namespace profile;

	void PxProfileEventHandler::parseEventBuffer( const PxU8* inBuffer, PxU32 inBufferSize, PxProfileEventHandler& inHandler, bool inSwapBytes )
	{
		if ( inSwapBytes == false )
			parseEventData<false>( inBuffer, inBufferSize, &inHandler );
		else
			parseEventData<true>( inBuffer, inBufferSize, &inHandler );
	}

	template<PxU32 TNumEvents>
	struct ProfileBulkEventHandlerBuffer
	{
		Event mEvents[TNumEvents];
		PxU32 mEventCount;
		PxProfileBulkEventHandler* mHandler;
		ProfileBulkEventHandlerBuffer( PxProfileBulkEventHandler* inHdl )
			: mEventCount( 0 )
			, mHandler( inHdl )
		{
		}
		void onEvent( const Event& inEvent )
		{
			mEvents[mEventCount] = inEvent;
			++mEventCount;
			if ( mEventCount == TNumEvents )
				flush();
		}
		void onEvent( const PxProfileEventId& inId, PxU32 threadId, PxU64 contextId, PxU8 cpuId, PxU8 threadPriority, PxU64 timestamp, EventTypes::Enum inType )
		{
			StartEvent theEvent;
			theEvent.init( threadId, contextId, cpuId, static_cast<PxU8>( threadPriority ), timestamp );
			onEvent( Event( EventHeader( static_cast<PxU8>( inType ), inId.mEventId ), theEvent ) );
		}
		void onStartEvent( const PxProfileEventId& inId, PxU32 threadId, PxU64 contextId, PxU8 cpuId, PxU8 threadPriority, PxU64 timestamp )
		{
			onEvent( inId, threadId, contextId, cpuId, threadPriority, timestamp, EventTypes::StartEvent );
		}
		void onStopEvent( const PxProfileEventId& inId, PxU32 threadId, PxU64 contextId, PxU8 cpuId, PxU8 threadPriority, PxU64 timestamp )
		{
			onEvent( inId, threadId, contextId, cpuId, threadPriority, timestamp, EventTypes::StopEvent );
		}
		void onEventValue( const PxProfileEventId& inId, PxU32 threadId, PxU64 contextId, PxI64 value )
		{
			EventValue theEvent;
			theEvent.init( value, contextId, threadId );
			onEvent( Event( inId.mEventId, theEvent ) );
		}
		void onCUDAProfileBuffer( PxU64 submitTimestamp, PxF32 timeSpan, const PxU8* cudaData, PxU32 bufLenInBytes, PxU32 bufferVersion )
		{
			CUDAProfileBuffer theEvent;
			theEvent.init( submitTimestamp, timeSpan, cudaData, bufLenInBytes, bufferVersion );
			onEvent( Event( 0, theEvent ) );
		}
		void flush()
		{
			if ( mEventCount )
				mHandler->handleEvents( mEvents, mEventCount );
			mEventCount = 0;
		}
	};


	void PxProfileBulkEventHandler::parseEventBuffer( const PxU8* inBuffer, PxU32 inBufferSize, PxProfileBulkEventHandler& inHandler, bool inSwapBytes )
	{
		ProfileBulkEventHandlerBuffer<256> hdler( &inHandler );
		if ( inSwapBytes )
			parseEventData<true>( inBuffer, inBufferSize, &hdler );
		else
			parseEventData<false>( inBuffer, inBufferSize, &hdler );
		hdler.flush();
	}

	struct PxProfileNameProviderImpl
	{
		PxProfileNameProvider* mImpl;
		PxProfileNameProviderImpl( PxProfileNameProvider* inImpl )
			: mImpl( inImpl )
		{
		}
		PxProfileNames getProfileNames() const { return mImpl->getProfileNames(); }
	};

	
	struct PxProfileNameProviderForward
	{
		PxProfileNames mNames;
		PxProfileNameProviderForward( PxProfileNames inNames )
			: mNames( inNames )
		{
		}
		PxProfileNames getProfileNames() const { return mNames; }
	};

	
	PxProfileZone& PxProfileZone::createProfileZone( PxAllocatorCallback* inAllocator, const char* inSDKName, PxProfileNames inNames, PxU32 inEventBufferByteSize )
	{
		typedef ZoneImpl<PxProfileNameProviderForward> TSDKType;
		return *PX_PROFILE_NEW( inAllocator, TSDKType ) ( inAllocator, inSDKName, inEventBufferByteSize, PxProfileNameProviderForward( inNames ) );
	}
	PxProfileZone& PxProfileZone::createProfileZone( PxFoundation* inFoundation, const char* inSDKName, PxProfileNames inNames, PxU32 inEventBufferByteSize )
	{
		PxAllocatorCallback* theCallback = NULL;
		if ( inFoundation )
			theCallback = &inFoundation->getAllocator();
		return createProfileZone( theCallback, inSDKName, inNames, inEventBufferByteSize );
	}

	PxProfileZone& PxProfileZone::createProfileZone( PxFoundation* inFoundation, const char* inSDKName, PxProfileNameProvider& inProvider, PxU32 inEventBufferByteSize )
	{
		return createProfileZone( inFoundation, inSDKName, inProvider.getProfileNames(), inEventBufferByteSize );
	}
	
	PxProfileZone& PxProfileZone::createProfileZone( PxAllocatorCallback* inAllocator, const char* inSDKName, PxProfileNameProvider& inProvider, PxU32 inEventBufferByteSize )
	{
		return createProfileZone( inAllocator, inSDKName, inProvider.getProfileNames(), inEventBufferByteSize );
	}

	PxProfileZoneManager& PxProfileZoneManager::createProfileZoneManager(PxFoundation* inFoundation )
	{
		return *PX_PROFILE_NEW( inFoundation, ZoneManagerImpl ) ( inFoundation );
	}

	PxProfileMemoryEventRecorder& PxProfileMemoryEventRecorder::createRecorder( PxFoundation* inFoundation )
	{
		return *PX_PROFILE_NEW( inFoundation, PxProfileMemoryEventRecorderImpl )( inFoundation );
	}
	
	PxProfileMemoryEventBuffer& PxProfileMemoryEventBuffer::createMemoryEventBuffer( PxFoundation* inFoundation, PxU32 inBufferSize )
	{
		return *PX_PROFILE_NEW( inFoundation, PxProfileMemoryEventBufferImpl )( inFoundation, inBufferSize );
	}
	
	PxProfileMemoryEventBuffer& PxProfileMemoryEventBuffer::createMemoryEventBuffer( PxAllocatorCallback& inAllocator, PxU32 inBufferSize )
	{
		return *PX_PROFILE_NEW( &inAllocator, PxProfileMemoryEventBufferImpl )( inAllocator, inBufferSize );
	}
	template<PxU32 TNumEvents>
	struct ProfileBulkMemoryEventHandlerBuffer
	{
		PxProfileBulkMemoryEvent mEvents[TNumEvents];
		PxU32 mEventCount;
		PxProfileBulkMemoryEventHandler* mHandler;
		ProfileBulkMemoryEventHandlerBuffer( PxProfileBulkMemoryEventHandler* inHdl )
			: mEventCount( 0 )
			, mHandler( inHdl )
		{
		}
		void onEvent( const PxProfileBulkMemoryEvent& evt )
		{
			mEvents[mEventCount] = evt;
			++mEventCount;
			if ( mEventCount == TNumEvents )
				flush();
		}

		template<typename TDataType>
		void operator()( const MemoryEventHeader&, const TDataType& ) {}

		void operator()( const MemoryEventHeader&, const AllocationEvent& evt )
		{
			onEvent( PxProfileBulkMemoryEvent( evt.mSize, evt.mType, evt.mFile, evt.mLine, evt.mAddress ) );
		}

		void operator()( const MemoryEventHeader&, const DeallocationEvent& evt )
		{
			onEvent( PxProfileBulkMemoryEvent( evt.mAddress ) );
		}

		void flush()
		{
			if ( mEventCount )
				mHandler->handleEvents( mEvents, mEventCount );
			mEventCount = 0;
		}
	};

	void PxProfileBulkMemoryEventHandler::parseEventBuffer( const PxU8* inBuffer, PxU32 inBufferSize, PxProfileBulkMemoryEventHandler& inHandler, bool inSwapBytes )
	{
		ProfileBulkMemoryEventHandlerBuffer<0x1000> theBuffer(&inHandler);
		if ( inSwapBytes )
		{
			MemoryEventParser<true> theParser( NULL );
			theParser.parseEventData( inBuffer, inBufferSize, &theBuffer );
		}
		else
		{
			MemoryEventParser<false> theParser( NULL );
			theParser.parseEventData( inBuffer, inBufferSize, &theBuffer );
		}
		theBuffer.flush();
	}

}
