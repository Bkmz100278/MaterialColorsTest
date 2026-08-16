using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MaterialColorsTest.Services;
using MaterialColorsTest.ViewModels;
using MaterialColorsTest.Views;
using System;
using System.Windows.Interop;

namespace MaterialColorsTest.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MaterialColorsCommand : IExternalCommand
    {        
        private static bool _elemWinActive;

        public Result Execute(ExternalCommandData commandData,
        ref string message, ElementSet elements)
        {
            
            var appInstance = commandData.Application;
            var currentDoc = appInstance.ActiveUIDocument?.Document;

            if (currentDoc == null)
            {
                TaskDialog.Show("Цвета материалов", "Нет открытого проекта.");
                return Result.Cancelled;
            }

            var matService = new RevitMaterialService(currentDoc);
            var dlgService = new DialogService();
            var mainVm = new MainViewModel(matService, dlgService);

           
            while (true)
            {
                mainVm.PickRequested = false;

                var mainWindow = new MainWindow { DataContext = mainVm };
                new WindowInteropHelper(mainWindow) { Owner = appInstance.MainWindowHandle };

                EventHandler onClosed = (sender, args) => mainWindow.Close();
                mainVm.CloseRequested += onClosed;
                mainWindow.ShowDialog();
                mainVm.CloseRequested -= onClosed;

                if (!mainVm.PickRequested)
                    break;

                OpenElementMaterialView(appInstance, matService, dlgService);

                
                mainVm.Reload();
            }

            return Result.Succeeded;
        }
       

        private void OpenElementMaterialView(UIApplication appInstance,
        RevitMaterialService matService, IDialogService dlgService)
        {
            var activeDoc = appInstance.ActiveUIDocument;

            Reference chosenRef;
            try
            {
                chosenRef = activeDoc.Selection.PickObject(
                ObjectType.Element, "Выберите элемент");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return; // User pressed Esc — simply return to the main window.
            }

            var targetElement = activeDoc.Document.GetElement(chosenRef);
            var dataRows = matService.GetElementMaterials(targetElement);

            if (dataRows.Count == 0)
            {
                dlgService.ShowInfo("У выбранного элемента не найдено материалов.");
                return;
            }

            if (_elemWinActive)
            {
                dlgService.ShowInfo("Окно материалов элемента уже открыто.");
                return;
            }

            _elemWinActive = true;

            try
            {
                var elementVm = new ElementMaterialsViewModel(
                matService, dlgService, dataRows);

                var elementWindow = new ElementMaterialsWindow { DataContext = elementVm };
                new WindowInteropHelper(elementWindow) { Owner = appInstance.MainWindowHandle };

                EventHandler onWindowClose = (sender, args) => elementWindow.Close();
                elementVm.CloseRequested += onWindowClose;
                elementWindow.ShowDialog();
                elementVm.CloseRequested -= onWindowClose;
            }
            finally
            {
                _elemWinActive = false;
            }
        }
    }
}
