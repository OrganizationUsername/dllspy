namespace DllSpy.Core.Tests.Fixtures
{
    // Simple routable component with single route
    [Route("/counter")]
    public class Counter : ComponentBase
    {
        [Parameter]
        public int IncrementAmount { get; set; }
    }

    // Component with multiple routes
    [Route("/weather")]
    [Route("/forecast")]
    public class WeatherForecast : ComponentBase
    {
        [Parameter]
        public string City { get; set; }

        [Parameter]
        public int Days { get; set; }
    }

    // Authorized component
    [Route("/admin/settings")]
    [Authorize(Roles = "Admin")]
    public class AdminSettings : ComponentBase
    {
        [Parameter]
        public string Section { get; set; }
    }

    // Component with auth but no roles
    [Route("/profile")]
    [Authorize]
    public class UserProfile : ComponentBase { }

    // Non-routable component (no [Route]) — should be excluded
    public class SharedLayout : ComponentBase
    {
        [Parameter]
        public string Title { get; set; }
    }

    // Component with AllowAnonymous
    [Route("/public-info")]
    [AllowAnonymous]
    public class PublicInfo : ComponentBase { }
}
