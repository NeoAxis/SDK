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

#include "PxVehicleUpdate.h"
#include "PxVehicleSDK.h"
#include "PxVehicleSuspWheelTire4.h"
#include "PxVehicleDrive4W.h"
#include "PxVehicleDriveTank.h"
#include "PxVehicleSuspLimitConstraintShader.h"
#include "PxVehicleDefaults.h"
#include "PxVehicleTireFriction.h"
#include "PxVehicleUtilTelemetry.h"
#include "PxQuat.h"
#include "PxShape.h"
#include "PxRigidDynamic.h"
#include "PxBatchQuery.h"
#include "PxHeightField.h"
#include "PxTriangleMesh.h"
#include "PxHeightFieldGeometry.h"
#include "PxTriangleMeshGeometry.h"
#include "PxConstraint.h"
#include "PxMaterial.h"
#include "PxTolerancesScale.h"
#include "PxRigidBodyExt.h"
#include "PsFoundation.h"
#include "PsUtilities.h"
#include "CmBitMap.h"

#if defined (PX_PSP2)
#include <stdint.h> // intptr_t
#endif

using namespace physx;

//TODO: lsd - handle case where wheels are spinning in different directions.
//TODO: ackermann - use faster approximate functions for PxTan/PxATan because the results don't need to be too accurate here.
//TODO: tire lat slip - do we really use PxAbs(vz) as denominator, that's not in the paper?
//TODO: tire camber angle and camber vs jounce table
//TODO: toe vs jounce table.
//TODO: pneumatic trail.
//TODO: maybe just set the car wheel omega to the blended value?
//TODO: multi-stepping only needs a single pass at working out the jounce.
//TODO: we probably need to have a graphics jounce and a physics jounce and 
//TODO: expose sticky friction values in api.
//TODO: blend the graphics jounce towards the physics jounce to avoid graphical pops at kerbs etc.
//TODO: better graph of friction vs slip.  Need to account for negative slip and positive slip differences.

namespace physx
{

PxVec3 gRight(1,0,0);
PxVec3 gUp(0,1,0);
PxVec3 gForward(0,0,1);

void PxVehicleSetBasisVectors(const PxVec3& up, const PxVec3& forward)
{
	gRight=up.cross(forward);
	gUp=up;
	gForward=forward;
}

PxF32 gThresholdForwardSpeedForWheelAngleIntegration=0;
PxF32 gRecipThresholdForwardSpeedForWheelAngleIntegration=0;
PxF32 gMinLongSpeedForTireModel=0;
PxF32 gRecipMinLongSpeedForTireModel=0;
PxF32 gMinLatSpeedForTireModel=0;
PxF32 gStickyTireFrictionThresholdSpeed=0;

void setVehicleToleranceScale(const PxTolerancesScale& ts)
{
	gThresholdForwardSpeedForWheelAngleIntegration=5.0f*ts.length;
	gRecipThresholdForwardSpeedForWheelAngleIntegration=1.0f/gThresholdForwardSpeedForWheelAngleIntegration;

	gMinLongSpeedForTireModel=0.2f*ts.length;
	gRecipMinLongSpeedForTireModel=1.0f/gMinLongSpeedForTireModel;

	gMinLatSpeedForTireModel = 1.0f*ts.length;

	gStickyTireFrictionThresholdSpeed=0.2f*ts.length;
}

void resetVehicleToleranceScale()
{
	gThresholdForwardSpeedForWheelAngleIntegration=0;
	gRecipThresholdForwardSpeedForWheelAngleIntegration=0;

	gMinLongSpeedForTireModel=0;
	gRecipMinLongSpeedForTireModel=0;

	gMinLatSpeedForTireModel = 0;

	gStickyTireFrictionThresholdSpeed=0;
}

const PxF32 gStickyTireFrictionDamping=0.01f;
const PxF32 gLowForwardSpeedThresholdTime=1.0f;

#define PX_MAX_NUM_SUSPWHEELTIRE4 (PX_MAX_NUM_WHEELS >>2)

#if PX_DEBUG_VEHICLE_ON

//Render data.
PxVec3* gCarTireForceAppPoints=NULL;
PxVec3* gCarSuspForceAppPoints=NULL;

//Graph data
PxF32* gCarWheelGraphData[PX_MAX_NUM_WHEELS]={NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL};
PxF32* gCarEngineGraphData=NULL;

PX_FORCE_INLINE void updateGraphDataInternalWheelDynamics(const PxU32 startIndex, const PxF32* carWheelSpeeds)
{
	//Grab the internal rotation speeds for graphing before we update them.
	if(gCarWheelGraphData[startIndex])
	{
		for(PxU32 i=0;i<4;i++)
		{
			PX_ASSERT((startIndex+i) < PX_MAX_NUM_WHEELS);
			PX_ASSERT(gCarWheelGraphData[startIndex+i]);
			gCarWheelGraphData[startIndex+i][PxVehicleGraph::eCHANNEL_WHEEL_OMEGA]=carWheelSpeeds[i];
		}
	}
}

PX_FORCE_INLINE void updateGraphDataInternalEngineDynamics(const PxF32 carEngineSpeed)
{
	if(gCarEngineGraphData)
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_ENGINE_REVS]=carEngineSpeed;
}

PX_FORCE_INLINE void updateGraphDataControlInputs(const PxF32 accel, const PxF32 brake, const PxF32 handbrake, const PxF32 steerLeft, const PxF32 steerRight)
{
	if(gCarEngineGraphData)
	{
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_ACCEL_CONTROL]=accel;
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_BRAKE_CONTROL]=brake;
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_HANDBRAKE_CONTROL]=handbrake;
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_STEER_LEFT_CONTROL]=steerLeft;
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_STEER_RIGHT_CONTROL]=steerRight;
	}
}
PX_FORCE_INLINE void updateGraphDataGearRatio(const PxF32 G)
{
	if(gCarEngineGraphData)
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_GEAR_RATIO]=G;
}
PX_FORCE_INLINE void updateGraphDataEngineDriveTorque(const PxF32 engineDriveTorque)
{
	if(gCarEngineGraphData)
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_ENGINE_DRIVE_TORQUE]=engineDriveTorque;
}
PX_FORCE_INLINE void updateGraphDataClutchSlip(const PxF32* wheelSpeeds, const PxF32* aveWheelSpeedContributions, const PxF32 engineSpeed, const PxF32 G)
{
	if(gCarEngineGraphData)
	{
		PxF32 averageWheelSpeed=0;
		for(PxU32 i=0;i<4;i++)
		{
			averageWheelSpeed+=wheelSpeeds[i]*aveWheelSpeedContributions[i];
		}
		averageWheelSpeed*=G;
		gCarEngineGraphData[PxVehicleGraph::eCHANNEL_CLUTCH_SLIP]=averageWheelSpeed-engineSpeed;
	}
}

PX_FORCE_INLINE void zeroGraphDataWheels(const PxU32 startIndex, const PxU32 type)
{
	if(gCarWheelGraphData[0])
	{
		for(PxU32 i=0;i<4;i++)
		{
			PX_ASSERT((startIndex+i) < PX_MAX_NUM_WHEELS);
			PX_ASSERT(gCarWheelGraphData[startIndex+i]);
			gCarWheelGraphData[startIndex+i][type]=0.0f;
		}
	}
}
PX_FORCE_INLINE void updateGraphDataSuspJounce(const PxU32 startIndex, const PxU32 wheel, const PxF32 jounce)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_JOUNCE]=jounce;
	}
}
PX_FORCE_INLINE void updateGraphDataSuspForce(const PxU32 startIndex, const PxU32 wheel, const PxF32 springForce)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_SUSPFORCE]=springForce;
	}
}
PX_FORCE_INLINE void updateGraphDataTireLoad(const PxU32 startIndex, const PxU32 wheel, const PxF32 filteredTireLoad)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_TIRELOAD]=filteredTireLoad;
	}
}
PX_FORCE_INLINE void updateGraphDataNormTireLoad(const PxU32 startIndex, const PxU32 wheel, const PxF32 filteredNormalisedTireLoad)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_NORMALIZED_TIRELOAD]=filteredNormalisedTireLoad;
	}
}
PX_FORCE_INLINE void updateGraphDataNormLongTireForce(const PxU32 startIndex, const PxU32 wheel, const PxF32 normForce)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_NORM_TIRE_LONG_FORCE]=normForce;
	}
}
PX_FORCE_INLINE void updateGraphDataNormLatTireForce(const PxU32 startIndex, const PxU32 wheel, const PxF32 normForce)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_NORM_TIRE_LAT_FORCE]=normForce;
	}
}
PX_FORCE_INLINE void updateGraphDataLatTireSlip(const PxU32 startIndex, const PxU32 wheel, const PxF32 latSlip)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_TIRE_LAT_SLIP]=latSlip;
	}
}
PX_FORCE_INLINE void updateGraphDataLongTireSlip(const PxU32 startIndex, const PxU32 wheel, const PxF32 longSlip)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_TIRE_LONG_SLIP]=longSlip;
	}
}
PX_FORCE_INLINE void updateGraphDataTireFriction(const PxU32 startIndex, const PxU32 wheel, const PxF32 friction)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_TIRE_FRICTION]=friction;
	}
}
PX_FORCE_INLINE void updateGraphDataNormTireAligningMoment(const PxU32 startIndex, const PxU32 wheel, const PxF32 normAlignMoment)
{
	if(gCarWheelGraphData[0])
	{
		PX_ASSERT((startIndex+wheel) < PX_MAX_NUM_WHEELS);
		PX_ASSERT(gCarWheelGraphData[startIndex+wheel]);
		gCarWheelGraphData[startIndex+wheel][PxVehicleGraph::eCHANNEL_NORM_TIRE_ALIGNING_MOMENT]=normAlignMoment;
	}
}

#endif //DEBUG_VEHICLE_ON

//It would be great to use PsHashSet for the hash table of PxMaterials but
//PsHashSet will never, ever work on spu so this will need to do instead.
//Perf isn't really critical so this will do in the meantime.
//It is probably wasteful to compute the hash table each update
//but this is really not an expensive operation so keeping the api as 
//simple as possible wins out at the cost of a relatively very small number of wasted cycles.
class VehicleSurfaceTypeHashTable
{
public:

	VehicleSurfaceTypeHashTable(const PxVehicleDrivableSurfaceToTireFrictionPairs& pairs)
		: mNumEntries(pairs.mNumSurfaceTypes),
 	      mMaterials(pairs.mDrivableSurfaceMaterials),
	      mDrivableSurfaceTypes(pairs.mDrivableSurfaceTypes)
	{
		for(PxU32 i=0;i<eHASH_SIZE;i++)
		{
			mHeadIds[i]=PX_MAX_U32;
		}
		for(PxU32 i=0;i<eMAX_NUM_KEYS;i++)
		{
			mNextIds[i]=PX_MAX_U32;
		}

		if(mNumEntries>0)
		{
			//Compute the number of bits to right-shift that gives the maximum number of unique hashes.
			//Keep searching until we find either a set of completely unique hashes or a peak count of unique hashes.
			PxU32 prevShift=0;
			PxU32 shift=2;
			PxU32 prevNumUniqueHashes=0;
			PxU32 currNumUniqueHashes=0;
			while( ((currNumUniqueHashes=computeNumUniqueHashes(shift)) > prevNumUniqueHashes) && currNumUniqueHashes!=mNumEntries)
			{
				prevNumUniqueHashes=currNumUniqueHashes;
				prevShift=shift;
				shift = (shift << 1);
			}
			if(currNumUniqueHashes!=mNumEntries)
			{
				//Stopped searching because we have gone past the peak number of unqiue hashes.
				mShift = prevShift;
			}
			else
			{
				//Stopped searching because we found a unique hash for each key.
				mShift = shift;
			}

			//Compute the hash values with the optimum shift.
			for(PxU32 i=0;i<mNumEntries;i++)
			{
				const PxMaterial* const material=mMaterials[i];
				const PxU32 hash=computeHash(material,mShift);
				if(PX_MAX_U32==mHeadIds[hash])
				{
					mNextIds[i]=PX_MAX_U32;
					mHeadIds[hash]=i;
				}
				else
				{
					mNextIds[i]=mHeadIds[hash];
					mHeadIds[hash]=i;
				}
			}
		}
	}
	~VehicleSurfaceTypeHashTable()
	{
	}

	PX_FORCE_INLINE PxU32 get(const PxMaterial* const key) const 
	{
		PX_ASSERT(key);
		const PxU32 hash=computeHash(key, mShift);
		PxU32 id=mHeadIds[hash];
		while(PX_MAX_U32!=id)
		{
			const PxMaterial* const mat=mMaterials[id];
			if(key==mat)
			{
				return mDrivableSurfaceTypes[id].mType;
			}
			id=mNextIds[id];
		}

		PX_ASSERT(false);
		return 0;
	}

private:

	PxU32 mNumEntries; 
	const PxMaterial* const* mMaterials;
	const PxVehicleDrivableSurfaceType* mDrivableSurfaceTypes;

	static PX_FORCE_INLINE PxU32 computeHash(const PxMaterial* const key, const PxU32 shift) 
	{
		const uintptr_t ptr = (((uintptr_t) key) >> shift);
		const uintptr_t hash = (ptr & (eHASH_SIZE-1));
		return (PxU32)hash;
	}

	PxU32 computeNumUniqueHashes(const PxU32 shift) const
	{
		PxU32 words[eHASH_SIZE >>5];
		PxU8* bitmapBuffer[sizeof(Cm::BitMap)];
		Cm::BitMap* bitmap=(Cm::BitMap*)bitmapBuffer;
		bitmap->setWords(words, eHASH_SIZE >>5);

		PxU32 numUniqueHashes=0;
		Ps::memZero(words, sizeof(PxU32)*(eHASH_SIZE >>5));
		for(PxU32 i=0;i<mNumEntries;i++)
		{
			const PxMaterial* const material=mMaterials[i];
			const PxU32 hash=computeHash(material, shift);
			if(!bitmap->test(hash))
			{
				bitmap->set(hash);
				numUniqueHashes++;
			}
		}
		return numUniqueHashes;
	}

	enum
	{
		eHASH_SIZE=PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES
	};
	PxU32 mHeadIds[eHASH_SIZE];
	enum
	{
		eMAX_NUM_KEYS=PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES
	};
	PxU32 mNextIds[eMAX_NUM_KEYS];

	PxU32 mShift;
};

class PxVehicleTireForceCalculator4
{
public:

	const void* mShaderData[4];
	PxVehicleComputeTireForce mShader;
private:
};

PX_FORCE_INLINE void processAutoBox(const PxF32 timestep, const PxVehicleDriveSimData& vehCoreSimData, PxVehicleDriveDynData& vehCore)
{
	PX_ASSERT(vehCore.mUseAutoGears);

	//If still undergoing a gear change triggered by the autobox 
	//then turn off the accelerator pedal.  This happens in autoboxes
	//to stop the driver revving the engine crazily then damaging the 
	//clutch when the clutch re-engages at the end of the gear change.
	const PxU32 currentGear=vehCore.mCurrentGear;
	const PxU32 targetGear=vehCore.mTargetGear;
	if(targetGear!=currentGear && PxVehicleGearsData::eNEUTRAL==currentGear)
	{
		vehCore.mControlAnalogVals[PxVehicleDriveDynData::eANALOG_INPUT_ACCEL]=0.0f;
	}

	//Only process the autobox if no gear change is underway and the time passed since the last autobox
	//gear change is greater than the autobox latency.
	PxF32 autoBoxSwitchTime=vehCore.mAutoBoxSwitchTime;
	const PxF32 autoBoxLatencyTime=vehCoreSimData.getAutoBoxData().mDownRatios[PxVehicleGearsData::eREVERSE];
	if(targetGear==currentGear && autoBoxSwitchTime > autoBoxLatencyTime)
	{
		//Work out if the autobox wants to switch up or down.
		const PxF32 normalisedEngineOmega=vehCore.mEnginespeed*vehCoreSimData.getEngineData().getRecipMaxOmega();
		const PxVehicleAutoBoxData& autoBoxData=vehCoreSimData.getAutoBoxData();

		bool gearUp=false;
		if(normalisedEngineOmega > autoBoxData.mUpRatios[currentGear] && PxVehicleGearsData::eREVERSE!=currentGear)
		{
			//If revs too high and not in reverse and not undergoing a gear change then switch up. 
			gearUp=true;
		}

		bool gearDown=false;
		if(normalisedEngineOmega < autoBoxData.mDownRatios[currentGear] && currentGear > PxVehicleGearsData::eFIRST)
		{
			//If revs too low and in gear greater than first and not undergoing a gear change then change down.
			gearDown=true;
		}

		//Start the gear change and reset the time since the last autobox gear change.
		if(gearUp || gearDown)
		{
			vehCore.mGearUpPressed=gearUp;
			vehCore.mGearDownPressed=gearDown;
			vehCore.mAutoBoxSwitchTime=0.0f;
		}
	}
	else
	{
		autoBoxSwitchTime+=timestep;
		vehCore.mAutoBoxSwitchTime=autoBoxSwitchTime;
	}
}

void processGears(const PxF32 timestep, const PxVehicleGearsData& gears, PxVehicleDriveDynData& car)
{
	//const PxVehicleGearsData& gears=car.mVehicleSimData.getGearsData();

	//Process the gears.
	if(car.mGearUpPressed && gears.mNumRatios-1!=car.mCurrentGear && car.mCurrentGear==car.mTargetGear)
	{
		//Car wants to go up a gear and can go up a gear and not already undergoing a gear change.
		if(PxVehicleGearsData::eREVERSE==car.mCurrentGear)
		{
			//In reverse so switch to first through neutral.
			car.mGearSwitchTime=0;
			car.mTargetGear=PxVehicleGearsData::eFIRST;
			car.mCurrentGear=PxVehicleGearsData::eNEUTRAL;
		}
		else if(PxVehicleGearsData::eNEUTRAL==car.mCurrentGear)
		{
			//In neutral so switch to first and stay in neutral.
			car.mGearSwitchTime=0;
			car.mTargetGear=PxVehicleGearsData::eFIRST;
			car.mCurrentGear=PxVehicleGearsData::eNEUTRAL;
		}
		else
		{
			//Switch up a gear through neutral.
			car.mGearSwitchTime=0;
			car.mTargetGear=car.mCurrentGear+1;
			car.mCurrentGear=PxVehicleGearsData::eNEUTRAL;
		}
	}
	if(car.mGearDownPressed && PxVehicleGearsData::eREVERSE!=car.mCurrentGear && car.mCurrentGear==car.mTargetGear)
	{
		//Car wants to go down a gear and can go down a gear and not already undergoing a gear change
		if(PxVehicleGearsData::eFIRST==car.mCurrentGear)
		{
			//In first so switch to reverse through neutral.
			car.mGearSwitchTime=0;
			car.mTargetGear=PxVehicleGearsData::eREVERSE;
			car.mCurrentGear=PxVehicleGearsData::eNEUTRAL;
		}
		else if(PxVehicleGearsData::eNEUTRAL==car.mCurrentGear)
		{
			//In neutral so switch to reverse and stay in neutral.
			car.mGearSwitchTime=0;
			car.mTargetGear=PxVehicleGearsData::eREVERSE;
			car.mCurrentGear=PxVehicleGearsData::eNEUTRAL;
		}
		else
		{
			//Switch down a gear through neutral.
			car.mGearSwitchTime=0;
			car.mTargetGear=car.mCurrentGear-1;			
			car.mCurrentGear=PxVehicleGearsData::eNEUTRAL;
		}
	}
	if(car.mCurrentGear!=car.mTargetGear)
	{
		if(car.mGearSwitchTime>gears.mSwitchTime)
		{
			car.mCurrentGear=car.mTargetGear;
			car.mGearSwitchTime=0;
		}
		else
		{
			car.mGearSwitchTime+=timestep;
		}
	}
}

PX_FORCE_INLINE PxF32 computeSign(const PxF32 f)
{
	return physx::intrinsics::fsel(f, physx::intrinsics::fsel(-f, 0.0f, 1.0f), -1.0f); 
}

PX_FORCE_INLINE PxF32 computeGearRatio(const PxVehicleGearsData& gearsData, const PxU32 currentGear)
{
	const PxF32 gearRatio=gearsData.mRatios[currentGear]*gearsData.mFinalRatio;
	return gearRatio;
}

PX_FORCE_INLINE PxF32 computeEngineDriveTorque(const PxVehicleEngineData& engineData, const PxF32 omega, const PxF32 accel)
{
	const PxF32 engineDriveTorque=accel*engineData.mPeakTorque*engineData.mTorqueCurve.getYVal(omega*engineData.getRecipMaxOmega());
	return engineDriveTorque;
}

PX_FORCE_INLINE PxF32 computeEngineDampingRate(const PxVehicleEngineData& engineData, const PxF32 gear, const PxF32 accel)
{
	const PxF32 fullThrottleDamping = engineData.mDampingRateFullThrottle;
	const PxF32 zeroThrottleDamping = (PxVehicleGearsData::eNEUTRAL!=gear) ? engineData.mDampingRateZeroThrottleClutchEngaged : engineData.mDampingRateZeroThrottleClutchDisengaged;
	const PxF32 engineDamping = zeroThrottleDamping + (fullThrottleDamping-zeroThrottleDamping)*accel;
	return engineDamping;
}

PX_FORCE_INLINE void splitTorque
(const PxF32 w1, const PxF32 w2, const PxF32 diffBias, const PxF32 defaultSplitRatio,
 PxF32* t1, PxF32* t2)
{
	PX_ASSERT(computeSign(w1)==computeSign(w2) && 0.0f!=computeSign(w1));
	const PxF32 w1Abs=PxAbs(w1);
	const PxF32 w2Abs=PxAbs(w2);
	const PxF32 omegaMax=PxMax(w1Abs,w2Abs);
	const PxF32 omegaMin=PxMin(w1Abs,w2Abs);
	const PxF32 delta=omegaMax-diffBias*omegaMin;
	const PxF32 deltaTorque=physx::intrinsics::fsel(delta, delta/omegaMax , 0.0f);
	const PxF32 f1=physx::intrinsics::fsel(w1Abs-w2Abs, defaultSplitRatio*(1.0f-deltaTorque), defaultSplitRatio*(1.0f+deltaTorque));
	const PxF32 f2=physx::intrinsics::fsel(w1Abs-w2Abs, (1.0f-defaultSplitRatio)*(1.0f+deltaTorque), (1.0f-defaultSplitRatio)*(1.0f-deltaTorque));
	const PxF32 denom=1.0f/(f1+f2);
	*t1=f1*denom;
	*t2=f2*denom;
	PX_ASSERT((*t1 + *t2) >=0.999f && (*t1 + *t2) <=1.001f);  
}

PX_FORCE_INLINE void computeDiffTorqueRatios
(const PxVehicleDifferential4WData& diffData, const PxF32 handbrake, const PxF32* PX_RESTRICT wheelOmegas, PxF32* PX_RESTRICT diffTorqueRatios)
{
	//If the handbrake is on only deliver torque to the front wheels.
	PxU32 type=diffData.mType;
	if(handbrake>0)
	{
		switch(diffData.mType)
		{
		case PxVehicleDifferential4WData::eDIFF_TYPE_LS_4WD:
			type=PxVehicleDifferential4WData::eDIFF_TYPE_LS_FRONTWD;
			break;
		case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_4WD:
			type=PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_FRONTWD;
			break;
		default:
			break;
		}
	}

	const PxF32 wfl=wheelOmegas[PxVehicleDrive4W::eFRONT_LEFT_WHEEL];
	const PxF32 wfr=wheelOmegas[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL];
	const PxF32 wrl=wheelOmegas[PxVehicleDrive4W::eREAR_LEFT_WHEEL];
	const PxF32 wrr=wheelOmegas[PxVehicleDrive4W::eREAR_RIGHT_WHEEL];

	const PxF32 centreBias=diffData.mCentreBias;
	const PxF32 frontBias=diffData.mFrontBias;
	const PxF32 rearBias=diffData.mRearBias;

	const PxF32 frontRearSplit=diffData.mFrontRearSplit;
	const PxF32 frontLeftRightSplit=diffData.mFrontLeftRightSplit;
	const PxF32 rearLeftRightSplit=diffData.mRearLeftRightSplit;

	const PxF32 oneMinusFrontRearSplit=1.0f-diffData.mFrontRearSplit;
	const PxF32 oneMinusFrontLeftRightSplit=1.0f-diffData.mFrontLeftRightSplit;
	const PxF32 oneMinusRearLeftRightSplit=1.0f-diffData.mRearLeftRightSplit;

	const PxF32 swfl=computeSign(wfl);

	//Split a torque of 1 between front and rear.
	//Then split that torque between left and right.
	PxF32 torqueFrontLeft=0;
	PxF32 torqueFrontRight=0;
	PxF32 torqueRearLeft=0;
	PxF32 torqueRearRight=0;
	switch(type)
	{
	case PxVehicleDifferential4WData::eDIFF_TYPE_LS_4WD:
		if(0.0f!=swfl && swfl==computeSign(wfr) && swfl==computeSign(wrl) && swfl==computeSign(wrr))
		{
			PxF32 torqueFront,torqueRear;
			const PxF32 omegaFront=PxAbs(wfl+wfr);
			const PxF32 omegaRear=PxAbs(wrl+wrr);
			splitTorque(omegaFront,omegaRear,centreBias,frontRearSplit,&torqueFront,&torqueRear);
			splitTorque(wfl,wfr,frontBias,frontLeftRightSplit,&torqueFrontLeft,&torqueFrontRight);
			splitTorque(wrl,wrr,rearBias,rearLeftRightSplit,&torqueRearLeft,&torqueRearRight);
			torqueFrontLeft*=torqueFront;
			torqueFrontRight*=torqueFront;
			torqueRearLeft*=torqueRear;
			torqueRearRight*=torqueRear;
		}
		else
		{
			//TODO: need to handle this case.
			torqueFrontLeft=frontRearSplit*frontLeftRightSplit;
			torqueFrontRight=frontRearSplit*oneMinusFrontLeftRightSplit;
			torqueRearLeft=oneMinusFrontRearSplit*rearLeftRightSplit;
			torqueRearRight=oneMinusFrontRearSplit*oneMinusRearLeftRightSplit;
		}
		break;

	case PxVehicleDifferential4WData::eDIFF_TYPE_LS_FRONTWD:
		if(0.0f!=swfl && swfl==computeSign(wfr))
		{
			splitTorque(wfl,wfr,frontBias,frontLeftRightSplit,&torqueFrontLeft,&torqueFrontRight);
		}
		else
		{
			torqueFrontLeft=frontLeftRightSplit;
			torqueFrontRight=oneMinusFrontLeftRightSplit;
		}
		break;

	case PxVehicleDifferential4WData::eDIFF_TYPE_LS_REARWD:

		if(0.0f!=computeSign(wrl) && computeSign(wrl)==computeSign(wrr))
		{
			splitTorque(wrl,wrr,rearBias,rearLeftRightSplit,&torqueRearLeft,&torqueRearRight);
		}
		else
		{
			torqueRearLeft=rearLeftRightSplit;
			torqueRearRight=oneMinusRearLeftRightSplit;
		}
		break;

	case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_4WD:
		torqueFrontLeft=frontRearSplit*frontLeftRightSplit;
		torqueFrontRight=frontRearSplit*oneMinusFrontLeftRightSplit;
		torqueRearLeft=oneMinusFrontRearSplit*rearLeftRightSplit;
		torqueRearRight=oneMinusFrontRearSplit*oneMinusRearLeftRightSplit;
		break;

	case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_FRONTWD:
		torqueFrontLeft=frontLeftRightSplit;
		torqueFrontRight=oneMinusFrontLeftRightSplit;
		break;

	case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_REARWD:
		torqueRearLeft=rearLeftRightSplit;
		torqueRearRight=oneMinusRearLeftRightSplit;
		break;

	default:
		PX_ASSERT(false);
		break;
	}

	diffTorqueRatios[PxVehicleDrive4W::eFRONT_LEFT_WHEEL]=torqueFrontLeft;
	diffTorqueRatios[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL]=torqueFrontRight;
	diffTorqueRatios[PxVehicleDrive4W::eREAR_LEFT_WHEEL]=torqueRearLeft;
	diffTorqueRatios[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]=torqueRearRight;

	PX_ASSERT(((torqueFrontLeft+torqueFrontRight+torqueRearLeft+torqueRearRight) >= 0.999f) && ((torqueFrontLeft+torqueFrontRight+torqueRearLeft+torqueRearRight) <= 1.001f));
}

PX_FORCE_INLINE void computeDiffAveWheelSpeedContributions
(const PxVehicleDifferential4WData& diffData, const PxF32 handbrake, PxF32* PX_RESTRICT diffAveWheelSpeedContributions)
{
	PxU32 type=diffData.mType;

	//If the handbrake is on only deliver torque to the front wheels.
	if(handbrake>0)
	{
		switch(diffData.mType)
		{
		case PxVehicleDifferential4WData::eDIFF_TYPE_LS_4WD:
			type=PxVehicleDifferential4WData::eDIFF_TYPE_LS_FRONTWD;
			break;
		case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_4WD:
			type=PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_FRONTWD;
			break;
		default:
			break;
		}
	}

	const PxF32 frontRearSplit=diffData.mFrontRearSplit;
	const PxF32 frontLeftRightSplit=diffData.mFrontLeftRightSplit;
	const PxF32 rearLeftRightSplit=diffData.mRearLeftRightSplit;

	const PxF32 oneMinusFrontRearSplit=1.0f-diffData.mFrontRearSplit;
	const PxF32 oneMinusFrontLeftRightSplit=1.0f-diffData.mFrontLeftRightSplit;
	const PxF32 oneMinusRearLeftRightSplit=1.0f-diffData.mRearLeftRightSplit;

	switch(type)
	{
	case PxVehicleDifferential4WData::eDIFF_TYPE_LS_4WD:
	case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_4WD:		
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_LEFT_WHEEL]=frontRearSplit*frontLeftRightSplit;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL]=frontRearSplit*oneMinusFrontLeftRightSplit;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_LEFT_WHEEL]=oneMinusFrontRearSplit*rearLeftRightSplit;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]=oneMinusFrontRearSplit*oneMinusRearLeftRightSplit;
		break;
	case PxVehicleDifferential4WData::eDIFF_TYPE_LS_FRONTWD:
	case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_FRONTWD:
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_LEFT_WHEEL]=frontLeftRightSplit;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL]=oneMinusFrontLeftRightSplit;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_LEFT_WHEEL]=0.0f;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]=0.0f;
		break;
	case PxVehicleDifferential4WData::eDIFF_TYPE_LS_REARWD:
	case PxVehicleDifferential4WData::eDIFF_TYPE_OPEN_REARWD:
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_LEFT_WHEEL]=0.0f;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL]=0.0f;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_LEFT_WHEEL]=rearLeftRightSplit;
		diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]=oneMinusRearLeftRightSplit;
		break;
	default:
		PX_ASSERT(false);
		break;
	}

	PX_ASSERT((diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_LEFT_WHEEL] + 
			   diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL] + 
			   diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_LEFT_WHEEL] +
			   diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]) >= 0.999f &&
			   (diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_LEFT_WHEEL] + 
			   diffAveWheelSpeedContributions[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL] + 
			   diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_LEFT_WHEEL] +
			   diffAveWheelSpeedContributions[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]) <= 1.001f);
}

PX_FORCE_INLINE void computeBrakeAndHandBrakeTorques
(const PxVehicleWheelData* PX_RESTRICT wheelDatas, const PxF32* PX_RESTRICT wheelOmegas, const PxF32 brake, const PxF32 handbrake, 
 PxF32* PX_RESTRICT brakeTorques, bool* isBrakeApplied)
{
	//At zero speed offer no brake torque allowed.

	const PxF32 sign0=computeSign(wheelOmegas[0]); 
	brakeTorques[0]=(-brake*sign0*wheelDatas[0].mMaxBrakeTorque-handbrake*sign0*wheelDatas[0].mMaxHandBrakeTorque);
	isBrakeApplied[0]=((brake*wheelDatas[0].mMaxBrakeTorque+handbrake*wheelDatas[0].mMaxHandBrakeTorque)!=0);

	const PxF32 sign1=computeSign(wheelOmegas[1]); 
	brakeTorques[1]=(-brake*sign1*wheelDatas[1].mMaxBrakeTorque-handbrake*sign1*wheelDatas[1].mMaxHandBrakeTorque);
	isBrakeApplied[1]=((brake*wheelDatas[1].mMaxBrakeTorque+handbrake*wheelDatas[1].mMaxHandBrakeTorque)!=0);

	const PxF32 sign2=computeSign(wheelOmegas[2]); 
	brakeTorques[2]=(-brake*sign2*wheelDatas[2].mMaxBrakeTorque-handbrake*sign2*wheelDatas[2].mMaxHandBrakeTorque);
	isBrakeApplied[2]=((brake*wheelDatas[2].mMaxBrakeTorque+handbrake*wheelDatas[2].mMaxHandBrakeTorque)!=0);

	const PxF32 sign3=computeSign(wheelOmegas[3]); 
	brakeTorques[3]=(-brake*sign3*wheelDatas[3].mMaxBrakeTorque-handbrake*sign3*wheelDatas[3].mMaxHandBrakeTorque);
	isBrakeApplied[3]=((brake*wheelDatas[3].mMaxBrakeTorque+handbrake*wheelDatas[3].mMaxHandBrakeTorque)!=0);
}

PX_FORCE_INLINE void computeTankBrakeTorques
(const PxVehicleWheelData* PX_RESTRICT wheelDatas, const PxF32* PX_RESTRICT wheelOmegas, const PxF32 brakeLeft, const PxF32 brakeRight, 
 PxF32* PX_RESTRICT brakeTorques, bool* isBrakeApplied)
{
	//At zero speed offer no brake torque allowed.

	const PxF32 sign0=computeSign(wheelOmegas[0]); 
	brakeTorques[0]=(-brakeLeft*sign0*wheelDatas[0].mMaxBrakeTorque);
	isBrakeApplied[0]=((brakeLeft*wheelDatas[0].mMaxBrakeTorque)!=0);

	const PxF32 sign1=computeSign(wheelOmegas[1]); 
	brakeTorques[1]=(-brakeRight*sign1*wheelDatas[1].mMaxBrakeTorque);
	isBrakeApplied[1]=((brakeRight*wheelDatas[1].mMaxBrakeTorque)!=0);

	const PxF32 sign2=computeSign(wheelOmegas[2]); 
	brakeTorques[2]=(-brakeLeft*sign2*wheelDatas[2].mMaxBrakeTorque);
	isBrakeApplied[2]=((brakeLeft*wheelDatas[2].mMaxBrakeTorque)!=0);

	const PxF32 sign3=computeSign(wheelOmegas[3]); 
	brakeTorques[3]=(-brakeRight*sign3*wheelDatas[3].mMaxBrakeTorque);
	isBrakeApplied[3]=((brakeRight*wheelDatas[3].mMaxBrakeTorque)!=0);
}

PX_FORCE_INLINE PxF32 computeClutchStrength(const PxVehicleClutchData& clutchData, const PxU32 currentGear)
{
	return ((PxVehicleGearsData::eNEUTRAL!=currentGear) ? clutchData.mStrength : 0.0f);
}

PX_FORCE_INLINE PxF32 computeFilteredNormalisedTireLoad(const PxVehicleTireLoadFilterData& filterData, const PxF32 normalisedLoad)
{
	return physx::intrinsics::fsel(filterData.mMinNormalisedLoad-normalisedLoad, 
		0.0f, 
		physx::intrinsics::fsel(normalisedLoad-filterData.mMaxNormalisedLoad, 
		filterData.mMaxNormalisedLoad, 
		filterData.mMaxFilteredNormalisedLoad*(normalisedLoad-filterData.mMinNormalisedLoad)*filterData.getDenominator()));
}

PX_FORCE_INLINE void computeAckermannSteerAngles
(const PxF32 steer, const PxF32 steerGain, 
 const PxF32 ackermannAccuracy, const PxF32 width, const PxF32 axleSeparation,  
 PxF32* PX_RESTRICT leftAckermannSteerAngle, PxF32* PX_RESTRICT rightAckermannSteerAngle)
{
	PX_ASSERT(steer>=-1.01f && steer<=1.01f);
	PX_ASSERT(steerGain<PxPi);

	const PxF32 steerAngle=steer*steerGain;

	if(0==steerAngle)
	{
		*leftAckermannSteerAngle=0;
		*rightAckermannSteerAngle=0;
		return;
	}

	//Work out the ackermann steer for +ve steer then swap and negate the steer angles if the steer is -ve.
	//TODO: use faster approximate functions for PxTan/PxATan because the results don't need to be too accurate here.
	const PxF32 rightSteerAngle=PxAbs(steerAngle);
	const PxF32 dz=axleSeparation;
	const PxF32 dx=width + dz/PxTan(rightSteerAngle);
	const PxF32 leftSteerAnglePerfect=PxAtan(dz/dx);
	const PxF32 leftSteerAngle=rightSteerAngle + ackermannAccuracy*(leftSteerAnglePerfect-rightSteerAngle);
	*rightAckermannSteerAngle=physx::intrinsics::fsel(steerAngle, rightSteerAngle, -leftSteerAngle);
	*leftAckermannSteerAngle=physx::intrinsics::fsel(steerAngle, leftSteerAngle, -rightSteerAngle);
}

PX_FORCE_INLINE void computeAckermannCorrectedSteerAngles
(const PxVehicleDriveSimData4W& vehCoreSimData, const PxVehicleWheels4SimData& vehSuspWheelTire4SimData, const PxF32 steer, 
 PxF32* PX_RESTRICT steerAngles)
{
	const PxVehicleAckermannGeometryData& ackermannData=vehCoreSimData.getAckermannGeometryData();
	const PxF32 ackermannAccuracy=ackermannData.mAccuracy;
	const PxF32 axleSeparation=ackermannData.mAxleSeparation;

	{
	const PxVehicleWheelData& wheelDataFL=vehSuspWheelTire4SimData.getWheelData(PxVehicleDrive4W::eFRONT_LEFT_WHEEL);
	const PxVehicleWheelData& wheelDataFR=vehSuspWheelTire4SimData.getWheelData(PxVehicleDrive4W::eFRONT_RIGHT_WHEEL);
	const PxF32 steerGainFront=PxMax(wheelDataFL.mMaxSteer,wheelDataFR.mMaxSteer);
	const PxF32 frontWidth=ackermannData.mFrontWidth;
	PxF32 frontLeftSteer,frontRightSteer;
	computeAckermannSteerAngles(steer,steerGainFront,ackermannAccuracy,frontWidth,axleSeparation,&frontLeftSteer,&frontRightSteer);
	steerAngles[PxVehicleDrive4W::eFRONT_LEFT_WHEEL]=wheelDataFL.mToeAngle+frontLeftSteer;
	steerAngles[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL]=wheelDataFR.mToeAngle+frontRightSteer;
	}

	{
	const PxVehicleWheelData& wheelDataRL=vehSuspWheelTire4SimData.getWheelData(PxVehicleDrive4W::eREAR_LEFT_WHEEL);
	const PxVehicleWheelData& wheelDataRR=vehSuspWheelTire4SimData.getWheelData(PxVehicleDrive4W::eREAR_RIGHT_WHEEL);
	const PxF32 steerGainRear=PxMax(wheelDataRL.mMaxSteer,wheelDataRR.mMaxSteer);
	const PxF32 rearWidth=ackermannData.mRearWidth;
	PxF32 rearLeftSteer,rearRightSteer;
	computeAckermannSteerAngles(steer,steerGainRear,ackermannAccuracy,rearWidth,axleSeparation,&rearLeftSteer,&rearRightSteer);
	steerAngles[PxVehicleDrive4W::eREAR_LEFT_WHEEL]=wheelDataRL.mToeAngle-rearLeftSteer;
	steerAngles[PxVehicleDrive4W::eREAR_RIGHT_WHEEL]=wheelDataRR.mToeAngle-rearRightSteer;
	}
}

#define ONE_TWENTYSEVENTH 0.037037f
#define ONE_THIRD 0.33333f
PX_FORCE_INLINE PxF32 smoothingFunction1(const PxF32 K)
{
	//Equation 20 in CarSimEd manual Appendix F.
	//Looks a bit like a curve of sqrt(x) for 0<x<1 but reaching 1.0 on y-axis at K=3. 
	PX_ASSERT(K>=0.0f);
	return PxMin(1.0f, K - ONE_THIRD*K*K + ONE_TWENTYSEVENTH*K*K*K);
}
PX_FORCE_INLINE PxF32 smoothingFunction2(const PxF32 K)
{
	//Equation 21 in CarSimEd manual Appendix F.
	//Rises to a peak at K=0.75 and falls back to zero by K=3
	PX_ASSERT(K>=0.0f);
	return (K - K*K + ONE_THIRD*K*K*K - ONE_TWENTYSEVENTH*K*K*K*K);
}

PX_FORCE_INLINE void computeTireDirs(const PxVec3& chassisLatDir, const PxVec3& hitNorm, const PxF32 wheelSteerAngle, PxVec3& tireLongDir, PxVec3& tireLatDir)
{
	PX_ASSERT(chassisLatDir.magnitude()>0.999f && chassisLatDir.magnitude()<1.001f);
	PX_ASSERT(hitNorm.magnitude()>0.999f && hitNorm.magnitude()<1.001f);

	//Compute the tire axes in the ground plane.
	PxVec3 tzRaw=chassisLatDir.cross(hitNorm);
	PxVec3 txRaw=hitNorm.cross(tzRaw);
	tzRaw.normalize();
	txRaw.normalize();
	//Rotate the tire using the steer angle.
	const PxF32 cosWheelSteer=PxCos(wheelSteerAngle);
	const PxF32 sinWheelSteer=PxSin(wheelSteerAngle);
	const PxVec3 tz=tzRaw*cosWheelSteer + txRaw*sinWheelSteer;
	const PxVec3 tx=txRaw*cosWheelSteer - tzRaw*sinWheelSteer;
	tireLongDir=tz;
	tireLatDir=tx;
}

PX_FORCE_INLINE void computeTireSlips
(const PxF32 longSpeed, const PxF32 latSpeed, const PxF32 wheelOmega, const PxF32 wheelRadius, const bool isBrakeApplied, const bool isTank,
 PxF32& longSlip, PxF32& latSlip)
{
	const PxF32 longSpeedAbs=PxAbs(longSpeed);

	//Lateral slip is easy.
	latSlip = PxAtan(latSpeed/(longSpeedAbs+gMinLatSpeedForTireModel));//TODO: do we really use PxAbs(vz) as denominator?

	if(isTank)
	{
		//Longitudinal slip is a bit harder because we need to avoid a divide-by-zero.
		const PxF32 wheelLinSpeed=wheelOmega*wheelRadius;
		if(isBrakeApplied)
		{
			longSlip = (longSpeedAbs >= PxAbs(wheelLinSpeed)) ? (wheelLinSpeed-longSpeed)/(longSpeedAbs + 1e-5f) : (wheelLinSpeed-longSpeed)/PxAbs(wheelLinSpeed);
		}
		else
		{
			longSlip = (wheelLinSpeed - longSpeed)/(longSpeedAbs + gMinLongSpeedForTireModel);
			//Smoothing - want a graph that is smoothly blends near 0 and 1.  This should do: (1-cos(theta))/2
			longSlip *= longSpeedAbs<gMinLongSpeedForTireModel ? 0.5f*(1.0f - 0.99f*PxCos(PxPi*longSpeedAbs*gRecipMinLongSpeedForTireModel)) : 1.0f;
		}
	}
	else
	{
		if(longSpeed==0 && wheelOmega==0)
		{
			longSlip=0.0f;
		}
		else if(PxAbs(longSpeed) > PxAbs(wheelOmega*wheelRadius))
		{
			longSlip = (wheelOmega*wheelRadius - longSpeed)/(PxAbs(longSpeed)+0.1f);
		}
		else
		{
			longSlip = (wheelOmega*wheelRadius - longSpeed)/(PxAbs(wheelOmega*wheelRadius)+1.0f);
		}
	}
}

PX_FORCE_INLINE void computeTireFriction(const PxVehicleTireData& tireData, const PxF32 longSlip, const PxF32 frictionMultiplier, PxF32& friction)
{
	const PxF32 x0=tireData.mFrictionVsSlipGraph[0][0];
	const PxF32 y0=tireData.mFrictionVsSlipGraph[0][1];
	const PxF32 x1=tireData.mFrictionVsSlipGraph[1][0];
	const PxF32 y1=tireData.mFrictionVsSlipGraph[1][1];
	const PxF32 x2=tireData.mFrictionVsSlipGraph[2][0];
	const PxF32 y2=tireData.mFrictionVsSlipGraph[2][1];
	const PxF32 recipx1Minusx0=tireData.getFrictionVsSlipGraphRecipx1Minusx0();
	const PxF32 recipx2Minusx1=tireData.getFrictionVsSlipGraphRecipx2Minusx1();
	const PxF32 longSlipAbs=PxAbs(longSlip);
	PxF32 mu;
	if(longSlipAbs<x1)
	{
		mu=y0 + (y1-y0)*(longSlipAbs-x0)*recipx1Minusx0;
	}
	else if(longSlipAbs<x2)
	{
		mu=y1 + (y2-y1)*(longSlipAbs-x1)*recipx2Minusx1;
	}
	else
	{
		mu=y2;
	}
	PX_ASSERT(mu>=0);
	friction=mu*frictionMultiplier;
}

PX_FORCE_INLINE void updateLowForwardSpeedTimer
(const PxF32 longSpeed, const PxF32 wheelOmega, const PxF32 wheelRadius, const PxF32 recipWheelRadius,  const bool isIntentionToAccelerate,
 const PxF32 timestep, PxF32& lowForwardSpeedTime)
{
	//If the tire is rotating slowly and the forward speed is slow then increment the slow forward speed timer.
	//If the intention of the driver is to accelerate the vehicle then reset the timer because the intention has been signalled NOT to bring 
	//the wheel to rest.
	PxF32 longSpeedAbs=PxAbs(longSpeed);
	if((longSpeedAbs<gStickyTireFrictionThresholdSpeed) && (PxAbs(wheelOmega)< gStickyTireFrictionThresholdSpeed*recipWheelRadius) && !isIntentionToAccelerate)
	{
		lowForwardSpeedTime+=timestep;		
	}
	else
	{
		lowForwardSpeedTime=0;
	}
}

PX_FORCE_INLINE void activateStickyFrictionConstraint
(const PxF32 longSpeed, const PxF32 wheelOmega, const PxF32 lowForwardSpeedTime, const bool isIntentionToAccelerate,
 bool& stickyTireActiveFlag, PxF32& stickyTireTargetSpeed)
{
	 //Setup the sticky friction constraint to bring the vehicle to rest at the tire contact point.
	 //The idea here is to resolve the singularity of the tire long slip at low vz by replacing the long force with a velocity constraint.
	 //Only do this if we can guarantee that the intention is to bring the car to rest (no accel pedal applied).
	 //Smoothly reduce error to zero to avoid bringing car immediately to rest.  This avoids graphical glitchiness.
	 //We're going to replace the longitudinal tire force with the sticky friction so set the long slip to zero to ensure zero long force.
	 //Apply sticky friction to this tire if 
	 //(1) the wheel is locked (this means the brake/handbrake must be on) and the forward speed at the tire contact point is vanishingly small and
	 //    the drive of vehicle has no intention to accelerate the vehicle.
	 //(2) the accumulated time of low forward speed is greater than a threshold.
	 PxF32 longSpeedAbs=PxAbs(longSpeed);
	 stickyTireActiveFlag=false;
	 stickyTireTargetSpeed=0.0f;
	 if((longSpeedAbs < gStickyTireFrictionThresholdSpeed && 0.0f==wheelOmega && !isIntentionToAccelerate) || lowForwardSpeedTime>gLowForwardSpeedThresholdTime)
	 {
		 stickyTireActiveFlag=true;
		 stickyTireTargetSpeed=longSpeed*gStickyTireFrictionDamping;
	 }
}

void PxVehicleComputeTireForceDefault
(const void* tireShaderData, 
 const PxF32 tireFriction,
 const PxF32 longSlip, const PxF32 latSlip, const PxF32 camber,
 const PxF32 wheelOmega, const PxF32 wheelRadius, const PxF32 recipWheelRadius,
 const PxF32 restTireLoad, const PxF32 normalisedTireLoad, const PxF32 tireLoad,
 const PxF32 gravity, const PxF32 recipGravity,
 PxF32& wheelTorque, PxF32& tireLongForceMag, PxF32& tireLatForceMag, PxF32& tireAlignMoment)
{
	const PxVehicleTireData& tireData=*((PxVehicleTireData*)tireShaderData);

	PX_ASSERT(tireFriction>0);
	PX_ASSERT(tireLoad>0);
	PX_ASSERT(0.0f==camber);//Not supporting a camber angle yet.

	wheelTorque=0.0f;
	tireLongForceMag=0.0f;
	tireLatForceMag=0.0f;
	tireAlignMoment=0.0f;

	//If long slip/lat slip/camber are all zero than there will be zero tire force.
	if((0==latSlip)&&(0==longSlip)&&(0==camber))
	{
		return;
	}

	//Compute the lateral stiffness
	const PxF32 latStiff=restTireLoad*tireData.mLatStiffY*smoothingFunction1(normalisedTireLoad*3.0f/tireData.mLatStiffX);

	//Get the longitudinal stiffness
	const PxF32 longStiff=tireData.mLongitudinalStiffnessPerUnitGravity*gravity;
	const PxF32 recipLongStiff=tireData.getRecipLongitudinalStiffnessPerUnitGravity()*recipGravity;

	//Get the camber stiffness.
	const PxF32 camberStiff=tireData.mCamberStiffness;

	//Carry on and compute the forces.
	const PxF32 TEff = PxTan(latSlip - camber*camberStiff/latStiff);
	const PxF32 K = PxSqrt(latStiff*TEff*latStiff*TEff + longStiff*longSlip*longStiff*longSlip) /(tireFriction*tireLoad);
	//const PxF32 KAbs=PxAbs(K);
	PxF32 FBar = smoothingFunction1(K);//K - ONE_THIRD*PxAbs(K)*K + ONE_TWENTYSEVENTH*K*K*K;
	PxF32 MBar = smoothingFunction2(K); //K - KAbs*K + ONE_THIRD*K*K*K - ONE_TWENTYSEVENTH*KAbs*K*K*K;
	//Mbar = PxMin(Mbar, 1.0f);
	PxF32 nu=1;
	if(K <= 2.0f*PxPi)
	{
		const PxF32 latOverlLong=latStiff*recipLongStiff;
		nu = 0.5f*(1.0f + latOverlLong - (1.0f - latOverlLong)*PxCos(K*0.5f));
	}
	const PxF32 FZero = tireFriction*tireLoad / (PxSqrt(longSlip*longSlip + nu*TEff*nu*TEff));
	const PxF32 fz = longSlip*FBar*FZero;
	const PxF32 fx = -nu*TEff*FBar*FZero;
	//TODO: pneumatic trail.
	const PxF32 pneumaticTrail=1.0f;
	const PxF32	fMy= nu * pneumaticTrail * TEff * MBar * FZero;

	//We can add the torque to the wheel.
	wheelTorque=-fz*wheelRadius;
	tireLongForceMag=fz;
	tireLatForceMag=fx;
	tireAlignMoment=fMy;
}

void processSuspTireWheels
(const PxF32 timeFraction,
 const PxTransform& carChassisTrnsfm, const PxVec3& carChassisLinVel, const PxVec3& carChassisAngVel, const bool isTank,
 const PxVec3& gravity, const PxF32 gravityMagnitude, const PxF32 recipGravityMagnitude, const PxF32 timestep,
 const bool isIntentionToAccelerate,
 const PxVehicleWheels4SimData& vehWheels4SimData, const PxVehicleTireLoadFilterData& tireLoadFilterData, PxVehicleWheels4DynData& vehWheels4DynData, 
 const PxVehicleTireForceCalculator4& vehTireForceCalculator4, const PxU32 numActiveWheels,
 PxRigidDynamic* vehActor,
 const PxF32* PX_RESTRICT steerAngles, const bool* PX_RESTRICT isBrakeApplied, 
 const PxVehicleDrivableSurfaceToTireFrictionPairs* PX_RESTRICT frictionPairs,
 const PxU32 startIndex,
 PxVehicleConstraintShader::VehicleConstraintData& vehConstraintData,
 PxF32* PX_RESTRICT lowForwardSpeedTimers,
 PxF32* PX_RESTRICT jounces, PxF32* PX_RESTRICT forwardSpeeds, PxF32* PX_RESTRICT frictions, PxF32* PX_RESTRICT longSlips, PxF32* PX_RESTRICT latSlips, PxU32* PX_RESTRICT tireSurfaceTypes, PxMaterial** PX_RESTRICT tireSurfaceMaterials,
 PxF32* PX_RESTRICT tireTorques, 
 PxVec3& chassisForce, PxVec3& chassisTorque)
{
#if PX_DEBUG_VEHICLE_ON
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_JOUNCE);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_SUSPFORCE);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_TIRELOAD);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_NORMALIZED_TIRELOAD);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_NORM_TIRE_LONG_FORCE);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_NORM_TIRE_LAT_FORCE);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_TIRE_LONG_SLIP);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_TIRE_LAT_SLIP);
	zeroGraphDataWheels(startIndex,PxVehicleGraph::eCHANNEL_TIRE_FRICTION);
#endif

	bool* PX_RESTRICT suspLimitActiveFlags=vehConstraintData.mSuspLimitData.mActiveFlags;
	PxF32* PX_RESTRICT suspLimitErrors=vehConstraintData.mSuspLimitData.mErrors;

	bool* PX_RESTRICT stickyTireActiveFlags=vehConstraintData.mStickyTireData.mActiveFlags;
	PxVec3* PX_RESTRICT stickyTireDirs=vehConstraintData.mStickyTireData.mDirs;
	PxVec3* PX_RESTRICT stickyTireCMOffsets=vehConstraintData.mStickyTireData.mCMOffsets;
	PxF32* PX_RESTRICT stickyTireTargetSpeeds=vehConstraintData.mStickyTireData.mTargetSpeeds;

	//Vehicle data.
	const PxF32* PX_RESTRICT tireRestLoads=vehWheels4SimData.getTireRestLoadsArray();
	const PxF32* PX_RESTRICT recipTireRestLoads=vehWheels4SimData.getRecipTireRestLoadsArray();

	//Compute the right direction for later.
	const PxVec3 latDir=carChassisTrnsfm.rotate(gRight);

	//Hash table for quick lookup of drivable surface type from material
	VehicleSurfaceTypeHashTable surfaceTypeHashTable(*frictionPairs);

	//Arrays of hits (need to mask out wheels that had no raycast).
	PxU32 numHits4[4]={0,0,0,0};
	PxRaycastHit* hits4[4]={NULL,NULL,NULL,NULL};
	if(vehWheels4DynData.mSqResults)
	{
		for(PxU32 i=0;i<numActiveWheels;i++)
		{
			numHits4[i]=vehWheels4DynData.mSqResults[i].nbHits;
			hits4[i]=vehWheels4DynData.mSqResults[i].hits;
		}
	}

	PxF32 newLowForwardSpeedTimers[4];
	for(PxU32 i=0;i<4;i++)
	{
		const PxVehicleWheelData& wheel=vehWheels4SimData.getWheelData(i);
		const PxVehicleSuspensionData& susp=vehWheels4SimData.getSuspensionData(i);

		newLowForwardSpeedTimers[i]=lowForwardSpeedTimers[i];

#if PX_DEBUG_VEHICLE_ON
		updateGraphDataSuspJounce(startIndex, i,-susp.mMaxDroop);
#endif

		//If there is no ground intersection then the wheel 
		//will sit at max droop.
		PxF32 jounce=-susp.mMaxDroop;
		jounces[i]=jounce;
		suspLimitActiveFlags[i]=false;
		suspLimitErrors[i]=0.0f;
		stickyTireActiveFlags[i]=false;
		stickyTireTargetSpeeds[i]=0;

		//If there has been a hit then compute the suspension force and tire load.
		const PxU32 numHits=numHits4[i];
		if(numHits>0)
		{
			PxRaycastHit* hits=hits4[i];

			const PxVec3 hitPos=hits[0].impact;
			const PxVec3 hitNorm=hits[0].normal;
			PxMaterial* material = hits[0].shape->getMaterialFromInternalFaceIndex(hits[0].faceIndex);
			tireSurfaceMaterials[i]=material;
			PxU32 surfaceType;
			if(NULL!=material)
			{
				surfaceType=surfaceTypeHashTable.get(material);
			}
			else
			{
				PX_CHECK_MSG(material, "material is null ptr");
				surfaceType=0;
			}

			//Get the tire type.
			const PxU32 tireType=vehWheels4SimData.getTireData(i).mType;

			//Get the friction multiplier.
			const PxF32 frictionMultiplier=frictionPairs->getTypePairFriction(surfaceType,tireType);
			PX_ASSERT(frictionMultiplier>=0);
			tireSurfaceTypes[i]=surfaceType;

			//Compute the plane equation for the intersection plane found by the susp line raycast (n.p+d=0)
			const PxF32 hitD=-hitNorm.dot(hitPos);

			//Work out the point on the susp line that touches the intersection plane.
			//n.(v+wt)+d=0 where n,d describe the plane; v,w describe the susp ray; t is the point on the susp line.
			//t=-(n.v + d)/n.w
			const PxVec3& v=vehWheels4DynData.mSuspLineStarts[i];
			const PxVec3& w=vehWheels4DynData.mSuspLineDirs[i];
			const PxVec3& n=hitNorm;
			const PxF32 d=hitD;
			const PxF32 T=-(n.dot(v) + d)/(n.dot(w));

			//The rest pos of the susp line is 2*radius + maxBounce.
			const PxF32 restT=2.0f*wheel.mRadius+susp.mMaxCompression;

			//Compute the spring compression ie the difference between T and restT.
			//+ve means that the spring is compressed
			//-ve means that the spring is elongated.
			PxF32 dx=restT-T;

			//If the spring is elongated past its max droop then the wheel isn't touching the ground.
			//In this case the spring offers zero force and provides no support for the chassis/sprung mass.
			//Only carry on computing the spring force if the wheel is touching the ground.
			const PxF32 maxDroop=susp.mMaxDroop;
			PX_ASSERT(maxDroop>=0);
			PX_UNUSED(maxDroop);
			if(dx > -susp.mMaxDroop)
			{
				//Clamp the spring compression so that it is never greater than the max bounce.
				//Apply the susp limit constraint if the spring compression is greater than the max bounce.
				suspLimitErrors[i] = dx - susp.mMaxCompression;
				suspLimitActiveFlags[i] = (dx > susp.mMaxCompression);
				jounce=PxMin(dx,susp.mMaxCompression);

				//Store the jounce.
				jounces[i]=jounce;
#if PX_DEBUG_VEHICLE_ON
				updateGraphDataSuspJounce(startIndex, i,jounce);
#endif

				//Compute the speed of the rigid body along the suspension travel dir at the 
				//bottom of the wheel.
				const PxVec3 wheelBottomPos=v+w*(restT - jounce);
				const PxVec3 r=wheelBottomPos-carChassisTrnsfm.p;
				PxVec3 wheelBottomVel=carChassisLinVel;
				wheelBottomVel+=carChassisAngVel.cross(r);
				PxRigidDynamic* dynamicHitActor=hits[0].shape->getActor().is<PxRigidDynamic>();
				if(dynamicHitActor)
				{
					wheelBottomVel-=PxRigidBodyExt::getVelocityAtPos(*dynamicHitActor,wheelBottomPos);
				}
				const PxF32 jounceSpeed=wheelBottomVel.dot(w);

				//We've got the cm offset to apply to the sticky tire friction.  
				//Set it right now;
				stickyTireCMOffsets[i]=r;

				//Compute the spring force.
				PxF32 springForce=susp.mSprungMass*w.dot(gravity);		//gravity acting along spring direction
				springForce+=susp.mSpringStrength*jounce;				//linear spring
				springForce+=susp.mSpringDamperRate*jounceSpeed;		//damping

#if PX_DEBUG_VEHICLE_ON
				updateGraphDataSuspForce(startIndex,i,springForce);
#endif

				//Chassis force in opposite direction to spring travel direction.
				springForce*=-1.0f;
				const PxVec3 springForceJ=w*springForce;

				//Torque from spring force.
				const PxVec3 r2=carChassisTrnsfm.rotate(vehWheels4SimData.getSuspForceAppPointOffset(i));
				const PxVec3 springTorqueJ=r2.cross(springForceJ);

				//Add the suspension force/torque to the chassis force/torque.
				chassisForce+=springForceJ;
				chassisTorque+=springTorqueJ;

				//Now compute the tire load.
				//Add on the tire mass gravity force.
				PxF32 tireLoad=springForce*w.dot(hitNorm);
				tireLoad -= wheel.mMass*gravity.dot(hitNorm);

				//Apply the opposite force to the hit object.
				if(dynamicHitActor && !(dynamicHitActor->getRigidDynamicFlags() & PxRigidDynamicFlag::eKINEMATIC))
				{
					const PxVec3 hitForce=hitNorm*(-tireLoad)*timeFraction;
					PxRigidBodyExt::addForceAtPos(*dynamicHitActor,hitForce,hitPos);
				}

				//Normalize the tire load 
				//Now work out the normalized tire load.
				const PxF32 normalisedTireLoad=tireLoad*recipGravityMagnitude*recipTireRestLoads[i];
				//Filter the normalized tire load and compute the filtered tire load too.
				const PxF32 filteredNormalisedTireLoad=computeFilteredNormalisedTireLoad(tireLoadFilterData,normalisedTireLoad);
				const PxF32 filteredTireLoad=filteredNormalisedTireLoad*gravityMagnitude*tireRestLoads[i];

#if PX_DEBUG_VEHICLE_ON
				updateGraphDataTireLoad(startIndex,i,filteredTireLoad);
				updateGraphDataNormTireLoad(startIndex,i,filteredNormalisedTireLoad);
#endif

				if(filteredTireLoad*frictionMultiplier>0)
				{
					//Compute the lateral and longitudinal tire axes in the ground plane.
					PxVec3 tireLongDir;
					PxVec3 tireLatDir;
					computeTireDirs(latDir,hitNorm,steerAngles[i],tireLongDir,tireLatDir);

					//Now compute the speeds along each of the tire axes.
					const PxF32 tireLongSpeed=wheelBottomVel.dot(tireLongDir);
					const PxF32 tireLatSpeed=wheelBottomVel.dot(tireLatDir);
					forwardSpeeds[i]=tireLongSpeed;

					//Now compute the slips along each axes.
					const bool hasBrake=isBrakeApplied[i];
					const PxF32 wheelOmega=vehWheels4DynData.mWheelSpeeds[i];
					const PxF32 wheelRadius=vehWheels4SimData.getWheelData(i).mRadius;
					PxF32 longSlip;
					PxF32 latSlip;
					computeTireSlips(tireLongSpeed,tireLatSpeed,wheelOmega,wheelRadius,hasBrake,isTank,longSlip,latSlip);
					longSlips[i]=longSlip;
					latSlips[i]=latSlip;

					//Camber angle.
					const PxF32 camber=0.0f;

					//Compute the friction that will be experienced by the tire.
					const PxVehicleTireData& tireData=vehWheels4SimData.getTireData(i);
					PxF32 friction;
					computeTireFriction(tireData,longSlip,frictionMultiplier,friction);
					frictions[i]=friction;

					//check the accel value here
					//Update low forward speed timer.
					PxF32 lowForwardSpeedTimer=newLowForwardSpeedTimers[i];
					const PxF32 recipWheelRadius=vehWheels4SimData.getWheelData(i).getRecipRadius();
					updateLowForwardSpeedTimer(tireLongSpeed,wheelOmega,wheelRadius,recipWheelRadius,isIntentionToAccelerate,timestep,lowForwardSpeedTimer);
					newLowForwardSpeedTimers[i]=lowForwardSpeedTimer;

					//Activate sticky tire friction constraint if required.
					//If sticky tire friction is active then set the longitudinal slip to zero because 
					//the sticky tire constraint will take care of the longitudinal component of motion.
					bool stickyTireActiveFlag=false;
					PxF32 stickyTireTargetSpeed=0.0f;
					activateStickyFrictionConstraint(tireLongSpeed,wheelOmega,lowForwardSpeedTimer,isIntentionToAccelerate,stickyTireActiveFlag,stickyTireTargetSpeed);
					stickyTireActiveFlags[i]=stickyTireActiveFlag;
					stickyTireTargetSpeeds[i]=stickyTireTargetSpeed;
					stickyTireDirs[i]=tireLongDir;
					longSlip=(!stickyTireActiveFlag ? longSlip : 0.0f); 
					longSlips[i]=longSlip;

					//Compute the various tire torques.
					PxF32 wheelTorque=0;
					PxF32 tireLongForceMag=0;
					PxF32 tireLatForceMag=0;
					PxF32 tireAlignMoment=0;
					const PxF32 restTireLoad=gravityMagnitude*tireRestLoads[i];
					vehTireForceCalculator4.mShader(
						vehTireForceCalculator4.mShaderData[i],
						friction,
						longSlip,latSlip,camber,
						wheelOmega,wheelRadius,recipWheelRadius,
						restTireLoad,filteredNormalisedTireLoad,filteredTireLoad,
						gravityMagnitude, recipGravityMagnitude,
						wheelTorque,tireLongForceMag,tireLatForceMag,tireAlignMoment);

					//Apply the torque to the wheel (just store for now then we'll do this in the internal dynamics solver)
					tireTorques[i]=wheelTorque;

					//Apply the torque to the chassis.
					//Compute the tire force to apply to the chassis.
					const PxVec3 tireLongForce=tireLongDir*tireLongForceMag;
					const PxVec3 tireLatForce=tireLatDir*tireLatForceMag;
					const PxVec3 tireForce=tireLongForce+tireLatForce;
					//Compute the torque to apply to the chassis.
					const PxVec3 r=carChassisTrnsfm.rotate(vehWheels4SimData.getTireForceAppPointOffset(i));
					const PxVec3 tireTorque=r.cross(tireForce);
					//Add all the forces/torques together.
					chassisForce+=tireForce;
					chassisTorque+=tireTorque;

					//Graph all the data we just computed.
#if PX_DEBUG_VEHICLE_ON
					if(gCarTireForceAppPoints)
						gCarTireForceAppPoints[i]=carChassisTrnsfm.p + carChassisTrnsfm.rotate(vehWheels4SimData.getTireForceAppPointOffset(i));
					if(gCarSuspForceAppPoints)
						gCarSuspForceAppPoints[i]=carChassisTrnsfm.p + carChassisTrnsfm.rotate(vehWheels4SimData.getSuspForceAppPointOffset(i));

					if(gCarWheelGraphData[0])
					{
						updateGraphDataNormLongTireForce(startIndex, i, PxAbs(tireLongForceMag)*normalisedTireLoad/tireLoad);
						updateGraphDataNormLatTireForce(startIndex, i, PxAbs(tireLatForceMag)*normalisedTireLoad/tireLoad);
						updateGraphDataNormTireAligningMoment(startIndex, i, tireAlignMoment*normalisedTireLoad/tireLoad);
						updateGraphDataLongTireSlip(startIndex, i,longSlips[i]);
						updateGraphDataLatTireSlip(startIndex, i,latSlips[i]);
						updateGraphDataTireFriction(startIndex, i,frictions[i]);
					}
#endif
				}//filteredTireLoad*frictionMultiplier>0
			}
		}

		lowForwardSpeedTimers[i]=(newLowForwardSpeedTimers[i]!=lowForwardSpeedTimers[i] ? newLowForwardSpeedTimers[i] : 0.0f);
	}
}

PX_FORCE_INLINE void getVehicleControlValues(const PxVehicleDriveDynData& driveDynData, PxF32& accel, PxF32& brake, PxF32& handbrake, PxF32& steerLeft, PxF32& steerRight)
{
	accel=driveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_ACCEL];
	brake=driveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_BRAKE];
	handbrake=driveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE];
	steerLeft=driveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT];
	steerRight=driveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT];
}

PX_FORCE_INLINE void getTankControlValues(const PxVehicleDriveDynData& driveDynData, PxF32& accel, PxF32& brakeLeft, PxF32& brakeRight, PxF32& thrustLeft, PxF32& thrustRight)
{
	accel=driveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_ACCEL];
	brakeLeft=driveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT];
	brakeRight=driveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT];
	thrustLeft=driveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT];
	thrustRight=driveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT];
}

#define MAX_VECTORN_SIZE (PX_MAX_NUM_WHEELS+1)

class VectorN
{
public:

	VectorN(const PxU32 size)
		: mSize(size)
	{
		PX_ASSERT(mSize<MAX_VECTORN_SIZE);
	}
	~VectorN()
	{
	}

	VectorN(const VectorN& src)
	{
		for(PxU32 i=0;i<src.mSize;i++)
		{
			mValues[i]=src.mValues[i];
		}
		mSize=src.mSize;
	}

	VectorN& operator=(const VectorN& src)
	{
		for(PxU32 i=0;i<src.mSize;i++)
		{
			mValues[i]=src.mValues[i];
		}
		mSize=src.mSize;
		return *this;
	}

	PX_FORCE_INLINE PxF32& operator[] (const PxU32 i)
	{
		PX_ASSERT(i<mSize);
		return (mValues[i]);
	}

	PX_FORCE_INLINE const PxF32& operator[] (const PxU32 i) const
	{
		PX_ASSERT(i<mSize);
		return (mValues[i]);
	}

	PX_FORCE_INLINE PxU32 getSize() const {return mSize;}

private:

	PxF32 mValues[MAX_VECTORN_SIZE];
	PxU32 mSize;
};

class MatrixNN
{
public:

	MatrixNN()
		: mSize(0)
	{
	}
	MatrixNN(const PxU32 size)
		: mSize(size)
	{
		PX_ASSERT(mSize<MAX_VECTORN_SIZE);
	}
	MatrixNN(const MatrixNN& src)
	{
		for(PxU32 i=0;i<src.mSize;i++)
		{
			for(PxU32 j=0;j<src.mSize;j++)
			{
				mValues[i][j]=src.mValues[i][j];
			}
		}
		mSize=src.mSize;
	}
	~MatrixNN()
	{
	}

	MatrixNN& operator=(const MatrixNN& src)
	{
		for(PxU32 i=0;i<src.mSize;i++)
		{
			for(PxU32 j=0;j<src.mSize;j++)
			{
				mValues[i][j]=src.mValues[i][j];
			}
		}
		mSize=src.mSize;
		return *this;
	}

	PX_FORCE_INLINE PxF32 get(const PxU32 i, const PxU32 j) const
	{
		PX_ASSERT(i<mSize);
		PX_ASSERT(j<mSize);
		return mValues[i][j];
	}
	PX_FORCE_INLINE void set(const PxU32 i, const PxU32 j, const PxF32 val)
	{
		PX_ASSERT(i<mSize);
		PX_ASSERT(j<mSize);
		mValues[i][j]=val;
	}

	PX_FORCE_INLINE PxU32 getSize() const {return mSize;}

public:

	PxF32 mValues[MAX_VECTORN_SIZE][MAX_VECTORN_SIZE];
	PxU32 mSize;
};

 bool isValid(const MatrixNN& A, const VectorN& b, const VectorN& result) 
{
	PX_ASSERT(A.getSize()==b.getSize());
	PX_ASSERT(A.getSize()==result.getSize());
	const PxU32 size=A.getSize();

	//r=A*result-b
	VectorN r(size);
	for(PxU32 i=0;i<size;i++)
	{
		r[i]=-b[i];
		for(PxU32 j=0;j<size;j++)
		{
			r[i]+=A.get(i,j)*result[j];
		}
	}

	PxF32 rLength=0;
	PxF32 bLength=0;
	for(PxU32 i=0;i<size;i++)
	{
		rLength+=r[i]*r[i];
		bLength+=b[i]*b[i];
	}
	const PxF32 error=PxSqrt(rLength/(bLength+1e-5f));
	return (error<1e-5f);
}

class MatrixNNLUSolver
{
private:

	PxU32 mIndex[MAX_VECTORN_SIZE];
	MatrixNN mLU;

public:

	MatrixNNLUSolver(){}
	~MatrixNNLUSolver(){}

	//Algorithm taken from Numerical Recipes in Fortran 77, page 38

	void decomposeLU(const MatrixNN& a)
	{
#define TINY (1.0e-20f)

		const PxU32 size=a.mSize;

		//Copy a into result then work exclusively on the result matrix.
		MatrixNN LU=a;

		//Initialise index swapping values.
		for(PxU32 i=0;i<size;i++)
		{
			mIndex[i]=0xffffffff;
		}

		PxU32 imax=0;
		PxF32 big,dum,sum;
		PxF32 vv[MAX_VECTORN_SIZE];
		PxF32 d=1.0f;

		for(PxU32 i=0;i<=(size-1);i++)
		{
			big=0.0f;
			for(PxU32 j=0;j<=(size-1);j++)
			{
				const PxF32 temp=PxAbs(LU.get(i,j));
				big = temp>big ? temp : big;
			}
			PX_ASSERT(big!=0.0f);
			vv[i]=1.0f/big;
		}

		for(PxU32 j=0;j<=(size-1);j++)
		{
			for(PxU32 i=0;i<j;i++)
			{
				PxF32 sum=LU.get(i,j);
				for(PxU32 k=0;k<i;k++)
				{
					sum-=LU.get(i,k)*LU.get(k,j);
				}
				LU.set(i,j,sum);
			}

			big=0.0f;
			for(PxU32 i=j;i<=(size-1);i++)
			{
				sum=LU.get(i,j);
				for(PxU32 k=0;k<j;k++)
				{
					sum-=LU.get(i,k)*LU.get(k,j);
				}
				LU.set(i,j,sum);
				dum=vv[i]*PxAbs(sum);
				if(dum>=big)
				{
					big=dum;
					imax=i;
				}
			}

			if(j!=imax)
			{
				for(PxU32 k=0;k<size;k++)
				{
					dum=LU.get(imax,k);
					LU.set(imax,k,LU.get(j,k));
					LU.set(j,k,dum);
				}
				d=-d;
				vv[imax]=vv[j];
			}
			mIndex[j]=imax;

			if(LU.get(j,j)==0)
			{
				LU.set(j,j,TINY);
			}

			if(j!=(size-1))
			{
				dum=1.0f/LU.get(j,j);
				for(PxU32 i=j+1;i<=(size-1);i++)
				{
					LU.set(i,j,LU.get(i,j)*dum);
				}
			}
		}

		//Store the result.
		mLU=LU;
	}

	void solve(const VectorN& input, VectorN& result) const
	{
		const PxU32 size=input.getSize();

		result=input;

		PxU32 ip;
		PxU32 ii=0xffffffff;
		PxF32 sum;

		for(PxU32 i=0;i<size;i++)
		{
			ip=mIndex[i];
			sum=result[ip];
			result[ip]=result[i];
			if(ii!=-1)
			{
				for(PxU32 j=ii;j<=(i-1);j++)
				{
					sum-=mLU.get(i,j)*result[j];
				}
			}
			else if(sum!=0)
			{
				ii=i;
			}
			result[i]=sum;
		}
		for(PxI32 i=size-1;i>=0;i--)
		{
			sum=result[i];
			for(PxU32 j=i+1;j<=(size-1);j++)
			{
				sum-=mLU.get(i,j)*result[j];
			}
			result[i]=sum/mLU.get(i,i);
		}
	}
};


void solveDrive4WInternaDynamicsEnginePlusDrivenWheels
(const PxF32 subTimestep, 
 const PxF32 brake, const PxF32 handbrake,
 const PxF32 K, const PxF32 G, 
 const PxF32 engineDriveTorque, const PxF32 engineDampingRate,
 const PxF32* PX_RESTRICT diffTorqueRatios, const PxF32* PX_RESTRICT aveWheelSpeedContributions, 
 const PxF32* PX_RESTRICT brakeTorques, const bool* PX_RESTRICT isBrakeApplied, const PxF32* PX_RESTRICT tireTorques,
 const PxVehicleDriveSimData4W& vehCoreSimData, const PxVehicleWheels4SimData& vehSuspWheelTire4SimData,
 PxVehicleDriveDynData& vehCore, PxVehicleWheels4DynData& vehSuspWheelTire4)
{
	const PxF32 KG=K*G;
	const PxF32 KGG=K*G*G;

	MatrixNN A(4+1);
	VectorN b(4+1);

	const PxVehicleEngineData& engineData=vehCoreSimData.getEngineData();

	const PxF32* PX_RESTRICT wheelSpeeds=vehSuspWheelTire4.mWheelSpeeds;
	const PxF32 engineOmega=vehCore.mEnginespeed;

	//Wheels.
	{
		for(PxU32 i=0;i<4;i++)
		{
			const PxF32 dt=subTimestep*vehSuspWheelTire4SimData.getWheelData(i).getRecipMOI();
			const PxF32 R=diffTorqueRatios[i];
			const PxF32 dtKGGR=dt*KGG*R;
			A.set(i,0,dtKGGR*aveWheelSpeedContributions[0]);
			A.set(i,1,dtKGGR*aveWheelSpeedContributions[1]);
			A.set(i,2,dtKGGR*aveWheelSpeedContributions[2]);
			A.set(i,3,dtKGGR*aveWheelSpeedContributions[3]);
			A.set(i,i,1.0f+dtKGGR*aveWheelSpeedContributions[i]+dt*vehSuspWheelTire4SimData.getWheelData(i).mDampingRate);
			A.set(i,4,-dt*KG*R);
			b[i] = wheelSpeeds[i] + dt*(brakeTorques[i]+tireTorques[i]);
		}
	}

	//Engine.
	{
		const PxF32 dt=subTimestep;
		const PxF32 dtKG=dt*K*G;
		A.set(4,0,-dtKG*aveWheelSpeedContributions[0]);
		A.set(4,1,-dtKG*aveWheelSpeedContributions[1]);
		A.set(4,2,-dtKG*aveWheelSpeedContributions[2]);
		A.set(4,3,-dtKG*aveWheelSpeedContributions[3]);
		A.set(4,4,1.0f + dt*(K+engineDampingRate));
		b[4] = engineOmega + dt*engineDriveTorque;
	}

	//Solve Aw=b
	VectorN result(4+1);
	MatrixNNLUSolver solver;
	solver.decomposeLU(A);
	solver.solve(b,result);
	PX_ASSERT(isValid(A,b,result));

	//Check for sanity in the resultant internal rotation speeds.
	//If the brakes are on and the wheels have switched direction then lock them at zero.
	//newOmega=result[i], oldOmega=wheelSpeeds[i], if newOmega*oldOmega<=0 and isBrakeApplied then lock wheel.
	result[0]=(isBrakeApplied[0] && (wheelSpeeds[0]*result[0]<=0)) ? 0.0f : result[0];
	result[1]=(isBrakeApplied[1] && (wheelSpeeds[1]*result[1]<=0)) ? 0.0f : result[1];
	result[2]=(isBrakeApplied[2] && (wheelSpeeds[2]*result[2]<=0)) ? 0.0f : result[2];
	result[3]=(isBrakeApplied[3] && (wheelSpeeds[3]*result[3]<=0)) ? 0.0f : result[3];
	//Clamp the engine revs.
	result[4]=PxClamp(result[4],0.0f,engineData.mMaxOmega);

	//Copy back to the car's internal rotation speeds.
	vehSuspWheelTire4.mWheelSpeeds[0]=result[0];
	vehSuspWheelTire4.mWheelSpeeds[1]=result[1];
	vehSuspWheelTire4.mWheelSpeeds[2]=result[2];
	vehSuspWheelTire4.mWheelSpeeds[3]=result[3];
	vehCore.mEnginespeed=result[4];
}

void solveTankInternaDynamicsEnginePlusDrivenWheels
(const PxF32 subTimestep, 
 const PxF32 K, const PxF32 G, 
 const PxF32 engineDriveTorque, const PxF32 engineDampingRate,
 const PxF32* PX_RESTRICT diffTorqueRatios, const PxF32* PX_RESTRICT aveWheelSpeedContributions, const PxF32* PX_RESTRICT wheelGearings, 
 const PxF32* PX_RESTRICT brakeTorques, const bool* PX_RESTRICT isBrakeApplied, const PxF32* PX_RESTRICT tireTorques,
 const PxVehicleWheels4SimData* PX_RESTRICT wheels4SimDatas, PxVehicleWheels4DynData* PX_RESTRICT wheels4DynDatas, const PxU32 numWheels4, const PxU32 numActiveWheels, 
 const PxVehicleDriveSimData& driveSimData, PxVehicleDriveDynData& driveDynData)
{
	const PxF32 KG=K*G;
	const PxF32 KGG=K*G*G;

	//Rearrange data in a single array rather than scattered in blocks of 4.
	//This makes it easier later on.
	PxF32 recipMOI[PX_MAX_NUM_WHEELS];
	PxF32 dampingRates[PX_MAX_NUM_WHEELS];
	PxF32 wheelSpeeds[PX_MAX_NUM_WHEELS];
	PxF32 wheelRecipRadii[PX_MAX_NUM_WHEELS];

	for(PxU32 i=0;i<numWheels4-1;i++)
	{
		const PxVehicleWheelData& wheelData0=wheels4SimDatas[i].getWheelData(0);
		const PxVehicleWheelData& wheelData1=wheels4SimDatas[i].getWheelData(1);
		const PxVehicleWheelData& wheelData2=wheels4SimDatas[i].getWheelData(2);
		const PxVehicleWheelData& wheelData3=wheels4SimDatas[i].getWheelData(3);

		recipMOI[4*i+0]=wheelData0.getRecipMOI();
		recipMOI[4*i+1]=wheelData1.getRecipMOI();
		recipMOI[4*i+2]=wheelData2.getRecipMOI();
		recipMOI[4*i+3]=wheelData3.getRecipMOI();

		dampingRates[4*i+0]=wheelData0.mDampingRate;
		dampingRates[4*i+1]=wheelData1.mDampingRate;
		dampingRates[4*i+2]=wheelData2.mDampingRate;
		dampingRates[4*i+3]=wheelData3.mDampingRate;

		wheelRecipRadii[4*i+0]=wheelData0.getRecipRadius();
		wheelRecipRadii[4*i+1]=wheelData1.getRecipRadius();
		wheelRecipRadii[4*i+2]=wheelData2.getRecipRadius();
		wheelRecipRadii[4*i+3]=wheelData3.getRecipRadius();

		const PxVehicleWheels4DynData& suspWheelTire4=wheels4DynDatas[i];
		wheelSpeeds[4*i+0]=suspWheelTire4.mWheelSpeeds[0];
		wheelSpeeds[4*i+1]=suspWheelTire4.mWheelSpeeds[1];
		wheelSpeeds[4*i+2]=suspWheelTire4.mWheelSpeeds[2];
		wheelSpeeds[4*i+3]=suspWheelTire4.mWheelSpeeds[3];
	}
	const PxU32 numInLastBlock = 4 - (4*numWheels4 - numActiveWheels);
	for(PxU32 i=0;i<numInLastBlock;i++)
	{
		const PxVehicleWheelData& wheelData=wheels4SimDatas[numWheels4-1].getWheelData(i);
		recipMOI[4*(numWheels4-1)+i]=wheelData.getRecipMOI();
		dampingRates[4*(numWheels4-1)+i]=wheelData.mDampingRate;
		wheelRecipRadii[4*(numWheels4-1)+i]=wheelData.getRecipRadius();

		const PxVehicleWheels4DynData& suspWheelTire4=wheels4DynDatas[numWheels4-1];
		wheelSpeeds[4*(numWheels4-1)+i]=suspWheelTire4.mWheelSpeeds[i];
	}
	const PxF32 wheelRadius0=wheels4SimDatas[0].getWheelData(0).mRadius;
	const PxF32 wheelRadius1=wheels4SimDatas[0].getWheelData(1).mRadius;

	//Matrix M and rhs vector b that we use to solve Mw=b.
	MatrixNN M(numActiveWheels+1);
	VectorN b(numActiveWheels+1);

	//Wheels.
	{
		for(PxU32 i=0;i<numActiveWheels;i++)
		{
			const PxF32 dt=subTimestep*recipMOI[i];
			const PxF32 R=diffTorqueRatios[i];
			const PxF32 g=wheelGearings[i];
			const PxF32 dtKGGRg=dt*KGG*R*g;
			for(PxU32 j=0;j<numActiveWheels;j++)
			{
				M.set(i,j,dtKGGRg*aveWheelSpeedContributions[j]*wheelGearings[j]);
			}
			M.set(i,i,1.0f+dtKGGRg*aveWheelSpeedContributions[i]*wheelGearings[i]+dt*dampingRates[i]);
			M.set(i,numActiveWheels,-dt*KG*R*g);
			b[i] = wheelSpeeds[i] + dt*(brakeTorques[i]+tireTorques[i]);
		}
	}

	//Engine.
	{
		//const PxVehicleEngineData& engineData=driveSimData.getEngineData();
		const PxF32 engineOmega=driveDynData.mEnginespeed;

		const PxF32 dt=subTimestep;
		const PxF32 dtKG=dt*K*G;
		for(PxU32 i=0;i<numActiveWheels;i++)
		{
			M.set(numActiveWheels,i,-dtKG*aveWheelSpeedContributions[i]*wheelGearings[i]);
		}
		M.set(numActiveWheels,numActiveWheels,1.0f + dt*(K+engineDampingRate));
		b[numActiveWheels] = engineOmega + dt*engineDriveTorque;
	}

	//Now apply the constraints that all the odd numbers are equal and all the even numbers are equal.
	//ie w2,w4,w6 are all equal to w0 and w3,w5,w7 are all equal to w1.
	//That leaves (4*N+1) equations but only 3 unknowns: two wheels speeds and the engine speed.
	//Substitute these extra constraints into the matrix.
	MatrixNN A(numActiveWheels+1);
	for(PxU32 i=0;i<numActiveWheels+1;i++)
	{
		PxF32 sum0=M.get(i,0+0);
		PxF32 sum1=M.get(i,0+1);
		for(PxU32 j=2;j<numActiveWheels;j+=2)
		{
			sum0+=M.get(i,j+0)*wheelRadius0*wheelRecipRadii[j+0];
			sum1+=M.get(i,j+1)*wheelRadius1*wheelRecipRadii[j+1];
		}
		A.set(i,0,sum0);
		A.set(i,1,sum1);
		A.set(i,2,M.get(i,numActiveWheels));
	}
	
	//We have an over-determined problem because of the extra constraints 
	//on equal wheel speeds. Solve using the least squares method as in
	//http://s-mat-pcs.oulu.fi/~mpa/matreng/ematr5_5.htm

	//Work out the transpose
	MatrixNN AT(numActiveWheels+1);
	for(PxU32 i=0;i<numActiveWheels+1;i++)
	{
		for(PxU32 j=0;j<3;j++)
		{
			AT.set(j,i,A.get(i,j));
		}
	}

	//Compute A^T*A
	MatrixNN ATA(3);
	for(PxU32 i=0;i<3;i++)
	{
		for(PxU32 j=0;j<3;j++)
		{
			PxF32 sum=0.0f;
			for(PxU32 k=0;k<numActiveWheels+1;k++)
			{
				sum+=AT.get(i,k)*A.get(k,j);
			}
			ATA.set(i,j,sum);
		}
	}

	//Compute A^T*b;
	VectorN ATb(3);
	for(PxU32 i=0;i<3;i++)
	{
		PxF32 sum=0;
		for(PxU32 j=0;j<numActiveWheels+1;j++)
		{
			sum+=AT.get(i,j)*b[j];
		}
		ATb[i]=sum;
	}

	//Solve (A^T*A)*x = A^T*b
	MatrixNNLUSolver solver;
	VectorN result(3);
	solver.decomposeLU(ATA);
	solver.solve(ATb,result);

	//Clamp the engine revs between zero and maxOmega
	const PxF32 maxEngineOmega=driveSimData.getEngineData().mMaxOmega;
	const PxF32 newEngineOmega=PxClamp(result[2],0.0f,maxEngineOmega);

	//Apply the constraints on each of the equal wheel speeds.
	PxF32 wheelSpeedResults[PX_MAX_NUM_WHEELS];
	wheelSpeedResults[0]=result[0];
	wheelSpeedResults[1]=result[1];
	for(PxU32 i=2;i<numActiveWheels;i+=2)
	{
		wheelSpeedResults[i+0]=result[0];
		wheelSpeedResults[i+1]=result[1];
	}
	
	//Check for sanity in the resultant internal rotation speeds.
	//If the brakes are on and the wheels have switched direction then lock them at zero.
	for(PxU32 i=0;i<numActiveWheels;i++)
	{
		const PxF32 oldOmega=wheelSpeeds[i];
		const PxF32 newOmega=wheelSpeedResults[i];
		const bool hasBrake=isBrakeApplied[i];
		if(hasBrake && (oldOmega*newOmega <= 0))
		{
			wheelSpeedResults[i]=0.0f;
		}
	}

	//Copy back to the car's internal rotation speeds.
	for(PxU32 i=0;i<numWheels4-1;i++)
	{
		wheels4DynDatas[i].mWheelSpeeds[0]=wheelSpeedResults[4*i+0];
		wheels4DynDatas[i].mWheelSpeeds[1]=wheelSpeedResults[4*i+1];
		wheels4DynDatas[i].mWheelSpeeds[2]=wheelSpeedResults[4*i+2];
		wheels4DynDatas[i].mWheelSpeeds[3]=wheelSpeedResults[4*i+3];
	}
	for(PxU32 i=0;i<numInLastBlock;i++)
	{
		wheels4DynDatas[numWheels4-1].mWheelSpeeds[i]=wheelSpeedResults[4*(numWheels4-1)+i];
	}
	driveDynData.mEnginespeed=newEngineOmega;
}

void integrateWheelRotationVelocities
(const PxF32 subTimestep, 
 const PxF32 brake, const PxF32 handbrake, const PxF32* PX_RESTRICT tireTorques, const PxF32* PX_RESTRICT brakeTorques, 
 const PxVehicleWheels4SimData& vehSuspWheelTire4SimData, PxVehicleWheels4DynData& vehSuspWheelTire4)
{
	for(PxU32 i=0;i<4;i++)
	{
		//Compute the new angular speed of the wheel.
		const PxF32 oldOmega=vehSuspWheelTire4.mWheelSpeeds[i];
		const PxF32 newOmega=oldOmega+subTimestep*(tireTorques[i]+brakeTorques[i]);

		//Has the brake been applied?  It's hard to tell from brakeTorques[j] because that 
		//will be zero if the wheel is locked. Work it out from the brake and handbrake data.
		const PxF32 brakeGain=vehSuspWheelTire4SimData.getWheelData(i).mMaxBrakeTorque;
		const PxF32 handbrakeGain=vehSuspWheelTire4SimData.getWheelData(i).mMaxHandBrakeTorque;

		//Work out if the wheel should be locked.
		const bool brakeApplied=((brake*brakeGain + handbrake*handbrakeGain)!=0.0f);
		const bool wheelReversed=(oldOmega*newOmega <=0);
		const bool wheelLocked=(brakeApplied && wheelReversed);

		//Lock the wheel or apply its new angular speed.
		if(!wheelLocked)
		{
			vehSuspWheelTire4.mWheelSpeeds[i]=newOmega;
		}
		else
		{
			vehSuspWheelTire4.mWheelSpeeds[i]=0.0f;
		}
	}
}

void integrateWheelRotationAngles
(const PxF32 timestep,
 const PxF32 K, const PxF32 G, const PxF32 engineDriveTorque,
 const PxF32* PX_RESTRICT jounces, const PxF32* PX_RESTRICT diffTorqueRatios, const PxF32* PX_RESTRICT forwardSpeeds, const bool* isBrakeApplied,
 const PxVehicleDriveSimData& vehCoreSimData, const PxVehicleWheels4SimData& vehSuspWheelTire4SimData,
 PxVehicleDriveDynData& vehCore, PxVehicleWheels4DynData& vehSuspWheelTire4)
{
	const PxF32 KG=K*G;

	PxF32* PX_RESTRICT wheelSpeeds=vehSuspWheelTire4.mWheelSpeeds;
	PxF32* PX_RESTRICT wheelRotationAngles=vehSuspWheelTire4.mWheelRotationAngles;

	for(PxU32 j=0;j<4;j++)
	{
		//At low vehicle forward speeds we have some numerical difficulties getting the 
		//wheel rotation speeds to be correct due to the tire model's difficulties at low vz.
		//The solution is to blend between the rolling speed at the wheel and the wheel's actual rotation speed.
		//If the wheel is 
		//(i)   in the air, 
		//(ii)  under braking torque, 
		//(iii) driven by the engine through the gears and diff
		//then always use the wheel's actual rotation speed.
		//Just to be clear, this means we will blend when the wheel
		//(i)   is on the ground
		//(ii)  has no brake applied
		//(iii) has no drive torque applied from the clutch
		//(iv)  is at low forward speed
		PxF32 wheelOmega=wheelSpeeds[j];
		if(jounces[j] > -vehSuspWheelTire4SimData.getSuspensionData(j).mMaxDroop &&	//(i)   wheel touching ground
			false==isBrakeApplied[j] &&												//(ii)  no brake applied
			0.0f==diffTorqueRatios[j]*KG*engineDriveTorque &&						//(iii) no drive torque applied
			PxAbs(forwardSpeeds[j])<gThresholdForwardSpeedForWheelAngleIntegration)	//(iv)  low speed
		{
			const PxF32 recipWheelRadius=vehSuspWheelTire4SimData.getWheelData(j).getRecipRadius();
			const PxF32 alpha=PxAbs(forwardSpeeds[j])*gRecipThresholdForwardSpeedForWheelAngleIntegration;
			wheelOmega = (forwardSpeeds[j]*recipWheelRadius)*(1.0f-alpha) + wheelOmega*alpha;

			//TODO: maybe just set the car wheel omega to the blended value?
			//Not sure about this bit.  
			wheelSpeeds[j]=wheelOmega;
		}

		PxF32 newRotAngle=wheelRotationAngles[j]+wheelOmega*timestep;
		//Clamp the wheel rotation angle to a range (-10*pi,10*pi) to stop it getting crazily big.
		newRotAngle=physx::intrinsics::fsel(newRotAngle-10*PxPi, newRotAngle-10*PxPi, physx::intrinsics::fsel(-newRotAngle-10*PxPi, newRotAngle + 10*PxPi, newRotAngle));
		wheelRotationAngles[j]=newRotAngle;
	}
}

PX_FORCE_INLINE void storeJounceSlipFrictionSurfTypeSurfMatl
(const PxF32* PX_RESTRICT jounces, const PxF32* PX_RESTRICT longSlips,const PxF32* PX_RESTRICT latSlips, const PxF32* PX_RESTRICT tireFrictions, const PxU32* PX_RESTRICT tireSurfaceTypes, const PxMaterial*const* PX_RESTRICT tireSurfaceMaterials,
 PxVehicleWheels4DynData& vehSuspWheelTire4)
{
	for(PxU32 j=0;j<4;j++)
	{
		vehSuspWheelTire4.mSuspJounces[j]=jounces[j];
		vehSuspWheelTire4.mLongSlips[j]=longSlips[j];
		vehSuspWheelTire4.mLatSlips[j]=latSlips[j];
		vehSuspWheelTire4.mTireFrictions[j]=tireFrictions[j];
		vehSuspWheelTire4.mTireSurfaceTypes[j]=tireSurfaceTypes[j];
		vehSuspWheelTire4.mTireSurfaceMaterials[j]=tireSurfaceMaterials[j];
	}
}
void poseWheels
(const PxVehicleWheels4SimData& vehSuspWheelTire4SimData, const PxVehicleWheels4DynData& vehSuspWheelTire4, const PxF32* PX_RESTRICT steerAngles, const PxU8* wheelShapes, const PxU32 numWheelsToPose,
 PxRigidDynamic* vehActor)
{
	const PxF32* PX_RESTRICT jounces=vehSuspWheelTire4.mSuspJounces;
	const PxF32* PX_RESTRICT rotAngles=vehSuspWheelTire4.mWheelRotationAngles;
	const PxVec3 cmOffset=vehActor->getCMassLocalPose().p;
	for(PxU32 i=0;i<numWheelsToPose;i++)
	{
		if(wheelShapes[i]!=PX_MAX_U8)
		{
			//Get the shape.
			const PxU32 shapeIndex=wheelShapes[i];
			PxShape* shapeBuffer[1];
			vehActor->getShapes(shapeBuffer,1,shapeIndex);

			//Compute the transform of the wheel shapes. 
			const PxVec3 pos=cmOffset+vehSuspWheelTire4SimData.getWheelCentreOffset(i)-vehSuspWheelTire4SimData.getSuspTravelDirection(i)*jounces[i];
			const PxQuat quat(steerAngles[i], gUp);
			const PxQuat quat2(rotAngles[i],quat.rotate(gRight));
			const PxTransform t(pos,quat2*quat);

			//Pose the shape
			shapeBuffer[0]->setLocalPose(t);
		}
	}
}

template<class T> PX_FORCE_INLINE void initialiseAll4(const PxU32 numSuspWheelTire4, T* PX_RESTRICT valArray, const T val)
{
	for(PxU32 i=0;i<numSuspWheelTire4;i++)
	{
		valArray[4*i+0]=val;
		valArray[4*i+1]=val;
		valArray[4*i+2]=val;
		valArray[4*i+3]=val;
	}
}

template<class T> PX_FORCE_INLINE void initialiseAll4(const PxU32 numSuspWheelTire4, PxF32* PX_RESTRICT valArray, const T evenVal, const T oddVal)
{
	for(PxU32 i=0;i<numSuspWheelTire4;i++)
	{
		valArray[4*i+0]=evenVal;
		valArray[4*i+1]=oddVal;
		valArray[4*i+2]=evenVal;
		valArray[4*i+3]=oddVal;
	}
}

class PxVehicleUpdate
{
public:

#if PX_DEBUG_VEHICLE_ON
	static void updateSingleVehicleAndStoreTelemetryData(
		const PxF32 timestep, const PxVec3& gravity, const PxVehicleDrivableSurfaceToTireFrictionPairs& vehicleDrivableSurfaceToTireFrictionPairs, 
		PxVehicleWheels* focusVehicle, PxVehicleTelemetryData& telemetryData);
#endif

	static void update(
		const PxF32 timestep, const PxVec3& gravity, const PxVehicleDrivableSurfaceToTireFrictionPairs& vehicleDrivableSurfaceToTireFrictionPairs, 
		const PxU32 numVehicles, PxVehicleWheels** vehicles);

	static void suspensionRaycasts(
		PxBatchQuery* batchQuery, 
		const PxU32 numVehicles, PxVehicleWheels** vehicles, const PxU32 numSceneQueryResults, PxRaycastQueryResult* sceneQueryResults);

	static void updateDrive4W(
		const PxF32 timestep, 
		const PxVec3& gravity, const PxF32 gravityMagnitude, const PxF32 recipGravityMagnitude, 
		const PxVehicleDrivableSurfaceToTireFrictionPairs& drivableSurfaceToTireFrictionPairs,
		PxVehicleDrive4W* vehDrive4W);

	static void updateTank(
		const PxF32 timestep, 
		const PxVec3& gravity, const PxF32 gravityMagnitude, const PxF32 recipGravityMagnitude, 
		const PxVehicleDrivableSurfaceToTireFrictionPairs& drivableSurfaceToTireFrictionPairs,
		PxVehicleDriveTank* vehDriveTank);
};

void PxVehicleUpdate::updateDrive4W(
const PxF32 timestep, 
const PxVec3& gravity, const PxF32 gravityMagnitude, const PxF32 recipGravityMagnitude,
const PxVehicleDrivableSurfaceToTireFrictionPairs& drivableSurfaceToTireFrictionPairs,
PxVehicleDrive4W* vehDrive4W)
{
	PX_CHECK_AND_RETURN(
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_ACCEL]>-0.01f && 
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_ACCEL]<1.01f, 
		"Illegal vehicle control value - accel must be in range (0,1)");
	PX_CHECK_AND_RETURN(
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_BRAKE]>-0.01f && 
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_BRAKE]<1.01f, 
		"Illegal vehicle control value - brake must be in range (0,1)");
	PX_CHECK_AND_RETURN(
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE]>-0.01f && 
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE]<1.01f, 
		"Illegal vehicle control value - handbrake must be in range (0,1)");
	PX_CHECK_AND_RETURN(
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT]>-1.01f && 
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT]<1.01f, 
		"Illegal vehicle control value - left steer must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT]>-1.01f && 
		vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT]<1.01f, 
		"Illegal vehicle control value - right steer must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		PxAbs(vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT]-
			  vehDrive4W->mDriveDynData.mControlAnalogVals[PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT])<1.01f, 
		"Illegal vehicle control value - right steer value minus left steer value must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		!(vehDrive4W->getRigidDynamicActor()->getRigidDynamicFlags() & PxRigidDynamicFlag::eKINEMATIC),
		"Attempting to update a drive4W with a kinematic actor - this isn't allowed");

#if PX_DEBUG_VEHICLE_ON
	for(PxU32 i=0;i<vehDrive4W->mWheelsSimData.mNumWheels4;i++)
	{
		updateGraphDataInternalWheelDynamics(4*i,vehDrive4W->mWheelsDynData.mWheels4DynData[i].mWheelSpeeds);
	}
	updateGraphDataInternalEngineDynamics(vehDrive4W->mDriveDynData.mEnginespeed);
#endif

	//Unpack the vehicle.
	//Unpack the tank simulation and instanced dynamics components.
	const PxVehicleWheels4SimData* wheels4SimDatas=vehDrive4W->mWheelsSimData.mWheels4SimData;
	const PxVehicleTireLoadFilterData& tireLoadFilterData=vehDrive4W->mWheelsSimData.mNormalisedLoadFilter;
	PxVehicleWheels4DynData* wheels4DynDatas=vehDrive4W->mWheelsDynData.mWheels4DynData;
	const PxU32 numWheels4=vehDrive4W->mWheelsSimData.mNumWheels4;
	const PxU32 numActiveWheels=vehDrive4W->mWheelsSimData.mNumActiveWheels;
	const PxU32 numActiveWheelsInLast4=4-(4*numWheels4 - numActiveWheels);
	const PxVehicleDriveSimData4W driveSimData=vehDrive4W->mDriveSimData;
	PxVehicleDriveDynData& driveDynData=vehDrive4W->mDriveDynData;
	PxRigidDynamic* vehActor=vehDrive4W->mActor;

	//In each block of 4 wheels record how many wheels are active.
	PxU32 numActiveWheelsPerBlock4[PX_MAX_NUM_SUSPWHEELTIRE4]={0,0,0,0,0};
	numActiveWheelsPerBlock4[0]=PxMin(numActiveWheels,(PxU32)4);
	for(PxU32 i=1;i<numWheels4-1;i++)
	{
		numActiveWheelsPerBlock4[i]=4;
	}
	numActiveWheelsPerBlock4[numWheels4-1]=numActiveWheelsInLast4;
	PX_ASSERT(numActiveWheels == numActiveWheelsPerBlock4[0] + numActiveWheelsPerBlock4[1] + numActiveWheelsPerBlock4[2] + numActiveWheelsPerBlock4[3] + numActiveWheelsPerBlock4[4]); 

	//Organise the shader data in blocks of 4.
	PxVehicleTireForceCalculator4 tires4ForceCalculators[PX_MAX_NUM_SUSPWHEELTIRE4];
	for(PxU32 i=0;i<numWheels4;i++)
	{
		tires4ForceCalculators[i].mShaderData[0]=vehDrive4W->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+0];
		tires4ForceCalculators[i].mShaderData[1]=vehDrive4W->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+1];
		tires4ForceCalculators[i].mShaderData[2]=vehDrive4W->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+2];
		tires4ForceCalculators[i].mShaderData[3]=vehDrive4W->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+3];
		tires4ForceCalculators[i].mShader=vehDrive4W->mWheelsDynData.mTireForceCalculators->mShader;
	}

	//Mark the constraints as dirty to force them to be updated in the sdk.
	for(PxU32 i=0;i<numWheels4;i++)
	{
		wheels4DynDatas[i].getVehicletConstraintShader().mConstraint->markDirty();
	}

	//Compute the transform of the center of mass.
	PxTransform carChassisTransform;
	{
		carChassisTransform=vehActor->getGlobalPose().transform(vehActor->getCMassLocalPose());
	}

	//Update the auto-box and decide whether to change gear up or down.
	if(driveDynData.mUseAutoGears)
	{
		processAutoBox(timestep,driveSimData,driveDynData);
	}

	//Process gear-up/gear-down commands.
	{
		const PxVehicleGearsData& gearsData=driveSimData.getGearsData();
		processGears(timestep,gearsData,driveDynData);
	}

	//Clutch strength;
	PxF32 K;
	{
		const PxVehicleClutchData& clutchData=driveSimData.getClutchData();
		const PxU32 currentGear=driveDynData.mCurrentGear;
		K=computeClutchStrength(clutchData, currentGear);
	}

	//Gear ratio.
	PxF32 G;
	PxU32 currentGear;
	{
		const PxVehicleGearsData& gearsData=driveSimData.getGearsData();
		currentGear=driveDynData.mCurrentGear;
		G=computeGearRatio(gearsData,currentGear);
#if PX_DEBUG_VEHICLE_ON
		updateGraphDataGearRatio(G);
#endif
	}

	//Retrieve control values from vehicle controls.
	PxF32 accel,brake,handbrake,steerLeft,steerRight;
	PxF32 steer;
	bool isIntentionToAccelerate;
	{
		getVehicleControlValues(driveDynData,accel,brake,handbrake,steerLeft,steerRight);
		steer=steerRight-steerLeft;
		isIntentionToAccelerate = (accel>0.0f && 0.0f==brake && 0.0f==handbrake);
#if PX_DEBUG_VEHICLE_ON
		updateGraphDataControlInputs(accel,brake,handbrake,steerLeft,steerRight);
#endif
	}

	//Get the drive wheels (the first 4 wheels are the drive wheels).
	const PxVehicleWheels4SimData& wheels4SimData=wheels4SimDatas[0];
	PxVehicleWheels4DynData& wheels4DynData=wheels4DynDatas[0];
	const PxVehicleTireForceCalculator4& tires4ForceCalculator=tires4ForceCalculators[0];

	//Contribution of each driven wheel to average wheel speed at clutch.
	//With 4 driven wheels the average wheel speed at clutch is 
	//wAve = alpha0*w0 + alpha1*w1 + alpha2*w2 + alpha3*w3.
	//This next bit of code computes alpha0,alpha1,alpha2,alpha3.
	//For rear wheel drive alpha0=alpha1=0
	//For front wheel drive alpha2=alpha3=0
	PxF32 aveWheelSpeedContributions[4]={0.0f,0.0f,0.0f,0.0f};
	{
		const PxVehicleDifferential4WData& diffData=driveSimData.getDiffData();
		computeDiffAveWheelSpeedContributions(diffData,handbrake,aveWheelSpeedContributions);
#if PX_DEBUG_VEHICLE_ON
		updateGraphDataClutchSlip(wheels4DynData.mWheelSpeeds,aveWheelSpeedContributions,driveDynData.mEnginespeed,G);
#endif
	}

	//Ackermann-corrected steering angles.
	//http://en.wikipedia.org/wiki/Ackermann_steering_geometry
	PxF32 steerAngles[4]={0.0f,0.0f,0.0f,0.0f};
	{
		computeAckermannCorrectedSteerAngles(driveSimData,wheels4SimData,steer,steerAngles);
	}

	//Ready to do the update.
	PxVec3 carChassisLinVel=vehActor->getLinearVelocity();
	PxVec3 carChassisAngVel=vehActor->getAngularVelocity();
	const PxU32 numSubSteps=2;
	const PxF32 timeFraction=1.0f/(1.0f*numSubSteps);
	const PxF32 subTimestep=timestep*timeFraction;
	for(PxU32 k=0;k<numSubSteps;k++)
	{
		//Set the force and torque for the current update to zero.
		PxVec3 chassisForce(0,0,0);
		PxVec3 chassisTorque(0,0,0);

		//Bit of a trick here.
		//The sdk will apply gravity*dt completely independent of the tire forces.
		//Cars will never come to rest this way because even if the tire model brings the car
		//exactly to rest it will just be immediately perturbed by gravitational acceleration.
		//Maybe we should add gravity here before computing the tire forces so that the tire
		//forces act against the gravitational forces that will be later applied.  
		//We don't actually ever apply gravity to the rigid body, we just imagine the tire/susp 
		//forces that would be needed if gravity had already been applied.  The sdk, therefore, 
		//still needs to apply gravity to the chassis rigid body in its update.
		PX_ASSERT(carChassisLinVel==vehActor->getLinearVelocity());
		carChassisLinVel+=gravity*subTimestep;

		//Diff torque ratios needed (how we split the torque between the drive wheels).
		//The sum of the torque ratios is always 1.0f.
		//The drive torque delivered to each wheel is the total available drive torque multiplied by the 
		//diff torque ratio for each wheel.
		PxF32 diffTorqueRatios[4]={0.0f,0.0f,0.0f,0.0f};
		computeDiffTorqueRatios(driveSimData.getDiffData(),handbrake,wheels4DynData.mWheelSpeeds,diffTorqueRatios);

		//Compute the brake torques.
		PxF32 brakeTorques[4]={0.0f,0.0f,0.0f,0.0f};
		bool isBrakeApplied[4]={false,false,false,false};
		computeBrakeAndHandBrakeTorques
			(&wheels4SimData.getWheelData(0),wheels4DynData.mWheelSpeeds,brake,handbrake,
			 brakeTorques,isBrakeApplied);

		//Compute jounces, slips, tire forces, suspension forces etc.
		PxF32 jounces[4]={0.0f,0.0f,0.0f,0.0f};
		PxF32 forwardSpeeds[4]={0.0f,0.0f,0.0f,0.0f};
		PxF32 tireFrictions[4]={0.0f,0.0f,0.0f,0.0f};
		PxF32 longSlips[4]={0.0f,0.0f,0.0f,0.0f};
		PxF32 latSlips[4]={0.0f,0.0f,0.0f,0.0f};
		PxU32 tireSurfaceTypes[4]={PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN};
		PxMaterial* tireSurfaceMaterials[4]={NULL,NULL,NULL,NULL};
		PxF32 tireTorques[4]={0.0f,0.0f,0.0f,0.0f};
		processSuspTireWheels
			(timeFraction,
			 carChassisTransform, carChassisLinVel, carChassisAngVel, false,
			 gravity,gravityMagnitude,recipGravityMagnitude, subTimestep,
			 isIntentionToAccelerate,
			 wheels4SimData,tireLoadFilterData,wheels4DynData,tires4ForceCalculator,numActiveWheelsPerBlock4[0],
			 vehActor,
			 steerAngles, isBrakeApplied, 
			 &drivableSurfaceToTireFrictionPairs,
			 0,
			 wheels4DynData.getVehicletConstraintShader().mData, 
			 wheels4DynData.mTireLowForwardSpeedTimers,
			 jounces, forwardSpeeds, tireFrictions, longSlips, latSlips, tireSurfaceTypes,tireSurfaceMaterials,
			 tireTorques,chassisForce,chassisTorque);


		PxF32 engineDriveTorque;
		{
			const PxVehicleEngineData& engineData=driveSimData.getEngineData();
			const PxF32 engineOmega=driveDynData.mEnginespeed;
			engineDriveTorque=computeEngineDriveTorque(engineData,engineOmega,accel);
#if PX_DEBUG_VEHICLE_ON
			updateGraphDataEngineDriveTorque(engineDriveTorque);
#endif
		}

		PxF32 engineDampingRate;
		{
			const PxVehicleEngineData& engineData=driveSimData.getEngineData();
			engineDampingRate=computeEngineDampingRate(engineData,currentGear,accel);
		}

		//Update the wheel and engine speeds - 5x5 matrix coupling engine and wheels.
		solveDrive4WInternaDynamicsEnginePlusDrivenWheels(
			subTimestep, 
			brake,handbrake,
			K,G,
			engineDriveTorque,engineDampingRate,
			diffTorqueRatios,aveWheelSpeedContributions, 
			brakeTorques,isBrakeApplied,tireTorques,
			driveSimData,wheels4SimData,
			driveDynData,wheels4DynData);

		//Integrate wheel rotation angle (theta += omega*dt)
		integrateWheelRotationAngles
			(subTimestep,
			K,G,engineDriveTorque,
			jounces,diffTorqueRatios,forwardSpeeds,isBrakeApplied,
			driveSimData,wheels4SimData,
			driveDynData,wheels4DynData);

		//Store the jounce and slips so they may be queried later.
		storeJounceSlipFrictionSurfTypeSurfMatl
			(jounces,longSlips,latSlips,tireFrictions,tireSurfaceTypes,tireSurfaceMaterials,wheels4DynData);

		//////////////////////////////////////////////////////////////////////////
		//susp and tire forces from extra wheels (non-driven wheels)
		//////////////////////////////////////////////////////////////////////////
		for(PxU32 j=1;j<numWheels4;j++)
		{
			//Only the driven wheels can steer.
			PxF32 extraWheelSteerAngles[4]={0.0f,0.0f,0.0f,0.0f};
			//Only the driven wheels are connected to the diff.
			PxF32 extraWheelsDiffTorqueRatios[4]={0.0f,0.0f,0.0f,0.0f};

			//The extra wheels do have brakes.
			PxF32 extraWheelBrakeTorques[4]={0.0f,0.0f,0.0f,0.0f};
			bool extraIsBrakeApplied[4]={false,false,false,false};
			computeBrakeAndHandBrakeTorques
				(&wheels4SimDatas[j].getWheelData(0),wheels4DynDatas[j].mWheelSpeeds,brake,handbrake,
				extraWheelBrakeTorques,extraIsBrakeApplied);

			PxF32 extraWheelJounces[4]={0.0f,0.0f,0.0f,0.0f};
			PxF32 extraWheelForwardSpeeds[4]={0.0f,0.0f,0.0f,0.0f};
			PxF32 extraWheeTireFrictions[4]={0.0f,0.0f,0.0f,0.0f};
			PxF32 extraWheelLongSlips[4]={0.0f,0.0f,0.0f,0.0f};
			PxF32 extraWheelLatSlips[4]={0.0f,0.0f,0.0f,0.0f};
			PxU32 extraWheelTireSurfaceTypes[4]={PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN};
			PxMaterial* extraWheelTireSurfaceMaterials[4]={NULL,NULL,NULL,NULL};
			PxF32 extraWheelTireTorques[4]={0.0f,0.0f,0.0f,0.0f};
			processSuspTireWheels
			   (timeFraction,
				carChassisTransform, carChassisLinVel, carChassisAngVel, false,
				gravity,gravityMagnitude,recipGravityMagnitude, subTimestep,
				isIntentionToAccelerate,
				wheels4SimDatas[j],tireLoadFilterData,wheels4DynDatas[j],tires4ForceCalculators[j],numActiveWheelsPerBlock4[j],
				vehActor,
				extraWheelSteerAngles, extraIsBrakeApplied,
				&drivableSurfaceToTireFrictionPairs,
				4*j,
				wheels4DynDatas[j].getVehicletConstraintShader().mData, 
				wheels4DynDatas[j].mTireLowForwardSpeedTimers,
				extraWheelJounces, extraWheelForwardSpeeds, extraWheeTireFrictions, extraWheelLongSlips, extraWheelLatSlips, extraWheelTireSurfaceTypes, extraWheelTireSurfaceMaterials,
				extraWheelTireTorques,chassisForce,chassisTorque);

			//Integrate the tire torques (omega += (tireTorque + brakeTorque)*dt)
			integrateWheelRotationVelocities(subTimestep, brake, handbrake, extraWheelTireTorques, extraWheelBrakeTorques, wheels4SimDatas[j], wheels4DynDatas[j]);

			//Integrate wheel rotation angle (theta += omega*dt)
			integrateWheelRotationAngles
				(subTimestep,
				0,0,0,
				extraWheelJounces,extraWheelsDiffTorqueRatios,extraWheelForwardSpeeds,extraIsBrakeApplied,
				driveSimData,wheels4SimDatas[j],
				driveDynData,wheels4DynDatas[j]);

			//Store the jounce and slips so they may be queried later.
			storeJounceSlipFrictionSurfTypeSurfMatl
				(extraWheelJounces,extraWheelLongSlips,extraWheelLatSlips,extraWheeTireFrictions,extraWheelTireSurfaceTypes,extraWheelTireSurfaceMaterials,wheels4DynDatas[j]);
		}

		//Integrate the chassis velocity by applying the accumulated force and torque.
		vehActor->addForce(chassisForce*subTimestep,PxForceMode::eIMPULSE);
		vehActor->addTorque(chassisTorque*subTimestep,PxForceMode::eIMPULSE);
		carChassisLinVel=vehActor->getLinearVelocity();
		carChassisAngVel=vehActor->getAngularVelocity();
	}

	//Pose the wheels from jounces, rotations angles, and steer angles.
	poseWheels(wheels4SimDatas[0],wheels4DynDatas[0],steerAngles,&vehDrive4W->mWheelShapeMap[0],numActiveWheelsPerBlock4[0],vehActor);
	wheels4DynDatas[0].mSteerAngles[0]=steerAngles[0];
	wheels4DynDatas[0].mSteerAngles[1]=steerAngles[1];
	wheels4DynDatas[0].mSteerAngles[2]=steerAngles[2];
	wheels4DynDatas[0].mSteerAngles[3]=steerAngles[3];
	for(PxU32 j=1;j<numWheels4;j++)
	{
		PxF32 extraWheelsSteerAngles[4]=
		{
			wheels4SimDatas[j].getWheelData(0).mToeAngle,
			wheels4SimDatas[j].getWheelData(1).mToeAngle,
			wheels4SimDatas[j].getWheelData(2).mToeAngle,
			wheels4SimDatas[j].getWheelData(3).mToeAngle
		};
		poseWheels(wheels4SimDatas[j],wheels4DynDatas[j],extraWheelsSteerAngles,&vehDrive4W->mWheelShapeMap[4*j],numActiveWheelsPerBlock4[j],vehActor);
		wheels4DynDatas[j].mSteerAngles[0]=extraWheelsSteerAngles[0];
		wheels4DynDatas[j].mSteerAngles[1]=extraWheelsSteerAngles[1];
		wheels4DynDatas[j].mSteerAngles[2]=extraWheelsSteerAngles[2];
		wheels4DynDatas[j].mSteerAngles[3]=extraWheelsSteerAngles[3];
	}
}

void PxVehicleUpdate::updateTank
(const PxF32 timestep, 
 const PxVec3& gravity, const PxF32 gravityMagnitude, const PxF32 recipGravityMagnitude, 
 const PxVehicleDrivableSurfaceToTireFrictionPairs& drivableSurfaceToTireFrictionPairs,
 PxVehicleDriveTank* vehDriveTank)
{
	PX_CHECK_AND_RETURN(
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_ACCEL]>-0.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_ACCEL]<1.01f, 
		"Illegal tank control value - accel must be in range (0,1)" );
	PX_CHECK_AND_RETURN(
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT]>-0.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT]<1.01f, 
		"Illegal tank control value - left brake must be in range (0,1)");
	PX_CHECK_AND_RETURN(
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT]>-0.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT]<1.01f, 
		"Illegal tank control right value - right brake must be in range (0,1)");
	PX_CHECK_AND_RETURN(
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT]>-1.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT]<1.01f, 
		"Illegal tank control value - left thrust must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT]>-1.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT]<1.01f, 
		"Illegal tank control value - right thrust must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL==vehDriveTank->mDriveModel ||
		(vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT]>-0.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT]<1.01f), 
		"Illegal tank control value - left thrust must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL==vehDriveTank->mDriveModel ||
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT]>-0.01f && 
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT]<1.01f, 
		"Illegal tank control value - right thrust must be in range (-1,1)");
	PX_CHECK_AND_RETURN(
		PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL==vehDriveTank->mDriveModel ||
		0.0f==
			vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_LEFT]*
			vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_LEFT],
		"Illegal tank control value - thrust left and brake left simultaneously non-zero in standard drive mode");
	PX_CHECK_AND_RETURN(
		PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL==vehDriveTank->mDriveModel ||
		0.0f==
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_THRUST_RIGHT]*
		vehDriveTank->mDriveDynData.mControlAnalogVals[PxVehicleDriveTank::eANALOG_INPUT_BRAKE_RIGHT],
		"Illegal tank control value - thrust right and brake right simultaneously non-zero in standard drive mode");
	PX_CHECK_AND_RETURN(
		!(vehDriveTank->getRigidDynamicActor()->getRigidDynamicFlags() & PxRigidDynamicFlag::eKINEMATIC),
		"Attempting to update a tank with a kinematic actor - this isn't allowed");

#if PX_DEBUG_VEHICLE_ON
	for(PxU32 i=0;i<vehDriveTank->mWheelsSimData.mNumWheels4;i++)
	{
		updateGraphDataInternalWheelDynamics(4*i,vehDriveTank->mWheelsDynData.mWheels4DynData[i].mWheelSpeeds);
	}
	updateGraphDataInternalEngineDynamics(vehDriveTank->mDriveDynData.mEnginespeed);
#endif

	//Unpack the tank simulation and instanced dynamics components.
	const PxVehicleWheels4SimData* wheels4SimDatas=vehDriveTank->mWheelsSimData.mWheels4SimData;
	const PxVehicleTireLoadFilterData& tireLoadFilterData=vehDriveTank->mWheelsSimData.mNormalisedLoadFilter;
	PxVehicleWheels4DynData* wheels4DynDatas=vehDriveTank->mWheelsDynData.mWheels4DynData;
	const PxU32 numWheels4=vehDriveTank->mWheelsSimData.mNumWheels4;
	const PxU32 numActiveWheels=vehDriveTank->mWheelsSimData.mNumActiveWheels;
	const PxU32 numActiveWheelsInLast4=4-(4*numWheels4-numActiveWheels);
	const PxVehicleDriveSimData driveSimData=vehDriveTank->mDriveSimData;
	PxVehicleDriveDynData& driveDynData=vehDriveTank->mDriveDynData;
	PxRigidDynamic* vehActor=vehDriveTank->mActor;

	//In each block of 4 wheels record how many wheels are active.
	PxU32 numActiveWheelsPerBlock4[PX_MAX_NUM_SUSPWHEELTIRE4]={0,0,0,0,0};
	numActiveWheelsPerBlock4[0]=PxMin(numActiveWheels,(PxU32)4);
	for(PxU32 i=1;i<numWheels4-1;i++)
	{
		numActiveWheelsPerBlock4[i]=4;
	}
	numActiveWheelsPerBlock4[numWheels4-1]=numActiveWheelsInLast4;
	PX_ASSERT(numActiveWheels == numActiveWheelsPerBlock4[0] + numActiveWheelsPerBlock4[1] + numActiveWheelsPerBlock4[2] + numActiveWheelsPerBlock4[3] + numActiveWheelsPerBlock4[4]); 

	//Organise the shader data in blocks of 4.
	PxVehicleTireForceCalculator4 tires4ForceCalculators[PX_MAX_NUM_SUSPWHEELTIRE4];
	for(PxU32 i=0;i<numWheels4;i++)
	{
		tires4ForceCalculators[i].mShaderData[0]=vehDriveTank->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+0];
		tires4ForceCalculators[i].mShaderData[1]=vehDriveTank->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+1];
		tires4ForceCalculators[i].mShaderData[2]=vehDriveTank->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+2];
		tires4ForceCalculators[i].mShaderData[3]=vehDriveTank->mWheelsDynData.mTireForceCalculators->mShaderData[4*i+3];
		tires4ForceCalculators[i].mShader=vehDriveTank->mWheelsDynData.mTireForceCalculators->mShader;
	}

	//Mark the suspension/tire constraints as dirty to force them to be updated in the sdk.
	for(PxU32 i=0;i<numWheels4;i++)
	{
		wheels4DynDatas[i].getVehicletConstraintShader().mConstraint->markDirty();
	}

	//Compute the transform of the center of mass.
	PxTransform carChassisTransform;
	{
		carChassisTransform=vehActor->getGlobalPose().transform(vehActor->getCMassLocalPose());
	}

	//Retrieve control values from vehicle controls.
	PxF32 accel,brakeLeft,brakeRight,thrustLeft,thrustRight;
	{
		getTankControlValues(driveDynData,accel,brakeLeft,brakeRight,thrustLeft,thrustRight);
#if PX_DEBUG_VEHICLE_ON
		updateGraphDataControlInputs(accel,brakeLeft,brakeRight,thrustLeft,thrustRight);
#endif
	}
	bool isIntentionToAccelerate;
	PxF32 thrustLeftAbs;
	PxF32 thrustRightAbs;
	{
		thrustLeftAbs=PxAbs(thrustLeft);
		thrustRightAbs=PxAbs(thrustRight);
		isIntentionToAccelerate = (accel*(thrustLeftAbs+thrustRightAbs)>0);
	}

	//Update the auto-box and decide whether to change gear up or down.
	//If the tank is supposed to turn sharply don't process the auto-box.
	bool useAutoGears;
	if(vehDriveTank->getDriveModel()==PxVehicleDriveTank::eDRIVE_MODEL_SPECIAL)
	{
		useAutoGears = driveDynData.mUseAutoGears ? ((((thrustRight*thrustLeft) >= 0.0f) || (0.0f==thrustLeft && 0.0f==thrustRight)) ? true : false) : false;
	}
	else
	{
		useAutoGears = driveDynData.mUseAutoGears ? (thrustRight*brakeLeft>0 || thrustLeft*brakeRight>0 ? false : true) : false; 
	}
	if(useAutoGears)
	{
		processAutoBox(timestep,driveSimData,driveDynData);
	}

	//Process gear-up/gear-down commands.
	{
		const PxVehicleGearsData& gearsData=driveSimData.getGearsData();
		processGears(timestep,gearsData,driveDynData);
	}

	//Clutch strength;
	PxF32 K;
	{
		const PxVehicleClutchData& clutchData=driveSimData.getClutchData();
		const PxU32 currentGear=driveDynData.mCurrentGear;
		K=computeClutchStrength(clutchData, currentGear);
	}

	//Gear ratio.
	PxF32 G;
	PxU32 currentGear;
	{
		const PxVehicleGearsData& gearsData=driveSimData.getGearsData();
		currentGear=driveDynData.mCurrentGear;
		G=computeGearRatio(gearsData,currentGear);
#if PX_DEBUG_VEHICLE_ON
		updateGraphDataGearRatio(G);
#endif
	}


	//Set up contribution of each wheel to the average wheel speed at the clutch 
	//Set up the torque ratio delivered by the diff to each wheel.
	PxF32 aveWheelSpeedContributions[PX_MAX_NUM_WHEELS];
	const PxF32 invNumWheels=1.0f/(1.0f*numActiveWheels);
	PxF32 diffTorqueRatios[PX_MAX_NUM_WHEELS];
	PxF32 wheelGearings[PX_MAX_NUM_WHEELS];
	const PxF32 totalInput=thrustLeftAbs+thrustRightAbs;
	const PxF32 denom = (totalInput>0) ? 2.0f/totalInput : 1;
	const PxF32 diffTorqueRatioLeft=thrustLeftAbs*denom*invNumWheels;
	const PxF32 diffTorqueRatioRight=thrustRightAbs*denom*invNumWheels;
	initialiseAll4<PxF32>(numWheels4,aveWheelSpeedContributions,invNumWheels);
	initialiseAll4<PxF32>(numWheels4,diffTorqueRatios,diffTorqueRatioLeft,diffTorqueRatioRight);
	initialiseAll4<PxF32>(numWheels4,wheelGearings,computeSign(thrustLeft),computeSign(thrustRight));


#if PX_DEBUG_VEHICLE_ON
	updateGraphDataClutchSlip(wheels4DynDatas[0].mWheelSpeeds,aveWheelSpeedContributions,driveDynData.mEnginespeed,G);
#endif

	//Ackermann-corrected steer angles 
	//For tanks this is always zero because they turn by torque delivery rather than a steering mechanism.
	PxF32 steerAngles[PX_MAX_NUM_WHEELS];
	initialiseAll4<PxF32>(numWheels4,steerAngles,0.0f);

	//Ready to do the update.
	PxVec3 carChassisLinVel=vehActor->getLinearVelocity();
	PxVec3 carChassisAngVel=vehActor->getAngularVelocity();
	const PxU32 numSubSteps=8;
	const PxF32 timeFraction=1.0f/(1.0f*numSubSteps);
	const PxF32 subTimestep=timestep*timeFraction;
	for(PxU32 k=0;k<numSubSteps;k++)
	{
		//Set the force and torque for the current update to zero.
		PxVec3 chassisForce(0,0,0);
		PxVec3 chassisTorque(0,0,0);

		//Bit of a trick here.
		//The sdk will apply gravity*dt completely independent of the tire forces.
		//Cars will never come to rest this way because even if the tire model brings the car
		//exactly to rest it will just be immediately perturbed by gravitational acceleration.
		//Maybe we should add gravity here before computing the tire forces so that the tire
		//forces act against the gravitational forces that will be later applied.  
		//We don't actually ever apply gravity to the rigid body, we just imagine the tire/susp 
		//forces that would be needed if gravity had already been applied.  The sdk, therefore, 
		//still needs to apply gravity to the chassis rigid body in its update.
		PX_ASSERT(carChassisLinVel==vehActor->getLinearVelocity());
		carChassisLinVel+=gravity*subTimestep;

		//Compute the brake torques.
		PxF32 brakeTorques[PX_MAX_NUM_WHEELS];
		bool isBrakeApplied[PX_MAX_NUM_WHEELS];
		initialiseAll4<PxF32>(numWheels4,brakeTorques,0.0f);
		initialiseAll4<bool>(numWheels4,isBrakeApplied,false);
		for(PxU32 i=0;i<numWheels4;i++)
		{
			computeTankBrakeTorques
				(&wheels4SimDatas[i].getWheelData(0),wheels4DynDatas[i].mWheelSpeeds,brakeLeft,brakeRight,
				&brakeTorques[i*4],&isBrakeApplied[i*4]);
		}

		//Compute jounces, slips, tire forces, suspension forces etc.
		PxF32 jounces[PX_MAX_NUM_WHEELS];
		PxF32 forwardSpeeds[PX_MAX_NUM_WHEELS];
		PxF32 tireFrictions[PX_MAX_NUM_WHEELS];
		PxF32 longSlips[PX_MAX_NUM_WHEELS];
		PxF32 latSlips[PX_MAX_NUM_WHEELS];
		PxU32 tireSurfaceTypes[PX_MAX_NUM_WHEELS];
		PxMaterial* tireSurfaceMaterials[PX_MAX_NUM_WHEELS];
		PxF32 tireTorques[PX_MAX_NUM_WHEELS];
		initialiseAll4<PxF32>(numWheels4,jounces,0.0f);
		initialiseAll4<PxF32>(numWheels4,forwardSpeeds,0.0f);
		initialiseAll4<PxF32>(numWheels4,tireFrictions,0.0f);
		initialiseAll4<PxF32>(numWheels4,longSlips,0.0f);
		initialiseAll4<PxF32>(numWheels4,latSlips,0.0f);
		initialiseAll4<PxU32>(numWheels4,tireSurfaceTypes,PxVehicleDrivableSurfaceType::eSURFACE_TYPE_UNKNOWN);
		initialiseAll4<PxMaterial*>(numWheels4,tireSurfaceMaterials,NULL);
		initialiseAll4<PxF32>(numWheels4,tireTorques,0.0f);
		for(PxU32 i=0;i<numWheels4;i++)
		{
			processSuspTireWheels
			   (timeFraction,
				carChassisTransform, carChassisLinVel, carChassisAngVel, true,
				gravity,gravityMagnitude,recipGravityMagnitude, subTimestep,
				isIntentionToAccelerate,
				wheels4SimDatas[i],tireLoadFilterData,wheels4DynDatas[i],tires4ForceCalculators[i],numActiveWheelsPerBlock4[i],
				vehActor,
				&steerAngles[i*4], &isBrakeApplied[i*4], 
				&drivableSurfaceToTireFrictionPairs,
				i*4,
				wheels4DynDatas[i].getVehicletConstraintShader().mData, 
				wheels4DynDatas[i].mTireLowForwardSpeedTimers,
				&jounces[i*4], &forwardSpeeds[i*4], &tireFrictions[i*4], &longSlips[i*4], &latSlips[i*4], &tireSurfaceTypes[i*4], &tireSurfaceMaterials[i*4], 
				&tireTorques[i*4], 
				chassisForce, chassisTorque);
		}


		PxF32 engineDriveTorque;
		{
			const PxVehicleEngineData& engineData=driveSimData.getEngineData();
			const PxF32 engineOmega=driveDynData.mEnginespeed;
			engineDriveTorque=computeEngineDriveTorque(engineData,engineOmega,accel);
#if PX_DEBUG_VEHICLE_ON
			updateGraphDataEngineDriveTorque(engineDriveTorque);
#endif
		}

		PxF32 engineDampingRate;
		{
			const PxVehicleEngineData& engineData=driveSimData.getEngineData();
			engineDampingRate=computeEngineDampingRate(engineData,currentGear,accel);
		}

		//Update the wheel and engine speeds - 5x5 matrix coupling engine and wheels.
		solveTankInternaDynamicsEnginePlusDrivenWheels(
			subTimestep, 
			K,G,
			engineDriveTorque,engineDampingRate,
			diffTorqueRatios,aveWheelSpeedContributions,wheelGearings,
			brakeTorques,isBrakeApplied,tireTorques,
			wheels4SimDatas,wheels4DynDatas,numWheels4,numActiveWheels,
			driveSimData,driveDynData);

		//Integrate wheel rotation angle (theta += omega*dt)
		for(PxU32 i=0;i<numWheels4;i++)
		{
			integrateWheelRotationAngles
				(subTimestep,
				K,G,engineDriveTorque,
				jounces,diffTorqueRatios,forwardSpeeds,isBrakeApplied,
				driveSimData,wheels4SimDatas[i],
				driveDynData,wheels4DynDatas[i]);

			storeJounceSlipFrictionSurfTypeSurfMatl
				(&jounces[4*i],&longSlips[4*i],&latSlips[4*i],&tireFrictions[4*i],&tireSurfaceTypes[4*i],&tireSurfaceMaterials[4*i],wheels4DynDatas[i]);

		}

		//Integrate the chassis velocity by applying the accumulated force and torque.
		vehActor->addForce(chassisForce*subTimestep,PxForceMode::eIMPULSE);
		vehActor->addTorque(chassisTorque*subTimestep,PxForceMode::eIMPULSE);
		carChassisLinVel=vehActor->getLinearVelocity();
		carChassisAngVel=vehActor->getAngularVelocity();
	}

	//Pose the wheels transforms from the jounces, rotations angles, and steer angles.
	for(PxU32 i=0;i<numWheels4;i++)
	{
		poseWheels(wheels4SimDatas[i],wheels4DynDatas[i],steerAngles,&vehDriveTank->mWheelShapeMap[4*i],numActiveWheelsPerBlock4[i],vehActor);
	}
}

}//namespace physx

#if PX_DEBUG_VEHICLE_ON

void PxVehicleUpdate::updateSingleVehicleAndStoreTelemetryData
(const PxF32 timestep, const PxVec3& gravity, const PxVehicleDrivableSurfaceToTireFrictionPairs& vehicleDrivableSurfaceToTireFrictionPairs, 
 PxVehicleWheels* vehWheels, PxVehicleTelemetryData& telemetryData)
{
	PX_CHECK_MSG(gravity.magnitude()>0, "gravity vector must have non-zero length");
	PX_CHECK_MSG(timestep>0, "timestep must be greater than zero");
	PX_CHECK_AND_RETURN(gThresholdForwardSpeedForWheelAngleIntegration>0, "PxInitVehicleSDK needs to be called before ever calling PxVehicleUpdateSingleVehicleAndStoreTelemetryData");
	PX_CHECK_MSG(vehWheels->mWheelsSimData.getNumWheels()==telemetryData.getNumWheelGraphs(), "vehicle and telemetry data need to have the same number of wheels");

#ifdef PX_CHECKED
	for(PxU32 i=0;i<vehWheels->mWheelsSimData.mNumWheels4;i++)
	{
		PX_CHECK_MSG(vehWheels->mWheelsDynData.mWheels4DynData[i].mSqResults, "Need to call PxVehicle4WSuspensionRaycasts before trying to update");
	}
	for(PxU32 i=0;i<vehWheels->mWheelsSimData.mNumActiveWheels;i++)
	{
		PX_CHECK_MSG(vehWheels->mWheelsDynData.mTireForceCalculators->mShaderData[i], "Need to set non-null tire force shader data ptr");
	}
	PX_CHECK_MSG(vehWheels->mWheelsDynData.mTireForceCalculators->mShader, "Need to set non-null tire force shader function");
#endif

	PxF32 engineGraphData[PxVehicleGraph::eMAX_NUM_ENGINE_CHANNELS];
	PxF32 wheelGraphData[PX_MAX_NUM_WHEELS][PxVehicleGraph::eMAX_NUM_WHEEL_CHANNELS];
	PxVec3 suspForceAppPoints[PX_MAX_NUM_WHEELS];
	PxVec3 tireForceAppPoints[PX_MAX_NUM_WHEELS];
	gCarEngineGraphData=engineGraphData;
	for(PxU32 i=0;i<4*vehWheels->mWheelsSimData.mNumWheels4;i++)
	{
		gCarWheelGraphData[i]=wheelGraphData[i];
	}
	for(PxU32 i=4*vehWheels->mWheelsSimData.mNumWheels4;i<4*PX_MAX_NUM_SUSPWHEELTIRE4;i++)
	{
		gCarWheelGraphData[i]=NULL;
	}
	gCarSuspForceAppPoints=suspForceAppPoints;
	gCarTireForceAppPoints=tireForceAppPoints;


	const PxF32 gravityMagnitude=gravity.magnitude();
	const PxF32 recipGravityMagnitude=1.0f/gravityMagnitude;

	switch(vehWheels->mType)
	{
	case eVEHICLE_TYPE_DRIVE4W:
		{
			PxVehicleDrive4W* vehDrive4W=(PxVehicleDrive4W*)vehWheels;

			PxVehicleUpdate::updateDrive4W(
				timestep,
				gravity,gravityMagnitude,recipGravityMagnitude,
				vehicleDrivableSurfaceToTireFrictionPairs,
				vehDrive4W);
				
			for(PxU32 i=0;i<vehWheels->mWheelsSimData.mNumActiveWheels;i++)
			{
				telemetryData.mWheelGraphs[i].updateTimeSlice(wheelGraphData[i]);
				telemetryData.mSuspforceAppPoints[i]=suspForceAppPoints[i];
				telemetryData.mTireforceAppPoints[i]=tireForceAppPoints[i];
			}
			telemetryData.mEngineGraph->updateTimeSlice(engineGraphData);
		}
		break;
	case eVEHICLE_TYPE_DRIVETANK:
		{
			PxVehicleDriveTank* vehDriveTank=(PxVehicleDriveTank*)vehWheels;

			PxVehicleUpdate::updateTank(
				timestep,gravity,gravityMagnitude,recipGravityMagnitude,
				vehicleDrivableSurfaceToTireFrictionPairs,
				vehDriveTank);
				
			for(PxU32 i=0;i<vehWheels->mWheelsSimData.mNumActiveWheels;i++)
			{
				telemetryData.mWheelGraphs[i].updateTimeSlice(wheelGraphData[i]);
				telemetryData.mSuspforceAppPoints[i]=suspForceAppPoints[i];
				telemetryData.mTireforceAppPoints[i]=tireForceAppPoints[i];
			}
			telemetryData.mEngineGraph->updateTimeSlice(engineGraphData);
		}
		break;
	default:
		PX_CHECK_MSG(false, "updateSingleVehicleAndStoreTelemetryData - unsupported vehicle type"); 
		break;
	}
}

void physx::PxVehicleUpdateSingleVehicleAndStoreTelemetryData
(const PxReal timestep, const PxVec3& gravity, const physx::PxVehicleDrivableSurfaceToTireFrictionPairs& vehicleDrivableSurfaceToTireFrictionPairs, 
 PxVehicleWheels* focusVehicle, PxVehicleTelemetryData& telemetryData)
{
	PxVehicleUpdate::updateSingleVehicleAndStoreTelemetryData
		(timestep, gravity, vehicleDrivableSurfaceToTireFrictionPairs, focusVehicle, telemetryData);
}

#endif

////////////////////////////////////////////////////////////

void PxVehicleUpdate::update
(const PxF32 timestep, const PxVec3& gravity, const PxVehicleDrivableSurfaceToTireFrictionPairs& vehicleDrivableSurfaceToTireFrictionPairs, 
 const PxU32 numVehicles, PxVehicleWheels** vehicles)
{
	PX_CHECK_AND_RETURN(gravity.magnitude()>0, "gravity vector must have non-zero length");
	PX_CHECK_AND_RETURN(timestep>0, "timestep must be greater than zero");
	PX_CHECK_AND_RETURN(gThresholdForwardSpeedForWheelAngleIntegration>0, "PxInitVehicleSDK needs to be called before ever calling PxVehicleUpdates");

#ifdef PX_CHECKED
	for(PxU32 i=0;i<numVehicles;i++)
	{
		const PxVehicleWheels* const vehWheels=vehicles[i];
		for(PxU32 j=0;j<vehWheels->mWheelsSimData.mNumWheels4;j++)
		{
			PX_CHECK_MSG(vehWheels->mWheelsDynData.mWheels4DynData[j].mSqResults, "Need to call PxVehicle4WSuspensionRaycasts before trying to update");
		}
		for(PxU32 i=0;i<vehWheels->mWheelsSimData.mNumActiveWheels;i++)
		{
			PX_CHECK_MSG(vehWheels->mWheelsDynData.mTireForceCalculators->mShaderData[i], "Need to set non-null tire force shader data ptr");
		}
		PX_CHECK_MSG(vehWheels->mWheelsDynData.mTireForceCalculators->mShader, "Need to set non-null tire force shader function");
	}
#endif

#if PX_DEBUG_VEHICLE_ON
	gCarEngineGraphData=NULL;
	for(PxU32 j=0;j<PX_MAX_NUM_WHEELS;j++)
	{
		gCarWheelGraphData[j]=NULL;
	}
	gCarSuspForceAppPoints=NULL;
	gCarTireForceAppPoints=NULL;
#endif

	const PxF32 gravityMagnitude=gravity.magnitude();
	const PxF32 recipGravityMagnitude=1.0f/gravityMagnitude;

	for(PxU32 i=0;i<numVehicles;i++)
	{
		PxVehicleWheels* vehWheels=vehicles[i];

		switch(vehWheels->mType)
		{
		case eVEHICLE_TYPE_DRIVE4W:
			{
				PxVehicleDrive4W* vehDrive4W=(PxVehicleDrive4W*)vehWheels;

				PxVehicleUpdate::updateDrive4W(					
					timestep,
					gravity,gravityMagnitude,recipGravityMagnitude,
					vehicleDrivableSurfaceToTireFrictionPairs,
					vehDrive4W);
				}
			break;

		case eVEHICLE_TYPE_DRIVETANK:
			{
				PxVehicleDriveTank* vehDriveTank=(PxVehicleDriveTank*)vehWheels;

				PxVehicleUpdate::updateTank(
					timestep,
					gravity,gravityMagnitude,recipGravityMagnitude,
					vehicleDrivableSurfaceToTireFrictionPairs,
					vehDriveTank);
			}
			break;	
			
		default:
			PX_CHECK_MSG(false, "update - unsupported vehicle type"); 
			break;
		}
	}
}

void physx::PxVehicleUpdates
(const PxReal timestep, const PxVec3& gravity, const PxVehicleDrivableSurfaceToTireFrictionPairs& vehicleDrivableSurfaceToTireFrictionPairs, 
 const PxU32 numVehicles, PxVehicleWheels** vehicles)
{
	PxVehicleUpdate::update(timestep, gravity, vehicleDrivableSurfaceToTireFrictionPairs, numVehicles, vehicles);
}

////////////////////////////////////////////////////////////

void PxVehicleWheels4SuspensionRaycasts
(PxBatchQuery* batchQuery, const PxU32 numVehicles, PxRaycastQueryResult* sceneQueryResults, 
 const PxVehicleWheels4SimData& wheels4SimData, PxVehicleWheels4DynData& wheels4DynData, const PxSceneQueryFilterData carFilterData, const PxU32 numActiveWheels,
 PxRigidDynamic* vehActor)
{
	//Get the transform of the chassis.
	const PxTransform carChassisTrnsfm=vehActor->getGlobalPose().transform(vehActor->getCMassLocalPose());

	//Add a raycast for each wheel.
	for(PxU32 j=0;j<numActiveWheels;j++)
	{
		const PxVehicleSuspensionData& susp=wheels4SimData.getSuspensionData(j);
		const PxF32 maxDroop=susp.mMaxDroop;
		const PxF32 maxBounce=susp.mMaxCompression;
		const PxVehicleWheelData& wheel=wheels4SimData.getWheelData(j);
		const PxF32 radius=wheel.mRadius;
		PX_ASSERT(maxBounce>=0);
		PX_ASSERT(maxDroop>=0);

		//Direction of raycast.
		const PxVec3 downwardSuspensionTravelDir=carChassisTrnsfm.rotate(wheels4SimData.getSuspTravelDirection(j));

		//Position at top of wheel at maximum compression.
		PxVec3 wheelPosition=carChassisTrnsfm.transform(wheels4SimData.getWheelCentreOffset(j));
		wheelPosition-=downwardSuspensionTravelDir*(radius+maxBounce);

		//Total length from top of wheel at max compression to bottom of wheel at max droop.
		PxF32 rayLength=radius + maxBounce  + maxDroop + radius;
		//Add another radius on for good measure.
		rayLength+=radius;

		//Store the susp line ray for later use.
		wheels4DynData.mSuspLineStarts[j]=wheelPosition;
		wheels4DynData.mSuspLineDirs[j]=downwardSuspensionTravelDir;
		wheels4DynData.mSuspLineLengths[j]=rayLength;

		//Add the raycast to the scene query.
		batchQuery->raycastSingle(wheelPosition, downwardSuspensionTravelDir, rayLength, carFilterData, PxSceneQueryFlag::eIMPACT|PxSceneQueryFlag::eNORMAL|PxSceneQueryFlag::eDISTANCE|PxSceneQueryFlag::eUV);
	}
}
void PxVehicleUpdate::suspensionRaycasts(PxBatchQuery* batchQuery, const PxU32 numVehicles, PxVehicleWheels** vehicles, const PxU32 numSceneQueryesults, PxRaycastQueryResult* sceneQueryResults)
{
	//Reset all hit counts to zero.
	for(PxU32 i=0;i<numSceneQueryesults;i++)
	{
		sceneQueryResults[i].nbHits=0;
	}

	PxRaycastQueryResult* sqres=sceneQueryResults;

	//Work out the rays for the suspension line raycasts and perform all the raycasts.
	for(PxU32 i=0;i<numVehicles;i++)
	{
		//Get the current car.
		PxVehicleWheels& veh=*vehicles[i];
		const PxVehicleWheels4SimData* PX_RESTRICT wheels4SimData=veh.mWheelsSimData.mWheels4SimData;
		PxVehicleWheels4DynData* PX_RESTRICT wheels4DynData=veh.mWheelsDynData.mWheels4DynData;
		const PxU32 numWheels4=veh.mWheelsSimData.mNumWheels4;
		const PxU32 numActiveWheels=veh.mWheelsSimData.mNumActiveWheels;
		const PxU32 numActiveWheelsInLast4=4-(4*numWheels4 - numActiveWheels);
		PxRigidDynamic* vehActor=veh.mActor;

		//Find the index of one of the wheel shapes (doesn't really matter which one).
		PxU8 firstWheelShapeIndex=PX_MAX_U8;
		PxU32 k=0;
		while(firstWheelShapeIndex==PX_MAX_U8 && k<veh.mWheelsSimData.mNumActiveWheels)
		{
			firstWheelShapeIndex=veh.mWheelShapeMap[k];
			k++;
		}

		if(firstWheelShapeIndex!=PX_MAX_U8)
		{
			//Get the filter data of any of the wheels (doesn't really matter which one).
			PxShape* shapeBuffer[1];
			vehActor->getShapes(shapeBuffer,1,firstWheelShapeIndex);
			const PxSceneQueryFilterData carFilterData(shapeBuffer[0]->getQueryFilterData(), PxSceneQueryFilterFlag::eSTATIC|PxSceneQueryFilterFlag::eDYNAMIC|PxSceneQueryFilterFlag::ePREFILTER);

			//Set the results pointer and start the raycasts.
			for(PxU32 j=0;j<numWheels4-1;j++)
			{
				wheels4DynData[j].mSqResults=NULL;
				if((sceneQueryResults + numSceneQueryesults) >= (sqres+4))
				{
					wheels4DynData[j].mSqResults=sqres;
					PxVehicleWheels4SuspensionRaycasts(batchQuery,numVehicles,sqres,wheels4SimData[j],wheels4DynData[j],carFilterData,4,vehActor);
				}
				else
				{
					PX_CHECK_MSG(false, "PxVehicleUpdate::suspensionRaycasts - numSceneQueryesults not bit enough to support one raycast hit report per wheel.  Increase size of sceneQueryResults");
				}
				sqres+=4;
			}
			{
				wheels4DynData[numWheels4-1].mSqResults=NULL;
				if((sceneQueryResults + numSceneQueryesults) >= (sqres+numActiveWheelsInLast4))
				{
					wheels4DynData[numWheels4-1].mSqResults=sqres;
					PxVehicleWheels4SuspensionRaycasts(batchQuery,numVehicles,sqres,wheels4SimData[numWheels4-1],wheels4DynData[numWheels4-1],carFilterData,numActiveWheelsInLast4,vehActor);
				}
				else
				{
					PX_CHECK_MSG(false, "PxVehicleUpdate::suspensionRaycasts - numSceneQueryesults not bit enough to support one raycast hit report per wheel.  Increase size of sceneQueryResults");
				}
				sqres+=numActiveWheelsInLast4;
			}
		}
		else
		{
			//None of the wheels is mapped to a shape.
			sqres+=numActiveWheels;
		}
	}

	batchQuery->execute();
}

void physx::PxVehicleSuspensionRaycasts(PxBatchQuery* batchQuery, const PxU32 numVehicles, PxVehicleWheels** vehicles, const PxU32 numSceneQueryesults, PxRaycastQueryResult* sceneQueryResults)
{
	PxVehicleUpdate::suspensionRaycasts(batchQuery, numVehicles, vehicles, numSceneQueryesults, sceneQueryResults);
}
