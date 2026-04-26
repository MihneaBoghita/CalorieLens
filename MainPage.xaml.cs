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
            await DisplayAlert("Camera", "Camera will open here", "OK");
        }
    }
}
