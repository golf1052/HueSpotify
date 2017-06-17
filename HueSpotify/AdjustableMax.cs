namespace HueSpotify
{
    public class AdjustableMax
    {
        public float CurrentMax { get; private set; }
        private float lastValue;
        private float value;
        public float Value
        {
            get
            {
                if (CurrentMax == 0)
                {
                    return 0;
                }
                float returningValue = value / CurrentMax;
                return returningValue;
            }
            set
            {
                this.lastValue = this.value;
                this.value = value;
                if (CurrentMax < value)
                {
                    CurrentMax = value;
                }
            }
        }

        public AdjustableMax()
        {
            CurrentMax = 0;
            value = 0;
        }

        public void Reset()
        {
            Reset(0);
        }

        public void Reset(float percent)
        {
            CurrentMax *= percent;
            value = 0;
        }
    }
}
