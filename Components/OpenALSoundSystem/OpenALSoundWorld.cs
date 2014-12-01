// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using Engine;
using Engine.SoundSystem;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.Utils;
using Tao.OpenAl;

namespace OpenALSoundSystem
{
	sealed class OpenALSoundWorld : SoundWorld
	{
		static OpenALSoundWorld instance;

		IntPtr alDevice;
		IntPtr alContext;

		internal List<OpenALRealChannel> realChannels;
		internal List<OpenALRealChannel> fileStreamRealChannels;

		Thread thread;
		volatile bool needAbortThread;

		IntPtr/*HWND*/ hWnd;

		internal string captureDeviceName = "";

		OpenALCaptureSound recordingSound;

		public static CriticalSection criticalSection;

		float[] tempFloatArray6 = new float[ 6 ];

		//

		internal static bool CheckError()
		{
			int error = Al.alGetError();

			if( error == Al.AL_NO_ERROR )
				return false;

			string text;

			switch( error )
			{
			case Al.AL_INVALID_ENUM: text = "Invalid enum"; break;
			case Al.AL_INVALID_VALUE: text = "Invalid value"; break;
			case Al.AL_INVALID_OPERATION: text = "Invalid operation"; break;
			case Al.AL_OUT_OF_MEMORY: text = "Out of memory"; break;
			case Al.AL_INVALID_NAME: text = "Invalid name"; break;
			default: text = string.Format( "Unknown error ({0})", error ); break;
			}

			Log.Warning( "OpenALSoundSystem: Internal error: {0}.", text );

			return true;
		}

		protected override bool InitLibrary( IntPtr mainWindowHandle,
			int maxReal2DChannels, int maxReal3DChannels )
		{
			instance = this;

			NativeLibraryManager.PreLoadLibrary( "libogg" );
			NativeLibraryManager.PreLoadLibrary( "libvorbis" );
			NativeLibraryManager.PreLoadLibrary( "libvorbisfile" );

			//preload OpenAL32
			{
				string fileName;
				if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
					fileName = "OpenAL32.dll";
				else if( PlatformInfo.Platform == PlatformInfo.Platforms.MacOSX )
					fileName = "OpenAL32.dylib";
				else if( PlatformInfo.Platform == PlatformInfo.Platforms.Android )
					fileName = "libOpenAL32.so";
				else
				{
					Log.Fatal( "OpenALSoundWorld: InitLibrary: Unknown platform." );
					return false;
				}

				string path = Path.Combine( NativeLibraryManager.GetNativeLibrariesDirectory(), fileName );
				if( File.Exists( path ) )
					NativeLibraryManager.PreLoadLibrary( "OpenAL32" );
			}

			criticalSection = CriticalSection.Create();

			if( PlatformInfo.Platform == PlatformInfo.Platforms.Android )
			{
				Alc.alcSetJNIEnvironmentAndJavaVM(
					EngineApp.Instance._CallCustomPlatformSpecificMethod( "GetJNIEnvironment", IntPtr.Zero ),
					EngineApp.Instance._CallCustomPlatformSpecificMethod( "GetJavaVM", IntPtr.Zero ) );
			}

			//string[] devices = Alc.alcGetStringv( IntPtr.Zero, Alc.ALC_DEVICE_SPECIFIER );

			try
			{
				alDevice = Alc.alcOpenDevice( null );
			}
			catch( DllNotFoundException )
			{
				Log.InvisibleInfo( "OpenALSoundSystem: OpenAL not found." );
				return false;
			}
			if( alDevice == IntPtr.Zero )
			{
				Log.InvisibleInfo( "OpenALSoundSystem: No sound driver." );
				return false;
			}

			alContext = Alc.alcCreateContext( alDevice, IntPtr.Zero );
			if( alContext == IntPtr.Zero )
			{
				Log.Error( "OpenALSoundSystem: Create context failed." );
				return false;
			}

			Alc.alcMakeContextCurrent( alContext );

			if( CheckError() )
				return false;

			//get captureDeviceName
			try
			{
				captureDeviceName = Alc.alcGetString( alDevice, Alc.ALC_CAPTURE_DEFAULT_DEVICE_SPECIFIER );
			}
			catch { }

			//Channels
			realChannels = new List<OpenALRealChannel>();
			for( int n = 0; n < maxReal2DChannels; n++ )
			{
				OpenALRealChannel realChannel = new OpenALRealChannel();
				AddRealChannel( realChannel, false );
				realChannels.Add( realChannel );
			}
			for( int n = 0; n < maxReal3DChannels; n++ )
			{
				OpenALRealChannel realChannel = new OpenALRealChannel();
				AddRealChannel( realChannel, true );
				realChannels.Add( realChannel );
			}

			fileStreamRealChannels = new List<OpenALRealChannel>();

			thread = new Thread( new ThreadStart( ThreadFunction ) );
			thread.CurrentCulture = new System.Globalization.CultureInfo( "en-US" );
			thread.IsBackground = true;
			thread.Start();

			hWnd = mainWindowHandle;

			Al.alDistanceModel( Al.AL_NONE );

			return true;
		}

		protected override void ShutdownLibrary()
		{
			if( thread != null )
			{
				needAbortThread = true;
				Thread.Sleep( 50 );
				thread.Abort();
			}

			if( realChannels != null )
			{
				foreach( OpenALRealChannel realChannel in realChannels )
				{
					if( realChannel.alSource != 0 )
					{
						Al.alDeleteSources( 1, ref realChannel.alSource );
						realChannel.alSource = 0;
					}
				}
			}

			try
			{
				Alc.alcMakeContextCurrent( IntPtr.Zero );
				Alc.alcDestroyContext( alContext );
				Alc.alcCloseDevice( alDevice );
			}
			catch { }

			if( realChannels != null )
			{
				realChannels.Clear();
				realChannels = null;
			}

			if( criticalSection != null )
			{
				criticalSection.Dispose();
				criticalSection = null;
			}

			instance = null;
		}

		internal new static OpenALSoundWorld Instance
		{
			get { return instance; }
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

			OpenALSound sound;

			sound = (OpenALSound)base.SoundCreate( name, mode );
			if( sound != null )
			{
				criticalSection.Leave();
				return sound;
			}

			VirtualFileStream stream = CreateFileStream( name );
			if( stream == null )
			{
				criticalSection.Leave();
				Log.Warning( string.Format( "Creating sound \"{0}\" failed.", name ) );
				return null;
			}

			bool initialized;

			if( ( mode & SoundMode.Stream ) == 0 )
			{
				sound = new OpenALSampleSound( stream, SoundType.Unknown, name, mode, out initialized );
				stream.Close();
			}
			else
			{
				sound = new OpenALFileStreamSound( stream, true, SoundType.Unknown, name, mode,
					out initialized );
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

			OpenALSound sound;
			bool initialized;

			if( (int)( mode & SoundMode.Stream ) == 0 )
			{
				sound = new OpenALSampleSound( stream, soundType, null, mode, out initialized );
				if( closeStreamAfterReading )
					stream.Close();
			}
			else
			{
				sound = new OpenALFileStreamSound( stream, closeStreamAfterReading, soundType, null,
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

		public override Sound SoundCreateDataBuffer( SoundMode mode, int channels, int frequency,
			int bufferSize, DataReadDelegate dataReadCallback )
		{
			criticalSection.Enter();

			Sound sound;

			if( ( mode & SoundMode.Record ) != 0 )
			{
				OpenALCaptureSound captureSound = new OpenALCaptureSound(
					mode, channels, frequency, bufferSize );
				if( captureSound.alCaptureDevice == IntPtr.Zero )
				{
					criticalSection.Leave();
					return null;
				}
				sound = captureSound;
			}
			else
			{
				sound = new OpenALDataStreamSound( mode, channels, frequency, bufferSize,
					dataReadCallback );
			}

			criticalSection.Leave();

			return sound;
		}

		protected override void OnSetDopplerEffectScale( float dopplerScale )
		{
			criticalSection.Enter();

			Al.alDopplerFactor( dopplerScale );
			CheckError();
			Al.alDopplerVelocity( 1.0f );
			CheckError();

			criticalSection.Leave();
		}

		protected override void OnSetListener( Vec3 position, Vec3 velocity, Vec3 forward, Vec3 up )
		{
			criticalSection.Enter();

			Al.alListener3f( Al.AL_POSITION, position.X, position.Y, position.Z );
			Al.alListener3f( Al.AL_VELOCITY, velocity.X, velocity.Y, velocity.Z );

			unsafe
			{
				fixed( float* orientation = tempFloatArray6 )
				{
					orientation[ 0 ] = forward.X;
					orientation[ 1 ] = forward.Y;
					orientation[ 2 ] = forward.Z;
					orientation[ 3 ] = up.X;
					orientation[ 4 ] = up.Y;
					orientation[ 5 ] = up.Z;
					Al.alListenerfv( Al.AL_ORIENTATION, orientation );
				}
			}

			CheckError();

			criticalSection.Leave();
		}

		public override string DriverName
		{
			get
			{
				criticalSection.Enter();

				string version = "UNKNOWN";
				string device = "UNKNOWN";
				try
				{
					version = Al.alGetString( Al.AL_VERSION );
					device = Alc.alcGetString( alDevice, Alc.ALC_DEVICE_SPECIFIER );
				}
				catch { }

				string value = string.Format( "OpenAL {0}, Device: {1}", version, device );

				criticalSection.Leave();

				return value;
			}
		}

		public override string[] RecordDrivers
		{
			get
			{
				criticalSection.Enter();

				string[] value = null;
				try
				{
					value = Alc.alcGetStringv( IntPtr.Zero, Alc.ALC_CAPTURE_DEVICE_SPECIFIER );
				}
				catch { }
				if( value == null )
					value = new string[ 0 ];

				criticalSection.Leave();

				return value;
			}
		}

		public override int RecordDriver
		{
			get
			{
				return Array.IndexOf<string>( RecordDrivers, captureDeviceName );
			}
			set
			{
				captureDeviceName = RecordDrivers[ value ];
			}
		}

		public override bool RecordStart( Sound sound )
		{
			criticalSection.Enter();

			OpenALCaptureSound captureSound = sound as OpenALCaptureSound;
			if( captureSound == null )
			{
				criticalSection.Leave();
				Log.Warning( "OpenALSoundSystem: Recording failed. Is sound a not for recording." );
				return false;
			}

			Alc.alcCaptureStart( captureSound.alCaptureDevice );

			recordingSound = captureSound;

			criticalSection.Leave();
			return true;
		}

		public override void RecordStop()
		{
			criticalSection.Enter();

			if( recordingSound != null )
			{
				Alc.alcCaptureStop( recordingSound.alCaptureDevice );
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
			while( true )
			{
				criticalSection.Enter();

				for( int n = 0; n < fileStreamRealChannels.Count; n++ )
					fileStreamRealChannels[ n ].UpdateFileStreamFromThread();

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
				if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
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
				else
				{
					if( EngineApp.Instance.SystemPause )
						active = false;
				}
			}

			return active;
		}
	}
}
