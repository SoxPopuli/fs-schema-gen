module Parser.ItemType
open FSharp.Compiler.Symbols

type t =
    | List of t
    | Array of t
    | Option of t
    | Result of Ok: t * Error: t
    | Int8
    | Int16
    | Int32
    | Int64
    | Float
    | Decimal
    | String
    | Bool
    | Object
    | DateTime
    | Guid
    | Record of Name: string * Fields: (string * t) list
    | AnonymousRecord of (string * t) list
    | Tuple of t list
    | Other of FSharpType

let rec get (x: FSharpType) =
    let getArg (x: FSharpType) =
        x.GenericArguments
        |> Seq.head
        |> get

    if x.IsAnonRecordType then
        let fieldNames = x.AnonRecordTypeDetails.SortedFieldNames
        let types = x.GenericArguments |> Seq.map get

        (fieldNames, types) ||> Seq.zip |> Seq.toList |> AnonymousRecord
    else if x.IsTupleType then
        x.GenericArguments |> Seq.map get |> Seq.toList |> Tuple
    else if x.HasTypeDefinition && x.TypeDefinition.IsFSharpRecord then
        let fields = x.TypeDefinition.FSharpFields
        let fields = fields |> Seq.map (fun x -> x.Name, get x.FieldType) |> Seq.toList
        let recordName = x.TypeDefinition.DisplayName

        Record(recordName, fields)
    else
        let typeName = 
            try x.ErasedType.BasicQualifiedName with
            | :? System.InvalidOperationException -> x.BasicQualifiedName
        //printfn "%A := %A" x.BasicQualifiedName (x.StripAbbreviations().BasicQualifiedName)
        match typeName with
        | "System.Int8" -> Int8
        | "System.Int16" -> Int16
        | "System.Int32" -> Int32
        | "System.Int64" -> Int64
        | "System.Boolean" -> Bool
        | "System.String" -> String
        | "System.Double" -> Float
        | "System.Decimal" -> Decimal
        | "System.Object" -> Object
        | "System.DateTime" -> DateTime
        | "System.Guid" -> Guid
        | "Microsoft.FSharp.Core.array`1" ->
            x |> getArg |> Array
        | "Microsoft.FSharp.Collections.FSharpList`1" -> x |> getArg |> List
        | "Microsoft.FSharp.Core.FSharpOption`1" -> x |> getArg |> Option
        | "Microsoft.FSharp.Core.FSharpResult`2" ->
            let args = x.GenericArguments
            Result(get args[0], get args[1])
        | _ ->
            eprintfn "Unrecognised: %A" typeName
            Other x

let fromEntity (x: FSharpEntity) =
    x.AsType () |> get
