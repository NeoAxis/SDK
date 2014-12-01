// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WinFormsMultiViewAppExample
{
	public class OptionsLeaf
	{
		public virtual bool OnIsAllowOK()
		{
			return true;
		}

		public virtual void OnOK() { }

		public virtual Image GetImage() { return null; }
	}
}
