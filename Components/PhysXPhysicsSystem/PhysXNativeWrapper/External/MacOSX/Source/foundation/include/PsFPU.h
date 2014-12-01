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


#ifndef PX_FOUNDATION_PSFPU_H
#define PX_FOUNDATION_PSFPU_H

#include "Ps.h"
#include "foundation/PxUnionCast.h"

//unsigned integer representation of a floating-point value.
#ifdef PX_PS3
PX_FORCE_INLINE unsigned int PX_IR(const float x) 
{
	return physx::PxUnionCast<unsigned int, float>(x);
}
#else
#define PX_IR(x)			((PxU32&)(x))
#endif

//signed integer representation of a floating-point value.
#ifdef PX_PS3
PX_FORCE_INLINE int PX_SIR(const float x) 
{
	return physx::PxUnionCast<int, float>(x);
}
#else
#define PX_SIR(x)			((PxI32&)(x))
#endif


//Floating-point representation of a integer value.
#ifdef PX_PS3
PX_FORCE_INLINE float PX_FR(const unsigned int x)
{
	return physx::PxUnionCast<float, unsigned int>(x);
}
#else
#define PX_FR(x)			((PxF32&)(x))
#endif

#ifdef PX_PS3
PX_FORCE_INLINE float* PX_FPTR(unsigned int* x)
{
	return physx::PxUnionCast<float*, unsigned int*>(x);
}

PX_FORCE_INLINE float* PX_FPTR(int* x)
{
	return physx::PxUnionCast<float*, int*>(x);
}
#else
#define PX_FPTR(x)			((PxF32*)(x))
#endif

#define	PX_SIGN_BITMASK		0x80000000

namespace physx
{
namespace shdfnd
{
	class PX_FOUNDATION_API FPUGuard
	{
	public:
		FPUGuard(); // set fpu control word for PhysX
		~FPUGuard(); // restore fpu control word
	private:
		PxU32 mControlWords[8];
	};

} // namespace shdfnd
} // namespace physx

#endif
