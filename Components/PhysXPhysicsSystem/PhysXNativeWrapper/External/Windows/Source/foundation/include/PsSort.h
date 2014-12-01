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


#ifndef PX_FOUNDATION_PSSORT_H
#define PX_FOUNDATION_PSSORT_H

/** \addtogroup foundation
@{
*/

#include "PsSortInternals.h"
#include "PsAlloca.h"

#ifdef _DEBUG
#define PX_SORT_PARANOIA
#endif

/**
\brief Sorts an array of objects in ascending order, assuming
that the predicate implements the < operator:

\see Less, Greater
*/


#if !defined(PX_GNUC)
#pragma warning(push)
#pragma warning(disable:4706) // disable the warning that we did an assignment within a conditional expression, as this was intentional.
#endif

namespace physx
{
namespace shdfnd
{
	template<class T, class Predicate, class Allocator>
	void sort(T* elements, PxU32 count, const Predicate& compare, const Allocator& inAllocator )
	{
		static const int INITIAL_STACKSIZE = 32;
		static const PxU32 SMALL_SORT_CUTOFF = 5; // must be >= 3 since we need 3 for median

		PX_ALLOCA(stackMem, PxI32, INITIAL_STACKSIZE);
		internal::Stack<Allocator> stack(stackMem, INITIAL_STACKSIZE,inAllocator);

		PxI32 first = 0, last = count-1;
		if(last > first)
		{
			while(true)
			{
				while(last > first)
				{
					PX_ASSERT(first >= 0 && last < (PxI32)count);
					if(PxU32(last-first) < SMALL_SORT_CUTOFF)
					{
						internal::smallSort(elements, first, last, compare);
						break;
					}
					else
					{
						PxI32 partIndex = internal::partition(elements, first, last, compare);

						// push smaller sublist to minimize stack usage
						if((partIndex - first) < (last - partIndex)) 
						{
							stack.push(first, partIndex-1);
							first = partIndex + 1;
						}
						else
						{
							stack.push(partIndex+1, last);
							last = partIndex - 1;
						}
					}
				}

				if(stack.empty())
					break;

				stack.pop(first, last);
			}
		}
#ifdef PX_SORT_PARANOIA
		for(PxU32 i=1; i<count; i++)
			PX_ASSERT(!compare(elements[i],elements[i-1]));
#endif

	}

	template<class T, class Predicate>
	void sort(T* elements, PxU32 count, const Predicate& compare = Predicate() )
	{
		sort(elements, count, compare, typename shdfnd::AllocatorTraits<T>::Type());
	}

	template<class T>
	void sort(T* elements, PxU32 count)
	{
		sort(elements, count, shdfnd::Less<T>(),
			typename shdfnd::AllocatorTraits<T>::Type());
	}


} // namespace shdfnd
} // namespace physx

#if !defined(PX_GNUC)
#pragma warning(pop)
#endif

#endif
