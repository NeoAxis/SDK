// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
//#include "precompiled.h"
#include "hacd.h"

extern void Fatal(const char* text);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

class HACDInstance
{
public:
	HACD::HeapManager* heapManager;

	HACD::HACD* currentHACD;
	HACD::Vec3<HACD::Real>* points;
	HACD::Vec3<long>* triangles;

	HACDInstance()
	{
		heapManager = NULL;
		currentHACD = NULL;
		points = NULL;
		triangles = NULL;
	}
};

EXPORT void HACD_ClearComputed(HACDInstance* instance);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT HACDInstance* HACD_Init()
{
	HACDInstance* instance = new HACDInstance();
	instance->heapManager = HACD::createHeapManager();
	return instance;
}

EXPORT bool HACD_Compute(HACDInstance* instance, double* points, int pointCount, int* triangles, 
	int triangleCount, int maxTrianglesInDecimatedMesh, int maxVerticesPerConvexHull)
{
	srand(0);

	if(instance->currentHACD)
		Fatal("HACD_Compute: instance->currentHACD != NULL.");

	HACD::HACD* myHACD = HACD::CreateHACD(instance->heapManager);
	instance->currentHACD = myHACD;

	instance->points = new HACD::Vec3<HACD::Real>[pointCount];
	memcpy(instance->points, points, sizeof(HACD::Vec3<HACD::Real>) * pointCount);

	instance->triangles = new HACD::Vec3<long>[triangleCount];
	memcpy(instance->triangles, triangles, sizeof(HACD::Vec3<long>) * triangleCount);

	myHACD->SetPoints(instance->points);
	myHACD->SetNPoints(pointCount);
	myHACD->SetTriangles(instance->triangles);
	myHACD->SetNTriangles(triangleCount);
	myHACD->SetNClusters(1);
	myHACD->SetNTargetTrianglesDecimatedMesh(maxTrianglesInDecimatedMesh);
	myHACD->SetNVerticesPerCH(maxVerticesPerConvexHull);
	myHACD->SetAddExtraDistPoints(true);   
	myHACD->SetAddFacesPoints(true); 
	myHACD->SetConcavity(100);
	myHACD->SetCompacityWeight(0.0001);
	//myHACD->SetVolumeWeight(0.0);
	myHACD->SetConnectDist(.03f);//.03f * 1000);//scale factor is 1000 by default
	//myHACD->SetScaleFactor(1000);
	//myHACD->SetSmallClusterThreshold(0.25);
	//myHACD->SetCallBack(&CallBack);

	///////////////////////////////////////
	///////////////////////////////////////

	bool success = myHACD->Compute();	
	if(!success)
	{
		HACD_ClearComputed(instance);
		return false;
	}

	return true;
}

EXPORT int HACD_GetClusterCount(HACDInstance* instance)
{	
	return (int)instance->currentHACD->GetNClusters();
}

EXPORT void HACD_GetBufferSize(HACDInstance* instance, int cluster, int* pointCount, int* triangleCount)
{
	*pointCount = (int)instance->currentHACD->GetNPointsCH(cluster);
	*triangleCount = (int)instance->currentHACD->GetNTrianglesCH(cluster);
}

EXPORT void HACD_GetBuffer(HACDInstance* instance, int cluster, double* points, int* triangles)
{
	instance->currentHACD->GetCH(cluster, (HACD::Vec3<HACD::Real>*)points, (HACD::Vec3<long>*)triangles);
}

EXPORT void HACD_ClearComputed(HACDInstance* instance)
{
	if(instance->currentHACD)
	{
		HACD::DestroyHACD(instance->currentHACD);
		instance->currentHACD = NULL;
	}
	if(instance->points)
	{
		delete[] instance->points;
		instance->points = NULL;
	}
	if(instance->triangles)
	{
		delete[] instance->triangles;
		instance->triangles = NULL;
	}
}

EXPORT void HACD_Shutdown(HACDInstance* instance)
{
	if(instance)
	{
		HACD::releaseHeapManager(instance->heapManager);
		delete instance;
	}
}

