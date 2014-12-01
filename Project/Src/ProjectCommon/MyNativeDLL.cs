// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Engine;
using Engine.FileSystem;

namespace ProjectCommon
{
	/// <summary>
	/// This class is a example of custom native DLL connection.
	/// See \Game\Src\MyNativeDLL for native DLL example source code.
	/// </summary>
	public static class MyNativeDLL
	{
		static bool nativeDllIsPreloaded;

		///////////////////////////////////////////

		static class Wrapper
		{
			public const string library = "MyNativeDLL";
			public const CallingConvention convention = CallingConvention.Cdecl;

			//use [MarshalAs( UnmanagedType.U1 )] attribute for boolean parameters.

			[DllImport( library, CallingConvention = convention )]
			public static extern int Test( int parameter );
		}

		///////////////////////////////////////////

		static void PreloadNativeDLL()
		{
			if( !nativeDllIsPreloaded )
			{
				NativeLibraryManager.PreLoadLibrary( Wrapper.library );
				nativeDllIsPreloaded = true;
			}
		}

		public static void TestCall()
		{
			//we need preload native library first.
			PreloadNativeDLL();

			//now call native method.
			int result = Wrapper.Test( 0 );
			Log.Warning( "Native DLL call. Result: {0}.", result );
		}
	}

}
