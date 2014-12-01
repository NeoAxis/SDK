// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Runtime.InteropServices;
using System.Security;
using Engine.MathEx;

namespace DirectSoundSystem
{
	struct Wrapper
	{
		public const string library = "DirectSoundNativeWrapper";
		public const CallingConvention convention = CallingConvention.Cdecl;

		public static bool FAILED( int hr )
		{
			return hr < 0;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DSoundStructureSizes
	{
		int sizeWAVEFORMATEX;
		int sizeGUID;
		int sizeDSBUFFERDESC;
		int sizeDSBCAPS;
		int sizeDSCBCAPS;
		int sizeDSCBUFFERDESC;
		int sizeDS3DLISTENER;

		//

		unsafe public void Init()
		{
			sizeWAVEFORMATEX = sizeof( WAVEFORMATEX );
			sizeGUID = sizeof( GUID );
			sizeDSBUFFERDESC = sizeof( DSBUFFERDESC );
			sizeDSBCAPS = sizeof( DSBCAPS );
			sizeDSCBCAPS = sizeof( DSCBCAPS );
			sizeDSCBUFFERDESC = sizeof( DSCBUFFERDESC );
			sizeDS3DLISTENER = sizeof( DS3DLISTENER );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	//[StructLayout( LayoutKind.Sequential, Size = 18 )]
	[StructLayout( LayoutKind.Explicit, Size = 18 )]
	struct WAVEFORMATEX
	{
		[FieldOffset( 0 )]
		public ushort wFormatTag;
		[FieldOffset( 2 )]
		public ushort nChannels;
		[FieldOffset( 4 )]
		public uint nSamplesPerSec;
		[FieldOffset( 8 )]
		public uint nAvgBytesPerSec;
		[FieldOffset( 12 )]
		public ushort nBlockAlign;
		[FieldOffset( 14 )]
		public ushort wBitsPerSample;
		[FieldOffset( 16 )]
		public ushort cbSize;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct GUID
	{
		uint data1;
		ushort data2;
		ushort data3;
		byte data40;
		byte data41;
		byte data42;
		byte data43;
		byte data44;
		byte data45;
		byte data46;
		byte data47;

		public GUID( uint data1, ushort data2, ushort data3, byte data40, byte data41, 
			byte data42, byte data43, byte data44, byte data45, byte data46, byte data47 )
		{
			this.data1 = data1;
			this.data2 = data2;
			this.data3 = data3;
			this.data40 = data40;
			this.data41 = data41;
			this.data42 = data42;
			this.data43 = data43;
			this.data44 = data44;
			this.data45 = data45;
			this.data46 = data46;
			this.data47 = data47;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DSBUFFERDESC
	{
		public uint dwSize;
		public uint dwFlags;
		public uint dwBufferBytes;
		public uint dwReserved;
		public unsafe WAVEFORMATEX* lpwfxFormat;
		//#if DIRECTSOUND_VERSION >= 0x0700
		public GUID guid3DAlgorithm;
		//#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DSBCAPS
	{
		public uint dwSize;
		public uint dwFlags;
		public uint dwBufferBytes;
		public uint dwUnlockTransferRate;
		public uint dwPlayCpuOverhead;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DSCBCAPS
	{
		public uint dwSize;
		public uint dwFlags;
		public uint dwBufferBytes;
		public uint dwReserved;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DSCEFFECTDESC
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DSCBUFFERDESC
	{
		public uint dwSize;
		public uint dwFlags;
		public uint dwBufferBytes;
		public uint dwReserved;
		public unsafe WAVEFORMATEX* lpwfxFormat;
		//#if DIRECTSOUND_VERSION >= 0x0800
		public uint dwFXCount;
		public unsafe DSCEFFECTDESC* lpDSCFXDesc;
		//#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	struct DS3DLISTENER
	{
		public uint dwSize;
		public Vec3 vPosition;
		public Vec3 vVelocity;
		public Vec3 vOrientFront;
		public Vec3 vOrientTop;
		public float flDistanceFactor;
		public float flRolloffFactor;
		public float flDopplerFactor;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[UnmanagedFunctionPointer( CallingConvention.StdCall )]
	unsafe delegate int/*BOOL*/ DSENUMCALLBACKW( void*/*GUID*/ pGuid,
		[MarshalAs( UnmanagedType.LPWStr )] string description,
		[MarshalAs( UnmanagedType.LPWStr )] string module, void* pContext );

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct DSound
	{
		public const int S_OK = 0x00000000;

		public const uint DSSCL_NORMAL = 0x00000001;
		public const uint DSSCL_PRIORITY = 0x00000002;

		public const uint DSBCAPS_PRIMARYBUFFER = 0x00000001;
		public const uint DSBCAPS_STATIC = 0x00000002;
		public const uint DSBCAPS_LOCHARDWARE = 0x00000004;
		public const uint DSBCAPS_LOCSOFTWARE = 0x00000008;
		public const uint DSBCAPS_CTRL3D = 0x00000010;
		public const uint DSBCAPS_CTRLFREQUENCY = 0x00000020;
		public const uint DSBCAPS_CTRLPAN = 0x00000040;
		public const uint DSBCAPS_CTRLVOLUME = 0x00000080;
		public const uint DSBCAPS_CTRLPOSITIONNOTIFY = 0x00000100;
		public const uint DSBCAPS_CTRLFX = 0x00000200;
		public const uint DSBCAPS_STICKYFOCUS = 0x00004000;
		public const uint DSBCAPS_GLOBALFOCUS = 0x00008000;
		public const uint DSBCAPS_GETCURRENTPOSITION2 = 0x00010000;
		public const uint DSBCAPS_MUTE3DATMAXDISTANCE = 0x00020000;
		public const uint DSBCAPS_LOCDEFER = 0x00040000;

		public const uint DS3D_IMMEDIATE = 0x00000000;
		public const uint DS3D_DEFERRED = 0x00000001;

		public const uint DSCBSTART_LOOPING = 0x00000001;

		public const uint DSBPLAY_LOOPING = 0x00000001;

		public const int DSBVOLUME_MIN = -10000;
		public const int DSBVOLUME_MAX = 0;

		public const int DSBPAN_LEFT = -10000;
		public const int DSBPAN_RIGHT = 10000;

		public const uint DSBSTATUS_PLAYING = 0x00000001;
		public const uint DSBSTATUS_BUFFERLOST = 0x00000002;

		public const ushort WAVE_FORMAT_PCM = 1;

		public static GUID DS3DALG_DEFAULT = new GUID( 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 );
		public static GUID IID_IDirectSound3DBuffer8 = new GUID(
			0x279AFA86, 0x4981, 0x11CE, 0xA5, 0x21, 0x00, 0x20, 0xAF, 0x0B, 0xE5, 0x60 );
		public static GUID IID_IDirectSound3DListener = new GUID(
			0x279AFA84, 0x4981, 0x11CE, 0xA5, 0x21, 0x00, 0x20, 0xAF, 0x0B, 0xE5, 0x60 );

		//

		////////////////////////////////////////////////////////////////////////////////////////////////

		[DllImport( Wrapper.library, EntryPoint = "DSound_FreeOutString", CallingConvention = Wrapper.convention )]
		public unsafe static extern void FreeOutString( IntPtr pointer );

		public static string GetOutString( IntPtr pointer )
		{
			if( pointer != IntPtr.Zero )
			{
				string result = Marshal.PtrToStringUni( pointer );
				FreeOutString( pointer );
				return result;
			}
			else
				return null;
		}

		[DllImport( Wrapper.library, EntryPoint = "DSound_GetStructureSizes", CallingConvention = Wrapper.convention )]
		public unsafe static extern void GetStructureSizes( out DSoundStructureSizes sizes );

		[DllImport( Wrapper.library, EntryPoint = "DSound_CoInitialize", CallingConvention = Wrapper.convention )]
		public unsafe static extern int CoInitialize( void* pvReserved );

		[DllImport( Wrapper.library, EntryPoint = "DSound_DirectSoundCreate8", CallingConvention = Wrapper.convention )]
		public unsafe static extern int DirectSoundCreate8( void*/*GUID*/ pcGuidDevice,
			out void*/*IDirectSound8*/ ppDS8, void* pUnkOuter );

		[DllImport( Wrapper.library, EntryPoint = "DSound_Get_DSERR_BUFFERLOST", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Get_DSERR_BUFFERLOST();

		[DllImport( Wrapper.library, EntryPoint = "DSound_Get_DSERR_NODRIVER", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Get_DSERR_NODRIVER();

		[DllImport( Wrapper.library, EntryPoint = "DSound_DirectSoundCaptureCreate", CallingConvention = Wrapper.convention )]
		public unsafe static extern int DirectSoundCaptureCreate( void*/*GUID*/ pcGuidDevice,
			out void*/*IDirectSoundCapture8*/ ppDSC, void* pUnkOuter );

		[DllImport( Wrapper.library, EntryPoint = "DSound_DirectSoundCaptureEnumerateW", CallingConvention = Wrapper.convention )]
		public unsafe static extern int DirectSoundCaptureEnumerateW(
			DSENUMCALLBACKW pDSEnumCallback, void* pContext );

		[DllImport( Wrapper.library, EntryPoint = "DSound_DXGetErrorStringW", CallingConvention = Wrapper.convention )]
		//[return: MarshalAs( UnmanagedType.LPWStr )]
		public unsafe static extern IntPtr/*string*/ DXGetErrorStringW( int hr );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct IDirectSound8
	{
		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound8_Release", CallingConvention = Wrapper.convention )]
		public unsafe static extern uint Release( void*/*IDirectSound8*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound8_CreateSoundBuffer", CallingConvention = Wrapper.convention )]
		public unsafe static extern int CreateSoundBuffer( void*/*IDirectSound8*/ _this,
			ref DSBUFFERDESC pcDSBufferDesc,
			out void*/*IDirectSoundBuffer*/ ppDSBuffer, void* pUnkOuter );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound8_DuplicateSoundBuffer", CallingConvention = Wrapper.convention )]
		public unsafe static extern int DuplicateSoundBuffer( void*/*IDirectSound8*/ _this,
			void*/*IDirectSoundBuffer*/ pDSBufferOriginal,
			out void*/*IDirectSoundBuffer*/ ppDSBufferDuplicate );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound8_SetCooperativeLevel", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetCooperativeLevel( void*/*IDirectSound8*/ _this,
			IntPtr hWnd, uint dwLevel );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct IDirectSound3DListener
	{
		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DListener_Release", CallingConvention = Wrapper.convention )]
		public unsafe static extern uint Release( void*/*IDirectSound3DListener*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DListener_GetAllParameters", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetAllParameters( void*/*IDirectSound3DListener*/ _this,
			ref DS3DLISTENER pListener );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DListener_SetAllParameters", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetAllParameters( void*/*IDirectSound3DListener*/ _this,
			ref DS3DLISTENER pListener, uint dwApply );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct IDirectSoundBuffer
	{
		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_QueryInterface", CallingConvention = Wrapper.convention )]
		public unsafe static extern int QueryInterface( void*/*IDirectSoundBuffer*/ _this,
			ref GUID pGuid, void** pInterface );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_Release", CallingConvention = Wrapper.convention )]
		public unsafe static extern uint Release( void*/*IDirectSoundBuffer*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_GetCaps", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetCaps( void*/*IDirectSoundBuffer*/ _this,
			ref DSBCAPS pDSCaps );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_GetStatus", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetStatus( void*/*IDirectSoundBuffer*/ _this,
			out uint pdwStatus );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_Lock", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Lock( void*/*IDirectSoundBuffer*/ _this,
			uint dwOffset, uint dwBytes, void** ppvAudioPtr1, uint* pdwAudioBytes1,
			void** ppvAudioPtr2, uint* pdwAudioBytes2, uint dwFlags );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_Unlock", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Unlock( void*/*IDirectSoundBuffer*/ _this, void* pvAudioPtr1,
			uint dwAudioBytes1, void* pvAudioPtr2, uint dwAudioBytes2 );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_Play", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Play( void*/*IDirectSoundBuffer*/ _this, uint dwReserved1,
			uint dwPriority, uint dwFlags );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_GetCurrentPosition", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetCurrentPosition( void*/*IDirectSoundBuffer*/ _this,
			uint* pdwCurrentPlayCursor, uint* pdwCurrentWriteCursor );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_SetCurrentPosition", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetCurrentPosition( void*/*IDirectSoundBuffer*/ _this,
			uint dwNewPosition );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_SetFormat", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetFormat( void*/*IDirectSoundBuffer*/ _this,
			ref WAVEFORMATEX pcfxFormat );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_GetVolume", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetVolume( void*/*IDirectSoundBuffer*/ _this,
			out int plVolume );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_SetVolume", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetVolume( void*/*IDirectSoundBuffer*/ _this, int lVolume );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_SetPan", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetPan( void*/*IDirectSoundBuffer*/ _this, int lPan );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_SetFrequency", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetFrequency( void*/*IDirectSoundBuffer*/ _this,
			uint dwFrequency );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_Stop", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Stop( void*/*IDirectSoundBuffer*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundBuffer_Restore", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Restore( void*/*IDirectSoundBuffer*/ _this );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct IDirectSound3DBuffer8
	{
		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DBuffer8_Release", CallingConvention = Wrapper.convention )]
		public unsafe static extern uint Release( void*/*IDirectSound3DBuffer8*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DBuffer8_SetPosition", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetPosition( void*/*IDirectSound3DBuffer8*/ _this,
			float x, float y, float z, uint dwApply );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DBuffer8_SetVelocity", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetVelocity( void*/*IDirectSound3DBuffer8*/ _this,
			float x, float y, float z, uint dwApply );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSound3DBuffer8_SetMinDistance", CallingConvention = Wrapper.convention )]
		public unsafe static extern int SetMinDistance( void*/*IDirectSound3DBuffer8*/ _this,
			float flMinDistance, uint dwApply );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct IDirectSoundCapture8
	{
		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCapture8_Release", CallingConvention = Wrapper.convention )]
		public unsafe static extern uint Release( void*/*IDirectSoundCapture8*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCapture8_CreateCaptureBuffer", CallingConvention = Wrapper.convention )]
		public unsafe static extern int CreateCaptureBuffer( void*/*IDirectSoundCapture8*/ _this,
			ref DSCBUFFERDESC pcDSCBufferDesc,
			out void*/*IDirectSoundCaptureBuffer*/ ppDSCBuffer,
			void* pUnkOuter );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	struct IDirectSoundCaptureBuffer
	{
		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_Release", CallingConvention = Wrapper.convention )]
		public unsafe static extern uint Release( void*/*IDirectSoundCaptureBuffer*/ _this );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_GetCaps", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetCaps( void*/*IDirectSoundCaptureBuffer*/ _this,
			ref DSCBCAPS pDSCBCaps );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_GetCurrentPosition", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetCurrentPosition( void*/*IDirectSoundCaptureBuffer*/ _this,
			uint* pdwCapturePosition, uint* pdwReadPosition );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_Lock", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Lock( void*/*IDirectSoundCaptureBuffer*/ _this,
			uint dwOffset, uint dwBytes,
			void** ppvAudioPtr1, uint* pdwAudioBytes1,
			void** ppvAudioPtr2, uint* pdwAudioBytes2, uint dwFlags );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_Unlock", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Unlock( void*/*IDirectSoundCaptureBuffer*/ _this,
			void* pvAudioPtr1, uint dwAudioBytes1,
			void* pvAudioPtr2, uint dwAudioBytes2 );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_Start", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Start( void*/*IDirectSoundCaptureBuffer*/ _this, 
			uint dwFlags );

		[DllImport( Wrapper.library, EntryPoint = "NIDirectSoundCaptureBuffer_Stop", CallingConvention = Wrapper.convention )]
		public unsafe static extern int Stop( void*/*IDirectSoundCaptureBuffer*/ _this );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

}
