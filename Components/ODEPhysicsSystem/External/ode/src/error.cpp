/*************************************************************************
 *                                                                       *
 * Open Dynamics Engine, Copyright (C) 2001,2002 Russell L. Smith.       *
 * All rights reserved.  Email: russ@q12.org   Web: www.q12.org          *
 *                                                                       *
 * This library is free software; you can redistribute it and/or         *
 * modify it under the terms of EITHER:                                  *
 *   (1) The GNU Lesser General Public License as published by the Free  *
 *       Software Foundation; either version 2.1 of the License, or (at  *
 *       your option) any later version. The text of the GNU Lesser      *
 *       General Public License is included with this library in the     *
 *       file LICENSE.TXT.                                               *
 *   (2) The BSD-style license that is included with this library in     *
 *       the file LICENSE-BSD.TXT.                                       *
 *                                                                       *
 * This library is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the files    *
 * LICENSE.TXT and LICENSE-BSD.TXT for more details.                     *
 *                                                                       *
 *************************************************************************/

#include <ode/odeconfig.h>
#include <ode/error.h>

//betauser

#ifdef PLATFORM_WINDOWS
	#include <windows.h>
#endif

#ifdef PLATFORM_MACOS
	#undef malloc
	#undef calloc
	#undef realloc
	#undef free
	#include <Carbon/Carbon.h>
#endif

#ifdef PLATFORM_ANDROID
	#undef malloc
	#undef calloc
	#undef realloc
	#undef free
	#include <android/log.h>
#endif


///////////////////////////////////////////////////////////////////////////////////////////////////

void ShowMessageBox(const char* text, const char* title)
{
#ifdef PLATFORM_WINDOWS

	MessageBox(0, text, title, MB_OK | MB_ICONWARNING);

#elif defined(PLATFORM_MACOS)

	CFStringRef textRef = CFStringCreateWithCString(NULL, text, kCFStringEncodingASCII);
	CFStringRef titleRef = CFStringCreateWithCString(NULL, text, kCFStringEncodingASCII);
	CFUserNotificationDisplayAlert(0, kCFUserNotificationStopAlertLevel, NULL, NULL, NULL, 
		titleRef, textRef, CFSTR("OK"), NULL, NULL, NULL );
	CFRelease(textRef);
	CFRelease(titleRef);

#elif defined(PLATFORM_ANDROID)
//!!!!!!dr
	char tempBuffer[4096];
	sprintf(tempBuffer, "\n%s\n\n%s\n", title, text);
	__android_log_write(ANDROID_LOG_ERROR,"NeoAxis Engine: Ode", tempBuffer);

#else
	#error
#endif
}

extern "C" void dError (int num, const char *msg, ...)
{
	va_list ap;
	va_start (ap,msg);

	char s[1000],title[100];
#ifdef PLATFORM_WINDOWS
	_snprintf (title,sizeof(title),"ODE Error %d",num);
	_vsnprintf (s,sizeof(s),msg,ap);
#else
	snprintf (title,sizeof(title),"ODE Error %d",num);
	snprintf (s,sizeof(s),msg,ap);
#endif
	s[sizeof(s)-1] = 0;

	ShowMessageBox(s, title);

	exit(1);
}


extern "C" void dDebug (int num, const char *msg, ...)
{
	va_list ap;
	va_start (ap,msg);

	char s[1000],title[100];
#ifdef PLATFORM_WINDOWS
	_snprintf (title,sizeof(title),"ODE INTERNAL ERROR %d",num);
	_vsnprintf (s,sizeof(s),msg,ap);
#else
	snprintf (title,sizeof(title),"ODE INTERNAL ERROR %d",num);
	vsnprintf (s,sizeof(s),msg,ap);
#endif
	s[sizeof(s)-1] = 0;

	ShowMessageBox(s, title);

	abort();
}


extern "C" void dMessage (int num, const char *msg, ...)
{
	//va_list ap;
	//va_start (ap,msg);
	//if (message_function) message_function (num,msg,ap);
	//else printMessage (num,"ODE Message",msg,ap);
}
