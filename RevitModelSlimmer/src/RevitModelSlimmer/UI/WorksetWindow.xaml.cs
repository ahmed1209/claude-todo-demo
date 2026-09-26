using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.DB;

namespace RevitModelSlimmer.UI
{
    public sealed class WorksetRow : INotifyPropertyChanged
    {
        private bool _isChecked;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public WorksetId Id { get; set; }
        public string Name { get; set; }
        public int ElementCount { get; set; }
        public string Owner { get; set; }
        public bool IsEditable { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }
        public string EditableText { get { return IsEditable ? "Yes" : "No"; } }
    }

    public partial class WorksetWindow : Window
    {
        private readonly ObservableCollection<WorksetRow> _rows;

        public WorksetWindow(IntPtr ownerHandle, IEnumerable<WorksetRow> rows)
        {
            InitializeComponent();
            if (ownerHandle != IntPtr.Zero)
            {
                new WindowInteropHelper(this).Owner = ownerHandle;
            }

            _rows = new ObservableCollection<WorksetRow>(rows.OrderBy(r => r.ElementCount).ThenBy(r => r.Name));
            foreach (WorksetRow row in _rows)
            {
                row.IsChecked = row.ElementCount == 0 && row.IsEditable && !row.IsDefault && !row.IsActive;
                row.PropertyChanged += (s, e) => UpdateState();
            }
            ItemsGrid.ItemsSource = _rows;
            TargetCombo.ItemsSource = _rows.OrderBy(r => r.Name).ToList();
            TargetCombo.SelectedItem = _rows.FirstOrDefault(r => r.IsActive) ?? _rows.FirstOrDefault(r => r.IsDefault) ?? _rows.FirstOrDefault();
            UpdateState();
        }

        public IList<WorksetRow> CheckedRows
        {
            get { return _rows.Where(r => r.IsChecked).ToList(); }
        }

        public bool DeleteElements
        {
            get { return DeleteRadio.IsChecked == true; }
        }

        public WorksetRow TargetWorkset
        {
            get { return TargetCombo.SelectedItem as WorksetRow; }
        }

        private void UpdateState()
        {
            int selected = _rows.Count(r => r.IsChecked);
            int elements = _rows.Where(r => r.IsChecked).Sum(r => r.ElementCount);
            CountText.Text = selected + " of " + _rows.Count + " workset(s) checked, " + elements + " element(s) affected";
            OkButton.IsEnabled = selected > 0;
        }

        private void CheckEmpty_Click(object sender, RoutedEventArgs e)
        {
            foreach (WorksetRow row in _rows) row.IsChecked = row.ElementCount == 0 && row.IsEditable && !row.IsDefault && !row.IsActive;
        }

        private void UncheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (WorksetRow row in _rows) row.IsChecked = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!DeleteElements)
            {
                WorksetRow target = TargetWorkset;
                if (target == null)
                {
                    MessageBox.Show(this, "Pick a target workset for the elements, or choose to delete the elements.", "Model Slimmer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (target.IsChecked)
                {
                    MessageBox.Show(this, "The target workset '" + target.Name + "' is itself checked for deletion. Pick another target.", "Model Slimmer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
