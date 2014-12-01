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
// Copyright (c) 2004-2008 AGEIA Technologies, Inc. All rights reserved.
// Copyright (c) 2001-2004 NovodeX AG. All rights reserved.
  
#include "PxVisualDebuggerExt.h"

#if PX_SUPPORT_VISUAL_DEBUGGER
#include "PvdNetworkStreams.h"
#include "PvdConnectionManager.h"
#include "PsFoundation.h"
using namespace physx::shdfnd;
using namespace physx;
using namespace physx::debugger;


PVD::PvdConnection* PxVisualDebuggerExt::createConnection( physx::debugger::comm::PvdConnectionManager* inMgr, const char* inHost
										, int inPort
										, unsigned int inTimeoutInMilliseconds
										, PxVisualDebuggerConnectionFlags inConnectionType )
{
	TConnectionFlagsType theFlags( (PxU32)inConnectionType );
	inMgr->connect( Foundation::getInstance().getAllocator(), inHost, inPort, inTimeoutInMilliseconds, theFlags );
	return inMgr->getAndAddRefCurrentConnection();
}

PVD::PvdConnection* PxVisualDebuggerExt::createConnection( physx::debugger::comm::PvdConnectionManager* inMgr
												, const char* filename
												, PxVisualDebuggerConnectionFlags inConnectionType )
{
	TConnectionFlagsType theFlags( (PxU32)inConnectionType );
	inMgr->connect( Foundation::getInstance().getAllocator(), filename, theFlags );
	return inMgr->getAndAddRefCurrentConnection();
}

#else
namespace physx { namespace debugger { namespace comm {
class PvdConnectionManager;
class PvdConnection;
}}}

PVD::PvdConnection* physx::PxVisualDebuggerExt::createConnection( physx::debugger::comm::PvdConnectionManager* , const char* 
										, int 
										, unsigned int 
										, physx::PxVisualDebuggerConnectionFlags  ) { return NULL; }

PVD::PvdConnection* physx::PxVisualDebuggerExt::createConnection( physx::debugger::comm::PvdConnectionManager*
												, const char*
												, physx::PxVisualDebuggerConnectionFlags ) { return NULL; }

#endif //PX_SUPPORT_VISUAL_DEBUGGER
