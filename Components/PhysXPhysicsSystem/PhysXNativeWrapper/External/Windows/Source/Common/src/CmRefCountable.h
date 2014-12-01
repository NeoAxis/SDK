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


#ifndef PX_PHYSICS_COMMON_REFCOUNTABLE
#define PX_PHYSICS_COMMON_REFCOUNTABLE

#include "CmPhysXCommon.h"
#include "PsAtomic.h"
#include "PxAssert.h"
#include "CmMetaData.h"

namespace physx
{
namespace Cm
{

	// simple thread-safe reference count
	// when the ref count is zero, the object is in an undefined state (pending delete)

	class RefCountable
	{
	public:
// PX_SERIALIZATION
		RefCountable(PxRefResolver&) : mRefCount(1)	{}
		static	void	getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION
		explicit RefCountable(PxU32 initialCount = 1)
			: mRefCount(initialCount)
		{
			PX_ASSERT(mRefCount!=0);
		}

		virtual ~RefCountable() {}

		/**
		Calls 'delete this;'. It needs to be overloaded for classes also deriving from 
		PxSerializable and call 'Cm::deleteSerializedObject(this);' instead.
		*/
		virtual	void onRefCountZero()
		{
			delete this;
		}

		void incRefCount()
		{
			physx::shdfnd::atomicIncrement(&mRefCount);
			// value better be greater than 1, or we've created a ref to an undefined object
			PX_ASSERT(mRefCount>1);
		}

		void decRefCount()
		{
			if(physx::shdfnd::atomicDecrement(&mRefCount) == 0)
				onRefCountZero();
		}

		PX_FORCE_INLINE PxU32 getRefCount() const
		{
			return mRefCount;
		}
	private:
		volatile PxI32 mRefCount;
	};


} // namespace Cm

}

#endif
