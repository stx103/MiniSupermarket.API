namespace MiniSupermarket.WinForms
{
    // [Tiêu chí 11] Project WinForms là client riêng trong solution; khởi động giao diện và gọi backend qua ApiClientService (HttpClient).
    internal static class Program
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
            Application.Run(new FormLogin());
        }
    }
}