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


#ifndef NP_REVOLUTEJOINTCONSTRAINT_H
#define NP_REVOLUTEJOINTCONSTRAINT_H

#include "ExtJoint.h"
#include "PxRevoluteJoint.h"
#include "PsIntrinsics.h"

namespace physx
{

class PxConstraintSolverPrepKernel;
class PxConstraintProjectionKernel;

namespace Ext
{
	struct RevoluteJointData : public JointData
	{
							PxReal					driveVelocity;
							PxReal					driveForceLimit;
							PxReal					driveGearRatio;

							PxJointLimitPair		limit;
							
							PxReal					tqHigh;
							PxReal					tqLow;
							PxReal					tqPad;

							PxReal					projectionLinearTolerance;
							PxReal					projectionAngularTolerance;
							
							PxRevoluteJointFlags	jointFlags;
		EXPLICIT_PADDING(	PxU16					paddingFromFlags);

		// forestall compiler complaints about not being able to generate a constructor
	private:
		RevoluteJointData(const PxJointLimitPair &pair):
			limit(pair) {}
	};

typedef Joint<PxRevoluteJoint, PxJointType::eREVOLUTE> RevoluteJointT;

	class RevoluteJoint : public RevoluteJointT
	{
	public:
// PX_SERIALIZATION
									RevoluteJoint(PxRefResolver& v)	: RevoluteJointT(v)	{}
									DECLARE_SERIAL_CLASS(RevoluteJoint, RevoluteJointT)
		virtual		bool			resolvePointers(PxRefResolver&, void*);
		virtual		void			exportExtraData(PxSerialStream& stream);
		virtual		char*			importExtraData(char* address, PxU32& totalPadding);
		static		void			getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION
		virtual						~RevoluteJoint()
		{
			if(getSerialFlags()&PxSerialFlag::eOWNS_MEMORY)
				PX_FREE(mData);
		}

		RevoluteJoint(const PxTolerancesScale& scale,
					  PxRigidActor* actor0, const PxTransform& localFrame0, 
					  PxRigidActor* actor1, const PxTransform& localFrame1)
		 {
// PX_SERIALIZATION
			setSerialType(PxConcreteType::eUSER_REVOLUTE_JOINT);
//~PX_SERIALIZATION
			RevoluteJointData* data = reinterpret_cast<RevoluteJointData*>(PX_ALLOC(sizeof(RevoluteJointData), PX_DEBUG_EXP("RevoluteJointData")));
			Ps::memSet(data, 0, sizeof(RevoluteJointData));
			mData = data;

			initCommonData(*data, actor0, localFrame0, actor1, localFrame1);

			data->projectionLinearTolerance = 1e10f;
			data->projectionAngularTolerance = PxPi;
			data->driveForceLimit = PX_MAX_F32;
			data->driveVelocity = 0;
			data->driveGearRatio = 1.0f;
			data->limit = PxJointLimitPair(-PxPi/2, PxPi/2, 0.05f);
			data->jointFlags = PxRevoluteJointFlags();
		 }

		PxJointLimitPair		getLimit()	const;
		void					setLimit(const PxJointLimitPair& limit);

		PxReal					getDriveVelocity() const;
		void					setDriveVelocity(PxReal velocity);

		PxReal					getDriveForceLimit() const;
		void					setDriveForceLimit(PxReal forceLimit);

		PxReal					getDriveGearRatio() const;
		void					setDriveGearRatio(PxReal gearRatio);

		void					setProjectionAngularTolerance(PxReal tolerance);
		PxReal					getProjectionAngularTolerance()				const;

		void					setProjectionLinearTolerance(PxReal distance);
		PxReal					getProjectionLinearTolerance()				const;
		
		PxRevoluteJointFlags	getRevoluteJointFlags(void)					const;
		void					setRevoluteJointFlags(PxRevoluteJointFlags flags);
		void					setRevoluteJointFlag(PxRevoluteJointFlag::Enum flag, bool value);

		void*					prepareData();
		
		bool					attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1);

	private:

		static PxConstraintShaderTable sShaders;

		PX_FORCE_INLINE RevoluteJointData& data() const				
		{	
			return *static_cast<RevoluteJointData*>(mData);
		}

	};

} // namespace Ext

namespace Ext
{
	extern "C"  PxU32 RevoluteJointSolverPrep(Px1DConstraint* constraints,
		PxVec3& body0WorldOffset,
		PxU32 maxConstraints,
		const void* constantBlock,
		const PxTransform& bA2w,
		const PxTransform& bB2w);
}

}

#endif
