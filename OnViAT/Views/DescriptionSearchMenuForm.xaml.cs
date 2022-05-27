using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnViAT.Models;


namespace OnViAT.Views
{
    public class DescriptionSearchMenuForm:Window
    {
        public DescriptionSearchMenuForm()
        {
            InitializeComponent();
// #if DEBUG
//                     this.AttachDevTools();
// #endif
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.Width = 640;
            this.Height = 64;
        }
        
        private OntologySearchModel _searchModel;
        private bool _filesSearch;
        private bool _fragsSearch;
        
        public void SetupSearchModel(OntologySearchModel searchModel)
        {
            this._searchModel = searchModel;
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
            //throw new NotImplementedException();
        }

        
        private void Search_OnClick(object? sender, RoutedEventArgs e)
        {
            var searchString = this.FindControl<TextBox>("searchString").Text;
            if (string.IsNullOrWhiteSpace(searchString)||(!_filesSearch&&!_fragsSearch))
            {
                this.Close();
                return;
            }
            _searchModel.SearchByDescription(_filesSearch, _fragsSearch, searchString);
            
            var owner =((MainWindow)this.Owner);
            owner.UpdateAll();
            owner.SwitchSearch(true);
            owner.UpdateFilesList(this._searchModel.SearchResults);
            this.Close();
           // throw new NotImplementedException();
        }

        
        private void FilesSearch_OnChecked(object? sender, RoutedEventArgs e)
        {
            _filesSearch = !_filesSearch;
          
        }

        private void FragsSearch_OnChecked(object? sender, RoutedEventArgs e)
        {
            _fragsSearch = !_fragsSearch;
      
        }
        
    }
}