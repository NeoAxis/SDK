// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXMaterial.h"
#include "PhysXScene.h"
#include "PhysXJoint.h"
#include "PhysXShape.h"
#include "PhysXBody.h"
#include "PhysXVehicle.h"
#ifdef PLATFORM_WINDOWS
	#include <windows.h>
#endif
#ifdef PLATFORM_MACOS
	#include <errno.h>
	#include <iconv.h>	
	#import <Carbon/Carbon.h>
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifdef PLATFORM_WINDOWS
	#ifdef PX_CHECKED
		#if _MSC_VER >= 1400 && defined(_M_X64)
			#pragma comment (lib, "PhysX3CHECKED_x64.lib")
			#pragma comment (lib, "PhysX3CookingCHECKED_x64.lib")
			#pragma comment (lib, "PhysX3CommonCHECKED_x64.lib")
			#pragma comment (lib, "PhysX3ExtensionsCHECKED.lib")
			#pragma comment (lib, "PhysXProfileSDKCHECKED.lib")
			#pragma comment (lib, "PhysX3VehicleCHECKED.lib")
		#else
			#pragma comment (lib, "PhysX3CHECKED_x86.lib")
			#pragma comment (lib, "PhysX3CookingCHECKED_x86.lib")
			#pragma comment (lib, "PhysX3CommonCHECKED_x86.lib")
			#pragma comment (lib, "PhysX3ExtensionsCHECKED.lib")
			#pragma comment (lib, "PhysXProfileSDKCHECKED.lib")
			#pragma comment (lib, "PhysX3VehicleCHECKED.lib")
		#endif
	#else
		#if _MSC_VER >= 1400 && defined(_M_X64)
			#pragma comment (lib, "PhysX3_x64.lib")
			#pragma comment (lib, "PhysX3Cooking_x64.lib")
			#pragma comment (lib, "PhysX3Common_x64.lib")
			#pragma comment (lib, "PhysX3Extensions.lib")
			#pragma comment (lib, "PhysXProfileSDK.lib")
			#pragma comment (lib, "PhysX3Vehicle.lib")
		#else
			#pragma comment (lib, "PhysX3_x86.lib")
			#pragma comment (lib, "PhysX3Cooking_x86.lib")
			#pragma comment (lib, "PhysX3Common_x86.lib")
			#pragma comment (lib, "PhysX3Extensions.lib")
			#pragma comment (lib, "PhysXProfileSDK.lib")
			#pragma comment (lib, "PhysX3Vehicle.lib")
		#endif
	#endif
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

LogDelegate* logDelegate = NULL;
PhysXWorld* world = NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifdef PLATFORM_MACOS

template <class In, class Out>
void ConvertString(iconv_t cd, const In& in, Out* out, const typename Out::value_type errorSign)
{
	typedef typename In::value_type InType;
	typedef typename Out::value_type OutType;

	char* inPointer = (char*)in.data();
	size_t inLength = in.length() * sizeof(InType);

	const size_t bufferSize = 4096;
	OutType buffer[bufferSize];

	out->clear();

	while(inLength != 0)
	{
		char* tempPointer = (char*)buffer;
		size_t tempLength = bufferSize * sizeof(OutType);

		size_t result = iconv(cd, &inPointer, &inLength, &tempPointer, &tempLength);
		size_t n = (OutType*)(tempPointer) - buffer;

		out->append(buffer, n);

		if(result == (size_t)-1)
		{
			if(errno == EINVAL || errno == EILSEQ)
			{
				out->append(1, errorSign);
				inPointer += sizeof(InType);
				inLength -= sizeof(InType);
			}
			else if(errno == E2BIG && n == 0)
			{
				//Fatal("iconv: The buffer is too small.");
			}
		}
	}
}

#endif

WString ToUTFWide(const String& str)
{
	WString result;
	if(!str.empty())
	{

#ifdef PLATFORM_WINDOWS
		int size = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, NULL, 0);
		if(size)
		{
			wchar_t* wString = (wchar_t*)_alloca(size * sizeof(wchar_t));
			//wchar_t* wString = new wchar_t[size];
			if(MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, wString, size) != 0)
				result = wString;
			//delete[] wString;
		}
#else
		static iconv_t cd = (iconv_t)-1;
		if(cd == (iconv_t)-1)
		{
			cd = iconv_open("UTF-32LE", "UTF-8");
			//if (cd == (iconv_t)(-1))
			//	Fatal("iconv_open failed.");
		}
		ConvertString(cd, str, &result, '?');
#endif

	}
	return result;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

wchar16* CreateOutString(const WString& str)
{
#ifdef _WIN32
	wchar16* result = new wchar_t[str.length() + 1];
	wcscpy(result, str.c_str());
	return result;
#else
	int len = str.length();
	wchar16* result = new wchar16[len + 1];
	for(int n = 0; n < len; n++)
		result[n] = (wchar16)str[n];
	result[len] = 0;
	return result;
#endif
}

wchar16* CreateOutString(const String& str)
{
	return CreateOutString(ToUTFWide(str));
}

EXPORT void PhysXNativeWrapper_FreeOutString(wchar16* pointer)
{
	delete[] pointer;
}

void LogMessage(const char* text)
{
	logDelegate(CreateOutString(text));
}

void Fatal(const char* text)
{
#ifdef PLATFORM_MACOS
	CFStringRef textRef = CFStringCreateWithCString(NULL, text, kCFStringEncodingUTF8);
	CFUserNotificationDisplayAlert(0, kCFUserNotificationStopAlertLevel, NULL, NULL, NULL, 
		CFSTR("Fatal"), textRef, CFSTR("OK"), NULL, NULL, NULL);
	CFRelease(textRef);
#else
	MessageBoxA(NULL, text, "Fatal", MB_OK | MB_ICONEXCLAMATION);
#endif
	exit(0);
}
