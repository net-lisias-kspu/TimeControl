﻿namespace TimeControl.KeyBindings
{
    public class WarpToNextKACAlarm : TimeControlKeyBinding
    {
        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private void UpdateDescription()
        {
            SetDescription = Description = "Rails Warp to Next KAC Alarm";
        }

        public WarpToNextKACAlarm()
        {
            TimeControlKeyActionName = TimeControlKeyAction.WarpToNextKACAlarm;
            UpdateDescription();
        }

        public override void Press()
        {
            if (!TimeController.IsReady || !RailsWarpController.IsReady || !KACWrapper.InstanceExists || !(HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
            {
                return;
            }

            if (TimeController.Instance.ClosestKACAlarm != null)
            {
                double TargetUT = TimeController.Instance.ClosestKACAlarm.AlarmTime;
                RailsWarpController.Instance.RailsWarpToUT( TargetUT );
            }
        }
    }
}
/*
All code in this file Copyright(c) 2016 Nate West

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
