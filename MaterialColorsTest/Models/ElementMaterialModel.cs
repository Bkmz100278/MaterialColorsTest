using Autodesk.Revit.DB;


namespace MaterialColorsTest.Models
{
    /// <summary>
    ///  Материал у элемента , с именем семейства.
    /// </summary>

    // Объединили сущность цвета материала с именем семейства

    public class ElementMaterialModel : MaterialModel 
    {
        public string FamilyName { get; }

        public ElementMaterialModel(ElementId id, string name, Color color, string familyName) : base(id, name, color)

        { 
            FamilyName = familyName;
        }
    }
}
