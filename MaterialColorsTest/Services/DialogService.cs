using System.Windows;
//using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Media = System.Windows.Media;
using WinForms = System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Security.Cryptography.X509Certificates;


namespace MaterialColorsTest.Services
{

    // Интерфейс делаю для создания абстракции. Что бы поддержать MVVM


    public interface IDialogService
    {
        Media.Color? PickColor(Media.Color assignedcolor); // ? Чтобы не вылетало при отмене

        void ShowError(string message);

        void ShowInfo(string message);    
    }


    public class DialogService : IDialogService
    {
        public Media.Color? PickColor(Media.Color assignedcolor)
        {

            using (var dialog = new WinForms.ColorDialog   // юзинг - концентрируем ресурсы а потом их освобождаем
            {
                FullOpen = true,
                Color = System.Drawing.Color.FromArgb(assignedcolor.R, assignedcolor.G, assignedcolor.B)

            })

            {
                if (dialog.ShowDialog() != WinForms.DialogResult.OK)
                    return null;

                return Media.Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);

            }
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Цвета материалов не годятся - Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);                         
        }


        public void ShowInfo(string message)
        {
            MessageBox.Show(message, "Цвета материалов",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
