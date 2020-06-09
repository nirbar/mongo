using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MongoDB.Bootstrapper.BA.View
{
    public partial class EulaView : UserControl
    {
        public EulaView()
        {
            InitializeComponent();
        }

        private void RichTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is RichTextBox richTextBox))
            {
                return;
            }

            byte[] documentBytes = Encoding.UTF8.GetBytes(Properties.Resources.Eula);
            using (MemoryStream reader = new MemoryStream(documentBytes))
            {
                reader.Position = 0;
                TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                textRange.Load(reader, DataFormats.Rtf);
            }
        }
    }
}
