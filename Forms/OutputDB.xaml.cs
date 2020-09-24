using BatchModelCheck.DB;
using LiveCharts;
using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interop;

namespace BatchModelCheck.Forms
{
    /// <summary>
    /// Логика взаимодействия для OutputDB.xaml
    /// </summary>
    public partial class OutputDB : Window
    {
        public ObservableCollection<DbTypeGraph> Data = new ObservableCollection<DbTypeGraph>();
        private ChartValues<DateTimePoint> GetData(List<DbRowData> data, string name)
        {
            var values = new ChartValues<DateTimePoint>();
            foreach (DbRowData row in data)
            {
                foreach (DbError err in row.Errors)
                {
                    if (err.Name == name)
                    {
                        values.Add(new DateTimePoint(row.DateTime, err.Count));
                    }
                }
            }
            if (values.Count == 1)
            {
                values.Add(new DateTimePoint(DateTime.Now, values[0].Value));
            }
            return values;
        }
        public string ProjectName { get; set; }
        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public void Update()
        {
            foreach (DbTypeGraph type in Data)
            {
                if (type.IsChecked)
                {
                    if (!SeriesCollection.Contains(type.Line))
                    {
                        SeriesCollection.Add(type.Line);
                    }
                }
                else
                {
                    if (SeriesCollection.Contains(type.Line))
                    {
                        SeriesCollection.Remove(type.Line);
                    }
                }
            }
        }
        public OutputDB(string projectName, List<DbRowData> data)
        {
#if Revit2020
            Owner = ModuleData.RevitWindow;
#endif
#if Revit2018
            WindowInteropHelper helper = new WindowInteropHelper(this);
            helper.Owner = ModuleData.MainWindowHandle;
#endif
            ProjectName = projectName;
            InitializeComponent();
            HashSet<string> uniq = new HashSet<string>();
            foreach (DbRowData row in data)
            {
                foreach (DbError err in row.Errors)
                {
                    uniq.Add(err.Name);
                }
            }
            SeriesCollection = new SeriesCollection();
            foreach (string type in uniq)
            {
                Data.Add(new DbTypeGraph(GetData(data, type), type));
            }
            Update();
            icTypes.ItemsSource = Data;
            XFormatter = val => new DateTime((long)val).ToString("dd MMM");
            YFormatter = val => Math.Round(val, 0).ToString();
            DataContext = this;
        }
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            Update();
        }
    }

}
