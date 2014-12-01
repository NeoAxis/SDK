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


#ifndef PX_FOUNDATION_PS_WINDOWS_STRING_H
#define PX_FOUNDATION_PS_WINDOWS_STRING_H

#include "Ps.h"

#include <stdio.h>
#include <string.h>
#include <stdarg.h>

#pragma warning(push)
#pragma warning(disable: 4995 4996)

namespace physx
{
	namespace string
	{
		PX_INLINE PxI32 stricmp(const char *str, const char *str1) {return(::_stricmp(str, str1));}
		PX_INLINE PxI32 strnicmp(const char *str, const char *str1, size_t len) {return(::_strnicmp(str, str1, len));}
		PX_INLINE PxI32 strncat_s(char* a, PxI32 b, const char* c, size_t d) {return(::strncat_s(a, b, c, d));}
		PX_INLINE PxI32 strncpy_s( char *strDest, size_t sizeInBytes, const char *strSource, size_t count) {return(::strncpy_s( strDest,sizeInBytes,strSource, count));}
		PX_INLINE void strcpy_s(char* dest, size_t size, const char* src) {::strcpy_s(dest, size, src);}
		PX_INLINE void strcat_s(char* dest, size_t size, const char* src) {::strcat_s(dest, size, src);}
		PX_INLINE PxI32 _vsnprintf(char* dest, size_t size, const char* src, va_list arg) 
		{
			PxI32 r = ::_vsnprintf(dest, size, src, arg);

			return r;
		}
		PX_INLINE PxI32 vsprintf_s(char* dest, size_t size, const char* src, va_list arg)
		{
			PxI32 r = ::vsprintf_s(dest, size, src, arg);

			return r;
		}

		PX_INLINE PxI32 sprintf_s( char * _DstBuf, size_t _DstSize, const char * _Format, ...)
		{
			va_list arg;
			va_start( arg, _Format );
			PxI32 r = ::vsprintf_s(_DstBuf, _DstSize, _Format, arg);
			va_end(arg);

			return r;
		}
		PX_INLINE PxI32 sscanf_s( const char *buffer, const char *format,  ...)
		{
			va_list arg;
			va_start( arg, format );
			PxI32 r = ::sscanf_s(buffer, format, arg);
			va_end(arg);

			return r;
		};

		PX_INLINE void strlwr(char* str)
		{
			while ( *str )
			{
				if ( *str>='A' &&  *str<='Z' ) *str+=32;
				str++;
			}
		}

		PX_INLINE void strupr(char* str)
		{
			while ( *str )
			{
				if ( *str>='a' &&  *str<='z' ) *str-=32;
				str++;
			}
		}

	}
} // namespace physx

#pragma warning(pop)

#endif

