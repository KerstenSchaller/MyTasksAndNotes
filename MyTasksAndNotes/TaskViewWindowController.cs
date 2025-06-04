using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyTasksAndNotes
{
    public class TaskViewWindowController
    {

        RichTextBox editor;
        Task task;


        public TaskViewWindowController(RichTextBox _editor) 
        {
            editor = _editor;
        }

        public void addFile(string file) 
        {
            if (IsImageFile(file))
            {
                InsertImage(file);
            }
        }
        public void InsertImage(string filePath)
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

        public void InsertBitmap(BitmapSource bitmap)
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

        public bool IsImageFile(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath)?.ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }
    }
}
