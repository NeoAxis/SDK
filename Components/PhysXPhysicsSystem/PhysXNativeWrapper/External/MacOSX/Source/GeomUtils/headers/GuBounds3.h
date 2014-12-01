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


#ifndef PX_PHYSICS_GEOMUTILS_BOUNDS
#define PX_PHYSICS_GEOMUTILS_BOUNDS

/** \addtogroup geomutils
@{
*/


#include "CmPhysXCommon.h"
#include "PsVecMath.h"

namespace physx
{
namespace Gu
{
class PxBounds3V
{
public:

	/**
	\brief Default constructor, not performing any initialization for performance reason.
	\remark Use empty() function below to construct empty bounds.
	*/
	PX_FORCE_INLINE PxBounds3V()	{}

	/**
	\brief Construct from two bounding points
	*/
	PX_FORCE_INLINE PxBounds3V(const Ps::aos::Vec3VArg minimum, const Ps::aos::Vec3VArg maximum);

	/**
	\brief Return empty bounds. 
	*/
	static PX_FORCE_INLINE PxBounds3V empty();

	/**
	\brief returns the AABB containing v0 and v1.
	\param v0 first point included in the AABB.
	\param v1 second point included in the AABB.
	*/
	static PX_FORCE_INLINE PxBounds3V boundsOfPoints(const Ps::aos::Vec3VArg v0, const Ps::aos::Vec3VArg v1);

	/**
	\brief returns the AABB from center and extents vectors.
	\param center Center vector
	\param extent Extents vector
	*/
	static PX_FORCE_INLINE PxBounds3V centerExtents(const Ps::aos::Vec3VArg center, const Ps::aos::Vec3VArg extent);

	/**
	\brief Construct from center, extent, and (not necessarily orthogonal) basis
	*/
	static PX_INLINE PxBounds3V basisExtent(const Ps::aos::Vec3VArg center, const Ps::aos::Mat33V& basis, const Ps::aos::Vec3VArg extent);

	/**
	\brief Construct from pose and extent
	*/
	static PX_INLINE PxBounds3V poseExtent(const PxTransform& pose, const Ps::aos::Vec3VArg extent);

	/**
	\brief Sets empty to true
	*/
	PX_FORCE_INLINE void setEmpty();

	/**
	\brief Sets infinite bounds
	*/
	PX_FORCE_INLINE void setInfinite();

	/**
	\brief expands the volume to include v
	\param v Point to expand to.
	*/
	PX_FORCE_INLINE void include(const Ps::aos::Vec3VArg v);

	/**
	\brief expands the volume to include b.
	\param b Bounds to perform union with.
	*/
	PX_FORCE_INLINE void include(const PxBounds3V& b);

	PX_FORCE_INLINE bool isEmpty() const;

	/**
	\brief indicates whether the intersection of this and b is empty or not.
	\param b Bounds to test for intersection.
	*/
	PX_FORCE_INLINE bool intersects(const PxBounds3V& b) const;


	/**
	\brief indicates if these bounds contain v.
	\param v Point to test against bounds.
	*/
	PX_FORCE_INLINE bool contains(const Ps::aos::Vec3VArg v) const;

	/**
	 \brief	checks a box is inside another box.
	 \param	box		the other AABB
	 */
	PX_FORCE_INLINE bool isInside(const PxBounds3V& box) const;

	/**
	\brief returns the center of this axis aligned box.
	*/
	PX_FORCE_INLINE Ps::aos::Vec3V getCenter() const;

	/**
	\brief returns the dimensions (width/height/depth) of this axis aligned box.
	*/
	PX_FORCE_INLINE Ps::aos::Vec3V getDimensions() const;

	/**
	\brief returns the extents, which are half of the width/height/depth.
	*/
	PX_FORCE_INLINE Ps::aos::Vec3V getExtents() const;

	/**
	\brief scales the AABB.
	\param scale Factor to scale AABB by.
	*/
	PX_FORCE_INLINE void scale(const Ps::aos::FloatVArg scale);

	/** 
	fattens the AABB in all 3 dimensions by the given distance. 
	*/
	PX_FORCE_INLINE void fatten(const Ps::aos::FloatVArg distance);

	/** 
	checks that the AABB values are not NaN
	*/

	PX_FORCE_INLINE bool isFinite() const;

	Ps::aos::Vec3V minimum, maximum;
};


/////////////////////////////////////////////////////////////////////////
PX_FORCE_INLINE PxBounds3V::PxBounds3V(const Ps::aos::Vec3VArg minimum, const Ps::aos::Vec3VArg maximum)
: minimum(minimum), maximum(maximum)
{
}

PX_FORCE_INLINE PxBounds3V PxBounds3V::empty()
{
	using namespace Ps::aos;
	const Vec3V minimum = FloatV_From_F32(PX_MAX_REAL);
	const Vec3V maximum = V3Neg(minimum);
	return PxBounds3V(minimum, maximum);
}

PX_FORCE_INLINE bool PxBounds3V::isFinite() const
{
	return Ps::aos::isValidVec3V(minimum) & Ps::aos::isValidVec3V(maximum);
}

PX_FORCE_INLINE PxBounds3V PxBounds3V::boundsOfPoints(const Ps::aos::Vec3VArg v0, const Ps::aos::Vec3VArg v1)
{
	const Ps::aos::Vec3V min = Ps::aos::V3Min(v0, v1);
	const Ps::aos::Vec3V max = Ps::aos::V3Max(v0, v1);
	return PxBounds3V(min, max);
}

PX_FORCE_INLINE PxBounds3V PxBounds3V::centerExtents(const Ps::aos::Vec3VArg center, const Ps::aos::Vec3VArg extent)
{
	const Ps::aos::Vec3V min = Ps::aos::V3Sub(center, extent);
	const Ps::aos::Vec3V max = Ps::aos::V3Add(center, extent);
	return PxBounds3V(min, max);
}

PX_INLINE PxBounds3V PxBounds3V::basisExtent(const Ps::aos::Vec3VArg center, const Ps::aos::Mat33V& basis, const Ps::aos::Vec3VArg extent)
{
	using namespace Ps::aos;
	// extended basis vectors
	const Vec3V c0 = V3Abs(V3Scale(basis.col0, V3GetX(extent)));
	const Vec3V c1 = V3Abs(V3Scale(basis.col1, V3GetY(extent)));
	const Vec3V c2 = V3Abs(V3Scale(basis.col2, V3GetZ(extent)));
	const Vec3V w = V3Add(c0, V3Add(c1, c2));
	return PxBounds3V(V3Sub(center, w), V3Add(center, w));
}

PX_INLINE PxBounds3V PxBounds3V::poseExtent(const PxTransform& pose, const Ps::aos::Vec3VArg extent)
{
	const PxMat33 q = PxMat33(pose.q);
	const Ps::aos::Vec3V p = Ps::aos::Vec3V_From_PxVec3(pose.p);
	const Ps::aos::Mat33V qv = Ps::aos::Mat33V_From_PxMat33(q);
	return basisExtent(p, qv, extent);
}

PX_FORCE_INLINE void PxBounds3V::setEmpty()
{

	minimum = Ps::aos::FloatV_From_F32(PX_MAX_REAL);
	maximum = Ps::aos::V3Neg(minimum);
}

PX_FORCE_INLINE void PxBounds3V::setInfinite()
{
	minimum = Ps::aos::FloatV_From_F32(-PX_MAX_REAL);
	maximum = Ps::aos::V3Neg(minimum);
}

PX_FORCE_INLINE void PxBounds3V::include(const Ps::aos::Vec3VArg v)
{
	PX_ASSERT(isFinite());
	minimum = Ps::aos::V3Min(v, minimum);
	maximum = Ps::aos::V3Max(v, maximum);
}

PX_FORCE_INLINE void PxBounds3V::include(const PxBounds3V& b)
{
	PX_ASSERT(isFinite());
	minimum = Ps::aos::V3Min(b.minimum, minimum);
	maximum = Ps::aos::V3Min(b.maximum, maximum);
}

PX_FORCE_INLINE bool PxBounds3V::isEmpty() const
{
	PX_ASSERT(isFinite());
	// Consistency condition for (Min, Max) boxes: minimum < maximum
	using namespace Ps::aos;
	return BAllEq(BAnyTrue3(V3IsGrtr(minimum, maximum)), BTTTT()) == 1;
}

PX_FORCE_INLINE bool PxBounds3V::intersects(const PxBounds3V& b) const
{
	PX_ASSERT(isFinite() && b.isFinite());
	using namespace Ps::aos;
	const BoolV notIntersect = BAnyTrue3(BOr(V3IsGrtr(b.minimum, maximum), V3IsGrtr(minimum, b.maximum)));
	return (BAllEq(notIntersect, BFFFF()) == 1);
}

PX_FORCE_INLINE bool PxBounds3V::contains(const Ps::aos::Vec3VArg v) const
{
	PX_ASSERT(isFinite());

	using namespace Ps::aos;
	return BAllEq(BAnd(V3IsGrtr(v, minimum), V3IsGrtr(maximum, v)), BTTTT()) == 1;
}

PX_FORCE_INLINE bool PxBounds3V::isInside(const PxBounds3V& box) const
{
	PX_ASSERT(isFinite() && box.isFinite());
	using namespace Ps::aos;
	const BoolV inside = BAllTrue3(BAnd(V3IsGrtr(box.minimum, minimum), V3IsGrtr(box.maximum, maximum)));
	return BAllEq(inside, BTTTT())==1;
}

PX_FORCE_INLINE Ps::aos::Vec3V PxBounds3V::getCenter() const
{
	PX_ASSERT(isFinite());
	const Ps::aos::Vec3V half= Ps::aos::FloatV_From_F32(0.5f);
	return Ps::aos::V3Mul(Ps::aos::V3Add(minimum, maximum), half);
}

PX_FORCE_INLINE Ps::aos::Vec3V PxBounds3V::getDimensions() const
{
	PX_ASSERT(isFinite());
	return Ps::aos::V3Sub(maximum, minimum);
}

PX_FORCE_INLINE Ps::aos::Vec3V PxBounds3V::getExtents() const
{
	PX_ASSERT(isFinite());
	const Ps::aos::FloatV half = Ps::aos::FloatV_From_F32(0.5f);
	return Ps::aos::V3Scale(getDimensions(), half);
}

PX_FORCE_INLINE void PxBounds3V::scale(const Ps::aos::FloatVArg scale)
{
	PX_ASSERT(isFinite());
	*this = centerExtents(getCenter(), Ps::aos::V3Scale(getExtents(), scale));
}

PX_FORCE_INLINE void PxBounds3V::fatten(const Ps::aos::FloatVArg distance)
{
	PX_ASSERT(isFinite());
	minimum = Ps::aos::V3Sub(minimum, distance);
	maximum = Ps::aos::V3Add(maximum, distance);
}

}

/**
\brief gets the transformed bounds of the passed AABB (resulting in a bigger AABB).
\param[in] matrix Transform to apply, can contain scaling as well
\param[in] bounds The bounds to transform.

*/
PX_INLINE Gu::PxBounds3V transform(const Ps::aos::Mat33V& matrix, const Gu::PxBounds3V& bounds)
{
	using namespace Gu;
	PX_ASSERT(bounds.isFinite());
	return bounds.isEmpty() ? bounds :
		PxBounds3V::basisExtent(Ps::aos::M33MulV3(matrix, bounds.getCenter()), matrix, bounds.getExtents());
}
//
///**
//\brief gets the transformed bounds of the passed AABB (resulting in a bigger AABB).
//\param[in] transform Transform to apply, can contain scaling as well
//\param[in] bounds The bounds to transform.
//*/
PX_INLINE Gu::PxBounds3V transform(const PxTransform& transform, const Gu::PxBounds3V& bounds)
{
	using namespace Gu;
	PX_ASSERT(bounds.isFinite());
	using namespace Ps::aos;
	const PxMat33 mat(transform.q);
	const Vec3V p =Vec3V_From_PxVec3(transform.p);
	const Mat33V q = Mat33V_From_PxMat33(mat);
	const Vec3V c = V3Add(p, M33MulV3(q, bounds.getCenter()));
	return bounds.isEmpty() ? bounds :
		PxBounds3V::basisExtent(c, q, bounds.getExtents());
}

}

#endif