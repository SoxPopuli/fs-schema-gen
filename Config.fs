module Parser.Config

open Tomlyn
open Tomlyn.Syntax
open Tomlyn.Model
open System.Collections.Generic
open Utils

type TypeTree = TypeTree.TypeTree

type JsonOptions = {
    Case: CaseType option
    KeepNulls: bool option
}

and CaseType =
    | Snake
    | Camel
    | Pascal

type Overrides = {
    Input: TypeTree option
    Output: TypeTree option
}

type Endpoint = {
    Input: string
    Output: string
    JsonOptions: JsonOptions option
    Overrides: Overrides option
}

type Config = { Endpoints: Endpoint list }

let private tryGet key (dict: #IDictionary<'a, 'b>) =
    if dict.ContainsKey key then Some dict[key] else None

module private Obj =
    let asString (x: obj) = x :?> string

    let asTable (x: obj) = x :?> Utils.Table

    let asBool (x: obj) = x :?> bool

let private parseJsonEntry (model: #Table) =
    let allowedCases = Map [ "snake", Snake; "camel", Camel; "pascal", Pascal ]

    let getCaseType case =
        if not (Map.containsKey case allowedCases) then
            let allowed = Map.keys allowedCases |> Utils.String.join ", "

            failwith $"Unexpected case value '{case}', allowed values are [{allowed}]"
        else
            allowedCases |> Map.find case

    let case = model |> tryGet "case" |> Option.map (Obj.asString >> getCaseType)

    let keepNulls = model |> tryGet "keep_nulls" |> Option.map Obj.asBool

    {
        JsonOptions.Case = case
        KeepNulls = keepNulls
    }

let private parseOverrides entityLoader (model: #Table) =
    let inputs =
        model
        |> tryGet "input"
        |> Option.map Obj.asTable
        |> Option.map (TypeTree.fromTable entityLoader "input")

    let outputs =
        model
        |> tryGet "output"
        |> Option.map Obj.asTable
        |> Option.map (TypeTree.fromTable entityLoader "output")

    {
        Overrides.Input = inputs
        Output = outputs
    }


let private parseEndpoint entityLoader (model: #Table) =
    let input = model["input"] :?> string

    let output = model["output"] :?> string

    let json = model |> tryGet "json" |> Option.map (Obj.asTable >> parseJsonEntry)

    let overrides =
        model
        |> tryGet "overrides"
        |> Option.map (Obj.asTable >> parseOverrides entityLoader)

    {
        Input = input
        Output = output
        JsonOptions = json
        Overrides = overrides
    }

let parseToml entityLoader (data: string) =
    let model = data |> Toml.Parse |> Toml.ToModel

    model["endpoint"] :?> TomlTableArray
    |> Seq.map Dictionary
    |> Seq.map (parseEndpoint entityLoader)
