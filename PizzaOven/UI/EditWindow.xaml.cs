using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace PizzaOven.UI
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        public string _name;
        public bool _folder;
        public string directory = null;
        public string newName;
        public string loadout = null;
        public EditWindow(string name, bool folder)
        {
            InitializeComponent();
            _folder = folder;
            if (!String.IsNullOrEmpty(name))
            {
                _name = name;
                NameBox.Text = name;
                Title = $"Edit {name}";
            }
            else
                if (_folder)
                    Title = "Create New Mod";
                else
                    Title = "Create New Loadout";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_folder)
                if (_name != null)
                    EditFolderName();
                else
                    CreateName();

        }
        private void CreateName()
        {
            var newDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{NameBox.Text}";
            if (!Directory.Exists(newDirectory))
            {
                directory = newDirectory;
                Close();
            }
            else
                Global.logger.WriteLine($"{newDirectory} already exists", LoggerType.Error);
        }
        private void EditFolderName()
        {
            if (!NameBox.Text.Equals(_name, StringComparison.InvariantCultureIgnoreCase))
            {
                var oldDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{_name}";
                var newDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{NameBox.Text}";
                if (!Directory.Exists(newDirectory))
                {
                    try
                    {
                        Directory.Move(oldDirectory, newDirectory);
                        var index = Global.config.ModList.ToList().FindIndex(x => x.name == _name);
                        Global.config.ModList[index].name = NameBox.Text;
                        Global.ModList = Global.config.ModList;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($"Couldn't rename {oldDirectory} to {newDirectory} ({ex.Message})", LoggerType.Error);
                    }
                }
                else
                    Global.logger.WriteLine($"{newDirectory} already exists", LoggerType.Error);
            }
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (_folder)
                    if (_name != null)
                        EditFolderName();
                    else
                        CreateName();
            }
        }
    }
}
