namespace sleepanalyzer.core.edfreader
open System.Collections.Generic
open System

type Edf =
    struct
        val Header : Dictionary<string, Object>
        val File:  Char array
        val Dig_min: float list
        val Phys_min: float list
        val Gain : float list
        val PointerState : int
        val EventsTime : string list
        val Signals : (float * float) list list
        val Events :  string list
        val Labels :  string list
        val FileName: string
    end
    new (header : Dictionary<string, Object>, file : Char array, dig_min: float list, phys_min: float list,
         gain: float list, pointerstate : int, eventstime: string list, signals: (float * float) list list, events: string list,
         labels: string list, filename: string)  = {       
                                    Header = header
                                    File = file
                                    Dig_min = dig_min
                                    Phys_min = phys_min
                                    Gain = gain
                                    PointerState = pointerstate
                                    EventsTime = eventstime
                                    Signals = signals
                                    Events = events
                                    Labels = labels
                                    FileName = filename
                                   }