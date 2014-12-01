// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#if (defined(_WIN32) || defined(WIN32))
	#define PLATFORM_WINDOWS
#elif defined(__APPLE_CC__)
	#define PLATFORM_MACOS
#else
	#error Platform is not supported.
#endif


#ifdef PLATFORM_WINDOWS
	#define _CRT_SECURE_NO_DEPRECATE 
	#ifndef _WIN32_DCOM
		#define _WIN32_DCOM
	#endif	
	#include <windows.h>
	#include <wbemidl.h>
	#include <strsafe.h>
#endif

#ifdef _DEBUG
	#error Debug version is not supported. You can switch to Release configuration or configure Debug project options.
#endif
