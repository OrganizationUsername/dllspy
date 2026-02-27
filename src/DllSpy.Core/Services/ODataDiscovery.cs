using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DllSpy.Core.Contracts;
using DllSpy.Core.Helpers;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Discovers OData endpoints by scanning for classes that inherit from ODataController.
    /// </summary>
    internal class ODataDiscovery : IDiscovery
    {
        private readonly AttributeAnalyzer _attributeAnalyzer;
        private readonly SecurityResolver _security;

        /// <summary>
        /// Initializes a new instance of <see cref="ODataDiscovery"/>.
        /// </summary>
        public ODataDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            _attributeAnalyzer = attributeAnalyzer ?? throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.ODataEndpoint;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            foreach (var type in ReflectionHelper.GetTypesSafe(assembly))
            {
                if (InheritsFromODataController(type))
                {
                    surfaces.AddRange(DiscoverODataEndpoints(type));
                }
            }

            return surfaces;
        }

        private static bool InheritsFromODataController(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            var current = type.BaseType;
            while (current != null)
            {
                if (current.Name == "ODataController")
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        private List<ODataEndpoint> DiscoverODataEndpoints(Type type)
        {
            var endpoints = new List<ODataEndpoint>();
            var entitySetName = GetEntitySetName(type);
            var routePrefix = GetRoutePrefix(type, entitySetName);
            var classSec = _security.ReadClass(type);

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                if (method.IsSpecialName) continue;

                var httpMethod = _attributeAnalyzer.GetHttpMethod(method) ?? InferHttpMethod(method.Name);
                var methodRoute = GetMethodRoute(method);
                var route = BuildRoute(routePrefix, methodRoute);
                var hasEnableQuery = HasEnableQueryAttribute(method);
                var merged = _security.Merge(classSec, method);
                var parameters = ReflectionHelper.GetParameters(method, _attributeAnalyzer);
                var returnType = ReflectionHelper.GetFriendlyTypeName(method.ReturnType);
                var isAsync = ReflectionHelper.IsAsyncMethod(method);

                endpoints.Add(new ODataEndpoint
                {
                    Route = route,
                    HttpMethod = httpMethod,
                    EntitySetName = entitySetName,
                    HasEnableQuery = hasEnableQuery,
                    ClassName = entitySetName,
                    MethodName = method.Name,
                    RequiresAuthorization = merged.RequiresAuthorization,
                    AllowAnonymous = merged.AllowAnonymous,
                    Roles = merged.Roles,
                    Policies = merged.Policies,
                    Parameters = parameters,
                    ReturnType = returnType,
                    IsAsync = isAsync,
                    SecurityAttributes = merged.SecurityAttributes
                });
            }

            return endpoints;
        }

        private static string GetEntitySetName(Type type)
        {
            var name = type.Name;
            if (name.EndsWith("ODataController", StringComparison.Ordinal))
                return name.Substring(0, name.Length - "ODataController".Length);
            if (name.EndsWith("Controller", StringComparison.Ordinal))
                return name.Substring(0, name.Length - "Controller".Length);
            return name;
        }

        private string GetRoutePrefix(Type type, string entitySetName)
        {
            // Check [ODataRoutePrefix] → fall back to [Route] → fall back to odata/{entitySetName}
            var odataPrefix = GetNamedAttributeTemplate(type, "ODataRoutePrefixAttribute");
            if (odataPrefix != null) return odataPrefix;

            var routeTemplate = _attributeAnalyzer.GetRouteTemplate(type);
            if (routeTemplate != null) return routeTemplate;

            return $"odata/{entitySetName}";
        }

        private string GetMethodRoute(MethodInfo method)
        {
            // Check [ODataRoute] → fall back to HTTP attribute template → fall back to empty
            var odataRoute = GetNamedAttributeTemplate(method, "ODataRouteAttribute");
            if (odataRoute != null) return odataRoute;

            var httpTemplate = _attributeAnalyzer.GetRouteTemplate(method);
            return httpTemplate;
        }

        private static string GetNamedAttributeTemplate(MemberInfo member, string attributeName)
        {
            try
            {
                var attr = Attribute.GetCustomAttributes(member, true)
                    .FirstOrDefault(a => a.GetType().Name == attributeName);
                if (attr != null)
                {
                    var templateProp = attr.GetType().GetProperty("Template");
                    return templateProp?.GetValue(attr) as string;
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            return null;
        }

        private static string BuildRoute(string prefix, string methodRoute)
        {
            if (!string.IsNullOrEmpty(methodRoute))
            {
                prefix = prefix.TrimEnd('/');
                methodRoute = methodRoute.TrimStart('/');
                return $"{prefix}/{methodRoute}";
            }
            return prefix;
        }

        private static bool HasEnableQueryAttribute(MethodInfo method)
        {
            try
            {
                return Attribute.GetCustomAttributes(method, true)
                    .Any(a => a.GetType().Name == "EnableQueryAttribute");
            }
            catch
            {
                return false;
            }
        }

        private static string InferHttpMethod(string methodName)
        {
            if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("List", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Find", StringComparison.OrdinalIgnoreCase))
                return "GET";

            if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase))
                return "POST";

            if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase))
                return "PUT";

            if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase))
                return "DELETE";

            if (methodName.StartsWith("Patch", StringComparison.OrdinalIgnoreCase))
                return "PATCH";

            return "GET";
        }
    }
}
