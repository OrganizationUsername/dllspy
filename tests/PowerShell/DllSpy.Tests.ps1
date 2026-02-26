param(
    [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
)

BeforeAll {
    $script:RepoRoot = $RepoRoot

    # Build the solution
    dotnet build "$script:RepoRoot/DllSpy.sln" -c Debug --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    Import-Module "$script:RepoRoot/out/DllSpy" -Force

    # The test assembly contains all fixture types (controllers, hubs, pages, components)
    $script:TestDll = "$script:RepoRoot/tests/DllSpy.Core.Tests/bin/Debug/net8.0/DllSpy.Core.Tests.dll"
}

Describe 'Search-DllSpy' {

    It 'Discovers surfaces from the test assembly' {
        $surfaces = Search-DllSpy -Path $script:TestDll
        $surfaces.Count | Should -BeGreaterThan 0
    }

    It 'Returns all surface types' {
        $surfaces = Search-DllSpy -Path $script:TestDll
        $types = $surfaces | ForEach-Object { $_.SurfaceType } | Sort-Object -Unique
        $types | Should -Contain 'HttpEndpoint'
        $types | Should -Contain 'SignalRMethod'
        $types | Should -Contain 'WcfOperation'
        $types | Should -Contain 'GrpcOperation'
        $types | Should -Contain 'RazorPage'
        $types | Should -Contain 'BlazorComponent'
    }

    It 'Filters by -Type' {
        $http = Search-DllSpy -Path $script:TestDll -Type HttpEndpoint
        $http | ForEach-Object { $_.SurfaceType | Should -Be 'HttpEndpoint' }
        $http.Count | Should -BeGreaterThan 0
    }

    It 'Filters by -HttpMethod' {
        $posts = Search-DllSpy -Path $script:TestDll -HttpMethod POST
        $posts | ForEach-Object { $_.SurfaceType | Should -Be 'HttpEndpoint' }
        $posts.Count | Should -BeGreaterThan 0
    }

    It 'Filters by -Class with wildcard' {
        $users = Search-DllSpy -Path $script:TestDll -Class User*
        $users | ForEach-Object { $_.ClassName | Should -BeLike 'User*' }
        $users.Count | Should -BeGreaterThan 0
    }

    It 'Filters by -RequiresAuth' {
        $auth = Search-DllSpy -Path $script:TestDll -RequiresAuth
        $auth | ForEach-Object { $_.RequiresAuthorization | Should -BeTrue }
        $auth.Count | Should -BeGreaterThan 0
    }

    It 'Filters by -AllowAnonymous' {
        $anon = Search-DllSpy -Path $script:TestDll -AllowAnonymous
        $anon | ForEach-Object { $_.AllowAnonymous | Should -BeTrue }
        $anon.Count | Should -BeGreaterThan 0
    }

    It 'Accepts pipeline input' {
        $surfaces = Get-Item $script:TestDll | Search-DllSpy
        $surfaces.Count | Should -BeGreaterThan 0
    }

    It 'Throws on nonexistent path' {
        { Search-DllSpy -Path '/nonexistent/fake.dll' -ErrorAction Stop } | Should -Throw
    }
}

Describe 'Test-DllSpy' {

    It 'Finds security issues' {
        $issues = Test-DllSpy -Path $script:TestDll
        $issues.Count | Should -BeGreaterThan 0
    }

    It 'Returns SecurityIssue objects with expected properties' {
        $issue = (Test-DllSpy -Path $script:TestDll)[0]
        $issue.Title | Should -Not -BeNullOrEmpty
        $issue.Severity | Should -Not -BeNullOrEmpty
        $issue.SurfaceRoute | Should -Not -BeNullOrEmpty
        $issue.ClassName | Should -Not -BeNullOrEmpty
    }

    It 'Filters by -MinimumSeverity' {
        $high = Test-DllSpy -Path $script:TestDll -MinimumSeverity High
        $high | ForEach-Object { $_.Severity | Should -BeIn @('High', 'Critical') }
        $high.Count | Should -BeGreaterThan 0
    }

    It 'Filters by -Type' {
        $httpIssues = Test-DllSpy -Path $script:TestDll -Type HttpEndpoint
        $httpIssues | ForEach-Object { $_.SurfaceType | Should -Be 'HttpEndpoint' }
        $httpIssues.Count | Should -BeGreaterThan 0
    }

    It 'Accepts pipeline input' {
        $issues = Get-Item $script:TestDll | Test-DllSpy
        $issues.Count | Should -BeGreaterThan 0
    }
}

Describe 'Assembly resolution' {

    It 'Discovers expected surface count' {
        $surfaces = Search-DllSpy -Path $script:TestDll
        $surfaces.Count | Should -Be 46
    }
}
