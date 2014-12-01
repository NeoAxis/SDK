// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

//Data structure for quick setup of scene queries for suspension raycasts.
class PhysXVehicleSceneQuery
{
public:
	PhysXVehicleSceneQuery(PxScene* scene, int wheelCount);
	~PhysXVehicleSceneQuery();

	int wheelCount;
	//One result for each wheel.
	PxRaycastQueryResult* raycastQueryResults;
	//One hit for each wheel.
	PxRaycastHit* raycastHits;

	PxBatchQuery* batchQuery;
};
