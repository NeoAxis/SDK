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


#ifndef PX_FOUNDATION_PS_LINUX_STRING_H
#define PX_FOUNDATION_PS_LINUX_STRING_H

#include <stdio.h>
#include <string.h>
#include <stdarg.h>
#include "Ps.h"

#pragma warning(push)
#pragma warning(disable: 4995 4996)

namespace physx
{
	namespace string
	{

		PX_INLINE void strcpy(char* dest, size_t size, const char* src) {::strcpy(dest, src);}
		PX_INLINE void strcat(char* dest, size_t size, const char* src) {::strcat(dest, src);}
		//PX_INLINE int strcasecmp(const char *str, const char *str1) {return(::strcasecmp(str, str1));}
		PX_INLINE PxI32 stricmp(const char *str, const char *str1) {return(::strcasecmp(str, str1));}
		PX_INLINE PxI32 strnicmp(const char *str, const char *str1, size_t len) {return(::strncasecmp(str, str1, len));}

		PX_INLINE PxI32 strncat_s(char *dstBfr, size_t dstSize, const char *srcBfr, size_t numCpy) 
		{
			if(!dstBfr || !srcBfr || !dstSize)
				return -1;
			
			size_t len = strlen(dstBfr);

			if(len >= dstSize)
				return -1;

			size_t remain = dstSize - len - 1;
			size_t transfer = remain > numCpy ? numCpy : remain;
			::memmove(dstBfr+len, srcBfr, transfer);
			dstBfr[len+transfer]='\0';
			return numCpy <= remain ? 0 : -1;
		}

		PX_INLINE PxI32 _vsnprintf(char* dest, size_t size, const char* src, va_list arg) 
		{
			PxI32 r = ::vsnprintf(dest, size, src, arg);

			return r;
		}
		PX_INLINE PxI32 vsprintf(char* dest, size_t size, const char* src, va_list arg)
		{
			PxI32 r = ::vsprintf(dest, src, arg);

			return r;
		}
		PX_INLINE int vsprintf_s(char* dest, size_t size, const char* src, va_list arg) 
		{
			int r = ::vsprintf( dest, src, arg );

			return r;
		}

		PX_INLINE int sprintf_s( char * _DstBuf, size_t _DstSize, const char * _Format, ...)
		{
			if ( _DstBuf == NULL || _Format == NULL )
			{
				return -1;
			}

			va_list arg;
			va_start( arg, _Format );
			int r = ::vsprintf( _DstBuf, _Format, arg );
			va_end(arg);

			return r;
		}

		PX_INLINE PxI32 sprintf( char * _DstBuf, size_t _DstSize, const char * _Format, ...)
		{
			va_list arg;
			va_start( arg, _Format );
			PxI32 r = ::vsprintf(_DstBuf, _Format, arg);
			va_end(arg);

			return r;
		}

		PX_INLINE int strncpy_s( char *strDest,size_t sizeInBytes,const char *strSource,size_t count)
		{
			if (	strDest		== NULL ||
					strSource	== NULL ||
					sizeInBytes == 0	)
			{
				return -1;
			}

			if ( sizeInBytes < count )
			{
				strDest[0] = 0;
				return -1;
			}

			::strncpy( strDest, strSource, count );
			return 0;
		}

		PX_INLINE void strcpy_s(char* dest, size_t size, const char* src)
		{
			::strncpy(dest, src, size);
		}

		PX_INLINE int strcat_s(char* dest, size_t size, const char* src)
		{
			::strcat(dest, src);
			return 0;
		}

		PX_INLINE PxI32 sscanf( const char *buffer, const char *format,  ...)
		{
			va_list arg;
			va_start( arg, format );
			PxI32 r = ::sscanf(buffer, format, arg);
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

