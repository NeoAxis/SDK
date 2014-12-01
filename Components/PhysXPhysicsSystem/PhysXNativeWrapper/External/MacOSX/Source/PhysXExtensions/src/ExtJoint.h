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


#ifndef NP_JOINTCONSTRAINT_H
#define NP_JOINTCONSTRAINT_H

#include "PsUserAllocated.h"
#include "PsUtilities.h"
#include "PxConstraint.h"
#include "PxConstraintExt.h"
#include "PxJoint.h"
#include "PxD6Joint.h"
#include "PxSerialFramework.h"
#include "PxRigidDynamic.h"
#include "PxRigidStatic.h"
#include "ExtVisualDebugger.h"
#include "CmMetaData.h"
#include "CmRenderOutput.h"

#if PX_SUPPORT_VISUAL_DEBUGGER
#include "PxScene.h"
#include "PxVisualDebugger.h"
#endif

// PX_SERIALIZATION
#include "CmReflection.h"

namespace physx
{

PxConstraint* resolveConstraintPtr(PxRefResolver& v, 
								   PxConstraint* old, 
								   PxConstraintConnector* connector,
								   PxConstraintShaderTable& shaders);

// ~PX_SERIALIZATION

namespace Ext
{
	struct JointData
	{	
							PxTransform				c2b[2];

							PxConstraintFlags		constraintFlags;
		EXPLICIT_PADDING(	PxU16					paddingFromFlags);
	};

	template <class Base, PxJointType::Enum t>
	class Joint : public Base, 
				  public PxConstraintConnector, 
				  public Ps::UserAllocated
	{
	public:
// PX_SERIALIZATION
						Joint(PxRefResolver& v) : Base(v)	{}
		virtual	void	collectForExport(PxCollection& c)
		{
			Base::collectForExport(c);
			mPxConstraint->collectForExport(c);
		}
		virtual		PxU32			getOrder()								const	{ return PxSerialOrder::eJOINT;							}
		virtual		void			registerNameForExport(PxNameManager& nm)		{ nm.registerName(&mName);								}
		virtual		bool			resolvePointers(PxRefResolver& resolver, void*)	{ mName = resolver.resolveName(mName);	return true;	}
//~PX_SERIALIZATION
		PxJointType::Enum	getType()		const
		{
			return t;
		}

		virtual bool updatePvdProperties(physx::debugger::comm::PvdDataStream& pvdConnection, const PxConstraint* c, PxPvdUpdateType::Enum updateType) const
		{
#if PX_SUPPORT_VISUAL_DEBUGGER

			if(updateType == PxPvdUpdateType::UPDATE_SIM_PROPERTIES)
			{
				Ext::VisualDebugger::simUpdate(pvdConnection, *this);
				return true;
			}
			else if(updateType == PxPvdUpdateType::UPDATE_ALL_PROPERTIES)
			{
				Ext::VisualDebugger::updatePvdProperties(pvdConnection, *this);
				return true;
			}
			else if(updateType == PxPvdUpdateType::CREATE_INSTANCE)
			{
				Ext::VisualDebugger::createPvdInstance(pvdConnection, *c, *this);
				return true;
			}
			else if(updateType == PxPvdUpdateType::RELEASE_INSTANCE)
			{
				Ext::VisualDebugger::releasePvdInstance(pvdConnection, *c, *this);
				return true;
			}
#endif
			return false;
		}


		void getActors(PxRigidActor*& actor0, PxRigidActor*& actor1)	const		
		{	
			if ( mPxConstraint ) mPxConstraint->getActors(actor0,actor1);
			else
			{
				actor0 = NULL;
				actor1 = NULL;
			}
		}
		
		void setActors(PxRigidActor* actor0, PxRigidActor* actor1)
		{	
			//TODO SDK-DEV
			//You can get the debugger stream from the NpScene
			//Ext::VisualDebugger::setActors( stream, this, mPxConstraint, actor0, actor1 );

#if PX_SUPPORT_VISUAL_DEBUGGER
			PxScene* scene = getScene();
			PxVisualDebugger* debugger = scene ? scene->getPhysics().getVisualDebugger() : NULL;
			if( debugger != NULL)
			{
				debugger::comm::PvdDataStream* conn = debugger->getPvdConnection(*scene);
				if( conn != NULL )
					Ext::VisualDebugger::setActors(
					*conn,
					*this,
					*mPxConstraint,
					actor0, 
					actor1
					);
			}
#endif

			mPxConstraint->setActors(actor0, actor1);
			mData->c2b[0] = getCom(actor0).transformInv(mLocalPose[0]);
			mData->c2b[1] = getCom(actor1).transformInv(mLocalPose[1]);
			mPxConstraint->markDirty();
		}

		// this is the local pose relative to the actor, and we store internally the local
		// pose relative to the body 

		PxTransform getLocalPose(PxJointActorIndex::Enum actor) const
		{	
			return mLocalPose[actor];
		}
		
		void setLocalPose(PxJointActorIndex::Enum actor, const PxTransform& pose)
		{
			PX_CHECK_AND_RETURN(pose.isValid(), "PxJoint::setLocalPose: transform is invalid");
			mLocalPose[actor] = pose;
			mData->c2b[actor] = getCom(actor).transformInv(pose); 
			mPxConstraint->markDirty();
		}

		void getBreakForce(PxReal& force, PxReal& torque)	const
		{
			mPxConstraint->getBreakForce(force,torque);
		}

		void setBreakForce(PxReal force, PxReal torque)
		{
			mPxConstraint->setBreakForce(force,torque);
		}


		PxConstraintFlags getConstraintFlags()									const
		{
			return mPxConstraint->getFlags();
		}

		void setConstraintFlags(PxConstraintFlags flags)
		{
			mPxConstraint->setFlags(flags);
			mData->constraintFlags = flags;
		}

		void setConstraintFlag(PxConstraintFlag::Type flag, bool value)
		{
			PxConstraintFlags newFlags = value ? mData->constraintFlags | flag : mData->constraintFlags & ~flag;
			mPxConstraint->setFlags(newFlags);
			mData->constraintFlags = newFlags;
		}


		const char* getName() const
		{
			return mName;
		}

		void setName(const char* name)
		{
			mName = name;
		}

		void onComShift(PxU32 actor)
		{
			mData->c2b[actor] = getCom(actor).transformInv(mLocalPose[actor]); 
			markDirty();
		}

		void* prepareData()
		{
			return mData;
		}

		PxJoint* getPxJoint()
		{
			return this;
		}

		void* getExternalReference(PxU32& typeID)
		{
			typeID = PxConstraintExtIDs::eJOINT;
			return static_cast<PxJoint*>( this );
		}

		PxConstraintConnector* getConnector()
		{
			return this;
		}

		PX_INLINE void setPxConstraint(PxConstraint* pxConstraint)
		{
			mPxConstraint = pxConstraint;
		}

		PX_INLINE PxConstraint* getPxConstraint()
		{
			return mPxConstraint;
		}

		PX_INLINE const PxConstraint* getPxConstraint() const
		{
			return mPxConstraint;
		}

		void release()
		{
			mPxConstraint->release();
		}

		void onConstraintRelease()
		{
			PX_FREE_AND_RESET(mData);
			delete this;
		}

		PxScene* getScene() const
		{
			return mPxConstraint ? mPxConstraint->getScene() : NULL;
		}

	private:
		PxConstraint* getConstraint() const { return mPxConstraint; }

	protected:
		
		PxTransform getCom(PxU32 index) const
		{
			PxRigidActor* a[2];
			mPxConstraint->getActors(a[0],a[1]);
			return getCom(a[index]);
		}

		PxTransform getCom(PxRigidActor* actor) const
		{
			if (!actor)
				return PxTransform::createIdentity();
			else if (actor->getType() == PxActorType::eRIGID_DYNAMIC || actor->getType() == PxActorType::eARTICULATION_LINK)
				return static_cast<PxRigidBody*>(actor)->getCMassLocalPose();
			else
			{
				PX_ASSERT(actor->getType() == PxActorType::eRIGID_STATIC);
				return static_cast<PxRigidStatic*>(actor)->getGlobalPose().getInverse();
			}
		}

		void initCommonData(JointData& data,
							PxRigidActor* actor0, const PxTransform& localFrame0, 
							PxRigidActor* actor1, const PxTransform& localFrame1)
		{
			mLocalPose[0] = localFrame0;
			mLocalPose[1] = localFrame1;
			data.c2b[0] = getCom(actor0).transformInv(localFrame0);
			data.c2b[1] = getCom(actor1).transformInv(localFrame1);
			data.constraintFlags = PxConstraintFlags();
		}


		Joint():
			mName(NULL)
		,	mPxConstraint(0)
		 {
			 Base::userData = NULL;
		 }


		void markDirty()
		{ 
			mPxConstraint->markDirty();
		}

		const char*						mName;
		PxTransform						mLocalPose[2];
		PxConstraint*					mPxConstraint;
		JointData*						mData;
	};

} // namespace Ext

}

#endif
