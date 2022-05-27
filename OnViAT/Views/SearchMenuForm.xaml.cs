using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnViAT.Enums;
using OnViAT.Models;
using OnViAT.ViewModels;

namespace OnViAT.Views
{
    public class SearchMenuForm:Window
    {
        public SearchMenuForm()
        {
            InitializeComponent();
            // #if DEBUG
            //         this.AttachDevTools();
            //     #endif
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
        
        private List<ListBoxItem> _searchObjects=new List<ListBoxItem>();

        private OntologySearchModel _searchModel;


        public void SetupSearchModel(OntologySearchModel searchModel)
        {
            this._searchModel = searchModel;
        }

        public void SetupHierarchyTree(ClassHierarchyTreeModel treeModel)
        {
            var treeview = this.FindControl<TreeView>("OntologyClassesTreeView");

            TreeViewItem root = new TreeViewItem();
            root.Header = treeModel;
            root.Tag = treeModel;
            root.IsExpanded = true;
            root.IsSelected = true;
            treeview.SelectedItem = root;
            unpackSubHierarchy(treeModel,root);
            
            var items = new List<TreeViewItem>();
            items.Add(root);
            treeview.Items = items;

            void unpackSubHierarchy(ClassHierarchyTreeModel rootTM, TreeViewItem rootTVI)
            {
                var Items = new List<TreeViewItem>();
                foreach (var subnode in rootTM.SubNodes)
                {
                    TreeViewItem subnodeTVI = new TreeViewItem();
                    subnodeTVI.Header = subnode;
                    subnodeTVI.Tag = subnode;
                    if (subnode.SubNodes.Count() > 0)
                    {
                        unpackSubHierarchy(subnode,subnodeTVI);
                    }
                    subnodeTVI.IsExpanded = true;
                    Items.Add(subnodeTVI);
                }
                rootTVI.Items = Items;
            }
        }

        private void Cancel_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
        private void AddSearchObjectToList(SearchIndividualModel searchIndividual)
        {
            var lbi = new ListBoxItem();
            lbi.ContextMenu = new ContextMenu();
            var menuItemsList = new List<MenuItem>();
            var mItem = new MenuItem();
            mItem.Header = "Удалить";
            mItem.Click += (object sender, RoutedEventArgs e) =>
            {
                RemoveSearchObject(searchIndividual);
            };
            
            menuItemsList.Add(mItem);
            lbi.Content = searchIndividual.ToString();
            lbi.Tag = searchIndividual;
            lbi.ContextMenu.Items = menuItemsList;
            _searchObjects.Add(lbi);
            
            this.FindControl<ListBox>("SearchObjectsList").Items = new List<ListBoxItem>(_searchObjects);
        }

        private List<SearchIndividualModel> ExportSearchObjectList()
        {
            return _searchObjects.Select(x => x.Tag as SearchIndividualModel).ToList();
        }

        private void RemoveSearchObject(SearchIndividualModel searchIndividual)
        {
            var found = _searchObjects
                .FirstOrDefault(x =>
                    (((SearchIndividualModel) x.Tag).UoName == searchIndividual.UoName &&
                     ((SearchIndividualModel) x.Tag).ClassUri == searchIndividual.ClassUri));
            if (found != null)
                _searchObjects.Remove(found);
            
            this.FindControl<ListBox>("SearchObjectsList").Items = new List<ListBoxItem>(_searchObjects);
        }
        private bool CheckSearchObjectInList(SearchIndividualModel searchIndividual)
        {
            bool isHere = this.FindControl<ListBox>("SearchObjectsList").Items
                .Cast<ListBoxItem>()
                .Any(x => ((x.Tag as SearchIndividualModel)?.ClassUri == searchIndividual.ClassUri &&
                           (x.Tag as SearchIndividualModel)?.UoName == searchIndividual.UoName));
            if(searchIndividual.NameComparison==NameComparisonMode.DontCount)
                isHere=this.FindControl<ListBox>("SearchObjectsList").Items
                    .Cast<ListBoxItem>()
                    .Any(x => ((x.Tag as SearchIndividualModel)?.ClassUri == searchIndividual.ClassUri));
            return isHere;
        }
        private void AddSearchObject_OnClick(object? sender, RoutedEventArgs e)
        {
           var treeviewSelItem = this.FindControl<TreeView>("OntologyClassesTreeView").SelectedItem as TreeViewItem;
           
           if(treeviewSelItem==null)
               return;
           
           var classUri = ((ClassHierarchyTreeModel) treeviewSelItem.Tag).URI;
           var uoName = this.FindControl<TextBox>("UOName").Text;
           var qty = this.FindControl<NumericUpDown>("Quantity").Value;
           uoName ??= "";

           var QCompMode = this.FindControl<StackPanel>("QCompSelection_rb").Children;
           int QCompSelectedIndex=
               QCompMode
                   .Cast<RadioButton>()
                   .Where(x => x.IsChecked == true)
                   .Select(x => QCompMode.IndexOf(x))
                   .First();

           var QCompAllModes = new[]
           {
               QuantityComparisonMode.DontCount,
               QuantityComparisonMode.Equal,
               QuantityComparisonMode.Greater,
               QuantityComparisonMode.Less,
               QuantityComparisonMode.GreaterOrEqual,
               QuantityComparisonMode.LessOrEqual,
           };

           var NameCompModes = new[]
           {
               NameComparisonMode.DontCount,
               NameComparisonMode.Equals,
               NameComparisonMode.Contains,
           };
           var NSearchMode = this.FindControl<StackPanel>("NameCompMode_rb").Children;
           int NSearchSelectedIndex=
               NSearchMode
                   .Cast<RadioButton>()
                   .Where(x => x.IsChecked == true)
                   .Select(x => NSearchMode.IndexOf(x))
                   .First();
           
           
           var HSearchMode = this.FindControl<StackPanel>("HSearchMode_rb").Children;
           int HSearchSelectedIndex=
           HSearchMode
               .Cast<RadioButton>()
               .Where(x => x.IsChecked == true)
               .Select(x => HSearchMode.IndexOf(x))
               .First();

           var HSearchAllModes = new[]
           {
                HierarchySearchMode.TypeOnly,
                HierarchySearchMode.TypeAndSubclasses,
           };
           
           var sObject = new SearchIndividualModel(classUri,uoName,qty.ToString());
           sObject.QuantityComparison = QCompAllModes[QCompSelectedIndex];
           sObject.HierarchySearch = HSearchAllModes[HSearchSelectedIndex];
           sObject.NameComparison = NameCompModes[NSearchSelectedIndex];
           if (String.IsNullOrWhiteSpace(this.FindControl<TextBox>("UOName").Text)&&sObject.NameComparison!=NameComparisonMode.DontCount) 
           {
               MessageBox.Show(this, "Название объекта не может быть пустым", "Ошибка", MessageBox.MessageBoxButtons.Ok);
               return;
           }
           if (CheckSearchObjectInList(sObject))
           {
                MessageBox.Show(this, "Данный объект поиска уже существует", "Ошибка", MessageBox.MessageBoxButtons.Ok);
                return;
           }
           AddSearchObjectToList(sObject);
            //_searchObjects.Add(SObject);

          // this.FindControl<ListBox>("SearchObjectsList").Items = new List<SearchIndividualModel>(_searchObjects);
           this.FindControl<TextBox>("UOName").Text = "";
           this.FindControl<NumericUpDown>("Quantity").Value=1;
           
           
        }
        
        private void SearchButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _searchModel.FillSearchObjects(ExportSearchObjectList());
           var owner =((MainWindow)this.Owner);
           owner.UpdateAll();
           owner.SwitchSearch(true);
           owner.UpdateFilesList(this._searchModel.SearchResults);
           this.Close();
        }

        private void QuantityCompModeRadionButton_OnChecked(object? sender, RoutedEventArgs e)
        {
            this.FindControl<NumericUpDown>("Quantity").IsEnabled = ((RadioButton) sender)?.Name != "Dont";
        }
        

        private void Dont_n_OnChecked(object? sender, RoutedEventArgs e)
        {
            this.FindControl<TextBox>("UOName").IsEnabled = ((RadioButton) sender)?.Name != "Dont_n";
        }
    }
}