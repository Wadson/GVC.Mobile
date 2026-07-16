using System.Diagnostics;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;

namespace GVC.Mobile.Views;

public partial class SobrePage : ContentPage
{
    public SobrePage()
    {
        InitializeComponent();

        // Versão do aplicativo
        lblVersao.Text = $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

        // (Opcional) Você pode ler outras informações de atributos do assembly
         var assembly = Assembly.GetExecutingAssembly();
         var empresa = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "GVC Tecnologia";
         var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "© 2025 GVC Tecnologia. Todos os direitos reservados.";
        // ...
    }

    private async void OnFaleConoscoClicked(object sender, EventArgs e)
    {
        try
        {
            var email = "suporte@gvc.com.br";
            var assunto = "Suporte - GVC Mobile";
            var uri = new Uri($"mailto:{email}?subject={Uri.EscapeDataString(assunto)}");
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao abrir e-mail: {ex.Message}");
            await DisplayAlert("Erro", "Não foi possível abrir o cliente de e-mail. Entre em contato pelo site.", "OK");
        }
    }
}