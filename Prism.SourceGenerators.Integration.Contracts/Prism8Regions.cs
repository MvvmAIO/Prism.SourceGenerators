namespace Prism.Regions
{
    public sealed class NavigationContext
    {
        public string Uri { get; set; } = string.Empty;
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
