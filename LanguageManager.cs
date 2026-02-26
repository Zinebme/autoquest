using System;
using System.Collections.Generic;

namespace autoquest
{
    public static class LanguageManager
    {
        public static event EventHandler? LanguageChanged;
        private static AppLanguage _currentLanguage = AppLanguage.French;

        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> _translations = new()
        {
            ["file_menu"] = new() { [AppLanguage.English] = "File", [AppLanguage.French] = "Fichier" },
            ["new_project"] = new() { [AppLanguage.English] = "New Project", [AppLanguage.French] = "Nouveau projet" },
            ["open_project"] = new() { [AppLanguage.English] = "Open Project", [AppLanguage.French] = "Ouvrir un projet" },
            ["save_project"] = new() { [AppLanguage.English] = "Save", [AppLanguage.French] = "Sauvegarder" },
            ["save_as"] = new() { [AppLanguage.English] = "Save As...", [AppLanguage.French] = "Sauvegarder sous..." },
            ["export_menu"] = new() { [AppLanguage.English] = "Export", [AppLanguage.French] = "Exporter" },
            ["export_excel"] = new() { [AppLanguage.English] = "Excel", [AppLanguage.French] = "Excel" },
            ["export_csv"] = new() { [AppLanguage.English] = "CSV", [AppLanguage.French] = "CSV" },
            ["quit"] = new() { [AppLanguage.English] = "Quit", [AppLanguage.French] = "Quitter" },
            ["edit_menu"] = new() { [AppLanguage.English] = "Edit", [AppLanguage.French] = "Édition" },
            ["copy"] = new() { [AppLanguage.English] = "Copy", [AppLanguage.French] = "Copier" },
            ["paste"] = new() { [AppLanguage.English] = "Paste", [AppLanguage.French] = "Coller" },
            ["delete"] = new() { [AppLanguage.English] = "Delete", [AppLanguage.French] = "Supprimer" },
            ["select_all"] = new() { [AppLanguage.English] = "Select All", [AppLanguage.French] = "Tout sélectionner" },
            ["view_menu"] = new() { [AppLanguage.English] = "View", [AppLanguage.French] = "Affichage" },
            ["dark_mode"] = new() { [AppLanguage.English] = "Dark Mode", [AppLanguage.French] = "Mode sombre" },
            ["zoom_in"] = new() { [AppLanguage.English] = "Zoom In", [AppLanguage.French] = "Zoom avant" },
            ["zoom_out"] = new() { [AppLanguage.English] = "Zoom Out", [AppLanguage.French] = "Zoom arrière" },
            ["language_menu"] = new() { [AppLanguage.English] = "Language", [AppLanguage.French] = "Langue" },
            ["english"] = new() { [AppLanguage.English] = "English", [AppLanguage.French] = "English" },
            ["french"] = new() { [AppLanguage.English] = "French", [AppLanguage.French] = "Français" },
            ["help_menu"] = new() { [AppLanguage.English] = "Help", [AppLanguage.French] = "Aide" },
            ["documentation"] = new() { [AppLanguage.English] = "Documentation", [AppLanguage.French] = "Documentation" },
            ["about"] = new() { [AppLanguage.English] = "About", [AppLanguage.French] = "À propos" },
            ["project_info"] = new() { [AppLanguage.English] = "Current Project", [AppLanguage.French] = "Projet en cours" },
            ["quick_actions"] = new() { [AppLanguage.English] = "Quick Actions", [AppLanguage.French] = "Actions rapides" },
            ["statistics"] = new() { [AppLanguage.English] = "Statistics", [AppLanguage.French] = "Statistiques" },
            ["project_files"] = new() { [AppLanguage.English] = "Loaded Files", [AppLanguage.French] = "Fichiers chargés" },
            ["extractions"] = new() { [AppLanguage.English] = "Extractions", [AppLanguage.French] = "Extractions" },
            ["to_verify"] = new() { [AppLanguage.English] = "To Verify", [AppLanguage.French] = "À vérifier" },
            ["config_title"] = new() { [AppLanguage.English] = "Variables to Extract", [AppLanguage.French] = "Variables à extraire" },
            ["config_add"] = new() { [AppLanguage.English] = "Add", [AppLanguage.French] = "Ajouter" },
            ["pages_config"] = new() { [AppLanguage.English] = "Pages Organization", [AppLanguage.French] = "Organisation des pages" },
            ["file_manager"] = new() { [AppLanguage.English] = "File Manager", [AppLanguage.French] = "Gestionnaire de fichiers" },
            ["delete_selected"] = new() { [AppLanguage.English] = "Delete", [AppLanguage.French] = "Supprimer" },
            ["previous"] = new() { [AppLanguage.English] = "Previous", [AppLanguage.French] = "Précédent" },
            ["next"] = new() { [AppLanguage.English] = "Next", [AppLanguage.French] = "Suivant" },
            ["extract_button"] = new() { [AppLanguage.English] = "Extract Patient", [AppLanguage.French] = "Extraire ce patient" },
            ["results_search"] = new() { [AppLanguage.English] = "Search patient...", [AppLanguage.French] = "Rechercher un patient..." },
            ["results_clear"] = new() { [AppLanguage.English] = "Clear", [AppLanguage.French] = "Effacer" },
            ["status_connecting"] = new() { [AppLanguage.English] = "Connecting...", [AppLanguage.French] = "Connexion..." },
            ["status_ready"] = new() { [AppLanguage.English] = "Ready", [AppLanguage.French] = "Prêt" },
            ["status_error"] = new() { [AppLanguage.English] = "Connection Error", [AppLanguage.French] = "Erreur de connexion" },
            ["status_quota_empty"] = new() { [AppLanguage.English] = "Quota exceeded", [AppLanguage.French] = "Quota épuisé" },
            ["status_quota_low"] = new() { [AppLanguage.English] = "Low quota: {0}", [AppLanguage.French] = "Quota faible: {0}" },
            ["session_expired"] = new() { [AppLanguage.English] = "Session expired", [AppLanguage.French] = "Session expirée" },
            ["msg_warning"] = new() { [AppLanguage.English] = "Warning", [AppLanguage.French] = "Attention" },
            ["msg_error"] = new() { [AppLanguage.English] = "Error", [AppLanguage.French] = "Erreur" },
            ["msg_no_project"] = new() { [AppLanguage.English] = "No project opened", [AppLanguage.French] = "Aucun projet ouvert" },
            ["msg_confirm_delete"] = new() { [AppLanguage.English] = "Delete selected items?", [AppLanguage.French] = "Supprimer les éléments sélectionnés ?" },
            ["msg_confirm_clear"] = new() { [AppLanguage.English] = "Clear all results?", [AppLanguage.French] = "Effacer tous les résultats ?" },
            ["status_saved"] = new() { [AppLanguage.English] = "Project saved", [AppLanguage.French] = "Projet sauvegardé" }
        };

        public static string GetText(string key)
        {
            if (_translations.TryGetValue(key, out var translation))
            {
                if (translation.TryGetValue(_currentLanguage, out var text))
                    return text;
            }
            return key;
        }

        public static string GetFormattedText(string key, params object[] args)
        {
            return string.Format(GetText(key), args);
        }

        public static void SetLanguage(AppLanguage language)
        {
            _currentLanguage = language;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
