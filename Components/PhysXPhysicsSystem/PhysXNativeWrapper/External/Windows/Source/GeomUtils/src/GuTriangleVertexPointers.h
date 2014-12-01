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


#ifndef PX_TRIANGLE_VERTEX_POINTERS_H
#define PX_TRIANGLE_VERTEX_POINTERS_H

#include "GuTriangleMeshData.h"
#include "GuTriangle32.h"
#include "../../LowLevel/common/include/utils/PxcMemFetch.h"

namespace physx
{
namespace Gu
{
	struct IndexTriple16 { PxU16 p0, p1, p2; };
	struct IndexTriple32 { PxU32 p0, p1, p2; };
	class TriangleVertexPointers
	{
	public:
		PX_FORCE_INLINE const PxVec3& operator[](PxU32 x) const
		{
			return *v[x];
		}

		TriangleVertexPointers()
		{
		}

		PX_FORCE_INLINE TriangleVertexPointers(const Gu::InternalTriangleMeshData& mesh, PxU32 triangleIndex)
		{
			set(mesh, triangleIndex);
		}

		static PX_FORCE_INLINE bool has16BitIndices(const Gu::InternalTriangleMeshData& mesh)	{ return mesh.m16BitIndices; }

		PX_FORCE_INLINE void set(const Gu::InternalTriangleMeshData& mesh, PxU32 triangleIndex)
		{
			if (has16BitIndices(mesh))
			{
				const Gu::TriangleT<PxU16>& indices = (reinterpret_cast<const Gu::TriangleT<PxU16>*>(mesh.mTriangles))[triangleIndex];
				v[0] = &(mesh.mVertices[indices[0]]);
				v[1] = &(mesh.mVertices[indices[1]]);
				v[2] = &(mesh.mVertices[indices[2]]);
			}
			else
			{
				const Gu::TriangleT<PxU32>& indices = (reinterpret_cast<const Gu::TriangleT<PxU32>*>(mesh.mTriangles))[triangleIndex];
				v[0] = &(mesh.mVertices[indices[0]]);
				v[1] = &(mesh.mVertices[indices[1]]);
				v[2] = &(mesh.mVertices[indices[2]]);
			}
		}

		// AP: scaffold, needs refactoring, this code is practically a duplicate of code in MeshInterface
		// This is due to MeshInterface struct being effectively the same as InternalTriangleMeshData
		// (Why do we have both?)
		static void PX_FORCE_INLINE getTriangleVerts(
			const Gu::InternalTriangleMeshData* meshDataLocalStorage,
			PxU32 TriangleIndex, PxVec3& v0, PxVec3& v1, PxVec3& v2)
		{
			#define memFetch pxMemFetchAsync
			PxMemFetchPtr mTris = PxMemFetchPtr(meshDataLocalStorage->mTriangles);
			PxMemFetchPtr mVerts = PxMemFetchPtr(meshDataLocalStorage->mVertices);
			PxMemFetchSmallBuffer buf0, buf1, buf2;
			PxU32 i0, i1, i2;
			if (meshDataLocalStorage->m16BitIndices)
			{
				IndexTriple16* inds = memFetch<IndexTriple16>(mTris+(sizeof(PxU16)*TriangleIndex*3), 5, buf0);
				pxMemFetchWait(5);
				i0 = inds->p0; i1 = inds->p1; i2 = inds->p2;
			} 
			else 
			{ 
				IndexTriple32* inds = memFetch<IndexTriple32>(mTris+(sizeof(PxU32)*TriangleIndex*3), 5, buf0);
				pxMemFetchWait(5);
				i0 = inds->p0; i1 = inds->p1; i2 = inds->p2;
			} 

			PxVec3* v[3];
			v[0] = memFetch<PxVec3>(mVerts+i0*12+0, 5, buf0);
			v[1] = memFetch<PxVec3>(mVerts+i1*12+0, 5, buf1);
			v[2] = memFetch<PxVec3>(mVerts+i2*12+0, 5, buf2);
			pxMemFetchWait(5);
			v0 = *v[0]; v1 = *v[1]; v2 = *v[2];
			#undef memFetch
		}

		template<int N> static void PX_FORCE_INLINE getTriangleVertsN(
			const Gu::InternalTriangleMeshData* meshDataLocalStorage,
			const PxU32* PX_RESTRICT triIndices, PxU32 indexCount, PxVec3 output[N][3])
		{
			PxMemFetchPtr mTris = PxMemFetchPtr(meshDataLocalStorage->mTriangles);
			PxMemFetchPtr mVerts = PxMemFetchPtr(meshDataLocalStorage->mVertices);
			PxMemFetchSmallBuffer buf0[N], buf1[N], buf2[N], buf3[N];

			PxVec3* v[N][3];
			PX_ASSERT(indexCount <= N);
			if (meshDataLocalStorage->m16BitIndices)
			{
				IndexTriple16* inds[N];
				for (PxU32 i = 0; i < indexCount; i++)
					inds[i] = pxMemFetchAsync<IndexTriple16>(mTris+(sizeof(PxU16)*triIndices[i]*3), i/*dma tag*/, buf3[i]);

				for (PxU32 i = 0; i < indexCount; i++)
				{
					pxMemFetchWait(i);
					v[i][0] = pxMemFetchAsync<PxVec3>(mVerts+inds[i]->p0*12+0, i, buf0[i]);
					v[i][1] = pxMemFetchAsync<PxVec3>(mVerts+inds[i]->p1*12+0, i, buf1[i]);
					v[i][2] = pxMemFetchAsync<PxVec3>(mVerts+inds[i]->p2*12+0, i, buf2[i]);
				}
			} 
			else 
			{ 
				IndexTriple32* inds[N];
				for (PxU32 i = 0; i < indexCount; i++)
					inds[i] = pxMemFetchAsync<IndexTriple32>(mTris+(sizeof(PxU32)*triIndices[i]*3), i/*dma tag*/, buf3[i]);

				for (PxU32 i = 0; i < indexCount; i++)
				{
					pxMemFetchWait(i);
					v[i][0] = pxMemFetchAsync<PxVec3>(mVerts+inds[i]->p0*12+0, i, buf0[i]);
					v[i][1] = pxMemFetchAsync<PxVec3>(mVerts+inds[i]->p1*12+0, i, buf1[i]);
					v[i][2] = pxMemFetchAsync<PxVec3>(mVerts+inds[i]->p2*12+0, i, buf2[i]);
				}
			} 

			for (PxU32 i = 0; i < indexCount; i++)
			{
				pxMemFetchWait(i);
				output[i][0] = *v[i][0];
				output[i][1] = *v[i][1];
				output[i][2] = *v[i][2];
			}
		}
	private:
		const PxVec3* v[3];
	};
}
//#endif
}

#endif
