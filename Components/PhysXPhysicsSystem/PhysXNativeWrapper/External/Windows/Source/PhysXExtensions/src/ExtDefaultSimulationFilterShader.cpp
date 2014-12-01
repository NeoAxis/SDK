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


#include "PxDefaultSimulationFilterShader.h"
#include "PsIntrinsics.h"
#include "PsAllocator.h"
#include "PsInlineArray.h"
#include "PxShape.h"
#include "CmPhysXCommon.h"

using namespace physx;

namespace
{
	#define GROUP_SIZE	32

	struct PxCollisionBitMap
	{
		PX_INLINE PxCollisionBitMap() : enable(true) {}

		bool operator()() const { return enable; }
		bool& operator= (const bool &v) { enable = v; return enable; } 

		private:
		bool enable;
	};

	PxCollisionBitMap gCollisionTable[GROUP_SIZE][GROUP_SIZE];

	PxFilterOp::Enum gFilterOps[3] = { PxFilterOp::PX_FILTEROP_AND, PxFilterOp::PX_FILTEROP_AND, PxFilterOp::PX_FILTEROP_AND };
	
	PxGroupsMask gFilterConstants[2];
	
	bool gFilterBool = false;

	static void gAND(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = mask0.bits0 & mask1.bits0;
		results.bits1 = mask0.bits1 & mask1.bits1;
		results.bits2 = mask0.bits2 & mask1.bits2;
		results.bits3 = mask0.bits3 & mask1.bits3;
	}
	static void gOR(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = mask0.bits0 | mask1.bits0;
		results.bits1 = mask0.bits1 | mask1.bits1;
		results.bits2 = mask0.bits2 | mask1.bits2;
		results.bits3 = mask0.bits3 | mask1.bits3;
	}
	static void gXOR(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = mask0.bits0 ^ mask1.bits0;
		results.bits1 = mask0.bits1 ^ mask1.bits1;
		results.bits2 = mask0.bits2 ^ mask1.bits2;
		results.bits3 = mask0.bits3 ^ mask1.bits3;
	}
	static void gNAND(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = ~(mask0.bits0 & mask1.bits0);
		results.bits1 = ~(mask0.bits1 & mask1.bits1);
		results.bits2 = ~(mask0.bits2 & mask1.bits2);
		results.bits3 = ~(mask0.bits3 & mask1.bits3);
	}
	static void gNOR(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = ~(mask0.bits0 | mask1.bits0);
		results.bits1 = ~(mask0.bits1 | mask1.bits1);
		results.bits2 = ~(mask0.bits2 | mask1.bits2);
		results.bits3 = ~(mask0.bits3 | mask1.bits3);
	}
	static void gNXOR(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = ~(mask0.bits0 ^ mask1.bits0);
		results.bits1 = ~(mask0.bits1 ^ mask1.bits1);
		results.bits2 = ~(mask0.bits2 ^ mask1.bits2);
		results.bits3 = ~(mask0.bits3 ^ mask1.bits3);
	}

	static void gSWAP_AND(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1)
	{
		results.bits0 = mask0.bits0 & mask1.bits2;
		results.bits1 = mask0.bits1 & mask1.bits3;
		results.bits2 = mask0.bits2 & mask1.bits0;
		results.bits3 = mask0.bits3 & mask1.bits1;
	}
	
	typedef void	(*FilterFunction)	(PxGroupsMask& results, const PxGroupsMask& mask0, const PxGroupsMask& mask1);

	FilterFunction const gTable[] = { gAND, gOR, gXOR, gNAND, gNOR, gNXOR, gSWAP_AND };

	static PxFilterData convert(const PxGroupsMask& mask)
	{
		PxFilterData fd;

		fd.word2 = mask.bits0 | (mask.bits1 << 16);
		fd.word3 = mask.bits2 | (mask.bits3 << 16);

		return fd;
	}

	static PxGroupsMask convert(const PxFilterData& fd)
	{
		PxGroupsMask mask;

		mask.bits0 = (PxU16)(fd.word2 & 0xffff);
		mask.bits1 = (PxU16)(fd.word2 >> 16);
		mask.bits2 = (PxU16)(fd.word3 & 0xffff);
		mask.bits3 = (PxU16)(fd.word3 >> 16);

		return mask;
	}
}

PxFilterFlags physx::PxDefaultSimulationFilterShader(
	PxFilterObjectAttributes attributes0,
	PxFilterData filterData0, 
	PxFilterObjectAttributes attributes1,
	PxFilterData filterData1,
	PxPairFlags& pairFlags,
	const void* constantBlock,
	PxU32 constantBlockSize)
{
	PX_UNUSED(constantBlock);
	PX_UNUSED(constantBlockSize);

	// let triggers through
	if(PxFilterObjectIsTrigger(attributes0) || PxFilterObjectIsTrigger(attributes1))
	{
		pairFlags = PxPairFlag::eTRIGGER_DEFAULT;
		return PxFilterFlags();
	}

	// Collision Group
	if (!gCollisionTable[filterData0.word0][filterData1.word0]())
	{
		return PxFilterFlag::eSUPPRESS;
	}

	// Filter function
	PxGroupsMask g0 = convert(filterData0);
	PxGroupsMask g1 = convert(filterData1);

	PxGroupsMask g0k0;	gTable[gFilterOps[0]](g0k0, g0, gFilterConstants[0]);
	PxGroupsMask g1k1;	gTable[gFilterOps[1]](g1k1, g1, gFilterConstants[0]);
	PxGroupsMask final;	gTable[gFilterOps[2]](final, g0k0, g1k1);
	
	bool r = final.bits0 || final.bits1 || final.bits2 || final.bits3;
	if (r != gFilterBool)
	{
		return PxFilterFlag::eSUPPRESS;
	}

	pairFlags = PxPairFlag::eCONTACT_DEFAULT;

	return PxFilterFlags();
}

bool physx::PxGetGroupCollisionFlag(const PxU16 group1, const PxU16 group2)
{
	PX_CHECK_AND_RETURN_NULL(group1 < 32 && group2 < 32, "Group must be less than 32");	

	return gCollisionTable[group1][group2]();
}

void physx::PxSetGroupCollisionFlag(const PxU16 group1, const PxU16 group2, const bool enable)
{	
	PX_CHECK_AND_RETURN(group1 < 32 && group2 < 32, "Group must be less than 32");	

	gCollisionTable[group1][group2] = enable;
	gCollisionTable[group2][group1] = enable;
}

PxU16 physx::PxGetGroup(const PxRigidActor& actor)
{
	PX_CHECK_AND_RETURN_NULL(actor.getNbShapes() >= 1,"There must be a shape in actor");

	PxShape* shape = NULL;
	actor.getShapes(&shape, 1);

	PxFilterData fd = shape->getSimulationFilterData();

	return (PxU16)fd.word0;
}

void physx::PxSetGroup(const PxRigidActor& actor, const PxU16 collisionGroup)
{	
	PX_CHECK_AND_RETURN(collisionGroup < 32,"Collision group must be less than 32");

	PxFilterData fd;
	
	if (actor.getNbShapes() == 1)
	{
		PxShape* shape = NULL;
		actor.getShapes(&shape, 1);

		// retrieve current group mask
		fd = shape->getSimulationFilterData();
		fd.word0 = collisionGroup;
		
		// set new filter data
		shape->setSimulationFilterData(fd);
	}
	else
	{
		PxShape* shape;
		PxU32 numShapes = actor.getNbShapes();
		shdfnd::InlineArray<PxShape*, 64> shapes;
		if(numShapes > 64)
		{
			shapes.resize(64);
		}
		else
		{
			shapes.resize(numShapes);
		}

		PxU32 iter = 1 + numShapes/64;

		for(PxU32 i=0; i < iter; i++)
		{
			PxU32 offset = i * 64;
			PxU32 size = numShapes - offset;
			if(size > 64)
				size = 64;

			actor.getShapes(shapes.begin(), size, offset);

			for(PxU32 j = size; j--;)
			{
				// retrieve current group mask
				shape = shapes[j];
				fd = shape->getSimulationFilterData();
				fd.word0 = collisionGroup;

				// set new filter data
				shape->setSimulationFilterData(fd);
			}
		}
	}
}

void physx::PxGetFilterOps(PxFilterOp::Enum& op0, PxFilterOp::Enum& op1, PxFilterOp::Enum& op2)
{
	op0 = gFilterOps[0];
	op1 = gFilterOps[1];
	op2 = gFilterOps[2];
}

void physx::PxSetFilterOps(const PxFilterOp::Enum& op0, const PxFilterOp::Enum& op1, const PxFilterOp::Enum& op2)
{
	gFilterOps[0] = op0;
	gFilterOps[1] = op1;
	gFilterOps[2] = op2;
}

bool physx::PxGetFilterBool()
{
	return gFilterBool;
}

void physx::PxSetFilterBool(const bool enable)
{
	gFilterBool = enable;
}

void physx::PxGetFilterConstants(PxGroupsMask& c0, PxGroupsMask& c1)
{
	c0 = gFilterConstants[0];
	c1 = gFilterConstants[1];
}

void physx::PxSetFilterConstants(const PxGroupsMask& c0, const PxGroupsMask& c1)
{
	gFilterConstants[0] = c0;
	gFilterConstants[1] = c1;
}

PxGroupsMask physx::PxGetGroupsMask(const PxRigidActor& actor)
{
	PX_CHECK_AND_RETURN_VAL(actor.getNbShapes() >= 1,"At least one shape must be in actor",PxGroupsMask());

	PxShape* shape = NULL;
	actor.getShapes(&shape, 1);

	PxFilterData fd = shape->getSimulationFilterData();
	
	return convert(fd);
}

void physx::PxSetGroupsMask(const PxRigidActor& actor, const PxGroupsMask& mask)
{
	PxFilterData tmp;
	PxFilterData fd = convert(mask);

	if (actor.getNbShapes() == 1)
	{
		PxShape* shape = NULL;
		actor.getShapes(&shape, 1);

		// retrieve current group
		tmp = shape->getSimulationFilterData();
		fd.word0 = tmp.word0;

		// set new filter data
		shape->setSimulationFilterData(fd);
	}
	else
	{
		PxShape* shape;
		PxU32 numShapes = actor.getNbShapes();
		shdfnd::InlineArray<PxShape*, 64> shapes;
		if(numShapes > 64)
		{
			shapes.resize(64);
		}
		else
		{
			shapes.resize(numShapes);
		}

		PxU32 iter = 1 + numShapes/64;

		for(PxU32 i=0; i < iter; i++)
		{
			PxU32 offset = i * 64;
			PxU32 size = numShapes - offset;
			if(size > 64)
				size = 64;

			actor.getShapes(shapes.begin(), size, offset);

			for(PxU32 j = size; j--;)
			{
				// retrieve current group mask
				shape = shapes[j];
				// retrieve current group
				tmp = shape->getSimulationFilterData();
				fd.word0 = tmp.word0;

				// set new filter data
				shape->setSimulationFilterData(fd);

			}
		}
	}
}
