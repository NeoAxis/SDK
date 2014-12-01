// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "MyNativeDLL.h"

EXPORT int Test( int parameter )
{
	if(sizeof(void*) == 8)
		return 64;
	else
		return 32;
}
