#ifndef ODECONFIG_H
#define ODECONFIG_H

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

#ifndef dDOUBLE
#ifndef dSINGLE
#define dSINGLE
#endif
#endif

//betauser
#define ODE_DLL
#define dTRIMESH_ENABLED 1
#define dTRIMESH_OPCODE 1
#define dNODEBUG

//betauser
#ifdef PLATFORM_WINDOWS
	#define _HAS_ITERATOR_DEBUGGING 0
#endif

//betauser
#include <memory.h>
#include <stdlib.h>
#ifdef _WIN32
	#include <malloc.h>
#endif
/* Pull in the standard headers */
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <math.h>
#include <string.h>
#include <float.h>
#include <math.h>
#include <wchar.h>
#ifndef _WIN32
  #include <alloca.h>
#endif

#ifdef PLATFORM_WINDOWS
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

//#include "opcode.h"

void ShowMessageBox(const char* text, const char* title);

//betauser END





#if defined(ODE_DLL) || defined(ODE_LIB) || !defined(_MSC_VER)
#define __ODE__
#endif

/* Define a DLL export symbol for those platforms that need it */
#if defined(_MSC_VER)
  #if defined(ODE_DLL)
    #define ODE_API __declspec(dllexport)
  #elif !defined(ODE_LIB)
    #define ODE_DLL_API __declspec(dllimport)
  #endif
#endif

#if !defined(ODE_API)
  #define ODE_API
#endif

#if defined(_MSC_VER)
#  define ODE_API_DEPRECATED __declspec(deprecated)
#elif defined (__GNUC__) && ( (__GNUC__ > 3) || ((__GNUC__ == 3) && (__GNUC_MINOR__ >= 1)) )
#  define ODE_API_DEPRECATED __attribute__((__deprecated__))
#else
#  define ODE_API_DEPRECATED
#endif

/* Well-defined common data types...need to define for 64 bit systems */
#if defined(_M_IA64) || defined(__ia64__) || defined(_M_AMD64) || defined(__x86_64__)
  #define X86_64_SYSTEM   1
  typedef int             int32;
  typedef unsigned int    uint32;
  typedef short           int16;
  typedef unsigned short  uint16;
  typedef char            int8;
  typedef unsigned char   uint8;
#else
  typedef int             int32;
  typedef unsigned int    uint32;
  typedef short           int16;
  typedef unsigned short  uint16;
  typedef char            int8;
  typedef unsigned char   uint8;
#endif

/* Visual C does not define these functions */
#if defined(_MSC_VER)
  #define copysignf _copysign
  #define copysign _copysign
#endif



/* Define the dInfinity macro */
#ifdef INFINITY
  #define dInfinity INFINITY
#elif defined(HUGE_VAL)
  #ifdef dSINGLE
    #ifdef HUGE_VALF
      #define dInfinity HUGE_VALF
    #else
      #define dInfinity ((float)HUGE_VAL)
    #endif
  #else
    #define dInfinity HUGE_VAL
  #endif
#else
  #ifdef dSINGLE
    #define dInfinity ((float)(1.0/0.0))
  #else
    #define dInfinity (1.0/0.0)
  #endif
#endif


#endif
