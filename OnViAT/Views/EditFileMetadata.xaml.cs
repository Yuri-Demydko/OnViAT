
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnViAT.Enums;
using OnViAT.Models;

namespace OnViAT.Views
{
    public class EditFileMetadata:Window
    {
        private MarkupModel _fileMarkupModel;
        public EditFileMetadata()
        {
            InitializeComponent();
            
            
//#if DEBUG
    //        this.AttachDevTools();
//#endif
        }

        private void FillData()
        {
            this.FindControl<TextBox>("Title").Text = _fileMarkupModel.FileMainMetadata[MetaDataTypes.Title];
            this.FindControl<TextBox>("Description").Text = _fileMarkupModel.FileMainMetadata[MetaDataTypes.Description];
        }

        private void UpdateData()
        {
            var title = this.FindControl<TextBox>("Title").Text;
            var description = this.FindControl<TextBox>("Description").Text;
            if (String.IsNullOrWhiteSpace(title))
                title = "Без названия";
            if (String.IsNullOrWhiteSpace(description))
                description = "Без описания";
            _fileMarkupModel.Ontology.EditMediaResourceMetadata(title,MetaDataTypes.Title);
            _fileMarkupModel.Ontology.EditMediaResourceMetadata(description,MetaDataTypes.Description);
        }
        public void SetMRW(MarkupModel markupModel)
        {
            this._fileMarkupModel = markupModel;
            FillData();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            UpdateData();
            this.Close();
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}