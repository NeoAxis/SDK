// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

class PhysXMaterial
{
public:
	float staticFriction;
	float dynamicFriction;
	float restitution;
	WString materialName;
	bool vehicleDrivableSurface;
	WString materialsMapKey;

	PxMaterial* material;
	int referenceCounter;

	PhysXMaterial(PxPhysics* physics, float staticFriction, float dynamicFriction, float restitution, 
		const WString& materialName, bool vehicleDrivableSurface, const WString& materialsMapKey);
	~PhysXMaterial();
};
