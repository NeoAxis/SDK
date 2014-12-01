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

// suppress LNK4221
#include "PxPreprocessor.h"
PX_DUMMY_SYMBOL

#if PX_SUPPORT_VISUAL_DEBUGGER

#include "PvdSceneVisualDebugger.h"
#include "PvdDataStream.h"

#include "ScPhysics.h"
#include "NpScene.h"
#include "PsFoundation.h"

#include "ScBodyCore.h"
#include "ScBodySim.h"
#include "ScConstraintSim.h"

#include "NpRigidDynamic.h"
#include "NpRigidStatic.h"

#include "NpArticulation.h"
#include "NpArticulationLink.h"
#include "NpArticulationJoint.h"

#include "NpParticleFluid.h"
#include "CmEventProfiler.h"

#include "ScbCloth.h"
#include "NpCloth.h"
#include "PvdConnection.h"
#include "PvdTypeNames.h"

#include "PvdImmediateRenderer.h"


namespace physx
{
namespace Pvd
{

#define UPDATE_PVD_PROPERTIES_CHECK() { if ( !isConnectedAndSendingDebugInformation() ) return; }

PX_FORCE_INLINE static NpScene* getNpScene(Scb::Scene* scbScene) 
{ 
	return static_cast<NpScene*>(scbScene->getPxScene());
}

PX_FORCE_INLINE static const NpRigidDynamic* getNpRigidDynamic(const Scb::Body* scbBody) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpRigidDynamic*>(0)->getScbActorFast()));
	return reinterpret_cast<const NpRigidDynamic*>(reinterpret_cast<const char*>(scbBody)-offset);
}

PX_FORCE_INLINE static NpRigidDynamic* getNpRigidDynamic(Scb::Body* scbBody) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpRigidDynamic*>(0)->getScbActorFast()));
	return reinterpret_cast<NpRigidDynamic*>(reinterpret_cast<char*>(scbBody)-offset);
}

PX_FORCE_INLINE static const NpRigidStatic* getNpRigidStatic(const Scb::RigidStatic* scbRigidStatic) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpRigidStatic*>(0)->getScbActorFast()));
	return reinterpret_cast<const NpRigidStatic*>(reinterpret_cast<const char*>(scbRigidStatic)-offset);
}

PX_FORCE_INLINE static NpRigidStatic* getNpRigidStatic(Scb::RigidStatic* scbRigidStatic) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpRigidStatic*>(0)->getScbActorFast()));
	return reinterpret_cast<NpRigidStatic*>(reinterpret_cast<char*>(scbRigidStatic)-offset);
}

PX_FORCE_INLINE static NpShape* getNpShape(Scb::Shape* scbShape) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpShape*>(0)->getScbShape()));
	return reinterpret_cast<NpShape*>(reinterpret_cast<char*>(scbShape)-offset);
}
PX_FORCE_INLINE static NpShape* getNpShape(const Scb::Shape* scbShape) 
{ 
	return getNpShape( const_cast< Scb::Shape* >( scbShape ) );
}

#if PX_USE_PARTICLE_SYSTEM_API

PX_FORCE_INLINE static const NpParticleSystem* getNpParticleSystem(const Scb::ParticleSystem* scbParticleSystem) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpParticleSystem*>(0)->getScbActor()));
	return reinterpret_cast<const NpParticleSystem*>(reinterpret_cast<const char*>(scbParticleSystem)-offset);
}

PX_FORCE_INLINE static const NpParticleFluid* getNpParticleFluid(const Scb::ParticleSystem* scbParticleSystem) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpParticleFluid*>(0)->getScbActor()));
	return reinterpret_cast<const NpParticleFluid*>(reinterpret_cast<const char*>(scbParticleSystem)-offset);
}

#endif

PX_FORCE_INLINE static const NpArticulationLink* getNpArticulationLink(const Scb::Body* scbArticulationLink) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpArticulationLink*>(0)->getScbActorFast()));
	return reinterpret_cast<const NpArticulationLink*>(reinterpret_cast<const char*>(scbArticulationLink)-offset);
}

PX_FORCE_INLINE static NpArticulationLink* getNpArticulationLink(Scb::Body* scbArticulationLink) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpArticulationLink*>(0)->getScbActorFast()));
	return reinterpret_cast<NpArticulationLink*>(reinterpret_cast<char*>(scbArticulationLink)-offset);
}

PX_FORCE_INLINE static NpArticulation* getNpArticulation(Scb::Articulation* scbArticulation) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpArticulation*>(0)->getArticulation()));
	return reinterpret_cast<NpArticulation*>(reinterpret_cast<char*>(scbArticulation)-offset);
}

PX_FORCE_INLINE static NpArticulationJoint* getNpArticulationJoint(Scb::ArticulationJoint* scbArticulationJoint) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpArticulationJoint*>(0)->getScbArticulationJoint()));
	return reinterpret_cast<NpArticulationJoint*>(reinterpret_cast<char*>(scbArticulationJoint)-offset);
}

PX_FORCE_INLINE static NpConstraint* getNpConstraint(Sc::ConstraintCore* scConstraint) 
{ 
	size_t scOffset = reinterpret_cast<size_t>(&(reinterpret_cast<Scb::Constraint*>(0)->getScConstraint()));
	size_t scbOffset = reinterpret_cast<size_t>(&(reinterpret_cast<NpConstraint*>(0)->getScbConstraint()));
	return reinterpret_cast<NpConstraint*>(reinterpret_cast<char*>(scConstraint)-scOffset-scbOffset);
}

#if PX_USE_PARTICLE_SYSTEM_API

PX_FORCE_INLINE static Scb::ParticleSystem* getScbParticleSystem(Sc::ParticleSystemCore* scParticleSystem) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<Scb::ParticleSystem*>(0)->getScParticleSystem()));
	return reinterpret_cast<Scb::ParticleSystem*>(reinterpret_cast<char*>(scParticleSystem)-offset);
}

#endif

#if PX_USE_CLOTH_API

PX_FORCE_INLINE static NpCloth* backptr(Scb::Cloth* cloth) 
{ 
	size_t offset = reinterpret_cast<size_t>(&(reinterpret_cast<NpCloth*>(0)->getScbCloth()));
	return reinterpret_cast<NpCloth*>(reinterpret_cast<char*>(cloth)-offset);
}

#endif

PX_FORCE_INLINE static const PxActor* getPxActor(const Scb::Actor* scbActor)
{
	PxActorType::Enum type = scbActor->getActorCoreSLOW().getActorCoreType();
	if(type == PxActorType::eRIGID_DYNAMIC)
	{
		return getNpRigidDynamic(static_cast<const Scb::Body*>(scbActor));
	}
	else if(type == PxActorType::eRIGID_STATIC)
	{
		return getNpRigidStatic(static_cast<const Scb::RigidStatic*>(scbActor));
	}
#if PX_USE_PARTICLE_SYSTEM_API
	else if (type == PxActorType::ePARTICLE_SYSTEM)
	{
		return getNpParticleSystem(static_cast<const Scb::ParticleSystem*>(scbActor));
	}
	else if (type == PxActorType::ePARTICLE_FLUID)
	{
		return getNpParticleFluid(static_cast<const Scb::ParticleSystem*>(scbActor));
	}
#endif
	else if (type == PxActorType::eARTICULATION_LINK)
	{
		return getNpArticulationLink(static_cast<const Scb::Body*>(scbActor));
	}
#if PX_USE_CLOTH_API
	else if (type == PxActorType::eCLOTH)
	{
		return backptr(const_cast<Scb::Cloth*>(static_cast<const Scb::Cloth*>(scbActor)));
	}
#endif
	
	return NULL;
}


namespace {
	struct PvdConstraintVisualizer : public PxConstraintVisualizer
	{
		physx::debugger::renderer::PvdImmediateRenderer& mRenderer;
		PvdConstraintVisualizer( const void* id, physx::debugger::renderer::PvdImmediateRenderer& r )
			: mRenderer( r )
		{
			mRenderer.setInstanceId( id );
		}
		virtual void visualizeJointFrames( const PxTransform& parent, const PxTransform& child )
		{
			mRenderer.visualizeJointFrames( parent, child );
		}

		virtual void visualizeLinearLimit( const PxTransform& t0, const PxTransform& t1, PxReal value, bool active )
		{
			mRenderer.visualizeLinearLimit( t0, t1, static_cast<PxF32>( value ), active );
		}

		virtual void visualizeAngularLimit( const PxTransform& t0, PxReal lower, PxReal upper, bool active)
		{
			mRenderer.visualizeAngularLimit( t0, static_cast<PxF32>( lower ), static_cast<PxF32>( upper ), active );
		}

		virtual void visualizeLimitCone( const PxTransform& t, PxReal ySwing, PxReal zSwing, bool active)
		{
			mRenderer.visualizeLimitCone( t, static_cast<PxF32>( ySwing ), static_cast<PxF32>( zSwing ), active );
		}

		virtual void visualizeDoubleCone( const PxTransform& t, PxReal angle, bool active)
		{
			mRenderer.visualizeDoubleCone( t, static_cast<PxF32>( angle ), active );
		}
	};
}


SceneVisualDebugger::SceneVisualDebugger(Scb::Scene& s)
: mPvdConnection(NULL)
, mImmediateRenderer( NULL )
, mScbScene(s)
, mConnectionType( 0 )
{
}


SceneVisualDebugger::~SceneVisualDebugger()
{
	if(isConnected())
	{
		releasePvdInstance();
		setCreateContactReports(false);
	}
	if (mImmediateRenderer)
		mImmediateRenderer->release();
	if(mPvdConnection)
		mPvdConnection->release();
}


physx::debugger::comm::PvdDataStream* SceneVisualDebugger::getPvdConnection() const
{
	return mPvdConnection;
}


void SceneVisualDebugger::setPvdConnection(physx::debugger::comm::PvdDataStream* c, PxU32 inConnectionType)
{
	if ( mImmediateRenderer )
		mImmediateRenderer->release();
	mImmediateRenderer = NULL;
	if(mPvdConnection)
		mPvdConnection->release();
	mConnectionType = inConnectionType;

	mPvdConnection = c;

	if(mPvdConnection)
		c->addRef();
	else
		mProfileZoneIdList.clear();		
}

void SceneVisualDebugger::setCreateContactReports(bool s)
{
	mScbScene.getScScene().setCreateContactReports(s);
}


bool SceneVisualDebugger::isConnected()
{ 
	return mPvdConnection && mPvdConnection->isConnected(); 
}

bool SceneVisualDebugger::isConnectedAndSendingDebugInformation()
{
	return isConnected()
			&& ( mConnectionType & physx::debugger::PvdConnectionType::Debug );
}

void SceneVisualDebugger::sendEntireScene()
{
	if(!isConnected())
		return;

	createPvdInstance();
	
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	// materials:
	{
//		NpScene* npScene = getNpScene(&mScbScene);
		PxsMaterialManager* manager = mScbScene.getScScene().getMaterialManager();
		PxsMaterialManagerIterator iter(*manager);
		while(iter.hasNextMaterial())
		{
			Sc::MaterialCore* mat = (Sc::MaterialCore*)iter.getNextMaterial();
			const PxMaterial* theMaterial = mat->getNxMaterial();
			sdkPvd.increaseReference(theMaterial);
		};
	/*	PxU32 numMaterials = mScbScene.getSceneMaterialTable().size();
		for(PxU32 i = 0; i < numMaterials; i++)
		{
			Scb::Material* scbMat( mScbScene.getSceneMaterialTable()[i] );
			const PxMaterial* theMaterial( scbMat->getNxMaterial() );
			sdkPvd.increaseReference( theMaterial );
		}*/
	}

	if ( isConnectedAndSendingDebugInformation() )
	{
		NpScene* npScene = getNpScene(&mScbScene);
		Ps::Array<PxActor*> actorArray;
		// RBs
		// static:
		{
			PxU32 numActors = npScene->getNbActors(PxActorTypeSelectionFlag::eRIGID_STATIC | PxActorTypeSelectionFlag::eRIGID_DYNAMIC );
			actorArray.resize(numActors);
			npScene->getActors(PxActorTypeSelectionFlag::eRIGID_STATIC | PxActorTypeSelectionFlag::eRIGID_DYNAMIC, actorArray.begin(), actorArray.size());
			for(PxU32 i = 0; i < numActors; i++)
			{
				PxActor* pxActor = actorArray[i];
				if ( pxActor->is<PxRigidStatic>() )
					mMetaDataBinding.createInstance( *mPvdConnection, *static_cast<PxRigidStatic*>(pxActor), *npScene, sdkPvd );
				else
					mMetaDataBinding.createInstance( *mPvdConnection, *static_cast<PxRigidDynamic*>(pxActor), *npScene, sdkPvd );
			}
		}
		// articulations & links
		{
			Ps::Array<PxArticulation*> articulations;
			PxU32 numArticulations = npScene->getNbArticulations();
			articulations.resize(numArticulations);
			npScene->getArticulations(articulations.begin(), articulations.size());
			for(PxU32 i = 0; i < numArticulations; i++)
				mMetaDataBinding.createInstance( *mPvdConnection, *articulations[i], *npScene, sdkPvd );
		}

#if PX_USE_PARTICLE_SYSTEM_API
		// particle systems & fluids:
		{
			PxU32 nbParticleSystems = mScbScene.getScScene().getNbParticleSystems();
			Sc::ParticleSystemCore** particleSystems = mScbScene.getScScene().getParticleSystems();
			for(PxU32 i = 0; i < nbParticleSystems; i++)
			{
				Scb::ParticleSystem* scbParticleSystem = getScbParticleSystem(particleSystems[i]);
				createPvdInstance(scbParticleSystem);
			}
		}
#endif

#if PX_USE_CLOTH_API
		//cloth 
		{
			NpScene* npScene = getNpScene(&mScbScene);
			Ps::Array<PxActor*> actorArray;
			PxU32 numActors = npScene->getNbActors(PxActorTypeSelectionFlag::eCLOTH);
			actorArray.resize(numActors);
			npScene->getActors(PxActorTypeSelectionFlag::eCLOTH, actorArray.begin(), actorArray.size());
			for(PxU32 i = 0; i < numActors; i++)
			{
				Scb::Cloth* scbCloth = &static_cast<NpCloth*>(actorArray[i])->getScbCloth();
				createPvdInstance(scbCloth);
			}
		}
#endif

		// joints
		{
			Sc::ConstraintIterator iterator;
			mScbScene.getScScene().initConstraintsIterator(iterator);
			Sc::ConstraintCore* constraint;
			for(constraint = iterator.getNext(); constraint; constraint = iterator.getNext())
			{
				updateConstraint(*constraint, PxPvdUpdateType::CREATE_INSTANCE);
				updateConstraint(*constraint, PxPvdUpdateType::UPDATE_ALL_PROPERTIES);
			}
		}
	}

	mPvdConnection->flush();
}


void SceneVisualDebugger::frameStart(PxReal simElapsedTime)
{
	if(!isConnected())
		return;
	mMetaDataBinding.sendBeginFrame( *mPvdConnection, mScbScene.getPxScene(), simElapsedTime );
	mPvdConnection->flush();
}


void SceneVisualDebugger::frameEnd()
{
	if(!isConnected())
		return;
	
	const PxScene* theScene = mScbScene.getPxScene();

	//Send the statistics for last frame.
	mMetaDataBinding.sendStats( *mPvdConnection, theScene  );
	//Flush the outstanding memory events.  PVD in some situations tracks memory events
	//and can display graphs of memory usage at certain points.  They have to get flushed
	//at some point...
	//Also note that PVD is a consumer of the profiling system events.  This ensures
	//that PVD gets a view of the profiling events that pertained to the last frame.
	physx::debugger::PvdScopedItem<physx::debugger::comm::PvdConnection> connection( NpPhysics::getInstance().getPvdConnectionManager()->getAndAddRefCurrentConnection() );
	if ( connection ) 
	{
		//Flushes memory and profiling events out of any buffered areas.
		connection->flush();
	}
	
	//End the frame *before* we send the dynamic object current data.
	//This ensures that contacts end up synced with the rest of the system.
	//Note that contacts were sent much earler in NpScene::fetchResults.
	mMetaDataBinding.sendEndFrame(*mPvdConnection, mScbScene.getPxScene() );
	//flush our data to the main connection
	mPvdConnection->flush(); 


	
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();


	if(isConnectedAndSendingDebugInformation())
	{
		const bool visualizeJoints = sdkPvd.isVisualizingConstraints();
		if ( visualizeJoints && mImmediateRenderer == NULL )
		{
			NpPhysics& physics( NpPhysics::getInstance() );
			physx::debugger::comm::PvdConnectionManager& mgr( *physics.getPvdConnectionManager() );
			physx::debugger::comm::PvdConnection* connection( mgr.getAndAddRefCurrentConnection() );
			if ( connection )
			{
				mImmediateRenderer = &connection->createRenderer();
				mImmediateRenderer->addRef();
				connection->release();
			}
		}
		{
			PvdVisualizer* vizualizer = NULL;
			if ( visualizeJoints ) vizualizer = this;
			CM_PROFILE_ZONE_WITH_SUBSYSTEM( mScbScene,PVD,sceneUpdate );
			mMetaDataBinding.updateDynamicActorsAndArticulations( *mPvdConnection, theScene, vizualizer );
		}

#if PX_USE_PARTICLE_SYSTEM_API
		// particle systems & fluids:
		{
			CM_PROFILE_ZONE_WITH_SUBSYSTEM( mScbScene,PVD,updatePariclesAndFluids );
			PxU32 nbParticleSystems = mScbScene.getScScene().getNbParticleSystems();
			Sc::ParticleSystemCore** particleSystems = mScbScene.getScScene().getParticleSystems();
			for(PxU32 i = 0; i < nbParticleSystems; i++)
			{
				Scb::ParticleSystem* scbParticleSystem = getScbParticleSystem(particleSystems[i]);
				if(scbParticleSystem->getFlags() & PxParticleBaseFlag::eENABLED)
					sendArrays(scbParticleSystem);
			}
		}
#endif

#if PX_USE_CLOTH_API
		{
			CM_PROFILE_ZONE_WITH_SUBSYSTEM( mScbScene,PVD,updateCloths);
			mMetaDataBinding.updateCloths( *mPvdConnection, *theScene );
		}
#endif
		// frame end moved to update contacts to have them in the previous frame.
	}
}


void SceneVisualDebugger::createPvdInstance()
{
	mPvdConnection->createInstance( mScbScene.getPxScene() );
	updatePvdProperties();
}


void SceneVisualDebugger::updatePvdProperties()
{
	PxScene* theScene = mScbScene.getPxScene();
	mMetaDataBinding.sendAllProperties( *mPvdConnection, *theScene  );
}


void SceneVisualDebugger::releasePvdInstance()
{
	mPvdConnection->destroyInstance(mScbScene.getPxScene());
}

template<typename TOperator>
inline void ActorTypeOperation( const PxActor* actor, TOperator op )
{
	switch( actor->getType() )
	{
	case PxActorType::eRIGID_STATIC: op( *static_cast<const PxRigidStatic*>( actor ) ); break;
	case PxActorType::eRIGID_DYNAMIC: op( *static_cast<const PxRigidDynamic*>( actor ) ); break;
	case PxActorType::eARTICULATION_LINK: op( *static_cast<const PxArticulationLink*>( actor ) ); break;
#if PX_USE_PARTICLE_SYSTEM_API
	case PxActorType::ePARTICLE_SYSTEM: op( *static_cast<const PxParticleSystem*>( actor ) ); break;
	case PxActorType::ePARTICLE_FLUID: op( *static_cast<const PxParticleFluid*>( actor ) ); break;
#endif
#if PX_USE_CLOTH_API
	case PxActorType::eCLOTH: op( *static_cast<const PxCloth*>( actor ) ); break;
#endif
	default:
		PX_ASSERT( false );
		break;
	};
}

struct CreateOp
{
	CreateOp& operator=( const CreateOp& );
	physx::debugger::comm::PvdDataStream&		mStream;
	PvdMetaDataBinding& mBinding;
	BufferRegistrar&	mRegistrar;
	PxScene&			mScene;
	CreateOp( physx::debugger::comm::PvdDataStream& str, PvdMetaDataBinding& bind, BufferRegistrar& reg, PxScene&		scene )
		: mStream( str ), mBinding( bind ), mRegistrar(reg), mScene( scene ) {}
	template<typename TDataType>
	void operator()( const TDataType& dtype ) {	mBinding.createInstance( mStream, dtype, mScene, mRegistrar ); }
	void operator()( const PxArticulationLink& link ) {}
#if PX_USE_PARTICLE_SYSTEM_API
	void operator()( const PxParticleSystem& dtype ) { mBinding.createInstance( mStream, dtype, mScene ); }
	void operator()( const PxParticleFluid& dtype ) { mBinding.createInstance( mStream, dtype, mScene ); }
#endif
};

struct UpdateOp
{
	CreateOp& operator=( const CreateOp& );
	physx::debugger::comm::PvdDataStream&		mStream;
	PvdMetaDataBinding& mBinding;
	UpdateOp( physx::debugger::comm::PvdDataStream& str, PvdMetaDataBinding& bind )
		: mStream( str ), mBinding( bind ) {}
	template<typename TDataType>
	void operator()( const TDataType& dtype ) {	mBinding.sendAllProperties( mStream, dtype ); }
};

struct DestroyOp
{
	CreateOp& operator=( const CreateOp& );
	physx::debugger::comm::PvdDataStream&		mStream;
	PvdMetaDataBinding& mBinding;
	PxScene&			mScene;
	DestroyOp( physx::debugger::comm::PvdDataStream& str, PvdMetaDataBinding& bind, PxScene&		scene )
		: mStream( str ), mBinding( bind ), mScene( scene ) {}
	template<typename TDataType>
	void operator()( const TDataType& dtype ) {	mBinding.destroyInstance( mStream, dtype, mScene ); }
	void operator()( const PxArticulationLink& dtype ) { mBinding.destroyInstance( mStream, dtype ); }
#if PX_USE_PARTICLE_SYSTEM_API
	void operator()( const PxParticleSystem& dtype ) { mBinding.destroyInstance( mStream, dtype, mScene ); }
	void operator()( const PxParticleFluid& dtype ) { mBinding.destroyInstance( mStream, dtype, mScene ); }
#endif
};

void SceneVisualDebugger::createPvdInstance(const PxActor* scbActor)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	PxScene* theScene = mScbScene.getPxScene();
	ActorTypeOperation( scbActor, CreateOp( *mPvdConnection, mMetaDataBinding, sdkPvd, *theScene ) );
}

void SceneVisualDebugger::updatePvdProperties(const PxActor* scbActor)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	//VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	//PxScene* theScene = mScbScene.getPxScene();
	ActorTypeOperation( scbActor, UpdateOp( *mPvdConnection, mMetaDataBinding ) );
}

void SceneVisualDebugger::releasePvdInstance(const PxActor* scbActor)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	//VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	PxScene* theScene = mScbScene.getPxScene();
	ActorTypeOperation( scbActor, DestroyOp( *mPvdConnection, mMetaDataBinding, *theScene ) );
	//The flush is here because when memory allocation systems have aggressive reuse policies
	//then we have issues.  The one case that reproduces some of those issues reliably is
	//SdkUnitTests --gtest_filter="*.createRandomConvex"
	//The address of the rigiddynamic is reused for the convex mesh.  Since the npphysics data stream
	//was flush but this wasn't the release mechanism for the rigiddynamic looked to PVD like we were
	//trying to remove shapes from a newly created convex mesh.
	//These considerations are extremely important where people are streaming information
	//into and out of a running simulation and they are very very difficult to keep correct.
	mPvdConnection->flush();
}


void SceneVisualDebugger::createPvdInstance(Scb::Actor* scbActor ) { createPvdInstance( getPxActor( scbActor ) ); }
void SceneVisualDebugger::updatePvdProperties(Scb::Actor* scbActor) { updatePvdProperties( getPxActor( scbActor ) ); }
void SceneVisualDebugger::releasePvdInstance(Scb::Actor* scbActor) { releasePvdInstance( getPxActor( scbActor ) ); }

template<typename TOperator>
inline void BodyTypeOperation( Scb::Body* scbBody, TOperator op )
{
	bool isArticulationLink = scbBody->getActorTypeSLOW() == PxActorType::eARTICULATION_LINK;
	if(isArticulationLink)
	{
		NpArticulationLink* link( getNpArticulationLink( scbBody ) );
		op( *static_cast<PxArticulationLink*>( link ) );
	}
	else
	{
		NpRigidDynamic* npRigidDynamic = getNpRigidDynamic(scbBody);
		op( *static_cast<PxRigidDynamic*>( npRigidDynamic ) );
	}
}

void SceneVisualDebugger::createPvdInstance(Scb::Body* scbBody)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	PxScene* theScene = mScbScene.getPxScene();
	bool isArticulationLink = scbBody->getActorTypeSLOW() == PxActorType::eARTICULATION_LINK;
	if ( !isArticulationLink )
		BodyTypeOperation( scbBody, CreateOp( *mPvdConnection, mMetaDataBinding, sdkPvd, *theScene ) );
}

void SceneVisualDebugger::updatePvdProperties(Scb::Body* scbBody)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	BodyTypeOperation( scbBody, UpdateOp( *mPvdConnection, mMetaDataBinding ) );
}

void SceneVisualDebugger::updateKinematicTarget( Scb::Body* scbBody, const PxTransform& target )
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	NpRigidDynamic* npRigidDynamic = getNpRigidDynamic(scbBody);
	mPvdConnection->setPropertyValue( npRigidDynamic, "KinematicTarget", target );
}

void SceneVisualDebugger::createPvdInstance(Scb::RigidStatic* scbRigidStatic)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return;
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	PxScene* theScene = mScbScene.getPxScene();
	PxRigidStatic* npRigidStatic = getNpRigidStatic(scbRigidStatic);
	mMetaDataBinding.createInstance( *mPvdConnection, *npRigidStatic, *theScene, sdkPvd );
}


void SceneVisualDebugger::updatePvdProperties(Scb::RigidStatic* scbRigidStatic)
{	
	UPDATE_PVD_PROPERTIES_CHECK();
	PxRigidStatic& rs( *getNpRigidStatic( scbRigidStatic ) );
	mMetaDataBinding.sendAllProperties( *mPvdConnection, rs );
}

void SceneVisualDebugger::releasePvdInstance(Scb::RigidObject* scbRigidObject) 
{ 
	releasePvdInstance( getPxActor( scbRigidObject ) ); 
}

void SceneVisualDebugger::createPvdInstance(const Scb::Shape* scbShape)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	PxShape* npShape = getNpShape(scbShape);
	PxActor& pxActor = getNpShape( scbShape)->getActorFast();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	mMetaDataBinding.createInstance( *mPvdConnection, *npShape,  static_cast<PxRigidActor&>( pxActor ), sdkPvd );
}

void SceneVisualDebugger::updatePvdProperties(const Scb::Shape* scbShape)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	mMetaDataBinding.sendAllProperties( *mPvdConnection, *getNpShape( const_cast<Scb::Shape*>( scbShape ) ) );
}


void SceneVisualDebugger::releaseAndRecreateGeometry( const Scb::Shape* scbShape )
{
	UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	mMetaDataBinding.releaseAndRecreateGeometry( *mPvdConnection, *getNpShape( const_cast<Scb::Shape*>( scbShape ) ), sdkPvd );
}

void SceneVisualDebugger::updateMaterials(const Scb::Shape* scbShape)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	mMetaDataBinding.updateMaterials( *mPvdConnection, *getNpShape( const_cast<Scb::Shape*>( scbShape ) ), sdkPvd );
}
void SceneVisualDebugger::releasePvdInstance(const Scb::Shape* scbShape)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	PxShape* npShape = getNpShape(scbShape);
	PxActor& pxActor = getNpShape( scbShape)->getActorFast();
	mMetaDataBinding.destroyInstance( *mPvdConnection, *npShape,  static_cast<PxRigidActor&>( pxActor ) );
}
void  SceneVisualDebugger::createPvdInstance(const Sc::MaterialCore* mat)//(const Scb::Material* scbMat)
{
	/*UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	const PxMaterial* theMaterial( scbMat->getNxMaterial() );
	sdkPvd.increaseReference( theMaterial );*/

	UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	const PxMaterial* theMaterial( mat->getNxMaterial() );
	sdkPvd.increaseReference( theMaterial );
}

void SceneVisualDebugger::updatePvdProperties(const Sc::MaterialCore* mat)//( const Scb::Material* material )
{
	/*UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	sdkPvd.updatePvdProperties( material->getNxMaterial() );*/
	UPDATE_PVD_PROPERTIES_CHECK();
	mMetaDataBinding.sendAllProperties( *mPvdConnection, *mat->getNxMaterial() );
}


void SceneVisualDebugger::releasePvdInstance(const Sc::MaterialCore* mat)//(const Scb::Material* scbMat)
{
	/*UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	sdkPvd.decreaseReference( scbMat->getNxMaterial() );*/
	UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	sdkPvd.decreaseReference( mat->getNxMaterial() );
}

void SceneVisualDebugger::createPvdInstance(Scb::Articulation* articulation)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	PxScene* theScene = mScbScene.getPxScene();
	NpArticulation* npArticulation = getNpArticulation(articulation);
	mMetaDataBinding.createInstance( *mPvdConnection, *npArticulation, *theScene, sdkPvd );
}


void SceneVisualDebugger::updatePvdProperties(Scb::Articulation* articulation)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	NpArticulation* npArticulation = getNpArticulation(articulation);
	mMetaDataBinding.sendAllProperties( *mPvdConnection, *npArticulation );
}


void SceneVisualDebugger::releasePvdInstance(Scb::Articulation* articulation)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	PxScene* theScene = mScbScene.getPxScene();
	NpArticulation* npArticulation = getNpArticulation(articulation);
	mMetaDataBinding.destroyInstance( *mPvdConnection, *npArticulation, *theScene );
}

void SceneVisualDebugger::updatePvdProperties(Scb::ArticulationJoint* articulationJoint)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	NpArticulationJoint* joint = getNpArticulationJoint( articulationJoint );
	mMetaDataBinding.sendAllProperties( *mPvdConnection, *joint );
}

#if PX_USE_PARTICLE_SYSTEM_API
void SceneVisualDebugger::createPvdInstance(Scb::ParticleSystem* scbParticleSys)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	createPvdInstance(getPxActor(scbParticleSys)); 
}

void SceneVisualDebugger::sendArrays(Scb::ParticleSystem* scbParticleSys)
{
	UPDATE_PVD_PROPERTIES_CHECK();

	NpParticleFluidReadData readData;
	PxU32 rdFlags = scbParticleSys->getParticleReadDataFlags();
	scbParticleSys->getScParticleSystem().getParticleReadData(readData);
	const PxActor* pxActor = getPxActor(scbParticleSys);
	PxActorType::Enum type = pxActor->getType();
	if ( type == PxActorType::ePARTICLE_SYSTEM )
		mMetaDataBinding.sendArrays( *mPvdConnection, *static_cast<const PxParticleSystem*>( pxActor ), readData, rdFlags );
	else if ( type == PxActorType::ePARTICLE_FLUID )
		mMetaDataBinding.sendArrays( *mPvdConnection, *static_cast<const PxParticleFluid*>( pxActor ), readData, rdFlags );
}

void SceneVisualDebugger::updatePvdProperties(Scb::ParticleSystem* scbParticleSys)
{
	updatePvdProperties( getPxActor(scbParticleSys) );
}

void SceneVisualDebugger::releasePvdInstance(Scb::ParticleSystem* scbParticleSys)
{
	releasePvdInstance( getPxActor(scbParticleSys) );
}
#endif // PX_USE_PARTICLE_SYSTEM_API

#if PX_USE_CLOTH_API
static inline PxCloth* toPx( Scb::Cloth* cloth )
{
	NpCloth* realCloth( backptr( cloth ) );
	PxActor* pxActor = &realCloth->getPxActorSLOW();
	return static_cast<PxCloth*>( pxActor );
}

void SceneVisualDebugger::createPvdInstance(Scb::Cloth* scbCloth)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	NpCloth* realCloth( backptr( scbCloth ) );
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	PxScene* theScene = mScbScene.getPxScene();
	mMetaDataBinding.createInstance( *mPvdConnection, *realCloth, *theScene, sdkPvd );
}

void SceneVisualDebugger::sendSimpleProperties( Scb::Cloth* cloth )
{
	if ( isConnectedAndSendingDebugInformation() )
		mMetaDataBinding.sendSimpleProperties( *mPvdConnection, *toPx( cloth ) );
}
void SceneVisualDebugger::sendMotionConstraints( Scb::Cloth* cloth )
{
	if ( isConnectedAndSendingDebugInformation() )
		mMetaDataBinding.sendMotionConstraints( *mPvdConnection, *toPx( cloth ) );
}
void SceneVisualDebugger::sendSeparationConstraints( Scb::Cloth* cloth )
{
	if ( isConnectedAndSendingDebugInformation() )
		mMetaDataBinding.sendSeparationConstraints( *mPvdConnection, *toPx( cloth ) );
}
void SceneVisualDebugger::sendCollisionSpheres( Scb::Cloth* cloth )
{
	if ( isConnectedAndSendingDebugInformation() )
		mMetaDataBinding.sendCollisionSpheres( *mPvdConnection, *toPx( cloth ) );
}
void SceneVisualDebugger::sendVirtualParticles( Scb::Cloth* cloth )
{
	if ( isConnectedAndSendingDebugInformation() )
		mMetaDataBinding.sendVirtualParticles( *mPvdConnection, *toPx( cloth ) );
}

void SceneVisualDebugger::releasePvdInstance(Scb::Cloth* cloth)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	mMetaDataBinding.destroyInstance( *mPvdConnection, *toPx(cloth), *mScbScene.getPxScene() );
}
#endif // PX_USE_CLOTH_API

bool SceneVisualDebugger::updateConstraint(const Sc::ConstraintCore& scConstraint, PxU32 updateType)
{
	if ( isConnectedAndSendingDebugInformation() == false ) return false;
	PxConstraintConnector* conn;
	bool success = false;
	if( (conn = scConstraint.getPxConnector()) != NULL
		&& isConnectedAndSendingDebugInformation() )
	{
		success = conn->updatePvdProperties(*mPvdConnection, scConstraint.getPxConstraint(), PxPvdUpdateType::Enum(updateType));
	}
	return success;
}


void SceneVisualDebugger::createPvdInstance(Scb::Constraint* constraint)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	updateConstraint(constraint->getScConstraint(), PxPvdUpdateType::CREATE_INSTANCE);
}


void SceneVisualDebugger::updatePvdProperties(Scb::Constraint* constraint)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	updateConstraint(constraint->getScConstraint(), PxPvdUpdateType::UPDATE_ALL_PROPERTIES);
}


void SceneVisualDebugger::releasePvdInstance(Scb::Constraint* constraint)
{
	UPDATE_PVD_PROPERTIES_CHECK();
	PxConstraintConnector* conn;
	Sc::ConstraintCore& scConstraint = constraint->getScConstraint();
	if( (conn = scConstraint.getPxConnector()) )
	{
		conn->updatePvdProperties(*mPvdConnection, scConstraint.getPxConstraint(), PxPvdUpdateType::RELEASE_INSTANCE);
	}
}

void SceneVisualDebugger::updateContacts()
{
	if(!isConnectedAndSendingDebugInformation())
		return;

	// if contacts are disabled, send empty array and return
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();
	const PxScene* theScene( mScbScene.getPxScene() );
	if(!sdkPvd.getTransmitContactsFlag())
	{
		mMetaDataBinding.sendContacts( *mPvdConnection, *theScene );
		return;
	}

	CM_PROFILE_ZONE_WITH_SUBSYSTEM( mScbScene,PVD,updateContacts );
	Sc::ContactIterator contactIter;
	mScbScene.getScScene().initContactsIterator(contactIter);
	mMetaDataBinding.sendContacts( *mPvdConnection, *theScene, contactIter );
}

void SceneVisualDebugger::visualize( PxArticulationLink& link )
{
#if PX_ENABLE_DEBUG_VISUALIZATION
	NpArticulationLink& npLink = static_cast<NpArticulationLink&>( link );
	const void* itemId = npLink.getInboundJoint();
	if ( itemId != NULL && mImmediateRenderer != NULL )
	{
		PvdConstraintVisualizer viz( itemId, *mImmediateRenderer );
		npLink.visualizeJoint( viz );
	}
#endif
}

void SceneVisualDebugger::updateJoints()
{
	if(!isConnected())
		return;
	
	VisualDebugger& sdkPvd = NpPhysics::getInstance().getPhysics()->getVisualDebugger();

	if(isConnectedAndSendingDebugInformation())
	{
		const bool visualizeJoints = sdkPvd.isVisualizingConstraints();
		if ( visualizeJoints && mImmediateRenderer == NULL )
		{
			NpPhysics& physics( NpPhysics::getInstance() );
			physx::debugger::comm::PvdConnectionManager& mgr( *physics.getPvdConnectionManager() );
			physx::debugger::comm::PvdConnection* connection( mgr.getAndAddRefCurrentConnection() );
			if ( connection )
			{
				mImmediateRenderer = &connection->createRenderer();
				mImmediateRenderer->addRef();
				connection->release();
			}
		}

		// joints
		{
			CM_PROFILE_ZONE_WITH_SUBSYSTEM( mScbScene,PVD,updateJoints );
			Sc::ConstraintIterator iterator;
			mScbScene.getScScene().initConstraintsIterator(iterator);
			Sc::ConstraintCore* constraint;
			PxI64 constraintCount = 0;

			for(constraint = iterator.getNext(); constraint; constraint = iterator.getNext())
			{
				PxPvdUpdateType::Enum updateType = getNpConstraint(constraint)->isDirty() ? PxPvdUpdateType::UPDATE_ALL_PROPERTIES : PxPvdUpdateType::UPDATE_SIM_PROPERTIES;
				updateConstraint(*constraint, updateType);
				PxConstraintConnector* conn = constraint->getPxConnector();
				//visualization is updated here
				{
					PxU32 typeId = 0;
					void* joint = NULL;
					if ( conn != NULL )
						joint = conn->getExternalReference( typeId );
					// visualize:
					Sc::ConstraintSim* sim = constraint->getSim();
					if(visualizeJoints 
						&& 
						sim 
						&& sim->getConstantsLL() 
						&& joint
						&& constraint->getVisualize()  )
					{
						Sc::BodySim* b0 = sim->getBody(0);
						Sc::BodySim* b1 = sim->getBody(1);
						PxTransform t0 = b0 ? b0->getBody2World() : PxTransform::createIdentity();
						PxTransform t1 = b1 ? b1->getBody2World() : PxTransform::createIdentity();
						PvdConstraintVisualizer viz( joint, *mImmediateRenderer );
						(*constraint->getVisualize())(viz, sim->getConstantsLL(), t0, t1, 0xffffFFFF);
					}
				}
				++constraintCount;
			}
			if ( mImmediateRenderer != NULL )
				mImmediateRenderer->flushRenderEvents();
			CM_PROFILE_VALUE( mScbScene,PVD,updateJoints, constraintCount );
		}
	}
}

} // namespace Pvd

}

#endif  // PX_SUPPORT_VISUAL_DEBUGGER
