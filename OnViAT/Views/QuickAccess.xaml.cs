using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace OnViAT.Views
{
    public class QuickAccess : Window
    {
        private MainWindow _mainWindow;

        private bool _maximized = true;
        public void SetMainWindow(MainWindow mw)
        {
            this._mainWindow = mw;
        }
        public QuickAccess()
        {
            InitializeComponent();
            this.Height = 626;
            this.Width = 54;
//#if DEBUG
            //        this.AttachDevTools();
//#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void RecordFragment_Onclick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.RecordFragment_Onclick(sender,e);
        }

        private void AddFragment_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.AddFragment_OnClick(sender, e);
        }

        private void EditFragment_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.EditFragment_OnClick(sender,e);
        }

        private void RemoveFragment_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.RemoveFragment_OnClick(sender,e);
        }

        private void AddIndividual_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.AddIndividual_OnClick(sender,e);
        }

        private void EditIndividual_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.EditIndividual_OnClick(sender,e);
        }

        private void RemoveIndividual_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.RemoveIndividual_OnClick(sender,e);
        }

        private void EditMetadataButton_Click(object? sender, RoutedEventArgs e)
        {
            _mainWindow.EditMetadataButton_Click(sender,e);
        }

        private void ClearSelectedFileMarkup_OnClick(object? sender, RoutedEventArgs e)
        {
            _mainWindow.ClearSelectedFileMarkup_OnClick(sender,e);
            //throw new NotImplementedException();
        }

        internal void TECH_BTN_CLICK(object? sender, RoutedEventArgs e)
        {
            _mainWindow.ExportXml_Click(sender,e);
        }

        private void Window_OnClosing(object? sender, CancelEventArgs e)
        {
            this._mainWindow.IsQuickAccessFormOpened = false;
            // throw new NotImplementedException();
        }

        private void InputElement_OnLostFocus(object? sender, RoutedEventArgs e)
        {
            
           // throw new NotImplementedException();
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
           // throw new NotImplementedException();
        }

        private void InputElement_OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (!_maximized)
                this.ClientSize = new Size(this.Width, 626);
            else
            {
                this.ClientSize = new Size(this.Width, 183);
            }

            _maximized = !_maximized;
            //throw new NotImplementedException();
        }

        private void Window_OnOpened(object? sender, EventArgs e)
        {
            this._mainWindow.UpdateButtonStatuses();
           // throw new NotImplementedException();
        }

        private void Export64_btn_OnClick(object? sender, RoutedEventArgs e)
        {
            this._mainWindow.ExportMarkup64_mi_OnClick(sender,e);
            //throw new NotImplementedException();
        }
    }
    
    
}