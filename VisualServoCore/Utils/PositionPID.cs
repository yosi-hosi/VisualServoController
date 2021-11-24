using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualServoCore
{
    class PositionPID
    {

        // ------- fields ------- //

        private readonly double _KP;
        private readonly double _KI;
        private readonly double _KD;
        private readonly double _dt; //処理周期
        private double _previousError;
        private double _integratedError;

        // ------- constructors ------- //

        public PositionPID(double  KP, double KI, double KD, double dt)
        {
            _KP = KP;
            _KI = KI;
            _KD = KD;
            _dt = dt;
        }

        // ------- public methods ------- //

        public double GetControl(double error)
        {
            double P, I, D;
            _integratedError += (error + _previousError) * _dt / 2;

            P = - _KP * error;
            I = - _KI * _integratedError * _dt;
            D = - _KD * (error - _previousError) / _dt;

            _previousError = error;

            return P + I + D;
        }
    }
}
