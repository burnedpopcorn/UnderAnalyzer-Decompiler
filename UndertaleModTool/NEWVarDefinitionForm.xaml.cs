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
            PopulateAssetTypes(); // Populate the AssetTypeComboBox in the first row
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

        // Populate the AssetTypeComboBox for the first row
        private void PopulateAssetTypes()
        {
            AssetTypeComboBox.ItemsSource = GetAssetTypes();
        }

        // Handle adding a new row with a TextBox and ComboBox
        private void AddVarRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new Grid for the new row
            Grid newRow = new Grid();
            newRow.Margin = new Thickness(10, 0, 10, 10);

            // Define the columns explicitly with GridLength.Star
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Define the rows
            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Create the TextBlock for "Variable"
            TextBlock variableLabel = new TextBlock
            {
                Text = "Variable:",
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center
            };
            newRow.Children.Add(variableLabel);
            Grid.SetRow(variableLabel, 0);
            Grid.SetColumn(variableLabel, 1);

            // Create the TextBox for entering the variable name
            TextBox variableTextBox = new TextBox
            {
                Margin = new Thickness(10)
            };
            newRow.Children.Add(variableTextBox);
            Grid.SetRow(variableTextBox, 1);
            Grid.SetColumn(variableTextBox, 1);

            // Create the TextBlock for "Asset Type"
            TextBlock assetTypeLabel = new TextBlock
            {
                Text = "Asset Type:",
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center
            };
            newRow.Children.Add(assetTypeLabel);
            Grid.SetRow(assetTypeLabel, 2);
            Grid.SetColumn(assetTypeLabel, 1);
            Grid.SetColumnSpan(assetTypeLabel, 3);

            // Create the ComboBox for asset types
            ComboBox newComboBox = new ComboBox
            {
                Margin = new Thickness(24, 0, 8, 0),
                ItemsSource = GetAssetTypes(), // Bind to static method to get asset types
            };
            newRow.Children.Add(newComboBox);
            Grid.SetRow(newComboBox, 2);
            Grid.SetColumn(newComboBox, 3);

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
