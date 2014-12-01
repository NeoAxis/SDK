// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

class PhysXJoint
{
public:
	PxPhysics* mPhysics;
	PxJoint* mJoint;
	PxRigidActor* mActor0;
	PxRigidActor* mActor1;

	PhysXJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, PxRigidActor* pActor1);
	~PhysXJoint();
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXHingeJoint : public PhysXJoint
{
public:
	PhysXHingeJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
		PxRigidActor* pActor1, const PxTransform& localFrame1);

	PxRevoluteJoint* GetJoint() { return (PxRevoluteJoint*)mJoint; }
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXFixedJoint : public PhysXJoint
{
public:
	PhysXFixedJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
		PxRigidActor* pActor1, const PxTransform& localFrame1);

	PxFixedJoint* GetJoint() { return (PxFixedJoint*)mJoint; }
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXSliderJoint : public PhysXJoint
{
public:
	PhysXSliderJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, 
		PxRigidActor* pActor1, const PxTransform& localFrame1);

	PxPrismaticJoint* GetJoint() { return (PxPrismaticJoint*)mJoint; }
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXD6Joint : public PhysXJoint
{
public:
	PhysXD6Joint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, PxRigidActor* pActor1, 
		const PxTransform& localFrame1);

	PxD6Joint* GetJoint() { return (PxD6Joint*)mJoint; }
};


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXDistanceJoint : public PhysXJoint
{
public:
	PhysXDistanceJoint(PxPhysics* pPhysics, PxRigidActor* pActor0, const PxTransform& localFrame0, PxRigidActor* pActor1, 
		const PxTransform& localFrame1);

	PxDistanceJoint* GetJoint() { return (PxDistanceJoint*)mJoint; }
};
