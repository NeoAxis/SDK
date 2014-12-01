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


#ifndef PVD_VISUALDEBUGGER_H
#define PVD_VISUALDEBUGGER_H

#if PX_SUPPORT_VISUAL_DEBUGGER

#include "PsUserAllocated.h"
#include "PxVisualDebugger.h"
#include "PvdConnectionManager.h"
#include "CmPhysXCommon.h"
#include "PxMetaDataPvdBinding.h"
#include "NpFactory.h"
#include "PxPhysX.h"

namespace physx
{

namespace Scb
{
	class Scene;
	class Body;
	class RigidStatic;
	class RigidObject;
	class Shape;
}
class PxGeometry;

namespace Sc
{
	class RigidCore;
}
class PxScene;
class PxTriangleMesh;
class PxConvexMesh;
class PxHeightField;
class PxClothFabric;

namespace Pvd
{

	class PvdMetaDataBinding;

struct SdkGroups
{
	enum Enum
	{
		Scenes,
		TriangleMeshes,
		ConvexMeshes,
		HeightFields,
		ClothFabrics,
		NUM_ELEMENTS,
	};
};


//////////////////////////////////////////////////////////////////////////
/*!
RemoteDebug supplies functionality for writing debug info to a stream
to be read by the remote debugger application.
*/
//////////////////////////////////////////////////////////////////////////
class VisualDebugger : public PxVisualDebugger, public Ps::UserAllocated, public physx::debugger::comm::PvdConnectionHandler, public Pvd::BufferRegistrar, public NpFactoryListener
{
public:
	VisualDebugger ();
	virtual ~VisualDebugger ();
	virtual void disconnect();
	virtual physx::debugger::comm::PvdConnection* getPvdConnectionFactory();
	virtual physx::debugger::comm::PvdDataStream* getPvdConnection(const PxScene& scene);
	virtual void setVisualizeConstraints( bool inViz );
	virtual bool isVisualizingConstraints();
	virtual void updateCamera(const char* name, const PxVec3& origin, const PxVec3& up, const PxVec3& target);
	virtual void setVisualDebuggerFlag(PxVisualDebuggerFlags::Enum flag, bool value);
	virtual PxU32 getVisualDebuggerFlags();

	// internal methods
	void sendClassDescriptions();
	bool isConnected();
	void checkConnection();
	void updateScenesPvdConnection();
	void setupSceneConnection(Scb::Scene& s);

	void sendEntireSDK();
	void createGroups();
	void releaseGroups();

	template<typename TDataType>
	inline void increaseReference(const TDataType* inItem)		{ if(incRef(inItem) == 1) { createPvdInstance(inItem); flush(); } }

	template<typename TDataType>
	inline void decreaseReference(const TDataType* inItem)		{ if(decRef(inItem) == 0) { destroyPvdInstance(inItem); flush(); } }

	PX_FORCE_INLINE bool	getTransmitContactsFlag()							{ return mFlags & PxVisualDebuggerFlags::eTRANSMIT_CONTACTS; }
	

	static PX_FORCE_INLINE const char* getPhysxNamespace() { return "physx3"; }

	void updatePvdProperties(const PxMaterial* mat);

	// physx::debugger::PvdConnectionHandler
	virtual void onPvdSendClassDescriptions( physx::debugger::comm::PvdConnection& inFactory );
	virtual void onPvdConnected( physx::debugger::comm::PvdConnection& inFactory );
	virtual void onPvdDisconnected( physx::debugger::comm::PvdConnection& inFactory );
	// physx::debugger::PvdConnectionHandler

	//Pvd::BufferRegistrar
	virtual void addRef( const PxMaterial* buffer ) { increaseReference( buffer ); }
	virtual void addRef( const PxConvexMesh* buffer ) { increaseReference( buffer ); }
	virtual void addRef( const PxTriangleMesh* buffer ) { increaseReference( buffer ); }
	virtual void addRef( const PxHeightField* buffer ) { increaseReference( buffer ); }
#if PX_USE_CLOTH_API
	virtual void addRef( const PxClothFabric* buffer ) { increaseReference( buffer ); }
#endif
	//Pvd::BufferRegistrar

	
	//NpFactoryListener
	virtual void onGuMeshFactoryBufferRelease(PxConvexMesh& data);
	virtual void onGuMeshFactoryBufferRelease(PxHeightField& data);
	virtual void onGuMeshFactoryBufferRelease(PxTriangleMesh& data);
#if PX_USE_CLOTH_API
	virtual void onNpFactoryBufferRelease(PxClothFabric& data);
#endif
	///NpFactoryListener
	
private:
	template<typename TDataType> void doMeshFactoryBufferRelease( TDataType& type );
	void createPvdInstance(const PxTriangleMesh* triMesh);
	void destroyPvdInstance(const PxTriangleMesh* triMesh);
	void createPvdInstance(const PxConvexMesh* convexMesh);
	void destroyPvdInstance(const PxConvexMesh* convexMesh);
	void createPvdInstance(const PxHeightField* heightField);
	void destroyPvdInstance(const PxHeightField* heightField);
	void createPvdInstance(const PxMaterial* mat);
	void destroyPvdInstance(const PxMaterial* mat);
#if PX_USE_CLOTH_API
	void createPvdInstance(const PxClothFabric* fabric);
	void destroyPvdInstance(const PxClothFabric* fabric);
#endif
	void flush();

	PX_FORCE_INLINE PxU32 incRef(const void* ptr);
	PX_FORCE_INLINE PxU32 decRef(const void* ptr);


	physx::debugger::comm::PvdDataStream*				mPvdConnection;
	physx::debugger::comm::PvdConnection*				mPvdConnectionFactory;
	PvdMetaDataBinding				mMetaDataBinding;

	Ps::HashMap<const void*, PxU32>	mRefCountMap;
	Ps::Mutex						mRefCountMapLock;

	bool							mConstraintVisualize;
	PxU32							mFlags;
};


PX_FORCE_INLINE PxU32 VisualDebugger::incRef(const void* ptr)
{
	Ps::Mutex::ScopedLock lock(mRefCountMapLock);

	if(mRefCountMap.find(ptr))
	{
		PxU32& counter = mRefCountMap[ptr];
		counter++;
		return counter;
	}
	else
	{
		mRefCountMap.insert(ptr, 1);
		return 1;
	}
}

PX_FORCE_INLINE PxU32 VisualDebugger::decRef(const void* ptr)
{
	Ps::Mutex::ScopedLock lock(mRefCountMapLock);
	const Ps::HashMap<const void*, PxU32>::Entry* entry = mRefCountMap.find(ptr);
	if ( entry )
	{
		PxU32& retval( const_cast<PxU32&>( entry->second ) );
		if ( retval )
			--retval;
		PxU32 theValue = retval;
		if ( !theValue )
			mRefCountMap.erase( ptr );
		return theValue;
	}
	return PX_MAX_U32;
}


} // namespace Pvd

}

#endif // PX_SUPPORT_VISUAL_DEBUGGER

#endif // VISUALDEBUGGER_H

