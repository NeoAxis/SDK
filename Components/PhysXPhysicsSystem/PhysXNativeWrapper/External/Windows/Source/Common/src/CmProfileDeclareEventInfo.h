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
#define CM_PROFILE_DECLARE_EVENT_INFO_H


/**
 *	This header expects the macro:
 *	PX_PROFILE_EVENT_DEFINITION_HEADER to be defined to a string which is included 
 *	in order to produce the enum and event id lists.
 * 
 *	This header needs to be of the form:
 *	
PX_PROFILE_BEGIN_SUBSYSTEM( Subsystem1 )
PX_PROFILE_EVENT( Subsystem1,Event1, Coarse )
PX_PROFILE_EVENT( Subsystem1,Event2, Detail )
PX_PROFILE_EVENT( Subsystem1,Event3, Medium )
PX_PROFILE_END_SUBSYSTEM( Subsystem1 )
 */
#include "physxprofilesdk/PxProfileCompileTimeEventFilter.h"
#include "physxprofilesdk/PxProfileEventNames.h"


namespace physx { namespace profile {

//Event id enumeration
#define PX_PROFILE_BEGIN_SUBSYSTEM( subsys ) 
#define PX_PROFILE_EVENT( subsystem, name, priority ) subsystem##name,
#define PX_PROFILE_END_SUBSYSTEM( subsys )
struct EventIds
{
	enum Enum
	{
#include PX_PROFILE_EVENT_DEFINITION_HEADER
	};
};
#undef PX_PROFILE_BEGIN_SUBSYSTEM
#undef PX_PROFILE_EVENT
#undef PX_PROFILE_END_SUBSYSTEM


//Event priority definition
#define PX_PROFILE_BEGIN_SUBSYSTEM( subsys )
#define PX_PROFILE_EVENT( subsys, name, priority ) \
	template<> struct EventPriority<EventIds::subsys##name> { static const PxU32 val = EventPriorities::priority; };
#define PX_PROFILE_END_SUBSYSTEM( subsys )
#include PX_PROFILE_EVENT_DEFINITION_HEADER
#undef PX_PROFILE_BEGIN_SUBSYSTEM
#undef PX_PROFILE_EVENT
#undef PX_PROFILE_END_SUBSYSTEM

} }


#undef CM_PROFILE_DECLARE_EVENT_INFO_H
