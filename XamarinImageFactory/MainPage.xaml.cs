using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using XamarinImageFactory.Common;
using XamarinImageFactory.Factories;
using XamarinImageFactory.Models;
using XamarinImageFactory.Utitlities;

namespace XamarinImageFactory
{
    public sealed partial class MainPage : Page
    {
        private StorageFolder _folder;
        private string _fileName;
        private StorageFile _file;

        private AndroidImageFactory _androidImageFactory;
        private AndroidDrawablesResult _drawableResults;

        private IOSImageFactory _iosImageFactory;
        private IOSAssetsResult _iosImageResults;

        public MainPage()
        {
            this.InitializeComponent();

            PrepareUIElements();
            Initialize();
        }

        private void Initialize()
        {
            _androidImageFactory = new AndroidImageFactory();
            _iosImageFactory = new IOSImageFactory();

            _drawableResults = _androidImageFactory.CreateAndroidDrawables();
            _iosImageResults = _iosImageFactory.CreateIOSResult();
        }

        private void PrepareUIElements()
        {
            PickButton.Click += OnPickButtonClicked;
            CreateImagesButton.Click += OnCreateImagesButtonClicked;

            LowQualityCheckbox.Checked += OnLowQualitySelected;
            MediumQualityCheckbox.Checked += OnMediumQualitySelected;
            HighQualityCheckbox.Checked += OnHighQualitySelected;
        }

        private async void OnLowQualitySelected(object sender, RoutedEventArgs e)
        {
            var file = _drawableResults.LDPI.File;
            await SetResultImageAsync(file);
        }

        private async void OnMediumQualitySelected(object sender, RoutedEventArgs e)
        {
            var file = _drawableResults.HDPI.File;
            await SetResultImageAsync(file);
        }

        private async void OnHighQualitySelected(object sender, RoutedEventArgs e)
        {
            var file = _drawableResults.XXXHDPI.File;
            await SetResultImageAsync(file);
        }

        private async Task SetResultImageAsync(StorageFile resultFile)
        {
            if (resultFile != null)
            {
                var img = new BitmapImage();
                img = await LoadImage(resultFile);
                ResultImage.Source = img;

                ResultFileInfoTextBlock.Text = await GetImageFileInfoAsync(resultFile, img);
            }
        }

        private async Task<string> GetImageFileInfoAsync(StorageFile file, BitmapImage bitmap)
        {
            var name = file.Name;
            var properties = await file.GetBasicPropertiesAsync();
            var understandableSize = FileSizeConverter.SizeSuffix((long)properties.Size, 2);

            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;

            return $"Name: {name}\nImageSize: {width}x{height}\nFileSize: {understandableSize}";
        }

        private async void OnCreateImagesButtonClicked(object sender, RoutedEventArgs e)
        {
            _folder = await CreateFolderForImagesAsync();
            if (_folder == null)
            {
                // Cancelled.
                return;
            }

            ProgressView.Visibility = Visibility.Visible;
            await StartCreatingImages();

            SetInitialResult();
            _folder = null;

            ProgressView.Visibility = Visibility.Collapsed;
        }

        private void SetInitialResult()
        {
            if (MediumQualityCheckbox.IsChecked == true)
            {
                OnMediumQualitySelected(this, null);
            }
            else
            {
                MediumQualityCheckbox.IsChecked = true;
            }
        }

        private async void OnPickButtonClicked(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            _file = await picker.PickSingleFileAsync();
            if (_file != null)
            {
                // Application now has read/write access to the picked file
                var img = new BitmapImage();
                img = await LoadImage(_file);
                MainImage.Source = img;

                FileInfoTextBlock.Text = await GetImageFileInfoAsync(_file, img);
            }
            else
            {
                // cancelled
            }
        }

        private static async Task<BitmapImage> LoadImage(StorageFile file)
        {
            var bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);

            bitmapImage.SetSource(stream);

            return bitmapImage;
        }

        private string GetFileName()
        {
            var fileName = string.IsNullOrWhiteSpace(FolderNameTextBox.Text) ? Guid.NewGuid().ToString() : FolderNameTextBox.Text;
            return $"{fileName}.png";
        }

        private async Task StartCreatingImages()
        {
            _fileName = GetFileName();

            var mainFile = await _folder.CreateFileAsync(_fileName, CreationCollisionOption.ReplaceExisting);

            await SaveStorageFileAsync(mainFile, _file);

            if (AndroidCheckBox.IsChecked == true)
            {
                await CreateAndroidFilesAsync(_fileName, _folder);
            }
        }

        private async Task CreateAndroidFilesAsync(string fileName, StorageFolder folder)
        {
            var androidFolder = await folder.CreateFolderAsync("Android", CreationCollisionOption.ReplaceExisting);

            _drawableResults.LDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.LDPI);
            _drawableResults.MDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.MDPI);
            _drawableResults.HDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.HDPI);
            _drawableResults.XHDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.XHDPI);
            _drawableResults.XXHDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.XXHDPI);
            _drawableResults.XXXHDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.XXXHDPI);
        }

        private async Task<StorageFile> CreateAndroidFileAsync(StorageFolder androidFolder, string fileName, AndroidDrawableTypes imageType)
        {
            var imageTypeName = AndroidDrawableTypes.GetName(typeof(AndroidDrawableTypes), imageType);
            var androidDrawableFolder = await androidFolder.CreateFolderAsync(imageTypeName, CreationCollisionOption.ReplaceExisting);
            var androidImageFile = await androidDrawableFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            await ResizeImageAsync(_file, androidImageFile, imageType);
            await SaveStorageFileAsync(androidImageFile);

            return androidImageFile;
        }

        private async Task SaveStorageFileAsync(StorageFile destinationFile, StorageFile file)
        {
            byte[] buffer;
            Stream stream = await file.OpenStreamForReadAsync();
            buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, (int)stream.Length);

            await FileIO.WriteBytesAsync(destinationFile, buffer);
        }

        private async Task SaveStorageFileAsync(StorageFile file)
        {
            byte[] buffer;
            Stream stream = await file.OpenStreamForReadAsync();
            buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, (int)stream.Length);

            await FileIO.WriteBytesAsync(file, buffer);
        }

        private async Task<Size> GetAndroidImageSizeAsycn(AndroidDrawableTypes imageType)
        {
            var imageProperties = await _file.Properties.GetImagePropertiesAsync();
            var imageSize = new Size(imageProperties.Width, imageProperties.Height);

            var size = Size.Empty;
            var factor = 1.0f;
            switch (imageType)
            {
                case AndroidDrawableTypes.LDPI:
                    factor = 0.75f / 4.0f;
                    break;

                case AndroidDrawableTypes.MDPI:
                    factor = 1.0f / 4.0f;
                    break;

                case AndroidDrawableTypes.HDPI:
                    factor = 1.5f / 4.0f;
                    break;

                case AndroidDrawableTypes.XHDPI:
                    factor = 2.0f / 4.0f;
                    break;

                case AndroidDrawableTypes.XXHDPI:
                    factor = 3.0f / 4.0f;
                    break;

                case AndroidDrawableTypes.XXXHDPI:
                    factor = 4.0f / 4.0f;
                    break;
            }

            size.Height = imageSize.Height * factor;
            size.Width = imageSize.Width * factor;

            return size;
        }

        private async Task<StorageFile> ResizeImageAsync(StorageFile sourceFile, StorageFile destinationFile, AndroidDrawableTypes imageType)
        {
            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(sourceStream);

                var scaledSize = await GetAndroidImageSizeAsycn(imageType);
                BitmapTransform transform = new BitmapTransform() { ScaledHeight = (uint)scaledSize.Height, ScaledWidth = (uint)scaledSize.Width };
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                using (var destinationStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var bitmapEncoderId = GetBitmapEncoderIdBasedOnExtension(sourceFile.Name);
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(bitmapEncoderId, destinationStream);

                    var detachedPixelData = pixelData.DetachPixelData();
                    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, (uint)scaledSize.Width, (uint)scaledSize.Height, 96, 96, detachedPixelData);
                    await encoder.FlushAsync();
                }

                return destinationFile;
            }
        }

        private Guid GetBitmapEncoderIdBasedOnExtension(string filename)
        {
            var bitmapEncoder = BitmapEncoder.PngEncoderId;

            var extension = Path.GetExtension(filename);
            switch(extension)
            {
                case ".png":
                    bitmapEncoder = BitmapEncoder.PngEncoderId;
                    break;

                case ".jpg":
                    bitmapEncoder = BitmapEncoder.JpegEncoderId;
                    break;
            }

            return bitmapEncoder;
        }

        private async Task SaveFileAsync(StorageFolder folder, string filename)
        {
            await folder.CreateFileAsync(filename);
        }

        private async Task<StorageFolder> CreateFolderForImagesAsync()
        {
            var picFolder = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            var yourFolder = await picFolder.RequestAddFolderAsync();

            return yourFolder;
        }
    }
}
