namespace WinFormsApp1;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    // New constructor to accept authenticated username
    public Form1(string authenticatedUsername) : this()
    {
        if (!string.IsNullOrEmpty(authenticatedUsername))
        {
            this.Text = $"Main - {authenticatedUsername}";
        }
    }
}