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


#include "ExtFixedJoint.h"
#include "ExtConstraintHelper.h"
#include "CmRenderOutput.h"
#include "CmVisualization.h"
#include "CmSerialAlignment.h"
#ifdef PX_PS3
#include "PS3/ExtFixedJointSpu.h"
#endif

using namespace physx;
using namespace Ext;

namespace physx
{
	PxFixedJoint* PxFixedJointCreate(PxPhysics& physics,
		PxRigidActor* actor0, const PxTransform& localFrame0,
		PxRigidActor* actor1, const PxTransform& localFrame1);
}

PxFixedJoint* physx::PxFixedJointCreate(PxPhysics& physics,
										PxRigidActor* actor0, const PxTransform& localFrame0,
										PxRigidActor* actor1, const PxTransform& localFrame1)
{
	PX_CHECK_AND_RETURN_NULL(localFrame0.isValid(), "PxFixedJointCreate: local frame 0 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(localFrame1.isValid(), "PxFixedJointCreate: local frame 1 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(actor0 && actor0->is<PxRigidBody>() || actor1 && actor1->is<PxRigidBody>(), "PxFixedJointCreate: at least one actor must be dynamic");

	FixedJoint* j = PX_NEW(FixedJoint)(physics.getTolerancesScale(), actor0, localFrame0, actor1, localFrame1);

	if(j->attach(physics, actor0, actor1))
		return j;

	PX_DELETE(j);
	return NULL;
}

PxReal FixedJoint::getProjectionLinearTolerance() const
{ 
	return data().projectionLinearTolerance; 
}

void FixedJoint::setProjectionLinearTolerance(PxReal tolerance)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(tolerance), "PxFixedJoint::setProjectionLinearTolerance: invalid parameter");
	data().projectionLinearTolerance = tolerance; 
	markDirty(); 
}

PxReal FixedJoint::getProjectionAngularTolerance() const
{ 
	return data().projectionAngularTolerance; 
}

void FixedJoint::setProjectionAngularTolerance(PxReal tolerance)	
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(tolerance) && tolerance >=0 && tolerance <= PxPi, "PxFixedJoint::setProjectionAngularTolerance: invalid parameter");
	data().projectionAngularTolerance = tolerance; markDirty(); 
}


namespace
{
void FixedJointVisualize(PxConstraintVisualizer& viz,
						 const void* constantBlock,
						 const PxTransform& body0Transform,
						 const PxTransform& body1Transform,
						 PxU32 flags)
{
	const FixedJointData& data = *reinterpret_cast<const FixedJointData*>(constantBlock);

	const PxTransform& t0 = body0Transform * data.c2b[0];
	const PxTransform& t1 = body1Transform * data.c2b[1];

	viz.visualizeJointFrames(t0, t1);
}



void FixedJointProject(const void* constantBlock,
					   PxTransform& bodyAToWorld,
					   PxTransform& bodyBToWorld,
					   bool projectToA)
{

	using namespace joint;
	const FixedJointData &data = *reinterpret_cast<const FixedJointData*>(constantBlock);

	PxTransform cA2w, cB2w, cB2cA, projected;
	computeDerived(data, bodyAToWorld, bodyBToWorld, cA2w, cB2w, cB2cA);

	bool linearTrunc, angularTrunc;
	projected.p = truncateLinear(cB2cA.p, data.projectionLinearTolerance, linearTrunc);
	projected.q = truncateAngular(cB2cA.q, PxSin(data.projectionAngularTolerance/2), PxCos(data.projectionAngularTolerance/2), angularTrunc);
	
	if(linearTrunc || angularTrunc)
		projectTransforms(bodyAToWorld, bodyBToWorld, cA2w, cB2w, projected, data, projectToA);
}
}

bool Ext::FixedJoint::attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1)
{
	mPxConstraint = physics.createConstraint(actor0, actor1, *this, sShaders, sizeof(FixedJointData));
	return mPxConstraint!=NULL;
}




// PX_SERIALIZATION
BEGIN_FIELDS(FixedJoint)
//	DEFINE_STATIC_ARRAY(FixedJoint, mData, PxField::eBYTE, sizeof(FixedJointData), Ps::F_SERIALIZE),
END_FIELDS(FixedJoint)

void FixedJoint::exportExtraData(PxSerialStream& stream)
{
	if(mData)
	{
		Cm::alignStream(stream, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		stream.storeBuffer(mData, sizeof(FixedJointData));
	}
}

char* FixedJoint::importExtraData(char* address, PxU32& totalPadding)
{
	if(mData)
	{
		address = Cm::alignStream(address, totalPadding, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		mData = reinterpret_cast<FixedJointData*>(address);
		address += sizeof(FixedJointData);
	}
	return address;
}

bool FixedJoint::resolvePointers(PxRefResolver& v, void* context)
{
	FixedJointT::resolvePointers(v, context);

	setPxConstraint(resolveConstraintPtr(v, getPxConstraint(), getConnector(), sShaders));
	return true;
}

//~PX_SERIALIZATION

#ifdef PX_PS3
PxConstraintShaderTable Ext::FixedJoint::sShaders = { Ext::FixedJointSolverPrep, ExtFixedJointSpu, EXTFIXEDJOINTSPU_SIZE, FixedJointProject, FixedJointVisualize };
#else
PxConstraintShaderTable Ext::FixedJoint::sShaders = { Ext::FixedJointSolverPrep, 0, 0, FixedJointProject, FixedJointVisualize };
#endif
