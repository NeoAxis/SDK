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


#ifndef PX_FOUNDATION_PSRENDEROUTPUT_H
#define PX_FOUNDATION_PSRENDEROUTPUT_H

#include "CmRenderBuffer.h"
#include "CmMatrix34.h"
#include "PsArray.h"
#include "PsNoCopy.h"
#include "foundation/PxMat44.h"
#include "PsUserAllocated.h"

namespace physx
{
namespace Cm
{
	struct DebugText;

	/**
	Output stream to fill RenderBuffer
	*/
	class PX_PHYSX_COMMON_API RenderOutput
	{
	public:

		RenderOutput(RenderBuffer& buffer) 
			: mTransform(PxMat44::createIdentity()), mBuffer(buffer) 
		{}

		enum Primitive {
			POINTS,
			LINES,
			LINESTRIP,
			TRIANGLES,
			TRIANGLESTRIP,
			TEXT
		};

		RenderOutput& operator<<(Primitive prim);
		RenderOutput& operator<<(PxU32 color); // 0xbbggrr
		RenderOutput& operator<<(const PxMat44& transform);
		RenderOutput& operator<<(const PxTransform&);

		RenderOutput& operator<<(PxVec3 vertex);
		RenderOutput& operator<<(const DebugText& text);

		PX_FORCE_INLINE PxDebugLine* reserveSegments(PxU32 nbSegments)
		{
			const PxU32 currentSize = mBuffer.mLines.size();
			mBuffer.mLines.resizeUninitialized(currentSize + nbSegments);
			return mBuffer.mLines.begin() + currentSize;
		}

		// PT: using the operators is just too slow.
		PX_FORCE_INLINE	void outputSegment(const PxVec3& v0, const PxVec3& v1)
		{
			// PT: TODO: replace pushBack with something faster like the versions below
			mBuffer.mLines.pushBack(PxDebugLine(v0, v1, mColor));

			// PT: TODO: use the "pushBackUnsafe" version or replace with ICE containers
/*			PxDebugLine& l = mBuffer.mLines.insert();
			l.pos0	= v0;
			l.pos1	= v1;
			l.color0 = l.color1 = mColor;

			PxDebugLine& l = mBuffer.mLines.pushBackUnsafe();
			l.pos0	= v0;
			l.pos1	= v1;
			l.color0 = l.color1 = mColor;*/
		}

		RenderOutput&	outputCapsule(PxF32 radius, PxF32 halfHeight, const Cm::Matrix34& absPose);

	private:

		RenderOutput& operator=(const RenderOutput&);

		Primitive		mPrim;
		PxU32			mColor;
		PxVec3			mVertex0, mVertex1;
		PxU32			mVertexCount;
		PxMat44			mTransform;
		RenderBuffer&	mBuffer;
	};

	/** debug render helper types */
	struct PX_PHYSX_COMMON_API DebugText 
	{
		DebugText(const PxVec3& position, PxReal size, const char* string, ...); 
		static const int sBufferSize = 1008; // sizeof(DebugText)==1kB
		char buffer[sBufferSize]; 
		PxVec3 position;
		PxReal size;
	};

	struct DebugBox 
	{
		explicit DebugBox(const PxVec3& extents, bool wireframe = true) 
		: minimum(-extents), maximum(extents), wireframe(wireframe) {}

		explicit DebugBox(const PxVec3& pos, const PxVec3& extents, bool wireframe = true) 
		: minimum(pos-extents), maximum(pos+extents), wireframe(wireframe) {}

		explicit DebugBox(const PxBounds3& bounds, bool wireframe = true)
		: minimum(bounds.minimum), maximum(bounds.maximum), wireframe(wireframe) {}

		PxVec3 minimum, maximum;
		bool wireframe;
	};
	PX_PHYSX_COMMON_API RenderOutput& operator<<(RenderOutput& out, const DebugBox& box);

	struct DebugArrow 
	{
		DebugArrow(const PxVec3& pos, const PxVec3& vec) 
		: base(pos), tip(pos+vec), headLength(vec.magnitude()*0.15f) {}

		DebugArrow(const PxVec3& pos, const PxVec3& vec, PxReal headLength) 
		: base(pos), tip(pos+vec), headLength(headLength) {}

		PxVec3 base, tip;
		PxReal headLength;
	};
	PX_PHYSX_COMMON_API RenderOutput& operator<<(RenderOutput& out, const DebugArrow& arrow);

	struct DebugBasis
	{
		DebugBasis(const PxVec3& extends, PxU32 colorX = PxDebugColor::eARGB_RED, 
			PxU32 colorY = PxDebugColor::eARGB_GREEN, PxU32 colorZ = PxDebugColor::eARGB_BLUE) 
		: extends(extends), colorX(colorX), colorY(colorY), colorZ(colorZ) {}
		PxVec3 extends;
		PxU32 colorX, colorY, colorZ;
	};
	PX_PHYSX_COMMON_API RenderOutput& operator<<(RenderOutput& out, const DebugBasis& basis);

	struct DebugCircle
	{
		DebugCircle(PxU32 nSegments, PxReal radius) 
		: nSegments(nSegments), radius(radius) {}
		PxU32 nSegments;
		PxReal radius;
	};
	PX_PHYSX_COMMON_API RenderOutput& operator<<(RenderOutput& out, const DebugCircle& circle);

	struct DebugArc
	{
		DebugArc(PxU32 nSegments, PxReal radius, PxReal minAngle, PxReal maxAngle) 
		: nSegments(nSegments), radius(radius), minAngle(minAngle), maxAngle(maxAngle) {}
		PxU32 nSegments;
		PxReal radius;
		PxReal minAngle, maxAngle;
	};
	PX_PHYSX_COMMON_API RenderOutput& operator<<(RenderOutput& out, const DebugArc& arc);

} // namespace Cm

}

#endif
