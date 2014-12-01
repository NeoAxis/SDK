// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.FileSystem.Archives;
using Engine.Utils;
using ICSharpCode.SharpZipLib.Zip;

namespace ZipArchive
{
	class ZipArchive : Archive
	{
		ZipFile zipFile;

		//

		public ZipArchive( ArchiveFactory factory, string fileName, ZipFile zipFile )
			: base( factory, fileName )
		{
			this.zipFile = zipFile;
		}

		public override void Dispose()
		{
			if( zipFile != null )
			{
				zipFile.Close();
				zipFile = null;
			}

			base.Dispose();
		}

		protected override void OnGetDirectoryAndFileList( out string[] directories,
			out GetListFileInfo[] files )
		{
			List<string> directoryNames = new List<string>();
			List<GetListFileInfo> fileInfos = new List<GetListFileInfo>();

			foreach( ZipEntry entry in zipFile )
			{
				if( entry.IsDirectory )
					directoryNames.Add( entry.Name );
				else if( entry.IsFile )
					fileInfos.Add( new GetListFileInfo( entry.Name, entry.Size ) );
			}

			directories = directoryNames.ToArray();
			files = fileInfos.ToArray();
		}

		protected override VirtualFileStream OnFileOpen( string inArchiveFileName )
		{
			lock( zipFile )
			{
				ZipEntry entry = zipFile.GetEntry( inArchiveFileName );
				Stream zipStream = zipFile.GetInputStream( entry );

				byte[] buffer = new byte[ entry.Size ];
				int readed = zipStream.Read( buffer, 0, (int)entry.Size );
				if( readed != buffer.Length )
					throw new Exception( "ZipArchive: Reading stream failed." );

				return new MemoryVirtualFileStream( buffer );
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	class ZipArchiveFactory : ArchiveFactory
	{
		public ZipArchiveFactory()
			: base( "zip" )
		{
		}

		protected override bool OnInit()
		{
			return true;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		protected override Archive OnLoadArchive( string fileName )
		{
			ZipFile zipFile;

			try
			{
				//Mono runtime specific
				if( RuntimeFramework.Runtime == RuntimeFramework.RuntimeType.Mono )
					ZipConstants.DefaultCodePage = 0;

				zipFile = new ZipFile( fileName );

				//encryption support
				//zipFile.Password = "qwerty";
			}
			catch( Exception e )
			{
				Log.Fatal( "ZipArchiveFactory: Loading of a zip file \"{0}\" failed ({1}).",
					fileName, e.Message );
				return null;
			}

			return new ZipArchive( this, fileName, zipFile );
		}
	}

}
