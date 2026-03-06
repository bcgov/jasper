# JASPER Version Tracking

## Overview

JASPER uses automatic versioning based on GitHub Actions workflow run numbers to track releases and enable version change notifications. The version is exposed through the `/api/application/info` endpoint.

## How It Works

### Backend Implementation

1. **Version in api.csproj**: Major.Minor prefix + GitHub Actions run_number as patch

   ```xml
   <VersionPrefix>1.0</VersionPrefix>
   <Version Condition="'$(BuildNumber)' != ''">$(VersionPrefix).$(BuildNumber)</Version>
   <Version Condition="'$(BuildNumber)' == ''">$(VersionPrefix).0</Version>
   ```

2. **GitHub Actions**: Injects the workflow run_number during build

   ```bash
   dotnet build /p:BuildNumber=${{ github.run_number }}
   ```

3. **API Endpoint**: Returns version in `/api/application/info`
   ```json
   {
     "version": "1.0.456",
     "nutrientFeLicenseKey": "...",
     "environment": "Production"
   }
   ```

### Version Format

- **Format**: `{Major}.{Minor}.{RunNumber}`
- **Example**: `1.0.456`
- **Major.Minor**: Manually set in api.csproj (e.g., `1.0`, `1.1`, `2.0`)
- **RunNumber**: Automatically incremented by GitHub Actions with each workflow run
- **Local Development**: Defaults to `1.0.0` when BuildNumber is not set

## Frontend Integration

### Detecting New Releases

To notify users when a new version is deployed:

```typescript
// Store the version when app loads
let currentVersion: string | null = null;

// Fetch version on app initialization
async function initializeVersion() {
  const response = await fetch('/api/application/info');
  const data = await response.json();
  currentVersion = data.version;
}

// Poll periodically or on visibility change
async function checkForNewVersion() {
  const response = await fetch('/api/application/info');
  const data = await response.json();

  if (currentVersion && data.version !== currentVersion) {
    // Show notification to user
    showUpdateNotification(
      'A new version of JASPER is available. Please refresh.'
    );
    return true;
  }
  return false;
}

// Example: Check every 5 minutes
setInterval(checkForNewVersion, 5 * 60 * 1000);

// Example: Check when tab becomes visible
document.addEventListener('visibilitychange', () => {
  if (!document.hidden) {
    checkForNewVersion();
  }
});
```

### Vue.js Example

```typescript
// composables/useVersionCheck.ts
import { ref, onMounted } from 'vue';

export function useVersionCheck() {
  const currentVersion = ref<string | null>(null);
  const hasNewVersion = ref(false);

  async function fetchVersion(): Promise<string> {
    const response = await fetch('/api/application/info');
    const data = await response.json();
    return data.version;
  }

  async function checkVersion() {
    const latestVersion = await fetchVersion();

    if (!currentVersion.value) {
      currentVersion.value = latestVersion;
    } else if (currentVersion.value !== latestVersion) {
      hasNewVersion.value = true;
    }
  }

  onMounted(async () => {
    await checkVersion();

    // Check every 5 minutes
    setInterval(checkVersion, 5 * 60 * 1000);

    // Check when tab becomes visible
    document.addEventListener('visibilitychange', () => {
      if (!document.hidden) checkVersion();
    });
  });

  return {
    currentVersion,
    hasNewVersion,
    checkVersion,
  };
}
```

## Updating the Version

### For Minor/Patch Releases

No action needed - Git SHA automatically changes with each commit.

### For Major/Minor Releases

Update the semantic version in `api/api.csproj`:

```xml
<PropertyGroup>
  <Version>1.1.0</Version>  <!-- Update this -->
  <InformationalVersion>$(Version)+$(SourceRevisionId)</InformationalVersion>
</PropertyGroup>
```

### Using Git Tags (Optional)

## Updating the Version

### For Most Deployments

No action needed - the patch version (run_number) automatically increments with each GitHub Actions workflow run.

- **Run 100**: `1.0.100`
- **Run 101**: `1.0.101`
- **Run 102**: `1.0.102`

### For Major/Minor Releases

Update the version prefix in `api/api.csproj`:

```xml
<PropertyGroup>
  <VersionPrefix>1.1</VersionPrefix>  <!-- Changed from 1.0 to 1.1 -->
  <Version Condition="'$(BuildNumber)' != ''">$(VersionPrefix).$(BuildNumber)</Version>
  <Version Condition="'$(BuildNumber)' == ''">$(VersionPrefix).0</Version>
</PropertyGroup>
```

After updating, the next deployment will be `1.1.{next_run_number}`.

### Using Git Tags (Optional)

For tracking major releases, create Git tags:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Benefits

1. ✅ **Automatic**: Version increments with every deployment
2. ✅ **Unique**: Each deployment has a unique, sequential version number
3. ✅ **Simple**: Easy to compare versions (1.0.101 > 1.0.100)
4. ✅ **No Manual Work**: No need to remember to bump patch numbers
5. ✅ **Frontend Detection**: Simple numeric comparison to detect new versions
6. ✅ **Traceable**: Run number links directly to GitHub Actions workflow

## Testing Locally

When running locally, the version defaults to `{VersionPrefix}.0`:

- **Local**: `1.0.0`
- **CI/CD Run 456**: `1.0.456`

To test with a specific build number locally:

```bash
dotnet build /p:BuildNumber=999
# Result: 1.0.999
```

## Workflow Run Number

The GitHub Actions `run_number` is:

- Repository-specific sequential counter
- Starts at 1 for each repository
- Increments with each workflow run
- Persists across branches
- Never resets (unlike run_id which is random)

You can view the run number in GitHub Actions:
`https://github.com/{org}/{repo}/actions/runs/{run_number}`
