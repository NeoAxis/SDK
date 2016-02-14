// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXJoint.h"
#include "PhysXBody.h"
#include "PhysXWorld.h"

PhysXJoint::PhysXJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, PxRigidActor* pActor1)
{
	this->mPhysics = pPhysics;
	this->mActor0 = pActor0;
	this->mActor1 = pActor1;
}
	
PhysXJoint::~PhysXJoint()
{
	mJoint->release();
}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PhysXHingeJoint::PhysXHingeJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
	PxRigidActor* pActor1, const PxTransform& localFrame1) : PhysXJoint(pPhysics, pActor0, pActor1)
{
	mJoint = PxRevoluteJointCreate(*mPhysics, pActor0, localFrame0, pActor1, localFrame1);
}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PhysXFixedJoint::PhysXFixedJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
	PxRigidActor* pActor1, const PxTransform& localFrame1) : PhysXJoint(pPhysics, pActor0, pActor1)
{
	mJoint = PxFixedJointCreate(*mPhysics, pActor0, localFrame0, pActor1, localFrame1);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PhysXSliderJoint::PhysXSliderJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
	PxRigidActor* pActor1, const PxTransform& localFrame1) : PhysXJoint(pPhysics, pActor0, pActor1)
{
	mJoint = PxPrismaticJointCreate(*mPhysics, pActor0, localFrame0, pActor1, localFrame1);
}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PhysXD6Joint::PhysXD6Joint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
	PxRigidActor* pActor1, const PxTransform& localFrame1) : PhysXJoint(pPhysics, pActor0, pActor1)
{
	mJoint = PxD6JointCreate(*mPhysics, pActor0, localFrame0, pActor1, localFrame1);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PhysXDistanceJoint::PhysXDistanceJoint( PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
	PxRigidActor* pActor1, const PxTransform& localFrame1 ) : PhysXJoint( pPhysics, pActor0, pActor1 )
{
	mJoint = PxDistanceJointCreate( *mPhysics, pActor0, localFrame0, pActor1, localFrame1 );
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT void PhysXJoint_Destroy(PhysXJoint* _this)
{
	delete _this;
}

EXPORT void PhysXJoint_GetGlobalPose(PhysXJoint* _this, PxVec3& globalPosition, PxQuat& globalRotation)
{
	PxTransform pose;
	if(_this->mActor0)
	{
		PxTransform actorPose = _this->mActor0->getGlobalPose();
		PxTransform localPose = _this->mJoint->getLocalPose(PxJointActorIndex::eACTOR0);
		pose = actorPose * localPose;
	}
	else if(_this->mActor1)
	{
		PxTransform actorPose = _this->mActor1->getGlobalPose();
		PxTransform localPose = _this->mJoint->getLocalPose(PxJointActorIndex::eACTOR1);
		pose = actorPose * localPose;
	}
	else
		pose = PxTransform::createIdentity();

	globalPosition = pose.p;
	globalRotation = pose.q;
}

EXPORT bool PhysXJoint_IsBroken(PhysXJoint* _this)
{
	return _this->mJoint->getConstraintFlags() & PxConstraintFlag::eBROKEN;
}

EXPORT void PhysXJoint_SetBreakForce(PhysXJoint* _this, float force, float torque)
{
	_this->mJoint->setBreakForce(force, torque);
}

EXPORT void PhysXJoint_SetCollisionEnable(PhysXJoint* _this, bool value)
{
	_this->mJoint->setConstraintFlag(PxConstraintFlag::eCOLLISION_ENABLED, value);
}

EXPORT void PhysXJoint_SetProjectionEnable(PhysXJoint* _this, bool value)
{
	_this->mJoint->setConstraintFlag(PxConstraintFlag::ePROJECTION, value);
}

EXPORT void PhysXJoint_SetVisualizationEnable(PhysXJoint* _this, bool value)
{
	_this->mJoint->setConstraintFlag(PxConstraintFlag::eVISUALIZATION, value);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void SetLimitProperties(PxJointLimitParameters& limit, float restitution, float spring, float damping)
{
	limit.restitution = restitution;
	//!!!!другие параметры теперь?
//	limit.spring = spring;
	limit.damping = damping;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXJoint* PhysXHingeJoint_Create(
	PhysXBody* actor0, const PxVec3& localPosition0, const PxQuat& localRotation0, 
	PhysXBody* actor1, const PxVec3& localPosition1, const PxQuat& localRotation1)
{
	PxTransform localFrame0(localPosition0, localRotation0);
	PxTransform localFrame1(localPosition1, localRotation1);
	return new PhysXHingeJoint(world->mPhysics, actor0->mActor, localFrame0, actor1->mActor, localFrame1);
}

EXPORT void PhysXHingeJoint_SetLimit(PhysXHingeJoint* _this, bool enabled, float low, float high, float limitContactDistance, 
	float restitution, float spring, float damping)
{
	_this->GetJoint()->setRevoluteJointFlag(PxRevoluteJointFlag::eLIMIT_ENABLED, enabled);

	if(enabled)
	{
		PxJointAngularLimitPair limit(low, high, limitContactDistance);
		SetLimitProperties(limit, restitution, spring, damping);
		if(limit.lower > limit.upper)
			limit.upper = limit.lower;
		if(limit.contactDistance > limit.upper - limit.lower)
			limit.upper = limit.lower + limit.contactDistance * 1.001f;
		_this->GetJoint()->setLimit(limit);
	}
}

EXPORT void PhysXHingeJoint_SetDrive(PhysXHingeJoint* _this, float velocity, const float maxForce, bool freeSpin, 
	float driveGearRatio)
{
	PxRevoluteJoint* joint = _this->GetJoint();
	if(velocity !=0 || maxForce != 0)
	{
		joint->setRevoluteJointFlag(PxRevoluteJointFlag::eDRIVE_ENABLED, true);
		joint->setRevoluteJointFlag(PxRevoluteJointFlag::eDRIVE_FREESPIN, freeSpin);
		joint->setDriveVelocity(velocity);
		joint->setDriveForceLimit(maxForce);
		joint->setDriveGearRatio(driveGearRatio);
	}
	else
	{
		joint->setRevoluteJointFlag(PxRevoluteJointFlag::eDRIVE_ENABLED, false);
	}
}

EXPORT bool PhysXHingeJoint_GetDrive(PhysXHingeJoint* _this, float& velocity, float& maxForce, bool& freeSpin)
{
	PxRevoluteJoint* joint = _this->GetJoint();
	freeSpin = joint->getRevoluteJointFlags() & PxRevoluteJointFlag::eDRIVE_FREESPIN;
	velocity = joint->getDriveVelocity();
	maxForce = joint->getDriveForceLimit();
	return joint->getRevoluteJointFlags() & PxRevoluteJointFlag::eDRIVE_ENABLED;
}

EXPORT void PhysXHingeJoint_SetProjectionTolerances( PhysXHingeJoint* _this, float linear, float angular )
{
	_this->GetJoint()->setProjectionLinearTolerance( linear );
	_this->GetJoint()->setProjectionAngularTolerance( angular );
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXJoint* PhysXFixedJoint_Create(
	PhysXBody* actor0, const PxVec3& localPosition0, const PxQuat& localRotation0, 
	PhysXBody* actor1, const PxVec3& localPosition1, const PxQuat& localRotation1)
{
	PxTransform localFrame0(localPosition0, localRotation0);
	PxTransform localFrame1(localPosition1, localRotation1);
	return new PhysXFixedJoint(world->mPhysics, actor0->mActor, localFrame0, actor1->mActor, localFrame1);
}

EXPORT void PhysXFixedJoint_SetProjectionTolerances( PhysXFixedJoint* _this, float linear, float angular )
{
	_this->GetJoint()->setProjectionLinearTolerance( linear );
	_this->GetJoint()->setProjectionAngularTolerance( angular );
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXJoint* PhysXSliderJoint_Create(
	PhysXBody* actor0, const PxVec3& localPosition0, const PxQuat& localRotation0, 
	PhysXBody* actor1, const PxVec3& localPosition1, const PxQuat& localRotation1)
{
	PxTransform localFrame0(localPosition0, localRotation0);
	PxTransform localFrame1(localPosition1, localRotation1);
	return new PhysXSliderJoint(world->mPhysics, actor0->mActor, localFrame0, actor1->mActor, localFrame1);
}

EXPORT void PhysXSliderJoint_SetLimit(PhysXSliderJoint* _this, bool enabled, float low, float high, 
	float limitContactDistance, float restitution, float spring, float damping)
{
	_this->GetJoint()->setPrismaticJointFlag(PxPrismaticJointFlag::eLIMIT_ENABLED, enabled);
	if(enabled)
	{
		PxTolerancesScale scale;
		PxJointLinearLimitPair limit(scale, low, high, limitContactDistance);
		SetLimitProperties(limit, restitution, spring, damping);

		if(limit.lower > limit.upper)
			limit.upper = limit.lower;
		if(limit.contactDistance > limit.upper - limit.lower)
			limit.upper = limit.lower + limit.contactDistance * 1.001f;

		_this->GetJoint()->setLimit(limit);
	}
}

EXPORT void PhysXSliderJoint_SetProjectionTolerances( PhysXSliderJoint* _this, float linear, float angular )
{
	_this->GetJoint()->setProjectionLinearTolerance( linear );
	_this->GetJoint()->setProjectionAngularTolerance( angular );
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXJoint* PhysXD6Joint_Create(
	PhysXBody* actor0, const PxVec3& localPosition0, const PxQuat& localRotation0, 
	PhysXBody* actor1, const PxVec3& localPosition1, const PxQuat& localRotation1 )
{
	PxTransform localFrame0(localPosition0, localRotation0);
	PxTransform localFrame1(localPosition1, localRotation1);
	return new PhysXD6Joint(world->mPhysics, actor0->mActor, localFrame0, actor1->mActor, localFrame1);
}

EXPORT void PhysXD6Joint_SetMotion(PhysXD6Joint* _this, PxD6Axis::Enum axis, PxD6Motion::Enum type)
{
	_this->GetJoint()->setMotion( axis, type );
}

EXPORT void PhysXD6Joint_SetLinearLimit(PhysXD6Joint* _this, float value, float limitContactDistance,
	float restitution, float spring, float damping)
{
	PxTolerancesScale scale;
	PxJointLinearLimit limit(scale, value, limitContactDistance);
	SetLimitProperties(limit, restitution, spring, damping);
	_this->GetJoint()->setLinearLimit(limit);
}

EXPORT void PhysXD6Joint_SetTwistLimit(PhysXD6Joint* _this, float low, float high, float limitContactDistance, 
	float restitution, float spring, float damping)
{
	PxJointAngularLimitPair limit(low, high, limitContactDistance);
	SetLimitProperties(limit, restitution, spring, damping);
	if(limit.lower > limit.upper)
		limit.upper = limit.lower;
	if(limit.contactDistance > limit.upper - limit.lower)
		limit.upper = limit.lower + limit.contactDistance * 1.001f;
	_this->GetJoint()->setTwistLimit(limit);
}

EXPORT void PhysXD6Joint_SetSwingLimit(PhysXD6Joint* _this, float yAngle, float zAngle, float limitContactDistance, 
	float restitution, float spring, float damping)
{
	PxJointLimitCone limit(yAngle, zAngle, limitContactDistance);
	SetLimitProperties(limit, restitution, spring, damping);

	const float epsilon = .001f;
	if(limit.yAngle < epsilon)
		limit.yAngle = epsilon;
	if(limit.yAngle >= PI - epsilon)
		limit.yAngle = PI - epsilon;
	if(limit.zAngle < epsilon)
		limit.zAngle = epsilon;
	if(limit.zAngle >= PI - epsilon)
		limit.zAngle = PI - epsilon;

	_this->GetJoint()->setSwingLimit(limit);
}

EXPORT void PhysXD6Joint_SetDrive( PhysXD6Joint* _this, PxD6Drive::Enum index, float spring, float damping, 
	float forceLimit, bool acceleration )
{
	_this->GetJoint()->setDrive(index, PxD6JointDrive(spring, damping, forceLimit, acceleration));
}

EXPORT void PhysXD6Joint_GetDrive( PhysXD6Joint* _this, PxD6Drive::Enum index, float& spring, float& damping, 
	float& forceLimit, bool& acceleration )
{
	PxD6JointDrive data = _this->GetJoint()->getDrive(index);
//!!!!!
spring = 0;
//	spring = data.spring;
	damping = data.damping;
	forceLimit = data.forceLimit;
	acceleration = data.flags & PxD6JointDriveFlag::eACCELERATION;
}

EXPORT void PhysXD6Joint_SetDrivePosition( PhysXD6Joint* _this, const PxVec3& position, const PxQuat& rotation )
{
	_this->GetJoint()->setDrivePosition(PxTransform(position, rotation));
}

EXPORT void PhysXD6Joint_GetDrivePosition( PhysXD6Joint* _this, PxVec3& position, PxQuat& rotation )
{
	PxTransform transform = _this->GetJoint()->getDrivePosition();
	position = transform.p;
	rotation = transform.q;
}

EXPORT void PhysXD6Joint_SetDriveVelocity( PhysXD6Joint* _this, const PxVec3& linear, const PxVec3& angular )
{
	_this->GetJoint()->setDriveVelocity(linear, angular);
}

EXPORT void PhysXD6Joint_GetDriveVelocity( PhysXD6Joint* _this, PxVec3& linear, PxVec3& angular )
{
	_this->GetJoint()->getDriveVelocity(linear, angular);
}

EXPORT void PhysXD6Joint_SetProjectionTolerances( PhysXD6Joint* _this, float linear, float angular )
{
	_this->GetJoint()->setProjectionLinearTolerance( linear );
	_this->GetJoint()->setProjectionAngularTolerance( angular );
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//EXPORT PhysXJoint* PhysXDistanceJoint_Create(
//	PhysXBody* actor0, const PxVec3& localPosition0, const PxQuat& localRotation0, 
//	PhysXBody* actor1, const PxVec3& localPosition1, const PxQuat& localRotation1)
//{
//	PxTransform localFrame0(localPosition0, localRotation0);
//	PxTransform localFrame1(localPosition1, localRotation1);
//	return new PhysXDistanceJoint(world->mPhysics, actor0->mActor, localFrame0, actor1->mActor, localFrame1);
//}
//
//EXPORT void PhysXDistanceJoint_SetMinDistanceEnabled( PhysXDistanceJoint* _this, bool value )
//{
//	_this->GetJoint()->setDistanceJointFlag( PxDistanceJointFlag::eMIN_DISTANCE_ENABLED, value );
//}
//
//EXPORT void PhysXDistanceJoint_SetMinDistance( PhysXDistanceJoint* _this, float value )
//{
//	_this->GetJoint()->setMinDistance( value );
//}
//
//EXPORT void PhysXDistanceJoint_SetMaxDistanceEnabled( PhysXDistanceJoint* _this, bool value )
//{
//	_this->GetJoint()->setDistanceJointFlag( PxDistanceJointFlag::eMAX_DISTANCE_ENABLED, value );
//}
//
//EXPORT void PhysXDistanceJoint_SetMaxDistance( PhysXDistanceJoint* _this, float value )
//{
//	_this->GetJoint()->setMaxDistance( value );
//}
//
//EXPORT void PhysXDistanceJoint_SetSpringEnabled( PhysXDistanceJoint* _this, bool value )
//{
//	_this->GetJoint()->setDistanceJointFlag( PxDistanceJointFlag::eSPRING_ENABLED, value );
//}
//
//EXPORT void PhysXDistanceJoint_SetSpring( PhysXDistanceJoint* _this, float value )
//{
//	_this->GetJoint()->setSpring( value );
//}
//
//EXPORT void PhysXDistanceJoint_SetTolerance( PhysXDistanceJoint* _this, float value )
//{
//	_this->GetJoint()->setTolerance( value );
//}
//
//EXPORT void PhysXDistanceJoint_SetDamping( PhysXDistanceJoint* _this, float value )
//{
//	_this->GetJoint()->setDamping( value );
//}
