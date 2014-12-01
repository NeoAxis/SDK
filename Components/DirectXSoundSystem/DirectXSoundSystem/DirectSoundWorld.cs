// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using Engine;
using Engine.SoundSystem;
using Engine.FileSystem;
using Engine.MathEx;
using Engine.Utils;

namespace DirectSoundSystem
{
	class DirectSoundWorld : SoundWorld
	{
		static DirectSoundWorld instance;

		public unsafe IDirectSound8* directSound;
		unsafe IDirectSound3DListener* listener;

		public List<DirectSoundRealChannel> realChannels;
		public List<DirectSoundRealChannel> fileStreamRealChannels;

		Thread thread;
		volatile bool needAbortThread;

		IntPtr/*HWND*/ hWnd;

		public int recordDriverIndex;
		public List<GUID> recordDriverGuids = new List<GUID>();
		List<string> recordDriverNames = new List<string>();

		DirectCaptureSound recordingSound;

		public static CriticalSection criticalSection;

		//

		public new static DirectSoundWorld Instance
		{
			get { return instance; }
		}

		public static void Warning( string text )
		{
			Log.Warning( "DirectXSoundSystem: " + text );
		}

		public static void Error( string text )
		{
			Log.Error( "DirectXSoundSystem: " + text );
		}

		public static void Warning( string methodName, int/*HRESULT*/ result )
		{
			string error = DSound.GetOutString( DSound.DXGetErrorStringW( result ) );
			Warning( string.Format( "{0} ({1})", error, methodName ) );
		}

		public static void Error( string methodName, int/*HRESULT*/ result )
		{
			string error = DSound.GetOutString( DSound.DXGetErrorStringW( result ) );
			Error( string.Format( "{0} ({1})", error, methodName ) );
		}

		unsafe int/*HRESULT*/ SetPrimaryBufferFormat( int primaryChannels, int primaryFrequency,
			int primaryBitRate, bool allowLogError )
		{
			int hr;
			void*/*IDirectSoundBuffer*/ primaryBuffer = null;

			// Get the primary buffer
			DSBUFFERDESC bufferDesc = new DSBUFFERDESC();
			//ZeroMemory( &bufferDesc, sizeof( DSBUFFERDESC ) );
			bufferDesc.dwSize = (uint)sizeof( DSBUFFERDESC );
			bufferDesc.dwFlags = /*DSound.DSBCAPS_CTRL3D | */DSound.DSBCAPS_PRIMARYBUFFER;
			//bufferDesc.dwBufferBytes = 0;
			//bufferDesc.lpwfxFormat = NULL;

			hr = IDirectSound8.CreateSoundBuffer( directSound, ref bufferDesc,
				out primaryBuffer, null );
			if( Wrapper.FAILED( hr ) )
			{
				if( allowLogError )
					Error( "CreateSoundBuffer", hr );
				return hr;
			}

			WAVEFORMATEX waveFormat = new WAVEFORMATEX();
			//ZeroMemory( &waveFormat, sizeof( WAVEFORMATEX ) );
			waveFormat.wFormatTag = (ushort)DSound.WAVE_FORMAT_PCM;
			waveFormat.nChannels = (ushort)primaryChannels;
			waveFormat.nSamplesPerSec = (uint)primaryFrequency;
			waveFormat.wBitsPerSample = (ushort)primaryBitRate;
			waveFormat.nBlockAlign = (ushort)( waveFormat.wBitsPerSample / 8 * waveFormat.nChannels );
			waveFormat.nAvgBytesPerSec = (uint)( waveFormat.nSamplesPerSec * waveFormat.nBlockAlign );

			hr = IDirectSoundBuffer.SetFormat( primaryBuffer, ref waveFormat );
			if( Wrapper.FAILED( hr ) )
			{
				IDirectSoundBuffer.Release( primaryBuffer );
				if( allowLogError )
					Error( "SetFormat", hr );
				return hr;
			}

			IDirectSoundBuffer.Release( primaryBuffer );

			return DSound.S_OK;
		}

		unsafe protected override bool InitLibrary( IntPtr mainWindowHandle,
			int maxReal2DChannels, int maxReal3DChannels )
		{
			NativeLibraryManager.PreLoadLibrary( "libogg" );
			NativeLibraryManager.PreLoadLibrary( "libvorbis" );
			NativeLibraryManager.PreLoadLibrary( "libvorbisfile" );
			NativeLibraryManager.PreLoadLibrary( "DirectSoundNativeWrapper" );

			{
				DSoundStructureSizes sizes = new DSoundStructureSizes();
				sizes.Init();

				DSoundStructureSizes originalSizes;
				DSound.GetStructureSizes( out originalSizes );

				FieldInfo[] fields = sizes.GetType().GetFields(
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );

				foreach( FieldInfo field in fields )
				{
					int originalSize = (int)field.GetValue( originalSizes );
					int size = (int)field.GetValue( sizes );

					if( originalSize != size )
					{
						Log.Fatal( "DirectXSoundSystem: Invalid unmanaged bridge. " +
							"Invalid \"{0}\". Native size = \"{1}\". Managed size = \"{2}\".", field.Name,
							originalSize, size );
						return false;
					}
				}
			}

			instance = this;

			criticalSection = CriticalSection.Create();

			DSound.CoInitialize( null );

			int hr;

			//create IDirectSound using the primary sound device
			void*/*IDirectSound8*/ directSoundTemp;
			hr = DSound.DirectSoundCreate8( null, out directSoundTemp, null );
			if( Wrapper.FAILED( hr ) )
			{
				if( hr == DSound.Get_DSERR_NODRIVER() )
				{
					Log.InvisibleInfo( "DirectXSoundSystem: No sound driver." );
					return false;
				}

				Error( "DirectSoundCreate8", hr );
				return false;
			}
			directSound = (IDirectSound8*)directSoundTemp;

			//set DirectSound cooperative level
			hWnd = mainWindowHandle;
			hr = IDirectSound8.SetCooperativeLevel( directSound, hWnd, DSound.DSSCL_PRIORITY );
			if( Wrapper.FAILED( hr ) )
			{
				Error( "SetCooperativeLevel", hr );
				return false;
			}

			//set primary buffer format
			{
				hr = SetPrimaryBufferFormat( 2, 44100, 16, false );
				if( Wrapper.FAILED( hr ) )
					hr = SetPrimaryBufferFormat( 2, 22050, 16, true );
				if( Wrapper.FAILED( hr ) )
					return false;
			}

			//get listener
			{
				void*/*IDirectSoundBuffer*/ primaryBuffer = null;

				// Obtain primary buffer, asking it for 3D control
				DSBUFFERDESC bufferDesc = new DSBUFFERDESC();
				//ZeroMemory( &bufferDesc, sizeof( DSBUFFERDESC ) );
				bufferDesc.dwSize = (uint)sizeof( DSBUFFERDESC );
				bufferDesc.dwFlags = DSound.DSBCAPS_CTRL3D | DSound.DSBCAPS_PRIMARYBUFFER;

				hr = IDirectSound8.CreateSoundBuffer( directSound, ref bufferDesc,
					out primaryBuffer, null );
				if( Wrapper.FAILED( hr ) )
				{
					Error( "CreateSoundBuffer", hr );
					return false;
				}

				void*/*IDirectSound3DListener*/ listenerTemp = null;

				GUID guid = DSound.IID_IDirectSound3DListener;
				if( Wrapper.FAILED( hr = IDirectSoundBuffer.QueryInterface( primaryBuffer,
					ref guid, &listenerTemp ) ) )
				{
					IDirectSoundBuffer.Release( primaryBuffer );
					Error( "QueryInterface", hr );
					return false;
				}
				listener = (IDirectSound3DListener*)listenerTemp;

				IDirectSoundBuffer.Release( primaryBuffer );
			}

			//update general parameters
			{
				DS3DLISTENER parameters = new DS3DLISTENER();
				parameters.dwSize = (uint)sizeof( DS3DLISTENER );
				IDirectSound3DListener.GetAllParameters( listener, ref parameters );
				parameters.flDistanceFactor = 1;
				parameters.flRolloffFactor = 0;
				parameters.flDopplerFactor = DopplerScale;
				hr = IDirectSound3DListener.SetAllParameters( listener, ref parameters, DSound.DS3D_IMMEDIATE );
				if( Wrapper.FAILED( hr ) )
					Warning( "IDirectSound3DListener.SetAllParameters", hr );
			}

			GenerateRecordDriverList();

			//Channels
			realChannels = new List<DirectSoundRealChannel>();
			for( int n = 0; n < maxReal2DChannels; n++ )
			{
				DirectSoundRealChannel realChannel = new DirectSoundRealChannel();
				AddRealChannel( realChannel, false );
				realChannels.Add( realChannel );
			}
			for( int n = 0; n < maxReal3DChannels; n++ )
			{
				DirectSoundRealChannel realChannel = new DirectSoundRealChannel();
				AddRealChannel( realChannel, true );
				realChannels.Add( realChannel );
			}

			fileStreamRealChannels = new List<DirectSoundRealChannel>();

			thread = new Thread( new ThreadStart( ThreadFunction ) );
			thread.CurrentCulture = new System.Globalization.CultureInfo( "en-US" );
			thread.IsBackground = true;
			thread.Start();

			return true;
		}

		unsafe protected override void ShutdownLibrary()
		{
			if( thread != null )
			{
				needAbortThread = true;
				Thread.Sleep( 50 );
				thread.Abort();
			}

			if( realChannels != null )
			{
				realChannels.Clear();
				realChannels = null;
			}

			if( directSound != null )
			{
				IDirectSound8.Release( directSound );
				directSound = null;
			}

			if( criticalSection != null )
			{
				criticalSection.Dispose();
				criticalSection = null;
			}

			instance = null;
		}

		protected override void OnUpdateLibrary()
		{
			if( IsActive() )
			{
				criticalSection.Enter();

				for( int n = 0; n < realChannels.Count; n++ )
					realChannels[ n ].Update();

				criticalSection.Leave();
			}
		}

		public override Sound SoundCreate( string name, SoundMode mode )
		{
			criticalSection.Enter();

			DirectSound sound;

			sound = (DirectSound)base.SoundCreate( name, mode );
			if( sound != null )
			{
				criticalSection.Leave();
				return sound;
			}

			VirtualFileStream stream = CreateFileStream( name );
			if( stream == null )
			{
				criticalSection.Leave();
				DirectSoundWorld.Warning( string.Format( "Creating sound \"{0}\" failed.", name ) );
				return null;
			}

			bool initialized;

			if( (int)( mode & SoundMode.Stream ) == 0 )
			{
				sound = new DirectSampleSound( stream, SoundType.Unknown, name,
					mode, out initialized );
				stream.Close();
			}
			else
			{
				sound = new DirectFileStreamSound( stream, true, SoundType.Unknown,
					name, mode, out initialized );
			}

			if( !initialized )
			{
				sound.Dispose();
				sound = null;
			}

			criticalSection.Leave();

			return sound;
		}

		public override Sound SoundCreate( VirtualFileStream stream, bool closeStreamAfterReading,
			SoundType soundType, SoundMode mode )
		{
			criticalSection.Enter();

			DirectSound sound;
			bool initialized;

			if( (int)( mode & SoundMode.Stream ) == 0 )
			{
				sound = new DirectSampleSound( stream, soundType, null, mode, out initialized );
				if( closeStreamAfterReading )
					stream.Close();
			}
			else
			{
				sound = new DirectFileStreamSound( stream, closeStreamAfterReading, soundType, null,
					mode, out initialized );
			}

			if( !initialized )
			{
				sound.Dispose();
				sound = null;
			}

			criticalSection.Leave();

			return sound;
		}

		unsafe public override Sound SoundCreateDataBuffer( SoundMode mode, int channels,
			int frequency, int bufferSize, DataReadDelegate dataReadCallback )
		{
			criticalSection.Enter();

			Sound sound;

			if( (int)( mode & SoundMode.Record ) != 0 )
			{
				DirectCaptureSound captureSound = new DirectCaptureSound(
					mode, channels, frequency, bufferSize );
				if( captureSound.soundCapture == null )
					captureSound = null;
				sound = captureSound;
			}
			else
			{
				sound = new DirectDataStreamSound( mode, channels, frequency,
					bufferSize, dataReadCallback );
			}

			criticalSection.Leave();

			return sound;
		}

		//void UpdateListe

		unsafe protected override void OnSetDopplerEffectScale( float dopplerScale )
		{
			criticalSection.Enter();

			DS3DLISTENER parameters = new DS3DLISTENER();
			parameters.dwSize = (uint)sizeof( DS3DLISTENER );
			IDirectSound3DListener.GetAllParameters( listener, ref parameters );
			parameters.flDopplerFactor = dopplerScale;
			int hr = IDirectSound3DListener.SetAllParameters( listener, ref parameters, DSound.DS3D_IMMEDIATE );
			if( Wrapper.FAILED( hr ) )
				Warning( "IDirectSound3DListener.SetAllParameters", hr );

			criticalSection.Leave();
		}

		unsafe protected override void OnSetListener( Vec3 position, Vec3 velocity,
			Vec3 forward, Vec3 up )
		{
			criticalSection.Enter();

			DS3DLISTENER parameters = new DS3DLISTENER();
			parameters.dwSize = (uint)sizeof( DS3DLISTENER );
			IDirectSound3DListener.GetAllParameters( listener, ref parameters );

			parameters.vPosition = new Vec3( position.X, position.Z, position.Y );
			parameters.vVelocity = new Vec3( velocity.X, velocity.Z, velocity.Y );
			parameters.vOrientFront = new Vec3( forward.X, forward.Z, forward.Y );
			parameters.vOrientTop = new Vec3( up.X, up.Z, up.Y );

			int hr = IDirectSound3DListener.SetAllParameters( listener, ref parameters,
				DSound.DS3D_IMMEDIATE );
			if( Wrapper.FAILED( hr ) )
				Warning( "IDirectSound3DListener.SetAllParameters", hr );

			criticalSection.Leave();
		}

		public override string DriverName
		{
			get { return "DirectSound8"; }
		}

		public override string[] RecordDrivers
		{
			get { return recordDriverNames.ToArray(); }
		}

		public override int RecordDriver
		{
			get { return recordDriverIndex; }
			set { recordDriverIndex = value; }
		}

		unsafe int SoundCaptureCallback( void*/*GUID*/ pGuid, string description,
			string module, void* pContext )
		{
			if( pGuid != null )
			{
				GUID* guid = (GUID*)pGuid;
				recordDriverGuids.Add( *guid );
				recordDriverNames.Add( description );
			}
			return 1;
		}

		unsafe void GenerateRecordDriverList()
		{
			DSENUMCALLBACKW callback = new DSENUMCALLBACKW( SoundCaptureCallback );
			DSound.DirectSoundCaptureEnumerateW( callback, null );
			recordDriverIndex = recordDriverGuids.Count != 0 ? 0 : -1;
		}

		unsafe public override bool RecordStart( Sound sound )
		{
			criticalSection.Enter();

			DirectCaptureSound captureSound = sound as DirectCaptureSound;
			if( captureSound == null )
			{
				criticalSection.Leave();
				DirectSoundWorld.Warning( "Recording failed. Is sound a not for recording." );
				return false;
			}

			captureSound.readPosition = 0;

			int hr = IDirectSoundCaptureBuffer.Start( captureSound.captureBuffer,
				DSound.DSCBSTART_LOOPING );
			if( Wrapper.FAILED( hr ) )
			{
				criticalSection.Leave();
				DirectSoundWorld.Warning( "IDirectSoundCaptureBuffer.Start", hr );
				return false;
			}

			recordingSound = captureSound;

			criticalSection.Leave();

			return true;
		}

		unsafe public override void RecordStop()
		{
			criticalSection.Enter();

			if( recordingSound != null )
			{
				int hr = IDirectSoundCaptureBuffer.Stop( recordingSound.captureBuffer );
				if( Wrapper.FAILED( hr ) )
					DirectSoundWorld.Warning( "IDirectSoundCaptureBuffer.Stop", hr );
				recordingSound = null;
			}

			criticalSection.Leave();
		}

		public override bool IsRecording()
		{
			return recordingSound != null;
		}

		void ThreadFunction()
		{
			unsafe
			{
				DSound.CoInitialize( null );
			}

			while( true )
			{
				criticalSection.Enter();

				for( int n = 0; n < fileStreamRealChannels.Count; n++ )
					fileStreamRealChannels[ n ].UpdateStream();

				criticalSection.Leave();

				while( !IsActive() )
					Thread.Sleep( 100 );

				Thread.Sleep( 10 );

				if( needAbortThread )
					break;
			}
		}

		[DllImport( "user32.dll" )]
		static extern int IsWindow( IntPtr hWnd );
		[DllImport( "user32.dll" )]
		static extern int IsWindowVisible( IntPtr hWnd );
		[DllImport( "user32.dll" )]
		static extern int IsIconic( IntPtr hWnd );
		[DllImport( "user32.dll" )]
		static extern IntPtr GetParent( IntPtr hWnd );

		bool IsActive()
		{
			ChannelGroup masterGroup = MasterChannelGroup;
			bool masterGroupPaused = masterGroup == null || masterGroup.Pause;

			bool active = !masterGroupPaused;

			if( active && _SuspendWorkingWhenApplicationIsNotActive )
			{
				IntPtr h = hWnd;
				while( h != IntPtr.Zero )
				{
					active = IsWindow( h ) != 0 && IsWindowVisible( h ) != 0 && IsIconic( h ) == 0;
					if( !active )
						break;

					h = GetParent( h );
				}
			}

			return active;
		}

	}
}
