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


#ifndef PX_PHYSICS_GEOMUTILS_PX_CUBE_INDEX
#define PX_PHYSICS_GEOMUTILS_PX_CUBE_INDEX

#include "PxVec3.h"
#include "CmPhysXCommon.h"

namespace physx
{

	enum CubeIndex
	{
		CUBE_RIGHT,
		CUBE_LEFT,
		CUBE_TOP,
		CUBE_BOTTOM,
		CUBE_FRONT,
		CUBE_BACK,

		CUBE_FORCE_DWORD	= 0x7fffffff
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/**
	 *	Cubemap lookup function.
	 *
	 *	To transform returned uvs into mapping coordinates :
	 *	u += 1.0f;	u *= 0.5f;
	 *	v += 1.0f;	v *= 0.5f;
	 *
	 *	\fn			CubemapLookup(const PxVec3& direction, float& u, float& v)
	 *	\param		direction	[in] a direction vector
	 *	\param		u			[out] impact coordinate on the unit cube, in [-1,1]
	 *	\param		v			[out] impact coordinate on the unit cube, in [-1,1]
	 *	\return		cubemap texture index
	 */
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	CubeIndex		CubemapLookup(const PxVec3& direction, float& u, float& v);

	PX_INLINE PxU32 ComputeCubemapOffset(const PxVec3& dir, PxU32 subdiv)
	{
		float u,v;
		const CubeIndex CI = CubemapLookup(dir, u, v);

		// Remap to [0, subdiv[
		const float Coeff = 0.5f * float(subdiv-1);
		u += 1.0f;	u *= Coeff;
		v += 1.0f;	v *= Coeff;

		// Compute offset
		return PxU32(CI)*(subdiv*subdiv) + PxU32(u)*subdiv + PxU32(v);
	}

	PX_INLINE PxU32 ComputeCubemapNearestOffset(const PxVec3& dir, PxU32 subdiv)
	{
		float u,v;
		const CubeIndex CI = CubemapLookup(dir, u, v);

		// Remap to [0, subdiv[
		const float Coeff = 0.5f * float(subdiv-1);
		u += 1.0f;	u *= Coeff;
		v += 1.0f;	v *= Coeff;

		// Round to nearest
		PxU32 ui = PxU32(u);
		PxU32 vi = PxU32(v);
		const float du = u - float(ui);
		const float dv = v - float(vi);
		if(du>0.5f)	ui++;
		if(dv>0.5f)	vi++;

		// Compute offset
		return PxU32(CI)*(subdiv*subdiv) + ui*subdiv + vi;
	}

}

#endif
