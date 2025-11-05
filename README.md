# Pipedrive CLI

A fast, lightweight command-line interface for managing your Pipedrive CRM. Built with .NET 8 and Native AOT compilation for lightning-fast startup times (~13ms).

## Features

- **⚡ Native AOT Compiled** - 13ms cold start with 12MB binary size
- **🔐 Secure Configuration** - Profile-based config stored at `~/.pipedrive/config.json`
- **🌍 Environment Variables** - Override config values via environment variables
- **🎨 Beautiful Console UI** - Rich formatting with Spectre.Console
- **🔄 Multi-Profile Support** - Manage multiple Pipedrive environments (dev, staging, prod)
- **📝 Full CRM Management** - Complete CRUD operations for leads, deals, persons, organizations, activities, and notes
- **🔀 Entity Merging** - Merge duplicate persons, deals, and organizations with confirmation prompts
- **🏷️ Custom Fields** - Automatic display of custom fields when viewing entities
- **⬆️ Auto-Update** - Background update checking with self-update capability

## Installation

### From Source

```bash
# Clone the repository
git clone https://github.com/stannardlabs/pipedrive-cli.git
cd pipedrive-cli

# Build and publish Native AOT binary
dotnet publish src/PipedriveCLI/PipedriveCLI.csproj -c Release -r linux-x64 --no-self-contained

# Copy binary to your PATH
sudo cp src/PipedriveCLI/bin/Release/net8.0/linux-x64/publish/pipedrive /usr/local/bin/
```

### Using .NET Runtime (Development)

```bash
dotnet run --project src/PipedriveCLI/PipedriveCLI.csproj -- [command] [options]
```

## Quick Start

1. **Configure your Pipedrive credentials:**

```bash
pipedrive config set --api-key YOUR_API_KEY --domain company.pipedrive.com
```

2. **Test the connection:**

```bash
pipedrive config test
```

3. **View your configuration:**

```bash
pipedrive config get
```

## Configuration

### Configuration File

The CLI stores configuration in `~/.pipedrive/config.json` with secure file permissions (600 on Unix systems):

```json
{
  "profiles": {
    "default": {
      "apiKey": "YOUR_API_KEY",
      "domain": "company.pipedrive.com"
    },
    "staging": {
      "apiKey": "STAGING_API_KEY",
      "domain": "staging.pipedrive.com"
    }
  },
  "activeProfile": "default"
}
```

### Environment Variables

You can override configuration values using environment variables:

- `PIPEDRIVE_API_KEY` - Override the API key
- `PIPEDRIVE_DOMAIN` - Override the domain

Example:
```bash
export PIPEDRIVE_API_KEY="temporary-key"
pipedrive config get  # Will show the overridden value
```

### Managing Profiles

**List all profiles:**
```bash
pipedrive config profile list
```

**Switch to a different profile:**
```bash
pipedrive config profile switch staging
```

**Set configuration for a specific profile:**
```bash
# First switch to the profile
pipedrive config profile switch staging

# Then configure it
pipedrive config set --api-key STAGING_KEY --domain staging.pipedrive.com
```

## Commands

### `config` - Configuration Management

Manage CLI configuration and profiles.

#### `config set` - Set Configuration Values

Set one or more configuration values for the active profile.

```bash
# Set API key and domain
pipedrive config set --api-key YOUR_KEY --domain company.pipedrive.com

# Options:
#   --api-key, -k    Pipedrive API key
#   --domain, -d     Pipedrive domain (e.g., company.pipedrive.com)
```

#### `config get` - Display Current Configuration

View the current configuration with masked API keys.

```bash
pipedrive config get
```

Output:
```
╭─────────────┬───────────────────────────────────────╮
│ Setting     │ Value                                 │
├─────────────┼───────────────────────────────────────┤
│ Profile     │ default                               │
│ API Key     │ abc1****xyz9                          │
│ Domain      │ company.pipedrive.com                 │
│ Config File │ /home/user/.pipedrive/config.json     │
╰─────────────┴───────────────────────────────────────╯
```

#### `config test` - Test API Connection

Verify that your API credentials are valid by making a test request to Pipedrive.

```bash
pipedrive config test
```

#### `config profile list` - List All Profiles

Display all available configuration profiles.

```bash
pipedrive config profile list
```

Output:
```
╭───────────┬─────────────────────────┬─────────────╮
│ Profile   │ Domain                  │ Status      │
├───────────┼─────────────────────────┼─────────────┤
│ default   │ company.pipedrive.com   │ ✓ Active    │
│ staging   │ staging.pipedrive.com   │             │
│ prod      │ prod.pipedrive.com      │             │
╰───────────┴─────────────────────────┴─────────────╯
```

#### `config profile switch` - Switch Active Profile

Change which profile is currently active.

```bash
pipedrive config profile switch [profile-name]

# Example:
pipedrive config profile switch staging
```

### `leads` - Leads Management

Manage Pipedrive leads with full CRUD operations and search capabilities.

```bash
# List all leads
pipedrive leads list [--limit 100] [--start 0]

# Get specific lead details
pipedrive leads get <id>

# Create a new lead
pipedrive leads create --title "Enterprise Deal" [--value 50000] [--expected-close-date "2025-12-31"]

# Update an existing lead
pipedrive leads update <id> --title "Updated Title" [--value 75000]

# Delete a lead
pipedrive leads delete <id> [--force]

# Search leads
pipedrive leads search "search term" [--limit 100]
```

### `deals` - Deals Management

Manage deals with status filtering and merge capabilities.

```bash
# List all deals
pipedrive deals list [--status open|won|lost] [--limit 100] [--start 0]

# Get specific deal details (includes custom fields)
pipedrive deals get <id>

# Create a new deal
pipedrive deals create --title "Q4 License" --value 25000 [--currency USD] [--person-id 123] [--org-id 456]

# Update an existing deal
pipedrive deals update <id> --title "Updated Deal" [--value 30000] [--status won]

# Delete a deal
pipedrive deals delete <id> [--force]

# Merge duplicate deals
pipedrive deals merge <source-id> <target-id> [--force]
# Note: Source deal will be deleted, target deal takes priority in conflicts
```

### `persons` - Persons (Contacts) Management

Manage contacts with email, phone, and organization associations.

```bash
# List all persons
pipedrive persons list [--limit 100] [--start 0]

# Get specific person details (includes custom fields)
pipedrive persons get <id>

# Create a new person
pipedrive persons create --name "John Doe" [--email john@example.com] [--phone "+1234567890"] [--org-id 123]

# Update an existing person
pipedrive persons update <id> --name "Jane Doe" [--email jane@example.com]

# Delete a person
pipedrive persons delete <id> [--force]

# Search persons
pipedrive persons search "search term" [--limit 100]

# Merge duplicate persons
pipedrive persons merge <source-id> <target-id> [--force]
```

### `organizations` - Organizations Management

Manage companies and organizations with search and merge capabilities.

```bash
# List all organizations
pipedrive organizations list [--limit 100] [--start 0]

# Get specific organization details (includes custom fields)
pipedrive organizations get <id>

# Create a new organization
pipedrive organizations create --name "Acme Corp" [--address "123 Main St, City, State"]

# Update an existing organization
pipedrive organizations update <id> --name "Updated Corp" [--address "New Address"]

# Delete an organization
pipedrive organizations delete <id> [--force]

# Search organizations
pipedrive organizations search "search term" [--limit 100]

# Merge duplicate organizations
pipedrive organizations merge <source-id> <target-id> [--force]
```

### `activities` - Activities Management

Manage tasks, calls, meetings, and other activities with due date tracking.

```bash
# List all activities
pipedrive activities list [--done 0|1] [--limit 100] [--start 0]

# Get specific activity details
pipedrive activities get <id>

# Create a new activity
pipedrive activities create --subject "Follow-up call" --type call [--due-date "2025-12-31"] [--due-time "14:00"]

# Update an existing activity
pipedrive activities update <id> --subject "Updated subject" [--due-date "2026-01-15"]

# Delete an activity
pipedrive activities delete <id> [--force]

# Mark activity as done
pipedrive activities mark-done <id>
```

### `notes` - Notes Management

Manage notes with HTML content support and entity associations.

```bash
# List all notes
pipedrive notes list [--limit 100] [--start 0]

# Get specific note details
pipedrive notes get <id>

# Create a new note
pipedrive notes create --content "Meeting notes here" [--deal-id 123] [--person-id 456] [--org-id 789]

# Update an existing note
pipedrive notes update <id> --content "Updated notes"

# Delete a note
pipedrive notes delete <id> [--force]
```

### `update` - Auto-Update

Check for and install CLI updates.

```bash
# Check for updates (stable releases only)
pipedrive update --check

# Install latest stable update
pipedrive update

# Check for updates including beta/pre-releases
pipedrive update --check --beta

# Install latest update (including betas) without confirmation
pipedrive update --beta --force
```

## Getting Your API Key

1. Log in to your Pipedrive account
2. Go to **Settings** → **Personal preferences** → **API**
3. Copy your personal API token
4. Use it with `pipedrive config set --api-key YOUR_TOKEN`

For more information, see the [Pipedrive API Documentation](https://developers.pipedrive.com/docs/api/v1).

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Linux, macOS, or Windows

### Build Commands

```bash
# Standard build
dotnet build

# Run tests
dotnet test

# Publish Native AOT binary (Linux)
dotnet publish src/PipedriveCLI/PipedriveCLI.csproj -c Release -r linux-x64

# Publish Native AOT binary (macOS)
dotnet publish src/PipedriveCLI/PipedriveCLI.csproj -c Release -r osx-arm64  # M1/M2 Mac
dotnet publish src/PipedriveCLI/PipedriveCLI.csproj -c Release -r osx-x64    # Intel Mac

# Publish Native AOT binary (Windows)
dotnet publish src/PipedriveCLI/PipedriveCLI.csproj -c Release -r win-x64
```

## Roadmap

The following features are planned for future releases:

### Export & Analysis
- **Bulk Export** - Export leads, deals, and contacts to CSV
- **AI-Powered Cleanup** - Analyze and sanitize CRM data
- **Data Quality Reports** - Identify incomplete or invalid data
- **Advanced Reporting** - Generate custom reports and analytics

For the full list of planned features and to suggest new ones, visit the [GitHub Issues](https://github.com/stannardlabs/pipedrive-cli/issues) page.

## Architecture

- **Native AOT Compilation** - Fast startup and small binary size
- **JSON Source Generators** - AOT-compatible serialization
- **System.CommandLine** - Modern CLI framework
- **Spectre.Console** - Beautiful console UI
- **Multi-Profile Support** - Manage multiple environments
- **Secure Storage** - Config files protected with Unix file permissions

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

Copyright © 2025 Stannard Labs

## Support

For issues and questions:
- Create an issue on [GitHub](https://github.com/stannardlabs/pipedrive-cli/issues)
- Check the [Pipedrive API Documentation](https://developers.pipedrive.com/docs/api/v1)
