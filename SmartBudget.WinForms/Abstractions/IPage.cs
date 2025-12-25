using SmartBudget.WinForms.Pages;


namespace SmartBudget.WinForms.Abstractions
{
    public sealed record NavigationContext(
    long? AccountId = null,
    object? Payload = null
);

    public interface IPage
    {
        PageKey Key { get; }
        string Title { get; }
        Control View { get; }

        /// <summary>Called every time the page becomes active.</summary>
        Task OnNavigatedTo(NavigationContext context);

        /// <summary>Optional: allow page to block navigation (unsaved changes, etc.).</summary>
        bool CanNavigateAway() => true;
    }
}
