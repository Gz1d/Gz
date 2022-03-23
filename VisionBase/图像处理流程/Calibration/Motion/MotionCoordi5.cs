﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace VisionBase
{
    public class MotionCoordi5:MotionBase
    {
        public override bool SetCoordiPos(double X, double Y, double Theta)
        {
            bool IsOk = true;
            IsOk = IsOk && SetAixsPos(AxisEnum.Coordi5_X1, X);
            IsOk = IsOk && SetAixsPos(AxisEnum.Coordi5_Y1, Y);
            IsOk = IsOk && SetAixsPos(AxisEnum.Coordi5_Theta1, Theta);
            if (MoveEvent == null) MoveEvent = new ManualResetEventSlim();
            MoveEvent.Reset();
            double NowX = 0, NowY = 0, NowTheta = 0;
            Task.Factory.StartNew(new Action(() => {
                while (IsOk) {
                    GetAxisPos(AxisEnum.Coordi5_X, out NowX);
                    GetAxisPos(AxisEnum.Coordi5_Y, out NowY);
                    GetAxisPos(AxisEnum.Coordi5_Theta, out NowTheta);
                    Thread.Sleep(10);
                    if (Math.Abs(NowX - X) < 0.01 && Math.Abs(NowY - Y) < 0.01 && Math.Abs(NowTheta - Theta) < 0.01)  {
                        MoveEvent.Set();
                    }
                }
            }));
            if (MoveEvent.Wait(5000)){
                IsOk = false;
                return true;
            }
            else {
                IsOk = false;
                return false;
            }
        }

        public override bool GetCoordiPos(out double X, out double Y, out double Z,out double Theta)
        {
            bool IsOk = true;
            X = 0;
            Y = 0;
            Z = 0;
            Theta = 0;
            IsOk = IsOk && GetAxisPos(AxisEnum.Coordi5_X, out X);
            IsOk = IsOk && GetAxisPos(AxisEnum.Coordi5_Y, out Y);
            IsOk = IsOk && GetAxisPos(AxisEnum.Coordi5_Z, out Z);
            IsOk = IsOk && GetAxisPos(AxisEnum.Coordi5_Theta, out Theta);
            return IsOk;
        }

    }
}
