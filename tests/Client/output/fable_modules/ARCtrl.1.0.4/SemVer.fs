module ARCtrl.SemVer

open ARCtrl.ISA.Regex.ActivePatterns

module SemVerAux =

    let Pattern =
            @"^(?<major>\d+)" +
            @"(\.(?<minor>\d+))?" +
            @"(\.(?<patch>\d+))?" +
            @"(-(?<pre>[0-9A-Za-z-\.]+))?" +
            @"(\+(?<build>[0-9A-Za-z-\.]+))?$"


type SemVer = {
    Major: int
    Minor: int
    Patch: int
    PreRelease: string option
    Metadata: string option
} with
    static member make major minor patch pre meta = {
        Major = major
        Minor = minor
        Patch = patch
        PreRelease = pre
        Metadata = meta
    }

    static member create(major: int, minor: int, patch: int, ?pre: string, ?meta: string) =
        {
            Major = major
            Minor = minor
            Patch = patch
            PreRelease = pre
            Metadata = meta
        }

    static member tryOfString (str: string) =
        match str with
        | Regex (SemVerAux.Pattern) m ->
            let g = m.Groups
            let major = int g.["major"].Value
            let minor = int g.["minor"].Value
            let patch = int g.["patch"].Value
            let pre = 
                match g.["pre"].Success with
                | true -> Some g.["pre"].Value
                | false -> None
            let meta =
                match g.["build"].Success with
                | true -> Some g.["build"].Value
                | false -> None
            Some <| SemVer.create(major, minor, patch, ?pre=pre, ?meta=meta)
        | _ -> None

    member this.AsString() : string =
        let sb = System.Text.StringBuilder()
        sb.Append(sprintf "%i.%i.%i" this.Major this.Minor this.Patch) |> ignore
        if this.PreRelease.IsSome then
            sb.Append(sprintf "-%s" this.PreRelease.Value) |> ignore
        if this.Metadata.IsSome then
            sb.Append(sprintf "+%s" this.Metadata.Value) |> ignore
        sb.ToString()