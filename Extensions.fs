namespace sleepanalyzer.core.edfreader
open System.Text.RegularExpressions
open System
module extensions =
    let Third (_, _, c) = c

    let Snd (_, b, _) = b

    let Fst (a, _, _) = a

    let Trim (x : string) =
        let y = x.Trim(' ')
        y
    let FindWithRegex (regex: string, candidate: string) = 
        let mutable ret = []
        let regex = new Regex(regex)
        let reg_matches = regex.Matches(candidate)
        for n in reg_matches do
            ret <- n.Value::ret
        ret

    let GroupByConvention (candidate: string []) = 
        let mutable ret = []
        for n in candidate do
            let holder = n.Trim().Split([|' '|], 3)
            let v = holder.[0].Trim([|'+'|])
            match holder.Length with
                | 3 -> ret <- (TimeSpan.FromSeconds(holder.[0].Trim([|'+'|]) |> float).ToString(),
                               TimeSpan.FromSeconds(holder.[1].Trim([|'+'|]) |> float).ToString(), holder.[2])::ret
                | _ -> ()
        List.rev ret
