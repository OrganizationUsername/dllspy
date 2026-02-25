using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DllSpy.Core.Contracts;
using DllSpy.Core.Helpers;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Discovers routable Blazor components by scanning for types that inherit from ComponentBase and have [Route] attributes.
    /// </summary>
    internal class BlazorDiscovery : IDiscovery
    {
        private readonly SecurityResolver _security;

        /// <summary>
        /// Initializes a new instance of <see cref="BlazorDiscovery"/>.
        /// </summary>
        public BlazorDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            if (attributeAnalyzer == null) throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.BlazorComponent;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            foreach (var type in ReflectionHelper.GetTypesSafe(assembly))
            {
                if (IsRoutableComponent(type))
                {
                    surfaces.AddRange(DiscoverRoutes(type));
                }
            }

            return surfaces;
        }

        private List<BlazorRoute> DiscoverRoutes(Type componentType)
        {
            var routes = new List<BlazorRoute>();
            var componentName = componentType.Name;
            var classSec = _security.ReadClass(componentType);
            var parameters = GetComponentParameters(componentType);

            var routeTemplates = GetRouteTemplates(componentType);

            foreach (var template in routeTemplates)
            {
                routes.Add(new BlazorRoute
                {
                    RouteTemplate = template,
                    ComponentName = componentName,
                    ClassName = componentName,
                    MethodName = template,
                    RequiresAuthorization = classSec.HasAuth,
                    AllowAnonymous = classSec.AllowAnon,
                    Roles = classSec.Roles,
                    Policies = classSec.Policies,
                    Parameters = new List<EndpointParameterInfo>(parameters),
                    ReturnType = "void",
                    IsAsync = false,
                    SecurityAttributes = classSec.SecurityAttributes
                });
            }

            return routes;
        }

        private static bool IsRoutableComponent(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            if (!InheritsFromComponentBase(type))
                return false;

            // Must have at least one [Route] attribute to be routable
            return GetRouteTemplates(type).Count > 0;
        }

        private static bool InheritsFromComponentBase(Type type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                if (current.Name == "ComponentBase")
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        private static List<string> GetRouteTemplates(Type type)
        {
            var templates = new List<string>();

            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(type, true))
                {
                    if (attr.GetType().Name == "RouteAttribute")
                    {
                        var templateProp = attr.GetType().GetProperty("Template");
                        if (templateProp != null)
                        {
                            var value = templateProp.GetValue(attr) as string;
                            if (!string.IsNullOrEmpty(value))
                            {
                                templates.Add(value);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return templates;
        }

        private static List<EndpointParameterInfo> GetComponentParameters(Type componentType)
        {
            var parameters = new List<EndpointParameterInfo>();

            foreach (var prop in componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var hasParameter = prop.GetCustomAttributes(true)
                    .Any(a => a.GetType().Name == "ParameterAttribute");

                if (hasParameter)
                {
                    parameters.Add(new EndpointParameterInfo
                    {
                        Name = prop.Name,
                        Type = ReflectionHelper.GetFriendlyTypeName(prop.PropertyType),
                        IsRequired = !ReflectionHelper.IsNullableType(prop.PropertyType),
                        Source = ParameterSource.Unknown
                    });
                }
            }

            return parameters;
        }
    }
}
