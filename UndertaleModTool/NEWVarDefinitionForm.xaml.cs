// VERY WIP
// Variable Definition JSON Maker
// with UI, because yeah

// only does Variables at the moment
// Enums and Functions will be done soon maybe

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

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
        public VarDefinitionForm()
        {
            InitializeComponent();

            // Set Initial Variable Row
            AddVarRow();
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

        #region Variable Functions

        // Add Variable Button Press
        private void AddVarRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Add Row Function
            AddVarRow();
        }

        // Add Row Function
        // Isn't built in above because of the Initial 8 Rows
        public void AddVarRow() 
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
            // Use UTMT's custom box
            ComboBoxDark newComboBox = new ComboBoxDark
            {
                ItemsSource = GetAssetTypes(), // Bind to static method to get asset types
            };
            newRow.Children.Add(newComboBox);
            Grid.SetRow(newComboBox, 0);  // Place it in the first row
            Grid.SetColumn(newComboBox, 1); // Place it in the second column (right half)

            // Add the new row to the VariableRowsPanel
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
        public void AddFunctionRow()
        {
            // Create a new Grid for the new row
            Grid newRow = new Grid();

            // Define Columns: 2 columns, each for a TextBox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // 50% width for the first TextBox
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // 50% width for the second TextBox

            // Define the rows (one row for the two TextBoxes)
            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Create first TextBox (e.g., Function name)
            TextBox functionTextBox1 = new TextBox
            {
                Text = "gml_Script_" // add this by default because underanalyzer is whack
            };
            newRow.Children.Add(functionTextBox1);
            Grid.SetRow(functionTextBox1, 0);  // Place it in the first row
            Grid.SetColumn(functionTextBox1, 0); // Place it in the first column (left half)

            // Create second TextBox (e.g., Function arguments)
            TextBox functionTextBox2 = new TextBox { };
            newRow.Children.Add(functionTextBox2);
            Grid.SetRow(functionTextBox2, 0);  // Place it in the first row
            Grid.SetColumn(functionTextBox2, 1); // Place it in the second column (right half)

            // Add the new row to the VariableRowsPanel
            VariableRowsPanel.Children.Add(newRow);
        }

        #endregion

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
                    }

                    // Check if both TextBoxes exist (Function row)
                    if (textBox1 != null && textBox2 != null)
                    {
                        // Get the function name and function argument from the two TextBoxes
                        string functionName = textBox1.Text;
                        string functionArgumentString = textBox2.Text;

                        // Seperate "Asset." and null
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
                        variableRows.Add(variableName, assetType);
                    }
                }
            }

            string dataname = data.GeneralInfo.DisplayName + "";
            string datanameclean = dataname.Replace("\"", "");

            try
            {
                // Main JSON structure
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
                File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Underanalyzer/" + datanameclean + "_CUSTOM.json", jsonString);

                MessageBox.Show("JSON File has been Saved");
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
            // Get User Input and make Var Def JSON File
            SaveButton_Func();

            if (data == null)
            {
                return;
            }

            string dataname = data.GeneralInfo.DisplayName + "";
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
            File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Definitions/" + datanameclean + "_LOADER.json", loaderString);
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

            string dataname = data.GeneralInfo.DisplayName + "";
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
            File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Definitions/" + datanameclean + "_LOADER.json", loaderString);
        }

        #endregion
    }
}
