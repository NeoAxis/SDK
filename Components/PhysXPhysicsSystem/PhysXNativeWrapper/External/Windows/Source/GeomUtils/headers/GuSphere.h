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


#ifndef PX_PHYSICS_GEOMUTILS_SPHERE
#define PX_PHYSICS_GEOMUTILS_SPHERE
/** \addtogroup geomutils
@{
*/

#include "PxVec3.h"
#include "CmPhysXCommon.h"

namespace physx
{

/**
\brief Enum to control the sphere generation method from a set of points.
*/
struct PxBSphereMethod
{
	enum Enum
	{
		eNONE,
		eGEMS,
		eMINIBALL,

		eFORCE_DWORD	= 0x7fffffff
	};
};

/**
\brief Represents a sphere defined by its center point and radius.
*/
namespace Gu
{
	class Sphere
	{
	public:
		/**
		\brief Constructor
		*/
		PX_INLINE Sphere()
		{
		}

		/**
		\brief Constructor
		*/
		PX_INLINE Sphere(const PxVec3& _center, PxF32 _radius) : center(_center), radius(_radius)
		{
		}
		/**
		\brief Copy constructor
		*/
		PX_INLINE Sphere(const Sphere& sphere) : center(sphere.center), radius(sphere.radius)
		{
		}
		/**
		\brief Destructor
		*/
		PX_INLINE ~Sphere()
		{
		}

		PX_INLINE	void	set(const PxVec3& _center, float _radius)		{ center = _center; radius = _radius;	}

		/**
		\brief Checks the sphere is valid.

		\return		true if the sphere is valid
		*/
		PX_INLINE bool isValid() const
		{
			// Consistency condition for spheres: Radius >= 0.0f
			return radius >= 0.0f;
		}

		/**
		\brief Tests if a point is contained within the sphere.

		\param[in] p the point to test
		\return	true if inside the sphere
		*/
		PX_INLINE bool contains(const PxVec3& p) const
		{
			return (center-p).magnitudeSquared() <= radius*radius;
		}

		/**
		\brief Tests if a sphere is contained within the sphere.

		\param		sphere	[in] the sphere to test
		\return		true if inside the sphere
		*/
		PX_INLINE bool contains(const Sphere& sphere)	const
		{
			// If our radius is the smallest, we can't possibly contain the other sphere
			if(radius < sphere.radius)	return false;
			// So r is always positive or null now
			const float r = radius - sphere.radius;
			return (center - sphere.center).magnitudeSquared() <= r*r;
		}

		/**
		\brief Tests if a box is contained within the sphere.

		\param		minimum		[in] minimum value of the box
		\param		maximum		[in] maximum value of the box
		\return		true if inside the sphere
		*/
		PX_INLINE bool contains(const PxVec3& minimum, const PxVec3& maximum) const
		{
			// I assume if all 8 box vertices are inside the sphere, so does the whole box.
			// Sounds ok but maybe there's a better way?
			const PxF32 R2 = radius * radius;
			PxVec3 p;
			p.x=maximum.x; p.y=maximum.y; p.z=maximum.z;	if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=minimum.x;									if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=maximum.x; p.y=minimum.y;					if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=minimum.x;									if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=maximum.x; p.y=maximum.y; p.z=minimum.z;	if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=minimum.x;									if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=maximum.x; p.y=minimum.y;					if((center-p).magnitudeSquared()>=R2)	return false;
			p.x=minimum.x;									if((center-p).magnitudeSquared()>=R2)	return false;

			return true;
		}

		/**
		\brief Tests if the sphere intersects another sphere

		\param		sphere	[in] the other sphere
		\return		true if spheres overlap
		*/
		PX_INLINE bool intersect(const Sphere& sphere) const
		{
			const PxF32 r = radius + sphere.radius;
			return (center - sphere.center).magnitudeSquared() <= r*r;
		}

		PxVec3	center;		//!< Sphere's center
		PxF32	radius;		//!< Sphere's radius
	};
}

}

/** @} */
#endif
