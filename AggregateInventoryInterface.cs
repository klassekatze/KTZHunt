using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
	public partial class Program : MyGridProgram
	{

		//can generate an inventory from a bunch of inventories, and can simplify programmatically sending stuff between such groupings.
		//the most immediate use here is automatically unstuffing grinders, stuffing torpedo launchers and other weapons, and sending excess garbage to connectors for ejection from the ship.
		//this interface doesn't recognize partial items, only whole units / integers.
		class AggregateInventoryInterface
		{
			int tickOffset = 0;
			static int offseti = 0;
			public AggregateInventoryInterface()
			{
				tickOffset = offseti;
				offseti += 10;
			}
			/*public void setContainers(List<IMyTerminalBlock> c)
            {
                containers = c;
            }*/
			public void setContainers<T>(List<T> c) where T : IMyTerminalBlock
			{
				containers.Clear();
				foreach (T i in c)
				{
					containers.Add(i);
				}
			}
			List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>();
			public Dictionary<MyItemType, int> items = new Dictionary<MyItemType, int>();

			int updateInterval = 60 * 3;
			int lastUpdateTick = 0;
			int tick = 0;
			//if called every tick will update the inventory manifest every 3 seconds.
			//may not be necessary - a force update is best before doing anything important.
			public void update(bool force = false, int ticks = 1)
			{
				tick += ticks;
				if (tick + tickOffset - lastUpdateTick > updateInterval || force)
				{
					lastUpdateTick = tick;
					items.Clear();
					foreach (IMyTerminalBlock t in containers)
					{
						for (int i = 0; i < t.InventoryCount; i++)
						{
							var inv = t.GetInventory(i);
							List<MyInventoryItem> t_items = new List<MyInventoryItem>();
							inv.GetItems(t_items);
							foreach (MyInventoryItem item in t_items)
							{
								if (!items.ContainsKey(item.Type)) items[item.Type] = (int)Math.Floor((double)item.Amount);
								else items[item.Type] += (int)Math.Floor((double)item.Amount);
							}
						}
					}
				}
			}
			//these return the amount of items that could not be sent (unavailable, no room, whatever). Ergo, 0 means all were transferred.
			public int TransferItemTo(MyItemType type, int amount_to_transfer, IMyInventory destination)
			{
				float unit_volume = type.GetItemInfo().Volume;
				foreach (IMyTerminalBlock t in containers)
				{
					for (int i = 0; i < t.InventoryCount; i++)
					{
						var inv = t.GetInventory(i);
						List<MyInventoryItem> t_items = new List<MyInventoryItem>();
						inv.GetItems(t_items);
						foreach (MyInventoryItem item in t_items)
						{
							if (item.Type == type)
							{
								int transfer_amt = (int)Math.Floor((double)item.Amount);

								int max_room_for = (int)Math.Floor((double)(destination.MaxVolume - destination.CurrentVolume + (MyFixedPoint)0.001) / unit_volume);

								if (max_room_for < transfer_amt) transfer_amt = max_room_for;

								if (transfer_amt > amount_to_transfer) transfer_amt = amount_to_transfer;
								//log(">sending " + transfer_amt + " of " + item.Type.SubtypeId);


								if (inv.TransferItemTo(destination, item, transfer_amt)) amount_to_transfer -= transfer_amt;
								if (amount_to_transfer <= 0) return 0;
							}
						}
					}
				}
				return amount_to_transfer;
			}
			//these return the amount of items that could not be sent (unavailable, no room, whatever). Ergo, 0 means all were transferred.
			public int TransferItemTo(MyItemType type, int amount, AggregateInventoryInterface destination)
			{
				foreach (IMyTerminalBlock t in destination.containers)
				{
					for (int i = 0; i < t.InventoryCount; i++)
					{

						var inv = t.GetInventory(i);
						//log("sending " + amount + " of " + type.SubtypeId + " to " + t.DefinitionDisplayNameText);
						amount = TransferItemTo(type, amount, inv);
						if (amount <= 0) return amount;
					}
				}
				return amount;
			}
			static string[] common_ammo_identifiers = new string[]
					{
						"missile",
						"ammo",
						"magazine",
						"torpedo",
						"slug",
						"box"
					};
			static Dictionary<string, string> bulkreplace = new Dictionary<string, string>();
			static void initbulk()
			{
				var r = bulkreplace;
				if (r.Count != 0) return;
				r["AngleGrinder"] = "Grinder";
				r["CrateTomato"] = "Crate of Tomatoes";
				r["HeavyArms"] = "Heavy Armaments";
				r["GravityGenerator"] = "Gravity Comp.";
				r["RadioCommunication"] = "Radio-comm Comp.";
				r["Detector"] = "Detector Comp.";
				r["LargeTube"] = "Large Steel Tube";
				r["Construction"] = "Construction Comp.";
			}
			static public string prettyItemName(MyItemType item)
			{
				initbulk();
				string name = item.SubtypeId.Replace("MyObjectBuilder_", "").Replace("_", " ");
				var nfo = item.GetItemInfo();
				foreach (KeyValuePair<string, string> kvp in bulkreplace)
				{
					if (name == kvp.Key)
					{
						name = kvp.Value;
						break;
					}
					else if (name.StartsWith(kvp.Key))
					{
						name = name.Replace(kvp.Key, kvp.Value);
						break;
					}
				}
				if (nfo.IsAmmo)
				{
					var l = name.ToLower();
					if (name.Length > 20) name = name.Replace("Magazine", "");
					else
					{

						if (name.StartsWith("Missile"))
						{
							name = name.Substring("Missile".Length) + " Missile";
						}

						if (l.IndexOf("magazine") == -1)// && l.IndexOf(' ') == -1)
						{

						}
					}
					l = name.ToLower();
					bool id = false;
					foreach (var i in common_ammo_identifiers)
					{
						if (l.IndexOf(i) != -1)
						{
							id = true;
							break;
						}
					}
					if (!id)
					{
						name += "Ammo";
					}
				}
				if (nfo.IsTool && name.Length > 5)
				{

					string nsub = name.Substring(0, name.Length - 5);
					if (name.EndsWith("1Item")) name = nsub;
					else if (name.EndsWith("2Item")) name = nsub + " (Enhanced)";
					else if (name.EndsWith("3Item")) name = nsub + " (Proficient)";
					else if (name.EndsWith("4Item")) name = nsub + " (Elite)";
				}
				int capcount = 0;
				foreach (char c in name) if (char.IsUpper(c)) capcount += 1;
				if (capcount <= 1)
				{
					if (nfo.IsOre && name != "Stone") return name + " Ore";
					if (nfo.IsIngot)
					{
						if (name == "Stone") return "Gravel";
						return name + " Ingot";
					}
					//if (name == "Stone Ingot") name = "Gravel";
				}
				else
				{
					string rename = "";
					// bool didLast = false;
					for (int i = 0; i < name.Length; i++)
					{
						if (i > 0 && i < name.Length - 1)
						{
							bool notlast = true;
							if (rename.Length > 0) notlast = rename[rename.Length - 1] != ' ';
							if (name[i - 1] != ' ' && name[i] != ' ' && name[i + 1] != ' ' && notlast)
							{
								bool prev = char.IsUpper(name[i - 1]);
								bool cur = char.IsUpper(name[i]);
								bool next = char.IsUpper(name[i + 1]);
								bool nextLetter = char.IsLetter(name[i + 1]);
								if ((cur && !next && nextLetter && name[i - 1] != '(' && name[i] != ' ') || (!prev && name[i - 1] != '(' && cur && name[i] != ' '))// && !prev)
								{
									rename += " ";
								}
								if (prev && !char.IsLetter(name[i]) && name[i] != ' ') rename += " ";
							}
						}
						rename += name[i]; ;
					}
					name = rename;
				}
				name = name.Replace(" Adv ", " Advanced ");
				if (nfo.IsAmmo && name.Length > 4)
				{
					if (name.EndsWith("MCRN")) name = name.Substring(0, name.Length - 4) + "(MCRN)";
					if (name.EndsWith("UNN")) name = name.Substring(0, name.Length - 3) + "(UNN)";
				}

				return name;
			}

			public string listInv(Func<MyItemType,bool> filter = null)
			{
				string r = "";

				int nlen = 0;
				Dictionary<string, int> entries = new Dictionary<string, int>();
				foreach (KeyValuePair<MyItemType, int> kvp in items)
				{
					if (filter != null) if (!filter(kvp.Key)) continue;
					entries[prettyItemName(kvp.Key)] = kvp.Value;
				}
				foreach (KeyValuePair<string, int> kvp in entries)if (kvp.Key.Length > nlen) nlen = kvp.Key.Length;
				foreach (KeyValuePair<string, int> kvp in entries)
				{
					string d = kvp.Key;
					if (d.Length < nlen) d += new string('_', nlen - d.Length);
					r += d + ": " + kvp.Value + "\n";
				}
				return r;
			}
		}
	}
}

