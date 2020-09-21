#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

type ProcessorMessage = ProcessJob of int64 * int64
type Message = 
    | START of int * int
    | DONE

let system = System.create "system" (Configuration.defaultConfig())

let sqrtOf x = x * x
let isSqrt (x: int64) (y: int64) = x * x = y

let boss = 
    spawn system "boss" 
        <| fun bossMailbox ->
            let mutable count = 0
            let worker = 
                spawn bossMailbox "worker" 
                    <| fun workerMailbox ->
                        let rec workerLoop() = 
                            actor {
                                let! ProcessJob(s,e) = workerMailbox.Receive ()
                                let sum1 = int64 (List.sumBy sqrtOf [s .. e])    
                                let right = int64 (sqrt(double sum1) + 0.5)
                                
                                if isSqrt right sum1 then
                                    printfn "ans: %u" s
                                    
                                printfn "bb"
                                printfn "%A" workerMailbox.Sender()
                                workerMailbox.Sender() <! DONE
                                printfn "aa"
                                return! workerLoop()
                            }
                        workerLoop()

            let rec bossLoop() =
                actor {
                    let! (msg: Message) = bossMailbox.Receive()
                    match msg with
                    | START (x, y) ->
                        for i in 1 .. x do
                            let s = int64 i
                            let e = int64 (i + y - 1)
                            worker <! ProcessJob(s,e)
                            count <- count + 1

                        return! bossLoop()   
                        
                    | DONE -> 
                        count <- count - 1
                        printfn "ccc %A" count
                        if count = 0 then 
                            printfn "DONE"
                        else
                            return! bossLoop()   
                }
            bossLoop()
       

let main() = 
    let args = System.Environment.GetCommandLineArgs()
    let n = int args.[3]
    let k = int args.[4]
    boss <! START (n, k)

    0

main()

