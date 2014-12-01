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


#ifndef PX_COLLISION_INTERNALTRIANGLEMESH
#define PX_COLLISION_INTERNALTRIANGLEMESH

#include "PxSimpleTriangleMesh.h"
#include "GuTriangleMeshData.h"
#include "../Opcode/OPC_LSSCollider.h"
#include "../Opcode/OPC_AABBCollider.h"
#include "../Opcode/OPC_OBBCollider.h"
#include "../Opcode/OPC_SphereCollider.h"
#include "../Opcode/OPC_MeshInterface.h"

namespace physx
{
	class PxInputStream;

	class PX_PHYSX_COMMON_API InternalTriangleMesh
	{
	public:
// PX_SERIALIZATION
												InternalTriangleMesh(PxRefResolver& v)	: mData(v), mOwnsMemory(0)	{}

					void						exportExtraData(PxSerialStream&);
					char*						importExtraData(char* address, PxU32& totalPadding);
		static		void						getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION
												InternalTriangleMesh();
												~InternalTriangleMesh();

					void						release();

					PxVec3*						allocateVertices(PxU32 nbVertices);
					void*						allocateTriangles(PxU32 nbTriangles, bool force32Bit = false);
					PxU16*						allocateMaterials();
					PxU32*						allocateFaceRemap();
					PxU32*						allocateAdjacencies();
	PX_FORCE_INLINE	bool						has16BitIndices()			const	{ return mData.m16BitIndices;	}	//this does not apply for mesh processing during cooking, there its always 32 bits.
	PX_FORCE_INLINE	PxU32						getNumVertices()			const	{ return mData.mNumVertices;	}
	PX_FORCE_INLINE	PxU32						getNumTriangles()			const	{ return mData.mNumTriangles;	}
	PX_FORCE_INLINE	const PxVec3*				getVertices()				const	{ return mData.mVertices;		}
	PX_FORCE_INLINE	const void*					getTriangles()				const	{ return mData.mTriangles;		}
	PX_FORCE_INLINE	const PxU16*				getMaterials()				const	{ return mMaterialIndices;		}
	PX_FORCE_INLINE	const PxU32*				getFaceRemap()				const	{ return mFaceRemap;			}
	PX_FORCE_INLINE	const PxU32*				getAdjacencies()			const	{ return mAdjacencies;			}

	PX_FORCE_INLINE	const Ice::RTreeMidphase&	getOpcodeModel()			const	{ return mData.mOpcodeModel;	}

	// Data for convex-vs-arbitrary-mesh
	PX_FORCE_INLINE	PxReal						getConvexEdgeThreshold()	const	{ return mConvexEdgeThreshold;	}
	PX_FORCE_INLINE	PxU32						getTrigSharedEdgeFlags(PxU32 trigIndex) const;
	PX_FORCE_INLINE	PxU32						getTrigSharedEdgeFlagsFromData(PxU32 trigIndex)const;
	PX_FORCE_INLINE	void						setTrigSharedEdgeFlag(PxU32 trigIndex, PxU32 edgeIndex);

	// Data for adjacencies
	PX_FORCE_INLINE	void						setTriangleAdjacency(PxU32 triangleIndex, PxU32 adjacency, PxU32 offset);

					bool						loadRTree(PxInputStream& modelData, const PxU32 meshVersion);
					void						setupMeshInterface();

				Gu::InternalTriangleMeshData	mData;
	protected:
					//@@NOT SEPARATED
					PxU16*						mMaterialIndices;		//!< the size of the array is numTriangles.
					//@@NOT SEPARATED
					PxU32*						mFaceRemap;				//!< new faces to old faces mapping (after cleaning, etc). Usage: old = faceRemap[new]

					PxU32*						mAdjacencies;			//!< Adjacency information for each face - 3 adjacent faces
					PxU32						mNumAdjacencies;        // only because of serial

public:	// only public for serial

					PxReal						mConvexEdgeThreshold;
protected:
					Ice::MeshInterface			mMeshInterface;
					PxU32						mOwnsMemory;	// PT: this should be packed as a bit with mData.m16BitIndices...

			friend class InternalTriangleMeshBuilder;
	};

PX_FORCE_INLINE PxU32 InternalTriangleMesh::getTrigSharedEdgeFlags(PxU32 trigIndex) const
	{
	if (mData.mExtraTrigData)
		return mData.mExtraTrigData[trigIndex];
	else return 0;
	}


PX_FORCE_INLINE PxU32 InternalTriangleMesh::getTrigSharedEdgeFlagsFromData(PxU32 trigIndex) const
	{
		PX_ASSERT(mData.mExtraTrigData);
		return mData.mExtraTrigData[trigIndex];
	}

PX_FORCE_INLINE void InternalTriangleMesh::setTrigSharedEdgeFlag(PxU32 trigIndex, PxU32 edgeIndex)
	{
	/*
	nibble-packed:
	PxU32 byteIndex = trigIndex >> 1;
	PxU32 flagsShift = 4 * (trigIndex & 1);	//lo or hi nibble

	extraTrigData[byteIndex].flags |=  ((1<<edgeIndex) << flagsShift);		//set new
	*/
	mData.mExtraTrigData[trigIndex] |= (1<<edgeIndex);
	}

PX_FORCE_INLINE void InternalTriangleMesh::setTriangleAdjacency(PxU32 triangleIndex, PxU32 adjacency, PxU32 offset)
	{
		PX_ASSERT(mAdjacencies);
		mAdjacencies[triangleIndex*3 + offset] = adjacency;
	}
}

#endif
