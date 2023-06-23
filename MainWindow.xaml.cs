using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using Wpflearning.Resize;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Reflection;
using System.Collections.Generic;

namespace Wpflearning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage originalImage;
        private System.Windows.Shapes.Rectangle cropRect;
        bool? drawn = false;
        public Line line;
        private Stack<BitmapImage> undo = new Stack<BitmapImage>();
        private Stack<BitmapImage> redo = new Stack<BitmapImage>();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Import_image(object sender, RoutedEventArgs e)
        {
            

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @"C:\Users\HP\source\repos\Wpflearning\Wpflearning\Images";
            openFileDialog.Filter = "Image files (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|All files (*.*)|*.*";

            // Show the OpenFileDialog and wait for the user to select a file
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                // Create a BitmapImage from the selected file and display it on the canvas
                originalImage = new BitmapImage(new System.Uri(openFileDialog.FileName));
                canvas.Width = originalImage.Width;
                canvas.Height = originalImage.Height;
                image.Source = originalImage;
                undo.Clear();
                Remove_lines();
            }


        }
        private void Remove_lines()
        {
            //Clears the canvas
            int count = canvas.Children.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (canvas.Children[i] != image)
                {
                    canvas.Children.RemoveAt(i);
                }
            }
        }


        private System.Windows.Point? lastPoint = null;
        private bool isDragging = false;
        private void Crop_Image(object sender, RoutedEventArgs e)
        {
            stackpanel_colours.Visibility = Visibility.Collapsed;
            isDrawing = false;
            // Attach event handlers for resizing the crop rectangle
            image.MouseDown += CropRect_MouseDown;
            image.MouseUp += CropRect_MouseUp;
            image.MouseMove += CropRect_MouseMove;
            isDragging = true;
        }

        

        private void CropRect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                lastPoint = e.GetPosition(canvas);
                cropRect = new System.Windows.Shapes.Rectangle();
                cropRect.Stroke = System.Windows.Media.Brushes.Red;
                cropRect.StrokeThickness = 2;
                cropRect.Fill = System.Windows.Media.Brushes.Transparent;
                cropRect.HorizontalAlignment = HorizontalAlignment.Center;
                cropRect.VerticalAlignment = VerticalAlignment.Center;
                cropRect.Cursor = Cursors.SizeAll;

                canvas.Children.Add(cropRect);
                isDragging = true;
                image.CaptureMouse();
            }
        }

        private void CropRect_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastPoint != null)
            {
                lastPoint = null;
                image.ReleaseMouseCapture();
                ShowCropConfirmation();
            }
        }

        private void CropRect_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && lastPoint != null)
            {
                System.Windows.Point currentPoint = e.GetPosition(canvas);
                double left = Math.Min(lastPoint.Value.X, currentPoint.X);
                double top = Math.Min(lastPoint.Value.Y, currentPoint.Y);
                double width = Math.Abs(lastPoint.Value.X - currentPoint.X);
                double height = Math.Abs(lastPoint.Value.Y - currentPoint.Y);

                // Ensure the crop rectangle stays within the bounds of the myImage control
                double maxWidth = image.ActualWidth - left;
                double maxHeight = image.ActualHeight - top;
                if (width > maxWidth)
                {
                    width = maxWidth;
                }
                if (height > maxHeight)
                {
                    height = maxHeight;
                }

                cropRect.Margin = new Thickness(left, top, 0, 0);
                cropRect.Width = width;
                cropRect.Height = height;
            }
        }

        private void ShowCropConfirmation()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to crop the image?", "Confirm Crop", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                
                CropButton_Click();
            }
            else
            {
                canvas.Children.Remove(cropRect);
                initialPoint = null;    
            }
        }

        private void CropButton_Click()
        {
           //Margin of the rectangle 
            int cropRectLeft = (int)(cropRect.Margin.Left);
            int cropRectTop = (int)(cropRect.Margin.Top);
            int cropRectWidth = (int)(cropRect.Width);
            int cropRectHeight = (int)(cropRect.Height);

            
            Int32Rect cropRectPosition = new Int32Rect(cropRectLeft, cropRectTop, cropRectWidth, cropRectHeight);

            CroppedBitmap croppedBitmap = new CroppedBitmap((BitmapSource)image.Source, cropRectPosition);

            BitmapImage croppedImage = new BitmapImage();
            croppedImage.BeginInit();
            croppedImage.StreamSource = new MemoryStream(ImageToByte(croppedBitmap));
            croppedImage.EndInit();

            
            canvas.Children.Remove(cropRect);
            canvas.Width = croppedBitmap.Width;
            canvas.Height = croppedBitmap.Height;

            undo.Push((BitmapImage)image.Source);
            redo.Clear();

            image.Source = croppedImage;

        }
        private static byte[] ImageToByte(BitmapSource img)
        {
            byte[] bytes;
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                bytes = stream.ToArray();
            }
            return bytes;
        }


        private void Resize_Image(object sender, RoutedEventArgs e)
        {
            stackpanel_colours.Visibility = Visibility.Collapsed;
            isDrawing = false;
            isDragging = false;
            if (image.Source != null)
            {
                ResizeDailog resizeDialog = new ResizeDailog();
                resizeDialog.Owner = this;
                resizeDialog.ShowDialog();

                if (resizeDialog.DialogResult == true)
                {

                    int newWidth = resizeDialog.NewWidth;
                    int newHeight = resizeDialog.NewHeight;

                    double scaleX = (double)newWidth / (double)image.Source.Width;
                    double scaleY = (double)newHeight / (double)image.Source.Height;

                    var resizedBitmap = new TransformedBitmap((BitmapImage)image.Source, new ScaleTransform(scaleX, scaleY));

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = null;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = new MemoryStream(ImageToByte(resizedBitmap));
                    bitmap.EndInit();


                    undo.Push((BitmapImage)image.Source);
                    redo.Clear();


                    canvas.Width = bitmap.Width;
                    canvas.Height= bitmap.Height;
                    image.Source = bitmap;
                }
            }
        }
        //private byte[] ConvertBitmapSourceToByteArray(BitmapSource bitmapSource)
        //{
            //byte[] data;
            //BitmapEncoder encoder = new PngBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            //using (MemoryStream ms = new MemoryStream())
            //{
                //encoder.Save(ms);
                //data = ms.ToArray();
            //}
            //return data;
        //}

        private bool isDrawing = false;
        private System.Windows.Point? initialPoint = null;
        private SolidColorBrush selectedColor = System.Windows.Media.Brushes.Black;


        private void Draw_onto_Image(object sender, RoutedEventArgs e)
        {
            stackpanel_colours.Visibility = Visibility.Visible;
            isDragging = false;
            image.MouseDown += image_MouseDown;
            image.MouseUp += image_MouseUp;
            image.MouseMove += image_MouseMove;


            isDrawing = !isDrawing;
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && initialPoint != null)
            {
                var currentPoint = e.GetPosition(image);
                line = new Line();
                line.Stroke = selectedColor;
                line.StrokeThickness = 3;
                line.X1 = initialPoint.Value.X;
                line.Y1 = initialPoint.Value.Y;
                line.X2 = currentPoint.X;
                line.Y2 = currentPoint.Y;

                
                canvas.Children.Add(line);
                //image.Children.Add(canvas);
                drawn = true;
                initialPoint = e.GetPosition(image);
            }
        }

        private void image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                drawn = true;
                initialPoint = e.GetPosition(image);
                image.CaptureMouse();
            }
        }

        private void image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            initialPoint = null;
            image.ReleaseMouseCapture();

        }
        //Sets the Colour of the brush
        private void green_click(object sender, RoutedEventArgs e)
        {
            selectedColor = System.Windows.Media.Brushes.LightGreen;
        }

        private void yellow_click(object sender, RoutedEventArgs e)
        {
            selectedColor = System.Windows.Media.Brushes.Yellow;
        }

        private void black_click(object sender, RoutedEventArgs e)
        {
            selectedColor = System.Windows.Media.Brushes.Black;
        }



        private void Apply_Filter(object sender, RoutedEventArgs e)
        {
            stackpanel_colours.Visibility = Visibility.Collapsed;
            isDrawing = false;
            isDragging = false;
            BitmapImage? currentImage = image.Source as BitmapImage;
            if (currentImage == null)
            {
                return;
            }
            ComboBoxItem selectedFilterItem = (ComboBoxItem)filterComboBox.SelectedItem;

            string selectedFilter = filterComboBox.SelectionBoxItem.ToString();

            // Apply the selected filter to the image

            if (selectedFilter == "Warm")
            {

                // Apply the warm filter
                ColorMatrix warmMatrix = new ColorMatrix(new float[][]{
                    new float[] {1.1f, 0, 0, 0, 0},
                    new float[] {0, 1.0f, 0, 0, 0},
                    new float[] {0, 0, 1.05f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}});
                currentImage = ApplyColorMatrix((BitmapImage)image.Source, warmMatrix);
            }

            else if (selectedFilter == "Cool")
            {
                // Apply the cool filter
                ColorMatrix coolMatrix = new ColorMatrix(new float[][]{
                    new float[] {0.9f, 0, 0, 0, 0},
                    new float[] {0, 0.9f, 1.2f, 0, 0},
                    new float[] {0, 0, 1.1f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}});
                currentImage = ApplyColorMatrix((BitmapImage)image.Source, coolMatrix);
            }

            else if (selectedFilter == "Sepia")
            {
                // Apply the sepia filter
                ColorMatrix sepiaMatrix = new ColorMatrix(new float[][]{
                new float[] {0.393f, 0.349f, 0.272f, 0, 0},
                new float[] {0.769f, 0.686f, 0.534f, 0, 0},
                new float[] {0.189f, 0.168f, 0.131f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}});
                currentImage = ApplyColorMatrix((BitmapImage)image.Source, sepiaMatrix);
            }

            undo.Push((BitmapImage)image.Source);
            redo.Clear();


            canvas.Width = currentImage.Width;
            canvas.Height = currentImage.Height;
            image.Source = currentImage;
        }

        private BitmapImage ApplyColorMatrix(BitmapImage sourceImage, ColorMatrix colorMatrix)
        {

            // Convert the BitmapImage to Bitmap
            Bitmap? bitmap = ConvertBitmapImageToBitmap(sourceImage);
            float dpi = 96; 
            if (sourceImage.DpiX > 0 && sourceImage.DpiY > 0)
            {
                dpi = (float)sourceImage.DpiX;
            }
            bitmap.SetResolution(dpi, dpi);

            // Apply the color matrix to the bitmap
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                graphics.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, bitmap.Width, bitmap.Height,
                    GraphicsUnit.Pixel, attributes);
            }

            //Convert the Bitmap back to BitmapImage
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Bmp);
            BitmapImage filteredImage = new BitmapImage();
            filteredImage.BeginInit();
            memoryStream.Seek(0, SeekOrigin.Begin);
            filteredImage.StreamSource = memoryStream;
            filteredImage.CacheOption = BitmapCacheOption.OnLoad;
            filteredImage.EndInit();
            filteredImage.Freeze();

            return filteredImage;
        }


        private static Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(stream);
                Bitmap bitmap = new Bitmap(stream);
                return new Bitmap(bitmap);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (undo.Count > 0)
            {
                // Pop the last edited image from the stack
                BitmapImage previousImage = undo.Pop();
                redo.Push((BitmapImage)image.Source);

                Remove_lines();
                
             

                // Set the previous image as the source of the image control
                canvas.Width = previousImage.Width;
                canvas.Height = previousImage.Height;
                image.Source = previousImage;
            }
        }
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (redo.Count > 0)
            {
                // Push the current image onto the stack
                undo.Push((BitmapImage)image.Source);

                Remove_lines();

                // Set the top image on the stack as the source of the image control
                BitmapImage nextImage = redo.Pop();

                canvas.Width = nextImage.Width;
                canvas.Height = nextImage.Height;
                image.Source = nextImage;
            }
        }
        private void Save_Copy_Click(object sender, RoutedEventArgs e)
        {
            stackpanel_colours.Visibility = Visibility.Collapsed;
            isDragging = false;
            isDrawing = false;
            if (image.Source != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp|Gif|*.gif";

                if (saveFileDialog.ShowDialog() == true)
                {
                    BitmapEncoder encoder = null;
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1:
                            encoder = new PngBitmapEncoder();
                            break;
                        case 2:
                            encoder = new JpegBitmapEncoder();
                            break;
                        case 3:
                            encoder = new BmpBitmapEncoder();
                            break;
                        case 4:
                            encoder = new GifBitmapEncoder();
                            break;
                    }

                    if (encoder != null)
                    {
                        if (drawn == true)
                        {
                            BitmapSource bitimage = (BitmapSource)image.Source;
                            int width = bitimage.PixelWidth;
                            int height = bitimage.PixelHeight;
                            // Create a new BitmapImage with the same dimensions as the original image
                            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
                            DrawingVisual dv = new DrawingVisual();

                            using (DrawingContext dc = dv.RenderOpen())
                            {
                                VisualBrush vb = new VisualBrush(canvas);

                                dc.DrawImage((BitmapSource)image.Source, new Rect(new System.Windows.Point(), new System.Windows.Size(image.ActualWidth,
                                    image.ActualHeight)));
                                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new System.Windows.Size(canvas.ActualWidth, canvas.ActualHeight)));
                            }

                            rtb.Render(dv);


                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            MemoryStream ms = new MemoryStream();
                            PngBitmapEncoder encoder1 = new PngBitmapEncoder();
                            encoder1.Frames.Add(BitmapFrame.Create(rtb));
                            encoder1.Save(ms);
                            bmp.StreamSource = ms;
                            bmp.EndInit();

                          

                            

                            image.Source = bmp;
                            Remove_lines();

                        }

                        // apply other photo editor features here before saving the image

                        encoder.Frames.Add(BitmapFrame.Create((BitmapSource)image.Source));
                        using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        {
                            encoder.Save(stream);
                        }
                        undo.Clear();
                    }
                }
            }
        }

        
    }
}
       
        
    

