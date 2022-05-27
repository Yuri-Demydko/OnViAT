using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using JetBrains.Annotations;
using LibVLCSharp.Shared;
using OnViAT.Enums;
using OnViAT.Helpers;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using OnViAT.LibVLC;
using OnViAT.Models;
using OnViAT.ViewModels;
using File = System.IO.File;

namespace OnViAT.Views
{
    public struct RecordingData
    {
        public bool IsRecording;
        public bool FromDock;
        private TimeSpan _begin;
        private TimeSpan _end;
        private bool _beginInit;
        private bool _endInit;

        public bool MarkersInit
        {
            get { return _beginInit && _endInit; }
        }

        public TimeSpan Begin
        {
            get => _begin;
            set
            {
                _begin = value;
                _beginInit = true;
            }
        }

        public TimeSpan End
        {
            get => _end;
            set
            {
                _end = value;
                _endInit = true;
            }
        }

        public void Reset()
        {
            this = new RecordingData();
        }
    }
    public class MainWindow : Window
    {
        private readonly VideoView _videoView;
        private readonly LibVLCSharp.Shared.LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private DispatcherTimer _videoTime;
        private StorageModel _storageModel;
        private TimeSpan _videoDuration;
        private RecordingData _fragmentRecording;
        internal bool IsQuickAccessFormOpened;
        private QuickAccess _qaForm;

        private OntologySearchModel _searchModel;
        private bool _searchActive;
        

        internal void UpdateButtonStatuses()
        {
            this.FindControl<MenuItem>("SaveDir_mi").IsEnabled = false;
            this.FindControl<MenuItem>("SaveFile_mi").IsEnabled = false;
            this.FindControl<MenuItem>("EditMeta_mi").IsEnabled = false;
            this.FindControl<MenuItem>("ClearMarkup_mi").IsEnabled = false;
            this.FindControl<MenuItem>("RecordFragment_mi").IsEnabled = false;
            this.FindControl<MenuItem>("AddFragment_mi").IsEnabled = false;
            this.FindControl<MenuItem>("EditFragment_mi").IsEnabled = false;
            this.FindControl<MenuItem>("RemoveFragment_mi").IsEnabled = false;
            this.FindControl<MenuItem>("AddIndividual_mi").IsEnabled = false;
            this.FindControl<MenuItem>("EditIndividual_mi").IsEnabled = false;
            this.FindControl<MenuItem>("RemoveIndividual_mi").IsEnabled = false;
            this.FindControl<MenuItem>("ExportMarkup_mi").IsEnabled = false;
            this.FindControl<MenuItem>("ExportMarkup64_mi").IsEnabled = false;
            this.FindControl<MenuItem>("RegenURIs_mi").IsEnabled = false;
            this.FindControl<MenuItem>("Search_mi").IsEnabled = false;
            this.FindControl<MenuItem>("File_mi").IsEnabled = false;
            this.FindControl<MenuItem>("Fragment_mi").IsEnabled = false;
            this.FindControl<MenuItem>("Individual_mi").IsEnabled = false;
            this.FindControl<MenuItem>("Ontology_mi").IsEnabled = false; //--ForLater
            this.FindControl<MenuItem>("Other_mi").IsEnabled = false;
            //this.FindControl<MenuItem>("ExportAdditionGraph_mi").IsEnabled = false;
           
            if (_storageModel != null && _storageModel.GetPseudoFileInfos().Count > 0)
            {
                this.FindControl<MenuItem>("SaveDir_mi").IsEnabled = true;
                this.FindControl<MenuItem>("Search_mi").IsEnabled = true;
                this.FindControl<MenuItem>("Other_mi").IsEnabled = true;
                this.FindControl<MenuItem>("RegenURIs_mi").IsEnabled = true;
                this.FindControl<MenuItem>("Ontology_mi").IsEnabled = true;
            }

            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                _storageModel.SelectedFile.Exists)
            {
                this.FindControl<MenuItem>("SaveFile_mi").IsEnabled = true;
                this.FindControl<MenuItem>("EditMeta_mi").IsEnabled = true;
                this.FindControl<MenuItem>("ClearMarkup_mi").IsEnabled = true;
                this.FindControl<MenuItem>("RecordFragment_mi").IsEnabled = true;
                this.FindControl<MenuItem>("AddFragment_mi").IsEnabled = true;
                this.FindControl<MenuItem>("ExportMarkup_mi").IsEnabled = true;
                this.FindControl<MenuItem>("ExportMarkup64_mi").IsEnabled = true;

                if (IsQuickAccessFormOpened)
                {
                    _qaForm.FindControl<Button>("RecordFragment_btn").IsEnabled = true;
                    _qaForm.FindControl<Button>("EditMetadata_btn").IsEnabled = true;
                    _qaForm.FindControl<Button>("ClearMarkup_btn").IsEnabled = true;
                    _qaForm.FindControl<Button>("AddFragment_btn").IsEnabled = true;
                    _qaForm.FindControl<Button>("ExportMarkup_btn").IsEnabled = true;
                    _qaForm.FindControl<Button>("ExportMarkup64_btn").IsEnabled = true;
                }

                if (this.FindControl<ListBox>("FragmentsList").SelectedItem is MediaFragmentModel)
                {
                    this.FindControl<MenuItem>("EditFragment_mi").IsEnabled = true;
                    this.FindControl<MenuItem>("RemoveFragment_mi").IsEnabled = true;
                    this.FindControl<MenuItem>("AddIndividual_mi").IsEnabled = true;

                    if (IsQuickAccessFormOpened)
                    {
                        _qaForm.FindControl<Button>("EditFragment_btn").IsEnabled = true;
                        _qaForm.FindControl<Button>("RemoveFragment_btn").IsEnabled = true;
                        _qaForm.FindControl<Button>("AddIndividual_btn").IsEnabled = true;
                    }

                    if (this.FindControl<ListBox>("IndividualsList").SelectedItem is ContentObjectIndividualModel)
                    {
                        this.FindControl<MenuItem>("EditIndividual_mi").IsEnabled = true;
                        this.FindControl<MenuItem>("RemoveIndividual_mi").IsEnabled = true;

                        if (IsQuickAccessFormOpened)
                        {
                            _qaForm.FindControl<Button>("EditIndividual_btn").IsEnabled = true;
                            _qaForm.FindControl<Button>("RemoveIndividual_btn").IsEnabled = true;
                        }
                    }
                }
            }

            if (this.FindControl<MenuItem>("EditMeta_mi").IsEnabled == true ||
                this.FindControl<MenuItem>("ClearMarkup_mi").IsEnabled == true)
            {
                this.FindControl<MenuItem>("File_mi").IsEnabled = true;
            }

            if (this.FindControl<MenuItem>("RecordFragment_mi").IsEnabled == true ||
                this.FindControl<MenuItem>("AddFragment_mi").IsEnabled == true ||
                this.FindControl<MenuItem>("EditFragment_mi").IsEnabled == true ||
                this.FindControl<MenuItem>("RemoveFragment_mi").IsEnabled == true)
            {
                this.FindControl<MenuItem>("Fragment_mi").IsEnabled = true;
            }

            if (this.FindControl<MenuItem>("AddIndividual_mi").IsEnabled ||
                this.FindControl<MenuItem>("EditIndividual_mi").IsEnabled ||
                this.FindControl<MenuItem>("RemoveIndividual_mi").IsEnabled)
            {
                this.FindControl<MenuItem>("Individual_mi").IsEnabled = true;
            }

            if (this.FindControl<MenuItem>("ExportMarkup_mi").IsEnabled ||
                this.FindControl<MenuItem>("ExportMarkup64_mi").IsEnabled)
                this.FindControl<MenuItem>("Other_mi").IsEnabled = true;
        }
        private VideoStates _videoState = VideoStates.NoFile;
        public MainWindow()
        {
            InitializeComponent();

            _videoView = this.Get<VideoView>("VideoView");

            _libVlc = new LibVLCSharp.Shared.LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.EndReached += new EventHandler<EventArgs>(_videoEndReached);
            _videoView.MediaPlayer = _mediaPlayer;
            UpdateButtonStatuses();
            SetAdditionalGraphPath(AdditionalGraphCachePathHelper.CheckCache());
            
//#if DEBUG
    //        this.AttachDevTools();
//#endif
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void UpdateFilesList(SearchExportModel se)
        {
            //Получить список найденных URI файлов
            var foundFiles = _storageModel.GetFileInfosBySE(se);
            
            if (!_searchActive) return;
            
            //Получить объект списка файлов из формы
            var filesList = this.FindControl<ListBox>("FilesList");
            
            //Получить список всех файлов хранилища
            var names=_storageModel.GetPseudoFileInfos();
            
            //Отфильтровать список отображаемых файлов через LINQ-выражение и вывести на форму
            filesList.Items = names.Where(r => foundFiles.Any(f => f.FileInfo.Name == r.FileInfo.Name)).ToList();
        }

        private void UpdateFragmentsList(SearchExportModel se)
        {
            if (!_searchActive) return;

            var fileuri = _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology.GetMediaResourceUri();
            var fragments = _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology
                .GetFragmentsCollection();
            if (se.GetFragmentsByFile(fileuri) == null)
            {
                this.FindControl<ListBox>("FragmentsList").Items = fragments;
                UpdateButtonStatuses();
                return;
            }
            
            var res = (from frag in fragments
                where se.GetFragmentsByFile(fileuri).Contains(frag.Uri)
                    select frag).ToList();
            

            this.FindControl<ListBox>("FragmentsList").Items = res;
            UpdateButtonStatuses();
        }

        private void UpdateIndividualsList(SearchExportModel se)
        {
            if (!_searchActive) return;
            var fragment = this.FindControl<ListBox>("FragmentsList").SelectedItem as MediaFragmentModel;
            if (fragment != null)
            {
                var individualsCollection = _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology
                    .GetCoIndividualsCollection(fragment.Uri);
                
                if (se.GetIndividualsByFragment(fragment.Uri) == null)
                {
                   
                    this.FindControl<ListBox>("IndividualsList").Items = individualsCollection;
                    return;
                }
                
                
                var res = individualsCollection
                    .Where(x => se.GetIndividualsByFragment(fragment.Uri).Contains(x.Uri)).ToList();
                
                this.FindControl<ListBox>("IndividualsList").Items = res;
                
            }
            else this.FindControl<ListBox>("IndividualsList").Items = new List<ContentObjectIndividualModel>();
            UpdateButtonStatuses();
            
        }
        private void UpdateFilesList()
        {
            if (_searchActive)
            {
                UpdateFilesList(_searchModel.SearchResults);
                return;
            }
            var filesList = this.FindControl<ListBox>("FilesList");
            var fnames=_storageModel.GetPseudoFileInfos();
            filesList.Items = fnames;
            UpdateButtonStatuses();
        }
        private async Task UpdateDirectoryAsync()
        {
            var fd = new OpenFolderDialog() { };
            var res = await fd.ShowAsync(this);
            //Console.WriteLine(res[0]);
            if (!String.IsNullOrWhiteSpace(res))
            {
                if (_videoView.MediaPlayer.IsPlaying)
                {
                    _videoView.MediaPlayer.Stop();
                    _videoTime.Stop();
                }

                this._storageModel = new StorageModel(res);
                var files = this.FindControl<ListBox>("FilesList");
                files.UnselectAll();
                files.Items=new List<string>();
                var fragments=this.FindControl<ListBox>("FragmentsList");
                fragments.UnselectAll();
                fragments.Items=new List<string>();
                var inds = this.FindControl<ListBox>("IndividualsList");
                inds.UnselectAll();
                inds.Items=new List<string>();
                UpdateFilesList();
            }
        }
        void _videoTimeTick(object sender, EventArgs e)
        {
            UpdateSlider();
        }
        void _videoEndReached(object sender, EventArgs e)
        {
            _videoTime.Stop();
            UpdateSlider();
            _videoState = VideoStates.JustLoaded;
            if (_fragmentRecording.IsRecording)
            {
                StartAndStopFragmentRecording();
            }
            
        }
        private void UpdateSlider()
        {
            try
            {
                var slider = this.FindControl<Slider>("VideoSlider");
                slider.Value = (this._mediaPlayer.Position / 1f) * 100f;
            }
            catch (Exception)
            {
                //this.FindControl<Slider>("VideoSlider").Value = 0;
            }
        }
        private void VideoSlider_OnTapped(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName))
            {
                var pos1 = this._mediaPlayer.Position;
                var time = this._mediaPlayer.Time;
                
                this._mediaPlayer.Position = (float) ((Slider) sender)!.Value / 100f;

                if (_fragmentRecording.IsRecording && this._mediaPlayer.Position < pos1)
                {
                    StartAndStopFragmentRecording();
                    _fragmentRecording.End = new TimeSpan(time);
                }
            }
            else
            {
                (((Slider) sender)!).Value = 0;
            }
        }
        
        private async void OpenDir_OnClick(object? sender, RoutedEventArgs e)
        {
           await UpdateDirectoryAsync();
        }
        
        private void UpdateFileInfo()
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName))
            {
                this.FindControl<TextBlock>("FileInfo").Text = _storageModel.GetSelectedFileInfo();
            }
            else
            {
                this.FindControl<TextBlock>("FileInfo").Text = "Видеофайл не выбран";
            }
        }
        private void UpdateMarkupInfo()
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName))
            {
                var metaDictionary = _storageModel
                    .FilesToMarkupModels[_storageModel.SelectedFile]
                    .Ontology
                    .GetMediaResourceMetadata();
                var uri=_storageModel
                    .FilesToMarkupModels[_storageModel.SelectedFile]
                    .Ontology
                    .GetMediaResourceUri();
                string[] resStrings=new string[6];
                resStrings[5] = "URI: " + uri;
                foreach (var key in metaDictionary.Keys)
                {
                    switch (key)
                    {
                        case MetaDataTypes.Title:
                        {
                            resStrings[0] = "Название: " + metaDictionary[key];
                            break;
                        }
                        case MetaDataTypes.Description:
                        {
                            resStrings[1] = "Описание: " + metaDictionary[key];
                            break;
                        }
                        case MetaDataTypes.Duration:
                        {
                            resStrings[2] = "Длительность: " + metaDictionary[key]+" секунд";
                            break;
                        }
                        case MetaDataTypes.CreationDate:
                        {
                            resStrings[3] = "Дата создания: " + metaDictionary[key];
                            break;
                        }
                        case MetaDataTypes.EditDate:
                        {
                            resStrings[4] = "Дата изменения: " + metaDictionary[key];
                            break;
                        }
                    }

                    string res = "";
                    foreach (var str in resStrings)
                    {
                        res += str + "\n";
                    }

                    this.FindControl<TextBlock>("MarkupInfo").Text = res;
                }
            }
            else
            {
                this.FindControl<TextBlock>("MarkupInfo").Text = "Видеофайл не выбран";
            }
        }
        private void FilesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_fragmentRecording.IsRecording)
                {
                    StartAndStopFragmentRecording();
                }

                var file = (FileViewModel)(((ListBox) (sender)).SelectedItem);
                _storageModel.SelectFile(file.FileInfo);
            
                if (_videoView.MediaPlayer.IsPlaying)
                {
                    _videoTime.Stop();
                    _videoView.MediaPlayer.Stop();
                }
                var tfile = TagLib.File.Create(_storageModel.SelectedFile.FullName);
                _videoDuration = tfile.Properties.Duration;

                _videoState = VideoStates.JustLoaded;
                UpdateSlider();
                //this.FindControl<TextBlock>("FName").Text = _storageModel.SelectedFileName;
                UpdateFragmentsList();
                UpdateIndividualsList();
                UpdateFragmentInfo();
                UpdateFileInfo();
                UpdateMarkupInfo();
                UpdateIndividualInfo();
                _videoView.MediaPlayer.Media = new Media(_libVlc,
                    _storageModel.SelectedFileName, FromType.FromPath);
            }
            catch (Exception)
            {
               var lb= ((ListBox) (sender));
               lb.UnselectAll();
               if (_videoView.MediaPlayer.IsPlaying)
               {
                   _videoTime.Stop();
                   _videoView.MediaPlayer.Stop();
               }
               UpdateFragmentsList();
               UpdateIndividualsList();
               UpdateFragmentInfo();
               UpdateFileInfo();
               UpdateIndividualInfo();
            }
        }

        public void SwitchSearch(bool mode)
        {
            _searchActive = mode;
        }
        public void UpdateFragmentsList()
        {
            if (_searchActive)
            {
                UpdateFragmentsList(_searchModel.SearchResults);
                return;
            }
            try
            {
                var fragments = _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology
                    .GetFragmentsCollection();
                this.FindControl<ListBox>("FragmentsList").Items = fragments;
                UpdateButtonStatuses();
            }
            catch(Exception)
            {
                this.FindControl<ListBox>("FragmentsList").Items = new List<string>();
                UpdateButtonStatuses();
            }
            // this.FindControl<ListBox>("FragmentsList").Items = items;
        }
        public void UpdateIndividualsList()
        {
            if (_searchActive)
            {
                UpdateIndividualsList(_searchModel.SearchResults);
                return;
            }
            var fragment = this.FindControl<ListBox>("FragmentsList").SelectedItem as MediaFragmentModel;
            if (fragment != null)
            {
                var IndividualsCollection = _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology
                    .GetCoIndividualsCollection(fragment.Uri);
                this.FindControl<ListBox>("IndividualsList").Items = IndividualsCollection;
                
            }
            else this.FindControl<ListBox>("IndividualsList").Items = new List<ContentObjectIndividualModel>();
            UpdateButtonStatuses();
            //UpdateIndividualInfo();
        }
        public void UpdateIndividualInfo()
        {
            var individual =
                this.FindControl<ListBox>("IndividualsList").SelectedItem as ContentObjectIndividualModel;
            if (individual != null)
            {
                this.FindControl<TextBlock>("IndividualInfo").Text = individual.GetAllInfo();
            }
            else this.FindControl<TextBlock>("IndividualInfo").Text = "Индивид не выбран";
            UpdateButtonStatuses();
        }
        internal void AddFragment_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && _storageModel.SelectedFile != null)
            {
                var form = new AddOrEditFragmentForm();
                form.SetMRW(_storageModel.FilesToMarkupModels[_storageModel.SelectedFile]);
                form.SetTimeMaximums(_videoDuration);
                form.ShowDialog(this);
            }
        }
        internal async void ExportXml_Click(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && _storageModel.SelectedFile != null)
            {
               await SaveExportAsync(_storageModel.FilesToMarkupModels[_storageModel.SelectedFile].DecodedFileMarkup,
                    _storageModel.SelectedFile.Name, "_RDF.xml");
            }
        }
        internal void EditMetadataButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && _storageModel.SelectedFile != null)
            {
                var form = new EditFileMetadata();
                form.SetMRW(_storageModel.FilesToMarkupModels[_storageModel.SelectedFile]);
                form.ShowDialog(this);
            }
        }
        private void SaveDir_OnClick(object? sender, RoutedEventArgs e)
        {
            SaveDir();
        }

        public void SaveDir()
        {
            if (_storageModel == null) return;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _mediaPlayer.Stop();
                var btn = this.FindControl<Button>("PlayPauseBTN");
                var imgS = this.FindControl<Image>("PlayVideoImg").Source;
                var img = new Image();
                img.Source = imgS;
                btn.Content = img;
                _videoState = VideoStates.JustLoaded;
            }
            foreach (var fileinfo_mrw_pair in _storageModel.FilesToMarkupModels)
            {
                if (fileinfo_mrw_pair.Key.Exists)
                {
                    fileinfo_mrw_pair.Value.ReWriteMarkup();
                }
            }
        }

        internal void ClearSelectedFileMarkup_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && _storageModel.SelectedFile != null)
            {
                _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].ClearMarkup();
            }
            UpdateFragmentsList();
            UpdateIndividualsList();
            UpdateIndividualInfo();
        }
        private void StartAndStopFragmentRecording()
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {

                if (!_fragmentRecording.IsRecording)
                {
                    if (_videoState == VideoStates.JustLoaded)
                    {
                        _videoView.MediaPlayer.Play(new Media(_libVlc,
                            _storageModel.SelectedFileName, FromType.FromPath));
                        // this.FindControl<Button>("PlayPauseButton").Content = "Pause";
                        _videoTime = new DispatcherTimer();
                        _videoTime.Interval = TimeSpan.FromMilliseconds(500);
                        _videoTime.Tick += new EventHandler(_videoTimeTick);
                        _videoTime.Start();
                        _videoState = VideoStates.HasPlayed;
                    }
                    else if (_videoState == VideoStates.HasPlayed)
                    {
                        if (_mediaPlayer.IsPlaying)
                        {
                            _videoTime.Stop();
                            _videoView.MediaPlayer.Pause();
                        }
                        _mediaPlayer.SetPause(false);
                        _videoTime.Start();
                        //VideoView.MediaPlayer.Pause();
                    }

                    _fragmentRecording.IsRecording = true;
                    
                    if(!_fragmentRecording.FromDock)
                    {
                        var btn = _qaForm.FindControl<Button>("RecordFragment_btn");
                        var tip = _qaForm.FindControl<TextBlock>("RecordFragmentToolTipBlock");
                        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                        var imgS = this.FindControl<Image>("StopRecImg").Source;
                        var img = new Image();
                        img.Source = imgS;
                        btn.Content = img;
                        tip.Text = "Остановить запись";
                    }
                    else
                    {
                        this.FindControl<MenuItem>("RecordFragment_mi").Header="Остановить запись фрагмента";
                    }
                    _fragmentRecording.Begin = TimeSpan.FromMilliseconds(this._mediaPlayer.Time);
                    
                    var ppbtn = this.FindControl<Button>("PlayPauseBTN");
                    var ppimgS = this.FindControl<Image>("PauseVideoImg").Source;
                    var ppimg = new Image();
                    ppimg.Source = ppimgS;
                    ppbtn.Content = ppimg;
                    
                }
                else
                {
                    _videoTime.Stop();
                    _videoView.MediaPlayer.Pause();
                    _fragmentRecording.IsRecording = false;
                    if(!_fragmentRecording.FromDock)
                    {
                        var btn = _qaForm.FindControl<Button>("RecordFragment_btn");
                        var tip = _qaForm.FindControl<TextBlock>("RecordFragmentToolTipBlock");
                        var imgS = this.FindControl<Image>("RecImg").Source;
                        var img = new Image();
                        img.Source = imgS;
                        btn.Content = img;
                        tip.Text = "Записать фрагмент";
                    }
                    else
                    {
                        this.FindControl<MenuItem>("RecordFragment_mi").Header="Начать запись фрагмента";
                    }
                    
                    _fragmentRecording.End = TimeSpan.FromMilliseconds(this._mediaPlayer.Time);
                    
                    var ppbtn = this.FindControl<Button>("PlayPauseBTN");
                    var ppimgS = this.FindControl<Image>("PlayVideoImg").Source;
                    var ppimg = new Image();
                    ppimg.Source = ppimgS;
                    ppbtn.Content = ppimg;

                    if (_fragmentRecording.MarkersInit)
                    {
                        var form = new AddOrEditFragmentForm();
                        form.SetMRW(_storageModel.FilesToMarkupModels[_storageModel.SelectedFile]);
                        form.SetTimeMaximums(_videoDuration);
                        form.SetupRecordedTimeMarkers(_fragmentRecording.Begin, _fragmentRecording.End);
                        form.ShowDialog(this);
                    }
                    _fragmentRecording.Reset();
                }
            }
        }
        internal void RecordFragment_Onclick(object? sender, RoutedEventArgs e)
        {
           // throw new Exception("TEST EXCEPTION!");
           _fragmentRecording.FromDock = false;
            StartAndStopFragmentRecording();
        }
        private void RecordFragment_MenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            _fragmentRecording.FromDock = true;
            StartAndStopFragmentRecording();
            // throw new NotImplementedException();
        }
        private void GoToFragment(MediaFragmentModel fragment)
        {
            if(fragment!=null)
            {
                TimeSpan fragment_begin = TimeSpan.Parse(fragment.BeginsAt);
                long milissecs_time = (long) Math.Floor(fragment_begin.TotalMilliseconds);
                long total_milisecs = (long) Math.Floor(_videoDuration.TotalMilliseconds);
                float newPos = ((float)milissecs_time / ((float)total_milisecs / 100))/100;
                
                _mediaPlayer.Stop();
                _mediaPlayer.Play();
                _videoTime = new DispatcherTimer();
                _videoTime.Interval = TimeSpan.FromMilliseconds(500);
                _videoTime.Tick += new EventHandler(_videoTimeTick);
                _videoTime.Start();
                _mediaPlayer.Position = newPos;
                _videoState = VideoStates.HasPlayed;
                
                var btn = this.FindControl<Button>("PlayPauseBTN");
                var imgS = this.FindControl<Image>("PauseVideoImg").Source;
                var img = new Image();
                img.Source = imgS;
                btn.Content = img;
                // _mediaPlayer.Pause();
                //_videoTime.Stop();
            }
        }
        private void UpdateFragmentInfo()
        {
            var fragment =
                this.FindControl<ListBox>("FragmentsList").SelectedItem as MediaFragmentModel;
            if (fragment != null)
            {
                this.FindControl<TextBlock>("FragmentInfo").Text = fragment.GetAllInfo();
            }
            else this.FindControl<TextBlock>("FragmentInfo").Text = "Фрагмент не выбран";
        }
        private void FragmentsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            GoToFragment((MediaFragmentModel) this.FindControl<ListBox>("FragmentsList").SelectedItem);
            UpdateIndividualsList();
            UpdateFragmentInfo();
            UpdateIndividualInfo();
            // throw new NotImplementedException();
        }
        internal void EditFragment_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
               var fragment= this.FindControl<ListBox>("FragmentsList").SelectedItem as MediaFragmentModel;
               if (fragment != null)
               {
                   var form = new AddOrEditFragmentForm();
                   form.SetMRW(_storageModel.FilesToMarkupModels[_storageModel.SelectedFile]);
                   form.SetTimeMaximums(_videoDuration);
                   form.SetupForEditing(fragment);
                   form.ShowDialog(this);
               }
            }
            UpdateFragmentInfo();
            //throw new NotImplementedException();
        }
        internal void RemoveFragment_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                var fragment = (MediaFragmentModel) this.FindControl<ListBox>("FragmentsList").SelectedItem;
                if (fragment != null)
                    _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology
                        .RemoveMediaFragment(fragment.Uri);
            }

            UpdateFragmentsList();
            UpdateIndividualsList();
            UpdateFragmentInfo();
            UpdateIndividualInfo();
        }
        internal void AddIndividual_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                var fragment = this.FindControl<ListBox>("FragmentsList").SelectedItem as MediaFragmentModel;
                if (fragment != null)
                {
                    var form = new AddOrEditIndividualForm();
                    // form.SetupHierarchyTree(_storageModel
                    //     .FilesToMarkupModels[_storageModel.SelectedFile]
                    //     .Ontology
                    //     .ExportGraphAsTree());
                    form.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,AdditionalGraphPath));
                    form.InitialSetup(_storageModel
                        .FilesToMarkupModels[_storageModel.SelectedFile], fragment.Uri);
                    form.ShowDialog(this);
                }
            }
        }
        private void IndividualsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateIndividualInfo();
            //throw new NotImplementedException();
        }
        internal void EditIndividual_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                var fragment = this.FindControl<ListBox>("FragmentsList").SelectedItem as MediaFragmentModel;
                if (fragment != null)
                {
                    var individual =
                        this.FindControl<ListBox>("IndividualsList").SelectedItem as ContentObjectIndividualModel;
                    if (individual != null)
                    {
                        var form = new AddOrEditIndividualForm();
                        /*form.SetupHierarchyTree(_storageModel
                            .FilesToMarkupModels[_storageModel.SelectedFile]
                            .Ontology
                            .ExportGraphAsTree());*/
                        form.InitialSetup(_storageModel
                            .FilesToMarkupModels[_storageModel.SelectedFile], fragment.Uri);
                        form.SetupForEditing(individual);
                        form.ShowDialog(this);
                    }
                }
            }
            UpdateIndividualInfo();
        }
        internal void RemoveIndividual_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                if (this.FindControl<ListBox>("IndividualsList").SelectedItem is ContentObjectIndividualModel individual)
                    _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].Ontology
                        .RemoveCoIndividual(individual.Uri);
            }
            
            UpdateIndividualsList();
            UpdateIndividualInfo();
        }
        private void OpenQA_MenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            if (IsQuickAccessFormOpened) return;
            _qaForm= new QuickAccess();
            _qaForm.SetMainWindow(this);
            IsQuickAccessFormOpened = true;
            _qaForm.Show();
        }
        private void PlayPauseBTN_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                var btn = this.FindControl<Button>("PlayPauseBTN");
                if (_videoState == VideoStates.JustLoaded)
                {
                    _videoView.MediaPlayer.Play(new Media(_libVlc,
                        _storageModel.SelectedFileName, FromType.FromPath));
                    //this.FindControl<Button>("PlayPauseButton").Content = "Pause";
                    _videoTime = new DispatcherTimer();
                    _videoTime.Interval = TimeSpan.FromMilliseconds(500);
                    _videoTime.Tick += new EventHandler(_videoTimeTick);
                    _videoTime.Start();
                    _videoState = VideoStates.HasPlayed;
                    
                    var imgS = this.FindControl<Image>("PauseVideoImg").Source;
                    var img = new Image();
                    img.Source = imgS;
                    btn.Content = img;
                }
                else if (_videoState == VideoStates.HasPlayed)
                {
                    if (_mediaPlayer.IsPlaying)
                    {
                        _videoTime.Stop();
                        _videoView.MediaPlayer.SetPause(true);
                        var imgS = this.FindControl<Image>("PlayVideoImg").Source;
                        var img = new Image();
                        img.Source = imgS;
                        btn.Content = img;
                    }
                    else
                    {
                        _videoTime.Start();
                        _videoView.MediaPlayer.SetPause(false);
                        var imgS = this.FindControl<Image>("PauseVideoImg").Source;
                        var img = new Image();
                        img.Source = imgS;
                        btn.Content = img;
                    }

                    
                }
            }
        }
        private void StopBTN_OnClick(object? sender, RoutedEventArgs e)
        {
            _mediaPlayer.Stop();
            var btn = this.FindControl<Button>("PlayPauseBTN");
            var imgS = this.FindControl<Image>("PlayVideoImg").Source;
            var img = new Image();
            img.Source = imgS;
            btn.Content = img;
            _videoState = VideoStates.JustLoaded;
            //throw new NotImplementedException();
        }
        private void SaveCurrentFile_OnClick(object? sender, RoutedEventArgs e)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _mediaPlayer.Stop();
                var btn = this.FindControl<Button>("PlayPauseBTN");
                var imgS = this.FindControl<Image>("PlayVideoImg").Source;
                var img = new Image();
                img.Source = imgS;
                btn.Content = img;
                _videoState = VideoStates.JustLoaded;
            }
            if(_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
             System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                _storageModel.FilesToMarkupModels[_storageModel.SelectedFile].ReWriteMarkup();
            }
        }
        private async Task SaveExportAsync(string data, string fname, string postfix)
        {
            var fd = new OpenFolderDialog() { };
            var res = await fd.ShowAsync(this);
            //Console.WriteLine(res[0]);
            if (!String.IsNullOrWhiteSpace(res))
            {
                StreamWriter sw = new StreamWriter(res+"/"+fname + postfix);
                sw.WriteLine(data);
                sw.Close();
            }
        }
        internal async void ExportMarkup64_mi_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_storageModel != null && !String.IsNullOrWhiteSpace(_storageModel.SelectedFileName) &&
                System.IO.File.Exists(_storageModel.SelectedFileName))
            {
                var tfile = TagLib.File.Create(_storageModel.SelectedFile.FullName);
               await SaveExportAsync(tfile.Tag.Description,_storageModel.SelectedFile.Name, "_64.txt");
            }
        
        }

        private void SearchBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            //temporary. Later it should open in non-dialog mode with checking for already opened form instances
            if (this._storageModel == null)
                return;
            
            var form =  new SearchMenuForm();
            form.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,AdditionalGraphPath));
            this._searchModel = new OntologySearchModel(this._storageModel);
            form.SetupSearchModel(this._searchModel);
            form.ShowDialog(this);

        }
       

        public void UpdateAll()
        {
            SwitchSearch(false);
            _storageModel.DeselectAll();
            UpdateFilesList();
            UpdateFragmentsList();
            UpdateIndividualsList();
            UpdateFragmentInfo();
            UpdateFileInfo();
            UpdateMarkupInfo();
            UpdateIndividualInfo();
        }
        private void StopSearch_OnClick(object? sender, RoutedEventArgs e)
        {
            SwitchSearch(false);
            UpdateAll();
        }

        private void RegenURIs_mi_OnClick(object? sender, RoutedEventArgs e)
        {
            ReCreateStorageModel();
            _storageModel.RegenerateAllUris();
            UpdateAll();
            //throw new NotImplementedException();
        }


        internal void ReCreateStorageModel()
        {
            _storageModel = new StorageModel(_storageModel.DirectoryPath);
        }

        private void ClearAllMarkups_mi_OnClick(object? sender, RoutedEventArgs e)
        {
            foreach (var markup in _storageModel.FilesToMarkupModels.Values)
            {
                markup.ClearMarkup();
                markup.ReWriteMarkup();
            }
        }

        private void QSearch_OnClick(object? sender, RoutedEventArgs e)
        {
            if (this._storageModel == null)
                return;
            
            var form =  new DescriptionSearchMenuForm();
            this._searchModel = new OntologySearchModel(this._storageModel);
            form.SetupSearchModel(this._searchModel);
            form.ShowDialog(this);
        }

        private string _additionalGraphPath = null;
        
        private string AdditionalGraphPath => !string.IsNullOrWhiteSpace(_additionalGraphPath)
                                              &&
                                              File.Exists(_additionalGraphPath) ? _additionalGraphPath : "";
        public void SetAdditionalGraphPath(string path)
        {
            if(!string.IsNullOrWhiteSpace(path)&&File.Exists(path))
            {
                this.FindControl<MenuItem>("ExportAdditionGraph_mi").IsEnabled = true;
                _additionalGraphPath = path;
            }
        }

        private void OntologySettings_mi_OnClick(object? sender, RoutedEventArgs e)
        {
            //temporary. Later it should open in non-dialog mode with checking for already opened form instances
            
            var form = new OntologyCustomizeForm();
            form.SetupConfigurationModel(new OntologyConfigurationModel(Constants.Constants.BASE_ONTOLOGY,AdditionalGraphPath));
            form.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,AdditionalGraphPath));
            
            
            form.ShowDialog(this);
        }

        public void UpdateOntologies(List<OntologyOperation> operations)
        {
            operations = operations.Where(r => r.Kind == OntologyOperationKind.RenameClass ||
                                               r.Kind == OntologyOperationKind.RemoveClass).ToList();
            foreach (var ontology in _storageModel.FilesToMarkupModels.Values.Select(r=>r.Ontology).ToList())
            {
                ontology.ApplyOperations(operations);
            }
        }

        private async void ExportAdditionGraph_mi_OnClick(object? sender, RoutedEventArgs e)
        {
            var fd = new SaveFileDialog();
            fd.InitialFileName = $"OnViAT Additional Graph {DateTime.Now.ToShortDateString()}.xml";

            fd.Directory = Directory.GetCurrentDirectory();

           var res= await fd.ShowAsync(this);

           if (!string.IsNullOrWhiteSpace(res)&&File.Exists(AdditionalGraphPath))
           {
               var graphStrCache = await File.ReadAllTextAsync(AdditionalGraphPath);
               if (OntologyGraphValidatorHelper.IsValid(graphStrCache, Constants.Constants.ONTOLOGY_ADDITION_MARKER))
               {
                   await File.WriteAllTextAsync(res, graphStrCache);
               }

           }
        }

        private async void ImportAdditionGraph_mi_OnClick(object? sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog() { };
            
            fd.AllowMultiple = false;
            
            fd.Filters = new List<FileDialogFilter>();
            
            fd.Filters.Add(new FileDialogFilter()
            {
                Name = "XML Files", Extensions = { "xml" }
            });
            
            var res = await fd.ShowAsync(this);

            if (!string.IsNullOrWhiteSpace(res.FirstOrDefault()))
            {
                var graphString = await File.ReadAllTextAsync(res.First());

                if (OntologyGraphValidatorHelper.IsValid(graphString, Constants.Constants.ONTOLOGY_ADDITION_MARKER))
                {
                    var newPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.Constants.ADDITION_GRAPH_CACHE);
                    await File.WriteAllTextAsync(newPath,graphString);
                    SetAdditionalGraphPath(newPath);
                    UpdateAll();
                }
            }
        }
    }
}