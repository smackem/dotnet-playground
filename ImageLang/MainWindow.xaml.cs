using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
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

            textEditor.Text = _model.Source;
            textEditor.Options.ConvertTabsToSpaces = true;
            textEditor.Options.HideCursorWhileTyping = true;
            textEditor.Options.HighlightCurrentLine = true;
            textEditor.Options.IndentationSize = 4;
            textEditor.Options.ShowSpaces = true;
            textEditor.Options.ShowTabs = true;
            textEditor.Options.ShowColumnRuler = true;

            IHighlightingDefinition customHighlighting;
            using (Stream s = GetType().Assembly.GetManifestResourceStream("ImageLang.ImageLang.xshd"))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource");

                using (var reader = new XmlTextReader(s))
                {
                    customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            HighlightingManager.Instance.RegisterHighlighting("ImageLang", new string[] { ".cool" }, customHighlighting);
            textEditor.SyntaxHighlighting = customHighlighting;
        }

        void OpenButton_OnClick(object sender, RoutedEventArgs e)
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

        void RenderButton_OnClick(object sender, RoutedEventArgs e)
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

        void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter
            && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                RenderButton_OnClick(sender, e);
            }
        }

        void TextEditor_TextChanged(object sender, EventArgs e)
        {
            _model.Source = textEditor.Text;
        }
    }
}
