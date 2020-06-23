namespace sleepanalyzer.core.edfreader
open System.IO
open System
open System.Text

type BaseEdfReader() = 
    member val internal Header = null with get,set
    member val internal File = null with get,set
    member val internal Dig_min = [] with get,set
    member val internal Phys_min = [] with get,set
    member val internal Gain = [] with get,set
    member val internal PointerState = 0 with get,set
    member val internal EventsTime = [] with get,set
    member val internal Labels = [] with get,set
    member val internal Signals : (float * float) list list = [] with get,set
    member val internal Events = [] with get,set
    member val internal FileBytes = [||] with get,set
    member public s.ReadOptimized (x: Stream, x_filename: string) = 
        let edfHeaderReader = new EdfHeader(x)
        s.Header <- edfHeaderReader.GetHeader
        s.File <- edfHeaderReader.payLoadText
        s.FileBytes <- edfHeaderReader.FileBytes
        s.Dig_min <- [for x in (s.Header.["digital_min"] :?> string list) -> try x |> float with _ -> 0.0]
        s.Phys_min <- [for x in (s.Header.["physical_min"] :?> string list) -> try x |> float with _ -> 0.0]
        let dig_max = [for x in (s.Header.["digital_max"] :?> string list) -> try x |> float with _ -> 0.0]
        let phys_max = [for x in (s.Header.["physical_max"] :?> string list) -> try x |> float with _ -> 0.0]

        let dig_range = [for i in 0 .. s.Dig_min.Length - 1 -> dig_max.[i] - s.Dig_min.[i]]
        let phys_range = [for i in 0 .. s.Phys_min.Length - 1 -> phys_max.[i] - s.Phys_min.[i]]

        s.Gain <- [for i in 0 .. phys_range.Length - 1 -> match dig_range.[i] with
                                                          | 0.0 -> 0.0
                                                          | _ ->  phys_range.[i] / dig_range.[i]]

        let periodList =  [for x in (s.Header.["n_samples_per_record"] :?> string list) -> try (1.0 / (x |> float)) with _ -> 1.0]
        let ns = (s.Header.["n_channels"] :?> int)
        let totalHeaderBytes = 256 + (ns * 256)
        s.PointerState <- totalHeaderBytes + 1
        let mutable lastByteIndex = s.PointerState
        // seek
        let samplesPerRecordList = s.Header.["n_samples_per_record"] :?> string list
        let mutable iterCounter = 0
        printf "%s" "\nStarting Ecg Data Ingestion...\n"
        while s.PointerState < s.FileBytes.Length do
            printf "%s" ("Read Iteration: " + (iterCounter |> string) + "\n")
            printf "%s" ((s.PointerState |> string) + " bytes of " + (s.FileBytes.Length |> string) + " read.\n")
            iterCounter <- iterCounter + 1
            
            try
                let mutable rawBytes = []
                for nsamp in samplesPerRecordList do
                    let nsampVal = try nsamp |> int with _ -> 1
                    let readSize = (nsampVal * 2)
                    lastByteIndex <- lastByteIndex + readSize

                    // lazy use of exception has flow-control --- Fix
                    if s.PointerState > s.FileBytes.Length then raise (System.AggregateException("Index passed"))
                    // guard against random 1 bit shift in some edf files
                    if lastByteIndex > s.FileBytes.Length then do lastByteIndex <- s.FileBytes.Length

                    let samples = s.FileBytes.[s.PointerState .. lastByteIndex - 1]
                    s.PointerState <- lastByteIndex + 1
                    rawBytes <- List.append rawBytes [samples]

                let mutable indexer = 0
                let mutable firstPassComplete = false
                let labels = s.Header.["label"] :?> string list
                s.Labels <- labels
                for rawBytesSubset in rawBytes do
                    if labels.[indexer] = "EDF Annotations" then
                        let ann = EdfTalAnnotation.GetTalAnnotations(Encoding.ASCII.GetString(rawBytesSubset))
                        s.EventsTime <- [for ann_item in ann -> extensions.Fst ann_item]
                        s.Events <- [for ann_item in ann -> extensions.Third ann_item]

                    else
                        let mutable tempSignalList : (float * float) list list = []
                        let dig = Async.Parallel [for index in 0..(rawBytesSubset.Length / sizeof<int16>) -> async{ return (try BitConverter.ToInt16(rawBytesSubset, index * sizeof<int16>) |> float with _ -> 0.0) }] |> Async.RunSynchronously
                        let period = periodList.[indexer]
                        let mutable startTime = 0.0
                        match iterCounter with 
                                        | 1 -> startTime <- 0.0 
                                        | _ -> if s.Signals.Length > 0 && s.Signals.[indexer].Length > 0 then do  startTime <- fst (s.Signals.[indexer] |> List.last)
                        let phys = Async.Parallel [for k in 1..dig.Length -> async { return ((period * (k |> float)) + startTime, (dig.[k - 1] - s.Dig_min.[indexer]) * s.Gain.[indexer] + s.Phys_min.[indexer ]) }] |> Async.RunSynchronously |> Array.toList
                        if iterCounter = 1 then
                            s.Signals <- List.append s.Signals [phys]
                        else
                            for po in 0..(s.Signals.Length - 1) do
                                if po = indexer then do
                                    tempSignalList <- List.append tempSignalList [(List.append s.Signals.[indexer] phys)]
                                else 
                                    tempSignalList <- List.append tempSignalList [s.Signals.[po]]
                            s.Signals <- tempSignalList
                    indexer <- indexer + 1
            with _ -> ()

        new Edf(s.Header, s.File, s.Dig_min, s.Phys_min, s.Gain, s.PointerState, s.EventsTime, s.Signals, s.Events, s.Labels, x_filename)