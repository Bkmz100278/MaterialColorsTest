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
