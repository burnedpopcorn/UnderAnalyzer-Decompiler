// Pizza Tower Enum Finder
// with UI, because yeah

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UndertaleModLib;

namespace UndertaleModTool
{
    public partial class CSTMPTENUM : Window
    {
        #region Initialize

        // For data.win reading
        private static readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        private static readonly UndertaleData Data = mainWindow.Data;

        // For Dark Mode Title Bar
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        // Initialize Everything
        public CSTMPTENUM()
        {
            InitializeComponent();
            AddENUMRow();
        }
        #endregion
        #region Enum stuffs

        // Add Function Button Press
        private void AddENUMRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Add Row Function for functions
            AddENUMRow();
        }

        // Add Enum row
        public void AddENUMRow(string functionName = "gml_Script_", string functionArguments = "scr_", string optionalArgumentsString = "state")
        {
            Grid newRow = new();

            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // For First Textbox (code name to check)
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // For Second TextBox (function prefix to check)
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });  // For Third TextBox (switch state var name)
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });  // For Remove Button

            newRow.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // First TextBox for code entry name
            TextBox functionTextBox1 = new()
            {
                Text = functionName
            };
            newRow.Children.Add(functionTextBox1);
            Grid.SetRow(functionTextBox1, 0);
            Grid.SetColumn(functionTextBox1, 0);

            // Second TextBox for state functions
            TextBox functionTextBox2 = new()
            {
                Text = functionArguments
            };
            newRow.Children.Add(functionTextBox2);
            Grid.SetRow(functionTextBox2, 0);
            Grid.SetColumn(functionTextBox2, 1);

            // Third TextBox for switch var name
            TextBox functionTextBox3 = new()
            {
                Text = optionalArgumentsString,
                Width = 75
            };
            newRow.Children.Add(functionTextBox3);
            Grid.SetRow(functionTextBox3, 0);
            Grid.SetColumn(functionTextBox3, 2);

            // Remove Button
            Button removeButton = new() { Content = "DEL", Width = 30 };
            removeButton.Click += (s, e) =>
            {
                VariableRowsPanel.Children.Remove(newRow);
            };
            newRow.Children.Add(removeButton);
            Grid.SetRow(removeButton, 0);
            Grid.SetColumn(removeButton, 3);

            VariableRowsPanel.Children.Add(newRow);
        }
        #endregion
        #region Save Inputs
        public void CSTMSaveInputsButton(object sender, RoutedEventArgs e)
        {
            // Loop through all rows in VariableRowsPanel
            var debugnum = 1;
            foreach (var item in VariableRowsPanel.Children)
            {
                if (item is Grid row)
                {
                    // textboxes
                    TextBox textBox1 = null;
                    TextBox textBox2 = null;
                    TextBox textBox3 = null;

                    foreach (var child in row.Children)
                    {
                        if (child is TextBox tBox)
                        {
                            if (textBox1 == null)
                                textBox1 = tBox;
                            else if (textBox2 == null)
                                textBox2 = tBox;
                            else if (textBox3 == null)
                                textBox3 = tBox;
                        }
                        // deal with remove button
                        else if (child is Button removeButton)
                        {
                            // If the row contains a "Remove" button, skip it
                            if (removeButton.IsPressed)
                            {
                                return; // Do nothing for this row if it's deleted
                            }
                        }
                    }

                    // Check if both TextBoxes exist (Function row)
                    if (textBox1 != null && textBox2 != null && textBox3 != null)
                    {
                        // Get the input text
                        string textBox1Text = textBox1.Text;
                        string textBox2Text = textBox2.Text;
                        string textBox3Text = textBox3.Text;

                        // split all functions
                        string[] functionsin_tbox2 = textBox2Text.Split(',')
                            .Select(arg => arg.Trim())  // remove spaces
                            .ToArray();

                        // Search for Pizza Tower Enum
                        if (textBox1Text != "gml_Script_" && textBox2Text != "scr_")// skip the generic ones
                        {
                            try
                            {
                                PT_AssetResolver.FindStateNames(Data.Code.ByName(textBox1Text), // Code Entry to search
                                textBox3Text,                                            // Switch Var Name, ex: switch (state)
                                functionsin_tbox2,                                          // scripts of state name, ex: (scr_player_normal(); --> normal
                                Data // just here because
                                );
                            }
                            catch (Exception ex)
                            {
                                mainWindow.ShowWarning($"Failed to Extract Pizza Tower Enums from Row {debugnum}");
                            }
                        }
                    }
                }
            }
            // call main pt json func
            PT_AssetResolver.InitializeTypes(Data);
        }

        public void CSTMUseDefaultsButton(object sender, RoutedEventArgs e)
        {
            AddENUMRow("gml_Object_obj_player_Step_0", "scr_player_, state_player_, scr_playerN_");
            AddENUMRow("gml_Object_obj_cheeseslime_Step_0", "scr_enemy_, scr_pizzagoblin_");
            AddENUMRow("gml_Object_obj_pepperman_Step_0", "scr_boss_, scr_pepperman_, scr_enemy_");
            AddENUMRow("gml_Object_obj_vigilanteboss_Step_0", "scr_vigilante_");
            AddENUMRow("gml_Object_obj_noiseboss_Step_0", "scr_noise_");
            AddENUMRow("gml_Object_obj_fakepepboss_Step_0", "scr_fakepepboss_, scr_boss_");
            AddENUMRow("gml_Object_obj_pizzafaceboss_Step_0", "scr_pizzaface_");
            AddENUMRow("gml_Object_obj_pizzafaceboss_p2_Step_0", "scr_pizzaface_p2_, scr_pizzaface_");
            AddENUMRow("gml_Object_obj_pizzafaceboss_p3_Step_0", "scr_pizzaface_p3_");
        }
        #endregion
    }
}
