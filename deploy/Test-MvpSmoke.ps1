#requires -Version 7.0

[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$ExpectedVersion,
    [string]$ComposeProjectName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$BaseUrl = $BaseUrl.TrimEnd("/")
$script:Session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$script:TemporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "khaikang-smoke-$([guid]::NewGuid().ToString('N'))"

function Write-SmokeStep {
    param([Parameter(Mandatory)][string]$Message)

    Write-Host "[smoke] $Message" -ForegroundColor Cyan
}

function Assert-True {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw "Smoke assertion failed: $Message"
    }
}

function Assert-Equal {
    param(
        [AllowNull()]$Actual,
        [AllowNull()]$Expected,
        [Parameter(Mandatory)][string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "Smoke assertion failed: $Message. Expected '$Expected', actual '$Actual'."
    }
}

function Invoke-KhaiKangRequest {
    param(
        [Parameter(Mandatory)][ValidateSet("GET", "POST", "PUT", "DELETE")][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [int[]]$ExpectedStatus = @(200),
        [AllowNull()]$Body,
        [string]$CsrfToken
    )

    $request = @{
        Uri = "$BaseUrl$Path"
        Method = $Method
        WebSession = $script:Session
        SkipHttpErrorCheck = $true
    }

    if ($PSBoundParameters.ContainsKey("Body") -and $null -ne $Body) {
        $request.Body = $Body | ConvertTo-Json -Depth 20 -Compress
        $request.ContentType = "application/json"
    }

    if (-not [string]::IsNullOrWhiteSpace($CsrfToken)) {
        $request.Headers = @{ "X-XSRF-TOKEN" = $CsrfToken }
    }

    $response = Invoke-WebRequest @request
    $statusCode = [int]$response.StatusCode
    if ($statusCode -notin $ExpectedStatus) {
        throw "$Method $Path returned HTTP $statusCode. Expected $($ExpectedStatus -join ', '). Body: $($response.Content)"
    }

    if ([string]::IsNullOrWhiteSpace($response.Content)) {
        return $null
    }

    return $response.Content | ConvertFrom-Json -Depth 20
}

function Wait-KhaiKangReady {
    param([int]$TimeoutSeconds = 180)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest `
                -Uri "$BaseUrl/api/v1/system/info" `
                -Method Get `
                -SkipHttpErrorCheck `
                -TimeoutSec 5
            if ([int]$response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            # Startup failures are reported after the bounded readiness window.
        }

        Start-Sleep -Seconds 1
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "KhaiKang did not become ready at $BaseUrl within $TimeoutSeconds seconds."
}

function Get-CsrfToken {
    $payload = Invoke-KhaiKangRequest -Method GET -Path "/api/v1/auth/csrf-token"
    Assert-True (-not [string]::IsNullOrWhiteSpace($payload.token)) "CSRF token must be returned"
    return [string]$payload.token
}

function Invoke-KhaiKangMutation {
    param(
        [Parameter(Mandatory)][ValidateSet("POST", "PUT", "DELETE")][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [int[]]$ExpectedStatus = @(200),
        [AllowNull()]$Body
    )

    $arguments = @{
        Method = $Method
        Path = $Path
        ExpectedStatus = $ExpectedStatus
        CsrfToken = Get-CsrfToken
    }
    if ($PSBoundParameters.ContainsKey("Body")) {
        $arguments.Body = $Body
    }

    return Invoke-KhaiKangRequest @arguments
}

function Send-KhaiKangFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$FilePath,
        [int[]]$ExpectedStatus = @(201)
    )

    $response = Invoke-WebRequest `
        -Uri "$BaseUrl$Path" `
        -Method Post `
        -WebSession $script:Session `
        -Headers @{ "X-XSRF-TOKEN" = Get-CsrfToken } `
        -Form @{ file = Get-Item -LiteralPath $FilePath } `
        -SkipHttpErrorCheck

    $statusCode = [int]$response.StatusCode
    if ($statusCode -notin $ExpectedStatus) {
        throw "POST $Path returned HTTP $statusCode. Expected $($ExpectedStatus -join ', '). Body: $($response.Content)"
    }

    return $response.Content | ConvertFrom-Json -Depth 20
}

function Test-KhaiKangDownload {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$ExpectedFilePath
    )

    $downloadPath = Join-Path $script:TemporaryDirectory "$([guid]::NewGuid().ToString('N')).download"
    $response = Invoke-WebRequest `
        -Uri "$BaseUrl$Path" `
        -Method Get `
        -WebSession $script:Session `
        -OutFile $downloadPath `
        -PassThru `
        -SkipHttpErrorCheck
    Assert-Equal ([int]$response.StatusCode) 200 "Attachment download must succeed for $Path"

    $expectedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $ExpectedFilePath).Hash
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $downloadPath).Hash
    Assert-Equal $actualHash $expectedHash "Downloaded attachment hash must match for $Path"
}

function Get-ComposeServiceContainers {
    param([Parameter(Mandatory)][string]$ProjectName)

    $containers = @{}
    $lines = & docker ps `
        --filter "label=com.docker.compose.project=$ProjectName" `
        --format '{{.ID}}|{{.Label "com.docker.compose.service"}}'
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to inspect Compose project '$ProjectName'."
    }

    foreach ($line in $lines) {
        $parts = $line -split "\|", 2
        if ($parts.Count -eq 2) {
            $containers[$parts[1]] = $parts[0]
        }
    }

    foreach ($service in @("postgres", "api", "web")) {
        Assert-True $containers.ContainsKey($service) "Compose project '$ProjectName' must contain a running $service service"
    }

    return $containers
}

function Restart-KhaiKangStack {
    param([Parameter(Mandatory)][string]$ProjectName)

    $containers = Get-ComposeServiceContainers -ProjectName $ProjectName
    foreach ($service in @("postgres", "api", "web")) {
        Write-SmokeStep "Restarting $service container"
        & docker restart $containers[$service] | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to restart the $service container in Compose project '$ProjectName'."
        }
    }

    Wait-KhaiKangReady
}

New-Item -ItemType Directory -Path $script:TemporaryDirectory | Out-Null

try {
    Write-SmokeStep "Waiting for API readiness"
    Wait-KhaiKangReady

    $systemInfo = Invoke-KhaiKangRequest -Method GET -Path "/api/v1/system/info"
    if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion)) {
        Assert-Equal $systemInfo.version $ExpectedVersion "API version must match"
    }

    Write-SmokeStep "Initializing a fresh administrator session"
    $setupStatus = Invoke-KhaiKangRequest -Method GET -Path "/api/v1/setup/status"
    Assert-True ([bool]$setupStatus.requiresInitialization) "MVP smoke test requires a fresh database"

    $credentials = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/setup/initialize"
    Assert-Equal $credentials.username "admin" "Initial administrator username"
    Assert-True ($credentials.initialPassword.Length -ge 12) "Initial administrator password must be generated"

    $login = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/auth/login" `
        -Body @{
            username = $credentials.username
            password = $credentials.initialPassword
            rememberMe = $false
        }
    Assert-True ([bool]$login.mustChangePassword) "Initial administrator must be prompted to change password"

    Write-SmokeStep "Creating two projects and one issue"
    $project = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/projects" `
        -ExpectedStatus @(201) `
        -Body @{
            code = "SMOKE"
            name = "MVP Smoke Project"
            description = "Created by deploy/Test-MvpSmoke.ps1"
        }
    $secondaryProject = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/projects" `
        -ExpectedStatus @(201) `
        -Body @{
            code = "AUX"
            name = "MVP Auxiliary Project"
            description = "Validates multiple project links."
        }
    Assert-Equal $project.code "SMOKE" "Primary project code must be normalized"

    $issue = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/projects/$($project.id)/issues" `
        -ExpectedStatus @(201) `
        -Body @{
            title = "Verify the RC deployment"
            typeCode = "task"
        }
    Assert-Equal $issue.key "SMOKE-1" "First issue key"

    $issueEvidencePath = Join-Path $script:TemporaryDirectory "issue-evidence.txt"
    [IO.File]::WriteAllText($issueEvidencePath, "KhaiKang issue smoke evidence", [Text.Encoding]::UTF8)
    $issueAttachment = Send-KhaiKangFile `
        -Path "/api/v1/projects/$($project.id)/issues/$($issue.id)/attachments" `
        -FilePath $issueEvidencePath
    Assert-Equal $issueAttachment.originalFileName "issue-evidence.txt" "Issue attachment filename"
    Test-KhaiKangDownload `
        -Path "/api/v1/projects/$($project.id)/issues/$($issue.id)/attachments/$($issueAttachment.id)/content" `
        -ExpectedFilePath $issueEvidencePath

    Write-SmokeStep "Creating a workspace linked to multiple projects"
    $workspace = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/test-workspaces" `
        -ExpectedStatus @(201) `
        -Body @{
            name = "MVP Release Verification"
            prefix = "MVP"
            description = "Fresh-image smoke workspace."
        }
    foreach ($projectId in @($project.id, $secondaryProject.id)) {
        $null = Invoke-KhaiKangMutation `
            -Method POST `
            -Path "/api/v1/test-workspaces/$($workspace.id)/projects" `
            -ExpectedStatus @(201) `
            -Body @{ projectId = $projectId }
    }
    $projectLinks = @(Invoke-KhaiKangRequest `
        -Method GET `
        -Path "/api/v1/test-workspaces/$($workspace.id)/projects")
    Assert-Equal $projectLinks.Count 2 "Workspace must retain both project links"

    Write-SmokeStep "Creating suite, tag, case, and case attachment"
    $suite = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/test-workspaces/$($workspace.id)/suites" `
        -ExpectedStatus @(201) `
        -Body @{
            parentId = $null
            name = "Deployment"
            description = "RC deployment coverage"
            sortOrder = 1
        }
    $tag = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/test-tags" `
        -ExpectedStatus @(201) `
        -Body @{
            name = "mvp-smoke"
            description = "Created by the deployment smoke test."
        }

    $snapshotTitle = "Published image starts from fresh volumes"
    $snapshotAction = "Start the immutable RC images."
    $snapshotExpected = "API and Web become healthy after migrations."
    $testCase = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/test-workspaces/$($workspace.id)/cases" `
        -ExpectedStatus @(201) `
        -Body @{
            suiteId = $suite.id
            title = $snapshotTitle
            description = "## RC smoke\n\nVerifies a **fresh** deployment."
            preconditions = "Fresh PostgreSQL and attachment volumes."
            overallExpectedResult = "The deployment is ready for MVP acceptance."
            sortOrder = 1
            steps = @(
                @{
                    action = $snapshotAction
                    expectedResult = $snapshotExpected
                }
            )
            tagIds = @($tag.id)
        }
    Assert-Equal @($testCase.steps).Count 1 "Test case must contain one step"

    $caseEvidencePath = Join-Path $script:TemporaryDirectory "case-evidence.png"
    [IO.File]::WriteAllBytes(
        $caseEvidencePath,
        [byte[]](0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a))
    $caseAttachment = Send-KhaiKangFile `
        -Path "/api/v1/test-workspaces/$($workspace.id)/cases/$($testCase.id)/attachments" `
        -FilePath $caseEvidencePath
    Assert-Equal $caseAttachment.originalFileName "case-evidence.png" "Case attachment filename"
    Test-KhaiKangDownload `
        -Path "/api/v1/test-workspaces/$($workspace.id)/cases/$($testCase.id)/attachments/$($caseAttachment.id)/content" `
        -ExpectedFilePath $caseEvidencePath

    Write-SmokeStep "Creating and activating a test plan"
    $plan = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/test-workspaces/$($workspace.id)/plans" `
        -ExpectedStatus @(201) `
        -Body @{
            name = "MVP RC acceptance"
            description = "Fixed smoke-test scope."
            caseIds = @($testCase.id)
        }
    Assert-Equal $plan.status "draft" "New plan status"
    $plan = Invoke-KhaiKangMutation `
        -Method PUT `
        -Path "/api/v1/test-workspaces/$($workspace.id)/plans/$($plan.id)" `
        -Body @{
            name = $plan.name
            description = $plan.description
            status = "active"
            version = $plan.version
            caseIds = @($testCase.id)
        }
    Assert-Equal $plan.status "active" "Activated plan status"

    Write-SmokeStep "Creating a run and verifying immutable case snapshots"
    $run = Invoke-KhaiKangMutation `
        -Method POST `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs" `
        -ExpectedStatus @(201) `
        -Body @{
            planId = $plan.id
            name = "MVP RC smoke run"
        }
    $runItem = @($run.items)[0]
    $runStep = @($runItem.steps)[0]
    Assert-Equal $runItem.caseTitle $snapshotTitle "Run case title snapshot"
    Assert-Equal $runStep.action $snapshotAction "Run action snapshot"
    Assert-Equal $runStep.expectedResult $snapshotExpected "Run expected-result snapshot"

    $updatedCase = Invoke-KhaiKangMutation `
        -Method PUT `
        -Path "/api/v1/test-workspaces/$($workspace.id)/cases/$($testCase.id)" `
        -Body @{
            suiteId = $suite.id
            title = "Source case changed after run creation"
            description = $testCase.description
            preconditions = $testCase.preconditions
            overallExpectedResult = $testCase.overallExpectedResult
            sortOrder = $testCase.sortOrder
            status = $testCase.status
            version = $testCase.version
            steps = @(
                @{
                    action = "Changed source action."
                    expectedResult = "Changed source result."
                }
            )
            tagIds = @($tag.id)
        }
    Assert-True ($updatedCase.version -gt $testCase.version) "Source case version must advance"

    $run = Invoke-KhaiKangRequest `
        -Method GET `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)"
    $runItem = @($run.items)[0]
    $runStep = @($runItem.steps)[0]
    Assert-Equal $runItem.caseTitle $snapshotTitle "Run title snapshot after source edit"
    Assert-Equal $runStep.action $snapshotAction "Run action snapshot after source edit"
    Assert-Equal $runStep.expectedResult $snapshotExpected "Run expected-result snapshot after source edit"

    Write-SmokeStep "Recording run evidence and completing the execution"
    $run = Invoke-KhaiKangMutation `
        -Method PUT `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)/items/$($runItem.id)/steps/$($runStep.id)" `
        -Body @{
            status = "passed"
            actualResult = "The services became healthy."
            version = $runStep.version
        }
    $runItem = @($run.items)[0]

    $runEvidencePath = Join-Path $script:TemporaryDirectory "run-evidence.txt"
    [IO.File]::WriteAllText($runEvidencePath, "KhaiKang run smoke evidence", [Text.Encoding]::UTF8)
    $runAttachment = Send-KhaiKangFile `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)/items/$($runItem.id)/attachments" `
        -FilePath $runEvidencePath
    Assert-Equal $runAttachment.originalFileName "run-evidence.txt" "Run attachment filename"
    Test-KhaiKangDownload `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)/items/$($runItem.id)/attachments/$($runAttachment.id)/content" `
        -ExpectedFilePath $runEvidencePath

    $run = Invoke-KhaiKangMutation `
        -Method PUT `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)/items/$($runItem.id)" `
        -Body @{
            status = "passed"
            actualResult = "MVP scenario passed."
            version = $runItem.version
        }
    Assert-Equal $run.progress.passed 1 "Run progress must contain one passed case"

    $run = Invoke-KhaiKangMutation `
        -Method PUT `
        -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)/status" `
        -Body @{
            status = "completed"
            summary = "MVP smoke test completed."
            version = $run.version
        }
    Assert-Equal $run.status "completed" "Completed run status"

    if (-not [string]::IsNullOrWhiteSpace($ComposeProjectName)) {
        Write-SmokeStep "Restarting the isolated Compose stack and rechecking persistence"
        Restart-KhaiKangStack -ProjectName $ComposeProjectName

        $script:Session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
        $null = Invoke-KhaiKangMutation `
            -Method POST `
            -Path "/api/v1/auth/login" `
            -Body @{
                username = $credentials.username
                password = $credentials.initialPassword
                rememberMe = $false
            }

        $persistedProject = Invoke-KhaiKangRequest -Method GET -Path "/api/v1/projects/$($project.id)"
        Assert-Equal $persistedProject.id $project.id "Project must persist after restart"
        $persistedIssue = Invoke-KhaiKangRequest `
            -Method GET `
            -Path "/api/v1/projects/$($project.id)/issues/$($issue.id)"
        Assert-Equal $persistedIssue.id $issue.id "Issue must persist after restart"
        $persistedWorkspace = Invoke-KhaiKangRequest `
            -Method GET `
            -Path "/api/v1/test-workspaces/$($workspace.id)"
        Assert-Equal $persistedWorkspace.id $workspace.id "Workspace must persist after restart"
        $persistedRun = Invoke-KhaiKangRequest `
            -Method GET `
            -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)"
        Assert-Equal $persistedRun.status "completed" "Completed run must persist after restart"
        Assert-Equal @($persistedRun.items)[0].caseTitle $snapshotTitle "Run snapshot must persist after restart"

        Test-KhaiKangDownload `
            -Path "/api/v1/projects/$($project.id)/issues/$($issue.id)/attachments/$($issueAttachment.id)/content" `
            -ExpectedFilePath $issueEvidencePath
        Test-KhaiKangDownload `
            -Path "/api/v1/test-workspaces/$($workspace.id)/cases/$($testCase.id)/attachments/$($caseAttachment.id)/content" `
            -ExpectedFilePath $caseEvidencePath
        Test-KhaiKangDownload `
            -Path "/api/v1/test-workspaces/$($workspace.id)/runs/$($run.id)/items/$($runItem.id)/attachments/$($runAttachment.id)/content" `
            -ExpectedFilePath $runEvidencePath
    }

    Write-SmokeStep "MVP smoke test passed"
    [pscustomobject]@{
        apiVersion = $systemInfo.version
        projectId = $project.id
        issueId = $issue.id
        workspaceId = $workspace.id
        caseId = $testCase.id
        planId = $plan.id
        runId = $run.id
        restartVerified = -not [string]::IsNullOrWhiteSpace($ComposeProjectName)
    } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $script:TemporaryDirectory) {
        Remove-Item -LiteralPath $script:TemporaryDirectory -Recurse -Force
    }
}
