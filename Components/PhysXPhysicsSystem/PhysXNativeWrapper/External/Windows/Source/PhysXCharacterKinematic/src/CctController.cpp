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

#include "PxController.h"
#include "CctController.h"
#include "CctBoxController.h"
#include "CctCharacterControllerManager.h"
#include "PxScene.h"
#include "PxRigidDynamic.h"
#include "PxShape.h"
#include "PxExtensionsAPI.h"
#include "PsMathUtils.h"
#include "PsUtilities.h"
#include "PxCoreUtilities.h"

using namespace physx;
using namespace Cct;

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Controller::Controller(const PxControllerDesc& desc, PxScene* s) :
	mScene					(s),
	mPreviousSceneTimestamp	(0xffffffff),
	mManager				(NULL),
	mGlobalTime				(0.0f),
	mPreviousGlobalTime		(0.0f),
	mProxyDensity			(0.0f),
	mProxyScaleCoeff		(0.0f),
	mCollisionFlags			(0),
	mCachedStandingOnMoving	(false)
{
	mType								= PxControllerShapeType::eFORCE_DWORD;
	mInteractionMode					= desc.interactionMode;
	mGroupsBitmask						= desc.groupsBitmask;

	mUserParams.mNonWalkableMode		= desc.nonWalkableMode;
	mUserParams.mSlopeLimit				= desc.slopeLimit;
	mUserParams.mContactOffset			= desc.contactOffset;
	mUserParams.mStepOffset				= desc.stepOffset;
	mUserParams.mInvisibleWallHeight	= desc.invisibleWallHeight;
	mUserParams.mMaxJumpHeight			= desc.maxJumpHeight;
	mUserParams.mHandleSlope			= desc.slopeLimit!=0.0f;

	mCallback							= desc.callback;
	mBehaviorCallback					= desc.behaviorCallback;
	mUserData							= desc.userData;

	mKineActor							= NULL;
	mPosition							= desc.position;
	mProxyDensity						= desc.density;
	mProxyScaleCoeff					= desc.scaleCoeff;

	mCctModule.mVolumeGrowth			= desc.volumeGrowth;

	mDeltaXP							= PxVec3(0);
	mOverlapRecover						= PxVec3(0);

	mUserParams.mUpDirection = PxVec3(0.0f);
	setUpDirectionInternal(desc.upDirection);
}

Controller::~Controller()
{
	if(mScene && mKineActor)
		mKineActor->release();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Controller::setUpDirectionInternal(const PxVec3& up)
{
	PX_CHECK_MSG(up.isNormalized(), "CCT: up direction must be normalized");

	if(mUserParams.mUpDirection==up)
		return;

//	const PxQuat q = Ps::computeQuatFromNormal(up);
	const PxQuat q = Ps::rotationArc(PxVec3(1.0f, 0.0f, 0.0f), up);

	mUserParams.mQuatFromUp		= q;
	mUserParams.mUpDirection	= up;

	// Update kinematic actor
	if(mKineActor)
	{
		PxTransform pose = mKineActor->getGlobalPose();
		pose.q = q;
		mKineActor->setGlobalPose(pose);	
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Controller::releaseInternal()
{
	mManager->releaseController(*getPxController());
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Controller::getInternalState(PxControllerState& state) const
{
	state.deltaXP			= mDeltaXP;
	state.touchedShape		= mCctModule.mTouchedShape;
	state.touchedObstacle	= const_cast<PxObstacle*>(mCctModule.mTouchedObstacle);
	state.standOnAnotherCCT	= (mCctModule.mFlags & STF_TOUCH_OTHER_CCT)!=0;
	state.standOnObstacle	= (mCctModule.mFlags & STF_TOUCH_OBSTACLE)!=0;
	state.isMovingUp		= (mCctModule.mFlags & STF_IS_MOVING_UP)!=0;
	state.collisionFlags	= mCollisionFlags;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Controller::getInternalStats(PxControllerStats& stats) const
{
	stats.nbFullUpdates		= mCctModule.mNbFullUpdates;
	stats.nbPartialUpdates	= mCctModule.mNbPartialUpdates;
	stats.nbIterations		= mCctModule.mNbIterations;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

bool Controller::setPos(const PxExtendedVec3& pos)
{
	mPosition = pos;

	// Update kinematic actor
	if(mKineActor)
	{
		PxTransform targetPose = mKineActor->getGlobalPose();
		targetPose.p = toVec3(mPosition);  // LOSS OF ACCURACY
		mKineActor->setKinematicTarget(targetPose);	
	}
	return true;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

bool Controller::createProxyActor(PxPhysics& sdk, const PxGeometry& geometry, const PxMaterial& material)
{
	// PT: we don't disable raycasting or CD because:
	// - raycasting is needed for visibility queries (the SDK otherwise doesn't know about the CCTS)
	// - collision is needed because the only reason we create actors there is to handle collisions with dynamic shapes
	// So it's actually wrong to disable any of those.

	PxTransform globalPose;
	globalPose.p = toVec3(mPosition);	// LOSS OF ACCURACY
	globalPose.q = mUserParams.mQuatFromUp;

	mKineActor = sdk.createRigidDynamic(globalPose);
	if(!mKineActor)
		return false;

	mKineActor->createShape(geometry, material);
	mKineActor->setRigidDynamicFlag(PxRigidDynamicFlag::eKINEMATIC, true);

	PxRigidBodyExt::updateMassAndInertia(*mKineActor, mProxyDensity);
	mScene->addActor(*mKineActor);
	return true;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PxShape* Controller::getKineShape() const
{
	// PT: TODO: cache this and avoid the virtual call
	PxShape* shape = NULL;
	PxU32 nb = mKineActor->getShapes(&shape, 1);
	PX_ASSERT(nb==1);
	PX_UNUSED(nb);
	return shape;
}
