// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once

#define EXPORT extern "C" __declspec(dllexport)

typedef unsigned int uint;
typedef unsigned char uint8;
#define SAFE_DELETE(q){if(q){delete q;q=NULL;}else 0;}

extern WCHAR* CreateOutString(const WCHAR* str);

///////////////////////////////////////////////////////////////////////////////////////////////

struct DSoundStructureSizes
{
public:

	int sizeWAVEFORMATEX;
	int sizeGUID;
	int sizeDSBUFFERDESC;
	int sizeDSBCAPS;
	int sizeDSCBCAPS;
	int sizeDSCBUFFERDESC;
	int sizeDS3DLISTENER;

	void Init()
	{
		sizeWAVEFORMATEX = sizeof( WAVEFORMATEX );
		sizeGUID = sizeof( GUID );
		sizeDSBUFFERDESC = sizeof( DSBUFFERDESC );
		sizeDSBCAPS = sizeof( DSBCAPS );
		sizeDSCBCAPS = sizeof( DSCBCAPS );
		sizeDSCBUFFERDESC = sizeof( DSCBUFFERDESC );
		sizeDS3DLISTENER = sizeof( DS3DLISTENER );
	}
};

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT void DSound_GetStructureSizes( DSoundStructureSizes* sizes )
{
	DSoundStructureSizes originalSizes;
	originalSizes.Init();
	*sizes = originalSizes;
}

EXPORT int DSound_CoInitialize(void* pvReserved)
{
	return CoInitialize(pvReserved);
}

EXPORT int DSound_DirectSoundCreate8( LPCGUID pcGuidDevice,
	LPDIRECTSOUND8* ppDS8, LPUNKNOWN pUnkOuter )
{
	return DirectSoundCreate8(pcGuidDevice, ppDS8, pUnkOuter);
}

EXPORT int DSound_Get_DSERR_BUFFERLOST()
{
	return DSERR_BUFFERLOST;
}

EXPORT int DSound_Get_DSERR_NODRIVER()
{
	return DSERR_NODRIVER;
}

EXPORT int DSound_DirectSoundCaptureCreate( LPCGUID pcGuidDevice,
	LPDIRECTSOUNDCAPTURE* ppDSC, LPUNKNOWN pUnkOuter )
{
	return DirectSoundCaptureCreate(pcGuidDevice, ppDSC, pUnkOuter);
}

EXPORT int DSound_DirectSoundCaptureEnumerateW( LPDSENUMCALLBACKW pDSEnumCallback, void* pContext )
{
	#ifndef _UNICODE
		#error need unicode
	#endif

	return DirectSoundCaptureEnumerate(pDSEnumCallback, pContext);
}

EXPORT const WCHAR* DSound_DXGetErrorStringW( int hr )
{
	#ifndef _UNICODE
		#error need unicode
	#endif

	const WCHAR* str = DXGetErrorString(hr);
	if(!str)
		return NULL;
	return CreateOutString(str);
}

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT uint NIDirectSound8_Release( IDirectSound8* _this )
{
	return _this->Release();
}

EXPORT int NIDirectSound8_CreateSoundBuffer( IDirectSound8* _this,
	LPCDSBUFFERDESC pcDSBufferDesc, LPDIRECTSOUNDBUFFER* ppDSBuffer, LPUNKNOWN pUnkOuter )
{
	return _this->CreateSoundBuffer(pcDSBufferDesc, ppDSBuffer, pUnkOuter);
}

EXPORT int NIDirectSound8_DuplicateSoundBuffer( IDirectSound8* _this,
	LPDIRECTSOUNDBUFFER pDSBufferOriginal, LPDIRECTSOUNDBUFFER* ppDSBufferDuplicate )
{
	return _this->DuplicateSoundBuffer(pDSBufferOriginal, ppDSBufferDuplicate);
}

EXPORT int NIDirectSound8_SetCooperativeLevel( IDirectSound8* _this, HWND hWnd, uint dwLevel )
{
	return _this->SetCooperativeLevel(hWnd, dwLevel);
}

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT uint NIDirectSound3DListener_Release( IDirectSound3DListener* _this )
{
	return _this->Release();
}

EXPORT int NIDirectSound3DListener_GetAllParameters( IDirectSound3DListener* _this,
	LPDS3DLISTENER pListener )
{
	return _this->GetAllParameters(pListener);
}

EXPORT int NIDirectSound3DListener_SetAllParameters( IDirectSound3DListener* _this,
	LPCDS3DLISTENER pListener, uint dwApply )
{
	return _this->SetAllParameters(pListener, dwApply);
}

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT int NIDirectSoundBuffer_QueryInterface( IDirectSoundBuffer* _this,
	REFIID pGuid, LPVOID* pInterface )
{
	return _this->QueryInterface(pGuid, pInterface);
}

EXPORT uint NIDirectSoundBuffer_Release( IDirectSoundBuffer* _this )
{
	return _this->Release();
}

EXPORT int NIDirectSoundBuffer_GetCaps( IDirectSoundBuffer* _this, LPDSBCAPS pDSCaps )
{
	return _this->GetCaps(pDSCaps);
}

EXPORT int NIDirectSoundBuffer_GetStatus( IDirectSoundBuffer* _this, LPDWORD pdwStatus )
{
	return _this->GetStatus(pdwStatus);
}

EXPORT int NIDirectSoundBuffer_Lock( IDirectSoundBuffer* _this, uint dwOffset, uint dwBytes,
	LPVOID* ppvAudioPtr1, LPDWORD pdwAudioBytes1,
	LPVOID* ppvAudioPtr2, LPDWORD pdwAudioBytes2, uint dwFlags )
{
	return _this->Lock(dwOffset, dwBytes, ppvAudioPtr1, pdwAudioBytes1,
		ppvAudioPtr2, pdwAudioBytes2, dwFlags);
}

EXPORT int NIDirectSoundBuffer_Unlock( IDirectSoundBuffer* _this, LPVOID pvAudioPtr1,
	uint dwAudioBytes1, LPVOID pvAudioPtr2, uint dwAudioBytes2 )
{
	return _this->Unlock(pvAudioPtr1, dwAudioBytes1, pvAudioPtr2, dwAudioBytes2);
}

EXPORT int NIDirectSoundBuffer_Play( IDirectSoundBuffer* _this, uint dwReserved1,
	uint dwPriority, uint dwFlags )
{
	return _this->Play(dwReserved1, dwPriority, dwFlags);
}

EXPORT int NIDirectSoundBuffer_GetCurrentPosition( IDirectSoundBuffer* _this,
	LPDWORD pdwCurrentPlayCursor, LPDWORD pdwCurrentWriteCursor )
{
	return _this->GetCurrentPosition(pdwCurrentPlayCursor, pdwCurrentWriteCursor);
}

EXPORT int NIDirectSoundBuffer_SetCurrentPosition( IDirectSoundBuffer* _this,
	uint dwNewPosition )
{
	return _this->SetCurrentPosition(dwNewPosition);
}

EXPORT int NIDirectSoundBuffer_SetFormat( IDirectSoundBuffer* _this, 
	LPCWAVEFORMATEX pcfxFormat )
{
	return _this->SetFormat(pcfxFormat);
}

EXPORT int NIDirectSoundBuffer_GetVolume( IDirectSoundBuffer* _this, LPLONG plVolume )
{
	return _this->GetVolume(plVolume);
}

EXPORT int NIDirectSoundBuffer_SetVolume( IDirectSoundBuffer* _this, int lVolume )
{
	return _this->SetVolume(lVolume);
}

EXPORT int NIDirectSoundBuffer_SetPan( IDirectSoundBuffer* _this, int lPan )
{
	return _this->SetPan(lPan);
}

EXPORT int NIDirectSoundBuffer_SetFrequency( IDirectSoundBuffer* _this, uint dwFrequency )
{
	return _this->SetFrequency(dwFrequency);
}

EXPORT int NIDirectSoundBuffer_Stop( IDirectSoundBuffer* _this )
{
	return _this->Stop();
}

EXPORT int NIDirectSoundBuffer_Restore( IDirectSoundBuffer* _this )
{
	return _this->Restore();
}

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT uint NIDirectSound3DBuffer8_Release( IDirectSound3DBuffer8* _this )
{
	return _this->Release();
}

EXPORT int NIDirectSound3DBuffer8_SetPosition( IDirectSound3DBuffer8* _this,
	float x, float y, float z, uint dwApply )
{
	return _this->SetPosition(x, y, z, dwApply);
}

EXPORT int NIDirectSound3DBuffer8_SetVelocity( IDirectSound3DBuffer8* _this,
	float x, float y, float z, uint dwApply )
{
	return _this->SetVelocity(x, y, z, dwApply);
}

EXPORT int NIDirectSound3DBuffer8_SetMinDistance( IDirectSound3DBuffer8* _this,
	float flMinDistance, uint dwApply )
{
	return _this->SetMinDistance(flMinDistance, dwApply);
}

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT uint NIDirectSoundCapture8_Release( IDirectSoundCapture8* _this )
{
	return _this->Release();
}

EXPORT int NIDirectSoundCapture8_CreateCaptureBuffer( IDirectSoundCapture8* _this,
	LPCDSCBUFFERDESC pcDSCBufferDesc, LPDIRECTSOUNDCAPTUREBUFFER* ppDSCBuffer, 
	LPUNKNOWN pUnkOuter )
{
	return _this->CreateCaptureBuffer(pcDSCBufferDesc, ppDSCBuffer, pUnkOuter);
}

///////////////////////////////////////////////////////////////////////////////////////////////

EXPORT uint NIDirectSoundCaptureBuffer_Release( IDirectSoundCaptureBuffer* _this )
{
	return _this->Release();
}

EXPORT int NIDirectSoundCaptureBuffer_GetCaps( IDirectSoundCaptureBuffer* _this, 
	LPDSCBCAPS pDSCBCaps )
{
	return _this->GetCaps(pDSCBCaps);
}

EXPORT int NIDirectSoundCaptureBuffer_GetCurrentPosition( IDirectSoundCaptureBuffer* _this,
	LPDWORD pdwCapturePosition, LPDWORD pdwReadPosition )
{
	return _this->GetCurrentPosition(pdwCapturePosition, pdwReadPosition);
}

EXPORT int NIDirectSoundCaptureBuffer_Lock( IDirectSoundCaptureBuffer* _this, 
	uint dwOffset, uint dwBytes, LPVOID* ppvAudioPtr1, LPDWORD pdwAudioBytes1,
	LPVOID* ppvAudioPtr2, LPDWORD pdwAudioBytes2, uint dwFlags )
{
	return _this->Lock(dwOffset, dwBytes, ppvAudioPtr1, pdwAudioBytes1,
		ppvAudioPtr2, pdwAudioBytes2, dwFlags);
}

EXPORT int NIDirectSoundCaptureBuffer_Unlock( IDirectSoundCaptureBuffer* _this, 
	LPVOID pvAudioPtr1, uint dwAudioBytes1, LPVOID pvAudioPtr2, uint dwAudioBytes2 )
{
	return _this->Unlock(pvAudioPtr1, dwAudioBytes1, pvAudioPtr2, dwAudioBytes2);
}

EXPORT int NIDirectSoundCaptureBuffer_Start( IDirectSoundCaptureBuffer* _this, uint dwFlags )
{
	return _this->Start(dwFlags);
}

EXPORT int NIDirectSoundCaptureBuffer_Stop( IDirectSoundCaptureBuffer* _this )
{
	return _this->Stop();
}

///////////////////////////////////////////////////////////////////////////////////////////////
