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

#include "PxVehicleTireFriction.h"
#include "PxPhysics.h"
#include "CmPhysXCommon.h"
#include "PsFoundation.h"
#include "PsUtilities.h"

namespace physx
{

PxVehicleDrivableSurfaceToTireFrictionPairs* PxVehicleDrivableSurfaceToTireFrictionPairs::create
(const PxU32 maxNumTireTypes, const PxU32 maxNumSurfaceTypes, const PxMaterial** drivableSurfaceMaterials, const PxVehicleDrivableSurfaceType* drivableSurfaceTypes)
{
	PX_CHECK_AND_RETURN_VAL(maxNumSurfaceTypes < eMAX_NUM_SURFACE_TYPES, "maxNumSurfaceTypes must be less than eMAX_NUM_SURFACE_TYPES", NULL);

	PxU32 byteSize = ((sizeof(PxU32)*(maxNumTireTypes*maxNumSurfaceTypes) + 15) & ~15);
	byteSize += ((sizeof(PxMaterial*)*maxNumSurfaceTypes + 15) & ~15);
	byteSize += ((sizeof(PxVehicleDrivableSurfaceType)*maxNumSurfaceTypes + 15) & ~15);
	byteSize += ((sizeof(PxVehicleDrivableSurfaceToTireFrictionPairs) + 15) & ~ 15);

	PxU8* ptr = (PxU8*)PX_ALLOC(byteSize, PX_DEBUG_EXP("PxVehicleDrivableSurfaceToTireFrictionPairs"));
	PxVehicleDrivableSurfaceToTireFrictionPairs* pairs = (PxVehicleDrivableSurfaceToTireFrictionPairs*)ptr;
	ptr += ((sizeof(PxVehicleDrivableSurfaceToTireFrictionPairs) + 15) & ~ 15);

	pairs->mPairs = (PxReal*)ptr;
	ptr += ((sizeof(PxU32)*(maxNumTireTypes*maxNumSurfaceTypes) + 15) & ~15);
	pairs->mDrivableSurfaceMaterials = (const PxMaterial**)ptr;
	ptr += ((sizeof(PxMaterial*)*maxNumSurfaceTypes + 15) & ~15);
	pairs->mDrivableSurfaceTypes = (PxVehicleDrivableSurfaceType*)ptr;
	ptr += ((sizeof(PxVehicleDrivableSurfaceType)*maxNumSurfaceTypes +15) & ~15);

	for(PxU32 i=0;i<maxNumSurfaceTypes;i++)
	{
		pairs->mDrivableSurfaceTypes[i] = drivableSurfaceTypes[i];
		pairs->mDrivableSurfaceMaterials[i] = drivableSurfaceMaterials[i];
	}
	for(PxU32 i=0;i<maxNumTireTypes*maxNumSurfaceTypes;i++)
	{
		pairs->mPairs[i]=1.0f;
	}

	pairs->mNumTireTypes=maxNumTireTypes;
	pairs->mNumSurfaceTypes=maxNumSurfaceTypes;

	return pairs;
}

void PxVehicleDrivableSurfaceToTireFrictionPairs::release()
{
	PX_FREE(this);
}

void PxVehicleDrivableSurfaceToTireFrictionPairs::setTypePairFriction(const PxU32 surfaceType, const PxU32 tireType, const PxReal value)
{
	PX_CHECK_AND_RETURN(tireType<mNumTireTypes, "Invalid tireType");
	PX_CHECK_AND_RETURN(surfaceType<mNumSurfaceTypes, "Invalid surfaceType");

	*(mPairs + mNumTireTypes*surfaceType + tireType) = value;
}

PxReal PxVehicleDrivableSurfaceToTireFrictionPairs::getTypePairFriction(const PxU32 surfaceType, const PxU32 tireType) const 
{
	PX_CHECK_AND_RETURN_VAL(tireType<mNumTireTypes, "Invalid tireType", 0.0f);
	PX_CHECK_AND_RETURN_VAL(surfaceType<mNumSurfaceTypes, "Invalid surfaceType", 0.0f);

	return *(mPairs + mNumTireTypes*surfaceType + tireType);
}




}//physx

