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
#include "PxJointRepXExtensions.h"
#include "PxExtensionMetaDataObjects.h"
#include "ExtJointMetaDataExtensions.h"
#include "RepXCoreExtensions.h"
#include "PxMetaDataObjects.h"
#include "PxProfileFoundationWrapper.h"
#include "RepXExtensionImpl.h"
#include "PsUtilities.h"

namespace physx { namespace repx {

	template<typename TJointType>
	inline TJointType* createJoint( PxPhysics& physics, 
									   PxRigidActor* actor0, const PxTransform& localFrame0, 
									   PxRigidActor* actor1, const PxTransform& localFrame1 )
	{
		return NULL;
	}

	template<>
	inline PxD6Joint*			createJoint<PxD6Joint>(PxPhysics& physics, 
										PxRigidActor* actor0, const PxTransform& localFrame0, 
										PxRigidActor* actor1, const PxTransform& localFrame1)
	{
		return PxD6JointCreate( physics, actor0, localFrame0, actor1, localFrame1 );
	}

	template<>
	inline PxDistanceJoint*	createJoint<PxDistanceJoint>(PxPhysics& physics, 
									 		  PxRigidActor* actor0, const PxTransform& localFrame0, 
											  PxRigidActor* actor1, const PxTransform& localFrame1)
	{
		return PxDistanceJointCreate( physics, actor0, localFrame0, actor1, localFrame1 );
	}

	template<>
	inline PxFixedJoint*		createJoint<PxFixedJoint>(PxPhysics& physics, 
										   PxRigidActor* actor0, const PxTransform& localFrame0, 
										   PxRigidActor* actor1, const PxTransform& localFrame1)
	{
		return PxFixedJointCreate( physics, actor0, localFrame0, actor1, localFrame1 );
	}

	template<>
	inline PxPrismaticJoint*	createJoint<PxPrismaticJoint>(PxPhysics& physics, 
											   PxRigidActor* actor0, const PxTransform& localFrame0, 
											   PxRigidActor* actor1, const PxTransform& localFrame1)
	{
		return PxPrismaticJointCreate( physics, actor0, localFrame0, actor1, localFrame1 );
	}

	template<>
	inline PxRevoluteJoint*	createJoint<PxRevoluteJoint>(PxPhysics& physics, 
											  PxRigidActor* actor0, const PxTransform& localFrame0, 
											  PxRigidActor* actor1, const PxTransform& localFrame1)
	{
		return PxRevoluteJointCreate( physics, actor0, localFrame0, actor1, localFrame1 );
	}

	template<>
	inline PxSphericalJoint* createJoint<PxSphericalJoint>(PxPhysics& physics, 
											   PxRigidActor* actor0, const PxTransform& localFrame0, 
											   PxRigidActor* actor1, const PxTransform& localFrame1)
	{
		return PxSphericalJointCreate( physics, actor0, localFrame0, actor1, localFrame1 );
	}

	template<typename TJointType>
	struct PxJointExtension : RepXExtensionImpl<TJointType>
	{
		PxJointExtension( PxAllocatorCallback& inAllocator ) : RepXExtensionImpl<TJointType>( inAllocator ) {}

		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			PxRigidActor* actor0 = NULL;
			PxRigidActor* actor1 = NULL;
			PxTransform localPose0 = PxTransform::createIdentity();
			PxTransform localPose1 = PxTransform::createIdentity();
			bool ok = true;
			if ( inReader.gotoChild( "Actors" ) )
			{
				ok = readReference<PxRigidActor>( inReader, *inIdMap, "actor0", actor0 );
				ok &= readReference<PxRigidActor>( inReader, *inIdMap, "actor1", actor1 );
				inReader.leaveChild();
			}
			TJointType* theJoint = !ok ? NULL : createJoint<TJointType>( *inArgs.mPhysics, actor0, localPose0, actor1, localPose1 );
			if ( theJoint )
				fileToObjectImpl( theJoint, inReader, inAllocator, inArgs, inIdMap );
			return createRepXObject( theJoint );
		}

		virtual TJointType* allocateObject( RepXInstantiationArgs& ) { return NULL; }
	};

	
	template<typename TObjType>
	struct SpecificExtensionAllocator : ExtensionAllocator
	{
		static RepXExtension* specific_allocator( PxAllocatorCallback& inCallback) { return PX_PROFILE_NEW(inCallback, TObjType)( inCallback ); }
		SpecificExtensionAllocator() : ExtensionAllocator( specific_allocator ) {}
	};

	static ExtensionAllocator gAllocators[] = 
	{
		SpecificExtensionAllocator<PxJointExtension<PxFixedJoint> >(),
		SpecificExtensionAllocator<PxJointExtension<PxDistanceJoint> >(),
		SpecificExtensionAllocator<PxJointExtension<PxD6Joint> >(),
		SpecificExtensionAllocator<PxJointExtension<PxPrismaticJoint> >(),
		SpecificExtensionAllocator<PxJointExtension<PxRevoluteJoint> >(),
		SpecificExtensionAllocator<PxJointExtension<PxSphericalJoint> >(),
	};
	
	static PxU32 gAllocatorCount = PX_ARRAY_SIZE(gAllocators);

	PxU32 getNumJointExtensions() { return gAllocatorCount; }
	PxU32 createJointExtensions( RepXExtension** outExtensions, PxU32 outBufferSize, PxAllocatorCallback& inCallback )
	{
		PxU32 numCreated = PxMin( outBufferSize, gAllocatorCount );
		for ( PxU32 idx = 0; idx < numCreated; ++idx )
			outExtensions[idx] = gAllocators[idx].allocateExtension(inCallback);
		return numCreated;
	}

} }
