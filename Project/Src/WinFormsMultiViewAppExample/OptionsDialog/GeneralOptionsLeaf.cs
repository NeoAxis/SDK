// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace WinFormsMultiViewAppExample
{
	public class GeneralOptionsLeaf : OptionsLeaf
	{
		float soundVolume;
		bool showSplashScreenAtStartup;

		//

		public GeneralOptionsLeaf()
		{
			soundVolume = MainForm.SoundVolume;
			showSplashScreenAtStartup = MainForm.showSplashScreenAtStartup;
		}

		[Category( "General" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		[DisplayName( "Sound Volume" )]
		[DefaultValue( .5f )]
		public float SoundVolume
		{
			get { return soundVolume; }
			set { soundVolume = value; }
		}

		[Category( "General" )]
		[DisplayName( "Show Splash Screen At Startup" )]
		[DefaultValue( true )]
		[TypeConverter( typeof( LocalizedBooleanConverter ) )]
		public bool ShowSplashScreenAtStartup
		{
			get { return showSplashScreenAtStartup; }
			set { showSplashScreenAtStartup = value; }
		}

		public override string ToString()
		{
			return "General";
		}

		public override void OnOK()
		{
			MainForm.SoundVolume = soundVolume;
			MainForm.showSplashScreenAtStartup = showSplashScreenAtStartup;

			base.OnOK();
		}
	}
}
