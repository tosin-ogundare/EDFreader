namespace sleepanalyzer.core.edfreader
open System.Text.RegularExpressions

type EdfTalAnnotation () =
    static member public GetTalAnnotations (annotationstxt: string) =

        let annotationstxt_p = Regex.Replace(Regex.Replace(annotationstxt,"[\u0014]" , " ", RegexOptions.Compiled),"[\u0015]" , " ", RegexOptions.Compiled)
        let annotationstxt_p_arr = Regex.Split(annotationstxt_p, "\0", RegexOptions.CultureInvariant)
        let talHolder = annotationstxt_p_arr |> extensions.GroupByConvention
        talHolder
