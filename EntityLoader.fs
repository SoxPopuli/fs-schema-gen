namespace Parser

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open Ionide
open System

type MultiValueDictionary<'Key, 'Value when 'Key: equality>() =
    inherit Collections.Generic.Dictionary<'Key, Collections.Generic.List<'Value>>()

    member this.Add(key, value) =
        if this.ContainsKey key then
            this[key].Add value
        else
            let list = Collections.Generic.List<'Value>()
            list.Add value
            this[key] <- list

type EntityLoader(checkData: FSharpCheckProjectResults) =
    // Cache looked up entities to speed up recall
    // let entityCache = MultiValueDictionary<string, FSharpEntity>()

    static member FromProjectOptions (loader: ProjectLoader) (options: ProjInfo.Types.ProjectOptions) =
        options
        |> loader.ParseAndCheckProjectAsync
        |> Async.RunSynchronously
        |> EntityLoader

    ///**WARNING**: May return *a lot* of data
    member _.FindAll() =
        let rec getNestedEntities acc (entity: FSharpEntity) =
            if Seq.isEmpty entity.NestedEntities then
                entity :: acc |> Seq.ofList
            else
                entity.NestedEntities |> Seq.map (getNestedEntities []) |> Seq.concat

        checkData.AssemblySignature.Entities
        |> Seq.map (getNestedEntities [])
        |> Seq.concat

    member _.FindEntityByPath(path: string seq) =
        checkData.AssemblySignature.FindEntityByPath(path |> Seq.toList)

    member this.FindEntityByPath([<ParamArray>] path) =
        path |> Array.toSeq |> this.FindEntityByPath
