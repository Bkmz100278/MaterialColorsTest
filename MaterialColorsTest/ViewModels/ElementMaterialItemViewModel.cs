using MaterialColorsTest.Models;
using MaterialColorsTest.ViewModels;

namespace MaterialColorsTest.ViewModels
{
    public class ElementMaterialItemViewModel : MaterialItemViewModel
    {
        public string FamilyName { get; }

        public ElementMaterialItemViewModel(ElementMaterialModel model) : base(model)
        {
            FamilyName = model.FamilyName;
        }
    }
}
