﻿using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using XamarinImageFactory.Common;
using XamarinImageFactory.Factories;
using XamarinImageFactory.Models;
using XamarinImageFactory.Tools;
using XamarinImageFactory.Utitlities;

namespace XamarinImageFactory
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IconsPage : Page
    {
        private readonly ImageFileManipulator _imageFileManipulator;

        private StorageFolder _lastUsedFolder;
        private StorageFolder _folder;
        private string _name;
        private StorageFile _file;

        private PlatformType _resultPlatformType;

        private AndroidImageFactory _androidImageFactory;
        private AndroidDrawablesResult _androidImageResults;

        private IOSImageFactory _iosImageFactory;
        private IOSAssetsResult _iosImageResults;

        private WindowsImageFactory _windowsImageFactory;
        private WindowsAssetsResult _windowsImageResults;

        private ImageResult _lowImageResult;
        private ImageResult _mediumImageResult;
        private ImageResult _highImageResult;

        private int ResultIndexSelected;

        public IconsPage()
        {
            this.InitializeComponent();

            _imageFileManipulator = new ImageFileManipulator();

            PrepareUIElements();
            Initialize();
        }

        private void Initialize()
        {
            _androidImageFactory = new AndroidImageFactory();
            _androidImageResults = _androidImageFactory.CreateAndroidDrawables();

            _iosImageFactory = new IOSImageFactory();
            _iosImageResults = _iosImageFactory.CreateIOSResult();

            _windowsImageFactory = new WindowsImageFactory();
            _windowsImageResults = _windowsImageFactory.CreateWindowsAssetsResult();

            _lowImageResult = new ImageResult();
            _mediumImageResult = new ImageResult();
            _highImageResult = new ImageResult();
        }

        private void PrepareUIElements()
        {
            ResultComboBox.SelectionChanged += OnResultComboBoxChanged;

            PickButton.Click += OnPickButtonClicked;
            CreateImagesButton.Click += OnCreateImagesButtonClicked;
            ShowFolderButton.Click += OnShowFolderClicked;

            LowQualityCheckbox.Checked += OnLowQualitySelected;
            MediumQualityCheckbox.Checked += OnMediumQualitySelected;
            HighQualityCheckbox.Checked += OnHighQualitySelected;
        }

        private async void OnShowFolderClicked(object sender, RoutedEventArgs e)
        {
            if (_lastUsedFolder != null)
            {
                await Launcher.LaunchFolderAsync(_lastUsedFolder);
            }
            else
            {
                var messageDialog = new MessageDialog("You have no folder to show", "No folder");
                var result = await messageDialog.ShowAsync();
            }
        }

        private async void OnResultComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            _resultPlatformType = (PlatformType)ResultComboBox.SelectedIndex;
            ChangeResultImageFiles();
            await SetResultImageBasedOnIndexAsync();
        }

        private void ChangeResultImageFiles()
        {
            switch (_resultPlatformType)
            {
                case PlatformType.ANDROID:
                    _lowImageResult = _androidImageResults.MDPI;
                    _mediumImageResult = _androidImageResults.XHDPI;
                    _highImageResult = _androidImageResults.XXXHDPI;
                    break;

                case PlatformType.IOS:
                    _lowImageResult = _iosImageResults.Image100;
                    _mediumImageResult = _iosImageResults.Image200;
                    _highImageResult = _iosImageResults.Image300;
                    break;

                case PlatformType.WINDOWS:
                    _lowImageResult = _windowsImageResults.Image100;
                    _mediumImageResult = _windowsImageResults.Image140;
                    _highImageResult = _windowsImageResults.Image240;
                    break;
            }
        }

        private async Task SetResultImageBasedOnIndexAsync()
        {
            switch(ResultIndexSelected)
            {
                case 0:
                    await SetResultImageAsync(_lowImageResult.File);
                    break;

                case 1:
                    await SetResultImageAsync(_mediumImageResult.File);
                    break;

                case 2:
                    await SetResultImageAsync(_highImageResult.File);
                    break;
            }
        }

        private async void OnLowQualitySelected(object sender, RoutedEventArgs e)
        {
            ResultIndexSelected = 0;
            await SetResultImageBasedOnIndexAsync();
        }

        private async void OnMediumQualitySelected(object sender, RoutedEventArgs e)
        {
            ResultIndexSelected = 1;
            await SetResultImageBasedOnIndexAsync();
        }

        private async void OnHighQualitySelected(object sender, RoutedEventArgs e)
        {
            ResultIndexSelected = 2;
            await SetResultImageBasedOnIndexAsync();
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
            else
            {
                ResultImage.Source = null;
                ResultFileInfoTextBlock.Text = "Nothing to show";
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
            if (_file == null)
            {
                var messageDialog = new MessageDialog("You have no selected file. Please select one first.", "No selected file");
                await messageDialog.ShowAsync();
                return;
            }

            if (iOSCheckBox.IsChecked == false && AndroidCheckBox.IsChecked == false && WindowsCheckBox.IsChecked == false)
            {
                var messageDialog = new MessageDialog("No image types are selected. What should we make?", "No image types");
                await messageDialog.ShowAsync();
                return;
            }

            _name = GetName();
            _folder = await CreateFolderForImagesAsync();
            if (_folder == null)
            {
                // Cancelled.
                return;
            }

            ProgressView.Visibility = Visibility.Visible;

            await StartCreatingImages();

            SetInitialResult();
            _lastUsedFolder = _folder;
            ResultTextBlock.Text = $"Images have been created in {_lastUsedFolder.Path}";

            ProgressView.Visibility = Visibility.Collapsed;
        }

        private void SetInitialResult()
        {
            SetComboBoxIfNotSet();
            if (MediumQualityCheckbox.IsChecked == true)
            {
                OnMediumQualitySelected(this, null);
            }
            else
            {
                MediumQualityCheckbox.IsChecked = true;
            }
        }

        private void SetComboBoxIfNotSet()
        {
            if (ResultComboBox.SelectedIndex < 0)
            {
                if (AndroidCheckBox.IsChecked == true)
                {
                    ResultComboBox.SelectedIndex = 0;
                }
                else if (iOSCheckBox.IsChecked == true)
                {
                    ResultComboBox.SelectedIndex = 1;
                }
                else if (WindowsCheckBox.IsChecked == true)
                {
                    ResultComboBox.SelectedIndex = 2;
                }
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

        private string GetName()
        {
            var fileName = string.IsNullOrWhiteSpace(FolderNameTextBox.Text) ? Guid.NewGuid().ToString() : FolderNameTextBox.Text;
            return fileName;
        }

        private async Task StartCreatingImages()
        {
            var mainFileName = $"{_name}.png";
            var mainFile = await _folder.CreateFileAsync(mainFileName, CreationCollisionOption.ReplaceExisting);

            await SaveStorageFileAsync(mainFile, _file);

            if (AndroidCheckBox.IsChecked == true)
            {
                await CreateAndroidFilesAsync(mainFileName, _folder);
            }

            if (iOSCheckBox.IsChecked == true)
            {
                await CreateIOSFilesAsync(_folder);
            }

            if (WindowsCheckBox.IsChecked == true)
            {
                await CreateWindowsFilesAsync(_folder);
            }
        }

        private async Task CreateWindowsFilesAsync(StorageFolder folder)
        {
            var windowsFolder = await folder.CreateFolderAsync("Windows", CreationCollisionOption.GenerateUniqueName);

            _windowsImageResults.Image100.File = await CreateWindowsFileAsync(windowsFolder, WindowsAssetTypes.Image100);
            _windowsImageResults.Image140.File = await CreateWindowsFileAsync(windowsFolder, WindowsAssetTypes.Image140);
            _windowsImageResults.Image180.File = await CreateWindowsFileAsync(windowsFolder, WindowsAssetTypes.Image180);
            _windowsImageResults.Image240.File = await CreateWindowsFileAsync(windowsFolder, WindowsAssetTypes.Image240);
        }

        private async Task CreateIOSFilesAsync(StorageFolder folder)
        {
            var iosFolder = await folder.CreateFolderAsync("IOS", CreationCollisionOption.GenerateUniqueName);

            _iosImageResults.Image100.File = await CreateIOSFileAsync(iosFolder, IOSAssetTypes.Image100);
            _iosImageResults.Image200.File = await CreateIOSFileAsync(iosFolder, IOSAssetTypes.Image200);
            _iosImageResults.Image300.File = await CreateIOSFileAsync(iosFolder, IOSAssetTypes.Image300);
        }

        private async Task CreateAndroidFilesAsync(string fileName, StorageFolder folder)
        {
            var androidFolder = await folder.CreateFolderAsync("Android", CreationCollisionOption.GenerateUniqueName);

            _androidImageResults.LDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.LDPI);
            _androidImageResults.MDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.MDPI);
            _androidImageResults.HDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.HDPI);
            _androidImageResults.XHDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.XHDPI);
            _androidImageResults.XXHDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.XXHDPI);
            _androidImageResults.XXXHDPI.File = await CreateAndroidFileAsync(androidFolder, fileName, AndroidDrawableTypes.XXXHDPI);
        }

        private async Task<StorageFile> CreateWindowsFileAsync(StorageFolder windowsFolder, WindowsAssetTypes imageType)
        {
            var fileName = GetWindowsFileName(imageType);
            var windowsFile = await windowsFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            var scaledSize = await GetWindowsImageSizeAsync(imageType);
            await _imageFileManipulator.ResizeImageAsync(_file, windowsFile, scaledSize);
            await SaveStorageFileAsync(windowsFile);

            return windowsFile;
        }

        private async Task<StorageFile> CreateIOSFileAsync(StorageFolder iosFolder, IOSAssetTypes imageType)
        {
            var fileName = GetIOSFileName(imageType);
            var iosImageFile = await iosFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            var scaledSize = await GetIOSImageSizeAsync(imageType);
            await _imageFileManipulator.ResizeImageAsync(_file, iosImageFile, scaledSize);
            await SaveStorageFileAsync(iosImageFile);

            return iosImageFile;
        }

        private async Task<StorageFile> CreateAndroidFileAsync(StorageFolder androidFolder, string fileName, AndroidDrawableTypes imageType)
        {
            var imageTypeName = AndroidDrawableTypes.GetName(typeof(AndroidDrawableTypes), imageType);
            var lowerLettersImageTypeName = imageTypeName.ToLower();
            var androidDrawableFolder = await androidFolder.CreateFolderAsync(lowerLettersImageTypeName, CreationCollisionOption.ReplaceExisting);
            var androidImageFile = await androidDrawableFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            var scaledSize = await GetAndroidImageSizeAsycn(imageType);
            await _imageFileManipulator.ResizeImageAsync(_file, androidImageFile, scaledSize);
            await SaveStorageFileAsync(androidImageFile);

            return androidImageFile;
        }

        private string GetWindowsFileName(WindowsAssetTypes windowsImageType)
        {
            var fileName = _name;
            switch (windowsImageType)
            {
                case WindowsAssetTypes.Image100:
                    fileName = $"{fileName}.scale-100.png";
                    break;

                case WindowsAssetTypes.Image140:
                    fileName = $"{fileName}.scale-140.png";
                    break;

                case WindowsAssetTypes.Image180:
                    fileName = $"{fileName}.scale-180.png";
                    break;

                case WindowsAssetTypes.Image240:
                    fileName = $"{fileName}.scale-240.png";
                    break;
            }

            return fileName;
        }

        private string GetIOSFileName(IOSAssetTypes iosType)
        {
            var fileName = _name;
            switch (iosType)
            {
                case IOSAssetTypes.Image100:
                    fileName = $"{fileName}.png";
                    break;

                case IOSAssetTypes.Image200:
                    fileName = $"{fileName}@2.png";
                    break;

                case IOSAssetTypes.Image300:
                    fileName = $"{fileName}@3.png";
                    break;
            }

            return fileName;
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

        private async Task<Size> GetWindowsImageSizeAsync(WindowsAssetTypes imageType)
        {
            var imageProperties = await _file.Properties.GetImagePropertiesAsync();
            var imageSize = new Size(imageProperties.Width, imageProperties.Height);

            var size = Size.Empty;
            var factor = 1.0f;
            switch (imageType)
            {
                case WindowsAssetTypes.Image100:
                    factor = 1.0f / 4.0f;
                    break;

                case WindowsAssetTypes.Image140:
                    factor = 1.4f / 4.0f;
                    break;

                case WindowsAssetTypes.Image180:
                    factor = 1.8f / 4.0f;
                    break;

                case WindowsAssetTypes.Image240:
                    factor = 2.4f / 4.0f;
                    break;
            }

            size.Height = imageSize.Height * factor;
            size.Width = imageSize.Width * factor;

            return size;
        }

        private async Task<Size> GetIOSImageSizeAsync(IOSAssetTypes imageType)
        {
            var imageProperties = await _file.Properties.GetImagePropertiesAsync();
            var imageSize = new Size(imageProperties.Width, imageProperties.Height);

            var size = Size.Empty;
            var factor = 1.0f;
            switch (imageType)
            {
                case IOSAssetTypes.Image100:
                    factor = 1.0f / 4.0f;
                    break;

                case IOSAssetTypes.Image200:
                    factor = 2.0f / 4.0f;
                    break;

                case IOSAssetTypes.Image300:
                    factor = 3.0f / 4.0f;
                    break;
            }

            size.Height = imageSize.Height * factor;
            size.Width = imageSize.Width * factor;

            return size;
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

        private async Task SaveFileAsync(StorageFolder folder, string filename)
        {
            await folder.CreateFileAsync(filename);
        }

        private async Task<StorageFolder> CreateFolderForImagesAsync()
        {
            var picFolder = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            var selectedFolder = await picFolder.RequestAddFolderAsync();
            if (selectedFolder == null)
            {
                return null;
            }

            var imageFolder = await selectedFolder.CreateFolderAsync(_name, CreationCollisionOption.GenerateUniqueName);

            return imageFolder;
        }
    }
}
