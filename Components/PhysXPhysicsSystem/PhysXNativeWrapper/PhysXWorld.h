// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXWorld
{
public:
	//MyAllocator mAllocator;
	PxProfileZoneManager* mProfileZoneManager;
	PxFoundation* mFoundation;
	MyErrorCallback mErrorCallback;
	PxPhysics* mPhysics;
	PxCooking* mCooking;

	typedef std::map<WString, PhysXMaterial*> MaterialMap;
	MaterialMap materials;
	std::set<PhysXMaterial*> vehicleDrivableMaterials;
	int vehicleDrivableMaterialsVersionCounter;

	PhysXWorld();
	PhysXMaterial* AllocMaterial( float staticFriction, float dynamicFriction, float restitution, const WString& materialName,
		bool vehicleDrivableSurface);
	void FreeMaterial( PhysXMaterial* material );
	void IncrementVehicleDrivableMaterialsVersion();
};
