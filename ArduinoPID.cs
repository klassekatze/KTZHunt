using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		public class ArduinoPID
		{

			public ArduinoPID(double Kp, double Ki, double Kd, double timestep, double min, double max) : this(0, 0, Kp, Ki, Kd, P_ON_E, 0)
			{
				SetSampleTime((int)(timestep * 1000));
				SetOutputLimits(min, max);
				SetMode(AUTOMATIC);
				SetControllerDirection(REVERSE);
			}

			public double runStep(double input)
			{
				myInput = input;
				Compute();
				return myOutput;
			}

			//


			static readonly int AUTOMATIC = 1;
			//static readonly int MANUAL = 0;
			//static readonly int DIRECT = 0;
			static readonly int REVERSE = 1;
			//static readonly int P_ON_M = 0;
			static readonly int P_ON_E = 1;




			double dispKp;              // * we'll hold on to the tuning parameters in user-entered 
			double dispKi;              //   format for display purposes
			double dispKd;              //

			double kp;                  // * (P)roportional Tuning Parameter
			double ki;                  // * (I)ntegral Tuning Parameter
			double kd;                  // * (D)erivative Tuning Parameter

			int controllerDirection;
			int pOn;

			double myInput;              // * Pointers to the Input, Output, and Setpoint variables
			double myOutput;             //   This creates a hard link between the variables and the 
			double mySetpoint;           //   PID, freeing the user from having to constantly tell us
										 //   what these values are.  with pointers we'll just know.

			//DateTime lastTime;
			double outputSum, lastInput;

			long SampleTime;
			double outMin, outMax;
			bool inAuto, pOnE;





			/*Constructor (...)*********************************************************
             *    The parameters specified here are those for for which we can't set up
             *    reliable defaults, so we need to have the user set them.
             ***************************************************************************/
			public ArduinoPID(double Input, /*double Output, */double Setpoint,
					double Kp, double Ki, double Kd, int POn, int ControllerDirection)
			{
				//myOutput = Output;
				myInput = Input;
				mySetpoint = Setpoint;
				inAuto = false;

				SetOutputLimits(0, 255);                //default output limit corresponds to
														//the arduino pwm limits

				SampleTime = 10;                           //default Controller Sample Time is 0.1 seconds

				SetControllerDirection(ControllerDirection);
				SetTunings(Kp, Ki, Kd, POn);

				//lastTime = millis()-SampleTime;
			}




			/* Compute() **********************************************************************
             *     This, as they say, is where the magic happens.  this function should be called
             *   every time "void loop()" executes.  the function will decide for itself whether a new
             *   pid Output needs to be computed.  returns true when the output is computed,
             *   false when nothing has been done.
             **********************************************************************************/
			public bool Compute()
			{
				if (!inAuto) return false;
				//var now = DateTime.Now;// millis();
				//long timeChange = (long)(now - lastTime).TotalMilliseconds;
				//if (timeChange >= SampleTime)
				{
					/*Compute all the working error variables*/
					double input = myInput;
					double error = mySetpoint - input;
					double dInput = (input - lastInput);
					outputSum += (ki * error);

					/*Add Proportional on Measurement, if P_ON_M is specified*/
					if (!pOnE) outputSum -= kp * dInput;

					if (outputSum > outMax) outputSum = outMax;
					else if (outputSum < outMin) outputSum = outMin;

					/*Add Proportional on Error, if P_ON_E is specified*/
					double output;
					if (pOnE) output = kp * error;
					else output = 0;

					/*Compute Rest of PID Output*/
					output += outputSum - kd * dInput;

					if (output > outMax) output = outMax;
					else if (output < outMin) output = outMin;
					myOutput = output;

					/*Remember some variables for next time*/
					lastInput = input;
					//lastTime = now;
					return true;
				}
				//else return false;
			}

			/* SetTunings(...)*************************************************************
             * This function allows the controller's dynamic performance to be adjusted.
             * it's called automatically from the constructor, but tunings can also
             * be adjusted on the fly during normal operation
             ******************************************************************************/
			public void SetTunings(double Kp, double Ki, double Kd, int POn)
			{
				if (Kp < 0 || Ki < 0 || Kd < 0) return;

				pOn = POn;
				pOnE = POn == P_ON_E;

				dispKp = Kp; dispKi = Ki; dispKd = Kd;

				double SampleTimeInSec = ((double)SampleTime) / 1000;
				kp = Kp;
				ki = Ki * SampleTimeInSec;
				kd = Kd / SampleTimeInSec;

				if (controllerDirection == REVERSE)
				{
					kp = (0 - kp);
					ki = (0 - ki);
					kd = (0 - kd);
				}
			}

			/* SetTunings(...)*************************************************************
             * Set Tunings using the last-rembered POn setting
             ******************************************************************************/
			public void SetTunings(double Kp, double Ki, double Kd)
			{
				SetTunings(Kp, Ki, Kd, pOn);
			}

			/* SetSampleTime(...) *********************************************************
             * sets the period, in Milliseconds, at which the calculation is performed
             ******************************************************************************/
			void SetSampleTime(int NewSampleTime)
			{
				if (NewSampleTime > 0)
				{
					double ratio = (double)NewSampleTime
									/ (double)SampleTime;
					ki *= ratio;
					kd /= ratio;
					SampleTime = (long)NewSampleTime;
				}
			}

			/* SetOutputLimits(...)****************************************************
             *     This function will be used far more often than SetInputLimits.  while
             *  the input to the controller will generally be in the 0-1023 range (which is
             *  the default already,)  the output will be a little different.  maybe they'll
             *  be doing a time window and will need 0-8000 or something.  or maybe they'll
             *  want to clamp it from 0-125.  who knows.  at any rate, that can all be done
             *  here.
             **************************************************************************/
			public void SetOutputLimits(double Min, double Max)
			{
				if (Min >= Max) return;
				outMin = Min;
				outMax = Max;

				if (inAuto)
				{
					if (myOutput > outMax) myOutput = outMax;
					else if (myOutput < outMin) myOutput = outMin;

					if (outputSum > outMax) outputSum = outMax;
					else if (outputSum < outMin) outputSum = outMin;
				}
			}

			/* SetMode(...)****************************************************************
             * Allows the controller Mode to be set to manual (0) or Automatic (non-zero)
             * when the transition from manual to auto occurs, the controller is
             * automatically initialized
             ******************************************************************************/
			public void SetMode(int Mode)
			{
				bool newAuto = (Mode == AUTOMATIC);
				if (newAuto && !inAuto)
				{  /*we just went from manual to auto*/
					Initialize();
				}
				inAuto = newAuto;
			}

			/* Initialize()****************************************************************
             *	does all the things that need to happen to ensure a bumpless transfer
             *  from manual to automatic mode.
             ******************************************************************************/
			public void Initialize()
			{
				outputSum = myOutput;
				lastInput = myInput;
				if (outputSum > outMax) outputSum = outMax;
				else if (outputSum < outMin) outputSum = outMin;
			}

			/* SetControllerDirection(...)*************************************************
             * The PID will either be connected to a DIRECT acting process (+Output leads
             * to +Input) or a REVERSE acting process(+Output leads to -Input.)  we need to
             * know which one, because otherwise we may increase the output when we should
             * be decreasing.  This is called from the constructor.
             ******************************************************************************/
			public void SetControllerDirection(int Direction)
			{
				if (inAuto && Direction != controllerDirection)
				{
					kp = (0 - kp);
					ki = (0 - ki);
					kd = (0 - kd);
				}
				controllerDirection = Direction;
			}
		}
	}
}
