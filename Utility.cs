


using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;



namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		public static bool tickcheck(ref int timer, int interval)
		{
			if (tick - timer > interval)
			{
				timer = tick - (timer % interval);
				return true;
			}
			return false;
		}
		public static string dist2str(double d)
		{
			if (d > 1000)
			{
				return (d / 1000).ToString("0.0") + "km";
			}
			else return d.ToString("0") + "m";
		}
		public static string v2ss(Vector3D v)
		{
			return "<" + v.X.ToString("0.0000") + "," + v.Y.ToString("0.0000") + "," + v.Z.ToString("0.0000") + ">";
		}
		static public double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
		{
			if (a.LengthSquared() == 0 || b.LengthSquared() == 0)
				return 0;
			else
				return Math.Acos(MathHelper.Clamp(a.Dot(b) / a.Length() / b.Length(), -1, 1));
		}

		public static double CalculateVelocity(double PAtTMinus2, double PAtTMinus1, double PAtTMinus0)
		{
			return PAtTMinus2 / 2.0 - 2.0 * PAtTMinus1 + 3.0 * PAtTMinus0 / 2.0;
		}

		/// <summary>
		/// Whip's ApplyGyroOverride Method v9 - 8/19/17
		/// use after pid controlling the speeds
		/// </summary>
		public static void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference)
		{
			var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs 
			var shipMatrix = reference.WorldMatrix;
			var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix);

			foreach (var thisGyro in gyro_list)
			{
				var gyroMatrix = thisGyro.WorldMatrix;
				var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));

				thisGyro.Pitch = (float)transformedRotationVec.X;
				thisGyro.Yaw = (float)transformedRotationVec.Y;
				thisGyro.Roll = (float)transformedRotationVec.Z;
				thisGyro.GyroOverride = true;
			}
		}

		public static void setGyrolock(List<IMyGyro> gyro_list, bool lck)
		{
			foreach (var thisGyro in gyro_list)
			{
				thisGyro.GyroOverride = lck;
			}
		}

		/// <summary>
		/// Whip's GetRotationAnglesSimultaneous - Last modified: 07/05/2020
		/// Gets axis angle rotation and decomposes it upon each cardinal axis.
		/// Has the desired effect of not causing roll oversteer. Does NOT use
		/// sequential rotation angles.
		/// Set desiredUpVector to Vector3D.Zero if you don't care about roll.
		/// Dependencies:
		/// SafeNormalize
		/// </summary>
		public static void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double yaw, out double pitch, out double roll)
		{
			desiredForwardVector = SafeNormalize(desiredForwardVector);

			MatrixD transposedWm;
			MatrixD.Transpose(ref worldMatrix, out transposedWm);
			Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
			Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

			Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
			Vector3D axis;
			double angle;
			if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector))
			{
				axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
				angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
			}
			else
			{
				leftVector = SafeNormalize(leftVector);
				Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);

				// Create matrix
				MatrixD targetMatrix = MatrixD.Zero;
				targetMatrix.Forward = desiredForwardVector;
				targetMatrix.Left = leftVector;
				targetMatrix.Up = upVector;

				axis = new Vector3D(targetMatrix.M23 - targetMatrix.M32,
									targetMatrix.M31 - targetMatrix.M13,
									targetMatrix.M12 - targetMatrix.M21);

				double trace = targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33;
				angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1));
			}

			if (Vector3D.IsZero(axis))
			{
				angle = desiredForwardVector.Z < 0 ? 0 : Math.PI;
				yaw = angle;
				pitch = 0;
				roll = 0;
				return;
			}

			axis = SafeNormalize(axis);
			yaw = -axis.Y * angle;
			pitch = axis.X * angle;
			roll = -axis.Z * angle;
		}

		public static Vector3D SafeNormalize(Vector3D a)
		{
			if (Vector3D.IsZero(a))
				return Vector3D.Zero;

			if (Vector3D.IsUnit(ref a))
				return a;

			return Vector3D.Normalize(a);
		}
		static double d180bypi = (180 / Math.PI);
		public static double ConvertRadiansToDegrees(double radians)
		{
			double degrees = d180bypi * radians;
			return (degrees);
		}
		static double dpiby180 = (Math.PI / 180);
		public static double ConvertDegreesToRadians(double degrees)
		{
			double radians = dpiby180 * degrees;
			return (radians);
		}
		public static double ClosestApproachDistance(Vector3D target, Vector3D target_velocity, Vector3D me, Vector3D me_velocity)
		{
			// This makes the calculation much simpler by changing the frame of
			// reference so that the target appears stationary in relation to me.
			// Now it's a matter of calculating closest approach between a point and
			// a ray, instead of two rays.
			Vector3D target_relative_velocity = me_velocity - target_velocity;

			// Initial distance between you and the target. This becomes the hypotenuse
			// of a right triangle.
			double initial_distance = Vector3D.Distance(me, target);

			// Use the dot product of the normalized vector to target and my normalized
			// velocity to determine the angle of approach.
			double angle_of_approach = Math.Acos(Vector3D.Dot(Vector3D.Normalize(target - me), Vector3D.Normalize(target_relative_velocity)));

			if (angle_of_approach < Math.PI * 0.5) // Make sure the projectile is headed toward the target.
			{
				// We have the hypotenuse and theta, so now it's just trig to determine
				// distance of closest approach.
				return Math.Sin(angle_of_approach) * initial_distance;
			}
			else
			{
				// I'm already moving tangent or headed away from the target. We're not
				// getting any closer than we already are.
				return initial_distance;
			}
		}

		// cpa_time(): compute the time of CPA for two tracks
		//    Input:  two tracks Tr1 and Tr2
		//    Return: the time at which the two tracks are closest
		public static double cpa_time(Vector3D Tr1_p, Vector3D Tr1_v, Vector3D Tr2_p, Vector3D Tr2_v)
		{
			Vector3D dv = Tr1_v - Tr2_v;

			double dv2 = Vector3D.Dot(dv, dv);
			if (dv2 < 0.00000001)      // the  tracks are almost parallel
				return 0.0;             // any time is ok.  Use time 0.

			Vector3D w0 = Tr1_p - Tr2_p;
			double cpatime = -Vector3D.Dot(w0, dv) / dv2;

			return cpatime;             // time of CPA
		}

		public static string Vector2GPSString(string l, Vector3D v)
		{
			//GPS: klassekatze #2:4186.55:17490.12:19153.15:#FFB775F1:
			return "GPS:" + l + ":" + v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00") + ":#FFB775F1:";
		}
	}
}

