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


#ifndef PX_FOUNDATION_PS_H
#define PX_FOUNDATION_PS_H

/*! \file top level include file for shared foundation */

#include "foundation/Px.h"

/**
Platform specific defines
*/
#ifdef PX_WINDOWS
	#pragma intrinsic(memcmp)
	#pragma intrinsic(memcpy)
	#pragma intrinsic(memset)
	#pragma intrinsic(abs)
	#pragma intrinsic(labs)
#endif

#if defined(PX_VC) && !defined(PX_NO_WARNING_PRAGMAS) // get rid of browser info warnings

	// ensure this is reported even when we compile at level 3

	#pragma warning( 3 : 4239 ) // report rvalue to non-const reference conversion as error

	// the 'default' here just ensures these warnings (which are off by default) are turned on.

	#pragma warning( default : 4265 ) // 'class' : class has virtual functions, but destructor is not virtual.
	#pragma warning( default : 4287 ) // 'operator' : unsigned/negative constant mismatch.
	#pragma warning( default : 4296 ) // 'operator' : expression is always false.
	#pragma warning( default : 4302 ) // 'conversion' : truncation from 'type 1' to 'type 2'.
	#pragma warning( default : 4529 ) // 'member_name' : forming a pointer-to-member requires explicit use of the address-of operator ('&') and a qualified name.
	#pragma warning( default : 4555 ) // expression has no effect; expected expression with side-effect.

	#pragma warning( disable : 4127 ) // conditional expression is constant
	#pragma warning( disable : 4201 ) // nonstandard extension used: nameless struct/union
	#pragma warning( disable : 4251 ) // class needs to have dll-interface to be used by clients of class
	#pragma warning( disable : 4324 ) // structure was padded due to __declspec(align())
	#pragma warning( disable : 4505 ) // local function has been removed
	#pragma warning( disable : 4512 ) // assignment operator could not be generated
	#pragma warning( disable : 4786 ) // identifier was truncated to '255' characters in the debug information
#endif

#ifdef PX_WII
#pragma warn_unusedarg off
#pragma warn_hidevirtual off
#pragma warn_implicitconv off
#endif

///*! restrict macro */
//#if defined(PX_PS3) || defined(PX_VC)
//#	define PX_RESTRICT __restrict
//#elif defined(PX_CW) && __STDC_VERSION__ >= 199901L
//#	define PX_RESTRICT restrict
//#else
//#	define PX_RESTRICT
//#endif
#if defined(PX_PS3) // this is to work around the GCC compiler warning about ignored restrict on return pointers
#define PX_RESTRICT_RETVAL
#else
#define PX_RESTRICT_RETVAL PX_RESTRICT
#endif

// An expression that should expand to nothing in non _DEBUG builds.  
// We currently use this only for tagging the purpose of containers for memory use tracking.
#if defined(_DEBUG)
#define PX_DEBUG_EXP(x) (x)
#define PX_DEBUG_EXP_C(x) x,
#else
#define PX_DEBUG_EXP(x)
#define PX_DEBUG_EXP_C(x)
#endif

#define PX_SIGN_BITMASK		0x80000000

// Macro for avoiding default assignment and copy 
// because NoCopy inheritance can increase class size on some platforms.
#define PX_NOCOPY(Class) \
	Class(const Class &); \
	Class &operator=(const Class &);

namespace physx
{
	namespace shdfnd 
	{
		// Int-as-bool type - has some uses for efficiency and with SIMD
		typedef int IntBool;
		static const IntBool IntFalse = 0;
		static const IntBool IntTrue = 1;

		template<class T, class Alloc> class Array;

		class ProfilerManager;
		class Sync;
		class RenderOutput;
		class RenderBuffer;
	}

} // namespace physx


#endif
