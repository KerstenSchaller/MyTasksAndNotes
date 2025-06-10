using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MyTasksAndNotes
{
    class NoteContainer
    {
        string path = "Notes";
        string globalPath = "";
        string baseName = "testNote";
        List<Note> notes = new List<Note>();
        List<Note> tasks = new List<Note>();
        int highestIndex;

        private static readonly Lazy<NoteContainer> _instance = new Lazy<NoteContainer>(() => new NoteContainer());
        public static NoteContainer Instance => _instance.Value;

        Note lastNote;
        Note lastTask;


        private NoteContainer()
        {
            var gPath = App.GlobalOptions.NoteStoragePath;
            globalPath = path;
            if(gPath != "") globalPath = Path.Combine(gPath, path);
            Directory.CreateDirectory(globalPath);
                
            highestIndex = ProcessSubfolders(globalPath, baseName);
        }

        public int ProcessSubfolders(string rootFolder, string prefix)
        {
            if (!Directory.Exists(rootFolder))
            {
                Console.WriteLine($"Directory '{rootFolder}' does not exist.");
                throw new Exception();
            }


            string[] subfolders = Directory.GetDirectories(rootFolder);
            Regex pattern = new Regex($"^{Regex.Escape(prefix)}(\\d+)$");

            int maxNumber = 0;

            foreach (var folder in subfolders)
            {
                string folderName = Path.GetFileName(folder);
                Match match = pattern.Match(folderName);

                if (match.Success)
                {
                    int number = int.Parse(match.Groups[1].Value);
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }

                    var tnote = new Note(rootFolder, prefix, (int)number); // just create to read task status
                    if (tnote.isTask)
                    {
                        lastTask = addTask(rootFolder, prefix, (int)number); // Call the provided action for each matching subfolder
                    }
                    else
                    {
                        lastNote = addNote(rootFolder, prefix, (int)number); // Call the provided action for each matching subfolder
                    }
                }
            }
            return maxNumber;
        }

        public List<Note> getNotes() 
        {
            return notes;
        }

        public List<Note> getTasks()
        {
            return tasks;
        }

        Note addNote(string rootFolder, string name, int uid)
        {
            Note note = new Note(rootFolder, name, uid);
            notes.Add(note);
            return note;
        }
        public Note addNewNote() 
        {
            highestIndex++;
            Note note = new Note(globalPath, baseName, highestIndex);
            notes.Add(note);
            return note;
        }

        Note addTask(string rootFolder, string name, int uid)
        {
            Note task = new Note(rootFolder, name, uid, true);
            tasks.Add(task);
            return task;
        }
        public Note addNewTask()
        {
            highestIndex++;
            Note task = new Note(globalPath, baseName, highestIndex, true);
            tasks.Add(task);
            return task;
        }

        public Note getLastNote()
        {
            return lastNote;
        }

        public Note getLastTask()
        {
            return lastTask;
        }
    }

        public enum TaskState { Todo,Done }
        public class Note
        {
            [JsonProperty] public Dictionary<int, NoteDataItem.NoteDataItem> noteDataItems = new Dictionary<int, NoteDataItem.NoteDataItem>();
            [JsonProperty] int uid;
            [JsonProperty] public string name="unnamed Note";

            [JsonProperty] public bool isTask = false;
            [JsonProperty] public TaskState taskState = TaskState.Todo;
            [JsonProperty] public DateTime creationTimestamp;
            [JsonProperty] public DateTime closingTimestamp;

            static uint numberOfNotes;
            string folderPath;
            string dataFilePath;



            public Note() { }
            public Note(string baseDirectory, string _name, int _uid, bool _isTask = false)
            {
                uid = _uid;
                folderPath = Path.Combine(baseDirectory, _name + uid);
                dataFilePath = Path.Combine(folderPath, "data.json");
                isTask = _isTask;
                creationTimestamp = DateTime.Now;

                if (name == "unnamed Note") name = name + uid;


                if (!Directory.Exists(folderPath) )
                {
                    // create
                    Directory.CreateDirectory(folderPath);
                    SerializeNote();
                }
                else
                {
                    // read existing
                    var tNote = DeserializeNote();
                    isTask = tNote.isTask;
                    name = tNote.name;
                    taskState = tNote.taskState;
                    creationTimestamp = tNote.creationTimestamp;
                    closingTimestamp = tNote.closingTimestamp;
                    noteDataItems = tNote.noteDataItems;
                }

            }

            public void setTaskDone() 
            {
                if (isTask) 
                {
                    taskState = TaskState.Done;
                    closingTimestamp = DateTime.Now;
                    SerializeNote();
                }
            }

            public bool addStringItem(string item)
            {
                bool retval = false;
                if (IsUrl(item))
                {
                    noteDataItems.Add(noteDataItems.Count, new NoteDataItem.Link(item));
                    retval = true;

                }
                else
                {
                    noteDataItems.Add(noteDataItems.Count, new NoteDataItem.Text(item));
                }
                if (name == ("unnamed Note" + uid)) name = item;// set name from first text item
                SerializeNote();

                return retval;


            }

            public void addImageItem(System.Drawing.Image item)
            {
                noteDataItems.Add(noteDataItems.Count, new NoteDataItem.Image(folderPath, item));
                SerializeNote();
            }

            public void addFileItem(string item)
            {
                noteDataItems.Add(noteDataItems.Count, new NoteDataItem.File(item));
                SerializeNote();

            }

            private uint getUid()
            {
                numberOfNotes++;
                return numberOfNotes;
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

            public void SerializeNote()
            {
                // set name from first text Item,
                foreach(var p in noteDataItems)
                {
                    if(p.Value is NoteDataItem.Text ) 
                    {
                        string tName = ((NoteDataItem.Text)(p.Value)).TextValue;
                        name = tName;
                        break;
                    }
                }
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented
                };
                var json = JsonConvert.SerializeObject(this, settings);
                System.IO.File.Delete(dataFilePath);
                System.IO.File.WriteAllText(dataFilePath, json);
            }

            // Static method: load Note from file
            public Note DeserializeNote()
            {

                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented
                };
                var json = System.IO.File.ReadAllText(dataFilePath);
                if (json == "") json = "{}";
                var retval = JsonConvert.DeserializeObject<Note>(json, settings);
                return retval;
            }
        }





    namespace NoteDataItem
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class NoteDataItem
        {
            [JsonProperty] public DateTime TimeStamp { get; set; } = DateTime.Now;

            public NoteDataItem() 
            {
                TimeStamp = DateTime.Now;
            }
        }

        public class Text : NoteDataItem
        {
            [JsonProperty] public string TextValue { get; set; }

            public Text() { }

            public Text(string text)
            {
                TextValue = text;
            }
        }

        public class Image : NoteDataItem
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

        public class File : NoteDataItem
        {
            [JsonProperty] public string Path { get; set; }

            public File() { }

            public File(string path)
            {
                Path = path;
            }
        }

        public class Link : NoteDataItem
        {
            [JsonProperty] public string Url { get; set; }

            public Link() { }

            public Link(string url)
            {
                Url = url;
            }
        }

        public class NoteDataItemConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(NoteDataItem);

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
