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


#include "ExtSphericalJoint.h"
#include "ExtConstraintHelper.h"
#include "CmConeLimitHelper.h"
#include "CmRenderOutput.h"
#include "CmVisualization.h"
#include "CmSerialAlignment.h"
#ifdef PX_PS3
#include "PS3/ExtSphericalJointSpu.h"
#endif

using namespace physx;
using namespace Ext;

namespace physx
{
	PxSphericalJoint* PxSphericalJointCreate(PxPhysics& physics,
		PxRigidActor* actor0, const PxTransform& localFrame0,
		PxRigidActor* actor1, const PxTransform& localFrame1);
}

PxSphericalJoint* physx::PxSphericalJointCreate(PxPhysics& physics,
												PxRigidActor* actor0, const PxTransform& localFrame0,
												PxRigidActor* actor1, const PxTransform& localFrame1)
{
	PX_CHECK_AND_RETURN_NULL(localFrame0.isValid(), "PxSphericalJointCreate: local frame 0 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(localFrame1.isValid(), "PxSphericalJointCreate: local frame 1 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(actor0 && actor0->is<PxRigidBody>() || actor1 && actor1->is<PxRigidBody>(), "PxSphericalJointCreate: at least one actor must be dynamic");

	SphericalJoint* j = PX_NEW(SphericalJoint)(physics.getTolerancesScale(), actor0, localFrame0, actor1, localFrame1);

	if(j->attach(physics, actor0, actor1))
		return j;

	PX_DELETE(j);
	return NULL;
}

void SphericalJoint::setProjectionLinearTolerance(PxReal tolerance)
{	
	PX_CHECK_AND_RETURN(PxIsFinite(tolerance), "PxSphericalJoint::setProjectionLinearTolerance: invalid parameter");
	data().projectionLinearTolerance = tolerance;
	markDirty(); 
}

PxReal SphericalJoint::getProjectionLinearTolerance() const	
{	
	return data().projectionLinearTolerance;		
}

void SphericalJoint::setLimitCone(const PxJointLimitCone &limit)
{	
	PX_CHECK_AND_RETURN(limit.isValid(), "PxSphericalJoint::setLimit: invalid parameter");
	data().limit = limit; 
}

PxJointLimitCone SphericalJoint::getLimitCone() const
{	
	return data().limit; 
}


PxSphericalJointFlags SphericalJoint::getSphericalJointFlags(void) const
{ 
	return data().jointFlags; 
}

void SphericalJoint::setSphericalJointFlags(PxSphericalJointFlags flags)
{ 
	data().jointFlags = flags; 
}
void SphericalJoint::setSphericalJointFlag(PxSphericalJointFlag::Enum flag, bool value)
{
	if(value)
		data().jointFlags |= flag;
	else
		data().jointFlags &= ~flag;
	markDirty();
}


void* Ext::SphericalJoint::prepareData()
{
	SphericalJointData& d = data();
	if(d.jointFlags & PxSphericalJointFlag::eLIMIT_ENABLED)
	{
		d.tanQYLimit = PxTan(d.limit.yAngle/4);
		d.tanQZLimit = PxTan(d.limit.zAngle/4);
		d.tanQPad	 = PxTan(d.limit.contactDistance/4);
	}
	return SphericalJointT::prepareData();
}

namespace
{

void SphericalJointVisualize(PxConstraintVisualizer& viz,
							 const void* constantBlock,
							 const PxTransform& body0Transform,
							 const PxTransform& body1Transform,
							 PxU32 flags)
{
	using namespace joint;
	const SphericalJointData& data = *reinterpret_cast<const SphericalJointData*>(constantBlock);

	PxTransform cA2w = body0Transform * data.c2b[0];
	PxTransform cB2w = body1Transform * data.c2b[1];

	viz.visualizeJointFrames(cA2w, cB2w);


	if(data.jointFlags & PxSphericalJointFlag::eLIMIT_ENABLED)
	{
		if(cA2w.q.dot(cB2w.q)<0)
			cB2w.q = -cB2w.q;

		PxTransform cB2cA = cA2w.transformInv(cB2w);	
		PxQuat swing, twist;
		Ps::separateSwingTwist(cB2cA.q,swing,twist);

		PxVec3 tanQSwing = PxVec3(0, Ps::tanHalf(swing.z,swing.w), -Ps::tanHalf(swing.y,swing.w));
		Cm::ConeLimitHelper coneHelper(data.tanQZLimit, data.tanQYLimit, data.tanQPad);
		viz.visualizeLimitCone(cA2w, data.tanQZLimit, data.tanQYLimit, 
			!coneHelper.contains(tanQSwing));
	}
}



void SphericalJointProject(const void* constantBlock,
						   PxTransform& bodyAToWorld,
						   PxTransform& bodyBToWorld,
						   bool projectToA)
{
	using namespace joint;

	const SphericalJointData& data = *reinterpret_cast<const SphericalJointData*>(constantBlock);

	PxTransform cA2w, cB2w, cB2cA, projected;
	joint::computeDerived(data, bodyAToWorld, bodyBToWorld, cA2w, cB2w, cB2cA);

	bool linearTrunc;
	projected.p = truncateLinear(cB2cA.p, data.projectionLinearTolerance, linearTrunc);

	if(linearTrunc)
	{
		projected.q = cB2cA.q;
		projectTransforms(bodyAToWorld, bodyBToWorld, cA2w, cB2w, projected, data, projectToA);
	}
}
}


bool Ext::SphericalJoint::attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1)
{
	mPxConstraint = physics.createConstraint(actor0, actor1, *this, sShaders, sizeof(SphericalJointData));
	return mPxConstraint!=NULL;
}


// PX_SERIALIZATION
BEGIN_FIELDS(SphericalJoint)
//	DEFINE_STATIC_ARRAY(SphericalJoint, mData, PxField::eBYTE, sizeof(SphericalJointData), Ps::F_SERIALIZE),
END_FIELDS(SphericalJoint)

void SphericalJoint::exportExtraData(PxSerialStream& stream)
{
	if(mData)
	{
		Cm::alignStream(stream, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		stream.storeBuffer(mData, sizeof(SphericalJointData));
	}
}

char* SphericalJoint::importExtraData(char* address, PxU32& totalPadding)
{
	if(mData)
	{
		address = Cm::alignStream(address, totalPadding, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		mData = reinterpret_cast<SphericalJointData*>(address);
		address += sizeof(SphericalJointData);
	}
	return address;
}

bool SphericalJoint::resolvePointers(PxRefResolver& v, void* context)
{
	SphericalJointT::resolvePointers(v, context);
	setPxConstraint(resolveConstraintPtr(v, getPxConstraint(), getConnector(), sShaders));
	return true;
}

//~PX_SERIALIZATION

#ifdef PX_PS3
PxConstraintShaderTable Ext::SphericalJoint::sShaders = { Ext::SphericalJointSolverPrep, ExtSphericalJointSpu, EXTSPHERICALJOINTSPU_SIZE, SphericalJointProject, SphericalJointVisualize };
#else
PxConstraintShaderTable Ext::SphericalJoint::sShaders = { Ext::SphericalJointSolverPrep, 0, 0, SphericalJointProject, SphericalJointVisualize };
#endif
