namespace ARCtrl.ISA

type StudyMaterials = 
    {   
        Sources : Source list option
        Samples : Sample list option
        OtherMaterials : Material list option
    }


    static member make sources samples otherMaterials =
        {
            Sources = sources
            Samples = samples
            OtherMaterials = otherMaterials           
        }
    static member create (?Sources,?Samples,?OtherMaterials) : StudyMaterials =
        StudyMaterials.make Sources Samples OtherMaterials

    static member empty =
        StudyMaterials.create ()

    static member getMaterials (am : StudyMaterials) =
        am.OtherMaterials |> Option.defaultValue []
        
    static member getSamples (am : StudyMaterials) =
        am.Samples |> Option.defaultValue []

    static member getSources (am : StudyMaterials) =
        am.Sources |> Option.defaultValue []