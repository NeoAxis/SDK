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


//#ifdef REMOVED

#ifndef PX_PHYSICS_COMMON_SERIAL_FRAMEWORK
#define PX_PHYSICS_COMMON_SERIAL_FRAMEWORK

#include "Px.h"

// PX_SERIALIZATION

#include "PxSerialFramework.h"
#include "CmPhysXCommon.h"
#include "CmReflection.h"
#include "PsUserAllocated.h"
#include "PsHashMap.h"
#include "PsHashSet.h"

namespace physx
{
namespace Cm
{
	static const PxEmpty& PX_EMPTY = *reinterpret_cast<const PxEmpty*>(size_t(0xDEADD00D));

	struct InternalUserRef
	{
		InternalUserRef(PxSerialObjectRef userID, PxU32 flags) : mUserID(userID), mFlags(flags)			{}
		InternalUserRef(const InternalUserRef& other) : mUserID(other.mUserID), mFlags(other.mFlags)	{}

		PX_FORCE_INLINE	bool	operator == (const InternalUserRef& other)	const
		{
			if(other.mUserID!=mUserID)	return false;
			if(other.mFlags!=mFlags)	return false;
			return true;
		}

		PX_FORCE_INLINE	bool	isPxLevelObject()	const
		{
			return (mFlags & 0x7fffffff)==0;
		};

		PxSerialObjectRef	mUserID;
		PxU32				mFlags;
	};

	// PT: from http://www.concentric.net/~ttwang/tech/inthash.htm
	PX_FORCE_INLINE PxU32 mix(PxU32 a, PxU32 b, PxU32 c)
	{
	  a=a-b;  a=a-c;  a=a^(c >> 13);
	  b=b-c;  b=b-a;  b=b^(a << 8); 
	  c=c-a;  c=c-b;  c=c^(b >> 13);
	  a=a-b;  a=a-c;  a=a^(c >> 12);
	  b=b-c;  b=b-a;  b=b^(a << 16);
	  c=c-a;  c=c-b;  c=c^(b >> 5);
	  a=a-b;  a=a-c;  a=a^(c >> 3);
	  b=b-c;  b=b-a;  b=b^(a << 10);
	  c=c-a;  c=c-b;  c=c^(b >> 15);
	  return c;
	}

	PX_INLINE PxU32 hash(const InternalUserRef& ref)
	{
		return mix(PxU32(ref.mUserID>>32), PxU32(ref.mUserID&0xffffffff), ref.mFlags);
	}

	struct CollectedObject
	{
		PX_FORCE_INLINE		CollectedObject(void* s, InternalUserRef userRef) : mObject(s), mInternalUserRef(userRef)	{}

		void*				mObject;
		InternalUserRef		mInternalUserRef;
	};

	typedef Ps::HashMap<void*, void*> HashMapResolver;
	class PX_PHYSX_COMMON_API RefResolver : public PxRefResolver, public Ps::UserAllocated
	{
		public:
								RefResolver() : mStringTable(NULL)	{}
		virtual	void*			newAddress(void* oldAddress) const;
		virtual	void			setNewAddress(void* oldAddress, void* newAddress);
		virtual	void			setStringTable(const char*);
		virtual	const char*		resolveName(const char*);

				HashMapResolver	mResolver;
				const char*		mStringTable;
	};

	typedef Ps::HashMap<InternalUserRef, void*> UserHashMapResolver;
	class PX_PHYSX_COMMON_API UserReferences : public PxUserReferences, public Ps::UserAllocated
	{
		public:
								UserReferences(const CollectedObject* refs = 0, PxU32 count = 0);
		virtual	PxSerializable*	getObjectFromRef(PxSerialObjectRef ref) const;
		virtual	bool			setObjectRef(PxSerializable& object, PxSerialObjectRef ref);
		virtual PxU32			getNbObjectRefs() const;
		virtual PxU32			getObjectRefs(PxSerialObjectAndRef* buffer, PxU32 bufSize) const;
		virtual bool			objectIsReferenced(PxSerializable&) const;

		virtual void			release() {PX_DELETE(this); }

				void*			internal_getObjectFromRef(InternalUserRef ref) const;
				bool			internal_setObjectRef(void* object, InternalUserRef ref);

		UserHashMapResolver				mResolver;
		Ps::HashSet<PxSerializable*>	mReferencedObjects;
	};

	class InternalCollection : public PxCollection
	{
		public:

		// Only for internal use. Bypasses virtual calls, specialized behaviour.
		PX_INLINE	void				internalAdd(PxSerializable* s)		{ mArray.pushBack(s);								}
		PX_INLINE	PxU32				internalGetNbObjects()		const	{ return mArray.size();								}
		PX_INLINE	PxSerializable*		internalGetObject(PxU32 i)	const	{ PX_ASSERT(i<mArray.size());	return mArray[i];	}
		PX_INLINE	PxSerializable**	internalGetObjects()				{ return &mArray[0];								}

		Ps::Array<PxSerializable*>		mArray;
	};

	PX_PHYSX_COMMON_API void 	serializeCollection(InternalCollection& collection, PxSerialStream& stream, bool exportNames);
	PX_PHYSX_COMMON_API bool 	deserializeCollection(InternalCollection& collection, RefResolver& Ref, void* buffer, PxU32 nbObjectsInCollection, PxU32 nbOldAddresses, void** oldAddresses);

	PX_PHYSX_COMMON_API bool 					registerClass(PxType type, PxClassCreationCallback callback);
	PX_PHYSX_COMMON_API PxSerializable* 		createClass(PxType type, char*& address, PxRefResolver& v);

	/**
	Any object deriving from PxSerializable needs to call this function instead of 'delete object;'. 

	We don't want implement 'operator delete' in PxSerializable because that would impose how
	memory of derived classes is allocated. Even though most or all of the time derived classes will 
	be user allocated, we don't want to put UserAllocatable into the API and derive from that.
	*/
	PX_INLINE void deleteSerializedObject(PxSerializable* object)
	{
		if(object->getSerialFlags() & PxSerialFlag::eOWNS_MEMORY)
			PX_DELETE(object);
		else
			object->~PxSerializable();
	}

	/*void	exportArray(PxSerialStream& stream, const void* data, PxU32 size, PxU32 sizeOfElement, PxU32 capacity);
	char*	importArray(char* address, void** data, PxU32 size, PxU32 sizeOfElement, PxU32 capacity);
	void	notifyResizeDeserializedArray();*/


} // namespace Cm

}

//~PX_SERIALIZATION

#endif
