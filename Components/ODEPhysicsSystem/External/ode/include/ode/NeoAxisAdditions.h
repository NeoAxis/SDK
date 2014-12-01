// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
//betauser
#ifndef _ODE_NEOAXIS_ADDITIONS_H_
#define _ODE_NEOAXIS_ADDITIONS_H_

#include <ode/odeconfig.h>
#include <ode/common.h>
#include <ode/collision.h>

#ifdef __cplusplus
extern "C" {
#endif

struct BodyData;
class NeoAxisAdditions;

struct CollisionEventData
{
	int shapeDictionaryIndex1;
	int shapeDictionaryIndex2;
	dVector3 position;
	dVector3 normal;
	float depth;
};

struct RayCastResult
{
	int shapeDictionaryIndex;
	dVector3 position;
	dVector3 normal;
	float distance;
	int triangleID;
};

ODE_API void CheckEnumAndStructuresSizes(int collisionEventData, int rayCastResult);

ODE_API NeoAxisAdditions* NeoAxisAdditions_Init(int maxContacts, float minERP, float maxERP, 
	float maxFriction, float bounceThreshold, dWorldID worldID, dSpaceID rootSpaceID, 
	dGeomID rayCastGeomID, dJointGroupID contactJointGroupID);
ODE_API void NeoAxisAdditions_Shutdown(NeoAxisAdditions* additions);

ODE_API void SetupContactGroups( NeoAxisAdditions* additions, int group0, int group1, 
	bool makeContacts );
ODE_API void DoRayCast(NeoAxisAdditions* additions, int contactGroup, int* count, 
	RayCastResult** data);
ODE_API void DoRayCastPiercing(NeoAxisAdditions* additions, int contactGroup, int* count, 
	RayCastResult** data);
ODE_API void DoVolumeCast(NeoAxisAdditions* additions, dGeomID volumeCastGeomID, int contactGroup, 
	int* count, int** data);
ODE_API bool DoCCDCast( NeoAxisAdditions* additions, dBodyID checkBodyID, int contactGroup, 
	float* minDistance );
ODE_API void SetGeomTriMeshSetRayCallback( dGeomID geomID );
ODE_API void DoSimulationStep(NeoAxisAdditions* additions, int* collisionEventCount, 
	CollisionEventData** collisionEvents);

ODE_API BodyData* CreateBodyData(dBodyID bodyID);
ODE_API void DestroyBodyData(BodyData* bodyData);
ODE_API void BodyDataAddJoint(BodyData* bodyData, dJointID jointID);
ODE_API void BodyDataRemoveJoint(BodyData* bodyData, dJointID jointID);

ODE_API void CreateShapeData( dGeomID geomID, BodyData* bodyData, int shapeDictionaryIndex, 
	bool shapeTypeMesh, int contactGroup, float hardness, float bounciness, float dynamicFriction, 
	float staticFriction);
ODE_API void DestroyShapeData( dGeomID geomID );

ODE_API void SetShapeContractGroup( dGeomID geomID, int contactGroup );
ODE_API void SetShapeMaterialProperties( dGeomID geomID, float hardness, float bounciness, float dynamicFriction, 
	float staticFriction );
ODE_API void SetShapePairDisableContacts( dGeomID geomID1, dGeomID geomID2, bool value );
ODE_API void SetJointContactsEnabled(dJointID jointID, bool contactsEnabled);


#ifdef __cplusplus
}
#endif

#endif
