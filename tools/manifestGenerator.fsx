open System.Xml.

type ManifestIcon = {
    Size    : int
    Id      :string
    Path    :string
}
with
    static member toXmlRessource (icon:ManifestIcon) =
        ()

type ManifestControl = {
    ControlType : string
    Id          : string
    Label       : string
    ToolTip     : string
    Icons       : ManifestIcon []
    Action      : string
}