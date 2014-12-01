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

//#ifdef REMOVED


#ifndef PX_SERIAL_ALIGNMENT_H
#define PX_SERIAL_ALIGNMENT_H

#include "Px.h"
#include "PsIntrinsics.h"
#include "CmLegacyStream.h"

// PX_SERIALIZATION

namespace physx
{

#define PX_SERIAL_ALIGNMENT
#define PX_SERIAL_DEFAULT_ALIGN	16
//#define PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA	16	// PT: for DE1340
#define PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA		0	// PT: for DE1340
#define PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP	16	// PT: for DE1340
namespace Cm
{

#ifdef PX_SERIAL_ALIGNMENT
	PX_INLINE PxU32 getPadding(size_t value, PxU32 alignment)
	{
		const PxU32 mask = alignment-1;
		const PxU32 overhead = PxU32(value) & mask;
		return (alignment - overhead) & mask;
	}
#endif

	PX_INLINE void alignStream(PxSerialStream& stream, PxU32 alignment=PX_SERIAL_DEFAULT_ALIGN)
	{
#ifdef PX_SERIAL_ALIGNMENT
		if(!alignment)
			return;
		PxI32 bytesToPad = getPadding(stream.getTotalStoredSize(), alignment);
		enum { BUFSIZE = 64 };
		char buf[BUFSIZE];
		Ps::memSet(buf, 0, bytesToPad < BUFSIZE ? bytesToPad : BUFSIZE);
		while(bytesToPad > 0)
		{
			stream.storeBuffer(buf, bytesToPad < BUFSIZE ? bytesToPad : BUFSIZE);
			bytesToPad -= BUFSIZE;
		}
		PX_ASSERT(!getPadding(stream.getTotalStoredSize(), alignment));
#else
		(void)stream;
		(void)alignment;
#endif
	}

	PX_INLINE char* alignStream(char* buffer, PxU32& total, PxU32 alignment=PX_SERIAL_DEFAULT_ALIGN)
	{
#ifdef PX_SERIAL_ALIGNMENT
		if(!alignment)
			return buffer;
		const PxU32 padding = getPadding(size_t(buffer), alignment);
		PX_ASSERT(!getPadding(size_t(buffer + padding), alignment));
		total += padding;
		return buffer + padding;
#else
		(void)total;
		(void)alignment;
		return buffer;
#endif
	}

} // namespace Cm

}

//~PX_SERIALIZATION
#endif
//#endif