// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Engine;

namespace ChatExample
{
	static class Program
	{
		[DllImport( "user32.dll" )]
		static extern bool SetProcessDPIAware();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
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