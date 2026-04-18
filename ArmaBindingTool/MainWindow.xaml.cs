using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharpDX.DirectInput;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace ArmaBindingTool
{
    public class ArmaActionItem
    {
        public string TechnicalName { get; set; } = "";
        public string Preset { get; set; } = ""; // z.B. "next", "previous", "click"
        public string CurrentBinding { get; set; } = "---";
        public int LineIndex { get; set; } // Position in der Datei für schnelleres Speichern
    }

    public partial class MainWindow : Window
    {
        private DirectInput _directInput = new DirectInput();
        private List<Joystick> _connectedJoysticks = new List<Joystick>();
        private Dictionary<string, int> _lastAxisValues = new Dictionary<string, int>();
        private List<string>? _confFileContent;
        private string _selectedFilePath = "";

        public ObservableCollection<ArmaActionItem> ActionItems { get; set; } = new ObservableCollection<ArmaActionItem>();
        private ICollectionView _filteredView;

        public MainWindow()
        {
            InitializeComponent();
            _filteredView = CollectionViewSource.GetDefaultView(ActionItems);
            ArmaActionsList.ItemsSource = _filteredView;

            FindJoysticks();
            CompositionTarget.Rendering += GameLoop;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.ToLower();
            _filteredView.Filter = obj => {
                var item = obj as ArmaActionItem;
                return item != null && (item.TechnicalName.ToLower().Contains(filter) || item.Preset.ToLower().Contains(filter));
            };
        }

        private void FindJoysticks()
        {
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            foreach (var deviceInstance in devices)
            {
                try
                {
                    var joystick = new Joystick(_directInput, deviceInstance.InstanceGuid);
                    joystick.SetCooperativeLevel(new System.Windows.Interop.WindowInteropHelper(this).Handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                    foreach (var deviceObject in joystick.GetObjects(DeviceObjectTypeFlags.Axis))
                        joystick.GetObjectPropertiesById(deviceObject.ObjectId).Range = new InputRange(0, 65535);
                    joystick.Acquire();
                    _connectedJoysticks.Add(joystick);
                    JoystickList.Items.Add(deviceInstance.InstanceName);
                }
                catch { }
            }
        }

        private void GameLoop(object? sender, EventArgs? e)
        {
            for (int i = 0; i < _connectedJoysticks.Count; i++)
            {
                var joystick = _connectedJoysticks[i];
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();

                    // 1. Achsen (wie bisher)
                    int[] allValues = { state.X, state.Y, state.Z, state.RotationX, state.RotationY, state.RotationZ, state.Sliders[0], state.Sliders[1] };
                    for (int a = 0; a < allValues.Length; a++)
                    {
                        int val = allValues[a];
                        string key = $"j{i}a{a}";
                        if (!_lastAxisValues.ContainsKey(key) || Math.Abs(_lastAxisValues[key] - val) > 3000)
                        {
                            _lastAxisValues[key] = val;
                            if (Math.Abs(val - 32767) > 15000)
                            {
                                string dir = (val < 32767) ? "-" : "+";
                                ProcessBinding($"joystick{i}:axis{a}{dir}");
                            }
                        }
                    }

                    // 2. Buttons (wie bisher)
                    for (int b = 0; b < state.Buttons.Length; b++)
                        if (state.Buttons[b]) ProcessBinding($"joystick{i}:button{b}");

                    // 3. NEU: Coolie Hat (POV)
                    for (int p = 0; p < state.PointOfViewControllers.Length; p++)
                    {
                        int povValue = state.PointOfViewControllers[p];
                        if (povValue != -1)
                        { // -1 bedeutet zentriert
                            string direction = "";
                            if (povValue == 0) direction = "up";
                            else if (povValue == 9000) direction = "right";
                            else if (povValue == 18000) direction = "down";
                            else if (povValue == 27000) direction = "left";

                            if (!string.IsNullOrEmpty(direction))
                            {
                                ProcessBinding($"joystick{i}:pov{p}{direction}");
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void ProcessBinding(string binding)
        {
            StatusLabel.Text = binding;
            if (ArmaActionsList.SelectedItem is ArmaActionItem selectedItem && _confFileContent != null)
            {
                if (selectedItem.CurrentBinding != binding)
                {
                    UpdateFileContent(selectedItem, binding);
                    selectedItem.CurrentBinding = binding;
                    ArmaActionsList.Items.Refresh();
                }
            }
        }

        private void UpdateFileContent(ArmaActionItem item, string newBinding)
        {
            if (_confFileContent == null) return;

            // Wir nutzen den gespeicherten LineIndex der "Input"-Zeile
            string line = _confFileContent[item.LineIndex];
            int inputIdx = line.IndexOf("Input");
            string indentation = line.Substring(0, inputIdx);

            _confFileContent[item.LineIndex] = $"{indentation}Input \"{newBinding}\"";

            try
            {
                File.WriteAllLines(_selectedFilePath, _confFileContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving file: " + ex.Message);
            }
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Arma Config (*.conf)|*.conf";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                _confFileContent = new List<string>(File.ReadAllLines(_selectedFilePath));
                FilePathLabel.Text = Path.GetFileName(_selectedFilePath);
                ParseFullFile();
            }
        }

        private void ParseFullFile()
        {
            if (_confFileContent == null) return;
            ActionItems.Clear();

            string currentAction = "";
            string currentPreset = "default";

            for (int i = 0; i < _confFileContent.Count; i++)
            {
                string line = _confFileContent[i].Trim();

                if (line.StartsWith("Action ") && line.Contains("{"))
                {
                    currentAction = line.Replace("Action ", "").Replace("{", "").Trim();
                    currentPreset = "default";
                }

                if (line.StartsWith("FilterPreset \""))
                {
                    currentPreset = line.Split('"')[1];
                }

                if (line.StartsWith("Input \""))
                {
                    int start = line.IndexOf("\"") + 1;
                    int end = line.LastIndexOf("\"");
                    string binding = line.Substring(start, end - start);

                    ActionItems.Add(new ArmaActionItem
                    {
                        TechnicalName = currentAction,
                        Preset = currentPreset,
                        CurrentBinding = binding,
                        LineIndex = i // Merken, wo genau in der Datei dieser Input steht
                    });
                }
            }
        }
    }
}