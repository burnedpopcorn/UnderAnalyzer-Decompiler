// VERY WIP
// Variable Definition JSON Maker
// with UI, because yeah

// only does Variables and Function Arguments at the moment
// Enums will be done soon maybe

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Text.Json.Nodes;

namespace UndertaleModTool
{
    public partial class VarDefinitionForm : Window
    {
        #region Public Assets

        // For data.win reading
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        // For Function Saving
        public static Dictionary<string, string[]> all_funcs;

        // Helper class to hold row data
        public class RowData
        {
            public string Text { get; set; }
            public string SelectedOption { get; set; }

            public string TexBox { get; set; }
        }

        // For Dark Mode Title Bar
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        #endregion

        // Initialize Everything
        public VarDefinitionForm(bool editing = false)
        {
            InitializeComponent();

            // If editing existing one, prompt user for JSON and load
            // This bool is set true or left false in MainWindow
            if (editing) 
            {
                PromptJSONLoad();
            }
            else
            {             
                // Set Initial Variable Row if making a New one
                AddVarRow();
            }
        }

        // List of all Asset Types for DropDown Menu
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

        #region Edit Existing JSON
        public void PromptJSONLoad()
        {
            // Open file dialog to select the JSON file to load
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                LoadExistingJson(filePath); // Load the selected JSON file
            }
        }
        public void LoadExistingJson(string filePath)
        {
            try
            {
                // Read file
                string jsonString = File.ReadAllText(filePath);
                // Deserialize the JSON
                var loadedJson = JsonSerializer.Deserialize<JsonDocument>(jsonString);

                if (loadedJson != null)
                {
                    // Check if GlobalNames is available
                    if (loadedJson.RootElement.TryGetProperty("GlobalNames", out var globalNamesElement))
                    {
                        // Get Important JSON Properties
                        var variables = globalNamesElement.GetProperty("Variables");
                        var functions = globalNamesElement.GetProperty("FunctionArguments");

                        // Clear existing rows (shouldn't happen, but just in case i guess)
                        VariableRowsPanel.Children.Clear();

                        // Loop through Variables and add them to the UI
                        foreach (var variable in variables.EnumerateObject())
                        {
                            string variableName = variable.Name;
                            string assetType = variable.Value.ToString();
                            AddVarRow(variableName, assetType); // Add Rows with all Vars in JSON
                        }

                        // Loop through Functions and add them to the UI
                        foreach (var function in functions.EnumerateObject())
                        {
                            string functionName = function.Name;
                            // Add all arguments
                            var functionArguments = function.Value.EnumerateArray()
                                .Select(arg => arg.ValueKind == JsonValueKind.Null ? "null" : arg.ToString())
                                .ToArray();
                            string functionArgumentsString = string.Join(", ", functionArguments); // Join all arguments with commas

                            AddFunctionRow(functionName, functionArgumentsString); // Add Rows with all Funcs in JSON
                        }
                        // SUCCESS!!!
                        MessageBox.Show("JSON File Loaded Successfully");
                    }
                    // Else FAILURE??????
                    // FUCKKKKKKKK
                    else
                    {
                        MessageBox.Show("No 'GlobalNames' property found in the JSON file.");
                    }
                }
                else
                {
                    MessageBox.Show("Error deserializing the JSON file.");
                }
            }
            // Extra Bad
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading JSON: {ex.Message}");
            }
        }
        #endregion

        #region Variable Functions

        // Add Variable Button Press
        private void AddVarRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Add Row Function
            AddVarRow();
        }

        // Add Row Function
        // Isn't built in above because of the Initial Row
        public void AddVarRow(string variableName = "", string assetType = "")
        {
            Grid newRow = new Grid();

            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // For Textbox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // For ComboBox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });  // For Remove Button

            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            TextBox variableTextBox = new TextBox { Text = variableName };  // Set the default text
            newRow.Children.Add(variableTextBox);
            Grid.SetRow(variableTextBox, 0);
            Grid.SetColumn(variableTextBox, 0);

            ComboBoxDark newComboBox = new ComboBoxDark
            {
                ItemsSource = GetAssetTypes(),
                SelectedItem = assetType // Set the default selection
            };
            newRow.Children.Add(newComboBox);
            Grid.SetRow(newComboBox, 0);
            Grid.SetColumn(newComboBox, 1);

            ButtonDark removeButton = new ButtonDark { Content = "DEL", Width = 30 };
            removeButton.Click += (s, e) =>
            {
                VariableRowsPanel.Children.Remove(newRow);
            };
            newRow.Children.Add(removeButton);
            Grid.SetRow(removeButton, 0);
            Grid.SetColumn(removeButton, 2);

            VariableRowsPanel.Children.Add(newRow);
        }

        #endregion

        #region Function Functions

        // Add Function Button Press
        private void AddFunctionRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Add Row Function for functions
            AddFunctionRow();
        }

        // Add Row Function for functions (2 TextBoxes)
        public void AddFunctionRow(string functionName = "gml_Script_", string functionArguments = "")
        {
            Grid newRow = new Grid();

            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // For First Textbox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // For Second TextBox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });  // For Remove Button

            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            TextBox functionTextBox1 = new TextBox
            {
                Text = functionName // Default function name
            };
            newRow.Children.Add(functionTextBox1);
            Grid.SetRow(functionTextBox1, 0);
            Grid.SetColumn(functionTextBox1, 0);

            TextBox functionTextBox2 = new TextBox
            {
                Text = functionArguments // Default function arguments
            };
            newRow.Children.Add(functionTextBox2);
            Grid.SetRow(functionTextBox2, 0);
            Grid.SetColumn(functionTextBox2, 1);

            ButtonDark removeButton = new ButtonDark { Content = "DEL", Width = 30 };
            removeButton.Click += (s, e) =>
            {
                VariableRowsPanel.Children.Remove(newRow);
            };
            newRow.Children.Add(removeButton);
            Grid.SetRow(removeButton, 0);
            Grid.SetColumn(removeButton, 2);

            VariableRowsPanel.Children.Add(newRow);
        }

        #endregion

        public void SaveButton_Func()
        {
            // Get data from data.win
            var data = mainWindow.Data;

            var rowsData = new List<RowData>();

            // Dictionaries for storing user input separately for function rows and variable rows
            Dictionary<string, string> variableRows = new Dictionary<string, string>(); // Variable name and asset type
            all_funcs = new Dictionary<string, string[]> { };

            // Loop through all rows in VariableRowsPanel
            foreach (var item in VariableRowsPanel.Children)
            {
                if (item is Grid row)
                {
                    // Identify if it's a function row or variable row
                    TextBox textBox1 = null;
                    ComboBox comboBox = null;

                    // Function TextBox
                    TextBox textBox2 = null;

                    // Check the children of the row (either TextBox, ComboBox, or both)
                    foreach (var child in row.Children)
                    {
                        if (child is TextBox tBox)
                        {
                            // If the first TextBox is found, assign it to textBox1
                            if (textBox1 == null)
                                textBox1 = tBox;
                            else
                                textBox2 = tBox; // If a second TextBox is found, assign it to textBox2 (function argument)
                        }
                        else if (child is ComboBox cBox)
                        {
                            comboBox = cBox;
                        }
                        else if (child is Button removeButton)
                        {
                            // If the row contains a "Remove" button, skip it when processing
                            if (removeButton.IsPressed)
                            {
                                return; // Do nothing for this row if it's deleted
                            }
                        }
                    }

                    // Check if both TextBoxes exist (Function row)
                    if (textBox1 != null && textBox2 != null)
                    {
                        // Get the function name and function argument from the two TextBoxes
                        string functionName = textBox1.Text;
                        string functionArgumentString = textBox2.Text;

                        // Separate "Asset." and null
                        var functionArguments = functionArgumentString
                            .Split(',')
                            .Select(arg => arg.Trim())  // trim extra spaces
                            .Select(arg => arg == "null" ? null : arg)  // Handle "null" as a null value
                            .ToArray();

                        all_funcs[functionName] = functionArguments;
                    }
                    // Check if there's one TextBox and one ComboBox (Variable row)
                    else if (textBox1 != null && comboBox != null)
                    {
                        // Get the variable name and selected asset type from the TextBox and ComboBox
                        string variableName = textBox1.Text;
                        string assetType = comboBox.SelectedItem as string;

                        // Add the variable name and asset type to the variableRows dictionary
                        if (!string.IsNullOrEmpty(variableName) && !string.IsNullOrEmpty(assetType))
                        {
                            variableRows.Add(variableName, assetType);
                        }
                    }
                }
            }

            try
            {
                // Main JSON structure
                var JSON = new
                {
                    // Enum Only Branch
                    Types = new
                    {
                        // Should be replaced later when feature is implimented
                        Enums = new { },
                        // Shit just for the Template
                        Constants = new { },
                        General = new { }
                    },
                    // Other Shit Branch
                    GlobalNames = new
                    {
                        // Store the variables in the "Variables" section
                        Variables = variableRows,
                        // and funcs in the FunctionArguments section
                        FunctionArguments = all_funcs,
                        // Shit just for the Template
                        FunctionReturn = new { }
                    },
                    // Shit just for the Template
                    CodeEntryNames = new { }
                };

                // Convert the parent object to a JSON string
                string jsonString = JsonSerializer.Serialize(JSON, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Underanalyzer/CUSTOM_DEFINITIONS.json", jsonString);

                MessageBox.Show("JSON File has been Saved\n\nRemember to Restart the Program for it to Apply");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving JSON:\n\n {ex.Message}");
            }
        }

        #region Loader JSON File Functions

        // Function to make Loader JSON File (Only for Specific Game that is Currently Loaded)
        public void SaveButtonSingle_Click(object sender, RoutedEventArgs e)
        {
            // Get data from data.win
            var data = mainWindow.Data;

            if (data == null)
            {
                // Failsafe just in case user is dumb
                Application.Current.MainWindow.ShowWarning("No data.win was loaded\nLoad a data.win first");
                return;
            }

            // Get User Input and make Var Def JSON File
            SaveButton_Func();

            string dataname = data.GeneralInfo.DisplayName + "";
            string datanameclean = dataname.Replace("\"", "");

            // Loader JSON
            var loader = new
            {
                LoadOrder = 0,
                Conditions = new[]
                {
                    new
                        {
                            ConditionKind = "DisplayName.Regex",
                            Value = $"(?i)^{datanameclean}"
                        }
                    },
                UnderanalyzerFilename = "CUSTOM_DEFINITIONS.json"
            };
            // Write Loader JSON
            string loaderString = JsonSerializer.Serialize(loader, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Definitions/LOADER.json", loaderString);
        }

        // Function to make Loader JSON File (For All Games)
        private void SaveButtonMulti_Click(object sender, RoutedEventArgs e)
        {
            // Get User Input and make Var Def JSON File
            SaveButton_Func();

            // Loader JSON
            var loader = new
            {
                LoadOrder = 0,
                Conditions = new[]
                {
                    new
                        {
                            ConditionKind = "Always"
                        }
                    },
                UnderanalyzerFilename = "CUSTOM_DEFINITIONS.json"
            };
            // Write Loader JSON
            string loaderString = JsonSerializer.Serialize(loader, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Definitions/LOADER.json", loaderString);
        }

        #endregion
    }
}
