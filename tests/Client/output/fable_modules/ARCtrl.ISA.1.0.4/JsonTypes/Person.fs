namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update
open Fable.Core

[<AttachMembers>]
type Person = 
    {   
        ID : URI option
        ORCID : string option
        LastName : string option
        FirstName : string option
        MidInitials : string option
        EMail : EMail option
        Phone : string option
        Fax : string option
        Address : string option
        Affiliation : string option
        Roles : OntologyAnnotation [] option
        Comments : Comment [] option  
    }

    static member make id orcid lastName firstName midInitials email phone fax address affiliation roles comments : Person =
        {
            ID = id
            ORCID = orcid
            LastName = lastName
            FirstName = firstName
            MidInitials = midInitials
            EMail = email
            Phone = phone
            Fax = fax
            Address = address
            Affiliation = affiliation
            Roles = roles
            Comments = comments
        }

    static member create (?Id,?ORCID,?LastName,?FirstName,?MidInitials,?Email,?Phone,?Fax,?Address,?Affiliation,?Roles,?Comments) : Person =
        Person.make Id ORCID LastName FirstName MidInitials Email Phone Fax Address Affiliation Roles Comments

    static member empty =
        Person.create ()

    static member tryGetByFullName (firstName : string) (midInitials : string) (lastName : string) (persons : Person []) =
        Array.tryFind (fun p -> 
            if midInitials = "" then 
                p.FirstName = Some firstName && p.LastName = Some lastName
            else 

                p.FirstName = Some firstName && p.MidInitials = Some midInitials && p.LastName = Some lastName
        ) persons

    ///// Returns true, if a person for which the predicate returns true exists in the investigation
    //static member exists (predicate : Person -> bool) (investigation:Investigation) =
    //    investigation.Contacts
    //    |> List.exists (predicate) 

    ///// Returns true, if the given person exists in the investigation
    //static member contains (person : Person) (investigation:Investigation) =
    //    exists ((=) person) investigation

    /// If an person with the given FirstName, MidInitials and LastName exists in the list, returns true
    static member existsByFullName (firstName : string) (midInitials : string) (lastName : string) (persons : Person []) =
        Array.exists (fun p -> 
            if midInitials = "" then 
                p.FirstName = Some firstName && p.LastName = Some lastName
            else 

                p.FirstName = Some firstName && p.MidInitials = Some midInitials && p.LastName = Some lastName
        ) persons

    /// adds the given person to the persons  
    static member add (persons : Person list) (person : Person) =
        List.append persons [person]

    /// Updates all persons for which the predicate returns true with the given person values
    static member updateBy (predicate : Person -> bool) (updateOption:UpdateOptions) (person : Person) (persons : Person []) =
        if Array.exists predicate persons 
        then
            persons
            |> Array.map (fun p -> if predicate p then updateOption.updateRecordType p person else p) 
        else 
            persons

    /// Updates all persons with the same FirstName, MidInitials and LastName as the given person with its values
    static member updateByFullName (updateOption:UpdateOptions) (person : Person) (persons : Person []) =
        Person.updateBy (fun p -> p.FirstName = person.FirstName && p.MidInitials = person.MidInitials && p.LastName = person.LastName) updateOption person persons
    
    /// If a person with the given FirstName, MidInitials and LastName exists in the list, removes it
    static member removeByFullName (firstName : string) (midInitials : string) (lastName : string) (persons : Person []) =
        Array.filter (fun p ->
            if midInitials = "" then
                (p.FirstName = Some firstName && p.LastName = Some lastName)
                |> not
            else
                (p.FirstName = Some firstName && p.MidInitials = Some midInitials && p.LastName = Some lastName)
                |> not
        ) persons

    // Roles
    
    /// Returns roles of a person
    static member getRoles (person : Person) =
        person.Roles
    
    /// Applies function f on roles of a person
    static member mapRoles (f : OntologyAnnotation [] -> OntologyAnnotation []) (person : Person) =
        { person with 
            Roles = Option.mapDefault [||] f person.Roles}
    
    /// Replaces roles of a person with the given roles
    static member setRoles (person : Person) (roles : OntologyAnnotation []) =
        { person with
            Roles = Some roles }

    // Comments
    
    /// Returns comments of a person
    static member getComments (person : Person) =
        person.Comments
    
    /// Applies function f on comments of a person
    static member mapComments (f : Comment [] -> Comment []) (person : Person) =
        { person with 
            Comments = Option.mapDefault [||] f person.Comments}
    
    /// Replaces comments of a person by given comment list
    static member setComments (person : Person) (comments : Comment []) =
        { person with
            Comments = Some comments }

    static member orcidKey = "ORCID"

    static member setOrcidFromComments (person : Person) =
        let isOrcidComment (c : Comment) = 
            c.Name.IsSome && (c.Name.Value.ToUpper().EndsWith(Person.orcidKey))
        let orcid,comments = 
            person.Comments
            |> Option.map (fun comments ->
                let orcid = 
                    comments
                    |> Array.tryPick (fun c -> if isOrcidComment c then c.Value else None)
                let comments = 
                    comments
                    |> Array.filter (isOrcidComment >> not)
                    |> Option.fromValueWithDefault [||]
                (orcid, comments)
            )
            |> Option.defaultValue (None, person.Comments)
        {person with ORCID = orcid; Comments = comments}

    static member setCommentFromORCID (person : Person) =
        let comments = 
            match person.ORCID, person.Comments with
            | Some orcid, Some comments -> 
                let comment = Comment.create (Name = Person.orcidKey, Value = orcid)
                Array.append comments [|comment|]
                |> Some
            | Some orcid, None -> 
                [|Comment.create (Name = Person.orcidKey, Value = orcid)|]
                |> Some
            | None, comments -> comments
        {person with Comments = comments}

    member this.Copy() : Person =
        let nextComments = this.Comments |> Option.map (Array.map (fun c -> c.Copy()))
        let nextRoles = this.Roles |> Option.map (Array.map (fun c -> c.Copy()))
        Person.make 
            this.ID 
            this.ORCID
            this.LastName 
            this.FirstName 
            this.MidInitials 
            this.EMail 
            this.Phone 
            this.Fax 
            this.Address 
            this.Affiliation 
            nextRoles
            nextComments
