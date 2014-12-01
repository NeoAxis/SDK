// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
//betauser

#include <ode/NeoAxisAdditions.h>
#include "collision_kernel.h"
#include "ode/objects.h"
#include "joints/joint.h"

typedef unsigned int uint;
class NeoAxisAdditions;

///////////////////////////////////////////////////////////////////////////////////////////////////

struct BodyData
{
	dBodyID bodyID;
	std::vector<dJointID> joints;

	BodyData(){}
};

///////////////////////////////////////////////////////////////////////////////////////////////////

struct ShapeData
{
	BodyData* bodyData;
	int shapeDictionaryIndex;
	bool shapeTypeMesh;

	int contactGroup;
	float hardness;
	float bounciness;
	float dynamicFriction;
	float staticFriction;

	std::set<ShapeData*> pairDisableContacts;

	ShapeData(){}
};

///////////////////////////////////////////////////////////////////////////////////////////////////

class NeoAxisAdditions
{
public:

	int maxContacts;
	float minERP;
	float maxERP;
	float maxFriction;
	float bounceThreshold;

	dWorldID worldID;

	/// The root of the ODE collision detection hierarchy.
	dSpaceID rootSpaceID;

	/// The ODE joint constraint group.
	dJointGroupID contactJointGroupID;

	//collision callbacks
	dContactGeom* contactArray;

	//ContactGroups
	uint contactGroupFlags[32];

	//RayCast
	dGeomID rayCastGeomID;
	bool rayCastPiercingMode;
	int rayCastContactGroup;
	std::queue<int> lastRayCastMeshTriangleList;
	
	//VolumeCast
	dGeomID volumeCastGeomID;
	int volumeCastContactGroup;

	//CCD
	dBodyID ccdCheckBodyID;
	int ccdCastContactGroup;
	float ccdMinDistance;
	bool ccdCastFound;

	std::vector<CollisionEventData> collisionEvents;
	std::vector<RayCastResult> rayCastResults;
	//key: shapeDirectoryIndex
	std::set<int> volumeCastResults;
	std::vector<int> volumeCastResultsAsList;

	//////////////////////////////////////////////

	NeoAxisAdditions(int maxContacts, float minERP, float maxERP, float maxFriction, 
		float bounceThreshold, dWorldID worldID, dSpaceID rootSpaceID, dGeomID rayCastGeomID, 
		dJointGroupID contactJointGroupID)
	{
		this->maxContacts = maxContacts;
		this->minERP = minERP;
		this->maxERP = maxERP;
		this->maxFriction = maxFriction;
		this->bounceThreshold = bounceThreshold;
		this->worldID = worldID;
		this->rootSpaceID = rootSpaceID;
		this->rayCastGeomID = rayCastGeomID;
		this->contactJointGroupID = contactJointGroupID;
		
		contactArray = new dContactGeom[maxContacts];
	}

	~NeoAxisAdditions()
	{
		delete[] contactArray;
	}

	void SetupContactGroups( int group0, int group1, bool makeContacts )
	{
		// The interaction always goes both ways, so we need to set the bit flags both ways.

		uint group0Bit = (uint)( 1 << group0 );
		uint group1Bit = (uint)( 1 << group1 );

		if( makeContacts )
		{
			// Raise the group1 bit in group0's array.
			contactGroupFlags[ group0 ] |= group1Bit;

			// Raise the group0 bit in group1's array.
			contactGroupFlags[ group1 ] |= group0Bit;
		}
		else
		{
			uint tempMask = 0xFFFFFFFF;
			uint notGroup0Bit = group0Bit ^ tempMask;
			uint notGroup1Bit = group1Bit ^ tempMask;

			// Lower the group1 bit in group0's array.
			contactGroupFlags[ group0 ] &= notGroup1Bit;

			// Lower the group0 bit in group1's array.
			contactGroupFlags[ group1 ] &= notGroup0Bit;
		}
	}

	bool IsContactGroupsContactable( int group0, int group1 )
	{
		uint group1Bit = (uint)( 1 << group1 );
		return ( contactGroupFlags[ group0 ] & group1Bit ) != 0;
	}

	bool ShapeIsContactableWithShape(ShapeData* shapeData1, ShapeData* shapeData2)
	{
		//check by contact group
		if( !IsContactGroupsContactable( shapeData1->contactGroup, shapeData2->contactGroup ) )
			return false;

		//shape pair flags (ShapePairFlags.DisableContacts)
		if( shapeData1->pairDisableContacts.size() && shapeData2->pairDisableContacts.size() )
		{
			if(shapeData1->pairDisableContacts.find(shapeData2) != 
				shapeData1->pairDisableContacts.end())
			{
				return false;
			}
		}

		//check common joint (Joint.ContactsEnabled or fixed joint)
		std::vector<dJointID>& joints1 = shapeData1->bodyData->joints;
		std::vector<dJointID>& joints2 = shapeData2->bodyData->joints;

		if(joints1.size() && joints2.size())
		{
			dxJoint* commonJoint = NULL;

			for(std::vector<dJointID>::iterator it1 = joints1.begin(); it1 != joints1.end(); it1++)
			{
				for(std::vector<dJointID>::iterator it2 = joints2.begin(); it2 != joints2.end(); it2++)
				{
					if(*it1 == *it2)
					{
						commonJoint = *it1;
						goto ttt;
					}
				}
			}
			ttt:;

			if(commonJoint)
			{
				if(commonJoint->type() == dJointTypeFixed)
					return false;
				if(!commonJoint->contactsEnabled)
					return false;
			}
		}

		return true;
	}

	static void CollisionCallbackStatic( void* data, dGeomID o1, dGeomID o2 )
	{
		NeoAxisAdditions* additions = (NeoAxisAdditions*)data;
		additions->CollisionCallback(data, o1, o2);
	}

	void CollisionCallback( void* data, dGeomID o1, dGeomID o2 )
	{
		bool isSpace0 = dGeomIsSpace( o1 ) != 0;
		bool isSpace1 = dGeomIsSpace( o2 ) != 0;

		if( isSpace0 || isSpace1 )
		{
			// Colliding a space with either a geom or another space.
			dSpaceCollide2( o1, o2, data, CollisionCallbackStatic );

			if( isSpace0 )
			{
				// Colliding all geoms internal to the space.
				dSpaceCollide( (dSpaceID)o1, data, CollisionCallbackStatic );
			}

			if( isSpace1 )
			{
				// Colliding all geoms internal to the space.
				dSpaceCollide( (dSpaceID)o2, data, CollisionCallbackStatic );
			}
		}
		else
		{
			ShapeData* shapeData1 = (ShapeData*)dGeomGetData( o1 );
			if( shapeData1 == NULL )
				return;
			ShapeData* shapeData2 = (ShapeData*)dGeomGetData( o2 );
			if( shapeData2 == NULL )
				return;

			dBodyID body1 = shapeData1->bodyData->bodyID;
			dBodyID body2 = shapeData2->bodyData->bodyID;

			bool sleeping1 = !body1 || ((body1->flags & dxBodyDisabled) != 0);
			bool sleeping2 = !body2 || ((body2->flags & dxBodyDisabled) != 0);

			if(sleeping1 && sleeping2)
				return;

			if( !ShapeIsContactableWithShape( shapeData1, shapeData2 ) )
				return;

			// Now actually test for collision between the two geoms.
			// This is one of the more expensive operations.
			int numContacts = dCollide( o1, o2, maxContacts, contactArray, sizeof( dContactGeom ) );

			// If the two objects didn't make any contacts, they weren't
			// touching, so just return.
			if( numContacts == 0 )
				return;

			//collision event
			dContactGeom* contact = contactArray;
			for( int n = 0; n < numContacts; n++ )
			{
				CollisionEventData eventData;

				eventData.shapeDictionaryIndex1 = shapeData1->shapeDictionaryIndex;
				eventData.shapeDictionaryIndex2 = shapeData2->shapeDictionaryIndex;
				eventData.position[0] = contact->pos[0];
				eventData.position[1] = contact->pos[1];
				eventData.position[2] = contact->pos[2];
				eventData.normal[0] = contact->normal[0];
				eventData.normal[1] = contact->normal[1];
				eventData.normal[2] = contact->normal[2];
				eventData.depth = contact->depth;

				collisionEvents.push_back(eventData);

				contact++;
			}

			for( int i = 0; i < numContacts; ++i )
			{
				dContact tempContact = {0};
				tempContact.surface.mode = (int)( dContactBounce | dContactSoftERP );

				// Average the hardness of the two materials.
				float hardness = ( shapeData1->hardness + shapeData2->hardness ) * .5f;

				// Convert hardness to ERP.  As hardness goes from
				// 0.0 to 1.0, ERP goes from min to max.
				tempContact.surface.soft_erp = hardness * ( maxERP - minERP ) + minERP;

				float shape1Friction;
				float shape2Friction;
				{
					float diffX = 0;
					float diffY = 0;
					float diffZ = 0;
					if(body1)
					{
						diffX += body1->lvel[0];
						diffY += body1->lvel[1];
						diffZ += body1->lvel[2];
					}
					if(body2)
					{
						diffX -= body2->lvel[0];
						diffY -= body2->lvel[1];
						diffZ -= body2->lvel[2];
					}
					bool dynamic = fabsf( diffX ) > .001f || fabsf( diffY ) > .001f || 
						fabsf( diffZ ) > .001f;

					if( dynamic )
					{
						shape1Friction = shapeData1->dynamicFriction;
						shape2Friction = shapeData2->dynamicFriction;
					}
					else
					{
						shape1Friction = shapeData1->staticFriction;
						shape2Friction = shapeData2->staticFriction;
					}
				}

				// As friction goes from 0.0 to 1.0, mu goes from 0.0
				// to max, though it is set to dInfinity when
				// friction == 1.0.
				//if( shape1Friction >= .999f && shape2Friction >= .999f )
				//{
				//	tempContact.surface.mu = dInfinity;
				//}
				//else
				//{
				float mu = shape1Friction * shape2Friction * maxFriction;
				if( body1 && body1->mass.mass != 0 )
					mu *= body1->mass.mass;
				if( body2 && body2->mass.mass != 0 )
					mu *= body2->mass.mass;
				tempContact.surface.mu = mu;
				//}

				// calculate bounciness of the two materials.
				float bounciness = shapeData1->bounciness * shapeData2->bounciness;

				// ODE's bounce parameter, a.k.a. restitution.
				tempContact.surface.bounce = bounciness;

				// ODE's bounce_vel parameter is a threshold:
				// the relative velocity of the two objects must be
				// greater than this for bouncing to occur at all.
				tempContact.surface.bounce_vel = bounceThreshold;

				tempContact.geom = contactArray[ i ];

				dJointID contactJoint = dJointCreateContact( worldID, contactJointGroupID, 
					&tempContact );
				dJointAttach( contactJoint, body1, body2 );
			}
		}
	}

	//!!!!! no multithread support.
	static NeoAxisAdditions* tempAdditionsForTriCallback;

	static int TriRayCallbackStatic( dGeomID triMesh, dGeomID ray, int triangleIndex,
		dReal u, dReal v )
	{
		if(tempAdditionsForTriCallback)
			tempAdditionsForTriCallback->lastRayCastMeshTriangleList.push( triangleIndex );
		return 1;
	}

	static void RayCastCollisionCallbackStatic(void* data, dGeomID o1, dGeomID o2)
	{
		NeoAxisAdditions* additions = (NeoAxisAdditions*)data;
		additions->RayCastCollisionCallback(data, o1, o2);
	}

	void RayCastCollisionCallback( void* data, dGeomID o1, dGeomID o2 )
	{
		if( dGeomIsSpace( o1 ) != 0 || dGeomIsSpace( o2 ) != 0 )
		{
			// Colliding a space with either a geom or another space.
			dSpaceCollide2( o1, o2, data, RayCastCollisionCallbackStatic );
		}
		else
		{
			// Colliding two geoms.

			if( o1 == o2 )
				return;

			// Get pointers to the two geoms' GeomData structure. One of these 
			// (the one NOT belonging to the ray geom) will always be non-NULL.

			ShapeData* shapeData;
			shapeData = (ShapeData*)dGeomGetData( o1 );
			if( shapeData == NULL )
				shapeData = (ShapeData*)dGeomGetData( o2 );
			if( shapeData == NULL )
				return;

			if( !IsContactGroupsContactable( rayCastContactGroup, shapeData->contactGroup ) )
				return;

			while(lastRayCastMeshTriangleList.size())
				lastRayCastMeshTriangleList.pop();

			int numContacts = dCollide( o1, o2, maxContacts, contactArray, sizeof( dContactGeom ) );

			if( numContacts == 0 )
				return;

			dContactGeom* contact = contactArray;

			for( int n = 0; n < numContacts; n++ )
			{
				if( !rayCastPiercingMode )
				{
					float distance = contact->depth;

					if( rayCastResults.size() == 0 )
					{
						RayCastResult result;
						result.shapeDictionaryIndex = shapeData->shapeDictionaryIndex;
						result.position[0] = contact->pos[0];
						result.position[1] = contact->pos[1];
						result.position[2] = contact->pos[2];
						result.normal[0] = contact->normal[0];
						result.normal[1] = contact->normal[1];
						result.normal[2] = contact->normal[2];
						result.distance = distance;
						result.triangleID = 0;

						if( shapeData->shapeTypeMesh )
						{
							if(lastRayCastMeshTriangleList.size())
							{
								result.triangleID = lastRayCastMeshTriangleList.back();
								lastRayCastMeshTriangleList.pop();
							}
						}

						rayCastResults.push_back(result);
					}
					else
					{
						if( distance < rayCastResults[0].distance )
						{
							RayCastResult result;
							result.shapeDictionaryIndex = shapeData->shapeDictionaryIndex;
							result.position[0] = contact->pos[0];
							result.position[1] = contact->pos[1];
							result.position[2] = contact->pos[2];
							result.normal[0] = contact->normal[0];
							result.normal[1] = contact->normal[1];
							result.normal[2] = contact->normal[2];
							result.distance = distance;
							result.triangleID = 0;

							if( shapeData->shapeTypeMesh )
							{
								if(lastRayCastMeshTriangleList.size())
								{
									result.triangleID = lastRayCastMeshTriangleList.back();
									lastRayCastMeshTriangleList.pop();
								}
							}

							rayCastResults[0] = result;
						}
					}
				}
				else
				{
					//RayCastPiercing

					RayCastResult result;
					result.shapeDictionaryIndex = shapeData->shapeDictionaryIndex;
					result.position[0] = contact->pos[0];
					result.position[1] = contact->pos[1];
					result.position[2] = contact->pos[2];
					result.normal[0] = contact->normal[0];
					result.normal[1] = contact->normal[1];
					result.normal[2] = contact->normal[2];
					result.distance = contact->depth;
					result.triangleID = 0;

					if( shapeData->shapeTypeMesh )
					{
						if(lastRayCastMeshTriangleList.size())
						{
							result.triangleID = lastRayCastMeshTriangleList.back();
							lastRayCastMeshTriangleList.pop();
						}
					}

					rayCastResults.push_back(result);
				}

				contact++;
			}
		}
	}

	void DoRayCast(int contactGroup, int* count, RayCastResult** data)
	{
		tempAdditionsForTriCallback = this;

		rayCastPiercingMode = false;
		rayCastContactGroup = contactGroup;

		rayCastResults.resize(0);

		dSpaceCollide2( rayCastGeomID, (dGeomID)rootSpaceID, this, RayCastCollisionCallbackStatic );

		*count = rayCastResults.size();
		if(rayCastResults.size())
			*data = &rayCastResults[0];
		else
			*data = NULL;

		tempAdditionsForTriCallback = NULL;
	}

	void DoRayCastPiercing(int contactGroup, int* count, RayCastResult** data)
	{
		tempAdditionsForTriCallback = this;

		rayCastPiercingMode = true;
		rayCastContactGroup = contactGroup;

		rayCastResults.resize(0);

		dSpaceCollide2( rayCastGeomID, (dGeomID)rootSpaceID, this, RayCastCollisionCallbackStatic );

		*count = rayCastResults.size();
		if(rayCastResults.size())
			*data = &rayCastResults[0];
		else
			*data = NULL;

		tempAdditionsForTriCallback = NULL;
	}

	static void VolumeCastCollisionCallbackStatic( void* data, dGeomID o1, dGeomID o2 )
	{
		NeoAxisAdditions* additions = (NeoAxisAdditions*)data;
		additions->VolumeCastCollisionCallback(data, o1, o2);
	}

	void VolumeCastCollisionCallback( void* data, dGeomID o1, dGeomID o2 )
	{
		if( dGeomIsSpace( o1 ) != 0 || dGeomIsSpace( o2 ) != 0 )
		{
			// Colliding a space with either a geom or another space.
			dSpaceCollide2( o1, o2, data, VolumeCastCollisionCallbackStatic );
		}
		else
		{
			if( o1 == o2 )
				return;

			dGeomID obj = ( o1 == volumeCastGeomID ) ? o2 : o1;

			ShapeData* shapeData = (ShapeData*)dGeomGetData( obj );
			if( shapeData == NULL )
				return;

			if( !IsContactGroupsContactable( shapeData->contactGroup, volumeCastContactGroup ) )
				return;

			//alrealy added
			if(volumeCastResults.find(shapeData->shapeDictionaryIndex) != volumeCastResults.end())
				return;

			int numContacts = dCollide( o1, o2, 1, contactArray, sizeof(dContactGeom ) );

			if( numContacts == 0 )
				return;

			volumeCastResults.insert(shapeData->shapeDictionaryIndex);
		}
	}

	void DoVolumeCast(dGeomID volumeCastGeomID, int contactGroup, int* count, int** data)
	{
		this->volumeCastGeomID = volumeCastGeomID;
		this->volumeCastContactGroup = contactGroup;

		volumeCastResults.clear();
		volumeCastResultsAsList.resize(0);

		dSpaceCollide2( volumeCastGeomID, rootSpaceID, this, VolumeCastCollisionCallbackStatic );

		for(std::set<int>::iterator it = volumeCastResults.begin(); it != volumeCastResults.end(); 
			it++)
		{
			volumeCastResultsAsList.push_back(*it);
		}

		*count = volumeCastResultsAsList.size();
		if(volumeCastResultsAsList.size())
			*data = &volumeCastResultsAsList[0];
		else
			*data = NULL;
	}

	static void CCDCastCollisionCallbackStatic( void* data, dGeomID o1, dGeomID o2 )
	{
		NeoAxisAdditions* additions = (NeoAxisAdditions*)data;
		additions->CCDCastCollisionCallback(data, o1, o2);
	}

	void CCDCastCollisionCallback( void* data, dGeomID o1, dGeomID o2 )
	{
		if( dGeomIsSpace( o1 ) != 0 || dGeomIsSpace( o2 ) != 0 )
		{
			// Colliding a space with either a geom or another space.
			dSpaceCollide2( o1, o2, data, CCDCastCollisionCallbackStatic );
		}
		else
		{
			if( o1 == o2 )
				return;

			ShapeData* shapeData = NULL;
			shapeData = (ShapeData*)dGeomGetData( o1 );
			if( shapeData == NULL )
				shapeData = (ShapeData*)dGeomGetData( o2 );
			if( shapeData == NULL )
				return;

			if( shapeData->bodyData->bodyID == ccdCheckBodyID )
				return;

			//!!!!!contact pairs need too?
			if( !IsContactGroupsContactable( shapeData->contactGroup, ccdCastContactGroup ) )
				return;

			int numContacts = dCollide( o1, o2, maxContacts, contactArray, sizeof( dContactGeom ) );

			for( int n = 0; n < numContacts; n++ )
			{
				float distance = contactArray[ n ].depth;
				if( !ccdCastFound || distance < ccdMinDistance )
				{
					ccdCastFound = true;
					ccdMinDistance = distance;
				}
			}
		}
	}

	bool DoCCDCast( dBodyID checkBodyID, int contactGroup, float* minDistance )
	{
		ccdCheckBodyID = checkBodyID;
		ccdCastContactGroup = contactGroup;
		ccdMinDistance = 0;
		ccdCastFound = false;

		dSpaceCollide2( rayCastGeomID, rootSpaceID, this, CCDCastCollisionCallbackStatic );

		*minDistance = ccdMinDistance;
		return ccdCastFound;
	}

	void DoSimulationStep(int* collisionEventCount, CollisionEventData** collisionEvents)
	{
		this->collisionEvents.resize(0);

		// Do collision detection; add contacts to the contact joint group.
		dSpaceCollide( rootSpaceID, this, CollisionCallbackStatic );

		*collisionEventCount = this->collisionEvents.size();
		if(this->collisionEvents.size())
			*collisionEvents = &this->collisionEvents[0];
		else
			*collisionEvents = NULL;
	}

};

NeoAxisAdditions* NeoAxisAdditions::tempAdditionsForTriCallback = NULL;

///////////////////////////////////////////////////////////////////////////////////////////////////

void CheckEnumAndStructuresSizes(int collisionEventData, int rayCastResult)
{
	if(sizeof(CollisionEventData) != collisionEventData)
		dError(d_ERR_UNKNOWN, "sizeof(CollisionEventData) != collisionEventData");
	if(sizeof(RayCastResult) != rayCastResult)
		dError(d_ERR_UNKNOWN, "sizeof(RayCastResult) != rayCastResult");
}

NeoAxisAdditions* NeoAxisAdditions_Init(int maxContacts, float minERP, float maxERP, float maxFriction, 
	float bounceThreshold, dWorldID worldID, dSpaceID rootSpaceID, dGeomID rayCastGeomID, 
	dJointGroupID contactJointGroupID)
{
	return new NeoAxisAdditions(maxContacts, minERP, maxERP, maxFriction, bounceThreshold, 
		worldID, rootSpaceID, rayCastGeomID, contactJointGroupID);
}

void NeoAxisAdditions_Shutdown(NeoAxisAdditions* additions)
{
	if(additions)
		delete additions;
}

void SetupContactGroups( NeoAxisAdditions* additions, int group0, int group1, bool makeContacts )
{
	additions->SetupContactGroups(group0, group1, makeContacts);
}

void DoRayCast(NeoAxisAdditions* additions, int contactGroup, int* count, RayCastResult** data)
{
	additions->DoRayCast(contactGroup, count, data);
}

void DoRayCastPiercing(NeoAxisAdditions* additions, int contactGroup, int* count, 
	RayCastResult** data)
{
	additions->DoRayCastPiercing(contactGroup, count, data);
}

void DoVolumeCast(NeoAxisAdditions* additions, dGeomID volumeCastGeomID, int contactGroup, 
	int* count, int** data)
{
	additions->DoVolumeCast(volumeCastGeomID, contactGroup, count, data);
}

bool DoCCDCast( NeoAxisAdditions* additions, dBodyID checkBodyID, int contactGroup, 
	float* minDistance )
{
	return additions->DoCCDCast(checkBodyID, contactGroup, minDistance);
}

void SetGeomTriMeshSetRayCallback( dGeomID geomID )
{
	dGeomTriMeshSetRayCallback( geomID, NeoAxisAdditions::TriRayCallbackStatic );
}

void DoSimulationStep(NeoAxisAdditions* additions, int* collisionEventCount, 
	CollisionEventData** collisionEvents)
{
	additions->DoSimulationStep(collisionEventCount, collisionEvents);
}

BodyData* CreateBodyData(dBodyID bodyID)
{
	BodyData* bodyData = new BodyData();
	bodyData->bodyID = bodyID;
	return bodyData;
}

void DestroyBodyData(BodyData* bodyData)
{
	delete bodyData;
}

void BodyDataAddJoint(BodyData* bodyData, dJointID jointID)
{
	bodyData->joints.push_back(jointID);
}

void BodyDataRemoveJoint(BodyData* bodyData, dJointID jointID)
{
	for(int n = 0; n < (int)bodyData->joints.size(); n++)
	{
		if(bodyData->joints[n] == jointID)
		{
			bodyData->joints.erase(bodyData->joints.begin() + n);
			return;
		}
	}
}

void CreateShapeData( dGeomID geomID, BodyData* bodyData, int shapeDictionaryIndex, 
	bool shapeTypeMesh, int contactGroup, float hardness, float bounciness, 
	float dynamicFriction, float staticFriction)
{
	ShapeData* shapeData = new ShapeData();

	shapeData->bodyData = bodyData;
	shapeData->shapeDictionaryIndex = shapeDictionaryIndex;
	shapeData->shapeTypeMesh = shapeTypeMesh;
	shapeData->contactGroup = contactGroup;
	shapeData->hardness = hardness;
	shapeData->bounciness = bounciness;
	shapeData->dynamicFriction = dynamicFriction;
	shapeData->staticFriction = staticFriction;

	dGeomSetData( geomID, shapeData );
}

void DestroyShapeData( dGeomID geomID )
{
	ShapeData* shapeData = (ShapeData*)dGeomGetData(geomID);
	if(shapeData)
	{
		//pairDisableContacts: remove from linked ShapeDatas
		for(std::set<ShapeData*>::iterator it = shapeData->pairDisableContacts.begin(); 
			it != shapeData->pairDisableContacts.end(); it++)
		{
			ShapeData* otherShapeData = *it;
			otherShapeData->pairDisableContacts.erase(shapeData);
		}

		delete shapeData;
		dGeomSetData( geomID, NULL );
	}
}

void SetShapeContractGroup( dGeomID geomID, int contactGroup )
{
	ShapeData* shapeData = (ShapeData*)dGeomGetData(geomID);
	
	shapeData->contactGroup = contactGroup;
}

void SetShapeMaterialProperties( dGeomID geomID, float hardness, float bounciness, float dynamicFriction, float staticFriction )
{
	ShapeData* shapeData = (ShapeData*)dGeomGetData(geomID);

	shapeData->hardness = hardness;
	shapeData->bounciness = bounciness;
	shapeData->dynamicFriction = dynamicFriction;
	shapeData->staticFriction = staticFriction;
}

void SetShapePairDisableContacts( dGeomID geomID1, dGeomID geomID2, bool value )
{
	ShapeData* shapeData1 = (ShapeData*)dGeomGetData(geomID1);
	ShapeData* shapeData2 = (ShapeData*)dGeomGetData(geomID2);

	if(value)
	{
		shapeData1->pairDisableContacts.insert(shapeData2);
		shapeData2->pairDisableContacts.insert(shapeData1);
	}
	else
	{
		shapeData1->pairDisableContacts.erase(shapeData2);
		shapeData2->pairDisableContacts.erase(shapeData1);
	}
}

void SetJointContactsEnabled(dJointID jointID, bool contactsEnabled)
{
	jointID->contactsEnabled = contactsEnabled;
}
