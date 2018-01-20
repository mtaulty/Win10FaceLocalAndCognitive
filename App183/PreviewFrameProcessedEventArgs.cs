using System;
namespace App183
{
  using Windows.Media;

  class PreviewFrameProcessedEventArgs<T> : EventArgs
  {
    public PreviewFrameProcessedEventArgs()
    {

    }
    public PreviewFrameProcessedEventArgs(
      T processingResults,
      VideoFrame frame)
    {
      this.Results = processingResults;
      this.Frame = frame;
    }
    public T Results { get; set; }
    public VideoFrame Frame { get; set; }
  }
}
