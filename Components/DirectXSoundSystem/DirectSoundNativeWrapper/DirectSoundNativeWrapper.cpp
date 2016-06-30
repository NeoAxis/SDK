// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "DirectSoundNativeWrapper.h"

#pragma comment (lib, "dsound.lib")
#pragma comment (lib, "dxerr.lib")
#pragma comment (lib, "dxguid.lib")

#include <stdarg.h>
#include <stdio.h>
#include <wchar.h>
int (WINAPIV * __vsnwprintf)(wchar_t *, size_t, const wchar_t *, va_list) = _vsnwprintf;

WCHAR* CreateOutString(const WCHAR* str)
{
#ifndef _UNICODE
	#error need unicode
#endif

	WCHAR* result = new WCHAR[wcslen(str) + 1];
	wcscpy_s(result, wcslen(str) + 1, str);
	return result;
}

EXPORT void DSound_FreeOutString(WCHAR* pointer)
{
	delete[] pointer;
}
