using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace MaterialColorsTest.Buttons
{
    internal static class AddButtons
    {
        private const string TabName = "BIM АР";
        private const string PanelName = "Инструменты";

        public static void RunButtons(UIControlledApplication app)
        {
            RibbonPanel panel = GetOrCreatePanel(app);
            AddMaterialColorsButton(panel);
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication app)
        {
            // Вкладка могла быть создана другим плагином — ошибку глушим осознанно.
            try
            {
                app.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Вкладка уже существует.
            }

            // Ищем панель среди существующих на вкладке.
            List<RibbonPanel> panels = app.GetRibbonPanels(TabName);

            foreach (RibbonPanel p in panels)
            {
                if (p.Name == PanelName)
                    return p;
            }

            // Не нашли — создаём.
            return app.CreateRibbonPanel(TabName, PanelName);
        }

        private static PushButton AddMaterialColorsButton(RibbonPanel panel)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            var buttonData = new PushButtonData(
                "MaterialColorsButton",              // внутреннее имя (уникальное)
                "Цвета\nматериалов",                 // текст на кнопке
                assemblyPath,
                "MaterialColorsTest.Commands.MaterialColorsCommand");

            var button = (PushButton)panel.AddItem(buttonData);

            button.LargeImage = LoadImage("MaterialColors_32.png");
            button.Image = LoadImage("MaterialColors_16.png");

            button.ToolTip = "Управление цветами материалов проекта.";
            button.LongDescription =
                "Открывает таблицу всех материалов проекта с поиском по имени. " +
                "Для каждого материала можно выбрать новый цвет и применить изменения " +
                "одной транзакцией. Кнопка «Выбрать элемент» позволяет отобрать материалы " +
                "конкретного элемента модели, включая вложенные семейства и материалы, " +
                "назначенные по категории или подкатегории.";

            return button;
        }

        private static BitmapImage LoadImage(string fileName)
        {
            // Гарантируем, что схема pack:// зарегистрирована
            // (важно, пока Revit ещё не поднял WPF-инфраструктуру при старте).
            var _ = System.IO.Packaging.PackUriHelper.UriSchemePack;

            return new BitmapImage(new Uri(
                $"pack://application:,,,/MaterialColorsTest;component/Resources/{fileName}",
                UriKind.Absolute));
        }
    }
}
