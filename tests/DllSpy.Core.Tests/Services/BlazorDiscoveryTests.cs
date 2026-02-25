using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class BlazorDiscoveryTests
    {
        private readonly List<BlazorRoute> _routes;

        public BlazorDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new BlazorDiscovery(analyzer);
            var assembly = typeof(Counter).Assembly;
            var surfaces = discovery.Discover(assembly);
            _routes = surfaces.Cast<BlazorRoute>().ToList();
        }

        [Fact]
        public void Discovers_AllRoutableComponents()
        {
            // Counter:1 + WeatherForecast:2 + AdminSettings:1 + UserProfile:1 + PublicInfo:1 = 6
            Assert.Equal(6, _routes.Count);
        }

        [Fact]
        public void SingleRoute_DiscoveredCorrectly()
        {
            var counter = _routes.Where(r => r.ComponentName == "Counter").ToList();
            Assert.Single(counter);
            Assert.Equal("/counter", counter[0].RouteTemplate);
        }

        [Fact]
        public void MultipleRoutes_CreateMultipleSurfaces()
        {
            var weather = _routes.Where(r => r.ComponentName == "WeatherForecast").ToList();
            Assert.Equal(2, weather.Count);
            Assert.Contains(weather, r => r.RouteTemplate == "/weather");
            Assert.Contains(weather, r => r.RouteTemplate == "/forecast");
        }

        [Fact]
        public void NonRoutableComponent_IsExcluded()
        {
            Assert.DoesNotContain(_routes, r => r.ComponentName == "SharedLayout");
        }

        [Fact]
        public void DisplayRoute_FormattedCorrectly()
        {
            var counter = _routes.First(r => r.ComponentName == "Counter");
            Assert.Equal("Blazor /counter", counter.DisplayRoute);
        }

        [Fact]
        public void Parameters_DiscoveredFromProperties()
        {
            var counter = _routes.First(r => r.ComponentName == "Counter");
            Assert.Contains(counter.Parameters, p => p.Name == "IncrementAmount");
        }

        [Fact]
        public void MultipleParameters_Discovered()
        {
            var weather = _routes.First(r => r.ComponentName == "WeatherForecast");
            Assert.Contains(weather.Parameters, p => p.Name == "City");
            Assert.Contains(weather.Parameters, p => p.Name == "Days");
        }

        [Fact]
        public void AuthorizedComponent_DetectedCorrectly()
        {
            var admin = _routes.First(r => r.ComponentName == "AdminSettings");
            Assert.True(admin.RequiresAuthorization);
            Assert.Contains("Admin", admin.Roles);
        }

        [Fact]
        public void AuthWithoutRoles_DetectedCorrectly()
        {
            var profile = _routes.First(r => r.ComponentName == "UserProfile");
            Assert.True(profile.RequiresAuthorization);
            Assert.Empty(profile.Roles);
        }

        [Fact]
        public void AllowAnonymous_DetectedCorrectly()
        {
            var pub = _routes.First(r => r.ComponentName == "PublicInfo");
            Assert.True(pub.AllowAnonymous);
        }

        [Fact]
        public void UnauthenticatedComponent_DetectedCorrectly()
        {
            var counter = _routes.First(r => r.ComponentName == "Counter");
            Assert.False(counter.RequiresAuthorization);
            Assert.False(counter.AllowAnonymous);
        }
    }
}
