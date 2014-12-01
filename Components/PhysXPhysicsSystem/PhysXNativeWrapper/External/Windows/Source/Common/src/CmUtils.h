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


#ifndef PX_PHYSICS_COMMON_UTILS
#define PX_PHYSICS_COMMON_UTILS


#include "PxVec3.h"
#include "PxMat33.h"

#include "CmPhysXCommon.h"

namespace physx
{
namespace Cm
{

PX_INLINE void transformInertiaTensor(const PxVec3& invD, const PxMat33& M, PxMat33& mIInv)
{
	const float	axx = invD.x*M(0,0), axy = invD.x*M(1,0), axz = invD.x*M(2,0);
	const float	byx = invD.y*M(0,1), byy = invD.y*M(1,1), byz = invD.y*M(2,1);
	const float	czx = invD.z*M(0,2), czy = invD.z*M(1,2), czz = invD.z*M(2,2);

	mIInv(0,0) = axx*M(0,0) + byx*M(0,1) + czx*M(0,2);
	mIInv(1,1) = axy*M(1,0) + byy*M(1,1) + czy*M(1,2);
	mIInv(2,2) = axz*M(2,0) + byz*M(2,1) + czz*M(2,2);

	mIInv(0,1) = mIInv(1,0)	= axx*M(1,0) + byx*M(1,1) + czx*M(1,2);
	mIInv(0,2) = mIInv(2,0)	= axx*M(2,0) + byx*M(2,1) + czx*M(2,2);
	mIInv(1,2) = mIInv(2,1)	= axy*M(2,0) + byy*M(2,1) + czy*M(2,2);
}

template <typename T, PxU32 size>
struct Block
{
	PxU8 mem[sizeof(T)*size];
	Block() {}	// get around VS warning C4345, otherwise useless
};


// Array with externally managed storage.
// Allocation and resize policy are managed by the owner, 
// Very minimal functionality right now, just POD types

template <typename T, 
		  typename Owner, 
		  typename IndexType,
		  void (Owner::*realloc)(T*& currentMem, IndexType& currentCapacity, IndexType size, IndexType requiredMinCapacity)>
class OwnedArray
{
public:
	OwnedArray() 
	: mData(0)
	, mCapacity(0) 
	, mSize(0)
	{}

	~OwnedArray()		// owner must call releaseMem before destruction
	{
		PX_ASSERT(mCapacity==0);
	}

	void pushBack(T& element, Owner& owner)
	{
		// there's a failure case if here if we push an existing element which causes a resize -
		// a rare case not worth coding around; if you need it, copy the element then push it.

		PX_ASSERT(&element<mData || &element>=mData+mSize);
		if(mSize==mCapacity)
			(owner.*realloc)(mData, mCapacity, mSize, mSize+1);

		PX_ASSERT(mData && mSize<mCapacity);
		mData[mSize++] = element;
	}

	IndexType size() const
	{
		return mSize;
	}

	void replaceWithLast(IndexType index)
	{
		PX_ASSERT(index<mSize);
		mData[index] = mData[--mSize];
	}

	T* begin() const
	{
		return mData;
	}

	T* end() const
	{
		return mData+mSize;
	}

	T& operator [](IndexType index)
	{
		PX_ASSERT(index<mSize);
		return mData[index];
	}

	const T& operator [](IndexType index) const
	{
		PX_ASSERT(index<mSize);
		return mData[index];
	}


	void reserve(IndexType capacity, Owner &owner)
	{
		if(capacity>=mCapacity)
			(owner.*realloc)(mData, mCapacity, mSize, capacity);
	}

	void releaseMem(Owner &owner)
	{
		mSize = 0;
		(owner.*realloc)(mData, mCapacity, 0, 0);
	}

private:
	T*					mData;
	IndexType			mCapacity;
	IndexType			mSize;

	// just in case someone tries to use a non-POD in here
	union FailIfNonPod
	{
		T t;
		int x;
	};
};

} // namespace Cm

}

#endif
