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


#ifndef PX_PHYSICS_GEOMUTILS_PX_GEOMETRY_UNION
#define PX_PHYSICS_GEOMUTILS_PX_GEOMETRY_UNION

#include "CmPhysXCommon.h"
#include <stddef.h>
#include "PxBoxGeometry.h"
#include "PxSphereGeometry.h"
#include "PxCapsuleGeometry.h"
#include "PxPlaneGeometry.h"
#include "PxConvexMeshGeometry.h"
#include "PxTriangleMeshGeometry.h"
#include "PxHeightFieldGeometry.h"
#include "CmMetaData.h"

namespace physx
{

class PxSimpleTriangleMesh;
class PxBounds;

namespace Gu
{
	struct ConvexHullData;
	struct InternalTriangleMeshData;
	struct HeightFieldData;
	class GeometryUnion;
}

struct PxConvexMeshGeometryLL: public PxConvexMeshGeometry
{
	const Gu::ConvexHullData*			hullData;
};

struct MaterialIndicesStruct
{

	MaterialIndicesStruct() : indices(NULL), numIndices(0)
	{
	}

	~MaterialIndicesStruct()
	{
		deallocate();
	}

	void allocate(PxU32 size)
	{
		indices = (PxU16*)PX_ALLOC(sizeof(PxU16) * size, PX_DEBUG_EXP("MaterialIndicesStruct::allocate"));
		numIndices = size;
	}

	void deallocate()
	{
		PX_FREE(indices);
		numIndices = 0;
	}
	PxU16* indices; // the remap table for material index
	PxU16  numIndices; //the size of the remap table
};

struct PxTriangleMeshGeometryLL: public PxTriangleMeshGeometry
{
	const Gu::InternalTriangleMeshData*	meshData;
	const PxU16*						materialIndices;
	MaterialIndicesStruct				materials;	
};

struct PxHeightFieldGeometryLL : public PxHeightFieldGeometry
{
	const Gu::HeightFieldData*			heightFieldData;			
	MaterialIndicesStruct				materials;
};

// We sometimes overload capsule code for spheres, so every sphere should have 
// valid capsule data (height = 0). This is preferable to a typedef so that we
// can maintain traits separately for a sphere, but some care is required to deal
// with the fact that when a reference to a capsule is extracted, it may have its
// type field set to eSPHERE

template <typename T>
struct PxcGeometryTraits
{
	enum {TypeID = PxGeometryType::eINVALID };
};
template <typename T> struct PxcGeometryTraits<const T> { enum { TypeID = PxcGeometryTraits<T>::TypeID }; };

template <> struct PxcGeometryTraits<PxBoxGeometry>				{ enum { TypeID = PxGeometryType::eBOX }; };
template <> struct PxcGeometryTraits<PxSphereGeometry>			{ enum { TypeID = PxGeometryType::eSPHERE }; };
template <> struct PxcGeometryTraits<PxCapsuleGeometry>			{ enum { TypeID = PxGeometryType::eCAPSULE }; };
template <> struct PxcGeometryTraits<PxPlaneGeometry>			{ enum { TypeID = PxGeometryType::ePLANE }; };
template <> struct PxcGeometryTraits<PxConvexMeshGeometryLL>	{ enum { TypeID = PxGeometryType::eCONVEXMESH }; };
template <> struct PxcGeometryTraits<PxTriangleMeshGeometryLL>	{ enum { TypeID = PxGeometryType::eTRIANGLEMESH }; };
template <> struct PxcGeometryTraits<PxHeightFieldGeometryLL> 	{ enum { TypeID = PxGeometryType::eHEIGHTFIELD }; };
template<class T> PX_INLINE void checkType(const Gu::GeometryUnion& geometry);
template<> PX_INLINE void checkType<PxCapsuleGeometry>(const Gu::GeometryUnion& geometry);
template<> PX_INLINE void checkType<const PxCapsuleGeometry>(const Gu::GeometryUnion& geometry);


namespace Gu
{

class InvalidGeometry: public PxGeometry
{
public:
	PX_CUDA_CALLABLE PX_INLINE InvalidGeometry() :	PxGeometry(PxGeometryType::eINVALID) {}
};

class PX_PHYSX_COMMON_API GeometryUnion
{
public:
// PX_SERIALIZATION
	GeometryUnion(PxRefResolver& v)	{}
	static	void	getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION

	PX_CUDA_CALLABLE PX_INLINE GeometryUnion() { reinterpret_cast<InvalidGeometry&>(geometry) = InvalidGeometry(); }


	PX_CUDA_CALLABLE PX_INLINE const PxGeometry&		get()		const	{ return reinterpret_cast<const PxGeometry&>(geometry);				}
	PX_CUDA_CALLABLE PX_INLINE PxGeometryType::Enum	getType()		const	{ return reinterpret_cast<const PxGeometry&>(geometry).getType();	}

	PX_CUDA_CALLABLE void	set(const PxGeometry& g);

	template<class Geom> PX_CUDA_CALLABLE PX_FORCE_INLINE Geom& get()
	{
		checkType<Geom>(*this);
		return reinterpret_cast<Geom&>(geometry);
	}

	template<class Geom> PX_CUDA_CALLABLE PX_FORCE_INLINE const Geom& get() const
	{
		checkType<Geom>(*this);
		return reinterpret_cast<Geom&>(geometry);
	}
	void computeBounds(const PxTransform& transform, const PxBounds3* PX_RESTRICT localSpaceBounds, PxVec3& origin, PxVec3& extent) const;	//AABB in world space.

	PxF32 computeBoundsWithCCDThreshold(const PxTransform& transform, const PxBounds3* PX_RESTRICT localSpaceBounds, PxVec3& origin, PxVec3& extent) const;	//AABB in world space.

	PxF32 computeInSphereRadius(const PxTransform& transform, PxVec3& center) const;

private:

	union {
		PxU8	box[sizeof(PxBoxGeometry)];
		PxU8	sphere[sizeof(PxSphereGeometry)];
		PxU8	capsule[sizeof(PxCapsuleGeometry)];
		PxU8	plane[sizeof(PxPlaneGeometry)];
		PxU8	convex[sizeof(PxConvexMeshGeometryLL)];
		PxU8	mesh[sizeof(PxTriangleMeshGeometryLL)];
		PxU8	heightfield[sizeof(PxHeightFieldGeometryLL)];
		PxU8	invalid[sizeof(InvalidGeometry)];
	} geometry;
};


}  // namespace Gu

template<class T> PX_CUDA_CALLABLE PX_FORCE_INLINE void checkType(const Gu::GeometryUnion& geometry)
{
	PxcGeometryTraits<T> traits;
	traits = traits;
	PX_ASSERT(static_cast<PxU32>(geometry.getType()) == static_cast<PxU32>(PxcGeometryTraits<T>::TypeID));
}

template<> PX_CUDA_CALLABLE PX_FORCE_INLINE void checkType<PxCapsuleGeometry>(const Gu::GeometryUnion& geometry)
{
	PX_ASSERT(geometry.getType() == PxGeometryType::eCAPSULE || geometry.getType() == PxGeometryType::eSPHERE);
}

template<> PX_CUDA_CALLABLE PX_FORCE_INLINE void checkType<const PxCapsuleGeometry>(const Gu::GeometryUnion& geometry)
{
	PX_ASSERT(geometry.getType()== PxGeometryType::eCAPSULE || geometry.getType() == PxGeometryType::eSPHERE);
}

// the shape structure relies on punning capsules and spheres 

//#pragma warning "TODO: GNU C don't like this hack"
#ifndef ANDROID
PX_COMPILE_TIME_ASSERT(offsetof(PxCapsuleGeometry, radius) == offsetof(PxSphereGeometry, radius));
#endif

}

#endif
