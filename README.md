# Pipedrive CLI

A fast, lightweight command-line interface for managing your Pipedrive CRM. Built with .NET 8 and Native AOT compilation for lightning-fast startup times (~13ms).

## Features

- **⚡ Native AOT Compiled** - 13ms cold start with 12MB binary size
- **🔐 Secure Configuration** - Profile-based config stored at `~/.pipedrive/config.json`
- **🌍 Environment Variables** - Override config values via environment variables
- **🎨 Beautiful Console UI** - Rich formatting with Spectre.Console
- **🔄 Multi-Profile Support** - Manage multiple Pipedrive environments (dev, staging, prod)

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
      "domain": "company.pipedrive.com",
      "emailGatewayUrl": "https://gateway.example.com",
      "emailGatewayApiKey": "GATEWAY_KEY"
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
- `EMAIL_GATEWAY_URL` - Override the email gateway URL
- `EMAIL_GATEWAY_API_KEY` - Override the email gateway API key

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

# Set email gateway configuration (optional)
pipedrive config set --email-gateway-url https://gateway.example.com --email-gateway-api-key GATEWAY_KEY

# Options:
#   --api-key, -k              Pipedrive API key
#   --domain, -d               Pipedrive domain (e.g., company.pipedrive.com)
#   --email-gateway-url, -e    Email Gateway URL for approval workflow
#   --email-gateway-api-key, -g Email Gateway API key
```

#### `config get` - Display Current Configuration

View the current configuration with masked API keys.

```bash
pipedrive config get
```

Output:
```
╭───────────────────────┬────────────────────────────────────────────╮
│ Setting               │ Value                                      │
├───────────────────────┼────────────────────────────────────────────┤
│ Profile               │ default                                    │
│ API Key               │ abc1****xyz9                               │
│ Domain                │ company.pipedrive.com                      │
│ Email Gateway URL     │ https://gateway.example.com                │
│ Email Gateway API Key │ gate****key8                               │
│ Config File           │ /home/user/.pipedrive/config.json          │
╰───────────────────────┴────────────────────────────────────────────╯
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

### Data Management Commands
- **Leads** - List, create, update, delete, search, and convert leads
- **Deals** - Manage deals and pipeline stages
- **Persons** - Manage contacts and people
- **Organizations** - Manage companies and organizations
- **Activities** - Manage tasks, calls, meetings, and other activities

### Export & Analysis
- **Bulk Export** - Export leads, deals, and contacts to CSV
- **AI-Powered Cleanup** - Analyze and sanitize CRM data
- **Duplicate Detection** - Find and merge duplicate records
- **Data Quality Reports** - Identify incomplete or invalid data

### Email Integration
- **Email Gateway** - Compose and send emails with approval workflow
- **Template Support** - Use email templates for common scenarios
- **Batch Operations** - Send bulk emails with personalization

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
