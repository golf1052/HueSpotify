using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueSpotify
{
    public class Timer
    {
        private DateTime time;
        private TimeSpan resetTime;
        private float resetValue;
        private Action<float> resetFunc;

        public Timer(TimeSpan resetTime, Action<float> resetFunc, float resetValue)
        {
            this.resetTime = resetTime;
            this.resetValue = resetValue;
            this.resetFunc = resetFunc;
        }

        public void Update()
        {
            if (time + resetTime < DateTime.Now)
            {
                Debug.WriteLine($"Timer of {resetTime.TotalSeconds} invoked");
                time = DateTime.Now;
                resetFunc.Invoke(resetValue);
            }
        }

        public void Reset()
        {
            time = DateTime.Now;
        }
    }
}
