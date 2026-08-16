using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using MaterialColorsTest.Services;


namespace MaterialColorsTest.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly RevitMaterialService _materialService;
        private readonly IDialogService _dialogService;
        private string _searchText = string.Empty;

        public ObservableCollection<MaterialItemViewModel> Materials { get; }
        public ICollectionView MaterialsView { get; }

        public RelayCommand PickColorCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand ResetCommand { get; }
        public RelayCommand PickElementCommand { get; }

        /// <summary>Флаг для команды: пользователь запросил выбор элемента.</summary>
        public bool PickRequested { get; set; }

        /// <summary>Просьба закрыть окно (обрабатывается снаружи).</summary>
        public event EventHandler CloseRequested;

        public MainViewModel(RevitMaterialService materialService, IDialogService dialogService)
        {
            _materialService = materialService;
            _dialogService = dialogService;

            Materials = new ObservableCollection<MaterialItemViewModel>(
                _materialService.GetMaterials().Select(m => new MaterialItemViewModel(m)));

            MaterialsView = CollectionViewSource.GetDefaultView(Materials);
            MaterialsView.Filter = FilterMaterial;

            PickColorCommand = new RelayCommand(PickColor, p => p is MaterialItemViewModel);
            ApplyCommand = new RelayCommand(Apply, p => Materials.Any(m => m.IsModified));
            ResetCommand = new RelayCommand(Reset, p => Materials.Any(m => m.IsModified));
            PickElementCommand = new RelayCommand(PickElement);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                    MaterialsView.Refresh();
            }
        }

        public void Reload()
        {
            Materials.Clear();

            foreach (var model in _materialService.GetMaterials())
                Materials.Add(new MaterialItemViewModel(model));
        }

        private bool FilterMaterial(object obj)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            return obj is MaterialItemViewModel item &&
                   item.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void PickColor(object parameter)
        {
            try
            {
                var item = (MaterialItemViewModel)parameter;
                var picked = _dialogService.PickColor(item.NewColor);

                if (picked.HasValue)
                    item.NewColor = picked.Value;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
            }
        }

        private void Apply(object parameter)
        {
            try
            {
                var changes = Materials
                    .Where(m => m.IsModified)
                    .ToDictionary(m => m.Id, m => m.ToRevitColor());

                _materialService.ApplyColors(changes);

                _dialogService.ShowInfo($"Изменено материалов: {changes.Count}.");

                Reload();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
            }
        }

        private void Reset(object parameter)
        {
            foreach (var item in Materials)
                item.NewColor = item.OriginalColor;
        }

        private void PickElement(object parameter)
        {
            PickRequested = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
