using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace HueSpotify
{
    public class Timer
    {
        DispatcherTimer dispatcherTimer;
        private DateTime time;
        private TimeSpan resetTime;
        private float resetValue;
        private Action<float> resetFunc;
        private Action func;

        public Timer(TimeSpan resetTime, Action<float> resetFunc, float resetValue, bool useDispatcherTimer = true)
        {
            dispatcherTimer = new DispatcherTimer();
            this.resetTime = resetTime;
            this.resetValue = resetValue;
            this.resetFunc = resetFunc;
            if (useDispatcherTimer)
            {
                dispatcherTimer.Interval = resetTime;
                dispatcherTimer.Tick += DispatcherTimer_Tick;
                dispatcherTimer.Start();
            }
        }

        public Timer(TimeSpan resetTime, Action func)
        {
            dispatcherTimer = new DispatcherTimer();
            this.resetTime = resetTime;
            this.func = func;
            dispatcherTimer.Interval = resetTime;
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, object e)
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
