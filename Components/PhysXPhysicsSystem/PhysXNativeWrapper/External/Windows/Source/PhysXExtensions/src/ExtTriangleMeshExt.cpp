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


#include "PxTriangleMeshExt.h"
#include "PxMeshQuery.h"
#include "PxTriangleMeshGeometry.h"
#include "PxTriangleMesh.h"

using namespace physx;

PxFindOverlapTriangleMeshUtil::PxFindOverlapTriangleMeshUtil() : mResultsMemory(mResults), mNbResults(0), mMaxNbResults(64)
{
}

PxFindOverlapTriangleMeshUtil::~PxFindOverlapTriangleMeshUtil()
{
	if(mResultsMemory != mResults)
		delete [] mResultsMemory;
}

PxU32 PxFindOverlapTriangleMeshUtil::findOverlap(const PxGeometry& geom, const PxTransform& geomPose, const PxTriangleMeshGeometry& triGeom, const PxTransform& meshPose)
{
	bool overflow;
	PxU32 nbTouchedTris = PxMeshQuery::findOverlapTriangleMesh(geom, geomPose, triGeom, meshPose, mResultsMemory, mMaxNbResults, 0, overflow);

	if(overflow)
	{
		const PxU32 maxNbTris = triGeom.triangleMesh->getNbTriangles();
		if(!maxNbTris)
		{
			mNbResults = 0;
			return 0;
		}

		if(mMaxNbResults<maxNbTris)
		{
			if(mResultsMemory != mResults)
				delete [] mResultsMemory;

			mResultsMemory = new PxU32[maxNbTris];
			mMaxNbResults = maxNbTris;
		}
		nbTouchedTris = PxMeshQuery::findOverlapTriangleMesh(geom, geomPose, triGeom, meshPose, mResultsMemory, mMaxNbResults, 0, overflow);
		PX_ASSERT(nbTouchedTris);
		PX_ASSERT(!overflow);
	}
	mNbResults = nbTouchedTris;
	return nbTouchedTris;
}

PxU32 PxFindOverlapTriangleMeshUtil::findOverlap(const PxGeometry& geom, const PxTransform& geomPose, const PxHeightFieldGeometry& hfGeom, const PxTransform& hfPose)
{
	bool overflow = true;
	PxU32 nbTouchedTris = 0;
	do
	{
		nbTouchedTris = PxMeshQuery::findOverlapHeightField(geom, geomPose, hfGeom, hfPose, mResultsMemory, mMaxNbResults, 0, overflow);
		if(overflow)
		{
			const PxU32 maxNbTris = mMaxNbResults * 2;

			if(mResultsMemory != mResults)
				delete [] mResultsMemory;

			mResultsMemory = new PxU32[maxNbTris];
			mMaxNbResults = maxNbTris;
		}
	}while(overflow);

	mNbResults = nbTouchedTris;
	return nbTouchedTris;
}
