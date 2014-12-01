// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXScene.h"
#include "PhysXShape.h"

void* GetPointerByFilterData(const PxFilterData& filterData)
{
	unsigned long long pointerHigh = filterData.word0;
	pointerHigh = pointerHigh << 32;
	unsigned long long pointerLow = filterData.word1;
	return (void*) (pointerHigh | pointerLow);
}

PxFilterFlags MyFilterShader(	
	PxFilterObjectAttributes attributes0, PxFilterData filterData0, 
	PxFilterObjectAttributes attributes1, PxFilterData filterData1,
	PxPairFlags& pairFlags, const void* constantBlock, PxU32 constantBlockSize)
{
    // let triggers through, and do any other prefiltering you need.
    if(PxFilterObjectIsTrigger(attributes0) || PxFilterObjectIsTrigger(attributes1))
    {
		pairFlags = PxPairFlag::eTRIGGER_DEFAULT;
		return PxFilterFlag::eDEFAULT;
    }
	
	PhysXShape* shape0 = (PhysXShape*)GetPointerByFilterData(filterData0);
	PhysXShape* shape1 = (PhysXShape*)GetPointerByFilterData(filterData1);
	PhysXScene* scene = shape0->mScene;

    PxU32 shapeGroup0 = filterData0.word2;
    PxU32 shapeGroup1 = filterData1.word2;

	if((shape0->mScene->GetGroupCollisionFlag(shapeGroup0) & (1 << shapeGroup1)) == 0)
		return PxFilterFlag::eSUPPRESS;
	if(shape1 && shape0->IsShapeInPairs(shape1))
		return PxFilterFlag::eSUPPRESS;

	if(shape0->isWheel && shape1->materials[ 0 ]->vehicleDrivableSurface)
		return PxFilterFlag::eSUPPRESS;
	if(shape1->isWheel && shape0->materials[ 0 ]->vehicleDrivableSurface)
		return PxFilterFlag::eSUPPRESS;
	//if(shape0->mBody->ownerVehicle != NULL && shape1->material->vehicleDrivableSurface)
	//	return PxFilterFlag::eSUPPRESS;
	//if(shape1->mBody->ownerVehicle != NULL && shape0->material->vehicleDrivableSurface)
	//	return PxFilterFlag::eSUPPRESS;

	pairFlags = PxPairFlag::eCONTACT_DEFAULT;
	pairFlags |= PxPairFlag::eNOTIFY_TOUCH_FOUND;
	pairFlags |= PxPairFlag::eNOTIFY_TOUCH_PERSISTS;
	pairFlags |= PxPairFlag::eNOTIFY_CONTACT_POINTS;
	//ccd
	pairFlags |= PxPairFlag::eSWEPT_INTEGRATION_LINEAR;

    return PxFilterFlag::eDEFAULT;
}

PhysXScene::PhysXScene(PxPhysics* pPhysics, int numThreads, PxU32* affinityMasks)
{
	mPhysics = pPhysics;

	PxSceneDesc sceneDesc(mPhysics->getTolerancesScale());
	sceneDesc.simulationEventCallback = this;

	sceneDesc.staticStructure = PxPruningStructure::eDYNAMIC_AABB_TREE;
	sceneDesc.dynamicStructure = PxPruningStructure::eDYNAMIC_AABB_TREE;

	//collision filtering
	sceneDesc.filterShader = MyFilterShader;

	sceneDesc.flags = PxSceneFlag::eENABLE_SWEPT_INTEGRATION;
	//sceneDesc.flags |= PxSceneFlag::eENABLE_SWEPT_INTEGRATION;
	//sceneDesc.flags &= ~PxSceneFlag::eADAPTIVE_FORCE;

	//eENABLE_ONE_DIRECTIONAL_FRICTION
	//eENABLE_TWO_DIRECTIONAL_FRICTION
	//eENABLE_PCM

	sceneDesc.cpuDispatcher = PxDefaultCpuDispatcherCreate(numThreads, affinityMasks);
	mScene = mPhysics->createScene(sceneDesc);

	contactListShrinkCounter = 0;
}

PhysXScene::~PhysXScene()
{
	if(mScene)
		mScene->release();
}

void PhysXScene::onContact(const PxContactPairHeader& pairHeader, const PxContactPair* pairs, PxU32 nbPairs)
{
	const int staticBufferCapacity = 2048;
	byte staticBuffer[staticBufferCapacity];

	for(PxU32 i=0; i < nbPairs; i++)
	{
		const PxContactPair& pair = pairs[i];
		if(pair.events & (PxPairFlag::eNOTIFY_TOUCH_FOUND | PxPairFlag::eNOTIFY_TOUCH_PERSISTS))
		{
			byte* allocatedBuffer = NULL;		
			byte* buffer;

			if(pair.requiredBufferSize > staticBufferCapacity)
			{
				allocatedBuffer = new byte[pair.requiredBufferSize];
				buffer = allocatedBuffer;
			}
			else
				buffer = staticBuffer;

			PxContactPairPoint* contactPointBuffer = (PxContactPairPoint*)buffer;
			pair.extractContacts(contactPointBuffer, pair.requiredBufferSize);

			for(PxU16 nContact = 0; nContact < pair.contactCount; nContact++)
			{
				const PxContactPairPoint& contactPoint = contactPointBuffer[nContact];

				PhysXShape* shape1 = (PhysXShape*)pair.shapes[0]->userData;
				PhysXShape* shape2 = (PhysXShape*)pair.shapes[1]->userData;

				PhysXScene::ContactReport contact;
				contact.shapeIndex1 = shape1->mIdentifier;
				contact.shapeIndex2 = shape2->mIdentifier;
				contact.contactPoint = contactPoint.position;
				contact.normal = contactPoint.normal;
				contact.separation = contactPoint.separation;
				contactList.push_back(contact);
			}

			if(allocatedBuffer)
				delete[] allocatedBuffer;
		}
	}
}

EXPORT void PhysXScene_SetGravity( PhysXScene* _this, const PxVec3& vec )
{
	_this->mScene->setGravity(vec);
}

void PhysXScene::Simulate( float elapsedTime )
{
	//clear and shrink contactList
	if(contactList.size() * 4 < contactList.capacity())
	{
		contactListShrinkCounter++;
		if(contactListShrinkCounter == 100)
		{
			contactList.clear();
			std::vector<ContactReport>().swap(contactList);
			contactListShrinkCounter = 0;
		}
	}
	else
		contactListShrinkCounter = 0;
	contactList.resize(0);

	mScene->simulate(elapsedTime);
}

EXPORT void PhysXScene_Simulate( PhysXScene* _this, float elapsedTime )
{
	_this->Simulate( elapsedTime );
}

EXPORT bool PhysXScene_FetchResults( PhysXScene* _this, bool block )
{
	return _this->mScene->fetchResults(block);
}

EXPORT void PhysXScene_AddBody( PhysXScene* _this, PhysXBody* pBody)
{
	_this->mScene->addActor(*pBody->mActor);
}

EXPORT void PhysXScene_RemoveBody( PhysXScene* _this, PhysXBody* pBody)
{
	_this->mScene->removeActor(*pBody->mActor);
}

struct SortRayCastResults
{
	bool operator()(NativeRayCastResult const& a, NativeRayCastResult const& b) const
	{
		return a.distance < b.distance;
	}
};

EXPORT int PhysXScene_RayCast( PhysXScene* _this, const PxVec3& origin, const PxVec3& unitDir,
	float distance, uint contactGroupMask, bool piercing )
{
	_this->lastRayCastResults.resize(0);

	PxSceneQueryFilterData filterData;
	filterData.data.word0 = contactGroupMask;
	PxSceneQueryFlags outputFlags = PxSceneQueryFlag::eDISTANCE | PxSceneQueryFlag::eIMPACT | PxSceneQueryFlag::eNORMAL;

	if(piercing)
	{
		if(_this->rayCastHitBuffer.size() == 0)
			_this->rayCastHitBuffer.resize(256);

		int hitCount;
		do
		{
			// The return value is the number of hits in the buffer, or -1 if the buffer overflowed.
			bool blockingHit;
			hitCount = _this->mScene->raycastMultiple(origin, unitDir, distance, outputFlags, 
				&_this->rayCastHitBuffer[0], _this->rayCastHitBuffer.size(), blockingHit, filterData);
			if(hitCount != -1)
				break;
			_this->rayCastHitBuffer.resize(_this->rayCastHitBuffer.size() * 2);
		}while(true);

		for( int n = 0; n < hitCount; n++ )
		{
			const PxRaycastHit& hit = _this->rayCastHitBuffer[n];

			bool isTriangleMesh = hit.shape->getGeometryType() == PxGeometryType::eTRIANGLEMESH;
			bool isHeightfield = hit.shape->getGeometryType() == PxGeometryType::eHEIGHTFIELD;

			//TriangleMesh, Heightfield shapes
			if(isTriangleMesh || isHeightfield)
			{
				if(_this->rayCastHitBuffer2.size() == 0)
					_this->rayCastHitBuffer2.resize(256);

				int hitCount2;
				do
				{
					hitCount2 = hit.shape->raycast(origin, unitDir, distance, outputFlags, 
						_this->rayCastHitBuffer2.size(), &_this->rayCastHitBuffer2[0], false);
					if(hitCount2 != -1 && hitCount2 < (int)_this->rayCastHitBuffer2.size())
						break;
					_this->rayCastHitBuffer2.resize(_this->rayCastHitBuffer2.size() * 2);
				}while(true);

				for( int n = 0; n < hitCount2; n++ )
				{
					const PxRaycastHit& hit2 = _this->rayCastHitBuffer2[n];

					NativeRayCastResult result;
					result.shape = (PhysXShape*)hit.shape->userData;
					result.worldImpact = hit2.impact;
					result.worldNormal = hit2.normal;
					result.distance = hit2.distance;

					if(isTriangleMesh)
					{
						PxShape* pxShape = result.shape->mShape;
						const PxTriangleMeshGeometry& geometry = pxShape->getGeometry().triangleMesh();
						const PxU32* remap = geometry.triangleMesh->getTrianglesRemap();
						if(remap != NULL)
							result.faceID = remap[hit2.faceIndex];
						else
							result.faceID = hit2.faceIndex;
					}
					else
						result.faceID = hit2.faceIndex;

					_this->lastRayCastResults.push_back(result);
				}
			}
			else
			{
				NativeRayCastResult result;
				result.shape = (PhysXShape*)hit.shape->userData;
				result.worldImpact = hit.impact;
				result.worldNormal = hit.normal;
				result.distance = hit.distance;
				result.faceID = 0;
				_this->lastRayCastResults.push_back(result);
			}
		}

		//sort by distance
		std::sort(_this->lastRayCastResults.begin(), _this->lastRayCastResults.end(), SortRayCastResults());

		return _this->lastRayCastResults.size();
	}
	else
	{
		PxRaycastHit hit;
		bool found = _this->mScene->raycastSingle(origin, unitDir, distance, outputFlags, hit, filterData);

		if(found && hit.shape)
		{
			NativeRayCastResult result;
			result.shape = (PhysXShape*)hit.shape->userData;
			result.worldImpact = hit.impact;
			result.worldNormal = hit.normal;
			result.distance = hit.distance;

			bool isTriangleMesh = hit.shape->getGeometryType() == PxGeometryType::eTRIANGLEMESH;
			//bool isHeightfield = hit.shape->getGeometryType() == PxGeometryType::eHEIGHTFIELD;

			if(isTriangleMesh)
			{
				PxShape* pxShape = result.shape->mShape;
				const PxTriangleMeshGeometry& geometry = pxShape->getGeometry().triangleMesh();
				const PxU32* remap = geometry.triangleMesh->getTrianglesRemap();
				if(remap != NULL)
					result.faceID = remap[hit.faceIndex];
				else
					result.faceID = hit.faceIndex;
			}
			else
				result.faceID = hit.faceIndex;

			_this->lastRayCastResults.push_back(result);
			return 1;
		}
		else
			return 0;
	}
}

EXPORT NativeRayCastResult* PhysXScene_GetLastRayCastResults( PhysXScene* _this )
{
	return &_this->lastRayCastResults[0];
}

int VolumeCast(PhysXScene* _this, const PxGeometry& geometry, const PxTransform& pose, PxU32 contactGroupMask)
{
	_this->lastVolumeCastShapeIdentifiers.resize(0);

	PxSceneQueryFilterData filterData;
	filterData.data.word0 = contactGroupMask;

	if(_this->volumeCastShapeBuffer.size() == 0)
		_this->volumeCastShapeBuffer.resize(256);

	int hitCount;
	do
	{
		// Overlap against all static & dynamic objects (no filtering)
		// The return value is the number of hits in the buffer, or -1 if the buffer overflowed.
		hitCount = _this->mScene->overlapMultiple(geometry, pose, &_this->volumeCastShapeBuffer[0], 
			_this->volumeCastShapeBuffer.size(), filterData);
		if(hitCount != -1)
			break;
		_this->volumeCastShapeBuffer.resize(_this->volumeCastShapeBuffer.size() * 2);
	}while(true);

	for( int n = 0; n < hitCount; n++ )
	{
		PxShape* pxShape = _this->volumeCastShapeBuffer[n];
		PhysXShape* shape = (PhysXShape*)pxShape->userData;
		_this->lastVolumeCastShapeIdentifiers.push_back(shape->mIdentifier);
	}
	return hitCount;
}

EXPORT int PhysXScene_OverlapOBBShapes(PhysXScene* _this, const PxVec3& origin, const PxVec3& halfExtents, 
	const PxQuat& rotation, uint contactGroupMask)
{
	PxBoxGeometry geometry(halfExtents);
	PxTransform pose(origin, rotation);
	return VolumeCast(_this, geometry, pose, contactGroupMask);
}

EXPORT int PhysXScene_OverlapSphereShapes(PhysXScene* _this, const PxVec3& origin, float radius, 
	uint contactGroupMask)
{
	PxSphereGeometry geometry(radius);
	PxTransform pose(origin, PxQuat::createIdentity());
	return VolumeCast(_this, geometry, pose, contactGroupMask);
}

EXPORT int PhysXScene_OverlapCapsuleShapes(PhysXScene* _this, const PxVec3& origin, const PxQuat& rotation, 
	float radius, float halfHeight, uint contactGroupMask)
{
	PxCapsuleGeometry geometry(radius, halfHeight);
	PxTransform pose(origin, rotation);
	return VolumeCast(_this, geometry, pose, contactGroupMask);
}

EXPORT int* PhysXScene_GetLastVolumeCastShapeIdentifiers( PhysXScene* _this )
{
	return &_this->lastVolumeCastShapeIdentifiers[0];
}

EXPORT void PhysXScene_SetGroupCollisionFlag(PhysXScene* _this, int group1, int group2, bool enable)
{
	if(enable)
	{
		_this->mGroupCollisionFlags[group1] |= (1 << group2);
		_this->mGroupCollisionFlags[group2] |= (1 << group1);
	}
	else
	{
		_this->mGroupCollisionFlags[group1] &= ~(1 << group2);
		_this->mGroupCollisionFlags[group2] &= ~(1 << group1);
	}
}

EXPORT void PhysXScene_SetShapePairFlags(PhysXScene* _this, PhysXShape* shape1, PhysXShape* shape2, bool disableContacts)
{
	if(disableContacts)
	{
		shape1->AddShapeToPairs(shape2);
		shape2->AddShapeToPairs(shape1);
	}
	else
	{
		shape1->RemoveShapePair(shape2);
		shape2->RemoveShapePair(shape1);
	}
}

EXPORT void PhysXScene_GetContactReportList(PhysXScene* _this, PhysXScene::ContactReport*& pContactList, int& contactCount)
{
	const std::vector<PhysXScene::ContactReport>& contactList = _this->contactList;
	contactCount = contactList.size();
	if(contactCount > 0)
		pContactList = (PhysXScene::ContactReport*)&contactList[0];
	else
		pContactList = NULL;
}

EXPORT void PhysXScene_SetEnableDebugVisualization(PhysXScene* _this, bool enable)
{
	if(enable)
	{
		_this->mScene->setVisualizationParameter(PxVisualizationParameter::eSCALE, 1.0f);
		_this->mScene->setVisualizationParameter(PxVisualizationParameter::eJOINT_LOCAL_FRAMES, .1f);
		_this->mScene->setVisualizationParameter(PxVisualizationParameter::eJOINT_LIMITS, .15f);

		//_this->mScene->setVisualizationParameter(PxVisualizationParameter::eCOLLISION_SHAPES, 1.0f);
	}
	else
		_this->mScene->setVisualizationParameter(PxVisualizationParameter::eSCALE, 0);
}

EXPORT void PhysXScene_GetDebugVisualizationData(PhysXScene* _this, int& lineCount, Line* lines)
{
	const PxRenderBuffer& renderBuffer = _this->mScene->getRenderBuffer();
	lineCount = renderBuffer.getNbLines();
	if(lines)
	{
		Line* dest = lines;
		const PxDebugLine* source = renderBuffer.getLines();
		for(int n = 0; n < lineCount; n++)
		{
			dest->start = source->pos0;
			dest->end = source->pos1;
			dest++;
			source++;
		}
	}
}
