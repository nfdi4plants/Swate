module Swate.Components.AuthenticationTypes

type SignInInformation = {
    GitLabBaseUrl: string
    PersonalAccessToken: string
    OnErrorCallback: exn -> unit
}

type GitLabIdentity = {
    provider: string
    extern_uid: string
    saml_provider_id: int option
}

type GitLabScimIdentity = obj

/// https://docs.gitlab.com/api/users/#as-an-administrator-2
type GitLabUser = {
    id: int
    username: string
    public_email: string option
    name: string
    state: string
    locked: bool
    avatar_url: string
    web_url: string
    created_at: string
    bio: string
    location: string
    linkedin: string
    twitter: string
    discord: string
    website_url: string
    github: string
    job_title: string
    pronouns: string option
    organization: string
    bot: bool
    work_information: string option
    local_time: string option
    last_sign_in_at: string
    confirmed_at: string
    last_activity_on: string
    email: string
    theme_id: int
    color_scheme_id: int
    projects_limit: int
    current_sign_in_at: string
    identities: GitLabIdentity list
    can_create_group: bool
    can_create_project: bool
    two_factor_enabled: bool
    external: bool
    private_profile: bool
    commit_email: string
    preferred_language: string
    shared_runners_minutes_limit: int option
    extra_shared_runners_minutes_limit: int option
    scim_identities: GitLabScimIdentity list
}

type UserInformation = {
    Name: string
    Email: string
    AvatarUrl: string
    TargetDataHub: string
} with

    static member FromGitLabUser (gitLabUser: GitLabUser) (targetDataHub: string) : UserInformation = {
        Name = gitLabUser.name
        Email = gitLabUser.email
        AvatarUrl = gitLabUser.avatar_url
        TargetDataHub = targetDataHub
    }

/// Platform-agnostic account summary for multi-account UI.
type AccountSummary = {
    AccountId: string
    Name: string
    Email: string
    AvatarUrl: string
    TargetDataHub: string
    IsActive: bool
}