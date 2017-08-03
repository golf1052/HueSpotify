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
        private Action func;

        public Timer(TimeSpan resetTime, Action<float> resetFunc, float resetValue)
        {
            this.resetTime = resetTime;
            this.resetValue = resetValue;
            this.resetFunc = resetFunc;
        }

        public Timer(TimeSpan resetTime, Action func)
        {
            this.resetTime = resetTime;
            this.func = func;
        }

        public void Update()
        {
            if (time + resetTime < DateTime.Now)
            {
                Debug.WriteLine($"Timer of {resetTime.TotalSeconds} invoked");
                time = DateTime.Now;
                if (resetFunc != null)
                {
                    resetFunc.Invoke(resetValue);
                }
                else if (func != null)
                {
                    func.Invoke();
                }
            }
        }

        public void Reset()
        {
            time = DateTime.Now;
        }
    }
}
