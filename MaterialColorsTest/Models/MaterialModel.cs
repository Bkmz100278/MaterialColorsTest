using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace MaterialColorsTest.Models
{
    /// <summary>
    ///  Сущность данных самого материала.  
    /// </summary>


    // Связали Цвет id и название материала в единую сущность

    public class MaterialModel
    {
        public ElementId Id { get; }

        public string Name { get; }

        public Color Color { get; }

        public MaterialModel(ElementId id, string name, Color color)
        {
            Id = id;
            Name = name;
            Color = color;
        }
    }
}
