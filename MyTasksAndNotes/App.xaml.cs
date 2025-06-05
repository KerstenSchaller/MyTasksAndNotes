using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MyTasksAndNotes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Options GlobalOptions { get; private set; } = new Options();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadOptions();
        }

        public static void LoadOptions()
        {
            if (File.Exists("options.json"))
            {
                var json = File.ReadAllText("options.json");
                GlobalOptions = JsonSerializer.Deserialize<Options>(json) ?? new Options();
            }
        }

        public static void SaveOptions()
        {
            var json = JsonSerializer.Serialize(GlobalOptions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("options.json", json);
        }
    }
}
