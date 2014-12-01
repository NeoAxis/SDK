// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.Utils;

namespace WinFormsMultiViewAppExample
{
	static class Program
	{
		[DllImport( "user32.dll" )]
		static extern bool SetProcessDPIAware();

		[STAThread]
		static void Main()
		{
			if( Environment.OSVersion.Version.Major >= 6 )
			{
				try
				{
					SetProcessDPIAware();
				}
				catch { }
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( new MainForm() );
		}
	}
}
