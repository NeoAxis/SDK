// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Engine;
using Engine.FileSystem;
using Engine.Utils;
using Engine.SoundSystem;
using OggVorbisTheora;

namespace DirectSoundSystem
{
	class DirectSound : Sound
	{
		public unsafe WAVEFORMATEX* waveFormat;

		//Value: IDirectSoundBuffer*
		public List<IntPtr> soundBuffers = new List<IntPtr>();
		public int bufferSize;
		Stack<IntPtr> freeSoundBuffers = new Stack<IntPtr>();

		unsafe IDirectSoundBuffer* CreateBuffer( int needBufferSize )
		{
			uint creationFlags = 0;

			if( (int)( Mode & SoundMode.Mode3D ) != 0 )
				creationFlags |= DSound.DSBCAPS_CTRL3D;
			else
				creationFlags |= DSound.DSBCAPS_CTRLPAN;

			if( (int)( Mode & SoundMode.Software ) != 0 )
				creationFlags |= DSound.DSBCAPS_LOCSOFTWARE;

			creationFlags |= DSound.DSBCAPS_CTRLFREQUENCY;
			creationFlags |= DSound.DSBCAPS_CTRLVOLUME;
			creationFlags |= DSound.DSBCAPS_GETCURRENTPOSITION2;

			if( ( creationFlags & DSound.DSBCAPS_CTRLFX ) != 0 )
			{
				//нельзя DuplicateSoundBuffer делать для DSBCAPS_CTRLFX
				//не забыть патом данные заливать во все буферы
				Log.Fatal( "(creationFlags & DSBCAPS_CTRLFX) != 0." );
			}

			int hr;
			void*/*IDirectSoundBuffer*/ soundBuffer;

			if( soundBuffers.Count == 0 )
			{
				DSBUFFERDESC bufferDesc = new DSBUFFERDESC();
				//ZeroMemory( &bufferDesc, sizeof( DSBUFFERDESC ) );
				bufferDesc.dwSize = (uint)sizeof( DSBUFFERDESC );
				bufferDesc.dwFlags = creationFlags;
				bufferDesc.dwBufferBytes = (uint)needBufferSize;
				bufferDesc.guid3DAlgorithm = DSound.DS3DALG_DEFAULT;
				bufferDesc.lpwfxFormat = waveFormat;

				hr = IDirectSound8.CreateSoundBuffer( DirectSoundWorld.Instance.directSound,
					ref bufferDesc, out soundBuffer, null );
				//hr = DirectSoundWorld.Instance.directSound->CreateSoundBuffer(
				//   &bufferDesc, &soundBuffer, NULL );

				if( Wrapper.FAILED( hr ) )
				{
					DirectSoundWorld.Warning( "CreateSoundBuffer", hr );
					return null;
				}

				//get bufferSize
				DSBCAPS bufferCaps = new DSBCAPS();
				//ZeroMemory( &bufferCaps, sizeof( DSBCAPS ) );
				bufferCaps.dwSize = (uint)sizeof( DSBCAPS );
				IDirectSoundBuffer.GetCaps( soundBuffer, ref bufferCaps );
				bufferSize = (int)bufferCaps.dwBufferBytes;
			}
			else
			{
				hr = IDirectSound8.DuplicateSoundBuffer( DirectSoundWorld.Instance.directSound,
					(IDirectSoundBuffer*)soundBuffers[ 0 ].ToPointer(), out soundBuffer );
				if( Wrapper.FAILED( hr ) )
				{
					DirectSoundWorld.Warning( "DuplicateSoundBuffer", hr );
					return null;
				}
			}

			return (IDirectSoundBuffer*)soundBuffer;
		}

		public unsafe IDirectSoundBuffer* GetBuffer( int needBufferSize )
		{
			if( freeSoundBuffers.Count == 0 )
			{
				IDirectSoundBuffer* soundBuffer = CreateBuffer( needBufferSize );
				if( soundBuffer == null )
					return null;

				soundBuffers.Add( (IntPtr)soundBuffer );
				freeSoundBuffers.Push( (IntPtr)soundBuffer );
			}

			if( soundBuffers.Count >= 256 )
				Log.Fatal( "DirectSound.GetBuffer: soundBuffers.Count >= 256." );

			return (IDirectSoundBuffer*)freeSoundBuffers.Pop().ToPointer();
		}

		public unsafe void FreeBuffer( IDirectSoundBuffer* soundBuffer )
		{
			freeSoundBuffers.Push( (IntPtr)soundBuffer );
		}

		public unsafe bool RestoreSoundBuffers( out bool restored )
		{
			IDirectSoundBuffer* soundBuffer = (IDirectSoundBuffer*)soundBuffers[ 0 ].ToPointer();

			int hr;

			restored = false;

			uint status;
			hr = IDirectSoundBuffer.GetStatus( soundBuffer, out status );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "IDirectSoundBuffer.GetStatus", hr );
				return false;
			}

			if( ( status & DSound.DSBSTATUS_BUFFERLOST ) != 0 )
			{
				int DSERR_BUFFERLOST = DSound.Get_DSERR_BUFFERLOST();

				// Since the app could have just been activated, then
				// DirectSound may not be giving us control yet, so
				// the restoring the buffer may fail.
				// If it does, sleep until DirectSound gives us control.
				do
				{
					hr = IDirectSoundBuffer.Restore( soundBuffer );
					if( hr == DSERR_BUFFERLOST )
						Thread.Sleep( 10 );
				}
				while( ( hr = IDirectSoundBuffer.Restore( soundBuffer ) ) == DSERR_BUFFERLOST );

				restored = true;
			}

			return true;
		}

		unsafe protected override void OnDispose()
		{
			DirectSoundWorld.criticalSection.Enter();

			if( soundBuffers.Count != freeSoundBuffers.Count )
				Log.Fatal( "DirectSound.OnDispose: soundBuffers.Count == freeSoundBuffers.Count" );

			for( int n = soundBuffers.Count - 1; n >= 0; n-- )
			{
				IDirectSoundBuffer* soundBuffer = (IDirectSoundBuffer*)soundBuffers[ n ].ToPointer();
				IDirectSoundBuffer.Release( soundBuffer );
			}
			soundBuffers.Clear();
			freeSoundBuffers.Clear();

			if( waveFormat != null )
			{
				NativeUtils.Free( (IntPtr)waveFormat );
				waveFormat = null;
			}

			DirectSoundWorld.criticalSection.Leave();

			base.OnDispose();
		}

		protected static SoundType GetSoundTypeByName( string name )
		{
			//get soundType from name
			string extension = Path.GetExtension( name );

			if( string.Compare( extension, ".ogg", true ) == 0 )
				return SoundType.OGG;
			else if( string.Compare( extension, ".wav", true ) == 0 )
				return SoundType.WAV;
			return SoundType.Unknown;
		}

		protected static SoundType GetSoundTypeByStream( VirtualFileStream stream )
		{
			byte[] buffer = new byte[ 4 ];

			if( stream.Read( buffer, 0, 4 ) != 4 )
				return SoundType.Unknown;

			stream.Seek( -4, SeekOrigin.Current );

			if( ( buffer[ 0 ] == 'o' || buffer[ 0 ] == 'O' ) &&
				( buffer[ 1 ] == 'g' || buffer[ 1 ] == 'g' ) &&
				( buffer[ 2 ] == 'g' || buffer[ 2 ] == 'g' ) )
			{
				return SoundType.OGG;
			}

			if( ( buffer[ 0 ] == 'r' || buffer[ 0 ] == 'R' ) &&
				( buffer[ 1 ] == 'i' || buffer[ 1 ] == 'I' ) &&
				( buffer[ 2 ] == 'f' || buffer[ 2 ] == 'F' ) &&
				( buffer[ 3 ] == 'f' || buffer[ 3 ] == 'F' ) )
			{
				return SoundType.WAV;
			}

			return SoundType.Unknown;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class DirectSampleSound : DirectSound
	{
		public byte[] soundSamples;

		//

		bool LoadSamplesFromStream( VirtualFileStream stream, SoundType soundType,
			out int channels, out int frequency, out float timeLength, out string error )
		{
			channels = 0;
			frequency = 0;
			timeLength = 0;
			error = null;

			switch( soundType )
			{
			case SoundType.OGG:
				{
					VorbisFileReader vorbisFileReader = new VorbisFileReader( stream, false );
					VorbisFile.File vorbisFile = new VorbisFile.File();

					if( !vorbisFileReader.OpenVorbisFile( vorbisFile ) )
					{
						vorbisFile.Dispose();
						vorbisFileReader.Dispose();

						error = "Reading failed";
						return false;
					}

					int numSamples = (int)vorbisFile.pcm_total( -1 );

					vorbisFile.get_info( -1, out channels, out frequency );
					timeLength = (float)vorbisFile.time_total( -1 );

					int size = numSamples * channels;
					int sizeInBytes = size * 2;
					soundSamples = new byte[ sizeInBytes ];

					unsafe
					{
						fixed( byte* pSoundSamples = soundSamples )
						{
							int samplePos = 0;
							while( samplePos < sizeInBytes )
							{
								int readBytes = vorbisFile.read( (IntPtr)( pSoundSamples + samplePos ),
									sizeInBytes - samplePos, 0, 2, 1, IntPtr.Zero );

								if( readBytes <= 0 )
									break;

								samplePos += readBytes;
							}
						}
					}

					vorbisFile.Dispose();
					vorbisFileReader.Dispose();
				}
				return true;

			case SoundType.WAV:
				{
					int sizeInBytes;
					if( !WavLoader.Load( stream, out channels, out frequency, out soundSamples,
						out sizeInBytes, out error ) )
					{
						return false;
					}
					timeLength = (float)( soundSamples.Length / channels / 2 ) / (float)frequency;
				}
				return true;

			}

			error = "Unknown file type";
			return false;
		}

		unsafe public DirectSampleSound( VirtualFileStream stream,
			SoundType soundType, string name, SoundMode mode, out bool initialized )
		{
			initialized = false;

			int channels;
			int frequency;
			float timeLength;

			if( soundType == SoundType.Unknown )
			{
				if( name != null )
					soundType = GetSoundTypeByName( name );
				else
					soundType = GetSoundTypeByStream( stream );
			}

			string error;
			if( !LoadSamplesFromStream( stream, soundType, out channels, out frequency,
				out timeLength, out error ) )
			{
				if( name != null )
				{
					DirectSoundWorld.Warning( string.Format( "Creating sound \"{0}\" failed ({1}).",
						name, error ) );
				}
				else
				{
					DirectSoundWorld.Warning( string.Format( "Creating sound from stream failed ({0}).",
						error ) );
				}
				return;
			}

			//convert to mono for 3D
			if( (int)( mode & SoundMode.Mode3D ) != 0 && channels == 2 )
			{
				byte[] oldSamples = soundSamples;
				soundSamples = new byte[ oldSamples.Length / 2 ];
				for( int n = 0; n < soundSamples.Length; n += 2 )
				{
					soundSamples[ n + 0 ] = oldSamples[ n * 2 + 0 ];
					soundSamples[ n + 1 ] = oldSamples[ n * 2 + 1 ];
				}
				channels = 1;
			}

			//create buffer
			waveFormat = (WAVEFORMATEX*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo,
				sizeof( WAVEFORMATEX ) );
			NativeUtils.ZeroMemory( (IntPtr)waveFormat, sizeof( WAVEFORMATEX ) );
			waveFormat->wFormatTag = DSound.WAVE_FORMAT_PCM;
			waveFormat->nChannels = (ushort)channels;
			waveFormat->nSamplesPerSec = (uint)frequency;
			waveFormat->wBitsPerSample = 16;
			waveFormat->nBlockAlign = (ushort)( ( waveFormat->nChannels * waveFormat->wBitsPerSample ) / 8 );
			waveFormat->nAvgBytesPerSec = waveFormat->nSamplesPerSec * waveFormat->nBlockAlign;

			Init( name, mode, timeLength, channels, frequency );
			initialized = true;
		}

		unsafe public bool FillSoundBuffersWithData()
		{
			IDirectSoundBuffer* soundBuffer = (IDirectSoundBuffer*)soundBuffers[ 0 ].ToPointer();

			int hr;

			void* lockedBuffer = null;
			uint lockedBufferSize = 0;

			hr = IDirectSoundBuffer.Lock( soundBuffer, 0, (uint)soundSamples.Length,
				&lockedBuffer, &lockedBufferSize, (void**)null, (uint*)null, 0 );

			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "IDirectSoundBuffer.Lock", hr );
				return false;
			}

			if( (int)lockedBufferSize < soundSamples.Length )
			{
				Log.Fatal( "DirectSampleSound.FillSoundBuffersWithData: " +
					"lockedBufferSize >= soundSamples->Length" );
			}

			Marshal.Copy( soundSamples, 0, (IntPtr)lockedBuffer, soundSamples.Length );

			if( soundSamples.Length < (int)lockedBufferSize )
			{
				// fill with silence remaining bytes
				NativeUtils.FillMemory( (IntPtr)( (byte*)lockedBuffer + soundSamples.Length ),
					(int)lockedBufferSize - soundSamples.Length,
					(byte)( waveFormat->wBitsPerSample == 8 ? 128 : 0 ) );
			}

			IDirectSoundBuffer.Unlock( soundBuffer, lockedBuffer, lockedBufferSize, (void*)null, 0 );

			return true;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class DirectDataBufferSound : DirectSound
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class DirectFileStreamSound : DirectDataBufferSound
	{
		public VorbisFile.File vorbisFile;
		VorbisFileReader vorbisFileReader;
		public bool needConvertToMono;

		//

		unsafe public DirectFileStreamSound( VirtualFileStream stream, bool closeStreamAfterReading,
			SoundType soundType, string name, SoundMode mode, out bool initialized )
		{
			initialized = false;

			if( soundType == SoundType.Unknown )
			{
				if( name != null )
					soundType = GetSoundTypeByName( name );
				else
					soundType = GetSoundTypeByStream( stream );
			}

			if( soundType != SoundType.OGG )
			{
				DirectSoundWorld.Warning( string.Format(
					"Streaming is not supported for \"{0}\" files ({1}).", soundType, name ) );
				return;
			}

			vorbisFile = new VorbisFile.File();

			vorbisFileReader = new VorbisFileReader( stream, closeStreamAfterReading );

			if( !vorbisFileReader.OpenVorbisFile( vorbisFile ) )
			{
				vorbisFileReader.Dispose();
				DirectSoundWorld.Warning( string.Format( "Creating sound \"{0}\" failed.", name ) );
				return;
			}

			int channels;
			int frequency;

			long numSamples = vorbisFile.pcm_total( -1 );
			vorbisFile.get_info( -1, out channels, out frequency );

			//convert to mono for 3D
			if( (int)( mode & SoundMode.Mode3D ) != 0 && channels == 2 )
			{
				needConvertToMono = true;
				channels = 1;
			}

			waveFormat = (WAVEFORMATEX*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo,
				sizeof( WAVEFORMATEX ) );
			NativeUtils.ZeroMemory( (IntPtr)waveFormat, sizeof( WAVEFORMATEX ) );
			waveFormat->wFormatTag = DSound.WAVE_FORMAT_PCM;
			waveFormat->nChannels = (ushort)channels;
			waveFormat->nSamplesPerSec = (uint)frequency;
			waveFormat->wBitsPerSample = 16;
			waveFormat->nBlockAlign = (ushort)( ( waveFormat->nChannels * waveFormat->wBitsPerSample ) / 8 );
			waveFormat->nAvgBytesPerSec = waveFormat->nSamplesPerSec * waveFormat->nBlockAlign;

			double length = (double)numSamples / (double)frequency;

			Init( name, mode, (float)length, channels, frequency );
			initialized = true;
		}

		public void Rewind()
		{
			if( vorbisFile != null )
			{
				vorbisFile.Dispose();
				vorbisFile = null;
			}

			vorbisFileReader.RewindStreamToBegin();

			vorbisFile = new VorbisFile.File();
			if( !vorbisFileReader.OpenVorbisFile( vorbisFile ) )
			{
				DirectSoundWorld.Warning( string.Format( "Creating sound failed \"{0}\".", Name ) );
				return;
			}
		}

		protected override void OnDispose()
		{
			DirectSoundWorld.criticalSection.Enter();

			if( vorbisFile != null )
			{
				vorbisFile.Dispose();
				vorbisFile = null;
			}
			if( vorbisFileReader != null )
			{
				vorbisFileReader.Dispose();
				vorbisFileReader = null;
			}

			DirectSoundWorld.criticalSection.Leave();

			base.OnDispose();
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class DirectDataStreamSound : DirectDataBufferSound
	{
		public SoundWorld.DataReadDelegate dataReadCallback;
		public int creationBufferSize;

		unsafe public DirectDataStreamSound( SoundMode mode, int channels, int frequency, int bufferSize,
			SoundWorld.DataReadDelegate dataReadCallback )
		{
			this.dataReadCallback = dataReadCallback;
			this.creationBufferSize = bufferSize;

			waveFormat = (WAVEFORMATEX*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo,
				sizeof( WAVEFORMATEX ) );
			NativeUtils.ZeroMemory( (IntPtr)waveFormat, sizeof( WAVEFORMATEX ) );
			waveFormat->wFormatTag = DSound.WAVE_FORMAT_PCM;
			waveFormat->nChannels = (ushort)channels;
			waveFormat->nSamplesPerSec = (uint)frequency;
			waveFormat->wBitsPerSample = 16;
			waveFormat->nBlockAlign = (ushort)( ( waveFormat->nChannels * waveFormat->wBitsPerSample ) / 8 );
			waveFormat->nAvgBytesPerSec = waveFormat->nSamplesPerSec * waveFormat->nBlockAlign;

			Init( null, mode, 100000.0f, channels, frequency );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class DirectCaptureSound : DirectSound
	{
		public unsafe IDirectSoundCapture8* soundCapture;
		public unsafe IDirectSoundCaptureBuffer* captureBuffer;
		public int readPosition;

		unsafe public DirectCaptureSound( SoundMode mode, int channels, int frequency, int bufferSize )
		{
			SoundMode newMode = mode | SoundMode.Loop | SoundMode.Software;

			int hr;

			if( DirectSoundWorld.Instance.recordDriverIndex == -1 )
			{
				DirectSoundWorld.Warning( "Recording failed. No active device." );
				return;
			}
			GUID deviceGuid = DirectSoundWorld.Instance.recordDriverGuids[
				DirectSoundWorld.Instance.recordDriverIndex ];

			//soundCapture
			void*/*IDirectSoundCapture8*/ tempSoundCapture;
			hr = DSound.DirectSoundCaptureCreate( &deviceGuid, out tempSoundCapture, null );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "DirectSoundCaptureCreate", hr );
				return;
			}
			soundCapture = (IDirectSoundCapture8*)tempSoundCapture;

			//waveFormat
			waveFormat = (WAVEFORMATEX*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo,
				sizeof( WAVEFORMATEX ) );
			NativeUtils.ZeroMemory( (IntPtr)waveFormat, sizeof( WAVEFORMATEX ) );
			waveFormat->wFormatTag = DSound.WAVE_FORMAT_PCM;
			waveFormat->nChannels = (ushort)channels;
			waveFormat->nSamplesPerSec = (uint)frequency;
			waveFormat->wBitsPerSample = 16;
			waveFormat->nBlockAlign = (ushort)( ( waveFormat->nChannels * waveFormat->wBitsPerSample ) / 8 );
			waveFormat->nAvgBytesPerSec = waveFormat->nSamplesPerSec * waveFormat->nBlockAlign;

			//captureBuffer

			DSCBUFFERDESC bufferDesc = new DSCBUFFERDESC();
			//ZeroMemory( &bufferDesc, sizeof( DSCBUFFERDESC ) );
			bufferDesc.dwSize = (uint)sizeof( DSCBUFFERDESC );
			bufferDesc.dwBufferBytes = (uint)bufferSize;
			bufferDesc.lpwfxFormat = waveFormat;

			void*/*IDirectSoundCaptureBuffer*/ tempCaptureBuffer;

			hr = IDirectSoundCapture8.CreateCaptureBuffer( soundCapture,
				ref bufferDesc, out tempCaptureBuffer, null );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "CreateCaptureBuffer", hr );
				IDirectSoundCapture8.Release( soundCapture );
				soundCapture = null;
				return;
			}
			captureBuffer = (IDirectSoundCaptureBuffer*)tempCaptureBuffer;

			//get bufferSize
			DSCBCAPS bufferCaps = new DSCBCAPS();
			//ZeroMemory( &bufferCaps, sizeof( DSCBCAPS ) );
			bufferCaps.dwSize = (uint)sizeof( DSCBCAPS );
			IDirectSoundCaptureBuffer.GetCaps( captureBuffer, ref bufferCaps );
			this.bufferSize = (int)bufferCaps.dwBufferBytes;

			Init( null, newMode, 100000.0f, channels, frequency );
		}

		unsafe protected override void OnDispose()
		{
			DirectSoundWorld.criticalSection.Enter();

			if( captureBuffer != null )
			{
				IDirectSoundCaptureBuffer.Release( captureBuffer );
				captureBuffer = null;
			}

			if( soundCapture != null )
			{
				IDirectSoundCapture8.Release( soundCapture );
				soundCapture = null;
			}

			DirectSoundWorld.criticalSection.Leave();

			base.OnDispose();
		}

		unsafe public override int RecordRead( byte[] buffer, int length )
		{
			DirectSoundWorld.criticalSection.Enter();

			int hr;

			uint dwBufferPosition;
			hr = IDirectSoundCaptureBuffer.GetCurrentPosition( captureBuffer,
				(uint*)null, &dwBufferPosition );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.criticalSection.Leave();
				return 0;
			}
			int bufferPosition = (int)dwBufferPosition;

			int bytesAvailable;
			if( bufferPosition >= readPosition )
				bytesAvailable = bufferPosition - readPosition;
			else
				bytesAvailable = ( bufferSize - readPosition ) + bufferPosition;

			int needLength = Math.Min( length, bytesAvailable );

			if( needLength == 0 )
			{
				DirectSoundWorld.criticalSection.Leave();
				return 0;
			}

			void* lockedBuffer = null;
			uint lockedBufferSize = 0;
			void* lockedBuffer2 = null;
			uint lockedBufferSize2 = 0;

			int startPosition = readPosition - needLength;
			if( startPosition < 0 )
				startPosition += bufferSize;

			hr = IDirectSoundCaptureBuffer.Lock( captureBuffer, (uint)startPosition, (uint)needLength,
				&lockedBuffer, &lockedBufferSize, &lockedBuffer2, &lockedBufferSize2, 0 );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.criticalSection.Leave();
				DirectSoundWorld.Warning( "IDirectSoundCaptureBuffer.Lock", hr );
				return 0;
			}

			if( lockedBuffer != null && lockedBufferSize != 0 )
				Marshal.Copy( (IntPtr)lockedBuffer, buffer, 0, (int)lockedBufferSize );
			if( lockedBuffer2 != null && lockedBufferSize2 != 0 )
			{
				Marshal.Copy( (IntPtr)lockedBuffer2, buffer,
					(int)lockedBufferSize, (int)lockedBufferSize2 );
			}

			IDirectSoundCaptureBuffer.Unlock( captureBuffer, lockedBuffer, lockedBufferSize,
				lockedBuffer2, lockedBufferSize2 );

			readPosition += needLength;
			if( readPosition >= bufferSize )
				readPosition -= bufferSize;

			DirectSoundWorld.criticalSection.Leave();

			return (int)lockedBufferSize + (int)lockedBufferSize2;
		}
	}
}
