// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#if (defined(_WIN32) || defined(__WIN32__))
	#define PLATFORM_WINDOWS
#elif defined(__APPLE_CC__)
	#define PLATFORM_MACOS
#else
	#error Platform is not supported.
#endif

#include <memory.h>
#include <stdlib.h>
//#ifdef PLATFORM_WINDOWS
//	#include <malloc.h>
//#endif
#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include <float.h>
#include <math.h>

//#include "MemoryManager.h"
//
//#undef malloc
//#undef calloc
//#undef realloc
//#undef free

//#ifdef _WIN32
//	#define _DefinedMemoryAllocationType MemoryAllocationType_SoundAndVideo
//	#include "MemoryManager_ManageNew.h"
//	#undef _DefinedMemoryAllocationType
//#else
//	#define _DefinedMemoryAllocationType MemoryAllocationType_SoundAndVideo
//	#include "MemoryManager_SimpleNew.h"
//	#undef _DefinedMemoryAllocationType
//#endif

#ifdef PLATFORM_WINDOWS
	#include <windows.h>
#endif

#ifdef PLATFORM_MACOS
	#include <Carbon/Carbon.h>
#endif

#include "hacdHACD.h"
#include "hacdMicroAllocator.h"

#ifdef _DEBUG
#error Debug version are not supported.
#endif

#ifdef _WIN32
	#define EXPORT extern "C" __declspec(dllexport)
#else
	#define EXPORT extern "C" __attribute__ ((visibility("default")))
#endif
