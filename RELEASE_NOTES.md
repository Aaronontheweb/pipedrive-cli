#### 0.10.0 July 01 2026 ####

This release adds powerful date range filtering to `deals list` — filter by expected close date (`--closing-after` / `--closing-before`) or by when deals were actually won (`--won-after` / `--won-before`). It also surfaces won and lost timestamps in `deals get` output.

**New Features**

- **Closing Date Filtering** ([#146](https://github.com/Aaronontheweb/pipedrive-cli/issues/146), [#165](https://github.com/Aaronontheweb/pipedrive-cli/pull/165))
  - Added `--closing-after <date>` and `--closing-before <date>` options to `deals list` to filter by expected close date
  - Supports YYYY-MM-DD format (e.g. `2026-04-01`)
  - Example: `pipedrive deals list --closing-after 2026-04-01 --closing-before 2026-06-30`

- **Won/Lost Date Filtering** ([#164](https://github.com/Aaronontheweb/pipedrive-cli/issues/164), [#165](https://github.com/Aaronontheweb/pipedrive-cli/pull/165))
  - Added `--won-after <date>` and `--won-before <date>` options to `deals list` to filter by actual won time
  - Useful for reporting on closed-won deals within a specific period
  - Example: `pipedrive deals list --status won --won-after 2026-04-01 --won-before 2026-06-30`

- **Won/Lost Time Display** ([#163](https://github.com/Aaronontheweb/pipedrive-cli/issues/163), [#165](https://github.com/Aaronontheweb/pipedrive-cli/pull/165))
  - `deals get` now displays `won_time` and `lost_time` for closed deals

#### 0.9.0 June 24 2026 ####

This release adds time-window filtering and cursor-based pagination to the `deals list` and `activities list` commands, making it practical to sync only recently changed records at scale. It also includes an improved credentials setup guide in the README.

**New Features**

- **Update-Window Filters for Deals and Activities**
  - Added `--updated-since` and `--updated-until` options to `deals list` and `activities list` to retrieve only records changed within a specified time window
  - Supports ISO 8601 timestamps (e.g. `2026-06-24T00:00:00Z`)
  - Useful for incremental sync workflows where you only need to process recently modified records
  - Example: `pipedrive deals list --updated-since 2026-06-01T00:00:00Z --updated-until 2026-06-24T00:00:00Z`
  - Example: `pipedrive activities list --updated-since 2026-06-24T00:00:00Z`

- **Cursor-Based Pagination**
  - Added `--cursor` option to `deals list` and `activities list` for efficient traversal of large result sets using Pipedrive v2 API pagination
  - The API returns a `next_cursor` value when more pages are available; pass it to subsequent calls to continue
  - Example: `pipedrive deals list --limit 500 --cursor <next_cursor_value>`

- **Activity Sorting**
  - Activities can now be retrieved in a consistent sort order when using update-window filters

**Documentation**

- **Credentials Setup Guide** ([#151](https://github.com/Aaronontheweb/pipedrive-cli/issues/151))
  - Added a prominent **Getting Your Pipedrive Credentials** section to the README, visible without scrolling
  - Covers how to find your API token (Settings → Personal preferences → API) and your Pipedrive domain
  - Guides users to use `pipedrive config set` / `pipedrive config test` as the primary setup path

---

#### 0.8.2 January 26 2026 ####

This release adds the ability to convert leads to deals and discover organization custom field definitions.

**New Features**

- **Lead Conversion** ([#140](https://github.com/Aaronontheweb/pipedrive-cli/issues/140), [#143](https://github.com/Aaronontheweb/pipedrive-cli/pull/143))
  - Added `pipedrive leads convert <lead-id>` command to convert a lead to a deal
  - Uses Pipedrive's async conversion API to preserve lead history and create a properly linked deal
  - Supports `--stage-id` option to specify target stage (automatically determines pipeline)
  - Supports `--pipeline-id` option to specify target pipeline (ignored if stage-id is provided)
  - Eliminates the need to manually create deals and lose audit trail
  - Example: `pipedrive leads convert abc123 --stage-id 5`

- **Organization Fields Discovery** ([#141](https://github.com/Aaronontheweb/pipedrive-cli/issues/141), [#142](https://github.com/Aaronontheweb/pipedrive-cli/pull/142))
  - Added `pipedrive organizationFields list` command to discover organization field definitions
  - Displays custom field keys and their human-readable names
  - Supports `--custom-only` option to show only custom fields
  - Supports `--search` option to filter fields by name
  - Essential for identifying hash keys when updating organization custom fields
  - Example: `pipedrive organizationFields list --custom-only`

---

#### 0.8.1 January 21 2026 ####

This release adds the ability to specify a lost reason when marking deals as lost.

**New Features**

- **Lost Reason Support for Deals** ([#136](https://github.com/Aaronontheweb/pipedrive-cli/issues/136), [#137](https://github.com/Aaronontheweb/pipedrive-cli/pull/137))
  - Added `--lost-reason` option to `deals update` command
  - When marking a deal as lost, users can now specify the reason directly
  - The lost reason appears in Pipedrive's deal history and reporting
  - Validates that `--lost-reason` cannot be used with `--status won`
  - Example: `pipedrive deals update 123 --status lost --lost-reason "Customer chose competitor"`

---

#### 0.8.0 January 13 2026 ####

This release adds deal product management capabilities and fixes several important filtering and data management bugs.

**New Features**

- **Deal Product Management** ([#127](https://github.com/Aaronontheweb/pipedrive-cli/issues/127), [#132](https://github.com/Aaronontheweb/pipedrive-cli/pull/132))
  - `pipedrive deals products <deal-id>` - List all products attached to a deal
  - `pipedrive deals add-product <deal-id> --product-id <id> --quantity <n> --price <amount>` - Add a product to a deal
  - `pipedrive deals remove-product <deal-id> --attachment-id <id>` - Remove a product from a deal
  - `pipedrive deals clear-products <deal-id>` - Remove all products from a deal
  - Displays product name, quantity, price, and total value
  - Enables complete product-based deal management directly from CLI
  - Example: `pipedrive deals add-product 123 --product-id 456 --quantity 5 --price 99.99`

**Bug Fixes**

- **Fixed Organization Deals Filter** ([#125](https://github.com/Aaronontheweb/pipedrive-cli/issues/125), [#129](https://github.com/Aaronontheweb/pipedrive-cli/pull/129))
  - Fixed `deals list --org-id` filter returning no results
  - The `/organizations/{id}/deals` endpoint doesn't support `status=all` like the regular `/deals` endpoint
  - Now maps "all" to "all_not_deleted" for organization deals endpoint
  - Ensures consistent behavior across different deal listing methods

- **Filter Archived Lead Activities** ([#126](https://github.com/Aaronontheweb/pipedrive-cli/issues/126), [#131](https://github.com/Aaronontheweb/pipedrive-cli/pull/131))
  - Activities associated with archived leads are now filtered out by default from `activities list`
  - Prevents stale/irrelevant activities from cluttering activity list during triage
  - Added `--include-archived-leads` option (default: false) to show activities for archived leads when needed
  - Automatically fetches lead details for activities with LeadId to determine archived status

- **Support for Clearing Date Fields** ([#128](https://github.com/Aaronontheweb/pipedrive-cli/issues/128), [#130](https://github.com/Aaronontheweb/pipedrive-cli/pull/130))
  - Fixed inability to clear date fields on deals
  - The Pipedrive API requires explicit null values to clear date fields
  - Added support for `--expected-close-date "clear"` or `--expected-close-date "null"` to remove dates
  - New UpdateDealAsync overload accepts fields to clear
  - Example: `pipedrive deals update 123 --expected-close-date clear`

---

#### 0.7.3 December 10 2025 ####

This release adds powerful filtering capabilities to activities, persons, and emails commands, making it easier to manage CRM data and review communication history.

**New Features**

- **Date Filtering for Activities** ([#114](https://github.com/Aaronontheweb/pipedrive-cli/issues/114), [#115](https://github.com/Aaronontheweb/pipedrive-cli/pull/115))
  - Added `--overdue` option to quickly filter activities with due dates before today
  - Added `--due-before <date>` option to filter activities due before a specific date (YYYY-MM-DD format)
  - Added `--due-after <date>` option to filter activities due after a specific date (YYYY-MM-DD format)
  - Filters can be combined to create date ranges (e.g., `--due-after 2025-01-01 --due-before 2025-02-01`)
  - Example: `pipedrive activities list --overdue`

- **Address and Custom Field Support for Persons** ([#111](https://github.com/Aaronontheweb/pipedrive-cli/issues/111), [#116](https://github.com/Aaronontheweb/pipedrive-cli/pull/116))
  - Added `--address` / `-a` option to update the postal address field
  - Added `--custom-field` / `-c` option to update custom fields using `key=value` format (can be specified multiple times)
  - Uses AOT-compatible JSON serialization for native compilation support
  - Example: `pipedrive persons update 123 --address "123 Main St, Test City" --custom-field "custom_key=value"`

- **Person and Deal Filters for Email Threads** ([#110](https://github.com/Aaronontheweb/pipedrive-cli/issues/110), [#117](https://github.com/Aaronontheweb/pipedrive-cli/pull/117))
  - Added `--person-id` / `-p` option to filter email threads involving a specific person
  - Added `--deal-id` / `-d` option to filter email threads linked to a specific deal
  - Filters can be combined with existing folder and pagination options
  - Helps review communication history before customer outreach
  - Example: `pipedrive emails threads --person-id 12702 --folder sent`

---

#### 0.7.2 December 5 2025 ####

Reverted redundant `--yes`/`-y` aliases from update command - the existing `--force`/`-f` flag already provides this functionality.

---

#### 0.7.0 December 4 2025 ####

This release focuses on stability improvements, fixing critical display issues and improving error handling across the CLI.

**Bug Fixes**

- **Fixed ID Corruption in Table Rendering** ([#102](https://github.com/Aaronontheweb/pipedrive-cli/issues/102), [#103](https://github.com/Aaronontheweb/pipedrive-cli/issues/103))
  - Added NoWrap() to ID columns across all list commands
  - Prevents IDs from wrapping across multiple lines in narrow terminal windows
  - Ensures copy-paste reliability for automation and scripting

- **Improved API Error Handling** ([#101](https://github.com/Aaronontheweb/pipedrive-cli/issues/101), [#104](https://github.com/Aaronontheweb/pipedrive-cli/issues/104))
  - Now displays actual Pipedrive API error messages instead of generic HTTP errors
  - Helps users understand and resolve API issues faster
  - Better troubleshooting for validation errors and permission issues

- **Silenced Update Check Failures** ([#99](https://github.com/Aaronontheweb/pipedrive-cli/issues/99), [#104](https://github.com/Aaronontheweb/pipedrive-cli/issues/104))
  - Update availability checks no longer interrupt user workflow with error messages
  - Graceful degradation when GitHub API is unreachable
  - Maintains responsive CLI experience even when offline

**New Features**

- **Lead ID Filter for Notes** ([#98](https://github.com/Aaronontheweb/pipedrive-cli/issues/98), [#105](https://github.com/Aaronontheweb/pipedrive-cli/issues/105))
  - Added `--lead-id` option to `notes list` command
  - Filter notes by associated lead UUID
  - Example: `pipedrive notes list --lead-id "e126ec80-cfff-11f0-8c59-fd43bfd483d9"`

---

#### 0.6.0 December 3 2025 ####

This release adds extensive filtering capabilities across activities, notes, and deals, plus new commands for discovering custom fields and managing lead lifecycle.

**New Features**

- **Deal Fields Discovery** ([#89](https://github.com/Aaronontheweb/pipedrive-cli/issues/89))
  - `pipedrive dealFields list` - Discover all deal field keys including custom fields
  - Essential for identifying hash keys needed when using `--custom-fields` on deals update

- **Lead Archive Management** ([#91](https://github.com/Aaronontheweb/pipedrive-cli/issues/91))
  - `pipedrive leads archive <id>` - Archive a lead
  - `pipedrive leads unarchive <id>` - Restore an archived lead

**Command Enhancements**

- **Activities Filtering** ([#74](https://github.com/Aaronontheweb/pipedrive-cli/issues/74)) - Added `--deal-id`, `--person-id`, `--org-id` filters to `activities list`
- **Activities Participants** ([#75](https://github.com/Aaronontheweb/pipedrive-cli/issues/75)) - Added `--participants` option to `activities create` and `activities update`
- **Activities Done Flag** ([#92](https://github.com/Aaronontheweb/pipedrive-cli/issues/92)) - Added `--done` option to `activities update` command
- **Activities Association Display** ([#95](https://github.com/Aaronontheweb/pipedrive-cli/issues/95)) - Shows association type prefix (Deal:, Lead:, Person:, Org:) in activities list
- **Persons Update Org** ([#80](https://github.com/Aaronontheweb/pipedrive-cli/issues/80)) - Added `--org-id` option to `persons update` command
- **Notes Filtering** ([#82](https://github.com/Aaronontheweb/pipedrive-cli/issues/82)) - Added `--deal-id`, `--person-id`, `--org-id` filters to `notes list`
- **Deals Filtering by Org** ([#83](https://github.com/Aaronontheweb/pipedrive-cli/issues/83)) - Added `--org-id` filter to `deals list` command
- **Deals Update Fields** ([#90](https://github.com/Aaronontheweb/pipedrive-cli/issues/90)) - Added `--person-id`, `--org-id`, `--probability` options to `deals update`
- **Force Flag Alias** ([#93](https://github.com/Aaronontheweb/pipedrive-cli/issues/93)) - Added `-y` as alias for `--force`/`-f` on all delete and merge commands

**Bug Fixes**

- **DealParticipant Deserialization** ([#73](https://github.com/Aaronontheweb/pipedrive-cli/issues/73)) - Fixed JSON deserialization for `person_id` field in deal participants

---

#### 0.5.3 December 2 2025 ####

This release adds deal participant management, historical close dates for deals, and lead association for activities.

**New Features**

- **Deal Participant Management** ([#67](https://github.com/Aaronontheweb/pipedrive-cli/issues/67))
  - `pipedrive deals participants <deal-id>` - List all participants of a deal
  - `pipedrive deals add-participant <deal-id> --person-id <id>` - Add a person as deal participant
  - `pipedrive deals remove-participant <deal-id> --participant-id <id>` - Remove a participant
  - Displays participant name, email, and when they were added
  - Enables managing additional stakeholders on deals directly from CLI

- **Historical Close Dates for Deals** ([#60](https://github.com/Aaronontheweb/pipedrive-cli/issues/60))
  - Added `--won-time` option to `deals update` command
  - Added `--lost-time` option to `deals update` command
  - Accepts formats: `YYYY-MM-DD` or `YYYY-MM-DD HH:mm:ss`
  - Prevents close time defaulting to current timestamp when cleaning up old deals
  - Example: `pipedrive deals update 862 --status lost --lost-time "2024-06-30"`

- **Lead Association for Activities** ([#65](https://github.com/Aaronontheweb/pipedrive-cli/issues/65))
  - Added `--lead-id` / `-l` option to `activities create` command
  - Activities can now be directly associated with leads (UUID format)
  - Example: `pipedrive activities create --subject "Follow-up" --due-date "2025-12-09" --lead-id "e126ec80-cfff-11f0-8c59-fd43bfd483d9"`

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.5.3).

**Upgrade from 0.5.2**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Example Usage**

```bash
# Add a participant to a deal
pipedrive deals add-participant 123 --person-id 456

# List all participants on a deal
pipedrive deals participants 123

# Close a deal with historical date
pipedrive deals update 789 --status won --won-time "2024-11-15"

# Create an activity associated with a lead
pipedrive activities create --subject "Initial call" --type call --due-date "2025-12-10" --lead-id "abc123-def456"
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.5.2 December 2 2025 ####

This release adds the ability to filter deals by pipeline, making pipeline-specific auditing and cleanup workflows much more efficient.

**New Features**

- **Pipeline Filter for Deals List** ([#58](https://github.com/Aaronontheweb/pipedrive-cli/issues/58))
  - Added `--pipeline-id` / `-p` option to `pipedrive deals list` command
  - Filter deals to show only those in a specific pipeline
  - Uses native Pipedrive API filtering for efficient server-side results
  - Combines with existing `--status` filter for precise queries
  - Example: `pipedrive deals list --pipeline-id 15 --status open`

---

#### 0.5.0 December 2 2025 ####

This release adds comprehensive email management capabilities, Smart BCC field support, and fixes critical search API bugs.

**New Features**

- **Email Management System** ([#53](https://github.com/Aaronontheweb/pipedrive-cli/pull/53))
  - `pipedrive emails list-for-deal <deal-id>` - List all emails associated with a deal
  - `pipedrive emails list-for-person <person-id>` - View email history for a person
  - `pipedrive emails get <message-id> [--include-body]` - Retrieve specific email message details
  - `pipedrive emails threads [--folder inbox|sent|drafts|archive]` - Browse mail threads by folder
  - `pipedrive emails thread <thread-id>` - Get specific mail thread information
  - `pipedrive emails thread-messages <thread-id> [--include-body]` - View all messages in a thread
  - Complete API support for email viewing across deals, persons, and mail threads
  - 17 new unit tests ensuring JSON serialization reliability
  - Closes [#51](https://github.com/Aaronontheweb/pipedrive-cli/issues/51)

- **Smart BCC (cc_email) Field Support** ([#52](https://github.com/Aaronontheweb/pipedrive-cli/pull/52))
  - Display `cc_email` field in formatted output for all entity types
  - Available for deals, leads, persons, organizations, and activities
  - Enables easy access to Smart BCC addresses for automatic email sync
  - Helps users and automation tools quickly identify the correct BCC address
  - Closes [#50](https://github.com/Aaronontheweb/pipedrive-cli/issues/50)

**Bug Fixes**

- **Fixed Search API JSON Parsing** (commit e8a8ee4)
  - Resolved JSON deserialization errors when using organization and person search commands
  - Pipedrive search API returns different response structure than list API
  - Added proper `SearchData` models for organizations and persons
  - Search responses now correctly transform to standard list format
  - Fixes [#49](https://github.com/Aaronontheweb/pipedrive-cli/issues/49)

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.5.0).

**Upgrade from 0.4.0**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Example Usage**

```bash
# View all emails for a deal
pipedrive emails list-for-deal 123

# Get a specific email with body content
pipedrive emails get 456 --include-body

# Browse inbox mail threads
pipedrive emails threads --folder inbox

# View all messages in a thread
pipedrive emails thread-messages 789 --include-body

# Check Smart BCC address for a deal
pipedrive deals get 123  # cc_email field now displayed
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.4.0 November 21 2025 ####

This release adds email template support, improves leads list filtering, and fixes a display bug.

**New Features**

- **Email Templates Support** ([#46](https://github.com/Aaronontheweb/pipedrive-cli/pull/46))
  - `pipedrive templates list` - List all email templates in your Pipedrive account
  - `pipedrive templates get <id>` - View a specific email template
  - `--content` flag shows plain text preview of HTML template content
  - Displays template name, owner, sharing settings, and timestamps
  - Example: `pipedrive templates list`

- **Leads Status Filter** ([#45](https://github.com/Aaronontheweb/pipedrive-cli/pull/45))
  - Added `--status` option to `leads list` command
  - Filter options: `active` (default), `archived`, `all`
  - Active leads are now shown by default, reducing clutter
  - Example: `pipedrive leads list --status archived`

**Bug Fixes**

- **Fixed Markup.Escape in Leads Display** ([#44](https://github.com/Aaronontheweb/pipedrive-cli/pull/44))
  - Fixed rendering errors when lead titles contained special characters like `[`, `]`
  - Previously, titles like `[Inquiry] Company Name` would cause "Could not find color or style 'Inquiry'" errors
  - Properly escapes special characters in both `leads list` and `leads search` output

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.4.0).

**Upgrade from 0.3.0**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Example Usage**

```bash
# List all email templates
pipedrive templates list

# View a specific template with content preview
pipedrive templates get 123 --content

# List only active leads (new default)
pipedrive leads list

# List archived leads
pipedrive leads list --status archived

# List all leads regardless of status
pipedrive leads list --status all
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.3.0 November 21 2025 ####

This release adds powerful custom field management, JSON output for automation, and pipeline management commands.

**New Features**

- **Custom Field Support for Deals** ([#43](https://github.com/Aaronontheweb/pipedrive-cli/pull/43))
  - Update custom fields directly via CLI with `--custom-fields` flag on `deals update`
  - Automatically displays custom fields with friendly names when viewing deals
  - CustomFieldCache service resolves hash keys to user-friendly field names
  - Add `--raw-keys` flag to show hash keys when needed for scripting
  - Graceful fallback to hash keys if field cache fails
  - Example: `pipedrive deals update 123 --custom-fields "Project Status=In Progress,Priority=High"`

- **JSON Output for Automation** ([#42](https://github.com/Aaronontheweb/pipedrive-cli/pull/42))
  - Added `--json` flag to `deals get` command for machine-readable output
  - Added `--json` flag to `organizations get` command for machine-readable output
  - Enables building robust automation scripts without direct API calls
  - Uses AOT-safe JSON serialization with source generators
  - Example: `pipedrive deals get 123 --json | jq '.value'`

- **Pipelines Management** ([#37](https://github.com/Aaronontheweb/pipedrive-cli/pull/37))
  - `pipedrive pipelines list` - Display all pipelines in tabular format
  - `pipedrive pipelines get <id>` - Retrieve detailed pipeline information
  - `pipedrive pipelines stages [--pipeline-id <id>]` - List stages with optional filtering
  - Complete Pipeline and Stage models with deal probability and timestamps
  - Helps users understand deal progression workflows

- **User Assignment for Activities** ([#42](https://github.com/Aaronontheweb/pipedrive-cli/pull/42))
  - Added `--user-id` option to `activities create` command
  - Enables proper task assignment when creating activities via CLI
  - Example: `pipedrive activities create --subject "Follow up" --user-id 456 --deal-id 123`

**Bug Fixes**

- **Fixed Custom Field Display Issue** (commit d5b9ec4)
  - Resolved `Markup.Escape` bug that caused rendering errors with special characters in custom fields
  - Custom field values now display correctly regardless of content

- **Fixed Markup Escape in Deals List** ([#37](https://github.com/Aaronontheweb/pipedrive-cli/pull/37))
  - Fixed rendering errors when deal titles contained special characters like `[`, `]`, or `{`
  - Properly escapes special characters in deal titles, status, and dates
  - Prevents Spectre.Console from misinterpreting markup characters

**Improvements**

- **Sensible Default Filters** ([#37](https://github.com/Aaronontheweb/pipedrive-cli/pull/37))
  - `deals list` now defaults to showing open deals only (use `--status all` to see all)
  - `activities list` now defaults to showing undone activities (use `--done true` to see completed)
  - Reduces clutter in most common use cases while maintaining full flexibility

- **API Model Improvements** ([#43](https://github.com/Aaronontheweb/pipedrive-cli/pull/43))
  - Made BaseField properties nullable to handle inconsistent API responses
  - Improved lenient JSON parsing for field definitions
  - Enhanced error handling with graceful degradation

**Dependencies**

- Bump actions/download-artifact from 5 to 6 ([#21](https://github.com/Aaronontheweb/pipedrive-cli/pull/21))
- Bump actions/checkout from 5.0.1 to 6.0.0 ([#38](https://github.com/Aaronontheweb/pipedrive-cli/pull/38))

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.3.0).

**Upgrade from 0.2.2**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Example Usage**

```bash
# View a deal with custom fields displayed with friendly names
pipedrive deals get 123

# Update custom fields
pipedrive deals update 123 --custom-fields "Project Status=Completed,Budget=$50000"

# Get JSON output for scripting
pipedrive deals get 123 --json | jq '.custom_fields'

# List all pipelines
pipedrive pipelines list

# Create an activity assigned to a specific user
pipedrive activities create --subject "Call prospect" --user-id 789 --deal-id 123

# List only open deals (new default)
pipedrive deals list
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.2.2 November 20 2025 ####

This release fixes critical installation issues and improves the configuration setup experience.

**Bug Fixes**

- **Fixed Installation Script URLs** ([#34](https://github.com/Aaronontheweb/pipedrive-cli/pull/34))
  - Resolved 404 errors when accessing install scripts
  - Updated repository URLs from stannardlabs to Aaronontheweb
  - Users can now successfully download and install the CLI

- **Fixed Post-Install Instructions** ([#35](https://github.com/Aaronontheweb/pipedrive-cli/pull/35))
  - Corrected invalid `config set-api-token` command in install scripts
  - Updated to show correct `config set --api-key <key> --domain <domain>` syntax
  - Error messages now display concrete examples instead of placeholders

**Improvements**

- **Automatic Domain Normalization** ([#35](https://github.com/Aaronontheweb/pipedrive-cli/pull/35))
  - Users can now provide just their company name (e.g., `company`) instead of the full domain
  - Automatically appends `.pipedrive.com` if not present
  - Handles protocol removal (`https://`, `http://`), trailing slashes, and case normalization
  - Users can copy their domain directly from Pipedrive settings without formatting

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.2.2).

**Upgrade from 0.2.1**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.2.1 November 4th 2025 ####

This release adds a usability improvement to the configuration management system.

**Improvements**

- **Profile Flag for Config Set** ([#27](https://github.com/Aaronontheweb/pipedrive-cli/pull/27))
  - Added `--profile` flag to `config set` command
  - Allows creating and configuring profiles without switching to them first
  - Maintains backward compatibility - config still applies to active profile when flag is omitted
  - Usage: `pipedrive config set --profile staging --api-key YOUR_KEY --domain company.pipedrive.com`

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.2.1).

**Upgrade from 0.2.0**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.2.0 November 4th 2025 ####

This release adds powerful new features for managing notes and merging duplicate entities, along with custom field support and important stability improvements.

**New Features**

- **Notes API** ([#20](https://github.com/Aaronontheweb/pipedrive-cli/pull/20))
  - Complete CRUD operations for notes (list, get, create, update, delete)
  - Support for associations with deals, persons, organizations, leads, and projects
  - HTML content support with automatic sanitization
  - Rich filtering options by entity type and ID
  - Commands:
    - `pipedrive notes list` - List all notes with filtering
    - `pipedrive notes get <id>` - Get specific note details
    - `pipedrive notes create` - Create new notes with content and associations
    - `pipedrive notes update <id>` - Update existing notes
    - `pipedrive notes delete <id>` - Delete notes with confirmation

- **Entity Merge Commands** ([#20](https://github.com/Aaronontheweb/pipedrive-cli/pull/20))
  - Merge duplicate persons, deals, and organizations
  - Automatic conflict resolution (target entity takes priority)
  - Confirmation prompts to prevent accidental data loss
  - Source entity is deleted after successful merge
  - Commands:
    - `pipedrive persons merge <id> <merge-with-id>` - Merge two persons
    - `pipedrive deals merge <id> <merge-with-id>` - Merge two deals
    - `pipedrive organizations merge <id> <merge-with-id>` - Merge two organizations

- **Custom Fields Support** ([#24](https://github.com/Aaronontheweb/pipedrive-cli/pull/24))
  - Automatically displays custom fields when viewing entities
  - Works with deals, persons, and organizations
  - Uses `JsonExtensionData` to capture Pipedrive's hash-based field keys
  - AOT-compatible implementation with manual JSON parsing
  - Custom fields appear in the output of:
    - `pipedrive deals get <id>`
    - `pipedrive persons get <id>`
    - `pipedrive organizations get <id>`

**Bug Fixes**

- **Fixed Reference Field Serialization** ([#17](https://github.com/Aaronontheweb/pipedrive-cli/pull/17), [#18](https://github.com/Aaronontheweb/pipedrive-cli/pull/18), [#19](https://github.com/Aaronontheweb/pipedrive-cli/pull/19))
  - Resolved "read too much or not enough" errors in `PipedriveReferenceConverter`
  - Fixed API model serialization to prevent 400 Bad Request errors
  - Corrected Owner type handling across all entity models
  - Added proper null handling for reference fields

- **Fixed Display Issues** ([#19](https://github.com/Aaronontheweb/pipedrive-cli/pull/19))
  - Fixed 41 markup injection vulnerabilities in entity display code
  - Properly escape special characters in entity names and field values
  - Prevents display formatting errors when data contains special characters like `[`, `]`, or `{`

**Improvements**

- **Enhanced Test Coverage** ([#19](https://github.com/Aaronontheweb/pipedrive-cli/pull/19))
  - Added comprehensive deserialization tests covering all entity types
  - Tests verify correct handling of reference fields and null values
  - Improved reliability and reduced crash scenarios

**Technical Highlights**

- All new features maintain Native AOT compatibility
- JSON serialization uses source generators throughout
- Comprehensive test coverage for merge operations
- Follows existing architectural patterns and code style

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.2.0).

**Upgrade from 0.1.0**

The CLI includes auto-update functionality. Simply run:

```bash
pipedrive update
```

**Example Usage**

```bash
# View a deal with custom fields
pipedrive deals get 123

# Add a note to a deal
pipedrive notes create --content "Follow up next week" --deal-id 123

# Merge duplicate persons
pipedrive persons merge 456 789

# List all notes for a specific organization
pipedrive notes list --org-id 101
```

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.1.0 January 4th 2025 ####

**FIRST STABLE RELEASE** - The Pipedrive CLI is now production-ready!

This release promotes 0.1.0-beta3 to stable status. After successful beta testing, the CLI is ready for production use with all core features tested and working correctly.

**What's Included**

This is the first stable release of the Pipedrive CLI - a fast, lightweight command-line interface for managing your Pipedrive CRM. Built with .NET 8 and Native AOT compilation for lightning-fast startup times (~13ms) and small binary size (~12MB).

**Core Features**

- **Configuration Management**
  - Multi-profile support (default, staging, production)
  - Secure credential storage with Unix file permissions (600)
  - Environment variable overrides (PIPEDRIVE_API_KEY, PIPEDRIVE_DOMAIN)
  - Profile switching and API connection testing

- **CRM Entity Management**
  - **Leads**: List, get, create, update, delete, and search
  - **Deals**: Full CRUD operations with status filtering
  - **Activities**: Management with due date/time and completion tracking
  - **Persons (Contacts)**: Complete contact management with email/phone support
  - **Organizations**: Company management with search capabilities

- **Auto-Update System**
  - Background update checking on CLI startup (non-blocking)
  - `pipedrive update` command with check, force, and beta options
  - Platform-specific self-update mechanism (Windows PowerShell, Unix bash)
  - Automatic backup and rollback on update failure
  - Support for both stable and pre-release versions

- **Installation Scripts**
  - `install.sh` for Linux/macOS with multi-architecture support
  - `install.ps1` for Windows with architecture detection
  - Beta/pre-release support with `--beta` flag
  - Uninstall functionality with config directory cleanup

**Why This Release Matters**

The stable release fixes a critical issue with the auto-update checker. Previous beta versions would get 404 errors when checking for updates because the GitHub releases API's `/latest` endpoint only returns stable releases (not pre-releases). With 0.1.0 stable now published, the auto-update system will work correctly for all users.

**Installation**

Using the installer script (recommended):

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.1.0).

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

**Technical Highlights**

- **Native AOT Compilation**: 13ms cold start time, 12MB binary size
- **JSON Source Generators**: Full AOT compatibility
- **Cross-Platform**: Linux, macOS (Intel/ARM), Windows support
- **Comprehensive Testing**: 38 unit tests covering all API operations

**Documentation**

- Full documentation: https://github.com/Aaronontheweb/pipedrive-cli/blob/dev/README.md
- Pipedrive API: https://developers.pipedrive.com/docs/api/v1

**Feedback**

Please report any issues or feature requests at https://github.com/Aaronontheweb/pipedrive-cli/issues

---

#### 0.1.0-beta3 January 4th 2025 ####

**CRITICAL HOTFIX RELEASE** - All users of 0.1.0-beta2 must upgrade immediately.

**Bug Fixes**

- **Fixed API URL Construction Bug** (commit 8464db1)
  - **Impact**: All API operations in 0.1.0-beta2 were completely broken, returning 401 Unauthorized errors
  - **Root Cause**: `UriBuilder.Path` was replacing the entire path, removing the `/api/v1/` prefix from API requests
  - **Symptom**: Requests were going to `https://domain.com/leads` instead of `https://domain.com/api/v1/leads`
  - **Resolution**: Fixed URL construction to preserve the BaseAddress path when HttpClient makes requests
  - **Result**: All API endpoints (leads, deals, activities, persons, organizations) now work correctly

**Technical Details**

The bug was introduced by using `UriBuilder.Path` to set the endpoint path, which replaces the entire path component of the URI rather than appending to it. This caused the `/api/v1/` prefix from the HttpClient's BaseAddress to be stripped out.

The fix constructs the relative URL path directly as a string, allowing HttpClient to properly combine it with the BaseAddress that includes the `/api/v1/` prefix.

**Installation**

```bash
# Linux/macOS
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.sh | bash -s -- --beta

# Windows PowerShell
iwr https://raw.githubusercontent.com/Aaronontheweb/pipedrive-cli/dev/install.ps1 -useb | iex
```

Or download binaries directly from the [releases page](https://github.com/Aaronontheweb/pipedrive-cli/releases/tag/0.1.0-beta3).

---

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
