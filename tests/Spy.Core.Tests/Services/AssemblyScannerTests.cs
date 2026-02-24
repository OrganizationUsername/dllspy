using System;
using System.IO;
using System.Linq;
using Spy.Core.Contracts;
using Spy.Core.Services;
using Spy.Core.Tests.Fixtures;
using Xunit;

namespace Spy.Core.Tests.Services
{
    public class AssemblyScannerTests
    {
        private readonly AssemblyScanner _scanner;
        private readonly AssemblyReport _report;

        public AssemblyScannerTests()
        {
            _scanner = ScannerFactory.Create();
            _report = _scanner.ScanAssembly(typeof(UsersController).Assembly);
        }

        [Fact]
        public void ScanAssembly_ReturnsBothHttpAndSignalRSurfaces()
        {
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.HttpEndpoint);
            Assert.Contains(_report.Surfaces, s => s.SurfaceType == SurfaceType.SignalRMethod);
        }

        [Fact]
        public void TotalSurfaces_IsCorrect()
        {
            // 12 HTTP + 5 SignalR = 17
            Assert.Equal(17, _report.TotalSurfaces);
        }

        [Fact]
        public void TotalHttpEndpoints_IsCorrect()
        {
            Assert.Equal(12, _report.TotalHttpEndpoints);
        }

        [Fact]
        public void TotalSignalRMethods_IsCorrect()
        {
            Assert.Equal(5, _report.TotalSignalRMethods);
        }

        [Fact]
        public void TotalClasses_IsCorrect()
        {
            // Users, Admin, Public, Plain, ChatHub, NotificationHub, LifecycleHub
            Assert.Equal(7, _report.TotalClasses);
        }

        [Fact]
        public void AuthenticatedSurfaces_CountIsCorrect()
        {
            // Update, Delete, GetDashboard, CreateSetting, Subscribe, Broadcast
            Assert.Equal(6, _report.AuthenticatedSurfaces);
        }

        [Fact]
        public void AnonymousSurfaces_CountIsCorrect()
        {
            // AllowAnonymous or !RequiresAuthorization = 11
            Assert.Equal(11, _report.AnonymousSurfaces);
        }

        [Fact]
        public void HighSeverityIssues_ForUnauthenticatedStateChanging()
        {
            var highIssues = _report.SecurityIssues.Where(i => i.Severity == SecuritySeverity.High).ToList();
            // HTTP: UsersController.Create (POST), PublicController.Submit (POST)
            // SignalR: ChatHub.SendMessage, ChatHub.JoinRoom, LifecycleHub.SendPing
            Assert.Equal(5, highIssues.Count);
        }

        [Fact]
        public void MediumSeverityIssues_ForMissingAuthDeclaration()
        {
            var mediumIssues = _report.SecurityIssues.Where(i => i.Severity == SecuritySeverity.Medium).ToList();
            // UsersController.GetAll, UsersController.GetById, PlainController.Index, PlainController.Details
            Assert.Equal(4, mediumIssues.Count);
        }

        [Fact]
        public void LowSeverityIssues_ForAuthWithoutRoles()
        {
            var lowIssues = _report.SecurityIssues.Where(i => i.Severity == SecuritySeverity.Low).ToList();
            // UsersController.Update, NotificationHub.Subscribe
            Assert.Equal(2, lowIssues.Count);
        }

        [Fact]
        public void TotalSecurityIssues_IsCorrect()
        {
            Assert.Equal(11, _report.TotalSecurityIssues);
        }

        [Fact]
        public void ScanAssembly_WithNullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _scanner.ScanAssembly((string)null));
        }

        [Fact]
        public void ScanAssembly_WithEmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _scanner.ScanAssembly(""));
        }

        [Fact]
        public void ScanAssembly_WithNonexistentPath_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => _scanner.ScanAssembly("/nonexistent/path.dll"));
        }

        [Fact]
        public void ScanAssembly_WithNonAssemblyFile_ThrowsInvalidOperationException()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "not a .NET assembly");
                Assert.Throws<InvalidOperationException>(() => _scanner.ScanAssembly(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Report_HasAssemblyName()
        {
            Assert.False(string.IsNullOrEmpty(_report.AssemblyName));
        }

        [Fact]
        public void Report_HasScanTimestamp()
        {
            Assert.True(_report.ScanTimestamp > DateTime.MinValue);
            Assert.True(_report.ScanTimestamp <= DateTime.UtcNow.AddSeconds(1));
        }
    }
}
