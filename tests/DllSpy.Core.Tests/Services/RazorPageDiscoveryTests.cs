using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using DllSpy.Core.Tests.Fixtures.Pages;
using DllSpy.Core.Tests.Fixtures.Pages.Products;
using DllSpy.Core.Tests.Fixtures.Pages.Admin;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class RazorPageDiscoveryTests
    {
        private readonly List<RazorPageHandler> _handlers;

        public RazorPageDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new RazorPageDiscovery(analyzer);
            var assembly = typeof(IndexModel).Assembly;
            var surfaces = discovery.Discover(assembly);
            _handlers = surfaces.Cast<RazorPageHandler>().ToList();
        }

        [Fact]
        public void Discovers_AllPageHandlers()
        {
            // IndexModel:1 + ContactModel:2 + DetailsModel:1 + EditModel:3 + DashboardModel:2 + LoginModel:2 = 11
            Assert.Equal(11, _handlers.Count);
        }

        [Fact]
        public void InfersRoute_FromNamespace_SimplePage()
        {
            var index = _handlers.First(h => h.PageModelName == "IndexModel");
            Assert.Equal("/Index", index.PageRoute);
        }

        [Fact]
        public void InfersRoute_FromNamespace_NestedPage()
        {
            var details = _handlers.First(h => h.PageModelName == "DetailsModel");
            Assert.Equal("/Products/Details", details.PageRoute);
        }

        [Fact]
        public void ParsesHttpMethod_FromHandlerName()
        {
            var onGet = _handlers.First(h => h.PageModelName == "IndexModel" && h.MethodName == "OnGet");
            Assert.Equal("GET", onGet.HttpMethod);

            var onPost = _handlers.First(h => h.PageModelName == "ContactModel" && h.MethodName == "OnPostAsync");
            Assert.Equal("POST", onPost.HttpMethod);
        }

        [Fact]
        public void ParsesNamedHandler()
        {
            var deleteHandler = _handlers.First(h => h.PageModelName == "EditModel" && h.MethodName == "OnPostDeleteAsync");
            Assert.Equal("POST", deleteHandler.HttpMethod);
            Assert.Equal("Delete", deleteHandler.HandlerName);
        }

        [Fact]
        public void DefaultHandler_HasNullHandlerName()
        {
            var onGet = _handlers.First(h => h.PageModelName == "IndexModel" && h.MethodName == "OnGet");
            Assert.Null(onGet.HandlerName);
        }

        [Fact]
        public void DisplayRoute_WithoutHandler()
        {
            var onGet = _handlers.First(h => h.PageModelName == "IndexModel" && h.MethodName == "OnGet");
            Assert.Equal("GET /Index", onGet.DisplayRoute);
        }

        [Fact]
        public void DisplayRoute_WithNamedHandler()
        {
            var export = _handlers.First(h => h.PageModelName == "DashboardModel" && h.MethodName == "OnPostExportAsync");
            Assert.Equal("POST /Admin/Dashboard?handler=Export", export.DisplayRoute);
        }

        [Fact]
        public void ClassLevelAuth_InheritedByHandlers()
        {
            var dashGet = _handlers.First(h => h.PageModelName == "DashboardModel" && h.MethodName == "OnGet");
            Assert.True(dashGet.RequiresAuthorization);
            Assert.Contains("Admin", dashGet.Roles);
        }

        [Fact]
        public void MethodLevelAllowAnonymous_OverridesClassAuth()
        {
            var loginGet = _handlers.First(h => h.PageModelName == "LoginModel" && h.MethodName == "OnGet");
            Assert.True(loginGet.AllowAnonymous);
            Assert.False(loginGet.RequiresAuthorization);
        }

        [Fact]
        public void BindProperty_IncludedAsParameters()
        {
            var contactPost = _handlers.First(h => h.PageModelName == "ContactModel" && h.MethodName == "OnPostAsync");
            Assert.Contains(contactPost.Parameters, p => p.Name == "Email" && p.Source == ParameterSource.Form);
            Assert.Contains(contactPost.Parameters, p => p.Name == "Message" && p.Source == ParameterSource.Form);
        }

        [Fact]
        public void MethodParameters_AreDiscovered()
        {
            var details = _handlers.First(h => h.PageModelName == "DetailsModel" && h.MethodName == "OnGet");
            Assert.Contains(details.Parameters, p => p.Name == "id");
        }
    }
}
