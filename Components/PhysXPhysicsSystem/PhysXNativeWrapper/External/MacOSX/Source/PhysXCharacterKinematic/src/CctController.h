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

#ifndef CCT_CONTROLLER
#define CCT_CONTROLLER

/* Exclude from documentation */
/** \cond */

#include "CctCharacterController.h"
#include "PsUserAllocated.h"

namespace physx
{

class PxPhysics;
class PxScene;
class PxRigidDynamic;
class PxGeometry;
class PxMaterial;

namespace Cct
{
	class CharacterControllerManager;

	class Controller : public Ps::UserAllocated
	{
	public:
													Controller(const PxControllerDesc& desc, PxScene* scene);
		virtual										~Controller();

					void							releaseInternal();
					void							getInternalState(PxControllerState& state)	const;
					void							getInternalStats(PxControllerStats& stats)	const;

		virtual		PxF32							getHalfHeightInternal()				const	= 0;
		virtual		bool							getWorldBox(PxExtendedBounds3& box)	const	= 0;
		virtual		PxController*					getPxController()							= 0;

					PxControllerShapeType::Enum		mType;
					PxCCTInteractionMode::Enum		mInteractionMode;
					PxU32							mGroupsBitmask;
		// User params
					CCTParams						mUserParams;
					PxUserControllerHitReport*		mCallback;
					PxControllerBehaviorCallback*	mBehaviorCallback;
					void*							mUserData;
		// Internal data
					SweepTest						mCctModule;			// Internal CCT object. Optim test for Ubi.
					PxRigidDynamic*					mKineActor;			// Associated kinematic actor
					PxExtendedVec3					mPosition;			// Current position
					PxVec3							mDeltaXP;
					PxVec3							mOverlapRecover;
					PxScene*						mScene;				// Handy scene owner
					PxU32							mPreviousSceneTimestamp;
					CharacterControllerManager*		mManager;			// Owner manager
					PxF32							mGlobalTime;
					PxF32							mPreviousGlobalTime;
					PxF32							mProxyDensity;		// Density for proxy actor
					PxF32							mProxyScaleCoeff;	// Scale coeff for proxy actor
					PxU32							mCollisionFlags;	// Last known collision flags (PxControllerFlag)
					bool							mCachedStandingOnMoving;
	protected:
		// Internal methods
					void							setUpDirectionInternal(const PxVec3& up);
					PxShape*						getKineShape()	const;
					bool							createProxyActor(PxPhysics& sdk, const PxGeometry& geometry, const PxMaterial& material);
					bool							setPos(const PxExtendedVec3& pos);
					void							findTouchedObject(const PxControllerFilters& filters, const PxObstacleContext* obstacleContext, const PxVec3& upDirection);
					bool							rideOnTouchedObject(SweptVolume& volume, const PxVec3& upDirection, PxVec3& disp);
					PxU32							move(SweptVolume& volume, const PxVec3& disp, PxF32 minDist, PxF32 elapsedTime, const PxControllerFilters& filters, const PxObstacleContext* obstacles, bool constrainedClimbingMode);
	};

} // namespace Cct

}

/** \endcond */
#endif
