using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HueSpotify.Hue
{
    public class Light
    {
        private string baseUrl;
        public string Name { get; set; }
        public byte LastBrightness { get; set; }

        private HttpClient httpClient;
        private Random random;

        public int color;

        public Light(string baseUrl, string name)
        {
            this.baseUrl = baseUrl;
            Name = name;
            LastBrightness = 0;
            httpClient = new HttpClient();
            random = new Random();
        }

        public ushort GetAssignedColor()
        {
            if (color == 0)
            {
                return HelperMethods.GetRandomInRed();
            }
            else if (color == 1)
            {
                return HelperMethods.GetRandomInOrange();
            }
            else if (color == 2)
            {
                return HelperMethods.GetRandomInYellow();
            }
            else if (color == 3)
            {
                return HelperMethods.GetRandomInGreen();
            }
            else if (color == 4)
            {
                return HelperMethods.GetRandomInBlue();
            }
            else if (color == 5)
            {
                return HelperMethods.GetRandomInIndigo();
            }
            else if (color == 6)
            {
                return HelperMethods.GetRandomInViolet();
            }
            return 0;
        }

        public async Task Update()
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{baseUrl}/lights/{Name}");
            JObject responseObject = JObject.Parse(await response.Content.ReadAsStringAsync());
            LastBrightness = (byte)responseObject["state"]["bri"];
        }

        public void DimLight()
        {
            JObject o = GetDefaultObject();
            o["bri"] = 0;
            LastBrightness = 0;
            UpdateState(o);
        }

        public void SetBrightness(byte brightness)
        {
            SetBrightness(brightness, 0);
        }

        public void SetBrightness(byte brightness, byte transitionTime)
        {
            JObject o = GetDefaultObject(transitionTime);
            o["bri"] = brightness;
            LastBrightness = brightness;
            UpdateState(o);
        }

        public void SetRandomColor()
        {
            JObject o = GetDefaultObject();
            o["hue"] = random.Next(255);
            UpdateState(o);
        }

        public void SetColor(ushort color, byte saturation)
        {
            SetColor(color, saturation, 0);
        }

        public void SetColor(ushort color, byte saturation, byte transitionTime)
        {
            JObject o = GetDefaultObject(transitionTime);
            o["hue"] = color;
            o["sat"] = saturation;
            UpdateState(o);
        }

        private void UpdateState(JObject o)
        {
            httpClient.PutAsync($"{baseUrl}/lights/{Name}/state", o.ToStringContent());
        }

        JObject GetDefaultObject(byte transitionTime = 0)
        {
            JObject o = new JObject();
            o["transitiontime"] = transitionTime;
            return o;
        }
    }
}
