using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace RevitModelSlimmer.UI
{
    /// <summary>Row view-model for the generic check-list window.</summary>
    public sealed class SelectionItem : INotifyPropertyChanged
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

        /// <summary>Display name, e.g. family name or view name.</summary>
        public string Name { get; set; }

        /// <summary>Category / kind / audit check name.</summary>
        public string Group { get; set; }

        /// <summary>Free text with counts and hints.</summary>
        public string Details { get; set; }

        /// <summary>Short status such as "Unused (verified)" or "High".</summary>
        public string Status { get; set; }

        /// <summary>Whether the tool recommends deleting this item. Recommended items start checked.</summary>
        public bool Recommended { get; set; }

        /// <summary>Sort key used to order rows (higher first).</summary>
        public int Weight { get; set; }

        /// <summary>Element(s) to delete when the row is checked. Usually one id.</summary>
        public List<ElementId> ElementIds { get; } = new List<ElementId>();

        /// <summary>Arbitrary payload for the caller.</summary>
        public object Tag { get; set; }

        public string SearchText
        {
            get { return (Name + " " + Group + " " + Details + " " + Status).ToLowerInvariant(); }
        }
    }
}
