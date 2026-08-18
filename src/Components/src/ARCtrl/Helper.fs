module ARCtrl.Helper

let doptstr (o: string option) = Option.defaultValue "" o

let isNumber (input: string) =
    let success, _ = System.Double.TryParse(input)
    success
