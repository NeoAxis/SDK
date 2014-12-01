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
#ifndef PX_PXFILEBUFTOPXOUTPUTSTREAM_H
#define PX_PXFILEBUFTOPXOUTPUTSTREAM_H
#include "PsFile.h"
#include "PxFileBuffer.h"


namespace physx { 

	class PxFileBufPxOutputStream : public PxOutputStream
	{
		PxFileBuffer& mBuffer;

		PxFileBufPxOutputStream( const PxOutputStream& inOther );
		PxFileBufPxOutputStream& operator=( const PxOutputStream& inOther );
	public:
		PxFileBufPxOutputStream( PxFileBuffer& inBuffer ) : mBuffer( inBuffer ) { }

		template<typename TDataType>
		inline PxOutputStream& store( TDataType inValue )
		{
			write( &inValue, sizeof( inValue ) );
			return *this;
		}

		virtual PxU32 write(const void* src, PxU32 count)
		{
			PxU32 theSize( mBuffer.write( src, count ) );
			PX_ASSERT( theSize == count );
			return theSize;
		}
		//Do an operation with the file stream, returning the result
		//of the operation.
		template<typename TReturnValue, typename TOperator>
		static PX_INLINE TReturnValue WithFileStream( const char* inFilename, PxFileBuf::OpenMode inMode, TOperator inOperator )
		{
			PxFileBuffer theFileBuf( inFilename, inMode );
			PxFileBufPxOutputStream theStream( theFileBuf );
			return inOperator( theStream );
		}
	};
}

#endif 