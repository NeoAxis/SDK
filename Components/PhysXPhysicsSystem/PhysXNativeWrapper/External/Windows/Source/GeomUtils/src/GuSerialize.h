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


#ifndef PX_SERIALIZE
#define PX_SERIALIZE

#include "PxSimpleTypes.h"
#include "PsFPU.h"
#include "CmPhysXCommon.h"
#include "CmLegacyStream.h"
#include "PxIO.h"

namespace physx
{

PX_PHYSX_COMMON_API void 		saveChunk(PxI8 a, PxI8 b, PxI8 c, PxI8 d, PxOutputStream& stream);
PX_PHYSX_COMMON_API void 		readChunk(PxI8& a, PxI8& b, PxI8& c, PxI8& d, PxInputStream& stream);

PX_PHYSX_COMMON_API PxU16 		readWord(bool mismatch, PxInputStream& stream);
PX_PHYSX_COMMON_API PxU32 		readDword(bool mismatch, PxInputStream& stream);
PX_PHYSX_COMMON_API PxF32 		readFloat(bool mismatch, PxInputStream& stream);

PX_PHYSX_COMMON_API void 		writeWord(PxU16 value, bool mismatch, PxOutputStream& stream);
PX_PHYSX_COMMON_API void 		writeDword(PxU32 value, bool mismatch, PxOutputStream& stream);
PX_PHYSX_COMMON_API void 		writeFloat(PxF32 value, bool mismatch, PxOutputStream& stream);

PX_PHYSX_COMMON_API bool 		readFloatBuffer(PxF32* dest, PxU32 nbFloats, bool mismatch, PxInputStream& stream);
PX_PHYSX_COMMON_API void 		writeWordBuffer(const PxU16* src, PxU32 nb, bool mismatch, PxOutputStream& stream);
PX_PHYSX_COMMON_API void 		writeFloatBuffer(const PxF32* src, PxU32 nb, bool mismatch, PxOutputStream& stream);

PX_PHYSX_COMMON_API bool 		writeHeader(PxI8 a, PxI8 b, PxI8 c, PxI8 d, PxU32 version, bool mismatch, PxOutputStream& stream);
PX_PHYSX_COMMON_API bool 		readHeader(PxI8 a, PxI8 b, PxI8 c, PxI8 d, PxU32& version, bool& mismatch, PxInputStream& stream);

PX_INLINE	PxU16 flip(const PxU16* v)
	{
	const PxU8* b = (const PxU8*)v;
	PxU16 f;
	PxU8* bf = (PxU8*)&f;
	bf[0] = b[1];
	bf[1] = b[0];
	return f;
	}

PX_INLINE   PxU32 flip(const PxU32* v)
	{
	const PxU8* b = (const PxU8*)v;
	PxU32 f;
	PxU8* bf = (PxU8*)&f;
	bf[0] = b[3];
	bf[1] = b[2];
	bf[2] = b[1];
	bf[3] = b[0];
	return f;
	}

#if !defined(PX_WII) 
// This one is to flip floats for reading
PX_INLINE	PxF32 flip(const PxF32* v)
	{
	PxU32 d = flip((const PxU32*)v);
	return PX_FR(d);
	}
#endif 

// This one is to flip floats for writing
PX_INLINE	void flipForWriting(PxF32& v)
	{
	PxU32 d = flip((const PxU32*)&v);

	// MS: It is important to modify the value directly and not use a temporary variable or a return
	//     value. The reason for this is that a flipped float might have a bit pattern which indicates
	//     an invalid float. If such a float is assigned to another float, the bit pattern
	//     can change again (hell knows why; maybe to map invalid floats to a common invalid pattern?).
	//     When reading the float and flipping again, the changed bit pattern will result in a different
	//     float than the original one.
	(PxU32&)v = d;
	}

PX_INLINE	bool	readIntBuffer(PxU32* dest, PxU32 nbInts, bool mismatch, PxInputStream& stream)
	{
	return readFloatBuffer((PxF32*)dest, nbInts, mismatch, stream);
	}

PX_INLINE	void	writeIntBuffer(const PxU32* src, PxU32 nb, bool mismatch, PxOutputStream& stream)
	{
	writeFloatBuffer((const PxF32*)src, nb, mismatch, stream);
	}

PX_PHYSX_COMMON_API PxU32 	computeMaxIndex(const PxU32* indices, PxU32 nbIndices);
PX_PHYSX_COMMON_API PxU16 	computeMaxIndex(const PxU16* indices, PxU32 nbIndices);
PX_PHYSX_COMMON_API void 	storeIndices(PxU32 maxIndex, PxU32 nbIndices, const PxU32* indices, PxOutputStream& stream, bool platformMismatch);
PX_PHYSX_COMMON_API void 	readIndices(PxU32 maxIndex, PxU32 nbIndices, PxU32* indices, PxInputStream& stream, bool platformMismatch);

}

#endif
