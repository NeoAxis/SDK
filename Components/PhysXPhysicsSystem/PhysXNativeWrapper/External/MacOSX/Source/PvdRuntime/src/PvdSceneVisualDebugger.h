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


#ifndef PVD_SCENEVISUALDEBUGGER_H
#define PVD_SCENEVISUALDEBUGGER_H

#if PX_SUPPORT_VISUAL_DEBUGGER

#include "PsUserAllocated.h"
#include "PsArray.h"
#include "CmPhysXCommon.h"
#include "PxMetaDataPvdBinding.h"

namespace physx { namespace debugger {
	namespace comm {
		class PvdDataStream;
	}
	namespace renderer
	{
		class PvdImmediateRenderer;
	}
}}

namespace physx
{

class PxSimulationStatistics;
class PxGeometry;

namespace profile
{	
	class PxProfileZone;
}

namespace Scb
{
	class Scene;
	class Actor;
	class Body;
	class RigidStatic;
	class RigidObject;
	class Shape;
	//class Material;
	class ParticleSystem;
	class Constraint;
	class Articulation;
	class ArticulationJoint;
	class Cloth;
}

namespace Sc
{
	class MaterialCore;
	class RigidCore;
	class ConstraintCore;
}

namespace Pvd
{

struct SceneGroups
{
	enum Enum
	{
		RigidDynamics = 1, // 0 is reserved for SimulationStatistics
		RigidStatics,
		Joints,
		Articulations,
		ParticleSystems,
		Materials,
		ProfileZones,
		Cloths,
		NUM_ELEMENTS,
	};
};

//////////////////////////////////////////////////////////////////////////
/*!
RemoteDebug supplies functionality for writing debug info to a stream
to be read by the remote debugger application.
*/
//////////////////////////////////////////////////////////////////////////
class SceneVisualDebugger: public Ps::UserAllocated, public PvdVisualizer
{
public:
	SceneVisualDebugger(Scb::Scene&);
	virtual ~SceneVisualDebugger();
	
	void setPvdConnection(physx::debugger::comm::PvdDataStream* c, PxU32 inConnectionType);
	physx::debugger::comm::PvdDataStream* getPvdConnection() const;

	// internal methods
	void sendClassDescriptions();
	bool isConnected();
	bool isConnectedAndSendingDebugInformation();

	void sendEntireScene();
	void frameStart(PxReal simulateElapsedTime);
	void frameEnd();
	void createPvdInstance();
	void updatePvdProperties();
	void releasePvdInstance();
	
	void createPvdInstance(const PxActor* actor); // temporary for deformables and particle systems
	void updatePvdProperties(const PxActor* actor);
	void releasePvdInstance(const PxActor* actor); // temporary for deformables and particle systems

	void createPvdInstance(Scb::Actor* scbActor); // temporary for deformables and particle systems
	void updatePvdProperties(Scb::Actor* scbActor);
	void releasePvdInstance(Scb::Actor* scbActor); // temporary for deformables and particle systems

	void createPvdInstance(Scb::Body* scbBody);
	void updatePvdProperties(Scb::Body* scbBody);
	void updateKinematicTarget(Scb::Body* scbBody,const PxTransform& p);

	void createPvdInstance(Scb::RigidStatic* scbRigidStatic);
	void updatePvdProperties(Scb::RigidStatic* scbRigidStatic);

	void releasePvdInstance(Scb::RigidObject* scbRigidObject);

	
	void createPvdInstance(const Scb::Shape* scbShape);
	void updateMaterials(const Scb::Shape* scbShape);
	void updatePvdProperties(const Scb::Shape* scbShape);
	void releaseAndRecreateGeometry( const Scb::Shape* scbShape );
	void releasePvdInstance(const Scb::Shape* scbShape);

	//void createPvdInstance(const Scb::Material* scbMat);
	//void updatePvdProperties( const Scb::Material* material );
	//void releasePvdInstance(const Scb::Material* scbMat);

	void createPvdInstance(const Sc::MaterialCore* scbMat);
	void updatePvdProperties( const Sc::MaterialCore* material );
	void releasePvdInstance(const Sc::MaterialCore* scbMat);

	void createPvdInstance(Scb::Articulation* articulation);
	void updatePvdProperties(Scb::Articulation* articulation);
	void releasePvdInstance(Scb::Articulation* articulation);

	void updatePvdProperties(Scb::ArticulationJoint* articulationJoint);

	void createPvdInstance(Scb::ParticleSystem* scbParticleSys);
	void updatePvdProperties(Scb::ParticleSystem* scbParticleSys);
	void releasePvdInstance(Scb::ParticleSystem* scbParticleSys);
	void sendArrays(Scb::ParticleSystem* scbParticleSys);
	
	void createPvdInstance(Scb::Cloth* scbCloth);
	void sendSimpleProperties( Scb::Cloth* cloth );
	void sendMotionConstraints( Scb::Cloth* cloth );
	void sendCollisionSpheres( Scb::Cloth* cloth );
	void sendVirtualParticles( Scb::Cloth* cloth );
	void sendSeparationConstraints( Scb::Cloth* cloth );
	void releasePvdInstance(Scb::Cloth* scbCloth);




	void createPvdInstance(Scb::Constraint* constraint);
	void updatePvdProperties(Scb::Constraint* constraint);
	void releasePvdInstance(Scb::Constraint* constraint);

	void updateContacts();
	void setCreateContactReports(bool);

	void updateJoints();

	//PvdVisualizer
	virtual void visualize( PxArticulationLink& link );
	
private:
	bool updateConstraint(const Sc::ConstraintCore& scConstraint, PxU32 updateType);


	physx::debugger::comm::PvdDataStream*					mPvdConnection;
	physx::debugger::renderer::PvdImmediateRenderer*		mImmediateRenderer;
	Scb::Scene&							mScbScene;
	Ps::Array<PxU64>					mProfileZoneIdList;
	PxU32								mConnectionType;
	PvdMetaDataBinding					mMetaDataBinding;
};

} // namespace Pvd

}

#endif // PX_SUPPORT_VISUAL_DEBUGGER

#endif // VISUALDEBUGGER_H

