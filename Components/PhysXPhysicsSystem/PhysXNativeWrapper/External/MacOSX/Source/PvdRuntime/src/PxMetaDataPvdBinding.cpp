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

#include "PxSimpleTypes.h"
#include "PsArray.h"
//using namespace physx::shdfnd;
#include "PxVisualDebugger.h"
#include "PxMetaDataPvdBinding.h"
#include "Px.h"

#ifdef PX_VC
#pragma warning(push)
#pragma warning(disable:4100)
#endif

#include "PxMetaDataObjects.h"

#ifdef PX_VC
#pragma warning(pop)
#endif


#include "PvdConnection.h"
#include "PvdDataStream.h"
#include "PxScene.h"
#include "ScShapeIterator.h"
#include "ScBodyCore.h"
#include "PvdMetaDataExtensions.h"
#include "PvdMetaDataPropertyVisitor.h"
#include "PvdMetaDataDefineProperties.h"
#include "PvdMetaDataBindingData.h"
#include "PxRigidDynamic.h"
#include "PxArticulation.h"
#include "PxArticulationLink.h"
#include "PvdTypeNames.h"

using namespace physx::debugger;
using namespace physx;
using namespace Sc;

namespace physx { namespace Pvd {

PvdMetaDataBinding::PvdMetaDataBinding() 
		: mBindingData( PX_NEW( PvdMetaDataBindingData )() )
{
}

PvdMetaDataBinding::~PvdMetaDataBinding()
{
	PX_DELETE( mBindingData );
	mBindingData = NULL;
}

template<typename TDataType, typename TValueType, typename TClassType>
inline void definePropertyStruct( PvdDataStream& inStream, const char* pushName = NULL )
{
	PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
	PvdClassInfoValueStructDefine definitionObj( helper );
	bool doPush = pushName && *pushName;
	if ( doPush )
		definitionObj.pushName( pushName );
	visitAllPvdProperties<TDataType>( definitionObj );
	if ( doPush )
		definitionObj.popName();
	helper.addPropertyMessage(getPvdNamespacedNameForType<TClassType>(), getPvdNamespacedNameForType<TValueType>(), sizeof(TValueType));
}

template<typename TDataType, typename TValueType>
inline void definePropertyStruct( PvdDataStream& inStream )
{
	definePropertyStruct<TDataType,TValueType,TDataType>( inStream );
}

template<typename TDataType>
inline void createClassAndDefineProperties( PvdDataStream& inStream )
{
	inStream.createClass<TDataType>();
	PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
	PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<TDataType>() );
	visitAllPvdProperties<TDataType>( definitionObj );
}

template<typename TDataType, typename TParentType>
inline void createClassDeriveAndDefineProperties( PvdDataStream& inStream )
{
	inStream.createClass<TDataType>();
	inStream.deriveClass<TParentType,TDataType>();
	PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
	PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<TDataType>() );
	visitInstancePvdProperties<TDataType>( definitionObj );
}

void PvdMetaDataBinding::registerSDKProperties( PvdDataStream& inStream ) 
{ 

	//PxPhysics
	{
		inStream.createClass<PxPhysics>();
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<PxPhysics>() );
		helper.pushName( "TolerancesScale" );
		visitAllPvdProperties<PxTolerancesScale>( definitionObj );
		helper.popName();
		inStream.createProperty<PxPhysics,ObjectRef>( "Scenes", "children", PropertyType::Array );
		inStream.createProperty<PxPhysics,ObjectRef>( "Materials", "children", PropertyType::Array );
		inStream.createProperty<PxPhysics,ObjectRef>( "HeightFields", "children", PropertyType::Array );
		inStream.createProperty<PxPhysics,ObjectRef>( "ConvexMeshes", "children", PropertyType::Array );
		inStream.createProperty<PxPhysics,ObjectRef>( "TriangleMeshes", "children", PropertyType::Array );
		inStream.createProperty<PxPhysics,ObjectRef>( "ClothFabrics", "children", PropertyType::Array );
		inStream.createProperty<PxPhysics,PxU32>( "Version.Major" );
		inStream.createProperty<PxPhysics,PxU32>( "Version.Minor" );
		inStream.createProperty<PxPhysics,PxU32>( "Version.Bugfix" );
		definePropertyStruct<PxTolerancesScale,PxTolerancesScaleGeneratedValues,PxPhysics>( inStream, "TolerancesScale" );
	}
	//PxScene
	{
		/*
		struct PvdContact
	{
		PxVec3 point;
		PxVec3 axis;
		const void* shape0;
		const void* shape1;
		PxReal separation;
		PxReal normalForce;
		PxU32 internalFaceIndex0;
		PxU32 internalFaceIndex1;
		bool normalForceAvailable;
	};*/
		{ //contact information
			inStream.createClass<PvdContact>();
			inStream.createProperty<PvdContact,PxVec3>( "Point" );
			inStream.createProperty<PvdContact,PxVec3>( "Axis" );
			inStream.createProperty<PvdContact,ObjectRef>( "Shapes[0]" );
			inStream.createProperty<PvdContact,ObjectRef>( "Shapes[1]" );
			inStream.createProperty<PvdContact,PxF32>( "Separation" );
			inStream.createProperty<PvdContact,PxF32>( "NormalForce" );
			inStream.createProperty<PvdContact,PxU32>( "InternalFaceIndex[0]" );
			inStream.createProperty<PvdContact,PxU32>( "InternalFaceIndex[1]" );
			inStream.createProperty<PvdContact,bool>( "NormalForceValid" );
		}
		inStream.createClass<PxScene>();
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<PxScene>() );
		visitAllPvdProperties<PxSceneDesc>( definitionObj );
		helper.pushName( "SimulationStatistics" );
		visitAllPvdProperties<PxSimulationStatistics>( definitionObj );
		helper.popName();
		inStream.createProperty<PxScene,ObjectRef>( "Physics", "parents", PropertyType::Scalar );
		inStream.createProperty<PxScene,PxU32>( "Timestamp" );
		inStream.createProperty<PxScene,PxReal>( "SimulateElapsedTime" );
		definePropertyStruct<PxSceneDesc,PxSceneDescGeneratedValues,PxScene>( inStream );
		definePropertyStruct<PxSimulationStatistics,PxSimulationStatisticsGeneratedValues,PxScene>( inStream, "SimulationStatistics" );
		inStream.createProperty<PxScene,PvdContact>( "Contacts", "", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "RigidStatics", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "RigidDynamics", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "Articulations", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "ParticleSystems", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "ParticleFluids", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "Cloths", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "Joints", "children", PropertyType::Array );
		inStream.createProperty<PxScene,ObjectRef>( "Constraints", "children", PropertyType::Array );
	}
	//PxMaterial
	{
		createClassAndDefineProperties<PxMaterial>( inStream );
		definePropertyStruct<PxMaterial,PxMaterialGeneratedValues,PxMaterial>( inStream );
		inStream.createProperty<PxMaterial,ObjectRef>( "Physics", "parents", PropertyType::Scalar );
	}
	//PxHeightField
	{
		{
			inStream.createClass<PxHeightFieldSample>();
			inStream.createProperty<PxHeightFieldSample,PxU16>( "Height" );
			inStream.createProperty<PxHeightFieldSample,PxU8>( "MaterialIndex[0]" );
			inStream.createProperty<PxHeightFieldSample,PxU8>( "MaterialIndex[1]" );
		}

		inStream.createClass<PxHeightField>();
		//It is important the PVD fields match the RepX fields, so this has
		//to be hand coded.
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<PxHeightField>() );
		visitAllPvdProperties<PxHeightFieldDesc>( definitionObj );
		inStream.createProperty<PxHeightField,PxHeightFieldSample>("Samples", "", PropertyType::Array );
		inStream.createProperty<PxHeightField,ObjectRef>( "Physics", "parents", PropertyType::Scalar );
		definePropertyStruct<PxHeightFieldDesc,PxHeightFieldDescGeneratedValues,PxHeightField>( inStream );
	}
	//PxConvexMesh
	{
		{ //hull polygon data.
			inStream.createClass<PvdHullPolygonData>();
			inStream.createProperty<PvdHullPolygonData,PxU16>( "NumVertices" );
			inStream.createProperty<PvdHullPolygonData,PxU16>( "IndexBase" );
		}
		inStream.createClass<PxConvexMesh>();
		inStream.createProperty<PxConvexMesh,PxF32>( "Mass" );
		inStream.createProperty<PxConvexMesh,PxQuat>( "LocalInertia" );
		inStream.createProperty<PxConvexMesh,PxVec3>( "LocalCenterOfMass" );
		inStream.createProperty<PxConvexMesh,PxVec3>( "Points", "", PropertyType::Array );
		inStream.createProperty<PxConvexMesh,PvdHullPolygonData>( "HullPolygons", "", PropertyType::Array );
		inStream.createProperty<PxConvexMesh,PxU8>( "PolygonIndexes", "", PropertyType::Array );
		inStream.createProperty<PxConvexMesh,ObjectRef>( "Physics", "parents", PropertyType::Scalar );
	}
	//PxTriangleMesh
	{
		inStream.createClass<PxTriangleMesh>();
		inStream.createProperty<PxTriangleMesh,PxVec3>( "Points", "", PropertyType::Array );
		inStream.createProperty<PxTriangleMesh,PxU32>( "Triangles", "", PropertyType::Array );
		inStream.createProperty<PxTriangleMesh,PxU16>( "MaterialIndices", "", PropertyType::Array );
		inStream.createProperty<PxTriangleMesh,ObjectRef>( "Physics", "parents", PropertyType::Scalar );
	}
	{ //PxGeometry
		inStream.createClass<PxGeometry>();
		inStream.createProperty<PxGeometry,ObjectRef>( "Shape", "parents", PropertyType::Scalar );
	}
	{ //PxBoxGeometry
		createClassDeriveAndDefineProperties<PxBoxGeometry,PxGeometry>( inStream );
		definePropertyStruct<PxBoxGeometry,PxBoxGeometryGeneratedValues,PxBoxGeometry>( inStream );
	}
	{//PxSphereGeometry
		createClassDeriveAndDefineProperties<PxSphereGeometry,PxGeometry>( inStream );
		definePropertyStruct<PxSphereGeometry,PxSphereGeometryGeneratedValues,PxSphereGeometry>( inStream );
	}
	{ //PxCapsuleGeometry
		createClassDeriveAndDefineProperties<PxCapsuleGeometry,PxGeometry>( inStream );
		definePropertyStruct<PxCapsuleGeometry,PxCapsuleGeometryGeneratedValues,PxCapsuleGeometry>( inStream );
	}
	{ //PxPlaneGeometry
		createClassDeriveAndDefineProperties<PxPlaneGeometry,PxGeometry>( inStream );
	}
	{ //PxConvexMeshGeometry
		createClassDeriveAndDefineProperties<PxConvexMeshGeometry,PxGeometry>( inStream );
		definePropertyStruct<PxConvexMeshGeometry,PxConvexMeshGeometryGeneratedValues,PxConvexMeshGeometry>( inStream );
	}
	{ //PxTriangleMeshGeometry
		createClassDeriveAndDefineProperties<PxTriangleMeshGeometry,PxGeometry>( inStream );
		definePropertyStruct<PxTriangleMeshGeometry,PxTriangleMeshGeometryGeneratedValues,PxTriangleMeshGeometry>( inStream );
	}
	{ //PxHeightFieldGeometry
		createClassDeriveAndDefineProperties<PxHeightFieldGeometry,PxGeometry>( inStream );
		definePropertyStruct<PxHeightFieldGeometry,PxHeightFieldGeometryGeneratedValues,PxHeightFieldGeometry>( inStream );
	}
	{ //PxShape
		createClassAndDefineProperties<PxShape>( inStream );
		definePropertyStruct<PxShape,PxShapeGeneratedValues,PxShape>( inStream );
		inStream.createProperty<PxShape,ObjectRef>( "Geometry", "children" );
		inStream.createProperty<PxShape,ObjectRef>( "Materials", "children", PropertyType::Array );
		inStream.createProperty<PxShape,ObjectRef>( "Actor", "parents" );
	}
	//PxActor
	{
		createClassAndDefineProperties<PxActor>( inStream );
		inStream.createProperty<PxActor,ObjectRef>( "Scene", "parents" );
	}
	//PxRigidActor
	{
		createClassDeriveAndDefineProperties<PxRigidActor,PxActor>( inStream );
		inStream.createProperty<PxRigidActor,ObjectRef>( "Shapes", "children", PropertyType::Array );
		inStream.createProperty<PxRigidActor,ObjectRef>( "Joints", "children", PropertyType::Array );
	}
	//PxRigidStatic
	{
		createClassDeriveAndDefineProperties<PxRigidStatic,PxRigidActor>( inStream );
		definePropertyStruct<PxRigidStatic,PxRigidStaticGeneratedValues,PxRigidStatic>( inStream );
	}
	{//PxRigidBody
		createClassDeriveAndDefineProperties<PxRigidBody,PxRigidActor>( inStream );
	}
	//PxRigidDynamic
	{
		createClassDeriveAndDefineProperties<PxRigidDynamic,PxRigidBody>( inStream );
		//If anyone adds a 'getKinematicTarget' to PxRigidDynamic you can remove the line
		//below (after the code generator has run).
		inStream.createProperty<PxRigidDynamic,PxTransform>( "KinematicTarget" );
		definePropertyStruct<PxRigidDynamic,PxRigidDynamicGeneratedValues,PxRigidDynamic>( inStream );
		//Manually define the update struct.
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		/*struct PxRigidDynamicUpdateBlock
		{
			Transform	GlobalPose;
			Float3		LinearVelocity;
			Float3		AngularVelocity;
			PxU8		IsSleeping;
			PxU8		padding[3];
		};
		*/
		helper.pushName( "GlobalPose" ); helper.addPropertyMessageArg<PxTransform>( offsetof( PxRigidDynamicUpdateBlock, GlobalPose ) );helper.popName();
		helper.pushName( "LinearVelocity" ); helper.addPropertyMessageArg<PxVec3>( offsetof( PxRigidDynamicUpdateBlock, LinearVelocity ) ); helper.popName();
		helper.pushName( "AngularVelocity" ); helper.addPropertyMessageArg<PxVec3>( offsetof( PxRigidDynamicUpdateBlock, AngularVelocity ) ); helper.popName();
		helper.pushName( "IsSleeping" ); helper.addPropertyMessageArg<bool>( offsetof( PxRigidDynamicUpdateBlock, IsSleeping ) ); helper.popName();
		helper.addPropertyMessage<PxRigidDynamic,PxRigidDynamicUpdateBlock>();
	}
	{ //PxArticulation
		createClassAndDefineProperties<PxArticulation>( inStream );
		inStream.createProperty<PxArticulation,ObjectRef>( "Scene", "parents" );
		inStream.createProperty<PxArticulation,ObjectRef>( "Links", "children", PropertyType::Array );
		definePropertyStruct<PxArticulation,PxArticulationGeneratedValues,PxArticulation>( inStream );
	}
	{ //PxArticulationLink
		createClassDeriveAndDefineProperties<PxArticulationLink,PxRigidBody>( inStream );
		inStream.createProperty<PxArticulationLink,ObjectRef>( "Parent", "parents" );
		inStream.createProperty<PxArticulationLink,ObjectRef>( "Links", "children", PropertyType::Array );
		inStream.createProperty<PxArticulationLink,ObjectRef>( "InboundJoint", "children" );
		definePropertyStruct<PxArticulationLink,PxArticulationLinkGeneratedValues,PxArticulationLink>( inStream );
		
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		/*struct PxArticulationLinkUpdateBlock
		{
			Transform	GlobalPose;
			Float3		LinearVelocity;
			Float3		AngularVelocity;
		};
		*/
		helper.pushName( "GlobalPose" ); helper.addPropertyMessageArg<PxTransform>( offsetof( PxArticulationLinkUpdateBlock, GlobalPose ) );helper.popName();
		helper.pushName( "LinearVelocity" ); helper.addPropertyMessageArg<PxVec3>( offsetof( PxArticulationLinkUpdateBlock, LinearVelocity ) ); helper.popName();
		helper.pushName( "AngularVelocity" ); helper.addPropertyMessageArg<PxVec3>( offsetof( PxArticulationLinkUpdateBlock, AngularVelocity ) ); helper.popName();
		helper.addPropertyMessage<PxArticulationLink,PxArticulationLinkUpdateBlock>();
	}
	{ //PxArticulationJoint
		createClassAndDefineProperties<PxArticulationJoint>( inStream );
		inStream.createProperty<PxArticulationJoint,ObjectRef>( "Link", "parents" );
		definePropertyStruct<PxArticulationJoint,PxArticulationJointGeneratedValues,PxArticulationJoint>( inStream );
	}
	{ //PxConstraint
		createClassAndDefineProperties<PxConstraint>( inStream );
		definePropertyStruct<PxConstraint,PxConstraintGeneratedValues,PxConstraint>( inStream );
	}
#if PX_USE_PARTICLE_SYSTEM_API
	{ //PxParticleBase
		createClassDeriveAndDefineProperties<PxParticleBase,PxActor>( inStream );
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<PxParticleBase>() );
		visitParticleSystemBufferProperties( makePvdPropertyFilter( definitionObj ) );
	}
	{ //PxParticleSystem
		createClassDeriveAndDefineProperties<PxParticleSystem,PxParticleBase>( inStream );
		definePropertyStruct<PxParticleSystem,PxParticleSystemGeneratedValues,PxParticleSystem>( inStream );
	}
	{ //PxParticleFluid
		createClassDeriveAndDefineProperties<PxParticleFluid,PxParticleBase>( inStream );
		PvdPropertyDefinitionHelper& helper( inStream.getPropertyDefinitionHelper() );
		PvdClassInfoDefine definitionObj( helper, getPvdNamespacedNameForType<PxParticleFluid>() );
		visitParticleFluidBufferProperties( makePvdPropertyFilter( definitionObj ) );
		definePropertyStruct<PxParticleFluid,PxParticleFluidGeneratedValues,PxParticleFluid>( inStream );
	}
#endif
#if PX_USE_CLOTH_API
	{ //PxClothFabric
		createClassAndDefineProperties<PxClothFabric>( inStream );
		definePropertyStruct<PxClothFabric,PxClothFabricGeneratedValues,PxClothFabric>( inStream );
		inStream.createProperty<PxClothFabric,ObjectRef>( "Physics", "parents" );
		inStream.createProperty<PxClothFabric,ObjectRef>( "Cloths", "children", PropertyType::Array );
	}
	{ //PxCloth
		{//PxClothParticle
			createClassAndDefineProperties<PxClothParticle>( inStream );
		}
		{//PvdPositionAndRadius
			inStream.createClass<PvdPositionAndRadius>();
			inStream.createProperty<PvdPositionAndRadius,PxVec3>( "Position" );
			inStream.createProperty<PvdPositionAndRadius,PxF32>( "Radius" );
		}
		createClassDeriveAndDefineProperties<PxCloth,PxActor>( inStream );
		definePropertyStruct<PxCloth,PxClothGeneratedValues,PxCloth>( inStream );
		inStream.createProperty<PxCloth,ObjectRef>( "Fabric" );
		inStream.createProperty<PxCloth,PxClothParticle>( "ParticleBuffer", "", PropertyType::Array );
		inStream.createProperty<PxCloth,PvdPositionAndRadius>( "MotionConstraints", "", PropertyType::Array );
		inStream.createProperty<PxCloth,PvdPositionAndRadius>( "CollisionSpheres", "", PropertyType::Array );
		inStream.createProperty<PxCloth,PvdPositionAndRadius>( "SeparationConstraints", "", PropertyType::Array );
		inStream.createProperty<PxCloth,PxU32>( "CollisionSpherePairs", "", PropertyType::Array );
		inStream.createProperty<PxCloth,PxU32>( "VirtualParticleTriangleAndWeightIndexes", "", PropertyType::Array );
		inStream.createProperty<PxCloth,PxVec3>( "VirtualParticleWeights", "", PropertyType::Array );
	}
#endif
}

template<typename TClassType, typename TValueType, typename TDataType>
static void doSendAllProperties( PvdDataStream& inStream, const TDataType* inDatatype, const void* instanceId )
{
	TValueType theValues( inDatatype );
	inStream.setPropertyMessage( instanceId, theValues );
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxPhysics& inPhysics )
{
	PxTolerancesScale theScale( inPhysics.getTolerancesScale() );
	doSendAllProperties<PxPhysics,PxTolerancesScaleGeneratedValues>( inStream, &theScale, &inPhysics );
	inStream.setPropertyValue( &inPhysics, "Version.Major", (PxU32)PX_PHYSICS_VERSION_MAJOR );
	inStream.setPropertyValue( &inPhysics, "Version.Minor", (PxU32)PX_PHYSICS_VERSION_MINOR );
	inStream.setPropertyValue( &inPhysics, "Version.Bugfix", (PxU32)PX_PHYSICS_VERSION_BUGFIX );
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxScene& inScene )
{
	PxPhysics& physics( const_cast<PxScene&>( inScene ).getPhysics() );
	PxTolerancesScale theScale;
	PxSceneDesc theDesc( theScale );
	inScene.saveToDesc( theDesc );
	PxSceneDescGeneratedValues theValues( &theDesc );
	inStream.setPropertyMessage( &inScene, theValues );
	//Create parent/child relationship.
	inStream.setPropertyValue( &inScene, "Physics", (const void*)&physics );
	inStream.pushBackObjectRef( &physics, "Scenes", &inScene );
}

void PvdMetaDataBinding::sendBeginFrame( PvdDataStream& inStream, const PxScene* inScene, PxReal simulateElapsedTime )
{
	inStream.beginSection( inScene, "frame" );	
	inStream.setPropertyValue( inScene, "Timestamp", inScene->getTimestamp() );
	inStream.setPropertyValue( inScene, "SimulateElapsedTime", simulateElapsedTime );
}
void PvdMetaDataBinding::sendContacts( PvdDataStream& inStream, const PxScene& inScene )
{
	inStream.setPropertyValue( &inScene, "Contacts", DataRef<const PxU8>(), getPvdNamespacedNameForType<PvdContact>() );
}

void PvdMetaDataBinding::sendContacts( PvdDataStream& inStream, const PxScene& inScene, Sc::ContactIterator& inContacts )
{
	inStream.beginSetPropertyValue( &inScene, "Contacts", getPvdNamespacedNameForType<PvdContact>() );

	static const PxU32 NUM_STACK_ELT = 32;
	PvdContact stack[NUM_STACK_ELT];
	PvdContact* pvdContacts = stack;
	PvdContact* pvdContactsEnd = pvdContacts+NUM_STACK_ELT;

	
	PvdContact* curOut = pvdContacts;
	Sc::ContactIterator::Pair* pair;
	Sc::ContactIterator::Contact* contact;
	while( (pair = inContacts.getNextPair()) )
	{
		while( (contact = pair->getNextContact()) )
		{
			curOut->point = contact->point;
			curOut->axis = contact->normal;
			curOut->shape0 = contact->shape0;
			curOut->shape1 = contact->shape1;
			curOut->separation = contact->separation;
			curOut->normalForce = contact->normalForce;
			curOut->internalFaceIndex0 = contact->faceIndex0;
			curOut->internalFaceIndex1 = contact->faceIndex1;
			curOut->normalForceAvailable = contact->normalForceAvailable;

			curOut++;
			if(curOut == pvdContactsEnd)
			{
				inStream.appendPropertyValueData(DataRef<const PxU8>( (PxU8*)(pvdContacts), sizeof(PvdContact) * NUM_STACK_ELT ) );
				curOut = pvdContacts;
			}
		}
	}

	if(curOut != pvdContacts)
		inStream.appendPropertyValueData(DataRef<const PxU8>( (PxU8*)(pvdContacts), sizeof(PvdContact) * PxU32(curOut-pvdContacts)));
	inStream.endSetPropertyValue();
}
void PvdMetaDataBinding::sendStats( PvdDataStream& inStream, const PxScene* inScene  )
{
	PxSimulationStatistics theStats;
	inScene->getSimulationStatistics( theStats );
	PxSimulationStatisticsGeneratedValues values( &theStats );
	inStream.setPropertyMessage( inScene, values );
}

void PvdMetaDataBinding::sendEndFrame( PvdDataStream& inStream, const PxScene* inScene )
{
	inStream.endSection( inScene, "frame" );
}

template<typename TDataType>
void addPhysicsGroupProperty( PvdDataStream& inStream, const char* groupName, const TDataType& inData, const PxPhysics& ownerPhysics )
{
	inStream.setPropertyValue( &inData, "Physics", (const void*)&ownerPhysics );
	inStream.pushBackObjectRef( &ownerPhysics, groupName, &inData );
	//Buffer type objects *have* to be flushed directly out once created else scene creation doesn't
	//work.
	inStream.flush();
}

template<typename TDataType>
void removePhysicsGroupProperty( PvdDataStream& inStream, const char* groupName, const TDataType& inData, const PxPhysics& ownerPhysics )
{
	inStream.removeObjectRef( &ownerPhysics, groupName, &inData );
	inStream.destroyInstance( &inData );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxMaterial& inMaterial, const PxPhysics& ownerPhysics )
{
	inStream.createInstance( &inMaterial );
	sendAllProperties( inStream, inMaterial );
	addPhysicsGroupProperty( inStream, "Materials", inMaterial, ownerPhysics );
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxMaterial& inMaterial )
{
	PxMaterialGeneratedValues values( &inMaterial );
	inStream.setPropertyMessage( &inMaterial, values );
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxMaterial& inMaterial, const PxPhysics& ownerPhysics )
{
	removePhysicsGroupProperty( inStream, "Materials", inMaterial, ownerPhysics );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxHeightField& inData, const PxPhysics& ownerPhysics )
{
	inStream.createInstance( &inData );
	PxHeightFieldDesc theDesc;
	//Save the height field to desc.
	theDesc.nbRows					= inData.getNbRows();
	theDesc.nbColumns				= inData.getNbColumns();
	theDesc.format					= inData.getFormat();
	theDesc.samples.stride			= inData.getSampleStride();
	theDesc.samples.data			= NULL;
	theDesc.thickness				= inData.getThickness();
	theDesc.convexEdgeThreshold		= inData.getConvexEdgeThreshold();
	theDesc.flags					= inData.getFlags();
	
	PxU32 theCellCount = inData.getNbRows() * inData.getNbColumns();
	PxU32 theSampleStride = sizeof( PxHeightFieldSample );
	PxU32 theSampleBufSize = theCellCount * theSampleStride;
	mBindingData->mTempU8Array.resize( theSampleBufSize );
	PxHeightFieldSample* theSamples = reinterpret_cast< PxHeightFieldSample*> ( mBindingData->mTempU8Array.begin() );
	inData.saveCells( theSamples, theSampleBufSize );
	theDesc.samples.data = theSamples;
	PxHeightFieldDescGeneratedValues values( &theDesc );
	inStream.setPropertyMessage( &inData, values );
	PxHeightFieldSample* theSampleData = reinterpret_cast<PxHeightFieldSample*>(mBindingData->mTempU8Array.begin());
	inStream.setPropertyValue( &inData, "Samples", theSampleData, theCellCount );
	

	addPhysicsGroupProperty( inStream, "HeightFields", inData, ownerPhysics );
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxHeightField& inData, const PxPhysics& ownerPhysics )
{
	removePhysicsGroupProperty( inStream, "HeightFields", inData, ownerPhysics );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxConvexMesh& inData, const PxPhysics& ownerPhysics )
{
	inStream.createInstance( &inData );
	PxReal mass;
	PxMat33 localInertia;
	PxVec3 localCom;
	inData.getMassInformation(mass, reinterpret_cast<PxMat33 &>(localInertia), localCom);
	inStream.setPropertyValue( &inData, "Mass", mass );
	inStream.setPropertyValue( &inData, "LocalInertia", PxQuat( localInertia ));
	inStream.setPropertyValue( &inData, "LocalCenterOfMass", localCom);
	
	
	// update arrays:
	// vertex Array:
	{
		const PxVec3* vertexPtr = inData.getVertices();
		const PxU32 numVertices = inData.getNbVertices();
		inStream.setPropertyValue( &inData, "Points", vertexPtr, numVertices );
	}

	// HullPolyArray:
	PxU16 maxIndices = 0;
	{
		
		PxU32 numPolygons = inData.getNbPolygons();
		PvdHullPolygonData* tempData = mBindingData->allocateTemp<PvdHullPolygonData>( numPolygons );
		//Get the polygon data stripping the plane equations
		for(PxU32 index = 0; index < numPolygons; index++)
		{
			PxHullPolygon curOut;
			inData.getPolygonData(index, curOut);
			maxIndices = PxMax(maxIndices, PxU16(curOut.mIndexBase + curOut.mNbVerts));
			tempData[index].mIndexBase = curOut.mIndexBase;
			tempData[index].mNumVertices = curOut.mNbVerts;
		}
		inStream.setPropertyValue( &inData, "HullPolygons", tempData, numPolygons );
	}

	// poly index Array:
	{
		const PxU8* indices = inData.getIndexBuffer();
		inStream.setPropertyValue( &inData, "PolygonIndexes", indices, maxIndices );
	}
	addPhysicsGroupProperty( inStream, "ConvexMeshes", inData, ownerPhysics );
}
void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxConvexMesh& inData, const PxPhysics& ownerPhysics )
{
	removePhysicsGroupProperty( inStream, "ConvexMeshes", inData, ownerPhysics );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxTriangleMesh& inData, const PxPhysics& ownerPhysics )
{
	inStream.createInstance( &inData );
	bool hasMatIndex = inData.getTriangleMaterialIndex(0) != 0xffff;
	// update arrays:
	// vertex Array:
	{ 
		const PxVec3* vertexPtr = inData.getVertices();
		const PxU32 numVertices = inData.getNbVertices();
		inStream.setPropertyValue( &inData, "Points", vertexPtr, numVertices );
	}

	// index Array:
	{
		const bool has16BitIndices = inData.has16BitTriangleIndices();
		const PxU32 numIndexes = inData.getNbTriangles() * 3;
		const PxU8* trianglePtr = reinterpret_cast<const PxU8*>(inData.getTriangles());
		//We declared this type as a 32 bit integer above.
		//PVD will automatically unsigned-extend data that is smaller than the target type.
		if ( has16BitIndices )
			inStream.setPropertyValue( &inData, "Triangles", reinterpret_cast<const PxU16*>( trianglePtr ), numIndexes );
		else
			inStream.setPropertyValue( &inData, "Triangles", reinterpret_cast<const PxU32*>( trianglePtr ), numIndexes );
	}

	// material Array:
	if(hasMatIndex)
	{
		PxU32 numMaterials = inData.getNbTriangles();
		PxU16* matIndexData = mBindingData->allocateTemp<PxU16>( numMaterials );
		for(PxU32 m = 0; m < numMaterials; m++)
			matIndexData[m] = inData.getTriangleMaterialIndex(m);
		inStream.setPropertyValue( &inData, "MaterialIndices", matIndexData, numMaterials );
	}
	addPhysicsGroupProperty( inStream, "TriangleMeshes", inData, ownerPhysics );
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxTriangleMesh& inData, const PxPhysics& ownerPhysics )
{
	removePhysicsGroupProperty( inStream, "TriangleMeshes", inData, ownerPhysics );
}

template<typename TDataType>
struct GeometryBufferRegisterOp
{
	void registerBuffers( const TDataType&, BufferRegistrar&) {} 
};

template<>
struct GeometryBufferRegisterOp<PxConvexMeshGeometry>
{
	void registerBuffers( const PxConvexMeshGeometry geom, BufferRegistrar& registrar ) { registrar.addRef( geom.convexMesh ); }
};
template<>
struct GeometryBufferRegisterOp<PxTriangleMeshGeometry>
{
	void registerBuffers( const PxTriangleMeshGeometry geom, BufferRegistrar& registrar ) { registrar.addRef( geom.triangleMesh ); }
};
template<>
struct GeometryBufferRegisterOp<PxHeightFieldGeometry>
{
	void registerBuffers( const PxHeightFieldGeometry geom, BufferRegistrar& registrar ) { registrar.addRef( geom.heightField ); }
};

template<typename TGeneratedValuesType, typename TGeomType>
void sendGeometry( PvdDataStream& inStream, const PxShape& inShape, const TGeomType& geom, BufferRegistrar& registrar )
{
	const void* geomInst = (reinterpret_cast<const PxU8*>( &inShape ) ) + 4;
	inStream.createInstance( getPvdNamespacedNameForType<TGeomType>(), geomInst );
	GeometryBufferRegisterOp<TGeomType>().registerBuffers( geom, registrar );
	TGeneratedValuesType values( &geom );
	inStream.setPropertyMessage( geomInst, values );
	inStream.setPropertyValue( &inShape, "Geometry", geomInst );
	inStream.setPropertyValue( geomInst, "Shape", (const void*)&inShape );
}

void setGeometry( PvdDataStream& inStream, const PxShape& inObj, BufferRegistrar& registrar )
{
	switch( inObj.getGeometryType() )
	{
#define SEND_PVD_GEOM_TYPE( enumType, geomType, valueType )								\
	case PxGeometryType::enumType:														\
		{																				\
			Px##geomType geom;															\
			inObj.get##geomType( geom );												\
			sendGeometry<valueType>( inStream, inObj, geom, registrar );				\
		}																				\
		break;
		SEND_PVD_GEOM_TYPE( eSPHERE, SphereGeometry, PxSphereGeometryGeneratedValues );
		//Plane geometries don't have any properties, so this avoids using a property
		//struct for them.
		case PxGeometryType::ePLANE:
			{
				PxPlaneGeometry geom;
				inObj.getPlaneGeometry( geom );
				const void* geomInst = (reinterpret_cast<const PxU8*>( &inObj ) ) + 4;
				inStream.createInstance( getPvdNamespacedNameForType<PxPlaneGeometry>(), geomInst );
				inStream.setPropertyValue( &inObj, "Geometry", geomInst );
				inStream.setPropertyValue( geomInst, "Shape", (const void*)&inObj );
			}
		break;
		SEND_PVD_GEOM_TYPE( eCAPSULE, CapsuleGeometry, PxCapsuleGeometryGeneratedValues );
		SEND_PVD_GEOM_TYPE( eBOX, BoxGeometry, PxBoxGeometryGeneratedValues );
		SEND_PVD_GEOM_TYPE( eCONVEXMESH, ConvexMeshGeometry, PxConvexMeshGeometryGeneratedValues );
		SEND_PVD_GEOM_TYPE( eTRIANGLEMESH, TriangleMeshGeometry, PxTriangleMeshGeometryGeneratedValues );
		SEND_PVD_GEOM_TYPE( eHEIGHTFIELD, HeightFieldGeometry, PxHeightFieldGeometryGeneratedValues );
#undef SEND_PVD_GEOM_TYPE
		default:
			PX_ASSERT( false );
			break;
	}
}

void setMaterials( PvdDataStream& inStream, const PxShape& inObj, BufferRegistrar& registrar, PvdMetaDataBindingData* mBindingData )
{
	PxU32 numMaterials = inObj.getNbMaterials();
	PxMaterial** materialPtr = mBindingData->allocateTemp<PxMaterial*>( numMaterials );
	inObj.getMaterials( materialPtr, numMaterials );
	for ( PxU32 idx = 0; idx < numMaterials; ++idx )
	{
		registrar.addRef( materialPtr[idx] );
		inStream.pushBackObjectRef( &inObj, "Materials", materialPtr[idx] );
	}
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxShape& inObj, const PxRigidActor& owner, BufferRegistrar& registrar )
{
	if ( inStream.isInstanceValid( &inObj ) == true 
		|| inStream.isInstanceValid( &owner ) == false ) 
		return;

	inStream.createInstance( &inObj );
	inStream.pushBackObjectRef( &owner, "Shapes", &inObj );
	inStream.setPropertyValue( &inObj, "Actor", (const void*)&owner );
	sendAllProperties( inStream, inObj );
	setGeometry( inStream, inObj, registrar );
	setMaterials( inStream, inObj, registrar, mBindingData );
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxShape& inObj )
{
	PxShapeGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}

void PvdMetaDataBinding::releaseAndRecreateGeometry( PvdDataStream& inStream, const PxShape& inObj, BufferRegistrar& registrar )
{
	const void* geomInst = (reinterpret_cast<const PxU8*>( &inObj ) ) + 4;
	inStream.destroyInstance( geomInst );
	setGeometry( inStream, inObj, registrar );
}

void PvdMetaDataBinding::updateMaterials( PvdDataStream& inStream, const PxShape& inObj, BufferRegistrar& registrar )
{
	//Clear the shape's materials array.
	inStream.setPropertyValue( &inObj, "Materials", DataRef<const PxU8>(), getPvdNamespacedNameForType<ObjectRef>() );
	setMaterials( inStream, inObj, registrar, mBindingData );
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxShape& inObj, const PxRigidActor& owner )
{
	if ( inStream.isInstanceValid( &inObj ) )
	{
		inStream.removeObjectRef( &owner, "Shapes", &inObj );
		const void* geomInst = (reinterpret_cast<const PxU8*>( &inObj ) ) + 4;
		inStream.destroyInstance( geomInst );
		inStream.destroyInstance( &inObj );
	}
}

template<typename TDataType>
void addSceneGroupProperty( PvdDataStream& inStream, const char* groupName, const TDataType& inObj, const PxScene& inScene )
{
	inStream.createInstance( &inObj );
	inStream.pushBackObjectRef( &inScene, groupName, &inObj );
	inStream.setPropertyValue( &inObj, "Scene", (const void*)(&inScene) );

}

template<typename TDataType>
void removeSceneGroupProperty( PvdDataStream& inStream, const char* groupName, const TDataType& inObj, const PxScene& inScene )
{
	inStream.removeObjectRef( &inScene, groupName, &inObj );
	inStream.destroyInstance( &inObj );
}

void sendShapes( PvdMetaDataBinding& binding, PvdDataStream& inStream, const PxRigidActor& inObj, BufferRegistrar& registrar )
{
	InlineArray<PxShape*, 5> shapeData;
	PxU32 nbShapes = inObj.getNbShapes();
	shapeData.resize( nbShapes );
	inObj.getShapes( shapeData.begin(), nbShapes );
	for ( PxU32 idx = 0; idx < nbShapes; ++idx )
		binding.createInstance( inStream, *shapeData[idx], inObj, registrar );
}

void releaseShapes( PvdMetaDataBinding& binding, PvdDataStream& inStream, const PxRigidActor& inObj )
{
	InlineArray<PxShape*, 5> shapeData;
	PxU32 nbShapes = inObj.getNbShapes();
	shapeData.resize( nbShapes );
	inObj.getShapes( shapeData.begin(), nbShapes );
	for ( PxU32 idx = 0; idx < nbShapes; ++idx )
		binding.destroyInstance( inStream, *shapeData[idx], inObj );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxRigidStatic& inObj, const PxScene& ownerScene, BufferRegistrar& registrar )
{
	addSceneGroupProperty( inStream, "RigidStatics", inObj, ownerScene );
	sendAllProperties( inStream, inObj );
	sendShapes( *this, inStream, inObj, registrar );
}
void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxRigidStatic& inObj )
{
	PxRigidStaticGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}
void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxRigidStatic& inObj, const PxScene& ownerScene )
{
	releaseShapes( *this, inStream, inObj );
	removeSceneGroupProperty( inStream, "RigidStatics", inObj, ownerScene );
}
void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxRigidDynamic& inObj, const PxScene& ownerScene, BufferRegistrar& registrar )
{
	addSceneGroupProperty( inStream, "RigidDynamics", inObj, ownerScene );
	sendAllProperties( inStream, inObj );
	sendShapes( *this, inStream, inObj,registrar );
}
void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxRigidDynamic& inObj )
{
	PxRigidDynamicGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}
void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxRigidDynamic& inObj, const PxScene& ownerScene )
{
	releaseShapes( *this, inStream, inObj );
	removeSceneGroupProperty( inStream, "RigidDynamics", inObj, ownerScene );
}

void addChild( PvdDataStream& inStream, const void* inParent, const PxArticulationLink& inChild )
{
	inStream.pushBackObjectRef( inParent, "Links", &inChild );
	inStream.setPropertyValue( &inChild, "Parent", inParent );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxArticulation& inObj, const PxScene& ownerScene, BufferRegistrar& registrar )
{
	addSceneGroupProperty( inStream, "Articulations", inObj, ownerScene );
	sendAllProperties( inStream, inObj );
	PxU32 numLinks = inObj.getNbLinks();
	mBindingData->mArticulationLinks.resize( numLinks );
	inObj.getLinks( mBindingData->mArticulationLinks.begin(), numLinks );
	//From Dilip Sequiera:
	/*
		No, there can only be one root, and in all the code I wrote (which is not 100% of the HL code for articulations), 
		the index of a child is always > the index of the parent.
	*/

	//Create all the links
	for ( PxU32 idx = 0; idx < numLinks; ++idx )
		createInstance( inStream, *mBindingData->mArticulationLinks[idx], registrar );
	//Setup the link graph
	for ( PxU32 idx = 0; idx < numLinks; ++idx )
	{
		PxArticulationLink* link = mBindingData->mArticulationLinks[idx];
		if ( idx == 0 )
			addChild( inStream, &inObj, *link );

		PxU32 numChildren = link->getNbChildren();
		PxArticulationLink** children = mBindingData->allocateTemp<PxArticulationLink*>( numChildren );
		link->getChildren( children, numChildren );
		for ( PxU32 idx = 0; idx < numChildren; ++idx )
			addChild( inStream, link, *children[idx] );
	}
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxArticulation& inObj )
{
	PxArticulationGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxArticulation& inObj, const PxScene& ownerScene )
{
	removeSceneGroupProperty( inStream, "Articulations", inObj, ownerScene );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxArticulationLink& inObj, BufferRegistrar& registrar )
{
	inStream.createInstance( &inObj );
	PxArticulationJoint* joint( inObj.getInboundJoint() );
	if ( joint )
	{
		inStream.createInstance( joint );
		inStream.setPropertyValue( &inObj, "InboundJoint", (const void*)joint );
		inStream.setPropertyValue( joint, "Link", (const void*)&inObj );
		sendAllProperties( inStream, *joint );
	}
	sendAllProperties( inStream, inObj );
	sendShapes( *this, inStream, inObj, registrar );
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxArticulationLink& inObj )
{
	PxArticulationLinkGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values ); 
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxArticulationLink& inObj )
{
	PxArticulationJoint* joint( inObj.getInboundJoint() );
	if ( joint )
		inStream.destroyInstance( joint );
	releaseShapes( *this, inStream, inObj );
	inStream.destroyInstance( &inObj );
}
//These are created as part of the articulation link's creation process, so outside entities don't need to
//create them.
void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxArticulationJoint& inObj )
{
	PxArticulationJointGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}

template<typename TReadDataType>
struct ParticleFluidUpdater
{
	TReadDataType& mData;
	Array<PxU8>& mTempU8Array;
	PvdDataStream& mStream;
	const void* mInstanceId;
	PxU32 mRdFlags;
	ParticleFluidUpdater( TReadDataType& d, PvdDataStream& s, const void* id, PxU32 flags, Array<PxU8>& tempArray )
		: mData( d )
		, mTempU8Array( tempArray )
		, mStream( s )
		, mInstanceId( id )
		, mRdFlags( flags )
	{
	}

	template<PxU32 TKey, typename TObjectType, typename TPropertyType, PxU32 TEnableFlag>
	void handleBuffer( const PxBufferPropertyInfo< TKey, TObjectType, PxStrideIterator<const TPropertyType>, TEnableFlag >& inProp, NamespacedName datatype )
	{
		PxU32 numValidParticles = mData.numValidParticles;
		PxU32 validParticleRange = mData.validParticleRange;
		PxStrideIterator<const TPropertyType> iterator( inProp.get( &mData ) );
		const PxU32* validParticleBitmap = mData.validParticleBitmap;
		
		if( numValidParticles == 0 || iterator.ptr() == NULL || inProp.isEnabled(mRdFlags) == false )
			return;

		// setup the pvd array
		DataRef<const PxU8> propData;
		mTempU8Array.resize(numValidParticles * sizeof(TPropertyType));
		TPropertyType* tmpArray  = reinterpret_cast<TPropertyType*>(mTempU8Array.begin());
		propData = DataRef<const PxU8>( mTempU8Array.begin(), mTempU8Array.size() );
		if(numValidParticles == validParticleRange)
		{
			for ( PxU32 idx = 0; idx < numValidParticles; ++idx )
				tmpArray[idx] = iterator[idx];
		}
		else
		{
			PxU32 tIdx = 0;
			// iterate over bitmap and send all valid particles
			for (PxU32 w = 0; w <= (validParticleRange-1) >> 5; w++)
			{
				for (PxU32 b = validParticleBitmap[w]; b; b &= b-1)
				{
					tmpArray[tIdx++] = iterator[w<<5|Ps::lowestSetBit(b)];
				}
			}
			PX_ASSERT(tIdx == numValidParticles);
		}
		mStream.setPropertyValue( mInstanceId, inProp.mName, propData, datatype );
	}
	template<PxU32 TKey, typename TObjectType, typename TPropertyType, PxU32 TEnableFlag>
	void handleBuffer( const PxBufferPropertyInfo< TKey, TObjectType, PxStrideIterator<const TPropertyType>, TEnableFlag >& inProp )
	{
		handleBuffer( inProp, getPvdNamespacedNameForType<TPropertyType>() );
	}

	template<PxU32 TKey, typename TObjectType, typename TEnumType, typename TStorageType, PxU32 TEnableFlag>
	void handleFlagsBuffer( const PxBufferPropertyInfo< TKey, TObjectType, PxStrideIterator<const PxFlags<TEnumType, TStorageType> >, TEnableFlag >& inProp, const PxU32ToName* )
	{
		handleBuffer( inProp, getPvdNamespacedNameForType<TStorageType>() );
	}
};


#if PX_USE_PARTICLE_SYSTEM_API
void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxParticleSystem& inObj, const PxScene& ownerScene )
{
	addSceneGroupProperty( inStream, "ParticleSystems", inObj, ownerScene );
	sendAllProperties( inStream, inObj );
	PxParticleReadData* readData( const_cast<PxParticleSystem&>( inObj ).lockParticleReadData() );
	if ( readData )
	{
		PxU32 readFlags = inObj.getParticleReadDataFlags();
		sendArrays( inStream, inObj, *readData, readFlags );
		readData->unlock();
	}
}
void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxParticleSystem& inObj )
{
	PxParticleSystemGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}
void PvdMetaDataBinding::sendArrays( PvdDataStream& inStream, const PxParticleSystem& inObj, PxParticleReadData& inData, PxU32 inFlags )
{
	ParticleFluidUpdater<PxParticleReadData> theUpdater( inData, inStream, (const PxActor*)&inObj, inFlags, mBindingData->mTempU8Array );
	visitParticleSystemBufferProperties( makePvdPropertyFilter( theUpdater ) );
}
void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxParticleSystem& inObj, const PxScene& ownerScene )
{
	removeSceneGroupProperty( inStream, "ParticleSystems", inObj, ownerScene );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxParticleFluid& inObj, const PxScene& ownerScene )
{
	addSceneGroupProperty( inStream, "ParticleFluids", inObj, ownerScene );
	sendAllProperties( inStream, inObj );
	PxParticleFluidReadData* readData( const_cast<PxParticleFluid&>( inObj ).lockParticleFluidReadData() );
	if ( readData )
	{
		PxU32 readFlags = inObj.getParticleReadDataFlags();
		sendArrays( inStream, inObj, *readData, readFlags );
		readData->unlock();
	}
}
void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxParticleFluid& inObj )
{
	PxParticleFluidGeneratedValues values( &inObj );
	inStream.setPropertyMessage( &inObj, values );
}
void PvdMetaDataBinding::sendArrays( PvdDataStream& inStream, const PxParticleFluid& inObj, PxParticleFluidReadData& inData, PxU32 inFlags )
{
	ParticleFluidUpdater<PxParticleFluidReadData> theUpdater( inData, inStream, (const PxActor*)&inObj, inFlags, mBindingData->mTempU8Array );
	visitParticleSystemBufferProperties( makePvdPropertyFilter( theUpdater ) );
	visitParticleFluidBufferProperties( makePvdPropertyFilter( theUpdater ) );
}
void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxParticleFluid& inObj, const PxScene& ownerScene )
{
	removeSceneGroupProperty( inStream, "ParticleFluids", inObj, ownerScene );
}
#endif // PX_USE_PARTICLE_SYSTEM_API

template<typename TBlockType, typename TActorType, typename TOperator>
void updateActor( PvdDataStream& inStream, TActorType** actorGroup, PxU32 numActors, TOperator sleepingOp, PvdMetaDataBindingData& bindingData )
{
	TBlockType theBlock;
	if ( numActors == 0 ) return;
	for ( PxU32 idx = 0; idx < numActors; ++idx )
	{
		TActorType* theActor( actorGroup[idx] );
		bool sleeping = sleepingOp( theActor, theBlock );
		bool wasSleeping = bindingData.mSleepingActors.contains( theActor );

		if ( sleeping == false || sleeping != wasSleeping )
		{
			theBlock.GlobalPose = theActor->getGlobalPose();
			theBlock.AngularVelocity = theActor->getAngularVelocity();
			theBlock.LinearVelocity = theActor->getLinearVelocity();
			inStream.sendPropertyMessageFromGroup( theActor, theBlock );
			if ( sleeping != wasSleeping )
			{
				if ( sleeping )
					bindingData.mSleepingActors.insert( theActor );
				else
					bindingData.mSleepingActors.erase( theActor );
			}
		}
	}
}

struct RigidDynamicUpdateOp
{
	bool operator()( PxRigidDynamic* actor, PxRigidDynamicUpdateBlock& block )
	{
		bool sleeping = actor->isSleeping();
		block.IsSleeping = sleeping;
		return sleeping;
	}
};

struct ArticulationLinkUpdateOp
{
	bool sleeping;
	ArticulationLinkUpdateOp( bool s ) : sleeping( s ){}
	bool operator()( PxArticulationLink*, PxArticulationLinkUpdateBlock& )
	{
		return sleeping;
	}
};

void PvdMetaDataBinding::updateDynamicActorsAndArticulations( PvdDataStream& inStream, const PxScene* inScene, PvdVisualizer* linkJointViz )
{
	PX_COMPILE_TIME_ASSERT( sizeof( PxRigidDynamicUpdateBlock ) == 14 * 4 );
	{
		PxU32 actorCount = inScene->getNbActors( PxActorTypeSelectionFlag::eRIGID_DYNAMIC );
		if ( actorCount )
		{
			inStream.beginPropertyMessageGroup<PxRigidDynamicUpdateBlock>();
			mBindingData->mActors.resize( actorCount );
			PxActor** theActors = mBindingData->mActors.begin();
			inScene->getActors( PxActorTypeSelectionFlag::eRIGID_DYNAMIC, theActors, actorCount );
			updateActor<PxRigidDynamicUpdateBlock>( inStream, reinterpret_cast<PxRigidDynamic**>( theActors ), actorCount, RigidDynamicUpdateOp(), *mBindingData );
			inStream.endPropertyMessageGroup();
		}
	}
	{
		PxU32 articulationCount = inScene->getNbArticulations();
		if ( articulationCount )
		{
			mBindingData->mArticulations.resize( articulationCount );
			PxArticulation** firstArticulation = mBindingData->mArticulations.begin();
			PxArticulation** lastArticulation = firstArticulation + articulationCount;
			inScene->getArticulations( firstArticulation, articulationCount );
			inStream.beginPropertyMessageGroup<PxArticulationLinkUpdateBlock>();
			for ( ; firstArticulation < lastArticulation; ++firstArticulation )
			{
				PxU32 linkCount = (*firstArticulation)->getNbLinks();
				bool sleeping = (*firstArticulation)->isSleeping();
				if ( linkCount )
				{
					mBindingData->mArticulationLinks.resize( linkCount );
					PxArticulationLink** theLink = mBindingData->mArticulationLinks.begin();
					(*firstArticulation)->getLinks( theLink, linkCount );
					updateActor<PxArticulationLinkUpdateBlock>( inStream, theLink, linkCount, ArticulationLinkUpdateOp( sleeping ), *mBindingData );
					if ( linkJointViz )
					{
						for ( PxU32 idx = 0; idx < linkCount; ++idx ) linkJointViz->visualize( *theLink[idx] );
					}
				}
			}
			inStream.endPropertyMessageGroup();
			firstArticulation = mBindingData->mArticulations.begin();
			for ( ; firstArticulation < lastArticulation; ++firstArticulation )
				inStream.setPropertyValue( *firstArticulation, "IsSleeping", (*firstArticulation)->isSleeping() );
		}
	}
}

template<typename TObjType>
struct CollectionOperator
{
	Array<PxU8>&	mTempArray;
	const TObjType& mObject;
	PvdDataStream&	mStream;

	CollectionOperator( Array<PxU8>& ary, const TObjType& obj, PvdDataStream& stream ) : mTempArray( ary ), mObject( obj ), mStream( stream ) {}
	void pushName( const char* ) {}
	void popName() {}
	template< typename TAccessor > void simpleProperty(PxU32 key, const TAccessor& ) {}
	template< typename TAccessor > void flagsProperty(PxU32 key, const TAccessor&, const PxU32ToName* ) {}

	template<typename TColType, typename TDataType, typename TCollectionProp >
	void handleCollection( const TCollectionProp& prop, NamespacedName dtype, PxU32 countMultiplier = 1 )
	{
		PxU32 count = prop.size( &mObject );
		mTempArray.resize( count * sizeof( TDataType ) );
		TColType* start = reinterpret_cast<TColType*>( mTempArray.begin() );
		prop.get( &mObject, start, count * countMultiplier );
		mStream.setPropertyValue( &mObject, prop.mName, DataRef<const PxU8>( mTempArray.begin(), mTempArray.size() ), dtype );
	}
	template< PxU32 TKey, typename TObject, typename TColType >
	void handleCollection( const PxReadOnlyCollectionPropertyInfo<TKey,TObject,TColType>& prop )
	{
		handleCollection<TColType,TColType>( prop, getPvdNamespacedNameForType<TColType>() );
	}
	//Enumerations or bitflags.
	template< PxU32 TKey, typename TObject, typename TColType >
	void handleCollection( const PxReadOnlyCollectionPropertyInfo<TKey,TObject,TColType>& prop, const PxU32ToName* )
	{
		PX_COMPILE_TIME_ASSERT( sizeof( TColType ) == sizeof( PxU32 ) );
		handleCollection<TColType,PxU32>( prop, getPvdNamespacedNameForType<PxU32>() );
	}
};

#if PX_USE_CLOTH_API
struct PxClothFabricCollectionOperator : CollectionOperator<PxClothFabric>
{
	PxClothFabricCollectionOperator( Array<PxU8>& ary, const PxClothFabric& obj, PvdDataStream& stream ) 
		: CollectionOperator<PxClothFabric>( ary, obj, stream ) {}
	
	template< PxU32 TKey, typename TObject, typename TColType >
	void handleCollection( const PxReadOnlyCollectionPropertyInfo<TKey,TObject,TColType>& prop )
	{
		CollectionOperator<PxClothFabric>::handleCollection<TColType,TColType>( prop, getPvdNamespacedNameForType<TColType>(), sizeof( TColType ) );
	}

	//Enumerations or bitflags.
	template< PxU32 TKey, typename TObject, typename TColType >
	void handleCollection( const PxReadOnlyCollectionPropertyInfo<TKey,TObject,TColType>& prop, const PxU32ToName* )
	{
		PX_COMPILE_TIME_ASSERT( sizeof( TColType ) == sizeof( PxU32 ) );
		CollectionOperator<PxClothFabric>::handleCollection<TColType,PxU32>( prop, getPvdNamespacedNameForType<PxU32>() );
	}
};

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxClothFabric& fabric, const PxPhysics& ownerPhysics )
{
	inStream.createInstance( &fabric );
	addPhysicsGroupProperty( inStream, "ClothFabrics", fabric, ownerPhysics );
	sendAllProperties( inStream, fabric );
}

void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxClothFabric& fabric )
{
	PxClothFabricCollectionOperator op( mBindingData->mTempU8Array, fabric, inStream );
	visitInstancePvdProperties<PxClothFabric>( op );

	PxClothFabricGeneratedValues values( &fabric );
	inStream.setPropertyMessage( &fabric, values );
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxClothFabric& fabric, const PxPhysics& ownerPhysics )
{
	removePhysicsGroupProperty( inStream, "ClothFabrics", fabric, ownerPhysics );
}

void PvdMetaDataBinding::createInstance( PvdDataStream& inStream, const PxCloth& cloth, const PxScene& ownerScene, BufferRegistrar& registrar )
{
	addSceneGroupProperty( inStream, "Cloths", cloth, ownerScene );
	PxClothFabric* fabric = cloth.getFabric();
	if ( fabric != NULL )
	{
		registrar.addRef( cloth.getFabric() );
		inStream.setPropertyValue( &cloth, "Fabric", (const void*)fabric );
		inStream.pushBackObjectRef( fabric, "Cloths", &cloth );
	}
	sendAllProperties( inStream, cloth );
}
void PvdMetaDataBinding::sendAllProperties( PvdDataStream& inStream, const PxCloth& cloth )
{
	sendSimpleProperties( inStream, cloth );
	sendMotionConstraints( inStream, cloth );
	sendCollisionSpheres( inStream, cloth );
	sendVirtualParticles( inStream, cloth );
	sendSeparationConstraints( inStream, cloth );
}
void PvdMetaDataBinding::sendSimpleProperties( PvdDataStream& inStream, const PxCloth& cloth )
{
	PxClothGeneratedValues values( &cloth );
	inStream.setPropertyMessage( &cloth, values );
}
void PvdMetaDataBinding::sendMotionConstraints( PvdDataStream& inStream, const PxCloth& cloth )
{
	PxU32 count = cloth.getNbMotionConstraints();
	PxClothParticleMotionConstraint* constraints = mBindingData->allocateTemp<PxClothParticleMotionConstraint>( count );
	if ( count ) cloth.getMotionConstraints( constraints );
	inStream.setPropertyValue( &cloth, "MotionConstraints", mBindingData->tempToRef(), getPvdNamespacedNameForType<PvdPositionAndRadius>() );
}

void PvdMetaDataBinding::sendCollisionSpheres( PvdDataStream& inStream, const PxCloth& cloth, bool sendPairs )
{
	PxU32 numSpheres = cloth.getNbCollisionSpheres();
	PxU32 numIndices = 2*cloth.getNbCollisionSpherePairs();
	PxU32 numPlanes = cloth.getNbCollisionPlanes();
	PxU32 numConvexes = cloth.getNbCollisionConvexes();
	PxU32 sphereBytes = numSpheres * sizeof( PxClothCollisionSphere );
	PxU32 pairBytes = numIndices * sizeof( PxU32 );
	PxU32 planesBytes = numPlanes * sizeof(PxClothCollisionPlane);
	PxU32 convexBytes = numConvexes * sizeof(PxU32);

	mBindingData->mTempU8Array.resize( sphereBytes + pairBytes + planesBytes + convexBytes );
	PxU8* bufferStart = mBindingData->mTempU8Array.begin();
	PxClothCollisionSphere* sphereBuffer = reinterpret_cast<PxClothCollisionSphere*>( mBindingData->mTempU8Array.begin() );
	PxU32* indexBuffer = reinterpret_cast<PxU32*>(sphereBuffer + numSpheres);
	PxClothCollisionPlane* planeBuffer = reinterpret_cast<PxClothCollisionPlane*>(indexBuffer + numIndices);
	PxU32* convexBuffer = reinterpret_cast<PxU32*>(planeBuffer + numPlanes);

	cloth.getCollisionData( sphereBuffer, indexBuffer, planeBuffer, convexBuffer );
	inStream.setPropertyValue( &cloth, "CollisionSpheres", DataRef<const PxU8>( bufferStart, sphereBytes ), getPvdNamespacedNameForType<PvdPositionAndRadius>() );
	if ( sendPairs )
		inStream.setPropertyValue( &cloth, "CollisionSpherePairs", DataRef<const PxU8>( bufferStart + sphereBytes, pairBytes ), getPvdNamespacedNameForType<PxU32>() );
}

void PvdMetaDataBinding::sendVirtualParticles( PvdDataStream& inStream, const PxCloth& cloth )
{
	PxU32 numParticles = cloth.getNbVirtualParticles();
	PxU32 numWeights = cloth.getNbVirtualParticleWeights();
	PxU32 numIndexes = numParticles * 4;
	PxU32 numIndexBytes = numIndexes * sizeof( PxU32 );
	PxU32 numWeightBytes = numWeights * sizeof( PxVec3 );
	
	mBindingData->mTempU8Array.resize( PxMax( numIndexBytes, numWeightBytes ) );
	PxU8* dataStart = mBindingData->mTempU8Array.begin();

	PxU32* indexStart = reinterpret_cast<PxU32*>( dataStart );
	if (numIndexes)
		cloth.getVirtualParticles( indexStart );
	inStream.setPropertyValue( &cloth, "VirtualParticleTriangleAndWeightIndexes", DataRef<const PxU8>( dataStart, numIndexBytes ), getPvdNamespacedNameForType<PxU32>() );

	PxVec3* weightStart = reinterpret_cast<PxVec3*>( dataStart );
	if (numWeights)
		cloth.getVirtualParticleWeights( weightStart );
	inStream.setPropertyValue( &cloth, "VirtualParticleWeights", DataRef<const PxU8>( dataStart, numWeightBytes ), getPvdNamespacedNameForType<PxVec3>() );
}
void PvdMetaDataBinding::sendSeparationConstraints( PvdDataStream& inStream, const PxCloth& cloth )
{
	PxU32 count = cloth.getNbSeparationConstraints();
	PxU32 byteSize = count * sizeof(PxClothParticleSeparationConstraint); 
	mBindingData->mTempU8Array.resize( byteSize );
	if ( count ) cloth.getSeparationConstraints( reinterpret_cast<PxClothParticleSeparationConstraint*>( mBindingData->mTempU8Array.begin() ) );
	inStream.setPropertyValue( &cloth, "SeparationConstraints", mBindingData->tempToRef(), getPvdNamespacedNameForType<PvdPositionAndRadius>() );
}
#endif // PX_USE_CLOTH_API

//per frame update

#if PX_USE_CLOTH_API

void PvdMetaDataBinding::updateCloths( PvdDataStream& inStream, const PxScene& inScene )
{
	PxU32 actorCount = inScene.getNbActors( PxActorTypeSelectionFlag::eCLOTH );
	if ( actorCount  == 0 ) return;
	mBindingData->mActors.resize( actorCount );
	PxActor** theActors = mBindingData->mActors.begin();
	inScene.getActors( PxActorTypeSelectionFlag::eCLOTH, theActors, actorCount );
	PX_COMPILE_TIME_ASSERT( sizeof( PxClothParticle ) == sizeof( PxVec3 ) + sizeof( PxF32 ) );
	for ( PxU32 idx =0; idx < actorCount; ++idx )
	{
		PxCloth* theCloth = static_cast<PxCloth*>( theActors[idx] );
		bool isSleeping = theCloth->isSleeping();
		bool wasSleeping = mBindingData->mSleepingActors.contains( theCloth );
		if ( isSleeping == false || isSleeping != wasSleeping )
		{
			PxClothReadData* theData = theCloth->lockClothReadData();
			if ( theData != NULL )
			{
				PxU32 numBytes = sizeof( PxClothParticle ) * theCloth->getNbParticles();
				inStream.setPropertyValue( theCloth, "ParticleBuffer", DataRef<const PxU8>( reinterpret_cast<const PxU8*>( theData->particles ), numBytes ), getPvdNamespacedNameForType<PxClothParticle>() );
				theData->unlock();
			}
		}
		if ( isSleeping != wasSleeping )
		{
			inStream.setPropertyValue( theCloth, "IsSleeping", isSleeping );
			if ( isSleeping )
				mBindingData->mSleepingActors.insert( theActors[idx] );
			else
				mBindingData->mSleepingActors.erase( theActors[idx] );
		}
	}
}

void PvdMetaDataBinding::destroyInstance( PvdDataStream& inStream, const PxCloth& cloth, const PxScene& ownerScene )
{
	PxClothFabric* fabric = cloth.getFabric();
	if ( fabric )
		inStream.removeObjectRef( fabric, "Cloths", &cloth );
	removeSceneGroupProperty( inStream, "Cloths", cloth, ownerScene );
}

#endif // PX_USE_CLOTH_API

}}

#endif
