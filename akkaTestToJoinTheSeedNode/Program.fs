﻿open Akka
open Akka.Actor
open Akka.Cluster
open System.Text
open Akkling
open Akkling.Cluster.Sharding
open Akka.Cluster.Sharding
open System.Collections.Immutable
open System.Threading.Tasks
open System.Threading

let configWithPort () =
    let config = Configuration.parse ("""
        akka {
          stdout-loglevel = DEBUG
          loglevel = DEBUG
          #log-config-on-start = on 
          
          debug {  
                receive = on 
                autoreceive = on
                lifecycle = on
                event-stream = on
                unhandled = on
            }
          actor {
        
    
            # debug.unhandled = on
        
            provider = cluster
            inbox {
                inbox-size = 100000
            }
          }
          remote {
            dot-netty.tcp {
              #byte-order = "little-endian"
              hostname = 0.0.0.0
              port = 9100
            }
          }
          cluster {
                        auto-down-unreachable-after = off
                        roles = []
          }

          extensions = ["Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider,Akka.Cluster.Tools"]
        }
        """)
    config
 

[<EntryPoint>]
let main argv =
    let mre = ManualResetEvent(false)
    try 
        let system_local = Akka.Actor.ActorSystem.Create("cluster-system", configWithPort()) 
        let cluster = Cluster.Get system_local
        let il = ImmutableList.Create<Address>(seq[
            Address.Parse @"akka.tcp://cluster-system@192.168.43.122:9000"
            ]|>Seq.toArray)
        cluster.JoinSeedNodes il
        let rec show () =
            async {
                printfn "%A" cluster.State.Leader
                printfn "%A" cluster.State.Members
                printfn "%A" cluster.State.Unreachable
                do! Async.Sleep 1000
                do! show ()
            }
        show () |> Async.Start
        Task.WaitAll [| system_local.WhenTerminated |]
    with
    | exn ->
        printfn "%A" exn
    mre.WaitOne() |> ignore
    0 // return an integer exit code
