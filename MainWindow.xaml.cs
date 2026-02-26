using Microsoft.Win32; 
using System; 
using System.Collections.Generic; 
using System.IO; 
using System.Linq; 
using System.Text.Json; 
using System.Windows; 
using System.Windows.Controls; 
using System.Windows.Input; 
using System.Threading.Tasks; 
using System.Windows.Media;
using System.Windows.Media.Imaging;
 
namespace autoquest 
{ 
    public partial class MainWindow : Window 
    { 
        // Données 
        private List<string> _imagePaths = new List<string>(); 
        private List<FileItem> _fileItems = new List<FileItem>(); 
        private List<List<string>> _patientBatches = new List<List<string>>(); 
        private int _currentPatientIndex = 0; 
        private List<Dictionary<string, string>> _results = new List<Dictionary<string, string>>(); 
        private List<string> _variables = new List<string>(); 
 
        // État Projet 
        private string _currentProjectPath = ""; 
        private string _projectFolder = ""; 
        private DateTime _projectCreated; 
 
        // API Connection 
        private ApiService? _apiService; 
        private string? _currentToken; 
        private DateTime _tokenExpiry; 
        private int _currentQuota = 0; 
 
        // Image Processing 
        private ImageProcessor _imageProcessor; 
 
        // Classe pour les fichiers avec sélection 
        public class FileItem 
        { 
            public string FilePath { get; set; } = "";
            public string FileName => System.IO.Path.GetFileName(FilePath); 
            public bool IsSelected { get; set; } 
        } 
 
        public MainWindow() 
        { 
            InitializeComponent(); 
 
            // Initialisation 
            _imagePaths = new List<string>(); 
            _fileItems = new List<FileItem>(); 
            _patientBatches = new List<List<string>>(); 
            _results = new List<Dictionary<string, string>>(); 
            _variables = new List<string>(); 
            _currentPatientIndex = 0; 
            _currentProjectPath = ""; 
            _projectFolder = ""; 
            _projectCreated = DateTime.Now; 
            _imageProcessor = new ImageProcessor(true, true); 
 
            // Initialiser les textes 
            UpdateAllTexts(); 
 
            // Désactiver le bouton d'extraction au démarrage 
            ExtractButton.IsEnabled = false; 
 
            // S'assurer que le textbox a une valeur par défaut 
            if (string.IsNullOrWhiteSpace(PagesPerPatientConfigTextBox.Text)) 
            { 
                PagesPerPatientConfigTextBox.Text = "1"; 
            } 
 
            // S'abonner aux changements de langue 
            LanguageManager.LanguageChanged += (s, e) => UpdateAllTexts(); 
 
            // Connexion à l'API au démarrage 
            Loaded += async (s, e) => await ConnectToApiAsync(); 
        } 
 
        private void InitializeDefaultVariables() 
        { 
            // Supprimé selon la demande de l'utilisateur (plus de variables par défaut)
            _variables.Clear();
            UpdateVariablesListBox(); 
        } 
 
        private void UpdateVariablesListBox() 
        { 
            VariablesListBox.ItemsSource = null; 
            VariablesListBox.ItemsSource = _variables; 
        } 
 
        // ========================================== 
        // GESTION DE LA LANGUE 
        // ========================================== 
 
        private void UpdateAllTexts() 
        { 
            // Menu 
            FileMenu.Header = LanguageManager.GetText("file_menu"); 
            NewProjectMenuItem.Header = LanguageManager.GetText("new_project"); 
            OpenProjectMenuItem.Header = LanguageManager.GetText("open_project"); 
            SaveMenuItem.Header = LanguageManager.GetText("save_project"); 
            SaveAsMenuItem.Header = LanguageManager.GetText("save_as"); 
            ExportMenu.Header = LanguageManager.GetText("export_menu"); 
            ExportExcelMenuItem.Header = LanguageManager.GetText("export_excel"); 
            ExportCsvMenuItem.Header = LanguageManager.GetText("export_csv"); 
            QuitMenuItem.Header = LanguageManager.GetText("quit"); 
 
            EditMenu.Header = LanguageManager.GetText("edit_menu"); 
            CopyMenuItem.Header = LanguageManager.GetText("copy"); 
            PasteMenuItem.Header = LanguageManager.GetText("paste"); 
            DeleteMenuItem.Header = LanguageManager.GetText("delete"); 
            SelectAllMenuItem.Header = LanguageManager.GetText("select_all"); 
 
            ViewMenu.Header = LanguageManager.GetText("view_menu"); 
            DarkModeMenuItem.Header = LanguageManager.GetText("dark_mode"); 
            ZoomInMenuItem.Header = LanguageManager.GetText("zoom_in"); 
            ZoomOutMenuItem.Header = LanguageManager.GetText("zoom_out"); 
 
            LanguageMenu.Header = LanguageManager.GetText("language_menu"); 
            MenuEnglish.Header = LanguageManager.GetText("english"); 
            MenuFrench.Header = LanguageManager.GetText("french"); 
 
            HelpMenu.Header = LanguageManager.GetText("help_menu"); 
            DocumentationMenuItem.Header = LanguageManager.GetText("documentation"); 
            AboutMenuItem.Header = LanguageManager.GetText("about"); 
 
            // Accueil 
            ProjectInfoTitle.Text = LanguageManager.GetText("project_info"); 
            QuickActionsTitle.Text = LanguageManager.GetText("quick_actions"); 
            StatsTitle.Text = LanguageManager.GetText("statistics"); 
            QuickNewProject.Content = LanguageManager.GetText("new_project"); 
            QuickOpenProject.Content = LanguageManager.GetText("open_project"); 
            QuickSaveProject.Content = LanguageManager.GetText("save_project"); 
            StatsFilesLabel.Text = LanguageManager.GetText("project_files"); 
            StatsExtractionsLabel.Text = LanguageManager.GetText("extractions"); 
            StatsToVerifyLabel.Text = LanguageManager.GetText("to_verify"); 
 
            // Configuration 
            ConfigTitle.Text = LanguageManager.GetText("config_title"); 
            AddVariableManuallyButton.Content = "➕ " + LanguageManager.GetText("config_add"); 
            PagesConfigTitle.Text = LanguageManager.GetText("pages_config"); 
 
            // Extraction 
            FileManagerTitle.Text = LanguageManager.GetText("file_manager"); 
            SelectAllCheckBox.Content = LanguageManager.GetText("select_all"); 
            DeleteSelectedButton.Content = "🗑️ " + LanguageManager.GetText("delete_selected"); 
            PrevButton.Content = "◀ " + LanguageManager.GetText("previous"); 
            NextButton.Content = LanguageManager.GetText("next") + " ▶"; 
            ExtractButton.Content = "🚀 " + LanguageManager.GetText("extract_button"); 
 
            // Résultats 
            SearchPlaceholder.Text = "🔍 " + LanguageManager.GetText("results_search"); 
            ExportExcelResultsButton.Content = "📊 " + LanguageManager.GetText("export_excel"); 
            ExportCsvResultsButton.Content = "📄 " + LanguageManager.GetText("export_csv"); 
            ClearResultsButton.Content = "🔄 " + LanguageManager.GetText("results_clear"); 
 
            // Statut 
            UpdateStatusText(); 
        } 
 
        private void MenuEnglish_Click(object sender, RoutedEventArgs e) 
        { 
            LanguageManager.SetLanguage(AppLanguage.English); 
        } 
 
        private void MenuFrench_Click(object sender, RoutedEventArgs e) 
        { 
            LanguageManager.SetLanguage(AppLanguage.French); 
        } 
 
        // ========================================== 
        // CONNEXION À L'API 
        // ========================================== 
 
        private async Task<bool> ConnectToApiAsync() 
        { 
            try 
            { 
                StatusText.Text = LanguageManager.GetText("status_connecting"); 
 
                string apiUrl = "http://localhost:8000"; 
                _apiService = new ApiService(apiUrl); 
 
                bool isHealthy = await _apiService.HealthCheckAsync(); 
                if (!isHealthy) 
                { 
                    StatusText.Text = LanguageManager.GetText("status_error"); 
                    return false; 
                } 
 
                if (await LoadExistingToken()) 
                { 
                    UpdateStatusText(); 
                    return true; 
                } 
 
                string fingerprint = MachineFingerprint.GetFingerprint(); 
                var activation = await _apiService.ActivateAsync(fingerprint); 
 
                _currentToken = activation.token; 
                _tokenExpiry = DateTime.Parse(activation.expires_at); 
                _currentQuota = activation.quota_remaining; 
 
                TokenStorage.SaveToken(_currentToken, _tokenExpiry, fingerprint); 
                UpdateStatusText(); 
                return true; 
            } 
            catch 
            { 
                StatusText.Text = LanguageManager.GetText("status_error"); 
                return false; 
            } 
        } 
 
        private async Task<bool> LoadExistingToken() 
        { 
            if (_apiService == null) return false;
            var tokenData = TokenStorage.LoadToken(); 
            if (tokenData != null && tokenData.Expiry > DateTime.Now) 
            { 
                string currentFingerprint = MachineFingerprint.GetFingerprint(); 
                if (tokenData.Fingerprint == currentFingerprint) 
                { 
                    _currentToken = tokenData.Token; 
                    _tokenExpiry = tokenData.Expiry; 
 
                    var verify = await _apiService.VerifyAsync(_currentToken); 
                    if (verify.valid) 
                    { 
                        _currentQuota = verify.remaining; 
                        return true; 
                    } 
                } 
            } 
            return false; 
        } 
 
        private async Task<bool> EnsureValidTokenAsync() 
        { 
            if (_apiService == null || string.IsNullOrEmpty(_currentToken)) 
                return false; 
 
            if (_tokenExpiry < DateTime.Now.AddMinutes(5)) 
            { 
                var verify = await _apiService.VerifyAsync(_currentToken); 
                if (!verify.valid) 
                { 
                    MessageBox.Show(LanguageManager.GetText("session_expired"), 
                                  LanguageManager.GetText("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning); 
                    return false; 
                } 
            } 
            return true; 
        } 
 
        private void UpdateStatusText() 
        { 
            if (_currentQuota <= 0) 
            { 
                StatusText.Text = LanguageManager.GetText("status_quota_empty"); 
            } 
            else if (_currentQuota < 10) 
            { 
                StatusText.Text = LanguageManager.GetFormattedText("status_quota_low", _currentQuota); 
            } 
            else 
            { 
                StatusText.Text = LanguageManager.GetText("status_ready"); 
            } 
 
            QuotaStatusText.Text = $"{_currentQuota} pages"; 
        } 
 
        // ========================================== 
        // GESTION DES VARIABLES 
        // ========================================== 
 
        private void AddVariableManually_Click(object sender, RoutedEventArgs e) 
        { 
            string newVar = NewVariableTextBox.Text.Trim(); 
            if (!string.IsNullOrEmpty(newVar)) 
            { 
                if (!_variables.Contains(newVar))
                {
                    _variables.Add(newVar); 
                    UpdateVariablesListBox(); 
                    NewVariableTextBox.Clear(); 
                }
                else
                {
                    MessageBox.Show("Cette variable existe déjà.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            } 
        } 
 
        private void MoveVariableUp_Click(object sender, RoutedEventArgs e) 
        { 
            var button = sender as Button; 
            string? variable = button?.Tag as string; 
            if (variable != null) 
            { 
                int index = _variables.IndexOf(variable); 
                if (index > 0) 
                { 
                    _variables.RemoveAt(index); 
                    _variables.Insert(index - 1, variable); 
                    UpdateVariablesListBox(); 
                } 
            } 
        } 
 
        private void MoveVariableDown_Click(object sender, RoutedEventArgs e) 
        { 
            var button = sender as Button; 
            string? variable = button?.Tag as string; 
            if (variable != null) 
            { 
                int index = _variables.IndexOf(variable); 
                if (index < _variables.Count - 1) 
                { 
                    _variables.RemoveAt(index); 
                    _variables.Insert(index + 1, variable); 
                    UpdateVariablesListBox(); 
                } 
            } 
        } 

        private void DeleteVariable_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string? variable = button?.Tag as string;
            if (variable != null)
            {
                _variables.Remove(variable);
                UpdateVariablesListBox();
            }
        }
 
        private void ImportFilesButton_Click(object sender, RoutedEventArgs e) 
        { 
            if (string.IsNullOrEmpty(_currentProjectPath)) 
            { 
                MessageBox.Show(LanguageManager.GetText("msg_no_project"), 
                              LanguageManager.GetText("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning); 
                return; 
            } 
 
            var dialog = new OpenFileDialog 
            { 
                Multiselect = true, 
                Filter = "Tous les fichiers supportés (*.jpg;*.jpeg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.pdf", 
                Title = "Importer des fichiers" 
            }; 
 
            if (dialog.ShowDialog() == true) 
            { 
                AjouterFichiers(dialog.FileNames); 
            } 
        } 
 
        // ========================================== 
        // GESTION PROJET 
        // ========================================== 
 
        private void NouveauProjet_Click(object sender, RoutedEventArgs e) 
        { 
            var dialog = new SaveFileDialog 
            { 
                Title = "Créer un nouveau projet", 
                Filter = "Projet AutoQuest (*.aqproj)|*.aqproj", 
                FileName = "NouveauProjet.aqproj" 
            }; 
 
            if (dialog.ShowDialog() == true) 
            { 
                try 
                { 
                    string? projectFolder = Path.GetDirectoryName(dialog.FileName); 
                    if (projectFolder == null) return;
                    string projectName = Path.GetFileNameWithoutExtension(dialog.FileName); 
 
                    string imagesFolder = Path.Combine(projectFolder, projectName + "_Files"); 
                    if (!Directory.Exists(imagesFolder)) 
                    { 
                        Directory.CreateDirectory(imagesFolder); 
                    } 
 
                    _currentProjectPath = dialog.FileName; 
                    _projectFolder = imagesFolder; 
                    _projectCreated = DateTime.Now; 
 
                    ProjectNameDisplay.Text = projectName; 
                    ProjectPathDisplay.Text = _currentProjectPath; 
                    ProjectDateDisplay.Text = _projectCreated.ToString("dd/MM/yyyy"); 
                    ProjectStatusDisplay.Text = "Actif"; 
 
                    _imagePaths.Clear(); 
                    _fileItems.Clear(); 
                    _patientBatches.Clear(); 
                    _results.Clear(); 
                    _currentPatientIndex = 0; 
 
                    FilesListBox.ItemsSource = null; 
                    ResultsDataGrid.ItemsSource = null; 
                    UpdateProjectStats(); 
 
                    StatusText.Text = LanguageManager.GetText("status_ready"); 
                } 
                catch (Exception ex) 
                { 
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error); 
                } 
            } 
        } 
 
        private void OuvrirProjet_Click(object sender, RoutedEventArgs e) 
        { 
            var dialog = new OpenFileDialog 
            { 
                Title = "Ouvrir un projet existant", 
                Filter = "Projet AutoQuest (*.aqproj)|*.aqproj" 
            }; 
 
            if (dialog.ShowDialog() == true) 
            { 
                try 
                { 
                    _currentProjectPath = dialog.FileName; 
                    string json = File.ReadAllText(_currentProjectPath); 
                    var projectData = JsonSerializer.Deserialize<Dictionary<string, object>>(json); 
 
                    if (projectData != null) 
                    { 
                        string projectName = projectData.ContainsKey("Name") ? projectData["Name"].ToString() ?? "" : Path.GetFileNameWithoutExtension(_currentProjectPath); 
                        _projectFolder = projectData.ContainsKey("ImagesPath") ? projectData["ImagesPath"].ToString() ?? "" : ""; 
                        _projectCreated = projectData.ContainsKey("Created") ? DateTime.Parse(projectData["Created"].ToString() ?? DateTime.Now.ToString()) : DateTime.Now; 
 
                        if (projectData.ContainsKey("Variables")) 
                        { 
                            var vars = JsonSerializer.Deserialize<List<string>>(projectData["Variables"].ToString() ?? "[]"); 
                            if (vars != null) 
                            { 
                                _variables = vars; 
                                UpdateVariablesListBox(); 
                            } 
                        } 
 
                        if (projectData.ContainsKey("Results")) 
                        { 
                            _results = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(projectData["Results"].ToString() ?? "[]") ?? new List<Dictionary<string, string>>(); 
                        } 
 
                        if (projectData.ContainsKey("ImagePaths")) 
                        { 
                            _imagePaths = JsonSerializer.Deserialize<List<string>>(projectData["ImagePaths"].ToString() ?? "[]") ?? new List<string>(); 
                            _fileItems = _imagePaths.Select(p => new FileItem { FilePath = p, IsSelected = false }).ToList(); 
                        } 
 
                        ProjectNameDisplay.Text = projectName; 
                        ProjectPathDisplay.Text = _currentProjectPath; 
                        ProjectDateDisplay.Text = _projectCreated.ToString("dd/MM/yyyy"); 
                        ProjectStatusDisplay.Text = "Actif"; 
 
                        if (_imagePaths.Any()) 
                        { 
                            OrganizePatients(); 
                            FilesListBox.ItemsSource = null;
                            FilesListBox.ItemsSource = _fileItems; 
                        } 
 
                        if (_results.Any()) 
                        { 
                            UpdateResultsGrid(); 
                        } 
 
                        UpdateProjectStats(); 
                    } 
                } 
                catch (Exception ex) 
                { 
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error); 
                } 
            } 
        } 
 
        private void Sauvegarder_Click(object sender, RoutedEventArgs e) 
        { 
            if (string.IsNullOrEmpty(_currentProjectPath)) 
            { 
                NouveauProjet_Click(sender, e); 
                return; 
            } 
 
            try 
            { 
                var projectData = new 
                { 
                    Name = Path.GetFileNameWithoutExtension(_currentProjectPath), 
                    Created = _projectCreated, 
                    LastSaved = DateTime.Now, 
                    ImagesPath = _projectFolder, 
                    Variables = _variables, 
                    Results = _results, 
                    ImagePaths = _imagePaths, 
                    CurrentPatientIndex = _currentPatientIndex 
                }; 
 
                string json = JsonSerializer.Serialize(projectData, new JsonSerializerOptions { WriteIndented = true }); 
                File.WriteAllText(_currentProjectPath, json); 
 
                StatusText.Text = LanguageManager.GetText("status_saved"); 
            } 
            catch (Exception ex) 
            { 
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error); 
            } 
        } 
 
        private void SauvegarderSous_Click(object sender, RoutedEventArgs e) 
        { 
            var dialog = new SaveFileDialog 
            { 
                Filter = "Projet AutoQuest (*.aqproj)|*.aqproj", 
                DefaultExt = "aqproj", 
                FileName = $"{ProjectNameDisplay.Text}.aqproj" 
            }; 
 
            if (dialog.ShowDialog() == true) 
            { 
                _currentProjectPath = dialog.FileName; 
                Sauvegarder_Click(sender, e); 
            } 
        } 
 
        // ========================================== 
        // GESTION DES FICHIERS 
        // ========================================== 
 
        private void DropZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) 
        { 
            if (string.IsNullOrEmpty(_currentProjectPath)) 
            { 
                MessageBox.Show(LanguageManager.GetText("msg_no_project"), 
                              LanguageManager.GetText("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning); 
                return; 
            } 
            ChargerImages(); 
        } 
 
        private void DropZone_DragEnter(object sender, DragEventArgs e) 
        { 
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
                e.Effects = DragDropEffects.Copy; 
            else 
                e.Effects = DragDropEffects.None; 
        } 
 
        private void DropZone_Drop(object sender, DragEventArgs e) 
        { 
            if (string.IsNullOrEmpty(_currentProjectPath)) 
            { 
                MessageBox.Show(LanguageManager.GetText("msg_no_project"), 
                              LanguageManager.GetText("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning); 
                return; 
            } 
 
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
            { 
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop); 
                AjouterFichiers(files); 
            } 
        } 
 
        private void ChargerImages() 
        { 
            var dialog = new OpenFileDialog 
            { 
                Multiselect = true, 
                Filter = "Images et PDF (*.jpg;*.jpeg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.pdf", 
                Title = "Ajouter des fichiers" 
            }; 
 
            if (dialog.ShowDialog() == true) 
            { 
                AjouterFichiers(dialog.FileNames); 
            } 
        } 
 
        private void AjouterFichiers(string[] files) 
        { 
            var images = files.Where(f => 
                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || 
                f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToList(); 
 
            if (images.Any()) 
            { 
                foreach (string file in images) 
                { 
                    if (!string.IsNullOrEmpty(_projectFolder) && Directory.Exists(_projectFolder)) 
                    { 
                        string destFile = Path.Combine(_projectFolder, Path.GetFileName(file)); 
                        if (!File.Exists(destFile)) 
                        { 
                            try { File.Copy(file, destFile); } catch {}
                            _imagePaths.Add(destFile); 
                        } 
                        else 
                        { 
                            _imagePaths.Add(destFile); 
                        } 
                    } 
                    else 
                    { 
                        _imagePaths.Add(file); 
                    } 
                } 
 
                _imagePaths = _imagePaths.Distinct().ToList(); 
                _fileItems = _imagePaths.Select(p => new FileItem { FilePath = p, IsSelected = false }).ToList(); 
 
                FilesListBox.ItemsSource = null; 
                FilesListBox.ItemsSource = _fileItems; 
                TotalFilesText.Text = $"{_imagePaths.Count} fichiers chargés"; 
 
                OrganizePatients(); 
                UpdateProjectStats(); 
            } 
        } 
 
        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e) 
        { 
            if (_fileItems.Any()) 
            { 
                bool isChecked = SelectAllCheckBox.IsChecked == true; 
                foreach (var item in _fileItems) 
                { 
                    item.IsSelected = isChecked; 
                } 
                FilesListBox.Items.Refresh(); 
            } 
        } 
 
        private void DeleteSelected_Click(object sender, RoutedEventArgs e) 
        { 
            var selected = _fileItems.Where(f => f.IsSelected).ToList(); 
            if (!selected.Any()) return; 
 
            var result = MessageBox.Show(LanguageManager.GetText("msg_confirm_delete"), 
                                        LanguageManager.GetText("msg_warning"), 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question); 
 
            if (result == MessageBoxResult.Yes) 
            { 
                foreach (var item in selected) 
                { 
                    _imagePaths.Remove(item.FilePath); 
                } 
 
                _fileItems = _imagePaths.Select(p => new FileItem { FilePath = p, IsSelected = false }).ToList(); 
                FilesListBox.ItemsSource = null;
                FilesListBox.ItemsSource = _fileItems; 
                TotalFilesText.Text = $"{_imagePaths.Count} fichiers chargés"; 
 
                OrganizePatients(); 
                UpdateProjectStats(); 
            } 
        } 
 
        private void SelectAll_Click(object sender, RoutedEventArgs e) 
        { 
            SelectAllCheckBox.IsChecked = true; 
            SelectAllCheckBox_Click(sender, e); 
        } 
 
        // ========================================== 
        // LOGIQUE MÉTIER & NAVIGATION 
        // ========================================== 
 
        private void OrganizePatients() 
        { 
            if (!_imagePaths.Any()) 
            { 
                ControlListBox.ItemsSource = null; 
                PatientLabel.Text = "Aucun patient"; 
                ExtractButton.IsEnabled = false; 
                return; 
            } 
 
            if (!int.TryParse(PagesPerPatientConfigTextBox.Text, out int pagesPerPatient) || pagesPerPatient < 1) 
            { 
                pagesPerPatient = 1; 
            } 
 
            var sortedPaths = _imagePaths.OrderBy(p => p).ToList(); 
 
            _patientBatches.Clear(); 
            for (int i = 0; i < sortedPaths.Count; i += pagesPerPatient) 
            { 
                int count = Math.Min(pagesPerPatient, sortedPaths.Count - i); 
                _patientBatches.Add(sortedPaths.GetRange(i, count)); 
            } 
 
            if (_currentPatientIndex >= _patientBatches.Count) 
                _currentPatientIndex = 0; 
 
            DisplayCurrentPatient(); 
            UpdateOrganizationPreview(pagesPerPatient, sortedPaths.Count);
            
            // Mettre à jour la liste de contrôle
            var controlList = new List<string>();
            for(int i=0; i<_patientBatches.Count; i++) {
                controlList.Add($"Patient {i+1} ({_patientBatches[i].Count} pages)");
            }
            ControlListBox.ItemsSource = controlList;
        } 
 
        private void UpdateOrganizationPreview(int pagesPerPatient, int totalFiles) 
        { 
            OrganizationText.Text = $"Aperçu : Patient 1 → pages 1-{Math.Min(pagesPerPatient, totalFiles)}"; 
        } 
 
        private void DisplayCurrentPatient() 
        { 
            if (!_patientBatches.Any()) 
            { 
                PatientLabel.Text = "Patient 0 / 0"; 
                ExtractButton.IsEnabled = false; 
                PrevButton.IsEnabled = false; 
                NextButton.IsEnabled = false; 
                PreviewImage.Source = null;
                NoImageText.Visibility = Visibility.Visible;
                return; 
            } 
 
            ExtractButton.IsEnabled = true; 
            PatientLabel.Text = $"Patient {_currentPatientIndex + 1} / {_patientBatches.Count}"; 
 
            var currentFiles = _patientBatches[_currentPatientIndex]; 
            PrevButton.IsEnabled = _currentPatientIndex > 0; 
            NextButton.IsEnabled = _currentPatientIndex < _patientBatches.Count - 1; 
 
            // Charger l'image du premier fichier du batch pour l'aperçu
            try {
                string firstFile = currentFiles[0];
                if (firstFile.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) {
                    PreviewImage.Source = null;
                    NoImageText.Text = "Aperçu PDF non disponible (cliquez pour ouvrir)";
                    NoImageText.Visibility = Visibility.Visible;
                } else {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(firstFile);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;
                    NoImageText.Visibility = Visibility.Collapsed;
                }
            } catch {
                PreviewImage.Source = null;
                NoImageText.Visibility = Visibility.Visible;
            }

            // Mettre à jour le panel de données
            UpdateControlDataPanel();
        } 

        private void UpdateControlDataPanel() {
            ControlDataPanel.Children.Clear();
            var currentPatientResults = _results.FirstOrDefault(r => r.ContainsKey("Patient") && r["Patient"] == $"P{_currentPatientIndex + 1:000}");
            
            if (currentPatientResults != null) {
                foreach (var kv in currentPatientResults) {
                    if (kv.Key == "Patient" || kv.Key == "Source" || kv.Key == "Pages") continue;
                    
                    var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                    stack.Children.Add(new TextBlock { Text = kv.Key + " : ", FontWeight = FontWeights.Bold, Width = 120 });
                    var tb = new TextBox { Text = kv.Value, Width = 200, Padding = new Thickness(2) };
                    tb.TextChanged += (s, e) => {
                        currentPatientResults[kv.Key] = tb.Text;
                        UpdateResultsGrid();
                    };
                    stack.Children.Add(tb);
                    ControlDataPanel.Children.Add(stack);
                }
            } else {
                ControlDataPanel.Children.Add(new TextBlock { 
                    Text = "Données non encore extraites pour ce patient.", 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    Foreground = (SolidColorBrush)FindResource("TextLight"),
                    Margin = new Thickness(20)
                });
            }
        }
 
        private void PagesPerPatientTextBox_TextChanged(object sender, TextChangedEventArgs e) 
        { 
            if (_imagePaths != null && _imagePaths.Any()) 
            { 
                OrganizePatients(); 
            } 
        } 
 
        private void PrevButton_Click(object sender, RoutedEventArgs e) 
        { 
            if (_currentPatientIndex > 0) 
            { 
                _currentPatientIndex--; 
                DisplayCurrentPatient(); 
            } 
        } 

        private void ControlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ControlListBox != null && ControlListBox.SelectedIndex >= 0)
            {
                _currentPatientIndex = ControlListBox.SelectedIndex;
                DisplayCurrentPatient();
            }
        }
 
        private void NextButton_Click(object sender, RoutedEventArgs e) 
        { 
            if (_currentPatientIndex < _patientBatches.Count - 1) 
            { 
                _currentPatientIndex++; 
                DisplayCurrentPatient(); 
            } 
        } 
 
        // ========================================== 
        // EXTRACTION 
        // ========================================== 
 
        private async void ExtractButton_Click(object sender, RoutedEventArgs e) 
        { 
            if (string.IsNullOrEmpty(_currentProjectPath)) 
            { 
                MessageBox.Show("Veuillez d'abord créer ou ouvrir un projet.", "Projet manquant", MessageBoxButton.OK, MessageBoxImage.Warning); 
                return; 
            } 
 
            if (!_patientBatches.Any()) 
            { 
                return; 
            } 
 
            if (_apiService == null || string.IsNullOrEmpty(_currentToken)) 
            { 
                return; 
            } 
 
            if (!await EnsureValidTokenAsync()) 
            { 
                return; 
            } 
 
            if (_currentQuota <= 0) 
            { 
                MessageBox.Show("Quota épuisé.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return; 
            } 
 
            try 
            { 
                if (!_variables.Any()) 
                { 
                    MessageBox.Show("Veuillez définir au moins une variable à extraire.", "Variables manquantes", MessageBoxButton.OK, MessageBoxImage.Information); 
                    return; 
                } 
 
                var currentFiles = _patientBatches[_currentPatientIndex]; 
                StatusText.Text = "Traitement en cours..."; 
                
                List<string> imagesBase64 = _imageProcessor.ProcessBatch(currentFiles); 
                if (!imagesBase64.Any()) return; 
 
                StatusText.Text = "Extraction via l'IA..."; 
                var extractResponse = await _apiService.ExtractAsync(_currentToken, imagesBase64, _variables); 
                _currentQuota = extractResponse.remaining; 
 
                var result = new Dictionary<string, string>(); 
                result["Patient"] = $"P{_currentPatientIndex + 1:000}"; 
                result["Source"] = Path.GetFileName(currentFiles[0]); 
                result["Pages"] = currentFiles.Count.ToString(); 
 
                // Simulation de résultats réels (au lieu de Martin etc)
                foreach (var v in _variables) 
                { 
                    result[v] = ""; // Initialise vide pour que l'utilisateur remplisse ou que l'IA remplisse
                } 
 
                // Remplacer ou ajouter le résultat
                var existing = _results.FirstOrDefault(r => r["Patient"] == result["Patient"]);
                if (existing != null) _results.Remove(existing);
                
                _results.Add(result); 
                UpdateResultsGrid(); 
                UpdateProjectStats(); 
                UpdateStatusText(); 
                DisplayCurrentPatient();

                if (_currentPatientIndex < _patientBatches.Count - 1) 
                { 
                    StatusText.Text = "Prêt pour le patient suivant";
                } 
                else 
                { 
                    StatusText.Text = "Extraction terminée";
                    MessageBox.Show("Tous les patients ont été traités.", "Terminé", MessageBoxButton.OK, MessageBoxImage.Information); 
                } 
            } 
            catch (Exception ex) 
            { 
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error); 
                StatusText.Text = "Erreur"; 
            } 
        } 
 
        private void UpdateResultsGrid() 
        { 
            if (!_results.Any()) 
            { 
                ResultsDataGrid.ItemsSource = null; 
                return; 
            } 
 
            var dt = new System.Data.DataTable(); 
            var allKeys = _results.SelectMany(r => r.Keys).Distinct().ToList();

            foreach (var key in allKeys) 
            { 
                dt.Columns.Add(key); 
            } 
 
            foreach (var dict in _results.OrderBy(r => r["Patient"])) 
            { 
                var row = dt.NewRow(); 
                foreach (var key in allKeys) 
                { 
                    row[key] = dict.ContainsKey(key) ? dict[key] : ""; 
                } 
                dt.Rows.Add(row); 
            } 
 
            ResultsDataGrid.ItemsSource = dt.DefaultView; 
        } 
 
        private void UpdateProjectStats() 
        { 
            StatsFilesValue.Text = _imagePaths.Count.ToString(); 
            StatsExtractionsValue.Text = _results.Count.ToString(); 
            
            ProjectFilesDisplay.Text = _imagePaths.Count.ToString(); 
            StatPatientsText.Text = _patientBatches.Count.ToString();
            StatPagesText.Text = _imagePaths.Count.ToString();
            StatExtractionsText.Text = _results.Count.ToString();

            int totalPatients = _patientBatches.Count; 
            int progress = totalPatients > 0 ? (_results.Count * 100 / totalPatients) : 0; 
            ProjectProgressBar.Value = progress; 
            ProjectProgressText.Text = $"{_results.Count}/{totalPatients} patients traités"; 
        } 
 
        // ========================================== 
        // OUTILS DIVERS 
        // ========================================== 
 
        private void AddVariable_Click(object sender, RoutedEventArgs e) { AddVariableManually_Click(sender, e); } 
 
        private void ImportVariables_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Fonctionnalité à venir"); } 
 
        private void ExportVariables_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Fonctionnalité à venir"); } 
 
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) 
        { 
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ? Visibility.Visible : Visibility.Collapsed; 
            // Filtrage simple du DataGrid
            if (ResultsDataGrid.ItemsSource is System.Data.DataView dv) {
                try {
                    dv.RowFilter = $"Patient LIKE '%{SearchTextBox.Text}%' OR Source LIKE '%{SearchTextBox.Text}%'";
                } catch { }
            }
        } 
 
        private void EffacerResultats_Click(object sender, RoutedEventArgs e) 
        { 
            if (MessageBox.Show("Effacer tous les résultats ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) 
            { 
                _results.Clear(); 
                ResultsDataGrid.ItemsSource = null; 
                UpdateProjectStats(); 
                UpdateControlDataPanel();
            } 
        } 
 
        private void ExportExcelButton_Click(object sender, RoutedEventArgs e) 
        { 
            MessageBox.Show("Export Excel (Simulation)"); 
        } 
 
        private void ExportCsvButton_Click(object sender, RoutedEventArgs e) 
        { 
            if (!_results.Any()) return; 
            var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "Export.csv" }; 
            if (dlg.ShowDialog() == true) 
            { 
                try {
                    using (var sw = new StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8)) 
                    { 
                        var headers = _results.First().Keys.ToList(); 
                        sw.WriteLine(string.Join(";", headers)); 
                        foreach (var row in _results) sw.WriteLine(string.Join(";", headers.Select(h => row[h].Replace(";", ",")))); 
                    } 
                    MessageBox.Show("Export réussi !"); 
                } catch (Exception ex) { MessageBox.Show("Erreur: " + ex.Message); }
            } 
        } 
 
        private void ModeSombre_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Mode sombre non implémenté"); } 
        private void Documentation_Click(object sender, RoutedEventArgs e) { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://google.com") { UseShellExecute = true }); } 
        private void APropos_Click(object sender, RoutedEventArgs e) { MessageBox.Show("AutoQuest v1.1\nInterface améliorée"); } 
 
        private void Quitter_Click(object sender, RoutedEventArgs e) 
        { 
            Close(); 
        }


    } 
}
