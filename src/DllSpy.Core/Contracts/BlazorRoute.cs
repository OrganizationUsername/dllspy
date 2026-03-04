namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents a discovered routable Blazor component.
    /// </summary>
    public class BlazorRoute : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.BlazorComponent;

        /// <summary>Gets or sets the route template from the [Route] attribute (e.g. "/counter").</summary>
        public string RouteTemplate { get; set; }

        /// <inheritdoc />
        public override string Route { get => RouteTemplate; set => RouteTemplate = value; }

        /// <summary>Gets or sets the Blazor component class name (e.g. "Counter").</summary>
        public string ComponentName { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => $"Blazor {RouteTemplate}";
    }
}
