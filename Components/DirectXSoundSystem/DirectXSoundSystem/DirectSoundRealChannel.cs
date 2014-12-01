// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine;
using Engine.FileSystem;
using Engine.SoundSystem;
using Engine.Utils;
using Engine.MathEx;
using OggVorbisTheora;

namespace DirectSoundSystem
{
	class DirectSoundRealChannel : RealChannel
	{
		DirectSound currentSound;

		unsafe IDirectSoundBuffer* currentSoundBuffer;
		unsafe IDirectSound3DBuffer8* currentSound3DBuffer;

		//stream
		unsafe byte* streamBuffer;
		int streamBufferLength;
		bool needStopAfterBufferRead;
		bool streamNeedWriteFirstPart;
		bool needStopVirtualChannel;

		byte[] tempDataStreamReadArray = new byte[ 0 ];

		//

		public void Update()
		{
			if( currentSound == null )
				return;

			if( ( currentSound.Mode & SoundMode.Mode3D ) != 0 )
				UpdateVolume2();

			DirectSampleSound currentSampleSound = currentSound as DirectSampleSound;
			if( currentSampleSound != null )
			{
				UpdateSample();
				return;
			}

			DirectDataStreamSound currentDataStreamSound = currentSound as DirectDataStreamSound;
			if( currentDataStreamSound != null )
				UpdateStream();

			if( needStopVirtualChannel )
			{
				CurrentVirtualChannel.Stop();
				needStopVirtualChannel = false;
			}
		}

		unsafe void UpdateSample()
		{
			uint status;

			int hr = IDirectSoundBuffer.GetStatus( currentSoundBuffer, out status );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "IDirectSoundBuffer.GetStatus", hr );
				return;
			}

			if( ( status & DSound.DSBSTATUS_PLAYING ) == 0 )
				CurrentVirtualChannel.Stop();
		}

		//file,data streams

		unsafe void BeginStreamPlay()
		{
			//clear buffer
			{
				void* lockedBuffer = null;
				uint lockedBufferSize = 0;

				int hr = IDirectSoundBuffer.Lock( currentSoundBuffer, 0, (uint)currentSound.bufferSize,
					&lockedBuffer, &lockedBufferSize, (void**)null, (uint*)null, 0 );
				if( Wrapper.FAILED( hr ) )
				{
					DirectSoundWorld.Warning( "IDirectSoundBuffer.Lock", hr );
					return;
				}

				NativeUtils.FillMemory( (IntPtr)lockedBuffer, (int)lockedBufferSize,
					(byte)( currentSound.waveFormat->wBitsPerSample == 8 ? 128 : 0 ) );

				IDirectSoundBuffer.Unlock( currentSoundBuffer, lockedBuffer, lockedBufferSize, null, 0 );
			}

			UpdateStreamBuffer( true );
			UpdateStreamBuffer( false );
			streamNeedWriteFirstPart = true;
			needStopAfterBufferRead = false;
		}

		unsafe public void UpdateStream()
		{
			if( currentSound == null )
				return;

			uint playPosition;
			uint writePosition;

			int hr = IDirectSoundBuffer.GetCurrentPosition( currentSoundBuffer,
				&playPosition, &writePosition );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "IDirectSoundBuffer.GetCurrentPosition", hr );
				return;
			}

			bool needRead;
			if( streamNeedWriteFirstPart )
			{
				needRead = (int)playPosition >= currentSound.bufferSize / 2 &&
					(int)writePosition >= currentSound.bufferSize / 2;
			}
			else
			{
				needRead = (int)playPosition < currentSound.bufferSize / 2 &&
					(int)writePosition < currentSound.bufferSize / 2;
			}

			if( needRead )
			{
				if( needStopAfterBufferRead )
				{
					needStopVirtualChannel = true;
					//CurrentVirtualChannel.Stop();
					needStopAfterBufferRead = false;
					goto end;
				}

				UpdateStreamBuffer( streamNeedWriteFirstPart );
				streamNeedWriteFirstPart = !streamNeedWriteFirstPart;
			}

			end: ;
		}

		unsafe void UpdateStreamBuffer( bool firstPart )
		{
			DirectFileStreamSound currentFileStreamSound = currentSound as DirectFileStreamSound;

			void* lockedBuffer = null;
			uint lockedBufferSize = 0;

			int hr = IDirectSoundBuffer.Lock( currentSoundBuffer,
				(uint)( firstPart ? 0 : currentSound.bufferSize / 2 ),
				(uint)( currentSound.bufferSize / 2 ), &lockedBuffer, &lockedBufferSize,
				(void**)null, (uint*)null, 0 );
			if( Wrapper.FAILED( hr ) )
			{
				DirectSoundWorld.Warning( "IDirectSoundBuffer.Lock", hr );
				return;
			}

			if( (int)lockedBufferSize < currentSound.bufferSize / 2 )
			{
				Log.Fatal( "DirectSoundRealChannel.UpdateStreamBuffer: " +
					"lockedBufferSize >= currentSound->bufferSize / 2." );
			}

			bool repeated = false;

			int readed = 0;
			again:

			if( currentFileStreamSound != null )
			{
				readed += ReadDataFromFileStream( (IntPtr)( (byte*)lockedBuffer + readed ),
					currentSound.bufferSize / 2 - readed );
			}
			else
			{
				readed += ReadDataFromDataStream( (IntPtr)( (byte*)lockedBuffer + readed ),
					currentSound.bufferSize / 2 - readed );
			}

			if( readed < currentSound.bufferSize / 2 )
			{
				NativeUtils.FillMemory( (IntPtr)( (byte*)lockedBuffer + readed ),
					(int)lockedBufferSize - readed,
					(byte)( currentSound.waveFormat->wBitsPerSample == 8 ? 128 : 0 ) );

				if( (int)( currentSound.Mode & SoundMode.Loop ) != 0 )
				{
					if( currentFileStreamSound != null )
					{
						//loop play. we need recreate vorbis file
						currentFileStreamSound.Rewind();

						if( !repeated )
						{
							repeated = true;
							goto again;
						}
					}
				}
			}

			if( readed == 0 )
			{
				//need stop
				if( (int)( currentSound.Mode & SoundMode.Loop ) == 0 )
				{
					if( currentFileStreamSound != null )
						needStopAfterBufferRead = true;
				}
			}

			IDirectSoundBuffer.Unlock( currentSoundBuffer, lockedBuffer, lockedBufferSize, null, 0 );
		}

		//file stream
		unsafe int ReadDataFromFileStream( IntPtr buffer, int needRead )
		{
			DirectFileStreamSound currentFileStreamSound = (DirectFileStreamSound)currentSound;

			while( streamBufferLength < needRead )
			{
				int readBytes = currentFileStreamSound.vorbisFile.read(
					(IntPtr)( streamBuffer + streamBufferLength ),
					currentSound.bufferSize - streamBufferLength, 0, 2, 1, IntPtr.Zero );

				//convert to mono for 3D
				if( readBytes > 0 && currentFileStreamSound.needConvertToMono )
				{
					byte* pointer = (byte*)streamBuffer + streamBufferLength;

					readBytes /= 2;

					for( int n = 0; n < readBytes; n += 2 )
					{
						*( pointer + n + 0 ) = *( pointer + n * 2 + 0 );
						*( pointer + n + 1 ) = *( pointer + n * 2 + 1 );
					}
				}

				if( readBytes > 0 )
					streamBufferLength += readBytes;
				else
					break;
			}

			if( streamBufferLength == 0 )
				return 0;

			int totalReaded = Math.Min( streamBufferLength, needRead );
			NativeUtils.CopyMemory( buffer, (IntPtr)streamBuffer, totalReaded );

			streamBufferLength -= totalReaded;
			if( streamBufferLength > 0 )
			{
				NativeUtils.MoveMemory( (IntPtr)streamBuffer, (IntPtr)( streamBuffer + totalReaded ),
					streamBufferLength );
			}

			return totalReaded;
		}

		int ReadDataFromDataStream( IntPtr buffer, int needRead )
		{
			DirectDataStreamSound currentDataStreamSound = (DirectDataStreamSound)currentSound;

			if( tempDataStreamReadArray.Length < needRead )
				tempDataStreamReadArray = new byte[ needRead ];

			int readed = currentDataStreamSound.dataReadCallback( tempDataStreamReadArray, 0, needRead );
			if( readed != 0 )
				Marshal.Copy( tempDataStreamReadArray, 0, buffer, readed );

			return readed;
		}

		unsafe protected override void PostAttachVirtualChannel()
		{
			DirectSoundWorld.criticalSection.Enter();

			int hr;

			currentSound = (DirectSound)CurrentVirtualChannel.CurrentSound;

			bool mode3d = (int)( currentSound.Mode & SoundMode.Mode3D ) != 0;
			bool loop = (int)( currentSound.Mode & SoundMode.Loop ) != 0;

			//DirectSampleSound
			DirectSampleSound currentSampleSound = currentSound as DirectSampleSound;
			if( currentSampleSound != null )
			{
				int lastBufferCount = currentSound.soundBuffers.Count;

				currentSoundBuffer = currentSound.GetBuffer( currentSampleSound.soundSamples.Length );
				if( currentSoundBuffer == null )
				{
					PreDetachVirtualChannel();
					DirectSoundWorld.criticalSection.Leave();
					return;
				}

				bool needFillData = false;

				if( lastBufferCount == 0 )
					needFillData = true;

				bool restored = false;
				if( !currentSound.RestoreSoundBuffers( out restored ) )
				{
					PreDetachVirtualChannel();
					DirectSoundWorld.criticalSection.Leave();
					return;
				}
				if( restored )
					needFillData = true;

				if( needFillData )
				{
					if( !currentSampleSound.FillSoundBuffersWithData() )
					{
						PreDetachVirtualChannel();
						DirectSoundWorld.criticalSection.Leave();
						return;
					}
				}
			}

			//DirectFileStreamSound, DirectDataStreamSound
			DirectFileStreamSound currentFileStreamSound = currentSound as DirectFileStreamSound;
			DirectDataStreamSound currentDataStreamSound = currentSound as DirectDataStreamSound;
			if( currentFileStreamSound != null || currentDataStreamSound != null )
			{
				int needBufferSize;

				if( currentFileStreamSound != null )
				{
					int numSamples = (int)currentFileStreamSound.vorbisFile.pcm_total( -1 );
					int channels;
					int rate;
					currentFileStreamSound.vorbisFile.get_info( -1, out channels, out rate );
					int sizeInBytes = numSamples * channels * 2;

					needBufferSize = sizeInBytes / 2;

					if( needBufferSize > 65536 * 2 )
						needBufferSize = 65536 * 2;
				}
				else
				{
					needBufferSize = currentDataStreamSound.creationBufferSize;
				}

				currentSoundBuffer = currentSound.GetBuffer( needBufferSize );
				if( currentSoundBuffer == null )
				{
					PreDetachVirtualChannel();
					DirectSoundWorld.criticalSection.Leave();
					return;
				}

				streamBuffer = (byte*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo,
					currentSound.bufferSize );
				streamBufferLength = 0;

				bool restored = false;
				if( !currentSound.RestoreSoundBuffers( out restored ) )
				{
					PreDetachVirtualChannel();
					DirectSoundWorld.criticalSection.Leave();
					return;
				}
				if( restored )
				{
					//buffer will be cleared in the BeginStreamPlay()
				}
			}

			//currentSound3DBuffer
			if( mode3d )
			{
				void*/*IDirectSound3DBuffer8*/ sound3DBuffer;

				GUID guid = DSound.IID_IDirectSound3DBuffer8;
				hr = IDirectSoundBuffer.QueryInterface( currentSoundBuffer, ref guid, &sound3DBuffer );
				if( Wrapper.FAILED( hr ) )
				{
					PreDetachVirtualChannel();
					DirectSoundWorld.Warning( "IDirectSoundBuffer.QueryInterface", hr );
					DirectSoundWorld.criticalSection.Leave();
					return;
				}
				currentSound3DBuffer = (IDirectSound3DBuffer8*)sound3DBuffer;
			}

			//update parameters
			if( mode3d )
			{
				UpdatePosition2();
				UpdateVelocity2();
			}
			else
				UpdatePan2();
			UpdatePitch2();
			UpdateVolume2();

			UpdateTime2();

			if( currentFileStreamSound != null || currentDataStreamSound != null )
				BeginStreamPlay();

			uint playFlags = 0;

			if( loop || currentFileStreamSound != null || currentDataStreamSound != null )
				playFlags |= DSound.DSBPLAY_LOOPING;

			hr = IDirectSoundBuffer.Play( currentSoundBuffer, 0, 0, playFlags );
			if( Wrapper.FAILED( hr ) )
			{
				PreDetachVirtualChannel();
				DirectSoundWorld.Warning( "IDirectSoundBuffer.Play", hr );
				DirectSoundWorld.criticalSection.Leave();
				return;
			}

			if( currentFileStreamSound != null )
				DirectSoundWorld.Instance.fileStreamRealChannels.Add( this );

			needStopVirtualChannel = false;

			DirectSoundWorld.criticalSection.Leave();
		}

		unsafe protected override void PreDetachVirtualChannel()
		{
			DirectSoundWorld.criticalSection.Enter();

			DirectFileStreamSound currentFileStreamSound = currentSound as DirectFileStreamSound;
			if( currentFileStreamSound != null )
				DirectSoundWorld.Instance.fileStreamRealChannels.Remove( this );

			if( currentSound3DBuffer != null )
			{
				IDirectSound3DBuffer8.Release( currentSound3DBuffer );
				currentSound3DBuffer = null;
			}

			if( currentSoundBuffer != null )
			{
				IDirectSoundBuffer.Stop( currentSoundBuffer );
				currentSound.FreeBuffer( currentSoundBuffer );
				currentSoundBuffer = null;
			}

			if( streamBuffer != null )
			{
				NativeUtils.Free( (IntPtr)streamBuffer );
				streamBuffer = null;
			}
			needStopAfterBufferRead = false;
			streamBufferLength = 0;

			needStopVirtualChannel = false;

			currentSound = null;

			DirectSoundWorld.criticalSection.Leave();
		}

		unsafe void UpdatePosition2()
		{
			if( currentSound3DBuffer != null )
			{
				Vec3 value = CurrentVirtualChannel.Position;

				int hr = IDirectSound3DBuffer8.SetPosition( currentSound3DBuffer,
					value.X, value.Z, value.Y, DSound.DS3D_IMMEDIATE );
				if( Wrapper.FAILED( hr ) )
					DirectSoundWorld.Warning( "IDirectSoundBuffer.SetPosition", hr );
			}
		}

		protected override void UpdatePosition()
		{
			DirectSoundWorld.criticalSection.Enter();
			if( currentSound != null )
				UpdatePosition2();
			DirectSoundWorld.criticalSection.Leave();
		}

		unsafe void UpdateVelocity2()
		{
			if( currentSound3DBuffer != null )
			{
				Vec3 value = CurrentVirtualChannel.Velocity;

				int hr = IDirectSound3DBuffer8.SetVelocity( currentSound3DBuffer,
					value.X, value.Z, value.Y, DSound.DS3D_IMMEDIATE );
				if( Wrapper.FAILED( hr ) )
					DirectSoundWorld.Warning( "IDirectSoundBuffer.SetVelocity", hr );
			}
		}

		protected override void UpdateVelocity()
		{
			DirectSoundWorld.criticalSection.Enter();
			if( currentSound != null )
				UpdateVelocity2();
			DirectSoundWorld.criticalSection.Leave();
		}

		unsafe void UpdateVolume2()
		{
			float value = CurrentVirtualChannel.GetTotalVolume() * CurrentVirtualChannel.GetRolloffFactor();

			int volume;
			if( value > 0.001f )
				volume = (int)( 20.0f * 100.0f * Math.Log10( value ) );
			else
				volume = DSound.DSBVOLUME_MIN;

			if( volume < DSound.DSBVOLUME_MIN )
				volume = DSound.DSBVOLUME_MIN;
			if( volume > DSound.DSBVOLUME_MAX )
				volume = DSound.DSBVOLUME_MAX;

			//update volume for dublicate buffers (IDirectSoundBuffer8.SetVolume problem)
			//it need have volume not equal to original buffer.
			bool isOriginalBuffer = false;
			IDirectSoundBuffer* originalBuffer = (IDirectSoundBuffer*)
				currentSound.soundBuffers[ 0 ].ToPointer();
			isOriginalBuffer = originalBuffer == currentSoundBuffer;

			if( !isOriginalBuffer )
			{
				int originalBufferVolume = 0;
				IDirectSoundBuffer.GetVolume( originalBuffer, out originalBufferVolume );

				if( volume == originalBufferVolume )
				{
					if( volume == DSound.DSBVOLUME_MAX )
						volume = DSound.DSBVOLUME_MAX - 1;
					else
						volume++;
				}
			}

			//change volume
			int hr = IDirectSoundBuffer.SetVolume( currentSoundBuffer, volume );
			if( Wrapper.FAILED( hr ) )
				DirectSoundWorld.Warning( "IDirectSoundBuffer.SetVolume", hr );

			//update volume of dublicate buffers
			if( isOriginalBuffer )
			{
				List<DirectSoundRealChannel> realChannels = DirectSoundWorld.Instance.realChannels;
				for( int nRealChannel = 0; nRealChannel < realChannels.Count; nRealChannel++ )
				{
					DirectSoundRealChannel realChannel = realChannels[ nRealChannel ];
					if( realChannel != this && realChannel.currentSound == currentSound )
						realChannel.UpdateVolume();
				}
			}
		}

		protected override void UpdateVolume()
		{
			DirectSoundWorld.criticalSection.Enter();
			if( currentSound != null )
				UpdateVolume2();
			DirectSoundWorld.criticalSection.Leave();
		}

		//unsafe void UpdateMinDistance2()
		//{
		//   if( currentSound3DBuffer != null )
		//   {
		//      float value = (float)CurrentVirtualChannel.MinDistance;

		//      int hr = IDirectSound3DBuffer8.SetMinDistance( currentSound3DBuffer, value,
		//         DSound.DS3D_IMMEDIATE );
		//      if( Wrapper.FAILED( hr ) )
		//         DirectSoundWorld.Warning( "IDirectSound3DBuffer8.SetMinDistance", hr );
		//   }
		//}

		//protected override void UpdateMinDistance()
		//{
		//   DirectSoundWorld.criticalSection.Enter();
		//   if( currentSound != null )
		//      UpdateMinDistance2();
		//   DirectSoundWorld.criticalSection.Leave();
		//}

		unsafe void UpdatePitch2()
		{
			float pitch = (float)currentSound.waveFormat->nSamplesPerSec *
				CurrentVirtualChannel.GetTotalPitch();

			int hr = IDirectSoundBuffer.SetFrequency( currentSoundBuffer, (uint)pitch );
			if( Wrapper.FAILED( hr ) )
				DirectSoundWorld.Warning( "IDirectSoundBuffer.SetFrequency", hr );
		}

		protected override void UpdatePitch()
		{
			DirectSoundWorld.criticalSection.Enter();
			if( currentSound != null )
				UpdatePitch2();
			DirectSoundWorld.criticalSection.Leave();
		}

		unsafe void UpdatePan2()
		{
			float value = CurrentVirtualChannel.Pan;
			MathFunctions.Clamp( ref value, -1, 1 );

			int pan;

			if( Math.Abs( value ) < .001f )
			{
				pan = 0;
			}
			else if( value < -.999f )
			{
				pan = DSound.DSBPAN_LEFT;
			}
			else if( value > .999f )
			{
				pan = DSound.DSBPAN_RIGHT;
			}
			else
			{
				pan = (int)( 20.0f * 100.0f * Math.Log10( 1 - Math.Abs( value ) ) );
				pan = Math.Abs( pan );
				if( value < 0 )
					pan = -pan;

				if( pan < DSound.DSBPAN_LEFT )
					pan = DSound.DSBPAN_LEFT;
				if( pan > DSound.DSBPAN_RIGHT )
					pan = DSound.DSBPAN_RIGHT;
			}

			int hr = IDirectSoundBuffer.SetPan( currentSoundBuffer, pan );
			if( Wrapper.FAILED( hr ) )
				DirectSoundWorld.Warning( "IDirectSoundBuffer.SetPan", hr );
		}

		protected override void UpdatePan()
		{
			DirectSoundWorld.criticalSection.Enter();
			if( currentSound != null )
				UpdatePan2();
			DirectSoundWorld.criticalSection.Leave();
		}

		unsafe void UpdateTime2()
		{
			float time = CurrentVirtualChannel.Time;

			//* 2 - 16 bit
			uint position = (uint)( time * (
				currentSound.waveFormat->nChannels * currentSound.waveFormat->nSamplesPerSec * 2 ) );

			if( position >= (uint)currentSound.bufferSize - 4 )
				position = (uint)currentSound.bufferSize - 4;
			position /= 4;
			position *= 4;

			int hr = IDirectSoundBuffer.SetCurrentPosition( currentSoundBuffer, position );
			if( Wrapper.FAILED( hr ) )
				DirectSoundWorld.Warning( "IDirectSoundBuffer.SetCurrentPosition", hr );
		}

		protected override void UpdateTime()
		{
			DirectSoundWorld.criticalSection.Enter();
			if( currentSound != null )
				UpdateTime2();
			DirectSoundWorld.criticalSection.Leave();
		}
	}
}
