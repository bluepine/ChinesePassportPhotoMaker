﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
/*
 * Todos
 * 让绿色方框无法移动过红色区域
 */

namespace ChinesePassportPhotoMaker
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    /*
     *  ObjectManipulationControl objects here, this object type is to track position,
     *  providing scaling/moving function, also acts as image info tracker
     *  
     *  Ideas here is,
     *  _imageViewerControl is the main control, that ImageViewer object in XAML pointed to
     *  _overlayFloatingViewerControl is to control the floating overlay
     *  _loadedImageViewerControl keeps user loaded image info
     *  _exampleImageViewerControl keeps original example image info
     *  
     *  When user loads a image, or switch between example to their own photo
     *  _imageViewerControl will act as a pointer pointing to one of them
     *  
     */
    private ObjectManipulationControl _imageViewerControl = new ObjectManipulationControl(0, 0);
    private ObjectManipulationControl _exampleImageViewerControl = new ObjectManipulationControl(0,0);
    private ObjectManipulationControl _loadedImageViewerControl = new ObjectManipulationControl(0, 0);
    private ObjectManipulationControl _overlayFloatingViewerControl = new ObjectManipulationControl(0, 0);

    public static readonly int  ImageViewerWidth = 354;
    public static readonly int ImageViewerHeight = 472;

    public static readonly Thickness ImageViewerBorderTickness = new Thickness(10);
    public static readonly double ImageViewerBorderWidth = ImageViewerWidth + ImageViewerBorderTickness.Bottom * 2;
    public static readonly double ImageViewerBorderHeight = ImageViewerHeight + ImageViewerBorderTickness.Bottom * 2;

    public static readonly double ImageViewerWidthD = ImageViewerWidth;
    public static readonly double ImageViewerHeightD = ImageViewerHeight;
    public static readonly double ImageWidth = ImageViewerWidth * 2;
    public static readonly double ImageHeight = ImageViewerHeight * 2;
    public static readonly double ImageViewX = ImageViewerWidth / -2.0;
    public static readonly double ImageViewY = ImageViewerHeight / -2.0;
    
    
    public static readonly double YellowBlockWidth = ImageViewerWidth / 2.0;
    
    public static readonly double YellowBlockHeight = 15;
    
    public static readonly double YellowBlockX = (ImageViewerWidth - YellowBlockWidth) /2;
    
    public static readonly double YellowBlockY = UpperRedBlockHeight;
    
    public static double UpperRedBlockHeight
    {
      get { return 10; }
    }
    
    public static double LowerRedBlockHeight
    {
      get { return ImageViewerHeight - UpperRedBlockHeight - OutterRefBlockHeight - YellowBlockHeight ;}
    }
    public static double LowerRedBlockY
    {
      get { return ImageViewerHeight - LowerRedBlockHeight;}
    }
    public static double HorizontalCenterX
    {
      get { return (double)ImageViewerWidth / 2; }
    }
    public static double OutterRefBlockWidth = 253;
    
    public static double OutterRefBlockHeight = 378;
    
    public static double InnerRefBlockWidth
    {
      get { return 177; }
    }
    public static double InnerRefBlockHeight
    {
      get { return 331; }
    }
    public static double InnerRefBlockX
    {
      get { return (OutterRefBlockWidth - InnerRefBlockWidth)/2; }
    }
    public static double InnerRefBlockY
    {
      get { return (OutterRefBlockHeight - InnerRefBlockHeight)/2;}
    }
    public static Rect OutterRefBlock
    {
      get { return new Rect(0, 0, OutterRefBlockWidth, OutterRefBlockHeight); }
    }
    public static Rect InnerRefBlock
    {
      get { return new Rect(InnerRefBlockX, InnerRefBlockY, InnerRefBlockWidth, InnerRefBlockHeight); }
    }

    public static double OverlayFloatingViewerX
    {
      get { return (ImageViewerWidth - OutterRefBlockWidth) / 2.0; }
    }
    public static double OverlayFloatingViewerY
    {
      get { return UpperRedBlockHeight + YellowBlockHeight; }
    }

    private readonly double _overlayFloatingUpperLimit= UpperRedBlockHeight;
    private readonly double _overlayFloatingLowerLimit = ImageViewerHeight - LowerRedBlockHeight - OutterRefBlockHeight;


    public MainWindow()
    {
      InitializeComponent();
      ShowGuidesCheckBox.IsChecked = true;
      ShowHelpersCheckBox.IsChecked = true;
      ShowExampleCheckBox.IsChecked = true;
      UseExamplePhoto();
    }

    /*
     * Custom methods here
     */
    private void UseExamplePhoto()
    {
      if (ImageViewer != null)
      {
        _exampleImageViewerControl = new ObjectManipulationControl(ImageViewX, ImageViewY);
        _exampleImageViewerControl.Image = new BitmapImage(new Uri("pack://application:,,,/ChinesePassportPhotoMaker;component/Resources/Example.png", UriKind.RelativeOrAbsolute));
        _exampleImageViewerControl.SetImageWidthHeight(ImageWidth, ImageHeight);
        SwitchToExampleImage();
      }
      if (OverlayFloating != null)
      {
        _overlayFloatingViewerControl = new ObjectManipulationControl(OverlayFloatingViewerX, OverlayFloatingViewerY);
        _overlayFloatingViewerControl.ResetToDefaultXY();
        Canvas.SetLeft(OverlayFloating, _overlayFloatingViewerControl.GetCoordsX());
        Canvas.SetTop(OverlayFloating, _overlayFloatingViewerControl.GetCoordsY());
      }
    }

    private void SwitchToLoadedImage()
    {
      _imageViewerControl = _loadedImageViewerControl;
      RefreshImageViewer();
    }

    private void SwitchToExampleImage()
    {
      _imageViewerControl = _exampleImageViewerControl;
      _imageViewerControl.ResetToDefaultXY();
      RefreshImageViewer();
    }

    private void RefreshImageViewer()
    {
      ImageViewer.Source = _imageViewerControl.Image;
      ImageViewer.Width = _imageViewerControl.ImageWidth;
      ImageViewer.Height = _imageViewerControl.ImageHeight;
      Canvas.SetLeft(ImageViewer, _imageViewerControl.GetCoordsX());
      Canvas.SetTop(ImageViewer, _imageViewerControl.GetCoordsY());
    }

    /*
     * To resize the rendered bitmap
     * This is from:
     * https://stackoverflow.com/questions/15779564/resize-image-in-wpf/24419190
     */
    private BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin)
    {
      var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

      var group = new DrawingGroup();
      RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
      group.Children.Add(new ImageDrawing(source, rect));

      var drawingVisual = new DrawingVisual();
      using (var drawingContext = drawingVisual.RenderOpen())
        drawingContext.DrawDrawing(group);

      var resizedImage = new RenderTargetBitmap(
          width, height,         // Resized dimensions
          96, 96,                // Default DPI values
          PixelFormats.Default); // Default pixel format
      resizedImage.Render(drawingVisual);

      return BitmapFrame.Create(resizedImage);
    }
    /*
     *  Idea is to pull the Canvas
     *  that holds user-edited image,
     *  Render as image and save to file
     *  (Like an automatic screenshot)
     */
    private void SaveImageFromCanvasToFile()
    {
      Transform transform = ImageCanvas.LayoutTransform;
      ImageCanvas.LayoutTransform = null;
      Size size = new Size(ImageCanvas.Width, ImageCanvas.Height);
      ImageCanvas.Measure(size);
      ImageCanvas.Arrange(new Rect(size));
      RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Default);
      renderBitmap.Render(ImageOnlyCanvas);
      ImageSource im = (ImageSource)renderBitmap.Clone();
      BitmapFrame bp = CreateResizedImage(im, ImageViewerWidth, ImageViewerHeight, 0); // Around 300 DPI

      SaveFileDialog saveFileDialog = new SaveFileDialog();
      saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
      saveFileDialog.Filter =
        "PNG文件|" +
        "*.png";
      if (saveFileDialog.ShowDialog() == true)
      {
        using (FileStream outStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
        {
          PngBitmapEncoder encoder = new PngBitmapEncoder();
          encoder.Frames.Add(bp);
          encoder.Save(outStream);
        }
      }
      ImageOnlyCanvas.LayoutTransform = transform;
    }

    /*
     * UI related events here
     */
    private void ImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
    {
      _imageViewerControl.IsSelected = true;
      _imageViewerControl.SetCoordsMouseDown(
          Mouse.GetPosition(Application.Current.MainWindow).X,
          Mouse.GetPosition(Application.Current.MainWindow).Y);
      //DebugLabel1.Content = Mouse.GetPosition(Application.Current.MainWindow).X.ToString();
      //DebugLabel2.Content = Mouse.GetPosition(Application.Current.MainWindow).Y.ToString();
    }

    private void ImageViewer_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (_imageViewerControl.IsSelected)
      {
        _imageViewerControl.SetCoordsMouseUp();
        //DebugLabel3.Content = Mouse.GetPosition(Application.Current.MainWindow).X.ToString();
        //DebugLabel4.Content = Mouse.GetPosition(Application.Current.MainWindow).Y.ToString();
        _imageViewerControl.IsSelected = false;
      }
    }

    private void ImageOrOverlay_MouseMove(object sender, MouseEventArgs e)
    {
      //DebugLabel5.Content = Mouse.GetPosition(Application.Current.MainWindow).X.ToString();
      //DebugLabel6.Content = Mouse.GetPosition(Application.Current.MainWindow).Y.ToString();
      if (_imageViewerControl.IsSelected && e.LeftButton == MouseButtonState.Pressed)
      {
        _imageViewerControl.SetCoordsMouseInMoving(
        Mouse.GetPosition(Application.Current.MainWindow).X,
        Mouse.GetPosition(Application.Current.MainWindow).Y);
        Canvas.SetLeft(ImageViewer, _imageViewerControl.GetCoordsX());
        Canvas.SetTop(ImageViewer, _imageViewerControl.GetCoordsY());
      }
      else if (_overlayFloatingViewerControl.IsSelected && e.LeftButton == MouseButtonState.Pressed)
      {
        double CurrentY = _overlayFloatingViewerControl.CanvasY +
          _overlayFloatingViewerControl.CoordMouseInMovingY -
          _overlayFloatingViewerControl.CoordMouseDownY;
        _overlayFloatingViewerControl.CoordMouseInMovingY = Mouse.GetPosition(Application.Current.MainWindow).Y;
        CurrentY = _overlayFloatingViewerControl.GetCoordsY();
        if(CurrentY < _overlayFloatingUpperLimit)
          CurrentY = _overlayFloatingUpperLimit;

        if(CurrentY > _overlayFloatingLowerLimit)
          CurrentY = _overlayFloatingLowerLimit;
          
        Canvas.SetTop(OverlayFloating, CurrentY);
        // if (CurrentY > _overlayFloatingUpperLimit && CurrentY < _overlayFloatingLowerLimit)
        // {
        //   Canvas.SetTop(OverlayFloating, _overlayFloatingViewerControl.GetCoordsY());
        // }
        if (CurrentY <= _overlayFloatingUpperLimit)
          _overlayFloatingViewerControl.CanvasY = _overlayFloatingUpperLimit;
        if (CurrentY >= _overlayFloatingLowerLimit)
          _overlayFloatingViewerControl.CanvasY = _overlayFloatingLowerLimit;
      }
    }

    private void ImageViewer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (e.Delta > 0)
      {
        _imageViewerControl.SetImageByPoint(2);
      }
      else if (e.Delta < 0)
      {
        _imageViewerControl.SetImageByPoint(-3);
      }
      ImageViewer.Width = _imageViewerControl.ImageWidth;
      ImageViewer.Height = _imageViewerControl.ImageHeight;
    }

    private void OverlayFloating_MouseDown(object sender, MouseButtonEventArgs e)
    {
      _overlayFloatingViewerControl.IsSelected = true;
      _overlayFloatingViewerControl.CoordMouseDownY = Mouse.GetPosition(Application.Current.MainWindow).Y;
    }

    private void OverlayFloating_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (_overlayFloatingViewerControl.IsSelected)
      {
        _overlayFloatingViewerControl.SetCoordsMouseUpY();
        _overlayFloatingViewerControl.IsSelected = false;
      }
    }

    private void ShowGuidesCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      if (OverlayFloating != null)
      {
        OverlayFloating.Visibility = Visibility.Visible;
      }
      if (OverlayFixed != null)
      {
        OverlayFixed.Visibility = Visibility.Visible;
      }
    }

    private void ShowGuidesCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (OverlayFloating != null)
      {
        OverlayFloating.Visibility = Visibility.Collapsed;
      }
      if (OverlayFixed != null)
      {
        OverlayFixed.Visibility = Visibility.Collapsed;
      }
    }

    private void ShowHelpersCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      if (Helpers != null)
      {
        Helpers.Visibility = Visibility.Visible;
      }
    }

    private void ShowHelpersCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if(Helpers != null)
      {
        Helpers.Visibility = Visibility.Collapsed;
      }
    }

    private void ShowExampleCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      UseExamplePhoto();
    }

    private void ShowExampleCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (_loadedImageViewerControl.Image != null)
      {
        SwitchToLoadedImage();
      }

    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
      openFileDialog.Filter =
        "图片文件|" +
        "*.bmp;*.tif;*.tif;*.png;*.jpg;*.jpeg;|" +
        "所有文件 (*.*)|" +
        "*.*";
      if (openFileDialog.ShowDialog() == true)
      {
        _loadedImageViewerControl = new ObjectManipulationControl(0, 0);
        _loadedImageViewerControl.Image = new BitmapImage(
          new Uri(openFileDialog.FileName, 
          UriKind.Absolute));
        _loadedImageViewerControl.SetImageWidthHeight(ImageWidth, ImageHeight);
        SwitchToLoadedImage();
        ShowExampleCheckBox.IsChecked = false;
      }
    }
    /*
     * https://stackoverflow.com/questions/10238694/example-using-hyperlink-in-wpf
     */
    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
      Hyperlink link = sender as Hyperlink;
      System.Diagnostics.Process.Start(new ProcessStartInfo
      {
        FileName = "http://cs.mfa.gov.cn/zlbg/bgzl/hzlxz/202204/t20220408_10665772.shtml",
        UseShellExecute = true
      });
    }
    private void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
      SaveImageFromCanvasToFile();
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      System.Windows.Application.Current.Shutdown();
    }
  }
}
