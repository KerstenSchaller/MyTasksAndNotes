using MyTasksAndNotes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Windows.Forms.Design.AxImporter;

namespace MyTasksAndNotes
{



public partial class OptionsWindow : Window
{
    private Dictionary<string, FrameworkElement> _controls = new();

    public OptionsWindow()
    {
        InitializeComponent();
        GenerateUIFromOptions(App.GlobalOptions);
    }

    private void GenerateUIFromOptions(Options options)
    {
        var properties = typeof(Options).GetProperties();

        foreach (var prop in properties)
        {
            var label = new TextBlock { Text = prop.Name, Margin = new Thickness(0, 5, 0, 2) };
            OptionsPanel.Children.Add(label);

            FrameworkElement control = prop.PropertyType switch
            {
                Type t when t == typeof(string) => new TextBox { Text = (string?)prop.GetValue(options) ?? "" },
                Type t when t == typeof(int) => new TextBox { Text = prop.GetValue(options)?.ToString() ?? "0" },
                Type t when t == typeof(bool) => new CheckBox { IsChecked = (bool?)prop.GetValue(options) ?? false },
                _ => new TextBlock { Text = $"Unsupported: {prop.PropertyType}" }
            };

            if (control != null)
            {
                OptionsPanel.Children.Add(control);
                _controls[prop.Name] = control;
            }
        }

        var saveButton = new Button { Content = "Save", Margin = new Thickness(0, 10, 0, 0) };
        saveButton.Click += SaveButton_Click;
        OptionsPanel.Children.Add(saveButton);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var options = App.GlobalOptions;
        foreach (var (propName, control) in _controls)
        {
            var prop = typeof(Options).GetProperty(propName);
            if (prop == null) continue;

            try
            {
                if (control is TextBox textBox)
                {
                    if (prop.PropertyType == typeof(int))
                        prop.SetValue(options, int.Parse(textBox.Text));
                    else
                        prop.SetValue(options, textBox.Text);
                }
                else if (control is CheckBox checkBox)
                {
                    prop.SetValue(options, checkBox.IsChecked == true);
                }
            }
            catch
            {
                MessageBox.Show($"Invalid value for {propName}");
            }
        }

        App.SaveOptions();
        MessageBox.Show("Options saved.");
    }
}
}