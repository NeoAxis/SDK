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

#include "PxVehicleDrive.h"
#include "PxVehicleSDK.h"
#include "PxVehicleSuspWheelTire4.h"
#include "PxVehicleSuspLimitConstraintShader.h"
#include "PxVehicleDefaults.h"
#include "PxRigidDynamic.h"
#include "PxShape.h"
#include "PsFoundation.h"
#include "PsUtilities.h"
#include "CmPhysXCommon.h"

namespace physx
{

bool PxVehicleDriveSimData::isValid() const
{
	PX_CHECK_AND_RETURN_VAL(mEngine.isValid(), "Invalid PxVehicleCoreSimulationData.mEngine", false);
	PX_CHECK_AND_RETURN_VAL(mGears.isValid(), "Invalid PxVehicleCoreSimulationData.mGears", false);
	PX_CHECK_AND_RETURN_VAL(mClutch.isValid(), "Invalid PxVehicleCoreSimulationData.mClutch", false);
	PX_CHECK_AND_RETURN_VAL(mAutoBox.isValid(), "Invalid PxVehicleCoreSimulationData.mAutoBox", false);
	return true;
}

void PxVehicleDriveSimData::setEngineData(const PxVehicleEngineData& engine)
{
	PX_CHECK_AND_RETURN(engine.mTorqueCurve.getNumDataPairs()>0, "Engine torque curve must specify at least one entry");
	PX_CHECK_AND_RETURN(engine.mPeakTorque>0, "Engine peak torque  must be greater than zero");
	PX_CHECK_AND_RETURN(engine.mMaxOmega>0, "Engine max omega must be greater than zero");
	PX_CHECK_AND_RETURN(engine.mDampingRateFullThrottle>=0, "Full throttle damping rate must be greater than or equal to zero");
	PX_CHECK_AND_RETURN(engine.mDampingRateZeroThrottleClutchEngaged>=0, "Zero throttle clutch engaged clutch damping rate must be greater than or equal to zero");
	PX_CHECK_AND_RETURN(engine.mDampingRateZeroThrottleClutchDisengaged>=0, "Zero throttle clutch disengaged damping rate must be greater than or equal to zero");
	mEngine=engine;
	mEngine.mRecipMaxOmega=1.0f/engine.mMaxOmega;
}

void PxVehicleDriveSimData::setGearsData(const PxVehicleGearsData& gears)
{
	PX_CHECK_AND_RETURN(gears.mRatios[PxVehicleGearsData::eREVERSE]<0, "Reverse gear ratio must be negative");
	PX_CHECK_AND_RETURN(gears.mRatios[PxVehicleGearsData::eNEUTRAL]==0, "Neutral gear ratio must be zero");
	PX_CHECK_AND_RETURN(gears.mRatios[PxVehicleGearsData::eFIRST]>0, "First gear ratio must be positive");
	PX_CHECK_AND_RETURN(PxVehicleGearsData::eSECOND>=gears.mNumRatios || (gears.mRatios[PxVehicleGearsData::eSECOND]>0 && gears.mRatios[PxVehicleGearsData::eSECOND] < gears.mRatios[PxVehicleGearsData::eFIRST]), "Second gear ratio must be positive and less than first gear ratio");
	PX_CHECK_AND_RETURN(PxVehicleGearsData::eTHIRD>=gears.mNumRatios || (gears.mRatios[PxVehicleGearsData::eTHIRD]>0 && gears.mRatios[PxVehicleGearsData::eTHIRD] < gears.mRatios[PxVehicleGearsData::eSECOND]), "Third gear ratio must be positive and less than second gear ratio");
	PX_CHECK_AND_RETURN(PxVehicleGearsData::eFOURTH>=gears.mNumRatios || (gears.mRatios[PxVehicleGearsData::eFOURTH]>0 && gears.mRatios[PxVehicleGearsData::eFOURTH] < gears.mRatios[PxVehicleGearsData::eTHIRD]), "Fourth gear ratio must be positive and less than third gear ratio");
	PX_CHECK_AND_RETURN(PxVehicleGearsData::eFIFTH>=gears.mNumRatios || (gears.mRatios[PxVehicleGearsData::eFIFTH]>0 && gears.mRatios[PxVehicleGearsData::eFIFTH] < gears.mRatios[PxVehicleGearsData::eFOURTH]), "Fifth gear ratio must be positive and less than fourth gear ratio");
	PX_CHECK_AND_RETURN(PxVehicleGearsData::eSIXTH>=gears.mNumRatios || (gears.mRatios[PxVehicleGearsData::eSIXTH]>0 && gears.mRatios[PxVehicleGearsData::eSIXTH] < gears.mRatios[PxVehicleGearsData::eFIFTH]), "Sixth gear ratio must be positive and less than fifth gear ratio");
	PX_CHECK_AND_RETURN(gears.mFinalRatio>0, "Final gear ratio must be greater than zero");
	PX_CHECK_AND_RETURN(gears.mNumRatios>=3, "Number of gear ratios must be at least 3 - we need at least reverse, neutral, and a forward gear");

	mGears=gears;
}

void PxVehicleDriveSimData::setClutchData(const PxVehicleClutchData& clutch)
{
	PX_CHECK_AND_RETURN(clutch.mStrength>0, "Clutch strength must be greater than zero");

	mClutch=clutch;
}

void PxVehicleDriveSimData::setAutoBoxData(const PxVehicleAutoBoxData& autobox)
{
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eREVERSE]>=0, "Autobox gearup ratio in reverse must be greater than or equal to zero");
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eNEUTRAL]>=0, "Autobox gearup ratio in neutral must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eFIRST]>=0, "Autobox gearup ratio in first must be greater than or equal to zero");
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eSECOND]>=0, "Autobox gearup ratio in second must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eTHIRD]>=0, "Autobox gearup ratio in third must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eFOURTH]>=0, "Autobox gearup ratio in fourth must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mUpRatios[PxVehicleGearsData::eFIFTH]>=0, "Autobox gearup ratio in fifth must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eREVERSE]>=0, "Autobox geardown ratio in reverse must be greater than or equal to zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eNEUTRAL]>=0, "Autobox geardown ratio in neutral must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eFIRST]>=0, "Autobox geardown ratio in first must be greater than or equal to zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eSECOND]>=0, "Autobox geardown ratio in second must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eTHIRD]>=0, "Autobox geardown ratio in third must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eFOURTH]>=0, "Autobox geardown ratio in fourth must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eFIFTH]>=0, "Autobox geardown ratio in fifth must be greater than zero");
	PX_CHECK_AND_RETURN(autobox.mDownRatios[PxVehicleGearsData::eSIXTH]>=0, "Autobox geardown ratio in fifth must be greater than zero");

	mAutoBox=autobox;
}

///////////////////////////////////

PxVehicleDriveDynData::PxVehicleDriveDynData()
	:	mUseAutoGears(false),
		mGearUpPressed(false),
		mGearDownPressed(false),
		mCurrentGear(PxVehicleGearsData::eNEUTRAL),
		mTargetGear(PxVehicleGearsData::eNEUTRAL),
		mEnginespeed(0.0f),
		mGearSwitchTime(0.0f),
		mAutoBoxSwitchTime(0.0f)
{
	for(PxU32 i=0;i<eMAX_NUM_ANALOG_INPUTS;i++)
	{
		mControlAnalogVals[i]=0.0f;		
	}
}

void PxVehicleDriveDynData::setToRestState()
{
	//Set analog inputs to zero so the vehicle starts completely at rest.
	for(PxU32 i=0;i<eMAX_NUM_ANALOG_INPUTS;i++)
	{
		mControlAnalogVals[i]=0.0f;
	}
	mGearUpPressed=false;
	mGearDownPressed=false;

	//Set the vehicle to neutral gear.
	mCurrentGear=PxVehicleGearsData::eNEUTRAL;
	mTargetGear=PxVehicleGearsData::eNEUTRAL;
	mGearSwitchTime=0.0f;
	mAutoBoxSwitchTime=0.0f;

	//Set internal dynamics to zero so the vehicle starts completely at rest.
	mEnginespeed=0.0f;
}

bool PxVehicleDriveDynData::isValid() const
{
	return true;
}

void PxVehicleDriveDynData::setAnalogInput(const PxReal analogVal, const PxU32 type)
{
	PX_CHECK_AND_RETURN(analogVal>=-1.01f && analogVal<=1.01f, "PxVehicleDriveDynData::setAnalogInput - analogVal must be in range (-1,1)");
	PX_CHECK_AND_RETURN(type<eMAX_NUM_ANALOG_INPUTS, "PxVehicleDriveDynData::setAnalogInput - illegal type");
	mControlAnalogVals[type]=analogVal;
}

PxReal PxVehicleDriveDynData::getAnalogInput(const PxU32 type)
{
	PX_CHECK_AND_RETURN_VAL(type<eMAX_NUM_ANALOG_INPUTS, "PxVehicleDriveDynData::getAnalogInput - illegal type", 0.0f);
	return mControlAnalogVals[type];
}

///////////////////////////////////

bool PxVehicleDrive::isValid() const
{
	PX_CHECK_AND_RETURN_VAL(PxVehicleWheels::isValid(), "invalid PxVehicleWheels", false);
	PX_CHECK_AND_RETURN_VAL(mDriveDynData.isValid(), "Invalid PxVehicleDrive.mCoreSimData", false);
	return true;
}

PxU32 PxVehicleDrive::computeByteSize(const PxU32 numWheels4)
{
	return PxVehicleWheels::computeByteSize(numWheels4);
}

PxU8* PxVehicleDrive::patchupPointers(PxVehicleDrive* veh, PxU8* ptr, const PxU32 numWheels4, const PxU32 numWheels)
{
	return PxVehicleWheels::patchupPointers(veh,ptr,numWheels4,numWheels);
}

void PxVehicleDrive::free()
{
	PxVehicleWheels::free();
}

void PxVehicleDrive::setup
(PxPhysics* physics, PxRigidDynamic* vehActor, 
 const PxVehicleWheelsSimData& wheelsData,  
 const PxU32 numDrivenWheels, const PxU32 numNonDrivenWheels)
{
	//Set up the wheels.
	PxVehicleWheels::setup(physics,vehActor,wheelsData,numDrivenWheels,numNonDrivenWheels);
}

void PxVehicleDrive::setToRestState()
{
	//Set core to rest state.
	PxVehicleWheels::setToRestState();

	//Set dynamics data to rest state.
	mDriveDynData.setToRestState();
}












} //namespace physx

