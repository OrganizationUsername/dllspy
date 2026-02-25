using System.Threading.Tasks;

namespace DllSpy.Core.Tests.Fixtures.Pages
{
    // Simple page with GET handler — route should be /Index
    public class IndexModel : PageModel
    {
        public void OnGet() { }
    }

    // Page with POST handler — route should be /Contact
    public class ContactModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Message { get; set; }

        public void OnGet() { }
        public Task OnPostAsync() => Task.CompletedTask;
    }
}

namespace DllSpy.Core.Tests.Fixtures.Pages.Products
{
    // Nested namespace — route should be /Products/Details
    public class DetailsModel : PageModel
    {
        public void OnGet(int id) { }
    }

    // Page with named handlers — route should be /Products/Edit
    public class EditModel : PageModel
    {
        [BindProperty]
        public string Name { get; set; }

        public void OnGet(int id) { }
        public Task OnPostAsync() => Task.CompletedTask;
        public Task OnPostDeleteAsync(int id) => Task.CompletedTask;
    }
}

namespace DllSpy.Core.Tests.Fixtures.Pages.Admin
{
    // Authorized page — route should be /Admin/Dashboard
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        public void OnGet() { }
        public Task OnPostExportAsync() => Task.CompletedTask;
    }

    // Page with method-level AllowAnonymous — route should be /Admin/Login
    [Authorize]
    public class LoginModel : PageModel
    {
        [AllowAnonymous]
        public void OnGet() { }

        public Task OnPostAsync() => Task.CompletedTask;
    }
}
