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
using ClosedXML.Excel;
 
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
        private Dictionary<string, List<ExtractedField>> _patientExtractions = new();
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

        // Drag and drop for variables
        private Point _dragStartPoint;
        private string? _draggedItem;
 
        // Classe pour les fichiers avec sélection 
        public class FileItem 
        { 
            public string FilePath { get; set; } = "";
            public string FileName => System.IO.Path.GetFileName(FilePath); 
            public bool IsSelected { get; set; } 
        } 

        public class PageImageItem
        {
            public BitmapImage? ImageSource { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public int PageIndex { get; set; }
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
            // Onglets
            HomeTab.Header = LanguageManager.GetText("dashboard_tab");
            ConfigTab.Header = LanguageManager.GetText("config_tab");
            ExtractTab.Header = LanguageManager.GetText("extract_tab");
            ResultsTab.Header = LanguageManager.GetText("results_tab");

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
            AddVariableManuallyButton.Content = "➕ " + LanguageManager.GetText("config_manual_add");
            BulkAddVariablesButton.Content = "📥 " + LanguageManager.GetText("bulk_add_placeholder");
            PagesConfigTitle.Text = LanguageManager.GetText("pages_config"); 
 
            // Extraction 
            FileManagerTitle.Text = LanguageManager.GetText("file_manager"); 
            SelectAllCheckBox.Content = LanguageManager.GetText("select_all"); 
            DeleteSelectedButton.Content = "🗑️ " + LanguageManager.GetText("delete_selected"); 
            PrevButton.Content = "◀ " + LanguageManager.GetText("previous"); 
            NextButton.Content = LanguageManager.GetText("next") + " ▶"; 
            ExtractButton.Content = "🚀 " + LanguageManager.GetText("extract_button"); 
            ControlViewTitle.Text = "🔍 " + LanguageManager.GetText("control_verification_title");

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

        private void BulkAddVariables_Click(object sender, RoutedEventArgs e)
        {
            string text = BulkVariablesTextBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (var line in lines)
            {
                string varName = line.Trim();
                if (!string.IsNullOrEmpty(varName) && !_variables.Contains(varName))
                {
                    _variables.Add(varName);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                UpdateVariablesListBox();
                BulkVariablesTextBox.Clear();
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

        private void VariablesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            // Trouver l'item sur lequel on a cliqué
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var element = listBox.InputHitTest(e.GetPosition(listBox)) as DependencyObject;
            while (element != null && !(element is ListBoxItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is ListBoxItem item)
            {
                _draggedItem = item.Content as string;
            }
            else
            {
                _draggedItem = null;
            }
        }

        private void VariablesListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var listBox = sender as ListBox;
                    if (listBox == null) return;

                    DragDrop.DoDragDrop(listBox, _draggedItem, DragDropEffects.Move);
                }
            }
        }

        private void VariablesListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                string? droppedData = e.Data.GetData(typeof(string)) as string;
                if (droppedData == null) return;

                var listBox = sender as ListBox;
                if (listBox == null) return;

                // Trouver la position d'insertion
                var element = listBox.InputHitTest(e.GetPosition(listBox)) as DependencyObject;
                while (element != null && !(element is ListBoxItem))
                {
                    element = VisualTreeHelper.GetParent(element);
                }

                int targetIndex = -1;
                if (element is ListBoxItem item)
                {
                    targetIndex = listBox.ItemContainerGenerator.IndexFromContainer(item);
                }
                else
                {
                    targetIndex = _variables.Count - 1;
                }

                int oldIndex = _variables.IndexOf(droppedData);
                if (oldIndex != -1 && oldIndex != targetIndex)
                {
                    _variables.RemoveAt(oldIndex);
                    if (targetIndex > oldIndex) targetIndex--; // Ajustement car on a supprimé l'élément

                    if (targetIndex < 0) targetIndex = 0;
                    if (targetIndex >= _variables.Count) _variables.Add(droppedData);
                    else _variables.Insert(targetIndex, droppedData);

                    UpdateVariablesListBox();
                }
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
            if (_patientBatches == null || !_patientBatches.Any())
            {
                OrganizationText.Text = LanguageManager.GetText("overview") + " : " + LanguageManager.GetText("no_files");
                return;
            }

            var previewLines = new List<string>();
            string patientLabel = LanguageManager.GetText("patient");
            for (int i = 0; i < Math.Min(3, _patientBatches.Count); i++)
            {
                var files = _patientBatches[i].Select(p => Path.GetFileName(p));
                previewLines.Add($"{patientLabel} {i + 1} : {string.Join(", ", files)}");
            }

            if (_patientBatches.Count > 3)
            {
                previewLines.Add("...");
            }

            OrganizationText.Text = LanguageManager.GetText("overview") + " :\n" + string.Join("\n", previewLines);
        } 
 
        private void DisplayCurrentPatient() 
        { 
            if (!_patientBatches.Any()) 
            { 
                PatientLabel.Text = "Patient 0 / 0"; 
                ExtractButton.IsEnabled = false; 
                PrevButton.IsEnabled = false; 
                NextButton.IsEnabled = false; 
                MultiPagePreviewControl.ItemsSource = null;
                NoImageText.Visibility = Visibility.Visible;
                return; 
            } 
 
            ExtractButton.IsEnabled = true; 
            PatientLabel.Text = $"Patient {_currentPatientIndex + 1} / {_patientBatches.Count}"; 
 
            var currentFiles = _patientBatches[_currentPatientIndex]; 
            PrevButton.IsEnabled = _currentPatientIndex > 0; 
            NextButton.IsEnabled = _currentPatientIndex < _patientBatches.Count - 1; 
 
            var imageItems = new List<PageImageItem>();
            for (int i = 0; i < currentFiles.Count; i++)
            {
                try
                {
                    string file = currentFiles[i];
                    if (file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(file);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    imageItems.Add(new PageImageItem
                    {
                        ImageSource = bitmap,
                        Width = bitmap.PixelWidth,
                        Height = bitmap.PixelHeight,
                        PageIndex = i
                    });
                }
                catch { }
            }

            MultiPagePreviewControl.ItemsSource = imageItems;
            NoImageText.Visibility = imageItems.Any() ? Visibility.Collapsed : Visibility.Visible;
            if (!imageItems.Any() && currentFiles.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            {
                NoImageText.Text = "Aperçu PDF non disponible";
            }
            else
            {
                NoImageText.Text = "Aucun visuel disponible";
            }

            // Mettre à jour le panel de données
            UpdateControlDataPanel();

            // Dessiner les annotations après un court délai pour laisser le temps au layout de se faire
            Dispatcher.BeginInvoke(new Action(() => DrawAnnotations()), System.Windows.Threading.DispatcherPriority.Background);
        } 

        private void DrawAnnotations()
        {
            if (MultiPagePreviewControl == null) return;
            string patientId = $"P{_currentPatientIndex + 1:000}";
            if (!_patientExtractions.TryGetValue(patientId, out var fields)) return;

            for (int i = 0; i < MultiPagePreviewControl.Items.Count; i++)
            {
                var container = MultiPagePreviewControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                var canvas = FindVisualChild<Canvas>(container);
                if (canvas == null) continue;

                canvas.Children.Clear();
                var item = MultiPagePreviewControl.Items[i] as PageImageItem;
                if (item == null) continue;

                foreach (var field in fields)
                {
                    foreach (var ann in field.annotations)
                    {
                        if (ann.page_index == item.PageIndex && ann.box.Count == 4)
                        {
                            // [ymin, xmin, ymax, xmax] normalized 0-1000
                            double ymin = ann.box[0] * item.Height / 1000.0;
                            double xmin = ann.box[1] * item.Width / 1000.0;
                            double ymax = ann.box[2] * item.Height / 1000.0;
                            double xmax = ann.box[3] * item.Width / 1000.0;

                            var rect = new System.Windows.Shapes.Rectangle {
                                Stroke = Brushes.Red, StrokeThickness = 2,
                                Width = Math.Max(1, xmax - xmin), Height = Math.Max(1, ymax - ymin),
                                ToolTip = field.variable + " : " + field.value
                            };
                            Canvas.SetLeft(rect, xmin);
                            Canvas.SetTop(rect, ymin);
                            canvas.Children.Add(rect);
                        }
                    }
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
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
 
                // Utiliser les résultats de l'API s'ils existent
                if (extractResponse.fields != null && extractResponse.fields.Any())
                {
                    _patientExtractions[result["Patient"]] = extractResponse.fields;
                    foreach (var field in extractResponse.fields)
                    {
                        result[field.variable] = field.value;
                    }
                }
                else
                {
                    foreach (var v in _variables)
                    {
                        result[v] = "";
                    }
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
            if (!_results.Any())
            {
                MessageBox.Show(LanguageManager.GetText("msg_no_data"),
                              LanguageManager.GetText("msg_warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Export_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Résultats");

                        // En-têtes
                        var headers = _results.SelectMany(r => r.Keys).Distinct().ToList();
                        for (int i = 0; i < headers.Count; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                        }

                        // Données
                        for (int r = 0; r < _results.Count; r++)
                        {
                            var rowData = _results[r];
                            for (int c = 0; c < headers.Count; c++)
                            {
                                string header = headers[c];
                                if (rowData.ContainsKey(header))
                                {
                                    worksheet.Cell(r + 2, c + 1).Value = rowData[header];
                                }
                            }
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(dialog.FileName);
                    }
                    MessageBox.Show("Export Excel réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'export Excel : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
