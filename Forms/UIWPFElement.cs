using KPLNDataBase.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BatchModelCheck.Forms
{
    public class UIWPFElement : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public DbElement Element { get; set; }
        public static ObservableCollection<UIWPFElement> GetCollection(ObservableCollection<DbDocument> elements)
        {
            ObservableCollection<UIWPFElement> collection = new ObservableCollection<UIWPFElement>();
            foreach (DbElement el in elements)
            { collection.Add(new UIWPFElement(el)); }
            return collection;
        }
        public static ObservableCollection<UIWPFElement> GetCollection(ObservableCollection<DbProject> elements)
        {
            ObservableCollection<UIWPFElement> collection = new ObservableCollection<UIWPFElement>();
            foreach (DbElement el in elements)
            { collection.Add(new UIWPFElement(el)); }
            return collection;
        }
        public static ObservableCollection<UIWPFElement> GetCollection(ObservableCollection<DbSubDepartment> elements)
        {
            ObservableCollection<UIWPFElement> collection = new ObservableCollection<UIWPFElement>();
            foreach (DbElement el in elements)
            { collection.Add(new UIWPFElement(el)); }
            return collection;
        }
        private bool _isChecked { get; set; }
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                NotifyPropertyChanged();
            }
        }
        public UIWPFElement(DbElement element)
        {
            Id = element.Id;
            Element = element;
            _isChecked = false;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
