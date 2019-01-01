using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ImageLang
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowModel _model;

        public MainWindow()
        {
            InitializeComponent();

            _model = new MainWindowModel();

            DataContext = _model;
        }

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == true)
            {
                using (var stream = dialog.OpenFile())
                {
                    _model.Load(stream);
                }
            }
        }

        private void RenderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var oldCursor = Cursor;
            Cursor = Cursors.Wait;
            try
            {
                _model.Render();
            }
            finally
            {
                Cursor = oldCursor;
            }
        }
    }
}
