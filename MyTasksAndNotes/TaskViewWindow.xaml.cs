using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;

using MyTasksAndNotes;

namespace RichTextEditor
{
    public partial class TaskViewWindow : Window
    {

        private DispatcherTimer _typingTimer;
        private TimeSpan _typingDelay = TimeSpan.FromSeconds(5);

        TaskViewWindowController taskViewWindowController;

        public TaskViewWindow()
        {
            InitializeComponent();
            // handle pasting, just data content, files must be handeled via capturing ctrl + v
            System.Windows.DataObject.AddPastingHandler(editor, OnPasting);

            // handles capturing of  ctrl + v
            editor.PreviewKeyDown += Editor_PreviewKeyDown;


            // timer used for autosaving
            _typingTimer = new DispatcherTimer
            {
                Interval = _typingDelay
            };
            _typingTimer.Tick += TypingTimer_Tick;


            // handle text change in editor
            editor.AddHandler(RichTextBox.TextChangedEvent, new TextChangedEventHandler(RichTextBox_TextChanged));

            
            taskViewWindowController = new TaskViewWindowController(editor);

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
                        if (taskViewWindowController.IsImageFile(filePath))
                        {
                            taskViewWindowController.InsertImage(filePath);
                            e.Handled = true; // Prevent further handling
                        }
                    }
                }
                // redundant... image data is handled through pasting callback
                /*
                else if (Clipboard.ContainsImage())
                {
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                       taskViewWindowController.InsertBitmap(bitmap);
                       e.Handled = true;
                    }
                }
                */
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


        // only hits for "not File" clipboard content: text and images copied directly as "screenshot"
        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            var data = e.DataObject;

            // Case 1: Pasting image files
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    taskViewWindowController.addFile(file);
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
                    taskViewWindowController.InsertBitmap(bitmap);
                    e.CancelCommand(); // Prevent default paste
                }
            }

            // Else: let default paste happen (text, etc.)
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
            // save tasks?!
        }

      

    }
}

