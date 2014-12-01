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

#include "PxVisualDebuggerExt.h"
#include "ExtVisualDebugger.h"
#include "PxExtensionMetaDataObjects.h"

#include "ExtD6Joint.h"
#include "ExtFixedJoint.h"
#include "ExtSphericalJoint.h"
#include "ExtDistanceJoint.h"
#include "ExtSphericalJoint.h"
#include "ExtRevoluteJoint.h"
#include "ExtPrismaticJoint.h"
#include "ExtJointMetaDataExtensions.h"
#include "PvdMetaDataPropertyVisitor.h"
#include "PvdMetaDataDefineProperties.h"
#include "PvdObjectModelBaseTypes.h"
#include "PvdTypeNames.h"


namespace physx { namespace debugger {
	#define DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP( type ) DEFINE_PVD_TYPE_NAME_MAP( type, "physx3", #type )

	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxJoint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxJointGeneratedValues);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxFixedJoint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxFixedJointGeneratedValues);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxDistanceJoint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxDistanceJointGeneratedValues);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxPrismaticJoint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxPrismaticJointGeneratedValues);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxRevoluteJoint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxRevoluteJointGeneratedValues);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxSphericalJoint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxSphericalJointGeneratedValues);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxD6Joint);
	DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP(PxD6JointGeneratedValues);

#undef DEFINE_NATIVE_PVD_PHYSX3_TYPE_MAP

}}

namespace physx
{


namespace Ext
{
	using namespace Pvd;

	template<typename TObjType, typename TOperator>
	inline void visitPvdInstanceProperties( TOperator inOperator )
	{
		PxClassInfoTraits<TObjType>().Info.visitInstanceProperties( makePvdPropertyFilter( inOperator ), 0 );	
	}

	template<typename TObjType, typename TOperator>
	inline void visitPvdProperties( TOperator inOperator )
	{
		PvdPropertyFilter<TOperator> theFilter( makePvdPropertyFilter( inOperator ) );
		PxU32 thePropCount = PxClassInfoTraits<TObjType>().Info.visitBaseProperties( theFilter );
		PxClassInfoTraits<TObjType>().Info.visitInstanceProperties( theFilter, thePropCount );
	}

	using namespace Pvd;

	VisualDebugger::PvdNameSpace::PvdNameSpace(physx::debugger::comm::PvdDataStream& conn, const char* name)
		: mConnection(conn)
	{
	}

	VisualDebugger::PvdNameSpace::~PvdNameSpace()
	{
	}

	void VisualDebugger::releasePvdInstance(physx::debugger::comm::PvdDataStream& pvdConnection, const PxConstraint& c, const PxJoint& joint)
	{
		if(!pvdConnection.isConnected())
			return;
		//remove from scene and from any attached actors.
		PxRigidActor* actor0, *actor1;
		c.getActors( actor0, actor1 );
		pvdConnection.removeObjectRef( c.getScene(), "Joints", &joint );
		if ( actor0 ) pvdConnection.removeObjectRef( actor0, "Joints", &joint );
		if ( actor1 ) pvdConnection.removeObjectRef( actor1, "Joints", &joint );
		pvdConnection.destroyInstance(&joint);
	}

	template<typename TObjType>
	void registerProperties( PvdDataStream& inStream )
	{
		inStream.createClass<TObjType>();
		PvdPropertyDefinitionHelper& theHelper( inStream.getPropertyDefinitionHelper() );
		PvdClassInfoDefine theDefinitionObj( theHelper, getPvdNamespacedNameForType<TObjType>() );
		visitPvdInstanceProperties<TObjType>( theDefinitionObj );
	}

	template<typename TObjType, typename TValueStructType>
	void registerPropertiesAndValueStruct( PvdDataStream& inStream )
	{
		inStream.createClass<TObjType>();
		inStream.deriveClass<PxJoint,TObjType>();
		PvdPropertyDefinitionHelper& theHelper( inStream.getPropertyDefinitionHelper() );
		{
			PvdClassInfoDefine theDefinitionObj( theHelper, getPvdNamespacedNameForType<TObjType>() );
			visitPvdInstanceProperties<TObjType>( theDefinitionObj );
		}
		{
			PvdClassInfoValueStructDefine theDefinitionObj( theHelper );
			visitPvdProperties<TObjType>( theDefinitionObj );
			theHelper.addPropertyMessage<TObjType,TValueStructType>();
		}
	}

	void VisualDebugger::sendClassDescriptions(physx::debugger::comm::PvdDataStream& inStream)
	{
		{ //PxJoint
			registerProperties<PxJoint>( inStream );
			inStream.createProperty<PxJoint,ObjectRef>( "Parent", "parents" );
			registerPropertiesAndValueStruct<PxDistanceJoint,PxDistanceJointGeneratedValues>( inStream);
			registerPropertiesAndValueStruct<PxFixedJoint,PxFixedJointGeneratedValues>( inStream);
			registerPropertiesAndValueStruct<PxPrismaticJoint,PxPrismaticJointGeneratedValues>( inStream);
			registerPropertiesAndValueStruct<PxSphericalJoint,PxSphericalJointGeneratedValues>( inStream);
			registerPropertiesAndValueStruct<PxRevoluteJoint,PxRevoluteJointGeneratedValues>( inStream);
			registerPropertiesAndValueStruct<PxD6Joint,PxD6JointGeneratedValues>( inStream);
		}
	}
	
	void VisualDebugger::setActors( physx::debugger::comm::PvdDataStream& inStream, const PxJoint& inJoint, const PxConstraint& c, const PxActor* newActor0, const PxActor* newActor1 )
	{
		PxRigidActor* actor0, *actor1;
		c.getActors( actor0, actor1 );
		if ( actor0 )
			inStream.removeObjectRef( (PxActor*)actor0, "Joints", &inJoint );
		if ( actor1 )
			inStream.removeObjectRef( (PxActor*)actor1, "Joints", &inJoint );

		if ( newActor0 )
			inStream.pushBackObjectRef( newActor0, "Joints", &inJoint );
		if ( newActor1 )
			inStream.pushBackObjectRef( newActor1, "Joints", &inJoint );
		inStream.setPropertyValue( &inJoint, "Actors.actor0", (const void*)newActor0 );
		inStream.setPropertyValue( &inJoint, "Actors.actor1", (const void*)newActor1 );
		const void* parent = newActor0 ? (const void*)newActor0 : (const void*) newActor1;
		inStream.setPropertyValue( &inJoint, "Parent", parent );
	}

	template<typename TValueStructType, typename TObjType>
	void sendAllProperties( PvdDataStream& inStream, const TObjType& inSource )
	{
		TValueStructType theValueStruct( &inSource );
		inStream.setPropertyMessage( &inSource, theValueStruct );
	}

	template<typename TObjType>
	void createInstance( PvdDataStream& inStream, const PxConstraint& c, const TObjType& inSource )
	{
		const PxJoint* theJoint = &inSource;
		PxRigidActor* actor0, *actor1;
		c.getActors( actor0, actor1 );
		inStream.createInstance( &inSource );
		inStream.pushBackObjectRef( c.getScene(), "Joints", (const void*)theJoint );
		if ( actor0 )
			inStream.pushBackObjectRef( (PxActor*)actor0, "Joints", theJoint );
		if ( actor1 )
			inStream.pushBackObjectRef( (PxActor*)actor1, "Joints", theJoint );
		const void* parent = actor0 ? (const void*)actor0 : (const void*) actor1;
		inStream.setPropertyValue( theJoint, "Parent", parent );
	}
	
#define IMPLEMENT_JOINT_PVD_OPERATIONS( jointtype ) \
	void VisualDebugger::updatePvdProperties(physx::debugger::comm::PvdDataStream& pvdConnection, const jointtype& joint) { sendAllProperties<jointtype##GeneratedValues>( pvdConnection, joint ); }	\
	void VisualDebugger::simUpdate(physx::debugger::comm::PvdDataStream&, const jointtype&) {}																											\
	void VisualDebugger::createPvdInstance(physx::debugger::comm::PvdDataStream& pvdConnection, const PxConstraint& c, const jointtype& joint)															\
	{																																																	\
		createInstance( pvdConnection, c, joint );																																						\
	}

	IMPLEMENT_JOINT_PVD_OPERATIONS( PxD6Joint );
	IMPLEMENT_JOINT_PVD_OPERATIONS( PxDistanceJoint );
	IMPLEMENT_JOINT_PVD_OPERATIONS( PxFixedJoint );
	IMPLEMENT_JOINT_PVD_OPERATIONS( PxPrismaticJoint );
	IMPLEMENT_JOINT_PVD_OPERATIONS( PxRevoluteJoint );
	IMPLEMENT_JOINT_PVD_OPERATIONS( PxSphericalJoint );
}

}

#else

#include "CmPhysXCommon.h"
#include "PxVisualDebuggerExt.h"

namespace physx { namespace debugger {
	class PvdDataStream;
}}

#endif // PX_SUPPORT_VISUAL_DEBUGGER
