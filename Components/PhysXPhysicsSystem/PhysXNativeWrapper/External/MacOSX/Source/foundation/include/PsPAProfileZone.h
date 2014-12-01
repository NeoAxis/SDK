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


#ifndef __PSPA_PROFILE_ZONE__
#define __PSPA_PROFILE_ZONE__

#include "foundation/PxAssert.h"
#include "PsUserAllocated.h"
#include "PsPAEventSrc.h"
#include "PsNoCopy.h"

namespace physx
{
namespace shdfnd
{

// Holds name/enum to EventID mapping for a subsection of code
class PAProfileZone : public UserAllocated, public NoCopy
{
public:
    PAProfileZone( const char **eventNames, int numNames )
      : mNames( eventNames )
	  , mCount( numNames )
    {
        mEventArray = (EventID*) PX_ALLOC( sizeof(EventID) * mCount );
        for( int i = 0 ; i < mCount ; i++ )
			mEventArray[ i ] = Foundation::getInstance().getPAUtils().registerEvent( mNames[ i ] );
    }
    ~PAProfileZone()
    {
        PX_DELETE_POD(mEventArray);
    }
    PX_INLINE EventID getEventID( int eventEnum )
    {
        PX_ASSERT( eventEnum < mCount );
        return mEventArray[ eventEnum ];
    }
    PX_INLINE void start( int eventEnum, unsigned short data )
    {
        PX_ASSERT( eventEnum < mCount );
        Foundation::getInstance().getPAUtils().startEvent( mEventArray[ eventEnum ], data );
    }
    PX_INLINE void stop( int eventEnum, unsigned short data )
    {
        PX_ASSERT( eventEnum < mCount );
        Foundation::getInstance().getPAUtils().stopEvent( mEventArray[ eventEnum ], data );
    }
    PX_INLINE void stat( int eventEnum, unsigned long stat, unsigned long ident = 0 )
    {
        Foundation::getInstance().getPAUtils().statEvent( mEventArray[ eventEnum ], stat, ident );
    }
    PX_INLINE bool isEventEnabled( int eventEnum )
    {
        PX_ASSERT( eventEnum < mCount );
        return Foundation::getInstance().getPAUtils().isEventEnabled( mEventArray[ eventEnum ] );
    }
    PX_INLINE unsigned int getNumNames() const { return mCount; }
    PX_INLINE const char * getName( int eventEnum ) const { return mNames[ eventEnum ]; }

protected:
    EventID *       mEventArray;
    const char **   mNames;
    int             mCount;
};

// Scoped profiling zone
class PAPerfScope : public NoCopy
{
public:
    PAPerfScope( PAProfileZone& z, int eventEnum, PxU16 stat ) : mZone( z ), mEvent( eventEnum )
    {
        mZone.start( mEvent, stat );
    }
    ~PAPerfScope()
    {
        mZone.stop( mEvent, 0 );
    }
protected:
    PAProfileZone& mZone;
    int            mEvent;
};

} // end namespace shdfnd
} // end namespace physx

#endif
