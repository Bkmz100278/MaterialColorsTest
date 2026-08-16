using Autodesk.Revit.DB;
using MaterialColorsTest.Models;
using Media = System.Windows.Media;


namespace MaterialColorsTest.ViewModels
{
    public class MaterialItemViewModel : ViewModelBase
    {
        private Media.Color _newColor;

        public ElementId Id { get; }

        public string Name { get; }

        public Media.Color OriginalColor { get; }

        public MaterialItemViewModel(MaterialModel model)
        {
            Id = model.Id;  
            Name = model.Name;

            OriginalColor = model.Color.IsValid 
                ? Media.Color.FromRgb(model.Color.Red , model.Color.Green , model.Color.Blue)
                : Media.Colors.White;

            _newColor = OriginalColor;

        }

        public Media.Color NewColor
        {
            get => _newColor;

            set 
            {
                if (Set(ref _newColor, value))
                {
                    OnPropertyChanged(nameof(NewBrush));
                    OnPropertyChanged(nameof(IsModified));
                }            
            }       
        }

        public Media.Brush OriginalBrush => new Media.SolidColorBrush(OriginalColor);

        public Media.Brush NewBrush => new Media.SolidColorBrush(_newColor);
            
        public bool IsModified => _newColor != OriginalColor;

        public Color ToRevitColor() => new Color(_newColor.R, _newColor.G, _newColor.B);

    }
}
