using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HueSpotify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AudioGraph audioGraph;
        AudioFrameOutputNode frameOutputNode;

        DeviceInformation audioInput;
        DeviceInformation audioOutput;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var audioInputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var device in audioInputDevices)
            {
                if (device.Name.Contains("Output"))
                {
                    audioInput = device;
                }
            }
            var audioOutputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender);
            foreach (var device in audioOutputDevices)
            {
                if (device.Name.Contains("Input"))
                {
                    audioOutput = device;
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

            //for (int i = 0; i < 220; i++)
            //{
            //    maxListLeft.Add(new AdjustableMax());
            //    maxListRight.Add(new AdjustableMax());
            //}

            audioGraph.QuantumProcessed += AudioGraph_QuantumProcessed;
            audioGraph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
            audioGraph.Start();
            inputNode.Start();
            //outputNode.Start();
            frameOutputNode.Start();
        }

        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            AudioFrame audioFrame = frameOutputNode.GetFrame();
            List<float[]> amplitudeData = HelperMethods.ProcessFrameOutput(audioFrame);
            List<float[]> channelData = HelperMethods.GetFftData(HelperMethods.ConvertTo512(amplitudeData, audioGraph), audioGraph);
            for (int i = 0; i < channelData.Count / 2; i++)
            {
                float[] leftChannel = channelData[i];
                float[] rightChannel = channelData[i + 1];

                for (int j = 0; j < 220; j++)
                {
                    //maxListLeft[j].Value = leftChannel[j];
                    //maxListRight[j].Value = rightChannel[j];
                }

                //averageLowLeft.Value = HelperMethods.Average(leftChannel, 0, 16);
                //averageLowRight.Value = HelperMethods.Average(rightChannel, 0, 16);
            }
        }

        private void AudioGraph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            Debug.WriteLine("UNRECOVERABLE ERRORRRRRR");
        }
    }
}
