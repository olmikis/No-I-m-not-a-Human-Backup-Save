using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace no_im_not_a_human_saver
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string SavesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SavedSaves");
        private string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private string RemotePath = null;

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(SavesPath);

            LoadSettings();
            if (string.IsNullOrEmpty(RemotePath))
            {
                FindRemotePath();
            }

            RefreshSavesList();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                SteamPathBox.Text = settings.SteamPath ?? "";
                RemotePath = settings.RemotePath;
            }
        }

        private void SaveSettings()
        {
            var settings = new Settings { SteamPath = SteamPathBox.Text, RemotePath = RemotePath };
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }

        private void BrowseSteamButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите исполняемый файл Steam.exe или папку Steam",
                Filter = "Папка Steam|steam.exe|Все файлы|*.*",
                CheckFileExists = false,
                FileName = "Выберите папку"
            };

            if (dialog.ShowDialog() == true)
            {
                string path = Path.GetDirectoryName(dialog.FileName);
                SteamPathBox.Text = path;
                SaveSettings();
                FindRemotePath();
            }
        }

        private void FindRemotePath()
        {
            string steamPath = SteamPathBox.Text;

            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
            {
                MessageBox.Show("Путь к Steam не задан или недействителен.");
                return;
            }

            string userDataPath = Path.Combine(steamPath, "userdata");

            if (!Directory.Exists(userDataPath))
            {
                MessageBox.Show("Не удалось найти папку Steam userdata.");
                return;
            }

            foreach (var userDir in Directory.GetDirectories(userDataPath))
            {
                string gameDir = Path.Combine(userDir, "3180070", "remote");
                if (Directory.Exists(gameDir))
                {
                    RemotePath = gameDir;
                    SaveSettings();
                    return;
                }
            }

            MessageBox.Show("Не удалось найти путь к сохранениям игры (3180070\\remote).");
        }

        private void RefreshSavesList()
        {
            var saves = new List<SavItem>();
            foreach (var file in Directory.GetFiles(SavesPath, "*.sav"))
            {
                var fi = new FileInfo(file);
                var name = fi.Name;
                string comment = ExtractCommentFromFileName(name);
                saves.Add(new SavItem { Name = name, Date = fi.CreationTime.ToString(), Comment = comment });
            }
            SavesList.ItemsSource = saves;
        }

        // Извлечение комментария из имени файла
        private string ExtractCommentFromFileName(string fileName)
        {
            var parts = fileName.Split(new[] { "[", "]" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return "Без комментария";
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RemotePath))
            {
                MessageBox.Show("Путь к сохранениям не найден.");
                return;
            }

            string sourcePath = Path.Combine(RemotePath, "GameSaveData.sav");
            if (!File.Exists(sourcePath))
            {
                MessageBox.Show("Файл GameSaveData.sav не найден в папке remote.");
                return;
            }

            string comment = string.IsNullOrWhiteSpace(CommentBox.Text) ? "Без комментария" : CommentBox.Text;
            // Убираем недопустимые символы из комментария
            comment = string.Join("_", comment.Split(Path.GetInvalidFileNameChars()));

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string destPath = Path.Combine(SavesPath, $"GameSaveData_{timestamp}[{comment}].sav");
            File.Copy(sourcePath, destPath, true);
            RefreshSavesList();

            CommentBox.Clear(); // Очищаем поле после бэкапа
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (SavItem)SavesList.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Выберите файл для восстановления.");
                return;
            }

            if (string.IsNullOrEmpty(RemotePath))
            {
                MessageBox.Show("Путь к сохранениям не найден.");
                return;
            }

            string sourcePath = Path.Combine(SavesPath, selected.Name);
            string destPath = Path.Combine(RemotePath, "GameSaveData.sav");
            File.Copy(sourcePath, destPath, true);
            MessageBox.Show("Сохранение восстановлено.");
        }
    }

    public class Settings
    {
        public string SteamPath { get; set; }
        public string RemotePath { get; set; }
    }

    public class SavItem
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Comment { get; set; }
    }
}

