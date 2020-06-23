namespace sleepanalyzer.core.edfreader
open System
open System.IO
open System.Text
open System.Collections.Generic

type EdfHeader(x : Stream) =
    let filebytes = [| for i in  1.. (x.Length |> int) -> new byte() |]
    let _fToken = x.Read(filebytes, 0, filebytes.Length)
    let m = System.Text.Encoding.ASCII.GetString(filebytes)
    member internal s.FileBytes = filebytes
    member val internal pointerState = 0 with get,set
    member val private headerDictionary = new Dictionary<string, Object>() with get,set
    member val internal payLoadText = [||] with get,set

    member internal s.GetHeader = 
        s.payLoadText <- m |> Seq.toArray
        s.pointerState <- s.pointerState + 8
        let mutable lastCharacterIndex = s.pointerState + 79

        s.headerDictionary.Add("local_subject_id", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex]) |> extensions.Trim)
        s.pointerState <- lastCharacterIndex +  1
        lastCharacterIndex <- lastCharacterIndex + 80

        s.headerDictionary.Add("local_recording_id", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex]) |> extensions.Trim)
        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 8

        let timeholder1 = [for x in extensions.FindWithRegex("(\d+)", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex])) -> x |> int]
        let (day, month, year) = (timeholder1.[2], timeholder1.[1], timeholder1.[0])

        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 8

        let timeholder2 = [for x in extensions.FindWithRegex("(\d+)", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex])) -> x |> int]
        let (sec, min, hour) = (timeholder2.[2], timeholder2.[1], timeholder2.[0])

        s.headerDictionary.Add("date_time", String.Format("{0}/{1}/{2}-{3}:{4}:{5}", month, day, year, hour, min, sec))
        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 8

        s.headerDictionary.Add("n_header_bytes", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex]) |> extensions.Trim)
        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 44

        let subtype = new string(s.payLoadText.[s.pointerState .. lastCharacterIndex] |> Array.take 5)
        s.headerDictionary.Add("EDF+", subtype.StartsWith("EDF+"))
        s.headerDictionary.Add("contiguous", subtype <> "EDF+D")
        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 8

        s.headerDictionary.Add("n_records", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex]))
        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 8

        s.headerDictionary.Add("record_length", new string(s.payLoadText.[s.pointerState .. lastCharacterIndex]))
        s.pointerState <- lastCharacterIndex + 1
        lastCharacterIndex <- lastCharacterIndex + 4

        let nchannels = new string(s.payLoadText.[s.pointerState .. lastCharacterIndex]) |> int
        s.headerDictionary.Add("n_channels", nchannels)

        s.headerDictionary.Add("label", [for i in 1 .. nchannels -> new string(s.payLoadText.[ (1 + lastCharacterIndex + 16 * (i - 1)) .. (lastCharacterIndex + 16 * i) ])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 16 * nchannels
        s.headerDictionary.Add("transducer_type", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 80 * (i - 1)).. (lastCharacterIndex + 80 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 80 * nchannels
        s.headerDictionary.Add("units", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 8 * (i - 1)).. (lastCharacterIndex + 8 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 8 * nchannels
        s.headerDictionary.Add("physical_min", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 8 * (i - 1)).. (lastCharacterIndex + 8 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 8 * nchannels
        s.headerDictionary.Add("physical_max", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 8 * (i - 1)).. (lastCharacterIndex + 8 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 8 * nchannels
        s.headerDictionary.Add("digital_min", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 8 * (i - 1)).. (lastCharacterIndex + 8 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 8 * nchannels
        s.headerDictionary.Add("digital_max", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 8 * (i - 1)).. (lastCharacterIndex + 8 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 8 * nchannels
        s.headerDictionary.Add("prefiltering", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 80 * (i - 1)).. (lastCharacterIndex + 80 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 80 * nchannels
        s.headerDictionary.Add("n_samples_per_record", [for i in 1 .. nchannels -> new string(s.payLoadText.[(1 + lastCharacterIndex + 8 * (i - 1)).. (lastCharacterIndex + 8 * i)])|> extensions.Trim])
        lastCharacterIndex <- lastCharacterIndex + 8 * nchannels

        // reserved
        s.pointerState <- lastCharacterIndex + (32 * nchannels)
        s.headerDictionary
