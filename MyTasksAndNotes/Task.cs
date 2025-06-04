using MyTasksAndNotes.TaskDataItem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Formats.Asn1;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace MyTasksAndNotes
{
    class TaskContainer 
    {
        string path = "tasks";
        public Task testTask;
        public TaskContainer()
        {
            //if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                testTask = new Task(path, "testTask");
            }

        }

       
    }
    class Task
    {
        [JsonProperty] public List<TaskDataItem.TaskDataItem> taskDataItems = new List<TaskDataItem.TaskDataItem>();
        [JsonProperty] uint uid;
        [JsonProperty] string name;
        static uint numberOfTasks;
        string folderPath;
        string dataFilePath;

        public Task() { }
        public Task(string baseDirectory, string _name) 
        {
            name = _name;
            uid = getUid();
            folderPath = Path.Combine(baseDirectory, name + "_" + uid);
            dataFilePath = Path.Combine(folderPath, "data.json");

            
            if (!Directory.Exists(folderPath))
            {
                // create
                Directory.CreateDirectory(folderPath);
                System.IO.File.Create(dataFilePath);
            }
            else 
            {
                // read existing
                var ttask = new Task();
                ttask = DeserializeTask();
                taskDataItems = ttask.taskDataItems;
            }

        }

        public bool addStringItem(string item) 
        {
            bool retval = false;
            if (IsUrl(item)) 
            {
                taskDataItems.Add(new TaskDataItem.Link(item));
                retval = true;

            }
            else
            {
                taskDataItems.Add(new TaskDataItem.Text(item));
            }
            SerializeTask();
            return retval;


        }

        public void addImageItem(System.Drawing.Image item)
        {
            taskDataItems.Add(new TaskDataItem.Image(folderPath, item));
            SerializeTask();
        }

        public void addFileItem(string item)
        {
            taskDataItems.Add(new TaskDataItem.File(item));
            SerializeTask();

        }

        private uint getUid()
        {
            numberOfTasks++;
            return numberOfTasks;
        }

        public bool IsUrl(string input)
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

        void SerializeTask()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(this, settings);
            System.IO.File.Delete(dataFilePath);
            System.IO.File.WriteAllText(dataFilePath, json);
        }

        // Static method: load task from file
        Task DeserializeTask()
        {

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            var json = System.IO.File.ReadAllText(dataFilePath);
            return JsonConvert.DeserializeObject<Task>(json,settings);
        }
    }



    namespace TaskDataItem
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class TaskDataItem
        {
            //[JsonProperty] public DateTime TimeStamp { get; set; } = DateTime.Now;
            //[JsonProperty] public string Value { get; set; }
        }

        public class Text : TaskDataItem
        {
            [JsonProperty] public string TextValue { get; set; }

            public Text() { }

            public Text(string text)
            {
                TextValue = text;
            }
        }

        public class Image : TaskDataItem
        {
            [JsonProperty] public string Path { get; set; }
           

            public Image() { }

            public Image(string basePath, System.Drawing.Image image)
            {
                string folder = "images";
                Directory.CreateDirectory(System.IO.Path.Combine(basePath, folder));

                string fileName = $"image_{Guid.NewGuid()}.png";
                Path = System.IO.Path.Combine(basePath, folder, fileName);
                image.Save(Path);
            }
        }

        public class File : TaskDataItem
        {
            [JsonProperty] public string Path { get; set; }

            public File() { }

            public File(string path)
            {
                Path = path;
                //Path = "";
            }
        }

        public class Link : TaskDataItem
        {
            [JsonProperty] public string Url { get; set; }

            public Link() { }

            public Link(string url)
            {
                Url = url;
            }
        }

        public class TaskDataItemConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(TaskDataItem);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                var type = jo["$type"]?.ToString();

                if (type == null)
                    throw new JsonSerializationException("Missing $type for polymorphic deserialization.");

                Type targetType = Type.GetType(type);
                if (targetType == null)
                    throw new JsonSerializationException($"Unknown type: {type}");

                return jo.ToObject(targetType, serializer);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                JObject jo = JObject.FromObject(value, serializer);
                jo.AddFirst(new JProperty("$type", value.GetType().AssemblyQualifiedName));
                jo.WriteTo(writer);
            }
        }

    }
}
