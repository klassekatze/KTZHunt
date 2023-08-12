using EmptyKeys.UserInterface.Generated.ModIoConsentView_Bindings;
using ParallelTasks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{


		Vector3D directionTo(Vector3D a, Vector3D b)
		{
			Vector3D offset = b - a;
			Vector3D forward;
			Vector3D.Normalize(ref offset, out forward);
			return forward;
		}

		double steppedPredictive = 0;
		ArduinoPID predictPID = new ArduinoPID(3.4, 0, 0.13, 1 / 60, 500.0d / 60, 500.0d / 60);
		void resetPID()
		{
			predictPID = new ArduinoPID(3.4, 0, 0.13, 1 / 60, 500.0d / 60, 500.0d / 60);
		}

		int looselockTicks = 0;
		int lockTicks = 0;

		bool DEBUG = false;

		bool lastab = false;

		string aimbot_txt = "";

		double[] track = new double[60];
		int calls = 0;
		int tpos = 0;

		int minTick = -1;
		public void aimbot()
		{
			if (tick > minTick)
			{
				var ab = false;
				try
				{
					DateTime bf = DateTime.Now;
					ab = aimbot_();
					double ms = (DateTime.Now - bf).TotalMilliseconds;

					//uuuuuugh
					track[tpos] = ms;
					calls += 1;
					tpos = (tpos + 1) % 60;
					double avg = 0;
					foreach (var d in track) avg += d;
					avg = avg / (calls > 60 ? 60 : calls);
					if (avg > 0.3)
					{
						log("delay (JIT?)", LT.LOG_N);
						minTick = tick + 60;//JIT is shit, don't blaze me bro
					}
				}
				catch (Exception) { ab = false; }
				if (!ab && lastab)
				{
					var ctrl = getController();
					ApplyGyroOverride(0, 0, 0, gyros, ctrl);
					setGyrolock(gyros, false);
					aimbot_txt = "";
				}
				lastab = ab;
			}
			else
			{
				track[tpos] = 0;
				calls += 1;
				tpos = (tpos + 1) % 60;
			}
		}

		Vector3D p2;
		Vector3D p1;
		Vector3D p0;
		long id = -1;
		void vrec(Vector3D p, long i)
		{
			if (i != id)
			{
				p2 = p1 = p0 = Vector3D.Zero;
				id = i;
			}
			p2 = p1;
			p1 = p0;
			p0 = p;
		}
		bool lastahead = false;
		int ticksAB = 0;

		const int secs = 5;
		const int trlen = 60 * secs;
		bool[] trackacc = new bool[trlen];
		int tat = 0;
		int totr = 0;
		void rta(bool a)
		{
			trackacc[tat] = a;
			tat = (tat + 1) % trlen;
			totr += 1;
		}
		int rtat()
		{
			int r = 0;
			foreach (bool b in trackacc) if (b) r += 1;
			if (totr > trlen) return r / secs;
			else return -1;
		}

		public IMyTerminalBlock getSubtargeter()
		{
			IMyTerminalBlock targeter = null;
			foreach (var w in subsystemTargeters)
			{
				if (modAPIWeaponCore.IsWeaponReadyToFire(w))
				{
					targeter = w;
					break;
				}
			}
			return targeter;
		}

		IMyTerminalBlock lGun = null;

		class PropCache
		{
			public ITerminalProperty<bool> ovrTProp = null;
			public ITerminalProperty<bool> shtTProp = null;
		}

		Dictionary<string, PropCache> propCache = new Dictionary<string, PropCache>();
		PropCache getProps(IMyTerminalBlock b)
		{
			var n = b.DefinitionDisplayNameText;
			if (propCache.ContainsKey(n)) return propCache[n];
			else
			{
				PropCache c = new PropCache();
				try
				{
					c.ovrTProp = b.GetProperty("WC_Override").AsBool();
					c.shtTProp = b.GetProperty("WC_Shoot").AsBool();
				}
				catch (Exception) { }
				propCache[n] = c;
				return c;
			}
		}

		int firingMode = 1;
		string[] firemodeLabel = {"WC API","WC TRM"};
		int intrcptMode = 1;
		string[] intrcptmodeLabel = { "WC API", "INTERNAL" };

		int fireTick = -1;
		int fireCooldown = 4;
		bool fireDirty = false;
		void afireWeapon(IMyTerminalBlock b)
		{
			if (fireTick + fireCooldown < tick)
			{
				fireTick = tick;
				if (firingMode == 0) modAPIWeaponCore.FireWeaponOnce(b);

				else if (firingMode == 1)
				{
					var p = getProps(b);
					if (p.ovrTProp != null) p.ovrTProp.SetValue(b, true);
					if (p.shtTProp != null) p.shtTProp.SetValue(b, true);
					fireDirty = true;
				}
			}
		}
		void aunfireWeapon(IMyTerminalBlock b)
		{
			if (firingMode == 1)
			{
				if (fireDirty && tick != fireTick)
				{
					var p = getProps(b);
					if (p.ovrTProp != null) p.ovrTProp.SetValue(b, false);
					if (p.shtTProp != null) p.shtTProp.SetValue(b, false);
					fireDirty = false;
				}
			}
		}

		public bool aimbot_()
		{
			//Echo("" + (fixedGuns.Count == 0));
			//Echo("" + (subsystemTargeters.Count == 0));
			if (fixedGuns.Count == 0) return false;
			if (subsystemTargeters.Count == 0)
			{
				aimbot_txt = "no subtargeters, configure a torp launcher for subsystems";
				return false;
			}
			if (gyroECU == null) return false;

			IMyTerminalBlock targeter = getSubtargeter();
			//if (targeter == null) targeter = ctc;
			if (targeter == null)
			{
				aimbot_txt = "subtargeters aren't loaded. loaded torpedo needed";
				return false;
			}

			var t = modAPIWeaponCore.GetWeaponTarget(targeter).GetValueOrDefault();
			//Echo("" + (t.Type));
			if (t.IsEmpty() || (t.Type != MyDetectedEntityType.LargeGrid && t.Type != MyDetectedEntityType.SmallGrid)) return false;

			vrec(t.Position, t.EntityId);
			Vector3D vel = t.Velocity;
			if (p2 != Vector3D.Zero) vel = new Vector3D(CalculateVelocity(p2.X, p1.X, p0.X), CalculateVelocity(p2.Y, p1.Y, p0.Y), CalculateVelocity(p2.Z, p1.Z, p0.Z)) / 60;
			//var ctrl = getController();
			//vel -= ctrl.GetShipVelocities().LinearVelocity/60;

			IMyTerminalBlock fixedGun = null;
			Vector3D gunpos = Vector3D.Zero;
			var range = 0d;
			var distsqr = 0d;
			foreach (var w in fixedGuns)
			{
				if (modAPIWeaponCore.IsWeaponReadyToFire(w))
				{
					range = modAPIWeaponCore.GetMaxWeaponRange(w, 0);
					gunpos = w.GetPosition();
					distsqr = (gunpos - t.Position).LengthSquared();
					if (distsqr < range * range)
					{
						fixedGun = w;
						break;
					}
				}
			}
			if (fixedGun == null)
			{
				fixedGun = fixedGuns[0];
				gunpos = fixedGun.GetPosition();
				range = modAPIWeaponCore.GetMaxWeaponRange(fixedGun, 0);
				distsqr = (gunpos - t.Position).LengthSquared();
			}
			if (fixedGun != lGun)
			{
				lGun = fixedGun;
				looselockTicks = 0;
				lockTicks = 0;
				ticksAB = 0;
			}
			aunfireWeapon(fixedGun);
			//t = modAPIWeaponCore.GetWeaponTarget(fixedGun).GetValueOrDefault();


			//if (t.IsEmpty() || (t.Type != MyDetectedEntityType.LargeGrid && t.Type != MyDetectedEntityType.SmallGrid)) return;

			//if(DEBUG)dAgent.DrawGPS("gun",fixedGun.GetPosition());

			var sc = modAPIWeaponCore.GetWeaponScope(fixedGun, 0);

			//if (DEBUG) dAgent.DrawGPS("scope1x", sc.Item1, Color.Green);
			// if (DEBUG) dAgent.DrawGPS("scope2x", sc.Item1 + sc.Item2, Color.Green);

			//if (DEBUG) dAgent.DrawGPS("trg", t.Position, Color.Green);


			//if (DEBUG) dAgent.DrawLine(sc.Item1, sc.Item1 + (sc.Item2 * range), Color.Red);

			//Echo(v2ss(sc.Item1));
			var firepos = sc.Item1;

			float ps = ammoVelocities.ContainsKey(fixedGun.DefinitionDisplayNameText) ? ammoVelocities[fixedGun.DefinitionDisplayNameText] : 0;
			if (ps == 0)
			{
				aimbot_txt = "err: no ammoVelocities value for \"" + fixedGun.DefinitionDisplayNameText + "\"\n";
				return false;
			}
			aimbot_txt = "";


			var pred = Vector3D.Zero;

			if (intrcptMode == 1)
			{
				var v = controllers[0].GetShipVelocities().LinearVelocity;
				double[] tui = BallisticInterceptSolver.CalculateBallisticIntercept(firepos, Vector3D.Zero, ps, t.Position, t.Velocity);
				if (tui.Length > 0)
				{
					for (int i = 0; i < tui.Length; i++)
					{
						if (tui[i] > 0)
						{
							//aimbot_txt += "tui:" + tui[i] + "\n";
							pred = t.Position + ((Vector3D)t.Velocity * tui[i]);
							break;
						}
					}
					/*for (int i = 0; i < tui.Length; i++)
					{
						aimbot_txt += "tui["+i+"]:" + tui[i] + "\n";
					}*/
				}
			}
			else
			{
				pred = modAPIWeaponCore.GetPredictedTargetPosition(fixedGun, t.EntityId, 0).GetValueOrDefault();
			}

			//var pred = modAPIWeaponCore.GetPredictedTargetPosition(fixedGun, t.EntityId, 0).GetValueOrDefault();
			if (pred != Vector3D.Zero)
			{
				//if (DEBUG) dAgent.DrawGPS("pred", pred, Color.Red);
				//Echo("setting ECU");

				/*Vector3D d_now = directionTo(firepos, pred);
                Vector3D velfrac = t.Velocity * 0.002f;
                Vector3D d_later = directionTo(firepos, pred + velfrac);*/

				//Vector3D vel = t.Velocity;
				//var ctrl = getController();
				//vel -= ctrl.GetShipVelocities().LinearVelocity;


				Vector3D aim = directionTo(firepos, pred);
				Vector3D aim_later = directionTo(firepos, pred + (vel * 0.002f));

				var deviation_now = ConvertRadiansToDegrees(VectorAngleBetween(aim, fixedGun.WorldMatrix.Forward));
				var deviation_later = ConvertRadiansToDegrees(VectorAngleBetween(aim_later, fixedGun.WorldMatrix.Forward));

				if (deviation_now > 35)
				{

					steppedPredictive = 0;
					resetPID();
				}
				bool ahead = deviation_now > deviation_later;
				bool behind = deviation_now < deviation_later;
				ticksAB += 1;
				if (ahead != lastahead || (!ahead && !behind))
				{
					lastahead = ahead;
					ticksAB = 0;
				}

				float dev = (float)deviation_now;
				if (ahead) dev = -dev;

				//steppedPredictive += (float)predictPID.runStep(dev);







				//steppedPredictive = (float)predictPID.runStep(dev)*2;
				//else if (behind) steppedPredictive += stepPerSec / 60;
				//if (ahead) steppedPredictive -= vel.Length() / 60;
				//else if (behind) steppedPredictive += vel.Length() / 60;

				//if (steppedPredictive < 0) steppedPredictive = 0;
				//else if (steppedPredictive > 5) steppedPredictive = 5;




				Vector3D p_target = pred + (vel * steppedPredictive * 4);// (vel /60);// + (vel * steppedPredictive);//(now * (1 - steppedPredictive)) + (future * (steppedPredictive));
				aim = directionTo(firepos, p_target);

				Vector3D up = SafeNormalize(vel);






				if (deviation_now > 0.02 && deviation_now < 1)
				{
					if (ahead) steppedPredictive -= deviation_now;// vel.Length() * ticksAB / 8 / 60 / 60;
					else if (behind) steppedPredictive += deviation_now;// vel.Length() * ticksAB / 8 / 60 / 60;
				}
				//if (deviation_now > 1) steppedPredictive = 0;


				/*if (lockTicks == 0)
                {
                    if (ahead) steppedPredictive -= vel.Length() * ticksAB / 8 / 60 / 60;
                    else if (behind) steppedPredictive += vel.Length() * ticksAB / 8 / 60 / 60;
                }*/
				double dist2targ = Math.Sqrt(distsqr);
				double track_error = (fixedGun.WorldMatrix.Forward - aim).Length() * dist2targ;

				//if (fixlcd != null)
				//{
				//    fixlcd.WriteText("trg:" + t.Name);
				//    fixlcd.WriteText("\n" + lockTicks + "," + (rtat())+","+track_error.ToString("0.0")+","+deviation_now.ToString("0.000") + "," + steppedPredictive.ToString("0.0"), true);
				// }
				aimbot_txt += "aimbot track error: " + track_error.ToString("0.0") + "m\n";
				aimbot_txt += deviation_now.ToString("0.000") + " degrees off target\n";
				aimbot_txt += "fire mode: " + firemodeLabel[firingMode] + "\n";
				aimbot_txt += "ballistic solver: " + intrcptmodeLabel[intrcptMode] + "\n";

				if (track_error < looseLockTE/*deviation_now < 0.05*/)
				{
					looselockTicks += 1;
				}
				else looselockTicks = 0;

				if (track_error < lockTE/*deviation_now < 0.05*/)
				{
					lockTicks += 1;
					rta(true);
				}
				else
				{
					lockTicks = 0;
					rta(false);
				}


				if (fireWeapon && (lockTicks > minlockTicks && looselockTicks > minlooseLockTicks) && modAPIWeaponCore.IsWeaponReadyToFire(fixedGun))
				{
					afireWeapon(fixedGun);
					lockTicks = 0;
				}

				var o = gyroECU.rotateToTarget(aim, fixedGun, fixedGun.WorldMatrix.Up, true);
				//Echo("" + o);
				return true;
			}
			else aimbot_txt = "aimbot: no solution\n";
			//else Echo("empty pred");

			return false;
			//dAgent.DrawGPS("scope2", sc.Item2);


			/*f (distsqr < range * range)
            {

            }*/











			//var r = modAPIWeaponCore.GetMaxWeaponRange(fixedGun, 0);
			//if(t.)


		}
		public static double looseLockTE = 5;
		public static double lockTE = 2;
		public static int minlooseLockTicks = 60;
		public static int minlockTicks = 3;
		public static bool fireWeapon = true;
	}
}
