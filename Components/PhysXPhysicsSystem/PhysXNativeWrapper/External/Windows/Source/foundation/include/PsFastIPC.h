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


#ifndef PS_FAST_IPC_H
#define PS_FAST_IPC_H

#include "PxSimpleTypes.h"
#include "PsIPC.h"

namespace physx
{
	namespace shdfnd
	{

PxIPC* createFastIPC(PxIPC::ErrorCode &errorCode,		// error code return if creation fails
					  PxIPC::ConnectionType connectionType=PxIPC::CT_CLIENT_OR_SERVER, // how to establish the connection
						const char* mappingObject="Global\\FastIPC",		// Name of communications channel
						PxU32 serverRingBufferSize=1024*4,				// buffer size for outgoing server messages.
						PxU32 clientRingBufferSize=1024*4, 			// buffer size for incoming client messages.
						bool allowLongMessages=true,
						bool	bufferSends=false,
						PxU32 maxBufferSendSize=1024*8);			// True if we want to buffer sends, if true, then pumpSendBuffers should be called by the send thread.

void	  releaseFastIPC(PxIPC* f);

}; // end of namespace
}; // end of namespace

#endif
