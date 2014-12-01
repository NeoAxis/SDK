// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#if (defined(_WIN32) || defined(__WIN32__))
	#define PLATFORM_WINDOWS
#elif defined(__APPLE_CC__)
	#define PLATFORM_MACOS
#elif defined(ANDROID)
	#define PLATFORM_ANDROID
#else
	#error Platform is not supported.
#endif

#include <memory.h>
#include <stdlib.h>
#ifdef PLATFORM_WINDOWS
	#include <malloc.h>
#endif
#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include <float.h>
#include <math.h>
#include <wchar.h>

#ifdef PLATFORM_MACOS
	#include <new>
#endif

//!!!!!!dr
#ifdef PLATFORM_ANDROID
	#include <stdlib.h>
	#include <pthread.h>
	#include <string.h>
	#include <stdint.h>
	#include <android/log.h>
#endif

//#include "MemoryManager.h"
//
//#undef malloc
//#undef calloc
//#undef realloc
//#undef free
//
//#ifdef _WIN32
//	#define _DefinedMemoryAllocationType MemoryAllocationType_SoundAndVideo
//	#include "MemoryManager_ManageNew.h"
//	#undef _DefinedMemoryAllocationType
//#else
//	#define _DefinedMemoryAllocationType MemoryAllocationType_SoundAndVideo
//	#include "MemoryManager_SimpleNew.h"
//	#undef _DefinedMemoryAllocationType
//#endif

#include <vector>

#ifdef WIN32
	#include <windows.h>
#endif

#ifdef _DEBUG
#error Debug version are not supported.
#endif
