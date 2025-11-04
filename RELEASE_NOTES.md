#### 0.1.0-beta2 January 4th 2025 ####

This release adds comprehensive auto-update functionality to the Pipedrive CLI, making it easy to stay up-to-date with the latest features and improvements.

**New Features**

- **Auto-Update System** ([#12](https://github.com/Aaronontheweb/pipedrive-cli/pull/12))
  - Background update checking on CLI startup (non-blocking, 3-second timeout)
  - Update notification banner when newer version available
  - `pipedrive update` command with multiple options:
    - `--check` - Check for updates without installing
    - `--force` - Install updates without confirmation prompt
  - Platform-specific self-update mechanism (Windows PowerShell, Unix bash)
  - Automatic backup and rollback on update failure
  - Progress indicators for download operations
  - Support for tar.gz (Unix) and zip (Windows) archives
  - GitHub releases integration via REST API
  - AOT-compatible JSON serialization with source generators

- **Beta/Pre-release Update Support** ([#13](https://github.com/Aaronontheweb/pipedrive-cli/pull/13))
  - `--beta` flag to opt into pre-release updates
  - Default behavior checks stable releases only
  - Clear distinction between stable and pre-release versions in notifications
  - Usage:
    - `pipedrive update --beta` - Update to latest including betas
    - `pipedrive update --beta --check` - Check for beta without installing
    - `pipedrive update --beta --force` - Install beta without confirmation

**Improvements**

- Fixed installation scripts to use correct repository owner
- Simplified release notes to focus on installation instructions

**Technical Details**

- Version comparison using semantic versioning
- Platform detection (linux-x64, win-x64, osx-x64, osx-arm64)
- Binary download with progress tracking
- Archive extraction with proper error handling
- Self-replacement with backup/rollback capability
- Silent failure on background check errors to prevent CLI delays

**Installation**

The installation scripts now support beta releases:

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash -s -- --beta

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.1.0-beta2).

---

#### 0.1.0-beta1 November 4th 2025 ####

This is the first beta release of the Pipedrive CLI - a fast, lightweight command-line interface for managing your Pipedrive CRM. Built with .NET 8 and Native AOT compilation for lightning-fast startup times (~13ms) and small binary size (~12MB).

**Features**

- **Configuration Management**
  - Multi-profile support (default, staging, production) with `config profile` commands
  - Secure credential storage at `~/.pipedrive/config.json` with Unix file permissions (600)
  - Environment variable overrides (PIPEDRIVE_API_KEY, PIPEDRIVE_DOMAIN)
  - Profile switching and API connection testing
  - Masked API key display for security

- **Leads Management**
  - List all leads with pagination (`leads list`)
  - Get specific lead details (`leads get`)
  - Create new leads with title, value, and expected close date (`leads create`)
  - Update existing leads (`leads update`)
  - Delete leads with confirmation (`leads delete`)
  - Search leads by term using Pipedrive API v2 (`leads search`)

- **Deals Management**
  - List all deals with status filtering (`deals list`)
  - Get specific deal details (`deals get`)
  - Create new deals with title, value, currency, person/org (`deals create`)
  - Update existing deals (`deals update`)
  - Delete deals with confirmation (`deals delete`)

- **Activities Management**
  - List all activities with done status filtering (`activities list`)
  - Get specific activity details (`activities get`)
  - Create new activities with subject, type, due date/time (`activities create`)
  - Update existing activities (`activities update`)
  - Delete activities with confirmation (`activities delete`)
  - Mark activities as completed (`activities mark-done`)

- **Persons (Contacts) Management**
  - List all persons with pagination (`persons list`)
  - Get specific person details including emails and phones (`persons get`)
  - Create new persons with name, email, phone, organization (`persons create`)
  - Update existing persons (`persons update`)
  - Delete persons with confirmation (`persons delete`)
  - Search persons by term (`persons search`)

- **Organizations Management**
  - List all organizations with pagination (`organizations list`)
  - Get specific organization details (`organizations get`)
  - Create new organizations with name and address (`organizations create`)
  - Update existing organizations (`organizations update`)
  - Delete organizations with confirmation (`organizations delete`)
  - Search organizations by term (`organizations search`)

- **Installation Scripts**
  - `install.sh` for Linux/macOS with multi-architecture support (x64, arm64, arm)
  - `install.ps1` for Windows with architecture detection (x64, x86)
  - Beta/pre-release support with `--beta` flag
  - Uninstall functionality with config directory cleanup
  - Automatic download from GitHub releases
  - PATH detection and configuration guidance

**Technical Features**

- **Native AOT Compilation** - 13ms cold start time, 12MB binary size
- **JSON Source Generators** - Full AOT compatibility with System.Text.Json
- **Spectre.Console** - Beautiful formatted tables, panels, and status indicators
- **System.CommandLine** - Modern CLI framework with built-in help
- **Cross-Platform** - Linux, macOS (Intel/ARM), Windows support
- **HTTP API Client** - Full CRUD operations with pagination support
- **Comprehensive Testing** - 38 unit tests covering all API operations

**Installation**

Using the installer script (recommended):

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash -s -- --beta

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.1.0-beta1).

**Getting Started**

1. Configure your Pipedrive API key:
```bash
pipedrive config set --api-key YOUR_API_KEY --domain company.pipedrive.com
```

2. Test the connection:
```bash
pipedrive config test
```

3. Start managing your CRM:
```bash
pipedrive leads list
pipedrive deals list --status won
pipedrive persons search "John Doe"
```

**Known Limitations**

This is a beta release. While all core features are functional and tested, the following are planned for future releases:
- Bulk export functionality
- Data quality reports
- Duplicate detection
- AI-powered data cleanup

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues
