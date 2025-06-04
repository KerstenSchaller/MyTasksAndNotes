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
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using Image = System.Windows.Controls.Image;

namespace RichTextEditor
{
    public partial class TaskViewWindow : Window
    {

        private DispatcherTimer _typingTimer;
        private TimeSpan _typingDelay = TimeSpan.FromSeconds(5);

        TaskViewWindowController taskViewWindowController;

        private bool suppressTextChanged = false;

        private string typedText = "";

        Paragraph currentParagraph;

        public TaskViewWindow()
        {
            InitializeComponent();
            // handle pasting, just data content, files must be handeled via capturing ctrl + v
            System.Windows.DataObject.AddPastingHandler(editor, OnPasting);

            // handles capturing of  ctrl + v
            editor.PreviewKeyDown += Editor_PreviewKeyDown;
            SpellCheck.SetIsEnabled(editor, false);





            taskViewWindowController = new TaskViewWindowController(editor, this);

            this.StateChanged += Window_StateChanged;
            this.Closing += MainWindow_Closing;

            editor.SelectionChanged += Editor_SelectionChanged;

            currentParagraph = GetCurrentParagraph();

        }


        private void saveTypedText() 
        {
            var para =  GetCurrentParagraph();
            string text = new TextRange(para.ContentStart, para.ContentEnd).Text;
            if (text == "") return;
            taskViewWindowController.addTextToTask(text);
            GetCurrentParagraph().Inlines.Add(new LineBreak());
            editor.Document.Blocks.Add(new Paragraph());
            

        }


        // used to capture ctrl + v events where the clipboard content is a file
        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            suppressTextChanged = true;
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                saveTypedText();
                if (Clipboard.ContainsFileDropList())
                {
                    var fileList = Clipboard.GetFileDropList();
                    foreach (string filePath in fileList)
                    {
                        if (taskViewWindowController.IsImageFile(filePath))
                        {
                            taskViewWindowController.InsertNewImage(filePath);
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
            suppressTextChanged = false;
        }

        private void InsertLink_Click(object sender, RoutedEventArgs e)
        {
            saveTypedText();
            suppressTextChanged = true;
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
            suppressTextChanged = false;
        }

        TextRange selectedRange = null;
        private void HighlightSelectedText(RichTextBox richTextBox)
        {
            if(selectedRange != null) selectedRange.ApplyPropertyValue(TextElement.BackgroundProperty, System.Windows.Media.Brushes.White);
            var para = richTextBox.CaretPosition.Paragraph;
            selectedRange = new TextRange(para.ContentStart, para.ContentEnd);
            selectedRange.ApplyPropertyValue(TextElement.BackgroundProperty, System.Windows.Media.Brushes.LightGray);
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            saveTypedText();
            suppressTextChanged = true;
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
            suppressTextChanged = false;
        }


        // only hits for "not File" clipboard content: text and images copied directly as "screenshot"
        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            saveTypedText();
            suppressTextChanged = true;
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
                    return;
                }
            }

            var text = Clipboard.GetText();
            taskViewWindowController.addNewText(text);
            e.CancelCommand(); // Prevent default paste
            return;

            // Else: let default paste happen (text, etc.)
            suppressTextChanged = false;
        }



        Block currentBlock;
        private Paragraph GetCurrentParagraph()
        {
            TextPointer caret = editor.CaretPosition;
            return caret.Paragraph;
        }
        private Block GetCurrentBlock()
        {
            TextPointer caret = editor.CaretPosition;
            return caret.Paragraph ?? caret.Parent as Block;
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Block newBlock = GetCurrentBlock();

            if (newBlock != null && newBlock != currentBlock)
            {
                currentBlock = newBlock;


                var para = currentParagraph;
                string text = new TextRange(para.ContentStart, para.ContentEnd).Text;
                if (text == "") return;
                if(para.Tag != null)
                {
                    taskViewWindowController.updateText((int)para.Tag, text);
                }
                else
                {
                    taskViewWindowController.addTextToTask(text);
                }
                HighlightSelectedText(editor);
                currentParagraph = GetCurrentParagraph();

                MessageBox.Show(text);
            }
        }





        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                saveTypedText();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Fires before the window is closed.
            saveTypedText();
        }
    }
}

