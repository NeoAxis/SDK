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

// no include guard on purpose!
#define CM_PROFILE_DEFINE_EVENT_INFO_H


/**
 *	This header expects the macro:
 *	PX_PROFILE_EVENT_DEFINITION_HEADER to be defined to a string which is included 
 *	multiple times.
 *
 *	This header needs to be of the form:
 *	
PX_PROFILE_BEGIN_SUBSYSTEM( Subsystem1 )
PX_PROFILE_EVENT( Subsystem1,Event1 )
PX_PROFILE_EVENT( Subsystem1,Event2 )
PX_PROFILE_EVENT( Subsystem1,Event3 )
PX_PROFILE_END_SUBSYSTEM( Subsystem1 )
 *
 *	Produces gPxProfileNames, a copyable object that names all the events and 
 *	subsystems.
 *
 *	
 */
#include "physxprofilesdk/PxProfileCompileTimeEventFilter.h"
#include "PsUtilities.h"

#define PX_PROFILE_BEGIN_SUBSYSTEM( subsys )
#define PX_PROFILE_EVENT( subsystem, name, priority ) PxProfileEventName( #subsystem "." #name, PxProfileEventId( EventIds::subsystem##name, PX_PROFILE_EVENT_FILTER_VALUE( subsystem, name ) ) ),
#define PX_PROFILE_END_SUBSYSTEM( subsys ) 
static PxProfileEventName gEventNames[] = {
#include PX_PROFILE_EVENT_DEFINITION_HEADER
};
#undef PX_PROFILE_BEGIN_SUBSYSTEM
#undef PX_PROFILE_EVENT
#undef PX_PROFILE_END_SUBSYSTEM

static PxU32 gEventNamesCount = PX_ARRAY_SIZE( gEventNames );
static PxProfileNames gPxProfileNames( gEventNamesCount, gEventNames );

#undef CM_PROFILE_DEFINE_EVENT_INFO_H
