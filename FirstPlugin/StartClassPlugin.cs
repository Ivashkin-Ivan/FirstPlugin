using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Text.RegularExpressions;

namespace FirstPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class StartClassPlugin : IExternalCommand //Главный модуль
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /// 1) Код написан в стиле "Hello world!" и требует рефакторинга; 
            /// 2) Использование шаблона MVVM не подразумевается;


            Document doc = commandData.Application.ActiveUIDocument.Document;
            var allFamilies = GetAllFamilies(doc);
            var rightSupFamilies = RightWrongFamilies(allFamilies, true);
            var wrongSupFamilies = RightWrongFamilies(allFamilies, false);
            var dependElementsInSupFamilies = DependElements(rightSupFamilies);
            var marks = GetMarks(rightSupFamilies);

            MainWindow mainWindow = new MainWindow(
                rightSupFamilies,
                wrongSupFamilies,
                doc,
                marks,
                dependElementsInSupFamilies,
                allFamilies
                );

            mainWindow.ShowDialog();

            return Result.Succeeded;
        }
        public static ICollection<Element> GetAllFamilies(Document doc)
        {
            FilteredElementCollector familiesCollector = new FilteredElementCollector(doc);
            ICollection<Element> allFamilies = familiesCollector.OfClass(typeof(Family)).ToElements();
            return allFamilies;
        }
        public static List<Element> RightWrongFamilies(ICollection<Element> allFamilies, bool flag) // Запуск при старте и по кнопке "Проверить"
        {
            var rightSupFamilies = new List<Element>();
            var wrongSupFamilies = new List<Element>();
            foreach (Element family in allFamilies)  // Использую Element, так как не понимаю как получить наследника family,FamilySymbol и др.              
            {                                        // в методах filter apiDocs нашёл только cast в Elements или ElementIds

                if (family.Name.ToString().Contains("sup"))
                {
                    if (family.Name.ToString().Contains("(") && family.Name.ToString().Contains(")"))
                    {
                        rightSupFamilies.Add(family);
                    }
                    else
                    {
                        wrongSupFamilies.Add(family);
                    }
                }

            }
            return flag ? rightSupFamilies : wrongSupFamilies; //Понятен ли такой синтаксис?
        }
        public static List<IList<ElementId>> DependElements(List<Element> rightSupFamilies) //Использую метод для получения list-ов созависимых от Element классов
        {
            ElementFilter filter = null; // Инициализировал "костыль"
            var dependElementsInSupFamilies = new List<IList<ElementId>>(); //Очень сложная структура
            foreach (Family family in rightSupFamilies)
            {
                dependElementsInSupFamilies.Add(family.GetDependentElements(filter)); //filter является "костылём", разобраться как работает
            }
            return dependElementsInSupFamilies;
        }
        private static List<string> GetMarks(List<Element> rightSupFamilies) // "Кустарный" метод для получения значения из скобок,
        {                                                                   // Нужно использовать стандартные методы, почитать про Regex
            var marks = new List<string>();
            foreach (Family f in rightSupFamilies)
            {
                string s = f.Name.ToString();
                int start = s.IndexOf('(') + 1;
                int end = s.IndexOf(')', start);
                string mark = s.Substring(start, end - start);
                marks.Add(mark);
            }
            return marks;
        }
        public static void SetMarks(
            Document doc,
            List<string> marks,
            List<IList<ElementId>> dependElementsInSupFamilies
            )
        {
            FilteredElementCollector familySymbolCollector = new FilteredElementCollector(doc);
            familySymbolCollector.OfClass(typeof(FamilySymbol));
            var familySymbolIds = familySymbolCollector.ToElementIds();
            for (int i = 0; i < dependElementsInSupFamilies.Count(); i++) // Цикл тройной вложенности, подозрение на сложность Θ(n^3) - Посмотреть в debag-е
                foreach (IList<ElementId> fs in dependElementsInSupFamilies)
                {
                    var symbolsIdList = new List<ElementId>(); // Можно ли создавать экземпляр класса в цикле, разобраться, как влияет на память и скорость?
                    foreach (ElementId id in dependElementsInSupFamilies[i])// Посмотреть в debag-е
                    {
                        if (familySymbolIds.Contains(id))
                        {
                            symbolsIdList.Add(id);
                            string mark = marks[i];
                            SetSymbolsMarks(doc, symbolsIdList, mark); //Метод внутри метода, можно ли так делать? Поставил модификатор privat
                        }
                    }
                }

        }
        private static void SetSymbolsMarks(Document doc, List<ElementId> symbolsIdList, string mark)
        {
            var cleanSymbolsFilter = new FilteredElementCollector(doc, symbolsIdList); //Неявный cast ICollection() ---> List(), узнать подробнее об этом
            cleanSymbolsFilter.OfClass(typeof(FamilySymbol));
            ICollection<Element> cleanSymbols = cleanSymbolsFilter.ToElements();
            var ADSK_Мark = new Guid("2204049c-d557-4dfc-8d70-13f19715e46d");
            List<Element> L = cleanSymbols.ToList(); //ICollection не итерируемый объект, в docs.microsoft предлагается итерация "через C++", изучить этот вопрос

            using (Transaction t = new Transaction(doc)) //Узнать подробнее про транзакции и способы получения/извлечения параметров
            {
                t.Start("SetParameter"); //Дорого ли стоит операция открытия и закрытия транзакции? Посмотреть в debag-е
                for (int i = 0; i < L.Count(); i++)
                {
                    L[i].get_Parameter(ADSK_Мark).Set($"{mark}.{i + 1}");
                }
                t.Commit();
            }

        }
        public static void SetGroup(Document doc, List<IList<ElementId>> dependElementsInSupFamilies)
        {
            var allInstanceForSetParam = new List<Element>();
            var ADSK_Group = new Guid("3de5f1a4-d560-4fa8-a74f-25d250fb3401");
            foreach (IList<ElementId> list in dependElementsInSupFamilies)
            {
                foreach (ElementId id in list)
                {
                    var e = doc.GetElement(id) as FamilySymbol;
                    if (e != null)
                    {
                        var instance = GetFamilyInstance(e, doc, ADSK_Group);
                        allInstanceForSetParam.Add(instance);
                    }
                }
            }
            using (Transaction t = new Transaction(doc)) // Подобная часть кода уже встречалась, нужно вынести методом!
            {
                t.Start("SetParameter");
                for (int i = 0; i < allInstanceForSetParam.Count(); i++)
                {
                    if (allInstanceForSetParam[i] != null)
                    {
                        allInstanceForSetParam[i].get_Parameter(ADSK_Group).Set("_s");
                    }
                }
                t.Commit();
            }
        }
        private static FamilyInstance GetFamilyInstance(Element e, Document doc, Guid param)
        {
            ElementFilter filter = null; //Опять костыль
            var list = e.GetDependentElements(filter);
            foreach (ElementId id in list)
            {
                var el = doc.GetElement(id) as FamilyInstance;
                if (el != null && el.get_Parameter(param).IsReadOnly == false)
                {
                    return el;
                }
            }
            return null; //Возможно это не нужно, потому что все пути уже ведут к выходу из метода...
        }
        public static void RenameFamily(int hash, List<Element> wrongSupFamilies, string newName, Document doc)
        {

            foreach (Element e in wrongSupFamilies)
            {
                if (e.GetHashCode() == hash)
                {
                    using (Transaction t = new Transaction(doc)) // Подобная часть кода уже встречалась, нужно вынести методом!
                    {
                        t.Start("SetFamilyName");
                            e.Name = newName; // В lookup свойство isReadOnly, но удалось записать имя... Как?
                        t.Commit();           // Большая вложенность
                    }
                }


            }
        }

    }
}

