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

#include "PxVehicleUtilTelemetry.h"
#include "PxVehicleSDK.h"
#include "PsFoundation.h"
#include "PsUtilities.h"
#include "stdio.h"
#include "CmPhysXCommon.h"

namespace physx
{

#if PX_DEBUG_VEHICLE_ON

PxVehicleGraphDesc::PxVehicleGraphDesc()
:	mPosX(PX_MAX_F32),
	mPosY(PX_MAX_F32),
	mSizeX(PX_MAX_F32),
	mSizeY(PX_MAX_F32),
	mBackgroundColor(PxVec3(PX_MAX_F32,PX_MAX_F32,PX_MAX_F32)),
	mAlpha(PX_MAX_F32)
{
}

bool PxVehicleGraphDesc::isValid() const
{
	PX_CHECK_AND_RETURN_VAL(mPosX != PX_MAX_F32, "PxVehicleGraphDesc.mPosX must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mPosY != PX_MAX_F32, "PxVehicleGraphDesc.mPosY must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mSizeX != PX_MAX_F32, "PxVehicleGraphDesc.mSizeX must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mSizeY != PX_MAX_F32, "PxVehicleGraphDesc.mSizeY must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mBackgroundColor.x != PX_MAX_F32 && mBackgroundColor.y != PX_MAX_F32 && mBackgroundColor.z != PX_MAX_F32, "PxVehicleGraphDesc.mBackgroundColor must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mAlpha != PX_MAX_F32, "PxVehicleGraphDesc.mAlpha must be initialised", false);
	return true;
}

PxVehicleGraphChannelDesc::PxVehicleGraphChannelDesc()
:	mMinY(PX_MAX_F32),
	mMaxY(PX_MAX_F32),
	mMidY(PX_MAX_F32),
	mColorLow(PxVec3(PX_MAX_F32,PX_MAX_F32,PX_MAX_F32)),
	mColorHigh(PxVec3(PX_MAX_F32,PX_MAX_F32,PX_MAX_F32)),
	mTitle(NULL)
{
}

bool PxVehicleGraphChannelDesc::isValid() const
{
	PX_CHECK_AND_RETURN_VAL(mMinY != PX_MAX_F32, "PxVehicleGraphChannelDesc.mMinY must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mMaxY != PX_MAX_F32, "PxVehicleGraphChannelDesc.mMaxY must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mMidY != PX_MAX_F32, "PxVehicleGraphChannelDesc.mMidY must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mColorLow.x != PX_MAX_F32 && mColorLow.y != PX_MAX_F32 && mColorLow.z != PX_MAX_F32, "PxVehicleGraphChannelDesc.mColorLow must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mColorHigh.x != PX_MAX_F32 && mColorHigh.y != PX_MAX_F32 && mColorHigh.z != PX_MAX_F32, "PxVehicleGraphChannelDesc.mColorHigh must be initialised", false);
	PX_CHECK_AND_RETURN_VAL(mTitle, "PxVehicleGraphChannelDesc.mTitle must be initialised", false);
	return true;
}

PxVehicleGraph::PxVehicleGraph()
{
	mBackgroundMinX=0;
	mBackgroundMaxX=0;
	mBackgroundMinY=0;
	mBackgroundMaxY=0;
	mSampleTide=0;
	mBackgroundColor=PxVec3(255,255,255),
		mBackgroundAlpha=1.0f;
	for(PxU32 i=0;i<eMAX_NUM_CHANNELS;i++)
	{
		mChannelMinY[i]=0;
		mChannelMaxY[i]=0;
		mChannelMidY[i]=0;
		mChannelColorLow[i]=PxVec3(0,0,255);
		mChannelColorHigh[i]=PxVec3(255,0,0);
		memset(mChannelSamples[i], 0, sizeof(PxReal)*eMAX_NUM_SAMPLES);
	}
	mNumChannels = 0;
	PX_COMPILE_TIME_ASSERT((size_t)eMAX_NUM_CHANNELS >= (size_t)eMAX_NUM_ENGINE_CHANNELS && (size_t)eMAX_NUM_CHANNELS >= (size_t)eMAX_NUM_WHEEL_CHANNELS);
}

PxVehicleGraph::~PxVehicleGraph()
{
}

void PxVehicleGraph::setup(const PxVehicleGraphDesc& desc, const eGraphType graphType)
{
	mBackgroundMinX = (desc.mPosX - 0.5f*desc.mSizeX);
	mBackgroundMaxX = (desc.mPosX + 0.5f*desc.mSizeX);
	mBackgroundMinY = (desc.mPosY - 0.5f*desc.mSizeY);
	mBackgroundMaxY = (desc.mPosY + 0.5f*desc.mSizeY);

	mBackgroundColor=desc.mBackgroundColor;
	mBackgroundAlpha=desc.mAlpha;

	mNumChannels = (eGRAPH_TYPE_WHEEL==graphType) ? (PxU32)eMAX_NUM_WHEEL_CHANNELS : (PxU32)eMAX_NUM_ENGINE_CHANNELS;
}

void PxVehicleGraph::setChannel(PxVehicleGraphChannelDesc& desc, const PxU32 channel)
{
	PX_ASSERT(channel<eMAX_NUM_CHANNELS);

	mChannelMinY[channel]=desc.mMinY;
	mChannelMaxY[channel]=desc.mMaxY;
	mChannelMidY[channel]=desc.mMidY;
	PX_CHECK_MSG(mChannelMinY[channel]<=mChannelMidY[channel], "mChannelMinY must be less than or equal to mChannelMidY");
	PX_CHECK_MSG(mChannelMidY[channel]<=mChannelMaxY[channel], "mChannelMidY must be less than or equal to mChannelMaxY");

	mChannelColorLow[channel]=desc.mColorLow;
	mChannelColorHigh[channel]=desc.mColorHigh;

	strcpy(mChannelTitle[channel], desc.mTitle);
}

void PxVehicleGraph::clearRecordedChannelData()
{
	mSampleTide=0;
	for(PxU32 i=0;i<eMAX_NUM_CHANNELS;i++)
	{
		memset(mChannelSamples[i], 0, sizeof(PxReal)*eMAX_NUM_SAMPLES);
	}
}

void PxVehicleGraph::updateTimeSlice(const PxReal* const samples)
{
	mSampleTide++;
	mSampleTide=mSampleTide%eMAX_NUM_SAMPLES;

	for(PxU32 i=0;i<mNumChannels;i++)
	{
		mChannelSamples[i][mSampleTide]=PxClamp(samples[i],mChannelMinY[i],mChannelMaxY[i]);
	}
}

void PxVehicleGraph::computeGraphChannel(const PxU32 channel, PxReal* xy, PxVec3* colors, char* title) const
{
	PX_ASSERT(channel<mNumChannels);
	const PxReal sizeX=mBackgroundMaxX-mBackgroundMinX;
	const PxReal sizeY=mBackgroundMaxY-mBackgroundMinY;

	for(PxU32 i=0;i<PxVehicleGraph::eMAX_NUM_SAMPLES;i++)
	{
		const PxU32 index=(mSampleTide+1+i)%PxVehicleGraph::eMAX_NUM_SAMPLES;
		xy[2*i+0]=mBackgroundMinX+sizeX*i/((PxReal)(PxVehicleGraph::eMAX_NUM_SAMPLES));
		const PxReal y=(mChannelSamples[channel][index]-mChannelMinY[channel])/(mChannelMaxY[channel]-mChannelMinY[channel]);		
		xy[2*i+1]=mBackgroundMinY+sizeY*y;
		colors[i]=mChannelSamples[channel][index]<mChannelMidY[channel] ? mChannelColorLow[channel] : mChannelColorHigh[channel];
	}

	strcpy(title,mChannelTitle[channel]);
}

void PxVehicleGraph::setupEngineGraph
(const PxF32 sizeX, const PxF32 sizeY, const PxF32 posX, const PxF32 posY, 
 const PxVec3& backgoundColor, const PxVec3& lineColorHigh, const PxVec3& lineColorLow)
{
	PxVehicleGraphDesc desc;
	desc.mSizeX=sizeX;
	desc.mSizeY=sizeY;
	desc.mPosX=posX;
	desc.mPosY=posY;
	desc.mBackgroundColor=backgoundColor;
	desc.mAlpha=0.5f;
	setup(desc,PxVehicleGraph::eGRAPH_TYPE_ENGINE);

	//Engine revs
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=800.0f;
		desc2.mMidY=400.0f;
		char title[64];
		sprintf(title, "engineRevs");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_ENGINE_REVS);
	}

	//Engine torque
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=1000.0f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "engineDriveTorque");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_ENGINE_DRIVE_TORQUE);
	}

	//Clutch slip
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-200.0f;
		desc2.mMaxY=200.0f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "clutchSlip");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_CLUTCH_SLIP);
	}

	//Accel control
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=1.1f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "accel");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_ACCEL_CONTROL);
	}

	//Brake control
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=1.1f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "brake/tank brake left");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_BRAKE_CONTROL);
	}

	//HandBrake control
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=1.1f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "handbrake/tank brake right");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_HANDBRAKE_CONTROL);
	}

	//Steer control
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-1.1f;
		desc2.mMaxY=1.1f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "steerLeft/tank thrust left");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_STEER_LEFT_CONTROL);
	}

	//Steer control
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-1.1f;
		desc2.mMaxY=1.1f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "steerRight/tank thrust right");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_STEER_RIGHT_CONTROL);
	}

	//Gear
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-4.f;
		desc2.mMaxY=20.f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "gearRatio");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_GEAR_RATIO);
	}
}

void PxVehicleGraph::setupWheelGraph
	(const PxF32 sizeX, const PxF32 sizeY, const PxF32 posX, const PxF32 posY, 
	const PxVec3& backgoundColor, const PxVec3& lineColorHigh, const PxVec3& lineColorLow)
{
	PxVehicleGraphDesc desc;
	desc.mSizeX=sizeX;
	desc.mSizeY=sizeY;
	desc.mPosX=posX;
	desc.mPosY=posY;
	desc.mBackgroundColor=backgoundColor;
	desc.mAlpha=0.5f;
	setup(desc,PxVehicleGraph::eGRAPH_TYPE_WHEEL);

	//Jounce data channel
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-0.2f;
		desc2.mMaxY=0.4f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "suspJounce");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_JOUNCE);
	}

	//Jounce susp force channel
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=20000.0f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "suspForce");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_SUSPFORCE);
	}

	//Tire load channel.
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=20000.0f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "tireLoad");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_TIRELOAD);
	}

	//Normalised tire load channel.
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=3.0f;
		desc2.mMidY=1.0f;
		char title[64];
		sprintf(title, "normTireLoad");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_NORMALIZED_TIRELOAD);
	}

	//Wheel omega channel
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-50.0f;
		desc2.mMaxY=250.0f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "wheelOmega");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_WHEEL_OMEGA);
	}

	//Tire friction
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=1.1f;
		desc2.mMidY=1.0f;
		char title[64];
		sprintf(title, "friction");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_TIRE_FRICTION);
	}


	//Tire long slip
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-0.2f;
		desc2.mMaxY=0.2f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "tireLongSlip");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_TIRE_LONG_SLIP);
	}

	//Normalised tire long force
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=2.0f;
		desc2.mMidY=1.0f;
		char title[64];
		sprintf(title, "normTireLongForce");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_NORM_TIRE_LONG_FORCE);
	}

	//Tire lat slip
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=-1.0f;
		desc2.mMaxY=1.0f;
		desc2.mMidY=0.0f;
		char title[64];
		sprintf(title, "tireLatSlip");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_TIRE_LAT_SLIP);
	}

	//Normalised tire lat force
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=2.0f;
		desc2.mMidY=1.0f;
		char title[64];
		sprintf(title, "normTireLatForce");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_NORM_TIRE_LAT_FORCE);
	}

	//Normalized aligning moment
	{
		PxVehicleGraphChannelDesc desc2;
		desc2.mColorHigh=lineColorHigh;
		desc2.mColorLow=lineColorLow;
		desc2.mMinY=0.0f;
		desc2.mMaxY=2.0f;
		desc2.mMidY=1.0f;
		char title[64];
		sprintf(title, "normTireAlignMoment");
		desc2.mTitle=title;
		setChannel(desc2,PxVehicleGraph::eCHANNEL_NORM_TIRE_ALIGNING_MOMENT);
	}
}

PxVehicleTelemetryData* physx::PxVehicleTelemetryData::allocate(const PxU32 numWheels)
{
	//Work out the byte size required.
	PxU32 size = sizeof(PxVehicleTelemetryData);
	size += sizeof(PxVehicleGraph);					//engine graph
	size += sizeof(PxVehicleGraph)*numWheels;		//wheel graphs
	size += sizeof(PxVec3)*numWheels;				//tire force app points
	size += sizeof(PxVec3)*numWheels;				//susp force app points

	//Allocate the memory.
	PxVehicleTelemetryData* vehTelData=(PxVehicleTelemetryData*)PX_ALLOC(size, PX_DEBUG_EXP("PxVehicleNWTelemetryData"));

	//Patch up the pointers.
	PxU8* ptr = (PxU8*)vehTelData + sizeof(PxVehicleTelemetryData);
	vehTelData->mEngineGraph = (PxVehicleGraph*)ptr;
	ptr += sizeof(PxVehicleGraph);			
	vehTelData->mWheelGraphs = (PxVehicleGraph*)ptr;
	ptr += sizeof(PxVehicleGraph)*numWheels;	
	vehTelData->mSuspforceAppPoints = (PxVec3*)ptr;
	ptr += sizeof(PxVec3)*numWheels;	
	vehTelData->mTireforceAppPoints = (PxVec3*)ptr;
	ptr += sizeof(PxVec3)*numWheels;	

	//Set the number of wheels in each structure that needs it.
	vehTelData->mNumActiveWheels=numWheels;

	//Finished.
	return vehTelData;
}

void PxVehicleTelemetryData::free()
{
	PX_FREE(this);
}

void physx::PxVehicleTelemetryData::setup
(const PxF32 graphSizeX, const PxF32 graphSizeY,
const PxF32 engineGraphPosX, const PxF32 engineGraphPosY,
const PxF32* const wheelGraphPosX, const PxF32* const wheelGraphPosY,
const PxVec3& backgroundColor, const PxVec3& lineColorHigh, const PxVec3& lineColorLow)
{
	mEngineGraph->setupEngineGraph
		(graphSizeX, graphSizeY, engineGraphPosX, engineGraphPosY, 
		backgroundColor, lineColorHigh, lineColorLow);

	const PxU32 numActiveWheels=mNumActiveWheels;
	for(PxU32 k=0;k<numActiveWheels;k++)
	{
		mWheelGraphs[k].setupWheelGraph
			(graphSizeX, graphSizeY, wheelGraphPosX[k], wheelGraphPosY[k], 
			 backgroundColor, lineColorHigh, lineColorLow);

		mTireforceAppPoints[k]=PxVec3(0,0,0);
		mSuspforceAppPoints[k]=PxVec3(0,0,0);
	}
}

void physx::PxVehicleTelemetryData::clear()
{
	mEngineGraph->clearRecordedChannelData();

	const PxU32 numActiveWheels=mNumActiveWheels;
	for(PxU32 k=0;k<numActiveWheels;k++)
	{
		mWheelGraphs[k].clearRecordedChannelData();
		mTireforceAppPoints[k]=PxVec3(0,0,0);
		mSuspforceAppPoints[k]=PxVec3(0,0,0);
	}
}

#endif //PX_DEBUG_VEHICLE_ON

} //physx




