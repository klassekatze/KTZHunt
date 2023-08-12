using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		public class WeaponStatAgent
		{
			public WeaponStatAgent()
			{

			}
			class WeaponState
			{
				public float restPower = 0;
				public float drawPower = 0;
				public int ticksToCharge = 0;
				public bool isCharging = false;
				int chargeStartTick = 0;

				public void setCharging(bool b)
				{
					if (b != isCharging)
					{
						isCharging = b;
						if (b) chargeStartTick = tick;
						else
						{
							ticksToCharge = tick - chargeStartTick;
						}
					}
				}
			}
			Dictionary<IMyTerminalBlock, WeaponState> wsdict = new Dictionary<IMyTerminalBlock, WeaponState>();

			Dictionary<string, int> initialTicksToCharge = new Dictionary<string, int>{
{"Dawson-Pattern Medium Railgun",420},
{"Farren-Pattern Heavy Railgun",600},
{"Mounted Zakosetara Heavy Railgun",750},
{"T-47 Roci Light Fixed Railgun",600},
{"V-14 Stiletto Light Railgun",300},
{"VX-12 Foehammer Ultra-Heavy Railgun",600},
{"Zakosetara Heavy Railgun",900}
 };

			public void update()
			{
				string o = "";
				var p = gProgram;
				//if (tick % 5 == 0)//this will create inaccuracies
				{
					foreach (var w in p.weaponCoreWeapons)
					{
						WeaponState ws = null;
						wsdict.TryGetValue(w, out ws);
						if (ws == null)
						{
							ws = new WeaponState();
							if (initialTicksToCharge.ContainsKey(w.DefinitionDisplayNameText)) ws.ticksToCharge = initialTicksToCharge[w.DefinitionDisplayNameText];
							wsdict[w] = ws;
						}
						float draw = p.modAPIWeaponCore.GetCurrentPower(w);
						if (p.modAPIWeaponCore.IsWeaponReadyToFire(w))
						{
							ws.restPower = draw;
							ws.setCharging(false);
						}
						else if (ws.restPower != 0 && draw != ws.restPower)
						{
							ws.drawPower = draw;
							ws.setCharging(true);
						}
					}

					foreach (var w in p.weaponCoreWeapons)
					{
						var ws = wsdict[w];
						o += w.CustomName + ":" + ws.isCharging + ":" + ws.ticksToCharge + "\n";
					}
					//	o += w.CustomName + ":" + p.modAPIWeaponCore.GetCurrentPower(w) + "\n";

					//IMypower x;
					//o += w.CustomName+"\n"+w.CustomInfo+"\n:\n"+w.DetailedInfo+"\n";
					if (p.weaponStatusLog != null) p.weaponStatusLog.WriteText(o);
				}

			}
		}
	}
}
