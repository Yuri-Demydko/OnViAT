using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnViAT.Models;
using OnViAT.ViewModels;

namespace OnViAT.Views
{
    public class AddOrEditIndividualForm:Window
    {
        private MarkupModel _markupModel;
        private string _fragmentUri;
        private string _editingIndividualUri;

        private bool _editing = false;
        
        //BASIC CODE FOR FORM INIT------------------
        public AddOrEditIndividualForm()
                {
                    InitializeComponent();
        // #if DEBUG
        //             this.AttachDevTools();
        // #endif
                }
        private void InitializeComponent()
                {
                    AvaloniaXamlLoader.Load(this);
                }
        //-------------------------------------------

        public void SetupHierarchyTree(ClassHierarchyTreeModel treeModel)
        {
            var treeview = this.FindControl<TreeView>("OntologyClassesTreeView");

            TreeViewItem root = new TreeViewItem();
            root.Header = treeModel;
            root.Tag = treeModel;
            root.IsExpanded = true;
            root.IsSelected = true;
            treeview.SelectedItem = root;
            UnpackSubHierarchy(treeModel,root);
            
            var items = new List<TreeViewItem>();
            items.Add(root);
            treeview.Items = items;

            void UnpackSubHierarchy(ClassHierarchyTreeModel rootTM, TreeViewItem rootTVI)
            {
                var treeViewItems = new List<TreeViewItem>();
                foreach (var subnode in rootTM.SubNodes)
                {
                    TreeViewItem subnodeTvi = new TreeViewItem();
                    subnodeTvi.Header = subnode;
                    subnodeTvi.Tag = subnode;
                    if (subnode.SubNodes.Any())
                    {
                        UnpackSubHierarchy(subnode,subnodeTvi);
                    }
                    subnodeTvi.IsExpanded = true;
                    treeViewItems.Add(subnodeTvi);
                }
                rootTVI.Items = treeViewItems;
            }
        }

        public void InitialSetup(MarkupModel markupModel, string fragmentUri)
        {
            this._markupModel = markupModel;
            this._fragmentUri = fragmentUri;
        }

        public void SetupForEditing(ContentObjectIndividualModel individual)
        {
            this.FindControl<StackPanel>("OntologyClassesTreeViewStackPanel").IsVisible = false;
            this.FindControl<NumericUpDown>("Quantity").Value = Convert.ToDouble(individual.Quantity);
            this.FindControl<TextBox>("UOName").Text = individual.UoName;
            this._editingIndividualUri = individual.Uri;
            this.MinHeight = 300;
            this.Height = 300;
            this.FindControl<TextBlock>("Header").Text = "Редактирование индивида";
            this.MinWidth = 320;
            this.Width = 320;
            _editing = true;
        }
        private void SaveIndividual()
        {
            var treeview_sel_item = this.FindControl<TreeView>("OntologyClassesTreeView").SelectedItem as TreeViewItem;
            if (!_editing)
            {
                if (treeview_sel_item != null)
                {
                    var ClassUri = ((ClassHierarchyTreeModel) treeview_sel_item.Tag).URI;
                    var UOName = this.FindControl<TextBox>("UOName").Text;
                    var Qty = this.FindControl<NumericUpDown>("Quantity").Value;
                    if (String.IsNullOrWhiteSpace(this.FindControl<TextBox>("UOName").Text))
                        UOName = "Без названия";

                    this._markupModel.Ontology.AddContentObjectIndividual(_fragmentUri, ClassUri, UOName,
                        Qty.ToString());


                }
            }
            else
            {
                var UOName = this.FindControl<TextBox>("UOName").Text;
                var Qty = this.FindControl<NumericUpDown>("Quantity").Value;
                if (String.IsNullOrWhiteSpace(this.FindControl<TextBox>("UOName").Text))
                    UOName = "Без названия";
                this._markupModel.Ontology.EditContentObjectIndividual(_editingIndividualUri,UOName,Qty.ToString());
            }
        }
        private void Save_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                SaveIndividual();
            }
            catch (Exception)
            {
                this.Close();
            }
            ((MainWindow)(this.Owner)).UpdateIndividualsList();
            this.Close();
            
            //this.Close();
        }

        private void Cancel_OnClick(object? sender, RoutedEventArgs e)
        {
         this.Close();
        }
        
    }
}