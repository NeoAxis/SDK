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


#ifndef PX_PHYSICS_COMMON_METADATA
#define PX_PHYSICS_COMMON_METADATA

#include "Px.h"
#include "CmLegacyStream.h"
#include "PsArray.h"
#include "CmPhysXCommon.h"
#include "CmReflection.h"
#include "CmMetaDataFlags.h"

#define PS_GENERATE_META_DATA

namespace physx
{
namespace Cm
{
	// PT: beware, must match corresponding structure in ConvX
	struct MetaDataEntry
	{
		const char*		mType;			//!< Field type (bool, byte, quaternion, etc)
		const char*		mName;			//!< Field name (appears exactly as in the source file)
		PxU32			mOffset;		//!< Offset from the start of the class (ie from "this", field is located at "this"+Offset)
		PxU32			mSize;			//!< sizeof(Type)
		PxU32			mCount;			//!< Number of items of type Type (0 for dynamic sizes)
		PxU32			mOffsetSize;	//!< Offset of dynamic size param, for dynamic arrays
		PxU32			mFlags;			//!< Field parameters
		PxU32			mAlignment;		//!< Explicit alignment added for DE1340
	};

#ifdef PS_GENERATE_META_DATA

	#define PS_STORE_ENTRY	stream.storeBuffer(&tmp, sizeof(MetaDataEntry))

	#define DEFINE_MD_ITEM(Class, type, name, flags)	\
	{ MetaDataEntry tmp = { #type, #name, (PxU32)OFFSET_OF(Class, name), SIZE_OF(Class, name), 1, 0, flags, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_ITEMS(Class, type, name, flags, count)	\
	{ MetaDataEntry tmp = { #type, #name, (PxU32)OFFSET_OF(Class, name), SIZE_OF(Class, name), count, 0, flags, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_ITEMS2(Class, type, name, flags)	\
	{ MetaDataEntry tmp = { #type, #name, (PxU32)OFFSET_OF(Class, name), SIZE_OF(Class, name), sizeof(((Class*)0)->name)/sizeof(type), 0, flags, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_JUNK(Class, count)	\
	{ MetaDataEntry tmp = { "PxU8", "Junk", 0, count, count, 0, MdFlags::ePADDING, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_CLASS(Class)	\
	{ MetaDataEntry tmp = { #Class, 0, 0, sizeof(Class), 0, 0, MdFlags::eCLASS, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_VCLASS(Class)	\
	{ MetaDataEntry tmp = { #Class, 0, 0, sizeof(Class), 0, 0, MdFlags::eCLASS|MdFlags::eVIRTUAL, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_TYPEDEF(newType, oldType)	{					\
		PX_COMPILE_TIME_ASSERT(sizeof(newType)==sizeof(oldType));	\
		MetaDataEntry tmp = { #newType, #oldType, 0, 0, 0, 0, MdFlags::eTYPEDEF, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_BASE_CLASS(Class, BaseClass)														\
	{																									\
		Class* myClass = reinterpret_cast<Class*>(42);													\
		BaseClass* s = static_cast<BaseClass*>(myClass);												\
		const PxU32 offset = PxU32(size_t(s) - size_t(myClass));										\
		MetaDataEntry tmp = { #Class, #BaseClass, offset, sizeof(Class), 0, 0, MdFlags::eCLASS, 0 };	\
		PS_STORE_ENTRY;																					\
	}

	#define DEFINE_MD_UNION(Class, name)	\
	{ MetaDataEntry tmp = { #Class, 0, (PxU32)OFFSET_OF(Class, name), SIZE_OF(Class, name), 1, 0, MdFlags::eUNION, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_UNION_TYPE(Class, type, enumValue)	\
	{ MetaDataEntry tmp = { #Class, #type, enumValue, 0, 0, 0, MdFlags::eUNION, 0 }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_EXTRA_DATA_ITEM(Class, type, control, align)	\
	{ MetaDataEntry tmp = { #type, 0, (PxU32)OFFSET_OF(Class, control), sizeof(type), 0, (PxU32)SIZE_OF(Class, control), MdFlags::eEXTRA_DATA|MdFlags::eEXTRA_ITEM, align }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_EXTRA_DATA_ITEMS(Class, type, control, count, flags, align)	\
	{ MetaDataEntry tmp = { #type, 0, (PxU32)OFFSET_OF(Class, control), (PxU32)SIZE_OF(Class, control), (PxU32)OFFSET_OF(Class, count), (PxU32)SIZE_OF(Class, count), MdFlags::eEXTRA_DATA|MdFlags::eEXTRA_ITEMS|flags, align }; PS_STORE_ENTRY;	}

	#define DEFINE_MD_EXTRA_DATA_ARRAY(Class, type, dyn_count, align, flags)	\
	{ MetaDataEntry tmp = { #type, 0, (PxU32)OFFSET_OF(Class, dyn_count), SIZE_OF(Class, dyn_count), align, 0, MdFlags::eEXTRA_DATA|flags, align }; PS_STORE_ENTRY;	}

#else

	#define PS_STORE_ENTRY
	#define DEFINE_MD_ITEM(Class, type, name, flags)
	#define DEFINE_MD_ITEMS(Class, type, name, flags, count)
	#define DEFINE_MD_ITEMS2(Class, type, name, flags)
	#define DEFINE_MD_JUNK(Class, count)
	#define DEFINE_MD_CLASS(Class)
	#define DEFINE_MD_VCLASS(Class)
	#define DEFINE_MD_TYPEDEF(newType, oldType)
	#define DEFINE_MD_BASE_CLASS(Class, BaseClass)
	#define DEFINE_MD_UNION(Class, name)
	#define DEFINE_MD_UNION_TYPE(Class, type, enumValue)
	#define DEFINE_MD_EXTRA_DATA_ITEM(Class, type, control, align)
	#define DEFINE_MD_EXTRA_DATA_ITEMS(Class, type, control, count, flags, align)
	#define DEFINE_MD_EXTRA_DATA_ARRAY(Class, type, dyn_count, align, flags)

#endif

// PT: we only need explicit padding while developing, for ConvX to detect missing members.
// But it's otherwise dangerous to keep it around, especially on 64-bit platforms.
#ifdef PX_X64
	#define EXPLICIT_PADDING(x)
#else
//	#define EXPLICIT_PADDING_METADATA
//	#define EXPLICIT_PADDING(x)	x
	#define EXPLICIT_PADDING(x)			// PT: do. not. change. this.
#endif

PX_PHYSX_COMMON_API void getFoundationMetaData(PxSerialStream& stream);


} // namespace Cm

}

#endif
