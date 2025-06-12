import {ArcTable, OntologyAnnotation, CompositeHeader, CompositeCell, IOType} from "@nfdi4plants/arctrl";

let oa_species = new OntologyAnnotation("species", "NCIT", "NCIT:C45293")
let oa_chlamy = new OntologyAnnotation("Chlamydomonas reinhardtii", "NCBITaxon", "NCBITaxon_3055")

let oa_temperature = new OntologyAnnotation("Temperature", "NCIT", "NCIT:C25206")
let oa_celcius = new OntologyAnnotation("degree celsius", "UO", "UO:0000027")

let oa_instrumentModel = new OntologyAnnotation("instrument model", "MS", "MS:1000031")
let oa_sciex = new OntologyAnnotation("SCIEX instrument model", "MS", "MS:1000121")

const sourceCells = [];
const sampleCells: any[] = [];
const instrumentCells: any[] = [];
const freeTextCells: any[] = [];
const temperatureCells: any[] = [];
const organismCells: any[] = [];


for (let i = 0; i <= 100; i++) {
    sourceCells.push(CompositeCell.createFreeText(`Source ${i}`));
    sampleCells.push(CompositeCell.createFreeText(`Sample ${i}`));
    instrumentCells.push(CompositeCell.createTerm(oa_sciex));
    freeTextCells.push(CompositeCell.createFreeText(`Free text ${i}`));
    temperatureCells.push(CompositeCell.createUnitized(`${Math.floor(i / 10)}`, oa_celcius));
    organismCells.push(CompositeCell.createTerm(oa_chlamy));
}

const LargeTable = ArcTable.init("Example Table")

LargeTable.AddColumn(CompositeHeader.input(IOType.source()), sourceCells);
LargeTable.AddColumn(CompositeHeader.output(IOType.sample()), sampleCells);
LargeTable.AddColumn(CompositeHeader.component(oa_instrumentModel), instrumentCells);
LargeTable.AddColumn(CompositeHeader.freeText("Banana Column!"), freeTextCells);
LargeTable.AddColumn(CompositeHeader.factor(oa_temperature), temperatureCells);
LargeTable.AddColumn(CompositeHeader.characteristic(oa_species), organismCells);

export default LargeTable;