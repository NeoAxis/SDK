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

#include "PxExtensionsAPI.h"
#include "PsFoundation.h"
#include "CmMetaData.h"
#include "CmSerialFramework.h"
#include "CmStringTable.h"
#include "PxGaussMapLimit.h"
#include "ExtDistanceJoint.h"
#include "ExtD6Joint.h"
#include "ExtFixedJoint.h"
#include "ExtPrismaticJoint.h"
#include "ExtRevoluteJoint.h"
#include "ExtSphericalJoint.h"
#include "PxIO.h"
#include "PxCoreUtilities.h"
#include "CmIO.h"
#include <stdio.h>

#if PX_SUPPORT_VISUAL_DEBUGGER
#include "PvdConnectionManager.h"
#include "ExtVisualDebugger.h"
#include "PvdConnection.h"
#include "PvdDataStream.h"
#endif

using namespace physx;
using namespace Ps;
using namespace physx::debugger;
using namespace physx::debugger::comm;


#if PX_SUPPORT_VISUAL_DEBUGGER
struct JointConnectionHandler : public physx::debugger::comm::PvdConnectionHandler
{
	virtual void onPvdSendClassDescriptions( physx::debugger::comm::PvdConnection& inFactory )
	{
		using namespace physx::debugger;
		//register the joint classes.
		PvdDataStream* connection = &inFactory.createDataStream();
		connection->addRef();
		Ext::VisualDebugger::sendClassDescriptions( *connection );
		connection->flush();
		connection->release();
	}
	virtual void onPvdConnected( physx::debugger::comm::PvdConnection& )
	{
	}
	virtual void onPvdDisconnected( physx::debugger::comm::PvdConnection&)
	{
	}
};

static JointConnectionHandler gPvdHandler;
#endif

bool PxInitExtensions(PxPhysics& physics)
{
	PX_ASSERT(static_cast<Ps::Foundation*>(&physics.getFoundation()) == &Ps::Foundation::getInstance());
	Ps::Foundation::incRefCount();

	physics.registerClass(PxConcreteType::eUSER_SPHERICAL_JOINT,	Ext::SphericalJoint::createInstance);
	physics.registerClass(PxConcreteType::eUSER_REVOLUTE_JOINT,	Ext::RevoluteJoint::createInstance);
	physics.registerClass(PxConcreteType::eUSER_DISTANCE_JOINT,	Ext::DistanceJoint::createInstance);
	physics.registerClass(PxConcreteType::eUSER_D6_JOINT,			Ext::D6Joint::createInstance);
	physics.registerClass(PxConcreteType::eUSER_PRISMATIC_JOINT,	Ext::PrismaticJoint::createInstance);
	physics.registerClass(PxConcreteType::eUSER_FIXED_JOINT,		Ext::FixedJoint::createInstance);
#if PX_SUPPORT_VISUAL_DEBUGGER
	if ( physics.getPvdConnectionManager() != NULL )
		physics.getPvdConnectionManager()->addHandler( gPvdHandler );
#endif
	return true;
}

void PxCloseExtensions(void)
{
	Ps::Foundation::decRefCount();
}

void PxRegisterExtJointMetaData(PxSerialStream& stream);
void PxDumpMetaData(PxOutputStream& stream, const PxPhysics& sdk)
{
	class MetaDataStream : public PxOutputStream
	{
		public:
		virtual	PxU32 write(const void* src, PxU32 count)
		{
			PX_ASSERT(count==sizeof(Cm::MetaDataEntry));
			const Cm::MetaDataEntry* entry = (const Cm::MetaDataEntry*)src;
			metaData.pushBack(*entry);
			return count;
		}		
		Array<Cm::MetaDataEntry> metaData;
	}s;

	PxGetSDKMetaData(sdk, s);

	Cm::OutputStreamWriter writer(s);
	Cm::LegacySerialStream legacyStream(writer);
	PxRegisterExtJointMetaData(legacyStream);

	physx::shdfnd::Array<char>	stringTable;

	PxU32 nb = s.metaData.size();
	Cm::MetaDataEntry* entries = s.metaData.begin();
	for(PxU32 i=0;i<nb;i++)
	{
		entries[i].mType = (const char*)size_t(Cm::addToStringTable(stringTable, entries[i].mType));
		entries[i].mName = (const char*)size_t(Cm::addToStringTable(stringTable, entries[i].mName));
	}

	PxU32 platformTag = 0;
#ifdef PX_X64
	platformTag = PX_MAKE_FOURCC('P','C','6','4');
	const PxU32 gaussMapLimit = PxGetGaussMapVertexLimitForPlatform(PxPlatform::ePC);
	const PxU32 tiledHeightFieldSamples = 0;
#endif
#if defined(PX_X86) || defined(__CYGWIN__)
	platformTag = PX_MAKE_FOURCC('P','C','3','2');
	const PxU32 gaussMapLimit = PxGetGaussMapVertexLimitForPlatform(PxPlatform::ePC);
	const PxU32 tiledHeightFieldSamples = 0;
#endif
#ifdef PX_X360
	platformTag = PX_MAKE_FOURCC('X','B','O','X');
	const PxU32 gaussMapLimit = PxGetGaussMapVertexLimitForPlatform(PxPlatform::eXENON);
	const PxU32 tiledHeightFieldSamples = 0;
#endif
#ifdef PX_PS3
	platformTag = PX_MAKE_FOURCC('P','S','_','3');
	const PxU32 gaussMapLimit = PxGetGaussMapVertexLimitForPlatform(PxPlatform::ePLAYSTATION3);
	const PxU32 tiledHeightFieldSamples = 1;
#endif
#ifdef PX_ARM
	platformTag = PX_MAKE_FOURCC('A','R','M',' ');
	const PxU32 gaussMapLimit = PxGetGaussMapVertexLimitForPlatform(PxPlatform::eARM);
	const PxU32 tiledHeightFieldSamples = 0;
#endif

	const PxU32 header = PX_MAKE_FOURCC('M','E','T','A');
	const PxU32 version = PX_PHYSICS_VERSION;
	const PxU32 ptrSize = sizeof(void*);
	stream.write(&header, 4);
	stream.write(&version, 4);
	stream.write(&ptrSize, 4);
	stream.write(&platformTag, 4);
	stream.write(&gaussMapLimit, 4);
	stream.write(&tiledHeightFieldSamples, 4);
	
	stream.write(&nb, 4);
	stream.write(entries, nb*sizeof(Cm::MetaDataEntry));

	PxU32 length = stringTable.size();
	const char* table = stringTable.begin();
	stream.write(&length, 4);
	stream.write(table, length);
}

// PT: TODO: move those functions to a separate file, remove all allocations

#include "PxConvexMesh.h"
#include "PxTriangleMesh.h"
#include "PxHeightField.h"
#include "PxMaterial.h"
#include "cloth/PxClothFabric.h"

void PxCollectForExportSDK(const PxPhysics& physics, PxCollection& collection)
{
	// Collect convexes
	{
		Ps::Array<PxConvexMesh*> objects(physics.getNbConvexMeshes());
		const PxU32 nb = physics.getConvexMeshes(objects.begin(), objects.size());
		PX_ASSERT(nb == objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}

	// Collect triangle meshes
	{
		Ps::Array<PxTriangleMesh*> objects(physics.getNbTriangleMeshes());
		const PxU32 nb = physics.getTriangleMeshes(objects.begin(), objects.size());

		PX_ASSERT(nb == objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}

	// Collect heightfields
	{
		Ps::Array<PxHeightField*> objects(physics.getNbHeightFields());
		const PxU32 nb = physics.getHeightFields(objects.begin(), objects.size());

		PX_ASSERT(nb == objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}

	// Collect materials
	{
		Ps::Array<PxMaterial*> objects(physics.getNbMaterials());
		const PxU32 nb = physics.getMaterials(objects.begin(), objects.size());

		PX_ASSERT(nb == objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}

#if PX_USE_CLOTH_API
	// Collect cloth fabrics
	{
		Ps::Array<PxClothFabric*> objects(physics.getNbClothFabrics());
		const PxU32 nb = physics.getClothFabrics(objects.begin(), objects.size());

		PX_ASSERT(nb == objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}
#endif
}

#include "PxScene.h"
#include "PxArticulation.h"
#include "PxAggregate.h"
void PxCollectForExportScene(const PxScene& scene, PxCollection& collection)
{
	// Collect actors
	{
		const PxActorTypeSelectionFlags selectionFlags = PxActorTypeSelectionFlag::eRIGID_STATIC
														|PxActorTypeSelectionFlag::eRIGID_DYNAMIC
#if PX_USE_PARTICLE_SYSTEM_API
														|PxActorTypeSelectionFlag::ePARTICLE_SYSTEM
														|PxActorTypeSelectionFlag::ePARTICLE_FLUID
#endif
#if PX_USE_CLOTH_API
														|PxActorTypeSelectionFlag::eCLOTH
#endif
														;

		Ps::Array<PxActor*> objects(scene.getNbActors(selectionFlags));
		const PxU32 nb = scene.getActors(selectionFlags, objects.begin(), objects.size());

		PX_ASSERT(nb==objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}


	// Collect constraints
	{
		Ps::Array<PxConstraint*> objects(scene.getNbConstraints());
		const PxU32 nb = scene.getConstraints(objects.begin(), objects.size());

		PX_ASSERT(nb==objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
		{
			PxU32 typeId;
			PxJoint* joint = reinterpret_cast<PxJoint*>(objects[i]->getExternalReference(typeId));
			if(typeId == PxConstraintExtIDs::eJOINT)
				joint->collectForExport(collection);
		}
	}

	// Collect articulations
	{
		Ps::Array<PxArticulation*> objects(scene.getNbArticulations());
		const PxU32 nb = scene.getArticulations(objects.begin(), objects.size());

		PX_ASSERT(nb==objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}

	// Collect aggregates
	{
		Ps::Array<PxAggregate*> objects(scene.getNbAggregates());
		const PxU32 nb = scene.getAggregates(objects.begin(), objects.size());

		PX_ASSERT(nb==objects.size());
		PX_UNUSED(nb);

		for(PxU32 i=0;i<objects.size();i++)
			objects[i]->collectForExport(collection);
	}
}
