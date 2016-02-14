// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once
#include "PhysXMaterial.h"

class PhysXVehicle;

class PhysXShape
{
public:
	PhysXScene* mScene;
	PhysXBody* mBody;
	PxShape* mShape;
	float mMass;
	int mIdentifier;
	std::vector<PhysXMaterial*> materials;//PhysXMaterial* material;
	PxU32 mContactGroup;
	std::set<PhysXShape*> mShapePairs;
	PxHeightField* heightField;
	bool isWheel;

	PhysXShape(PhysXScene* pScene, PhysXBody* pBody, const PxGeometry& geometry, const PxTransform& transform, 
		int materialCount, PhysXMaterial** materials, float mass, const PxU32 contactGroup);
	~PhysXShape();

	void SetContactGroup(PxU32 contactGroup);

	void AddShapeToPairs(PhysXShape* pShape)
	{
		mShapePairs.insert(pShape);
	}

	void RemoveShapePair(PhysXShape* pShape)
	{
		std::set<PhysXShape*>::iterator it = mShapePairs.find(pShape);
		if(it != mShapePairs.end())
			mShapePairs.erase(it);
	}

	bool IsShapeInPairs(PhysXShape* pShape)
	{
		return mShapePairs.find(pShape) != mShapePairs.end();
	}

	//int Raycast(const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist, NativeRayCastResult*& rayHits,
	//	const PxTransform* shapePose = NULL);
};