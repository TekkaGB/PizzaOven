using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Net.Http;
using System.Windows.Media;
using PizzaOven.UI;
using System.Windows.Controls.Primitives;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Data;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Threading;

namespace PizzaOven
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string version;
        // Separated from Global.config so that order is updated when datagrid is modified
        public List<string> exes;
        private FileSystemWatcher ModsWatcher;
        private FlowDocument defaultFlow = new FlowDocument();
        private string defaultText = "No mod is currently selected. Pressing launch will start a vanilla Pizza Tower. " +
            "Start downloading and using mods in the Browse Mods tab on top. Only one mod can be selected at a time.";
        public MainWindow()
        {
            InitializeComponent();
            Global.logger = new Logger(ConsoleWindow);
            Global.config = new();

            // Get Version Number
            var PizzaOvenVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = PizzaOvenVersion.Substring(0, PizzaOvenVersion.LastIndexOf('.'));

            Global.logger.WriteLine($"Launched PizzaOven Mod Manager v{version}!", LoggerType.Info);
            // Get Global.config if it exists
            if (File.Exists($@"{Global.assemblyLocation}{Global.s}Config.json"))
            {
                try
                {
                    var configString = File.ReadAllText($@"{Global.assemblyLocation}{Global.s}Config.json");
                    Global.config = JsonSerializer.Deserialize<Config>(configString);
                }
                catch (Exception e)
                {
                    Global.logger.WriteLine(e.Message, LoggerType.Error);
                }
            }

            // Last saved windows settings
            if (Global.config.Height != null && Global.config.Height >= MinHeight)
                Height = (double)Global.config.Height;
            if (Global.config.Width != null && Global.config.Width >= MinWidth)
                Width = (double)Global.config.Width;
            if (Global.config.Maximized)
                WindowState = WindowState.Maximized;
            if (Global.config.TopGridHeight != null)
                MainGrid.RowDefinitions[1].Height = new GridLength((double)Global.config.TopGridHeight, GridUnitType.Star);
            if (Global.config.BottomGridHeight != null)
                MainGrid.RowDefinitions[3].Height = new GridLength((double)Global.config.BottomGridHeight, GridUnitType.Star);
            if (Global.config.LeftGridWidth != null)
                MiddleGrid.ColumnDefinitions[0].Width = new GridLength((double)Global.config.LeftGridWidth, GridUnitType.Star);
            if (Global.config.RightGridWidth != null)
                MiddleGrid.ColumnDefinitions[2].Width = new GridLength((double)Global.config.RightGridWidth, GridUnitType.Star);

            if (Global.config.ModList == null)
                Global.config.ModList = new();
            Global.ModList = Global.config.ModList;


            Directory.CreateDirectory($@"{Global.assemblyLocation}{Global.s}Mods");

            // Watch mods folder to detect
            ModsWatcher = new FileSystemWatcher($@"{Global.assemblyLocation}{Global.s}Mods");
            ModsWatcher.Created += OnModified;
            ModsWatcher.Deleted += OnModified;
            ModsWatcher.Renamed += OnModified;

            Refresh();
            SelectItem();

            ModsWatcher.EnableRaisingEvents = true;

            defaultFlow.Blocks.Add(ConvertToFlowParagraph(defaultText));
            DescriptionWindow.Document = defaultFlow;
            var bitmap = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenPreview.png"));
            Preview.Source = bitmap;
            PreviewBG.Source = null;

            Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
            ModGrid.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            ModGridSearchButton.IsEnabled = false;
            App.Current.Dispatcher.Invoke(() =>
            {
                ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods", this);
            });
            if (Global.config.ModsFolder == null)
            {
                // Setup on launch if not setup yet
                if (Setup.GameSetup())
                    LaunchButton.IsEnabled = true;
                else
                {
                    LaunchButton.IsEnabled = false;
                    Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                }
            }
        }
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

        }
        private void OnModified(object sender, FileSystemEventArgs e)
        {
            Refresh();
            Global.UpdateConfig();
            // Bring window to front after download is done
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Activate();
            });
        }
        private async void SelectItem()
        {
           
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    var index = Global.ModList.ToList().FindIndex(mod => mod.enabled == true);
                    if (index != -1)
                    {
                        ModGrid.SelectedItem = ModGrid.Items[index];
                        ModGrid.ScrollIntoView(ModGrid.Items[index]);
                    }
                    else
                    {
                        ModGrid.SelectedIndex = -1;
                        ShowMetadata(null);
                    }
                });
            });
        }

        private async void Refresh()
        {
            var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods";
            // Add new folders found in Mods to the ModList
            foreach (var mod in Directory.GetDirectories(currentModDirectory))
            {
                if (Global.ModList.ToList().Where(x => x.name == Path.GetFileName(mod)).Count() == 0)
                {
                    Mod m = new Mod();
                    m.name = Path.GetFileName(mod);
                    m.enabled = false;
                    Thread.Sleep(1000);
                    if (File.Exists($"{mod}{Global.s}mod.json"))
                    {
                        FlowDocument descFlow = new FlowDocument();
                        var metadataString = File.ReadAllText($"{mod}{Global.s}mod.json");
                        Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);
                        m.preview = metadata.preview;
                    }
                    else
                        m.preview = new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenLogo.png");
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Add(m);
                    });
                    Global.logger.WriteLine($"Added {Path.GetFileName(mod)}", LoggerType.Info);
                }
            }
            // Remove deleted folders that are still in the ModList
            foreach (var mod in Global.ModList.ToList())
            {
                if (!Directory.GetDirectories(currentModDirectory).ToList().Select(x => Path.GetFileName(x)).Contains(mod.name))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Remove(mod);
                    });
                    Global.logger.WriteLine($"Deleted {mod.name}", LoggerType.Info);
                    continue;
                }
            }

            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ModGrid.ItemsSource = Global.ModList;
                    if (ModGrid.Items.Count == 0)
                        DropBox.Visibility = Visibility.Visible;
                    else
                        DropBox.Visibility = Visibility.Collapsed;
                    Stats.Text = $"{Global.ModList.Count} mods • {Directory.GetFiles($@"{Global.assemblyLocation}{Global.s}Mods", "*", SearchOption.AllDirectories).Length.ToString("N0")} files • " +
                    $"{StringConverters.FormatSize(new DirectoryInfo($@"{Global.assemblyLocation}{Global.s}Mods").GetDirectorySize())} • v{version}";
                });
            });
            Global.config.ModList = Global.ModList;
            Global.logger.WriteLine("Refreshed!", LoggerType.Info);
        }

        private async void Setup_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (Setup.GameSetup())
                {
                    Dispatcher.Invoke(() =>
                    {
                        LaunchButton.IsEnabled = true;
                    });
                }
            });
        }
        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            // Build Mod Loadout
            if (Global.config.ModsFolder != null)
            {
                ModGrid.IsEnabled = false;
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                ModGridSearchButton.IsEnabled = false;
                Refresh();
                Directory.CreateDirectory(Global.config.ModsFolder);
                Global.logger.WriteLine($"Cooking mods for Pizza Tower", LoggerType.Info);
                if (!await Build(Global.config.ModsFolder))
                {
                    Global.logger.WriteLine($"Pizza Oven failed to cook the selected mod and will not launch the game", LoggerType.Error);
                    ModGrid.IsEnabled = true;
                    ConfigButton.IsEnabled = true;
                    LaunchButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                    UpdateButton.IsEnabled = true;
                    ModGridSearchButton.IsEnabled = true;
                    return;
                }
                ModGrid.IsEnabled = true;
                ConfigButton.IsEnabled = true;
                LaunchButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                UpdateButton.IsEnabled = true;
                ModGridSearchButton.IsEnabled = true;
            }
            else
            {
                Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                return;
            }
            // Launch game
            if (Global.config.Launcher != null && File.Exists(Global.config.Launcher))
            {
                var path = Global.config.Launcher;
                try
                {
                    Global.UpdateConfig();
                    Global.logger.WriteLine($"Launching {path}", LoggerType.Info);
                    var ps = new ProcessStartInfo(path)
                    {
                        WorkingDirectory = Path.GetDirectoryName(Global.config.Launcher),
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps);
                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($"Couldn't launch {path} ({ex.Message})", LoggerType.Error);
                }
            }
            else
                Global.logger.WriteLine($"Please click Setup before starting!", LoggerType.Warning);
        }
        private void GameBanana_Click(object sender, RoutedEventArgs e)
        {
            var id = "7692";
            try
            {
                var ps = new ProcessStartInfo($"https://gamebanana.com/games/{id}")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't open up GameBanana ({ex.Message})", LoggerType.Error);
            }
        }
        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleWindow.ScrollToEnd();
        }

        private void ModGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            if (ModGrid.SelectedItem == null)
                element.ContextMenu.Visibility = Visibility.Collapsed;
            else
                element.ContextMenu.Visibility = Visibility.Visible;
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    var dialogResult = MessageBox.Show($@"Are you sure you want to delete {row.name}?" + Environment.NewLine + "This cannot be undone.", $@"Deleting {row.name}: Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await Task.Run(() => Directory.Delete($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{row.name}", true));
                            Global.logger.WriteLine($@"Deleting {row.name}.", LoggerType.Info);
                            ShowMetadata(null);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($@"Couldn't delete {row.name} ({ex.Message})", LoggerType.Error);
                        }
                    }
                }
        }

        private async Task<bool> Build(string path)
        {
            return await Task.Run(() =>
            {
                if (!ModLoader.Restart())
                    return false;
                var mods = Global.config.ModList.Where(x => x.enabled).ToList();
                if (mods.Count == 1)
                    return ModLoader.Build($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mods[0].name}");
                else if (mods.Count == 0)
                    return true;
                else
                    return false;
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Global.config.Height = RestoreBounds.Height;
                Global.config.Width = RestoreBounds.Width;
                Global.config.Maximized = true;
            }
            else
            {
                Global.config.Height = Height;
                Global.config.Width = Width;
                Global.config.Maximized = false;
            }
            Global.config.TopGridHeight = MainGrid.RowDefinitions[1].Height.Value;
            Global.config.BottomGridHeight = MainGrid.RowDefinitions[3].Height.Value;
            Global.config.LeftGridWidth = MiddleGrid.ColumnDefinitions[0].Width.Value;
            Global.config.RightGridWidth = MiddleGrid.ColumnDefinitions[2].Width.Value;
            Global.UpdateConfig();
            Application.Current.Shutdown();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    var folderName = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{row.name}";
                    if (Directory.Exists(folderName))
                    {
                        try
                        {
                            Process process = Process.Start("explorer.exe", folderName);
                            Global.logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                        }
                    }
                }
        }
        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);

            // Stop refreshing while renaming folders
            ModsWatcher.EnableRaisingEvents = false;
            foreach (var row in temp)
                if (row != null)
                {
                    EditWindow ew = new EditWindow(row.name, true);
                    ew.ShowDialog();
                }
            ModsWatcher.EnableRaisingEvents = true;
            Global.UpdateConfig();
            ModGrid.Items.Refresh();
        }
        private void FetchItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            ModsWatcher.EnableRaisingEvents = false;
            foreach (var row in temp)
                if (row != null)
                {
                    FetchWindow fw = new FetchWindow(row);
                    fw.ShowDialog();
                    if (fw.success)
                    {
                        ShowMetadata(row.name);
                        ModGrid.Items.Refresh();
                    }
                }
            ModsWatcher.EnableRaisingEvents = true;
        }
        private void Add_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Handled = true;
                e.Effects = DragDropEffects.Move;
                DropBox.Visibility = Visibility.Visible;
            }
        }
        private void Add_Leave(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DropBox.Visibility = Visibility.Collapsed;
        }
        private async void Add_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var ModsFolder = $"{Global.assemblyLocation}{Global.s}Mods";
            // Ensure that mods folder exists
            Directory.CreateDirectory(ModsFolder);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                await Task.Run(() => ExtractPackages(fileList));
            }
            DropBox.Visibility = Visibility.Collapsed;
        }
        private void ExtractPackages(string[] fileList)
        {
            var temp = $"{Global.assemblyLocation}{Global.s}temp";
            var ModsFolder = $"{Global.assemblyLocation}{Global.s}Mods";
            foreach (var file in fileList)
            {
                Directory.CreateDirectory(temp);
                // Move folder
                if (Directory.Exists(file))
                {
                    string path = $@"{temp}{Global.s}{Path.GetFileName(file)}";
                    int index = 2;
                    while (Directory.Exists(path))
                    {
                        path = $@"{temp}{Global.s}{Path.GetFileName(file)} ({index})";
                        index += 1;
                    }
                    MoveDirectory(file, path);
                }
                // Extract zip
                else if (Path.GetExtension(file).ToLower() == ".7z" || Path.GetExtension(file).ToLower() == ".rar" || Path.GetExtension(file).ToLower() == ".zip")
                {
                    string _ArchiveSource = file;
                    string _ArchiveType = Path.GetExtension(file);
                    if (File.Exists(_ArchiveSource))
                    {
                        try
                        {
                            if (Path.GetExtension(_ArchiveSource).Equals(".7z", StringComparison.InvariantCultureIgnoreCase))
                            {
                                using (var archive = SevenZipArchive.Open(_ArchiveSource))
                                {
                                    var reader = archive.ExtractAllEntries();
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                            reader.WriteEntryToDirectory($"{temp}{Global.s}{Path.GetFileNameWithoutExtension(file)}", new ExtractionOptions()
                                            {
                                                ExtractFullPath = true,
                                                Overwrite = true
                                            });
                                    }
                                }
                            }
                            else
                            {
                                using (Stream stream = File.OpenRead(_ArchiveSource))
                                using (var reader = ReaderFactory.Open(stream))
                                {
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                        {
                                            reader.WriteEntryToDirectory($"{temp}{Global.s}{Path.GetFileNameWithoutExtension(file)}", new ExtractionOptions()
                                            {
                                                ExtractFullPath = true,
                                                Overwrite = true
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"Couldn't extract {file}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        File.Delete(_ArchiveSource);
                    }
                }
                foreach (var folder in Directory.GetDirectories(temp, "*", SearchOption.TopDirectoryOnly))
                {
                    string path = $@"{ModsFolder}{Global.s}{Path.GetFileName(folder)}";
                    int index = 2;
                    while (Directory.Exists(path))
                    {
                        path = $@"{ModsFolder}{Global.s}{Path.GetFileName(folder)} ({index})";
                        index += 1;
                    }
                    MoveDirectory(folder, path);
                }
                if (Directory.Exists(temp))
                    Directory.Delete(temp, true);
            }
        }
        private static void MoveDirectory(string sourcePath, string targetPath)
        {
            //Copy all the files & Replaces any files with the same name
            foreach (var path in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var newPath = path.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Copy(path, newPath, true);
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var temp = Global.ModList.ToList();
            temp.ForEach(mod => mod.enabled = false);
            Global.ModList = new ObservableCollection<Mod>(temp);
            ShowMetadata(null);
            Global.UpdateConfig();
            ModGrid.SelectedIndex = -1;
        }
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
            ModGrid.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            ModGridSearchButton.IsEnabled = false;
            App.Current.Dispatcher.Invoke(() =>
            {
                ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods", this);
            });
        }
        private Paragraph ConvertToFlowParagraph(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);


            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                    };

                    hyperlink.RequestNavigate += (sender, args) =>
                    {
                        var ps = new ProcessStartInfo(segment)
                        {
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        Process.Start(ps);
                    };

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return paragraph;
        }

        private void ShowMetadata(string mod)
        {
            if (mod == null || !File.Exists($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mod}{Global.s}mod.json"))
            {
                DescriptionWindow.Document = defaultFlow;
                var bitmap = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenPreview.png"));
                Preview.Source = bitmap;
                PreviewBG.Source = null;
            }
            else
            {
                FlowDocument descFlow = new FlowDocument();
                var metadataString = File.ReadAllText($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mod}{Global.s}mod.json");
                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);

                var para = new Paragraph();
                if (metadata.submitter != null)
                {
                    para.Inlines.Add($"Submitter: ");
                    if (metadata.avi != null && metadata.avi.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.avi);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height = 35;
                        para.Inlines.Add(image);
                        para.Inlines.Add(" ");
                    }
                    if (metadata.upic != null && metadata.upic.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.upic);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height= 25;
                        para.Inlines.Add(image);
                    }
                    else
                        para.Inlines.Add(metadata.submitter);
                    descFlow.Blocks.Add(para);
                }
                if (metadata.preview != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = metadata.preview;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                    PreviewBG.Source = bitmap;
                }
                else
                {
                    var bitmap = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenPreview.png"));
                    Preview.Source = bitmap;
                    PreviewBG.Source = null;
                }
                    para = new Paragraph();
                    para.Inlines.Add("Category: ");
                if (metadata.caticon != null && metadata.caticon.ToString().Length > 0)
                {
                    BitmapImage bm = new BitmapImage(metadata.caticon);
                    Image image = new Image();
                    image.Source = bm;
                    image.Width = 20;
                    para.Inlines.Add(image);
                }
                para.Inlines.Add($" {metadata.cat}");
                descFlow.Blocks.Add(para);
                var text = "";
                if (!String.IsNullOrEmpty(metadata.description))
                    text += $"Description: {metadata.description}\n\n";
                if (!String.IsNullOrEmpty(metadata.filedescription))
                    text += $"File Description: {metadata.filedescription}\n\n";
                if (metadata.homepage != null && metadata.homepage.ToString().Length > 0)
                    text += $"Home Page: {metadata.homepage}";
                var init = ConvertToFlowParagraph(text);
                descFlow.Blocks.Add(init);
                DescriptionWindow.Document = descFlow;
                var descriptionText = new TextRange(DescriptionWindow.Document.ContentStart, DescriptionWindow.Document.ContentEnd);
                descriptionText.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Center);
            }
        }
        private void ModGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mod mod = (Mod)ModGrid.SelectedItem;
            if (mod != null)
                ShowMetadata(mod.name);
            var temp = Global.ModList.ToList();
            temp.ForEach(mod => mod.enabled = false);
            Global.ModList = new ObservableCollection<Mod>(temp);
            if (ModGrid.SelectedIndex == -1)
                ShowMetadata(null);
            else
                Global.ModList[ModGrid.SelectedIndex].enabled = true;
            Global.config.ModList = Global.ModList;
            Global.UpdateConfig();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            new ModDownloader().BrowserDownload("Pizza Tower", item);
        }
        private void AltDownload_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            new AltLinkWindow(item.AlternateFileSources, item.Title,
                "Pizza Tower",
                item.Link.AbsoluteUri).ShowDialog();
        }
        private void Homepage_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            try
            {
                var ps = new ProcessStartInfo(item.Link.ToString())
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't open up {item.Link} ({ex.Message})", LoggerType.Error);
            }
        }
        private int imageCounter;
        private int imageCount;
        private FlowDocument ConvertToFlowDocument(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);


            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                    };

                    hyperlink.RequestNavigate += (sender, args) => Process.Start(segment);

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return flowDocument;
        }
        private void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            HomepageButton.Content = $"{(TypeBox.SelectedValue as ComboBoxItem).Content.ToString().Trim().TrimEnd('s')} Page";
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (item.Compatible)
                DownloadButton.Visibility = Visibility.Visible;
            else
                DownloadButton.Visibility = Visibility.Collapsed;
            if (item.HasAltLinks)
                AltButton.Visibility = Visibility.Visible;
            else
                AltButton.Visibility = Visibility.Collapsed;
            DescPanel.DataContext = button.DataContext;
            MediaPanel.DataContext = button.DataContext;
            DescText.ScrollToHome();
            var text = "";
            text += item.ConvertedText;
            DescText.Document = ConvertToFlowDocument(text);
            ImageLeft.IsEnabled = true;
            ImageRight.IsEnabled = true;
            BigImageLeft.IsEnabled = true;
            BigImageRight.IsEnabled = true;
            imageCount = item.Media.Where(x => x.Type == "image").ToList().Count;
            imageCounter = 0;
            if (imageCount > 0)
            {
                Grid.SetColumnSpan(DescText, 1);
                ImagePanel.Visibility = Visibility.Visible;
                var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
                Screenshot.Source = image;
                BigScreenshot.Source = image;
                CaptionText.Text = item.Media[imageCounter].Caption;
                BigCaptionText.Text = item.Media[imageCounter].Caption;
                if (!String.IsNullOrEmpty(CaptionText.Text))
                {
                    BigCaptionText.Visibility = Visibility.Visible;
                    CaptionText.Visibility = Visibility.Visible;
                }
                else
                {
                    BigCaptionText.Visibility = Visibility.Collapsed;
                    CaptionText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Grid.SetColumnSpan(DescText, 2);
                ImagePanel.Visibility = Visibility.Collapsed;
            }
            if (imageCount == 1)
            {
                ImageLeft.IsEnabled = false;
                ImageRight.IsEnabled = false;
                BigImageLeft.IsEnabled = false;
                BigImageRight.IsEnabled = false;
            }

            DescPanel.Visibility = Visibility.Visible;
        }
        private void CloseDesc_Click(object sender, RoutedEventArgs e)
        {
            DescPanel.Visibility = Visibility.Collapsed;
        }
        private void CloseMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Collapsed;
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Visible;
        }

        private void ImageLeft_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (--imageCounter == -1)
                imageCounter = imageCount - 1;
            var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
            Screenshot.Source = image;
            CaptionText.Text = item.Media[imageCounter].Caption;
            BigScreenshot.Source = image;
            BigCaptionText.Text = item.Media[imageCounter].Caption;
            if (!String.IsNullOrEmpty(CaptionText.Text))
            {
                BigCaptionText.Visibility = Visibility.Visible;
                CaptionText.Visibility = Visibility.Visible;
            }
            else
            {
                BigCaptionText.Visibility = Visibility.Collapsed;
                CaptionText.Visibility = Visibility.Collapsed;
            }
        }

        private void ImageRight_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (++imageCounter == imageCount)
                imageCounter = 0;
            var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
            Screenshot.Source = image;
            CaptionText.Text = item.Media[imageCounter].Caption;
            BigScreenshot.Source = image;
            BigCaptionText.Text = item.Media[imageCounter].Caption;
            if (!String.IsNullOrEmpty(CaptionText.Text))
            {
                BigCaptionText.Visibility = Visibility.Visible;
                CaptionText.Visibility = Visibility.Visible;
            }
            else
            {
                BigCaptionText.Visibility = Visibility.Collapsed;
                CaptionText.Visibility = Visibility.Collapsed;
            }
        }
        private static bool selected = false;

        private static Dictionary<TypeFilter, List<GameBananaCategory>> cats = new();

        private static readonly List<GameBananaCategory> All = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "All",
                ID = null
            }
        }.ToList();
        private static readonly List<GameBananaCategory> None = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "- - -",
                ID = null
            }
        }.ToList();
        private async void InitializeBrowser()
        {
            using (var httpClient = new HttpClient())
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                // Initialize categories and games
                var gameID = "7692";
                var types = new string[] { "Mod", "Wip", "Sound" };
                double totalPages = 0;
                var counter = 0;
                foreach (var type in types)
                {
                    var requestUrl = $"https://gamebanana.com/apiv4/{type}Category/ByGame?_aGameRowIds[]={gameID}&_sRecordSchema=Custom" +
                        "&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50";
                    string responseString = "";
                    try
                    {
                        var responseMessage = await httpClient.GetAsync(requestUrl);
                        responseString = await responseMessage.Content.ReadAsStringAsync();
                        responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                        var numRecords = responseMessage.GetHeader("X-GbApi-Metadata_nRecordCount");
                        if (numRecords != -1)
                        {
                            totalPages = Math.Ceiling(numRecords / 50);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        switch (Regex.Match(ex.Message, @"\d+").Value)
                        {
                            case "443":
                                BrowserMessage.Text = "Your internet connection is down.";
                                break;
                            case "500":
                            case "503":
                            case "504":
                                BrowserMessage.Text = "GameBanana's servers are down.";
                                break;
                            default:
                                BrowserMessage.Text = ex.Message;
                                break;
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        BrowserMessage.Text = ex.Message;
                        return;
                    }
                    List<GameBananaCategory> response = new();
                    try
                    {
                        response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);
                    }
                    catch (Exception)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                        return;
                    }
                    cats.Add((TypeFilter)counter, response);

                    // Make more requests if needed
                    if (totalPages > 1)
                    {
                        for (double i = 2; i <= totalPages; i++)
                        {
                            var requestUrlPage = $"{requestUrl}&_nPage={i}";
                            try
                            {
                                responseString = await httpClient.GetStringAsync(requestUrlPage);
                                responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                            }
                            catch (HttpRequestException ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                switch (Regex.Match(ex.Message, @"\d+").Value)
                                {
                                    case "443":
                                        BrowserMessage.Text = "Your internet connection is down.";
                                        break;
                                    case "500":
                                    case "503":
                                    case "504":
                                        BrowserMessage.Text = "GameBanana's servers are down.";
                                        break;
                                    default:
                                        BrowserMessage.Text = ex.Message;
                                        break;
                                }
                                return;
                            }
                            catch (Exception ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                BrowserMessage.Text = ex.Message;
                                return;
                            }
                            try
                            {
                                response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);
                            }
                            catch (Exception ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                                return;
                            }
                            cats[(TypeFilter)counter] = cats[(TypeFilter)counter].Concat(response).ToList();
                        }
                    }
                    counter++;
                }
            }
            filterSelect = true;
            FilterBox.ItemsSource = FilterBoxList;
            CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
            SubCatBox.ItemsSource = None;
            CatBox.SelectedIndex = 0;
            SubCatBox.SelectedIndex = 0;
            FilterBox.SelectedIndex = 1;
            filterSelect = false;
            RefreshFilter();
            selected = true;
        }
        private void OnBrowserTabSelected(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
        }

        private static int page = 1;
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            --page;
            RefreshFilter();
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            ++page;
            RefreshFilter();
        }
        private void BrowserRefresh(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
            else
                RefreshFilter();
        }
        private static bool filterSelect;
        private static bool searched = false;
        private async void RefreshFilter()
        {
            NSFWCheckbox.IsEnabled = false;
            SearchBar.IsEnabled = false;
            SearchButton.IsEnabled = false;
            FilterBox.IsEnabled = false;
            TypeBox.IsEnabled = false;
            CatBox.IsEnabled = false;
            SubCatBox.IsEnabled = false;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            PageBox.IsEnabled = false;
            PerPageBox.IsEnabled = false;
            ClearCacheButton.IsEnabled = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            filterSelect = true;
            PageBox.SelectedValue = page;
            filterSelect = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            var search = searched ? SearchBar.Text : null;
            await FeedGenerator.GetFeed(page, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem,
                (GameBananaCategory)SubCatBox.SelectedItem, (PerPageBox.SelectedIndex + 1) * 10, (bool)NSFWCheckbox.IsChecked, search);
            FeedBox.ItemsSource = FeedGenerator.CurrentFeed.Records;
            if (FeedGenerator.error)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Visible;
                if (FeedGenerator.exception.Message.Contains("JSON tokens"))
                {
                    BrowserMessage.Text = "Uh oh! Pizza Oven failed to deserialize the GameBanana feed.";
                    return;
                }
                switch (Regex.Match(FeedGenerator.exception.Message, @"\d+").Value)
                {
                    case "443":
                        BrowserMessage.Text = "Your internet connection is down.";
                        break;
                    case "500":
                    case "503":
                    case "504":
                        BrowserMessage.Text = "GameBanana's servers are down.";
                        break;
                    default:
                        BrowserMessage.Text = FeedGenerator.exception.Message;
                        break;
                }
                return;
            }
            if (page < FeedGenerator.CurrentFeed.TotalPages)
                PageRight.IsEnabled = true;
            if (page != 1)
                PageLeft.IsEnabled = true;
            if (FeedBox.Items.Count > 0)
            {
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Collapsed;
                BrowserMessage.Visibility = Visibility.Visible;
                BrowserMessage.Text = "Pizza Oven couldn't find any mods.";
            }
            PageBox.ItemsSource = Enumerable.Range(1, (int)(FeedGenerator.CurrentFeed.TotalPages));

            LoadingBar.Visibility = Visibility.Collapsed;
            CatBox.IsEnabled = true;
            SubCatBox.IsEnabled = true;
            TypeBox.IsEnabled = true;
            FilterBox.IsEnabled = true;
            PageBox.IsEnabled = true;
            PerPageBox.IsEnabled = true;
            SearchBar.IsEnabled = true;
            SearchButton.IsEnabled = true;
            NSFWCheckbox.IsEnabled = true;
            ClearCacheButton.IsEnabled = true;
        }

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                if (!searched || FilterBox.SelectedIndex != 3)
                {
                    filterSelect = true;
                    var temp = FilterBox.SelectedIndex;
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = temp;
                    filterSelect = false;
                }
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void PerPageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                page = 1;
                RefreshFilter();
            }
        }
        private void TypeFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set categories
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set Categories
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as UniformGrid;
            grid.Columns = (int)grid.ActualWidth / 400 + 1;
        }
        private void OnResize(object sender, RoutedEventArgs e)
        {
            BigScreenshot.MaxHeight = ActualHeight - 240;
        }

        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = (int)PageBox.SelectedValue;
                RefreshFilter();
            }
        }
        private void NSFWCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                if (searched)
                {
                    filterSelect = true;
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                    filterSelect = false;
                }
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void ClearCache(object sender, RoutedEventArgs e)
        {
            FeedGenerator.ClearCache();
            RefreshFilter();
        }

        private void Search()
        {
            if (!filterSelect && IsLoaded && !String.IsNullOrWhiteSpace(SearchBar.Text))
            {
                filterSelect = true;
                FilterBox.ItemsSource = FilterBoxListWhenSearched;
                FilterBox.SelectedIndex = 3;
                NSFWCheckbox.IsChecked = true;
                // Set categories
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                searched = true;
                page = 1;
                RefreshFilter();
            }
        }
        private void SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }
        private static readonly List<string> FilterBoxList = new string[] { "Featured", "Recent", "Popular" }.ToList();
        private static readonly List<string> FilterBoxListWhenSearched = new string[] { "Featured", "Recent", "Popular", "- - -" }.ToList();

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsLoaded && ModGridSearchButton.IsEnabled)
            if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.F:
                        ModGrid_SearchBar.Focus();
                        break;
                }
            }
        }

        private void ModGrid_SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Enter))
                ModGridSearch();
        }
        private void ModGridSearch()
        {
            if (!String.IsNullOrEmpty(ModGrid_SearchBar.Text) && ModGridSearchButton.IsEnabled && Global.ModList.Count > 0)
            {
                string text = ModGrid_SearchBar.Text;
                Global.ModList = new ObservableCollection<Mod>(Global.ModList.Where(mod => mod.name.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                    .Concat(Global.ModList.Where(mod => !mod.name.Contains(text, StringComparison.InvariantCultureIgnoreCase))));
                
                Refresh();
                ModGrid.ScrollIntoView(ModGrid.Items[0]);             
            }
        }

        private void ModGridSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ModGridSearch();
        }

        private void Clear_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ModGrid_SearchBar.Clear();
        }

    }
}
