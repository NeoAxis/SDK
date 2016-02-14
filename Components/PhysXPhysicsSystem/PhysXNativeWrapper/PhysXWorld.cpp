// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXWorld.h"
#include "PhysXMaterial.h"
#include "PhysXMaterial.h"
#include "PhysXScene.h"
#include "StringUtils.h"

PxDefaultAllocator gDefaultAllocatorCallback;

PhysXWorld::PhysXWorld()
{
	mProfileZoneManager = NULL;
	mFoundation = NULL;
	mPhysics = NULL;
	mCooking = NULL;
	vehicleDrivableMaterialsVersionCounter = 1;
}

WString GetMaterialKey(float staticFriction, float dynamicFriction, float restitution, const WString& materialName, 
	bool vehicleDrivableSurface)
{
	std::wostringstream ss;
	ss << staticFriction << L" " << dynamicFriction << L" " << restitution << L" " << materialName << L" " << 
		(vehicleDrivableSurface ? L"T" : L"F");
	return ss.str();
}

PhysXMaterial* PhysXWorld::AllocMaterial( float staticFriction, float dynamicFriction, float restitution, 
	const WString& materialName, bool vehicleDrivableSurface )
{
	WString key = GetMaterialKey( staticFriction, dynamicFriction, restitution, materialName, vehicleDrivableSurface );

	//LogMessage(ConvertStringToUTF8((WString(L"Alloc: ") + key)).c_str());

	PhysXMaterial* material;
	MaterialMap::iterator it = materials.find(key);
	if(it == materials.end())
	{
		material = new PhysXMaterial(mPhysics, staticFriction, dynamicFriction, restitution, materialName, 
			vehicleDrivableSurface, key);
		materials[key] = material;

		if(material->vehicleDrivableSurface)
		{
			vehicleDrivableMaterials.insert(material);
			IncrementVehicleDrivableMaterialsVersion();
		}
	}
	else
		material = it->second;

	material->referenceCounter++;

	return material;
}

void PhysXWorld::FreeMaterial( PhysXMaterial* material )
{
	material->referenceCounter--;
	if(material->referenceCounter < 0)
		Fatal( "PhysXWorld: FreeMaterial: material->referenceCounter < 0.");
	if(material->referenceCounter == 0)
	{
		//LogMessage(ConvertStringToUTF8(WString(L"Free: ") + material->materialsMapKey).c_str());

		MaterialMap::iterator it = materials.find(material->materialsMapKey);
		if(it == materials.end())
			Fatal("PhysXWorld: FreeMaterial: it == materials.end().");
		else
			materials.erase(it);

		if(material->vehicleDrivableSurface)
		{
			vehicleDrivableMaterials.erase(material);
			IncrementVehicleDrivableMaterialsVersion();
		}

		delete material;
	}
}

void PhysXWorld::IncrementVehicleDrivableMaterialsVersion()
{
	vehicleDrivableMaterialsVersionCounter++;
	if(vehicleDrivableMaterialsVersionCounter >= 2100000000)
		vehicleDrivableMaterialsVersionCounter = 1;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT void* PhysXWorld_Alloc( int size )
{
	return new byte[size];
}

EXPORT void PhysXWorld_Free(void* pointer)
{
	delete[] ((byte*)pointer);
}

EXPORT bool PhysXWorld_Init(ReportErrorDelegate* reportErrorDelegate, wchar16** outErrorString, 
	LogDelegate* logDelegate, float cookingParamsSkinWidth)
{
	*outErrorString = NULL;

	::logDelegate = logDelegate;

	if(world)
		Fatal("PhysXWorld_Init: PhysX world is already initialized.");
	world = new PhysXWorld();

	world->mErrorCallback.reportErrorDelegate = reportErrorDelegate;

	world->mFoundation = PxCreateFoundation(PX_PHYSICS_VERSION, world->mAllocator, world->mErrorCallback);
	//PxAllocatorCallback* allocator = &gDefaultAllocatorCallback;
	//world->mFoundation = PxCreateFoundation(PX_PHYSICS_VERSION, *allocator, world->mErrorCallback);
	if(!world->mFoundation)
	{
		*outErrorString = CreateOutString("PxCreateFoundation failed.");
		return false;
	}

	world->mProfileZoneManager = &PxProfileZoneManager::createProfileZoneManager(world->mFoundation);
	if(!world->mProfileZoneManager)
	{
		*outErrorString = CreateOutString("PxProfileZoneManager::createProfileZoneManager failed.");
		return false;
	}

	PxTolerancesScale scale;

	world->mPhysics = PxCreatePhysics(PX_PHYSICS_VERSION, *world->mFoundation, scale, false, world->mProfileZoneManager);
	if(!world->mPhysics)
	{
		*outErrorString = CreateOutString("PxCreatePhysics failed.");
		return false;
	}

	if(!PxInitExtensions(*world->mPhysics))
	{
		*outErrorString = CreateOutString("PxInitExtensions failed.");
		return false;
	}

	PxCookingParams cookingParams( scale );
	cookingParams.skinWidth = cookingParamsSkinWidth;//0.025f;

	world->mCooking = PxCreateCooking(PX_PHYSICS_VERSION, *world->mFoundation, cookingParams);
	if(!world->mCooking)
	{
		*outErrorString = CreateOutString("PxCreateCooking failed.");
		return false;
	}

	PxInitVehicleSDK(*world->mPhysics);
	PxVehicleSetBasisVectors(PxVec3(0,0,1), PxVec3(1,0,0));

	return true;
}

EXPORT void PhysXWorld_Destroy()
{
	if(world)
	{
		PxCloseVehicleSDK();
		PxCloseExtensions();
		if(world->mCooking)
			world->mCooking->release();
		if(world->mPhysics)
			world->mPhysics->release();
		if(world->mProfileZoneManager)
			world->mProfileZoneManager->release();
		if(world->mFoundation)
			world->mFoundation->release();
		delete world;
		world = NULL;
	}
}

EXPORT void PhysXWorld_GetSDKVersion(int* major, int* minor, int* bugfix)
{
	*major = PX_PHYSICS_VERSION_MAJOR;
	*minor = PX_PHYSICS_VERSION_MINOR;
	*bugfix = PX_PHYSICS_VERSION_BUGFIX;
}

EXPORT PhysXScene* PhysXWorld_CreateScene(int numThreads, PxU32* affinityMasks)
{
	return new PhysXScene(world->mPhysics, numThreads, affinityMasks);
}

EXPORT void PhysXWorld_DestroyScene(PhysXScene* pScene)
{
	delete pScene;
}

EXPORT PhysXBody* PhysXWorld_CreateBody(PhysXScene* pScene, bool isStatic, const PxVec3& globalPosition, 
	const PxQuat& globalRotation)
{
	return new PhysXBody(pScene, isStatic, globalPosition, globalRotation);
}

EXPORT void PhysXWorld_DestroyBody(PhysXBody* pBody)
{
	delete pBody;
}

EXPORT PxConvexMesh* PhysXWorld_CreateConvexMesh(PxVec3* vertices, int vertexCount, void* indices, 
	int indexCount, bool use16BitIndices)
{
	int indexSize = use16BitIndices ? 2 : 4;

	PxConvexMeshDesc convexDesc;
	convexDesc.points.count = vertexCount;
	convexDesc.points.stride = sizeof(PxVec3);
	convexDesc.points.data = vertices;
	convexDesc.triangles.count = indexCount / 3;
	convexDesc.triangles.stride = indexSize * 3;
	convexDesc.triangles.data = indices;
	if(use16BitIndices)
		convexDesc.flags = PxConvexFlag::e16_BIT_INDICES;

	MyMemoryOutputStream buf;
	if(!world->mCooking->cookConvexMesh(convexDesc, buf))
		return NULL;
	MyMemoryInputData input(buf.getData(), buf.getSize());

	return world->mPhysics->createConvexMesh(input);
}

EXPORT void PhysXWorld_ReleaseConvexMesh(PxConvexMesh* mesh)
{
	mesh->release();
}

EXPORT bool PhysXWorld_CookTriangleMesh(PxVec3* vertices, int vertexCount, void* indices, 
	int indexCount, bool use16BitIndices, short* materialIndices, uint8** pData, int* pSize )
{
	int indexSize = use16BitIndices ? 2 : 4;

	PxTriangleMeshDesc triangleDesc;
	triangleDesc.points.count = vertexCount;
	triangleDesc.points.stride = sizeof(PxVec3);
	triangleDesc.points.data = vertices;
	triangleDesc.triangles.count = indexCount / 3;
	triangleDesc.triangles.stride = indexSize * 3;
	triangleDesc.triangles.data = indices;
	if(use16BitIndices)
		triangleDesc.flags = PxMeshFlag::e16_BIT_INDICES;
	triangleDesc.materialIndices.stride = 2;
	triangleDesc.materialIndices.data = (unsigned short*)materialIndices;

	MyMemoryOutputStream buffer(32768);
	if(!world->mCooking->cookTriangleMesh(triangleDesc, buffer))
		return false;

	int size = buffer.getSize();
	*pData = (uint8*)PhysXWorld_Alloc(size);
	memcpy(*pData, buffer.getData(), size);
	*pSize = size;
	return true;
}

EXPORT PxTriangleMesh* PhysXWorld_CreateTriangleMesh(uint8* data, int size)
{
	MyMemoryInputData input(data, size);
	return world->mPhysics->createTriangleMesh(input);
}

EXPORT void PhysXWorld_ReleaseTriangleMesh(PxTriangleMesh* mesh)
{
	mesh->release();
}
