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


#ifndef PX_PHYSX_PROFILE_SDK_MANAGER_IMPL_H
#define PX_PHYSX_PROFILE_SDK_MANAGER_IMPL_H
#include "PxProfileZoneManager.h"
#include "PxProfileBase.h"
#include "PsArray.h"
#include "PsMutex.h"
#include "PxProfileScopedMutexLock.h"
#include "PxProfileZone.h"
#include "PxProfileFoundationWrapper.h"

namespace physx { namespace profile {

	struct NullEventNameProvider : public PxProfileNameProvider
	{
		virtual PxProfileNames getProfileNames() const { return PxProfileNames( 0, 0 ); }
	};

	class ZoneManagerImpl : public PxProfileZoneManager
	{
		typedef ScopedLockImpl<Mutex> TScopedLockType;
		FoundationWrapper					mWrapper;
		ProfileArray<PxProfileZone*>			mZones;
		ProfileArray<PxProfileZoneHandler*>	mHandlers;
		PxUserCustomProfiler				*mUserCustomProfiler;
		Mutex mMutex;

		ZoneManagerImpl( const ZoneManagerImpl& inOther );
		ZoneManagerImpl& operator=( const ZoneManagerImpl& inOther );

	public:

		ZoneManagerImpl(PxFoundation* inFoundation) 
			: mWrapper( inFoundation )
			, mZones( mWrapper )
			, mHandlers( mWrapper ) 
			, mUserCustomProfiler(NULL)
		{}

		virtual ~ZoneManagerImpl()
		{
			//This assert would mean that a profile zone is outliving us.
			//This will cause a crash when the profile zone is released.
			PX_ASSERT( mZones.size() == 0 );
			while( mZones.size() )
				removeProfileZone( *mZones.back() );
		}

		virtual void addProfileZone( PxProfileZone& inSDK )
		{
			TScopedLockType lock( &mMutex );
			
			if ( inSDK.getProfileZoneManager() != NULL )
			{
				if ( inSDK.getProfileZoneManager() == this )
					return;
				else //there must be two managers in the system somehow.
				{
					PX_ASSERT( false );
					inSDK.getProfileZoneManager()->removeProfileZone( inSDK );
				}
			}
			inSDK.setUserCustomProfiler(mUserCustomProfiler);
			mZones.pushBack( &inSDK );
			inSDK.setProfileZoneManager( this );
			for ( PxU32 idx =0; idx < mHandlers.size(); ++idx )
				mHandlers[idx]->onZoneAdded( inSDK );
		}

		virtual void removeProfileZone( PxProfileZone& inSDK )
		{
			TScopedLockType lock( &mMutex );
			if ( inSDK.getProfileZoneManager() == NULL )
				return;

			else if ( inSDK.getProfileZoneManager() != this )
			{
				PX_ASSERT( false );
				inSDK.getProfileZoneManager()->removeProfileZone( inSDK );
				return;
			}

			inSDK.setProfileZoneManager( NULL );
			for ( PxU32 idx = 0; idx < mZones.size(); ++idx )
			{
				if ( mZones[idx] == &inSDK )
				{
					for ( PxU32 handler =0; handler < mHandlers.size(); ++handler )
						mHandlers[handler]->onZoneRemoved( inSDK );
					mZones.replaceWithLast( idx );
				}
			}
		}

		virtual void flushProfileEvents()
		{
			PxU32 sdkCount = mZones.size();
			for ( PxU32 idx = 0; idx < sdkCount; ++idx )
				mZones[idx]->flushProfileEvents();
		}

		virtual void addProfileZoneHandler( PxProfileZoneHandler& inHandler )
		{
			TScopedLockType lock( &mMutex );
			mHandlers.pushBack( &inHandler );
			for ( PxU32 idx = 0; idx < mZones.size(); ++idx )
				inHandler.onZoneAdded( *mZones[idx] );
		}

		virtual void removeProfileZoneHandler( PxProfileZoneHandler& inHandler )
		{
			TScopedLockType lock( &mMutex );
			for( PxU32 idx = 0; idx < mZones.size(); ++idx )
				inHandler.onZoneRemoved( *mZones[idx] );
			for( PxU32 idx = 0; idx < mHandlers.size(); ++idx )
			{
				if ( mHandlers[idx] == &inHandler )
					mHandlers.replaceWithLast( idx );
			}
		}
		
		virtual PxProfileZone& createProfileZone( const char* inSDKName, PxProfileNameProvider* inProvider, PxU32 inEventBufferByteSize )
		{
			NullEventNameProvider nullProvider;
			if ( inProvider == NULL )
				inProvider = &nullProvider;
			return createProfileZone( inSDKName, inProvider->getProfileNames(), inEventBufferByteSize );
		}
		
		
		virtual PxProfileZone& createProfileZone( const char* inSDKName, PxProfileNames inNames, PxU32 inEventBufferByteSize )
		{
			PxProfileZone& retval( PxProfileZone::createProfileZone( &mWrapper.getAllocator(), inSDKName, inNames, inEventBufferByteSize ) );
			addProfileZone( retval );
			return retval;
		}

		virtual void release() 
		{  
			PX_PROFILE_DELETE( mWrapper.getAllocator(), this );
		}

		// Notify all existing zones of the new user custom profiler
		virtual void setUserCustomProfiler(PxUserCustomProfiler *callback)
		{
			mUserCustomProfiler = callback;
			for ( PxU32 idx = 0; idx < mZones.size(); ++idx )
			{
				mZones[idx]->setUserCustomProfiler(mUserCustomProfiler);
			}
		}
	};
} }


#endif