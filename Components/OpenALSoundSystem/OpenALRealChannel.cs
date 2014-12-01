// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine;
using Engine.FileSystem;
using Engine.SoundSystem;
using Engine.MathEx;
using Engine.Utils;
using OggVorbisTheora;
using Tao.OpenAl;

namespace OpenALSoundSystem
{
	sealed class OpenALRealChannel : RealChannel
	{
		internal int alSource;

		internal OpenALSound currentSound;

		//stream
		unsafe byte* streamBuffer;
		int streamBufferSize;
		bool streamActive;

		byte[] tempDataStreamReadArray = new byte[ 0 ];

		//

		public OpenALRealChannel()
		{
		}

		unsafe protected override void PostAttachVirtualChannel()
		{
			OpenALSoundWorld.criticalSection.Enter();

			currentSound = (OpenALSound)CurrentVirtualChannel.CurrentSound;

			OpenALSampleSound sampleSound = currentSound as OpenALSampleSound;
			OpenALDataBufferSound streamSound = null;
			OpenALFileStreamSound fileStreamSound = null;
			OpenALDataStreamSound dataStreamSound = null;

			if( sampleSound == null )
			{
				streamSound = currentSound as OpenALDataBufferSound;
				fileStreamSound = currentSound as OpenALFileStreamSound;
				dataStreamSound = currentSound as OpenALDataStreamSound;
			}

			//create streamBuffer
			if( fileStreamSound != null )
			{
				int bufferSize = 0;

				int numSamples = (int)fileStreamSound.vorbisFile.pcm_total( -1 );
				int channels;
				int rate;
				fileStreamSound.vorbisFile.get_info( -1, out channels, out rate );

				if( fileStreamSound.needConvertToMono )
					channels = 1;

				int sizeInBytes = numSamples * channels * 2;

				bufferSize = sizeInBytes / 2;

				//!!!!!!!new
				if( bufferSize > 65536 * 4 )
					bufferSize = 65536 * 4;
				//if( bufferSize > 65356 )
				//   bufferSize = 65356;

				streamBufferSize = bufferSize;
				streamBuffer = (byte*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo, streamBufferSize );
			}

			if( dataStreamSound != null )
			{
				streamBufferSize = dataStreamSound.bufferSize;
				streamBuffer = (byte*)NativeUtils.Alloc( NativeMemoryAllocationType.SoundAndVideo,
					streamBufferSize );
			}

			//init source

			bool mode3d = ( currentSound.Mode & SoundMode.Mode3D ) != 0;
			bool loop = ( currentSound.Mode & SoundMode.Loop ) != 0;

			if( alSource == 0 )
			{
				Al.alGenSources( 1, out alSource );
				if( OpenALSoundWorld.CheckError() )
				{
					PreDetachVirtualChannel();
					OpenALSoundWorld.criticalSection.Leave();
					return;
				}
			}


			if( sampleSound != null )
			{
				//no stream sound
				Al.alSourcei( alSource, Al.AL_BUFFER, sampleSound.alBuffer );
				if( OpenALSoundWorld.CheckError() )
				{
					PreDetachVirtualChannel();
					OpenALSoundWorld.criticalSection.Leave();
					return;
				}
			}

			if( fileStreamSound != null )
				FileStreamStartPlay();

			if( dataStreamSound != null )
				DataStreamStartPlay();

			Al.alSourcei( alSource, Al.AL_SOURCE_RELATIVE, mode3d ? Al.AL_FALSE : Al.AL_TRUE );

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

			if( sampleSound != null )
				Al.alSourcei( alSource, Al.AL_LOOPING, loop ? Al.AL_TRUE : Al.AL_FALSE );
			else
				Al.alSourcei( alSource, Al.AL_LOOPING, Al.AL_FALSE );
			if( OpenALSoundWorld.CheckError() )
			{
				PreDetachVirtualChannel();
				OpenALSoundWorld.criticalSection.Leave();
				return;
			}

			UpdateTime2();

			//unpause
			Al.alSourcePlay( alSource );
			OpenALSoundWorld.CheckError();

			//add to fileStreamChannels
			if( fileStreamSound != null )
				OpenALSoundWorld.Instance.fileStreamRealChannels.Add( this );

			OpenALSoundWorld.criticalSection.Leave();
		}

		unsafe protected override void PreDetachVirtualChannel()
		{
			OpenALSoundWorld.criticalSection.Enter();

			Al.alSourceStop( alSource );
			OpenALSoundWorld.CheckError();

			if( currentSound is OpenALDataBufferSound )
			{
				if( currentSound is OpenALFileStreamSound )
					OpenALSoundWorld.Instance.fileStreamRealChannels.Remove( this );

				if( streamBuffer != null )
				{
					NativeUtils.Free( (IntPtr)streamBuffer );
					streamBuffer = null;
					streamBufferSize = 0;
				}
			}

			Al.alSourcei( alSource, Al.AL_BUFFER, 0 );
			OpenALSoundWorld.CheckError();

			currentSound = null;

			OpenALSoundWorld.criticalSection.Leave();
		}

		void UpdatePosition2()
		{
			Vec3 value = CurrentVirtualChannel.Position;

			Al.alSource3f( alSource, Al.AL_POSITION, value.X, value.Y, value.Z );
			OpenALSoundWorld.CheckError();
		}

		protected override void UpdatePosition()
		{
			OpenALSoundWorld.criticalSection.Enter();
			UpdatePosition2();
			OpenALSoundWorld.criticalSection.Leave();
		}

		void UpdateVelocity2()
		{
			Vec3 value = CurrentVirtualChannel.Velocity;

			Al.alSource3f( alSource, Al.AL_VELOCITY, value.X, value.Y, value.Z );
			OpenALSoundWorld.CheckError();
		}

		protected override void UpdateVelocity()
		{
			OpenALSoundWorld.criticalSection.Enter();
			UpdateVelocity2();
			OpenALSoundWorld.criticalSection.Leave();
		}

		void UpdateVolume2()
		{
			float value = CurrentVirtualChannel.GetTotalVolume() * CurrentVirtualChannel.GetRolloffFactor();
			Al.alSourcef( alSource, Al.AL_GAIN, value );
			OpenALSoundWorld.CheckError();
		}

		protected override void UpdateVolume()
		{
			OpenALSoundWorld.criticalSection.Enter();
			UpdateVolume2();
			OpenALSoundWorld.criticalSection.Leave();
		}

		void UpdatePitch2()
		{
			Al.alSourcef( alSource, Al.AL_PITCH, CurrentVirtualChannel.GetTotalPitch() );
			OpenALSoundWorld.CheckError();
		}

		protected override void UpdatePitch()
		{
			OpenALSoundWorld.criticalSection.Enter();
			UpdatePitch2();
			OpenALSoundWorld.criticalSection.Leave();
		}

		void UpdatePan2()
		{
			float value = CurrentVirtualChannel.Pan;
			MathFunctions.Clamp( ref value, -1, 1 );
			Al.alSource3f( alSource, Al.AL_POSITION, value * .1f, 0, 0 );
			OpenALSoundWorld.CheckError();
		}

		protected override void UpdatePan()
		{
			OpenALSoundWorld.criticalSection.Enter();
			UpdatePan2();
			OpenALSoundWorld.criticalSection.Leave();
		}

		void UpdateTime2()
		{
			Al.alSourcef( alSource, Al.AL_SEC_OFFSET, CurrentVirtualChannel.Time );
			OpenALSoundWorld.CheckError();
		}

		protected override void UpdateTime()
		{
			OpenALSoundWorld.criticalSection.Enter();
			UpdateTime2();
			OpenALSoundWorld.criticalSection.Leave();
		}

		public void Update()
		{
			if( currentSound == null )
				return;

			if( ( currentSound.Mode & SoundMode.Mode3D ) != 0 )
				UpdateVolume2();

			if( currentSound is OpenALSampleSound )
			{
				UpdateSample();
				return;
			}

			if( currentSound is OpenALDataStreamSound )
				UpdateDataStream();
		}

		void UpdateSample()
		{
			int state;
			Al.alGetSourcei( alSource, Al.AL_SOURCE_STATE, out state );
			OpenALSoundWorld.CheckError();
			if( state == Al.AL_STOPPED )
				CurrentVirtualChannel.Stop();
		}

		int ReadDataFromDataStream( IntPtr buffer, int needRead )
		{
			OpenALDataStreamSound currentDataStreamSound = (OpenALDataStreamSound)currentSound;

			if( tempDataStreamReadArray.Length < needRead )
				tempDataStreamReadArray = new byte[ needRead ];

			int readed = currentDataStreamSound.dataReadCallback( tempDataStreamReadArray, 0,
				needRead );
			if( readed != 0 )
				Marshal.Copy( tempDataStreamReadArray, 0, buffer, readed );

			if( readed < 16 )
			{
				readed = Math.Min( needRead, 16 );
				NativeUtils.ZeroMemory( buffer, readed );
			}

			return readed;
		}

		unsafe void DataStreamStartPlay()
		{
			OpenALDataStreamSound dataStreamSound = (OpenALDataStreamSound)currentSound;

			for( int n = 0; n < dataStreamSound.alDataBuffers.Length; n++ )
			{
				int readed = ReadDataFromDataStream( (IntPtr)streamBuffer, streamBufferSize );

				int alFormat = ( currentSound.channels == 1 ) ? Al.AL_FORMAT_MONO16 :
					Al.AL_FORMAT_STEREO16;
				Al.alBufferData( dataStreamSound.alDataBuffers[ n ], alFormat, streamBuffer,
					readed, currentSound.frequency );
			}

			fixed( int* pAlDataBuffers = dataStreamSound.alDataBuffers )
			{
				Al.alSourceQueueBuffers( alSource, dataStreamSound.alDataBuffers.Length,
					pAlDataBuffers );
			}
		}

		unsafe void UpdateDataStream()
		{
			OpenALDataStreamSound dataStreamSound = (OpenALDataStreamSound)currentSound;

			int alFormat = ( currentSound.channels == 1 ) ? Al.AL_FORMAT_MONO16 : Al.AL_FORMAT_STEREO16;

			int processed;

			Al.alGetSourcei( alSource, Al.AL_BUFFERS_PROCESSED, out processed );
			OpenALSoundWorld.CheckError();

			while( processed != 0 )
			{
				int alBuffer = 0;

				int readed = ReadDataFromDataStream( (IntPtr)streamBuffer, streamBufferSize );

				Al.alSourceUnqueueBuffers( alSource, 1, ref alBuffer );
				OpenALSoundWorld.CheckError();

				Al.alBufferData( alBuffer, alFormat, streamBuffer, readed, currentSound.frequency );
				OpenALSoundWorld.CheckError();

				Al.alSourceQueueBuffers( alSource, 1, ref alBuffer );
				OpenALSoundWorld.CheckError();

				processed--;
			}

			int state;
			Al.alGetSourcei( alSource, Al.AL_SOURCE_STATE, out state );
			OpenALSoundWorld.CheckError();

			if( state != Al.AL_PLAYING )
			{
				Al.alGetSourcei( alSource, Al.AL_BUFFERS_PROCESSED, out processed );
				Al.alSourcePlay( alSource );
			}
		}

		public void UpdateFileStreamFromThread()
		{
			if( currentSound == null )
				return;

			//update buffers
			int processed;
			Al.alGetSourcei( alSource, Al.AL_BUFFERS_PROCESSED, out processed );
			OpenALSoundWorld.CheckError();
			while( processed != 0 )
			{
				int alStreamBuffer = 0;
				Al.alSourceUnqueueBuffers( alSource, 1, ref alStreamBuffer );

				OpenALSoundWorld.CheckError();
				FileStream( alStreamBuffer );
				if( streamActive )
				{
					Al.alSourceQueueBuffers( alSource, 1, ref alStreamBuffer );
					OpenALSoundWorld.CheckError();
				}

				processed--;
			}

			//play if buffer stopped (from behind internal buffers processed)
			int state;
			Al.alGetSourcei( alSource, Al.AL_SOURCE_STATE, out state );
			OpenALSoundWorld.CheckError();
			if( state == Al.AL_STOPPED )
			{
				int queued;
				Al.alGetSourcei( alSource, Al.AL_BUFFERS_QUEUED, out queued );
				if( queued != 0 )
					Al.alSourcePlay( alSource );
			}

			//file stream played

			OpenALFileStreamSound fileStreamSound = (OpenALFileStreamSound)currentSound;

			if( !streamActive )
			{
				if( ( currentSound.Mode & SoundMode.Loop ) != 0 )
				{
					//loop play. we need recreate vorbis file

					//stop and unqueues sources
					Al.alSourceStop( alSource );
					Al.alGetSourcei( alSource, Al.AL_BUFFERS_PROCESSED, out processed );
					OpenALSoundWorld.CheckError();
					while( processed != 0 )
					{
						int alStreamBuffer = 0;
						Al.alSourceUnqueueBuffers( alSource, 1, ref alStreamBuffer );
						OpenALSoundWorld.CheckError();
						processed--;
					}

					//recreate vorbis file
					fileStreamSound.Rewind();

					FileStreamStartPlay();

					//Pause = false;
					Al.alSourcePlay( alSource );
					OpenALSoundWorld.CheckError();
				}
				else
				{
					CurrentVirtualChannel.Stop();
				}
			}
		}

		void FileStreamStartPlay()
		{
			OpenALDataBufferSound streamSound = (OpenALDataBufferSound)currentSound;

			for( int n = 0; n < streamSound.alDataBuffers.Length; n++ )
			{
				if( !FileStream( streamSound.alDataBuffers[ n ] ) )
					return;

				Al.alSourceQueueBuffers( alSource, 1, ref streamSound.alDataBuffers[ n ] );
				OpenALSoundWorld.CheckError();
			}
		}

		unsafe bool FileStream( int alStreamBuffer )
		{
			OpenALFileStreamSound fileStreamSound = (OpenALFileStreamSound)currentSound;

			int size = 0;

			streamActive = true;

			while( size < streamBufferSize )
			{
				byte* pointer = streamBuffer + size;

				int readBytes = fileStreamSound.vorbisFile.read( (IntPtr)pointer,
					streamBufferSize - size, 0, 2, 1, IntPtr.Zero );

				//convert to mono for 3D
				if( readBytes > 0 && fileStreamSound.needConvertToMono )
				{
					readBytes /= 2;

					for( int n = 0; n < readBytes; n += 2 )
					{
						pointer[ n + 0 ] = pointer[ n * 2 + 0 ];
						pointer[ n + 1 ] = pointer[ n * 2 + 1 ];
					}
				}

				if( readBytes > 0 )
					size += readBytes;
				else
					break;
			}

			if( size == 0 )
			{
				streamActive = false;
				return false;
			}

			int alFormat = ( currentSound.channels == 1 ) ? Al.AL_FORMAT_MONO16 :
				Al.AL_FORMAT_STEREO16;
			Al.alBufferData( alStreamBuffer, alFormat, streamBuffer, size, currentSound.frequency );
			OpenALSoundWorld.CheckError();

			return true;
		}
	}
}
