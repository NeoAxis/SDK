// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#ifdef _WIN32
	#define EXPORT extern "C" __declspec(dllexport)
#else
	#define EXPORT extern "C" __attribute__ ((visibility("default")))
#endif

typedef unsigned int uint;
typedef unsigned char uint8;
typedef unsigned short uint16;
#define SAFE_DELETE(q){if(q){delete q;q=NULL;}else 0;}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

typedef void ReportErrorDelegate( PxErrorCode::Enum code, wchar16* message, wchar16* file, int line );
wchar16* CreateOutString(const String& str);
typedef void LogDelegate( wchar16* message );
void LogMessage(const char* text);
void Fatal(const char* text);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class MyErrorCallback : public PxErrorCallback
{
public:
	ReportErrorDelegate* reportErrorDelegate;

	virtual void reportError(PxErrorCode::Enum code, const char* message, const char* file, int line)
	{
		reportErrorDelegate(code, CreateOutString(message), CreateOutString(file), line);
	}
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class MyMemoryOutputStream : public PxOutputStream
{
private:
	PxU8* mData;
	PxU32 mSize;
	PxU32 mCapacity;

public:
	MyMemoryOutputStream(): mData(NULL), mSize(0), mCapacity(0) {}

	MyMemoryOutputStream(int initialCapacity)
	{
		mCapacity = initialCapacity;
		mSize = 0;
		mData = new PxU8[mCapacity];
	}

	virtual ~MyMemoryOutputStream()
	{
		if(mData)
			delete[] mData;
	}

	PxU32 write(const void* src, PxU32 size)
	{
		PxU32 expectedSize = mSize + size;
		if(expectedSize > mCapacity)
		{
			if(mCapacity == 0)
				mCapacity = 4;
			while(expectedSize > mCapacity)
				mCapacity *= 2;

			PxU8* newData = new PxU8[mCapacity];
			if(mData)
			{
				memcpy(newData, mData, mSize);
				delete[] mData;
			}
			mData = newData;
		}
		memcpy(mData+mSize, src, size);
		mSize += size;
		return size;
	}

	PxU32 getSize()	const {	return mSize; }
	PxU8* getData()	const {	return mData; }
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class MyMemoryInputData : public PxInputData
{
private:
	PxU8* mData;
	PxU32 mSize;
	PxU32 mPosition;

public:
	MyMemoryInputData(PxU8* data, PxU32 size)
	{
		this->mData = data;
		this->mSize = size;
		this->mPosition = 0;
	}

	PxU32 read(void* dest, PxU32 count)
	{
		PxU32 length = PxMin<PxU32>(count, mSize - mPosition);
		memcpy(dest, mData + mPosition, length);
		mPosition += length;
		return length;
	}

	PxU32 getLength() const
	{
		return mSize;
	}

	void seek(PxU32 offset)
	{
		mPosition = PxMin<PxU32>(mSize, offset);
	}

	PxU32 tell() const
	{
		return mPosition;
	}
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class MyAllocator : public PxAllocatorCallback
{
public:

	void* allocate(size_t size, const char* typeName, const char* filename, int line)
	{
		//return _aligned_malloc(size, 16);
		return Memory_AllocAligned( MemoryAllocationType_Physics, size, 16, filename, line );
	}

	void deallocate(void* ptr)
	{
		//_aligned_free(ptr);
		Memory_FreeAligned( ptr );
	}
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

extern LogDelegate* logDelegate;
extern PhysXWorld* world;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
