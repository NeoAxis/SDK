// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;
using EditorBase;

namespace ExampleAddonCreationMEAddon
{
	public class AddonOptionsLeaf : OptionsLeaf
	{
		bool overrideCameraSettings;
		float cameraWavingAmplitude;
		float cameraWavingSpeed;

		//

		public AddonOptionsLeaf()
		{
			overrideCameraSettings = MapEditorAddonImpl.overrideCameraSettings;
			cameraWavingAmplitude = MapEditorAddonImpl.cameraWavingAmplitude;
			cameraWavingSpeed = MapEditorAddonImpl.cameraWavingSpeed;
		}

		[Category( "General" )]
		[DefaultValue( false )]
		[DisplayName( "Override Camera Settings" )]
		[TypeConverter( typeof( LocalizedBooleanConverter ) )]
		public bool OverrideCameraSettings
		{
			get { return overrideCameraSettings; }
			set { overrideCameraSettings = value; }
		}

		[Category( "General" )]
		[DefaultValue( .02f )]
		[DisplayName( "Ñamera Waving Amplitude" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, .3f )]
		public float ÑameraWavingAmplitude
		{
			get { return cameraWavingAmplitude; }
			set { cameraWavingAmplitude = value; }
		}

		[Category( "General" )]
		[DefaultValue( 1.0f )]
		[DisplayName( "Ñamera Waving Speed" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 10.0f )]
		public float CameraWavingSpeed
		{
			get { return cameraWavingSpeed; }
			set { cameraWavingSpeed = value; }
		}

		public override string ToString()
		{
			return "Add-on Example!";
		}

		protected override System.Drawing.Image GetImage()
		{
			return Properties.Resources.Addon_16;
		}

		protected override void OnOK()
		{
			MapEditorAddonImpl.overrideCameraSettings = overrideCameraSettings;
			MapEditorAddonImpl.cameraWavingAmplitude = cameraWavingAmplitude;
			MapEditorAddonImpl.cameraWavingSpeed = cameraWavingSpeed;

			base.OnOK();
		}
	}
}
