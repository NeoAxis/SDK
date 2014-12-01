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

#include "CctObstacleContext.h"

using namespace physx;
using namespace Cct;

static PX_FORCE_INLINE ObstacleHandle encodeHandle(PxU32 index, PxGeometryType::Enum type)
{
	PX_ASSERT(index<=0xffff);
	PX_ASSERT(type<=0xffff);
	return (PxU16(index)<<16)|PxU32(type);
}

static PX_FORCE_INLINE PxGeometryType::Enum decodeType(ObstacleHandle handle)
{
	return PxGeometryType::Enum(handle & 0xffff);
}

static PX_FORCE_INLINE PxU32 decodeIndex(ObstacleHandle handle)
{
	return handle>>16;
}


ObstacleContext::ObstacleContext()
{
}

ObstacleContext::~ObstacleContext()
{
}

void ObstacleContext::release()
{
	delete this;
}

ObstacleHandle ObstacleContext::addObstacle(const PxObstacle& obstacle)
{
	const PxGeometryType::Enum type = obstacle.getType();
	if(type==PxGeometryType::eBOX)
	{
		const PxU32 index = mBoxObstacles.size();
		mBoxObstacles.pushBack(static_cast<const PxBoxObstacle&>(obstacle));
		return encodeHandle(index, type);
	}
	else if(type==PxGeometryType::eCAPSULE)
	{
		const PxU32 index = mCapsuleObstacles.size();
		mCapsuleObstacles.pushBack(static_cast<const PxCapsuleObstacle&>(obstacle));
		return encodeHandle(index, type);
	}
	else return INVALID_OBSTACLE_HANDLE;
}

bool ObstacleContext::removeObstacle(ObstacleHandle handle)
{
	const PxGeometryType::Enum type = decodeType(handle);
	const PxU32 index = decodeIndex(handle);

	if(type==PxGeometryType::eBOX)
	{
		const PxU32 size = mBoxObstacles.size();
		PX_ASSERT(index<size);
		if(index>=size)
			return false;

		mBoxObstacles.replaceWithLast(index);
		return true;
	}
	else if(type==PxGeometryType::eCAPSULE)
	{
		const PxU32 size = mCapsuleObstacles.size();
		PX_ASSERT(index<size);
		if(index>=size)
			return false;

		mCapsuleObstacles.replaceWithLast(index);
		return true;
	}
	else return false;
}

bool ObstacleContext::updateObstacle(ObstacleHandle handle, const PxObstacle& obstacle)
{
	const PxGeometryType::Enum type = decodeType(handle);
	PX_ASSERT(type==obstacle.getType());
	if(type!=obstacle.getType())
		return false;

	const PxU32 index = decodeIndex(handle);

	if(type==PxGeometryType::eBOX)
	{
		const PxU32 size = mBoxObstacles.size();
		PX_ASSERT(index<size);
		if(index>=size)
			return false;

		mBoxObstacles[index] = static_cast<const PxBoxObstacle&>(obstacle);
		return true;
	}
	else if(type==PxGeometryType::eCAPSULE)
	{
		const PxU32 size = mCapsuleObstacles.size();
		PX_ASSERT(index<size);
		if(index>=size)
			return false;

		mCapsuleObstacles[index] = static_cast<const PxCapsuleObstacle&>(obstacle);
		return true;
	}
	else return false;
}

PxU32 ObstacleContext::getNbObstacles() const
{
	return mBoxObstacles.size() + mCapsuleObstacles.size();
}

const PxObstacle* ObstacleContext::getObstacle(PxU32 i) const
{
	const PxU32 nbBoxes = mBoxObstacles.size();
	if(i<nbBoxes)
		return &mBoxObstacles[i];
	i -= nbBoxes;

	const PxU32 nbCapsules = mCapsuleObstacles.size();
	if(i<nbCapsules)
		return &mCapsuleObstacles[i];

	return NULL;
}

#include "GuRaycastTests.h"
#include "PxBoxGeometry.h"
#include "PxCapsuleGeometry.h"
#include "PsMathUtils.h"
using namespace Gu;
const PxObstacle* ObstacleContext::raycastSingle(PxRaycastHit& hit, const PxVec3& origin, const PxVec3& unitDir, const PxReal distance) const
{
	PxRaycastHit localHit;
	PxF32 t = FLT_MAX;
	const PxObstacle* touchedObstacle = NULL;

	const PxSceneQueryFlags hintFlags = PxSceneQueryFlag::eDISTANCE;

	const PxU32 nbExtraBoxes = mBoxObstacles.size();
	for(PxU32 i=0;i<nbExtraBoxes;i++)
	{
		const PxBoxObstacle& userBoxObstacle = mBoxObstacles[i];

		PxU32 status = raycast_box(	PxBoxGeometry(userBoxObstacle.mHalfExtents),
									PxTransform(toVec3(userBoxObstacle.mPos), userBoxObstacle.mRot),
									origin, unitDir, distance,
									hintFlags,
									1, &localHit);
		if(status && localHit.distance<t)
		{
			t = localHit.distance;
			hit = localHit;
			touchedObstacle = &userBoxObstacle;
		}
	}

	const PxU32 nbExtraCapsules = mCapsuleObstacles.size();
	for(PxU32 i=0;i<nbExtraCapsules;i++)
	{
		const PxCapsuleObstacle& userCapsuleObstacle = mCapsuleObstacles[i];

		PxU32 status = raycast_capsule(	PxCapsuleGeometry(userCapsuleObstacle.mRadius, userCapsuleObstacle.mHalfHeight),
										PxTransform(toVec3(userCapsuleObstacle.mPos), userCapsuleObstacle.mRot),
										origin, unitDir, distance,
										hintFlags,
										1, &localHit);
		if(status && localHit.distance<t)
		{
			t = localHit.distance;
			hit = localHit;
			touchedObstacle = &userCapsuleObstacle;
		}
	}
	return touchedObstacle;
}