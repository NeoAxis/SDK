// Copyright (C) 2006-2012 NeoAxis Group Ltd.
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
