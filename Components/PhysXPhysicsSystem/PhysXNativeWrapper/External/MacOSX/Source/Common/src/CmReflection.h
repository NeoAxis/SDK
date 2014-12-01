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

#ifndef PX_PHYSICS_COMMON_REFLECTION
#define PX_PHYSICS_COMMON_REFLECTION

#include "PsArray.h"
#include "CmPhysXCommon.h"
#include "PsUtilities.h"

// PX_SERIALIZATION

// A stripped-down version of ICE reflection stuff

#ifndef OFFSET_OF
#ifdef __CELLOS_LV2__
#ifdef __SNC__
      #define OFFSET_OF(Class, Member)    (size_t)&(((Class*)0)->Member)
#else // __SNC__
      #define OFFSET_OF(Class, Member)    __builtin_offsetof(Class, Member)
#endif // __SNC__
#else
#if defined LINUX || defined __APPLE__
      #define OFFSET_OF(Class, Member)    __builtin_offsetof(Class, Member)
#else // LINUX
      #define OFFSET_OF(Class, Member)    (size_t)&(((Class*)0)->Member)
#endif // LINIX
#endif
#endif // ifndef OFFSET_OF	
	#define SIZE_OF(Class, Member)			sizeof(((Class*)0)->Member)
	#define ICE_COMPILE_TIME_ASSERT(exp)	extern char ICE_Dummy[ (exp) ? 1 : -1 ]

#include "PxFields.h"

namespace physx
{
namespace Cm
{
	enum FieldFlag
	{
		F_SERIALIZE		= (1<<0),	//!< Serialize this field
		F_ALIGN			= (1<<1),	//!< Align this serialized field on 16-bytes boundary
	};

	//! Defines a generic field
	#define _FIELD(type, name, fieldtype, count, offset_size, flags)	{ fieldtype, #name, (PxU32)OFFSET_OF(type, name), SIZE_OF(type, name), count, offset_size, flags }

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//! Defines a single base field
	#define DEFINE_FIELD(type, name, fieldtype, flags)						_FIELD(type, name, fieldtype, 1, 0, flags)
	//! Defines a static array of base fields
	#define DEFINE_STATIC_ARRAY(type, name, fieldtype, count, flags)		_FIELD(type, name, fieldtype, count, 0, flags)
	//! Defines a dynamic array of base fields
	#define DEFINE_DYNAMIC_ARRAY(type, name, fieldtype, name_size, flags)	_FIELD(type, name, fieldtype, 0, OFFSET_OF(type, name_size), flags)

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	#define BEGIN_FIELDS(current_class)																\
	const PxFieldDescriptor* current_class::getDescriptors(PxU32& size)	const	{					\
		static const PxFieldDescriptor mClassDescriptor[] = {										\
			{ PxField::eFORCE_DWORD, #current_class, 0, 0, 0, 0, 0 },

	//! Ends field declarations
	#define END_FIELDS(current_class)	};		\
	size = PX_ARRAY_SIZE(mClassDescriptor);		\
	return mClassDescriptor;	}

	#define DECLARE_FIELDS																			\
																									\
	const PxFieldDescriptor*		getDescriptors(PxU32& size)	const;								\
																									\
	const PxFieldDescriptor* findDescriptor(const char* name)	const								\
	{																								\
		if(!name)	return NULL;																	\
		PxU32 size;																					\
		const PxFieldDescriptor* FD = getDescriptors(size);										\
		while(size--)																				\
		{																							\
			if(strcmp(FD->mName, name)==0)	return FD;												\
			FD++;																					\
		}																							\
		return NULL;																				\
	}

	#define DECLARE_SERIAL_CLASS(current_class, base_class)											\
	DECLARE_FIELDS																					\
	public:																							\
	virtual	PxU32		getObjectSize()	const	{ return sizeof(*this);		}						\
																									\
	virtual bool	getFields(PxSerialStream& edit, PxU32 flags)	const							\
	{																								\
		base_class::getFields(edit, flags);															\
		PxU32 size;																					\
		const PxFieldDescriptor* Fields = current_class::getDescriptors(size);						\
		for(PxU32 i=0;i<size;i++)																	\
		{																							\
			if(Fields[i].mFlags&flags)	edit.storeBuffer(&Fields[i], 0);							\
		}																							\
		return true;																				\
	}																								\
																									\
	virtual bool getFields(PxSerialStream& edit, PxField::Enum type)	const						\
	{																								\
		base_class::getFields(edit, type);															\
		PxU32 size;																					\
		const PxFieldDescriptor* Fields = current_class::getDescriptors(size);						\
		for(PxU32 i=0;i<size;i++)																	\
		{																							\
			if(Fields[i].mType==type)	edit.storeBuffer(&Fields[i], 0);							\
		}																							\
		return true;																				\
	}																								\
																									\
	virtual bool getFields(PxSerialStream& edit)	const											\
	{																								\
		base_class::getFields(edit);																\
		PxU32 size;																					\
		const PxFieldDescriptor* Fields = current_class::getDescriptors(size);						\
		for(PxU32 i=0;i<size;i++)	edit.storeBuffer(&Fields[i], 0);								\
		return true;																				\
	}																								\
																									\
	virtual const PxFieldDescriptor* getFieldDescriptor(const char* name)	const					\
	{																								\
		if(!name)	return NULL;																	\
		const PxFieldDescriptor* FD = base_class::getFieldDescriptor(name);						\
		if(FD)	return FD;																			\
																									\
		return findDescriptor(name);																\
	}																								\
	static PxSerializable* createInstance(char*& address, PxRefResolver& v)							\
	{																								\
		current_class* NewObject = new (address) current_class(v);									\
		address += sizeof(*NewObject);																\
		return NewObject;																			\
	}

	namespace
	{
		template <typename T> class MDArray : public physx::shdfnd::Array<T>
		{
		public:
			static PX_FORCE_INLINE physx::PxU32 getDataOffset()           { return OFFSET_OF(MDArray<T>, mData); }
			static PX_FORCE_INLINE physx::PxU32 getDataSize()             { return SIZE_OF(MDArray<T>, mData); }
			static PX_FORCE_INLINE physx::PxU32 getSizeOffset()           { return OFFSET_OF(MDArray<T>, mSize); }
			static PX_FORCE_INLINE physx::PxU32 getSizeSize()             { return SIZE_OF(MDArray<T>, mSize); }
			static PX_FORCE_INLINE physx::PxU32 getCapacityOffset()       { return OFFSET_OF(MDArray<T>, mCapacity); }
			static PX_FORCE_INLINE physx::PxU32 getCapacitySize()         { return SIZE_OF(MDArray<T>, mCapacity); }
		};
	}

	//~PX_SERIALIZATION

} // namespace Cm

}

#endif
//#endif
