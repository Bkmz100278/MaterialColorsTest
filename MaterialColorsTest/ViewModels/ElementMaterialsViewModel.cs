using Autodesk.Revit.DB;
using MaterialColorsTest.Models;
using MaterialColorsTest.Services;
using MaterialColorsTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MaterialColorsTest.ViewModels
{
    public class ElementMaterialsViewModel : ViewModelBase
    {
        private readonly RevitMaterialService _materialService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<ElementMaterialItemViewModel> Materials { get; }

        public RelayCommand PickColorCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand ResetCommand { get; }

        public event EventHandler CloseRequested;

        public ElementMaterialsViewModel(
            RevitMaterialService materialService,
            IDialogService dialogService,
            IEnumerable<ElementMaterialModel> models)
        {
            _materialService = materialService;
            _dialogService = dialogService;

            Materials = new ObservableCollection<ElementMaterialItemViewModel>(
                models.Select(m => new ElementMaterialItemViewModel(m)));

            PickColorCommand = new RelayCommand(PickColor, p => p is MaterialItemViewModel);
            ApplyCommand = new RelayCommand(Apply, p => Materials.Any(m => m.IsModified));
            ResetCommand = new RelayCommand(Reset, p => Materials.Any(m => m.IsModified));
        }

        private void PickColor(object parameter)
        {
            try
            {
                var item = (MaterialItemViewModel)parameter;
                var selectedColor = _dialogService.PickColor(item.NewColor);

                if (selectedColor.HasValue)
                {
                    item.NewColor = selectedColor.Value;
                }
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
                // Один и тот же материал может встречаться в нескольких строках, 
                // поэтому цикл, в котором сохраняется последнее встреченное значение.

                var changes = new Dictionary<ElementId, Color>();

                foreach (var item in Materials.Where(m => m.IsModified))
                {
                    changes[item.Id] = item.ToRevitColor();
                }

                _materialService.ApplyColors(changes);

                _dialogService.ShowInfo($"Изменено материалов: {changes.Count}.");

                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
            }
        }

        private void Reset(object parameter)
        {
            foreach (var item in Materials)
            {
                item.NewColor = item.OriginalColor;
            }
        }
    }
}
