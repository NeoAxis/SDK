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


#ifndef PX_PHYSICS_COMMON_PTR_TABLE
#define PX_PHYSICS_COMMON_PTR_TABLE

#include "PxPhysXCommon.h"  // for PX_PHYSX_COMMON_API
#include "CmPhysXCommon.h"

namespace physx
{

class PxRefResolver;
class PxSerialStream;

namespace Cm
{
	// PT: specialized class to hold an array of pointers. Similar to an inline array of 1 pointer, but using 8 bytes instead of 20.
	// PT: please don't templatize this one.
	struct PX_PHYSX_COMMON_API PtrTable
	{
// PX_SERIALIZATION
		PtrTable(PxRefResolver& v) : mOwnsMemory(false)	{}
//		static	void	getMetaData(PxSerialStream& stream);
		void	exportExtraData(PxSerialStream& stream);
		char*	importExtraData(char* address, PxU32& totalPadding);
//~PX_SERIALIZATION
		PX_INLINE PtrTable() :
			mSingle		(NULL),
			mCount		(0),
			mOwnsMemory	(true),
			mBufferUsed	(false)	{}
		PX_INLINE ~PtrTable()	{ clear();}

		void	clear();
		void	setPtrs(void** ptrs, PxU32 count);
		void	addPtr(void* ptr);
		bool	findAndDeletePtr(void* ptr);

		PX_FORCE_INLINE	void*const*	getPtrs()	const	{ return mCount == 1 ? &mSingle : mList;	}

		union
		{
			void*	mSingle;
			void**	mList;
		};

		PxU16	mCount;
		bool	mOwnsMemory;
		bool	mBufferUsed;
	};
} // namespace Cm
#ifndef PX_X64
PX_COMPILE_TIME_ASSERT(sizeof(Cm::PtrTable)==8);
#endif

}

#endif
