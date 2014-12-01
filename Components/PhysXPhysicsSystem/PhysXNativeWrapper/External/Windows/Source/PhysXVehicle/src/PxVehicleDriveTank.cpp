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

#include "PxVehicleDriveTank.h"
#include "PxVehicleWheels.h"
#include "PxVehicleSDK.h"
#include "PxVehicleSuspWheelTire4.h"
#include "PxVehicleSuspLimitConstraintShader.h"
#include "PxVehicleDefaults.h"
#include "PxRigidDynamic.h"
#include "PxShape.h"
#include "PsFoundation.h"
#include "PsUtilities.h"
#include "CmPhysXCommon.h"
#include "PxScene.h"

namespace physx
{

bool PxVehicleDriveTank::isValid() const
{
	PX_CHECK_AND_RETURN_VAL(PxVehicleDrive::isValid(), "invalid PxVehicleDrive", false);
	PX_CHECK_AND_RETURN_VAL(mDriveSimData.isValid(), "Invalid PxVehicleDriveTank.mCoreSimData", false);
	return true;
}

PxVehicleDriveTank* PxVehicleDriveTank::allocate(const PxU32 numWheels)
{
	PX_CHECK_AND_RETURN_NULL(numWheels>0, "Cars with zero wheels are illegal");

	//Compute the bytes needed.
	const PxU32 numWheels4 = (((numWheels + 3) & ~3) >> 2);
	const PxU32 byteSize = sizeof(PxVehicleDriveTank) + + PxVehicleDrive::computeByteSize(numWheels4);

	//Allocate the memory.
	PxVehicleDriveTank* veh = (PxVehicleDriveTank*)PX_ALLOC(byteSize, PX_DEBUG_EXP("PxVehicleDriveTank"));

	//Patch up the pointers.
	PxU8* ptr = (PxU8*)veh + sizeof(PxVehicleDriveTank);
	PxVehicleDrive::patchupPointers(veh,ptr,numWheels4,numWheels);

	//Set the vehicle type.
	veh->mType = eVEHICLE_TYPE_DRIVETANK;

	//Set the default drive model.
	veh->mDriveModel = eDRIVE_MODEL_STANDARD;

	return veh;
}

void PxVehicleDriveTank::free()
{
	PxVehicleDrive::free();
}

void PxVehicleDriveTank::setup
(PxPhysics* physics, PxRigidDynamic* vehActor, 
 const PxVehicleWheelsSimData& wheelsData, const PxVehicleDriveSimData& driveData,
 const PxU32 numDrivenWheels)
{
	PX_CHECK_AND_RETURN(driveData.isValid(), "PxVehicleDriveTank::setup - illegal drive data");

	//Set up the wheels.
	PxVehicleDrive::setup(physics,vehActor,wheelsData,numDrivenWheels,0);

	//Start setting up the drive.
	PX_CHECK_MSG(driveData.isValid(), "PxVehicle4WDrive - invalid driveData");

	//Copy the simulation data.
	mDriveSimData = driveData;
}

PxVehicleDriveTank* PxVehicleDriveTank::create
(PxPhysics* physics, PxRigidDynamic* vehActor, 
 const PxVehicleWheelsSimData& wheelsData, const PxVehicleDriveSimData& driveData,
 const PxU32 numDrivenWheels)
{
	PxVehicleDriveTank* tank=PxVehicleDriveTank::allocate(numDrivenWheels);
	tank->setup(physics,vehActor,wheelsData,driveData,numDrivenWheels);
	return tank;
}


void PxVehicleDriveTank::setToRestState()
{
	//Set core to rest state.
	PxVehicleDrive::setToRestState();
}












} //namespace physx

