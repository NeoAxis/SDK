// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXBody.h"
#include "PhysXScene.h"
#include "StringUtils.h"

PhysXBody::PhysXBody(PhysXScene* pScene, bool isStatic, const PxVec3& globalPosition, const PxQuat& globalRotation)
{
	mScene = pScene;
	mIsStatic = isStatic;

	PxTransform globalPose(globalPosition, globalRotation);
	if(isStatic)
		mActor = mScene->mPhysics->createRigidStatic(globalPose);
	else
		mActor = mScene->mPhysics->createRigidDynamic(globalPose);

	ownerVehicle = NULL;
}
	
PhysXBody::~PhysXBody()
{
	for(int n = 0; n < (int)mShapes.size(); n++)
		delete mShapes[n];
	mShapes.clear();

	mActor->release();
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXShape* PhysXBody_CreateBoxShape(PhysXBody* _this, const PxVec3& position, const PxQuat& rotation, 
	const PxVec3& halfDimension, int materialCount, PhysXMaterial** materials, float mass, int contactGroup)
{
	PxBoxGeometry box = PxBoxGeometry(halfDimension);
	PhysXShape* shape = new PhysXShape(_this->mScene, _this, box, PxTransform(position, rotation), 
		materialCount, materials, mass, contactGroup);
	_this->mShapes.push_back(shape);
	return shape;
}

EXPORT PhysXShape* PhysXBody_CreateSphereShape(PhysXBody* _this, const PxVec3& position, const PxQuat& rotation, float radius, 
	int materialCount, PhysXMaterial** materials, float mass, int contactGroup)
{
	PxSphereGeometry sphere = PxSphereGeometry(radius);
	PhysXShape* shape = new PhysXShape(_this->mScene, _this, sphere, PxTransform(position, rotation), 
		materialCount, materials, mass, contactGroup);
	_this->mShapes.push_back(shape);
	return shape;
}

EXPORT PhysXShape* PhysXBody_CreateCapsuleShape(PhysXBody* _this, const PxVec3& position, const PxQuat& rotation, float radius, 
	float halfHeight, int materialCount, PhysXMaterial** materials, float mass, int contactGroup)
{
	PxCapsuleGeometry capsule = PxCapsuleGeometry(radius, halfHeight);
	PhysXShape* shape = new PhysXShape(_this->mScene, _this, capsule, PxTransform(position, rotation), 
		materialCount, materials, mass, contactGroup);
	_this->mShapes.push_back(shape);
	return shape;
}

EXPORT PhysXShape* PhysXBody_CreateConvexMeshShape(PhysXBody* _this, const PxVec3& position, const PxQuat& rotation, 
	PxConvexMesh* pConvexMesh, int materialCount, PhysXMaterial** materials, float mass, int contactGroup)
{
	PxConvexMeshGeometry convexMeshGeometry = PxConvexMeshGeometry(pConvexMesh);
	PhysXShape* shape = new PhysXShape(_this->mScene, _this, convexMeshGeometry, PxTransform(position, rotation), 
		materialCount, materials, mass, contactGroup);
	_this->mShapes.push_back(shape);
	return shape;
}

EXPORT PhysXShape* PhysXBody_CreateTriangleMeshShape(PhysXBody* _this, const PxVec3& position, const PxQuat& rotation, 
	PxTriangleMesh* pTriangleMesh, int materialCount, PhysXMaterial** materials, float mass, int contactGroup)
{
	PxTriangleMeshGeometry triangleMeshGeometry = PxTriangleMeshGeometry(pTriangleMesh);
	PhysXShape* shape = new PhysXShape(_this->mScene, _this, triangleMeshGeometry, PxTransform(position, rotation), 
		materialCount, materials, mass, contactGroup);
	_this->mShapes.push_back(shape);
	return shape;
}

EXPORT PhysXShape* PhysXBody_CreateHeightFieldShape(PhysXBody* _this, const PxVec3& position, const PxQuat& rotation, 
	int sampleCountX, int sampleCountY, HeightFieldShape_Sample* samples, const PxVec3& samplesScale, float thickness, 
	int materialCount, PhysXMaterial** materials, int contactGroup)
{
	if(sizeof(HeightFieldShape_Sample) != 4)
		Fatal("PhysXBody_CreateHeightFieldShape: sizeof(HeightFieldShape_Sample) != 4.");

	PxHeightFieldSample* samples2 = new PxHeightFieldSample[sampleCountX * sampleCountY];
	memset(samples2, 0, sizeof(PxHeightFieldSample) * sampleCountX * sampleCountY);

	for(int y = 0; y < sampleCountY; y++)
	{
		for(int x = 0; x < sampleCountX; x++)
		{
			HeightFieldShape_Sample* src = samples + (x + y * sampleCountX);
			PxHeightFieldSample* dest = samples2 + (x + y * sampleCountX);

			dest->height = (PxI16)((int)src->height / 2);
			dest->materialIndex0 = src->materialIndex0;
			dest->materialIndex1 = src->materialIndex1;
			dest->setTessFlag();
		}
	}

	PxHeightFieldDesc desc;
	desc.format = PxHeightFieldFormat::eS16_TM;
	desc.nbColumns = sampleCountX;
	desc.nbRows = sampleCountY;
	desc.samples.data = samples2;
	desc.samples.stride = sizeof(PxHeightFieldSample);
	desc.thickness = thickness;
	//desc.convexEdgeThreshold = .001f;
	//desc.convexEdgeThreshold = 0;
	//desc.flags = 0;

	PxHeightField* heightField = _this->mScene->mPhysics->createHeightField(desc);

	PxHeightFieldGeometry geometry;
	geometry.heightField = heightField;
	geometry.heightScale = samplesScale.z / 32767.0f;
	geometry.rowScale = samplesScale.x;
	geometry.columnScale = samplesScale.y;

	PhysXShape* shape = new PhysXShape(_this->mScene, _this, geometry, PxTransform(position, rotation), 
		materialCount, materials, 0, contactGroup);
	shape->heightField = heightField;
	_this->mShapes.push_back(shape);

	delete[] samples2;

	return shape;
}

EXPORT bool PhysXBody_IsSleeping(PhysXBody* _this)
{
	return ((PxRigidDynamic*)_this->mActor)->isSleeping();
}

EXPORT void PhysXBody_GetGlobalPoseLinearAngularVelocities(PhysXBody* _this, PxVec3& globalPosition, PxQuat& globalRotation, 
	PxVec3& linearVelocity, PxVec3& angularVelocity)
{
	PxRigidDynamic* rigidDynamic = (PxRigidDynamic*)_this->mActor;

	PxTransform globalPose = rigidDynamic->getGlobalPose();
	globalPosition = globalPose.p;
	globalRotation = globalPose.q;
	linearVelocity = rigidDynamic->getLinearVelocity();
	angularVelocity = rigidDynamic->getAngularVelocity();
}

EXPORT void PhysXBody_SetGlobalPose(PhysXBody* _this, const PxVec3& globalPosition, const PxQuat& globalRotation)
{
	PxTransform globalPose(globalPosition, globalRotation);
	_this->mActor->setGlobalPose(globalPose);
}

EXPORT void PhysXBody_SetLinearVelocity(PhysXBody* _this, const PxVec3& linearVelocity)
{
	((PxRigidDynamic*)_this->mActor)->setLinearVelocity(linearVelocity);
}
	
EXPORT void PhysXBody_SetAngularVelocity(PhysXBody* _this, const PxVec3& angularVelocity)
{
	((PxRigidDynamic*)_this->mActor)->setAngularVelocity(angularVelocity);
}

EXPORT void PhysXBody_PutToSleep(PhysXBody* _this)
{
	((PxRigidDynamic*)_this->mActor)->putToSleep();
}

EXPORT void PhysXBody_SetMassAndInertia( PhysXBody* _this, bool autoCenterOfMass, const PxVec3& manualMassLocalPosition, 
	const PxQuat& manualMassLocalRotation, const PxVec3& inertiaTensorFactor )
//EXPORT void PhysXBody_SetMassAndInertia( PhysXBody* _this, bool autoCenterOfMass )
{
	PxRigidDynamic* rigidDynamic = (PxRigidDynamic*)_this->mActor;

	float* masses = new float[_this->mShapes.size()];
	for(size_t i=0; i<_this->mShapes.size(); i++)
		masses[i] = _this->mShapes[i]->mMass;

	PxRigidBodyExt::setMassAndUpdateInertia(*rigidDynamic, masses, _this->mShapes.size(), NULL);
	if(!autoCenterOfMass)
	{
		//TO DO: а при ручном rotation будет Identity?
		rigidDynamic->setCMassLocalPose(PxTransform(manualMassLocalPosition, rigidDynamic->getCMassLocalPose().q));

		//set center of mass to (0,0,0).
		//rigidDynamic->setCMassLocalPose(PxTransform(PxVec3(0), rigidDynamic->getCMassLocalPose().q));
	}

	if( inertiaTensorFactor != PxVec3( 1, 1, 1 ) )
		rigidDynamic->setMassSpaceInertiaTensor( rigidDynamic->getMassSpaceInertiaTensor().multiply( inertiaTensorFactor ) );

	delete[] masses;
}

EXPORT int PhysXBody_GetShapeCount(PhysXBody* _this)
{
	return _this->mShapes.size();
}

EXPORT PhysXShape* PhysXBody_GetShape(PhysXBody* _this, int index)
{
	return _this->mShapes[index];
}

EXPORT void PhysXBody_WakeUp(PhysXBody* _this, float wakeCounterValue)
{
	((PxRigidDynamic*)_this->mActor)->wakeUp(wakeCounterValue);
}

EXPORT void PhysXBody_AddLocalForce(PhysXBody* _this, const PxVec3& force)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	PxVec3 globalForce = body->getGlobalPose().transform(force);
	body->addForce(globalForce, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddForce(PhysXBody* _this, const PxVec3& globalForce)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	body->addForce(globalForce, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddLocalTorque(PhysXBody* _this, const PxVec3& torque)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	PxVec3 globalTorque = body->getGlobalPose().transform(torque);
	body->addTorque(torque, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddTorque(PhysXBody* _this, const PxVec3& globalTorque)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	body->addTorque(globalTorque, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddLocalForceAtLocalPos(PhysXBody* _this, const PxVec3& force, const PxVec3& pos)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	PxRigidBodyExt::addLocalForceAtLocalPos(*body, force, pos, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddLocalForceAtPos(PhysXBody* _this, const PxVec3& force, const PxVec3& pos)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	PxRigidBodyExt::addLocalForceAtPos(*body, force, pos, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddForceAtLocalPos(PhysXBody* _this, const PxVec3& force, const PxVec3& pos)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	PxRigidBodyExt::addForceAtLocalPos(*body, force, pos, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_AddForceAtPos(PhysXBody* _this, const PxVec3& force, const PxVec3& pos)
{
	PxRigidDynamic* body = (PxRigidDynamic*)_this->mActor;
	PxRigidBodyExt::addForceAtPos(*body, force, pos, PxForceMode::eFORCE, true);
}

EXPORT void PhysXBody_SetLinearDamping(PhysXBody* _this, float damping)
{
	if(!_this->mIsStatic)
		((PxRigidDynamic*)_this->mActor)->setLinearDamping(damping);
}

EXPORT void PhysXBody_SetAngularDamping(PhysXBody* _this, float damping)
{
	if(!_this->mIsStatic)
		((PxRigidDynamic*)_this->mActor)->setAngularDamping(damping);
}

EXPORT void PhysXBody_SetSleepThreshold(PhysXBody* _this, float threshold)
{
	if(!_this->mIsStatic)
		((PxRigidDynamic*)_this->mActor)->setSleepThreshold(threshold);
}

EXPORT void PhysXBody_SetGravity(PhysXBody* _this, bool enabled)
{
	_this->mActor->setActorFlag(PxActorFlag::eDISABLE_GRAVITY, !enabled);
}

EXPORT void PhysXBody_SetMaxAngularVelocity(PhysXBody* _this, float value)
{
	((PxRigidDynamic*)_this->mActor)->setMaxAngularVelocity(value);
}

EXPORT void PhysXBody_EnableCCD(PhysXBody* _this, bool enable)
{
	for(int n = 0; n < (int)_this->mShapes.size(); n++)
		_this->mShapes[n]->mShape->setFlag(PxShapeFlag::eUSE_SWEPT_BOUNDS, enable);
}

EXPORT void PhysXBody_SetSolverIterationCounts(PhysXBody* _this, int positionIterations, int velocityIterations)
{
	((PxRigidDynamic*)_this->mActor)->setSolverIterationCounts(positionIterations, velocityIterations);
}

//EXPORT void PhysXBody_GetInertiaTensor(PhysXBody* _this, PxMat33& value)
//{
//	value = _this->inertiaTensor;
//}

EXPORT void PhysXBody_SetKinematicFlag( PhysXBody* _this, bool value )
{
	((PxRigidDynamic*)_this->mActor)->setRigidDynamicFlag( PxRigidDynamicFlag::eKINEMATIC, value );
}

EXPORT void PhysXBody_SetKinematicTarget( PhysXBody* _this, const PxVec3& pos, const PxQuat& rot )
{
	((PxRigidDynamic*)_this->mActor)->setKinematicTarget( PxTransform( pos, rot ) );
}
