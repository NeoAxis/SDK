// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using Engine;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.SoundSystem;
using Engine.Networking;

namespace ProjectCommon
{
	/// <summary>
	/// Defines a window class for Profiling Tool.
	/// </summary>
	public class ProfilingToolWindow : Control
	{
		static ProfilingToolWindow instance;

		[Config( "ProfilingToolWindow", "background" )]
		static bool background;

		[Config( "ProfilingToolWindow", "lastPageSelectionIndex" )]
		static int lastPageSelectionIndex;

		Control window;
		CheckBox backgroundCheckBox;
		ComboBox pageSelectionComboBox;
		Button pagePreviousButton;
		Button pageNextButton;
		Control pageAreaControl;

		List<Page> pages = new List<Page>();
		Page activePage;

		///////////////////////////////////////////

		public interface ICarInfo
		{
			string GetInfoForProfilingTool();
		}

		///////////////////////////////////////////

		public static ProfilingToolWindow Instance
		{
			get { return instance; }
		}

		public bool Background
		{
			get { return background; }
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			instance = this;

			window = ControlDeclarationManager.Instance.CreateControl( "GUI\\ProfilingTool\\ProfilingToolWindow.gui" );
			Controls.Add( window );

			backgroundCheckBox = (CheckBox)window.Controls[ "Background" ];
			backgroundCheckBox.Checked = background;
			backgroundCheckBox.CheckedChange += Background_CheckedChange;

			Button closeButton = (Button)window.Controls[ "Close" ];
			closeButton.Click += delegate( Button sender )
			{
				SetShouldDetach();
			};
			if( PlatformInfo.Platform == PlatformInfo.Platforms.MacOSX )
				closeButton.Text = closeButton.Text.Replace( "F11", "Fn+F5" );

			pageSelectionComboBox = (ComboBox)window.Controls[ "PageSelection" ];
			pageSelectionComboBox.SelectedIndexChange += pageSelectionComboBox_SelectedIndexChange;

			pagePreviousButton = (Button)window.Controls[ "PagePrevious" ];
			pagePreviousButton.Click += new Button.ClickDelegate( pagePreviousButton_Click );

			pageNextButton = (Button)window.Controls[ "PageNext" ];
			pageNextButton.Click += new Button.ClickDelegate( pageNextButton_Click );

			pageAreaControl = window.Controls[ "PageArea" ];

			//add pages
			pages.Add( new FrameStatisticsPage( "Frame Statistics", "FrameStatisticsPage.gui" ) );
			pages.Add( new SettngsPage( "Settings", "SettingsPage.gui" ) );
			pages.Add( new FullScreenEffectsPage( "Full-Screen Effects", "FullScreenEffectsPage.gui" ) );
			pages.Add( new TimePage( "Time Management", "TimeManagementPage.gui" ) );
			pages.Add( new RenderingSystemPage( "Rendering System", "RenderingSystemPage.gui" ) );
			pages.Add( new PhysicsSystemPage( "Physics System", "PhysicsSystemPage.gui" ) );
			pages.Add( new SoundSystemPage( "Sound System", "SoundSystemPage.gui" ) );
			pages.Add( new EntitySystemPage( "Entity System", "EntitySystemPage.gui" ) );
			pages.Add( new MemoryManagementPage( "Memory Management", "MemoryManagementPage.gui" ) );
			pages.Add( new DLLPage( "Assemblies and DLLs", "DLLPage.gui" ) );
			pages.Add( new NetworkingPage( "Networking", "NetworkingPage.gui" ) );
			pages.Add( new InputDevicesPage( "Input Devices", "InputDevicesPage.gui" ) );
			pages.Add( new FileSystemPage( "File System", "FileSystemPage.gui" ) );
			pages.Add( new ProjectSpecificPage( "Project Specific", "ProjectSpecificPage.gui" ) );

			//update pageSelectionComboBox
			foreach( Page page in pages )
				pageSelectionComboBox.Items.Add( page.Caption );
			if( lastPageSelectionIndex >= 0 && lastPageSelectionIndex < pages.Count )
				pageSelectionComboBox.SelectedIndex = lastPageSelectionIndex;
			else
				pageSelectionComboBox.SelectedIndex = 0;

			UpdateColorMultiplier();
		}

		protected override void OnDetach()
		{
			foreach( Page page in pages )
				page.OnDestroy();
			pages.Clear();

			instance = null;

			base.OnDetach();
		}

		void Background_CheckedChange( CheckBox sender )
		{
			background = backgroundCheckBox.Checked;
			UpdateColorMultiplier();
		}

		void ChangePage( Page page )
		{
			if( activePage == page )
				return;

			//hide old page
			if( activePage != null )
			{
				if( activePage.PageControl != null )
					activePage.PageControl.Visible = false;
			}

			activePage = page;

			//load .gui file
			if( activePage.PageControl == null )
			{
				string path = Path.Combine( "GUI\\ProfilingTool", activePage.FileName );
				Control control = ControlDeclarationManager.Instance.CreateControl( path );
				activePage.PageControl = control;
				pageAreaControl.Controls.Add( control );
				activePage.OnInit();
			}

			//show page
			if( activePage.PageControl != null )
				activePage.PageControl.Visible = true;

			window.Controls[ "PageNumber" ].Text = string.Format( "{0}/{1}",
				pageSelectionComboBox.SelectedIndex + 1, pageSelectionComboBox.Items.Count );

			lastPageSelectionIndex = pageSelectionComboBox.SelectedIndex;
		}

		void pageSelectionComboBox_SelectedIndexChange( ComboBox sender )
		{
			if( pageSelectionComboBox.SelectedIndex != -1 )
				ChangePage( pages[ pageSelectionComboBox.SelectedIndex ] );
		}

		void pagePreviousButton_Click( Button sender )
		{
			if( pageSelectionComboBox.SelectedIndex > 0 )
				pageSelectionComboBox.SelectedIndex = pageSelectionComboBox.SelectedIndex - 1;
		}

		void pageNextButton_Click( Button sender )
		{
			if( pageSelectionComboBox.SelectedIndex < pages.Count - 1 )
				pageSelectionComboBox.SelectedIndex = pageSelectionComboBox.SelectedIndex + 1;
		}

		void UpdateColorMultiplier()
		{
			if( background )
				ColorMultiplier = new ColorValue( 1, 1, 1, .5f );
			else
				ColorMultiplier = new ColorValue( 1, 1, 1 );
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.B )
			{
				backgroundCheckBox.Checked = !backgroundCheckBox.Checked;
				return true;
			}

			return false;
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( pagePreviousButton != null )
				pagePreviousButton.Enable = activePage != null && pages.IndexOf( activePage ) > 0;
			if( pageNextButton != null )
				pageNextButton.Enable = activePage != null && pages.IndexOf( activePage ) < pages.Count - 1;

			if( activePage != null )
				activePage.OnUpdate();
		}

		///////////////////////////////////////////

		abstract class Page
		{
			string caption;
			string fileName;
			Control pageControl;

			public Page( string caption, string fileName )
			{
				this.caption = caption;
				this.fileName = fileName;
			}

			public string Caption
			{
				get { return caption; }
			}

			public string FileName
			{
				get { return fileName; }
			}

			public Control PageControl
			{
				get { return pageControl; }
				set { pageControl = value; }
			}

			public virtual void OnInit() { }
			public virtual void OnUpdate() { }
			public virtual void OnDestroy() { }
		}

		///////////////////////////////////////////

		class FrameStatisticsPage : Page
		{
			public FrameStatisticsPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				int totalTriangles;
				int totalBatches;
				int totalLights;
				List<RenderStatisticsInfo.CameraInfo> cameras = new List<RenderStatisticsInfo.CameraInfo>();
				{
					IList<RenderStatisticsInfo.CameraInfo> allCameras;
					RendererWorld.Instance.Statistics.GetLastFrameStatistics(
						out totalTriangles, out totalBatches, out totalLights, out allCameras );

					//remove cameras without any geometry rendered
					foreach( RenderStatisticsInfo.CameraInfo cameraInfo in allCameras )
					{
						if( cameraInfo.Triangles != 0 && cameraInfo.Batches != 0 )
							cameras.Add( cameraInfo );
					}
				}

				PageControl.Controls[ "Triangles" ].Text = totalTriangles.ToString( "N0" );
				PageControl.Controls[ "Batches" ].Text = totalBatches.ToString( "N0" );
				PageControl.Controls[ "Lights" ].Text = totalLights.ToString( "N0" );

				//performance counter
				{
					float otherTime = 0;

					foreach( PerformanceCounter.Counter counter in PerformanceCounter.Counters )
					{
						PerformanceCounter.TimeCounter timeCounter = counter as PerformanceCounter.TimeCounter;

						if( timeCounter != null )
						{
							string counterNameWithoutSpaces = counter.Name.Replace( " ", "" );

							Control timeControl = PageControl.Controls[ counterNameWithoutSpaces + "Time" ];
							Control fpsControl = PageControl.Controls[ counterNameWithoutSpaces + "FPS" ];

							if( timeControl != null )
							{
								timeControl.Text = ( timeCounter.CalculatedValue * 1000.0f ).
									ToString( "F2" );
							}
							if( fpsControl != null )
								fpsControl.Text = ( 1.0f / timeCounter.CalculatedValue ).ToString( "F1" );

							if( !counter.InnerCounter )
							{
								if( counter == PerformanceCounter.TotalTimeCounter )
									otherTime += timeCounter.CalculatedValue;
								else
									otherTime -= timeCounter.CalculatedValue;
							}
						}
					}

					{
						TextBox timeControl = PageControl.Controls[ "OtherTime" ] as TextBox;
						TextBox fpsControl = PageControl.Controls[ "OtherFPS" ] as TextBox;

						if( timeControl != null )
							timeControl.Text = ( otherTime * 1000.0f ).ToString( "F2" );
						if( fpsControl != null )
							fpsControl.Text = ( 1.0f / otherTime ).ToString( "F1" );
					}
				}

				//cameras
				{
					ListBox camerasListBox = (ListBox)PageControl.Controls[ "Cameras" ];

					//update cameras list
					{
						List<string> newList = new List<string>();
						{
							foreach( RenderStatisticsInfo.CameraInfo cameraInfo in cameras )
							{
								string cameraInformation = string.Format( "{0}, {1}",
									cameraInfo.CameraName, cameraInfo.CameraPurpose );
								if( cameraInfo.CameraPurpose == Camera.Purposes.Compositor &&
									cameraInfo.CompositorName != "" )
								{
									cameraInformation += string.Format( " ({0}, {1})",
										cameraInfo.CompositorName, cameraInfo.CompositorPassName );
								}

								newList.Add( cameraInformation );
							}
							newList.Add( "Total " + newList.Count.ToString() );
						}

						bool needUpdate = false;
						{
							if( camerasListBox.Items.Count == newList.Count )
							{
								for( int n = 0; n < camerasListBox.Items.Count; n++ )
								{
									if( (string)camerasListBox.Items[ n ] != newList[ n ] )
									{
										needUpdate = true;
										break;
									}
								}
							}
							else
								needUpdate = true;
						}

						if( needUpdate )
						{
							string lastSelectedString = "";
							int lastSelectedEqualIndex = 0;
							if( camerasListBox.SelectedIndex != -1 )
							{
								lastSelectedString = (string)camerasListBox.SelectedItem;

								lastSelectedEqualIndex = -1;
								for( int n = 0; n <= camerasListBox.SelectedIndex; n++ )
								{
									if( (string)camerasListBox.Items[ n ] == lastSelectedString )
										lastSelectedEqualIndex++;
								}
							}

							camerasListBox.Items.Clear();
							foreach( string item in newList )
								camerasListBox.Items.Add( item );

							if( lastSelectedString != "" )
							{
								int skipCounter = 0;
								for( int n = 0; n < camerasListBox.Items.Count; n++ )
								{
									if( (string)camerasListBox.Items[ n ] == lastSelectedString )
									{
										if( lastSelectedEqualIndex == skipCounter )
										{
											camerasListBox.SelectedIndex = n;
											break;
										}
										skipCounter++;
									}
								}
							}

							if( camerasListBox.SelectedIndex == -1 )
								camerasListBox.SelectedIndex = camerasListBox.Items.Count - 1;
						}
					}

					//update camera info
					if( camerasListBox.SelectedIndex == camerasListBox.Items.Count - 1 )
					{
						//total statistics

						int staticMeshObjects = 0;
						int sceneNodes = 0;
						int guiRenderers = 0;
						int guiBatches = 0;
						int triangles = 0;
						int batches = 0;
						int lights = 0;

						foreach( RenderStatisticsInfo.CameraInfo cameraInfo in cameras )
						{
							staticMeshObjects += cameraInfo.StaticMeshObjects;
							sceneNodes += cameraInfo.SceneNodes;
							guiRenderers += cameraInfo.GuiRenderers;
							guiBatches += cameraInfo.GuiBatches;
							triangles += cameraInfo.Triangles;
							batches += cameraInfo.Batches;
							lights += cameraInfo.Lights;
						}

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text =
							staticMeshObjects.ToString( "N0" );
						PageControl.Controls[ "CameraSceneNodes" ].Text = sceneNodes.ToString( "N0" );
						PageControl.Controls[ "CameraGuiRenderers" ].Text = guiRenderers.ToString( "N0" );
						PageControl.Controls[ "CameraGuiBatches" ].Text = guiBatches.ToString( "N0" );
						PageControl.Controls[ "CameraTriangles" ].Text = triangles.ToString( "N0" );
						PageControl.Controls[ "CameraBatches" ].Text = batches.ToString( "N0" );
						PageControl.Controls[ "CameraLights" ].Text = lights.ToString( "N0" );

						PageControl.Controls[ "CameraOutdoorWalks" ].Text = "";
						PageControl.Controls[ "CameraPortalsPassed" ].Text = "";
						PageControl.Controls[ "CameraZonesPassed" ].Text = "";
					}
					else if( camerasListBox.SelectedIndex != -1 )
					{
						//selected camera statistics

						RenderStatisticsInfo.CameraInfo activeCameraInfo = cameras[ camerasListBox.SelectedIndex ];

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text =
							activeCameraInfo.StaticMeshObjects.ToString( "N0" );
						PageControl.Controls[ "CameraSceneNodes" ].Text =
							activeCameraInfo.SceneNodes.ToString( "N0" );
						PageControl.Controls[ "CameraGuiRenderers" ].Text =
							activeCameraInfo.GuiRenderers.ToString( "N0" );
						PageControl.Controls[ "CameraGuiBatches" ].Text =
							activeCameraInfo.GuiBatches.ToString( "N0" );
						PageControl.Controls[ "CameraTriangles" ].Text =
							activeCameraInfo.Triangles.ToString( "N0" );
						PageControl.Controls[ "CameraBatches" ].Text =
							activeCameraInfo.Batches.ToString( "N0" );
						PageControl.Controls[ "CameraLights" ].Text =
							activeCameraInfo.Lights.ToString( "N0" );

						if( activeCameraInfo.PortalSystemEnabled )
						{
							PageControl.Controls[ "CameraOutdoorWalks" ].Text =
								activeCameraInfo.PortalSystemOutdoorWalks.ToString( "N0" );
							PageControl.Controls[ "CameraPortalsPassed" ].Text =
								activeCameraInfo.PortalSystemPortalsPassed.ToString( "N0" );
							PageControl.Controls[ "CameraZonesPassed" ].Text =
								activeCameraInfo.PortalSystemZonesPassed.ToString( "N0" );
						}
						else
						{
							PageControl.Controls[ "CameraOutdoorWalks" ].Text = "No zones";
							PageControl.Controls[ "CameraPortalsPassed" ].Text = "No zones";
							PageControl.Controls[ "CameraZonesPassed" ].Text = "No zones";
						}
					}
					else
					{
						//no camera selected

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text = "";
						PageControl.Controls[ "CameraSceneNodes" ].Text = "";
						PageControl.Controls[ "CameraGuiRenderers" ].Text = "";
						PageControl.Controls[ "CameraGuiBatches" ].Text = "";
						PageControl.Controls[ "CameraTriangles" ].Text = "";
						PageControl.Controls[ "CameraBatches" ].Text = "";
						PageControl.Controls[ "CameraLights" ].Text = "";

						PageControl.Controls[ "CameraOutdoorWalks" ].Text = "";
						PageControl.Controls[ "CameraPortalsPassed" ].Text = "";
						PageControl.Controls[ "CameraZonesPassed" ].Text = "";
					}
				}
			}
		}

		///////////////////////////////////////////

		class RenderingSystemPage : Page
		{
			public RenderingSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					RenderSystem.Instance.Name, RendererWorld.Instance.ImplementationComponent );

				PageControl.Controls[ "Device" ].Text = string.Format( "{0} ({1})",
					RenderSystem.Instance.GPUDescription, RenderSystem.Instance.GPUCodeName );

				string gpuSyntaxes = "";
				foreach( string gpuSyntax in GpuProgramManager.Instance.SupportedSyntaxes )
				{
					if( gpuSyntaxes != "" )
						gpuSyntaxes += " ";
					gpuSyntaxes += gpuSyntax;
				}
				PageControl.Controls[ "GPUSyntaxes" ].Text = gpuSyntaxes;

				Button button;
				button = (Button)PageControl.Controls[ "TextureList" ];
				button.Click += textureListButton_Click;
				button = (Button)PageControl.Controls[ "MeshList" ];
				button.Click += meshListButton_Click;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				//Textures
				{
					uint totalCount;
					uint totalSize;
					uint loadedCount;
					uint loadedSize;
					uint compressedLoadedCount;
					uint compressedLoadedSize;
					uint uncompressedLoadedCount;
					uint uncompressedLoadedSize;
					uint manuallyCreatedCount;
					uint manuallyCreatedSize;
					uint renderTargetCount;
					uint renderTargetSize;

					TextureManager.Instance.GetStatistics(
						out totalCount, out totalSize,
						out loadedCount, out loadedSize,
						out compressedLoadedCount, out compressedLoadedSize,
						out uncompressedLoadedCount, out uncompressedLoadedSize,
						out manuallyCreatedCount, out manuallyCreatedSize,
						out renderTargetCount, out renderTargetSize );

					PageControl.Controls[ "TexturesTotalCount" ].Text = totalCount.ToString();
					PageControl.Controls[ "TexturesTotalSize" ].Text = ( (double)totalSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesLoadedCount" ].Text = loadedCount.ToString();
					PageControl.Controls[ "TexturesLoadedSize" ].Text = ( (double)loadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesCompressedLoadedCount" ].Text = compressedLoadedCount.ToString();
					PageControl.Controls[ "TexturesCompressedLoadedSize" ].Text =
						( (double)compressedLoadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesUncompressedLoadedCount" ].Text = uncompressedLoadedCount.ToString();
					PageControl.Controls[ "TexturesUncompressedLoadedSize" ].Text =
						( (double)uncompressedLoadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesManuallyCreatedCount" ].Text = manuallyCreatedCount.ToString();
					PageControl.Controls[ "TexturesManuallyCreatedSize" ].Text =
						( (double)manuallyCreatedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesRenderTargetCount" ].Text = renderTargetCount.ToString();
					PageControl.Controls[ "TexturesRenderTargetSize" ].Text =
						( (double)renderTargetSize / 1024 / 1024 ).ToString( "F2" );
				}

				//Meshes
				{
					uint count;
					uint size;
					MeshManager.Instance.GetStatistics( out count, out size );

					PageControl.Controls[ "MeshesCount" ].Text = count.ToString();
					PageControl.Controls[ "MeshesSize" ].Text = ( (double)size / 1024 / 1024 ).ToString( "F2" );
				}

				//Fonts
				{
					int count = 0;
					int size = 0;

					foreach( Font font in FontManager.Instance.Fonts )
					{
						count++;
						foreach( Texture texture in font.GetAllLoadedTextures() )
							size += texture.GetSizeInBytes();
					}

					PageControl.Controls[ "FontsCount" ].Text = count.ToString();
					PageControl.Controls[ "FontsSize" ].Text =
						( (double)size / 1024 / 1024 ).ToString( "F2" );
				}

				//GPU programs
				{
					PageControl.Controls[ "GPUPrograms" ].Text =
						GpuProgramManager.Instance.Programs.Count.ToString();

					int generatedAtRuntime = 0;
					int loadedFromCache = 0;
					foreach( GpuProgram gpuProgram in GpuProgramManager.Instance.Programs )
					{
						if( gpuProgram.HasLoadedFromShaderCache() )
							loadedFromCache++;
						else
							generatedAtRuntime++;
					}

					PageControl.Controls[ "GPUProgramsGeneratedAtRuntime" ].Text = generatedAtRuntime.ToString( "N0" );
					PageControl.Controls[ "GPUProgramsLoadedFromCache" ].Text = loadedFromCache.ToString( "N0" );

					if( RenderSystem.Instance.HasShaderModel3() && loadedFromCache == 0 )
					{
						PageControl.Controls[ "GPUProgramsLoadedFromCache" ].ColorMultiplier = new ColorValue( 1, 0, 0 );
					}
				}

				PageControl.Controls[ "Materials" ].Text = MaterialManager.Instance.GetMaterialCount().ToString();
				PageControl.Controls[ "HighLevelMaterials" ].Text = HighLevelMaterialManager.Instance.Materials.Count.ToString();
				PageControl.Controls[ "SceneNodes" ].Text = SceneManager.Instance.SceneNodes.Count.ToString( "N0" );
				PageControl.Controls[ "StaticMeshObjects" ].Text = SceneManager.Instance.StaticMeshObjects.Count.ToString( "N0" );

				{
					int objectCount;
					Bounds octreeBounds;
					int octreeNodeCount;
					SceneManager.Instance.SceneGraph.GetStatistics( out objectCount, out octreeBounds, out octreeNodeCount );
					PageControl.Controls[ "SceneGraphObjects" ].Text = objectCount.ToString( "N0" );
					PageControl.Controls[ "SceneGraphNodes" ].Text = octreeNodeCount.ToString( "N0" );
				}

				//Shader cache
				{
					OnlyForLoadShaderCacheManager manager = RendererWorld.ShaderCacheManager as
						OnlyForLoadShaderCacheManager;
					if( manager != null )
					{
						PageControl.Controls[ "ShaderCacheFileName" ].Text = Path.GetFileName( manager.CacheFileName );
						PageControl.Controls[ "ShaderCacheProgramCount" ].Text = manager.ProgramCount.ToString( "N0" );
					}
					else
					{
						PageControl.Controls[ "ShaderCacheFileName" ].Text = "";
						PageControl.Controls[ "ShaderCacheProgramCount" ].Text = "";
					}
				}
			}

			void textureListButton_Click( Button sender )
			{
				Texture[] textures = TextureManager.Instance.GetAllTextures();
				ArrayUtils.SelectionSort( textures, delegate( Texture texture1, Texture texture2 )
				{
					return string.Compare( texture1.Name, texture2.Name );
				} );

				Log.Info( "List of textures in the memory:" );
				foreach( Texture texture in textures )
					Log.Info( "{0}, {1}x{2}, {3}", texture.Name, texture.Size.X, texture.Size.Y, texture.Format );
				Log.Info( "TOTAL: {0}", textures.Length );

				if( EngineConsole.Instance != null )
				{
					EngineConsole.Instance.Active = true;
					EngineConsole.Instance.ScrollDown();
				}
			}

			void meshListButton_Click( Button sender )
			{
				List<Mesh> meshes = new List<Mesh>();
				foreach( Mesh mesh in MeshManager.Instance.LoadedMeshes )
					meshes.Add( mesh );
				foreach( Mesh mesh in MeshManager.Instance.CreatedMeshes )
					meshes.Add( mesh );
				ListUtils.SelectionSort( meshes, delegate( Mesh mesh1, Mesh mesh2 )
				{
					return string.Compare( mesh1.Name, mesh2.Name );
				} );

				Log.Info( "List of meshes in the memory:" );
				foreach( Mesh mesh in meshes )
					Log.Info( mesh.Name );
				Log.Info( "TOTAL: {0}", meshes.Count );

				if( EngineConsole.Instance != null )
				{
					EngineConsole.Instance.Active = true;
					EngineConsole.Instance.ScrollDown();
				}
			}
		}

		/////////////////////////////////////////////

		class PhysicsSystemPage : Page
		{
			long lastRayCasts = -1;
			long lastVolumeCasts = 0;
			long lastCreatedBodies = 0;
			long lastPushedToWorldBodies = 0;

			public PhysicsSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					PhysicsWorld.Instance.DriverName, PhysicsWorld.Instance.ImplementationComponent );

				//CheckBox hardwareAccelecatedCheckBox = (CheckBox)PageControl.Controls[
				//   "HardwareAccelerated" ];
				//hardwareAccelecatedCheckBox.Enable = false;
				//hardwareAccelecatedCheckBox.Checked = PhysicsWorld.Instance.HardwareAccelerated;
			}

			ICarInfo FindCar()
			{
				if( Map.Instance != null )
				{
					foreach( Entity entity in Map.Instance.Children )
					{
						ICarInfo carInfo = entity as ICarInfo;
						if( carInfo != null )
							return carInfo;
					}
				}
				return null;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				int scenes = 0;
				int bodies = 0;
				int bodiesStatic = 0;
				int bodiesDynamic = 0;
				int bodiesActive = 0;
				int joints = 0;
				int motors = 0;
				long totalRayCasts = 0;
				long totalVolumeCasts = 0;
				long totalCreatedBodies = 0;
				long totalPushedToWorldBodies = 0;
				foreach( PhysicsScene scene in PhysicsWorld.Instance.Scenes )
				{
					scenes++;
					bodies += scene.Bodies.Count;
					foreach( Body body in scene.Bodies )
					{
						if( !body.Static )
						{
							bodiesDynamic++;
							if( !body.Sleeping )
								bodiesActive++;
						}
						else
							bodiesStatic++;
					}
					joints += scene.Joints.Count;
					motors += scene.Motors.Count;

					long rayCasts;
					long volumeCasts;
					long createdBodies;
					long pushedToWorldBodies;
					scene.GetStatistics( out rayCasts, out volumeCasts, out createdBodies, out pushedToWorldBodies );
					unchecked
					{
						totalRayCasts += rayCasts;
						totalVolumeCasts += volumeCasts;
						totalCreatedBodies += createdBodies;
						totalPushedToWorldBodies += pushedToWorldBodies;
					}
				}

				if( lastRayCasts == -1 )
				{
					lastRayCasts = totalRayCasts;
					lastVolumeCasts = totalVolumeCasts;
					lastCreatedBodies = totalCreatedBodies;
					lastPushedToWorldBodies = totalPushedToWorldBodies;
				}

				string carInfo;
				{
					ICarInfo iCarInfo = FindCar();
					if( iCarInfo != null )
						carInfo = iCarInfo.GetInfoForProfilingTool();
					else
						carInfo = "No car instance";
				}

				PageControl.Controls[ "Scenes" ].Text = scenes.ToString( "N0" );
				PageControl.Controls[ "Bodies" ].Text = bodies.ToString( "N0" );
				PageControl.Controls[ "BodiesStatic" ].Text = bodiesStatic.ToString( "N0" );
				PageControl.Controls[ "BodiesDynamic" ].Text = bodiesDynamic.ToString( "N0" );
				PageControl.Controls[ "BodiesActive" ].Text = bodiesActive.ToString( "N0" );
				PageControl.Controls[ "Joints" ].Text = joints.ToString( "N0" );
				PageControl.Controls[ "Motors" ].Text = motors.ToString( "N0" );

				PageControl.Controls[ "RayCastsLastFrame" ].Text = ( totalRayCasts - lastRayCasts ).ToString( "N0" );
				PageControl.Controls[ "VolumeCastsLastFrame" ].Text = ( totalVolumeCasts - lastVolumeCasts ).ToString( "N0" );
				PageControl.Controls[ "CreatedBodiesLastFrame" ].Text = ( totalCreatedBodies - lastCreatedBodies ).ToString( "N0" );
				PageControl.Controls[ "PushedToWorldBodiesLastFrame" ].Text =
					( totalPushedToWorldBodies - lastPushedToWorldBodies ).ToString( "N0" );
				PageControl.Controls[ "RayCastsTotal" ].Text = totalRayCasts.ToString( "N0" );
				PageControl.Controls[ "VolumeCastsTotal" ].Text = totalVolumeCasts.ToString( "N0" );
				PageControl.Controls[ "CreatedBodiesTotal" ].Text = totalCreatedBodies.ToString( "N0" );
				PageControl.Controls[ "PushedToWorldBodiesTotal" ].Text = totalPushedToWorldBodies.ToString( "N0" );

				PageControl.Controls[ "Car" ].Text = carInfo;

				lastRayCasts = totalRayCasts;
				lastVolumeCasts = totalVolumeCasts;
				lastCreatedBodies = totalCreatedBodies;
				lastPushedToWorldBodies = totalPushedToWorldBodies;
			}
		}

		/////////////////////////////////////////////

		class SoundSystemPage : Page
		{
			public SoundSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				ComboBox comboBox;

				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					SoundWorld.Instance.DriverName, SoundWorld.Instance.ImplementationComponent );

				comboBox = (ComboBox)PageControl.Controls[ "InformationType" ];
				comboBox.Items.Add( "Loaded sounds" );
				comboBox.Items.Add( "Virtual channels" );
				comboBox.Items.Add( "Real channels" );
				comboBox.SelectedIndex = 0;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				int informationTypeIndex = ( (ComboBox)PageControl.Controls[ "InformationType" ] ).
					SelectedIndex;

				StringBuilder text = new StringBuilder( "" );

				if( informationTypeIndex == 0 )
				{
					//loaded sounds

					text.AppendFormat( "Count: {0}\n", SoundWorld.Instance.Sounds.Count );
					text.Append( "\n" );

					foreach( Sound sound in SoundWorld.Instance.Sounds )
						text.AppendFormat( "{0}\n", sound );
				}
				else if( informationTypeIndex == 1 )
				{
					//virtual channels

					int activeChannelCount = SoundWorld.Instance.ActiveVirtual2DChannels.Count +
						SoundWorld.Instance.ActiveVirtual3DChannels.Count;

					text.AppendFormat( "Active channels: {0}\n", activeChannelCount );
					text.Append( "\n" );

					for( int nChannels = 0; nChannels < 2; nChannels++ )
					{
						IEnumerable<VirtualChannel> activeChannels = nChannels == 0 ?
							SoundWorld.Instance.ActiveVirtual2DChannels :
							SoundWorld.Instance.ActiveVirtual3DChannels;

						foreach( VirtualChannel virtualChannel in activeChannels )
						{
							if( virtualChannel.CurrentRealChannel != null )
								text.Append( "Real - " );
							else
								text.Append( "Virtual - " );

							string soundName;

							if( virtualChannel.CurrentSound.Name != null )
								soundName = virtualChannel.CurrentSound.Name;
							else
								soundName = "DataBuffer";
							text.AppendFormat( "{0}  Volume {1}\n", soundName,
								virtualChannel.GetTotalVolume().ToString( "F3" ) );
						}
					}
				}
				else
				{
					//real channels

					int freeCount = 0;
					int activeCount = 0;

					for( int nRealChannels = 0; nRealChannels < 2; nRealChannels++ )
					{
						IEnumerable<RealChannel> realChannels = nRealChannels == 0 ?
							SoundWorld.Instance.Real2DChannels : SoundWorld.Instance.Real3DChannels;
						foreach( RealChannel realChannel in realChannels )
						{
							if( realChannel.CurrentVirtualChannel == null )
								freeCount++;
							else
								activeCount++;
						}
					}

					text.AppendFormat( "Free channels: {0}\n", freeCount );
					text.AppendFormat( "Active channels: {0}\n", activeCount );
					text.Append( "\n" );

					bool last3d = false;

					for( int nRealChannels = 0; nRealChannels < 2; nRealChannels++ )
					{
						IList<RealChannel> realChannels = nRealChannels == 0 ?
							SoundWorld.Instance.Real2DChannels : SoundWorld.Instance.Real3DChannels;

						int lastActiveIndex = 0;
						{
							for( int nChannel = 0; nChannel < realChannels.Count; nChannel++ )
							{
								if( realChannels[ nChannel ].CurrentVirtualChannel != null )
									lastActiveIndex = nChannel;
							}
						}

						for( int nChannel = 0; nChannel < realChannels.Count; nChannel++ )
						{
							if( nChannel > lastActiveIndex )
								break;

							RealChannel realChannel = realChannels[ nChannel ];

							if( !last3d && realChannel.Is3D )
							{
								last3d = true;
								text.Append( "\n" );
							}

							text.AppendFormat( "{0}: ", realChannel.Is3D ? "3D" : "2D" );

							VirtualChannel virtualChannel = realChannel.CurrentVirtualChannel;
							if( virtualChannel != null )
							{
								string soundName;
								if( virtualChannel.CurrentSound.Name != null )
									soundName = virtualChannel.CurrentSound.Name;
								else
									soundName = "DataBuffer";
								text.AppendFormat( "{0} - Volume: {1}", soundName, virtualChannel.GetTotalVolume().ToString( "F2" ) );
								if( nRealChannels == 1 )
									text.AppendFormat( ", Rolloff: {0}", virtualChannel.GetRolloffFactor().ToString( "F2" ) );
							}
							else
								text.Append( "Free" );
							text.Append( "\n" );
						}
					}
				}

				PageControl.Controls[ "Information" ].Text = text.ToString();
			}
		}

		/////////////////////////////////////////////

		class EntitySystemPage : Page
		{
			public EntitySystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				if( Entities.Instance != null )
				{
					PageControl.Controls[ "EntityClasses" ].Text =
						EntityTypes.Instance.Classes.Count.ToString( "N0" );
					PageControl.Controls[ "EntityTypes" ].Text =
						EntityTypes.Instance.Types.Count.ToString( "N0" );
					PageControl.Controls[ "Entities" ].Text =
						Entities.Instance.EntitiesCollection.Count.ToString( "N0" );
					PageControl.Controls[ "SubscribedToTickEvents" ].Text =
						Entities.Instance.GetAmountOfEntitiesSubscribedToOnTick().ToString( "N0" );
				}
				else
				{
					PageControl.Controls[ "EntityClasses" ].Text = "";
					PageControl.Controls[ "EntityTypes" ].Text = "";
					PageControl.Controls[ "Entities" ].Text = "";
					PageControl.Controls[ "SubscribedToTickEvents" ].Text = "";
					PageControl.Controls[ "SceneGraphObjects" ].Text = "";
					PageControl.Controls[ "SceneGraphNodes" ].Text = "";
				}

				if( Map.Instance != null && Map.Instance.SceneGraph != null )
				{
					int objectCount;
					Bounds octreeBounds;
					int octreeNodeCount;
					Map.Instance.SceneGraph.GetStatistics( out objectCount, out octreeBounds, out octreeNodeCount );
					PageControl.Controls[ "SceneGraphObjects" ].Text = objectCount.ToString( "N0" );
					PageControl.Controls[ "SceneGraphNodes" ].Text = octreeNodeCount.ToString( "N0" );
				}
				else
				{
					PageControl.Controls[ "SceneGraphObjects" ].Text = "";
					PageControl.Controls[ "SceneGraphNodes" ].Text = "";
				}
			}
		}

		/////////////////////////////////////////////

		class MemoryManagementPage : Page
		{
			public MemoryManagementPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				PageControl.Controls[ "NetMemory" ].Text = GC.GetTotalMemory( false ).ToString( "N0" );

				int totalAllocatedMemory = 0;
				int totalAllocationCount = 0;

				for( int n = 0; n < (int)NativeMemoryAllocationType.Count; n++ )
				{
					NativeMemoryAllocationType allocationType = (NativeMemoryAllocationType)n;

					int allocatedMemory;
					int allocationCount;
					NativeMemoryManager.GetStatistics( allocationType, out allocatedMemory,
						out allocationCount );

					string typeString = allocationType.ToString();

					PageControl.Controls[ typeString + "Allocations" ].Text =
						allocationCount.ToString( "N0" );
					PageControl.Controls[ typeString + "Memory" ].Text =
						allocatedMemory.ToString( "N0" );

					totalAllocatedMemory += allocatedMemory;
					totalAllocationCount += allocationCount;
				}

				PageControl.Controls[ "TotalAllocations" ].Text = totalAllocationCount.ToString( "N0" );
				PageControl.Controls[ "TotalMemory" ].Text = totalAllocatedMemory.ToString( "N0" );

				int crtAllocatedMemory;
				int crtAllocationCount;
				NativeMemoryManager.GetCRTStatistics( out crtAllocatedMemory, out crtAllocationCount );

				PageControl.Controls[ "CRTAllocations" ].Text = crtAllocationCount.ToString( "N0" );
				PageControl.Controls[ "CRTMemory" ].Text = crtAllocatedMemory.ToString( "N0" );
			}
		}

		/////////////////////////////////////////////

		class DLLPage : Page
		{
			bool updated;
			ComboBox comboBox;

			//

			public DLLPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				comboBox = (ComboBox)PageControl.Controls[ "InformationType" ];
				comboBox.Items.Add( "Managed assemblies" );
				comboBox.Items.Add( "DLLs" );
				comboBox.SelectedIndex = 0;
				comboBox.SelectedIndexChange += ComboBox_SelectedIndexChange;
			}

			void ComboBox_SelectedIndexChange( ComboBox sender )
			{
				updated = false;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				if( updated )
					return;
				updated = true;

				int informationTypeIndex = ( (ComboBox)PageControl.Controls[ "InformationType" ] ).
					SelectedIndex;

				ListBox listBox = (ListBox)PageControl.Controls[ "Information" ];

				int lastSelectedIndex = listBox.SelectedIndex;

				listBox.Items.Clear();

				if( informationTypeIndex == 0 )
				{
					//managed assemblies

					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

					List<AssemblyName> resultAssemblyNames = new List<AssemblyName>( assemblies.Length );
					{
						List<Assembly> remainingAssemblies = new List<Assembly>( assemblies );

						while( true )
						{
							Assembly notReferencedAssembly = null;
							{
								foreach( Assembly assembly in remainingAssemblies )
								{
									AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();

									foreach( Assembly a in remainingAssemblies )
									{
										if( assembly == a )
											continue;

										AssemblyName aName = a.GetName();

										foreach( AssemblyName referencedAssembly in referencedAssemblies )
										{
											if( referencedAssembly.Name == aName.Name )
												goto nextAssembly;
										}
									}

									notReferencedAssembly = assembly;
									break;

									nextAssembly: ;
								}
							}

							if( notReferencedAssembly != null )
							{
								remainingAssemblies.Remove( notReferencedAssembly );
								resultAssemblyNames.Add( notReferencedAssembly.GetName() );
							}
							else
							{
								//no exists not referenced assemblies
								foreach( Assembly assembly in remainingAssemblies )
									resultAssemblyNames.Add( assembly.GetName() );
								break;
							}
						}
					}

					foreach( AssemblyName assemblyName in resultAssemblyNames )
					{
						string text = string.Format( "{0}, {1}", assemblyName.Name,
							assemblyName.Version );
						listBox.Items.Add( text );
					}
				}
				else if( informationTypeIndex == 1 )
				{
					//dlls
					string[] names = EngineApp.Instance.GetNativeModuleNames();

					ArrayUtils.SelectionSort( names, delegate( string s1, string s2 )
					{
						return string.Compare( s1, s2, true );
					} );

					foreach( string name in names )
					{
						string text = string.Format( "{0} - {1}", Path.GetFileName( name ), name );
						listBox.Items.Add( text );
					}
				}

				if( lastSelectedIndex >= 0 && lastSelectedIndex < listBox.Items.Count )
					listBox.SelectedIndex = lastSelectedIndex;
				if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
					listBox.SelectedIndex = 0;
			}
		}

		/////////////////////////////////////////////

		class NetworkingPage : Page
		{
			public NetworkingPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			void GetConnectedNodeData( NetworkNode.ConnectedNode connectedNode, StringBuilder text )
			{
				if( connectedNode.Status == NetworkConnectionStatuses.Connected )
				{
					text.AppendFormat( "- Connection with {0}\n", connectedNode.RemoteEndPoint.Address );

					NetworkNode.ConnectedNode.StatisticsData statistics = connectedNode.Statistics;

					text.AppendFormat(
						"-   Send: Total: {0} kb, Speed: {1} b/s\n",
						statistics.GetBytesSent( true ) / 1024,
						(long)statistics.GetBytesSentPerSecond( true ) );

					text.AppendFormat(
						"-   Receive: Total: {0} kb, Speed: {1} b/s\n",
						statistics.GetBytesReceived( true ) / 1024,
						(long)statistics.GetBytesReceivedPerSecond( true ) );

					text.AppendFormat( "-   Ping: {0} ms\n",
						(int)( connectedNode.LastRoundtripTime * 1000 ) );
				}
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				StringBuilder text = new StringBuilder( "" );

				//GameNetworkServer
				GameNetworkServer server = GameNetworkServer.Instance;
				if( server != null )
				{
					text.Append( "Server:\n" );
					foreach( NetworkNode.ConnectedNode connectedNode in server.ConnectedNodes )
						GetConnectedNodeData( connectedNode, text );
					text.Append( "\n" );
				}

				//GameNetworkClient
				GameNetworkClient client = GameNetworkClient.Instance;
				if( client != null && client.Status == NetworkConnectionStatuses.Connected )
				{
					text.Append( "Client:\n" );
					GetConnectedNodeData( client.ServerConnectedNode, text );
					text.Append( "\n" );
				}

				if( text.Length == 0 )
					text.Append( "No connections" );

				//EntitySystem statistics
				if( EntitySystemWorld.Instance != null )
				{
					EntitySystemWorld.NetworkingInterface networkingInterface =
						EntitySystemWorld.Instance._GetNetworkingInterface();
					if( networkingInterface != null )
					{
						string[] lines = networkingInterface.GetStatisticsAsText();
						foreach( string line in lines )
							text.AppendFormat( "-   {0}\n", line );
					}
				}

				PageControl.Controls[ "Data" ].Text = text.ToString();
			}
		}

		/////////////////////////////////////////////

		class InputDevicesPage : Page
		{
			ColorValue disabledColor = new ColorValue( .15f, .15f, .35f );
			ColorValue enabledColor = new ColorValue( 1, 1, 1 );
			ColorValue pressedColor = new ColorValue( 0, 1, 0 );

			JoystickInputDevice selectedJoystick;

			////////////////////////////////////////

			class TestEffectAxisItem
			{
				public JoystickAxes[] axes;

				public TestEffectAxisItem( JoystickAxes[] axes )
				{
					this.axes = axes;
				}

				public override string ToString()
				{
					string text = "";
					foreach( JoystickAxes axis in axes )
					{
						if( text.Length != 0 )
							text += " and ";
						text += axis.ToString();
					}
					return text;
				}
			}

			////////////////////////////////////////

			public InputDevicesPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				//update device list
				{
					ComboBox devicesComboBox = (ComboBox)PageControl.Controls[ "Devices" ];

					devicesComboBox.Items.Clear();

					if( InputDeviceManager.Instance != null )
					{
						foreach( InputDevice device in InputDeviceManager.Instance.Devices )
						{
							JoystickInputDevice joystick = device as JoystickInputDevice;
							if( joystick != null )
								devicesComboBox.Items.Add( joystick );
						}
					}

					if( devicesComboBox.Items.Count > 0 )
					{
						devicesComboBox.SelectedIndexChange += devicesComboBox_SelectedIndexChange;
						devicesComboBox.SelectedIndex = 0;
					}
					else
					{
						devicesComboBox.Enable = false;
					}
				}

				//subscribe to button click events
				( (Button)PageControl.Controls[ "EffectDestroy" ] ).Click += EffectDestroy_Click;
				( (Button)PageControl.Controls[ "TestEffectCreate" ] ).Click += TestEffectCreate_Click;
			}

			void devicesComboBox_SelectedIndexChange( ComboBox sender )
			{
				selectedJoystick = sender.SelectedItem as JoystickInputDevice;

				//update TestEffectType combo box
				{
					ComboBox typeComboBox = (ComboBox)PageControl.Controls[ "TestEffectType" ];

					typeComboBox.Items.Clear();

					if( selectedJoystick != null && selectedJoystick.ForceFeedbackController != null )
					{
						int typeCount = Enum.GetValues( typeof( ForceFeedbackEffectTypes ) ).Length;
						for( int n = 0; n < typeCount; n++ )
						{
							ForceFeedbackEffectTypes effectType = (ForceFeedbackEffectTypes)n;
							if( selectedJoystick.ForceFeedbackController.IsEffectSupported( effectType ) )
							{
								typeComboBox.Items.Add( effectType );
								if( effectType == ForceFeedbackEffectTypes.ConstantForce )
									typeComboBox.SelectedIndex = typeComboBox.Items.Count - 1;
							}
						}
					}

					if( typeComboBox.Items.Count > 0 && typeComboBox.SelectedIndex == -1 )
						typeComboBox.SelectedIndex = 0;
				}

				//update TestEffectAxis combo box
				{
					ComboBox axisComboBox = (ComboBox)PageControl.Controls[ "TestEffectAxis" ];

					axisComboBox.Items.Clear();

					if( selectedJoystick != null && selectedJoystick.ForceFeedbackController != null )
					{
						bool existsX = false;
						bool existsY = false;

						for( int n = 0; n < selectedJoystick.Axes.Length; n++ )
						{
							JoystickInputDevice.Axis axis = selectedJoystick.Axes[ n ];

							axisComboBox.Items.Add( new TestEffectAxisItem( new JoystickAxes[] { axis.Name } ) );

							if( axis.Name == JoystickAxes.X )
								existsX = true;
							if( axis.Name == JoystickAxes.Y )
								existsY = true;
						}

						if( existsX && existsY )
						{
							axisComboBox.Items.Add( new TestEffectAxisItem(
								new JoystickAxes[] { JoystickAxes.X, JoystickAxes.Y } ) );
							axisComboBox.SelectedIndex = axisComboBox.Items.Count - 1;
						}
					}

					if( axisComboBox.Items.Count > 0 && axisComboBox.SelectedIndex == -1 )
						axisComboBox.SelectedIndex = 0;
				}
			}

			void UpdateInfoControls()
			{
				string text;
				if( selectedJoystick != null )
				{
					text = string.Format( "Buttons: {1}, Axes: {0}, POVs: {2}, Sliders: {3}\n",
						selectedJoystick.Buttons.Length, selectedJoystick.Axes.Length,
						selectedJoystick.POVs.Length, selectedJoystick.Sliders.Length );

					if( selectedJoystick.ForceFeedbackController != null )
						text += "Force feedback supported";
					else
						text += "Force feedback does not supported";
				}
				else
					text = "";

				PageControl.Controls[ "Info" ].ColorMultiplier =
					selectedJoystick != null ? enabledColor : disabledColor;
				PageControl.Controls[ "InfoValue" ].Text = text;
			}

			void UpdateButtonControls()
			{
				PageControl.Controls[ "Buttons" ].ColorMultiplier =
					( selectedJoystick != null && selectedJoystick.Buttons.Length > 0 ) ?
					enabledColor : disabledColor;

				for( int n = 0; n < 10; n++ )
				{
					string name = string.Format( "Button{0}Name", n + 1 );
					Control control = PageControl.Controls[ name ];

					ColorValue color;
					if( selectedJoystick != null && n < selectedJoystick.Buttons.Length )
						color = enabledColor;
					else
						color = disabledColor;

					control.ColorMultiplier = color;
				}

				for( int n = 0; n < 30; n++ )
				{
					ColorValue color;

					if( selectedJoystick != null && n < selectedJoystick.Buttons.Length )
					{
						if( selectedJoystick.Buttons[ n ].Pressed )
							color = pressedColor;
						else
							color = enabledColor;
					}
					else
					{
						color = disabledColor;
					}

					string name = string.Format( "Button{0}", n + 1 );
					Control control = PageControl.Controls[ name ];
					control.ColorMultiplier = color;
				}
			}

			void UpdatePOVControls()
			{
				PageControl.Controls[ "POVs" ].ColorMultiplier =
					( selectedJoystick != null && selectedJoystick.POVs.Length > 0 ) ?
					enabledColor : disabledColor;

				for( int n = 0; n < 3; n++ )
				{
					JoystickInputDevice.POV pov = null;
					if( selectedJoystick != null && n < selectedJoystick.POVs.Length )
						pov = selectedJoystick.POVs[ n ];

					string labelName = string.Format( "POV{0}Name", n + 1 );
					string name = string.Format( "POV{0}", n + 1 );
					Control labelControl = PageControl.Controls[ labelName ];
					Control control = PageControl.Controls[ name ];
					labelControl.ColorMultiplier = pov != null ? enabledColor : disabledColor;
					control.ColorMultiplier = pov != null ? enabledColor : disabledColor;

					//update POV buttons
					{
						JoystickPOVDirections[] directions = new JoystickPOVDirections[]
						{
							JoystickPOVDirections.North, 
							JoystickPOVDirections.South,
							JoystickPOVDirections.East, 
							JoystickPOVDirections.West
						};

						foreach( JoystickPOVDirections direction in directions )
						{
							bool pressed = pov != null && ( pov.Value & direction ) > 0;

							string buttonName = string.Format( "Button{0}", direction );
							Control buttonControl = control.Controls[ buttonName ];
							buttonControl.ColorMultiplier = pressed ? pressedColor : new ColorValue( 1, 1, 1 );
						}
					}
				}
			}

			void UpdateBarControlValue( Control control, float value )
			{
				Rect screenRectangle = control.GetScreenRectangle();
				Vec2 rectSize = screenRectangle.Size;
				float width = rectSize.X / 2 * Math.Abs( value );
				Rect rect;
				if( value > 0 )
				{
					float left = screenRectangle.Left + rectSize.X / 2;
					rect = new Rect( left, screenRectangle.Top, left + width, screenRectangle.Bottom );
				}
				else
				{
					float right = screenRectangle.Left + rectSize.X / 2;
					rect = new Rect( right - width, screenRectangle.Top, right, screenRectangle.Bottom );
				}

				Control barControl = control.Controls[ "Bar" ];
				barControl.SetScreenClipRectangle( rect );
			}

			void UpdateAxisControls()
			{
				PageControl.Controls[ "Axes" ].ColorMultiplier =
					( selectedJoystick != null && selectedJoystick.Axes.Length > 0 ) ?
					enabledColor : disabledColor;

				for( int n = 0; n < 6; n++ )
				{
					JoystickInputDevice.Axis axis = null;
					if( selectedJoystick != null && n < selectedJoystick.Axes.Length )
						axis = selectedJoystick.Axes[ n ];
					float value = axis != null ? axis.Value : 0;

					string labelName = string.Format( "Axis{0}Name", n + 1 );
					Control labelControl = PageControl.Controls[ labelName ];
					string name = string.Format( "Axis{0}", n + 1 );
					Control control = PageControl.Controls[ name ];

					ColorValue color = axis != null ? enabledColor : disabledColor;
					labelControl.ColorMultiplier = color;
					labelControl.Text = axis != null ? axis.Name.ToString() : "Not available";
					control.ColorMultiplier = color;
					UpdateBarControlValue( control, value );
				}
			}

			void UpdateSliderControls()
			{
				{
					ColorValue color = ( selectedJoystick != null && selectedJoystick.Sliders.Length > 0 ) ?
						enabledColor : disabledColor;
					PageControl.Controls[ "Sliders" ].ColorMultiplier = color;
					PageControl.Controls[ "SliderX" ].ColorMultiplier = color;
					PageControl.Controls[ "SliderY" ].ColorMultiplier = color;
				}

				for( int n = 0; n < 6; n++ )
				{
					JoystickInputDevice.Slider slider = null;
					if( selectedJoystick != null && n < selectedJoystick.Sliders.Length )
						slider = selectedJoystick.Sliders[ n ];
					Vec2 value = slider != null ? slider.Value : Vec2.Zero;

					string labelName = string.Format( "Slider{0}Name", n + 1 );
					string nameX = string.Format( "Slider{0}X", n + 1 );
					string nameY = string.Format( "Slider{0}Y", n + 1 );
					Control labelControl = PageControl.Controls[ labelName ];
					Control controlX = PageControl.Controls[ nameX ];
					Control controlY = PageControl.Controls[ nameY ];

					ColorValue color = slider != null ? enabledColor : disabledColor;
					labelControl.ColorMultiplier = color;
					controlX.ColorMultiplier = color;
					controlY.ColorMultiplier = color;
					UpdateBarControlValue( controlX, value.X );
					UpdateBarControlValue( controlY, value.Y );
				}
			}

			ForceFeedbackEffect GetSelectedEffect()
			{
				ListBox listBox = (ListBox)PageControl.Controls[ "EffectList" ];
				return listBox.SelectedItem as ForceFeedbackEffect;
			}

			void UpdateEffectList()
			{
				ListBox listBox = (ListBox)PageControl.Controls[ "EffectList" ];

				List<ForceFeedbackEffect> newList = new List<ForceFeedbackEffect>();
				{
					if( selectedJoystick != null && selectedJoystick.ForceFeedbackController != null )
					{
						foreach( ForceFeedbackEffect effect in selectedJoystick.ForceFeedbackController.Effects )
							newList.Add( effect );
					}
				}

				bool needUpdate = false;
				{
					if( newList.Count == listBox.Items.Count )
					{
						for( int n = 0; n < newList.Count; n++ )
						{
							if( newList[ n ] != listBox.Items[ n ] )
								needUpdate = true;
						}
					}
					else
						needUpdate = true;
				}

				if( needUpdate )
				{
					object lastSelectedItem = listBox.SelectedItem;

					listBox.Items.Clear();
					foreach( ForceFeedbackEffect effect in newList )
					{
						listBox.Items.Add( effect );
						if( effect == lastSelectedItem )
							listBox.SelectedIndex = listBox.Items.Count - 1;
					}

					if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
						listBox.SelectedIndex = 0;
				}
			}

			void UpdateSelectedEffectInfo()
			{
				StringBuilder text = new StringBuilder();

				ForceFeedbackEffect effect = GetSelectedEffect();
				if( effect != null )
				{
					text.Append( effect.ToString() );

					if( effect.Direction != null )
					{
						text.Append( "; Direction =" );
						foreach( float value in effect.Direction )
							text.AppendFormat( " {0}", value.ToString( "F2" ) );
					}

					text.AppendFormat( "; Duration = {0}", effect.Duration.ToString( "F2" ) );

					//ConstantForce type
					ForceFeedbackConstantForceEffect constantForce = effect as ForceFeedbackConstantForceEffect;
					if( constantForce != null )
					{
						text.AppendFormat( "; Magnitude = {0}", constantForce.Magnitude.ToString( "F2" ) );
					}

					//SawtoothDown, SawtoothUp, Sine, Square, Triangle types.
					ForceFeedbackPeriodicEffect periodic = effect as ForceFeedbackPeriodicEffect;
					if( periodic != null )
					{
						text.AppendFormat( "; Magnitude = {0}", periodic.Magnitude.ToString( "F2" ) );
						text.AppendFormat( "; Offset = {0}", periodic.Offset.ToString( "F2" ) );
						text.AppendFormat( "; Phase = {0}", periodic.Phase.ToString( "F2" ) );
						text.AppendFormat( "; Period = {0}", periodic.Period.ToString( "F2" ) );
					}

					//Spring, Damper, Friction, Inertia types.
					ForceFeedbackConditionEffect condition = effect as ForceFeedbackConditionEffect;
					if( condition != null )
					{
						text.AppendFormat( "; Offset = {0}", condition.Offset.ToString( "F2" ) );
						text.AppendFormat( "; PositiveSaturation = {0}", condition.PositiveSaturation.ToString( "F2" ) );
						text.AppendFormat( "; NegativeSaturation = {0}", condition.NegativeSaturation.ToString( "F2" ) );
						text.AppendFormat( "; PositiveCoefficient = {0}", condition.PositiveCoefficient.ToString( "F2" ) );
						text.AppendFormat( "; NegativeCoefficient = {0}", condition.NegativeCoefficient.ToString( "F2" ) );
						text.AppendFormat( "; DeadBand = {0}", condition.DeadBand.ToString( "F2" ) );
					}

					//Ramp type
					ForceFeedbackRampEffect ramp = effect as ForceFeedbackRampEffect;
					if( ramp != null )
					{
						text.AppendFormat( "; StartForce = {0}", ramp.StartForce.ToString( "F2" ) );
						text.AppendFormat( "; EndForce = {0}", ramp.EndForce.ToString( "F2" ) );
					}

				}

				PageControl.Controls[ "EffectInfo" ].Text = text.ToString();
			}

			void UpdateForceFeedbackControls()
			{
				UpdateEffectList();
				UpdateSelectedEffectInfo();

				bool availableForceFeedback = selectedJoystick != null &&
					selectedJoystick.ForceFeedbackController != null;

				ColorValue color = availableForceFeedback ? enabledColor : disabledColor;
				PageControl.Controls[ "ForceFeedbackEffects" ].ColorMultiplier = color;
				PageControl.Controls[ "EffectList" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectLabel" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectTypeLabel" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectAxisLabel" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectType" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectAxis" ].ColorMultiplier = color;
				PageControl.Controls[ "EffectInfo" ].ColorMultiplier = color;
				PageControl.Controls[ "EffectDestroy" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectCreate" ].ColorMultiplier = color;

				PageControl.Controls[ "EffectDestroy" ].Enable = availableForceFeedback &&
					GetSelectedEffect() != null;
				PageControl.Controls[ "TestEffectCreate" ].Enable = availableForceFeedback;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				UpdateInfoControls();
				UpdateButtonControls();
				UpdatePOVControls();
				UpdateAxisControls();
				UpdateSliderControls();
				UpdateForceFeedbackControls();
			}

			void EffectDestroy_Click( Button sender )
			{
				ForceFeedbackEffect effect = GetSelectedEffect();
				if( effect != null )
					effect.Destroy();
			}

			void TestEffectCreate_Click( Button sender )
			{
				if( selectedJoystick == null )
					return;
				if( selectedJoystick.ForceFeedbackController == null )
					return;

				ForceFeedbackEffectTypes effectType = ForceFeedbackEffectTypes.ConstantForce;
				JoystickAxes[] axes = null;
				{
					ComboBox typeComboBox = (ComboBox)PageControl.Controls[ "TestEffectType" ];
					ComboBox axisComboBox = (ComboBox)PageControl.Controls[ "TestEffectAxis" ];
					if( typeComboBox.SelectedIndex != -1 && axisComboBox.SelectedIndex != -1 )
					{
						effectType = (ForceFeedbackEffectTypes)typeComboBox.SelectedItem;
						axes = ( (TestEffectAxisItem)axisComboBox.SelectedItem ).axes;
					}
				}

				if( axes == null )
					return;

				//create effect
				ForceFeedbackEffect effect = selectedJoystick.ForceFeedbackController.CreateEffect( effectType, axes );
				if( effect != null )
				{
					//you can set duration in seconds.
					//effect.Duration = 3;

					if( axes.Length == 2 )
					{
						Radian angle = new Degree( 30 ).InRadians();
						float x = MathFunctions.Cos( angle );
						float y = MathFunctions.Sin( angle );
						effect.SetDirection( new float[] { x, y } );
					}

					//ConstantForce type
					ForceFeedbackConstantForceEffect constantForce = effect as ForceFeedbackConstantForceEffect;
					if( constantForce != null )
					{
						constantForce.Magnitude = 1;
					}

					//SawtoothDown, SawtoothUp, Sine, Square, Triangle types.
					ForceFeedbackPeriodicEffect periodic = effect as ForceFeedbackPeriodicEffect;
					if( periodic != null )
					{
						periodic.Magnitude = 1.0f;
						periodic.Offset = 0.0f;
						periodic.Phase = 0.5f;
						periodic.Period = 2;
					}

					//Spring, Damper, Friction, Inertia types.
					ForceFeedbackConditionEffect condition = effect as ForceFeedbackConditionEffect;
					if( condition != null )
					{
						condition.Offset = 0.5f;
						condition.PositiveSaturation = 1.0f;
						condition.NegativeSaturation = 1.0f;
						condition.PositiveCoefficient = 1.0f;
						condition.NegativeCoefficient = -1.0f;
						condition.DeadBand = 0.0f;
					}

					//Ramp type
					ForceFeedbackRampEffect ramp = effect as ForceFeedbackRampEffect;
					if( ramp != null )
					{
						ramp.StartForce = -1.0f;
						ramp.EndForce = 1.0f;
					}

					effect.Start();
				}
			}
		}

		/////////////////////////////////////////////

		class SettngsPage : Page
		{
			List<CheckBox> checkBoxes = new List<CheckBox>();
			bool disableChangeEvents;

			//

			public SettngsPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				Type type = typeof( EngineDebugSettings );
				InitCheckBox( "StaticPhysics", type.GetProperty( "DrawStaticPhysics" ) );
				InitCheckBox( "DynamicPhysics", type.GetProperty( "DrawDynamicPhysics" ) );
				InitCheckBox( "PhysicsRayVolumeChecks", type.GetProperty( "DrawPhysicsRayVolumeChecks" ) );
				InitCheckBox( "SceneGraphOfSceneObjects", type.GetProperty( "DrawSceneGraphOfSceneObjects" ) );
				InitCheckBox( "SceneGraphOfMapObjects", type.GetProperty( "DrawSceneGraphOfMapObjects" ) );
				InitCheckBox( "Regions", type.GetProperty( "DrawRegions" ) );
				InitCheckBox( "MapObjectBounds", type.GetProperty( "DrawMapObjectBounds" ) );
				InitCheckBox( "SceneNodeBounds", type.GetProperty( "DrawSceneNodeBounds" ) );
				InitCheckBox( "StaticMeshObjectBounds", type.GetProperty( "DrawStaticMeshObjectBounds" ) );
				InitCheckBox( "ZonesPortalsOccluders", type.GetProperty( "DrawZonesPortalsOccluders" ) );
				InitCheckBox( "FrustumTest", type.GetProperty( "FrustumTest" ) );
				InitCheckBox( "Lights", type.GetProperty( "DrawLights" ) );
				InitCheckBox( "StaticGeometry", type.GetProperty( "DrawStaticGeometry" ) );
				InitCheckBox( "Models", type.GetProperty( "DrawModels" ) );
				InitCheckBox( "Effects", type.GetProperty( "DrawEffects" ) );
				InitCheckBox( "Gui", type.GetProperty( "DrawGui" ) );
				InitCheckBox( "Wireframe", type.GetProperty( "DrawWireframe" ) );
				InitCheckBox( "FullScreenEffects", type.GetProperty( "DrawFullScreenEffects" ) );
				InitCheckBox( "GameSpecificDebugGeometry", type.GetProperty( "DrawGameSpecificDebugGeometry" ) );
				InitCheckBox( "DrawShadowDebugging", type.GetProperty( "DrawShadowDebugging" ) );

				( (Button)PageControl.Controls[ "Defaults" ] ).Click += Defaults_Click;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				disableChangeEvents = true;
				foreach( CheckBox checkBox in checkBoxes )
					UpdateCheckBox( checkBox );
				disableChangeEvents = false;
			}

			void Defaults_Click( Button sender )
			{
				foreach( Control control in PageControl.Controls )
				{
					CheckBox checkBox = control as CheckBox;
					if( checkBox == null )
						continue;

					PropertyInfo property = checkBox.UserData as PropertyInfo;
					if( property == null )
						continue;

					DefaultValueAttribute[] attributes = (DefaultValueAttribute[])property.
						GetCustomAttributes( typeof( DefaultValueAttribute ), true );

					if( attributes.Length == 0 )
						continue;

					checkBox.Checked = (bool)attributes[ 0 ].Value;
				}
			}

			void InitCheckBox( string name, PropertyInfo property )
			{
				CheckBox checkBox = (CheckBox)PageControl.Controls[ name ];
				checkBox.UserData = property;
				checkBox.Checked = (bool)property.GetValue( null, null );

				checkBox.CheckedChange += checkBox_CheckedChange;

				checkBoxes.Add( checkBox );
			}

			void checkBox_CheckedChange( CheckBox sender )
			{
				if( !disableChangeEvents )
				{
					PropertyInfo p = (PropertyInfo)sender.UserData;
					p.SetValue( null, !(bool)p.GetValue( null, null ), null );
				}
			}

			void UpdateCheckBox( CheckBox checkBox )
			{
				PropertyInfo property = (PropertyInfo)checkBox.UserData;

				checkBox.Checked = (bool)property.GetValue( null, null );
			}
		}

		///////////////////////////////////////////

		class TimePage : Page
		{
			static bool independentParameters;
			CheckBox independentParametersCheckBox;
			ScrollBar engineTimeScaleScrollBar;
			ScrollBar soundPitchScaleScrollBar;
			bool disableChangeEvents;

			//

			public TimePage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				//IndependentParameters
				independentParametersCheckBox = (CheckBox)PageControl.Controls[ "IndependentParameters" ];
				UpdateEngineTimeScaleValue();
				independentParametersCheckBox.Checked = independentParameters;
				independentParametersCheckBox.CheckedChange += independentParametersCheckBox_CheckedChange;

				//EngineTimeScale
				engineTimeScaleScrollBar = (ScrollBar)PageControl.Controls[ "EngineTimeScale" ];
				engineTimeScaleScrollBar.Value = EngineApp.Instance.TimeScale;
				UpdateEngineTimeScaleValue();
				engineTimeScaleScrollBar.ValueChange += engineTimeScaleScrollBar_ValueChange;

				//SoundPitchScale
				soundPitchScaleScrollBar = (ScrollBar)PageControl.Controls[ "SoundPitchScale" ];
				soundPitchScaleScrollBar.Value = SoundWorld.Instance.MasterChannelGroup.Pitch;
				UpdateSoundPitchScaleValue();
				soundPitchScaleScrollBar.ValueChange += soundPitchScaleScrollBar_ValueChange;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				disableChangeEvents = true;

				engineTimeScaleScrollBar.Value = EngineApp.Instance.TimeScale;
				UpdateEngineTimeScaleValue();
				soundPitchScaleScrollBar.Value = SoundWorld.Instance.MasterChannelGroup.Pitch;
				UpdateSoundPitchScaleValue();

				disableChangeEvents = false;
			}

			void independentParametersCheckBox_CheckedChange( CheckBox sender )
			{
				if( !independentParametersCheckBox.Checked )
					soundPitchScaleScrollBar.Value = engineTimeScaleScrollBar.Value;
				independentParameters = independentParametersCheckBox.Checked;
			}

			void engineTimeScaleScrollBar_ValueChange( ScrollBar sender )
			{
				if( !disableChangeEvents )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					EngineApp.Instance.TimeScale = value;
					UpdateEngineTimeScaleValue();

					if( !independentParametersCheckBox.Checked && soundPitchScaleScrollBar.Value != engineTimeScaleScrollBar.Value )
						soundPitchScaleScrollBar.Value = engineTimeScaleScrollBar.Value;
				}
			}

			void soundPitchScaleScrollBar_ValueChange( ScrollBar sender )
			{
				if( !disableChangeEvents )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					SoundWorld.Instance.MasterChannelGroup.Pitch = value;
					UpdateSoundPitchScaleValue();

					if( !independentParametersCheckBox.Checked && soundPitchScaleScrollBar.Value != engineTimeScaleScrollBar.Value )
						engineTimeScaleScrollBar.Value = soundPitchScaleScrollBar.Value;
				}
			}

			void UpdateEngineTimeScaleValue()
			{
				PageControl.Controls[ "EngineTimeScaleValue" ].Text =
					EngineApp.Instance.TimeScale.ToString( "F1" );
			}

			void UpdateSoundPitchScaleValue()
			{
				PageControl.Controls[ "SoundPitchScaleValue" ].Text =
					SoundWorld.Instance.MasterChannelGroup.Pitch.ToString( "F1" );
			}

		}

		/////////////////////////////////////////////

		class FullScreenEffectsPage : Page
		{
			Viewport viewport;

			ListBox listBoxAvailable;
			ListBox listBoxEnabled;

			CheckBox checkBoxManualControl;
			CheckBox checkBoxEnabled;
			ScrollBar[] scrollBarFloatParameters = new ScrollBar[ 11 ];
			CheckBox[] checkBoxBoolParameters = new CheckBox[ 11 ];

			bool disableChangeEvents;

			static bool manualControl;
			static string lastSelectedEffect = "";

			//

			public FullScreenEffectsPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			static int GetCompositorPriority( string compositorName )
			{
				Compositor compositor = CompositorManager.Instance.GetByName( compositorName );
				if( compositor != null )
					return compositor.Priority;
				return 100;
			}

			public override void OnInit()
			{
				base.OnInit();

				disableChangeEvents = true;

				viewport = RendererWorld.Instance.DefaultViewport;

				//checkBoxControlManually
				checkBoxManualControl = (CheckBox)PageControl.Controls[ "ManualControl" ];
				checkBoxManualControl.Checked = manualControl;
				checkBoxManualControl.Click += checkBoxManualControl_Click;

				//listBoxAvailable
				{
					listBoxAvailable = (ListBox)PageControl.Controls[ "List" ];

					List<string> names = new List<string>();
					foreach( Compositor compositor in CompositorManager.Instance.Compositors )
						names.Add( compositor.Name );
					ListUtils.SelectionSort( names, delegate( string name1, string name2 )
					{
						int priority1 = GetCompositorPriority( name1 );
						int priority2 = GetCompositorPriority( name2 );
						if( priority1 < priority2 )
							return -1;
						if( priority1 > priority2 )
							return 1;
						return 0;
					} );

					foreach( string name in names )
						listBoxAvailable.Items.Add( name );

					listBoxAvailable.SelectedIndexChange += listBox_SelectedIndexChange;

					for( int n = 0; n < listBoxAvailable.Items.Count; n++ )
					{
						Button itemButton = listBoxAvailable.ItemButtons[ n ];
						CheckBox checkBox = (CheckBox)itemButton.Controls[ "CheckBox" ];
						checkBox.Click += listBoxCheckBox_Click;
						checkBox.Enable = false;
					}

					if( listBoxAvailable.Items.Count > 0 )
						listBoxAvailable.SelectedIndex = 0;
				}

				//listBoxEnabled
				listBoxEnabled = (ListBox)PageControl.Controls[ "ListEnabled" ];

				//checkBoxEnabled
				checkBoxEnabled = (CheckBox)PageControl.Controls[ "Enabled" ];
				checkBoxEnabled.Click += checkBoxEnabled_Click;
				checkBoxEnabled.Enable = false;

				//scrollBarFloatParameters, checkBoxBoolParameters
				for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
				{
					scrollBarFloatParameters[ n ] =
						(ScrollBar)PageControl.Controls[ "FloatParameter" + n.ToString() ];
					scrollBarFloatParameters[ n ].ValueChange += floatParameter_ValueChange;
				}
				for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
				{
					checkBoxBoolParameters[ n ] =
						(CheckBox)PageControl.Controls[ "BoolParameter" + n.ToString() ];
					checkBoxBoolParameters[ n ].CheckedChange += boolParameter_CheckedChange;
				}

				//ApplyToTheScreenGUI
				{
					CheckBox checkBox = (CheckBox)PageControl.Controls[ "ApplyToTheScreenGUI" ];
					checkBox.Click += applyToTheScreenGUICheckBox_Click;
				}

				UpdateListBoxAvailable();
				UpdateApplyToTheScreenGUICheckBox();
				UpdateCurrentEffectControls();
				UpdateListBoxEnabled();

				if( !string.IsNullOrEmpty( lastSelectedEffect ) )
				{
					for( int n = 0; n < listBoxAvailable.Items.Count; n++ )
					{
						Button itemButton = listBoxAvailable.ItemButtons[ n ];
						string name = GetListCompositorItemName( (string)listBoxAvailable.Items[ n ] );
						if( lastSelectedEffect == name )
							listBoxAvailable.SelectedIndex = n;
					}
				}

				disableChangeEvents = false;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				disableChangeEvents = true;

				UpdateListBoxAvailable();
				UpdateApplyToTheScreenGUICheckBox();
				UpdateCurrentEffectControls();
				UpdateListBoxEnabled();

				disableChangeEvents = false;
			}

			string GetListCompositorItemName( string itemName )
			{
				return itemName.Split( new char[] { ' ' } )[ 0 ];
			}

			void checkBoxManualControl_Click( CheckBox sender )
			{
				manualControl = checkBoxManualControl.Checked;
				MapCompositorManager.PreventCompositorInstancesUpdate = manualControl;
			}

			void listBox_SelectedIndexChange( ListBox sender )
			{
				if( !disableChangeEvents )
				{
					disableChangeEvents = true;

					UpdateCurrentEffectControls();

					if( listBoxAvailable.SelectedItem != null )
						lastSelectedEffect = GetListCompositorItemName( listBoxAvailable.SelectedItem.ToString() );

					disableChangeEvents = false;
				}
			}

			void listBoxCheckBox_Click( CheckBox sender )
			{
				//set listBox current item
				for( int n = 0; n < listBoxAvailable.Items.Count; n++ )
				{
					Button itemButton = listBoxAvailable.ItemButtons[ n ];
					if( itemButton.Controls[ "CheckBox" ] == sender )
						listBoxAvailable.SelectedIndex = n;
				}
				checkBoxEnabled.Checked = sender.Checked;
				UpdateCurrentEffect( false );
			}

			void applyToTheScreenGUICheckBox_Click( CheckBox sender )
			{
				EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer = sender.Checked;
			}

			void checkBoxEnabled_Click( CheckBox sender )
			{
				if( listBoxAvailable.SelectedItem != null )
				{
					Button itemButton = listBoxAvailable.ItemButtons[ listBoxAvailable.SelectedIndex ];
					( (CheckBox)itemButton.Controls[ "CheckBox" ] ).Checked = sender.Checked;

					UpdateCurrentEffect( false );
				}
			}

			void floatParameter_ValueChange( ScrollBar sender )
			{
				PageControl.Controls[ sender.Name + "Value" ].Text = sender.Value.ToString( "F2" );
				if( !disableChangeEvents )
					UpdateCurrentEffect( true );
			}

			void boolParameter_CheckedChange( CheckBox sender )
			{
				if( !disableChangeEvents )
					UpdateCurrentEffect( true );
			}

			void UpdateListBoxAvailable()
			{
				for( int n = 0; n < listBoxAvailable.Items.Count; n++ )
				{
					Button itemButton = listBoxAvailable.ItemButtons[ n ];
					CheckBox checkBox = (CheckBox)itemButton.Controls[ "CheckBox" ];
					string name = GetListCompositorItemName( (string)listBoxAvailable.Items[ n ] );
					CompositorInstance compositorInstance = viewport.GetCompositorInstance( name );
					checkBox.Checked = compositorInstance != null && compositorInstance.Enabled;
					checkBox.Enable = manualControl;
				}
			}

			void UpdateListBoxEnabled()
			{
				List<string> list = new List<string>();
				foreach( CompositorInstance instance in viewport.CompositorInstances )
				{
					if( instance.Enabled )
						list.Add( instance.Compositor.Name );
				}

				bool update = false;
				{
					if( list.Count == listBoxEnabled.Items.Count )
					{
						for( int n = 0; n < list.Count; n++ )
						{
							if( list[ n ] != (string)listBoxEnabled.Items[ n ] )
							{
								update = true;
								break;
							}
						}
					}
					else
						update = true;
				}

				if( update )
				{
					listBoxEnabled.Items.Clear();
					foreach( string name in list )
						listBoxEnabled.Items.Add( name );
				}
			}

			void UpdateApplyToTheScreenGUICheckBox()
			{
				CheckBox checkBox = (CheckBox)PageControl.Controls[ "ApplyToTheScreenGUI" ];
				checkBox.Checked = EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer;
			}

			List<PropertyInfo> GetCompositorInstanceProperties( Type type )
			{
				List<PropertyInfo> list = new List<PropertyInfo>();

				foreach( PropertyInfo property in type.GetProperties(
					BindingFlags.Public | BindingFlags.Instance ) )
				{
					if( !property.CanWrite )
						continue;

					BrowsableAttribute[] browsableAttributes = (BrowsableAttribute[])property.
						GetCustomAttributes( typeof( BrowsableAttribute ), true );
					if( browsableAttributes.Length != 0 )
					{
						bool browsable = true;
						foreach( BrowsableAttribute browsableAttribute in browsableAttributes )
							if( !browsableAttribute.Browsable )
								browsable = false;
						if( !browsable )
							continue;
					}

					if( property.Name == "Enabled" )
						continue;

					list.Add( property );
				}

				return list;
			}

			void UpdateCurrentEffectControls()
			{
				bool[] usedFloats = new bool[ scrollBarFloatParameters.Length ];
				bool[] usedBools = new bool[ checkBoxBoolParameters.Length ];

				if( listBoxAvailable.SelectedItem != null )
				{
					string name = GetListCompositorItemName( listBoxAvailable.SelectedItem.ToString() );

					//Set effect name
					PageControl.Controls[ "Name" ].Text = name;

					//Update "Enabled" check box

					Button itemButton = listBoxAvailable.ItemButtons[ listBoxAvailable.SelectedIndex ];
					checkBoxEnabled.Checked = ( (CheckBox)itemButton.Controls[ "CheckBox" ] ).Checked;
					checkBoxEnabled.Enable = manualControl;

					//Add parameters

					CompositorInstance instance = viewport.GetCompositorInstance( name );
					if( instance != null )
					{
						List<PropertyInfo> properties = GetCompositorInstanceProperties( instance.GetType() );

						int paramCount = 0;

						foreach( PropertyInfo property in properties )
						{
							if( property.PropertyType == typeof( float ) )
							{
								//float

								if( paramCount + 1 <= scrollBarFloatParameters.Length )
								{
									Range range = new Range( 0, 1 );
									{
										EditorLimitsRangeAttribute[] attributes = (EditorLimitsRangeAttribute[])
											property.GetCustomAttributes( typeof( EditorLimitsRangeAttribute ), true );
										if( attributes.Length != 0 )
											range = attributes[ 0 ].LimitsRange;
									}

									PageControl.Controls[ string.Format( "FloatParameter{0}Text", paramCount ) ].Visible = true;
									PageControl.Controls[ string.Format( "FloatParameter{0}Text", paramCount ) ].Text = property.Name;
									PageControl.Controls[ string.Format( "FloatParameter{0}Value", paramCount ) ].Visible = true;
									scrollBarFloatParameters[ paramCount ].Visible = true;
									scrollBarFloatParameters[ paramCount ].ValueRange = range;
									scrollBarFloatParameters[ paramCount ].Value = (float)property.GetValue( instance, null );
									scrollBarFloatParameters[ paramCount ].Enable = manualControl;
									usedFloats[ paramCount ] = true;
									paramCount++;
								}
							}
							else if( property.PropertyType == typeof( Vec2 ) ||
								property.PropertyType == typeof( Range ) )
							{
								//Vec2, Range

								if( paramCount + 2 <= scrollBarFloatParameters.Length )
								{
									Range range = new Range( 0, 1 );
									{
										EditorLimitsRangeAttribute[] attributes = (EditorLimitsRangeAttribute[])
											property.GetCustomAttributes( typeof( EditorLimitsRangeAttribute ), true );
										if( attributes.Length != 0 )
											range = attributes[ 0 ].LimitsRange;
									}

									Vec2 value;
									if( property.PropertyType == typeof( Vec2 ) )
										value = (Vec2)property.GetValue( instance, null );
									else
										value = ( (Range)property.GetValue( instance, null ) ).ToVec2();

									string[] componentNames;

									if( property.PropertyType == typeof( Vec2 ) )
										componentNames = new string[] { "X", "Y" };
									else
										componentNames = new string[] { "Minimum", "Maximum" };

									for( int n = 0; n < 2; n++ )
									{
										PageControl.Controls[ string.Format( "FloatParameter{0}Text",
											paramCount ) ].Visible = true;
										PageControl.Controls[ string.Format( "FloatParameter{0}Text",
											paramCount ) ].Text = property.Name + "." + componentNames[ n ];
										PageControl.Controls[ string.Format( "FloatParameter{0}Value",
											paramCount ) ].Visible = true;
										scrollBarFloatParameters[ paramCount ].Visible = true;
										scrollBarFloatParameters[ paramCount ].ValueRange = range;
										scrollBarFloatParameters[ paramCount ].Value = value[ n ];
										scrollBarFloatParameters[ paramCount ].Enable = manualControl;
										usedFloats[ paramCount ] = true;
										paramCount++;
									}
								}
							}
							else if( property.PropertyType == typeof( ColorValue ) )
							{
								//ColorValue

								bool noAlphaChannel = property.GetCustomAttributes(
									typeof( ColorValueNoAlphaChannelAttribute ), true ).Length != 0;
								int components = noAlphaChannel ? 3 : 4;

								if( paramCount + components <= scrollBarFloatParameters.Length )
								{
									ColorValue value = (ColorValue)property.GetValue( instance, null );

									string[] componentNames = new string[] { "Red", "Green", "Blue", "Alpha" };

									for( int n = 0; n < components; n++ )
									{
										PageControl.Controls[ string.Format( "FloatParameter{0}Text",
											paramCount ) ].Visible = true;
										PageControl.Controls[ string.Format( "FloatParameter{0}Text",
											paramCount ) ].Text = property.Name + "." + componentNames[ n ];
										PageControl.Controls[ string.Format( "FloatParameter{0}Value",
											paramCount ) ].Visible = true;
										scrollBarFloatParameters[ paramCount ].Visible = true;
										scrollBarFloatParameters[ paramCount ].ValueRange = new Range( 0, 1 );
										scrollBarFloatParameters[ paramCount ].Value = value[ n ];
										scrollBarFloatParameters[ paramCount ].Enable = manualControl;
										usedFloats[ paramCount ] = true;
										paramCount++;
									}
								}
							}
							else if( property.PropertyType == typeof( bool ) )
							{
								//bool

								if( paramCount + 1 <= checkBoxBoolParameters.Length )
								{
									CheckBox checkBox = checkBoxBoolParameters[ paramCount ];

									checkBox.Visible = true;
									checkBox.Text = property.Name;
									checkBox.Checked = (bool)property.GetValue( instance, null );
									checkBox.Enable = manualControl;

									usedBools[ paramCount ] = true;
									paramCount++;
								}
							}
						}
					}
				}

				//Hide not used controls
				for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
				{
					if( !usedFloats[ n ] )
					{
						string s = "FloatParameter" + n.ToString();
						PageControl.Controls[ s + "Text" ].Visible = false;
						PageControl.Controls[ s ].Visible = false;
						PageControl.Controls[ s + "Value" ].Visible = false;
					}
				}
				for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
				{
					if( !usedBools[ n ] )
					{
						string s = "BoolParameter" + n.ToString();
						PageControl.Controls[ s ].Visible = false;
					}
				}
			}

			void UpdateCurrentEffect( bool updateParameters )
			{
				string name = GetListCompositorItemName( listBoxAvailable.SelectedItem.ToString() );

				bool enabled = checkBoxEnabled.Checked;
				CompositorInstance instance = viewport.GetCompositorInstance( name );

				if( enabled )
				{
					//Enable

					int priority = GetCompositorPriority( name );
					if( viewport.CompositorInstances.Count != 0 )
					{
						int n;
						for( n = viewport.CompositorInstances.Count - 1; n >= 0; n-- )
						{
							if( priority >= GetCompositorPriority(
								viewport.CompositorInstances[ n ].Compositor.Name ) )
							{
								break;
							}
						}
						int index = n + 1;
						if( index < viewport.CompositorInstances.Count )
							instance = viewport.AddCompositor( name, index );
						else
							instance = viewport.AddCompositor( name );
					}
					else
						instance = viewport.AddCompositor( name );

					if( instance != null )
						instance.Enabled = true;
				}
				else
				{
					//Disable
					viewport.RemoveCompositor( name );
				}

				if( enabled && updateParameters )
				{
					List<PropertyInfo> properties = GetCompositorInstanceProperties( instance.GetType() );

					int paramCount = 0;

					foreach( PropertyInfo property in properties )
					{
						if( property.PropertyType == typeof( float ) )
						{
							//float
							if( paramCount + 1 <= scrollBarFloatParameters.Length )
							{
								property.SetValue( instance, scrollBarFloatParameters[ paramCount ].Value, null );
								paramCount++;
							}
						}
						else if( property.PropertyType == typeof( Vec2 ) ||
							property.PropertyType == typeof( Range ) )
						{
							//Vec2, Range
							if( paramCount + 2 <= scrollBarFloatParameters.Length )
							{
								float x = scrollBarFloatParameters[ paramCount ].Value;
								paramCount++;
								float y = scrollBarFloatParameters[ paramCount ].Value;
								paramCount++;
								if( property.PropertyType == typeof( Vec2 ) )
									property.SetValue( instance, new Vec2( x, y ), null );
								else
									property.SetValue( instance, new Range( x, y ), null );
							}
						}
						else if( property.PropertyType == typeof( ColorValue ) )
						{
							//ColorValue

							bool noAlphaChannel = property.GetCustomAttributes(
								typeof( ColorValueNoAlphaChannelAttribute ), true ).Length != 0;
							int components = noAlphaChannel ? 3 : 4;

							if( paramCount + components <= scrollBarFloatParameters.Length )
							{
								ColorValue value = (ColorValue)property.GetValue( instance, null );

								value.Red = scrollBarFloatParameters[ paramCount ].Value;
								paramCount++;
								value.Green = scrollBarFloatParameters[ paramCount ].Value;
								paramCount++;
								value.Blue = scrollBarFloatParameters[ paramCount ].Value;
								paramCount++;
								if( !noAlphaChannel )
								{
									value.Alpha = scrollBarFloatParameters[ paramCount ].Value;
									paramCount++;
								}
								property.SetValue( instance, value, null );
							}
						}
						else if( property.PropertyType == typeof( bool ) )
						{
							//bool
							if( paramCount + 1 <= checkBoxBoolParameters.Length )
							{
								property.SetValue( instance, checkBoxBoolParameters[ paramCount ].Checked, null );
								paramCount++;
							}
						}
					}
				}

				if( name == "HDR" )
				{
					//update MRT rendering settings for HDR compositor
					EngineApp.RenderTechnique = enabled ? "HDR" : "Standard";
					GameEngineInitialization.ConfigureMRTRendering();

					//recreate SSAO compositor
					CompositorInstance ssaoInstance = viewport.GetCompositorInstance( "SSAO" );
					if( ssaoInstance != null )
					{
						if( ssaoInstance.Enabled )
						{
							ssaoInstance.Enabled = false;
							ssaoInstance.Enabled = true;
						}
					}
				}

			}
		}

		/////////////////////////////////////////////

		class FileSystemPage : Page
		{
			bool subscribedToLogs;

			public FileSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				CheckBox checkBox = (CheckBox)PageControl.Controls[ "LogFileOperations" ];
				checkBox.Checked = VirtualFileSystem.LoggingFileOperations;
				checkBox.CheckedChange += checkBox_CheckedChange;

				Button button = (Button)PageControl.Controls[ "Clear" ];
				button.Click += buttonClear_Click;

				if( VirtualFileSystem.LoggingFileOperations )
					SubscribeToLogs();
			}

			void checkBox_CheckedChange( CheckBox sender )
			{
				VirtualFileSystem.LoggingFileOperations = sender.Checked;
				if( VirtualFileSystem.LoggingFileOperations )
					SubscribeToLogs();
				else
					UnsubscribeFromLogs();
			}

			void buttonClear_Click( Button sender )
			{
				ListBox listBox = (ListBox)PageControl.Controls[ "Output" ];
				listBox.Items.Clear();
			}

			public override void OnDestroy()
			{
				UnsubscribeFromLogs();
				base.OnDestroy();
			}

			void SubscribeToLogs()
			{
				if( !subscribedToLogs )
				{
					Log.Handlers.InfoHandler += Log_Handlers_InfoHandler;
					subscribedToLogs = true;
				}
			}

			void UnsubscribeFromLogs()
			{
				if( subscribedToLogs )
				{
					Log.Handlers.InfoHandler -= Log_Handlers_InfoHandler;
					subscribedToLogs = false;
				}
			}

			void Log_Handlers_InfoHandler( string text, ref bool dumpToLogFile )
			{
				const int maxItemCount = 10000;
				const string prefix = "Logging File Operations: ";

				int index = text.IndexOf( prefix );
				if( index != -1 )
				{
					string text2 = text.Substring( index + prefix.Length );

					ListBox listBox = (ListBox)PageControl.Controls[ "Output" ];

					while( listBox.Items.Count >= maxItemCount )
						listBox.Items.RemoveAt( 0 );
					listBox.Items.Add( text2 );
					listBox.SelectedIndex = listBox.Items.Count - 1;
				}
			}
		}

		/////////////////////////////////////////////

		class ProjectSpecificPage : Page
		{
			public ProjectSpecificPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				//code from your brain: add me here.
			}
		}

	}
}
