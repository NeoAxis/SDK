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


#ifndef NP_SPHERICALJOINTCONSTRAINT_H
#define NP_SPHERICALJOINTCONSTRAINT_H

#include "ExtJoint.h"
#include "PxSphericalJoint.h"

namespace physx
{
namespace Ext
{
	struct SphericalJointData: public JointData
	{
		PxJointLimitCone		limit;
		PxReal					tanQYLimit;
		PxReal					tanQZLimit;
		PxReal					tanQPad;

		PxReal					projectionLinearTolerance;

		PxSphericalJointFlags	jointFlags;
		EXPLICIT_PADDING(	PxU16					paddingFromFlags);
		// forestall compiler complaints about not being able to generate a constructor
	private:
		SphericalJointData(const PxJointLimitCone &cone):
			limit(cone) {}
	};

typedef Joint<PxSphericalJoint, PxJointType::eSPHERICAL> SphericalJointT;

	class SphericalJoint : public SphericalJointT
	{
	public:
// PX_SERIALIZATION
									SphericalJoint(PxRefResolver& v)	: SphericalJointT(v)	{}
									DECLARE_SERIAL_CLASS(SphericalJoint, SphericalJointT)
		virtual		void			exportExtraData(PxSerialStream& stream);
		virtual		char*			importExtraData(char* address, PxU32& totalPadding);
		virtual		bool			resolvePointers(PxRefResolver&, void*);
		static		void			getMetaData(PxSerialStream& stream);
//~PX_SERIALIZATION
		virtual						~SphericalJoint()
		{
			if(getSerialFlags()&PxSerialFlag::eOWNS_MEMORY)
				PX_FREE(mData);
		}

		SphericalJoint(const PxTolerancesScale& scale,
					   PxRigidActor* actor0, const PxTransform& localFrame0, 
					   PxRigidActor* actor1, const PxTransform& localFrame1)
		 {
// PX_SERIALIZATION
			setSerialType(PxConcreteType::eUSER_SPHERICAL_JOINT);
//~PX_SERIALIZATION
			SphericalJointData* data = reinterpret_cast<SphericalJointData*>(PX_ALLOC(sizeof(SphericalJointData), PX_DEBUG_EXP("SphericalJointData")));
			mData = data;

			initCommonData(*data,actor0, localFrame0, actor1, localFrame1);
			data->projectionLinearTolerance = 1e10f;
			data->limit = PxJointLimitCone(PxPi/2, PxPi/2, 0.05f);
			data->jointFlags = PxSphericalJointFlags();
		 }

		void					setProjectionLinearTolerance(PxReal distance);
		PxReal					getProjectionLinearTolerance() const;

		void					setLimitCone(const PxJointLimitCone &limit);
		PxJointLimitCone		getLimitCone() const;

		PxSphericalJointFlags	getSphericalJointFlags(void) const;
		void					setSphericalJointFlags(PxSphericalJointFlags flags);
		void					setSphericalJointFlag(PxSphericalJointFlag::Enum flag, bool value);

		void*					prepareData();

		bool					attach(PxPhysics &physics, PxRigidActor* actor0, PxRigidActor* actor1);
	private:

		static PxConstraintShaderTable sShaders;

		PX_FORCE_INLINE SphericalJointData& data() const				
		{	
			return *static_cast<SphericalJointData*>(mData);
		}
	};

} // namespace Ext

namespace Ext
{
	extern "C"  PxU32 SphericalJointSolverPrep(Px1DConstraint* constraints,
		PxVec3& body0WorldOffset,
		PxU32 maxConstraints,
		const void* constantBlock,							  
		const PxTransform& bA2w,
		const PxTransform& bB2w);
}

}

#endif
