namespace App183
{
  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using Windows.Devices.Enumeration;
  using Windows.Media.Capture;
  using Windows.Media.MediaProperties;
  using Windows.UI.Xaml.Controls;

  class CameraPreviewManager
  {
    public CameraPreviewManager(CaptureElement captureElement)
    {
      this.captureElement = captureElement;
    }
    public async Task<VideoEncodingProperties> StartPreviewToCaptureElementAsync(
      Func<DeviceInformation, bool> deviceFilter)
    {
      var preferredCamera = await this.GetFilteredCameraOrDefaultAsync(deviceFilter);

      MediaCaptureInitializationSettings initialisationSettings = new MediaCaptureInitializationSettings()
      {
        StreamingCaptureMode = StreamingCaptureMode.Video,
        VideoDeviceId = preferredCamera.Id
      };
      this.mediaCapture = new MediaCapture();

      await this.mediaCapture.InitializeAsync(initialisationSettings);

      this.captureElement.Source = this.mediaCapture;

      await this.mediaCapture.StartPreviewAsync();

      return (this.mediaCapture.VideoDeviceController.GetMediaStreamProperties(
        MediaStreamType.VideoPreview) as VideoEncodingProperties);
    }
    public async Task StopPreviewAsync()
    {
      await this.mediaCapture.StopPreviewAsync();
      this.captureElement.Source = null;
    }
    async Task<DeviceInformation> GetFilteredCameraOrDefaultAsync(
      Func<DeviceInformation, bool> deviceFilter)
    {
      var videoCaptureDevices = await DeviceInformation.FindAllAsync(
        DeviceClass.VideoCapture);

      var selectedCamera = videoCaptureDevices.SingleOrDefault(deviceFilter);

      if (selectedCamera == null)
      {
        // we fall back to the first camera that we can find.
        selectedCamera = videoCaptureDevices.FirstOrDefault();
      }
      return (selectedCamera);
    }
    public MediaCapture MediaCapture
    {
      get
      {
        return (this.mediaCapture);
      }
    }
    public VideoEncodingProperties VideoProperties
    {
      get
      {
        return (this.mediaCapture.VideoDeviceController.GetMediaStreamProperties(
          MediaStreamType.VideoPreview) as VideoEncodingProperties);
      }
    }
    CaptureElement captureElement;
    MediaCapture mediaCapture;
  }
}
