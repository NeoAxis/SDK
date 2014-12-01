// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#ifdef PLATFORM_WINDOWS
	#define EXPORT extern "C" __declspec(dllexport)
#elif defined(PLATFORM_MACOS)
	#define EXPORT extern "C" __attribute__ ((visibility("default")))
#else
	#error Unknown platform
#endif
