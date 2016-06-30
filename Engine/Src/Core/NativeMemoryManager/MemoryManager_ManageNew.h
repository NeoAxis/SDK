// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#ifdef NATIVE_MEMORY_MANAGER_ENABLE

#include "MemoryManager_SimpleNew.h"

#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable : 4595)
#endif

inline void* NATIVEMEMORYMANAGER_CALLING_CONVENTION operator new(size_t nSize, int nType, const char* lpszFileName, int nLine)
{
	return Memory_Alloc( _DefinedMemoryAllocationType, (int)nSize, lpszFileName, nLine );
}

inline void NATIVEMEMORYMANAGER_CALLING_CONVENTION operator delete(void* p, int nType, const char*, int)
{
	Memory_Free( p );
}

inline void* NATIVEMEMORYMANAGER_CALLING_CONVENTION operator new[](size_t nSize, int nType, const char* lpszFileName, int nLine)
{
	return ::operator new(nSize, nType, lpszFileName, nLine);
}

inline void NATIVEMEMORYMANAGER_CALLING_CONVENTION operator delete[](void* p, int nType, const char* lpszFileName, int nLine)
{
	::operator delete(p, nType, lpszFileName, nLine);
}

inline void* NATIVEMEMORYMANAGER_CALLING_CONVENTION operator new(size_t nSize, const char* lpszFileName, int nLine)
{
	return ::operator new(nSize, 1/*_NORMAL_BLOCK*/, lpszFileName, nLine);
}

inline void* NATIVEMEMORYMANAGER_CALLING_CONVENTION operator new[](size_t nSize, const char* lpszFileName, int nLine)
{
	return ::operator new[](nSize, 1/*_NORMAL_BLOCK*/, lpszFileName, nLine);
}

inline void NATIVEMEMORYMANAGER_CALLING_CONVENTION operator delete(void* pData, const char*, int)
{
	::operator delete(pData);
}

inline void NATIVEMEMORYMANAGER_CALLING_CONVENTION operator delete[](void* pData, const char*, int)
{
	::operator delete(pData);
}

#ifdef _WIN32
#pragma warning( pop ) 
#endif

#define new new(__FILE__, __LINE__)

#endif //NATIVE_MEMORY_MANAGER_ENABLE
