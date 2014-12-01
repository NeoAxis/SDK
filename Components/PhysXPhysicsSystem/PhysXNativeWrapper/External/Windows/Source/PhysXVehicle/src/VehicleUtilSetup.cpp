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

#include "PxVehicleSDK.h"
#include "PxVehicleUtilSetup.h"
#include "PxVehicleDrive4W.h"
#include "PxVehicleWheels.h"
#include "PsFoundation.h"
#include "PsUtilities.h"

namespace physx
{

void PxVehicle4WEnable3WMode(const bool removeFrontWheel, PxVehicleWheelsSimData& wheelsSimData, PxVehicleDriveSimData4W& driveSimData)
{
	const PxU32 wheelToRemove = removeFrontWheel ? PxVehicleDrive4W::eFRONT_LEFT_WHEEL : PxVehicleDrive4W::eREAR_LEFT_WHEEL;
	const PxU32 wheelToModify =  removeFrontWheel ? PxVehicleDrive4W::eFRONT_RIGHT_WHEEL : PxVehicleDrive4W::eREAR_RIGHT_WHEEL;

	//Disable the front left wheel.
	wheelsSimData.disableWheel(wheelToRemove);

	//Now reposition the front-right wheel so that it lies at the centre of the front axle.
	{
		PxVec3 offsets[4]={
			wheelsSimData.getWheelCentreOffset(PxVehicleDrive4W::eFRONT_LEFT_WHEEL),
			wheelsSimData.getWheelCentreOffset(PxVehicleDrive4W::eFRONT_RIGHT_WHEEL),
			wheelsSimData.getWheelCentreOffset(PxVehicleDrive4W::eREAR_LEFT_WHEEL),
			wheelsSimData.getWheelCentreOffset(PxVehicleDrive4W::eREAR_RIGHT_WHEEL)};

			offsets[wheelToModify].x=0;

			wheelsSimData.setWheelCentreOffset(PxVehicleDrive4W::eFRONT_LEFT_WHEEL,offsets[PxVehicleDrive4W::eFRONT_LEFT_WHEEL]);
			wheelsSimData.setWheelCentreOffset(PxVehicleDrive4W::eFRONT_RIGHT_WHEEL,offsets[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL]);
			wheelsSimData.setWheelCentreOffset(PxVehicleDrive4W::eREAR_LEFT_WHEEL,offsets[PxVehicleDrive4W::eREAR_LEFT_WHEEL]);
			wheelsSimData.setWheelCentreOffset(PxVehicleDrive4W::eREAR_RIGHT_WHEEL,offsets[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]);
	}
	{
		PxVec3 suspOffset=wheelsSimData.getSuspForceAppPointOffset(wheelToModify);
		suspOffset.x=0;
		wheelsSimData.setSuspForceAppPointOffset(wheelToModify,suspOffset);
	}
	{
		PxVec3 tireOffset=wheelsSimData.getTireForceAppPointOffset(wheelToModify);
		tireOffset.x=0;
		wheelsSimData.setTireForceAppPointOffset(wheelToModify,tireOffset);
	}

	if(PxVehicleDrive4W::eFRONT_RIGHT_WHEEL==wheelToModify)
	{
		//Disable the ackermann steer correction because we only have a single steer wheel now.
		PxVehicleAckermannGeometryData ackermannData=driveSimData.getAckermannGeometryData();
		ackermannData.mAccuracy=0.0f;
		driveSimData.setAckermannGeometryData(ackermannData);
	}

	//We need to set up the differential to make sure that the missing wheel is ignored.
	PxVehicleDifferential4WData diffData =driveSimData.getDiffData();
	if(PxVehicleDrive4W::eFRONT_RIGHT_WHEEL==wheelToModify)	
	{
		diffData.mFrontBias=PX_MAX_F32;
		diffData.mFrontLeftRightSplit=0.0f;
	}
	else
	{
		diffData.mRearBias=PX_MAX_F32;
		diffData.mRearLeftRightSplit=0.0f;
	}
	driveSimData.setDiffData(diffData);

	//The front-right wheel needs to support the mass that was supported by the disabled front-left wheel.
	//Update the suspension data to preserve both the natural frequency and damping ratio.
	PxVehicleSuspensionData suspData=wheelsSimData.getSuspensionData(wheelToModify);
	suspData.mSprungMass*=2.0f;
	suspData.mSpringStrength*=2.0f;
	suspData.mSpringDamperRate*=2.0f;
	wheelsSimData.setSuspensionData(wheelToModify,suspData);
}

void PxVehicle4WEnable3WTadpoleMode(PxVehicleWheelsSimData& wheelsSimData, PxVehicleDriveSimData4W& driveSimData)
{
	PxVehicle4WEnable3WMode(false,wheelsSimData,driveSimData);
}

void PxVehicle4WEnable3WDeltaMode(PxVehicleWheelsSimData& wheelsSimData, PxVehicleDriveSimData4W& driveSimData)
{
	PxVehicle4WEnable3WMode(true,wheelsSimData,driveSimData);
}

}//physx