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


#ifndef NP_PRISMATICJOINTCONSTRAINT_H
#define NP_PRISMATICJOINTCONSTRAINT_H

#include "ExtJoint.h"
#include "PxPrismaticJoint.h"
#include "PxTolerancesScale.h"

namespace physx
{
namespace Ext
{
	struct PrismaticJointData : public JointData
	{
		PxJointLimitPair		limit;
		PxReal					projectionLinearTolerance;
		PxReal					projectionAngularTolerance;

		PxPrismaticJointFlags	jointFlags;
		EXPLICIT_PADDING(	PxU16					paddingFromFlags);

		// forestall compiler complaints about not being able to generate a constructor
	private:
		PrismaticJointData(const PxJointLimitPair &pair):
			limit(pair) {}
	};

typedef Joint<PxPrismaticJoint, PxJointType::ePRISMATIC> PrismaticJointT;

	class PrismaticJoint : public PrismaticJointT
	{
	public:
// PX_SERIALIZATION
									PrismaticJoint(PxRefResolver& v)	: PrismaticJointT(v)	{}
									DECLARE_SERIAL_CLASS(PrismaticJoint, PrismaticJointT)
		virtual		void			exportExtraData(PxSerialStream& stream);
		virtual		char*			importExtraData(char* address, PxU32& totalPadding);
		virtual		bool			resolvePointers(PxRefResolver&, void*);
		static		void			getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION
		virtual						~PrismaticJoint()
		{
			if(getSerialFlags()&PxSerialFlag::eOWNS_MEMORY)
				PX_FREE(mData);
		}

		PrismaticJoint(const PxTolerancesScale& scale,
					   PxRigidActor* actor0, const PxTransform& localFrame0, 
					   PxRigidActor* actor1, const PxTransform& localFrame1)
		 {
// PX_SERIALIZATION
			setSerialType(PxConcreteType::eUSER_PRISMATIC_JOINT);
//~PX_SERIALIZATION
			PrismaticJointData* data = reinterpret_cast<PrismaticJointData*>(PX_ALLOC(sizeof(PrismaticJointData), PX_DEBUG_EXP("PrismaticJointData")));
			mData = data;

			data->limit = PxJointLimitPair(-PX_MAX_F32, PX_MAX_F32, 0.01f * scale.length);
			data->projectionLinearTolerance = 1e10f;
			data->projectionAngularTolerance = PxPi;
			data->jointFlags = PxPrismaticJointFlags();

			initCommonData(*data, actor0, localFrame0, actor1, localFrame1);
		 }

		void					setProjectionAngularTolerance(PxReal tolerance);
		PxReal					getProjectionAngularTolerance()					const;

		void					setProjectionLinearTolerance(PxReal tolerance);
		PxReal					getProjectionLinearTolerance()					const;

		PxJointLimitPair		getLimit()										const;
		void					setLimit(const PxJointLimitPair& limit);

		PxPrismaticJointFlags	getPrismaticJointFlags(void)					const;
		void					setPrismaticJointFlags(PxPrismaticJointFlags flags);
		void					setPrismaticJointFlag(PxPrismaticJointFlag::Enum flag, bool value);
		
		bool					attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1);

	private:
		PX_FORCE_INLINE PrismaticJointData& data() const				
		{	
			return *static_cast<PrismaticJointData*>(mData);
		}

		static PxConstraintShaderTable sShaders;
	};
} // namespace Ext

namespace Ext
{
	extern "C"  PxU32 PrismaticJointSolverPrep(Px1DConstraint* constraints,
		PxVec3& body0WorldOffset,
		PxU32 maxConstraints,
		const void* constantBlock,
		const PxTransform& bA2w,
		const PxTransform& bB2w);
}

}

#endif
