﻿module Archer.Quiver.TestAdapter.AssemblySystem

open System
open System.Reflection
open Archer.CoreTypes.InternalTypes
open Archer.Quiver.TestAdapter.FileSystem
open Archer.Quiver.TestAdapter.FileWrappers
open Microsoft.VisualStudio.TestPlatform.ObjectModel

type IPropertyWrapper =
    abstract member IsAssignableFrom<'desiredType> : unit -> bool
    abstract member GetStaticValue<'outPutType> : unit -> 'outPutType
    
type ITypeWrapper =
    abstract member GetPublicStaticPropertiesOf<'outPutType> : unit -> 'outPutType array
    
type IAssemblyWrapper =
    abstract member GetExportedTypes: unit -> ITypeWrapper array
    
type ITestLoader =
    abstract member GetTests: unit -> ITest array

type PropertyWrapper (prop: PropertyInfo) =
    interface IPropertyWrapper with
        member _.IsAssignableFrom<'desiredType> () =
            prop.PropertyType.IsAssignableFrom typeof<'desiredType>
            
        member _.GetStaticValue<'outPutType>() =
            null |> prop.GetValue :?> 'outPutType
            
type TypeWrapper (t: Type) =
    member _.GetProperties (bindingFlags: BindingFlags) =
        t.GetProperties bindingFlags
        |> Array.map (fun p -> p |> PropertyWrapper :> IPropertyWrapper)
        
    interface ITypeWrapper with
        member this.GetPublicStaticPropertiesOf<'outPutType> () =
            this.GetProperties (BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
            |> Array.filter (fun prop ->
                prop.IsAssignableFrom<'outPutType> ()
            )
            |> Array.map (fun p -> p.GetStaticValue<'outPutType> ())
            
type AssemblyWrapper (assembly: Assembly) =
    new (file: IFileInfoWrapper) =
        AssemblyWrapper (Assembly.LoadFile file.FullName)
        
    interface IAssemblyWrapper with
        member _.GetExportedTypes () =
            assembly.GetExportedTypes ()
            |> Array.map (fun t -> t |> TypeWrapper :> ITypeWrapper)
            
type TestLoader (assembly: IAssemblyWrapper) =
    new (file: IFileInfoWrapper) =
        TestLoader (AssemblyWrapper file)
        
    interface ITestLoader with
        member _.GetTests () =
            assembly.GetExportedTypes ()
            |> Array.map (fun t ->
                t.GetPublicStaticPropertiesOf<ITest>()
            )
            |> Array.concat
            
let getTestLoadersThroughAssembly (getLocator: string -> #IAssemblyLocator) (exampleFile: string) =
    let locator = getLocator exampleFile
    locator.GetPossibleTestFiles ()
    |> Array.map TestLoader
    
let getTestLoaders = getTestLoadersThroughAssembly AssemblyLocator

let getTests (loaders: #ITestLoader array) =
    loaders
    |> Array.map (fun l -> l.GetTests ())
    |> Array.concat
    
let getTestCase (pathHelper: IPathWrapper) (test: ITest) =
     let getTestFullName (test: ITest) = $"%s{test.ContainerPath}.%s{test.ContainerName}.%s{test.TestName}"
     
     let tc = TestCase (test |> getTestFullName, ExecutorUri |> Uri, pathHelper.Join (test.Location.FilePath, test.Location.FileName))
     tc