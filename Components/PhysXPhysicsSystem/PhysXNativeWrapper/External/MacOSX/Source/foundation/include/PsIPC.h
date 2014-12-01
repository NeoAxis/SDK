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


#ifndef PS_IPC_H
#define PS_IPC_H

#include "Ps.h"

namespace physx
{
	namespace shdfnd
	{

	class PxIPC
	{
		public:
			

    	enum ConnectionType
    	{
    		CT_CLIENT, 					// start up as a client, will succeed even if the server has not yet been found.
    		CT_CLIENT_REQUIRE_SERVER,   // start up as a client, but only if the server already exists.
    		CT_SERVER, 					// will start up as a server, will fail if an existing server is already open.
    		CT_CLIENT_OR_SERVER,  		// connect as either a client or server, don't care who is created first.
    		CT_LAST
    	};

    	enum ErrorCode
    	{
    		EC_OK,   					// no error.
    		EC_FAIL,					// generic failure.
    		EC_SERVER_ALREADY_EXISTS, 	// couldn't create a server, because the server already exists.
    		EC_CLIENT_ALREADY_EXISTS, 	// couldn't create a client, because an existing client is already registered.
    		EC_CLIENT_SERVER_ALREADY_EXISTS, // both the client and server channels are already used
    		EC_SERVER_NOT_FOUND,		// client opened with a required server, which was not found.
    		EC_BUFFER_MISSMATCH,      	// the reserved buffers for client/server do not match up.
    		EC_MAPFILE_CREATE,          // failed to create the shared memory map file.
    		EC_MAPFILE_VIEW,			// failed to map the memory view of he
    		// communications errors.
    		EC_SEND_DATA_EXCEEDS_MAX_BUFFER, // trying to send more data than can even fit in the sednd buffe.
    		EC_SEND_DATA_TOO_LARGE,		// the data we tried to send exceeds the available room int the output ring buffer.
    		EC_SEND_BUFFER_FULL,        // the send buffer is completely full.
    		EC_SEND_FROM_WRONG_THREAD,  // Tried to do a send from a different thread
    		EC_RECEIVE_FROM_WRONG_THREAD, // Tried to do a recieve from a different thread
    		EC_NO_RECEIVE_PENDING,		// tried to acknowledge a receive but none was pending.
    	};



    	virtual	bool			pumpPendingSends(void) = 0; // give up a time slice to pending sends; returns true if there are still pends sending.
    	virtual ErrorCode		sendData(const void *data,PxU32 data_len,bool bufferIfFull) = 0;
    	virtual const void * 	receiveData(PxU32 &data_len) = 0;
    	virtual	ErrorCode			receiveAcknowledge(void) = 0; // acknowledge that we have processed the incoming message and can advance the read buffer.


    	virtual bool isServer(void) const = 0; // returns true if we are opened as a server.

    	virtual bool haveConnection(void) const = 0;

		virtual bool canSend(PxU32 len) = 0; // return true if we can send a message of this size.

		virtual void release(void) = 0;

	protected:
		virtual ~PxIPC(void) { };

	};
	}; // end of namespace
}; // end of namespace

#endif
