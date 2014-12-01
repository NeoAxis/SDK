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


#include "PxJoint.h"
#include "ExtJoint.h"
#include "ExtD6Joint.h"
#include "ExtFixedJoint.h"
#include "ExtSphericalJoint.h"
#include "ExtDistanceJoint.h"
#include "ExtSphericalJoint.h"
#include "ExtRevoluteJoint.h"
#include "ExtPrismaticJoint.h"
#include "CmSerialAlignment.h"

using namespace physx;
using namespace Ps;
using namespace Cm;
using namespace Ext;

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_JointData(PxSerialStream& stream)
{
	DEFINE_MD_CLASS(JointData)

#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(JointData,	PxTransform,		c2b,				0)
#else
	DEFINE_MD_ITEMS(JointData,	PxTransform,		c2b,				0, 2)
#endif
	DEFINE_MD_ITEM(JointData,	PxConstraintFlags,	constraintFlags,	0)
#ifdef EXPLICIT_PADDING_METADATA
	DEFINE_MD_ITEM(JointData,	PxU16,				paddingFromFlags,	MdFlags::ePADDING)
#endif
}

static void getMetaData_PxD6JointDrive(PxSerialStream& stream)
{
	DEFINE_MD_TYPEDEF(PxD6JointDriveFlags, PxU32)

	DEFINE_MD_CLASS(PxD6JointDrive)
	DEFINE_MD_ITEM(PxD6JointDrive,	PxReal,				spring,		0)
	DEFINE_MD_ITEM(PxD6JointDrive,	PxReal,				damping,	0)
	DEFINE_MD_ITEM(PxD6JointDrive,	PxReal,				forceLimit,	0)
	DEFINE_MD_ITEM(PxD6JointDrive,	PxD6JointDriveFlags,	flags,		0)
}

static void getMetaData_PxJointLimitParameters(PxSerialStream& stream)
{
	DEFINE_MD_CLASS(PxJointLimitParameters)

	DEFINE_MD_ITEM(PxJointLimitParameters,	PxReal,		restitution,		0)
	DEFINE_MD_ITEM(PxJointLimitParameters,	PxReal,		spring,				0)
	DEFINE_MD_ITEM(PxJointLimitParameters,	PxReal,		damping,			0)
	DEFINE_MD_ITEM(PxJointLimitParameters,	PxReal,		contactDistance,	0)
}

static void getMetaData_PxJointLimit(PxSerialStream& stream)
{
	DEFINE_MD_CLASS(PxJointLimit)
	DEFINE_MD_BASE_CLASS(PxJointLimit, PxJointLimitParameters)

	DEFINE_MD_ITEM(PxJointLimit,	PxReal,		value,		0)
}

static void getMetaData_PxJointLimitPair(PxSerialStream& stream)
{
	DEFINE_MD_CLASS(PxJointLimitPair)
	DEFINE_MD_BASE_CLASS(PxJointLimitPair, PxJointLimitParameters)

	DEFINE_MD_ITEM(PxJointLimitPair,	PxReal,		upper,		0)
	DEFINE_MD_ITEM(PxJointLimitPair,	PxReal,		lower,		0)
}

static void getMetaData_PxJointLimitCone(PxSerialStream& stream)
{
	DEFINE_MD_CLASS(PxJointLimitCone)
	DEFINE_MD_BASE_CLASS(PxJointLimitCone, PxJointLimitParameters)

	DEFINE_MD_ITEM(PxJointLimitCone,	PxReal,		yAngle,		0)
	DEFINE_MD_ITEM(PxJointLimitCone,	PxReal,		zAngle,		0)
}

void PxJoint::getMetaData(PxSerialStream& stream)
{
	DEFINE_MD_VCLASS(PxJoint)
	DEFINE_MD_BASE_CLASS(PxJoint, PxSerializable)

	DEFINE_MD_ITEM(PxJoint, void,	userData,	MdFlags::ePTR)
}

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_RevoluteJointData(PxSerialStream& stream)
{
	DEFINE_MD_TYPEDEF(PxRevoluteJointFlags, PxU16)

	DEFINE_MD_CLASS(RevoluteJointData)
	DEFINE_MD_BASE_CLASS(RevoluteJointData, JointData)

	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					driveVelocity,				0)
	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					driveForceLimit,			0)
	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					driveGearRatio,				0)

	DEFINE_MD_ITEM(RevoluteJointData,	PxJointLimitPair,		limit,						0)

	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					tqHigh,						0)
	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					tqLow,						0)
	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					tqPad,						0)

	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					projectionLinearTolerance,	0)
	DEFINE_MD_ITEM(RevoluteJointData,	PxReal,					projectionAngularTolerance,	0)

	DEFINE_MD_ITEM(RevoluteJointData,	PxRevoluteJointFlags,	jointFlags,					0)
#ifdef EXPLICIT_PADDING_METADATA
	DEFINE_MD_ITEM(RevoluteJointData,	PxU16,					paddingFromFlags,			MdFlags::ePADDING)
#endif
}

void RevoluteJoint::getMetaData(PxSerialStream& stream)
{
	getMetaData_RevoluteJointData(stream);

	DEFINE_MD_VCLASS(RevoluteJoint)
	DEFINE_MD_BASE_CLASS(RevoluteJoint, PxJoint)
	DEFINE_MD_BASE_CLASS(RevoluteJoint, PxConstraintConnector)

	DEFINE_MD_ITEM(RevoluteJoint,	char,			mName,			MdFlags::ePTR)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(RevoluteJoint,	PxTransform,	mLocalPose,		0)
#else
	DEFINE_MD_ITEMS(RevoluteJoint,	PxTransform,	mLocalPose,		0, 2)
#endif
	DEFINE_MD_ITEM(RevoluteJoint,	PxConstraint,	mPxConstraint,	MdFlags::ePTR)
	DEFINE_MD_ITEM(RevoluteJoint,	JointData,		mData,			MdFlags::ePTR)

	//------ Extra-data ------

	DEFINE_MD_EXTRA_DATA_ITEM(RevoluteJoint, RevoluteJointData,		mData, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP)
}

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_SphericalJointData(PxSerialStream& stream)
{
	DEFINE_MD_TYPEDEF(PxSphericalJointFlags, PxU16)

	DEFINE_MD_CLASS(SphericalJointData)
	DEFINE_MD_BASE_CLASS(SphericalJointData, JointData)

	DEFINE_MD_ITEM(SphericalJointData,	PxJointLimitCone,		limit,						0)
	DEFINE_MD_ITEM(SphericalJointData,	PxReal,					tanQYLimit,					0)
	DEFINE_MD_ITEM(SphericalJointData,	PxReal,					tanQZLimit,					0)
	DEFINE_MD_ITEM(SphericalJointData,	PxReal,					tanQPad,					0)

	DEFINE_MD_ITEM(SphericalJointData,	PxReal,					projectionLinearTolerance,	0)

	DEFINE_MD_ITEM(SphericalJointData,	PxSphericalJointFlags,	jointFlags,					0)
#ifdef EXPLICIT_PADDING_METADATA
	DEFINE_MD_ITEM(SphericalJointData,	PxU16,					paddingFromFlags,			MdFlags::ePADDING)
#endif
}

void SphericalJoint::getMetaData(PxSerialStream& stream)
{
	getMetaData_SphericalJointData(stream);

	DEFINE_MD_VCLASS(SphericalJoint)
	DEFINE_MD_BASE_CLASS(SphericalJoint, PxJoint)
	DEFINE_MD_BASE_CLASS(SphericalJoint, PxConstraintConnector)

	DEFINE_MD_ITEM(SphericalJoint,		char,			mName,			MdFlags::ePTR)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(SphericalJoint,	PxTransform,	mLocalPose,		0)
#else
	DEFINE_MD_ITEMS(SphericalJoint,		PxTransform,	mLocalPose,		0, 2)
#endif
	DEFINE_MD_ITEM(SphericalJoint,		PxConstraint,	mPxConstraint,	MdFlags::ePTR)
	DEFINE_MD_ITEM(SphericalJoint,		JointData,		mData,			MdFlags::ePTR)

	//------ Extra-data ------

	DEFINE_MD_EXTRA_DATA_ITEM(SphericalJoint, SphericalJointData, mData, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP)
}

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_DistanceJointData(PxSerialStream& stream)
{
	DEFINE_MD_TYPEDEF(PxDistanceJointFlags, PxU16)

	DEFINE_MD_CLASS(DistanceJointData)
	DEFINE_MD_BASE_CLASS(DistanceJointData, JointData)

	DEFINE_MD_ITEM(DistanceJointData,	PxReal,					minDistance,		0)
	DEFINE_MD_ITEM(DistanceJointData,	PxReal,					maxDistance,		0)
	DEFINE_MD_ITEM(DistanceJointData,	PxReal,					tolerance,			0)
	DEFINE_MD_ITEM(DistanceJointData,	PxReal,					spring,				0)
	DEFINE_MD_ITEM(DistanceJointData,	PxReal,					damping,			0)
	DEFINE_MD_ITEM(DistanceJointData,	PxDistanceJointFlags,	jointFlags,			0)
#ifdef EXPLICIT_PADDING_METADATA
	DEFINE_MD_ITEM(DistanceJointData,	PxU16,					paddingFromFlags,	MdFlags::ePADDING)
#endif
}

void DistanceJoint::getMetaData(PxSerialStream& stream)
{
	getMetaData_DistanceJointData(stream);

	DEFINE_MD_VCLASS(DistanceJoint)
	DEFINE_MD_BASE_CLASS(DistanceJoint, PxJoint)
	DEFINE_MD_BASE_CLASS(DistanceJoint, PxConstraintConnector)

	DEFINE_MD_ITEM(DistanceJoint,	char,			mName,			MdFlags::ePTR)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(DistanceJoint,	PxTransform,	mLocalPose,		0)
#else
	DEFINE_MD_ITEMS(DistanceJoint,	PxTransform,	mLocalPose,		0, 2)
#endif
	DEFINE_MD_ITEM(DistanceJoint,	PxConstraint,	mPxConstraint,	MdFlags::ePTR)
	DEFINE_MD_ITEM(DistanceJoint,	JointData,		mData,			MdFlags::ePTR)

	//------ Extra-data ------

	DEFINE_MD_EXTRA_DATA_ITEM(DistanceJoint, DistanceJointData, mData, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP)
}

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_D6JointData(PxSerialStream& stream)
{
	DEFINE_MD_TYPEDEF(PxD6Motion::Enum, PxU32)

	DEFINE_MD_CLASS(D6JointData)
	DEFINE_MD_BASE_CLASS(D6JointData, JointData)

#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(D6JointData,	PxD6Motion::Enum,	motion,						0)
#else
	DEFINE_MD_ITEMS(D6JointData,	PxD6Motion::Enum,	motion,						0, 6)
#endif
	DEFINE_MD_ITEM(D6JointData,		PxJointLimit,		linearLimit,				0)
	DEFINE_MD_ITEM(D6JointData,		PxJointLimitCone,	swingLimit,					0)
	DEFINE_MD_ITEM(D6JointData,		PxJointLimitPair,	twistLimit,					0)

#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(D6JointData,	PxD6JointDrive,		drive,						0)
#else
	DEFINE_MD_ITEMS(D6JointData,	PxD6JointDrive,		drive,						0, PxD6Drive::eCOUNT)
#endif

	DEFINE_MD_ITEM(D6JointData,		PxTransform,		drivePosition,				0)
	DEFINE_MD_ITEM(D6JointData,		PxVec3,				driveLinearVelocity,		0)
	DEFINE_MD_ITEM(D6JointData,		PxVec3,				driveAngularVelocity,		0)

	DEFINE_MD_ITEM(D6JointData,		PxU32,				locked,						0)
	DEFINE_MD_ITEM(D6JointData,		PxU32,				limited,					0)
	DEFINE_MD_ITEM(D6JointData,		PxU32,				driving,					0)

	DEFINE_MD_ITEM(D6JointData,		PxReal,				thSwingY,					0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				thSwingZ,					0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				thSwingPad,					0)
	
	DEFINE_MD_ITEM(D6JointData,		PxReal,				tqSwingY,					0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				tqSwingZ,					0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				tqSwingPad,					0)

	DEFINE_MD_ITEM(D6JointData,		PxReal,				tqTwistLow,					0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				tqTwistHigh,				0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				tqTwistPad,					0)

	DEFINE_MD_ITEM(D6JointData,		PxReal,				linearMinDist,				0)

	DEFINE_MD_ITEM(D6JointData,		PxReal,				projectionLinearTolerance,	0)
	DEFINE_MD_ITEM(D6JointData,		PxReal,				projectionAngularTolerance,	0)
}

void D6Joint::getMetaData(PxSerialStream& stream)
{
	getMetaData_D6JointData(stream);

	DEFINE_MD_VCLASS(D6Joint)
	DEFINE_MD_BASE_CLASS(D6Joint, PxJoint)
	DEFINE_MD_BASE_CLASS(D6Joint, PxConstraintConnector)

	DEFINE_MD_ITEM(D6Joint,		char,			mName,				MdFlags::ePTR)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(D6Joint,	PxTransform,	mLocalPose,			0)
#else
	DEFINE_MD_ITEMS(D6Joint,	PxTransform,	mLocalPose,			0, 2)
#endif
	DEFINE_MD_ITEM(D6Joint,		PxConstraint,	mPxConstraint,		MdFlags::ePTR)
	DEFINE_MD_ITEM(D6Joint,		JointData,		mData,				MdFlags::ePTR)

	DEFINE_MD_ITEM(D6Joint,		bool,			mRecomputeMotion,	0)
	DEFINE_MD_ITEM(D6Joint,		bool,			mRecomputeLimits,	0)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(D6Joint,	bool,			mPadding,			MdFlags::ePADDING)
#else
	DEFINE_MD_ITEMS(D6Joint,	bool,			mPadding,			MdFlags::ePADDING, 2)
#endif

	//------ Extra-data ------

	DEFINE_MD_EXTRA_DATA_ITEM(D6Joint, D6JointData, mData, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP)
}

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_PrismaticJointData(PxSerialStream& stream)
{
	DEFINE_MD_TYPEDEF(PxPrismaticJointFlags, PxU16)

	DEFINE_MD_CLASS(PrismaticJointData)
	DEFINE_MD_BASE_CLASS(PrismaticJointData, JointData)

	DEFINE_MD_ITEM(PrismaticJointData,	PxJointLimitPair,		limit,						0)
	DEFINE_MD_ITEM(PrismaticJointData,	PxReal,					projectionLinearTolerance,	0)
	DEFINE_MD_ITEM(PrismaticJointData,	PxReal,					projectionAngularTolerance,	0)
	DEFINE_MD_ITEM(PrismaticJointData,	PxPrismaticJointFlags,	jointFlags,					0)
#ifdef EXPLICIT_PADDING_METADATA
	DEFINE_MD_ITEM(PrismaticJointData,	PxU16,					paddingFromFlags,			MdFlags::ePADDING)
#endif
}

void PrismaticJoint::getMetaData(PxSerialStream& stream)
{
	getMetaData_PrismaticJointData(stream);

	DEFINE_MD_VCLASS(PrismaticJoint)
	DEFINE_MD_BASE_CLASS(PrismaticJoint, PxJoint)
	DEFINE_MD_BASE_CLASS(PrismaticJoint, PxConstraintConnector)

	DEFINE_MD_ITEM(PrismaticJoint,		char,			mName,			MdFlags::ePTR)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(PrismaticJoint,	PxTransform,	mLocalPose,		0)
#else
	DEFINE_MD_ITEMS(PrismaticJoint,		PxTransform,	mLocalPose,		0, 2)
#endif
	DEFINE_MD_ITEM(PrismaticJoint,		PxConstraint,	mPxConstraint,	MdFlags::ePTR)
	DEFINE_MD_ITEM(PrismaticJoint,		JointData,		mData,			MdFlags::ePTR)

	//------ Extra-data ------

	DEFINE_MD_EXTRA_DATA_ITEM(PrismaticJoint, PrismaticJointData, mData, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP)
}

///////////////////////////////////////////////////////////////////////////////

static void getMetaData_FixedJointData(PxSerialStream& stream)
{
	DEFINE_MD_CLASS(FixedJointData)
	DEFINE_MD_BASE_CLASS(FixedJointData, JointData)

	DEFINE_MD_ITEM(FixedJointData,	PxReal,	projectionLinearTolerance,	0)
	DEFINE_MD_ITEM(FixedJointData,	PxReal,	projectionAngularTolerance,	0)
}

void FixedJoint::getMetaData(PxSerialStream& stream)
{
	getMetaData_FixedJointData(stream);

	DEFINE_MD_VCLASS(FixedJoint)
	DEFINE_MD_BASE_CLASS(FixedJoint, PxJoint)
	DEFINE_MD_BASE_CLASS(FixedJoint, PxConstraintConnector)

	DEFINE_MD_ITEM(FixedJoint,		char,			mName,			MdFlags::ePTR)
#ifdef DEFINE_MD_ITEMS2
	DEFINE_MD_ITEMS2(FixedJoint,	PxTransform,	mLocalPose,		0)
#else
	DEFINE_MD_ITEMS(FixedJoint,		PxTransform,	mLocalPose,		0, 2)
#endif
	DEFINE_MD_ITEM(FixedJoint,		PxConstraint,	mPxConstraint,	MdFlags::ePTR)
	DEFINE_MD_ITEM(FixedJoint,		JointData,		mData,			MdFlags::ePTR)

	//------ Extra-data ------

	DEFINE_MD_EXTRA_DATA_ITEM(FixedJoint, FixedJointData, mData, PX_SERIAL_DEFAULT_ALIGN_EXTRA_DATA_WIP)
}

///////////////////////////////////////////////////////////////////////////////

void PxRegisterExtJointMetaData(PxSerialStream& stream)
{
	DEFINE_MD_VCLASS(PxConstraintConnector)

	getMetaData_JointData(stream);
	getMetaData_PxD6JointDrive(stream);
	getMetaData_PxJointLimitParameters(stream);
	getMetaData_PxJointLimit(stream);
	getMetaData_PxJointLimitPair(stream);
	getMetaData_PxJointLimitCone(stream);

	PxJoint::getMetaData(stream);
	RevoluteJoint::getMetaData(stream);
	SphericalJoint::getMetaData(stream);
	DistanceJoint::getMetaData(stream);
	D6Joint::getMetaData(stream);
	PrismaticJoint::getMetaData(stream);
	FixedJoint::getMetaData(stream);
}

///////////////////////////////////////////////////////////////////////////////
