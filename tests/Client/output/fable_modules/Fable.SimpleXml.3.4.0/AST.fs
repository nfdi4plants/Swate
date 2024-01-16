namespace Fable.SimpleXml

[<AutoOpen>]
module AST = 

    type XmlElement = { 
        Namespace : string option
        Name : string
        Attributes : Map<string, string>
        Content : string 
        Children : XmlElement list 
        SelfClosing : bool
        IsTextNode : bool
        IsComment : bool
    }

    type XmlDocument = {
        Declaration : Map<string, string> option 
        Root : XmlElement 
    }