﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using Sandbox.ModAPI;
using System.IO;
using IMyTextSurfaceProvider = Sandbox.ModAPI.IMyTextSurfaceProvider;
using VRage.Game.GUI.TextPanel;
using Sandbox.Game.Entities.Blocks;
using System.Diagnostics;
using static LCDText2.VideoBuffer;
using Sandbox.Definitions;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage;
using VRage.Utils;

namespace LocalLCD
{
	public class VideoPlayerScript
	{
		MyEntity3DSoundEmitter AudioEmitter;
		bool playing = false;
		bool done = false;
		bool error = false;
		IMyCubeGrid fake;
		Sandbox.ModAPI.IMyTextPanel Parent;
		private List<IMySlimBlock> blocks = new List<IMySlimBlock>();
		internal void PlayAudio(byte[] audioframes, int size, int sampleRate)
		{
			//MyLog.Default.WriteLine($"VideoPlayerScript PlayAudio {AudioEmitter != null}");
			if (AudioEmitter != null)
				AudioEmitter.PlaySound(audioframes, size, sampleRate, 1, 200);
		}




		private Sandbox.ModAPI.Ingame.IMyTextSurface surface;

		public VideoPlayerScript(Sandbox.ModAPI.IMyTextPanel panel)
		{
			AudioEmitter = new MyEntity3DSoundEmitter((MyEntity)panel);
			Parent = panel;
		}

		public static VideoPlayerScript Factory(Sandbox.ModAPI.IMyTextPanel Panel)
		{
			return new VideoPlayerScript(Panel);
		}

		public void Update()
		{
			if(fake != null)
			{
				var matrix = new MatrixD(Parent.WorldMatrix);
				matrix.Translation += Parent.WorldMatrix.Left * (Parent.CubeGrid.GridSize * 0.5);
				matrix.Translation += Parent.WorldMatrix.Forward * -0.01;
				fake.SetWorldMatrix(matrix);
			}

        }

		public void InitFake()
		{ 
			if (fake != null)
				return;
			//WideLCDScreenVideoPlayer
			//WideLCDScreenWithBattery
			var prefab = MyDefinitionManager.Static.GetPrefabDefinition("WideLCDScreenWithBattery");
			if (prefab.CubeGrids == null)
			{
				MyDefinitionManager.Static.ReloadPrefabsFromFile(prefab.PrefabPath);
				prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefab.Id.SubtypeName);
			}
			var tempList = new List<MyObjectBuilder_EntityBase>();


			// We SHOULD NOT make any changes directly to the prefab, we need to make a Value copy using Clone(), and modify that instead.
			foreach (var grid in prefab.CubeGrids)
			{
				var gridBuilder = (MyObjectBuilder_CubeGrid)grid.Clone();
				gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(Parent.WorldMatrix.Translation, grid.PositionAndOrientation.Value.Forward, grid.PositionAndOrientation.Value.Up);

				tempList.Add(gridBuilder);
			}
			var entities = new List<IMyEntity>();
			MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);


			foreach (var item in tempList)
			{
				MyObjectBuilder_CubeGrid cubegrid = (MyObjectBuilder_CubeGrid)item;

				cubegrid.CreatePhysics = false;
				cubegrid.Immune = true;
				cubegrid.IsStatic = true;
				cubegrid.AngularVelocity = Vector3.Zero;
				cubegrid.LinearVelocity = Vector3.Zero;
				var newfake = (MyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilder(item);
				newfake.SyncFlag = false;
				newfake.Save = false;
				newfake.IsPreview = true;
				newfake.Flags |= EntityFlags.NeedsWorldMatrix | EntityFlags.IsNotGamePrunningStructureObject;
				fake = newfake;
				MyAPIGateway.Entities.AddEntity(fake);
				fake.NeedsWorldMatrix = true;
				Parent.NeedsWorldMatrix = true;
				Parent.Parent.NeedsWorldMatrix = true;
				if (fake.Flags.HasFlag(EntityFlags.Save))
				{
					fake.Flags &= ~EntityFlags.Save;
				}
				fake.Synchronized = false;


				blocks.Clear();
				fake.GetBlocks(blocks);
				foreach (var block in blocks)
				{
					if (block.FatBlock is Sandbox.ModAPI.IMyTextPanel)
					{
						var issurface = (block.FatBlock as IMyTextSurfaceProvider)?.GetSurface(0);
						if (issurface != null)
						{
							surface = issurface;
							surface.ContentType = ContentType.TEXT_AND_IMAGE;
							surface.Alignment = TextAlignment.CENTER;
							surface.FontSize = 0.072f;
							surface.Font = "Mono Color";
							//MyAPIGateway.Utilities.ShowMessage("found screen", surface.ToString());
						}
						else
						{
							if (block.FatBlock != null)
							{
								block.FatBlock.Flags &= ~EntityFlags.Visible;

							}

						}
					}
				}

			}
	}

		internal void DeletePlayer()
		{
			if(fake != null)
			{
				surface = null;
				fake.Close();
				fake = null;
			}
			if (AudioEmitter != null && AudioEmitter.IsPlaying)
			{
				AudioEmitter.StopSound(true);
			}


		}

		private void AudioEmitter_StoppedPlaying(MyEntity3DSoundEmitter obj)
		{


		}
		float fontsize = 1f;
		public void SetFontSize(float size)
		{
			if(surface != null)
				surface.FontSize = size;
			fontsize = size;
		}
		

		public void PlayNextFrame(string chars)
		{

			if (fake == null)
			{
				//MyLog.Default.WriteLineAndConsole($"VideoPlayerScript InitFake");
				InitFake();
			}
				
			if(surface != null)
			{
				//MyLog.Default.WriteLineAndConsole($"VideoPlayerScript WriteText");
				surface.WriteText(chars);
			}
		}
		


		public void Close()
		{
			fake = null;
			surface = null;
		}
	}
}
