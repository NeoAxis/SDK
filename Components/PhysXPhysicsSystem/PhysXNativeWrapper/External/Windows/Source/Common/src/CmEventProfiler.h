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


#ifndef PX_PHYSICS_COMMON_PROFILE_EVENT_H
#define PX_PHYSICS_COMMON_PROFILE_EVENT_H

#define PX_PROFILE_EVENT_PROFILE_THRESHOLD EventPriorities::Detail //default knob
#include "PxPhysXCommon.h" // added for definition of PX_PHYSX_COMMON_API
#include "PxProfileCompileTimeEventFilter.h"
#include "PxProfileScopedEvent.h"
#include "PxProfileEventNames.h"
#include "PxProfileZone.h"
#include "PxProfileEventId.h"


#define PX_PROFILE_EVENT_DEFINITION_HEADER "CmProfileEventDefs.h"


	//Define all the event enumeration values as well as functions for creating the events.
#include "CmProfileDeclareEventInfo.h"

namespace physx
{
namespace Cm
{
	struct CmEventNameProvider : public physx::PxProfileNameProvider
	{
		PX_PHYSX_COMMON_API physx::PxProfileNames getProfileNames() const;
	};

#define PX_PROFILE_BEGIN_SUBSYSTEM( subsys ) struct subsys {
#define PX_PROFILE_EVENT( subsys, name, priority ) static const physx::PxProfileEventId name; \
	PX_PHYSX_COMMON_API static const physx::PxProfileEventId& Get##name() { return name; }
#define PX_PROFILE_END_SUBSYSTEM( subsys ) };
	struct ProfileEventId
	{
#include "CmProfileEventDefs.h"	
	};
#undef PX_PROFILE_BEGIN_SUBSYSTEM
#undef PX_PROFILE_EVENT
#undef PX_PROFILE_END_SUBSYSTEM

	class EventProfiler
	{
		physx::PxU64					mEventContext;
		physx::PxProfileEventSender*			mSDK;
	public:
		EventProfiler( physx::PxProfileEventSender* inSDK = NULL, physx::PxU64 inEventContext = 0 )
			: mEventContext( inEventContext )
			, mSDK( inSDK )
		{
		}
		EventProfiler( const EventProfiler& other )  { *this = other; }
		EventProfiler& operator=( const EventProfiler& other )
		{
			mEventContext = other.mEventContext;
			mSDK = other.mSDK;
			return *this;
		}
		physx::PxProfileEventSender* getProfileEventSender() { return mSDK; }
		physx::PxU64 getEventContext() const { return mEventContext; }
	};
}

template<bool TEnabled>
class CmProfileZone
{
	physx::PxProfileEventSender* mEventSender;
	physx::PxU16 mEventId;
	physx::PxU64 mEventContext;
public:
	template<typename TProfileDataProvider>
	CmProfileZone( TProfileDataProvider& inProvider, physx::PxU16 inEventId )
		: mEventSender( inProvider.getEventProfiler().getProfileEventSender() )
		, mEventId( inEventId )
		, mEventContext( inProvider.getEventProfiler().getEventContext() )
	{
		PX_ASSERT( mEventSender );
		mEventSender->startEvent( inEventId, mEventContext );
	}

	~CmProfileZone()
	{
		PX_ASSERT( mEventSender );
		mEventSender->stopEvent( mEventId, mEventContext );
	}
};

template<>
class CmProfileZone<false> 
{
public:
	template<typename TProfileDataProvider> CmProfileZone( TProfileDataProvider&, physx::PxU16) {}
};


template<bool TEnabled>
struct CmProfileValue
{
	template<typename TProfileDataProvider>
	CmProfileValue( TProfileDataProvider& inProvider, physx::PxU16 inEventId, physx::PxI64 theValue )
	{
		physx::PxProfileEventSender* theEventSender = inProvider.getEventProfiler().getProfileEventSender();
		physx::PxU64 theContext = inProvider.getEventProfiler().getEventContext();
		PX_ASSERT( theEventSender );
		theEventSender->eventValue( inEventId, theContext, theValue );
	}
};

template<> struct CmProfileValue<false>
{
	template<typename TProfileDataProvider> 
	CmProfileValue( TProfileDataProvider&, physx::PxU16, physx::PxI64 )
	{
	}
};

//---------------------------------------------------------------------------

inline physx::PxU64 getProfileEventContext() { return 0; }

#define CM_PROFILE_START( _p, _id) physx::profile::startEvent( _id.mCompileTimeEnabled, _p.getProfileEventSender(), _id, _p.getEventContext() );
#define CM_PROFILE_STOP( _p, _id) physx::profile::stopEvent( _id.mCompileTimeEnabled, _p.getProfileEventSender(), _id, _p.getEventContext() );

#define CM_PROFILE_ZONE( _p, _id) \
	physx::profile::DynamicallyEnabledScopedEvent<PxProfileEventSender> scopedEvent( _p.getProfileEventSender(), _id, _p.getEventContext() );

#define CM_PROFILE_ZONE_WITH_SUBSYSTEM( _p, subsystem, eventId ) CmProfileZone<PX_PROFILE_EVENT_FILTER_VALUE(subsystem,eventId)> __zone( _p, physx::profile::EventIds::subsystem##eventId );
#define CM_PROFILE_VALUE( _p, subsystem, eventId, value ) CmProfileValue<PX_PROFILE_EVENT_FILTER_VALUE(subsystem,eventId)> __val( _p, physx::profile::EventIds::subsystem##eventId, static_cast<PxI64>( value ) );

// there is just one filtering option for all tasks now
#define CM_PROFILE_TASK_ZONE(_p, _id) CM_PROFILE_ZONE( _p, _id )

#define CM_CROSSTHREAD_FAKE_THREADID 99999789
#define CM_PROFILE_START_CROSSTHREAD( _p, _id) \
if ( _id.mCompileTimeEnabled && _p.getProfileEventSender() ) _p.getProfileEventSender()->startEvent( _id, _p.getEventContext(), CM_CROSSTHREAD_FAKE_THREADID );

#define CM_PROFILE_STOP_CROSSTHREAD( _p, _id) \
if ( _id.mCompileTimeEnabled && _p.getProfileEventSender() ) _p.getProfileEventSender()->stopEvent( _id, _p.getEventContext(), CM_CROSSTHREAD_FAKE_THREADID );

}

#endif

