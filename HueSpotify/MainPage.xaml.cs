using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using HueSpotify.Hue;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HueSpotify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const int lowSection = 6;
        const int midSection = 100;

        string baseUrl = string.Empty;

        Light bedroom;
        Light bed;
        Light livingRoom;
        Light desk;

        AudioGraph audioGraph;
        AudioFrameOutputNode frameOutputNode;

        DeviceInformation audioInput;
        DeviceInformation audioOutput;

        HttpClient httpClient;

        AverageValue low;
        AverageValue mid;
        AverageValue high;

        List<AdjustableMax> maxListLeft;
        List<AdjustableMax> maxListRight;
        List<Rectangle> rectangles;
        Rectangle lowBar;
        Rectangle midBar;
        Rectangle highBar;

        Random random;

        Timer silenceTimer;
        Timer balanceTimer;
        Timer lightSwitchTimer;

        public MainPage()
        {
            this.InitializeComponent();

            random = new Random();
            httpClient = new HttpClient();
            low = new AverageValue();
            mid = new AverageValue();
            high = new AverageValue();
            maxListLeft = new List<AdjustableMax>(220);
            maxListRight = new List<AdjustableMax>(220);
            rectangles = new List<Rectangle>(220);
            lowBar = GetBasicRectangle();
            lowBar.Fill = new SolidColorBrush(Color.FromArgb(127, 255, 0, 0));
            lowBar.Width = lowSection * 3;
            lowBar.Margin = new Thickness(0, 0, 0, 0);
            rectangleGrid.Children.Add(lowBar);
            midBar = GetBasicRectangle();
            midBar.Fill = new SolidColorBrush(Color.FromArgb(127, 0, 255, 0));
            midBar.Width = (midSection - lowSection) * 3;
            midBar.Margin = new Thickness(lowSection * 3, 0, 0, 0);
            rectangleGrid.Children.Add(midBar);
            highBar = GetBasicRectangle();
            highBar.Fill = new SolidColorBrush(Color.FromArgb(127, 100, 149, 237));
            highBar.Width = (220 - midSection) * 3;
            highBar.Margin = new Thickness(midSection * 3, 0, 0, 0);
            rectangleGrid.Children.Add(highBar);
            for (int i = 0; i < 220; i++)
            {
                maxListLeft.Add(new AdjustableMax());
                maxListRight.Add(new AdjustableMax());
                Rectangle rect = GetBasicRectangle();
                if (i < lowSection)
                {
                    rect.Fill = new SolidColorBrush(Colors.Red);
                }
                else if (i < midSection)
                {
                    rect.Fill = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    rect.Fill = new SolidColorBrush(Colors.CornflowerBlue);
                }
                rect.Width = 3;
                rect.Margin = new Thickness(i * 3, 0, 0, 0);
                rectangleGrid.Children.Add(rect);
                rectangles.Add(rect);
            }
            silenceTimer = new Timer(TimeSpan.FromSeconds(5), Reset, 0, false);
            balanceTimer = new Timer(TimeSpan.FromMinutes(2), Reset, 0.1f);
            lightSwitchTimer = new Timer(TimeSpan.FromSeconds(30), AssignNewColors);
        }

        Rectangle GetBasicRectangle()
        {
            Rectangle r = new Rectangle();
            r.HorizontalAlignment = HorizontalAlignment.Left;
            r.VerticalAlignment = VerticalAlignment.Bottom;
            r.Height = 0;
            return r;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string bridgeIp = await GetBridgeIp();
            baseUrl = $"http://{bridgeIp}/api/{Secrets.HueUsername}";

            bedroom = new Light(baseUrl, "1");
            bed = new Light(baseUrl, "2");
            livingRoom = new Light(baseUrl, "3");
            desk = new Light(baseUrl, "4");

            await TurnOnLights();
            await ResetLights();

            var audioInputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var device in audioInputDevices)
            {
                Debug.WriteLine(device.Name);
                if (device.Name.Contains("Stereo Mix"))
                {
                    audioInput = device;
                    break;
                }
                else if (device.Name.Contains("Output"))
                {
                    audioInput = device;
                    break;
                }
            }
            var audioOutputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            foreach (var device in audioOutputDevices)
            {
                Debug.WriteLine(device.Name);
                if (device.Name.Contains("Speakers (ASUS Xonar DGX Audio Device)"))
                {
                    audioOutput = device;
                    break;
                }
                //if (device.Name.Contains("Speakers (Logitech G930 Gaming Headset)"))
                //{
                //    audioOutput = device;
                //    break;
                //}
                else if (device.Name.Contains("Input"))
                {
                    audioOutput = device;
                    break;
                }
            }

            AudioGraphSettings audioGraphSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            audioGraphSettings.DesiredSamplesPerQuantum = 440;
            audioGraphSettings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Default;
            audioGraphSettings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            audioGraphSettings.PrimaryRenderDevice = audioOutput;
            CreateAudioGraphResult audioGraphResult = await AudioGraph.CreateAsync(audioGraphSettings);
            if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
            {
                Debug.WriteLine("AudioGraph creation failed! " + audioGraphResult.Status);
                return;
            }
            audioGraph = audioGraphResult.Graph;
            CreateAudioDeviceInputNodeResult inputNodeResult = await audioGraph.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Media, audioGraph.EncodingProperties, audioInput);
            if (inputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.WriteLine("AudioDeviceInputNode creation failed! " + inputNodeResult.Status);
                return;
            }
            AudioDeviceInputNode inputNode = inputNodeResult.DeviceInputNode;
            CreateAudioDeviceOutputNodeResult outputNodeResult = await audioGraph.CreateDeviceOutputNodeAsync();
            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.WriteLine("AudioDeviceOutputNode creation failed! " + outputNodeResult.Status);
                return;
            }
            AudioDeviceOutputNode outputNode = outputNodeResult.DeviceOutputNode;
            frameOutputNode = audioGraph.CreateFrameOutputNode();
            inputNode.AddOutgoingConnection(frameOutputNode);
            //inputNode.AddOutgoingConnection(outputNode);

            // sync
            //audioGraph.QuantumStarted += AudioGraph_QuantumStarted;

            // async
            // we want to use the async version because we only update lights when we detect a beat
            // if we were using sync it would take longer to find beats
            audioGraph.QuantumProcessed += AudioGraph_QuantumProcessed;
            audioGraph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
            audioGraph.Start();
            inputNode.Start();
            //outputNode.Start();
            frameOutputNode.Start();

            //DateTime lastTime = DateTime.Now;
            //TimeSpan flipTime = TimeSpan.FromMilliseconds(100);
            //bool isRed = true;
            //JObject redLight = GetDefaultObject();
            //redLight["hue"] = 0;
            //redLight["sat"] = 255;
            //httpClient.PutAsync($"{baseUrl}/groups/0/action", redLight.ToStringContent());
            //while (true)
            //{
            //    if (lastTime + flipTime < DateTime.Now)
            //    {
            //        if (isRed)
            //        {
            //            JObject white = GetDefaultObject();
            //            white["sat"] = 0;
            //            httpClient.PutAsync($"{baseUrl}/groups/0/action", white.ToStringContent());
            //            isRed = false;
            //        }
            //        else
            //        {
            //            JObject red = GetDefaultObject();
            //            red["sat"] = 255;
            //            httpClient.PutAsync($"{baseUrl}/groups/0/action", red.ToStringContent());
            //            isRed = true;
            //        }
            //        lastTime = DateTime.Now;
            //    }
            //}
        }

        //private void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        //{
        //}

        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            ProcessBeats();
        }

        void ProcessBeats()
        {
            AudioFrame audioFrame = frameOutputNode.GetFrame();
            List<float[]> amplitudeData = HelperMethods.ProcessFrameOutput(audioFrame);
            List<float[]> channelData = HelperMethods.GetFftData(HelperMethods.ConvertTo512(amplitudeData, audioGraph), audioGraph);
            if (channelData.Count <= 0)
            {
                return;
            }
            for (int i = 0; i < 1; i++)
            {
                float[] leftChannel = channelData[i];
                float[] rightChannel = channelData[i + 1];

                for (int j = 0; j < 220; j++)
                {
                    maxListLeft[j].Value = leftChannel[j];
                    maxListRight[j].Value = rightChannel[j];
                }

                low.Set(leftChannel, rightChannel, 0, lowSection);
                mid.Set(leftChannel, rightChannel, lowSection, midSection);
                high.Set(leftChannel, rightChannel, midSection);
                int number = random.Next();
                byte brightnessFade = 10;
                if (low.Check(0.5f))
                {
                    livingRoom.SetColor(livingRoom.GetAssignedColor(), 255);
                    desk.SetColor(desk.GetAssignedColor(), 255);
                    byte currentBright = (byte)(low.GetAverage() * 127f);
                    if (currentBright >= livingRoom.LastBrightness)
                    {
                        livingRoom.SetBrightness(currentBright);
                        desk.SetBrightness(currentBright);
                        livingRoom.SetBrightness(0, brightnessFade);
                        desk.SetBrightness(0, brightnessFade);
                    }
                    //SetColor(0, 0, 3, 10);
                }
                //SetBrightness((byte)(low.GetAverage() * 255f), 3);
                if (mid.Check(0.5f))
                {
                    // green: 25500
                    bedroom.SetColor(bedroom.GetAssignedColor(), 255);
                    byte currentBright = (byte)(mid.GetAverage() * 127f);
                    if (currentBright >= bedroom.LastBrightness)
                    {
                        bedroom.SetBrightness(currentBright);
                        bedroom.SetBrightness(0, brightnessFade);
                    }
                    //SetColor(25500, 0, 1, 10);
                }
                //SetBrightness((byte)(mid.GetAverage() * 255f), 0);
                if (high.Check(0.5f))
                {
                    // blue: 46920
                    bed.SetColor(bed.GetAssignedColor(), 255);
                    byte currentBright = (byte)(high.GetAverage() * 127f);
                    if (currentBright >= bed.LastBrightness)
                    {
                        bed.SetBrightness(currentBright);
                        bed.SetBrightness(0, brightnessFade);
                    }
                    //SetColor(46920, 0, 2, 10);
                }
                //SetBrightness((byte)(high.GetAverage() * 255f), 3);

                //Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                //    () =>
                //    {
                //        for (int j = 0; j < rectangles.Count; j++)
                //        {
                //            rectangles[j].Height = maxListLeft[j].Value * 500;
                //        }
                //        lowBar.Height = low.GetAverage() * 500;
                //        midBar.Height = mid.GetAverage() * 500;
                //        highBar.Height = high.GetAverage() * 500;
                //        //if (low.LastCheck)
                //        //{
                //        //    FadeOut("beat", Colors.Red, HorizontalAlignment.Left);
                //        //}
                //        //if (mid.LastCheck)
                //        //{
                //        //    FadeOut("beat", Colors.Green, HorizontalAlignment.Center);
                //        //}
                //        //if (high.LastCheck)
                //        //{
                //        //    FadeOut("beat", Colors.CornflowerBlue, HorizontalAlignment.Right);
                //        //}
                //    }).AsTask().Wait();
            }

            silenceTimer.Update();
            //balanceTimer.Update();
            //lightSwitchTimer.Update();
            if (low.CheckValue(0.01f))
            {
                silenceTimer.Reset();
            }
            // this kills UI performance
            //bedroom.Update();
            //bed.Update();
            //livingRoom.Update();
            //desk.Update();
        }

        void AssignNewColors()
        {
            livingRoom.color = random.Next(0, 7);
            desk.color = random.Next(0, 7);
            bedroom.color = random.Next(0, 7);
            bed.color = random.Next(0, 7);
        }

        void Reset()
        {
            Reset(0);
        }

        void Reset(float percent)
        {
            for (int i = 0; i < 220; i++)
            {
                maxListLeft[i].Reset(percent);
                maxListRight[i].Reset(percent);
            }
            low.Reset(percent);
            mid.Reset(percent);
            high.Reset(percent);
        }

        async Task FadeOut(string text)
        {
            await FadeOut(text, Colors.Black, HorizontalAlignment.Center);
        }

        async Task FadeOut(string text, Color color, HorizontalAlignment horizontalAlignment)
        {
            TextBlock testBlock = new TextBlock();
            testBlock.HorizontalAlignment = horizontalAlignment;
            testBlock.VerticalAlignment = VerticalAlignment.Center;
            var thickness = new Thickness(0, 0, 0, 0);
            testBlock.FontSize = 48;
            testBlock.Text = text;
            testBlock.Foreground = new SolidColorBrush(color);
            rectangleGrid.Children.Add(testBlock);
            while (testBlock.Opacity > 0)
            {
                testBlock.Opacity -= 0.1;
                thickness.Bottom += 5;
                testBlock.Margin = thickness;
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
            rectangleGrid.Children.Remove(testBlock);
        }

        private bool CheckAverageValue(AdjustableMax leftValue, AdjustableMax rightValue, float value)
        {
            return leftValue.Value >= value && rightValue.Value >= value;
        }

        private float GetAverageValue(float leftValue, float rightValue)
        {
            return (leftValue + rightValue) / 2f;
        }

        async Task TurnOnLights()
        {
            HttpResponseMessage response = await httpClient.GetAsync($"{baseUrl}/lights");
            JObject responseObject = JObject.Parse(await response.Content.ReadAsStringAsync());
            foreach (var o in responseObject)
            {
                JObject light = (JObject)o.Value;
                if (!(bool)light["state"]["on"])
                {
                    JObject on = new JObject();
                    on["on"] = true;
                    await httpClient.PutAsync($"{baseUrl}/lights/{o.Key}/state", on.ToStringContent());
                }
            }
        }

        async Task ResetLights()
        {
            JObject reset = GetDefaultObject();
            reset["bri"] = 63;
            reset["ct"] = 500;
            //reset["sat"] = 255;
            //reset["hue"] = 46920;
            await httpClient.PutAsync($"{baseUrl}/groups/0/action", reset.ToStringContent());
        }

        JObject GetDefaultObject()
        {
            JObject o = new JObject();
            o["transitiontime"] = 0;
            return o;
        }

        async Task<string> GetBridgeIp()
        {
            HttpResponseMessage response = await httpClient.GetAsync("https://www.meethue.com/api/nupnp");
            JArray responseObject = JArray.Parse(await response.Content.ReadAsStringAsync());
            return (string)responseObject[0]["internalipaddress"];
        }

        private void AudioGraph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            Debug.WriteLine("UNRECOVERABLE ERRORRRRRR");
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            
        }

        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
        }

        private void rectangleGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
        }

        private void rectangleGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void lowSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            //livingRoom.SetBrightness(255);
            //desk.SetBrightness(255);
            //livingRoom.SetColor((ushort)e.NewValue, 255);
            //desk.SetColor((ushort)e.NewValue, 255);
        }
    }
}
