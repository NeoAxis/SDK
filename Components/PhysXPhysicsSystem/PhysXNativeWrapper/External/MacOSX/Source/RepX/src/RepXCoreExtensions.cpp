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
#ifndef REPX_REPXCOREEXTENSIONS_H
#define REPX_REPXCOREEXTENSIONS_H

namespace physx { 
	namespace shdfnd2 {}
	namespace repx {
	using namespace physx::shdfnd2;
}}
#include "RepXVisitorWriter.h"
#include "RepX.h"
#include "PxRigidStatic.h"
#include "PxShape.h"
#include "PxBoxGeometry.h"
#include "PxScene.h"
#include "PxMetaDataObjects.h"
#include "CmIO.h"
#include "PxStreamOperators.h"
#include "PxConvexMeshGeometry.h"
#include "PxSphereGeometry.h"
#include "PxPlaneGeometry.h"
#include "PxCapsuleGeometry.h"
#include "PxHeightFieldGeometry.h"
#include "PxTriangleMesh.h"
#include "PxCooking.h"
#include "PxHeightField.h"
#include "RepXMemoryAllocator.h"
#include "PxConvexMesh.h"
#include "PxArticulation.h"
#include "PxArticulationLink.h"
#include "PxArticulationJoint.h"
#include "RepXExtensionImpl.h"
#include "PxHeightFieldDesc.h"
#include "PxArticulationLink.h"
#include "PxProfileFoundationWrapper.h"
#include "PsUtilities.h"

namespace physx { namespace repx {
	using namespace physx::profile;

typedef ProfileHashMap< const TRepXId, const PxArticulationLink* > TArticulationLinkLinkMap;
typedef PxReadOnlyPropertyInfo<PxPropertyInfoName::PxArticulationLink_InboundJoint, PxArticulationLink, PxArticulationJoint *> TIncomingJointPropType;

}}

#include "RepXCoreExtensions.h"					//The core extension definition file.
#include "RepXImpl.h"							//Utility functions used by everything
#include "RepXCoreExtensionSerializer.h"		//specializations for writing
#include "RepXCoreExtensionDeserializer.h"		//specializations for reading
//The implementation must be included last as it needs to see all of the serialization
//specializations going on ahead of it when it calls write/readAllProperties.
#include "RepXExtensionImpl.h"					//The implementation of the extensions.



namespace physx { namespace repx {

	//*************************************************************
	//	Actual extension implementations
	//*************************************************************
	struct PxMaterialRepXExtension : public RepXExtensionImpl<PxMaterial>
	{
		PxMaterialRepXExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxMaterial>( inCallback ) {}
		virtual PxMaterial* allocateObject( RepXInstantiationArgs& inArgs )
		{
			return inArgs.mPhysics->createMaterial(0, 0, 0);
		}
	};

	struct PxRigidStaticRepXExtension : public RepXExtensionImpl<PxRigidStatic>
	{
		PxRigidStaticRepXExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxRigidStatic>( inCallback ) {}
		virtual PxRigidStatic* allocateObject( RepXInstantiationArgs& inArgs )
		{
			return inArgs.mPhysics->createRigidStatic( PxTransform::createIdentity() );
		}
	};
	
	struct PxRigidDynamicRepXExtension : public RepXExtensionImpl<PxRigidDynamic>
	{
		PxRigidDynamicRepXExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxRigidDynamic>( inCallback ) {}
		virtual PxRigidDynamic* allocateObject( RepXInstantiationArgs& inArgs )
		{
			return inArgs.mPhysics->createRigidDynamic( PxTransform::createIdentity() );
		}
	};

	template<typename TTriIndexElem>
	inline void writeTriangle( MemoryBuffer& inTempBuffer, const Triangle<TTriIndexElem>& inTriangle )
	{
		inTempBuffer << inTriangle.mIdx0 
			<< " " << inTriangle.mIdx1
			<< " " << inTriangle.mIdx2;
	}


	PxU32 materialAccess( const PxTriangleMesh* inMesh, PxU32 inIndex ) { return inMesh->getTriangleMaterialIndex( inIndex ); }
	template<typename TDataType>
	void writeDatatype( MemoryBuffer& inTempBuffer, const TDataType& inType ) { inTempBuffer << inType; }

	struct PxTriangleMeshExtension  : public RepXExtensionImpl<PxTriangleMesh>
	{
		PxTriangleMeshExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxTriangleMesh>( inCallback ) {}
		virtual void objectToFileImpl( const PxTriangleMesh* mesh, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			bool hasMatIndex = mesh->getTriangleMaterialIndex(0) != 0xffff;
			//PxU32 numVerts = mesh->getNbVertices();
			writeBuffer( inWriter, inTempBuffer, 2, mesh->getVertices(), mesh->getNbVertices(), "Points", writePxVec3 );
			bool isU16 = mesh->has16BitTriangleIndices();
			PxU32 triCount = mesh->getNbTriangles();
			if ( isU16 )
				writeBuffer( inWriter, inTempBuffer, 2, reinterpret_cast<const Triangle<PxU16>* >( mesh->getTriangles() ), triCount, "Triangles", writeTriangle<PxU16> );
			else
				writeBuffer( inWriter, inTempBuffer, 2, reinterpret_cast<const Triangle<PxU32>* >( mesh->getTriangles() ), triCount, "Triangles", writeTriangle<PxU32> );
			if ( hasMatIndex )
				writeBuffer( inWriter, inTempBuffer, 6, mesh, materialAccess, triCount, "materialIndices", writeDatatype<PxU32> );
		}
		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			//We can't do a simple inverse; we *have* to cook data to get a mesh.
			PxTriangleMeshDesc theDesc;
			readStridedBufferProperty<PxVec3>( inReader, "points", theDesc.points, inAllocator);
			readStridedBufferProperty<Triangle<PxU32> >( inReader, "triangles", theDesc.triangles, inAllocator);
			PxU32 triCount;
			readStridedBufferProperty<PxMaterialTableIndex>( inReader, "materialIndices", theDesc.materialIndices, triCount, inAllocator);
			//Now cook the bastard.
			TMemoryPoolManager theManager(inAllocator.getAllocator());
			MemoryBuffer theTempBuf( &theManager );
			PX_ASSERT( inArgs.mCooker );
			inArgs.mCooker->cookTriangleMesh( theDesc, theTempBuf );
			PxTriangleMesh* theMesh = inArgs.mPhysics->createTriangleMesh( theTempBuf );
			return createRepXObject( theMesh );
		}
		//We never allow this to be called.
		virtual PxTriangleMesh* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }
	};

	struct PxHeightFieldExtension : public RepXExtensionImpl<PxHeightField>
	{
		PxHeightFieldExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxHeightField>( inCallback ) {}
		//Conversion from scene object to descriptor.
		virtual void objectToFileImpl( const PxHeightField* inHeightField, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			PxHeightFieldDesc theDesc;

			theDesc.nbRows					= inHeightField->getNbRows();
			theDesc.nbColumns				= inHeightField->getNbColumns();
			theDesc.format					= inHeightField->getFormat();
			theDesc.samples.stride			= inHeightField->getSampleStride();
			theDesc.samples.data			= NULL;
			theDesc.thickness				= inHeightField->getThickness();
			theDesc.convexEdgeThreshold		= inHeightField->getConvexEdgeThreshold();
			theDesc.flags					= inHeightField->getFlags();

			PxU32 theCellCount = inHeightField->getNbRows() * inHeightField->getNbColumns();
			PxU32 theSampleStride = sizeof( PxHeightFieldSample );
			PxU32 theSampleBufSize = theCellCount * theSampleStride;
			PxHeightFieldSample* theSamples = reinterpret_cast< PxHeightFieldSample*> ( inTempBuffer.mManager->allocate( theSampleBufSize ) );
			inHeightField->saveCells( theSamples, theSampleBufSize );
			theDesc.samples.data = theSamples;
			writeAllProperties( &theDesc, inWriter, inTempBuffer, *inIdMap );
			writeStridedBufferProperty<PxHeightFieldSample>( inWriter, inTempBuffer, "samples", theDesc.samples, theDesc.nbRows * theDesc.nbColumns, 6, writeHeightFieldSample);
			inTempBuffer.mManager->deallocate( reinterpret_cast<PxU8*>(theSamples) );
		}

		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			PxHeightFieldDesc theDesc;
			readAllProperties( inArgs, inReader, &theDesc, inAllocator, *inIdMap );
			//Now read the data...
			PxU32 count = 0; //ignored becaues numRows and numColumns tells the story
			readStridedBufferProperty<PxHeightFieldSample>( inReader, "samples", theDesc.samples, count, inAllocator);
			PxHeightField* retval = inArgs.mPhysics->createHeightField( theDesc );
			return createRepXObject( retval );
		}

		virtual PxHeightField* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }
	};

	struct PxConvexMeshExtension  : public RepXExtensionImpl<PxConvexMesh>
	{
		PxConvexMeshExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxConvexMesh>( inCallback ) {}
		virtual void objectToFileImpl( const PxConvexMesh* mesh, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			writeBuffer( inWriter, inTempBuffer, 2, mesh->getVertices(), mesh->getNbVertices(), "points", writePxVec3 );
		}

		//Conversion from scene object to descriptor.
		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			PxConvexMeshDesc theDesc;
			readStridedBufferProperty<PxVec3>( inReader, "points", theDesc.points, inAllocator);
			theDesc.flags = PxConvexFlag::eCOMPUTE_CONVEX;
			TMemoryPoolManager theManager(inAllocator.getAllocator());
			MemoryBuffer theTempBuf( &theManager );
			PX_ASSERT( inArgs.mCooker );
			inArgs.mCooker->cookConvexMesh( theDesc, theTempBuf );
			PxConvexMesh* theMesh = inArgs.mPhysics->createConvexMesh( theTempBuf );
			return createRepXObject( theMesh );
		}

		virtual PxConvexMesh* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }
	};

#if PX_USE_CLOTH_API
	void writeFabricPhaseType( PxOutputStream& stream, const PxU32& phaseType )
	{
		const PxU32ToName* conversion = PxEnumTraits<PxClothFabricPhaseType::Enum>().NameConversion;
		for ( const PxU32ToName* conv = conversion; conv->mName != NULL; ++conv )
			if ( conv->mValue == phaseType ) stream << conv->mName;
	}

	template<> struct StrToImpl<PxClothFabricPhaseType::Enum> {
	void strto( PxClothFabricPhaseType::Enum& datatype,const char*& ioData )
	{
		const PxU32ToName* conversion = PxEnumTraits<PxClothFabricPhaseType::Enum>().NameConversion;
		eatwhite( ioData );
		char buffer[512];
		nullTerminateWhite(ioData, buffer, 512 );
		for ( const PxU32ToName* conv = conversion; conv->mName != NULL; ++conv )
			if ( 0 == physx::PxStricmp( buffer, conv->mName ) )
			{
				datatype = static_cast<PxClothFabricPhaseType::Enum>( conv->mValue );
				return;
			}
	}
	};
#endif // PX_USE_CLOTH_API

#if PX_USE_CLOTH_API

	struct PxClothFabricExtension : public RepXExtensionImpl<PxClothFabric>
	{
		PxClothFabricExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxClothFabric>( inCallback ) {}
		virtual void objectToFileImpl( const PxClothFabric* data, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			
			FoundationWrapper& wrapper( inTempBuffer.mManager->getWrapper() );
			PxU32 numParticles = data->getNbParticles();
			PxU32 numPhases = data->getNbPhases();
			PxU32 numRestvalues = data->getNbRestvalues();
			PxU32 numSets = data->getNbSets();
			PxU32 numFibers = data->getNbFibers();
			PxU32 numIndices = data->getNbParticleIndices();

			ProfileArray<PxU8> dataBuffer( wrapper );
			dataBuffer.resize( PxMax( PxMax( PxMax( numPhases, numFibers ), numRestvalues), numIndices ) * sizeof( PxU32 ) );
			PxU32* indexPtr( reinterpret_cast<PxU32*>( dataBuffer.begin() ) );

			writeProperty( inWriter, *inIdMap, inTempBuffer, "NbParticles", numParticles );

			data->getPhases( indexPtr, numPhases );
			writeBuffer( inWriter, inTempBuffer, 18, indexPtr, PtrAccess<PxU32>, numPhases, "Phases", BasicDatatypeWrite<PxU32> );

			PX_COMPILE_TIME_ASSERT( sizeof( PxClothFabricPhaseType::Enum ) == sizeof( PxU32 ) );
			for ( PxU32 idx = 0; idx < numPhases; ++idx )
				indexPtr[idx] = static_cast<PxU32>( data->getPhaseType( idx ) );
			writeBuffer( inWriter, inTempBuffer, 18, indexPtr, PtrAccess<PxU32>, numPhases, "PhaseTypes", writeFabricPhaseType );

			PX_COMPILE_TIME_ASSERT( sizeof( PxReal ) == sizeof( PxU32 ) );
			PxReal* realPtr = reinterpret_cast< PxReal* >( indexPtr );
			data->getRestvalues( realPtr, numRestvalues );
			writeBuffer( inWriter, inTempBuffer, 18, realPtr, PtrAccess<PxReal>, numRestvalues, "Restvalues", BasicDatatypeWrite<PxReal> );

			data->getSets( indexPtr, numSets );
			writeBuffer( inWriter, inTempBuffer, 18, indexPtr, PtrAccess<PxU32>, numSets, "Sets", BasicDatatypeWrite<PxU32> );

			data->getFibers( indexPtr, numFibers );
			writeBuffer( inWriter, inTempBuffer, 18, indexPtr, PtrAccess<PxU32>, numFibers, "Fibers", BasicDatatypeWrite<PxU32> );

			data->getParticleIndices( indexPtr, numIndices );
			writeBuffer( inWriter, inTempBuffer, 18, indexPtr, PtrAccess<PxU32>, numIndices, "ParticleIndices", BasicDatatypeWrite<PxU32> );
		}

		//Conversion from scene object to descriptor.
		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			PxU32 strideIgnored = 0;

			PxU32 numParticles;
			readProperty( inReader, "NbParticles", numParticles );

			PxU32 numPhases = 0;
			void* phases = NULL;
			void* phaseTypes = NULL;
			readStridedBufferProperty<PxU32>( inReader, "Phases", phases, strideIgnored, numPhases, inAllocator );
			readStridedBufferProperty<PxClothFabricPhaseType::Enum>( inReader, "PhaseTypes", phaseTypes, strideIgnored, numPhases, inAllocator );

			PxU32 numRestvalues = 0;
			void* restvalues = NULL;
			readStridedBufferProperty<PxF32>( inReader, "Restvalues", restvalues, strideIgnored, numRestvalues, inAllocator );

			PxU32 numSets = 0;
			void* sets = NULL;
			readStridedBufferProperty<PxU32>( inReader, "Sets", sets, strideIgnored, numSets, inAllocator );

			PxU32 numFibers = 0;
			void* fibers = NULL;
			readStridedBufferProperty<PxU32>( inReader, "Fibers", fibers, strideIgnored, numFibers, inAllocator );

			PxU32 numIndices = 0;
			void* indices = NULL;
			readStridedBufferProperty<PxU32>( inReader, "ParticleIndices", indices, strideIgnored, numIndices, inAllocator );

			PxClothFabric* newFabric = inArgs.mPhysics->createClothFabric( numParticles, numPhases, reinterpret_cast<PxU32*>( phases ), 
				reinterpret_cast< PxClothFabricPhaseType::Enum* >( phaseTypes ), numRestvalues, reinterpret_cast<PxReal*>( restvalues ), 
				numSets, reinterpret_cast<PxU32*>( sets ), reinterpret_cast<PxU32*>( fibers ), reinterpret_cast<PxU32*>( indices ) );

			PX_ASSERT( newFabric );
			return createRepXObject( newFabric );
		}

		virtual PxClothFabric* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }	
	};
	
	void clothParticleWriter( PxOutputStream& stream, const PxClothParticle& particle )
	{
		stream << particle.pos;
		stream << " ";
		stream << particle.invWeight;
	}

	template<> struct StrToImpl<PxClothParticle> {
	void strto( PxClothParticle& datatype,const char*& ioData )
	{
		StrToImpl<PxF32>().strto( datatype.pos[0], ioData );
		StrToImpl<PxF32>().strto( datatype.pos[1], ioData );
		StrToImpl<PxF32>().strto( datatype.pos[2], ioData );
		StrToImpl<PxF32>().strto( datatype.invWeight, ioData );
	}
	};

	void clothSphereWriter( PxOutputStream& stream, const PxClothCollisionSphere& sphere )
	{
		stream << sphere.pos;
		stream << " ";
		stream << sphere.radius;
	}

	void clothPlaneWriter( PxOutputStream& stream, const PxClothCollisionPlane& plane )
	{
		stream << plane.normal;
		stream << " ";
		stream << plane.distance;
	}
	
	template<> struct StrToImpl<PxClothCollisionSphere> {
	void strto( PxClothCollisionSphere& datatype,const char*& ioData )
	{
		StrToImpl<PxF32>().strto( datatype.pos[0], ioData );
		StrToImpl<PxF32>().strto( datatype.pos[1], ioData );
		StrToImpl<PxF32>().strto( datatype.pos[2], ioData );
		StrToImpl<PxF32>().strto( datatype.radius, ioData );
	}
	};

	struct PxClothExtension : public RepXExtensionImpl<PxCloth>
	{
		PxClothExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxCloth>( inCallback ) {}
		//virtual PxCloth*			createCloth(const PxTransform& globalPose, PxClothFabric& fabric, const PxClothParticle* particles, const PxClothCollisionData& collData, PxClothFlags flags) = 0;
		virtual void objectToFileImpl( const PxCloth* data, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			PxClothReadData* readData( const_cast<PxCloth*>( data )->lockClothReadData() );
			writeBuffer( inWriter, inTempBuffer, 4, readData->particles, PtrAccess<PxClothParticle>, data->getNbParticles(), "Particles", clothParticleWriter );
			readData->unlock();

			writeReference( inWriter, *inIdMap, "Fabric", data->getFabric() );

			PxClothFlags clothFlags( data->getClothFlags() );
			FoundationWrapper& wrapper( inTempBuffer.mManager->getWrapper() );
			ProfileArray<PxU8> dataBuffer( wrapper );

			const PxCloth& cloth = *data;
			PxU32 numSpheres = cloth.getNbCollisionSpheres();
			PxU32 numIndices = 2*cloth.getNbCollisionSpherePairs();
			PxU32 numPlanes = cloth.getNbCollisionPlanes();
			PxU32 numConvexes = cloth.getNbCollisionConvexes();
			PxU32 sphereBytes = numSpheres * sizeof( PxClothCollisionSphere );
			PxU32 pairBytes = numIndices * sizeof( PxU32 );
			PxU32 planesBytes = numPlanes * sizeof(PxClothCollisionPlane);
			PxU32 convexBytes = numConvexes * sizeof(PxU32);

			dataBuffer.resize( sphereBytes + pairBytes + planesBytes + convexBytes );
			PxClothCollisionSphere* sphereBuffer = reinterpret_cast<PxClothCollisionSphere*>( dataBuffer.begin() );
			PxU32* indexBuffer = reinterpret_cast<PxU32*>(sphereBuffer + numSpheres);
			PxClothCollisionPlane* planeBuffer = reinterpret_cast<PxClothCollisionPlane*>(indexBuffer + numIndices);
			PxU32* convexBuffer = reinterpret_cast<PxU32*>(planeBuffer + numPlanes);

			data->getCollisionData( sphereBuffer, indexBuffer, planeBuffer, convexBuffer );
			writeBuffer( inWriter, inTempBuffer, 4, sphereBuffer, PtrAccess<PxClothCollisionSphere>, numSpheres, "CollisionSpheres", clothSphereWriter );
			writeBuffer( inWriter, inTempBuffer, 18, indexBuffer, PtrAccess<PxU32>, numIndices, "CollisionSphereIndexes", BasicDatatypeWrite<PxU32> );
			writeBuffer( inWriter, inTempBuffer, 4, planeBuffer, PtrAccess<PxClothCollisionPlane>, numSpheres, "CollisionPlanes", clothPlaneWriter );
			writeBuffer( inWriter, inTempBuffer, 18, convexBuffer, PtrAccess<PxU32>, numIndices, "CollisionConvexMasks", BasicDatatypeWrite<PxU32> );
			writeFlagsProperty( inWriter, inTempBuffer, "ClothFlags", clothFlags, PxEnumTraits<PxClothFlag::Enum>().NameConversion );	
			PxU32 numVirtualParticles = data->getNbVirtualParticles();
			PxU32 numWeightTableEntries = data->getNbVirtualParticleWeights();
			PxU32 totalNeeded = static_cast<PxU32>( PxMax( numWeightTableEntries * sizeof( PxVec3 ), numVirtualParticles * sizeof( PxU32 ) * 4 ) );
			if ( dataBuffer.size() < totalNeeded )
				dataBuffer.resize( totalNeeded );
			PxVec3* weightTableEntries = reinterpret_cast<PxVec3*>( dataBuffer.begin() );
			data->getVirtualParticleWeights( weightTableEntries );
			writeBuffer( inWriter, inTempBuffer, 6, weightTableEntries, PtrAccess<PxVec3>, numWeightTableEntries, "VirtualParticleWeightTableEntries", BasicDatatypeWrite<PxVec3> );
			PxU32* virtualParticles = reinterpret_cast<PxU32*>( dataBuffer.begin() );
			data->getVirtualParticles( virtualParticles );
			writeBuffer( inWriter, inTempBuffer, 18, virtualParticles, PtrAccess<PxU32>, numVirtualParticles, "VirtualParticles", BasicDatatypeWrite<PxU32> );
			//ug.  Now write the rest of the object data that the meta data generator got.
			writeAllProperties( data, inWriter, inTempBuffer, *inIdMap );
		}
		
		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			PxU32 strideIgnored;
			PxU32 numParticles;
			void* particles = NULL;
			PxU32 numCollisionSpheres;
			void* collisionSpheres = NULL;
			PxU32 numCollisionSphereIndexes;
			void* collisionSphereIndexes = NULL;
			PxU32 numVirtualParticleWeightTableEntries;
			void* virtualParticleWeightTableEntries = NULL;
			PxU32 numVirtualParticles;
			void* virtualParticles = NULL;
			PxClothFlags flags;
			PxClothFabric* fabric = NULL;
			readReference<PxClothFabric>( inReader, *inIdMap, "Fabric", fabric );
			
			REPX_REPORT_ERROR_IF( fabric, RepXErrorCode::eInvalidParameters, "Fabric" );
			readStridedBufferProperty<PxClothParticle>( inReader, "Particles", particles, strideIgnored, numParticles, inAllocator );
			readStridedBufferProperty<PxClothCollisionSphere>( inReader, "CollisionSpheres", collisionSpheres, strideIgnored, numCollisionSpheres, inAllocator );
			readStridedBufferProperty<PxU32>( inReader, "CollisionSphereIndexes", collisionSphereIndexes, strideIgnored, numCollisionSphereIndexes, inAllocator );
			readStridedBufferProperty<PxVec3>( inReader, "VirtualParticleWeightTableEntries", virtualParticleWeightTableEntries, strideIgnored, numVirtualParticleWeightTableEntries, inAllocator );
			readStridedBufferProperty<PxU32>( inReader, "VirtualParticles", virtualParticles, strideIgnored, numVirtualParticles, inAllocator );
			readFlagsProperty( inReader, inAllocator, "ClothFlags", PxEnumTraits<PxClothFlag::Enum>().NameConversion, flags );
			PxTransform initialPose( PxTransform::createIdentity() );
			if ( fabric != NULL )
			{
				PxClothCollisionData theData;
				theData.numPairs = numCollisionSphereIndexes;
				theData.numSpheres = numCollisionSpheres;
				theData.spheres = reinterpret_cast<PxClothCollisionSphere*>( collisionSpheres );
				theData.pairIndexBuffer = reinterpret_cast<PxU32*>( collisionSphereIndexes );

				PxCloth* cloth = inArgs.mPhysics->createCloth( initialPose, *fabric, reinterpret_cast<PxClothParticle*>( particles ), theData, flags );
				readAllProperties( inArgs, inReader, cloth, inAllocator, *inIdMap );

				if ( numVirtualParticles && numVirtualParticleWeightTableEntries )
				{
					cloth->setVirtualParticles( numVirtualParticles, reinterpret_cast<PxU32*>( virtualParticles )
												, numVirtualParticleWeightTableEntries, reinterpret_cast<PxVec3*>( virtualParticleWeightTableEntries ) );
				}
				return createRepXObject( cloth );
			}
			return RepXObject();
		}

		virtual PxCloth* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }	
	};

#endif // PX_USE_CLOTH_API
	
	struct PxArticulationExtension  : public RepXExtensionImpl<PxArticulation>
	{
		PxArticulationExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxArticulation>( inCallback ) {}
		virtual PxArticulation* allocateObject( RepXInstantiationArgs& inArgs ) { return inArgs.mPhysics->createArticulation(); }
		virtual void objectToFileImpl( const PxArticulation* inObj, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			TNameStack nameStack( inTempBuffer.mManager->mWrapper );
			TArticulationLinkLinkMap linkMap( inTempBuffer.mManager->mWrapper );
			RepXVisitorWriter<PxArticulation> writer( nameStack, inWriter, inObj, inTempBuffer, *inIdMap, &linkMap );
			RepXPropertyFilter<RepXVisitorWriter<PxArticulation> > theOp( writer );
			visitAllProperties<PxArticulation>( theOp );
		}
	};

	struct PxAggregateExtension :  public RepXExtensionImpl<PxAggregate>
	{
	PxAggregateExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<PxAggregate>( inCallback ) {}
	virtual void objectToFileImpl( const PxAggregate* data, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
	{
		PxArticulationLink *link = NULL;
		inWriter.addAndGotoChild( "Actors" );
		for(PxU32 i = 0; i < data->getNbActors(); ++i)
		{
			PxActor* actor;

			if(data->getActors(&actor, 1, i))
			{
				link = actor->isArticulationLink();
			}

			if(link && !link->getInboundJoint() )
			{
				writeProperty( inWriter, *inIdMap, inTempBuffer, "PxArticulationRef",  &link->getArticulation());			
			}
			else if( !link )
			{
				writeProperty( inWriter, *inIdMap, inTempBuffer, "PxActorRef", inIdMap->getIdForLiveObject(actor));			
			}
		}

		inWriter.leaveChild( );
		
		writeProperty( inWriter, *inIdMap, inTempBuffer, "NumActors", data->getNbActors() );
		writeProperty( inWriter, *inIdMap, inTempBuffer, "MaxNbActors", data->getMaxNbActors() );
		writeProperty( inWriter, *inIdMap, inTempBuffer, "SelfCollision", data->getSelfCollision() );
		
		writeAllProperties( data, inWriter, inTempBuffer, *inIdMap );
	}

	virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
	{
		PxU32 numActors;
		readProperty( inReader, "NumActors", numActors );
		PxU32 maxNbActors;
		readProperty( inReader, "MaxNbActors", maxNbActors );
		if(numActors == 0 || maxNbActors == 0)
		{
			RepXObject();
		}
		bool selfCollision;
		readProperty( inReader, "SelfCollision", selfCollision );

		PxAggregate* theAggregate = inArgs.mPhysics->createAggregate(maxNbActors, selfCollision);
		readAllProperties( inArgs, inReader, theAggregate, inAllocator, *inIdMap );
		
		inReader.pushCurrentContext();
		if ( inReader.gotoChild( "Actors" ) )
		{
			inReader.pushCurrentContext();
			for( bool matSuccess = inReader.gotoFirstChild(); matSuccess;
						matSuccess = inReader.gotoNextSibling() )
			{
				const char* actorType = inReader.getCurrentItemName();
				if ( 0 == physx::PxStricmp( actorType, "PxActorRef" ) ) 
				{
					PxActor *actor = NULL;
					readReference<PxActor>( inReader, *inIdMap, actor );
					REPX_REPORT_ERROR_IF( actor, RepXErrorCode::eInvalidParameters, "PxActorRef" );
					if(actor)
					{
						PxScene *currScene = actor->getScene();
						if(currScene)
						{
							currScene->removeActor(*actor);
						}
						theAggregate->addActor(*actor);
					}
				}
				else if ( 0 == physx::PxStricmp( actorType, "PxArticulationRef" ) ) 
				{
					PxArticulation* articulation = NULL;
					readReference<PxArticulation>( inReader, *inIdMap, articulation );
					REPX_REPORT_ERROR_IF( articulation, RepXErrorCode::eInvalidParameters, "PxArticulationRef" );
					if(articulation)
					{
						PxScene *currScene = articulation->getScene();
						if(currScene)
						{
							currScene->removeArticulation(*articulation);
						}
						theAggregate->addArticulation(*articulation);
					}
				}	
			}
			inReader.popCurrentContext();
			inReader.leaveChild();
		}
		inReader.popCurrentContext();

		return createRepXObject(theAggregate);
	}

	virtual PxAggregate* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }	
	};
	
#if PX_USE_PARTICLE_SYSTEM_API

	template<typename TParticleType>
	inline TParticleType* createParticles( PxPhysics& physics, PxU32 maxParticles, bool perParticleRestOffset )
	{
		return NULL;
	}

	template<>
	inline PxParticleSystem* createParticles<PxParticleSystem>(PxPhysics& physics, PxU32 maxParticles, bool perParticleRestOffset)
	{
		return physics.createParticleSystem( maxParticles, perParticleRestOffset );
	}
	
	template<>
	inline PxParticleFluid* createParticles<PxParticleFluid>(PxPhysics& physics, PxU32 maxParticles, bool perParticleRestOffset)
	{
		return physics.createParticleFluid( maxParticles, perParticleRestOffset );
	}

	template<typename TParticleType>
	struct PxParticleExtension : RepXExtensionImpl<TParticleType>
	{
		PxParticleExtension( PxAllocatorCallback& inCallback ) : RepXExtensionImpl<TParticleType>( inCallback ) {}
		virtual void objectToFileImpl( const TParticleType* data, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			PxParticleReadData* readData( const_cast<TParticleType*>( data )->lockParticleReadData() );
			writeProperty( inWriter, *inIdMap, inTempBuffer, "NbParticles",			readData->numValidParticles );
			writeProperty( inWriter, *inIdMap, inTempBuffer, "ValidParticleRange",	readData->validParticleRange );
			PxParticleReadDataFlags readFlags(data->getParticleReadDataFlags());

			if(readData->validParticleRange > 0)
			{
				writeBuffer( inWriter, inTempBuffer, 8 , readData->validParticleBitmap, PtrAccess<PxU32>, ((readData->validParticleRange-1) >> 5) + 1 ,"ValidParticleBitmap",	BasicDatatypeWrite<PxU32> );

				writeStridedBufferProperty<PxVec3>( inWriter, inTempBuffer, "PositionBuffer", readData->positionBuffer, readData->numValidParticles, 6, writePxVec3);
								
				if(readFlags & PxParticleReadDataFlag::eVELOCITY_BUFFER)
				{
					writeStridedBufferProperty<PxVec3>( inWriter, inTempBuffer, "VelocityBuffer", readData->velocityBuffer, readData->numValidParticles, 6, writePxVec3);
				}
				if(readFlags & PxParticleReadDataFlag::eREST_OFFSET_BUFFER)
				{
					writeStridedBufferProperty<PxF32>( inWriter, inTempBuffer, "RestOffsetBuffer", readData->restOffsetBuffer, readData->numValidParticles, 6, BasicDatatypeWrite<PxF32>);
				}
			}
			readData->unlock();

			writeProperty( inWriter, *inIdMap, inTempBuffer, "MaxParticles", data->getMaxParticles() );	

			PxParticleBaseFlags baseFlags(data->getParticleBaseFlags());
			writeFlagsProperty( inWriter, inTempBuffer, "ParticleBaseFlags", baseFlags, PxEnumTraits<PxParticleBaseFlag::Enum>().NameConversion );	

			writeFlagsProperty( inWriter, inTempBuffer, "ParticleReadDataFlags", readFlags, PxEnumTraits<PxParticleReadDataFlag::Enum>().NameConversion );	

			PxVec3	normal;
			PxReal	distance;
			data->getProjectionPlane(normal, distance);
			PxMetaDataPlane plane(normal, distance);
			writeProperty( inWriter, *inIdMap, inTempBuffer, "ProjectionPlane",			plane);		

			writeAllProperties( data, inWriter, inTempBuffer, *inIdMap );
		}	

		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			PxU32 strideIgnored = 0;
			PxU32 numbParticles;
			readProperty( inReader, "NbParticles", numbParticles );

			PxU32 validParticleRange;
			readProperty( inReader, "ValidParticleRange", validParticleRange );

			PxU32 numWrite;
			void* tempValidParticleBitmap = NULL;
			readStridedBufferProperty<PxU32>( inReader, "ValidParticleBitmap", tempValidParticleBitmap, strideIgnored, numWrite, inAllocator );
			PxU32 *validParticleBitmap = reinterpret_cast<PxU32*>( tempValidParticleBitmap );

			void* tempPosBuf = NULL;
			readStridedBufferProperty<PxVec3>( inReader, "PositionBuffer", tempPosBuf, strideIgnored, numWrite, inAllocator );
			PxStrideIterator<const PxVec3> posBuffer(reinterpret_cast<const PxVec3*> (tempPosBuf));

			void* tempVelBuf = NULL;
			readStridedBufferProperty<PxVec3>( inReader, "VelocityBuffer", tempVelBuf, strideIgnored, numWrite, inAllocator );
			PxStrideIterator<const PxVec3> velBuffer(reinterpret_cast<const PxVec3*> (tempVelBuf));

			void* tempRestBuf = NULL;
			readStridedBufferProperty<PxVec3>( inReader, "RestOffsetBuffer", tempRestBuf, strideIgnored, numWrite, inAllocator );
			PxStrideIterator<const PxF32> restBuffer(reinterpret_cast<const PxF32*> (tempRestBuf));

			Ps::Array<PxU32>	validIndexBuf;
			Ps::Array<PxVec3>	validPosBuf, validVelBuf;
			Ps::Array<PxF32>	validRestBuf;

			bool perParticleRestOffset = !!tempRestBuf;
			bool bVelBuff = !!tempVelBuf;

			if (validParticleRange > 0)
			{
				for (PxU32 w = 0; w <= (validParticleRange-1) >> 5; w++)
				{
					for (PxU32 b = validParticleBitmap[w]; b; b &= b-1)
					{
						PxU32	index = (w<<5|physx::shdfnd::lowestSetBit(b));
						validIndexBuf.pushBack(index);
						validPosBuf.pushBack(posBuffer[index]);
						if(bVelBuff)
							validVelBuf.pushBack(velBuffer[index]);
						if(perParticleRestOffset)
							validRestBuf.pushBack(restBuffer[index]);
					}			
				}          

				PX_ASSERT(validIndexBuf.size() == numbParticles);
				
				PxU32 maxParticleNum;
				readProperty( inReader, "MaxParticles", maxParticleNum );
				TParticleType* theParticle( createParticles<TParticleType>( *inArgs.mPhysics, maxParticleNum, perParticleRestOffset ) );
				PX_ASSERT( theParticle );
				readAllProperties( inArgs, inReader, theParticle, inAllocator, *inIdMap );

				PxParticleBaseFlags baseFlags;
				readFlagsProperty( inReader, inAllocator, "ParticleBaseFlags", PxEnumTraits<PxParticleBaseFlag::Enum>().NameConversion, baseFlags );

				PxU32 flagData = 1;
				for(PxU32 i = 0; i < 16; i++)
				{		
					flagData = 1 << i;
					if( !(flagData & PxParticleBaseFlag::ePER_PARTICLE_REST_OFFSET) && (!!((PxU32)baseFlags  & flagData)) )
					{
						theParticle->setParticleBaseFlag((PxParticleBaseFlag::Enum)flagData, true);
					}					
				}

				PxParticleReadDataFlags readFlags;
				readFlagsProperty( inReader, inAllocator, "ParticleReadDataFlags", PxEnumTraits<PxParticleReadDataFlag::Enum>().NameConversion, readFlags );
				for(PxU32 i = 0; i < 16; i++)
				{
					flagData = 1 << i;
					if( !!((PxU32)readFlags  & flagData) )
					{
						theParticle->setParticleReadDataFlag((PxParticleReadDataFlag::Enum)flagData, true);
					}					
				}

				PxParticleCreationData creationData;
				creationData.numParticles = numbParticles;
				creationData.indexBuffer = PxStrideIterator<const PxU32>(validIndexBuf.begin());
				creationData.positionBuffer = PxStrideIterator<const PxVec3>(validPosBuf.begin());
				if(bVelBuff)
					creationData.velocityBuffer = PxStrideIterator<const PxVec3>(validVelBuf.begin());
				if(perParticleRestOffset)
					creationData.restOffsetBuffer = PxStrideIterator<const PxF32>(validRestBuf.begin());

				theParticle->createParticles(creationData);	

				return createRepXObject( theParticle );
			}
			else
			{
				return RepXObject();
			}

		}
		virtual TParticleType* allocateObject( RepXInstantiationArgs& inArgs ) { return NULL; }	
	};

#endif // #if PX_USE_PARTICLE_SYSTEM_API

	template<typename TObjType>
	struct SpecificExtensionAllocator : ExtensionAllocator
	{
		static RepXExtension* specific_allocator(PxAllocatorCallback& inCallback) 
		{ 
			return PX_PROFILE_NEW( inCallback, TObjType )(inCallback); 
		}
		SpecificExtensionAllocator() : ExtensionAllocator( specific_allocator ) {}
	};

	static ExtensionAllocator gAllocators[] = 
	{
		SpecificExtensionAllocator<PxMaterialRepXExtension>(),
		SpecificExtensionAllocator<PxRigidStaticRepXExtension>(),
		SpecificExtensionAllocator<PxRigidDynamicRepXExtension>(),
		SpecificExtensionAllocator<PxTriangleMeshExtension>(),
		SpecificExtensionAllocator<PxHeightFieldExtension>(),
		SpecificExtensionAllocator<PxConvexMeshExtension>(),
		SpecificExtensionAllocator<PxArticulationExtension>(),
		SpecificExtensionAllocator<PxAggregateExtension>(),
#if PX_USE_CLOTH_API
		SpecificExtensionAllocator<PxClothFabricExtension>(),
		SpecificExtensionAllocator<PxClothExtension>(),
#endif
#if PX_USE_PARTICLE_SYSTEM_API
		SpecificExtensionAllocator<PxParticleExtension<PxParticleSystem> >(),
		SpecificExtensionAllocator<PxParticleExtension<PxParticleFluid> >(),
#endif
	};
	
	static PxU32 gAllocatorCount = PX_ARRAY_SIZE( gAllocators );
	
	PxU32 getNumCoreExtensions() { return gAllocatorCount; }
	PxU32 createCoreExtensions( RepXExtension** outExtensions, PxU32 outBufferSize, PxAllocatorCallback& inCallback )
	{
		PxU32 extCount = PxMin( outBufferSize, gAllocatorCount );
		for ( PxU32 idx =0; idx < extCount; ++idx )
			outExtensions[idx] = gAllocators[idx].allocateExtension(inCallback);
		return extCount;
	}
} }
#endif
