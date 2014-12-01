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


#ifndef PX_PHYSICS_GEOMUTILS_VEC_BOX
#define PX_PHYSICS_GEOMUTILS_VEC_BOX

/** \addtogroup geomutils
@{
*/

#include "GuVecConvex.h"
#include "PsVecTransform.h"
#include "GuConvexSupportTable.h"

namespace physx
{
namespace Gu
{
	class CapsuleV;

	void computeOBBPoints(Ps::aos::Vec3V* PX_RESTRICT pts, const Ps::aos::Vec3VArg center, const Ps::aos::Vec3VArg extents, const Ps::aos::Vec3VArg base0, const Ps::aos::Vec3VArg base1, const Ps::aos::Vec3VArg base2);

	PX_FORCE_INLINE Ps::aos::FloatV CalculateBoxMargin(const Ps::aos::Vec3VArg extent)
	{
		using namespace Ps::aos;
		
		const FloatV r0 = FloatV_From_F32(0.25f);
		const FloatV min = V3ExtractMin(extent);//FMin(V3GetX(extent), FMin(V3GetY(extent), V3GetZ(extent)));
		return FMul(min, r0);
	}

	/**
	\brief Represents an oriented bounding box. 

	As a center point, extents(radii) and a rotation. i.e. the center of the box is at the center point, 
	the box is rotated around this point with the rotation and it is 2*extents in width, height and depth.
	*/

	/**
	Box geometry

	The rot member describes the world space orientation of the box.
	The center member gives the world space position of the box.
	The extents give the local space coordinates of the box corner in the positive octant.
	Dimensions of the box are: 2*extent.
	Transformation to world space is: worldPoint = rot * localPoint + center
	Transformation to local space is: localPoint = T(rot) * (worldPoint - center)
	Where T(M) denotes the transpose of M.
	*/

	class BoxV : public ConvexV
	{
	public:
		/**
		\brief Constructor
		*/
		PX_INLINE BoxV() : ConvexV(E_BOX)
		{
		}

		/**
		\brief Constructor

		\param _center Center of the OBB
		\param _extents Extents/radii of the obb.
		\param _rot rotation to apply to the obb.
		*/
	/*	PX_INLINE BoxV(const Ps::aos::Vec3VArg _center, const Ps::aos::Vec3VArg _extents, const PxMat33Legacy& _rot) : ConvexV(E_BOX, _center), extents(_extents)
		{
			PxMat33 pxRot;
			_rot.getColumnMajor(&pxRot.column0.x);
			rot.col0 = Ps::aos::Vec3V_From_PxVec3(pxRot.column0);
			rot.col1 = Ps::aos::Vec3V_From_PxVec3(pxRot.column1);
			rot.col2 = Ps::aos::Vec3V_From_PxVec3(pxRot.column2);
		}*/

		//! Construct from center, extent and rotation
		PX_FORCE_INLINE BoxV(const Ps::aos::Vec3VArg origin, const Ps::aos::Vec3VArg extent, const Ps::aos::Mat33V& base) : 
																											ConvexV(E_BOX, origin), rot(base), extents(extent)
		{
			using namespace Ps::aos;
			const FloatV r0 = FloatV_From_F32(0.025f);
			const FloatV min = V3ExtractMin(extent);//FMin(V3GetX(extent), FMin(V3GetY(extent), V3GetZ(extent)));
			margin = FMul(min, r0);
		}

		PX_FORCE_INLINE BoxV(const Ps::aos::Vec3VArg origin, const Ps::aos::Vec3VArg extent, 
			const Ps::aos::Vec3VArg col0, const Ps::aos::Vec3VArg col1, const Ps::aos::Vec3VArg col2) : 
																									ConvexV(E_BOX, origin), extents(extent)
		{
			using namespace Ps::aos;
			const FloatV r0 = FloatV_From_F32(0.025f);
			const FloatV min = V3ExtractMin(extent);//FMin(V3GetX(extent), FMin(V3GetY(extent), V3GetZ(extent)));
			margin = FMul(min, r0);
			rot.col0 = col0;
			rot.col1 = col1;
			rot.col2 = col2;
		}

		PX_FORCE_INLINE BoxV(const Ps::aos::Vec3VArg origin, const Ps::aos::Vec3VArg extent, const Ps::aos::Mat33V& base, const Ps::aos::FloatVArg _margin) : 
																											ConvexV(E_BOX, origin, _margin), rot(base), extents(extent)
		{}																								//! Copy constructor

		//! construct from a matrix(center and rotation) + extent
		PX_FORCE_INLINE BoxV(const Ps::aos::Mat34V& mat, const Ps::aos::Vec3VArg extent) : ConvexV(E_BOX, mat.col3), rot(Ps::aos::Mat33V(mat.col0, mat.col1, mat.col2)), extents(extent)
		{}

		//! Copy constructor
		PX_FORCE_INLINE BoxV(const BoxV& other) : ConvexV(E_BOX, other.center, other.margin), rot(other.rot), extents(other.extents)
		{}

		

		/*PX_INLINE BoxV(const Box& other)
		{
			using namespace Ps::aos;
			rot = Mat33V_From_PxMat33(other.rot);
			center = Vec3V_From_PxVec3(other.center);
			extents = Vec3V_From_PxVec3(other.extents);
		}*/

		/**
		\brief Destructor
		*/
		PX_INLINE ~BoxV()
		{
		}

		//! Assignment operator
		PX_INLINE const BoxV& operator=(const BoxV& other)
		{
			rot		= other.rot;
			center	= other.center;
			extents	= other.extents;
			return *this;
		}

		/**
		\brief Setups an empty box.
		*/
		PX_INLINE void setEmpty()
		{
			using namespace Ps::aos;
			center = V3Zero();
			extents = Vec3V_From_F32(-PX_MAX_REAL);
			rot = M33Identity();
		}

		/**
		\brief Checks the box is valid.

		\return	true if the box is valid
		*/
		PX_INLINE bool isValid() const
		{
			// Consistency condition for (Center, Extents) boxes: Extents >= 0.0f
			/*if(extents.x < 0.0f)	return false;
			if(extents.y < 0.0f)	return false;
			if(extents.z < 0.0f)	return false;
		
			return true;*/

			using namespace Ps::aos;
			const Vec3V zero = V3Zero();
			return BAllEq(V3IsGrtrOrEq(extents, zero), BTTTT()) == 1;
		}

/////////////
		PX_FORCE_INLINE	void	setAxes(const Ps::aos::Vec3VArg axis0, const Ps::aos::Vec3VArg axis1, const Ps::aos::Vec3VArg axis2)
		{
			rot.col0 = axis0;
			rot.col1 = axis1;
			rot.col2 = axis2;
		}


		
		PX_INLINE	Ps::aos::Vec3V	rotate(const Ps::aos::Vec3VArg src)	const
		{
			//return rot * src;
			return Ps::aos::M33MulV3(rot, src);
		}

		PX_INLINE	Ps::aos::Vec3V	rotateInv(const Ps::aos::Vec3VArg src)	const
		{
			//return rot.transformTranspose(src);
			return Ps::aos::M33TrnspsMulV3(rot, src);
		}

		//get the world space point from the local space
		PX_INLINE	Ps::aos::Vec3V	transformFromLocalToWorld(const Ps::aos::Vec3VArg src)	const
		{
			//return rot * src + center;
			return Ps::aos::V3Add(Ps::aos::M33MulV3(rot, src), center);
		}

		PX_INLINE	Ps::aos::Vec3V	transformFromWorldToLocal(const Ps::aos::Vec3VArg src)	const
		{
			//return Inv(rot) * (src - center);
			return Ps::aos::M33TrnspsMulV3(rot, Ps::aos::V3Sub(src, center));
		}

	/*	PX_INLINE	PxTransform getTransform()	const
		{
			using namespace Ps::aos;
			PX_ALIGN(16, PxMat33 pxMat);
			PxMat33_From_Mat33V(rot, pxMat);
			PxVec3 c;
			PxVec3_From_Vec3V(center, c);
			return PxTransform(c, PxQuat(pxMat));
		}*/


		PX_INLINE Ps::aos::Vec3V computeAABBExtent() const
		{
			using namespace Ps::aos;
			return M33TrnspsMulV3(rot, extents);
		}

		/**
		Computes the obb points.
		\param		pts	[out] 8 box points
		\return		true if success
		*/
		PX_INLINE void computeBoxPoints(Ps::aos::Vec3V* PX_RESTRICT pts) const
		{
			return Gu::computeOBBPoints(pts, center, extents, rot.col0, rot.col1, rot.col2);
		}

		/**
		\brief recomputes the OBB after an arbitrary transform by a 4x4 matrix.
		\param	mtx		[in] the transform matrix
		\param	obb		[out] the transformed OBB
		*/
		PX_INLINE	void rotate(const Ps::aos::Mat34V& mtx, BoxV& obb)	const
		{
			using namespace Ps::aos;
			// The extents remain constant
			obb.extents = extents;
			// The center gets x-formed
			obb.center =M34MulV3(mtx, obb.center);
			// Combine rotations
			const Mat33V mtxR = Mat33V(mtx.col0, mtx.col1, mtx.col2);
			obb.rot =M33MulM33(mtxR, rot);
		}

		void create(const Gu::CapsuleV& capsule);

		/**
		\brief checks the OBB is inside another OBB.
		\param		box		[in] the other OBB
		*/
		PxU32 isInside(const BoxV& box)	const;

		PX_FORCE_INLINE Support getSupportMapping()const
		{
			return BoxSupport;
		}

		PX_FORCE_INLINE Support getSweepSupportMapping()const
		{
			return BoxSweepSupport;
		}

		PX_FORCE_INLINE SupportMargin getSupportMarginMapping()const
		{
			return BoxSupportMargin;
		}


		PX_FORCE_INLINE Ps::aos::Vec3V supportSweep(const Ps::aos::Vec3VArg dir)const  
		{

			using namespace Ps::aos;
			const Vec3V zero = V3Zero();
			//transfer dir into the local space of the box
			const Vec3V _dir = M33TrnspsMulV3(rot, dir);
			const Vec3V p = V3Sel(V3IsGrtr(_dir, zero), extents, V3Neg(extents));
			//transfer p into the world space
			return V3Add(center, M33MulV3(rot, p));
			
		}

		PX_FORCE_INLINE Ps::aos::Vec3V support(const Ps::aos::Vec3VArg dir)const  
		{

			using namespace Ps::aos;
			const Vec3V zero = V3Zero();
			//transfer dir into the local space of the box
			const Vec3V _dir = M33TrnspsMulV3(rot, dir);
			const Vec3V p = V3Sel(V3IsGrtr(_dir, zero), extents, V3Neg(extents));
			//const Vec3V p = V3ScaleAdd(_dir, margin, _p);
			//transfer p into the world space
			return V3Add(center, M33MulV3(rot, p));
			
		}
  


		PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin, Ps::aos::Vec3V& support)const
		{
			using namespace Ps::aos;
		
			const Vec3V zero = V3Zero();
			const Vec3V _extents = V3Sub(extents, _margin);
			//transfer dir into the local space of the box
			const Vec3V _dir = M33TrnspsMulV3(rot, dir);
			const Vec3V p = V3Sel(V3IsGrtr(_dir, zero), _extents, V3Neg(_extents));

			//transfer p into the world space
			const Vec3V ret = V3Add(center, M33MulV3(rot, p));
			support = ret;
			return ret;
		}

		//PX_FORCE_INLINE Ps::aos::Vec3V supportMargin(const Ps::aos::Vec3VArg dir, const Ps::aos::FloatVArg _margin, const Ps::aos::PsTransformV& relTra, Ps::aos::Vec3V& support)const
		//{
		//	using namespace Ps::aos;
		//
		//	const Vec3V zero = V3Zero();
		//	const Vec3V _extents = V3Sub(extents, _margin);
		//	//transfer dir into the local space of the box
		//	//const Vec3V _dir = M33TrnspsMulV3(rot, dir);
		//	const Vec3V _dir = relTra.rotate(dir);
		//	const Vec3V p = V3Sel(V3IsGrtr(_dir, zero), _extents, V3Neg(_extents));

		//	//transfer p into the world space
		//	const Vec3V ret = V3Add(center, M33MulV3(rot, p));
		//	support = ret;
		//	return ret;

		//}

	
		Ps::aos::Mat33V rot;
		Ps::aos::Vec3V extents;
	};
}	//PX_COMPILE_TIME_ASSERT(sizeof(Gu::BoxV) == 96);

}

/** @} */
#endif
