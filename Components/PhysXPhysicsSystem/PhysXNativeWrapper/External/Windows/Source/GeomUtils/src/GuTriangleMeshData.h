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


#ifndef PX_TRIANGLEMESH_H
#define PX_TRIANGLEMESH_H

#include "PxVec3.h"
#include "GuCollisionModel.h"
#include "../Opcode/OPC_HybridModel.h"

namespace physx
{
namespace Gu
{

struct EdgeListData;

//Data

PX_ALIGN_PREFIX(16)
struct InternalTriangleMeshData
{
// PX_SERIALIZATION
						InternalTriangleMeshData()											{}
						InternalTriangleMeshData(PxRefResolver& v)	: mOpcodeModel(v)		{}
//~PX_SERIALIZATION

	// 16 bytes block
						PxU32				mNumVertices;
						PxU32				mNumTriangles;
						PxVec3*				mVertices;
						void*				mTriangles;				//!< 16 (<= 0xffff #vertices) or 32 bit trig indices (mNumTriangles * 3)
	// 16 bytes block
						Ice::RTreeMidphase	mOpcodeModel;

	// 16 bytes block
						PxBounds3			mAABB;
						PxU8*				mExtraTrigData;			//one per trig
	/*
	low 3 bits (mask: 7) are the edge flags:
	b001 = 1 = ignore edge 0 = edge v0-->v1
	b010 = 2 = ignore edge 1 = edge v0-->v2
	b100 = 4 = ignore edge 2 = edge v1-->v2
	*/
						bool				m16BitIndices;			//!< Whether indices are 16 or 32 bits wide.  In cooking we are always using 32 bits, otherwise we could tell from the number of vertices.
																	//AM: TODO: make into a flags field when we have more flags to worry about.
	EXPLICIT_PADDING(	bool				mPaddingFromBool[3]);

} PX_ALIGN_SUFFIX(16);
PX_COMPILE_TIME_ASSERT((sizeof(Gu::InternalTriangleMeshData)&15) == 0);
PX_COMPILE_TIME_ASSERT((PX_OFFSET_OF(Gu::InternalTriangleMeshData, mOpcodeModel)&15) == 0);

} // namespace Gu

}

#endif
