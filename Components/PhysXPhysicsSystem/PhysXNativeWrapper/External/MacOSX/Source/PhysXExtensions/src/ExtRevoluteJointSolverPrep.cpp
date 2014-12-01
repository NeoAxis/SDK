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


#include "ExtRevoluteJoint.h"
#include "PsUtilities.h"
#include "ExtConstraintHelper.h"
#include "CmRenderOutput.h"
#include "PsMathUtils.h"

namespace physx
{
namespace Ext
{
	PxU32 RevoluteJointSolverPrep(Px1DConstraint* constraints,
		PxVec3& body0WorldOffset,
		PxU32 maxConstraints,
		const void* constantBlock,
		const PxTransform& bA2w,
		const PxTransform& bB2w)
	{
		const RevoluteJointData& data = *reinterpret_cast<const RevoluteJointData*>(constantBlock);

		const PxJointLimitPair& limit = data.limit;

		bool limitEnabled = data.jointFlags & PxRevoluteJointFlag::eLIMIT_ENABLED;
		bool limitIsLocked = limitEnabled && limit.lower >= limit.upper;

		PxTransform cA2w = bA2w * data.c2b[0];
		PxTransform cB2w = bB2w * data.c2b[1];

		if(cB2w.q.dot(cA2w.q)<0) 
			cB2w.q = -cB2w.q;

		body0WorldOffset = cB2w.p-bA2w.p;
		Ext::joint::ConstraintHelper ch(constraints, cB2w.p - bA2w.p, cB2w.p - bB2w.p);

		ch.prepareLockedAxes(cA2w.q, cB2w.q, cA2w.transformInv(cB2w.p), 7, limitIsLocked ? 7 : 6);

		if(limitIsLocked)
			return ch.getCount();

		PxVec3 axis = cA2w.rotate(PxVec3(1,0,0));

		if(data.jointFlags & PxRevoluteJointFlag::eDRIVE_ENABLED)
		{
			Px1DConstraint *c = ch.getConstraintRow();

			c->solveGroup = 0;

			c->linear0			= PxVec3(0);
			c->angular0			= axis;
			c->linear1			= PxVec3(0);
			c->angular1			= axis * data.driveGearRatio;

			c->velocityTarget	= data.driveVelocity;

			bool freeSpin = data.jointFlags & PxRevoluteJointFlag::eDRIVE_FREESPIN;
			c->maxImpulse = freeSpin && data.driveVelocity < 0 ? 0 : PX_MAX_F32;
			c->minImpulse = freeSpin && data.driveVelocity > 0 ? 0 : -PX_MAX_F32;
		}


		if(limitEnabled)
		{
			PxQuat cB2cAq = cA2w.q.getConjugate() * cB2w.q;
			PxQuat twist(cB2cAq.x,0,0,cB2cAq.w);

			PxReal magnitude = twist.normalize();
			PxReal tqPhi = magnitude<1e-6f ? 0 : twist.x / (1.0f + twist.w);

			ch.quarterAnglePair(tqPhi, data.tqLow, data.tqHigh, data.tqPad, axis, limit);
		}

		return ch.getCount();
	}
}//namespace

}

//~PX_SERIALIZATION

