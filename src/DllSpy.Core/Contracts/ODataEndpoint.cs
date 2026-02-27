namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents a discovered OData endpoint on an ODataController.
    /// </summary>
    public class ODataEndpoint : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.ODataEndpoint;

        /// <summary>Gets or sets the full route template for this endpoint.</summary>
        public string Route { get; set; }

        /// <summary>Gets or sets the HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).</summary>
        public string HttpMethod { get; set; }

        /// <summary>Gets or sets the entity set name inferred from the controller name.</summary>
        public string EntitySetName { get; set; }

        /// <summary>Gets or sets whether the [EnableQuery] attribute is present on the method.</summary>
        public bool HasEnableQuery { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => $"{HttpMethod} {Route}";
    }
}
