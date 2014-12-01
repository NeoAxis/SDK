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
#ifndef REPX_EXTENSION_IMPL_H
#define REPX_EXTENSION_IMPL_H
#include "RepX.h"
#include "PsUserAllocated.h"
#include "PxProfileFoundationWrapper.h"
#include "RepXVisitorWriter.h"
#include "RepXVisitorReader.h"


namespace physx { namespace repx {
	using namespace physx::profile;

	/**
	 *	The repx extension impl takes the raw, untyped repx extension interface
	 *	and implements the simpler functions plus does the reinterpret-casts required 
	 *	for any object to implement the extension safely.
	 */
	template<typename TLiveType>
	struct RepXExtensionImpl : public RepXExtension, UserAllocated
	{
	private:
		RepXExtensionImpl( const RepXExtensionImpl& inOther );
		RepXExtensionImpl& operator=( const RepXExtensionImpl& inOther );

	public:
		PxAllocatorCallback& mAllocator;

		RepXExtensionImpl( PxAllocatorCallback& inAllocator )
			: mAllocator( inAllocator )
		{
		}

		virtual void destroy() { PX_PROFILE_DELETE( mAllocator, this ); }
		virtual const char* getTypeName() { return getExtensionNameForType( (const TLiveType*)NULL ); }

		virtual void objectToFile( RepXObject inLiveObject, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			const TLiveType* theObj = reinterpret_cast<const TLiveType*>( inLiveObject.mLiveObject );
			objectToFileImpl( theObj, inIdMap, inWriter, inTempBuffer );
		}

		virtual RepXObject fileToObject( RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			TLiveType* theObj( allocateObject( inArgs ) );
			if ( theObj )
				fileToObjectImpl( theObj, inReader, inAllocator, inArgs, inIdMap );
			return createRepXObject( theObj );
		}

		virtual void objectToFileImpl( const TLiveType* inObj, RepXIdToRepXObjectMap* inIdMap, RepXWriter& inWriter, MemoryBuffer& inTempBuffer )
		{
			writeAllProperties( inObj, inWriter, inTempBuffer, *inIdMap );
		}

		virtual void fileToObjectImpl( TLiveType* inObj, RepXReader& inReader, RepXMemoryAllocator& inAllocator, RepXInstantiationArgs& inArgs, RepXIdToRepXObjectMap* inIdMap )
		{
			readAllProperties( inArgs, inReader, inObj, inAllocator, *inIdMap );
		}

		virtual TLiveType* allocateObject( RepXInstantiationArgs& inArgs ) = 0;
	};
	
} }

#endif