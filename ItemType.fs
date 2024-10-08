module Parser.ItemType

open FSharp.Compiler.Symbols

type ItemType =
    | List of ItemType
    | Array of ItemType
    | Dictionary of Key: ItemType * Value: ItemType
    | Option of ItemType
    | Result of Ok: ItemType * Error: ItemType
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
    | Record of Name: string * Fields: (string * ItemType) list
    | AnonymousRecord of (string * ItemType) list
    | Tuple of ItemType list
    | Other of FSharpType

let rec get (x: FSharpType) =
    let getArg (x: FSharpType) = x.GenericArguments[0] |> get

    let getArgPair (x: FSharpType) =
        let args = x.GenericArguments
        (get args[0], get args[1])

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
            try
                x.ErasedType.BasicQualifiedName
            with :? System.InvalidOperationException ->
                x.BasicQualifiedName

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
        | "Microsoft.FSharp.Core.array`1" -> x |> getArg |> Array
        | "Microsoft.FSharp.Collections.FSharpList`1" -> x |> getArg |> List
        | "Microsoft.FSharp.Core.FSharpOption`1" -> x |> getArg |> Option
        | "System.Collections.Generic.Dictionary`2" -> x |> getArgPair |> Dictionary
        | "Microsoft.FSharp.Core.FSharpResult`2" -> x |> getArgPair |> Result
        | _ ->
            eprintfn "Unrecognised: %A" typeName
            Other x

let fromEntity (x: FSharpEntity) = x.AsType() |> get
