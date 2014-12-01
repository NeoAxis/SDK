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

#ifndef PX_VEHICLE_SUSP_LIMIT_CONSTRAINT_SHADER_H
#define PX_VEHICLE_SUSP_LIMIT_CONSTRAINT_SHADER_H
/** \addtogroup vehicle
  @{
*/

#include "extensions/PxConstraintExt.h"
#include "PxConstraintDesc.h"
#include "PxConstraint.h"
#include "PxTransform.h"
#include "PsAllocator.h"

#ifndef PX_DOXYGEN
namespace physx
{
#endif

class PxVehicleConstraintShader : public PxConstraintConnector
{
public:

	friend class PxVehicleWheels;

	PxVehicleConstraintShader(PxVehicleWheels* vehicle)
		: mConstraint(NULL),
		  mVehicle(vehicle)
	{
	}
	~PxVehicleConstraintShader()
	{
	}

	void release()
	{
		if(mConstraint)
		{
			mConstraint->release();
		}
	}

	virtual void			onComShift(PxU32 actor)	{ PX_UNUSED(actor); }

	virtual void*			prepareData()	
	{
		return &mData;
	}

	virtual bool			updatePvdProperties(physx::debugger::comm::PvdDataStream& pvdConnection,
		const PxConstraint* c,
		PxPvdUpdateType::Enum updateType) const	 { PX_UNUSED(c); PX_UNUSED(updateType); PX_UNUSED(&pvdConnection); return true;}

	virtual void			onConstraintRelease()
	{
		mVehicle->mOnConstraintReleaseCounter--;
		if(0==mVehicle->mOnConstraintReleaseCounter)
		{
			PX_FREE(mVehicle);
		}
	}

	virtual void*			getExternalReference(PxU32& typeID) { typeID = PxConstraintExtIDs::eVEHICLE_SUSP_LIMIT; return this; }

	static PxU32 vehicleSuspLimitConstraintSolverPrep(
		Px1DConstraint* constraints,
		PxVec3& body0WorldOffset,
		PxU32 maxConstraints,
		const void* constantBlock,
		const PxTransform& bodyAToWorld,
		const PxTransform& bodyBToWorld
		)
	{
		PX_UNUSED(maxConstraints);
		PX_UNUSED(body0WorldOffset);
		PX_UNUSED(bodyBToWorld);
		PX_ASSERT(bodyAToWorld.isValid()); PX_ASSERT(bodyBToWorld.isValid());

		VehicleConstraintData* data = (VehicleConstraintData*)constantBlock;
		PxU32 numActive=0;

		//Susp limit constraints.
		for(PxU32 i=0;i<4;i++)
		{
			if(data->mSuspLimitData.mActiveFlags[i])
			{
				Px1DConstraint& p=constraints[numActive];
				p.linear0=bodyAToWorld.q.rotate(data->mSuspLimitData.mDirs[i]);
				p.angular0=bodyAToWorld.q.rotate(data->mSuspLimitData.mCMOffsets[i].cross(data->mSuspLimitData.mDirs[i]));
				p.geometricError=data->mSuspLimitData.mErrors[i];
				p.linear1=PxVec3(0);
				p.angular1=PxVec3(0);
				p.minImpulse=-FLT_MAX;
				p.maxImpulse=0;
				p.velocityTarget=0;		
				numActive++;
			}
		}

		//Sticky tire friction constraints.
		for(PxU32 i=0;i<4;i++)
		{
			if(data->mStickyTireData.mActiveFlags[i])
			{
				Px1DConstraint& p=constraints[numActive];
				p.linear0=data->mStickyTireData.mDirs[i];
				p.angular0=data->mStickyTireData.mCMOffsets[i].cross(data->mStickyTireData.mDirs[i]);
				p.geometricError=0.0f;
				p.linear1=PxVec3(0);
				p.angular1=PxVec3(0);
				p.minImpulse=-FLT_MAX;
				p.maxImpulse=FLT_MAX;
				p.velocityTarget=data->mStickyTireData.mTargetSpeeds[i];		
				numActive++;
			}
		}

		return numActive;
	}

	static void visualiseConstraint(PxConstraintVisualizer &viz,
		const void* constantBlock,
		const PxTransform& body0Transform,
		const PxTransform& body1Transform,
		PxU32 flags){ PX_UNUSED(&viz); PX_UNUSED(constantBlock); PX_UNUSED(body0Transform); 
					  PX_UNUSED(body1Transform); PX_UNUSED(flags); 
					  PX_ASSERT(body0Transform.isValid()); PX_ASSERT(body1Transform.isValid()); }

public:

	struct SuspLimitConstraintData
	{
		PxVec3 mCMOffsets[4];
		PxVec3 mDirs[4];
		PxReal mErrors[4];
		bool mActiveFlags[4];
	};
	struct StickyTireConstraintData
	{
		PxVec3 mCMOffsets[4];
		PxVec3 mDirs[4];
		PxReal mTargetSpeeds[4];
		bool mActiveFlags[4];
	};

	struct VehicleConstraintData
	{
		SuspLimitConstraintData mSuspLimitData;
		StickyTireConstraintData mStickyTireData;
	};
	VehicleConstraintData mData;

	PxConstraint* mConstraint;

private:

	PxVehicleWheels* mVehicle;

#ifdef PX_X64
	PxU32 mPad[3];
#endif
};

#ifndef PX_DOXYGEN
} // namespace physx
#endif


/** @} */
#endif //PX_VEHICLE_SUSP_LIMIT_CONSTRAINT_SHADER_H
