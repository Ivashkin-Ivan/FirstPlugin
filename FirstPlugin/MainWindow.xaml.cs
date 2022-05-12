
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
        List<Element> AllRightFamilies;
        List<Element> AllWrongSupFamilies;
        Document Doc;
        List<string> Marks;
        List<IList<ElementId>> DependElementsInSupFamilies;


        public MainWindow(List<Element> rightSupFamilies, List<Element> wrongSupFamilies, Document doc, List<string> marks, List<IList<ElementId>> dependElementsInSupFamilies)
        {
            InitializeComponent(); // Разобраться с инициализацией
            Doc = doc;
            Marks = marks;
            DependElementsInSupFamilies = dependElementsInSupFamilies;
            AllRightFamilies = rightSupFamilies;
            AllWrongSupFamilies = wrongSupFamilies;
            AllFamiliesView.ItemsSource = AllRightFamilies;
            AllFamiliesView.DisplayMemberPath = "Name";
            WrongNameFamilies.ItemsSource = AllWrongSupFamilies;
            WrongNameFamilies.DisplayMemberPath = "Name";

        }

        private void Set_Mark(object sender, RoutedEventArgs e)
        {

        }
        private void Rename_Family(object sender, RoutedEventArgs e)
        {

        }

        private void Check_RightFamilies(object sender, RoutedEventArgs e)
        {

        }

        private void Set_Group(object sender, RoutedEventArgs e)
        {

        }

        
    }
}
