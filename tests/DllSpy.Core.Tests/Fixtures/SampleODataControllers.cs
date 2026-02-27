using System.Collections.Generic;

namespace DllSpy.Core.Tests.Fixtures
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        public string Company { get; set; }
    }

    // No auth, 4 methods, some with [EnableQuery]
    [ODataRoutePrefix("odata/Products")]
    public class ProductsODataController : ODataController
    {
        [EnableQuery]
        [HttpGet]
        public List<ProductDto> Get() => null;

        [EnableQuery]
        [HttpGet("{key}")]
        public ProductDto Get(int key) => null;

        [HttpPost]
        public void Post([FromBody] ProductDto product) { }

        [HttpDelete("{key}")]
        public void Delete(int key) { }
    }

    // Authorized with roles, 2 methods
    [Authorize(Roles = "Admin")]
    [ODataRoutePrefix("odata/Orders")]
    public class OrdersODataController : ODataController
    {
        [EnableQuery]
        [HttpGet]
        public List<OrderDto> Get() => null;

        [HttpPost]
        public void Post([FromBody] OrderDto order) { }
    }

    // Authorized without roles, one method with [AllowAnonymous]
    [Authorize]
    public class CustomersODataController : ODataController
    {
        [EnableQuery]
        [HttpGet]
        public List<CustomerDto> Get() => null;

        [AllowAnonymous]
        [HttpGet("{key}")]
        public CustomerDto GetById(int key) => null;
    }
}
