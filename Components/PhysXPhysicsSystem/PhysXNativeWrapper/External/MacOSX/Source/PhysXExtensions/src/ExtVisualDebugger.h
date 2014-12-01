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


#ifndef EXT_VISUAL_DEBUGGER_H
#define EXT_VISUAL_DEBUGGER_H

#if PX_SUPPORT_VISUAL_DEBUGGER

#include "CmPhysXCommon.h"
#include "PsUserAllocated.h"
#include "PxVisualDebuggerExt.h"
#include "PxJoint.h"

namespace physx { namespace debugger { namespace comm {
	class PvdDataStream;
}}}

namespace physx
{

class PxJoint;
class PxD6Joint;
class PxDistanceJoint;
class PxFixedJoint;
class PxPrismaticJoint;
class PxRevoluteJoint;
class PxSphericalJoint;



#define JOINT_GROUP 3

namespace Ext
{
	class VisualDebugger: public PxVisualDebuggerExt, public Ps::UserAllocated
	{
	public:
		class PvdNameSpace
		{
		public:
			PvdNameSpace(physx::debugger::comm::PvdDataStream& conn, const char* name);
			~PvdNameSpace();
		private:
			physx::debugger::comm::PvdDataStream& mConnection;
		};

		static void setActors( physx::debugger::comm::PvdDataStream& PvdDataStream, const PxJoint& inJoint, const PxConstraint& c, const PxActor* newActor0, const PxActor* newActor1 );

#define DEFINE_JOINT_PVD_OPERATIONS( jointtype )														\
		static void updatePvdProperties(physx::debugger::comm::PvdDataStream& pvdConnection, const jointtype& joint);		\
		static void simUpdate(physx::debugger::comm::PvdDataStream& pvdConnection, const jointtype& joint);				\
		static void createPvdInstance(physx::debugger::comm::PvdDataStream& pvdConnection, const PxConstraint& c, const jointtype& joint);

		DEFINE_JOINT_PVD_OPERATIONS( PxD6Joint );
		DEFINE_JOINT_PVD_OPERATIONS( PxDistanceJoint );
		DEFINE_JOINT_PVD_OPERATIONS( PxFixedJoint );
		DEFINE_JOINT_PVD_OPERATIONS( PxPrismaticJoint );
		DEFINE_JOINT_PVD_OPERATIONS( PxRevoluteJoint );
		DEFINE_JOINT_PVD_OPERATIONS( PxSphericalJoint );
		
		static void releasePvdInstance(physx::debugger::comm::PvdDataStream& pvdConnection, const PxConstraint& c, const PxJoint& joint);
		static void sendClassDescriptions(physx::debugger::comm::PvdDataStream& pvdConnection);
	};
}

}

#endif // PX_SUPPORT_VISUAL_DEBUGGER
#endif // EXT_VISUAL_DEBUGGER_H