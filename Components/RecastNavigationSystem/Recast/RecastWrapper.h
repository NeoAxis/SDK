// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#ifdef _WIN32
	#define EXPORT extern "C" __declspec(dllexport)
#else
	#define EXPORT extern "C" __attribute__ ((visibility("default")))
#endif

typedef unsigned int uint;
typedef unsigned char uint8;
typedef unsigned short uint16;
#define SAFE_DELETE(q){if(q){delete q;q=NULL;}else 0;}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

extern void Fatal(const char* text);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
