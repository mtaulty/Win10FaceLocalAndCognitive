namespace App183
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Windows.Graphics.Imaging;
  using Windows.Media.Capture;
  using Windows.Media.FaceAnalysis;
  using Windows.Media.MediaProperties;

  class FaceDetectionFrameProcessor : PreviewFrameProcessor<IReadOnlyList<BitmapBounds>>
  {
    FaceDetector detector;

    public FaceDetectionFrameProcessor(MediaCapture capture,
      VideoEncodingProperties videoProperties) : base(capture, videoProperties)
    {

    }
    protected override async Task InitialiseForProcessingLoopAsync()
    {
      this.detector = await FaceDetector.CreateAsync();
    }
    protected override BitmapPixelFormat BitmapFormat
    {
      get
      {
        var supportedBitmapFormats = FaceDetector.GetSupportedBitmapPixelFormats();
        return (supportedBitmapFormats.First());
      }
    }
    protected async override Task<IReadOnlyList<BitmapBounds>> ProcessBitmapAsync(SoftwareBitmap bitmap)
    {
      var faces = await this.detector.DetectFacesAsync(bitmap);

      return (faces.Select(f => f.FaceBox).ToList().AsReadOnly());
    }
  }
}
