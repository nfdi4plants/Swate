namespace SpreadsheetInterface

///<summary>This type is used to interface between standalone, electron and excel logic and will forward the command to the correct logic.</summary>
type Msg =
| Initialize
| InitializeResponse of Swatehost
| CreateAnnotationTable of tryUsePrevOutput:bool