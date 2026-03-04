namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents a discovered handler on an ASP.NET Core Razor Page.
    /// </summary>
    public class RazorPageHandler : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.RazorPage;

        /// <summary>Gets or sets the inferred page route (e.g. "/Products/Details").</summary>
        public string PageRoute { get; set; }

        /// <inheritdoc />
        public override string Route
        {
            get => HandlerName != null ? $"{PageRoute}?handler={HandlerName}" : PageRoute;
            set => PageRoute = value;
        }

        /// <summary>Gets or sets the HTTP method parsed from the handler name (e.g. "GET", "POST").</summary>
        public string HttpMethod { get; set; }

        /// <summary>Gets or sets the named handler suffix, or null for the default handler (e.g. "Export" from OnGetExport).</summary>
        public string HandlerName { get; set; }

        /// <summary>Gets or sets the PageModel class name (e.g. "DetailsModel").</summary>
        public string PageModelName { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => HandlerName != null
            ? $"{HttpMethod} {PageRoute}?handler={HandlerName}"
            : $"{HttpMethod} {PageRoute}";
    }
}
