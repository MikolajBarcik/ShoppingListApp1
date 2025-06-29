using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoppingListApp.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace ShoppingListApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private const string SAVE_FILE_PATH = "shopping_list.json";

    [ObservableProperty]
    private string newItemName = string.Empty;

    [ObservableProperty]
    private string duplicateMessage = string.Empty;

    [ObservableProperty]
    private bool isListCreated = false;

    [ObservableProperty]
    private bool canCreateNewList = true;

    [ObservableProperty]
    private string saveFileName = string.Empty;

    [ObservableProperty]
    private bool showMainMenu = true;

    [ObservableProperty]
    private bool canLoadList = true;

    [ObservableProperty]
    private List<string> availableLists = new();

    [ObservableProperty]
    private bool showLoadMenu = false;

    [ObservableProperty]
    private bool showDeleteConfirmation = false;

    [ObservableProperty]
    private string deleteConfirmationMessage = "";

    [ObservableProperty]
    private string listToDelete = "";

    public ObservableCollection<ShoppingItem> Items { get; set; } = new();

    public MainWindowViewModel()
    {
        // Aplikacja startuje bez wczytywania - użytkownik wybiera co chce zrobić
    }

    [RelayCommand]
    private void AddItem()
    {
        if (!string.IsNullOrWhiteSpace(NewItemName))
        {
            string trimmedName = NewItemName.Trim();
            
            if (Items.Any(item => item.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                DuplicateMessage = "Ten artykuł już istnieje na liście.";
                return;
            }

            var newItem = new ShoppingItem { Name = trimmedName };
            newItem.PropertyChanged += OnItemPropertyChanged;
            Items.Add(newItem);
            NewItemName = string.Empty;
            DuplicateMessage = string.Empty;
            CanCreateNewList = false;
            CanLoadList = false;
            
            SortItems();
            SaveShoppingList();
        }
    }

    [RelayCommand]
    private void RemoveItem(ShoppingItem item)
    {
        if (item != null)
        {
            Items.Remove(item);
            if (Items.Count == 0)
            {
                CanCreateNewList = true;
                CanLoadList = true;
            }
            SaveShoppingList();
        }
    }

    [RelayCommand]
    private void CreateNewList()
    {
        Items.Clear();
        NewItemName = string.Empty;
        DuplicateMessage = string.Empty;
        IsListCreated = true;
        CanCreateNewList = true;
        CanLoadList = true;
        SaveShoppingList();
    }

    [RelayCommand]
    private void Close()
    {
        SaveShoppingList();
        Environment.Exit(0);
    }

    [RelayCommand]
    private void SaveList()
    {
        if (!string.IsNullOrWhiteSpace(SaveFileName))
        {
            SaveShoppingList(SaveFileName.Trim() + ".json");
        }
        else
        {
            SaveShoppingList();
        }
    }

    [RelayCommand]
    private void LoadList()
    {
        AvailableLists = GetSavedLists();
        if (AvailableLists.Count > 0)
        {
            ShowLoadMenu = true;
        }
        else
        {
            // Brak zapisanych list - wczytaj domyślną
            LoadShoppingList();
            if (Items.Count > 0)
            {
                IsListCreated = true;
                CanCreateNewList = false;
                CanLoadList = false;
            }
            else
            {
                IsListCreated = true;
                CanCreateNewList = false;
                CanLoadList = true;
            }
            ShowMainMenu = false;
        }
    }

    [RelayCommand]
    private void StartNewList()
    {
        ShowMainMenu = false;
        IsListCreated = true;
        CanCreateNewList = true;
    }

    [RelayCommand]
    private void LoadExistingList()
    {
        AvailableLists = GetSavedLists();
        if (AvailableLists.Count > 0)
        {
            ShowLoadMenu = true;
            ShowMainMenu = false;
        }
        else
        {
            // Brak zapisanych list - wczytaj domyślną
            LoadShoppingList();
            if (Items.Count > 0)
            {
                IsListCreated = true;
                CanCreateNewList = false;
                CanLoadList = false;
            }
            else
            {
                IsListCreated = true;
                CanCreateNewList = true;
                CanLoadList = true;
            }
            ShowMainMenu = false;
        }
    }

    [RelayCommand]
    private void LoadSpecificList(string fileName)
    {
        LoadSpecificListInternal(fileName);
        ShowLoadMenu = false;
        ShowMainMenu = false;
        CanCreateNewList = false;
    }

    [RelayCommand]
    private void BackToMainMenu()
    {
        ShowLoadMenu = false;
        ShowMainMenu = true;
    }

    [RelayCommand]
    private void DeleteList(string fileName)
    {
        ListToDelete = fileName;
        DeleteConfirmationMessage = $"Czy na pewno chcesz usunąć listę '{fileName}'? Ta operacja nie może być cofnięta.";
        ShowDeleteConfirmation = true;
        ShowLoadMenu = false;
    }

    [RelayCommand]
    private void ConfirmDelete()
    {
        DeleteSavedList(ListToDelete);
        ShowDeleteConfirmation = false;
        ShowLoadMenu = true;
        ListToDelete = "";
        DeleteConfirmationMessage = "";
    }

    [RelayCommand]
    private void CancelDelete()
    {
        ShowDeleteConfirmation = false;
        ShowLoadMenu = true;
        ListToDelete = "";
        DeleteConfirmationMessage = "";
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShoppingItem.IsBought))
        {
            SortItems();
            SaveShoppingList();
        }
    }

    private void SortItems()
    {
        var sortedItems = Items.OrderBy(item => item.IsBought).ToList();
        
        Items.Clear();
        foreach (var item in sortedItems)
        {
            Items.Add(item);
        }
    }

    private void SaveShoppingList(string? fileName = null)
    {
        try
        {
            var data = new
            {
                Items = Items.ToList(),
                IsListCreated = IsListCreated
            };
            
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string filePath = fileName ?? SAVE_FILE_PATH;
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd zapisu: {ex.Message}");
        }
    }

    private void LoadShoppingList()
    {
        try
        {
            if (File.Exists(SAVE_FILE_PATH))
            {
                string json = File.ReadAllText(SAVE_FILE_PATH);
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                
                if (data?.Items != null)
                {
                    foreach (var itemData in data.Items)
                    {
                        var item = new ShoppingItem 
                        { 
                            Name = itemData.Name.ToString(),
                            IsBought = itemData.IsBought
                        };
                        item.PropertyChanged += OnItemPropertyChanged;
                        Items.Add(item);
                    }
                }
                
                if (data?.IsListCreated != null)
                {
                    IsListCreated = data.IsListCreated;
                }
                
                if (Items.Count > 0)
                {
                    CanCreateNewList = false;
                    SortItems();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd wczytywania: {ex.Message}");
        }
    }

    private List<string> GetSavedLists()
    {
        var savedLists = new List<string>();
        try
        {
            var files = Directory.GetFiles(".", "*.json");
            foreach (var file in files)
            {
                // Sprawdź, czy plik zawiera pole 'Items' (czyli jest listą zakupów)
                try
                {
                    string json = File.ReadAllText(file);
                    if (json.Contains("\"Items\""))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            savedLists.Add(fileName);
                        }
                    }
                }
                catch { /* ignoruj błędy pojedynczych plików */ }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd pobierania list: {ex.Message}");
        }
        return savedLists;
    }

    private void LoadSpecificListInternal(string fileName)
    {
        try
        {
            string filePath = fileName + ".json";
            if (File.Exists(filePath))
            {
                Items.Clear();
                string json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                
                if (data?.Items != null)
                {
                    foreach (var itemData in data.Items)
                    {
                        var item = new ShoppingItem 
                        { 
                            Name = itemData.Name.ToString(),
                            IsBought = itemData.IsBought
                        };
                        item.PropertyChanged += OnItemPropertyChanged;
                        Items.Add(item);
                    }
                }
                
                if (data?.IsListCreated != null)
                {
                    IsListCreated = data.IsListCreated;
                }
                
                if (Items.Count > 0)
                {
                    CanCreateNewList = false;
                    CanLoadList = false;
                }
                else
                {
                    CanCreateNewList = true;
                    CanLoadList = true;
                }
                
                SortItems();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd wczytywania listy: {ex.Message}");
        }
    }

    private void DeleteSavedList(string fileName)
    {
        try
        {
            string filePath = fileName + ".json";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                // Odśwież listę dostępnych plików
                AvailableLists = GetSavedLists();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd usuwania listy: {ex.Message}");
        }
    }
}