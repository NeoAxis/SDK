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

#include "PxPhysXCommon.h"
#include "PxProfileBase.h"

#include "PvdVisualDebugger.h"
#include "PvdDataStream.h"
#include "PvdConnection.h"

#include "ScPhysics.h"
#include "NpScene.h"
#include "PsFoundation.h"

#include "ScBodyCore.h"

#include "NpRigidDynamic.h"
#include "NpRigidStatic.h"
#include "PxHeightFieldSample.h"
#include "PvdTypeNames.h"

namespace physx
{
namespace Pvd
{

VisualDebugger::VisualDebugger()
: mPvdConnection(NULL)
, mPvdConnectionFactory(NULL)
, mConstraintVisualize(true)
, mFlags(0)
{
}


VisualDebugger::~VisualDebugger()
{
	disconnect();
}


void VisualDebugger::disconnect()
{
	NpPhysics& npPhysics = NpPhysics::getInstance();
	if ( npPhysics.getPvdConnectionManager() )
		npPhysics.getPvdConnectionManager()->disconnect();
}


physx::debugger::comm::PvdConnection* VisualDebugger::getPvdConnectionFactory()
{
	return mPvdConnectionFactory;
}

physx::debugger::comm::PvdDataStream* VisualDebugger::getPvdConnection(const PxScene& scene)
{
	const NpScene& npScene = static_cast<const NpScene&>(scene);
	return npScene.getScene().getSceneVisualDebugger().getPvdConnection();
}


void VisualDebugger::updateCamera(const char* name, const PxVec3& origin, const PxVec3& up, const PxVec3& target)
{
	NpPhysics& npPhysics = NpPhysics::getInstance();
	npPhysics.getPvdConnectionManager()->setCamera( name, origin, up, target );
}



void VisualDebugger::setVisualizeConstraints( bool inVisualizeJoints )
{
	mConstraintVisualize = inVisualizeJoints;
}
bool VisualDebugger::isVisualizingConstraints()
{
	return mConstraintVisualize;
}

void VisualDebugger::setVisualDebuggerFlag(PxVisualDebuggerFlags::Enum flag, bool value)
{
	if(value)
		mFlags |= PxU32(flag);
	else
		mFlags &= ~PxU32(flag);
	//This has been a bug against the debugger for some time,
	//changing this flag doesn't always change the sending-contact-reports behavior.
	if ( flag == PxVisualDebuggerFlags::eTRANSMIT_CONTACTS )
	{
		if ( isConnected() )
		{
			NpPhysics& npPhysics = NpPhysics::getInstance();
			PxU32 numScenes = npPhysics.getNbScenes();
			for(PxU32 i = 0; i < numScenes; i++)
			{
				NpScene* npScene = npPhysics.getScene(i);
				Scb::Scene& scbScene = npScene->getScene();
				scbScene.getSceneVisualDebugger().setCreateContactReports(value);
			}
		}
	}
}

PxU32 VisualDebugger::getVisualDebuggerFlags()
{
	return mFlags;
}


bool VisualDebugger::isConnected()
{ 
	return mPvdConnectionFactory && mPvdConnectionFactory->isConnected(); 
}


void VisualDebugger::checkConnection()
{
	if ( mPvdConnectionFactory != NULL ) mPvdConnectionFactory->checkConnection();
}


void VisualDebugger::updateScenesPvdConnection()
{
	NpPhysics& npPhysics = NpPhysics::getInstance();
	PxU32 numScenes = npPhysics.getNbScenes();
	for(PxU32 i = 0; i < numScenes; i++)
	{
		NpScene* npScene = npPhysics.getScene(i);
		Scb::Scene& scbScene = npScene->getScene();
		setupSceneConnection(scbScene);
	}
}

void VisualDebugger::setupSceneConnection(Scb::Scene& s)
{
	physx::debugger::comm::PvdDataStream* conn = mPvdConnectionFactory ? &mPvdConnectionFactory->createDataStream() : NULL;
	if(conn)
	s.getSceneVisualDebugger().setPvdConnection(conn, mPvdConnectionFactory ? mPvdConnectionFactory->getConnectionType() : physx::debugger::TConnectionFlagsType(0));
	s.getSceneVisualDebugger().setCreateContactReports(conn ? getTransmitContactsFlag() : false);
}


void VisualDebugger::sendClassDescriptions()
{
	if(!isConnected())
		return;
	mMetaDataBinding.registerSDKProperties(*mPvdConnection);
	mPvdConnection->flush();
}

void VisualDebugger::onPvdSendClassDescriptions( physx::debugger::comm::PvdConnection& inFactory )
{
	physx::debugger::comm::PvdConnection* cf( &inFactory );
	if(!cf)
		return;

	if(mPvdConnection && mPvdConnectionFactory)
		disconnect();

	mPvdConnectionFactory = cf;
	mPvdConnection = &mPvdConnectionFactory->createDataStream();
	if(!mPvdConnection)
		return;
	mPvdConnection->addRef();
	sendClassDescriptions();
}

void VisualDebugger::onPvdConnected( physx::debugger::comm::PvdConnection& inFactory )
{
	if(!mPvdConnection)
		return;
	updateScenesPvdConnection();
	sendEntireSDK();
}
void VisualDebugger::onPvdDisconnected( physx::debugger::comm::PvdConnection& inFactory )
{
	if(mPvdConnection)
	{
//		NpPhysics& npPhysics = NpPhysics::getInstance();
		if(mPvdConnection->isConnected())
		{
			mPvdConnection->destroyInstance(&NpPhysics::getInstance());
			mPvdConnection->flush();
		}
		mPvdConnection->release();
		mPvdConnection = NULL;
		mPvdConnectionFactory = NULL;
		updateScenesPvdConnection();
		mRefCountMapLock.lock();
		mRefCountMap.clear();
		mRefCountMapLock.unlock();
	}
}

template<typename TDataType> void VisualDebugger::doMeshFactoryBufferRelease( TDataType& type )
{
	if ( mPvdConnectionFactory != NULL 
		&& mPvdConnection != NULL
		&& mPvdConnectionFactory->getConnectionType() & physx::debugger::PvdConnectionType::Debug )
	{
		Ps::Mutex::ScopedLock locker(mRefCountMapLock);
		if ( mRefCountMap.find( &type ) )
		{
			mRefCountMap.erase( &type );
			destroyPvdInstance( &type );
			mPvdConnection->flush();
		}
	}
}

void VisualDebugger::onGuMeshFactoryBufferRelease(PxConvexMesh& data)
{
	doMeshFactoryBufferRelease( data );
}
void VisualDebugger::onGuMeshFactoryBufferRelease(PxHeightField& data)
{
	doMeshFactoryBufferRelease( data );
}
void VisualDebugger::onGuMeshFactoryBufferRelease(PxTriangleMesh& data)
{
	doMeshFactoryBufferRelease( data );
}
#if PX_USE_CLOTH_API
void VisualDebugger::onNpFactoryBufferRelease(PxClothFabric& data)
{
	doMeshFactoryBufferRelease( data );
}
#endif

void VisualDebugger::sendEntireSDK()
{
	NpPhysics& npPhysics = NpPhysics::getInstance();
	mPvdConnection->createInstance( (PxPhysics*)&npPhysics );
	npPhysics.getPvdConnectionManager()->setIsTopLevelUIElement( &npPhysics, true );
	mMetaDataBinding.sendAllProperties( *mPvdConnection, npPhysics );

#define SEND_BUFFER_GROUP( type, name ) {					\
		Ps::Array<type*> buffers;							\
		PxU32 numBuffers = npPhysics.getNb##name();			\
		buffers.resize(numBuffers);							\
		npPhysics.get##name(buffers.begin(), numBuffers);	\
		for(PxU32 i = 0; i < numBuffers; i++)				\
			increaseReference(buffers[i]);					\
	}

	SEND_BUFFER_GROUP( PxMaterial, Materials );
	SEND_BUFFER_GROUP( PxTriangleMesh, TriangleMeshes );
	SEND_BUFFER_GROUP( PxConvexMesh, ConvexMeshes );
	SEND_BUFFER_GROUP( PxHeightField, HeightFields );

#if PX_USE_CLOTH_API
	SEND_BUFFER_GROUP( PxClothFabric, ClothFabrics );
#endif

	mPvdConnection->flush();
	PxU32 numScenes = npPhysics.getNbScenes();
	for(PxU32 i = 0; i < numScenes; i++)
	{
		NpScene* npScene = npPhysics.getScene(i);
		Scb::Scene& scbScene = npScene->getScene();

		scbScene.getSceneVisualDebugger().sendEntireScene();
	}
}


void VisualDebugger::createPvdInstance(const PxTriangleMesh* triMesh)
{
	mMetaDataBinding.createInstance( *mPvdConnection, *triMesh, NpPhysics::getInstance() );
}

void VisualDebugger::destroyPvdInstance(const PxTriangleMesh* triMesh)
{
	mMetaDataBinding.destroyInstance( *mPvdConnection, *triMesh, NpPhysics::getInstance() );
}


void VisualDebugger::createPvdInstance(const PxConvexMesh* convexMesh)
{
	mMetaDataBinding.createInstance( *mPvdConnection, *convexMesh, NpPhysics::getInstance() );
}

void VisualDebugger::destroyPvdInstance(const PxConvexMesh* convexMesh)
{
	mMetaDataBinding.destroyInstance( *mPvdConnection, *convexMesh, NpPhysics::getInstance() );
}

void VisualDebugger::createPvdInstance(const PxHeightField* heightField)
{
	mMetaDataBinding.createInstance( *mPvdConnection, *heightField, NpPhysics::getInstance() );
}

void VisualDebugger::destroyPvdInstance(const PxHeightField* heightField)
{
	mMetaDataBinding.destroyInstance( *mPvdConnection, *heightField, NpPhysics::getInstance() );
}

void VisualDebugger::createPvdInstance(const PxMaterial* mat)
{
	mMetaDataBinding.createInstance( *mPvdConnection, *mat, NpPhysics::getInstance() );
}

void VisualDebugger::updatePvdProperties(const PxMaterial* mat)
{
	mMetaDataBinding.sendAllProperties( *mPvdConnection, *mat );
}

void VisualDebugger::destroyPvdInstance(const PxMaterial* mat)
{
	mMetaDataBinding.destroyInstance( *mPvdConnection, *mat, NpPhysics::getInstance() );
}

#if PX_USE_CLOTH_API
void VisualDebugger::createPvdInstance(const PxClothFabric* fabric)
{
	NpPhysics* npPhysics = &NpPhysics::getInstance();
	mMetaDataBinding.createInstance( *mPvdConnection, *fabric, *npPhysics );
}

void VisualDebugger::destroyPvdInstance(const PxClothFabric* fabric)
{
	NpPhysics* npPhysics = &NpPhysics::getInstance();
	mMetaDataBinding.destroyInstance( *mPvdConnection, *fabric, *npPhysics );
}
#endif

void VisualDebugger::flush() 
{
	mPvdConnection->flush();
}


} // namespace Pvd

}

#endif  // PX_SUPPORT_VISUAL_DEBUGGER
