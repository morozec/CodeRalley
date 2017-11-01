using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk
{
    class FrictionCalculator
    {
        private const double Eps = 0.00001;
        public static double ApplyA(double speed, double a)
        {
            var resSpeed = speed + a;
            if (resSpeed < 0) resSpeed = 0;         

            return resSpeed;
        }
    }
}
