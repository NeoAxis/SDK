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
#include "CmVisualization.h"
#include "CmSerialAlignment.h"

#ifdef PX_PS3
#include "PS3/ExtRevoluteJointSpu.h"
#endif

using namespace physx;
using namespace Ext;

namespace physx
{
	PxRevoluteJoint* PxRevoluteJointCreate(PxPhysics& physics,
		PxRigidActor* actor0, const PxTransform& localFrame0,
		PxRigidActor* actor1, const PxTransform& localFrame1);
}

PxRevoluteJoint* physx::PxRevoluteJointCreate(PxPhysics& physics,
											  PxRigidActor* actor0, const PxTransform& localFrame0,
											  PxRigidActor* actor1, const PxTransform& localFrame1)
{
	PX_CHECK_AND_RETURN_NULL(localFrame0.isValid(), "PxRevoluteJointCreate: local frame 0 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(localFrame1.isValid(), "PxRevoluteJointCreate: local frame 1 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(actor0 && actor0->is<PxRigidBody>() || actor1 && actor1->is<PxRigidBody>(), "PxRevoluteJointCreate: at least one actor must be dynamic");

	RevoluteJoint* j = PX_NEW(RevoluteJoint)(physics.getTolerancesScale(), actor0, localFrame0, actor1, localFrame1);

	if(j->attach(physics, actor0, actor1))
		return j;

	PX_DELETE(j);
	return NULL;
}


PxJointLimitPair RevoluteJoint::getLimit()	const
{ 
	return data().limit;	
}

void RevoluteJoint::setLimit(const PxJointLimitPair& limit)
{ 
	PX_CHECK_AND_RETURN(limit.isValid() && limit.lower >= -PxPi && limit.upper <= PxPi, "PxRevoluteJoint::setLimit: invalid parameter");
	data().limit = limit; markDirty();	
}

PxReal RevoluteJoint::getDriveVelocity() const
{ 
	return data().driveVelocity;	
}

void RevoluteJoint::setDriveVelocity(PxReal velocity)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(velocity), "PxRevoluteJoint::setDriveVelocity: invalid parameter");
	data().driveVelocity = velocity; 
	markDirty(); 
}

PxReal RevoluteJoint::getDriveForceLimit() const
{ 
	return data().driveForceLimit;	
}

void RevoluteJoint::setDriveForceLimit(PxReal forceLimit)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(forceLimit), "PxRevoluteJoint::setDriveForceLimit: invalid parameter");
	data().driveForceLimit = forceLimit; 
	markDirty(); 
}

PxReal RevoluteJoint::getDriveGearRatio() const
{ 
	return data().driveGearRatio;	
}

void RevoluteJoint::setDriveGearRatio(PxReal gearRatio)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(gearRatio) && gearRatio>0, "PxRevoluteJoint::setDriveGearRatio: invalid parameter");
	data().driveGearRatio = gearRatio; 
	markDirty(); 
}

void RevoluteJoint::setProjectionAngularTolerance(PxReal tolerance)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(tolerance) && tolerance>=0 && tolerance<=PxPi, "PxRevoluteJoint::setProjectionAngularTolerance: invalid parameter");
	data().projectionAngularTolerance = tolerance;
	markDirty();	
}

PxReal RevoluteJoint::getProjectionAngularTolerance() const	
{ 
	return data().projectionAngularTolerance; 
}

void RevoluteJoint::setProjectionLinearTolerance(PxReal tolerance)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(tolerance), "PxRevoluteJoint::setProjectionLinearTolerance: invalid parameter");
	data().projectionLinearTolerance = tolerance;
	markDirty(); 
}

PxReal RevoluteJoint::getProjectionLinearTolerance() const
{ 
	return data().projectionLinearTolerance;		
}

PxRevoluteJointFlags RevoluteJoint::getRevoluteJointFlags(void)	const
{ 
	return data().jointFlags; 
}

void RevoluteJoint::setRevoluteJointFlags(PxRevoluteJointFlags flags)
{ 
	data().jointFlags = flags; 
}

void RevoluteJoint::setRevoluteJointFlag(PxRevoluteJointFlag::Enum flag, bool value)
{
	if(value)
		data().jointFlags |= flag;
	else
		data().jointFlags &= ~flag;
	markDirty();
}




void* Ext::RevoluteJoint::prepareData()
{
	data().tqHigh =  PxTan(data().limit.upper/4);
	data().tqLow = PxTan(data().limit.lower/4);
	data().tqPad = PxTan(data().limit.contactDistance/4);

	return RevoluteJointT::prepareData();
}


namespace
{

void RevoluteJointProject(const void* constantBlock,
						  PxTransform& bodyAToWorld,
						  PxTransform& bodyBToWorld,
						  bool projectToA)
{
	using namespace joint;

	const RevoluteJointData& data = *reinterpret_cast<const RevoluteJointData*>(constantBlock);

	PxTransform cA2w, cB2w, cB2cA, projected;
	computeDerived(data, bodyAToWorld, bodyBToWorld, cA2w, cB2w, cB2cA);

	bool linearTrunc, angularTrunc;
	projected.p = truncateLinear(cB2cA.p, data.projectionLinearTolerance, linearTrunc);

	PxQuat swing, twist, projSwing;
	Ps::separateSwingTwist(cB2cA.q,swing,twist);
	projSwing = truncateAngular(swing, PxSin(data.projectionAngularTolerance/2), PxCos(data.projectionAngularTolerance/2), angularTrunc);
	
	if(linearTrunc || angularTrunc)
	{
		projected.q = projSwing * twist;
		projectTransforms(bodyAToWorld, bodyBToWorld, cA2w, cB2w, projected, data, projectToA);
	}
}

void RevoluteJointVisualize(PxConstraintVisualizer& viz,
		 					const void* constantBlock,
							const PxTransform& body0Transform,
							const PxTransform& body1Transform,
							PxU32 flags)
{
	const RevoluteJointData& data = *reinterpret_cast<const RevoluteJointData*>(constantBlock);

	const PxTransform& t0 = body0Transform * data.c2b[0];
	const PxTransform& t1 = body1Transform * data.c2b[1];

	viz.visualizeJointFrames(t0, t1);

	if(data.jointFlags & PxRevoluteJointFlag::eLIMIT_ENABLED)
		viz.visualizeAngularLimit(t0, data.limit.lower, data.limit.upper, false);
}
}

bool Ext::RevoluteJoint::attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1)
{
	mPxConstraint = physics.createConstraint(actor0, actor1, *this, sShaders, sizeof(RevoluteJointData));
	return mPxConstraint!=NULL;
}

// PX_SERIALIZATION
BEGIN_FIELDS(RevoluteJoint)
//	DEFINE_STATIC_ARRAY(RevoluteJoint, mData, PxField::eBYTE, sizeof(RevoluteJointData), Ps::F_SERIALIZE),
END_FIELDS(RevoluteJoint)

void RevoluteJoint::exportExtraData(PxSerialStream& stream)
{
	if(mData)
	{
		Cm::alignStream(stream, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		stream.storeBuffer(mData, sizeof(RevoluteJointData));
	}
}

char* RevoluteJoint::importExtraData(char* address, PxU32& totalPadding)
{
	if(mData)
	{
		address = Cm::alignStream(address, totalPadding, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		mData = reinterpret_cast<RevoluteJointData*>(address);
		address += sizeof(RevoluteJointData);
	}
	return address;
}

bool RevoluteJoint::resolvePointers(PxRefResolver& v, void* context)
{
	RevoluteJointT::resolvePointers(v, context);

	setPxConstraint(resolveConstraintPtr(v, getPxConstraint(), getConnector(), sShaders));
	return true;
}

//~PX_SERIALIZATION

#ifdef PX_PS3
PxConstraintShaderTable Ext::RevoluteJoint::sShaders = { Ext::RevoluteJointSolverPrep, ExtRevoluteJointSpu, EXTREVOLUTEJOINTSPU_SIZE, RevoluteJointProject, RevoluteJointVisualize };
#else
PxConstraintShaderTable Ext::RevoluteJoint::sShaders = { Ext::RevoluteJointSolverPrep, 0, 0, RevoluteJointProject, RevoluteJointVisualize };
#endif