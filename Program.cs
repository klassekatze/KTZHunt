using Sandbox.Game.EntityComponents;
using Sandbox.Graphics;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		static public Program gProgram = null;
		public static GyroECU gyroECU = null;
		GrindMgr gmgr = new GrindMgr();
		WeaponStatAgent wsa = new WeaponStatAgent();


		Dictionary<string, float> ammoVelocities = new Dictionary<string, float> {{"T-47 Roci Light Fixed Railgun",9000 },{"Zakosetara Heavy Railgun", 8000 },{ "Kess Hashari Cannon", 2000},{"Glapion Collective Gatling Cannon",2500 }};

		public Program()
		{
			gProgram = this;
			Runtime.UpdateFrequency = UpdateFrequency.Update1;

			GridTerminalSystem.GetBlocksOfType(thrusters, b => b.CubeGrid == Me.CubeGrid);
			foreach (var b in thrusters) if (b.ThrustOverridePercentage > 0) b.ThrustOverridePercentage = 0;
			GridTerminalSystem.GetBlocksOfType(gyros, b => b.CubeGrid == Me.CubeGrid);
			foreach (var b in gyros) if(b.GyroOverride)b.GyroOverride = false;

			chkInitializations();

			if (Me.CustomData.Length < 3)
			{
				_Save();
			}
		}
		public WcPbApi modAPIWeaponCore = null;
		public bool modAPIWeaponCoreReady = false;
		int lastApiCheck = int.MinValue;
		int lastBlockCheck = int.MinValue;

		static int tick = -1;

		IEnumerator<bool> blockCheckMachine = null;
		void mkBlockCheckMachine()
		{
			if (blockCheckMachine != null) blockCheckMachine.Dispose();
			blockCheckMachine = blockLoader();
		}
		public void chkInitializations()
		{
			if (modAPIWeaponCore == null || !modAPIWeaponCoreReady)
			{
				if (tickcheck(ref lastApiCheck, 5 * 60))
				{
					if (modAPIWeaponCore == null) try { modAPIWeaponCore = new WcPbApi(); } catch (Exception) { }
					if (modAPIWeaponCore != null && !modAPIWeaponCoreReady)
					{
						try
						{
							if (modAPIWeaponCore.Activate(Me))
							{
								modAPIWeaponCoreReady = true;
								mkBlockCheckMachine();
							}
						}
						catch (Exception) { }
					}
				}
			}

			if (blockCheckMachine != null)
			{
				if (!blockCheckMachine.MoveNext())
				{
					blockCheckMachine.Dispose();
					blockCheckMachine = null;
				}
			}
			else if (tickcheck(ref lastBlockCheck, 5 * 60 * 60)) mkBlockCheckMachine();
		}



		IMyTextSurface consoleLog = null;
		IMyTextSurface statusLog = null;
		IMyTextSurface profileLog = null;
		IMyTextSurface radarLog = null;
		IMyTextSurface weaponStatusLog = null;

		List<IMyTerminalBlock> weaponCoreWeapons = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> subsystemTargeters = new List<IMyTerminalBlock>();
		List<IMyTerminalBlock> fixedGuns = new List<IMyTerminalBlock>();
		List<IMyShipController> controllers = new List<IMyShipController>();
		List<IMyGyro> gyros = new List<IMyGyro>();

		List<IMyCargoContainer> all_cargos = new List<IMyCargoContainer>();
		List<IMyCargoContainer> loot_cargos = new List<IMyCargoContainer>();

		List<IMyShipConnector> connectors = new List<IMyShipConnector>();
		List<IMyShipGrinder> grinders = new List<IMyShipGrinder>();

		AggregateInventoryInterface ammoInterface = new AggregateInventoryInterface();
		AggregateInventoryInterface cargoInterface = new AggregateInventoryInterface();
		AggregateInventoryInterface lootInterface = new AggregateInventoryInterface();
		AggregateInventoryInterface connectorInterface = new AggregateInventoryInterface();
		AggregateInventoryInterface grinderInterface = new AggregateInventoryInterface();

		public List<IMyThrust> thrusters = new List<IMyThrust>();
		public List<IMyLightingBlock> indicators = new List<IMyLightingBlock>();
		public void setlight(Color c)
		{
			foreach (IMyLightingBlock b in indicators)
			{
				b.Color = c;
			}
		}


		//IMyTextSurface radarLog2 = null;

			public bool isThis(IMyTerminalBlock b)
			{
				return b.OwnerId == Me.OwnerId && b.CubeGrid == Me.CubeGrid;
			}

		bool blocksNeverLoaded = true;
		int blstep = 0;
		public IEnumerator<bool> blockLoader()
		{
			blstep = 0;
			consoleLog = null;
			statusLog = null;
			profileLog = null;
			weaponStatusLog = null;
			List<IMyTerminalBlock> LCDs = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(LCDs, b => (b is IMyTextSurface) && b.CubeGrid == Me.CubeGrid);
			foreach (var b in LCDs)
			{
				IMyTextSurface s = b as IMyTextSurface;
				if (b.CustomData.Contains("statusLog")) statusLog = s;
				else if (b.CustomData.Contains("consoleLog")) consoleLog = s;
				else if (b.CustomData.Contains("profileLog")) profileLog = s;
				else if (b.CustomData.Contains("radarLog")) radarLog = s;
				else if (b.CustomData.Contains("weaponStatusLog")) weaponStatusLog = s;
			}
			blstep++;
			yield return true;

			if (!modAPIWeaponCoreReady) yield return true;

			weaponCoreWeapons.Clear();
			subsystemTargeters.Clear();
			GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(weaponCoreWeapons, b => b.CubeGrid == Me.CubeGrid && b.IsFunctional && modAPIWeaponCore.HasCoreWeapon(b));

			IMyBlockGroup ind = GridTerminalSystem.GetBlockGroupWithName("subtargeter");
			if (ind != null)
			{
				ind.GetBlocksOfType<IMyTerminalBlock>(subsystemTargeters);
			}
			else
			{
				foreach (var b in weaponCoreWeapons)
				{
					if (modAPIWeaponCore.GetMaxWeaponRange(b, 0) > 7000)
					{
						try
						{
							bool targetingSubSystems = b.GetValueBool("WC_SubSystems");
							int targetedSubSystem = (int)b.GetValue<Int64>("WC_PickSubSystem");
							if (targetedSubSystem > 0 && targetedSubSystem < modAPIWeaponCore.WcBlockTypeLabels.Length)
							{
								subsystemTargeters.Add(b);
							}
						}
						catch (Exception) { }
					}
				}
			}
			blstep++;
			yield return true;

			fixedGuns.Clear();
			ind = null;
			ind = GridTerminalSystem.GetBlockGroupWithName("fixedbot");
			if (ind != null)
			{
				ind.GetBlocksOfType<IMyTerminalBlock>(fixedGuns);
			}
			else
			{
				foreach (var b in weaponCoreWeapons)
				{
					var t = b.DefinitionDisplayNameText.ToLower();
					if (!t.Contains("mounted") && (t.Contains("t-47") || t.Contains("zako")))
					{
						fixedGuns.Add(b);
					}
				}
			}
			blstep++;
			yield return true;

			controllers.Clear();
			GridTerminalSystem.GetBlocksOfType(controllers, isThis);
			blstep++;
			yield return true;
			gyros.Clear();
			GridTerminalSystem.GetBlocksOfType(gyros, isThis);
			if (gyroECU == null) gyroECU = new GyroECU(gyros, getController());
			blstep++;
			yield return true;
			grinders.Clear();
			GridTerminalSystem.GetBlocksOfType(grinders, isThis);
			grinderInterface.setContainers(grinders);
			blstep++;
			yield return true;

			all_cargos.Clear();
			GridTerminalSystem.GetBlocksOfType(all_cargos, isThis);
			cargoInterface.setContainers(all_cargos);
			yield return true;
			blstep++;

			loot_cargos.Clear();
			foreach (var b in all_cargos) if (b.CustomName.Contains("Loot")) loot_cargos.Add(b);
			lootInterface.setContainers(loot_cargos);
			yield return true;
			blstep++;

			connectors.Clear();
			GridTerminalSystem.GetBlocksOfType(connectors, isThis);
			connectorInterface.setContainers(connectors);

			List<IMyTerminalBlock> tmp = new List<IMyTerminalBlock>();
			tmp.AddArray(all_cargos.ToArray());
			tmp.AddArray(connectors.ToArray());
			ammoInterface.setContainers(tmp);

			yield return true;
			blstep++;

			thrusters.Clear();
			GridTerminalSystem.GetBlocksOfType(thrusters, b => b.CubeGrid == Me.CubeGrid);

			ind = GridTerminalSystem.GetBlockGroupWithName("indicators");
			if (ind != null)
			{
				ind.GetBlocksOfType(indicators);
			}
			yield return true;
			blstep++;




			if (blocksNeverLoaded)
			{
				log("All blocks init in core.", LT.LOG_N);
				ApplyGyroOverride(0, 0, 0, gyros, getController());
				setGyrolock(gyros, false);
			}
			blocksNeverLoaded = false;
			blstep++;
			yield return false;
		}


		IMyShipController getController()
		{
			foreach (var c in controllers)
			{
				if (c.IsUnderControl && c.IsMainCockpit) return c;
			}
			foreach (var c in controllers)
			{
				if (c.IsUnderControl) return c;
			}
			foreach (var c in controllers)
			{
				if (c.IsMainCockpit) return c;
			}
			if (controllers.Count > 0) return controllers[0];
			return null;
		}







		static Profiler initP = new Profiler("init");
		static Profiler mainP = new Profiler("main");


		public void Main(string arg, UpdateType upd)
		{
			mainP.start();
			main(arg, upd);
			mainP.stop();
			if (tick % 5 == 0)
			{
				Echo(tick.ToString());
				if (profileLog != null) profileLog.WriteText("name:ms1t:ms60t\n" + Profiler.getAllReports());
			}
			if (consoleLog != null && tick % 6 == 0)
			{
				consoleLog.WriteText(renderLoggedMessages());
			}
		}

		List<MyItemType> accept = new List<MyItemType>();
		public void ammoPull()
		{
			if (tick % 60 == 0)
			{
				ammoInterface.update(false, 100);
				foreach (IMyTerminalBlock gun in weaponCoreWeapons)
				{
					for (var i = 0; i < gun.InventoryCount; i++)
					{
						var inv = gun.GetInventory(i);
						accept.Clear();
						inv.GetAcceptedItems(accept);
						if (accept.Count < 10)
						{
							foreach (var it in accept)
							{
								double v = (double)it.GetItemInfo().Volume;
								if (!inv.IsFull && (double)(inv.MaxVolume - inv.CurrentVolume + (MyFixedPoint)0.001) >= v)
								{

									int stock = 0;
									cargoInterface.items.TryGetValue(it, out stock);
									if (stock > 0)
									{
										bool success = cargoInterface.TransferItemTo(it, 9999, inv) != 9999;
									}
								}
							}
						}
					}
				}
			}
		}

		bool matchingSpeed = false;
		DetectedEntity matchTarget = null;
		void toggleMatchSpeed()
		{
			matchingSpeed = !matchingSpeed;
			if (matchingSpeed)
			{
				//Runtime.UpdateFrequency = UpdateFrequency.Update1;
				setlight(Color.Blue);
			}
			else
			{
				//Runtime.UpdateFrequency = UpdateFrequency.Update10;
				matchTarget = null;
				setlight(Color.Green);
				foreach (var t in thrusters) t.ThrustOverridePercentage = 0;
			}
		}

		void readBool(ref string f, string n, string v, ref bool b)
		{
			if (f == n)
			{
				bool.TryParse(v, out b);
			}
		}

		public static bool ejectMaterials = true;
		public static bool useGyrolock = true;


		public void _Save()
		{
			List<string> sl = new List<string>();

			//string save = "";
			sl.Add("//whether to eject certain material from grinders out the connectors");
			sl.Add("Eject=" + ejectMaterials);
			sl.Add("//exact component names to eject");
			sl.Add("EjectDiscard=" + String.Join(",", GrindMgr.discard_components.ToArray()));
			sl.Add("//wildcards to always keep");
			sl.Add("EjectKeepFilter=" + String.Join(",", GrindMgr.keepfilters.ToArray()));
			sl.Add("//whether to gyrolock to counter angular momentum not from human input");
			sl.Add("Gyrolock=" + useGyrolock);
			sl.Add("//loose aimbot lock track error threshold");
			sl.Add("looseLockTE=" + looseLockTE);
			sl.Add("//final aimbot lock track error threshold");
			sl.Add("lockTE=" + lockTE);
			sl.Add("//min ticks within loose lock to fire");
			sl.Add("minlooseLockTicks=" + minlooseLockTicks);
			sl.Add("// min ticks within final lock to fire");
			sl.Add("minlockTicks=" + minlockTicks);
			sl.Add("//whether aimbot will fire weapons automatically at all");
			sl.Add("fireWeapon=" + fireWeapon);
			sl.Add("//aimbot firing mode. 0=WC PBAPI, 1=WC terminal override+shoot");
			sl.Add("firingMode=" + firingMode);
			sl.Add("//Ballistic intercept calc method. 0=WC PBAPI (less accurate now), 1=internal solver");
			sl.Add("intrcptMode=" + intrcptMode);
			sl.Add("//aimbot minimum ticks between fire commands");
			sl.Add("fireCooldown=" + fireCooldown);

			string av = "";
			foreach(var kvp in ammoVelocities)
			{
				if (av.Length > 0) av += ",";
				av += kvp.Key + ":" + kvp.Value.ToString();
			}
			sl.Add("ammoVelocities=" + av);

			string save = "";
			foreach (var l in sl)save += l + "\n";
			Me.CustomData = save;
		}
		public void Load(string d)
		{
			string[] lines = d.Split('\n');
			foreach(var l in lines)
			{
				if (l.StartsWith("//")) continue;

				var line = l.Split('=');
				if(line.Length >=2)
				{
					try
					{
						var a = line[0];
						var b = line[1];
						if (a == "Eject") ejectMaterials = bool.Parse(b);
						else if(a == "Gyrolock") useGyrolock = bool.Parse(b);
						else if (a == "looseLockTE") looseLockTE = double.Parse(b);
						else if (a == "lockTE") lockTE = double.Parse(b);
						else if (a == "minlooseLockTicks") minlooseLockTicks = int.Parse(b);
						else if (a == "minlockTicks") minlockTicks = int.Parse(b);
						else if (a == "fireWeapon") fireWeapon = bool.Parse(b);
						else if (a == "firingMode") firingMode = int.Parse(b);
						else if (a == "fireCooldown") fireCooldown = int.Parse(b);
						else if (a == "intrcptMode") intrcptMode = int.Parse(b);

						else if (a == "EjectDiscard")
						{
							GrindMgr.discard_components.Clear();
							GrindMgr.discard_components = new List<string>(b.Split(','));
						}
						else if (a == "EjectKeepFilter")
						{
							GrindMgr.keepfilters.Clear();
							GrindMgr.keepfilters = new List<string>(b.Split(','));
						}
						else if (a == "ammoVelocities")
						{
							ammoVelocities.Clear();
							string[] kvps = b.Split(',');
                            foreach (var p in kvps)
                            {
								string[] kvp = p.Split(':');
								if(kvp.Length > 1)
								{
									ammoVelocities[kvp[0]] = float.Parse(kvp[1]);
								}
                            }
                        }
					}
					catch(Exception) { }

				}
			}
			_Save();//rewrite so any missing (or new) entries get written in
		}


			string lastCustomData = "";

		public bool AIMBOT_ON = false;
		bool lastgyrolock = false;
		void main(string arg, UpdateType upd)
		{
			tick += 1;
			if (tick % 20 == 0) if (Me.Closed)
				{
					Runtime.UpdateFrequency = UpdateFrequency.None;
					return;
				}
			initP.start();
			if (tick % 20 == 0)
			{
				chkInitializations();
			}
			initP.stop();
			if (blocksNeverLoaded)
			{
				Echo("INITIALIZING: " + blstep + "/11");
				if (statusLog != null) statusLog.WriteText("INITIALIZING: " + blstep + "/11");
				return;
			}
			if (tick % 60 == 0)
			{
				if (Me.CustomData != lastCustomData)
				{
					log("Loading CustomData.", LT.LOG_N);
					lastCustomData = Me.CustomData;
					Load(lastCustomData);
				}
			}



			updRadar();

			gmgr.update();
			//wsa.update();
			ammoPull();
			//processThreats();
			//if (radarLog != null) radarLog.WriteText(renderThreats());
			#region aimbotmain
			var ctrl = getController();
			if (arg == "aimbot")
			{
				if (fixedGuns.Count > 0)
				{
					AIMBOT_ON = !AIMBOT_ON;
					if (!AIMBOT_ON)
					{
						//var 
						ApplyGyroOverride(0, 0, 0, gyros, ctrl);
						setGyrolock(gyros, false);
					}
				}
			}
			#endregion
			#region gyrolockmain
			bool gyrolock = false;
			//angularMomentumLock = false;
			if (useGyrolock && ctrl != null)
			{
				double av = ctrl.GetShipVelocities().AngularVelocity.Length();
				if (av > 0.01)
				{
					if (ctrl.IsUnderControl && ctrl.RollIndicator == 0 && ctrl.RotationIndicator == Vector2.Zero)
					{
						gyrolock = true;
					}
				}
			}
			if (gyrolock != lastgyrolock)
			{
				lastgyrolock = gyrolock;
				setGyrolock(gyros, gyrolock);
			}

			if (AIMBOT_ON)
			{
				aimbot();
				if (ctrl.IsUnderControl && (ctrl.RollIndicator != 0 || ctrl.RotationIndicator != Vector2.Zero))
				{
					//kludge but w/e lol
					setGyrolock(gyros, false);
				}
			}
			#endregion

			#region matchspeed
			if (arg.StartsWith("match"))
			{
				toggleMatchSpeed();
				if (matchingSpeed)
				{
					if (arg == "matchclosest")
					{
						if (detectedEntitiesL.Count > 0)
						{
							matchTarget = detectedEntitiesL[0];
						}
						else toggleMatchSpeed();
					}
					if (arg == "matchfocus")
					{
						MyDetectedEntityInfo? focus = modAPIWeaponCore.GetAiFocus(Me.CubeGrid.EntityId);
						if (focus.HasValue && !focus.Value.IsEmpty())
						{

							matchTarget = null;
							detectedEntitiesD.TryGetValue(focus.Value.EntityId, out matchTarget);
							if (matchTarget == null) toggleMatchSpeed();
						}
						else toggleMatchSpeed();
					}
				}
				else matchTarget = null;
			}
			if (matchingSpeed)// && tick % 20 == 0)
			{
				bool canTrack = false;
				if (matchTarget != null)
				{
					DetectedEntity d = null;
					detectedEntitiesD.TryGetValue(matchTarget.EntityId, out d);
					if (d != null)
					{
						canTrack = true;
						matchTarget = d;
					}

					if (!canTrack || matchTarget == null)
					{
						toggleMatchSpeed();
					}
					else
					{
						targetVelocityVec = matchTarget.Velocity;
					}
				}
			}
			if (matchingSpeed)
			{
				//if (tick % 20 == 0)
				{
					Echo("m: " + matchTarget.Name);

					//Vector3D targetVelocityVec

					var myVelocityVec = getController().GetShipVelocities().LinearVelocity;
					Echo("mv: " + myVelocityVec.X.ToString("0.0") + "," + myVelocityVec.Y.ToString("0.0") + "," + myVelocityVec.Z.ToString("0.0"));
					Echo("tv: " + targetVelocityVec.X.ToString("0.0") + "," + targetVelocityVec.Y.ToString("0.0") + "," + targetVelocityVec.Z.ToString("0.0"));
					SpeedMatcher();
				}//else Echo("not match");
			}
			else Echo("not match");

			#endregion

			if (arg == "turnonall")
			{
				List<IMyFunctionalBlock> tmp = new List<IMyFunctionalBlock>();
				GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(tmp, b => b.IsSameConstructAs(Me) && !(b is IMyShipGrinder) && !(b is IMyShipWelder));
				foreach (var b in tmp) b.Enabled = true;
			}
		}

		//taken mostly wholesale from https://github.com/Whiplash141/SpaceEngineersScripts/blob/master/Released/speed_matcher.cs
		Vector3D targetVelocityVec = new Vector3D(0, 0, 0);
		void SpeedMatcher()
		{
			var thisController = getController();
			var myVelocityVec = thisController.GetShipVelocities().LinearVelocity;
			var inputVec = thisController.MoveIndicator;
			var desiredDirectionVec = Vector3D.TransformNormal(inputVec, thisController.WorldMatrix); //world relative input vector 
			var relativeVelocity = myVelocityVec - targetVelocityVec;

			ApplyThrust(thrusters, relativeVelocity, desiredDirectionVec, thisController);
		}

		void ApplyThrust(List<IMyThrust> thrusters, Vector3D travelVec, Vector3D desiredDirectionVec, IMyShipController thisController)
		{
			var mass = thisController.CalculateShipMass().PhysicalMass;
			var gravity = thisController.GetNaturalGravity();

			var desiredThrust = mass * (2 * travelVec + gravity);
			var thrustToApply = desiredThrust;
			if (!Vector3D.IsZero(desiredDirectionVec))
			{
				thrustToApply = VectorRejection(desiredThrust, desiredDirectionVec);
			}

			//convert desired thrust vector to local
			//thrustToApply = Vector3D.TransformNormal(thrustToApply, MatrixD.Transpose(thisController.WorldMatrix));

			foreach (IMyThrust thisThrust in thrusters)
			{
				if (thisThrust.Enabled)
				{
					if (Vector3D.Dot(thisThrust.WorldMatrix.Backward, desiredDirectionVec) > .7071) //thrusting in desired direction
					{
						thisThrust.ThrustOverridePercentage = 1f;
					}
					else if (Vector3D.Dot(thisThrust.WorldMatrix.Forward, thrustToApply) > 0 && thisController.DampenersOverride)
					{
						var neededThrust = Vector3D.Dot(thrustToApply, thisThrust.WorldMatrix.Forward);
						var outputProportion = MathHelper.Clamp(neededThrust / thisThrust.MaxEffectiveThrust, 0, 1);
						thisThrust.ThrustOverridePercentage = (float)outputProportion;
						thrustToApply -= thisThrust.WorldMatrix.Forward * outputProportion * thisThrust.MaxEffectiveThrust;
					}
					else
					{
						thisThrust.ThrustOverridePercentage = 0.000001f;
					}
				}
			}
		}
		public Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
		{
			if (Vector3D.IsZero(b))
				return Vector3D.Zero;

			return a - a.Dot(b) / b.LengthSquared() * b;
		}
	}
}
