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


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Include Guard
#ifndef BIG_CONVEX_DATA_H
#define BIG_CONVEX_DATA_H

#include "GuBigConvexData.h"
#include "CmMetaData.h"

namespace physx
{
	class PxInputStream;
	class PX_PHYSX_COMMON_API BigConvexData : public Ps::UserAllocated
	{
		public:
// PX_SERIALIZATION
											BigConvexData(PxRefResolver&)	{}
		static		void					getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION
											BigConvexData();
											~BigConvexData();
		// Support vertex map
					bool					Load(PxInputStream& stream);

					PxU32					ComputeOffset(const PxVec3& dir)		const;
					PxU32					ComputeNearestOffset(const PxVec3& dir)	const;

		// Data access
		PX_INLINE	PxU32					GetSubdiv()								const	{ return mData.mSubdiv;											}
		PX_INLINE	PxU32					GetNbSamples()							const	{ return mData.mNbSamples;										}
		//~Support vertex map

		// Valencies
		// Data access
		PX_INLINE	PxU32					GetNbVerts()							const	{ return mData.mNbVerts;										}
		PX_INLINE	const Gu::Valency*		GetValencies()							const	{ return mData.mValencies;										}
		PX_INLINE	PxU16					GetValency(PxU32 i)						const	{ return mData.mValencies[i].mCount;							}
		PX_INLINE	PxU16					GetOffset(PxU32 i)						const	{ return mData.mValencies[i].mOffset;							}
		PX_INLINE	const PxU8*				GetAdjacentVerts()						const	{ return mData.mAdjacentVerts;									}

		PX_INLINE	PxU16					GetNbNeighbors(PxU32 i)					const	{ return mData.mValencies[i].mCount;							}
		PX_INLINE	const PxU8*				GetNeighbors(PxU32 i)					const	{ return &mData.mAdjacentVerts[mData.mValencies[i].mOffset];	}

// PX_SERIALIZATION
					void					exportExtraData(PxSerialStream& stream);
					char*					importExtraData(char* address, PxU32& totalPadding);
//~PX_SERIALIZATION
					Gu::BigConvexRawData	mData;
		protected:
					void*					mVBuffer;
		// Internal methods
					void					CreateOffsets();
					bool					VLoad(PxInputStream& stream);
		//~Valencies
		friend class BigConvexDataBuilder;
	};

}

#endif // BIG_CONVEX_DATA_H

