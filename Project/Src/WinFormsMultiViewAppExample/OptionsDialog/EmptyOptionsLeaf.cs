// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsMultiViewAppExample
{
	public class EmptyOptionsLeaf : OptionsLeaf
	{
		string displayName;

		public EmptyOptionsLeaf( string displayName )
		{
			this.displayName = displayName;
		}

		public override string ToString()
		{
			return displayName;
		}

	}
}
