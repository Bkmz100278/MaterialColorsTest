using Autodesk.Revit.UI;
using MaterialColorsTest.Buttons;
using System;

namespace MaterialColorsTest
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                AddButtons.RunButtons(app);
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Цвета материалов — ошибка загрузки", exception.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }
    }
}
