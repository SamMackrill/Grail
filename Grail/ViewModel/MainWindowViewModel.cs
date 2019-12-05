
namespace Grail.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        public static string Title => $"Grail {Extensions.GetVersion()}";
        public static string Version => Extensions.GetVersion();

        /// <inheritdoc />
        public override string Key => "Main";
    }
}
