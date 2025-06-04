using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;


namespace MyTasksAndNotes
{
    class TaskContainer 
    {
        string path = "tasks";
        public TaskContainer()
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

        }

       
    }
    class Task
    {
        List<TaskDataItem.TaskDataItem> taskDataItems = new List<TaskDataItem.TaskDataItem>();
        uint uid;
        static uint numberOfTasks; 
        public Task(string name) 
        {
            
            uid = getUid();
            var folderPath = name + "_" + uid;
            // create task dir
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

        }

        public void addStringItem(string item) 
        {
            if (IsUrl(item)) 
            {
                taskDataItems.Add(new TaskDataItem.Link(item));
            }
            else
            {
                taskDataItems.Add(new TaskDataItem.Text(item));
            }

        }

        public void addImageItem(Image item)
        {
            taskDataItems.Add(new TaskDataItem.Image(item));
        }

        public void addFileItem(string item)
        {
            taskDataItems.Add(new TaskDataItem.File(item));
        }

        private uint getUid()
        {
            numberOfTasks++;
            return numberOfTasks;
        }

        private bool IsUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Check if it's a valid URL
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFtp))
            {
                return true;
            }

            return false;
        }
    }



    namespace TaskDataItem {

        class TaskDataItem
        {
            DateTime timeStamp;
            string value;
            public TaskDataItem()
            {
            }
        }

        class Text : TaskDataItem
        {
            string text;
            public Text(string _text)
            {
                text = _text;
            }
        }

        class Image : TaskDataItem
        {
            string path;
            public Image(System.Drawing.Image image)
            {
                // save image and store path
            }
        }

        class File : TaskDataItem
        {
            string path;
            public File(string _path)
            {
                path = _path;
            }
        }

        class Link : TaskDataItem
        {
            string url;
            public Link(string _url)
            {
                url = _url;
            }
        }




    }
}
