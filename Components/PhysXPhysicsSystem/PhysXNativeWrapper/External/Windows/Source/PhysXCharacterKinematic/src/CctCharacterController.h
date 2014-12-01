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

#ifndef CCT_CHARACTER_CONTROLLER
#define CCT_CHARACTER_CONTROLLER

//#define USE_CONTACT_NORMAL_FOR_SLOPE_TEST

#include "PxController.h"
#include "CctUtils.h"
#include "PxTriangle.h"
#include "PsArray.h"
#include "PsHashSet.h"
#include "CmPhysXCommon.h"

namespace physx
{

struct PxFilterData;
class PxSceneQueryFilterCallback;
class PxObstacle;

namespace Cm
{
	class RenderBuffer;
}

namespace Cct
{
	struct CCTParams
	{
		PxCCTNonWalkableMode::Enum	mNonWalkableMode;
		PxQuat						mQuatFromUp;
		PxVec3						mUpDirection;
		PxF32						mSlopeLimit;
		PxF32						mContactOffset;
		PxF32						mStepOffset;
		PxF32						mInvisibleWallHeight;
		PxF32						mMaxJumpHeight;
		bool						mHandleSlope;		// True to handle walkable parts according to slope
	};

	template<class T, class A>
	PX_INLINE T* reserve(Ps::Array<T, A>& array, PxU32 nb)
	{
		const PxU32 currentSize = array.size();
		array.resizeUninitialized(array.size() + nb);
		return array.begin() + currentSize;
	}

	typedef Ps::Array<PxTriangle>	TriArray;
	typedef Ps::Array<PxU32>		IntArray;

	/* Exclude from documentation */
	/** \cond */

	struct TouchedGeomType
	{
		enum Enum
		{
			eUSER_BOX,
			eUSER_CAPSULE,
			eMESH,
			eBOX,
			eSPHERE,
			eCAPSULE,

			eLAST,

			eFORCE_DWORD	= 0x7fffffff
		};
	};

	class SweptVolume;

// PT: apparently stupid .Net aligns some of them on 8-bytes boundaries for no good reason. This is bad.
#pragma pack(push,4)

	struct TouchedGeom
	{
		TouchedGeomType::Enum	mType;
		const void*				mUserData;	// PxController or PxShape pointer
		PxExtendedVec3			mOffset;	// Local origin, typically the center of the world bounds around the character. We translate both
											// touched shapes & the character so that they are nearby this PxVec3, then add the offset back to
											// computed "world" impacts.
	};

	struct TouchedUserBox : public TouchedGeom
	{
		PxExtendedBox			mBox;
	};
	PX_COMPILE_TIME_ASSERT(sizeof(TouchedUserBox)==sizeof(TouchedGeom)+sizeof(PxExtendedBox));

	struct TouchedUserCapsule : public TouchedGeom
	{
		PxExtendedCapsule		mCapsule;
	};
	PX_COMPILE_TIME_ASSERT(sizeof(TouchedUserCapsule)==sizeof(TouchedGeom)+sizeof(PxExtendedCapsule));

	struct TouchedMesh : public TouchedGeom
	{
		PxU32			mNbTris;
		PxU32			mIndexWorldTriangles;
	};

	struct TouchedBox : public TouchedGeom
	{
		PxVec3			mCenter;
		PxVec3			mExtents;
		PxQuat			mRot;
	};

	struct TouchedSphere : public TouchedGeom
	{
		PxVec3			mCenter;		//!< Sphere's center
		PxF32			mRadius;		//!< Sphere's radius
	};

	struct TouchedCapsule : public TouchedGeom
	{
		PxVec3			mP0;		//!< Start of segment
		PxVec3			mP1;		//!< End of segment
		PxF32			mRadius;	//!< Capsule's radius
	};

#pragma pack(pop)

	struct SweptContact
	{
		PxExtendedVec3		mWorldPos;		// Contact position in world space
		PxVec3				mWorldNormal;	// Contact normal in world space
		PxF32				mDistance;		// Contact distance
		PxU32				mInternalIndex;	// Reserved for internal usage
		PxU32				mTriangleIndex;	// Triangle index for meshes/heightfields
		TouchedGeom*		mGeom;

		PX_FORCE_INLINE		void	setWorldPos(const PxVec3& localImpact, const PxExtendedVec3& offset)
		{
			mWorldPos.x = localImpact.x + offset.x;
			mWorldPos.y = localImpact.y + offset.y;
			mWorldPos.z = localImpact.z + offset.z;
		}
	};

	// PT: user-defined obstacles. Note that "user" is from the SweepTest class' point of view,
	// i.e. the PhysX CCT module is the user in this case. This is to limit coupling between the
	// core CCT module and the PhysX classes.
	struct UserObstacles// : PxObstacleContext
	{
		PxU32						mNbBoxes;
		const PxExtendedBox*		mBoxes;
		const void**				mBoxUserData;

		PxU32						mNbCapsules;
		const PxExtendedCapsule*	mCapsules;
		const void**				mCapsuleUserData;
	};

	struct InternalCBData_OnHit{};
	struct InternalCBData_FindTouchedGeom{};

	enum SweepTestFlag
	{
		STF_HIT_NON_WALKABLE	= (1<<0),
		STF_WALK_EXPERIMENT		= (1<<1),
		STF_VALIDATE_TRIANGLE	= (1<<2),	// Validate mTouchedTriangle
		STF_TOUCH_OTHER_CCT		= (1<<3),	// Are we standing on another CCT or not? (only updated for down pass)
		STF_TOUCH_OBSTACLE		= (1<<4),	// Are we standing on an obstacle or not? (only updated for down pass)
		STF_NORMALIZE_RESPONSE	= (1<<5),
		STF_FIRST_UPDATE		= (1<<6),
		STF_IS_MOVING_UP		= (1<<7),
		STF_RECREATE_CACHE		= (1<<8),
	};

	class SweepTest: public PxObserver
	{
	public:
		SweepTest();
		~SweepTest();

		PxU32				moveCharacter(
			const InternalCBData_FindTouchedGeom* userData,
			const InternalCBData_OnHit* user_data2,
			SweptVolume& volume,
			const PxVec3& direction,
			const UserObstacles& userObstacles,
			PxF32 min_dist,
			const PxControllerFilters& filters,
			bool constrainedClimbingMode,
			bool standingOnMoving
			);

		bool				doSweepTest(
			const InternalCBData_FindTouchedGeom* userDataTouchedGeom,
			const InternalCBData_OnHit* userDataOnHit,
			const UserObstacles& userObstacles,
			SweptVolume& swept_volume,
			const PxVec3& direction, PxU32 max_iter,
			PxU32* nb_collisions, PxF32 min_dist, const PxControllerFilters& filters, bool down_pass=false);

		void				findTouchedObstacles(const UserObstacles& userObstacles, const PxExtendedBounds3& world_box);

		void				voidTestCache()
		{
			mCachedTBV.setEmpty();
			if(mTouchedShape)
				mTouchedShape->getActor().unregisterObserver(*this);
			mTouchedShape = NULL;
			mTouchedObstacle = NULL;
		}

		virtual void onRelease(const PxObservable& observable);
		virtual		PxU32						getObjectSize()										const
		{
			return sizeof(SweepTest);
		}

		// private:
		Cm::RenderBuffer*	mRenderBuffer;
		PxU32				mRenderFlags;
		TriArray			mWorldTriangles;
		IntArray			mTriangleIndices;
		IntArray			mGeomStream;
		PxExtendedBounds3	mCachedTBV;
		PxU32				mCachedTriIndexIndex;
		mutable	PxU32		mCachedTriIndex[3];
		PxU32				mNbCachedStatic;
		PxU32				mNbCachedT;
	public:
#ifdef USE_CONTACT_NORMAL_FOR_SLOPE_TEST
		PxVec3				mCN;
#else
		PxVec3				mCN;
		//PxTriangle			mTouchedTriangle;
#endif
		//
		const PxObstacle*	mTouchedObstacle;	// Obstacle on which the CCT is standing
		PxShape*			mTouchedShape;		// Shape on which the CCT is standing
		PxVec3				mTouchedPos;		// Last known position of mTouchedShape/mTouchedObstacle
		// PT: TODO: union those
		PxVec3				mTouchedPosShape_Local;
		PxVec3				mTouchedPosShape_World;
		PxVec3				mTouchedPosObstacle_Local;
		PxVec3				mTouchedPosObstacle_World;
		//
		CCTParams			mUserParams;
		PxF32				mVolumeGrowth;		// Must be >1.0f and not too big
		PxF32				mContactPointHeight;	// UBI
		PxU32				mSQTimeStamp;
		PxU16				mNbFullUpdates;
		PxU16				mNbPartialUpdates;
		PxU16				mNbIterations;
		PxU32				mFlags;

	private:
		void				updateTouchedGeoms(	const InternalCBData_FindTouchedGeom* userData, const UserObstacles& userObstacles,
												const PxExtendedBounds3& worldBox, const PxControllerFilters& filters);
	};

	class CCTFilter	// PT: internal filter data, could be replaced with PxControllerFilters eventually
	{
		public:
		PX_FORCE_INLINE	CCTFilter() :
			mFilterData		(NULL),
			mFilterCallback	(NULL),
			mStaticShapes	(false),
			mDynamicShapes	(false),
			mPreFilter		(false),
			mPostFilter		(false),
			mCCTShapes		(NULL)
		{
		}
		const PxFilterData*			mFilterData;
		PxSceneQueryFilterCallback*	mFilterCallback;
		bool						mStaticShapes;
		bool						mDynamicShapes;
		bool						mPreFilter;
		bool						mPostFilter;
		Ps::HashSet<PxShape>*		mCCTShapes;
	};

	PxU32 getSceneTimestamp(const InternalCBData_FindTouchedGeom* userData);

	void findTouchedGeometry(const InternalCBData_FindTouchedGeom* userData,
		const PxExtendedBounds3& world_aabb,

		TriArray& world_triangles,
		IntArray& triIndicesArray,
		IntArray& geomStream,

		const CCTFilter& filter,
		const CCTParams& params);

	PxU32 shapeHitCallback(const InternalCBData_OnHit* userData, const SweptContact& contact, const PxVec3& dir, PxF32 length);
	PxU32 userHitCallback(const InternalCBData_OnHit* userData, const SweptContact& contact, const PxVec3& dir, PxF32 length);

} // namespace Cct

}

/** \endcond */
#endif
