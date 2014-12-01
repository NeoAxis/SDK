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


#ifndef PX_PHYSICS_COMMON_ID_POOL
#define PX_PHYSICS_COMMON_ID_POOL

#include "Px.h"
#include "CmPhysXCommon.h"
#include "PsArray.h"

// PX_SERIALIZATION
/*
#include "CmSerialFramework.h"
#include "CmSerialAlignment.h"
*/
//~PX_SERIALIZATION

namespace physx
{
namespace Cm
{
	class IDPool
	{
		PxU32				currentID;
		Ps::Array<PxU32>	freeIDs;
	public:
// PX_SERIALIZATION
/*		IDPool(PxRefResolver& v)	{}
		void	exportExtraData(PxSerialStream& stream)
		{
			Cm::alignStream(stream, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA);
			freeIDs.exportArray(stream, false);
		}
		char*	importExtraData(char* address, PxU32& totalPadding)
		{
			address = Cm::alignStream(address, totalPadding, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA);
			address = freeIDs.importArray(address);
			return address;
		}*/
//~PX_SERIALIZATION
		IDPool() : currentID(0), freeIDs(PX_DEBUG_EXP("IDPoolFreeIDs"))	{}

		void	freeID(PxU32 id)
		{
			// Allocate on first call
			// Add released ID to the array of free IDs
			freeIDs.pushBack(id);
		}

		void	freeAll()
		{
			currentID = 0;
			freeIDs.clear();
		}

		PxU32	getNewID()
		{
			// If recycled IDs are available, use them
			const PxU32 size = freeIDs.size();
			if(size)
			{
				const PxU32 id = freeIDs[size-1]; // Recycle last ID
				freeIDs.popBack();
				return id;
			}
			// Else create a new ID
			return currentID++;
		}
	};

} // namespace Cm

}

#endif
