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

#ifndef CCT_CHARACTER_CONTROLLER_MANAGER
#define CCT_CHARACTER_CONTROLLER_MANAGER

//Exclude file from docs
/** \cond */

#include "PxControllerManager.h"
#include "GuGeomUtils.h"
#include "PxMeshQuery.h"
#include "CmRenderOutput.h"
#include "CctUtils.h"
#include "PsHashSet.h"

namespace physx
{
namespace Cct
{
	class Controller;

	//Implements the PxControllerManager interface, this class used to be called ControllerManager
	class CharacterControllerManager : public PxControllerManager, public Ps::UserAllocated
	{
	public:
														CharacterControllerManager();
		virtual											~CharacterControllerManager();

		// PxControllerManager
		virtual			void							release();
		virtual			PxU32							getNbControllers()	const;
		virtual			PxController*					getController(PxU32 index);
		virtual			PxController*					createController(PxPhysics& sdk, PxScene* scene, const PxControllerDesc& desc);
		virtual			void							purgeControllers();
		virtual			PxRenderBuffer&					getRenderBuffer();
		virtual			void							setDebugRenderingFlags(PxU32 flags);
		virtual			PxObstacleContext*				createObstacleContext();
		virtual			void							computeInteractions(PxF32 elapsedTime);
		//~PxControllerManager

						void							releaseController(PxController& controller);
						Controller**					getControllers();
						void							resetObstaclesBuffers();

						Ps::HashSet<PxShape*>*			getCCTShapeHashSet() {return &mCCTShapes;}

						Cm::RenderBuffer*				mRenderBuffer;
						PxU32							mDebugRenderingFlags;
		// Shared buffers for obstacles
						Ps::Array<const void*>			mBoxUserData;
						Ps::Array<PxExtendedBox>		mBoxes;

						Ps::Array<const void*>			mCapsuleUserData;
						Ps::Array<PxExtendedCapsule>	mCapsules;
	protected:
						Ps::Array<Controller*>			mControllers;

						Ps::HashSet<PxShape*>			mCCTShapes;
	};

} // namespace Cct

}

/** \endcond */
#endif //CCT_CHARACTER_CONTROLLER_MANAGER
