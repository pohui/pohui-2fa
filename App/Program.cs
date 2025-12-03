namespace WinFormsApp1;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Show login form first. If login succeeds, run the main form with the authenticated username.
        using (var login = new LoginForm())
        {
            var result = login.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrEmpty(login.AuthenticatedUsername))
            {
                Application.Run(new Form1(login.AuthenticatedUsername));
            }
            else
            {
                // Exit the app if login failed or was cancelled
                return;
            }
        }
    }
}