using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DllSpy.Core.Contracts;

namespace DllSpy.Cli
{
    internal static class OutputWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        private static bool UseColor =>
            !Console.IsOutputRedirected &&
            Environment.GetEnvironmentVariable("NO_COLOR") is null;

        private static bool IsTty => !Console.IsOutputRedirected;

        // ANSI escape sequences (\e not available in C# 12)
        private const string Esc = "\x1B";
        private const string Reset = Esc + "[0m";
        private const string Bold = Esc + "[1m";
        private const string Dim = Esc + "[2m";
        private const string Red = Esc + "[31m";
        private const string Green = Esc + "[32m";
        private const string Yellow = Esc + "[33m";
        private const string Cyan = Esc + "[36m";

        private static string Colorize(string text, string color) =>
            UseColor ? $"{color}{text}{Reset}" : text;

        public static void PrintSurfaces(List<InputSurface> surfaces, OutputFormat? format)
        {
            var resolved = format ?? (IsTty ? OutputFormat.Table : OutputFormat.Tsv);

            if (resolved == OutputFormat.Json) { PrintJson(surfaces); return; }

            if (surfaces.Count == 0)
            {
                Console.Error.WriteLine("No surfaces found.");
                return;
            }

            if (resolved == OutputFormat.Table)
                PrintSurfacesTable(surfaces);
            else
                PrintSurfacesTsv(surfaces);

            Console.Error.WriteLine($"\n{surfaces.Count} surface(s) found.");
        }

        public static void PrintIssues(List<SecurityIssue> issues, OutputFormat? format)
        {
            var resolved = format ?? (IsTty ? OutputFormat.Table : OutputFormat.Tsv);

            if (resolved == OutputFormat.Json) { PrintJson(issues); return; }

            if (issues.Count == 0)
            {
                Console.Error.WriteLine("No issues found.");
                return;
            }

            if (resolved == OutputFormat.Table)
                PrintIssuesTable(issues);
            else
                PrintIssuesTsv(issues);

            Console.Error.WriteLine($"\n{issues.Count} issue(s) found.");
        }

        // ── Surfaces: terminal table ──────────────────────────────────

        private static void PrintSurfacesTable(List<InputSurface> surfaces)
        {
            const string typeH = "TYPE", routeH = "ROUTE", classH = "CLASS", methodH = "METHOD", authH = "AUTH";
            const int gaps = 10; // 4 x 2-char gaps + AUTH column ~4
            const int authCol = 4;

            int tw = Math.Max(typeH.Length, surfaces.Max(s => GetTypeLabel(s).Length));
            int rw = Math.Max(routeH.Length, surfaces.Max(s => s.DisplayRoute.Length));
            int cw = Math.Max(classH.Length, surfaces.Max(s => s.ClassName.Length));
            int mw = Math.Max(methodH.Length, surfaces.Max(s => s.MethodName.Length));

            int termWidth = GetTerminalWidth();
            int total = tw + rw + cw + mw + gaps + authCol;
            if (total > termWidth)
            {
                int budget = termWidth - tw - gaps - authCol; // TYPE and AUTH are short, keep them
                // Give ROUTE 50% of remaining budget, CLASS 25%, METHOD 25%
                rw = Math.Max(routeH.Length, budget / 2);
                cw = Math.Max(classH.Length, budget / 4);
                mw = Math.Max(methodH.Length, budget - rw - cw);
            }

            var fmt = $"{{0,-{tw}}}  {{1,-{rw}}}  {{2,-{cw}}}  {{3,-{mw}}}  {{4}}";

            Console.WriteLine(
                Colorize(string.Format(fmt, typeH, routeH, classH, methodH, authH), Bold));
            Console.WriteLine(
                Colorize(new string('─', tw + rw + cw + mw + gaps + authCol), Dim));

            foreach (var s in surfaces)
            {
                var auth = s.RequiresAuthorization ? Colorize("Yes", Green)
                         : s.AllowAnonymous        ? Colorize("Anon", Yellow)
                         : "No";
                Console.WriteLine(fmt,
                    Truncate(GetTypeLabel(s), tw),
                    Truncate(s.DisplayRoute, rw),
                    Truncate(s.ClassName, cw),
                    Truncate(s.MethodName, mw),
                    auth);
            }
        }

        // ── Surfaces: piped TSV ───────────────────────────────────────

        private static void PrintSurfacesTsv(List<InputSurface> surfaces)
        {
            Console.WriteLine("TYPE\tROUTE\tCLASS\tMETHOD\tAUTH");
            foreach (var s in surfaces)
            {
                var auth = s.RequiresAuthorization ? "Yes" : s.AllowAnonymous ? "Anon" : "No";
                Console.WriteLine($"{GetTypeLabel(s)}\t{s.DisplayRoute}\t{s.ClassName}\t{s.MethodName}\t{auth}");
            }
        }

        // ── Issues: terminal table ────────────────────────────────────

        private static void PrintIssuesTable(List<SecurityIssue> issues)
        {
            const string sevH = "SEVERITY", typeH = "TYPE", surfH = "SURFACE", titleH = "TITLE";
            const int gaps = 6; // 3 x 2-char gaps

            int sw = Math.Max(sevH.Length, issues.Max(i => i.Severity.ToString().Length));
            int tw = Math.Max(typeH.Length, issues.Max(i => i.SurfaceType.ToString().Length));
            int uw = Math.Max(surfH.Length, issues.Max(i => i.SurfaceRoute.Length));
            int titW = Math.Max(titleH.Length, issues.Max(i => i.Title.Length));

            int termWidth = GetTerminalWidth();
            int total = sw + tw + uw + titW + gaps;
            if (total > termWidth)
            {
                int budget = termWidth - sw - tw - gaps; // SEVERITY and TYPE are short, keep them
                // Give SURFACE 50%, TITLE 50%
                uw = Math.Max(surfH.Length, budget / 2);
                titW = Math.Max(titleH.Length, budget - uw);
            }

            var fmt = $"{{0,-{sw}}}  {{1,-{tw}}}  {{2,-{uw}}}  {{3}}";

            Console.WriteLine(
                Colorize(string.Format(fmt, sevH, typeH, surfH, titleH), Bold));
            Console.WriteLine(
                Colorize(new string('─', sw + tw + uw + titW + gaps), Dim));

            foreach (var i in issues)
            {
                var sev = ColorizeSeverity(i.Severity);
                Console.WriteLine(fmt, sev, Truncate(i.SurfaceType.ToString(), tw),
                    Truncate(i.SurfaceRoute, uw), Truncate(i.Title, titW));
            }
        }

        // ── Issues: piped TSV ─────────────────────────────────────────

        private static void PrintIssuesTsv(List<SecurityIssue> issues)
        {
            Console.WriteLine("SEVERITY\tTYPE\tSURFACE\tTITLE");
            foreach (var i in issues)
                Console.WriteLine($"{i.Severity}\t{i.SurfaceType}\t{i.SurfaceRoute}\t{i.Title}");
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string ColorizeSeverity(SecuritySeverity severity) => severity switch
        {
            SecuritySeverity.Critical => Colorize(severity.ToString(), Red + Bold),
            SecuritySeverity.High     => Colorize(severity.ToString(), Red),
            SecuritySeverity.Medium   => Colorize(severity.ToString(), Yellow),
            SecuritySeverity.Low      => Colorize(severity.ToString(), Cyan),
            _                         => severity.ToString()
        };

        private static string GetTypeLabel(InputSurface surface) => surface.SurfaceType switch
        {
            SurfaceType.HttpEndpoint => "HTTP",
            SurfaceType.SignalRMethod => "SignalR",
            SurfaceType.WcfOperation => "WCF",
            SurfaceType.GrpcOperation => "gRPC",
            SurfaceType.RazorPage => "Razor",
            SurfaceType.BlazorComponent => "Blazor",
            _ => surface.SurfaceType.ToString()
        };

        private static string Truncate(string value, int maxWidth)
        {
            if (value.Length <= maxWidth) return value;
            return maxWidth > 1 ? value.Substring(0, maxWidth - 1) + "…" : value.Substring(0, maxWidth);
        }

        private static int GetTerminalWidth()
        {
            try { return Console.WindowWidth; }
            catch { return 120; }
        }

        private static void PrintJson<T>(T data) =>
            Console.WriteLine(JsonSerializer.Serialize(data, JsonOptions));
    }
}
