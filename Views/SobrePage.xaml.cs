namespace GVC.Mobile.Views;

public partial class SobrePage : ContentPage
{
    public SobrePage()
    {
        InitializeComponent();

        lblVersao.Text =
            $"{AppInfo.Current.VersionString} " +
            $"({AppInfo.Current.BuildString})";
    }
}