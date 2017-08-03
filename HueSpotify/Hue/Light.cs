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

        public Light(string baseUrl, string name)
        {
            this.baseUrl = baseUrl;
            Name = name;
            LastBrightness = 0;
            httpClient = new HttpClient();
        }

        public async Task Update()
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{baseUrl}/lights/{Name}");
            JObject responseObject = JObject.Parse(await response.Content.ReadAsStringAsync());
            LastBrightness = (byte)responseObject["state"]["bri"];
        }

        public void SetBrightness(byte brightness)
        {
            JObject o = GetDefaultObject();
            o["bri"] = brightness;
            LastBrightness = brightness;
            httpClient.PutAsync($"{baseUrl}/lights/{Name}/state", o.ToStringContent());
        }

        public void SetBrightness(byte brightness, byte transitionTime)
        {
            JObject o = new JObject();
            o["transitiontime"] = transitionTime;
            o["bri"] = brightness;
            LastBrightness = brightness;
            httpClient.PutAsync($"{baseUrl}/lights/{Name}/state", o.ToStringContent());
        }

        JObject GetDefaultObject()
        {
            JObject o = new JObject();
            o["transitiontime"] = 0;
            return o;
        }
    }
}
