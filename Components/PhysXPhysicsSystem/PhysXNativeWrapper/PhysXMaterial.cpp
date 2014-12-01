// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXMaterial.h"
#include "PhysXWorld.h"
#include "StringUtils.h"

PhysXMaterial::PhysXMaterial(PxPhysics* physics, float staticFriction, float dynamicFriction, float restitution, 
	const WString& materialName, bool vehicleDrivableSurface, const WString& materialsMapKey)
{
	this->staticFriction = staticFriction;
	this->dynamicFriction = dynamicFriction;
	this->restitution = restitution;
	this->materialName = materialName;
	this->vehicleDrivableSurface = vehicleDrivableSurface;
	this->materialsMapKey = materialsMapKey;
	referenceCounter = 0;

	const float frictionCoef = 4.7f;
	material = physics->createMaterial(staticFriction * frictionCoef, dynamicFriction * frictionCoef, restitution);
	material->setFrictionCombineMode(PxCombineMode::eMULTIPLY);
	material->setRestitutionCombineMode(PxCombineMode::eMULTIPLY);
}

PhysXMaterial::~PhysXMaterial()
{
	material->release();
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXMaterial* PhysXMaterial_Create( float staticFriction, float dynamicFriction, float restitution, 
	wchar16* materialName, bool vehicleDrivableSurface )
{
	return world->AllocMaterial( staticFriction, dynamicFriction, restitution, TO_WCHAR_T(materialName), vehicleDrivableSurface );
}
