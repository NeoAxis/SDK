///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/*
 *	OPCODE - Optimized Collision Detection
 *	Copyright (C) 2001 Pierre Terdiman
 *	Homepage: http://www.codercorner.com/Opcode.htm
 */
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/**
 *	Main file for Opcode.dll.
 *	\file		Opcode.h
 *	\author		Pierre Terdiman
 *	\date		March, 20, 2001
 */
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Include Guard
#ifndef __OPCODE_H__
#define __OPCODE_H__

//betauser

#if (defined(_WIN32) || defined(__WIN32__))
	#define PLATFORM_WINDOWS
#elif defined(__APPLE_CC__)
	#define PLATFORM_MACOS
#elif defined(ANDROID)
	#define PLATFORM_ANDROID
#else
	#error Platform is not supported.
#endif

//betauser
#ifdef PLATFORM_WINDOWS
	#define _HAS_ITERATOR_DEBUGGING 0
#endif

#include <memory.h>
#include <stdlib.h>
#ifdef _WIN32
	#include <malloc.h>
#endif
#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include <float.h>
#include <math.h>
#include <wchar.h>
#ifndef _WIN32
  #include <alloca.h>
#endif

#ifdef _WIN32
	#define _MFC_OVERRIDES_NEW
	#include <crtdbg.h>
#endif

#ifdef __cplusplus
#ifdef PLATFORM_MACOS
	#include <new>
#endif
#endif //__cplusplus

#define _DefinedMemoryAllocationType MemoryAllocationType_Physics
#include "MemoryManager.h"

#ifdef __cplusplus

#include "MemoryManager_SimpleNew.h"

#include <vector>
#include <map>
#include <string>
#include <set>
#include <list>
#include <deque>
#include <queue>
#include <bitset>
#include <algorithm>
#include <functional>
#include <limits>
#include <fstream>
#include <iostream>
#include <iomanip>
#include <sstream>

////betauser
//#ifdef PLATFORM_WINDOWS
//	#ifndef STLPORT
//		#error STLPORT is not included.
//	#endif
//#endif

#ifdef PLATFORM_WINDOWS
#include "MemoryManager_ManageNew.h"
#endif// PLATFORM_WINDOWS

#endif

#undef malloc
#undef calloc
#undef realloc
#undef free
#define malloc XXX
#define calloc XXX
#define realloc XXX
#define free XXX

//betauser END

//betauser
#pragma warning(disable : 4312)
#pragma warning(disable : 4311)

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Things to help us compile on non-windows platforms

#if defined(__APPLE__) || defined(__MACOSX__)
#if __APPLE_CC__ < 1495
#define sqrtf sqrt
#define sinf sin
#define cosf cos
#define acosf acos
#define asinf asin
#endif
#endif

#ifndef _MSC_VER
#ifndef __int64
#define __int64 long long int
#endif
#ifndef __stdcall /* this is defined in MinGW and CygWin, so avoid the warning */
#define __stdcall /* */
#endif
#endif

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Compilation messages
#ifdef _MSC_VER
	#if defined(OPCODE_EXPORTS)
		// #pragma message("Compiling OPCODE")
	#elif !defined(OPCODE_EXPORTS)
		// #pragma message("Using OPCODE")
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Automatic linking
		#ifndef BAN_OPCODE_AUTOLINK
			#ifdef _DEBUG
				//#pragma comment(lib, "Opcode_D.lib")
			#else
				//#pragma comment(lib, "Opcode.lib")
			#endif
		#endif
	#endif
#endif

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Preprocessor
#ifndef ICE_NO_DLL
	#ifdef OPCODE_EXPORTS
		#define OPCODE_API// __declspec(dllexport)
	#else
		#define OPCODE_API// __declspec(dllimport)
	#endif
#else
		#define OPCODE_API
#endif

	#include "OPC_Settings.h"
	#include "OPC_IceHook.h"

	namespace Opcode
	{
		// Bulk-of-the-work
		#include "OPC_Common.h"
		#include "OPC_MeshInterface.h"
		// Builders
		#include "OPC_TreeBuilders.h"
		// Trees
		#include "OPC_AABBTree.h"
		#include "OPC_OptimizedTree.h"
		// Models
		#include "OPC_BaseModel.h"
		#include "OPC_Model.h"
		#include "OPC_HybridModel.h"
		// Colliders
		#include "OPC_Collider.h"
		#include "OPC_VolumeCollider.h"
		#include "OPC_TreeCollider.h"
		#include "OPC_RayCollider.h"
		#include "OPC_SphereCollider.h"
		#include "OPC_OBBCollider.h"
		#include "OPC_AABBCollider.h"
		#include "OPC_LSSCollider.h"
		#include "OPC_PlanesCollider.h"
		// Usages
		#include "OPC_Picking.h"


		FUNCTION OPCODE_API bool InitOpcode();
		FUNCTION OPCODE_API bool CloseOpcode();
	}

#endif // __OPCODE_H__
