// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using Engine;
using WinFormsAppFramework;

namespace WinFormsMultiViewAppExample
{
	class MultiViewAppEngineApp : WinFormsAppEngineApp
	{
		static MultiViewAppEngineApp instance;

		///////////////////////////////////////////

		public static new MultiViewAppEngineApp Instance
		{
			get { return instance; }
		}

		public MultiViewAppEngineApp( ApplicationTypes applicationType )
			: base( applicationType )
		{
			instance = this;
		}

		protected override bool OnCreate()
		{
			if( !base.OnCreate() )
				return false;
			return true;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			instance = null;
		}

		protected override void OnHideAnyEditorSplashForms()
		{
			base.OnHideAnyEditorSplashForms();

			if( SplashForm.Instance != null )
				SplashForm.Instance.Hide();
		}
	}
}
