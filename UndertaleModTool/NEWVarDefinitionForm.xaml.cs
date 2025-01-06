using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace UndertaleModTool
{
    public partial class VarDefinitionForm : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public VarDefinitionForm()
        {
            InitializeComponent();
        }

        // Method to return predefined asset types
        public static List<string> GetAssetTypes()
        {
            return new List<string>
            {
                "Bool",
                "Id.Instance",
                "Asset.Object",
                "Asset.Sprite",
                "Asset.Sound",
                "Asset.Room",
                "Asset.Background",
                "Asset.Tileset",
                "Asset.Path",
                "Asset.Script",
                "Asset.Font",
                "Asset.Timeline",
                "Asset.Shader",
                "Asset.Sequence",
                "Asset.AnimationCurve",
                "Asset.ParticleSystem",
                "Asset.RoomInstance",
                "Constant.Color",
                "Constant.VirtualKey"
            };
        }

        // Handle adding a new row with a TextBox and ComboBox
        private void AddVarRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new Grid for the new row
            Grid newRow = new Grid();

            // Define Colums for 
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // 50% width for the TextBox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });    // 50% width for the ComboBox

            // Define the rows (one row for TextBox and ComboBox)
            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Create Variable TextBox
            TextBox variableTextBox = new TextBox { };
            newRow.Children.Add(variableTextBox);
            Grid.SetRow(variableTextBox, 0);  // Place it in the first row
            Grid.SetColumn(variableTextBox, 0); // Place it in the first column (left half)

            // Create the ComboBox for asset types
            ComboBox newComboBox = new ComboBox
            {
                ItemsSource = GetAssetTypes(), // Bind to static method to get asset types
            };
            newRow.Children.Add(newComboBox);
            Grid.SetRow(newComboBox, 0);  // Place it in the first row
            Grid.SetColumn(newComboBox, 1); // Place it in the second column (right half)

            // Add the new row to the VariableRowsPanel
            VariableRowsPanel.Children.Add(newRow);
        }

        public void SaveButton_Func()
        {
            // Get data from data.win
            var data = mainWindow.Data;

            if (data == null)
            {
                // Failsafe just in case user is dumb
                Application.Current.MainWindow.ShowWarning("No data.win was loaded\nLoad a data.win first");
                return;
            }

            var rowsData = new List<RowData>();

            all_vars = new Dictionary<string, string> { };

            // Loop through all rows in VariableRowsPanel
            foreach (var item in VariableRowsPanel.Children)
            {
                if (item is Grid row)
                {
                    // Access TextBox and ComboBox elements more safely
                    TextBox textBox = null;
                    ComboBox comboBox = null;

                    foreach (var child in row.Children)
                    {
                        if (child is TextBox tBox)
                            textBox = tBox;
                        else if (child is ComboBox cBox)
                            comboBox = cBox;
                    }

                    // Check if both TextBox and ComboBox are found
                    if (textBox != null && comboBox != null)
                    {
                        var selectedOption = comboBox.SelectedItem as string; // Get selected asset type
                        string texBox = textBox.Text;

                        all_vars.Add(texBox, selectedOption);
                    }
                }
            }

            string dataname = data.GeneralInfo.Name + "";
            string datanameclean = dataname.Replace("\"", "");

            try
            {
                // Main JSON thingy
                var JSON = new
                {
                    // Enum Only Branch
                    Types = new
                    {
                        // goofy thing from above
                        Enums = "",
                        // Shit just for the Template
                        Constants = new { },
                        General = new { }
                    },
                    // Other Shit Branch
                    GlobalNames = new
                    {
                        // crackful
                        Variables = all_vars,
                        FunctionArguments = "",
                        // Shit just for the Template
                        FunctionReturn = new { }
                    },
                    // Shit just for the Template
                    CodeEntryNames = new { }
                };
                // Convert the parent object to a JSON string
                string jsonString = JsonSerializer.Serialize(JSON, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/GameSpecificData/Underanalyzer/" + datanameclean + "_CUSTOM.json", jsonString);

                MessageBox.Show("JSON File has been Saved");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving JSON:\n\n {ex.Message}");
            }
        }

        // Function to make Loader JSON File (Only for Specific Game that is Currently Loaded)
        public void SaveButtonSingle_Click(object sender, RoutedEventArgs e)
        {
            // Get data from data.win
            var data = mainWindow.Data;
            // Get User Input and make Var Def JSON File
            SaveButton_Func();

            if (data == null)
            {
                return;
            }

            string dataname = data.GeneralInfo.Name + "";
            string datanameclean = dataname.Replace("\"", "");

            // Loader JSON
            var loader = new
            {
                LoadOrder = 1,
                Conditions = new[]
                {
                        new
                        {
                            ConditionKind = "DisplayName.Regex",
                            Value = $"(?i)^{datanameclean}"
                        }
                    },
                UnderanalyzerFilename = datanameclean + "_CUSTOM.json"
            };
            // Write Loader JSON
            string loaderString = JsonSerializer.Serialize(loader, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/GameSpecificData/Definitions/" + datanameclean + "_LOADER.json", loaderString);
        }

        // Function to make Loader JSON File (For All Games)
        private void SaveButtonMulti_Click(object sender, RoutedEventArgs e)
        {
            // Get data from data.win
            var data = mainWindow.Data;
            // Get User Input and make Var Def JSON File
            SaveButton_Func();

            if (data == null)
            {
                return;
            }

            string dataname = data.GeneralInfo.Name + "";
            string datanameclean = dataname.Replace("\"", "");

            // Loader JSON
            var loader = new
            {
                LoadOrder = 1,
                Conditions = new[]
                {
                        new
                        {
                            ConditionKind = "Always"
                        }
                    },
                UnderanalyzerFilename = datanameclean + "_CUSTOM.json"
            };
            // Write Loader JSON
            string loaderString = JsonSerializer.Serialize(loader, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/GameSpecificData/Definitions/" + datanameclean + "_LOADER.json", loaderString);
        }

        public static Dictionary<string, string> all_vars;

        // Helper class to hold row data
        public class RowData
        {
            public string Text { get; set; }
            public string SelectedOption { get; set; }

            public string TexBox { get; set; }
        }
    }
}
