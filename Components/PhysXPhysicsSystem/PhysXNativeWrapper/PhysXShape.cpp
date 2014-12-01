// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXMaterial.h"
#include "PhysXShape.h"
#include "PhysXScene.h"
#include "PhysXBody.h"
#include "PhysXWorld.h"
#include "StringUtils.h"

PhysXShape::PhysXShape(PhysXScene* pScene, PhysXBody* pBody, const PxGeometry& geometry, const PxTransform& transform, 
	int materialCount, PhysXMaterial** materials, float mass, const PxU32 contactGroup)
{
	mScene = pScene;
	mBody = pBody;
	mMass = mass;
	mIdentifier = -1;
	mContactGroup = contactGroup;

	this->materials.reserve( materialCount );
	for( int n = 0; n < materialCount; n++ )
		this->materials.push_back( materials[ n ] );

	PxMaterial** pxMaterials = new PxMaterial*[ this->materials.size() ];
	for( int n = 0; n < (int)this->materials.size(); n++ )
		pxMaterials[ n ] = this->materials[ n ]->material;
	mShape = (PxShape*)pBody->mActor->createShape( geometry, pxMaterials, this->materials.size(), transform );
	delete[] pxMaterials;

	mShape->userData = (void*)this;
	SetContactGroup( contactGroup );

	heightField = NULL;
	isWheel = false;
}

PhysXShape::~PhysXShape()
{
	std::set<PhysXShape*>::iterator it;
	for(it = mShapePairs.begin(); it != mShapePairs.end(); it++)
	{
		PhysXShape* pShape = *it;
		pShape->RemoveShapePair(this);
	}
	mShapePairs.clear();

	mShape->release();

	for( int n = 0; n < (int)materials.size(); n++ )
		world->FreeMaterial( materials[ n ] );

	if(heightField)
	{
		heightField->release();
		heightField = NULL;
	}
}
	
void PhysXShape::SetContactGroup(PxU32 contactGroup)
{
	unsigned long long pointer = (unsigned long long)this;

	PxFilterData filterData;
	filterData.word0 = (PxU32)(pointer >> 32);
	filterData.word1 = (PxU32)((pointer << 32) >> 32);
	filterData.word2 = (PxU32)contactGroup;
	mShape->setSimulationFilterData(filterData);

	PxFilterData queryFilterData;
	queryFilterData.word0 = 1 << contactGroup;
	queryFilterData.word1 = (PxU32)(pointer >> 32);
	queryFilterData.word2 = (PxU32)((pointer << 32) >> 32);
	mShape->setQueryFilterData(queryFilterData);
}
	
//int PhysXShape::Raycast(const PxVec3& rayOrigin, const PxVec3& rayDir, PxReal maxDist, 
//	NativeRayCastResult*& pHits, const PxTransform* shapePose)
//{
//	const PxSceneQueryFlags outputFlags = PxSceneQueryFlag::eDISTANCE | PxSceneQueryFlag::eIMPACT |
//		PxSceneQueryFlag::eNORMAL;
//	int count = (int) mShape->raycast(rayOrigin, rayDir, maxDist, outputFlags, mMaxRaycastHits, 
//		mRayCastHits, false, shapePose);
//	if(count > 0)
//	{
//		pHits = (NativeRayCastResult*)malloc(sizeof(NativeRayCastResult) * count);
//		for(int i=0; i<count; i++)
//		{
//			pHits[i].shape = this;
//			pHits[i].worldImpact = mRayCastHits[i].impact;
//			pHits[i].worldNormal = mRayCastHits[i].normal;
//			pHits[i].faceID = mRayCastHits[i].faceIndex;
//			pHits[i].distance = mRayCastHits[i].distance;
//			//pHits[i].u = mRayCastHits[i].u;
//			//pHits[i].v = mRayCastHits[i].v;
//			//pHits[i].materialIndex = 0;
//			//pHits[i].flags = (PxU32)mRayCastHits[i].flags;
//		}
//	}
//	return count;
//}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT void PhysXShape_SetIdentifier(PhysXShape* _this, int value)
{
	_this->mIdentifier = value;
}

EXPORT int PhysXShape_GetIdentifier(PhysXShape* _this)
{
	return _this->mIdentifier;
}

EXPORT void PhysXShape_SetContactGroup(PhysXShape* _this, int contactGroup)
{
	_this->SetContactGroup(contactGroup);
}

EXPORT void PhysXShape_SetMaterialFreeOldMaterial( PhysXShape* _this, int materialIndex, PhysXMaterial* material )
{
	PhysXMaterial* oldMaterial = _this->materials[ materialIndex ];
	_this->materials[ materialIndex ] = material;
	world->FreeMaterial( oldMaterial );
}

EXPORT void PhysXShape_UpdatePhysXShapeMaterials( PhysXShape* _this )
{
	PxMaterial** pxMaterials = new PxMaterial*[ _this->materials.size() ];
	for( int n = 0; n < (int)_this->materials.size(); n++ )
		pxMaterials[ n ] = _this->materials[ n ]->material;
	_this->mShape->setMaterials( pxMaterials, _this->materials.size() );
	delete[] pxMaterials;
}

//EXPORT int PhysXShape_Raycast(PhysXShape* _this, const PxVec3& origin, const PxVec3& unitDir, 
//	const PxReal distance, NativeRayCastResult*& hitBuffer)
//{
//	return _this->Raycast(origin, unitDir, distance, hitBuffer);
//}
