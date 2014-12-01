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


#ifndef PX_FOUNDATION_PSARRAY_H
#define PX_FOUNDATION_PSARRAY_H

#include "foundation/PxAssert.h"
#include "PsAllocator.h"
#include "PsUtilities.h"	// for swap()

#include "PsFPU.h"

namespace physx
{
namespace shdfnd
{
	template<class Serializer>
	void exportArray(Serializer& stream, const void* data, PxU32 size, PxU32 sizeOfElement, PxU32 capacity);
	char* importArray(char* address, void** data, PxU32 size, PxU32 sizeOfElement, PxU32 capacity);

	/*!
	An array is a sequential container.

	Implementation note
	* entries between 0 and size are valid objects
	* we use inheritance to build this because the array is included inline in a lot
	  of objects and we want the allocator to take no space if it's not stateful, which
	  aggregation doesn't allow. Also, we want the metadata at the front for the inline
	  case where the allocator contains some inline storage space
	*/
	template<class T, class Alloc = typename AllocatorTraits<T>::Type >
	class Array : protected Alloc
	{

	public:

		typedef T*			Iterator;
		typedef const T*	ConstIterator;

		explicit  Array(const PxEmpty& v) : Alloc(v)
		{
			if(mData)
				mCapacity |= PX_SIGN_BITMASK;
		}

		// PT: export can be slow, so no inline here
		template<class Serializer>
		void				exportArray(Serializer& stream, bool finalize)
		{
			// PT: we don't want to serialize useless data so one might want to shrink the array before export.
			// This is not always true though, sometimes you want to avoid runtime reallocations and really
			// export the full array capacity.
			//			(void)finalize;
			// ### For some reason if I shrink the arrays before export the cloth is not tearable anymore
			if(finalize)
				shrink();

			if(mData)
			{
				// PT: some issues here...
				// If capacity>size we still need to export the full capacity, else a push_back on a deserialized array
				// could overwrite deserialized memory. One solution would be to shrink the array on import.
				if(mSize)
		//			stream.storeBuffer(data, size*sizeOfElement);
					stream.storeBuffer(mData, capacity()*sizeof(T));
				else if(capacity())
					stream.storeBuffer(mData, capacity()*sizeof(T));
			}
		}

		// PT: import must be as fast as possible, so inline here
		PX_FORCE_INLINE char*		importArray(char* address)
		{
			void** data = PxUnionCast<void**, T**>(&mData);//(void**)&mData;

			if(*data)
			{
				if(mSize)
				{
					*data = address;
		//			address += size*sizeof(T);
					address += capacity()*sizeof(T);
				}
				else if(capacity())
				{
					*data = address;
					address += capacity()*sizeof(T);
				}
			}
			return address;
		}

		/*!
		Default array constructor. Initialize an empty array
		*/
		PX_INLINE explicit Array(const Alloc& alloc = Alloc())
			: Alloc(alloc), mData(0), mSize(0), mCapacity(0) 
		{}

		/*!
		Initialize array with given capacity
		*/
		PX_INLINE explicit Array(PxU32 size, const T& a = T(), const Alloc& alloc = Alloc())
		: Alloc(alloc), mData(0), mSize(0), mCapacity(0) 
		{
			resize(size, a);
		}

		/*!
		Copy-constructor. Copy all entries from other array
		*/
		template <class A> 
		PX_INLINE explicit Array(const Array<T,A>& other, const Alloc& alloc = Alloc())
		: Alloc(alloc)
		{
			copy(other);
		}

		// This is necessary else the basic default copy constructor is used in the case of both arrays being of the same template instance
		// The C++ standard clearly states that a template constructor is never a copy constructor [2]. In other words, 
		// the presence of a template constructor does not suppress the implicit declaration of the copy constructor.
		// Also never make a copy constructor explicit, or copy-initialization* will no longer work. This is because
		// 'binding an rvalue to a const reference requires an accessible copy constructor' (http://gcc.gnu.org/bugs/)
		// *http://stackoverflow.com/questions/1051379/is-there-a-difference-in-c-between-copy-initialization-and-assignment-initializ
		PX_INLINE Array(const Array& other, const Alloc& alloc = Alloc())
		: Alloc(alloc)
		{
			copy(other);
		}

		/*!
		Initialize array with given length
		*/
		PX_INLINE explicit Array(const T* first, const T* last, const Alloc& alloc = Alloc())
			: Alloc(alloc), mSize(last<first?0:(PxU32)(last-first)), mCapacity(mSize)
		{
			mData = allocate(mSize);
			copy(mData, mData + mSize, first);
		}

		/*!
		Destructor
		*/
		PX_INLINE ~Array()
		{
			destroy(mData, mData + mSize);

			if(capacity() && !isInUserMemory())
				deallocate(mData);
		}

		/*!
		Assignment operator. Copy content (deep-copy)
		*/
		template <class A> 
		PX_INLINE Array& operator= (const Array<T,A>& rhs)
		{
			if(&rhs == this)
				return *this;

			clear();
			reserve(rhs.mSize);
			copy(mData, mData + rhs.mSize, rhs.mData);

			mSize = rhs.mSize;
			return *this;
		}

		PX_INLINE Array& operator= (const Array& t)  // Needs to be declared, see comment at copy-constructor
		{
			return operator=<Alloc>(t);
		}

		/*!
		Array indexing operator.
		\param i
		The index of the element that will be returned.
		\return
		The element i in the array.
		*/
		PX_FORCE_INLINE const T& operator[] (PxU32 i) const 
		{
			PX_ASSERT(i < mSize);
			return mData[i];
		}

		/*!
		Array indexing operator.
		\param i
		The index of the element that will be returned.
		\return
		The element i in the array.
		*/
		PX_FORCE_INLINE T& operator[] (PxU32 i) 
		{
			PX_ASSERT(i < mSize);
			return mData[i];
		}

		/*!
		Returns a pointer to the initial element of the array.
		\return
		a pointer to the initial element of the array.
		*/
		PX_FORCE_INLINE ConstIterator begin() const 
		{
			return mData;
		}

		PX_FORCE_INLINE Iterator begin()
		{
			return mData;
		}

		/*!
		Returns an iterator beyond the last element of the array. Do not dereference.
		\return
		a pointer to the element beyond the last element of the array.
		*/

		PX_FORCE_INLINE ConstIterator end() const 
		{
			return mData+mSize;
		}

		PX_FORCE_INLINE Iterator end()
		{
			return mData+mSize;
		}

		/*!
		Returns a reference to the first element of the array. Undefined if the array is empty.
		\return a reference to the first element of the array
		*/

		PX_FORCE_INLINE const T& front() const 
		{
			PX_ASSERT(mSize);
			return mData[0];
		}

		PX_FORCE_INLINE T& front()
		{
			PX_ASSERT(mSize);
			return mData[0];
		}

		/*!
		Returns a reference to the last element of the array. Undefined if the array is empty
		\return a reference to the last element of the array
		*/

		PX_FORCE_INLINE const T& back() const 
		{
			PX_ASSERT(mSize);
			return mData[mSize-1];
		}

		PX_FORCE_INLINE T& back()
		{
			PX_ASSERT(mSize);
			return mData[mSize-1];
		}


		/*!
		Returns the number of entries in the array. This can, and probably will,
		differ from the array capacity.
		\return
		The number of of entries in the array.
		*/
		PX_FORCE_INLINE PxU32 size() const 
		{
			return mSize;
		}

		/*!
		Clears the array.
		*/
		PX_INLINE void clear() 
		{
			destroy(mData, mData + mSize);
			mSize = 0;
		}

		/*!
		Returns whether the array is empty (i.e. whether its size is 0).
		\return
		true if the array is empty
		*/
		PX_FORCE_INLINE bool empty() const
		{
			return mSize==0;
		}

		/*!
		Finds the first occurrence of an element in the array.
		\param a
		The element to find. 
		*/


		PX_INLINE Iterator find(const T& a)
		{
			PxU32 index;
			for(index=0;index<mSize && mData[index]!=a;index++)
				;
			return mData+index;
		}

		PX_INLINE ConstIterator find(const T& a) const
		{
			PxU32 index;
			for(index=0;index<mSize && mData[index]!=a;index++)
				;
			return mData+index;
		}


		/////////////////////////////////////////////////////////////////////////
		/*!
		Adds one element to the end of the array. Operation is O(1).
		\param a
		The element that will be added to this array.
		*/
		/////////////////////////////////////////////////////////////////////////

		PX_FORCE_INLINE T& pushBack(const T& a)
		{
			if(capacity()<=mSize) 
				grow(capacityIncrement());

			PX_PLACEMENT_NEW((void*)(mData + mSize),T)(a);

			return mData[mSize++];
		}

		/////////////////////////////////////////////////////////////////////////
		/*!
		Returns the element at the end of the array. Only legal if the array is non-empty.
		*/
		/////////////////////////////////////////////////////////////////////////
		PX_INLINE T popBack() 
		{
			PX_ASSERT(mSize);
			T t = mData[mSize-1];
			mData[--mSize].~T();
			return t;
		}


		/////////////////////////////////////////////////////////////////////////
		/*!
		Construct one element at the end of the array. Operation is O(1).
		*/
		/////////////////////////////////////////////////////////////////////////
		PX_INLINE T& insert()
		{
			if(capacity()<=mSize) 
				grow(capacityIncrement());

			return *(new (mData+mSize++)T);
		}

		/////////////////////////////////////////////////////////////////////////
		/*!
		Subtracts the element on position i from the array and replace it with
		the last element.
		Operation is O(1)
		\param i
		The position of the element that will be subtracted from this array.
		\return
		The element that was removed.
		*/
		/////////////////////////////////////////////////////////////////////////
		PX_INLINE void replaceWithLast(PxU32 i)
		{
			PX_ASSERT(i<mSize);
			mData[i] = mData[--mSize];
			mData[mSize].~T();
		}

		PX_INLINE void replaceWithLast(Iterator i) 
		{
			replaceWithLast(static_cast<PxU32>(i-mData));
		}

		/////////////////////////////////////////////////////////////////////////
		/*!
		Replaces the first occurrence of the element a with the last element
		Operation is O(n)
		\param i
		The position of the element that will be subtracted from this array.
		\return Returns true if the element has been removed.
		*/
		/////////////////////////////////////////////////////////////////////////

		PX_INLINE bool findAndReplaceWithLast(const T& a)
		{
			PxU32 index = 0;
			while(index<mSize && mData[index]!=a)
				++index;
			if(index == mSize)
				return false;
			replaceWithLast(index);
			return true;
		}

		/////////////////////////////////////////////////////////////////////////
		/*!
		Subtracts the element on position i from the array. Shift the entire
		array one step.
		Operation is O(n)
		\param i
		The position of the element that will be subtracted from this array.
		*/
		/////////////////////////////////////////////////////////////////////////
		PX_INLINE void remove(PxU32 i)
		{
			PX_ASSERT(i<mSize);
			for(T* it=mData+i; it->~T(), ++i<mSize; ++it)
				new(it)T(mData[i]);

			--mSize;
		}

		/////////////////////////////////////////////////////////////////////////
		/*!
		Removes a range from the array.  Shifts the array so order is maintained.
		Operation is O(n)
		\param begin
		The starting position of the element that will be subtracted from this array.
		\param end
		The ending position of the elment that will be subtracted from this array.
		*/
		/////////////////////////////////////////////////////////////////////////
		PX_INLINE void removeRange(PxU32 begin,PxU32 count)
		{
			PX_ASSERT(begin<mSize);
			PX_ASSERT( (begin+count) <= mSize );
			for (PxU32 i=0; i<count; i++)
			{
				mData[begin+i].~T(); // call the destructor on the ones being removed first.
			}
			T* dest = &mData[begin]; // location we are copying the tail end objects to
			T* src  = &mData[begin+count]; // start of tail objects
			PxU32 move_count = mSize - (begin+count); // compute remainder that needs to be copied down
			for (PxU32 i=0; i<move_count; i++)
			{
				new ( dest ) T(*src); // copy the old one to the new location
			    src->~T(); // call the destructor on the old location
				dest++;
				src++;
			}
			mSize-=count;
		}


		//////////////////////////////////////////////////////////////////////////
		/*!
		Resize array
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_NOINLINE void resize(const PxU32 size, const T& a = T());

		PX_NOINLINE void resizeUninitialized(const PxU32 size);

		//////////////////////////////////////////////////////////////////////////
		/*!
		Resize array such that only as much memory is allocated to hold the 
		existing elements
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_INLINE void shrink()
		{
			recreate(mSize);
		}


		//////////////////////////////////////////////////////////////////////////
		/*!
		Deletes all array elements and frees memory.
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_INLINE void reset()
		{
			resize(0);
			shrink();
		}


		//////////////////////////////////////////////////////////////////////////
		/*!
		Ensure that the array has at least size capacity.
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_INLINE void reserve(const PxU32 capacity)
		{
			if(capacity > this->capacity())
				grow(capacity);
		}

		//////////////////////////////////////////////////////////////////////////
		/*!
		Query the capacity(allocated mem) for the array.
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_FORCE_INLINE PxU32 capacity()	const
		{
			return mCapacity & ~PX_SIGN_BITMASK;
		}

		//////////////////////////////////////////////////////////////////////////
		/*!
		Unsafe function to force the size of the array
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_FORCE_INLINE void forceSize_Unsafe(PxU32 size)
		{
			PX_ASSERT(size<=mCapacity);
			mSize = size;
		}

		//////////////////////////////////////////////////////////////////////////
		/*!
		Swap contents of an array without allocating temporary storage
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_INLINE void swap(Array<T,Alloc>& other)
		{
			shdfnd::swap(mData, other.mData);
			shdfnd::swap(mSize, other.mSize);
			shdfnd::swap(mCapacity, other.mCapacity);
		}

		//////////////////////////////////////////////////////////////////////////
		/*!
		Assign a range of values to this vector (resizes to length of range)
		*/
		//////////////////////////////////////////////////////////////////////////
		PX_INLINE void assign(const T* first, const T* last)
		{
			resizeUninitialized(PxU32(last-first));
			copy(begin(), end(), first);
		}

		// We need one bit to mark arrays that have been deserialized from a user-provided memory block.
		// For alignment & memory saving purpose we store that bit in the rarely used capacity member.
		PX_FORCE_INLINE	PxU32		isInUserMemory()		const
		{
			return mCapacity & PX_SIGN_BITMASK;
		}

	protected:

		// constructor for where we don't own the memory
		Array(T* memory, PxU32 size, PxU32 capacity, const Alloc &alloc = Alloc()): 
			 Alloc(alloc),	mData(memory), mSize(size), mCapacity(capacity|PX_SIGN_BITMASK) {}

		template <class A> 
		PX_NOINLINE void copy(const Array<T,A>& other)
		{
			if(!other.empty())
			{
				mData = allocate(mSize = mCapacity = other.size());
				copy(mData, mData + mSize, other.begin());
			}
			else
			{
				mData = NULL;
				mSize = 0;
				mCapacity = 0;
			}

			//mData = allocate(other.mSize);
			//mSize = other.mSize;
			//mCapacity = other.mSize;
			//copy(mData, mData + mSize, other.mData);

		}

		PX_INLINE T* allocate(PxU32 size)
		{
			return size ? (T*)Alloc::allocate(sizeof(T) * size, __FILE__, __LINE__) : 0;
		}

		PX_INLINE void deallocate(void* mem)
		{
			Alloc::deallocate(mem);
		}

		static PX_INLINE void create(T* first, T* last, const T& a)
		{
			for(; first<last; ++first)
				::new(first)T(a);
		}

		static PX_INLINE void copy(T* first, T* last, const T* src)
		{
			for(; first<last; ++first, ++src)
				::new (first)T(*src);
		}

		static PX_INLINE void destroy(T* first, T* last)
		{
			for(; first<last; ++first)
				first->~T();
		}

		/*!
		Resizes the available memory for the array.

		\param capacity
		The number of entries that the set should be able to hold.
		*/	
		PX_INLINE void grow(PxU32 capacity) 
		{
			PX_ASSERT(this->capacity() < capacity);
			recreate(capacity);
		}

		/*!
		Creates a new memory block, copies all entries to the new block and destroys old entries.

		\param capacity
		The number of entries that the set should be able to hold.
		*/
		PX_NOINLINE void recreate(PxU32 capacity);

		// The idea here is to prevent accidental brain-damage with pushBack or insert. Unfortunately
		// it interacts badly with InlineArrays with smaller inline allocations.
		// TODO(dsequeira): policy template arg, this is exactly what they're for.
		PX_INLINE PxU32 capacityIncrement()	const
		{
			const PxU32 capacity = this->capacity();
			return capacity == 0 ? 1 : capacity * 2;
		}

		T*					mData;
		PxU32				mSize;
		PxU32				mCapacity;
	};

	template<class T, class Alloc>
	PX_NOINLINE void Array<T, Alloc>::resize(const PxU32 size, const T& a)
	{
		reserve(size);
		create(mData + mSize, mData + size, a);
		destroy(mData + size, mData + mSize);
		mSize = size;
	}

	template<class T, class Alloc>
	PX_NOINLINE void Array<T, Alloc>::resizeUninitialized(const PxU32 size)
	{
		reserve(size);
		mSize = size;
	}

	template<class T, class Alloc>
	PX_NOINLINE void Array<T, Alloc>::recreate(PxU32 capacity)
	{
		T* newData = allocate(capacity);
		PX_ASSERT(!capacity || newData && newData != mData);

		copy(newData, newData + mSize, mData);
		destroy(mData, mData + mSize);
		if(!isInUserMemory())
			deallocate(mData);

		mData = newData;
		mCapacity = capacity;
	}

	template<class T, class Alloc>
	PX_INLINE void swap(Array<T, Alloc>& x, Array<T, Alloc>& y)
	{
		x.swap(y);
	}

} // namespace shdfnd
} // namespace physx

#endif
