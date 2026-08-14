using CalorieLens.Models;

namespace CalorieLens.Views;

public class MainCarouselPage : TabbedPage
{
    public MainCarouselPage(User user)
    {
        Title = "CalorieLens";

        var mainPage = new MainPage(user);
        mainPage.Title = "Azi";

        var journalPage = new JournalPage();
        journalPage.Title = "Jurnal";

        Children.Add(mainPage);
        Children.Add(journalPage);
    }
}