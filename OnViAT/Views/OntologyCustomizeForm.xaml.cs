using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnViAT.Helpers;
using OnViAT.Models;
using OnViAT.ViewModels;
using VDS.RDF;

namespace OnViAT.Views
{
    public class OntologyCustomizeForm:Window
    {
        public OntologyCustomizeForm()
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
        private OntologyConfigurationModel _ontologyConfigurationModel;
        public void SetupConfigurationModel(OntologyConfigurationModel model)
        {
            this._ontologyConfigurationModel = model;
        }
        private string SelectedClassUri
        {
            get
            {
                var tvi = this.FindControl<TreeView>("OntologyClassesTreeView")?.SelectedItem as TreeViewItem;
                return ((ClassHierarchyTreeModel?) tvi?.Tag)?.URI;
            }
        }
        private void UpdateButtonStatuses(string selectedUri)
        {
            var subclassBtn = this.FindControl<Button>("SubclassBtn");
            var renameBtn = this.FindControl<Button>("RenameBtn");
            var parallelBtn = this.FindControl<Button>("ParallelBtn");
            var removeBtn = this.FindControl<Button>("RemoveBtn");

            selectedUri ??= "";
            
            subclassBtn.IsEnabled = !SealedOntologyClassesHelper.Sealed(selectedUri);
            renameBtn.IsEnabled = _ontologyConfigurationModel.IsCustomClass(selectedUri)
                &&!string.IsNullOrWhiteSpace(NewUri);
            parallelBtn.IsEnabled = !ParallelSealedOntologyClassesHelper.ParallelSealed(selectedUri);
            removeBtn.IsEnabled = _ontologyConfigurationModel.IsCustomClass(selectedUri);


            this.FindControl<TextBox>("NewClassName").IsEnabled = subclassBtn.IsEnabled ||
                                                                  renameBtn.IsEnabled ||
                                                                  parallelBtn.IsEnabled ||
                                                                  removeBtn.IsEnabled;

        }
        private string NewUri
            => !string.IsNullOrWhiteSpace(this.FindControl<TextBox>("NewClassName").Text)?this.FindControl<TextBox>("NewClassName").Text:"Unnamed";
        private void ClearNewName()
        {
            this.FindControl<TextBox>("NewClassName").Text = "";
        }
        public void SetupHierarchyTree(ClassHierarchyTreeModel treeModel)
        {
            var treeview = this.FindControl<TreeView>("OntologyClassesTreeView");

            TreeViewItem root = new TreeViewItem();
            root.Header = treeModel.Header;
            root.Tag =  treeModel;
            root.IsExpanded = true;
            root.IsSelected = true;
            treeview.SelectedItem = root;

            UnpackSubHierarchy(treeModel,root);
            
            var items = new List<TreeViewItem>();
            items.Add(root);
            treeview.Items = items;

            void UnpackSubHierarchy(ClassHierarchyTreeModel rootTM, TreeViewItem rootTVI)
            {
                var Items = new List<TreeViewItem>();
                foreach (var subnode in rootTM.SubNodes)
                {
                    TreeViewItem subnodeTVI = new TreeViewItem();
                    subnodeTVI.Header = subnode.Header;
                    subnodeTVI.Tag = subnode;
              
                    if (subnode.SubNodes.Count() > 0)
                    {
                        UnpackSubHierarchy(subnode,subnodeTVI);
                    }
                    subnodeTVI.IsExpanded = true;
                    Items.Add(subnodeTVI);
                }
                rootTVI.Items = Items;
            }
        }
        private async void SubClass_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                _ontologyConfigurationModel.AddSubClass(SelectedClassUri, NewUri);
                var path=_ontologyConfigurationModel.SaveAdditionsGraph();
                (this.Owner as MainWindow)?.SetAdditionalGraphPath(path);
                this.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,path));
                this.ClearNewName();
            }
            catch
            {
               await MessageBox.Show(this, "Error", "Error",MessageBox.MessageBoxButtons.Ok);
            }
        }
        private async void ParallelClass_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                _ontologyConfigurationModel.AddParallelClass(SelectedClassUri, NewUri);
                var path=_ontologyConfigurationModel.SaveAdditionsGraph();
                (this.Owner as MainWindow)?.SetAdditionalGraphPath(path);
                this.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,path));
                this.ClearNewName();
            }
            catch
            {
                await MessageBox.Show(this, "Error", "Error",MessageBox.MessageBoxButtons.Ok);
            }
        }
        private async void Rename_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                _ontologyConfigurationModel.RenameCustomClass(SelectedClassUri, NewUri);
                var path=_ontologyConfigurationModel.SaveAdditionsGraph();
                (this.Owner as MainWindow)?.SetAdditionalGraphPath(path);
                this.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,path));
                this.ClearNewName();
            }
            catch
            {
                await MessageBox.Show(this, "Error", "Error",MessageBox.MessageBoxButtons.Ok);
            }
        }
        private async void Remove_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                _ontologyConfigurationModel.RemoveCustomClass(SelectedClassUri);
                var path=_ontologyConfigurationModel.SaveAdditionsGraph();
                (this.Owner as MainWindow)?.SetAdditionalGraphPath(path);
                this.SetupHierarchyTree(OntologyModel.ExportOntologyClassesAsTree(Constants.Constants.BASE_ONTOLOGY,path));
                this.ClearNewName();
            }
            catch
            {
                await MessageBox.Show(this, "Error", "Error",MessageBox.MessageBoxButtons.Ok);
            }
        }
        private void Window_OnClosing(object? sender, CancelEventArgs e)
        {
            (this.Owner as MainWindow)?.UpdateOntologies(this._ontologyConfigurationModel.Operations);
            (this.Owner as MainWindow)?.SaveDir();
            (this.Owner as MainWindow)?.ReCreateStorageModel();
           (this.Owner as MainWindow)?.UpdateAll();
            
        }
        private void OntologyClassesTreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var uri = e.AddedItems.Count > 0 ? ((ClassHierarchyTreeModel)e.AddedItems.OfType<TreeViewItem>().First().Tag).URI : "";
            UpdateButtonStatuses(uri);
        }
    }
}