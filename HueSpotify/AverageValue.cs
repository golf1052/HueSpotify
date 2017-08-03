using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueSpotify
{
    public class AverageValue
    {
        public AdjustableMax averageLeft;
        public AdjustableMax averageRight;

        private DateTime lastBeatTime;
        private TimeSpan minimumBeatTime;

        public bool LastCheck { get; private set; }

        public AverageValue()
        {
            averageLeft = new AdjustableMax();
            averageRight = new AdjustableMax();
            minimumBeatTime = TimeSpan.FromMilliseconds(400);
        }

        public void Set(float[] leftChannel, float[] rightChannel, int start)
        {
            Set(leftChannel, rightChannel, start, leftChannel.Length);
        }

        public void Set(float[] leftChannel, float[] rightChannel, int start, int stop)
        {
            averageLeft.Value = HelperMethods.Average(leftChannel, start, stop);
            averageRight.Value = HelperMethods.Average(rightChannel, start, stop);
        }

        public float GetAverage()
        {
            return (averageLeft.Value + averageRight.Value) / 2f;
        }

        public bool Check(float value)
        {
            bool loudEnough = averageLeft.Value >= value && averageRight.Value >= value;
            DateTime now = DateTime.Now;
            if (loudEnough && lastBeatTime + minimumBeatTime < now)
            {
                lastBeatTime = now;
                LastCheck = true;
                return LastCheck;
            }
            LastCheck = false;
            return LastCheck;
        }

        public bool CheckValue(float value)
        {
            return averageLeft.Value >= value && averageRight.Value >= value;
        }

        public void Reset()
        {
            averageLeft.Reset();
            averageRight.Reset();
        }

        public void Reset(float percent)
        {
            averageLeft.Reset(percent);
            averageRight.Reset(percent);
        }
    }
}
