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
#ifdef PLATFORM_WINDOWS
	#include <malloc.h>
#endif
#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include <float.h>
#include <math.h>
#include <wchar.h>
#include <vector>
#include <set>
#include <map>
#include <cmath>
#include <algorithm>
#include <sstream>
#ifdef PLATFORM_MACOS
	#include <new>
#endif

#ifdef PLATFORM_MACOS
	#define isfinite std::isfinite
#endif

#include "MemoryManager.h"
#undef malloc
#undef calloc
#undef realloc
#undef free
#undef _aligned_malloc
#undef _aligned_free

#include <string>

#include "PxPhysicsAPI.h"
#include "PxVehicleUtil.h"

using namespace physx;

#ifdef _DEBUG
	#error Debug version is not supported.
#endif

#define _DefinedMemoryAllocationType MemoryAllocationType_Physics
#include "MemoryManager_SimpleNew.h"

#ifdef PLATFORM_WINDOWS
	typedef wchar_t wchar16;
#else
	typedef unsigned short wchar16;
#endif

typedef unsigned char byte;
typedef unsigned short ushort;

typedef std::string String;
typedef std::wstring WString;

class PhysXBody;
class PhysXScene;
class PhysXMaterial;
class PhysXWorld;
struct NativeRayCastResult;

#define PI 3.14159265358979323846f
