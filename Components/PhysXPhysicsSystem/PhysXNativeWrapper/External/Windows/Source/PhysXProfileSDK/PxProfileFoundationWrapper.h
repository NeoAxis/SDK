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


#ifndef PX_PHYSX_PROFILE_FOUNDATION_WRAPPER_H
#define PX_PHYSX_PROFILE_FOUNDATION_WRAPPER_H
#include "PxProfileBase.h"
#include "PxFoundation.h"
#include "PxBroadcastingAllocator.h"
#include "PxAllocatorCallback.h"
#include "PxErrorCallback.h"
#include "PsArray.h"
#include "PsHashMap.h"

namespace physx { namespace profile {

	struct NullErrorCallback : public PxErrorCallback
	{
		void reportError(PxErrorCode::Enum, const char* , const char* , int )
		{

//			fprintf( stderr, "%s:%d: %s\n", file, line, message );
		}
	};

	struct FoundationWrapper
	{
		PxAllocatorCallback*			mUserAllocator;

		FoundationWrapper( PxAllocatorCallback& inUserAllocator )
			: mUserAllocator( &inUserAllocator )
		{
		}

		FoundationWrapper( PxAllocatorCallback* inUserAllocator )
			: mUserAllocator( inUserAllocator )
		{
		}
		
		FoundationWrapper( PxFoundation* inUserFoundation, bool useBaseAllocator = false )
			: mUserAllocator( NULL )
		{
			if ( inUserFoundation != NULL )
			{
				if ( useBaseAllocator )
					mUserAllocator = &inUserFoundation->getAllocatorCallback();
				else
					mUserAllocator = &inUserFoundation->getAllocator();
			}
		}

		PxAllocatorCallback&		getAllocator() const
		{
			PX_ASSERT( NULL != mUserAllocator );
			return *mUserAllocator;
		}
	};

	template <typename T>
	class WrapperReflectionAllocator
	{
		static const char* getName()
		{
#if defined PX_GNUC
			return __PRETTY_FUNCTION__;
#else
			return typeid(T).name();
#endif
		}
		FoundationWrapper* mWrapper;

	public:
		WrapperReflectionAllocator(FoundationWrapper& inWrapper) : mWrapper( &inWrapper )	{}
		WrapperReflectionAllocator( const WrapperReflectionAllocator& inOther )
			: mWrapper( inOther.mWrapper )
		{
		}
		WrapperReflectionAllocator& operator=( const WrapperReflectionAllocator& inOther )
		{
			mWrapper = inOther.mWrapper;
			return *this;
		}
		PxAllocatorCallback& getAllocator() { return mWrapper->getAllocator(); }
		void* allocate(size_t size, const char* filename, int line)
		{
#if defined(PX_CHECKED) // checked and debug builds
			static const char* handle = getName();
			if(!size)
				return 0;
			return getAllocator().allocate(size, handle, filename, line);
#else
			return getAllocator().allocate(size, "<no allocation names in this config>", filename, line);
#endif
		}
		void deallocate(void* ptr)
		{
			if(ptr)
				getAllocator().deallocate(ptr);
		}
	};
	
	struct WrapperNamedAllocator
	{
		FoundationWrapper*	mWrapper;
		const char*			mAllocationName;
		WrapperNamedAllocator(FoundationWrapper& inWrapper, const char* inAllocationName) 
			: mWrapper( &inWrapper )
			, mAllocationName( inAllocationName ) 
		{}
		WrapperNamedAllocator( const WrapperNamedAllocator& inOther )
			: mWrapper( inOther.mWrapper )
			, mAllocationName( inOther.mAllocationName )
		{
		}
		WrapperNamedAllocator& operator=( const WrapperNamedAllocator& inOther )
		{
			mWrapper = inOther.mWrapper;
			mAllocationName = inOther.mAllocationName;
			return *this;
		}
		PxAllocatorCallback& getAllocator() { return mWrapper->getAllocator(); }
		void* allocate(size_t size, const char* filename, int line)
		{
			static const char* handle = mAllocationName;
			if(!size)
				return 0;
			return getAllocator().allocate(size, handle, filename, line);
		}
		void deallocate(void* ptr)
		{
			if(ptr)
				getAllocator().deallocate(ptr);
		}
	};

	template<class T>
	struct ProfileArray : public Array<T, WrapperReflectionAllocator<T> >
	{
		typedef WrapperReflectionAllocator<T> TAllocatorType;

		ProfileArray( FoundationWrapper& inWrapper )
			: Array<T, TAllocatorType >( TAllocatorType( inWrapper ) )
		{
		}
		
		ProfileArray( const ProfileArray< T >& inOther )
			: Array<T, TAllocatorType >( inOther, inOther )
		{
		}
	};

	template<typename TKeyType, typename TValueType, typename THashType=Hash<TKeyType> >
	struct ProfileHashMap : public HashMap<TKeyType, TValueType, THashType, WrapperReflectionAllocator< TValueType > >
	{
		typedef HashMap<TKeyType, TValueType, THashType, WrapperReflectionAllocator< TValueType > > THashMapType;
		typedef WrapperReflectionAllocator<TValueType> TAllocatorType;
		ProfileHashMap( FoundationWrapper& inWrapper )
			: THashMapType( TAllocatorType( inWrapper ) )
		{
		}
	};

	template<typename TDataType>
	inline TDataType* PxProfileAllocate( PxAllocatorCallback* inAllocator, const char* file, int inLine )
	{
		FoundationWrapper wrapper( inAllocator );
		typedef WrapperReflectionAllocator< TDataType > TAllocator;
		TAllocator theAllocator( wrapper );
		return reinterpret_cast<TDataType*>( theAllocator.allocate( sizeof( TDataType ), file, inLine ) );
	}

	template<typename TDataType>
	inline TDataType* PxProfileAllocate( PxAllocatorCallback& inAllocator, const char* file, int inLine )
	{
		return PxProfileAllocate<TDataType>( &inAllocator, file, inLine );
	}

	template<typename TDataType>
	inline TDataType* PxProfileAllocate( PxFoundation* inFoundation, const char* file, int inLine )
	{
		FoundationWrapper wrapper( inFoundation );
		typedef WrapperReflectionAllocator< TDataType > TAllocator;
		TAllocator theAllocator( wrapper );
		return reinterpret_cast<TDataType*>( theAllocator.allocate( sizeof( TDataType ), file, inLine ) );
	}

	template<typename TDataType>
	inline void PxProfileDeleteAndDeallocate( FoundationWrapper& inAllocator, TDataType* inDType )
	{
		PX_ASSERT(inDType);
		PxAllocatorCallback& allocator( inAllocator.getAllocator() );
		inDType->~TDataType();
		allocator.deallocate( inDType );
	}

	template<typename TDataType>
	inline void PxProfileDeleteAndDeallocate( PxAllocatorCallback& inAllocator, TDataType* inDType )
	{
		FoundationWrapper wrapper( &inAllocator );
		typedef WrapperReflectionAllocator< TDataType > TAllocator;
		PxProfileDeleteAndDeallocate( wrapper, inDType );
	}
	
	template<typename TDataType>
	inline void PxProfileDeleteAndDeallocate( PxFoundation* inAllocator, TDataType* inDType )
	{
		FoundationWrapper wrapper( inAllocator );
		typedef WrapperReflectionAllocator< TDataType > TAllocator;
		PxProfileDeleteAndDeallocate( wrapper, inDType );
	}
} }

#define PX_PROFILE_NEW( allocator, dtype ) new (PxProfileAllocate<dtype>( allocator, __FILE__, __LINE__ )) dtype
#define PX_PROFILE_DELETE( allocator, obj ) PxProfileDeleteAndDeallocate( allocator, obj );

#endif