using Ookii.Dialogs.Wpf;
using PersonalFinanceOCR.TNGeWallet;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace PersonalFinanceOCR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TNGeWalletManager tNGeWalletManager = new TNGeWalletManager();

        public MainWindow()
        {
            InitializeComponent();

            // SET TITLE
            this.Title = $"Personal Finance OCR Utilities ({Assembly.GetExecutingAssembly().GetName().Version})";
            
            // SET DEFAULT DATE
            startDatePicker.SelectedDate = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            endDatePicker.SelectedDate = DateTime.Now;

            // SHOW RESULT ON FIRST START
            searchResult();
        }

        private void importResultPath_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "PDF File (*.pdf)|*.pdf";

            if (!VistaFileDialog.IsVistaFileDialogSupported)
            {
                MessageBox.Show(this, "Because you are not using Windows Vista or later, Faeture not supported!", "Not Supported");
            }
            
            if ((bool)dialog.ShowDialog(this))
            {
                string fileName = dialog.FileName;

                TNGeWalletManager manager = new TNGeWalletManager();
                manager.ProcessStatement(fileName);
                searchResult();
            }
        }

        private void searchResultButton_Click(object sender, RoutedEventArgs e)
        {
            searchResult();
        }

        private void searchResult()
        {
            DateTime startDate = startDatePicker.SelectedDate.Value;
            DateTime endDate = endDatePicker.SelectedDate.Value;

            var resultDict = tNGeWalletManager.SearchResult(startDate, endDate);

            resultListView.ItemsSource = resultDict.Keys;
        }

        private void exportResultButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedDates = resultListView.SelectedItems.Cast<string>()
                                                  .OrderByDescending(str => int.Parse(str.Split('-')[0]))
                                                  .ThenByDescending(str => int.Parse(str.Split('-')[1]))
                                                  .ThenByDescending(str => int.Parse(str.Split('-')[2]))
                                                  .ToList();
                if (selectedDates.Count > 0)
                {
                    VistaSaveFileDialog dialog = new VistaSaveFileDialog();
                    dialog.Filter = "CSV File (*.csv)|*.csv";

                    if (selectedDates.Count == 1)
                    {
                        dialog.FileName = $"{selectedDates.First()}.csv";
                    }
                    else
                    {
                        dialog.FileName = $"{selectedDates.Last()} to {selectedDates.First()}.csv";
                    }

                    if (!VistaFileDialog.IsVistaFileDialogSupported)
                    {
                        MessageBox.Show(this, "Because you are not using Windows Vista or later, Feature not supported!", "Not Supported");
                    }

                    if ((bool)dialog.ShowDialog(this))
                    {
                        string fileName = dialog.FileName;
                        tNGeWalletManager.ExportResult(selectedDates, fileName);
                    }
                }
                else
                {
                    MessageBox.Show("Please select something before export!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
