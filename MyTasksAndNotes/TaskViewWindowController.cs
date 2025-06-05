using System;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyTasksAndNotes
{
    public class TaskViewWindowController
    {

        System.Windows.Controls.RichTextBox editor;
        NoteContainer noteContainer;
        Note note;
        RichTextEditor.TaskViewWindow taskViewWindow;


        public TaskViewWindowController(System.Windows.Controls.RichTextBox _editor, RichTextEditor.TaskViewWindow _taskViewWindow, Note _note) 
        {
            editor = _editor;
            taskViewWindow = _taskViewWindow;
            
            note = _note;

            ProcessTaskDataItems(note.noteDataItems);

            var hyperlink = new Hyperlink(new Run("OpenAI"))
            {
                NavigateUri = new Uri("https://www.openai.com")
            };
            hyperlink.RequestNavigate += (s, ev) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ev.Uri.AbsoluteUri) { UseShellExecute = true });
                ev.Handled = true;
            };



        }

        public void addFile(string file) 
        {
            if (IsImageFile(file))
            {
                InsertImage(file);
            }
        }

        public void addNewText(string text)
        {
            bool isLink = note.addStringItem(text);
            if (isLink) 
            {
                addLink(text);
            }
            else
            { 
                addText(text);
            }
        }

        private void addText(string text, int? tag = null )
        {
            Paragraph newParagraph = new Paragraph(new Run(text));
            newParagraph.Tag = tag;
            editor.Document.Blocks.Add(newParagraph);

            editor.CaretPosition = newParagraph.ContentEnd;
            //editor.CaretPosition.InsertLineBreak();
        }

        public void addTextToTask(string text) 
        {
            note.addStringItem(text);
        }

        public void addNewLink(string url) 
        {
            note.addStringItem(url);
            addLink(url);
        }


        private void addLink(string url, int? tag = null)
        {
            var hyperlink = new Hyperlink(new Run(url))
            {
                NavigateUri = new Uri(url)
            };
            hyperlink.RequestNavigate += (s, ev) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ev.Uri.AbsoluteUri) { UseShellExecute = true });
                ev.Handled = true;
            };
            editor.CaretPosition.Paragraph?.Inlines.Add(hyperlink);
            //editor.CaretPosition = editor.CaretPosition.DocumentEnd;
            //editor.CaretPosition.InsertLineBreak();
        }

        public void InsertNewImage(string filePath) 
        {
            //InsertImage(filePath);
            note.addImageItem(InsertImage(filePath));
        }
        private System.Drawing.Image InsertImage(string filePath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage(new Uri(filePath, UriKind.Relative));
                var img = InsertBitmap(bitmap);
                return img;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error inserting image: " + ex.Message);
                return null;
            }
        }

        public System.Drawing.Image InsertBitmap(BitmapSource bitmap)
        {
            Image image = new Image
            {
                Source = bitmap,
                Width = 200,
                Stretch = Stretch.Uniform
            };


            editor.CaretPosition.InsertTextInRun(" ");
            editor.CaretPosition.Paragraph?.Inlines.Add(new InlineUIContainer(image));
            //editor.CaretPosition = editor.CaretPosition.DocumentEnd;
            //editor.CaretPosition.InsertLineBreak();

            return image.toSystemDrawing();
        }

        public bool IsImageFile(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath)?.ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }

        public void ProcessTaskDataItems(System.Collections.Generic.Dictionary<int,NoteDataItem.NoteDataItem> items)
        {
            foreach (var item in items)
            {
                switch (item.Value)
                {
                    case MyTasksAndNotes.NoteDataItem.Text textItem:
                        addText(textItem.TextValue, item.Key);
                        break;
                    case MyTasksAndNotes.NoteDataItem.Image imageItem:
                        InsertImage(imageItem.Path);
                        break;
                    case MyTasksAndNotes.NoteDataItem.File fileItem:
                        //HandleFile(fileItem);
                        break;
                    case MyTasksAndNotes.NoteDataItem.Link linkItem:
                        addLink(linkItem.Url, item.Key);
                        break;
                    default:
                        break;
                }
                addText(taskViewWindow.DelimeterLine);
                editor.Document.Blocks.Add(new Paragraph());
            }
        }

        internal void updateText(int tag, string text)
        {
            var item = (NoteDataItem.Text)note.noteDataItems[tag];
            item.TextValue = text;
            note.SerializeNote();

        }
    }
}
