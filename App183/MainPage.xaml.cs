namespace App183
{
  using Microsoft.ProjectOxford.Face;
  using Microsoft.ProjectOxford.Face.Contract;
  using System;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;
  using Windows.Graphics.Imaging;
  using Windows.Storage.Streams;
  using Windows.UI;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;

  public sealed partial class MainPage : Page
  {
    public MainPage()
    {
      this.InitializeComponent();
    }
    string CurrentVisualState
    {
      get
      {
        return (this.currentVisualState);
      }
      set
      {
        if (this.currentVisualState != value)
        {
          this.currentVisualState = value;
          this.ChangeStateAsync();
        }
      }
    }
    async Task ChangeStateAsync()
    {
      await Dispatcher.RunAsync(
        Windows.UI.Core.CoreDispatcherPriority.Normal,
        () =>
        {
          VisualStateManager.GoToState(this, this.currentVisualState, false);
        }
      );
    }
    async void OnStart(object sender, RoutedEventArgs args)
    {
      this.CurrentVisualState = "Playing";

      this.requestStopCancellationToken = new CancellationTokenSource();

      this.cameraPreviewManager = new CameraPreviewManager(this.captureElement);

      var videoProperties =
        await this.cameraPreviewManager.StartPreviewToCaptureElementAsync(
          vcd => vcd.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);

      this.faceDetectionProcessor = new FaceDetectionFrameProcessor(
        this.cameraPreviewManager.MediaCapture,
        this.cameraPreviewManager.VideoProperties);

      this.drawingHandler = new FacialDrawingHandler(
        this.drawCanvas, videoProperties, Colors.White);

      this.faceDetectionProcessor.FrameProcessed += (s, e) =>
      {
        // This event will fire on the task thread that the face
        // detection processor is running on. 
        this.drawingHandler.SetLatestFrameReceived(e.Results);

        this.CurrentVisualState =
          e.Results.Count > 0 ? "PlayingWithFace" : "Playing";

        this.CopyBitmapForOxfordIfRequestPending(e.Frame.SoftwareBitmap);
      };

      try
      {
        await this.faceDetectionProcessor.RunFrameProcessingLoopAsync(
          this.requestStopCancellationToken.Token);
      }
      catch (OperationCanceledException)
      {
      }
      await this.cameraPreviewManager.StopPreviewAsync();

      this.requestStopCancellationToken.Dispose();

      this.CurrentVisualState = "Stopped";
    }
    void CopyBitmapForOxfordIfRequestPending(SoftwareBitmap bitmap)
    {
      if ((this.copiedVideoFrameComplete != null) &&
       (!this.copiedVideoFrameComplete.Task.IsCompleted))
      {
        // We move to RGBA16 because that is a format that we will then be able
        // to use a BitmapEncoder on to move it to PNG and we *cannot* do async
        // work here because it'll break our processing loop.
        var convertedRgba16Bitmap = SoftwareBitmap.Convert(bitmap,
          BitmapPixelFormat.Rgba16);

        this.copiedVideoFrameComplete.SetResult(convertedRgba16Bitmap);
      }
    }
    void OnStop(object sender, RoutedEventArgs e)
    {
      this.requestStopCancellationToken.Cancel();
    }
    
    async void OnSubmitToOxfordAsync(object sender, RoutedEventArgs e)
    {
      // Because I constantly change visual states in the processing loop, I'm just doing
      // this with some code rather than with visual state changes because those would
      // get quickly overwritten while this work is ongoing.
      this.progressIndicator.Visibility = Visibility.Visible;

      // We create this task completion source which flags our main loop
      // to create a copy of the next frame that comes through and then
      // we pick that up here when it's done...
      this.copiedVideoFrameComplete = new TaskCompletionSource<SoftwareBitmap>();

      var bgra16CopiedFrame = await this.copiedVideoFrameComplete.Task;

      this.copiedVideoFrameComplete = null;

      InMemoryRandomAccessStream destStream = new InMemoryRandomAccessStream();

      // Now going to JPEG because Project Oxford can accept those.
      BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId,
        destStream);

      encoder.SetSoftwareBitmap(bgra16CopiedFrame);

      await encoder.FlushAsync();

      FaceServiceClient faceService = new FaceServiceClient(OxfordApiKey, OxfordEndpoint);

      Face[] faces = await faceService.DetectAsync(
        destStream.AsStream(), true, true, AllFaceAttributes);

      // We now get a bunch of face data for each face but I'm ignoring most of it
      // (like facial landmarks etc) and simply displaying the guess of the age
      // and the gender for the moment.
      if (faces != null)
      {
        txtAge.Text = faces[0].FaceAttributes.Age.ToString();
        txtGender.Text = faces[0].FaceAttributes.Gender.ToString();
      }
      else
      {
        txtAge.Text = "no age";
        txtGender.Text = "no gender";
      }
      this.progressIndicator.Visibility = Visibility.Collapsed;
    }
    string currentVisualState;
    FaceDetectionFrameProcessor faceDetectionProcessor;
    CancellationTokenSource requestStopCancellationToken;
    CameraPreviewManager cameraPreviewManager;
    FacialDrawingHandler drawingHandler;
        static FaceAttributeType[] AllFaceAttributes = new FaceAttributeType[]
            {
                FaceAttributeType.Accessories,
                FaceAttributeType.Age,
                FaceAttributeType.Blur,
                FaceAttributeType.Emotion,
                FaceAttributeType.Exposure,
                FaceAttributeType.FacialHair,
                FaceAttributeType.Gender,
                FaceAttributeType.Glasses,
                FaceAttributeType.Hair,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Makeup,
                FaceAttributeType.Noise,
                FaceAttributeType.Occlusion,
                FaceAttributeType.Smile
            };

#error "Your Oxford/Cognitive API Key for Face and the endpoint go here"
    static readonly string OxfordApiKey = "";
    static readonly string OxfordEndpoint = "";
    volatile TaskCompletionSource<SoftwareBitmap> copiedVideoFrameComplete;
  }
}