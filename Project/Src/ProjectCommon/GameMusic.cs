// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.SoundSystem;
using Engine.FileSystem;

namespace ProjectCommon
{
	/// <summary>
	/// Class for management of music in game.
	/// </summary>
	public static class GameMusic
	{
		static bool initialized;
		static ChannelGroup musicChannelGroup;
		static Sound musicSound;
		static VirtualChannel musicChannel;

		//

		static void Init()
		{
			if( initialized )
				return;

			if( SoundWorld.Instance == null )
				return;

			musicChannelGroup = SoundWorld.Instance.CreateChannelGroup();
			if( musicChannelGroup != null )
				SoundWorld.Instance.MasterChannelGroup.AddGroup( musicChannelGroup );

			initialized = true;
		}

		/// <summary>
		/// Gets the music channel group.
		/// </summary>
		public static ChannelGroup MusicChannelGroup
		{
			get
			{
				Init();
				return musicChannelGroup;
			}
		}

		/// <summary>
		/// Play music.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <param name="loop">Looping flag.</param>
		public static void MusicPlay( string fileName, bool loop )
		{
			Init();

			if( musicSound != null && string.Compare( musicSound.Name, fileName, true ) == 0 )
				return;

			MusicStop();

			if( !string.IsNullOrEmpty( fileName ) && VirtualFile.Exists( fileName ) )
			{
				SoundMode mode = SoundMode.Stream;
				if( loop )
					mode |= SoundMode.Loop;

				musicSound = SoundWorld.Instance.SoundCreate( fileName, mode );

				if( musicSound != null )
					musicChannel = SoundWorld.Instance.SoundPlay( musicSound, musicChannelGroup, .5f );
			}
		}

		/// <summary>
		/// Stop music.
		/// </summary>
		public static void MusicStop()
		{
			if( musicChannel != null )
			{
				musicChannel.Stop();
				musicChannel = null;
			}

			if( musicSound != null )
			{
				musicSound.Dispose();
				musicSound = null;
			}
		}

		public static VirtualChannel MusicChannel
		{
			get
			{
				if( musicChannel != null && musicChannel.IsStopped() )
					musicChannel = null;
				return musicChannel;
			}
		}
	}
}
