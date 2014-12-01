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

#ifndef CCT_CAPSULE_CONTROLLER
#define CCT_CAPSULE_CONTROLLER

/* Exclude from documentation */
/** \cond */

#include "CctController.h"
#include "PxCapsuleController.h"

namespace physx
{

class PxPhysics;

namespace Cct
{

	class CapsuleController : public PxCapsuleController, public Controller
	{
	public:
											CapsuleController(const PxControllerDesc& desc, PxPhysics& sdk, PxScene* scene);
		virtual								~CapsuleController();

		// Controller
		virtual	PxF32						getHalfHeightInternal()				const		{ return mRadius+mHeight*0.5f;			}
		virtual	bool						getWorldBox(PxExtendedBounds3& box) const;
		virtual	PxController*				getPxController()								{ return this;							}
		//~Controller

		// PxController
		virtual	PxControllerShapeType::Enum	getType()										{ return mType;							}
		virtual void						release()										{ releaseInternal();					}
		virtual	PxU32						move(const PxVec3& disp, PxF32 minDist, PxF32 elapsedTime, const PxControllerFilters& filters, const PxObstacleContext* obstacles);
		virtual	bool						setPosition(const PxExtendedVec3& position)		{ return setPos(position);				}
		virtual	bool						setFootPosition(const PxExtendedVec3& position);
		virtual	const PxExtendedVec3&		getPosition()						const		{ return mPosition;						}
		virtual	PxExtendedVec3				getFootPosition()					const;
		virtual	PxRigidDynamic*				getActor()							const		{ return mKineActor;					}
		virtual	void						setStepOffset(const float offset)				{ mUserParams.mStepOffset = offset;		}
		virtual	PxF32						getStepOffset()						const		{ return mUserParams.mStepOffset;		}
		virtual	void						setInteraction(PxCCTInteractionMode::Enum flag)	{ mInteractionMode = flag;				}
		virtual	PxCCTInteractionMode::Enum	getInteraction()					const		{ return mInteractionMode;				}
		virtual	void						setNonWalkableMode(PxCCTNonWalkableMode::Enum flag)	{ mUserParams.mNonWalkableMode = flag;	}
		virtual	PxCCTNonWalkableMode::Enum	getNonWalkableMode()				const			{ return mUserParams.mNonWalkableMode;	}
		virtual	void						setGroupsBitmask(PxU32 bitmask)					{ mGroupsBitmask = bitmask;				}
		virtual	PxU32						getGroupsBitmask()					const		{ return mGroupsBitmask;				}
		virtual PxF32						getContactOffset()					const		{ return mUserParams.mContactOffset;	}
		virtual PxVec3						getUpDirection()					const		{ return mUserParams.mUpDirection;		}
		virtual	void						setUpDirection(const PxVec3& up)				{ setUpDirectionInternal(up);			}
		virtual PxF32						getSlopeLimit()						const		{ return mUserParams.mSlopeLimit;		}
		virtual	void						invalidateCache();
		virtual	PxScene*					getScene()										{ return mScene;						}
		virtual	void*						getUserData()						const		{ return mUserData;						}
		virtual	void						getState(PxControllerState& state)	const		{ return getInternalState(state);		}
		virtual	void						getStats(PxControllerStats& stats)	const		{ return getInternalStats(stats);		}
		virtual	void						resize(PxReal height);
		//~PxController

		// PxCapsuleController
		virtual	PxF32						getRadius()							const		{ return mRadius;						}
		virtual	PxF32						getHeight()							const		{ return mHeight;						}
		virtual	PxCapsuleClimbingMode::Enum	getClimbingMode()					const;
		virtual	bool						setRadius(PxF32 radius);
		virtual	bool						setHeight(PxF32 height);
		virtual	bool						setClimbingMode(PxCapsuleClimbingMode::Enum);
		//~ PxCapsuleController

				void						getCapsule(PxExtendedCapsule& capsule)	const;

				PxF32						mRadius;
				PxF32						mHeight;
				PxCapsuleClimbingMode::Enum	mClimbingMode;
	};

} // namespace Cct

}

/** \endcond */
#endif
