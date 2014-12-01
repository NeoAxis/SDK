// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once
#include "PhysXMaterial.h"
#include "PhysXBody.h"

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct NativeRayCastResult
{
	PhysXShape* shape;//int shapeIdentifier;//PhysXShape* shape;
	PxVec3 worldImpact;
	PxVec3 worldNormal;
	int faceID;
	float distance;
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct Line
{
	PxVec3 start;
	PxVec3 end;
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXScene : public PxSimulationEventCallback
{
public:

	enum ShapePairFlags
	{
		DisableContacts = 1
	};

	struct ContactReport
	{
		int shapeIndex1;
		int shapeIndex2;
		PxVec3 contactPoint;
		PxVec3 normal;
		float separation;
	};

	// Implements PxSimulationEventCallback
	void onContact(const PxContactPairHeader& pairHeader, const PxContactPair* pairs, PxU32 nbPairs);
	void onTrigger(PxTriggerPair* pairs, PxU32 count) {}
	void onConstraintBreak(PxConstraintInfo*, PxU32) {}
	void onWake(PxActor** , PxU32 ) {}
	void onSleep(PxActor** , PxU32 ) {}

	PhysXScene(PxPhysics* pPhysics, int numThreads, PxU32* affinityMasks);
	~PhysXScene();

	PxU32 GetGroupCollisionFlag(int contactGroup)
	{
		return mGroupCollisionFlags[contactGroup];
	}

	void Simulate( float elapsedTime );

	//

	PxPhysics* mPhysics;
	PxScene* mScene;
	PxU32 mGroupCollisionFlags[32];

	std::vector<ContactReport> contactList;
	int contactListShrinkCounter;

	//shrink some time? but then we can have more than one physx calls.
	std::vector<PxRaycastHit> rayCastHitBuffer;
	std::vector<PxRaycastHit> rayCastHitBuffer2;
	std::vector<NativeRayCastResult> lastRayCastResults;

	//shrink some time? but then we can have more than one physx calls.
	std::vector<PxShape*> volumeCastShapeBuffer;
	std::vector<int> lastVolumeCastShapeIdentifiers;
};
