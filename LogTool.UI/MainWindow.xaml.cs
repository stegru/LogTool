using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using LogTool.Annotations;
using LogTool.LogProcessor;
using LogTool.LogProcessor.Parser;
using Microsoft.Win32;

namespace LogTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ParsedLog _selectedLog;
        private Account _selectedAccount;
        private Calendar _selectedCalendar;

        public ParsedLog SelectedLog
        {
            get => this._selectedLog;
            set
            {
                this._selectedLog = value;
                this.OnPropertyChanged();
            }
        }

        public Account SelectedAccount
        {
            get => this._selectedAccount;
            set
            {
                this._selectedAccount = value;
                this.OnPropertyChanged();
            }
        }

        public Calendar SelectedCalendar
        {
            get => this._selectedCalendar;
            set
            {
                this._selectedCalendar = value;
                this.OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        public ObservableCollection<ParsedLog> ParsedLogs { get; set; } = new ObservableCollection<ParsedLog>();
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                OverwritePrompt = true,
                AddExtension = true,
                FileName = this.SelectedLog.LogFile + ".json",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() == true)
            {
                this.SelectedLog.ExportJson(dialog.FileName);
            }
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                IEnumerable<ParsedLog> logs = Array.Empty<ParsedLog>();

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    logs = logs.Concat(LogParser.ProcessPath(file));
                }

                List<ParsedLog> list = logs.ToList();
                list.ForEach(this.ParsedLogs.Add);

                if (list.Count > 0)
                {
                    this.SelectedLog = list.First();
                }

                if (this.ParsedLogs.Count > 0)
                {
                    this.EmptyPrompt.Visibility = Visibility.Collapsed;
                    this.LogPanel.Visibility = Visibility.Visible;
                }
            }
        }
    }
}