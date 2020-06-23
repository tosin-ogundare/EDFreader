namespace sleepanalyzer.core.edfreader
open System
open System.IO
open System.Xml.Linq
open System.Linq
open System.Text
open System.Collections.Generic


/// <summary>
///     This extension is designned to read annotations contained outside of an EDF file
///     but specifically targets the XML annotations format used by http://sleepdata.org
///</summary>
type XMLAnnotationsExtension =
    static member public Load(x : Stream) =
        let xmlString = (new StreamReader(x, Encoding.UTF8)).ReadToEnd()
        let xmlDoc = XDocument.Parse(xmlString)
        let scoredEvents = xmlDoc.Root.Elements(XName.Get("ScoredEvents")).Elements(XName.Get("ScoredEvent"))
        let scoredSleepEvents = scoredEvents.Where(fun x -> x.Element(XName.Get("EventType")) <> null && x.Element(XName.Get("EventType")).Value.ToLowerInvariant() = "stages|stages").ToList()
        let Events = [for i in 0..scoredSleepEvents.Count - 1 -> (scoredSleepEvents.[i]).Element(XName.Get("EventConcept")).Value]
        let EventsTime = [for scoredSleepEvent in scoredSleepEvents -> TimeSpan.FromSeconds(scoredSleepEvent.Element(XName.Get("Start")).Value |> float).ToString()]
        new Edf(new Dictionary<string, Object>(), [||] , [], [], [], 0, EventsTime, [], Events, [], null) 
