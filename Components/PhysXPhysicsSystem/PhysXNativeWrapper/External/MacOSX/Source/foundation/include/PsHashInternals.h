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


#ifndef PX_FOUNDATION_PSHASHINTERNALS_H
#define PX_FOUNDATION_PSHASHINTERNALS_H

#include "PsBasicTemplates.h"
#include "PsArray.h"
#include "PsBitUtils.h"
#include "PsHash.h"
#include "PsNoCopy.h"

#ifdef PX_VC
#pragma warning(push)
#pragma warning(disable: 4512) // disable the 'assignment operator could not be generated' warning message
#endif

namespace physx
{
namespace shdfnd
{
	namespace internal
	{
		template <class Entry,
				  class Key,
				  class HashFn,
				  class GetKey,
				  class Allocator,
				  bool compacting>
		class HashBase : private Allocator
		{
			void init(PxU32 initialTableSize, float loadFactor)
			{
				mLoadFactor = loadFactor;
				mFreeList = (PxU32)EOL;
				mTimestamp = mSize = 0;
				mEntries = 0;

				if(initialTableSize)
					reserveInternal(initialTableSize);
			}

		public:
			typedef Entry EntryType;

			HashBase(PxU32 initialTableSize = 64, float loadFactor = 0.75f)
			:	Allocator(PX_DEBUG_EXP("hashBaseEntries")),
				mNext(Allocator(PX_DEBUG_EXP("hashBaseNext"))),
				mHash(Allocator(PX_DEBUG_EXP("hashBaseHash")))
			{
				init(initialTableSize, loadFactor);
			}

			HashBase(PxU32 initialTableSize, float loadFactor, const Allocator &alloc)
			:	Allocator(alloc),
				mNext(Allocator(alloc)),
				mHash(Allocator(alloc))
			{
				init(initialTableSize, loadFactor);
			}

			HashBase(const Allocator &alloc)
			:	Allocator(alloc),
				mNext(Allocator(alloc)),
				mHash(Allocator(alloc))
			{
				init(64, 0.75f);
			}

			~HashBase()
			{
				destroy(); //No need to clear()

				if(mEntries)
					Allocator::deallocate(mEntries);
			}

			static const PxU32 EOL = 0xffffffff;

			PX_INLINE Entry* create(const Key &k, bool &exists)
			{
				PxU32 h=0;
				if(mHash.size())
				{
					h = hash(k);
					PxU32 index = mHash[h];
					while(index!=EOL && !HashFn()(GetKey()(mEntries[index]), k))
						index = mNext[index];
					exists = index!=EOL;
					if(exists)
						return &mEntries[index];
				} else
					exists = false;

				if(freeListEmpty())
				{
					grow();
					h = hash(k);
				}

				PxU32 entryIndex = freeListGetNext();

				mNext[entryIndex] = mHash[h];
				mHash[h] = entryIndex;

				mSize++;
				mTimestamp++;

				return &mEntries[entryIndex];
			}

			PX_INLINE const Entry* find(const Key &k) const
			{
				if(!mHash.size())
					return NULL;

				PxU32 h = hash(k);
				PxU32 index = mHash[h];
				while(index!=EOL && !HashFn()(GetKey()(mEntries[index]), k))
					index = mNext[index];
				return index != EOL ? &mEntries[index]:0;
			}

			PX_INLINE bool erase(const Key &k)
			{
				if(!mHash.size())
					return false;

				PxU32 h = hash(k);
				PxU32 *ptr = &mHash[h];
				while(*ptr!=EOL && !HashFn()(GetKey()(mEntries[*ptr]), k))
					ptr = &mNext[*ptr];

				if(*ptr == EOL)
					return false;

				PxU32 index = *ptr;
				*ptr = mNext[index];

				mEntries[index].~Entry();

				mSize--;
				mTimestamp++;

				if(compacting && index!=mSize)
					replaceWithLast(index);

				freeListAdd(index);

				return true;
			}

			PX_INLINE PxU32 size() const
			{ 
				return mSize; 
			}

			void clear()
			{
				if(!mHash.size())
					return;

				for(PxU32 i = 0;i<mHash.size();i++)
					mHash[i] = (PxU32)EOL;
				for(PxU32 i = 0;i<mNext.size()-1;i++)
					mNext[i] = i+1;
				mNext.back() = (PxU32)EOL;
				mFreeList = 0;
				mSize = 0;
			}

			void reserve(PxU32 size)
			{
				if(size>mHash.size())
					reserveInternal(size);
			}

			PX_INLINE const Entry *getEntries() const
			{
				return &mEntries[0];
			}

		private:

			void destroy()
			{
				for(PxU32 i = 0;i<mHash.size();i++)
				{				
					for(PxU32 j = mHash[i]; j != EOL; j = mNext[j])
						mEntries[j].~Entry();
				}
			}

			template <typename HK, typename GK, class A, bool comp> 
			PX_NOINLINE void copy(const HashBase<Entry,Key,HK,GK,A,comp>& other)
			{
				reserve(other.mSize);

				for(PxU32 i = 0;i < other.size();i++)
				{
					for(PxU32 j = other.mHash[i]; j != EOL; j = other.mNext[j])
					{
						const Entry &otherEntry = other.mEntries[j];

						bool exists;
						Entry *newEntry = create(GK()(otherEntry), exists);
						PX_ASSERT(!exists);

						PX_PLACEMENT_NEW(newEntry, Entry)(otherEntry);
					}
				}
			}

			// free list management - if we're coalescing, then we use mFreeList to hold
			// the top of the free list and it should always be equal to size(). Otherwise,
			// we build a free list in the next() pointers.

			PX_INLINE void freeListAdd(PxU32 index)
			{
				if(compacting)
				{
					mFreeList--;
					PX_ASSERT(mFreeList == mSize);
				}
				else
				{
					mNext[index] = mFreeList;
					mFreeList = index;
				}
			}

			PX_INLINE void freeListAdd(PxU32 start, PxU32 end)
			{
				if(!compacting)
				{
					for(PxU32 i = start; i<end-1; i++)	// add the new entries to the free list
						mNext[i] = i+1;
					mNext[end-1] = (PxU32)EOL;
				}
				mFreeList = start;
			}

			PX_INLINE PxU32 freeListGetNext()
			{
				PX_ASSERT(!freeListEmpty());
				if(compacting)
				{
					PX_ASSERT(mFreeList == mSize);
					return mFreeList++;
				}
				else
				{
					PxU32 entryIndex = mFreeList;
					mFreeList = mNext[mFreeList];
					return entryIndex;
				}
			}

			PX_INLINE bool freeListEmpty()	const
			{
				if(compacting)
					return mSize == mNext.size();
				else
					return mFreeList == EOL;
			}

			PX_INLINE void replaceWithLast(PxU32 index)
			{
				PX_PLACEMENT_NEW(&mEntries[index], Entry)(mEntries[mSize]);
				mEntries[mSize].~Entry();
				mNext[index] = mNext[mSize];

				PxU32 h = hash(GetKey()(mEntries[index]));
				PxU32 *ptr;
				for(ptr = &mHash[h]; *ptr!=mSize; ptr = &mNext[*ptr])
					PX_ASSERT(*ptr!=EOL);
				*ptr = index;
			}


			PX_INLINE PxU32 hash(const Key &k) const
			{
				return HashFn()(k)&(mHash.size()-1);
			}

			void reserveInternal(PxU32 size)
			{
				size = nextPowerOfTwo(size);
				// resize the hash and reset
				mHash.resize(size);
				for(PxU32 i=0;i<mHash.size();i++)
					mHash[i] = (PxU32)EOL;

				PX_ASSERT(!(mHash.size()&(mHash.size()-1)));

				PxU32 oldSize = mNext.size();
				PxU32 newSize = PxU32(float(mHash.size())*mLoadFactor);

				// resize entry array
				Entry* newEntries = (Entry*)Allocator::allocate(newSize * sizeof(Entry), __FILE__, __LINE__);
				for(PxU32 i=0; i<mNext.size(); ++i)
				{
					PX_PLACEMENT_NEW(newEntries+i, Entry)(mEntries[i]);
					mEntries[i].~Entry();
				}
				Allocator::deallocate(mEntries);
				mEntries = newEntries;

				mNext.resize(newSize);

				freeListAdd(oldSize,newSize);

				// rehash all the existing entries
				for(PxU32 i=0;i<oldSize;i++)
				{
					PxU32 h = hash(GetKey()(mEntries[i]));
					mNext[i] = mHash[h];
					mHash[h] = i;
				}
			}

			void grow()
			{
				PX_ASSERT(mFreeList == EOL || compacting && mSize == mNext.size());

				PxU32 size = mHash.size()==0 ? 16 : mHash.size()*2;
				reserve(size);
			}

			Entry*					mEntries; // same size/capacity as mNext
			Array<PxU32, Allocator>	mNext;
			Array<PxU32, Allocator>	mHash;
			float					mLoadFactor;
			PxU32					mFreeList;
			PxU32					mTimestamp;
			PxU32					mSize;

		public:

			class Iter
			{
			public:
				PX_INLINE Iter(HashBase& b): mBucket(0), mEntry((PxU32)b.EOL), mTimestamp(b.mTimestamp), mBase(b)
				{
					if(mBase.mNext.size()>0)
					{
						mEntry = mBase.mHash[0];
						skip();
					}
				}

				PX_INLINE void check() const		{ PX_ASSERT(mTimestamp == mBase.mTimestamp);	}
				PX_INLINE Entry operator*()	const	{ check(); return mBase.mEntries[mEntry];		}
				PX_INLINE Entry* operator->() const	{ check(); return &mBase.mEntries[mEntry];		}
				PX_INLINE Iter operator++()			{ check(); advance(); return *this;				}
				PX_INLINE Iter operator++(int)		{ check(); Iter i = *this; advance(); return i;	}
				PX_INLINE bool done() const			{ check(); return mEntry == mBase.EOL;			}

			private:
				PX_INLINE void advance()			{ mEntry = mBase.mNext[mEntry]; skip();		    }
				PX_INLINE void skip()
				{
					while(mEntry==mBase.EOL) 
					{ 
						if(++mBucket == mBase.mHash.size())
							break;
						mEntry = mBase.mHash[mBucket];
					}
				}

				PxU32 mBucket;
				PxU32 mEntry;
				PxU32 mTimestamp;
				HashBase &mBase;
			};
		};

		template <class Key, 
				  class HashFn, 
				  class Allocator = Allocator,
				  bool Coalesced = false>
		class HashSetBase : private NoCopy
		{ 
		public:
			struct GetKey { PX_INLINE const Key &operator()(const Key &e) {	return e; }	};

			typedef HashBase<Key, Key, HashFn, GetKey, Allocator, Coalesced> BaseMap;
			typedef typename BaseMap::Iter Iterator;

			HashSetBase(PxU32 initialTableSize, 
						float loadFactor,
						const Allocator &alloc):	mBase(initialTableSize,loadFactor,alloc)	{}

			HashSetBase(const Allocator &alloc):	mBase(64,0.75f,alloc)	{}

			HashSetBase(PxU32 initialTableSize = 64,
						float loadFactor = 0.75f):	mBase(initialTableSize,loadFactor)	{}

			bool insert(const Key &k)
			{
				bool exists;
				Key *e = mBase.create(k,exists);
				if(!exists)
					PX_PLACEMENT_NEW(e, Key)(k);
				return !exists;
			}

			PX_INLINE bool		contains(const Key &k)	const	{	return mBase.find(k)!=0;		}
			PX_INLINE bool		erase(const Key &k)				{	return mBase.erase(k);			}
			PX_INLINE PxU32		size()					const	{	return mBase.size();			}
			PX_INLINE void		reserve(PxU32 size)				{	mBase.reserve(size);			}
			PX_INLINE void		clear()							{	mBase.clear();					}
		protected:
			BaseMap mBase;

		};

		template <class Key, 
			  class Value,
			  class HashFn, 
			  class Allocator = Allocator >

		class HashMapBase : private NoCopy
		{ 
		public:
			typedef Pair<const Key,Value> Entry;

			struct GetKey 
			{ 
				PX_INLINE const Key& operator()(const Entry& e) 
				{ 
					return e.first; 
				}	
			};

			typedef HashBase<Entry, Key, HashFn, GetKey, Allocator, true> BaseMap;
			typedef typename BaseMap::Iter Iterator;

			HashMapBase(PxU32 initialTableSize, float loadFactor, const Allocator &alloc):	mBase(initialTableSize,loadFactor,alloc)	{}

			HashMapBase(const Allocator &alloc):	mBase(64,0.75f,alloc)	{}

			HashMapBase(PxU32 initialTableSize = 64, float loadFactor = 0.75f):	mBase(initialTableSize,loadFactor)	{}

			bool insert(const Key /*&*/k, const Value /*&*/v)
			{
				bool exists;
				Entry *e = mBase.create(k,exists);
				if(!exists)
					PX_PLACEMENT_NEW(e, Entry)(k,v);
				return !exists;
			}

			Value &operator [](const Key &k)
			{
				bool exists;
				Entry *e = mBase.create(k, exists);
				if(!exists)
					PX_PLACEMENT_NEW(e, Entry)(k,Value());
		
				return e->second;
			}

			PX_INLINE const Entry *	find(const Key &k)		const	{	return mBase.find(k);			}
			PX_INLINE bool			erase(const Key &k)				{	return mBase.erase(k);			}
			PX_INLINE PxU32			size()					const	{	return mBase.size();			}
			PX_INLINE Iterator		getIterator()					{	return Iterator(mBase);			}
			PX_INLINE void			reserve(PxU32 size)				{	mBase.reserve(size);			}
			PX_INLINE void			clear()							{	mBase.clear();					}

		protected:
			BaseMap mBase;
		};

	}

} // namespace shdfnd
} // namespace physx

#ifdef PX_VC
#pragma warning(pop)
#endif

#endif
