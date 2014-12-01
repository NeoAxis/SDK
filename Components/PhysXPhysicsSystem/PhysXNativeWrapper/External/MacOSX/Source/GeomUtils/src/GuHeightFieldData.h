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


#ifndef PX_HEIGHTFIELD
#define PX_HEIGHTFIELD

#include "PxSimpleTypes.h"
#include "PxHeightFieldFlag.h"
#include "PxHeightFieldSample.h"
#include "PxBounds3.h"
#include "CmMetaData.h"
#include "CmSerialFramework.h"

namespace physx
{

// CA: New tiled memory layout on PS3
#ifdef __CELLOS_LV2__
#define HF_TILED_MEMORY_LAYOUT
#endif
#define HF_TILE_SIZE_U (4)	// PT: WARNING: if you change this value, you must also change it in ConvX
#define HF_TILE_SIZE_V (4)	// PT: WARNING: if you change this value, you must also change it in ConvX

namespace Gu
{

struct PX_PHYSX_COMMON_API HeightFieldData
{
// PX_SERIALIZATION
	PX_FORCE_INLINE				HeightFieldData()										{}
	PX_FORCE_INLINE				HeightFieldData(PxRefResolver& v) :	flags(Cm::PX_EMPTY)	{}
//~PX_SERIALIZATION

	//properties
						PxU32						rows;					// PT: WARNING: don't change this member's name (used in ConvX)
						PxU32						columns;				// PT: WARNING: don't change this member's name (used in ConvX)
						PxReal						rowLimit;				// PT: to avoid runtime int-to-float conversions on Xbox
						PxReal						colLimit;				// PT: to avoid runtime int-to-float conversions on Xbox
						PxReal						nbColumns;				// PT: to avoid runtime int-to-float conversions on Xbox
						PxHeightFieldSample*		samples;				// PT: WARNING: don't change this member's name (used in ConvX)
						PxReal						thickness;
						PxReal						convexEdgeThreshold;

						PxHeightFieldFlags			flags;
	EXPLICIT_PADDING(	PxU16						paddAfterFlags;)		// PT: Because PxHeightFieldFlags is 16bits. We should make PxHeightFieldFormat 16bits too.

						PxHeightFieldFormat::Enum	format;
						PxBounds3					mAABB;				

//#ifdef HF_TILED_MEMORY_LAYOUT
	// CA:  New tiled memory layout on PS3
						PxU32						rowsPadded;				// PT: WARNING: don't change this member's name (used in ConvX)
						PxU32						columnsPadded;			// PT: WARNING: don't change this member's name (used in ConvX)
						PxU32						tilesU;					// PT: WARNING: don't change this member's name (used in ConvX)
						PxU32						tilesV;					// PT: WARNING: don't change this member's name (used in ConvX)
//#endif
};

} // namespace Gu

}

#endif
