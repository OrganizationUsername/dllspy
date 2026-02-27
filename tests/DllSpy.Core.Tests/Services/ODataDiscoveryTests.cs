using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class ODataDiscoveryTests
    {
        private readonly List<ODataEndpoint> _endpoints;

        public ODataDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new ODataDiscovery(analyzer);
            var assembly = typeof(ProductsODataController).Assembly;
            var surfaces = discovery.Discover(assembly);
            _endpoints = surfaces.Cast<ODataEndpoint>().ToList();
        }

        [Fact]
        public void Discovers_AllODataEndpoints()
        {
            // ProductsODataController:4 + OrdersODataController:2 + CustomersODataController:2 = 8
            Assert.Equal(8, _endpoints.Count);
        }

        [Fact]
        public void EntitySetName_InferredFromControllerName()
        {
            Assert.All(_endpoints.Where(e => e.ClassName == "Products"), e => Assert.Equal("Products", e.EntitySetName));
            Assert.All(_endpoints.Where(e => e.ClassName == "Orders"), e => Assert.Equal("Orders", e.EntitySetName));
            Assert.All(_endpoints.Where(e => e.ClassName == "Customers"), e => Assert.Equal("Customers", e.EntitySetName));
        }

        [Fact]
        public void Products_RoutesResolvedFromODataRoutePrefix()
        {
            Assert.Contains(_endpoints, e => e.Route == "odata/Products" && e.HttpMethod == "GET" && e.MethodName == "Get" && e.Parameters.Count == 0);
            Assert.Contains(_endpoints, e => e.Route == "odata/Products/{key}" && e.HttpMethod == "GET" && e.MethodName == "Get" && e.Parameters.Count == 1);
            Assert.Contains(_endpoints, e => e.Route == "odata/Products" && e.HttpMethod == "POST" && e.MethodName == "Post");
            Assert.Contains(_endpoints, e => e.Route == "odata/Products/{key}" && e.HttpMethod == "DELETE" && e.MethodName == "Delete");
        }

        [Fact]
        public void Orders_RoutesResolvedFromODataRoutePrefix()
        {
            Assert.Contains(_endpoints, e => e.Route == "odata/Orders" && e.HttpMethod == "GET" && e.MethodName == "Get");
            Assert.Contains(_endpoints, e => e.Route == "odata/Orders" && e.HttpMethod == "POST" && e.MethodName == "Post");
        }

        [Fact]
        public void Customers_RoutesFallBackToConvention()
        {
            // No [ODataRoutePrefix] or [Route] → falls back to odata/{entitySetName}
            Assert.Contains(_endpoints, e => e.Route == "odata/Customers" && e.HttpMethod == "GET" && e.MethodName == "Get");
            Assert.Contains(_endpoints, e => e.Route == "odata/Customers/{key}" && e.HttpMethod == "GET" && e.MethodName == "GetById");
        }

        [Fact]
        public void HttpMethod_ResolvedFromAttributes()
        {
            var productsGet = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Products" && e.Parameters.Count == 0);
            Assert.Equal("GET", productsGet.HttpMethod);

            var productsPost = _endpoints.First(e => e.MethodName == "Post" && e.ClassName == "Products");
            Assert.Equal("POST", productsPost.HttpMethod);

            var productsDelete = _endpoints.First(e => e.MethodName == "Delete" && e.ClassName == "Products");
            Assert.Equal("DELETE", productsDelete.HttpMethod);
        }

        [Fact]
        public void EnableQuery_DetectedOnMethods()
        {
            var productsGet = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Products" && e.Parameters.Count == 0);
            Assert.True(productsGet.HasEnableQuery);

            var productsGetByKey = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Products" && e.Parameters.Count == 1);
            Assert.True(productsGetByKey.HasEnableQuery);

            var productsPost = _endpoints.First(e => e.MethodName == "Post" && e.ClassName == "Products");
            Assert.False(productsPost.HasEnableQuery);

            var ordersGet = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Orders");
            Assert.True(ordersGet.HasEnableQuery);
        }

        [Fact]
        public void Auth_InheritedFromClass()
        {
            var ordersGet = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Orders");
            Assert.True(ordersGet.RequiresAuthorization);
            Assert.Contains("Admin", ordersGet.Roles);

            var customersGet = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Customers");
            Assert.True(customersGet.RequiresAuthorization);
        }

        [Fact]
        public void AllowAnonymous_OverridesClassAuth()
        {
            var customersGetById = _endpoints.First(e => e.MethodName == "GetById" && e.ClassName == "Customers");
            Assert.True(customersGetById.AllowAnonymous);
            Assert.False(customersGetById.RequiresAuthorization);
        }

        [Fact]
        public void DisplayRoute_FormattedCorrectly()
        {
            var productsGet = _endpoints.First(e => e.MethodName == "Get" && e.ClassName == "Products" && e.Parameters.Count == 0);
            Assert.Equal("GET odata/Products", productsGet.DisplayRoute);

            var productsDelete = _endpoints.First(e => e.MethodName == "Delete" && e.ClassName == "Products");
            Assert.Equal("DELETE odata/Products/{key}", productsDelete.DisplayRoute);
        }

        [Fact]
        public void Parameters_ExtractedCorrectly()
        {
            var productsPost = _endpoints.First(e => e.MethodName == "Post" && e.ClassName == "Products");
            Assert.Single(productsPost.Parameters);
            Assert.Equal("product", productsPost.Parameters[0].Name);
            Assert.Equal("ProductDto", productsPost.Parameters[0].Type);
            Assert.Equal(ParameterSource.Body, productsPost.Parameters[0].Source);

            var productsDelete = _endpoints.First(e => e.MethodName == "Delete" && e.ClassName == "Products");
            Assert.Single(productsDelete.Parameters);
            Assert.Equal("key", productsDelete.Parameters[0].Name);
            Assert.Equal("int", productsDelete.Parameters[0].Type);
        }
    }
}
