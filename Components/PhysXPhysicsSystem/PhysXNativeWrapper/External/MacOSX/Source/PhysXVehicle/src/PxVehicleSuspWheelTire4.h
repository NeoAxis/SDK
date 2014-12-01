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

#ifndef PX_VEHICLE_SUSPWHEELTIRE_H
#define PX_VEHICLE_SUSPWHEELTIRE_H
/** \addtogroup vehicle
  @{
*/

#include "PxVehicleComponents.h"
#include "PxSimpleTypes.h"
#include "PxVec3.h"


#ifndef PX_DOXYGEN
namespace physx
{
#endif

struct PxRaycastQueryResult;
class PxVehicleConstraintShader;
class PxMaterial;



class PxVehicleWheels4SimData
{
public:

	PxVehicleWheels4SimData();

	bool isValid(const PxU32 id) const;

public:

	PX_FORCE_INLINE const PxVehicleSuspensionData&		getSuspensionData(const PxU32 id)			const {return mSuspensions[id];}
	PX_FORCE_INLINE const PxVehicleWheelData&			getWheelData(const PxU32 id)				const {return mWheels[id];}
	PX_FORCE_INLINE const PxVehicleTireData&			getTireData(const PxU32 id)					const {return mTires[id];}
	PX_FORCE_INLINE const PxVec3&						getSuspTravelDirection(const PxU32 id)		const {return mSuspDownwardTravelDirections[id];}
	PX_FORCE_INLINE const PxVec3&						getSuspForceAppPointOffset(const PxU32 id)	const {return mSuspForceAppPointOffsets[id];}
	PX_FORCE_INLINE const PxVec3&						getTireForceAppPointOffset(const PxU32 id)	const {return mTireForceAppPointOffsets[id];}
	PX_FORCE_INLINE const PxVec3&						getWheelCentreOffset(const PxU32 id)		const {return mWheelCentreOffsets[id];}
	PX_FORCE_INLINE const PxReal*						getTireRestLoadsArray()						const {return mTireRestLoads;}
	PX_FORCE_INLINE const PxReal*						getRecipTireRestLoadsArray()				const {return mRecipTireRestLoads;}

					void setSuspensionData				(const PxVehicleSuspensionData& susp, const PxU32 id);
					void setWheelData					(const PxVehicleWheelData& susp, const PxU32 id);
					void setTireData					(const PxVehicleTireData& tire, const PxU32 id);
					void setSuspTravelDirection			(const PxVec3& dir, const PxU32 id);
					void setSuspForceAppPointOffset		(const PxVec3& offset, const PxU32 id);
					void setTireForceAppPointOffset		(const PxVec3& offset, const PxU32 id);
					void setWheelCentreOffset			(const PxVec3& offset, const PxU32 id);

private:

	/**
	\brief Suspension simulation data
	@see setSuspensionData, getSuspensionData
	*/
	PxVehicleSuspensionData			mSuspensions[4];

	/**
	\brief Wheel simulation data
	@see setWheelData, getWheelData
	*/
	PxVehicleWheelData				mWheels[4];

	/**
	\brief Tire simulation data
	@see setTireData, getTireData
	*/
	PxVehicleTireData				mTires[4];

	/**
	\brief Direction of suspension travel, pointing downwards.
	*/
	PxVec3							mSuspDownwardTravelDirections[4];

	/**
	\brief Application point of suspension force specified as an offset from the rigid body centre of mass.
	*/
	PxVec3							mSuspForceAppPointOffsets[4];	//Offset from cm

	/**
	\brief Application point of tire forces specified as an offset from the rigid body centre of mass.
	*/
	PxVec3							mTireForceAppPointOffsets[4];	//Offset from cm

	/**
	\brief Position of wheel center specified as an offset from the rigid body centre of mass.
	*/
	PxVec3							mWheelCentreOffsets[4];			//Offset from cm

	/** 
	\brief Normalized tire load on each tire (load/rest load) at zero suspension jounce under gravity.
	*/
	PxReal							mTireRestLoads[4];	

	/** 
	\brief Reciprocal normalized tire load on each tire at zero suspension jounce under gravity.
	*/
	PxReal							mRecipTireRestLoads[4];	
};

class PxVehicleWheels4DynData
{
public:

	PxVehicleWheels4DynData()
		:	mSqResults(NULL)
	{
		for(PxU32 i=0;i<4;i++)
		{
			mWheelSpeeds[i]=0.0f;
			mTireLowForwardSpeedTimers[i]=0.0f;
			mWheelRotationAngles[i]=0.0f;
			mSuspJounces[i]=0.0f;
			mLongSlips[i]=0.0f;
			mLatSlips[i]=0.0f;
			mTireFrictions[i]=0.0f;
			mTireSurfaceTypes[i]=0;
			mTireSurfaceMaterials[i]=NULL;
			mSuspLineStarts[i]=PxVec3(0,0,0);
			mSuspLineDirs[i]=PxVec3(0,0,0);
			mSuspLineLengths[i]=0.0f;
		}
	}
	~PxVehicleWheels4DynData()
	{
	}

	bool isValid() const {return true;}

	/**
	\brief Rotation speeds of wheels 
	@see PxVehicle4WSetToRestState, PxVehicle4WGetWheelRotationSpeed, PxVehicle4WGetEngineRotationSpeed
	*/	
	PxReal mWheelSpeeds[4];

	/**
	\brief Timers used to trigger sticky friction to hold the car perfectly at rest. 
	\brief Used only internally.
	*/
	PxReal mTireLowForwardSpeedTimers[4];

	/**
	\brief Reported rotation angle about rolling axis.
	@see PxVehicle4WSetToRestState, PxVehicle4WGetWheelRotationAngle
	*/	
	PxReal mWheelRotationAngles[4];

	/**
	\brief Reported steer angle about up vector
	*/	
	PxReal mSteerAngles[4];

	/**
	\brief Reported compression of each suspension spring
	@see PxVehicle4WGetSuspJounce
	*/	
	PxReal mSuspJounces[4];

	/**
	\brief Reported longitudinal slip of each tire
	@see PxVehicle4WGetTireLongSlip
	*/	
	PxReal mLongSlips[4];

	/**
	\brief Reported lateral slip of each tire
	@see PxVehicle4WGetTireLatSlip
	*/	
	PxReal mLatSlips[4];

	/**
	\brief Reported friction experienced by each tire
	@see PxVehicle4WGetTireFriction
	*/	
	PxReal mTireFrictions[4];

	/**
	\brief Reported surface type experienced by each tire.
	@see PxVehicle4WGetTireDrivableSurfaceType
	*/	
	PxU32 mTireSurfaceTypes[4];

	/**
	\brief Reported PxMaterial experienced by each tire.
	@see PxVehicle4WGetTireDrivableSurfaceMaterial
	*/	
	const PxMaterial* mTireSurfaceMaterials[4];

	/**
	\brief Reported start point of suspension line raycasts used in more recent scene query.
	@see PxVehicle4WSuspensionRaycasts, PxVehicle4WGetSuspRaycast
	*/
	PxVec3 mSuspLineStarts[4];

	/**
	\brief Reported directions of suspension line raycasts used in more recent scene query.
	@see PxVehicle4WSuspensionRaycasts, PxVehicle4WGetSuspRaycast
	*/
	PxVec3 mSuspLineDirs[4];

	/**
	\brief Reported lengths of suspension line raycasts used in more recent scene query.
	@see PxVehicle4WSuspensionRaycasts, PxVehicle4WGetSuspRaycast
	*/
	PxReal mSuspLineLengths[4];

	/**
	\brief Used only internally.
	*/
	void setVehicleConstraintShader(PxVehicleConstraintShader* shader) {mVehicleConstraints=shader;}
	PxVehicleConstraintShader& getVehicletConstraintShader() const {return *mVehicleConstraints;} 

private:

	//Susp limits and sticky tire friction for all wheels.
	PxVehicleConstraintShader* mVehicleConstraints;

public:

	/**
	\brief Set by PxVehicle4WSuspensionRaycasts
	@see PxVehicle4WSuspensionRaycasts
	*/
	const PxRaycastQueryResult* mSqResults;

#ifndef PX_X64
	PxU32 mPad[2];
#endif
};
PX_COMPILE_TIME_ASSERT(0==(sizeof(PxVehicleWheels4DynData) & 15));

#ifndef PX_DOXYGEN
} // namespace physx
#endif

/** @} */
#endif //PX_VEHICLE_SUSPWHEELTIRE_H
