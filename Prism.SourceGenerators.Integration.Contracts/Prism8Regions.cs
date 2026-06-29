namespace Prism.Regions
{
    public sealed class NavigationContext
    {
        public string Uri { get; set; } = string.Empty;
        public INavigationParameters Parameters { get; } = new NavigationParameters();
    }

    public interface INavigationParameters
    {
        bool TryGetValue<T>(string key, out T value);
    }

    public sealed class NavigationParameters : INavigationParameters
    {
        public bool TryGetValue<T>(string key, out T value) { value = default; return false; }
    }

    public interface INavigationAware
    {
        void OnNavigatedTo(NavigationContext navigationContext);
        bool IsNavigationTarget(NavigationContext navigationContext);
        void OnNavigatedFrom(NavigationContext navigationContext);
    }

    public interface IRegionManager
    {
        void RequestNavigate(string regionName, string target);
    }
}
