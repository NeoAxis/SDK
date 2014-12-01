// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXVehicleSceneQuery.h"
#include "PhysXShape.h"
#include "PhysXBody.h"

void* GetPointerByQueryFilterData(const PxFilterData& filterData)
{
	unsigned long long pointerHigh = filterData.word1;
	pointerHigh = pointerHigh << 32;
	unsigned long long pointerLow = filterData.word2;
	return (void*) (pointerHigh | pointerLow);
}

static PxSceneQueryHitType::Enum PhysXVehicleWheelRaycastPreFilter(	PxFilterData filterData0, PxFilterData filterData1,
	const void* constantBlock, PxU32 constantBlockSize, PxSceneQueryFilterFlags& filterFlags)
{
	PhysXShape* shape = (PhysXShape*)GetPointerByQueryFilterData(filterData1);
	return shape->materials[ 0 ]->vehicleDrivableSurface ? PxSceneQueryHitType::eBLOCK : PxSceneQueryHitType::eNONE;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

PhysXVehicleSceneQuery::PhysXVehicleSceneQuery(PxScene* scene, int wheelCount)
{
	this->wheelCount = wheelCount;

	raycastQueryResults = new PxRaycastQueryResult[wheelCount];
	raycastHits = new PxRaycastHit[wheelCount];

	PxBatchQueryDesc desc;
	desc.userRaycastResultBuffer = raycastQueryResults;
	desc.userRaycastHitBuffer = raycastHits;
	desc.raycastHitBufferSize = wheelCount;
	desc.preFilterShader = PhysXVehicleWheelRaycastPreFilter;
	if(!desc.isValid())
		Fatal("PhysXVehicleSceneQuery: Constructor: !desc.isValid().");
	batchQuery = scene->createBatchQuery(desc);
}

PhysXVehicleSceneQuery::~PhysXVehicleSceneQuery()
{
	batchQuery->release();
	delete[] raycastQueryResults;
	delete[] raycastHits;
}
