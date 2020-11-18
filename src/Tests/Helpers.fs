[<AutoOpen>]
module TestHelpers

open Farmer
open Farmer.CoreTypes
open Microsoft.Rest.Serialization
open Farmer.Builders

let farmerToMs<'T when 'T : null> (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) data =
    data
    |> SafeJsonConvert.SerializeObject
    |> fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings)

let getResourceAtIndex serializationSettings index (builder:#IBuilder) =
    builder.BuildResources Location.WestEurope
    |> fun r -> r.[index].JsonModel |> farmerToMs serializationSettings

let findAzureResources<'T when 'T : null> (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) (deployment:#IDeploymentBuilder) =
    let template = Deployment.getTemplate deployment |> Writer.TemplateGeneration.processTemplate

    template.resources
    |> Seq.map SafeJsonConvert.SerializeObject
    |> Seq.choose (fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings) |> Option.ofObj)
    |> Seq.toList

type TypedArmTemplate<'ResT> = { Resources : 'ResT array }

let getFirstResourceOrFail (template: TypedArmTemplate<'ResourceType>) =
    if Array.length template.Resources < 1 then
        failwith "Template had no resources"
    template.Resources.[0]

let toTemplate loc (d : IBuilder) =
    arm {
        location loc
        add_resource d
    } 
    |> Deployment.getTemplate

let toTypedTemplate<'ResourceType> loc =
    toTemplate loc
    >> Writer.toJson
    >> SafeJsonConvert.DeserializeObject<TypedArmTemplate<'ResourceType>>
    >> getFirstResourceOrFail