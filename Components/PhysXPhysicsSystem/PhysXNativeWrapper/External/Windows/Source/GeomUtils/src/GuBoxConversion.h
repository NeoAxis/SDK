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


#ifndef PX_BOX_CONVERSION_H
#define PX_BOX_CONVERSION_H

// PT: TODO: THIS IS A TEMPORARY FILE. IT SHOULD EVENTUALLY VANISH. DON'T PUT ANYTHING IN THERE WITHOUT ASKING ME.

#include "GuBox.h"
#include "PsMathUtils.h"

namespace physx
{

// PT: TODO: get rid of this nonsense
PX_FORCE_INLINE void buildFrom(Gu::Box& dst, const PxVec3& center, const PxVec3& extents, const PxQuat& q)
{
	dst.center	= center;
	dst.extents	= extents;
	dst.rot		= PxMat33(q);
}

PX_FORCE_INLINE void buildMatrixFromBox(Cm::Matrix34& mat34, const Gu::Box& box)
{
	mat34.base0	= box.rot.column0;
	mat34.base1	= box.rot.column1;
	mat34.base2	= box.rot.column2;
	mat34.base3	= box.center;
}

// SD: function is now the same as FastVertex2ShapeScaling::transformQueryBounds
// PT: lots of LHS in that one. TODO: revisit...
PX_INLINE Gu::Box transform(const Cm::Matrix34& transfo, const Gu::Box& box)
{
	Gu::Box ret;
	PxMat33& obbBasis = ret.rot;

	obbBasis.column0 = transfo.rotate(box.rot.column0 * box.extents.x);
	obbBasis.column1 = transfo.rotate(box.rot.column1 * box.extents.y);
	obbBasis.column2 = transfo.rotate(box.rot.column2 * box.extents.z);

	ret.center = transfo.transform(box.center);
	ret.extents = Ps::optimizeBoundingBox(obbBasis);
	return ret;
}

// PT: TODO: move this to a better place
PX_INLINE void getInverse(PxMat33& dstRot, PxVec3& dstTrans, const PxMat33& srcRot, const PxVec3& srcTrans)
{
	const PxMat33 invRot = srcRot.getInverse();
	dstTrans = invRot.transform(-srcTrans);
	dstRot = invRot;
}

}

#endif
