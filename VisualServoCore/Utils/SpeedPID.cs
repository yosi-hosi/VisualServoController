using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualServoCore
{
    class SpeedPID
    {

        // ------- fields ------- //

        private readonly double _KP;
        private readonly double _KI;
        private readonly double _KD;
        private readonly double _dt; //処理周期
        private double _previousError;
        private double _previousError2;
        private double _previousControl;

        // ------- constructors ------- //

        public SpeedPID(double KP, double KI, double KD, double dt)
        {
            _KP = KP;
            _KI = KI;
            _KD = KD;
            _dt = dt;
        }

        // ------- public methods ------- //

        public double GetControl(double error)
        {
            double dP, dI, dD, control;

            dP = - _KP * (error - _previousError) / _dt;
            dI = - _KI * error;
            dD = - _KD * (error - 2 * _previousError + _previousError2) / _dt / _dt;

            _previousError2 = _previousError;
            _previousError = error;
            control = _previousControl + dP + dI + dD;
            _previousControl += dP + dI + dD;

            return control;
        }
    }
}
