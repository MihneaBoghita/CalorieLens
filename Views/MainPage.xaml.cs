using CalorieLens.Views;

namespace CalorieLens
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnOpenCamera(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }
    }
}