# Intention
Redesign the DataHubBrowser GitLab Explore experience to feel closer to GitLab, reduce visual noise, improve action clarity. Create clear difference between logged-in and logged-out states, and ensure the UI is intuitive and accessible for all users.

# Agent directive

- when a task is done, change it from [ ] to [x] and append "resolved" to the same line.
- when a Phase is done, reread The `#I Intention` the upcoming Phase.

## Phase 1 - Baseline and UX Contract
- [x] Define and lock final repo row metadata contract: owner, repo name, short description, stars, last updated, visibility icon (public/private), tags, license, and clone/open actions (resolved when approved). resolved
- [x] Define visual emphasis rules to avoid overload: one primary accent target per row (repo name/action), secondary muted metadata, and spacing groups by closeness (resolved when documented). resolved
- [x] Confirm icon set and syntax policy from existing patterns (iconify + swt prefixed classes) using Actionbar.fs and existing component usage, including privacy/sort/action icons (resolved when mapped). resolved

### Phase 1 Decisions (Locked)

#### Repo Row Metadata Contract

- Owner: derived from `path_with_namespace` first segment with fallback to ``namespace``.name.
- Repo name: primary clickable link to `web_url`; this remains the highest visual emphasis target.
- Short description: `description` with fallback text only when missing.
- Stars: compact numeric badge or icon+number in secondary metadata group.
- Last updated: show `updated_at` in compact human-readable format near stars/date cluster.
- Visibility: icon only (`public` / `private`, and optionally `internal` later) with tooltip and aria label.
- Tags: render from runtime project tags/topic fields once present in DTO; no hardcoded tags.
- License: render compactly from DTO when available; hide if absent.
- Actions: row uses ActionBar icon-only square buttons for clone/open and future actions.

#### Visual Emphasis Rules

- One primary accent per row: repo name link (and action icons only on interaction states).
- Secondary metadata style: owner, visibility, stars, updated date, tags, and license use muted text/ghost badge treatment.
- Closeness grouping:
	- Group A (identity): avatar, owner, repo name, visibility icon.
	- Group B (context): description.
	- Group C (facts): stars, updated date, tags, license.
	- Group D (actions): clone/open icons in right-aligned ActionBar.
- Density: keep vertical rhythm tight, avoid repeated high-contrast badges, and keep metadata in one compact scan line where possible.

#### Icon Syntax and Mapping Policy

- Syntax contract:
	- Use `swt:iconify <icon-name> [swt:size-*]` classes.
	- Action buttons must use `Actionbar.Button`/`Actionbar.Main` patterns for tooltip + square icon button styling.
	- Tooltips should provide accessible labels mirroring action/visibility semantics.
- Approved icon family baseline: Fluent icon names already used across components (`swt:fluent--...`).
- Initial mapping for this feature:
	- Visibility public: `swt:fluent--globe-24-regular`.
	- Visibility private: `swt:fluent--lock-closed-24-regular`.
	- Visibility internal: `swt:fluent--shield-24-regular`.
	- Sort ascending: `swt:fluent--arrow-sort-up-24-regular`.
	- Sort descending: `swt:fluent--arrow-sort-down-24-regular`.
	- Clone action: `swt:fluent--arrow-download-24-regular`.
	- Open action: `swt:fluent--open-24-regular`.

## Phase 2 - Data and State Model Updates
- [x] Extend Explore tab model to include All tab with explicit behavior: includes private repositories only when authenticated, otherwise only public results (resolved when tab enum and load branching plan is finalized). resolved
- [x] Add sort/filter state model in DataHubBrowser.fs: daisyUI join control with sort field select (Last Updated, Date Created, Name, Stars) and direction toggle icon; change triggers immediate fetch (resolved when state transitions are defined). resolved
- [x] Replace search auto-run behavior with explicit submit trigger state in DataHubBrowser.fs: typing does not fetch, Search button fetches, tab switch fetches immediately (resolved when trigger flow is defined). resolved
- [x] Add authentication state source in DataHubBrowser.fs from external user prop (`Some user` = authenticated, `None` = not authenticated), decoupled from PAT storage (resolved when source-of-truth is selected). resolved

## Phase 3 - API and Mock Alignment
- [x] Add visibility field mapping to project DTO mapping in GitLabApi.fs (`public`/`private` and optional `internal`) so privacy icon is data-driven instead of path parsing (resolved when DTO contract is updated). resolved
- [x] Ensure sort field and direction are wired into relevant GitLab calls for Your Repos, Your Organisations, and All in GitLabApi.fs; keep Most Starred fixed to stars descending (resolved when each endpoint path is mapped). resolved
- [x] Align API call auth behavior: authenticated mode includes PAT (private access), unauthenticated mode omits PAT (public-only access), without coupling PAT storage to component state (resolved when call paths are documented). resolved
- [x] Update mock repo objects to include visibility, tags, license, and validated created/updated dates for local explore mode in MockData.fs (resolved when mock schema matches runtime schema). resolved

## Phase 4 - UI Refactor of Repo Row
- [x] Replace private/public text badge with privacy icon (tooltip for accessibility) in repo row rendering inside DataHubBrowser.fs (resolved when icon replaces label everywhere). resolved
- [x] Add last updated display in compact metadata format, visually secondary, near stars/date group in DataHubBrowser.fs (resolved when date format and placement are consistent). resolved
- [x] Keep repo row metadata aligned to contract: owner, repo name, short description, stars, last updated, visibility icon, tags, license, and action buttons only in DataHubBrowser.fs (resolved when row contract is fully rendered). resolved
- [x] Replace right-side custom buttons with ActionBar integration from Actionbar.fs using square icon-only action buttons (clone/open/etc.) in DataHubBrowser.fs (resolved when all row actions use ActionBar). resolved
- [x] Implement avatar fallback: if no image, show daisyUI avatar placeholder with repo first letter in DataHubBrowser.fs (resolved when fallback appears for missing avatar_url). resolved
- [x] Keep row density GitLab-like: clear left-to-right scan path in DataHubBrowser.fs. On small screens layout should change from left-to-right to top-to-bottom (resolved when visual audit passes). resolved

## Phase 5 - Explore Controls and Access States
- [x] Add All tab in explore tabs and make it paginated like others in DataHubBrowser.fs (resolved when pagination metadata works for All). resolved
- [x] Keep Most Starred behavior fixed to stars descending and hide sort controls in that tab to avoid confusion in DataHubBrowser.fs (resolved when sort controls are conditionally hidden). resolved
- [x] Change search behavior to button-based execution only (no enter/debounce auto-fetch) in DataHubBrowser.fs, while tab changes still auto-fetch immediately (resolved when fetch only happens on defined triggers). resolved
- [x] Add filter control as daisyUI joined controls: left select for Last Updated/Date Created/Name/Stars, right icon toggle for ascending/descending; both trigger immediate fetch in DataHubBrowser.fs (resolved when both controls update query/load). resolved
- [x] Implement restricted-tab behavior for not authenticated state: Your Repos and Your Organisations are disabled/greyed, remain focusable with `aria-disabled`, and show tooltip "Log in to access this tab" on hover/focus in DataHubBrowser.fs (resolved when both keyboard and mouse users receive hint). resolved
- [x] Define Your Organisations fallback states in DataHubBrowser.fs: "No groups found" on successful empty result, "Failed to load groups" on fetch error, and normal repository list when a group is selected (resolved when all three states are rendered). resolved

## Phase 6 - Validation and QA
- [ ] Verify no regressions in loading/error/empty/pagination states for all tabs in DataHubBrowser.fs, including All tab and organisation empty/error states (resolved when manual test matrix passes).
- [x] Verify icon consistency with project conventions (swt iconify classes) across privacy, sort direction, filter controls, and row actions (resolved when all icons come from approved syntax). resolved
- [x] Verify accessibility basics: tooltips, focusable disabled-tab semantics with `aria-disabled`, button labels/aria, and keyboard navigation for action icons (resolved when checklist passes). resolved
- [ ] Verify visual simplicity requirement: only relevant metadata shown, color attracts to actionable/important elements, similarity/closeness principles are applied consistently (resolved when design review is accepted).
- [ ] Update Storybook/tests for changed behavior and test IDs (ActionBar icon actions, explicit search submit, conditional sort control visibility) (resolved when test suite/story checks pass).

## Phase 7 - Documentation and Handoff
- [x] Record final behavior and UI rationale in DataHubBrowserComponent.md including tab permissions, authentication prop contract, filter semantics, and action bar conventions (resolved when doc is complete). resolved
- [x] Add a concise developer/AGENTS note for future icon additions  (resolved when note is merged). resolved

## Final Behavior and UI Rationale

### Tab Permissions

- All: available for all users; authenticated users can receive private/internal results, unauthenticated users are restricted to public visibility.
- Most Starred: available for all users and always sorted by stars descending.
- Your Repos / Your Organisations: restricted for unauthenticated users; tabs stay focusable with `aria-disabled="true"`, visual disabled styling, and tooltip hint.

### Authentication Prop Contract

- `DataHubBrowser.GitLabEntry(?user: CurrentUserDto)` is the source of truth for authentication state.
- `Some user` means authenticated mode.
- `None` means unauthenticated mode.
- PAT storage/input is no longer used as authentication state source; unauthenticated public calls omit the PAT header.

### Filter and Search Semantics

- Search typing updates only local input state.
- Search is executed only via Search button.
- Tab switches still trigger immediate reload.
- Sort control uses a daisyUI joined control:
	- sort field select: Last Updated, Date Created, Name, Stars.
	- direction toggle: asc/desc icon button.
- Sort changes trigger immediate reload, except Most Starred where sorting remains fixed and controls are hidden.

### Action Bar Conventions

- Repo row actions are rendered via `Actionbar.Main` icon-only square buttons.
- Default row actions:
	- Clone (download icon)
	- Open (only when locally cloned)
- Visibility uses status icons with tooltip+aria semantics instead of text badges.
- Avatar rendering uses image when available, otherwise an initial placeholder.
