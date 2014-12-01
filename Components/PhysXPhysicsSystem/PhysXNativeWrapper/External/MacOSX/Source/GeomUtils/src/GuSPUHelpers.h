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

#ifndef PX_PHYSICS_GEOMUTILS_SPU_HELPERS_H
#define PX_PHYSICS_GEOMUTILS_SPU_HELPERS_H

#ifdef __SPU__
#include "CellUtil.h"

	// fetch meshData to SPU local storage
	#define	GU_FETCH_MESH_DATA(meshGeom)																																\
	const Gu::TriangleMesh* tm = static_cast<Gu::TriangleMesh*>(meshGeom.triangleMesh);																					\
	PX_ALIGN_PREFIX(16)  PxU8 meshBuffer[sizeof(InternalTriangleMeshData)+32] PX_ALIGN_SUFFIX(16);																		\
	InternalTriangleMeshData* meshData = pxMemFetchAsync<InternalTriangleMeshData>(meshBuffer, (uintptr_t)(&(tm->mMesh.mData)), sizeof(InternalTriangleMeshData), 1);	\
	pxMemFetchWait(1); // meshData

	#define GU_FETCH_HEIGHTFIELD_DATA(hfGeom)																															\
	PX_ALIGN_PREFIX(16)  PxU8 heightFieldBuffer[sizeof(Gu::HeightField)+32] PX_ALIGN_SUFFIX(16);																		\
	Gu::HeightField* heightField = pxMemFetchAsync<Gu::HeightField>(heightFieldBuffer, (uintptr_t)(hfGeom.heightField), sizeof(Gu::HeightField), 1);					\
	pxMemFetchWait(1);																																					\
	g_sampleCache.init((uintptr_t)(heightField->getData().samples), heightField->getData().tilesU);																		\
	const_cast<PxHeightFieldGeometry&>(hfGeom).heightField = heightField;

	//Because we have convex overlap, the convex shape may be cached, so check if it is on EA before DMA
	#define GU_FETCH_CONVEX_DATA(convexGeom)																				\
	PX_COMPILE_TIME_ASSERT(&((Gu::ConvexMesh*)NULL)->getHull()==NULL);														\
																															\
	Gu::ConvexMesh* cm = (Gu::ConvexMesh*)(convexGeom.convexMesh);														\
	if(isEffectiveAddress((PxU32)convexGeom.convexMesh))																	\
	{																														\
		void* convexMeshBuffer = PxAlloca(CELL_ALIGN_SIZE_16(sizeof(Gu::ConvexMesh)+32));									\
		cm = pxMemFetchAsync<Gu::ConvexMesh>(convexMeshBuffer, (uintptr_t)(cm), sizeof(Gu::ConvexHullData),1);			\
		pxMemFetchWait(1);																									\
	}																														\
																															\
	Gu::HullPolygonData* polys = const_cast<Gu::HullPolygonData*>(cm->getPolygons());										\
	if(isEffectiveAddress((PxU32)polys))																					\
	{																														\
		PxU32 nPolys = cm->getNbPolygonsFast();																			\
		const Gu::HullPolygonData* PX_RESTRICT polysEA = cm->getPolygons();												\
		const PxU32 polysSize = sizeof(Gu::HullPolygonData)*nPolys + sizeof(PxVec3)*cm->getNbVerts();						\
																															\
		void* hullBuffer = PxAlloca(CELL_ALIGN_SIZE_16(polysSize+32));														\
		polys = pxMemFetchAsync<Gu::HullPolygonData>(hullBuffer, (uintptr_t)(polysEA), polysSize, 1);						\
		pxMemFetchWait(1);																									\
	}																														\
																															\
	Gu::ConvexHullData* hull = &cm->getHull();																			\
	hull->mPolygons = polys;

#else
	#define	GU_FETCH_MESH_DATA(meshGeom)																																\
	const Gu::TriangleMesh* tm = static_cast<Gu::TriangleMesh*>(meshGeom.triangleMesh);																					\
	InternalTriangleMeshData* meshData = const_cast<InternalTriangleMeshData*>(&(tm->mMesh.mData));

	#define GU_FETCH_HEIGHTFIELD_DATA(hfGeom)																															\
	Gu::HeightField* heightField = pxMemFetchAsync<Gu::HeightField>(heightFieldBuffer, (uintptr_t)(hfGeom.heightField), sizeof(Gu::HeightField), 1);

	#define GU_FETCH_CONVEX_DATA(convexGeom)																				\
	Gu::ConvexMesh* cm = (Gu::ConvexMesh*)(convexGeom.convexMesh);	
#endif
#endif
