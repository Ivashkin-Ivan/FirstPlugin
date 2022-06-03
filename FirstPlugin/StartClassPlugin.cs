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
        private Document doc;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /// 1) Код написан в стиле "Hello world!" и требует рефакторинга; 
            /// 2) Использование шаблона MVVM не подразумевается; - а хотелось бы!


            doc = commandData.Application.ActiveUIDocument.Document; //лично мне нравится вынести doc в статическое поле чтобы не таскать ее по методам 
            var allFamilies = GetAllFamilies();
            var rightSupFamilies = RightWrongFamilies(allFamilies, isRight: true);
            var wrongSupFamilies = RightWrongFamilies(allFamilies, isRight: false);
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
        private List<Element> GetAllFamilies() // тут статик не нужен
        {
            //FilteredElementCollector familiesCollector = new FilteredElementCollector(doc);
            //ICollection<Element> allFamilies = familiesCollector.OfClass(typeof(Family)).ToElements();
            var allFamilies = new FilteredElementCollector(doc).OfClass(typeof(Family)).ToList(); // Зачем явная типизация icollection - просто привести к list и сделать в одну строку
            return allFamilies;
        } //зачем метод для одной строки?
        public static List<Element> RightWrongFamilies(ICollection<Element> allFamilies, bool isRight = true) // Запуск при старте и по кнопке "Проверить"
        {
            var rightSupFamilies = new List<Element>();
            var wrongSupFamilies = new List<Element>();
            foreach (Element family in allFamilies)  // Использую Element, так как не понимаю как получить наследника family,FamilySymbol и др.              
            {                                        // в методах filter apiDocs нашёл только cast в Elements или ElementIds

                if (family.Name.Contains("sup")) //name и так уже строка
                {
                    if (family.Name.Contains("(") && family.Name.Contains(")"))
                    {
                        rightSupFamilies.Add(family);
                    }
                    else//не собирать две коллекции, зачем? мы возвращаем все равно одну
                    //проверить на flag раньше и собирать только одну коллекцию и ее же возвращать
                    {
                        wrongSupFamilies.Add(family);
                    }
                }

            }//нейминг переменной flag поменять непонятно что она значит 
            return isRight ? rightSupFamilies : wrongSupFamilies; //Понятен ли такой синтаксис? //понятен но из за нейминга flag становится неочевидно что он должен вернуть
            //сделать например isRight или что то такое
        }
        public  List<IList<ElementId>> DependElements(List<Element> rightSupFamilies) //Использую метод для получения list-ов созависимых от Element классов // тут тоже статик не нужен
        {// проверить все модификаторы доступа паблик почти нигде не нужен
            ElementFilter filter = null; // Инициализировал "костыль"
            var dependElementsInSupFamilies = new List<IList<ElementId>>(); //Очень сложная структура //лучше сделать словарем
            foreach (Family family in rightSupFamilies)
            {
                dependElementsInSupFamilies.Add(family.GetDependentElements(filter)); //filter является "костылём", разобраться как работает
            }
            return dependElementsInSupFamilies;
        }
        private List<string> GetMarks(List<Element> rightSupFamilies) // "Кустарный" метод для получения значения из скобок,      
        {                                                                   // Нужно использовать стандартные методы, почитать про Regex - все верно
            var marks = new List<string>();                                //и тут тоже статик не нужен почитай для чего нужно это ключевое слово 
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
        public static void SetMarks(Document doc,List<string> marks,List<IList<ElementId>> dependElementsInSupFamilies)
        {
            FilteredElementCollector familySymbolCollector = new FilteredElementCollector(doc);
            familySymbolCollector.OfClass(typeof(FamilySymbol));
            var familySymbolIds = familySymbolCollector.ToElementIds();
            for (int i = 0; i < dependElementsInSupFamilies.Count(); i++) // Цикл тройной вложенности, подозрение на сложность Θ(n^3) - Посмотреть в debag-е
            {
                foreach (IList<ElementId> fs in dependElementsInSupFamilies)
                {
                    var symbolsIdList = new List<ElementId>(); // Можно ли создавать экземпляр класса в цикле, разобраться, как влияет на память и скорость?
                    foreach (ElementId id in dependElementsInSupFamilies[i])// Посмотреть в debag-е
                    {
                        if (familySymbolIds.Contains(id))
                        {
                            symbolsIdList.Add(id);
                            string mark = marks[i];
                            SetSymbolsMarks(doc, symbolsIdList, mark); //Метод внутри метода, можно ли так делать? Поставил модификатор privat - можно и private нужен не только здесь
                        }
                    }
                }
            }
            //ничего плохого нет в цикле тройной вложенности, если он необходим, но в данном случае спокойно можно обойтись без него
            //посмотри про ключевые слова break и continue  - смогут помочь 

        }
        private static void SetSymbolsMarks(Document doc, List<ElementId> symbolsIdList, string mark)
        {
            var cleanSymbolsFilter = new FilteredElementCollector(doc, symbolsIdList); //Неявный cast ICollection() ---> List(), узнать подробнее об этом - Это не совсем каст icollection это интерфейс а лист его реализует
            cleanSymbolsFilter.OfClass(typeof(FamilySymbol)); 
            List<Element> cleanSymbols = cleanSymbolsFilter.ToList();//в одну строку будет гораздо читабельнее и не пользуйся toElements в случаях с коллектором там уже элементы лежат
            var ADSK_Мark = new Guid("2204049c-d557-4dfc-8d70-13f19715e46d"); //лучше не хардкодить а вынести строку в отдельную переменную
            List<Element> L = cleanSymbols.ToList(); //ICollection не итерируемый объект, в docs.microsoft предлагается итерация "через C++", изучить этот вопрос - приводи коллекции к листу

            using (Transaction t = new Transaction(doc)) //Узнать подробнее про транзакции и способы получения/извлечения параметров
            {
                t.Start("SetParameter"); //Дорого ли стоит операция открытия и закрытия транзакции? Посмотреть в debag-е - нет не дорого
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
            foreach (IList<ElementId> list in dependElementsInSupFamilies) //посмотри какой нибудь видос про коллекции в шарпе ты их с интерфейсами путаешь
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
                    //а что если не нашлось бы такого элемента?
                }
            }
            return null; //Возможно это не нужно, потому что все пути уже ведут к выходу из метода...
            //убери эту строку и посмотри что тебе скажет вижуалка))

        }
        public static void RenameFamily(int hash, List<Element> wrongSupFamilies, string newName, Document doc)
        {

            foreach (Element e in wrongSupFamilies)
            {
                if (e.GetHashCode() == hash)
                {
                    using (Transaction t = new Transaction(doc)) // Подобная часть кода уже встречалась, нужно вынести методом! - я бы не стал делать отдельный метод для транзакций
                    {
                        t.Start("SetFamilyName");//очень много транзакций зачем их в цикле делать
                            e.Name = newName; // В lookup свойство isReadOnly, но удалось записать имя... Как?
                        t.Commit();           // Большая вложенность
                    }
                }


            }
        }
        //мое имхо слишком много методов, ооп это конечно круто но тут получается немного оверкодинг. Типа я бы расширил execute чтобы по нему был виден алгорит что за чем идет, ну надеюсь я понятно объяснил
        //методы которые ты делаешь для того чтобы вызвать их в интерфейсе лучше вынести в отдельный класс utils
    }
}

