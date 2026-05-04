using CalorieLens.Services;

namespace CalorieLens
{
    public partial class App : Application
    {
        public static DatabaseService Database { get; private set; }
        public App()
        {
            InitializeComponent();


            Database = new DatabaseService();

            MainPage = new NavigationPage(new StartPage(Database));

        }
    }
}