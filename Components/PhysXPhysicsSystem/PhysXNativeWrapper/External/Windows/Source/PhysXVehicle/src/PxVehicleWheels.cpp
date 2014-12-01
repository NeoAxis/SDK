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
#include "PxVehicleWheels.h"
#include "PxVehicleSuspWheelTire4.h"
#include "PxVehicleSuspLimitConstraintShader.h"
#include "PxVehicleDefaults.h"
#ifdef PX_PS3
#include "ps3/PxVehicle4WSuspLimitConstraintShaderSpu.h"
#endif
#include "PxRigidDynamic.h"
#include "PxShape.h"
#include "PsFoundation.h"
#include "PsUtilities.h"
#include "CmPhysXCommon.h"

namespace physx
{

extern PxVec3 gRight;
extern PxVec3 gUp;
extern PxVec3 gForward;

PxVehicleWheelsSimData* PxVehicleWheelsSimData::allocate(const PxU32 numWheels)
{
	//Byte size
	const PxU32 numWheels4 =(((numWheels + 3) & ~3) >> 2);
	const PxU32 byteSize = sizeof(PxVehicleWheelsSimData) + sizeof(PxVehicleWheels4SimData)*numWheels4;

	//Allocate
	PxU8* ptr = (PxU8*)PX_ALLOC(byteSize, PX_DEBUG_EXP("PxVehicleSuspWheelTireNSimData"));
	PxVehicleWheelsSimData* simData = (PxVehicleWheelsSimData*) ptr;

	//Patchup pointers.
	ptr += sizeof(PxVehicleWheelsSimData);
	simData->mWheels4SimData = (PxVehicleWheels4SimData*)ptr;
	simData->mNumWheels4 = numWheels4;
	simData->mNumActiveWheels = numWheels;

	//Placement new.
	for(PxU32 i=0;i<numWheels4;i++)
	{
		new(&simData->mWheels4SimData[i]) PxVehicleWheels4SimData();
	}
	new(&simData->mNormalisedLoadFilter) PxVehicleTireLoadFilterData();

	//Disable unused wheels.
	for(PxU32 i=numWheels;i<numWheels4*4;i++)
	{
		simData->disableWheel(i);
	}

	return simData;
}

void PxVehicleWheelsSimData::setChassisMass(const PxF32 chassisMass)
{

	//Target spring natural frequency = 9.66
	//Target spring damping ratio = 0.62
	const PxF32 mult=1.0f/(1.0f*mNumActiveWheels);
	const PxF32 sprungMass=chassisMass*mult;
	const PxF32 w0=9.66f;
	const PxF32 r=0.62f;
	for(PxU32 i=0;i<mNumActiveWheels;i++)
	{
		PxVehicleSuspensionData susp=getSuspensionData(i);
		susp.mSprungMass=sprungMass;
		susp.mSpringStrength=w0*w0*sprungMass;
		susp.mSpringDamperRate=r*2*sprungMass*w0;
		setSuspensionData(i,susp);
	}
}

void PxVehicleWheelsSimData::free()
{
	for(PxU32 i=0;i<mNumWheels4;i++)
	{
		mWheels4SimData[i].~PxVehicleWheels4SimData();
	}

	PX_FREE(this);
}

PxVehicleWheelsSimData& PxVehicleWheelsSimData::operator=(const PxVehicleWheelsSimData& src)
{
	PX_CHECK_MSG(mNumActiveWheels == src.mNumActiveWheels, "target PxVehicleSuspWheelTireNSimData must match the number of wheels in src");

	for(PxU32 i=0;i<src.mNumWheels4;i++)
	{
		mWheels4SimData[i] = src.mWheels4SimData[i];
	}

	mNormalisedLoadFilter = src.mNormalisedLoadFilter;

	return *this;
}

void PxVehicleWheelsSimData::copy(const PxVehicleWheelsSimData& src, const PxU32 srcWheel, const PxU32 wheel)
{
	PX_CHECK_AND_RETURN(srcWheel < src.mNumActiveWheels, "Illegal src wheel");
	PX_CHECK_AND_RETURN(wheel < mNumActiveWheels, "Illegal target wheel");

	setSuspensionData(wheel,src.getSuspensionData(srcWheel));
	setWheelData(wheel,src.getWheelData(srcWheel));
	setTireData(wheel,src.getTireData(srcWheel));
	setSuspTravelDirection(wheel,src.getSuspTravelDirection(srcWheel));
	setSuspForceAppPointOffset(wheel,src.getSuspForceAppPointOffset(srcWheel));
	setTireForceAppPointOffset(wheel,src.getTireForceAppPointOffset(srcWheel));
	setWheelCentreOffset(wheel,src.getWheelCentreOffset(srcWheel));
}

bool PxVehicleWheelsSimData::isValid() const
{
	for(PxU32 i=0;i<mNumWheels4-1;i++)
	{
		mWheels4SimData[i].isValid(0);
		mWheels4SimData[i].isValid(1);
		mWheels4SimData[i].isValid(2);
		mWheels4SimData[i].isValid(3);
	}
	const PxU32 numInLastBlock = 4 - (4*mNumWheels4 - mNumActiveWheels);
	for(PxU32 i=0;i<numInLastBlock;i++)
	{
		mWheels4SimData[mNumWheels4-1].isValid(i);
	}

	PX_CHECK_AND_RETURN_VAL(mNormalisedLoadFilter.isValid(), "Invalid PxVehicleWheelsSimData.mNormalisedLoadFilter", false);
	return true;
}

void PxVehicleWheelsSimData::disableWheel(const PxU32 wheel)
{
	PX_CHECK_AND_RETURN(wheel < 4*mNumWheels4, "PxVehicleWheelsSimData::disableWheel - Illegal wheel");

	//Set the wheel to lie at the centre of the chassis.
	setWheelCentreOffset(wheel,PxVec3(1e-5f,1e-5f,1e-5f));
	setTireForceAppPointOffset(wheel,PxVec3(1e-5f,1e-5f,1e-5f));
	setSuspForceAppPointOffset(wheel,PxVec3(1e-5f,1e-5f,1e-5f));

	//Remove the suspension spring.
	PxVehicleSuspensionData suspData=getSuspensionData(wheel);
	suspData.mMaxCompression=1e-5f;
	suspData.mMaxDroop=1e-5f;
	suspData.mSpringDamperRate=1e-5f;
	suspData.mSpringStrength=PX_DISABLED_SUSPENSION_SPRINTSTRENGTH;
	suspData.mSprungMass=PX_DISABLED_SUSPENSION_SPRUNGMASS;
	setSuspensionData(wheel,suspData);

	//Remove the wheel.
	PxVehicleWheelData wheelData=getWheelData(wheel);
	wheelData.mMaxSteer=0;
	wheelData.mRadius=1e-5f;
	wheelData.mMaxBrakeTorque=0;
	wheelData.mMaxHandBrakeTorque=0;
	wheelData.mToeAngle=0;
	wheelData.mWidth=1e-5f;
	wheelData.mMass=1e-5f;
	wheelData.mMOI=1e-5f;
	setWheelData(wheel,wheelData);
}

bool PxVehicleWheelsSimData::getIsWheelDisabled(const PxU32 wheel) const
{
	PX_CHECK_AND_RETURN_VAL(wheel < 4*mNumWheels4, "PxVehicleWheelsSimData::getIsWheelDisabled - Illegal wheel", false);
	const PxVehicleSuspensionData& suspData=getSuspensionData(wheel);
	return (PX_DISABLED_SUSPENSION_SPRUNGMASS==suspData.mSprungMass && PX_DISABLED_SUSPENSION_SPRINTSTRENGTH==suspData.mSpringStrength);
}

const PxVehicleSuspensionData& PxVehicleWheelsSimData::getSuspensionData(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getSuspensionData - Illegal wheel");
	return mWheels4SimData[id>>2].getSuspensionData(id & 3);
}

const PxVehicleWheelData& PxVehicleWheelsSimData::getWheelData(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getWheelData - Illegal wheel");
	return mWheels4SimData[id>>2].getWheelData(id & 3);
}

const PxVehicleTireData& PxVehicleWheelsSimData::getTireData(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getTireData - Illegal wheel");
	return mWheels4SimData[id>>2].getTireData(id & 3);
}

const PxVec3& PxVehicleWheelsSimData::getSuspTravelDirection(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getSuspTravelDirection - Illegal wheel");
	return mWheels4SimData[id>>2].getSuspTravelDirection(id & 3);
}

const PxVec3& PxVehicleWheelsSimData::getSuspForceAppPointOffset(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getSuspForceAppPointOffset - Illegal wheel");
	return mWheels4SimData[id>>2].getSuspForceAppPointOffset(id & 3);
}

const PxVec3& PxVehicleWheelsSimData::getTireForceAppPointOffset(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getTireForceAppPointOffset - Illegal wheel");
	return mWheels4SimData[id>>2].getTireForceAppPointOffset(id & 3);
}

const PxVec3& PxVehicleWheelsSimData::getWheelCentreOffset(const PxU32 id) const	
{
	PX_CHECK_MSG(id < 4*mNumWheels4, "PxVehicleWheelsSimData::getWheelCentreOffset - Illegal wheel");
	return mWheels4SimData[id>>2].getWheelCentreOffset(id & 3);
}

void PxVehicleWheelsSimData::setSuspensionData(const PxU32 id, const PxVehicleSuspensionData& susp)
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setSuspensionData - Illegal wheel");
	mWheels4SimData[id>>2].setSuspensionData(susp,id & 3);
}

void PxVehicleWheelsSimData::setWheelData(const PxU32 id, const PxVehicleWheelData& wheel)
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setWheelData - Illegal wheel");
	mWheels4SimData[id>>2].setWheelData(wheel,id & 3);
}

void PxVehicleWheelsSimData::setTireData(const PxU32 id, const PxVehicleTireData& tire)
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setTireData - Illegal wheel");
	mWheels4SimData[id>>2].setTireData(tire,id & 3);
}

void PxVehicleWheelsSimData::setSuspTravelDirection(const PxU32 id, const PxVec3& dir)										
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setSuspTravelDirection - Illegal wheel");
	mWheels4SimData[id>>2].setSuspTravelDirection(dir,id & 3);
}

void PxVehicleWheelsSimData::setSuspForceAppPointOffset(const PxU32 id, const PxVec3& offset)									
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setSuspForceAppPointOffset - Illegal wheel");
	mWheels4SimData[id>>2].setSuspForceAppPointOffset(offset,id & 3);
}

void PxVehicleWheelsSimData::setTireForceAppPointOffset(const PxU32 id, const PxVec3& offset)									
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setTireForceAppPointOffset - Illegal wheel");
	mWheels4SimData[id>>2].setTireForceAppPointOffset(offset,id & 3);
}

void PxVehicleWheelsSimData::setWheelCentreOffset(const PxU32 id, const PxVec3& offset)									
{
	PX_CHECK_AND_RETURN(id < 4*mNumWheels4, "PxVehicleWheelsSimData::setWheelCentreOffset - Illegal wheel");
	mWheels4SimData[id>>2].setWheelCentreOffset(offset,id & 3);
}

void PxVehicleWheelsSimData::setTireLoadFilterData(const PxVehicleTireLoadFilterData& tireLoadFilter)
{
	PX_CHECK_AND_RETURN(tireLoadFilter.mMaxNormalisedLoad>tireLoadFilter.mMinNormalisedLoad, "Illegal graph points");
	PX_CHECK_AND_RETURN(tireLoadFilter.mMaxFilteredNormalisedLoad>0, "Max filtered load must be greater than zero");
	mNormalisedLoadFilter=tireLoadFilter;
	mNormalisedLoadFilter.mDenominator=1.0f/(mNormalisedLoadFilter.mMaxNormalisedLoad-mNormalisedLoadFilter.mMinNormalisedLoad);
}

/////////////////////////////

bool PxVehicleWheelsDynData::isValid() const
{
	for(PxU32 i=0;i<mNumWheels4;i++)
	{
		PX_CHECK_AND_RETURN_VAL(mWheels4DynData[i].isValid(), "Invalid PxVehicleSuspWheelTireNDynData.mSuspWheelTire4DynData[i]", false);
	}
	return true;
}

void PxVehicleWheelsDynData::setToRestState()
{
	//Set susp/wheel/tires to rest state.
	const PxU32 numSuspWheelTire4=mNumWheels4;
	for(PxU32 i=0;i<numSuspWheelTire4;i++)
	{
		mWheels4DynData[i].mWheelSpeeds[0] = 0.0f;
		mWheels4DynData[i].mWheelSpeeds[1] = 0.0f;
		mWheels4DynData[i].mWheelSpeeds[2] = 0.0f;
		mWheels4DynData[i].mWheelSpeeds[3] = 0.0f;

		mWheels4DynData[i].mTireLowForwardSpeedTimers[0] = 0.0f;
		mWheels4DynData[i].mTireLowForwardSpeedTimers[1] = 0.0f;
		mWheels4DynData[i].mTireLowForwardSpeedTimers[2] = 0.0f;
		mWheels4DynData[i].mTireLowForwardSpeedTimers[3] = 0.0f;

		mWheels4DynData[i].mWheelRotationAngles[0] = 0.0f;
		mWheels4DynData[i].mWheelRotationAngles[1] = 0.0f;
		mWheels4DynData[i].mWheelRotationAngles[2] = 0.0f;
		mWheels4DynData[i].mWheelRotationAngles[3] = 0.0f;

		mWheels4DynData[i].mSteerAngles[0] = 0.0f;
		mWheels4DynData[i].mSteerAngles[1] = 0.0f;
		mWheels4DynData[i].mSteerAngles[2] = 0.0f;
		mWheels4DynData[i].mSteerAngles[3] = 0.0f;
	}
}

void PxVehicleWheelsDynData::setTireForceShaderFunction(PxVehicleComputeTireForce tireForceShaderFn)
{
	mTireForceCalculators->mShader=tireForceShaderFn;
}

void PxVehicleWheelsDynData::setTireForceShaderData(const PxU32 tireId, const void* tireForceShaderData)
{
	PX_CHECK_AND_RETURN(tireId < mNumActiveWheels, "PxVehicleWheelsDynData::setTireForceShaderData - Illegal tire");
	mTireForceCalculators->mShaderData[tireId]=tireForceShaderData;
}

PxReal PxVehicleWheelsDynData::getWheelRotationSpeed(const PxU32 wheelIdx) const
{
	PX_CHECK_AND_RETURN_VAL(wheelIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getWheelRotationSpeed - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(wheelIdx>>2)];
	return suspWheelTire4.mWheelSpeeds[wheelIdx & 3];
}

PxReal PxVehicleWheelsDynData::getWheelRotationAngle(const PxU32 wheelIdx) const
{
	PX_CHECK_AND_RETURN_VAL(wheelIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getWheelRotationAngle - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(wheelIdx>>2)];
	return suspWheelTire4.mWheelRotationAngles[wheelIdx & 3];
}

PxReal PxVehicleWheelsDynData::getSteer(const PxU32 wheelIdx) const
{
	PX_CHECK_AND_RETURN_VAL(wheelIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getSteer - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(wheelIdx>>2)];
	return suspWheelTire4.mSteerAngles[wheelIdx & 3];
}

PxReal PxVehicleWheelsDynData::getSuspJounce(const PxU32 suspIdx) const
{
	PX_CHECK_AND_RETURN_VAL(suspIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getSuspJounce - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(suspIdx>>2)];
	return suspWheelTire4.mSuspJounces[suspIdx & 3];
}

PxReal PxVehicleWheelsDynData::getTireLongSlip(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getTireLongSlip - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mLongSlips[tireIdx & 3];
}

PxReal PxVehicleWheelsDynData::getTireLatSlip(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getTireLatSlip - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mLatSlips[tireIdx & 3];
}

PxReal PxVehicleWheelsDynData::getTireFriction(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getTireFriction - Illegal wheel", 0.0f);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mTireFrictions[tireIdx & 3];
}

PxU32 PxVehicleWheelsDynData::getTireDrivableSurfaceType(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getTireDrivableSurfaceType - Illegal wheel", 0);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mTireSurfaceTypes[tireIdx & 3];
}

const PxMaterial* PxVehicleWheelsDynData::getTireDrivableSurfaceMaterial(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getTireDrivableSurfaceType - Illegal wheel", 0);
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mTireSurfaceMaterials[tireIdx & 3];
}

void PxVehicleWheelsDynData::getSuspRaycast(const PxU32 suspIdx, PxVec3& start, PxVec3& dir, PxReal& length) const
{
	PX_CHECK_AND_RETURN(suspIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getSuspRaycast - Illegal wheel");
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(suspIdx>>2)];
	const PxU32 idx=(suspIdx & 3);
	start=suspWheelTire4.mSuspLineStarts[idx];
	dir=suspWheelTire4.mSuspLineDirs[idx];
	length=suspWheelTire4.mSuspLineLengths[idx];
}

///////////////////////////////////

void PxVehicleWheels::setToRestState()
{
	//Set the rigid body to rest and clear all the accumulated forces and impulses.
	if(!(mActor->getRigidDynamicFlags() & PxRigidDynamicFlag::eKINEMATIC))
	{
		mActor->setLinearVelocity(PxVec3(0,0,0));
		mActor->setAngularVelocity(PxVec3(0,0,0));
		mActor->clearForce(PxForceMode::eFORCE);
		mActor->clearForce(PxForceMode::eIMPULSE);
		mActor->clearTorque(PxForceMode::eFORCE);
		mActor->clearTorque(PxForceMode::eIMPULSE);
	}

	//Set the wheels to rest state.
	mWheelsDynData.setToRestState();
}

bool PxVehicleWheels::isValid() const
{
	PX_CHECK_AND_RETURN_VAL(mWheelsSimData.isValid(), "invalid mWheelsSimData", false);
	PX_CHECK_AND_RETURN_VAL(mWheelsDynData.isValid(), "invalid mWheelsDynData", false);
	return true;
}

PxU32 PxVehicleWheels::computeByteSize(const PxU32 numWheels4)
{
	return 
		(sizeof(PxVehicleWheels4SimData)*numWheels4 +
		 sizeof(PxVehicleWheels4DynData)*numWheels4 +
		 sizeof(PxVehicleTireForceCalculator) + sizeof(void*)*4*numWheels4 +
		 sizeof(PxVehicleConstraintShader)*numWheels4);
}

PxU8* PxVehicleWheels::patchupPointers(PxVehicleWheels* veh, PxU8* ptr, const PxU32 numWheels4, const PxU32 numWheels)
{
	//Patchup pointers.
	veh->mWheelsSimData.mWheels4SimData = (PxVehicleWheels4SimData*)ptr;
	ptr += sizeof(PxVehicleWheels4SimData)*numWheels4;
	veh->mWheelsDynData.mWheels4DynData = (PxVehicleWheels4DynData*)ptr;
	ptr += sizeof(PxVehicleWheels4DynData)*numWheels4;
	veh->mWheelsDynData.mTireForceCalculators = (PxVehicleTireForceCalculator*)ptr;
	ptr += sizeof(PxVehicleTireForceCalculator);
	veh->mWheelsDynData.mTireForceCalculators->mShaderData = (const void**)ptr;
	ptr += sizeof(void*)*4*numWheels4;
	for(PxU32 i=0;i<numWheels4;i++)
	{
		veh->mWheelsDynData.mWheels4DynData[i].setVehicleConstraintShader((PxVehicleConstraintShader*)ptr);
		ptr += sizeof(PxVehicleConstraintShader);
	}

	//Placement new.
	for(PxU32 i=0;i<numWheels4;i++)
	{
		new(&veh->mWheelsSimData.mWheels4SimData[i]) PxVehicleWheels4SimData();
	}
	for(PxU32 i=0;i<numWheels4;i++)
	{
		new(&veh->mWheelsDynData.mWheels4DynData[i]) PxVehicleWheels4DynData();
	}
	new(veh->mWheelsDynData.mTireForceCalculators) PxVehicleTireForceCalculator;
	for(PxU32 i=0;i<numWheels4;i++)
	{
		new(&veh->mWheelsDynData.mWheels4DynData[i].getVehicletConstraintShader()) PxVehicleConstraintShader(veh);
	}
	new(&veh->mWheelsSimData.mNormalisedLoadFilter) PxVehicleTireLoadFilterData();

	//Set some data (could be done in placement new with an appropriate constructor but this is less hassle).
	veh->mWheelsSimData.mNumWheels4=numWheels4;
	veh->mWheelsDynData.mNumWheels4=numWheels4;
	veh->mWheelsSimData.mNumActiveWheels=numWheels;
	veh->mWheelsDynData.mNumActiveWheels=numWheels;
	veh->mOnConstraintReleaseCounter=numWheels4;

	//Set some more data.
	for(PxU32 i=0;i<numWheels;i++)
	{
		veh->mWheelShapeMap[i]=i;
	}
	for(PxU32 i=numWheels;i<PX_MAX_NUM_WHEELS;i++)
	{
		veh->mWheelShapeMap[i]=PX_MAX_U8;
	}

	return ptr;
}

void PxVehicleWheels::free()
{
	PX_CHECK_AND_RETURN(mWheelsSimData.mNumWheels4>0, "Cars with zero wheels are illegal");

	const PxU32 numSuspWheelTire4 = mWheelsSimData.mNumWheels4;

	for(PxU32 i=0;i<numSuspWheelTire4;i++)
	{
		mWheelsDynData.mWheels4DynData[i].getVehicletConstraintShader().release();
	}
}

void PxVehicleWheels::setup
(PxPhysics* physics, PxRigidDynamic* vehActor, 
 const PxVehicleWheelsSimData& wheelsData,
 const PxU32 numDrivenWheels, const PxU32 numNonDrivenWheels)
{
	PX_CHECK_AND_RETURN(wheelsData.getNumWheels() == mWheelsSimData.getNumWheels(), "PxVehicleWheels::setup - vehicle must be setup with same number of wheels as wheelsData");
	PX_CHECK_AND_RETURN(vehActor, "PxVehicleWheels::setup - vehActor is null ptr : you need to instantiate an empty PxRigidDynamic for the vehicle");
	PX_CHECK_AND_RETURN(vehActor->getNbShapes() >= (numDrivenWheels + numNonDrivenWheels + 1), "PxVehicleWheels::setup - not enough wheels shapes have been added to vehActor");

	PX_CHECK_AND_RETURN(wheelsData.isValid(), "PxVehicleWheels::setup -invalid wheelsData");

#ifdef PX_CHECKED
	PxF32 totalSprungMass=0.0f;
	for(PxU32 i=0;i<(numDrivenWheels+numNonDrivenWheels);i++)
	{
		totalSprungMass+=wheelsData.getSuspensionData(i).mSprungMass;
	}
	PX_CHECK_MSG(PxAbs((vehActor->getMass()-totalSprungMass)/vehActor->getMass()) < 0.01f, "Sum of suspension sprung masses doesn't match actor mass");
#endif

	//Copy the simulation data.
	mWheelsSimData=wheelsData;

	//Set the actor pointer.
	mActor=vehActor;

	//Pose the wheels from the wheels offset data.
	const PxTransform chassisCMOffset=vehActor->getCMassLocalPose();
	for(PxU32 i=0;i<wheelsData.mNumActiveWheels;i++)
	{
		if(mWheelShapeMap[i]!=PX_MAX_U8)
		{
			//Get the shape.
			const PxU32 shapeIndex=mWheelShapeMap[i];
			PxShape* shapeBuffer[1];
			vehActor->getShapes(shapeBuffer,1,shapeIndex);

			PxTransform wheelOffset=chassisCMOffset;
			wheelOffset.p+=wheelsData.getWheelCentreOffset(i);
			shapeBuffer[0]->setLocalPose(wheelOffset);
		}
	}

	//Set all the sq result ptrs to null.
	const PxU32 numSuspWheelTire4=wheelsData.mNumWheels4;
	for(PxU32 i=0;i<numSuspWheelTire4;i++)
	{
		mWheelsDynData.mWheels4DynData[i].mSqResults=NULL;
	}

	//Set up the suspension limits constraints.
	for(PxU32 i=0;i<numSuspWheelTire4;i++)
	{
		PxVehicleConstraintShader& shader=mWheelsDynData.mWheels4DynData[i].getVehicletConstraintShader();
		for(PxU32 j=0;j<4;j++)
		{
			shader.mData.mSuspLimitData.mCMOffsets[j]=wheelsData.mWheels4SimData[i].getSuspForceAppPointOffset(j);
			shader.mData.mSuspLimitData.mDirs[j]=wheelsData.mWheels4SimData[i].getSuspTravelDirection(j);
			shader.mData.mSuspLimitData.mErrors[j]=0.0f;
			shader.mData.mSuspLimitData.mActiveFlags[j]=false;

			shader.mData.mStickyTireData.mCMOffsets[j]=PxVec3(0,0,0);
			shader.mData.mStickyTireData.mDirs[j]=PxVec3(0,0,0);
			shader.mData.mStickyTireData.mTargetSpeeds[j]=0.0f;
			shader.mData.mStickyTireData.mActiveFlags[j]=false;
		}

#ifdef PX_PS3
		PxConstraintShaderTable t = 
		{ 
			PxVehicleConstraintShader::vehicleSuspLimitConstraintSolverPrep,
			PxVehicle4WSuspLimitConstraintShaderSpu,
			PXVEHICLE4WSUSPLIMITCONSTRAINTSHADERSPU_SIZE,
			0,
			PxVehicleConstraintShader::visualiseConstraint
		};
#else
		PxConstraintShaderTable t = 
		{ 
			PxVehicleConstraintShader::vehicleSuspLimitConstraintSolverPrep,
			0,
			0,
			0,
			PxVehicleConstraintShader::visualiseConstraint
		};
#endif

		shader.mConstraint=physics->createConstraint(vehActor, NULL, shader, t, sizeof(PxVehicleConstraintShader::VehicleConstraintData));
		shader.mConstraint->markDirty();
	}

	//Set up the shader data ptrs.
	for(PxU32 i=0;i<wheelsData.mNumActiveWheels;i++)
	{
		mWheelsDynData.setTireForceShaderData(i,&mWheelsSimData.getTireData(i));
	}

	//Disable the unused wheels.
	for(PxU32 i=wheelsData.mNumActiveWheels;i<4*mWheelsSimData.mNumWheels4;i++)
	{
		mWheelsSimData.disableWheel(i);
	}
}

bool PxVehicleWheels::isInAir() const
{
	for(PxU32 i=0;i<mWheelsSimData.getNumWheels();i++)
	{
		if(!isInAir(i))
		{
			return false;
		}
	}
	return true;
}

bool PxVehicleWheels::isInAir(const PxU32 wheelId) const
{
	const PxF32 jounce=mWheelsDynData.getSuspJounce(wheelId);
	const PxF32 maxDroop=mWheelsSimData.getSuspensionData(wheelId).mMaxDroop;
	return (jounce==-maxDroop);
}

void PxVehicleWheels::setWheelShapeMapping(const PxU32 wheelId, const PxI32 shapeId)
{
	PX_CHECK_AND_RETURN(wheelId<mWheelsSimData.mNumActiveWheels, "Illegal wheelId");
	PX_CHECK_AND_RETURN((-1==shapeId) || (shapeId < PX_MAX_NUM_WHEELS), "Illegal shapeId");
	mWheelShapeMap[wheelId]=(-1==shapeId) ? PX_MAX_U8 : shapeId;
}

///////////////////////////////////

PxReal PxVehicleWheels::computeForwardSpeed() const
{
	const PxTransform vehicleChassisTrnsfm=mActor->getGlobalPose().transform(mActor->getCMassLocalPose());
	return mActor->getLinearVelocity().dot(vehicleChassisTrnsfm.q.rotate(gForward));
}

///////////////////////////////////

PxReal PxVehicleWheels::computeSidewaysSpeed() const
{
	const PxTransform vehicleChassisTrnsfm=mActor->getGlobalPose().transform(mActor->getCMassLocalPose());
	return mActor->getLinearVelocity().dot(vehicleChassisTrnsfm.q.rotate(gRight));
}

PxVec3 PxVehicleWheelsDynData::getSuspLineStart(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getSuspLineStarts - Illegal wheel", PxVec3(0,0,0));
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mSuspLineStarts[tireIdx & 3];
}

PxVec3 PxVehicleWheelsDynData::getSuspLineDir(const PxU32 tireIdx) const
{
	PX_CHECK_AND_RETURN_VAL(tireIdx < mNumActiveWheels, "PxVehicleWheelsDynData::getSuspLineDirs - Illegal wheel", PxVec3(0,0,0));
	const PxVehicleWheels4DynData& suspWheelTire4=mWheels4DynData[(tireIdx>>2)];
	return suspWheelTire4.mSuspLineDirs[tireIdx & 3];
}


///////////////////////////////////

} //namespace physx

