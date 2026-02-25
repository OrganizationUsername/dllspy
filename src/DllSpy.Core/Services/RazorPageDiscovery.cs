using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DllSpy.Core.Contracts;
using DllSpy.Core.Helpers;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Discovers Razor Page handlers by scanning for types that inherit from PageModel.
    /// </summary>
    internal class RazorPageDiscovery : IDiscovery
    {
        private readonly AttributeAnalyzer _attributeAnalyzer;
        private readonly SecurityResolver _security;

        private static readonly Regex HandlerPattern = new Regex(
            @"^On(Get|Post|Put|Delete|Patch|Head|Options)(\w+)?$",
            RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of <see cref="RazorPageDiscovery"/>.
        /// </summary>
        public RazorPageDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            _attributeAnalyzer = attributeAnalyzer ?? throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.RazorPage;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            foreach (var type in ReflectionHelper.GetTypesSafe(assembly))
            {
                if (IsPageModel(type))
                {
                    surfaces.AddRange(DiscoverHandlers(type));
                }
            }

            return surfaces;
        }

        private List<RazorPageHandler> DiscoverHandlers(Type pageModelType)
        {
            var handlers = new List<RazorPageHandler>();
            var pageModelName = pageModelType.Name;
            var pageRoute = InferPageRoute(pageModelType);
            var classSec = _security.ReadClass(pageModelType);

            var methods = pageModelType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                if (method.IsSpecialName) continue;

                var match = HandlerPattern.Match(method.Name);
                if (!match.Success) continue;

                var httpMethod = match.Groups[1].Value.ToUpperInvariant();
                var suffix = match.Groups[2].Success ? match.Groups[2].Value : null;

                // Strip "Async" suffix to get the named handler
                if (suffix != null && suffix.EndsWith("Async", StringComparison.Ordinal))
                {
                    suffix = suffix.Substring(0, suffix.Length - 5);
                }
                var handlerName = !string.IsNullOrEmpty(suffix) ? suffix : null;

                var merged = _security.Merge(classSec, method);
                var parameters = ReflectionHelper.GetParameters(method, _attributeAnalyzer);

                // Also scan for [BindProperty] properties as parameters
                var bindProperties = GetBindProperties(pageModelType);
                parameters.AddRange(bindProperties);

                var returnType = ReflectionHelper.GetFriendlyTypeName(method.ReturnType);
                var isAsync = ReflectionHelper.IsAsyncMethod(method);

                handlers.Add(new RazorPageHandler
                {
                    PageRoute = pageRoute,
                    HttpMethod = httpMethod,
                    HandlerName = handlerName,
                    PageModelName = pageModelName,
                    ClassName = pageModelName,
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

            return handlers;
        }

        private static bool IsPageModel(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            var current = type.BaseType;
            while (current != null)
            {
                if (current.Name == "PageModel")
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        private static string InferPageRoute(Type pageModelType)
        {
            var ns = pageModelType.Namespace ?? string.Empty;
            var segments = ns.Split('.');

            // Find the "Pages" segment and take everything after it
            var pagesIndex = -1;
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.Equals(segments[i], "Pages", StringComparison.OrdinalIgnoreCase))
                {
                    pagesIndex = i;
                    break;
                }
            }

            var routeParts = new List<string>();
            if (pagesIndex >= 0 && pagesIndex < segments.Length - 1)
            {
                for (int i = pagesIndex + 1; i < segments.Length; i++)
                {
                    routeParts.Add(segments[i]);
                }
            }

            // Class name minus "Model" suffix
            var className = pageModelType.Name;
            if (className.EndsWith("Model", StringComparison.Ordinal) && className.Length > 5)
            {
                className = className.Substring(0, className.Length - 5);
            }
            routeParts.Add(className);

            return "/" + string.Join("/", routeParts);
        }

        private static List<EndpointParameterInfo> GetBindProperties(Type pageModelType)
        {
            var parameters = new List<EndpointParameterInfo>();

            foreach (var prop in pageModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var hasBindProperty = prop.GetCustomAttributes(true)
                    .Any(a => a.GetType().Name == "BindPropertyAttribute");

                if (hasBindProperty)
                {
                    parameters.Add(new EndpointParameterInfo
                    {
                        Name = prop.Name,
                        Type = ReflectionHelper.GetFriendlyTypeName(prop.PropertyType),
                        IsRequired = !ReflectionHelper.IsNullableType(prop.PropertyType),
                        Source = ParameterSource.Form
                    });
                }
            }

            return parameters;
        }
    }
}
