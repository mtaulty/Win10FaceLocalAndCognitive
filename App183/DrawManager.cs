namespace App183
{
  using Microsoft.Graphics.Canvas.UI.Xaml;
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using Windows.Foundation;
  using Windows.Graphics.Imaging;
  using Windows.Media.MediaProperties;
  using Windows.UI;

  static class RectExtensions
  {
    public static Rect Inflate(this Rect startRect, Rect containingRect, double inflation)
    {
      double newWidth = startRect.Width * inflation;

      double newHeight = startRect.Height * inflation;

      double newLeft =
        Math.Max(
          containingRect.Left,
          startRect.Left - (newWidth - startRect.Width) / 2.0d);

      double newTop =
        Math.Max(
          containingRect.Top,
          startRect.Top - (newHeight - startRect.Height) / 2.0d);

      return (new Rect(
        newLeft,
        newTop,
        Math.Min(newWidth, (containingRect.Right - newLeft)),
        Math.Min(newHeight, (containingRect.Bottom - newTop))));
    }
  }
  class FacialDrawingHandler
  {
    public FacialDrawingHandler(
      CanvasControl drawCanvas,
      VideoEncodingProperties videoEncodingProperties,
      Color strokeColour)
    {
      this.strokeColour = strokeColour;

      this.videoSize = new Size(videoEncodingProperties.Width,
        videoEncodingProperties.Height);

      this.drawCanvas = drawCanvas;

      this.drawCanvas.Draw += this.OnDraw;

      this.syncContext = SynchronizationContext.Current;
    }
    void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
      var faces = this.latestFaceLocations;

      if (faces != null)
      {
        foreach (var face in faces)
        {
          var scaledBox = this.ScaleVideoBitmapBoundsToDrawCanvasRect(face);

          args.DrawingSession.DrawRectangle(scaledBox, this.strokeColour);
        }
      }
    }
    Rect ScaleVideoBitmapBoundsToDrawCanvasRect(BitmapBounds bounds)
    {
      Rect rect = new Rect(
        (((float)bounds.X / this.videoSize.Width) * this.drawCanvas.ActualWidth),
        (((float)bounds.Y / this.videoSize.Height) * this.drawCanvas.ActualHeight),
        (((float)bounds.Width) / this.videoSize.Width * this.drawCanvas.ActualWidth),
        (((float)bounds.Height / this.videoSize.Height) * this.drawCanvas.ActualHeight));

      rect = rect.Inflate(
        new Rect(0, 0, this.drawCanvas.ActualWidth, this.drawCanvas.ActualHeight),
        INFLATION_FACTOR);

      return (rect);
    }
    internal void SetLatestFrameReceived(IReadOnlyList<BitmapBounds> faceLocations)
    {
      this.latestFaceLocations = faceLocations;

      this.syncContext.Post(
        _ =>
        {
          this.drawCanvas.Invalidate();
        }, null);
    }
    SynchronizationContext syncContext;
    IReadOnlyList<BitmapBounds> latestFaceLocations;
    Size videoSize;
    CanvasControl drawCanvas;
    Color strokeColour;

    const double INFLATION_FACTOR = 1.5d;
  }
}
