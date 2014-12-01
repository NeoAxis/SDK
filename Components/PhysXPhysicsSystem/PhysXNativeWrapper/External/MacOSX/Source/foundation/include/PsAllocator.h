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


#ifndef PX_FOUNDATION_PSALLOCATOR_H
#define PX_FOUNDATION_PSALLOCATOR_H

#include "foundation/PxAllocatorCallback.h"
#include "Ps.h"

#if (defined(PX_WINDOWS) || defined(PX_X360) || defined PX_WIN8ARM)
#include <typeinfo.h>
#endif
#if (defined(PX_APPLE))
#include <typeinfo>
#endif

#include <new>

// Allocation macros going through user allocator 
#ifdef _DEBUG
#define PX_ALLOC(n, name)		 physx::shdfnd::NamedAllocator(name).allocate(n, __FILE__, __LINE__)
#else
#define PX_ALLOC(n, name)        physx::shdfnd::Allocator().allocate(n, __FILE__, __LINE__)
#endif
#define PX_ALLOC_TEMP(n, name)   PX_ALLOC(n, name)
#define PX_FREE(x)              physx::shdfnd::Allocator().deallocate(x)
#define PX_FREE_AND_RESET(x)    { PX_FREE(x); x=0; }


// The following macros support plain-old-types and classes derived from UserAllocated.
#define PX_NEW(T)               new(physx::shdfnd::ReflectionAllocator<T>(), __FILE__, __LINE__) T
#define PX_NEW_TEMP(T)          PX_NEW(T)
#define PX_DELETE(x)            delete x
#define PX_DELETE_AND_RESET(x)  { PX_DELETE(x); x=0; }
#define PX_DELETE_POD(x)        { PX_FREE(x); x=0; }
#define PX_DELETE_ARRAY(x)      { PX_DELETE([]x); x=0; }

// aligned allocation
#define PX_ALIGNED16_ALLOC(n) 	physx::shdfnd::AlignedAllocator<16>().allocate(n, __FILE__, __LINE__)
#define PX_ALIGNED16_FREE(x)	physx::shdfnd::AlignedAllocator<16>().deallocate(x) 

//! placement new macro to make it easy to spot bad use of 'new'
#define PX_PLACEMENT_NEW(p, T)  new(p) T

// Don't use inline for alloca !!!
#ifdef PX_WINDOWS
    #include <malloc.h>
    #define PxAlloca(x) _alloca(x)
#elif defined(PX_LINUX) || defined(PX_ANDROID)
    #include <malloc.h>
    #define PxAlloca(x) alloca(x)
#elif defined(PX_PSP2)
    #include <alloca.h>
    #define PxAlloca(x) alloca(x)
#elif defined(PX_WIN8ARM)
    #include <malloc.h>
    #define PxAlloca(x) alloca(x)
#elif defined(PX_APPLE)
    #include <alloca.h>
    #define PxAlloca(x) alloca(x)
#elif defined(PX_PS3)
    #include <alloca.h>
    #define PxAlloca(x) alloca(x)
#elif defined(PX_X360)
    #include <malloc.h>
    #define PxAlloca(x) _alloca(x)
#elif defined(PX_WII)
    #include <alloca.h>
    #define PxAlloca(x) alloca(x)
#endif

namespace physx
{
namespace shdfnd
{
	PX_FOUNDATION_API PxAllocatorCallback& getAllocator();

	/*
	 * Bootstrap allocator using malloc/free.
	 * Don't use unless your objects get allocated before foundation is initialized.
	 */
	class RawAllocator
	{
	public:
		RawAllocator(const char* = 0) {}
		void* allocate(size_t size, const char*, int) 
		{
			// malloc returns valid pointer for size==0, no need to check
			return ::malloc(size); 
		}
		void deallocate(void* ptr) 
		{ 
			// free(0) is guaranteed to have no side effect, no need to check
			::free(ptr); 
		}
	};

	/**
	Allocator used to access the global PxAllocatorCallback instance without providing additional information.
	*/
	class PX_FOUNDATION_API Allocator
	{
	public:
		Allocator(const char* = 0) {}
		void* allocate(size_t size, const char* file, int line);
		void deallocate(void* ptr);
	};

	/**
	Allocator used to access the global PxAllocatorCallback instance using a dynamic name.
	*/
#if defined(_DEBUG) || defined(PX_CHECKED) // see comment in cpp
	class PX_FOUNDATION_API NamedAllocator
	{
	public:
		NamedAllocator(const PxEmpty&);
		NamedAllocator(const char* name = 0); // todo: should not have default argument!
		NamedAllocator(const NamedAllocator&);
		~NamedAllocator();
		NamedAllocator& operator=(const NamedAllocator&);
		void* allocate(size_t size, const char* filename, int line);
		void deallocate(void* ptr);
	};
#else
	class NamedAllocator;
#endif // _DEBUG

	/**
    Allocator used to access the global PxAllocatorCallback instance using a static name derived from T.
	*/
	template <typename T>
	class ReflectionAllocator
	{
		static const char* getName()
		{
#if defined PX_GNUC
			return __PRETTY_FUNCTION__;
#else
			return typeid(T).name();
#endif
		}
	public:
		ReflectionAllocator(const PxEmpty&)	{}
		ReflectionAllocator(const char* =0) {}
		inline ReflectionAllocator(const ReflectionAllocator& ) { }
		void* allocate(size_t size, const char* filename, int line)
		{
#if defined(PX_CHECKED) // checked and debug builds
			static const char* handle = getName();
			return size ? getAllocator().allocate(size, handle, filename, line) : 0;
#else
			return size ? getAllocator().allocate(size, "<no allocation names in this config>", filename, line) : 0;
#endif

		}
		void deallocate(void* ptr)
		{
			if(ptr)
				getAllocator().deallocate(ptr);
		}
	};

	template <typename T>
	struct AllocatorTraits
	{
#if defined(PX_CHECKED) // checked and debug builds
		typedef NamedAllocator Type;
#else
		typedef ReflectionAllocator<T> Type;
#endif
	};

    // if you get a build error here, you are trying to PX_NEW a class
    // that is neither plain-old-type nor derived from UserAllocated
	template <typename T, typename X>
	union EnableIfPod
	{
		int i; T t;
		typedef X Type;
	};

} // namespace shdfnd
} // namespace physx

// Global placement new for ReflectionAllocator templated by
// plain-old-type. Allows using PX_NEW for pointers and built-in-types.
//
// ATTENTION: You need to use PX_DELETE_POD or PX_FREE to deallocate
// memory, not PX_DELETE. PX_DELETE_POD redirects to PX_FREE.
//
// Rationale: PX_DELETE uses global operator delete(void*), which we dont' want to overload.
// Any other definition of PX_DELETE couldn't support array syntax 'PX_DELETE([]a);'.
// PX_DELETE_POD was preferred over PX_DELETE_ARRAY because it is used
// less often and applies to both single instances and arrays.
template <typename T>
PX_INLINE void* operator new(size_t size, physx::shdfnd::ReflectionAllocator<T> alloc, const char* fileName, typename physx::shdfnd::EnableIfPod<T, int>::Type line)
{
	return alloc.allocate(size, fileName, line);
}

template <typename T>
PX_INLINE void* operator new[](size_t size, physx::shdfnd::ReflectionAllocator<T> alloc, const char* fileName, typename physx::shdfnd::EnableIfPod<T, int>::Type line)
{
	return alloc.allocate(size, fileName, line);
}

// If construction after placement new throws, this placement delete is being called.
template <typename T>
PX_INLINE void  operator delete(void* ptr, physx::shdfnd::ReflectionAllocator<T> alloc, const char* fileName, typename physx::shdfnd::EnableIfPod<T, int>::Type line)
{
	alloc.deallocate(ptr);
}

// If construction after placement new throws, this placement delete is being called.
template <typename T>
PX_INLINE void  operator delete[](void* ptr, physx::shdfnd::ReflectionAllocator<T> alloc, const char* fileName, typename physx::shdfnd::EnableIfPod<T, int>::Type line)
{
	alloc.deallocate(ptr);
}

#endif
