using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		class GrindMgr
		{




			bool setup = false;

			// AggregateInventoryInterface grinders = new 
			static public List<string> discard_components = new List<string>(new string[]
			{
"BulletproofGlass",
"Canvas",
"Computer",
"Construction",
"Detector",
"Display",
"Explosives",
"Girder",
"GravityGenerator",
"InteriorPlate",
"LargeTube",
"Medical",
"MetalGrid",
"Motor",
"PowerCell",
"RadioCommunication",
"Reactor",
"SmallTube",
"SolarCell",
"SteelPlate",
"Superconductor",
"Thrust",
//todo: add packages, fuel tanks, etc here. all the shit in the serversaved bp
"AmmoCache",
"Fuel_Tank",//they're too fat.
"HeavyArms",
"SmallArms",
"ToolPack",
"SemiAutoPistolMagazine",
			});



			static public List<string> keepfilters = new List<string>(new string[]
			{
				"MCRN",
				"UNN",
				"Belter",
				"Adv",
				"Upg",
				"Experi",
				"Lidar"
			});

			bool ejecting = true;

			int tick = -1;
			public void update()
			{
				tick += 1;
				//gProgram.Echo(gProgram.grinders.Count + ":" + gProgram.connectors.Count);
				if (tick % 60 == 0)
				{

					if (gProgram.grinders.Count > 0 && gProgram.connectors.Count > 0)
					{
						if (!setup)
						{
							setup = true;
							foreach (var g in gProgram.grinders)
							{
								g.UseConveyorSystem = !ejectMaterials;
							}
						}
						if (ejectMaterials)
						{
							var grinders_on = gProgram.grinders.Count > 0 ? gProgram.grinders[0].Enabled : false;

							if (grinders_on)
							{

								gProgram.grinderInterface.update(true);

								foreach (var kvp in gProgram.grinderInterface.items)
								{
									var i = kvp.Key.GetItemInfo();
									/*
									 if ammo or ingot or ore or tool, keep.
									otherwise, if in vanilla comp list, trash
									unless it has wildcards in it, then keep anyway.
									 */

									bool keep = i.IsAmmo || i.IsIngot || i.IsOre || i.IsTool;
									if (!keep) keep = !discard_components.Contains(kvp.Key.SubtypeId);
									if (!keep)
									{
										foreach (var f in keepfilters)
										{
											if (kvp.Key.SubtypeId.IndexOf(f) != -1)
											{
												keep = true;
												break;
											}
										}
									}
									if (keep) gProgram.grinderInterface.TransferItemTo(kvp.Key, kvp.Value, gProgram.lootInterface);
									else gProgram.grinderInterface.TransferItemTo(kvp.Key, kvp.Value, gProgram.connectorInterface);
								}
							}

							gProgram.connectorInterface.update(grinders_on);

							bool shouldEject = gProgram.connectorInterface.items.Count > 0;
							if (shouldEject)
							{
								foreach (var c in gProgram.connectors)
								{
									if (c.IsConnected)
									{
										shouldEject = false;
										break;
									}
								}
							}
							if (shouldEject != ejecting)
							{
								ejecting = shouldEject;
								foreach (var c in gProgram.connectors)
								{
									c.ThrowOut = ejecting;
								}
							}
						}
					}
				}
			}
		}
	}
}
