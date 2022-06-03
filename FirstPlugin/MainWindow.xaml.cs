
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using FirstPlugin;


namespace FirstPlugin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Element> AllRightSupFamilies;
        List<Element> AllWrongSupFamilies;
        Document Doc;
        List<string> Marks;
        List<IList<ElementId>> DependElementsInSupFamilies;
        ICollection<Element> AllFamilies;


        public MainWindow(
            List<Element> rightSupFamilies, 
            List<Element> wrongSupFamilies, 
            Document doc, 
            List<string> marks, 
            List<IList<ElementId>> dependElementsInSupFamilies,
            ICollection<Element> allFamilies
            )
        {
            InitializeComponent(); // Разобраться с инициализацией
            Doc = doc;
            AllFamilies = allFamilies;
            Marks = marks;
            DependElementsInSupFamilies = dependElementsInSupFamilies;
            AllRightSupFamilies = rightSupFamilies;
            AllWrongSupFamilies = wrongSupFamilies;
            RightNameFamilies.ItemsSource = AllRightSupFamilies;
            RightNameFamilies.DisplayMemberPath = "Name";
            WrongNameFamilies.ItemsSource = AllWrongSupFamilies;
            WrongNameFamilies.DisplayMemberPath = "Name";

        }

        private void Set_Mark(object sender, RoutedEventArgs e)
        {
            StartClassPlugin.SetMarks(Doc, Marks, DependElementsInSupFamilies);
            MessageBox.Show("Марка прописана!");
        }
        private void Rename_Family(object sender, RoutedEventArgs e) // Можно добавить функционал переименования "правильных" семейств
                                                                     // Можно попробовать получать имя активного элемента LIstBox в TextBox
        {
            try
            {
                int hash = WrongNameFamilies.SelectedItem.GetHashCode();               // Обернуть в try catch
                string newName = RenameFamilies.Text;
                StartClassPlugin.RenameFamily(hash, AllWrongSupFamilies, newName, Doc);
                WrongNameFamilies.Items.Refresh();
            }
            catch(Exception) // Нужно разделить исключения, а не использовать общий exeption, (см.MessageBox)
            {
                MessageBox.Show("Ошибка: Элемент выбран из списка с корректными именами или имеет не уникальное имя!");
            }
        }
        private void Check_RightFamilies(object sender, RoutedEventArgs e)
        {
            AllRightSupFamilies = StartClassPlugin.RightWrongFamilies(AllFamilies, true);
            AllWrongSupFamilies = StartClassPlugin.RightWrongFamilies(AllFamilies, false);
            RightNameFamilies.ItemsSource = AllRightSupFamilies; // Дублирую код, потому что не понимаю как правильно обновить listbox
            RightNameFamilies.DisplayMemberPath = "Name";
            WrongNameFamilies.ItemsSource = AllWrongSupFamilies;
            WrongNameFamilies.DisplayMemberPath = "Name";
            MessageBox.Show("Проверка выполнена!");
        }

        private void Set_Group(object sender, RoutedEventArgs e)
        {
            StartClassPlugin.SetGroup(Doc,DependElementsInSupFamilies);
            MessageBox.Show("Группирование назначено!");
        }

        //у тебя нет userproof проверок - протестируй что будет если пользователь начнет тянуть интерфейс или нажимать кнопки не в правильной последовательности и т.д.
    }
}
