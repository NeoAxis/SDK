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

#include "PxVehicleUtilControl.h"
#include "PxVehicleSDK.h"
#include "PxVehicleDrive4W.h"
#include "PxSimpleTypes.h"

namespace physx
{

PxF32 processDigitalValue
(const PxU32 inputType, 
 const PxVehicleKeySmoothingData& keySmoothing, const bool digitalValue,
 const PxF32 timestep, 
 const PxF32 analogVal)
{
	PxF32 newAnalogVal=analogVal;
	if(digitalValue)
	{
		newAnalogVal+=keySmoothing.mRiseRates[inputType]*timestep;
	}
	else
	{
		newAnalogVal-=keySmoothing.mFallRates[inputType]*timestep;
	}

	return PxClamp(newAnalogVal,0.0f,1.0f);
}

void PxVehicleDrive4WSmoothDigitalRawInputsAndSetAnalogInputs
(const PxVehicleKeySmoothingData& keySmoothing, const PxFixedSizeLookupTable<8>& steerVsForwardSpeedTable,
 const PxVehicleDrive4WRawInputData& rawInputData, 
 const PxF32 timestep, 
 PxVehicleDrive4W& focusVehicle)
{
	const bool gearup=rawInputData.getGearUp();
	const bool geardown=rawInputData.getGearDown();
	focusVehicle.mDriveDynData.setGearDown(geardown);
	focusVehicle.mDriveDynData.setGearUp(gearup);

	const PxF32 accel=processDigitalValue(PxVehicleDrive4W::eANALOG_INPUT_ACCEL,keySmoothing,rawInputData.getDigitalAccel(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_ACCEL));
	focusVehicle.mDriveDynData.setAnalogInput(accel,PxVehicleDrive4W::eANALOG_INPUT_ACCEL);

	const PxF32 brake=processDigitalValue(PxVehicleDrive4W::eANALOG_INPUT_BRAKE,keySmoothing,rawInputData.getDigitalBrake(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_BRAKE));
	focusVehicle.mDriveDynData.setAnalogInput(brake,PxVehicleDrive4W::eANALOG_INPUT_BRAKE);

	const PxF32 handbrake=processDigitalValue(PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE,keySmoothing,rawInputData.getDigitalHandbrake(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE));
	focusVehicle.mDriveDynData.setAnalogInput(handbrake,PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE);

	PxF32 steerLeft=processDigitalValue(PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT,keySmoothing,rawInputData.getDigitalSteerLeft(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT));
	PxF32 steerRight=processDigitalValue(PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT,keySmoothing,rawInputData.getDigitalSteerRight(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT));
	const PxF32 vz=focusVehicle.computeForwardSpeed();
	const PxF32 vzAbs=PxAbs(vz);
	const bool isInAir=focusVehicle.isInAir();
	const PxF32 maxSteer=(isInAir ? 1.0f :steerVsForwardSpeedTable.getYVal(vzAbs));
	const PxF32 steer=PxAbs(steerRight-steerLeft);
	if(steer>maxSteer)
	{
		const PxF32 k=maxSteer/steer;
		steerLeft*=k;
		steerRight*=k;
	}
	focusVehicle.mDriveDynData.setAnalogInput(steerLeft, PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT);
	focusVehicle.mDriveDynData.setAnalogInput(steerRight, PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT);
}

//////////////////////////////////

//process value in range(0,1)
PX_FORCE_INLINE PxF32 processPositiveAnalogValue
(const PxF32 riseRate, const PxF32 fallRate,
 const PxF32 currentVal, const PxF32 targetVal,
 const PxF32 timestep)
{
	PX_ASSERT(targetVal>=-0.01f && targetVal<=1.01f);
	PxF32 val;
	if(currentVal<targetVal)
	{
		val=currentVal + riseRate*timestep;
		val=PxMin(val,targetVal);
	}
	else 
	{
		val=currentVal - fallRate*timestep;
		val=PxMax(val,targetVal);
	}
	return val;
}

//process value in range(-1,1)
PX_FORCE_INLINE PxF32 processAnalogValue
(const PxF32 riseRate, const PxF32 fallRate,  
 const PxF32 currentVal, const PxF32 targetVal,
 const PxF32 timestep)
{
	PX_ASSERT(PxAbs(targetVal)<=1.01f);

	PxF32 val=0.0f;	// PT: the following code could leave that variable uninitialized!!!!!
	if(0==targetVal)
	{
		//Drift slowly back to zero 
		if(currentVal>0)
		{
			val=currentVal-fallRate*timestep;
			val=PxMax(val,0.0f);
		}
		else if(currentVal<0)
		{
			val=currentVal+fallRate*timestep;
			val=PxMin(val,0.0f);
		}
	}
	else
	{
		if(currentVal < targetVal)
		{
			if(currentVal<0)
			{
				val=currentVal + fallRate*timestep;
				val=PxMin(val,targetVal);
			}
			else
			{
				val=currentVal + riseRate*timestep;
				val=PxMin(val,targetVal);
			}
		}
		else 
		{
			if(currentVal>0)
			{
				val=currentVal - fallRate*timestep;
				val=PxMax(val,targetVal);
			}
			else
			{
				val=currentVal - riseRate*timestep;
				val=PxMax(val,targetVal);
			}
		}	
	}
	return val;
}


void PxVehicleDrive4WSmoothAnalogRawInputsAndSetAnalogInputs
(const PxVehiclePadSmoothingData& padSmoothing, const PxFixedSizeLookupTable<8>& steerVsForwardSpeedTable,
 const PxVehicleDrive4WRawInputData& rawInputData, 
 const PxF32 timestep, 
 PxVehicleDrive4W& focusVehicle)
{
	//gearup/geardown
	const bool gearup=rawInputData.getGearUp();
	const bool geardown=rawInputData.getGearDown();
	focusVehicle.mDriveDynData.setGearUp(gearup);
	focusVehicle.mDriveDynData.setGearDown(geardown);

	//Update analog inputs for focus vehicle.

	//Process the accel.
	{
		const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDrive4W::eANALOG_INPUT_ACCEL];
		const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDrive4W::eANALOG_INPUT_ACCEL];
		const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_ACCEL);
		const PxF32 targetVal=rawInputData.getAnalogAccel();
		const PxF32 accel=processPositiveAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
		focusVehicle.mDriveDynData.setAnalogInput(accel,PxVehicleDrive4W::eANALOG_INPUT_ACCEL);
	}

	//Process the brake
	{
		const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDrive4W::eANALOG_INPUT_BRAKE];
		const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDrive4W::eANALOG_INPUT_BRAKE];
		const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_BRAKE);
		const PxF32 targetVal=rawInputData.getAnalogBrake();
		const PxF32 brake=processPositiveAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
		focusVehicle.mDriveDynData.setAnalogInput(brake,PxVehicleDrive4W::eANALOG_INPUT_BRAKE);
	}

	//Process the handbrake.
	{
		const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE];
		const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE];
		const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE);
		const PxF32 targetVal=rawInputData.getAnalogHandbrake();
		const PxF32 handbrake=processPositiveAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
		focusVehicle.mDriveDynData.setAnalogInput(handbrake,PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE);
	}

	//Process the steer
	{
		const PxF32 vz=focusVehicle.computeForwardSpeed();
		const PxF32 vzAbs=PxAbs(vz);
		const bool isInAir=focusVehicle.isInAir();
		const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT];
		const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT];
		const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT);
		const PxF32 targetVal=rawInputData.getAnalogSteer()*(isInAir ? 1.0f :steerVsForwardSpeedTable.getYVal(vzAbs));
		const PxF32 steer=processAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
		focusVehicle.mDriveDynData.setAnalogInput(0.0f, PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT);
		focusVehicle.mDriveDynData.setAnalogInput(steer, PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT);
	}
}

void PxVehicleDriveTankSmoothAnalogRawInputsAndSetAnalogInputs
(const PxVehiclePadSmoothingData& padSmoothing, 
 const PxVehicleDriveTankRawInputData& rawInputData, 
 const PxReal timestep, 
 PxVehicleDriveTank& focusVehicle)
{
	//Process the gearup/geardown buttons.
	const bool gearup=rawInputData.getGearUp();
	const bool geardown=rawInputData.getGearDown();
	focusVehicle.mDriveDynData.setGearUp(gearup);
	focusVehicle.mDriveDynData.setGearDown(geardown);

	//Process the accel.
	{
		const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_ACCEL];
		const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_ACCEL];
		const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_ACCEL);
		const PxF32 targetVal=rawInputData.getAnalogAccel();
		const PxF32 accel=processPositiveAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
		focusVehicle.mDriveDynData.setAnalogInput(accel,PxVehicleDriveTank::eANALOG_INPUT_ACCEL);
	}

	PX_ASSERT(focusVehicle.getDriveModel()==rawInputData.getDriveModel());
	switch(rawInputData.getDriveModel())
	{
	case PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL:
		{
			//Process the left brake.
			{
				const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT];
				const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT];
				const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
				const PxF32 targetVal=rawInputData.getAnalogLeftBrake();
				const PxF32 accel=processPositiveAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
				focusVehicle.mDriveDynData.setAnalogInput(accel,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
			}

			//Process the right brake.
			{
				const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT];
				const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT];
				const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
				const PxF32 targetVal=rawInputData.getAnalogRightBrake();
				const PxF32 accel=processPositiveAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
				focusVehicle.mDriveDynData.setAnalogInput(accel,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
			}

			//Left thrust
			{
				const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT];
				const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT];
				const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
				const PxF32 targetVal=rawInputData.getAnalogLeftThrust();
				const PxF32 val=processAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
				focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
			}

			//Right thrust
			{
				const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT];
				const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT];
				const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
				const PxF32 targetVal=rawInputData.getAnalogRightThrust();
				const PxF32 val=processAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
				focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
			}
		}
		break;

	case PxVehicleDriveTank::eDRIVE_MODEL_STANDARD:
		{
			//Right thrust
			{
				const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT];
				const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT];
				const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
				const PxF32 targetVal=rawInputData.getAnalogRightThrust()-rawInputData.getAnalogRightBrake();
				const PxF32 val=processAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
				if(val>0)
				{
					focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
					focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
				}
				else
				{
					focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
					focusVehicle.mDriveDynData.setAnalogInput(-val,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
				}
			}

			//Left thrust
			{
				const PxF32 riseRate=padSmoothing.mRiseRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT];
				const PxF32 fallRate=padSmoothing.mFallRates[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT];
				const PxF32 currentVal=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
				const PxF32 targetVal=rawInputData.getAnalogLeftThrust()-rawInputData.getAnalogLeftBrake();
				const PxF32 val=processAnalogValue(riseRate,fallRate,currentVal,targetVal,timestep);
				if(val>0)
				{
					focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
					focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
				}
				else
				{
					focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
					focusVehicle.mDriveDynData.setAnalogInput(-val,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
				}
			}
		}
		break;

	default:
		PX_ASSERT(false);
			break;
	}
}

void PxVehicleDriveTankSmoothDigitalRawInputsAndSetAnalogInputs
(const PxVehicleKeySmoothingData& keySmoothing, 
 const PxVehicleDriveTankRawInputData& rawInputData, 
 const PxF32 timestep, 
 PxVehicleDriveTank& focusVehicle)
{
	PxF32 val;
	val=processDigitalValue(PxVehicleDriveTank::eANALOG_INPUT_ACCEL,keySmoothing,rawInputData.getDigitalAccel(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_ACCEL));
	focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_ACCEL);
	val=processDigitalValue(PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT,keySmoothing,rawInputData.getDigitalLeftThrust(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT));
	focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
	val=processDigitalValue(PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT,keySmoothing,rawInputData.getDigitalRightThrust(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT));
	focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
	val=processDigitalValue(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT,keySmoothing,rawInputData.getDigitalLeftBrake(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT));
	focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
	val=processDigitalValue(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT,keySmoothing,rawInputData.getDigitalRightBrake(),timestep,focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT));
	focusVehicle.mDriveDynData.setAnalogInput(val,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);

	//Update digital inputs for focus vehicle.
	focusVehicle.mDriveDynData.setGearUp(rawInputData.getGearUp());
	focusVehicle.mDriveDynData.setGearDown(rawInputData.getGearDown());

	switch(rawInputData.getDriveModel())
	{
	case PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL:
		{
			const PxF32 thrustL=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
			focusVehicle.mDriveDynData.setAnalogInput(thrustL,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
			focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);

			const PxF32 thrustR=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
			focusVehicle.mDriveDynData.setAnalogInput(thrustR,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
			focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
		}
		break;
	case PxVehicleDriveTank::eDRIVE_MODEL_STANDARD:
		{
			const PxF32 thrustL=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
			if(thrustL>0)
			{
				focusVehicle.mDriveDynData.setAnalogInput(thrustL,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
				focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
			}
			else
			{
				focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT);
				focusVehicle.mDriveDynData.setAnalogInput(-thrustL,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT);
			}

			const PxF32 thrustR=focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT)-focusVehicle.mDriveDynData.getAnalogInput(PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
			if(thrustR>0)
			{
				focusVehicle.mDriveDynData.setAnalogInput(thrustR,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
				focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
			}
			else
			{
				focusVehicle.mDriveDynData.setAnalogInput(0.0f,PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT);
				focusVehicle.mDriveDynData.setAnalogInput(-thrustR,PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT);
			}
		}
		break;

	default:
		PX_ASSERT(false);
		break;
	}

}



} //physx

