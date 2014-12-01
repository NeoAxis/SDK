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


#ifndef PX_PHYSICS_GEOMUTILS_CAPSULE
#define PX_PHYSICS_GEOMUTILS_CAPSULE

/** \addtogroup geomutils
@{
*/

#include "GuSphere.h"
#include "GuDistancePointSegment.h"

namespace physx
{
namespace Gu
{
	class Box;

/**
\brief Represents a capsule.
*/
	class Capsule : public Segment
	{
	public:
		/**
		\brief Constructor
		*/
		PX_INLINE Capsule()
		{
		}

		/**
		\brief Constructor

		\param seg Line segment to create capsule from.
		\param _radius Radius of the capsule.
		*/
		PX_INLINE Capsule(const Segment& seg, PxF32 _radius) : Segment(seg), radius(_radius)
		{
		}

		/**
		\brief Destructor
		*/
		PX_INLINE ~Capsule()
		{
		}

		/**
		 \brief tests if a sphere is contained within the capsule.
		 \param		sphere	[in] the sphere to test
		 \warning	sphere and capsule must be in same space
		 */
		PX_FORCE_INLINE	bool	contains(const Gu::Sphere& sphere)	const
								{
									const float d = radius - sphere.radius;
									if(d>=0.0f)	return Gu::distancePointSegmentSquared(*this, sphere.center) <= d*d;
									else		return false;
								}

		/**
		 \brief tests if an LSS is contained within the capsule.
		 \param		lss		[in] the capsule to test
		 \warning	both LSS must be in same space
		 */
		PX_FORCE_INLINE	bool	contains(const Gu::Capsule& lss)	const
								{
									// We check the capsule contains the two spheres at the start and end of the sweep
									return contains(Gu::Sphere(lss.p0, lss.radius)) && contains(Gu::Sphere(lss.p1, lss.radius));
								}

		PxF32	radius;
	};
}

}

/** @} */
#endif
