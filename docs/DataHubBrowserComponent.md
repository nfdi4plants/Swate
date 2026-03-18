# Intention
Redesign the DataHubBrowser GitLab Explore experience to feel closer to GitLab, reduce visual noise, improve action clarity. Create clear difference between logged-in and logged-out states, and ensure the UI is intuitive and accessible for all users.

# Agent directive

- when a task is done, change it from [ ] to [x] and append "resolved" to the same line.
- when a Phase is done, reread The `#I Intention` the upcoming Phase.

## Phase 1 - Baseline and UX Contract
- [ ] Define and lock final repo row metadata contract: owner, repo name, short description, stars, last updated, visibility icon (public/private), tags, license, and clone/open actions (resolved when approved).
- [ ] Define visual emphasis rules to avoid overload: one primary accent target per row (repo name/action), secondary muted metadata, and spacing groups by closeness (resolved when documented).
- [ ] Confirm icon set and syntax policy from existing patterns (iconify + swt prefixed classes) using Actionbar.fs and existing component usage, including privacy/sort/action icons (resolved when mapped).

## Phase 2 - Data and State Model Updates
- [ ] Extend Explore tab model to include All tab with explicit behavior: includes private repositories only when authenticated, otherwise only public results (resolved when tab enum and load branching plan is finalized).
- [ ] Add sort/filter state model in DataHubBrowser.fs: daisyUI join control with sort field select (Last Updated, Date Created, Name, Stars) and direction toggle icon; change triggers immediate fetch (resolved when state transitions are defined).
- [ ] Replace search auto-run behavior with explicit submit trigger state in DataHubBrowser.fs: typing does not fetch, Search button fetches, tab switch fetches immediately (resolved when trigger flow is defined).
- [ ] Add authentication state source in DataHubBrowser.fs from external user prop (`Some user` = authenticated, `None` = not authenticated), decoupled from PAT storage (resolved when source-of-truth is selected).

## Phase 3 - API and Mock Alignment
- [ ] Add visibility field mapping to project DTO mapping in GitLabApi.fs (`public`/`private` and optional `internal`) so privacy icon is data-driven instead of path parsing (resolved when DTO contract is updated).
- [ ] Ensure sort field and direction are wired into relevant GitLab calls for Your Repos, Your Organisations, and All in GitLabApi.fs; keep Most Starred fixed to stars descending (resolved when each endpoint path is mapped).
- [ ] Align API call auth behavior: authenticated mode includes PAT (private access), unauthenticated mode omits PAT (public-only access), without coupling PAT storage to component state (resolved when call paths are documented).
- [ ] Update mock repo objects to include visibility, tags, license, and validated created/updated dates for local explore mode in MockData.fs (resolved when mock schema matches runtime schema).

## Phase 4 - UI Refactor of Repo Row
- [ ] Replace private/public text badge with privacy icon (tooltip for accessibility) in repo row rendering inside DataHubBrowser.fs (resolved when icon replaces label everywhere).
- [ ] Add last updated display in compact metadata format, visually secondary, near stars/date group in DataHubBrowser.fs (resolved when date format and placement are consistent).
- [ ] Keep repo row metadata aligned to contract: owner, repo name, short description, stars, last updated, visibility icon, tags, license, and action buttons only in DataHubBrowser.fs (resolved when row contract is fully rendered).
- [ ] Replace right-side custom buttons with ActionBar integration from Actionbar.fs using square icon-only action buttons (clone/open/etc.) in DataHubBrowser.fs (resolved when all row actions use ActionBar).
- [ ] Implement avatar fallback: if no image, show daisyUI avatar placeholder with repo first letter in DataHubBrowser.fs (resolved when fallback appears for missing avatar_url).
- [ ] Keep row density GitLab-like: clear left-to-right scan path in DataHubBrowser.fs. On small screens layout should change from left-to-right to top-to-bottom (resolved when visual audit passes).

## Phase 5 - Explore Controls and Access States
- [ ] Add All tab in explore tabs and make it paginated like others in DataHubBrowser.fs (resolved when pagination metadata works for All).
- [ ] Keep Most Starred behavior fixed to stars descending and hide sort controls in that tab to avoid confusion in DataHubBrowser.fs (resolved when sort controls are conditionally hidden).
- [ ] Change search behavior to button-based execution only (no enter/debounce auto-fetch) in DataHubBrowser.fs, while tab changes still auto-fetch immediately (resolved when fetch only happens on defined triggers).
- [ ] Add filter control as daisyUI joined controls: left select for Last Updated/Date Created/Name/Stars, right icon toggle for ascending/descending; both trigger immediate fetch in DataHubBrowser.fs (resolved when both controls update query/load).
- [ ] Implement restricted-tab behavior for not authenticated state: Your Repos and Your Organisations are disabled/greyed, remain focusable with `aria-disabled`, and show tooltip "Log in to access this tab" on hover/focus in DataHubBrowser.fs (resolved when both keyboard and mouse users receive hint).
- [ ] Define Your Organisations fallback states in DataHubBrowser.fs: "No groups found" on successful empty result, "Failed to load groups" on fetch error, and normal repository list when a group is selected (resolved when all three states are rendered).

## Phase 6 - Validation and QA
- [ ] Verify no regressions in loading/error/empty/pagination states for all tabs in DataHubBrowser.fs, including All tab and organisation empty/error states (resolved when manual test matrix passes).
- [ ] Verify icon consistency with project conventions (swt iconify classes) across privacy, sort direction, filter controls, and row actions (resolved when all icons come from approved syntax).
- [ ] Verify accessibility basics: tooltips, focusable disabled-tab semantics with `aria-disabled`, button labels/aria, and keyboard navigation for action icons (resolved when checklist passes).
- [ ] Verify visual simplicity requirement: only relevant metadata shown, color attracts to actionable/important elements, similarity/closeness principles are applied consistently (resolved when design review is accepted).
- [ ] Update Storybook/tests for changed behavior and test IDs (ActionBar icon actions, explicit search submit, conditional sort control visibility) (resolved when test suite/story checks pass).

## Phase 7 - Documentation and Handoff
- [ ] Record final behavior and UI rationale in DataHubBrowserComponent.md including tab permissions, authentication prop contract, filter semantics, and action bar conventions (resolved when doc is complete).
- [ ] Add a concise developer/AGENTS note for future icon additions  (resolved when note is merged).
