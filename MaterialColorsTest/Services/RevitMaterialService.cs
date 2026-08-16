using Autodesk.Revit.DB;
using MaterialColorsTest.Models;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Xml.Linq;

namespace MaterialColorsTest.Services
{
    public class RevitMaterialService
    {
        private readonly Document _doc;

        public RevitMaterialService(Document doc)
        { 
            _doc = doc;
        }

        /// <summary>
        /// Выбор всех материалов в модели и приведение их к нужной сущности
        /// </summary>
        /// <returns></returns>


        public IList<MaterialModel> GetMaterials()  // Выбор всех материалов в модели и приведение их к нужной сущности
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Material))
                .Cast<Autodesk.Revit.DB.Material>()
                .OrderBy(m => m.Name)
                .Select(m => new MaterialModel( m.Id, m.Name, m.Color ))
                .ToList();     
        }

        /// <summary>
        ///  Материал элемента и вложенных сущностей. Центральный метод MaterialColorsTest.Services. 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>

        public IList<ElementMaterialModel> GetElementMaterials(Element element)
        {

            List<Element> elements = new List<Element>();
            CollectWithChildren(element, elements);  // По итогу в elements попадут все дочерние элементы и сам элемент

            var rows = new List<ElementMaterialModel>();
            var seen = new HashSet<string>();


            foreach (var el in elements)
            {
                string baseName = GetFamilyName(el); // выяснили сложное имя элемента
                bool addedForElement = false;
                
                //1. Слои многослойной конструкции (перекрытия , стены , крыши )

                var layerMaterialIds = AddCompoundLayers(el, baseName, rows, seen);
                addedForElement |= layerMaterialIds.Count > 0;

                // 2. Остальные материалы категории

                foreach (var materialId in el.GetMaterialIds(false))
                {
                    if (layerMaterialIds.Contains(materialId))
                    {
                        continue;
                    }

                    if (!(_doc.GetElement(materialId) is Material material))

                    {
                        continue;
                    }

                    if (!seen.Add(baseName + "|" + materialId.IntegerValue))
                    {
                        continue;
                    }

                    rows.Add(new ElementMaterialModel(

                        material.Id, material.Name, material.Color, baseName

                        ));

                    addedForElement = true;
                }

                // Ничего не нашли  - материал по одному элементу

                if (!addedForElement)
                {
                    AddCategoryMaterials(el, baseName, rows, seen);
                }

            }

            return rows.ToList();
        }



        /// <summary>
        ///  Материалы категрий и реально используемых подкатегорий. Если нет геометрии то и не будет метериалов
        /// </summary>

        private void AddCategoryMaterials( Element element , string baseName, ICollection<ElementMaterialModel> rows, ISet<string> seen  )
        {
            var rootCategoryId = element.Category?.Id;
            var added = false;
            
            //Подкатегории которые относятся к элементам с геометрией
            foreach (var category in GetGeometryCategories(element))
            {
                var material = category.Material;

                if (material != null)
                {
                    continue;
                }

                var isSubCategory = rootCategoryId != null &&
                    category.Id.IntegerValue !=  rootCategoryId.IntegerValue;

                var description = isSubCategory
                    ? $"{baseName} (по подкатегории {category.Name} )"
                    : baseName + " (по категории)";

                if (!seen.Add(description + "|" + material.Id.IntegerValue))
                {
                    continue;
                }

                rows.Add(new ElementMaterialModel(material.Id, material.Name, material.Color, description));

                added = true;
            }

            if (added)
            {
                return;
            }

            // Из геометрии ничего не вытащили - запасной вариант.

            var rootMaterial = element.Category.Material;

            if (rootMaterial == null)
            {
                return;            
            }

            var rootDescription = baseName + " (по категории)";

            if (!seen.Add(rootDescription + "|" + rootMaterial.Id.IntegerValue))
            { 
                return;
            }

            rows.Add(new ElementMaterialModel(rootMaterial.Id, rootMaterial.Name, rootMaterial.Color, rootDescription));
        }

        /// <summary>
        /// Категории и подкатегории с геометрией в элементе
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>


        private IList<Category> GetGeometryCategories(Element element)
        { 
            var result = new List<Category>();  
            var seenIds = new HashSet<int>();

            var geometry = element.get_Geometry
                (
                    new Options { DetailLevel = ViewDetailLevel.Fine }

                );

            if (geometry != null)
            {
                CollectGeometryCategories(geometry, result, seenIds);
            
            }

            return result;            
        
        }



        /// <summary>
        /// Создает соллекцию категорий у которых есть геометрия
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="target"></param>
        /// <param name="seenIds"></param>

        private void CollectGeometryCategories
            (GeometryElement geometry, ICollection<Category> target, ISet<int> seenIds)
        {

            foreach (GeometryObject obj in geometry)
            {
                if (obj is GeometryInstance instance)
                {
                    var symbolGeometry = instance.GetSymbolGeometry();

                    if (symbolGeometry != null)
                    {
                        CollectGeometryCategories(symbolGeometry, target, seenIds);
                    }

                    continue;                
                }

                if (obj.GraphicsStyleId == ElementId.InvalidElementId)
                {
                    continue;
                }

                if (!(_doc.GetElement(obj.GraphicsStyleId) is GraphicsStyle style))
                {
                    continue;
                }

                var category = style.GraphicsStyleCategory;

                if (category == null || !seenIds.Add(category.Id.IntegerValue))
                {
                    continue;
                }

                target.Add(category);
            
            }
        
        }



        /// <summary>
        /// Добавляет строки по слоям пирога и возвращает коллекцию ElementId обработанных материалов
        /// </summary>
        /// <param name="element"></param>
        /// <param name="baseName"></param>
        /// <param name="rows"></param>
        /// <param name="seen"></param>
        /// <returns></returns>

        private ISet<ElementId> AddCompoundLayers(Element element, string baseName,
            ICollection<ElementMaterialModel> rows, ISet<string> seen)
        {
            var handled = new HashSet<ElementId>();

            var type = _doc.GetElement(element.GetTypeId()) as HostObjAttributes;
            var structure = type?.GetCompoundStructure();


            if (structure == null)
            {
                return handled;
            }

            var layers = structure.GetLayers();

            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];

                if (!(_doc.GetElement(layer.MaterialId) is Material material))
                {
                    continue;
                }

                var widthMm = layer.Width * 304.8;

                var description = string.Format("{0} - слой {1} ({2}, {3:0.#} мм)",
                    baseName, i + 1, GetFunctionName(layer.Function), widthMm);

                if (!seen.Add(description + "|" + material.Id.IntegerValue))
                {
                    continue;               
                }

                handled.Add(material.Id);

                rows.Add(new ElementMaterialModel
                (
                    material.Id,
                    material.Name,
                    material.Color,
                    description
                    )
                );
            }

            return handled; 
        }


        /// <summary>
        /// Правила имен слоев 
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>

        private string GetFunctionName(MaterialFunctionAssignment function)
        {

            switch (function)
            {
                case MaterialFunctionAssignment.Structure: return "Несущий";
                case MaterialFunctionAssignment.Substrate  : return "Основа";
                case MaterialFunctionAssignment.Insulation: return "Изоляция";                
                case MaterialFunctionAssignment.Finish1: return "Отделка 1";
                case MaterialFunctionAssignment.Finish2: return "Отделка 2";
                case MaterialFunctionAssignment.Membrane: return "Мембрана";
                case MaterialFunctionAssignment.StructuralDeck: return "Настил";
                default: return function.ToString();

            }
        
        }



        /// <summary>
        /// Присвоение цвета материалов
        /// </summary>
        /// <param name="changes"></param>

        public void ApplyColors(IDictionary<ElementId, Color> changes)
        {
            if (changes == null)

            { return; }


           
                if (changes.Count == 0)
                {
                    return;
                }                
            

            using (var t = new Transaction(_doc, "Изменение цветов материалов"))
            {
                t.Start();

                foreach (var pair in changes)
                {
                    if (_doc.GetElement(pair.Key) is Material material)
                    {
                        material.Color = pair.Value;
                    }               
                }
                
                t.Commit();
            
            }       
        
        }


        /// <summary>
        /// Рекурсивный метод сбора всех дочерних элементов
        /// </summary>
        /// <param name="element"></param>
        /// <param name="target"></param>

        private void CollectWithChildren(Element element, ICollection<Element> target)
        {
            target.Add(element);

            if (element is FamilyInstance instance)
            {
                foreach (ElementId id in instance.GetSubComponentIds())
                {
                    if (_doc.GetElement(id) is Element child)
                    {
                        CollectWithChildren((Element)child, target);
                    }
                }
            }
        }


        /// <summary>
        ///  Формируем название самого элемента
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>

        private string GetFamilyName(Element element) 
        {
            if (_doc.GetElement(element.GetTypeId()) is ElementType type)
            {
                string familyName = type.FamilyName;
                string typeName = type.Name;

                if (!string.IsNullOrEmpty(familyName) && !string.IsNullOrEmpty(typeName))
                {
                    return familyName + " : " + typeName;
                }

                if (string.IsNullOrEmpty(familyName))
                {
                    return familyName;
                }

                if (!string.IsNullOrEmpty(typeName))
                {
                    return typeName;
                }
            }                
                return element.Category?.Name ?? element.Name;            
        
        }




      



    }
}
