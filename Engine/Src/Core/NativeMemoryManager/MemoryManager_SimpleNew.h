// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#ifdef NATIVE_MEMORY_MANAGER_ENABLE

#if (defined(_WIN32) || defined(__WIN32__))
	#define MEMORYMANAGER_NEWDELETE_INLINE inline
#elif defined(__APPLE_CC__)
	#define MEMORYMANAGER_NEWDELETE_INLINE __private_extern__ inline __attribute__((always_inline))
#else
	#define MEMORYMANAGER_NEWDELETE_INLINE inline __attribute__((always_inline))
#endif

#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable : 4595)
#endif

MEMORYMANAGER_NEWDELETE_INLINE void* NATIVEMEMORYMANAGER_CALLING_CONVENTION operator new(size_t nSize)
{
	return Memory_Alloc( _DefinedMemoryAllocationType, (int)nSize, NULL, 0 );
}

MEMORYMANAGER_NEWDELETE_INLINE void NATIVEMEMORYMANAGER_CALLING_CONVENTION operator delete(void* p)
{
	Memory_Free( p );
}

MEMORYMANAGER_NEWDELETE_INLINE void* NATIVEMEMORYMANAGER_CALLING_CONVENTION operator new[](size_t nSize)
{
	return Memory_Alloc( _DefinedMemoryAllocationType, (int)nSize, NULL, 0 );
}

MEMORYMANAGER_NEWDELETE_INLINE void NATIVEMEMORYMANAGER_CALLING_CONVENTION operator delete[](void* p)
{
	Memory_Free( p );
}

#ifdef _WIN32
#pragma warning( pop ) 
#endif

#endif //NATIVE_MEMORY_MANAGER_ENABLE
