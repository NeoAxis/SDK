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


#ifndef PX_PHYSICS_COMMON_INVASIVESET
#define PX_PHYSICS_COMMON_INVASIVESET

#include "PxAssert.h"
#include "PsNoCopy.h"
#include "PsInlineArray.h"
#include "CmPhysXCommon.h"

#include "CmMetaData.h"

namespace physx
{
namespace Cm
{

	class InvasiveSetHook: private Ps::NoCopy
	{
	public:
		static	void	getMetaData(PxSerialStream& stream);
		static const PxU32 INDEX_NONE = 0xffffffff;
		PxU32 index;
		InvasiveSetHook(): index(INDEX_NONE) {}
		PX_INLINE bool isContained() const	{	return index!=INDEX_NONE; }
	};

	template <class Object, class HookAccessor>
	class InvasiveSet: private physx::shdfnd::NoCopy
	{
	public:
		InvasiveSet() : mArray(PX_DEBUG_EXP("invasiveSet"))
		{
		}

		PX_INLINE bool contains(Object* obj, HookAccessor h = HookAccessor()) const
		{
			const PxU32 index = h(obj).index;
			return index<mArray.size() && mArray[index] == obj; // INDEX_NONE > than any possible array size
		}

		PX_INLINE void insert(Object* obj, HookAccessor h = HookAccessor())
		{
			PX_ASSERT(!h(obj).isContained());
			mArray.pushBack(obj);
			h(obj).index = mArray.size()-1;
			PX_ASSERT(InvasiveSetHook::INDEX_NONE>mArray.size());
		}

		PX_INLINE void erase(Object* obj, HookAccessor h = HookAccessor())
		{
			PX_ASSERT(contains(obj,h));
			PxU32& ir = h(obj).index;
			mArray.replaceWithLast(ir);
			if(ir!=mArray.size())
				h(mArray[ir]).index = ir;
			ir = InvasiveSetHook::INDEX_NONE;
		}

		PX_INLINE void reserve(PxU32 size)
		{
			mArray.reserve(size);
		}

		PX_INLINE void shrink()
		{
			// Shrink the buffer such that it fits the needed size exactly
			mArray.shrink();
		}

		PX_INLINE PxU32 size() const
		{
			return mArray.size();
		}

		PX_INLINE Object* operator[](PxU32 i) const
		{
			PX_ASSERT(i<size());
			return mArray[i];
		}

		void clear(HookAccessor h = HookAccessor())
		{
			for(PxU32 i=0;i<size();i++)
				h(mArray[i]).index = InvasiveSetHook::INDEX_NONE;
			mArray.clear();
		}

	private:
		Ps::InlineArray<Object*,4>		mArray;
	};


} // namespace Cm

}

#endif
