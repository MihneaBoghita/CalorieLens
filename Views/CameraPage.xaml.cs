namespace CalorieLens.Views;

public partial class CameraPage : ContentPage
{
    private string? _imagePath;

    public CameraPage()
    {
        InitializeComponent();
    }

    private async void OnTakePhoto(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Eroare", "Camera nu este disponibila pe acest dispozitiv.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            await LoadPhoto(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", ex.Message, "OK");
        }
    }

    private async void OnPickPhoto(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            await LoadPhoto(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Eroare", ex.Message, "OK");
        }
    }

    private async Task LoadPhoto(FileResult? photo)
    {
        if (photo == null) return;

        // Salveaza imaginea in cache
        var filePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

        using (var stream = await photo.OpenReadAsync())
        using (var fileStream = File.OpenWrite(filePath))
        {
            await stream.CopyToAsync(fileStream);
        }

        _imagePath = filePath;

        // Afiseaza preview
        previewImage.Source = ImageSource.FromFile(filePath);
        imageFrame.IsVisible = true;
        placeholderFrame.IsVisible = false;
        analyzeButton.IsVisible = true;
    }

    private async void OnAnalyze(object sender, EventArgs e)
    {
        if (_imagePath == null) return;
        await Navigation.PushAsync(new FoodResultPage(_imagePath));
    }
}