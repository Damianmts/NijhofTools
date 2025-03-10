﻿using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using System.Security.Cryptography;
using System.Text;

namespace NijhofAddIn.ViewModels;

//TODO Models maken voor 'Laden', 'Plaatsen' en 'Sluiten'.

/// <summary>
/// Revit custom content library
/// 
/// Beheert de bestandsstructuur en weergave in een WPF-applicatie.
/// Bevat twee hoofdklassen:
/// - FileItem: Voor individuele bestands- en maprepresentatie met thumbnail ondersteuning
/// - LibraryViewModel: Voor het beheren van de algemene bestandsstructuur en selecties
/// 
/// FileItem functionaliteiten:
/// - Bestandsinformatie beheer (naam, pad, weergavenaam)
/// - Thumbnail generatie en caching
/// - Asynchrone bestandsverwerking
/// - Subbestand collectie beheer
/// 
/// LibraryViewModel functionaliteiten:
/// - Beheer van root bestanden en mappen
/// - Geselecteerde map tracking
/// - Dynamische mapinhoud weergave
/// - Recursieve mapstructuur laden
/// 
/// Implementeert MVVM-patroon met Observable Collections voor real-time UI updates.
/// </summary>

public class FileItem : INotifyPropertyChanged
    {
        private static readonly SemaphoreSlim _thumbnailSemaphore = new SemaphoreSlim(4);
        private static readonly ConcurrentDictionary<string, BitmapImage> _thumbnailCache = new ConcurrentDictionary<string, BitmapImage>();
        
        private static readonly string cacheFolder = @"F:\Revit\Nijhof Tools\cache\";

        static FileItem()
        {
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
        }

        public FileItem(string path)
        {
            SubFiles = new ObservableCollection<FileItem>();
            FullPath = path;
            ItemName = Path.GetFileName(path);
            DisplayName = Path.GetFileNameWithoutExtension(path);
            
            LoadThumbnailAsync();
        }
        
        private string GetCacheFilePath()
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(FullPath));
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return Path.Combine(cacheFolder, hash + ".png");
            }
        }
        
        private DateTime GetFileLastWriteTime()
        {
            return File.GetLastWriteTimeUtc(FullPath);
        }
        
        private bool IsCacheValid(string cacheFilePath)
        {
            if (!File.Exists(cacheFilePath))
                return false;

            DateTime cachedTime = File.GetLastWriteTimeUtc(cacheFilePath);
            DateTime currentFileTime = GetFileLastWriteTime();
            
            return currentFileTime <= cachedTime;
        }

        public async void LoadThumbnailAsync()
        {
            try
            {
                if (_thumbnailCache.TryGetValue(FullPath, out BitmapImage cachedImage))
                {
                    Thumbnail = cachedImage;
                    return;
                }
                
                string cacheFilePath = GetCacheFilePath();
                if (File.Exists(cacheFilePath) && IsCacheValid(cacheFilePath))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var image = new BitmapImage(new Uri(cacheFilePath, UriKind.Absolute));
                        Thumbnail = image;
                        _thumbnailCache.TryAdd(FullPath, image);
                    });
                    return;
                }

                string extension = Path.GetExtension(FullPath).ToLower();
                if (extension != ".rfa") return;

                await Task.Run(async () =>
                {
                    try
                    {
                        await _thumbnailSemaphore.WaitAsync();

                        var shellFile = ShellFile.FromFilePath(FullPath);
                        using (var shellThumb = shellFile?.Thumbnail?.Bitmap)
                        {
                            if (shellThumb != null)
                            {
                                using (var resized = ResizeBitmap(shellThumb, 256, 256))
                                {
                                    using (var memory = new MemoryStream())
                                    {
                                        resized.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                                        File.WriteAllBytes(cacheFilePath, memory.ToArray());
                                    }
                                    
                                    File.SetLastWriteTimeUtc(cacheFilePath, GetFileLastWriteTime());

                                    var bitmapImage = ConvertToBitmapImage(resized);
                                    _thumbnailCache.TryAdd(FullPath, bitmapImage);

                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        Thumbnail = bitmapImage;
                                    });
                                }
                            }
                        }
                    }
                    finally
                    {
                        _thumbnailSemaphore.Release();
                    }
                });
            }
            catch
            {
                Thumbnail = null;
            }
        }

        private System.Drawing.Bitmap ResizeBitmap(System.Drawing.Bitmap bitmap, int width, int height)
        {
            var result = new System.Drawing.Bitmap(width, height);
            using (var graphics = System.Drawing.Graphics.FromImage(result))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(bitmap, 0, 0, width, height);
            }

            return result;
        }

        private BitmapImage ConvertToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        public string ItemName { get; set; }
        public string DisplayName { get; set; }
        public string FullPath { get; set; }
        public ObservableCollection<FileItem> SubFiles { get; set; }

        private BitmapImage _thumbnail;
        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

public class LibraryViewModel : ObservableObject
{
    private ObservableCollection<FileItem> _rootFiles;
    private FileItem _selectedFolder;
    private ObservableCollection<FileItem> _selectedFolderContent;

    public ObservableCollection<FileItem> RootFiles
    {
        get => _rootFiles;
        set => SetProperty(ref _rootFiles, value);
    }

    public FileItem SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                LoadSelectedFolderContent();
            }
        }
    }

    public ObservableCollection<FileItem> SelectedFolderContent
    {
        get => _selectedFolderContent;
        set => SetProperty(ref _selectedFolderContent, value);
    }

    public LibraryViewModel()
    {
        RootFiles = new ObservableCollection<FileItem>();
        SelectedFolderContent = new ObservableCollection<FileItem>();
        LoadFolderStructure();
    }

    private void LoadSelectedFolderContent()
    {
        SelectedFolderContent.Clear();

        if (SelectedFolder == null) return;

        try
        {
            var files = Directory.GetFiles(SelectedFolder.FullPath)
                .Where(f => Path.GetExtension(f).ToLower() is ".rfa");
            foreach (var file in files)
            {
                SelectedFolderContent.Add(new FileItem(file));
            }
            
            LoadFilesFromSubfolders(SelectedFolder.FullPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij het laden van bestanden: {ex.Message}");
        }
    }

    private void LoadFilesFromSubfolders(string folderPath)
    {
        try
        {
            foreach (var subDir in Directory.GetDirectories(folderPath))
            {
                var files = Directory.GetFiles(subDir);
                foreach (var file in files)
                {
                    SelectedFolderContent.Add(new FileItem(file));
                }
                
                LoadFilesFromSubfolders(subDir);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij het laden van submappen: {ex.Message}");
        }
    }


    private void LoadFolderStructure()
    {
        string rootPath = @"F:\Stabiplan\Custom\Families";
        if (Directory.Exists(rootPath))
        {
            var directories = Directory.GetDirectories(rootPath);
            foreach (var directory in directories)
            {
                var subItem = new FileItem(directory);
                LoadSubFolders(subItem);
                RootFiles.Add(subItem);
            }
        }
    }

    private void LoadSubFolders(FileItem parentItem)
    {
        try
        {
            var directories = Directory.GetDirectories(parentItem.FullPath);
            foreach (var directory in directories)
            {
                var subItem = new FileItem(directory);
                LoadSubFolders(subItem);
                parentItem.SubFiles.Add(subItem);
            }
        }
        catch (Exception ex)
        {
            // Hier kun je eventueel foutafhandeling toevoegen
            MessageBox.Show($"Fout bij het laden van de map {parentItem.FullPath}: {ex.Message}");
        }
    }
}