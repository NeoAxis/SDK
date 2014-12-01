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

#include "PsFoundation.h"
#include "PsUtilities.h"
#include "CmPhysXCommon.h"

#include "PxMetaDataObjects.h"
#include "PxPhysicsAPI.h"

using namespace physx;

PX_PHYSX_CORE_API PxGeometryType::Enum PxShapeGeometryPropertyHelper::getGeometryType(const PxShape* inShape) const { return inShape->getGeometryType(); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxBoxGeometry& geometry) const { return inShape->getBoxGeometry( geometry ); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxSphereGeometry& geometry) const { return inShape->getSphereGeometry( geometry ); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxCapsuleGeometry& geometry) const { return inShape->getCapsuleGeometry( geometry ); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxPlaneGeometry& geometry) const { return inShape->getPlaneGeometry( geometry ); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxConvexMeshGeometry& geometry) const { return inShape->getConvexMeshGeometry( geometry ); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxTriangleMeshGeometry& geometry) const { return inShape->getTriangleMeshGeometry( geometry ); }
PX_PHYSX_CORE_API bool PxShapeGeometryPropertyHelper::getGeometry(const PxShape* inShape, PxHeightFieldGeometry& geometry) const { return inShape->getHeightFieldGeometry( geometry ); }

PX_PHYSX_CORE_API void PxShapeMaterialsPropertyHelper::setMaterials(PxShape* inShape, PxMaterial*const* materials, PxU32 materialCount) const
{
	inShape->setMaterials( materials, materialCount );
}

PX_PHYSX_CORE_API PxShape* PxRigidActorShapeCollectionHelper::createShape(PxRigidActor* inActor, const PxGeometry& geometry, PxMaterial& material,
														const PxTransform& localPose) const
{
	PX_CHECK_AND_RETURN_NULL(localPose.isValid(), "PxRigidActorShapeCollectionHelper::createShape localPose is not valid.");
	return inActor->createShape( geometry, material, localPose );
}
PX_PHYSX_CORE_API PxShape* PxRigidActorShapeCollectionHelper::createShape(PxRigidActor* inActor, const PxGeometry& geometry, PxMaterial *const* materials,
														PxU32 materialCount, const PxTransform& relativePose ) const
{
	PX_CHECK_AND_RETURN_NULL(relativePose.isValid(), "PxRigidActorShapeCollectionHelper::createShape relativePose is not valid.");
	return inActor->createShape( geometry, materials, materialCount, relativePose );
}

PX_PHYSX_CORE_API PxArticulationLink*	PxArticulationLinkCollectionPropHelper::createLink(PxArticulation* inArticulation, PxArticulationLink* parent,
																	   const PxTransform& pose) const
{
	PX_CHECK_AND_RETURN_NULL(pose.isValid(), "PxArticulationLinkCollectionPropHelper::createLink pose is not valid.");
	return inArticulation->createLink( parent, pose );
}



/*
	typedef void (*TSetterType)( TObjType*, TIndexType, TPropertyType );
	typedef TPropertyType (*TGetterType)( const TObjType* inObj, TIndexType );
	*/

inline void SetNumBroadPhaseAdd( PxSimulationStatistics* inStats, PxSimulationStatistics::VolumeType data, PxU32 val ) { inStats->numBroadPhaseAdds[data] = val; }
inline PxU32 GetNumBroadPhaseAdd( const PxSimulationStatistics* inStats, PxSimulationStatistics::VolumeType data) { return inStats->numBroadPhaseAdds[data]; }


PX_PHYSX_CORE_API NumBroadPhaseAddsProperty::NumBroadPhaseAddsProperty()
	: PxIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumBroadPhaseAdds
			, PxSimulationStatistics
			, PxSimulationStatistics::VolumeType
			, PxU32> ( "NumBroadPhaseAdds", SetNumBroadPhaseAdd, GetNumBroadPhaseAdd )
{
}

inline void SetNumBroadPhaseRemove( PxSimulationStatistics* inStats, PxSimulationStatistics::VolumeType data, PxU32 val ) { inStats->numBroadPhaseRemoves[data] = val; }
inline PxU32 GetNumBroadPhaseRemove( const PxSimulationStatistics* inStats, PxSimulationStatistics::VolumeType data) { return inStats->numBroadPhaseRemoves[data]; }


PX_PHYSX_CORE_API NumBroadPhaseRemovesProperty::NumBroadPhaseRemovesProperty()
	: PxIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumBroadPhaseRemoves
			, PxSimulationStatistics
			, PxSimulationStatistics::VolumeType
			, PxU32> ( "NumBroadPhaseRemoves", SetNumBroadPhaseRemove, GetNumBroadPhaseRemove )
{
}

inline void SetNumShape( PxSimulationStatistics* inStats, PxGeometryType::Enum data, PxU32 val ) { inStats->numShapes[data] = val; }
inline PxU32 GetNumShape( const PxSimulationStatistics* inStats, PxGeometryType::Enum data) { return inStats->numShapes[data]; }


PX_PHYSX_CORE_API NumShapesProperty::NumShapesProperty()
	: PxIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumShapes
			, PxSimulationStatistics
			, PxGeometryType::Enum
			, PxU32> ( "NumShapes", SetNumShape, GetNumShape )
{
}


inline void SetNumDiscreteContactPairs( PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2, PxU32 val ) { inStats->numDiscreteContactPairs[idx1][idx2] = val; }
inline PxU32 GetNumDiscreteContactPairs( const PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2 ) { return inStats->numDiscreteContactPairs[idx1][idx2]; }
PX_PHYSX_CORE_API NumDiscreteContactPairsProperty::NumDiscreteContactPairsProperty()
								: PxDualIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumDiscreteContactPairs
															, PxSimulationStatistics
															, PxGeometryType::Enum
															, PxGeometryType::Enum
															, PxU32> ( "NumDiscreteContactPairs", SetNumDiscreteContactPairs, GetNumDiscreteContactPairs )
{
}

inline void SetNumModifiedContactPairs( PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2, PxU32 val ) { inStats->numModifiedContactPairs[idx1][idx2] = val; }
inline PxU32 GetNumModifiedContactPairs( const PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2 ) { return inStats->numModifiedContactPairs[idx1][idx2]; }
PX_PHYSX_CORE_API NumModifiedContactPairsProperty::NumModifiedContactPairsProperty()
								: PxDualIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumModifiedContactPairs
															, PxSimulationStatistics
															, PxGeometryType::Enum
															, PxGeometryType::Enum
															, PxU32> ( "NumModifiedContactPairs", SetNumModifiedContactPairs, GetNumModifiedContactPairs )
{
}

inline void SetNumSweptIntegrationPairs( PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2, PxU32 val ) { inStats->numSweptIntegrationPairs[idx1][idx2] = val; }
inline PxU32 GetNumSweptIntegrationPairs( const PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2 ) { return inStats->numSweptIntegrationPairs[idx1][idx2]; }
PX_PHYSX_CORE_API NumSweptIntegrationPairsProperty::NumSweptIntegrationPairsProperty()
								: PxDualIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumSweptIntegrationPairs
															, PxSimulationStatistics
															, PxGeometryType::Enum
															, PxGeometryType::Enum
															, PxU32> ( "NumSweptIntegrationPairs", SetNumSweptIntegrationPairs, GetNumSweptIntegrationPairs )
{
}

inline void SetNumTriggerPairs( PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2, PxU32 val ) { inStats->numTriggerPairs[idx1][idx2] = val; }
inline PxU32 GetNumTriggerPairs( const PxSimulationStatistics* inStats, PxGeometryType::Enum idx1, PxGeometryType::Enum idx2 ) { return inStats->numTriggerPairs[idx1][idx2]; }
PX_PHYSX_CORE_API NumTriggerPairsProperty::NumTriggerPairsProperty()
								: PxDualIndexedPropertyInfo<PxPropertyInfoName::PxSimulationStatistics_NumTriggerPairs
															, PxSimulationStatistics
															, PxGeometryType::Enum
															, PxGeometryType::Enum
															, PxU32> ( "NumTriggerPairs", SetNumTriggerPairs, GetNumTriggerPairs )
{
}

inline PxSimulationStatistics GetStats( const PxScene* inScene ) { PxSimulationStatistics stats; inScene->getSimulationStatistics( stats ); return stats; }
PX_PHYSX_CORE_API SimulationStatisticsProperty::SimulationStatisticsProperty() 
	: PxReadOnlyPropertyInfo<PxPropertyInfoName::PxScene_SimulationStatistics, PxScene, PxSimulationStatistics >( "SimulationStatistics", GetStats )
{
}

#if PX_USE_PARTICLE_SYSTEM_API

inline void SetProjectionPlane( PxParticleBase* inBase, PxMetaDataPlane inPlane ) { inBase->setProjectionPlane( inPlane.normal, inPlane.distance ); } 
inline PxMetaDataPlane GetProjectionPlane( const PxParticleBase* inBase ) 
{ 
	PxMetaDataPlane retval;
	inBase->getProjectionPlane( retval.normal, retval.distance ); 
	return retval;
}

PX_PHYSX_CORE_API ProjectionPlaneProperty::ProjectionPlaneProperty() 
	: PxPropertyInfo< PxPropertyInfoName::PxParticleBase_ProjectionPlane, PxParticleBase, PxMetaDataPlane, PxMetaDataPlane >( "ProjectionPlane", SetProjectionPlane, GetProjectionPlane )
{
}

#endif // PX_USE_PARTICLE_SYSTEM_API

#if PX_USE_CLOTH_API

inline PxU32 GetNbPxClothFabric_Restvalues( const PxClothFabric* fabric ) { return fabric->getNbParticleIndices() - (fabric->getNbFibers() - 1); }
inline PxU32 GetPxClothFabric_Restvalues( const PxClothFabric* fabric, PxReal* outBuffer, PxU32 outBufLen ){ return fabric->getRestvalues( outBuffer, outBufLen ); }

PX_PHYSX_CORE_API RestvaluesProperty::RestvaluesProperty()
	: PxReadOnlyCollectionPropertyInfo<PxPropertyInfoName::PxClothFabric_Restvalues, PxClothFabric, PxReal>( "Restvalues", GetPxClothFabric_Restvalues, GetNbPxClothFabric_Restvalues )
{
}

inline PxU32 GetNbPxClothFabric_PhaseTypes( const PxClothFabric* fabric ) { return fabric->getNbPhases(); }
inline PxU32 GetPxClothFabric_PhaseTypes( const PxClothFabric* fabric, PxClothFabricPhaseType::Enum* outBuffer, PxU32 outBufLen )
{
	PxU32 numItems = PxMin( outBufLen, fabric->getNbPhases() );
	for ( PxU32 idx = 0; idx < numItems; ++idx )
		outBuffer[idx] = fabric->getPhaseType( idx );
	return numItems;
}

PX_PHYSX_CORE_API PhaseTypesProperty::PhaseTypesProperty()
	: PxReadOnlyCollectionPropertyInfo<PxPropertyInfoName::PxClothFabric_PhaseTypes, PxClothFabric, PxClothFabricPhaseType::Enum>( "PhaseTypes", GetPxClothFabric_PhaseTypes, GetNbPxClothFabric_PhaseTypes )
{
}

inline PxU32 GetNbPxCloth_PhaseSolverConfig( const PxCloth* cloth ) 
{
	return PxClothFabricPhaseType::eCOUNT - 1;  // don't count the eINVALID type
}

inline PxU32 GetPxCloth_PhaseSolverConfig( const PxCloth* cloth, PxClothPhaseSolverConfig* outBuffer, PxU32 count )
{
	PX_ASSERT(PxClothFabricPhaseType::eSTRETCHING == 1);
	PxU32 phaseType = PxClothFabricPhaseType::eSTRETCHING;
	PxU32 numItems = PxMin( GetNbPxCloth_PhaseSolverConfig( cloth ), count );
	for ( PxU32 idx = 0; idx < numItems; ++idx )
	{
		outBuffer[idx] = cloth->getPhaseSolverConfig( (PxClothFabricPhaseType::Enum)phaseType );
		phaseType++;
	}
	return numItems;
}

PX_PHYSX_CORE_API PhaseSolverConfigProperty::PhaseSolverConfigProperty()
	: PxReadOnlyCollectionPropertyInfo<PxPropertyInfoName::PxCloth_PhaseSolverConfig, PxCloth, PxClothPhaseSolverConfig>( "PhaseSolverConfig", GetPxCloth_PhaseSolverConfig, GetNbPxCloth_PhaseSolverConfig )
{
}


PX_PHYSX_CORE_API void PhaseSolverConfigProperty::set( PxCloth* cloth, const PxClothPhaseSolverConfig* data, PxU32 count )
{
	PX_ASSERT(PxClothFabricPhaseType::eSTRETCHING == 1);
	PxU32 phaseType = PxClothFabricPhaseType::eSTRETCHING;

	PxU32 numItems = PxMin( GetNbPxCloth_PhaseSolverConfig( cloth ), count );
	for ( PxU32 idx = 0; idx < numItems; ++idx )
	{
		cloth->setPhaseSolverConfig( (PxClothFabricPhaseType::Enum)phaseType, data[idx] );
		phaseType++;
	}
}

#endif // PX_USE_CLOTH_API