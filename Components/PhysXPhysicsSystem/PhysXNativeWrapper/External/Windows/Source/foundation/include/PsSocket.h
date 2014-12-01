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


#ifndef PX_FOUNDATION_PSSOCKET_H
#define PX_FOUNDATION_PSSOCKET_H


#include "PsUserAllocated.h"


namespace physx
{
namespace shdfnd
{
	/**
	Socket abstraction API
	*/

	class PX_FOUNDATION_API Socket : public UserAllocated
	{
	public:
		class Connection
		{
		public:
			virtual bool notifySocketConnection(Socket *newConnection) = 0; // notify the caller that a new connection has been established.  
			virtual ~Connection() {}
		};

		static const PxU32 DEFAULT_BUFFER_SIZE;

		Socket(bool inEnableBuffering = true );


		virtual ~Socket();


		/*!
		Opens a network socket for input and/or output

		\param host
		Name of the host to connect to. This can be an IP, URL, etc

		\param port
		The port to connect to on the remote host

		\param timeout
		Timeout in ms until the connection must be established.

		\return
		True if the connection was successful, false otherwise
		*/
		bool connect(const char* host, PxU16 port, PxU32 timeout = 1000);

		/*!
		Opens a network socket for input and/or output as a server.  Listens for new connections and notifies the application via the callback.
		*/
		bool listen(PxU16 port,Connection *callback);

		/*!
		Disconnects an open socket
		*/
		void disconnect();

		
		/*!
		Returns whether the socket is currently open (connected) or not.

		\return
		True if the socket is connected, false otherwise
		*/
		bool isConnected() const;


		/*!
		Returns the name of the connected host. This is the same as the string 
		that was supplied to the connect call.

		\return
		The name of the connected host
		*/
		const char* getHost() const;


		/*!
		Returns the port of the connected host. This is the same as the port 
		that was supplied to the connect call.

		\return
		The port of the connected host
		*/
		PxU16 getPort() const;

		
		/*!
		Flushes the output stream. Until the stream is flushed, there is no
		guarantee that the written data has actually reached the destination
		storage. Flush forces all buffered data to be sent to the output.

		\return
		True if the flush was successful, false otherwise
		*/
		bool flush();
 

 		/*!
		Writes data to the output stream.

		\param data
		Pointer to a  block of data to write to the stream

		\param length
		Amount of data to write, in bytes

		\return
		Number of bytes actually written. This could be lower than length if the
		end of the stream was encountered, but will succeed in most situations.
		*/

 		PxU32 write(const PxU8* data, PxU32 length);


		/*!
		Reads data from the output stream.

		\param data
		Pointer to a buffer where the read data will be stored.

		\param length
		Amount of data to read, in bytes. 

		\param clientId
		A reference which will be assigned the id number of the client this message came from.

		\return
		Number of bytes actually read. This could be lower than length if the
		end of the stream was encountered, but will succeed in most situations.
		*/
 		PxU32 read(PxU8* data, PxU32 length);


		/*!
		Sets blocking mode of the socket.
		Socket must be connected, otherwise calling this method won't take any effect.
		*/
		void setBlocking(bool blocking);

		
		/*!
		Returns whether read/write/flush calls to the socket are blocking.

		\return
		True if the socket is blocking.
		*/
		bool isBlocking() const;


	private:
		class SocketImpl *mImpl;
	};

} // namespace shdfnd
} // namespace physx

#endif
