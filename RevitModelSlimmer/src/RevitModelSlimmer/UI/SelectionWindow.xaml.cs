using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace RevitModelSlimmer.UI
{
    /// <summary>
    /// Generic "pick what to delete" dialog. Callers fill it with <see cref="SelectionItem"/> rows;
    /// rows flagged as recommended start checked. Returns the checked rows via <see cref="CheckedItems"/>.
    /// </summary>
    public partial class SelectionWindow : Window
    {
        private readonly ObservableCollection<SelectionItem> _items;
        private readonly ICollectionView _view;

        public SelectionWindow(IntPtr ownerHandle, string title, string header, string subHeader, IEnumerable<SelectionItem> items)
        {
            InitializeComponent();

            if (ownerHandle != IntPtr.Zero)
            {
                new WindowInteropHelper(this).Owner = ownerHandle;
            }

            Title = title;
            HeaderText.Text = header;
            SubHeaderText.Text = subHeader;

            _items = new ObservableCollection<SelectionItem>(items.OrderByDescending(i => i.Weight).ThenBy(i => i.Group).ThenBy(i => i.Name));
            foreach (SelectionItem item in _items)
            {
                item.IsChecked = item.Recommended;
                item.PropertyChanged += (s, e) => UpdateCount();
            }

            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = FilterRow;
            ItemsGrid.ItemsSource = _view;
            UpdateCount();
        }

        public string ActionButtonText
        {
            get { return OkButton.Content as string; }
            set { OkButton.Content = value; }
        }

        public void SetColumnHeaders(string name, string group, string details, string status)
        {
            NameColumn.Header = name;
            GroupColumn.Header = group;
            DetailsColumn.Header = details;
            StatusColumn.Header = status;
        }

        public void SetSummary(string summaryHeader, string summary)
        {
            SummaryExpander.Header = summaryHeader;
            SummaryText.Text = summary;
            SummaryExpander.Visibility = string.IsNullOrWhiteSpace(summary) ? Visibility.Collapsed : Visibility.Visible;
        }

        public IList<SelectionItem> CheckedItems
        {
            get { return _items.Where(i => i.IsChecked).ToList(); }
        }

        public IList<SelectionItem> AllItems
        {
            get { return _items.ToList(); }
        }

        private bool FilterRow(object obj)
        {
            var item = obj as SelectionItem;
            if (item == null) return false;
            if (RecommendedOnly.IsChecked == true && !item.Recommended) return false;
            string text = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return true;
            string[] terms = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string haystack = item.SearchText;
            return terms.All(t => haystack.Contains(t));
        }

        private IEnumerable<SelectionItem> VisibleItems
        {
            get { return _view.Cast<SelectionItem>(); }
        }

        private void UpdateCount()
        {
            int total = _items.Count;
            int selected = _items.Count(i => i.IsChecked);
            int elements = _items.Where(i => i.IsChecked).Sum(i => Math.Max(1, i.ElementIds.Count));
            CountText.Text = selected + " of " + total + " item(s) checked (" + elements + " element(s))";
            OkButton.IsEnabled = selected > 0;
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            _view?.Refresh();
        }

        private void HeaderCheck_Click(object sender, RoutedEventArgs e)
        {
            bool check = (sender as CheckBox)?.IsChecked == true;
            foreach (SelectionItem item in VisibleItems.ToList()) item.IsChecked = check;
        }

        private void CheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectionItem item in VisibleItems.ToList()) item.IsChecked = true;
        }

        private void UncheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectionItem item in _items) item.IsChecked = false;
        }

        private void CheckRecommended_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectionItem item in _items) item.IsChecked = item.Recommended;
        }

        private void ItemsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Space toggles every highlighted row, which is handy for multi-selecting with Shift/Ctrl.
            if (e.Key != Key.Space) return;
            List<SelectionItem> selected = ItemsGrid.SelectedItems.Cast<SelectionItem>().ToList();
            if (selected.Count == 0) return;
            bool target = !selected.All(i => i.IsChecked);
            foreach (SelectionItem item in selected) item.IsChecked = target;
            e.Handled = true;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
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
