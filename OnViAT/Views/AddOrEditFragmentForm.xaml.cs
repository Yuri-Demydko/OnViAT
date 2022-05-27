using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnViAT.Models;
using OnViAT.ViewModels;

namespace OnViAT.Views
{
    public class AddOrEditFragmentForm : Window
    {
        private MarkupModel _fileMarkupModel;
        private TimeSpan _videoTime;
        private bool _editing = false;
        private string _editing_fragment_id;
        public AddOrEditFragmentForm()
        {
            InitializeComponent();
//#if DEBUG
    //        this.AttachDevTools();
//#endif
        }

        public void SetupForEditing(MediaFragmentModel fragment)
        {
            TimeSpan begin = TimeSpan.Parse(fragment.BeginsAt);
            TimeSpan end = TimeSpan.Parse(fragment.EndsAt);
            
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");
            
            begins_h.Value = begin.Hours;
            begins_m.Value = begin.Minutes;
            begins_s.Value = begin.Seconds;
            
            ends_h.Value = end.Hours;
            ends_m.Value = end.Minutes;
            ends_s.Value = end.Seconds;

            _editing_fragment_id = fragment.Uri;
            this.FindControl<TextBox>("Description").Text = fragment.Description;
            _editing = true;
            this.FindControl<TextBlock>("Header").Text = "Редактирование фрагмента";
        }
        public void SetTimeMaximums(TimeSpan videoDuration)
        {
            _videoTime = videoDuration;
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");
            
            begins_h.Maximum = videoDuration.TotalHours;
            begins_m.Maximum = 60;
            begins_s.Maximum = 60;
            
            ends_h.Maximum = videoDuration.TotalHours;
            ends_m.Maximum = 60;
            ends_s.Maximum = 60;
           
           if (Convert.ToInt32(videoDuration.TotalHours)==0)
           {
               begins_h.IsEnabled = false;
               begins_m.Maximum = videoDuration.TotalMinutes;
               
               ends_h.IsEnabled = false;
               ends_m.Maximum = videoDuration.TotalMinutes;
           }
            if (Convert.ToInt32(videoDuration.TotalMinutes) == 0)
            {
                begins_m.IsEnabled = false;
                begins_s.Maximum = videoDuration.TotalSeconds;
                
                ends_m.IsEnabled = false;
                ends_s.Maximum = videoDuration.TotalSeconds;
            }
            if(Convert.ToInt32(videoDuration.TotalSeconds) == 0)
            {
                begins_s.IsEnabled = false;
                ends_s.IsEnabled = false;
            }

        }
        public void SetMRW(MarkupModel markupModel)
        {
            this._fileMarkupModel = markupModel;
        }

        public void SetupRecordedTimeMarkers(TimeSpan begin, TimeSpan end)
        {
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");

            begins_h.Value = begin.Hours;
            begins_m.Value = begin.Minutes;
            begins_s.Value = begin.Seconds;
            
            begins_h.IsEnabled = false;
            begins_m.IsEnabled = false;
            begins_s.IsEnabled = false;

            ends_h.Value = end.Hours;
            ends_m.Value = end.Minutes;
            ends_s.Value = end.Seconds;
            
            ends_h.IsEnabled = false;
            ends_m.IsEnabled = false;
            ends_s.IsEnabled = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SaveFragment()
        {
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");

            var begins = begins_h.Text + ":" + begins_m.Text + ":" + begins_s.Text;          //this.FindControl<TextBox>("BeginsAt").Text;
            var ends = ends_h.Text + ":" + ends_m.Text + ":" + ends_s.Text;   //this.FindControl<TextBox>("EndsAt").Text;
            var desc = this.FindControl<TextBox>("Description").Text;
            if (String.IsNullOrWhiteSpace(desc))
                desc = "Нет описания";
            
            if(!_editing)
                this._fileMarkupModel.Ontology.AddMediaFragment(begins,ends,desc);
            else
                this._fileMarkupModel.Ontology.EditMediaFragment(_editing_fragment_id,begins,ends,desc);
        }
        
        private void Ok_onClick(object? sender, RoutedEventArgs e)
        {
            //SaveFragment();
            try
            {
                SaveFragment();
            }
            catch (Exception)
            {
                this.Close();
            }
            ((MainWindow)(this.Owner)).UpdateFragmentsList();
            this.Close();
            // throw new NotImplementedException();
        }


        private void Cancel_onClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BH_onChange(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            ReCalculateMaximums();
            ReCalculateMinimums();
            if (begins_m.Value > begins_m.Maximum)
                begins_m.Value = begins_m.Maximum;
            if (begins_s.Value > begins_s.Maximum)
                begins_s.Value = begins_s.Maximum;
        }

        private void ReCalculateMinimums()
        {
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");
            
            var totalBeginsSeconds = begins_s.Value + begins_m.Value * 60 + begins_h.Value * 3600;
            var totalEndSeconds = ends_s.Value + ends_m.Value * 60 + ends_h.Value * 3600;

            var diff = _videoTime - TimeSpan.FromSeconds(totalBeginsSeconds);

            ends_h.Minimum = begins_h.Value;
            if (Math.Floor(diff.TotalMinutes) > 59)
                ends_m.Minimum = 0;
            else
            {
                ends_m.Minimum = begins_m.Value;
            }
            if (Math.Floor(diff.TotalSeconds) > 59)
                ends_s.Minimum = 0;
            else
            {
                ends_s.Minimum = begins_s.Value;
            }
        
            if (totalBeginsSeconds > totalEndSeconds)
            {
                ends_s.Value = begins_s.Value;
                ends_m.Value = begins_m.Value;
                ends_h.Value = begins_h.Value;
            }
        }
        private void ReCalculateMaximums()
        {
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            begins_m.Maximum = Math.Floor(_videoTime.TotalMinutes) - begins_h.Value * 60;
            begins_s.Maximum = Math.Floor(_videoTime.TotalSeconds) - begins_m.Value * 60-begins_h.Value*3600;
            
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");
            ends_m.Maximum = Math.Floor(_videoTime.TotalMinutes) - ends_h.Value * 60;
            ends_s.Maximum = Math.Floor(_videoTime.TotalSeconds) - ends_m.Value * 60-ends_h.Value*3600;

        }
        private void BMC(double NewValue)
        {
            var begins_h = this.FindControl<NumericUpDown>("BeginsAt_H");
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            
            if (NewValue == 60 && begins_h.Maximum >= begins_h.Value+1)
            {
                begins_m.Value = 0;
                begins_s.Value = 0;
                begins_h.Value += 1;
            }
            ReCalculateMaximums();
            ReCalculateMinimums();
        }
        private void EMC(double NewValue)
        {
            var ends_h = this.FindControl<NumericUpDown>("EndsAt_H");
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");
            
            if (NewValue == 60 && ends_h.Maximum >= ends_h.Value+1)
            {
                ends_m.Value = 0;
                ends_s.Value = 0;
                ends_h.Value += 1;
            }
            ReCalculateMaximums();
            ReCalculateMinimums();
        }
        private void BM_onChange(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            BMC(e.NewValue);
        }
        private void BS_onChange(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            var begins_m = this.FindControl<NumericUpDown>("BeginsAt_M");
            var begins_s = this.FindControl<NumericUpDown>("BeginsAt_S");
            if (e.NewValue == 60 && begins_m.Maximum >= begins_m.Value+1)
            {
                begins_s.Value = 0;
                begins_m.Value += 1;
               BMC(begins_m.Value + 1);
            }
            ReCalculateMaximums();
            ReCalculateMinimums();
        }

        private void EH_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
            var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");
            ReCalculateMaximums();
            if (ends_m.Value > ends_m.Maximum)
                ends_m.Value = ends_m.Maximum;
            if (ends_s.Value > ends_s.Maximum)
                ends_s.Value = ends_s.Maximum;
            ReCalculateMinimums();
        }

        private void EM_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
           EMC(e.NewValue);
        }

        private void ES_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
           var ends_m = this.FindControl<NumericUpDown>("EndsAt_M");
           var ends_s = this.FindControl<NumericUpDown>("EndsAt_S");

           if (e.NewValue == 60 && ends_m.Maximum >= ends_m.Value + 1)
           {
               ends_s.Value = 0;
               ends_m.Value += 1;
               EMC(ends_m.Value + 1);
           }

           ReCalculateMaximums();
           ReCalculateMinimums();
        }
    }
}