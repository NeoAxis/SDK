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


#ifndef PX_FOUNDATION_PSFOUNDATION_H
#define PX_FOUNDATION_PSFOUNDATION_H

#include "Ps.h"
#include "PsInlineArray.h"
#include "foundation/PxFoundation.h"
#include "foundation/PxErrors.h"
#include "PsMutex.h"
#include "foundation/PxBroadcastingAllocator.h"
#include "PsPAEventSrc.h"

#include <stdarg.h>

#include "PsHashMap.h"

namespace physx
{
namespace shdfnd
{

	union TempAllocatorChunk;

	class PxAllocatorListenerManager;

	class PX_FOUNDATION_API Foundation : public PxFoundation
	{
		typedef HashMap<const NamedAllocator*, const char*, 
			Hash<const NamedAllocator*>, Allocator> AllocNameMap;

		typedef Array<TempAllocatorChunk*, Allocator> AllocFreeTable;

										Foundation(PxErrorCallback& errc, PxAllocatorCallback& alloc);
										~Foundation();

	public:
		void							release();

		// factory
		static Foundation*				createInstance(PxU32 version, PxErrorCallback& errc, PxAllocatorCallback& alloc);
		// note, you MUST call destroyInstance iff createInstance returned true!
		static void						destroyInstance();

		static Foundation& 				getInstance();

		static void						incRefCount();  // this call requires a foundation object to exist already
		static void						decRefCount();  // this call requires a foundation object to exist already

		virtual PxErrorCallback&		getErrorCallback() const;
		virtual void					setErrorLevel(PxErrorCode::Enum mask);
		virtual PxErrorCode::Enum		getErrorLevel() const;

		virtual PxBroadcastingAllocator& getAllocator() const { return mAllocator; }
		virtual PxAllocatorCallback&	getAllocatorCallback() const;
		PxAllocatorCallback& 			getCheckedAllocator() { return mAllocator; }

		//! error reporting function
		void 							error(PxErrorCode::Enum, const char* file, int line, const char* messageFmt, ...);
		void 							errorImpl(PxErrorCode::Enum, const char* file, int line, const char* messageFmt, va_list );
		PxI32							getWarnOnceTimestamp(); 

		PX_INLINE	AllocNameMap&		getNamedAllocMap()		{ return mNamedAllocMap;		}
		PX_INLINE	Mutex&				getNamedAllocMutex()	{ return mNamedAllocMutex;		}

		PX_INLINE	AllocFreeTable&		getTempAllocFreeTable()	{ return mTempAllocFreeTable;	}
		PX_INLINE	Mutex&				getTempAllocMutex()		{ return mTempAllocMutex;		}

		PX_INLINE   PAUtils&            getPAUtils()			{ return mPAUtils;				}

	private:
		class AlignCheckAllocator: public PxBroadcastingAllocator
		{
			static const PxU32 MaxListenerCount = 5;
		public:
			AlignCheckAllocator(PxAllocatorCallback& originalAllocator)
					: mAllocator(originalAllocator)
					, mListenerCount( 0 ){}

			void					deallocate(void* ptr)		
			{ 
				//So here, for performance reasons I don't grab the mutex.
				//The listener array is very rarely changing; for most situations
				//only at startup.  So it is unlikely that using the mutex
				//will help a lot but it could have serious perf implications.
				PxU32 theCount = mListenerCount;
				for( PxU32 idx = 0; idx < theCount; ++idx )
					mListeners[idx]->onDeallocation( ptr );
				mAllocator.deallocate(ptr); 
			}
			void*					allocate(size_t size, const char* typeName, const char* filename, int line);
			PxAllocatorCallback&	getBaseAllocator() const	{ return mAllocator; }
			void registerAllocationListener( PxAllocationListener& inListener )
			{
				PX_ASSERT( mListenerCount < MaxListenerCount );
				if ( mListenerCount < MaxListenerCount )
				{
					mListeners[mListenerCount] = &inListener;
					++mListenerCount;
				}
			}
			void deregisterAllocationListener( PxAllocationListener& inListener )
			{
				for( PxU32 idx = 0; idx < mListenerCount; ++idx )
				{
					if ( mListeners[idx] == &inListener )
					{
						mListeners[idx] = mListeners[mListenerCount-1];
						--mListenerCount;
						break;
					}
				}
			}
		private:
			PxAllocatorCallback& mAllocator;
			//I am not sure about using a PxArray here.
			//For now, this is fine.
			PxAllocationListener*	mListeners[MaxListenerCount];
			volatile PxU32			mListenerCount;
		};

		// init order is tricky here: the mutexes require the allocator, the allocator may require the error stream
		PxErrorCallback&				mErrorCallback;
		mutable AlignCheckAllocator		mAllocator;

		PxErrorCode::Enum				mErrorMask;
		Mutex							mErrorMutex;

		AllocNameMap					mNamedAllocMap;
		Mutex							mNamedAllocMutex;

		AllocFreeTable					mTempAllocFreeTable;
		Mutex							mTempAllocMutex;

		PAUtils							mPAUtils;

		static Foundation*				mInstance;
		static PxU32					mRefCount;		
	};


	PX_INLINE Foundation& getFoundation()
	{
		return Foundation::getInstance();
	}

} // namespace shdfnd
} // namespace physx

//shortcut macros:
//usage: Foundation::error(PX_WARN, "static friction %f is is lower than dynamic friction %d", sfr, dfr);
#define PX_WARN ::physx::PxErrorCode::eDEBUG_WARNING, __FILE__, __LINE__
#define PX_INFO	::physx::PxErrorCode::eDEBUG_INFO, __FILE__, __LINE__

#if defined(_DEBUG) || defined(PX_CHECKED)
#ifdef __SPU__ // SCS: used in CCD from SPU. how can we fix that correctly?
#define PX_WARN_ONCE(condition, string) ((void)0)
#else
#define PX_WARN_ONCE(condition, string)	\
if (condition) { \
	static PxU32 timestap = 0; \
	if (timestap != Ps::getFoundation().getWarnOnceTimestamp()) { \
		timestap = Ps::getFoundation().getWarnOnceTimestamp(); \
		Ps::getFoundation().error(PX_WARN, string); \
	} \
}
#endif
#else
#define PX_WARN_ONCE(condition, string) ((void)0)
#endif

#endif

