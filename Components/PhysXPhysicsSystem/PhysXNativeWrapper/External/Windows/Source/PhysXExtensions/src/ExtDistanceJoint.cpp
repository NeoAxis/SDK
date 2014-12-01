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


#include "ExtDistanceJoint.h"
#include "CmSerialAlignment.h"
#ifdef PX_PS3
#include "PS3/ExtDistanceJointSpu.h"
#endif

using namespace physx;
using namespace Ext;

namespace physx
{
	PxDistanceJoint* PxDistanceJointCreate(PxPhysics& physics,
		PxRigidActor* actor0, const PxTransform& localFrame0,
		PxRigidActor* actor1, const PxTransform& localFrame1);
}

PxDistanceJoint* physx::PxDistanceJointCreate(PxPhysics& physics,
											  PxRigidActor* actor0, const PxTransform& localFrame0,
											  PxRigidActor* actor1, const PxTransform& localFrame1)
{
	PX_CHECK_AND_RETURN_NULL(localFrame0.isValid(), "PxDistanceJointCreate: local frame 0 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(localFrame1.isValid(), "PxDistanceJointCreate: local frame 1 is not a valid transform"); 
	PX_CHECK_AND_RETURN_NULL(actor0 && actor0->is<PxRigidBody>() || actor1 && actor1->is<PxRigidBody>(), "PxD6JointCreate: at least one actor must be dynamic");

	DistanceJoint* j = PX_NEW(DistanceJoint)(physics.getTolerancesScale(), actor0, localFrame0, actor1, localFrame1);

	if(j->attach(physics, actor0, actor1))
		return j;

	PX_DELETE(j);
	return NULL;
}

void DistanceJoint::setMinDistance(PxReal distance)	
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(distance), "PxDistanceJoint::setMinDistance: invalid parameter");
	data().minDistance = distance;
	markDirty();
}

PxReal DistanceJoint::getMinDistance() const
{ 
	return data().minDistance;		
}

void DistanceJoint::setMaxDistance(PxReal distance)	
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(distance), "PxDistanceJoint::setMaxDistance: invalid parameter");
	data().maxDistance = distance;
	markDirty();
}

PxReal DistanceJoint::getMaxDistance() const	
{ 
	return data().maxDistance;			
}

void DistanceJoint::setTolerance(PxReal tolerance) 
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(tolerance), "PxDistanceJoint::setTolerance: invalid parameter");
	data().tolerance = tolerance;
	markDirty();
}

PxReal DistanceJoint::getTolerance() const
{ 
	return data().tolerance;			
}

void DistanceJoint::setSpring(PxReal spring)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(spring), "PxDistanceJoint::setSpring: invalid parameter");
	data().spring = spring;
	markDirty();
}

PxReal DistanceJoint::getSpring() const	
{ 
	return data().spring;
}

void DistanceJoint::setDamping(PxReal damping)
{ 
	PX_CHECK_AND_RETURN(PxIsFinite(damping), "PxDistanceJoint::setDamping: invalid parameter");
	data().damping = damping;
	markDirty();	
}

PxReal DistanceJoint::getDamping() const
{ 
	return data().damping;
}

PxDistanceJointFlags DistanceJoint::getDistanceJointFlags(void) const
{ 
	return data().jointFlags;		
}

void DistanceJoint::setDistanceJointFlags(PxDistanceJointFlags flags) 
{ 
	data().jointFlags = flags; 
	markDirty();	
}

void DistanceJoint::setDistanceJointFlag(PxDistanceJointFlag::Enum flag, bool value)
{
	if(value)
		data().jointFlags |= flag;
	else
		data().jointFlags &= ~flag;
	markDirty();
}



namespace
{
void DistanceJointVisualize(PxConstraintVisualizer& viz,
							const void* constantBlock,
							const PxTransform& body0Transform,
							const PxTransform& body1Transform,
							PxU32 flags)
{
}

void DistanceJointProject(const void* constantBlock,
						  PxTransform& bodyAToWorld,
						  PxTransform& bodyBToWorld,
						  bool projectToA)
{
	// TODO
}


}

bool Ext::DistanceJoint::attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1)
{
	mPxConstraint = physics.createConstraint(actor0, actor1, *this, sShaders, sizeof(DistanceJointData));
	return mPxConstraint!=NULL;
}


// PX_SERIALIZATION
BEGIN_FIELDS(DistanceJoint)
//	DEFINE_STATIC_ARRAY(DistanceJoint, mData, PxField::eBYTE, sizeof(DistanceJointData), Ps::F_SERIALIZE),
END_FIELDS(DistanceJoint)

void DistanceJoint::exportExtraData(PxSerialStream& stream)
{
	if(mData)
	{
		Cm::alignStream(stream, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		stream.storeBuffer(mData, sizeof(DistanceJointData));
	}
}

char* DistanceJoint::importExtraData(char* address, PxU32& totalPadding)
{
	if(mData)
	{
		address = Cm::alignStream(address, totalPadding, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP);
		mData = reinterpret_cast<DistanceJointData*>(address);
		address += sizeof(DistanceJointData);
	}
	return address;
}

bool DistanceJoint::resolvePointers(PxRefResolver& v, void* context)
{
	DistanceJointT::resolvePointers(v, context);

	setPxConstraint(resolveConstraintPtr(v, getPxConstraint(), getConnector(), sShaders));
	return true;
}

//~PX_SERIALIZATION
#ifdef PX_PS3
PxConstraintShaderTable Ext::DistanceJoint::sShaders = { Ext::DistanceJointSolverPrep, ExtDistanceJointSpu, EXTDISTANCEJOINTSPU_SIZE, DistanceJointProject, DistanceJointVisualize };
#else
PxConstraintShaderTable Ext::DistanceJoint::sShaders = { Ext::DistanceJointSolverPrep, 0, 0, DistanceJointProject, DistanceJointVisualize };
#endif
