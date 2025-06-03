using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace RichTextEditor
{
    public partial class TaskViewWindow : Window
    {

        private DispatcherTimer _typingTimer;
        private TimeSpan _typingDelay = TimeSpan.FromSeconds(5);
        public TaskViewWindow()
        {
            InitializeComponent();
            System.Windows.DataObject.AddPastingHandler(editor, OnPasting);
            editor.PreviewKeyDown += Editor_PreviewKeyDown;

            _typingTimer = new DispatcherTimer
            {
                Interval = _typingDelay
            };
            _typingTimer.Tick += TypingTimer_Tick;

            editor.AddHandler(RichTextBox.TextChangedEvent, new TextChangedEventHandler(RichTextBox_TextChanged));

        }


        // used to capture ctrl + v events where the clipboard content is a file
        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Clipboard.ContainsFileDropList())
                {
                    var fileList = Clipboard.GetFileDropList();
                    foreach (string filePath in fileList)
                    {
                        if (IsImageFile(filePath))
                        {
                            InsertImage(filePath);
                            e.Handled = true; // Prevent further handling
                        }
                    }
                }
                else if (Clipboard.ContainsImage())
                {
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        InsertBitmap(bitmap);
                        e.Handled = true;
                    }
                }
            }
        }

        private void InsertLink_Click(object sender, RoutedEventArgs e)
        {
            var hyperlink = new Hyperlink(new Run("OpenAI"))
            {
                NavigateUri = new Uri("https://www.openai.com")
            };
            hyperlink.RequestNavigate += (s, ev) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ev.Uri.AbsoluteUri) { UseShellExecute = true });
                ev.Handled = true;
            };

            editor.CaretPosition.InsertTextInRun(" ");
            editor.CaretPosition.Paragraph?.Inlines.Add(hyperlink);
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFile.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFile.FileName));
                Image img = new Image
                {
                    Source = bitmap,
                    Width = 200
                };

                InlineUIContainer container = new InlineUIContainer(img, editor.CaretPosition);
            }
        }


        // only hits for not File clipboard content: text and images copied directly as "screenshot"
        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            var data = e.DataObject;

            // Case 1: Pasting image files
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (IsImageFile(file))
                    {
                        InsertImage(file);
                    }
                }

                e.CancelCommand(); // Prevent default paste
                return;
            }

            // Case 2: Bitmap content (from clipboard)
            if (Clipboard.ContainsImage())
            {
                var bitmap = Clipboard.GetImage();
                if (bitmap != null)
                {
                    InsertBitmap(bitmap);
                    e.CancelCommand(); // Prevent default paste
                }
            }

            // Else: let default paste happen (text, etc.)
        }

        private void InsertBitmap(BitmapSource bitmap)
        {
            Image image = new Image
            {
                Source = bitmap,
                Width = 200,
                Stretch = Stretch.Uniform
            };

            editor.CaretPosition.InsertTextInRun(" ");
            editor.CaretPosition.Paragraph?.Inlines.Add(new InlineUIContainer(image));
        }

        private bool IsImageFile(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath)?.ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }

        private void InsertImage(string filePath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage(new Uri(filePath));
                Image image = new Image
                {
                    Source = bitmap,
                    Width = 200
                };

                editor.CaretPosition.InsertTextInRun(" ");
                editor.CaretPosition.Paragraph?.Inlines.Add(new InlineUIContainer(image));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting image: " + ex.Message);
            }
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Restart the timer on each text change
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            SaveContent();
        }

        private void SaveContent()
        {
            //string text = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text;
            var items = ExtractTextAndImageMarkers(editor);
            // Your save logic here (to file, database, etc.)
            //Console.WriteLine("Auto-saved: " + text.Trim());
        }

        List<string> ExtractTextAndImageMarkers(RichTextBox richTextBox)
        {
            List<string> result = new List<string>();
            TextPointer pointer = richTextBox.Document.ContentStart;
            StringBuilder currentText = new StringBuilder();

            while (pointer != null && pointer.CompareTo(richTextBox.Document.ContentEnd) < 0)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    currentText.Append(textRun);
                    pointer = pointer.GetPositionAtOffset(textRun.Length);
                }
                else if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                {
                    object element = pointer.GetAdjacentElement(LogicalDirection.Forward);
                    if (element is InlineUIContainer || element is BlockUIContainer)
                    {
                        // Save current accumulated text if any
                        if (currentText.Length > 0)
                        {
                            result.Add(currentText.ToString());
                            currentText.Clear();
                        }

                        // Add marker for embedded object (image or file)
                        result.Add("[EmbeddedObject]");
                    }
                    pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                }
                else
                {
                    pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            // Add any remaining text
            if (currentText.Length > 0)
            {
                result.Add(currentText.ToString());
            }

            return result;
        }

    }
}

