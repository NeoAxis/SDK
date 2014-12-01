// This code contains NVIDIA Confidential Information and is disclosed to you 
// under a form of NVIDIA software license agreement provided separately to you.
//
// Notice
// NVIDIA Corporation and its licensors retain all intellectual property and
// proprietary rights in and to this software and related documentation and 
// any modifications thereto. Any use, reproduction, disclosure, or 
// distribution of this software and related documentation without an express 
// license agreement from NVIDIA Corporation is strictly prohibited.
// 
// ALL NVIDIA DESIGN SPECIFICATIONS, CODE ARE PROVIDED "AS IS.". NVIDIA MAKES
// NO WARRANTIES, EXPRESSED, IMPLIED, STATUTORY, OR OTHERWISE WITH RESPECT TO
// THE MATERIALS, AND EXPRESSLY DISCLAIMS ALL IMPLIED WARRANTIES OF NONINFRINGEMENT,
// MERCHANTABILITY, AND FITNESS FOR A PARTICULAR PURPOSE.
//
// Information and code furnished is believed to be accurate and reliable.
// However, NVIDIA Corporation assumes no responsibility for the consequences of use of such
// information or for any infringement of patents or other rights of third parties that may
// result from its use. No license is granted by implication or otherwise under any patent
// or patent rights of NVIDIA Corporation. Details are subject to change without notice.
// This code supersedes and replaces all information previously supplied.
// NVIDIA Corporation products are not authorized for use as critical
// components in life support devices or systems without express written approval of
// NVIDIA Corporation.
//
// Copyright (c) 2008-2012 NVIDIA Corporation. All rights reserved.
// Copyright (c) 2004-2008 AGEIA Technologies, Inc. All rights reserved.
// Copyright (c) 2001-2004 NovodeX AG. All rights reserved.  


#include "PxRigidBodyExt.h"
#include "PxShapeExt.h"

#include "ExtInertiaTensor.h"

#include "PsUserAllocated.h"
#include "PsAlloca.h"

#include "PxShape.h"

#include "PxBoxGeometry.h"
#include "PxSphereGeometry.h"
#include "PxCapsuleGeometry.h"
#include "PxPlaneGeometry.h"
#include "PxConvexMeshGeometry.h"
#include "PxTriangleMeshGeometry.h"
#include "PxHeightFieldGeometry.h"
#include "PxGeometryHelpers.h"

#include "PxConvexMesh.h"

#include "PxBatchQuery.h"

#include "PxRigidDynamic.h"
#include "PxRigidStatic.h"

using namespace physx;

static bool computeMassAndDiagInertia(Ext::InertiaTensorComputer& inertiaComp, 
		PxVec3& diagTensor, PxQuat& orient, PxReal& massOut, PxVec3& coM, bool lockCOM)
{
	// The inertia tensor and center of mass is relative to the actor at this point. Transform to the
	// body frame directly if CoM is specified, else use computed center of mass
	if (lockCOM)
	{
		inertiaComp.translate(-coM);  // base the tensor on user's desired center of mass.
	}
	else
	{
		//get center of mass - has to be done BEFORE centering.
		coM = inertiaComp.getCenterOfMass();

		//the computed result now needs to be centered around the computed center of mass:
		inertiaComp.center();
	}
	// The inertia matrix is now based on the body's center of mass desc.massLocalPose.p
	
	massOut = inertiaComp.getMass();
	diagTensor = PxDiagonalize(inertiaComp.getInertia(), orient);
	return true;
}

static void sweepRigidBody(PxRigidBody& body, PxBatchQuery& batchQuery, bool closestObject, const PxVec3& unitDir, const PxReal distance, PxSceneQueryFilterFlags filterFlags, 
		bool useShapeFilterData, PxFilterData* filterDataList, PxU32 filterDataCount, void* userData, const PxSweepCache* sweepCache)
{
	if (body.getNbShapes() == 0)
		return;

	const char* outOfMemMsg = "PxRigidBodyExt: LinearSweep: Out of memory, call failed.";
	bool succeeded = true;

	PX_ALLOCA(shapes, PxShape*, body.getNbShapes());
	if (!shapes)
	{
		Ps::getFoundation().error(PxErrorCode::eOUT_OF_MEMORY, __FILE__, __LINE__, outOfMemMsg);
		succeeded = false;
	}
	PxU32 nbShapes = body.getShapes(shapes, body.getNbShapes());
	
	PX_ALLOCA(geoms, const PxGeometry*, nbShapes);
	if (!geoms)
	{
		Ps::getFoundation().error(PxErrorCode::eOUT_OF_MEMORY, __FILE__, __LINE__, outOfMemMsg);
		succeeded = false;
	}

	PX_ALLOCA(poses, PxTransform, nbShapes);
	if (!poses)
	{
		Ps::getFoundation().error(PxErrorCode::eOUT_OF_MEMORY, __FILE__, __LINE__, outOfMemMsg);
		succeeded = false;
	}

	PxFilterData* filterData = NULL;
	PX_ALLOCA(filterDataBuffer, PxFilterData, nbShapes);
	if (useShapeFilterData)
	{
		filterData = filterDataBuffer;

		if (!filterDataBuffer)
		{
			Ps::getFoundation().error(PxErrorCode::eOUT_OF_MEMORY, __FILE__, __LINE__, outOfMemMsg);
			succeeded = false;
		}
	}
	else if (filterDataList)
	{
		if (filterDataCount == nbShapes)
		{
			filterData = filterDataList;
		}
		else
		{
			Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, "PxRigidBodyExt: LinearSweep: Number of filter data entries does not match number of shapes, call failed.");
			succeeded = false;
		}
	}

	if (succeeded)
	{
		PxU32 geomByteSize = 0;

		for(PxU32 i=0; i < nbShapes; i++)
		{
			poses[i] = PxShapeExt::getGlobalPose(*shapes[i]);

			if (useShapeFilterData)
				filterData[i] = shapes[i]->getSimulationFilterData();

			// Copy the non-supported geometry types too, to make sure the closest geometry index maps to the shapes
			switch(shapes[i]->getGeometryType())
			{
				case PxGeometryType::eSPHERE : 
				{
					geomByteSize += sizeof(PxSphereGeometry);
				}
				break;

				case PxGeometryType::eBOX : 
				{
					geomByteSize += sizeof(PxBoxGeometry);
				}
				break;

				case PxGeometryType::eCAPSULE : 
				{
					geomByteSize += sizeof(PxCapsuleGeometry);
				}
				break;

				case PxGeometryType::eCONVEXMESH : 
				{
					geomByteSize += sizeof(PxConvexMeshGeometry);
				}
				break;

				case PxGeometryType::ePLANE : 
				{
					geomByteSize += sizeof(PxPlaneGeometry);
				}
				break;

				case PxGeometryType::eTRIANGLEMESH : 
				{
					geomByteSize += sizeof(PxTriangleMeshGeometry);
				}
				break;

				case PxGeometryType::eHEIGHTFIELD : 
				{
					geomByteSize += sizeof(PxHeightFieldGeometry);
				}
				break;
				default:
				break;
			}
		}

		PX_ALLOCA(geomBuffer, PxU8, geomByteSize);

		if (geomBuffer)
		{
			PxU8* currBufferPos = geomBuffer;

			for(PxU32 i=0; i < nbShapes; i++)
			{
				// Copy the non-supported geometry types too, to make sure the closest geometry index maps to the shapes
				switch(shapes[i]->getGeometryType())
				{
					case PxGeometryType::eSPHERE : 
					{
						PxSphereGeometry* g = reinterpret_cast<PxSphereGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getSphereGeometry(*g);
						currBufferPos += sizeof(PxSphereGeometry);
					}
					break;

					case PxGeometryType::eBOX : 
					{
						PxBoxGeometry* g = reinterpret_cast<PxBoxGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getBoxGeometry(*g);
						currBufferPos += sizeof(PxBoxGeometry);
					}
					break;

					case PxGeometryType::eCAPSULE : 
					{
						PxCapsuleGeometry* g = reinterpret_cast<PxCapsuleGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getCapsuleGeometry(*g);
						currBufferPos += sizeof(PxCapsuleGeometry);
					}
					break;

					case PxGeometryType::eCONVEXMESH : 
					{
						PxConvexMeshGeometry* g = reinterpret_cast<PxConvexMeshGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getConvexMeshGeometry(*g);
						currBufferPos += sizeof(PxConvexMeshGeometry);
					}
					break;

					case PxGeometryType::ePLANE : 
					{
						PxPlaneGeometry* g = reinterpret_cast<PxPlaneGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getPlaneGeometry(*g);
						currBufferPos += sizeof(PxPlaneGeometry);
					}
					break;

					case PxGeometryType::eTRIANGLEMESH : 
					{
						PxTriangleMeshGeometry* g = reinterpret_cast<PxTriangleMeshGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getTriangleMeshGeometry(*g);
						currBufferPos += sizeof(PxTriangleMeshGeometry);
					}
					break;

					case PxGeometryType::eHEIGHTFIELD : 
					{
						PxHeightFieldGeometry* g = reinterpret_cast<PxHeightFieldGeometry*>(currBufferPos);
						geoms[i] = g;
						shapes[i]->getHeightFieldGeometry(*g);
						currBufferPos += sizeof(PxHeightFieldGeometry);
					}
					break;
					default:
					break;
				}
			}

			if (closestObject)
				batchQuery.linearCompoundGeometrySweepSingle(geoms, poses, filterData, nbShapes, unitDir, distance, filterFlags, PxSceneQueryFlag::eIMPACT|PxSceneQueryFlag::eNORMAL|PxSceneQueryFlag::eDISTANCE|PxSceneQueryFlag::eUV, userData, sweepCache);
			else
				batchQuery.linearCompoundGeometrySweepMultiple(geoms, poses, filterData, nbShapes, unitDir, distance, filterFlags, PxSceneQueryFlag::eIMPACT|PxSceneQueryFlag::eNORMAL|PxSceneQueryFlag::eDISTANCE|PxSceneQueryFlag::eUV, userData, sweepCache);
		}
	}
}

static bool computeMassAndInertia(bool multipleMassOrDensity, PxRigidBody& body, const PxReal* densities, const PxReal* masses, PxU32 densityOrMassCount, Ext::InertiaTensorComputer& computer)
{
	PX_ASSERT(!densities || !masses);
	PX_ASSERT((densities || masses) && (densityOrMassCount > 0));

	Ext::InertiaTensorComputer inertiaComp(true);

	Ps::InlineArray<PxShape*, 16> shapes("PxShape*"); shapes.resize(body.getNbShapes());

	body.getShapes(shapes.begin(), shapes.size());

	PxU32 validShapeIndex = 0;
	PxReal currentMassOrDensity;
	const PxReal* massOrDensityArray;
	if (densities)
	{
		massOrDensityArray = densities;
		currentMassOrDensity = densities[0];
	}
	else
	{
		massOrDensityArray = masses;
		currentMassOrDensity = masses[0];
	}
	if (!PxIsFinite(currentMassOrDensity))
	{
		Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
			"computeMassAndInertia: Provided mass or density has no valid value");
		return false;
	}

	for(PxU32 i=0; i < shapes.size(); i++)
	{
		if (!(shapes[i]->getFlags() & PxShapeFlag::eSIMULATION_SHAPE))
			continue; 

		if (multipleMassOrDensity)
		{
			if (validShapeIndex < densityOrMassCount)
			{
				currentMassOrDensity = massOrDensityArray[validShapeIndex];

				if (!PxIsFinite(currentMassOrDensity))
				{
					Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
						"computeMassAndInertia: Provided mass or density has no valid value");
					return false;
				}
			}
			else
			{
				Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
					"computeMassAndInertia: Not enough mass/density values provided for all simulation shapes");
				return false;
			}
		}

		Ext::InertiaTensorComputer it(false);

		switch(shapes[i]->getGeometryType())
		{
		case PxGeometryType::eSPHERE : 
			{
				PxSphereGeometry g;
				bool ok = shapes[i]->getSphereGeometry(g);
				PX_ASSERT(ok);
				PX_UNUSED(ok);
				PxTransform temp(shapes[i]->getLocalPose());

				it.setSphere(g.radius, &temp);
			}
			break;

		case PxGeometryType::eBOX : 
			{
				PxBoxGeometry g;
				bool ok = shapes[i]->getBoxGeometry(g);
				PX_ASSERT(ok);
				PX_UNUSED(ok);
				PxTransform temp(shapes[i]->getLocalPose());

				it.setBox(g.halfExtents, &temp);
			}
			break;

		case PxGeometryType::eCAPSULE : 
			{
				PxCapsuleGeometry g;
				bool ok = shapes[i]->getCapsuleGeometry(g);
				PX_ASSERT(ok);
				PX_UNUSED(ok);
				PxTransform temp(shapes[i]->getLocalPose());

				it.setCapsule(0, g.radius, g.halfHeight, &temp);
			}
			break;

		case PxGeometryType::eCONVEXMESH : 
			{
				PxConvexMeshGeometry g;
				bool ok = shapes[i]->getConvexMeshGeometry(g);
				PX_ASSERT(ok);
				PX_UNUSED(ok);
				PxConvexMesh& convMesh = *g.convexMesh;

				PxReal convMass;
				PxMat33 convInertia;
				PxVec3 convCoM;
				convMesh.getMassInformation(convMass, reinterpret_cast<PxMat33&>(convInertia), convCoM);

				//scale the mass:
				convMass *= (g.scale.scale.x * g.scale.scale.y * g.scale.scale.z);
				convCoM = g.scale.rotation.rotateInv(g.scale.scale.multiply(g.scale.rotation.rotate(convCoM)));
				convInertia = Ext::MassProps::scaleInertia(convInertia, g.scale.rotation, g.scale.scale);

				it = Ext::InertiaTensorComputer(convInertia, convCoM, convMass);
				it.transform(shapes[i]->getLocalPose());
			}
			break;

		default :
			{

				Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
					"computeMassAndInertia: Dynamic actor with illegal collision shapes");
				return false;
			}
		}

		if (densities)
			it.scaleDensity(currentMassOrDensity);
		else if (multipleMassOrDensity)  // mass per shape -> need to scale density per shape
			it.scaleDensity(currentMassOrDensity / it.getMass());

		inertiaComp.add(it);

		validShapeIndex++;
	}

	if (validShapeIndex && masses && (!multipleMassOrDensity))  // at least one simulation shape and single mass for all shapes -> scale density at the end
	{
		inertiaComp.scaleDensity(currentMassOrDensity / inertiaComp.getMass());
	}

	computer = inertiaComp;
	return true;
}

static bool updateMassAndInertia(bool multipleMassOrDensity, PxRigidBody& body, const PxReal* densities, PxU32 densityCount, const PxVec3* massLocalPose)
{
	bool success;

	// default values in case there were no shapes
	PxReal massOut = 1.0f;
	PxVec3 diagTensor(1,1,1);
	PxQuat orient = PxQuat::createIdentity();
	bool lockCom = massLocalPose != NULL;
	PxVec3 com = lockCom ? *massLocalPose : PxVec3(0);

	if (densities && densityCount)
	{
		Ext::InertiaTensorComputer inertiaComp(true);
		if(computeMassAndInertia(multipleMassOrDensity, body, densities, NULL, densityCount, inertiaComp))
		{
			if(inertiaComp.getMass()!=0 && computeMassAndDiagInertia(inertiaComp, diagTensor, orient, massOut, com, lockCom))
				success = true;
			else
				success = false;  // body with no shapes provided or computeMassAndDiagInertia() failed
		}
		else
		{
			Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
				"PxRigidBodyExt::updateMassAndInertia: Mass and inertia computation failed, setting mass to 1 and inertia to (1,1,1)");

			success = false;
		}
	}
	else
	{
		Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
			"PxRigidBodyExt::updateMassAndInertia: No density specified, setting mass to 1 and inertia to (1,1,1)");

		success = false;
	}

	PX_ASSERT(orient.isFinite());
	PX_ASSERT(diagTensor.isFinite());
	PX_ASSERT(PxIsFinite(massOut));

	body.setMass(massOut);
	body.setMassSpaceInertiaTensor(diagTensor);
	body.setCMassLocalPose(PxTransform(com, orient));

	return success;
}

bool PxRigidBodyExt::updateMassAndInertia(PxRigidBody& body, const PxReal* densities, PxU32 densityCount, const PxVec3* massLocalPose)
{
	return ::updateMassAndInertia(true, body, densities, densityCount, massLocalPose);
}

bool PxRigidBodyExt::updateMassAndInertia(PxRigidBody& body, PxReal density, const PxVec3* massLocalPose)
{
	return ::updateMassAndInertia(false, body, &density, 1, massLocalPose);
}

bool static setMassAndUpdateInertia(bool multipleMassOrDensity, PxRigidBody& body, const PxReal* masses, PxU32 massCount, const PxVec3* massLocalPose)
{
	bool success;

	// default values in case there were no shapes
	PxReal massOut = 1.0f;
	PxVec3 diagTensor(1,1,1);
	PxQuat orient = PxQuat::createIdentity();
	bool lockCom = massLocalPose != NULL;
	PxVec3 com = lockCom ? *massLocalPose : PxVec3(0);

	if(masses && massCount)
	{
		Ext::InertiaTensorComputer inertiaComp(true);
		if(computeMassAndInertia(multipleMassOrDensity, body, NULL, masses, massCount, inertiaComp))
		{
			success = true;

			if (inertiaComp.getMass()!=0 && !computeMassAndDiagInertia(inertiaComp, diagTensor, orient, massOut, com, lockCom))
				success = false;  // computeMassAndDiagInertia() failed (mass zero?)

			if (massCount == 1)
				massOut = masses[0]; // to cover special case where body has no simulation shape
		}
		else
		{
			Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
				"PxRigidBodyExt::setMassAndUpdateInertia: Mass and inertia computation failed, setting mass to 1 and inertia to (1,1,1)");

			success = false;
		}
	}
	else
	{
		Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
			"PxRigidBodyExt::setMassAndUpdateInertia: No mass specified, setting mass to 1 and inertia to (1,1,1)");
		success = false;
	}

	PX_ASSERT(orient.isFinite());
	PX_ASSERT(diagTensor.isFinite());

	body.setMass(massOut);
	body.setMassSpaceInertiaTensor(diagTensor);
	body.setCMassLocalPose(PxTransform(com, orient));

	return success;
}

bool PxRigidBodyExt::setMassAndUpdateInertia(PxRigidBody& body, const PxReal* masses, PxU32 massCount, const PxVec3* massLocalPose)
{
	return ::setMassAndUpdateInertia(true, body, masses, massCount, massLocalPose);
}

bool PxRigidBodyExt::setMassAndUpdateInertia(PxRigidBody& body, PxReal mass, const PxVec3* massLocalPose)
{
	return ::setMassAndUpdateInertia(false, body, &mass, 1, massLocalPose);
}

PX_INLINE void addForceAtPosInternal(PxRigidBody& body, const PxVec3& force, const PxVec3& pos, PxForceMode::Enum mode, bool wakeup)
{
	if(mode == PxForceMode::eACCELERATION || mode == PxForceMode::eVELOCITY_CHANGE)
	{
		Ps::getFoundation().error(PxErrorCode::eINVALID_PARAMETER, __FILE__, __LINE__, 
			"PxRigidBodyExt::addForce methods do not support eACCELERATION or eVELOCITY_CHANGE modes");
		return;
	}

	const PxTransform globalPose = body.getGlobalPose();
	const PxVec3 centerOfMass = globalPose.transform(body.getCMassLocalPose().p);

	const PxVec3 torque = (pos - centerOfMass).cross(force);
	body.addForce(force, mode, wakeup);
	body.addTorque(torque, mode, wakeup);
}

void PxRigidBodyExt::addForceAtPos(PxRigidBody& body, const PxVec3& force, const PxVec3& pos, PxForceMode::Enum mode, bool wakeup)
{
	addForceAtPosInternal(body, force, pos, mode, wakeup);
}

void PxRigidBodyExt::addForceAtLocalPos(PxRigidBody& body, const PxVec3& force, const PxVec3& pos, PxForceMode::Enum mode, bool wakeup)
{
	//transform pos to world space
	const PxVec3 globalForcePos = body.getGlobalPose().transform(pos);

	addForceAtPosInternal(body, force, globalForcePos, mode, wakeup);
}

void PxRigidBodyExt::addLocalForceAtPos(PxRigidBody& body, const PxVec3& force, const PxVec3& pos, PxForceMode::Enum mode, bool wakeup)
{
	const PxVec3 globalForce = body.getGlobalPose().rotate(force);

	addForceAtPosInternal(body, globalForce, pos, mode, wakeup);
}

void PxRigidBodyExt::addLocalForceAtLocalPos(PxRigidBody& body, const PxVec3& force, const PxVec3& pos, PxForceMode::Enum mode, bool wakeup)
{
	const PxTransform globalPose = body.getGlobalPose();
	const PxVec3 globalForcePos = globalPose.transform(pos);
	const PxVec3 globalForce = globalPose.rotate(force);

	addForceAtPosInternal(body, globalForce, globalForcePos, mode, wakeup);
}

PX_INLINE PxVec3 getVelocityAtPosInternal(const PxRigidBody& body, const PxVec3& point)
{
	PxVec3 velocity = body.getLinearVelocity();
	velocity       += body.getAngularVelocity().cross(point);
	
	return velocity;
}

PxVec3 PxRigidBodyExt::getVelocityAtPos(const PxRigidBody& body, const PxVec3& point)
{
	const PxTransform globalPose = body.getGlobalPose();
	const PxVec3 centerOfMass    = globalPose.transform(body.getCMassLocalPose().p);
	const PxVec3 rpoint          = point - centerOfMass;

	return getVelocityAtPosInternal(body, rpoint);
}

PxVec3 PxRigidBodyExt::getLocalVelocityAtLocalPos(const PxRigidBody& body, const PxVec3& point)
{
	const PxTransform globalPose = body.getGlobalPose();
	const PxVec3 centerOfMass    = globalPose.transform(body.getCMassLocalPose().p);
	const PxVec3 rpoint          = globalPose.transform(point) - centerOfMass;

	return getVelocityAtPosInternal(body, rpoint);
}

PxVec3 PxRigidBodyExt::getVelocityAtOffset(const PxRigidBody& body, const PxVec3& point)
{
	const PxTransform globalPose = body.getGlobalPose();
	const PxVec3 centerOfMass    = globalPose.rotate(body.getCMassLocalPose().p);
	const PxVec3 rpoint          = point - centerOfMass;

	return getVelocityAtPosInternal(body, rpoint);
}

void PxRigidBodyExt::linearSweepSingle(PxRigidBody& body, PxBatchQuery& batchQuery, const PxVec3& unitDir, const PxReal distance,  PxSceneQueryFilterFlags filterFlags, bool useShapeFilterData, PxFilterData* filterDataList, PxU32 filterDataCount, void* userData, const PxSweepCache* sweepCache)
{
	sweepRigidBody(body, batchQuery, true, unitDir, distance, filterFlags, useShapeFilterData, filterDataList, filterDataCount, userData, sweepCache);
}

void PxRigidBodyExt::linearSweepMultiple(PxRigidBody& body, PxBatchQuery& batchQuery, const PxVec3& unitDir, const PxReal distance,  PxSceneQueryFilterFlags filterFlags, bool useShapeFilterData, PxFilterData* filterDataList, PxU32 filterDataCount, void* userData, const PxSweepCache* sweepCache)
{
	sweepRigidBody(body, batchQuery, false, unitDir, distance, filterFlags, useShapeFilterData, filterDataList, filterDataCount, userData, sweepCache);
}



