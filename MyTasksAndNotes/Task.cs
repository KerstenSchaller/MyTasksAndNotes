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
        int highestIndex;

        private static readonly Lazy<NoteContainer> _instance = new Lazy<NoteContainer>(() => new NoteContainer());
        public static NoteContainer Instance => _instance.Value;

        Note lastNote;


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

                    lastNote = addNote(rootFolder, prefix, (int)number); // Call the provided action for each matching subfolder
                }
            }
            return maxNumber;
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
            //Note note = new Note(globalPath, baseName, highestIndex);
            //notes.Add(note);
            Task task = new Task(globalPath, baseName, highestIndex);
            notes.Add(task);
            return task;
        }

        public Note getLastNote()
        {
            return lastNote;
        }
    }
        public class Note
        {
            //[JsonProperty] public List<NoteDataItem.NoteDataItem> NoteDataItems = new List<NoteDataItem.NoteDataItem>();
            [JsonProperty] public Dictionary<int, NoteDataItem.NoteDataItem> noteDataItems = new Dictionary<int, NoteDataItem.NoteDataItem>();
            [JsonProperty] int uid;
            [JsonProperty] string name;
            static uint numberOfNotes;
            string folderPath;
            string dataFilePath;


            public Note() { }
            public Note(string baseDirectory, string _name, int _uid)
            {
                name = _name;
                uid = _uid;
                folderPath = Path.Combine(baseDirectory, name + uid);
                dataFilePath = Path.Combine(folderPath, "data.json");


                if (!Directory.Exists(folderPath))
                {
                    // create
                    Directory.CreateDirectory(folderPath);
                }
                else
                {
                    // read existing
                    var tNote = DeserializeNote();
                    noteDataItems = tNote.noteDataItems;
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
                return JsonConvert.DeserializeObject<Note>(json, settings);
            }
        }

    public class Task : Note
    {
        [JsonProperty] const bool isTask = true;
        [JsonProperty] string testValue = "blabla";

        public Task(string baseDirectory, string _name, int _uid) : base(baseDirectory, _name, _uid) { }

    }



    namespace NoteDataItem
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class NoteDataItem
        {
            //[JsonProperty] public DateTime TimeStamp { get; set; } = DateTime.Now;
            //[JsonProperty] public string Value { get; set; }
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
                //Path = "";
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
