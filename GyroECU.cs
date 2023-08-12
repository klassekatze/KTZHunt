using Sandbox.ModAPI.Ingame;
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
		public class GyroECU
		{
			public const double runsPerSecond = 60;
			public const double timeStep = 1 / runsPerSecond;

			public static double gyroMaxRPM = 3.1415;

			// public ArduinoPID yawPID;
			//public ArduinoPID pitchPID;
			// public ArduinoPID rollPID;

			public double proportionalGain = 30;
			public double derivativeGain = 0.13;
			public double integralGain = 0.1;


			public double angleMultiplier = 1;

			List<IMyGyro> gyros = new List<IMyGyro>();
			IMyTerminalBlock fwd_ref;

			public GyroECU(List<IMyGyro> g, IMyTerminalBlock refer) : base()
			{
				fwd_ref = refer;
				gyros = g;
				initialize();
				resetPIDs();
			}

			Dictionary<double, PIDControlSet> PIDControlSets = new Dictionary<double, PIDControlSet>();
			public class PIDControlSet
			{
				public double proportionalGain = 3.5;//0.38;//3.2;//0.380;//2;//3.2;//4;///748;//2;
				public double integralGain = 0.0;
				public double derivativeGain = 0.3;
				public double maxAngularVel = double.MaxValue;
				public double angleMultiplier = 1;

				public PIDControlSet(double Kp, double Ki, double Kd, double maxAngular = 0)
				{
					proportionalGain = Kp;
					integralGain = Ki;
					derivativeGain = Kd;
					if (maxAngular == 0) maxAngularVel = double.MaxValue;
					else maxAngularVel = maxAngular;
					resetPIDs();
				}


				public ArduinoPID yawPID;
				public ArduinoPID pitchPID;
				public ArduinoPID rollPID;
				public void resetPIDs()
				{

					if (gProgram.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small) angleMultiplier = 2;
					else if (gProgram.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large) angleMultiplier = 1;

					yawPID = new ArduinoPID(proportionalGain, integralGain, derivativeGain, timeStep, -gyroMaxRPM * angleMultiplier, gyroMaxRPM * angleMultiplier);
					pitchPID = new ArduinoPID(proportionalGain, integralGain, derivativeGain, timeStep, -gyroMaxRPM * angleMultiplier, gyroMaxRPM * angleMultiplier);
					rollPID = new ArduinoPID(proportionalGain, integralGain, derivativeGain, timeStep, -gyroMaxRPM * angleMultiplier, gyroMaxRPM * angleMultiplier);
				}
				public double pitchSpeed;
				public double yawSpeed;
				public double rollSpeed;
				public void runStep(double pitch, double yaw, double roll)
				{
					pitchSpeed = pitchPID.runStep(pitch);
					yawSpeed = yawPID.runStep(yaw);
					rollSpeed = rollPID.runStep(roll);
				}
			}
			public void runStep(double pitch, double yaw, double roll)
			{
				foreach (PIDControlSet set in PIDControlSets.Values) set.runStep(pitch, yaw, roll);
			}
			PIDControlSet curSet = null;

			int lastSetValidTick = -1;
			public PIDControlSet getSwitchedPID(double deviation, double angularVel)
			{

				PIDControlSet s = null;
				foreach (KeyValuePair<double, PIDControlSet> set in PIDControlSets)
				{
					if (deviation < set.Key && angularVel < set.Value.maxAngularVel)
					{
						s = set.Value;
						break;
					}
				}
				if (s == null) s = PIDControlSets.Last().Value;

				if (s == curSet) lastSetValidTick = tick;

				if (tick - lastSetValidTick < 3 && curSet != null) s = curSet;
				else
				{
					curSet = s;
					curSet.resetPIDs();
				}
				return curSet;
			}

			public void initialize()
			{
				PIDControlSets[2] = new PIDControlSet(14, 0, 0.0, gyroMaxRPM / 8);
				PIDControlSets[4] = new PIDControlSet(10, 0, 0.13, gyroMaxRPM / 6);

				PIDControlSets[8] = new PIDControlSet(5, 0, 0.13, gyroMaxRPM / 3);
				PIDControlSets[25] = new PIDControlSet(4, 0, 0.13, gyroMaxRPM);
				PIDControlSets[30] = new PIDControlSet(3.5, 0, 0.13);
			}




			public void resetPIDs()
			{
				if (fwd_ref.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small) angleMultiplier = 2;
				else if (fwd_ref.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large) angleMultiplier = 1;

				foreach (KeyValuePair<double, PIDControlSet> set in PIDControlSets)
				{
					set.Value.resetPIDs();
				}
			}

			public double thresholdRadian = ConvertDegreesToRadians(0.05);// ConvertDegreesToRadians(3);

			public bool currentlyOnTarget = true;

			public int ticksOnTarget = 0;
			public int ticksOnTargetLimit = 40;

			public void rest()
			{
				currentlyOnTarget = true;
				ApplyGyroOverride(0, 0, 0, gyros, fwd_ref);
			}

			int lastcalltick = -10;

			public double rotateToTarget(Vector3D desired_forward_heading, IMyTerminalBlock fwd, Vector3D up = new Vector3D(), bool force = false)
			{
				if (tick - lastcalltick > 5) resetPIDs();
				lastcalltick = tick;

				double angleBetween = VectorAngleBetween(desired_forward_heading, fwd.WorldMatrix.Forward);
				bool correcting = true;

				if (angleBetween > thresholdRadian) ticksOnTarget = 0;
				else ticksOnTarget += 1;

				if (ticksOnTarget < ticksOnTargetLimit && currentlyOnTarget)
				{
					currentlyOnTarget = false;
					if (!force) resetPIDs();
					correcting = true;
				}
				else if (ticksOnTarget > ticksOnTargetLimit && !currentlyOnTarget)
				{
					currentlyOnTarget = true;
					if (!force) ApplyGyroOverride(0, 0, 0, gyros, fwd);
					correcting = false;
				}

				if (correcting || force)
				{
					double pitch = 0;
					double yaw = 0;
					double roll = 0;
					GetRotationAnglesSimultaneous(desired_forward_heading, up, fwd.WorldMatrix, out yaw, out pitch, out roll);

					double pitchSpeed = 0;
					double yawSpeed = 0;
					double rollSpeed = 0;

					double angularVelocity = gProgram.getController().GetShipVelocities().AngularVelocity.Length();

					var set = getSwitchedPID(ConvertRadiansToDegrees(angleBetween), angularVelocity);
					runStep(pitch, yaw, roll);
					pitchSpeed = set.pitchSpeed;
					yawSpeed = set.yawSpeed;
					rollSpeed = set.rollSpeed;

					//gProgram.Echo("set gyro " + gyros.Count+","+pitchSpeed + "," + yawSpeed + "," + rollSpeed);
					ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, gyros, fwd);
				}
				return angleBetween;
			}
			/*
						double lastPitchSpeed = double.MinValue;
						double lastYawSpeed = double.MinValue;
						double lastRollSpeed = double.MinValue;

						double lastAppliedPitch = double.MinValue;
						double lastAppliedYaw = double.MinValue;
						double lastAppliedRoll = double.MinValue;*/
		}
	}
}
