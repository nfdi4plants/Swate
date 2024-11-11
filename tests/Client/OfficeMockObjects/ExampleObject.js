import { OfficeMockObject } from "office-addin-mock";

//Problem still exists, cannot parse collections: https://github.com/OfficeDev/Office-Addin-Scripts/issues/729

const headerRange = {
  values: [
    [
      "header 0",
      "header 1",
      "header 2",
    ]
  ],
  load: function () {
    return this
  },
}

const bodyDataRange = {
  values: [
    [
      "body 0",
      "body 1",
      "body 2",
    ]
  ],
  load: function () {
    return this
  },
}

function createTable(name, position, worksheet) {
  return {
    name: name,
    position: position,
    worksheet: worksheet,
    columns: {
      items: [],
      rowCount: 0,
      values: [],
      load: function () {
        return this.columns
      }
    },
    range: {
      address: "C2:G3",
      format: {
        autofitColumns: function(){
          return
        },
        autofitRows: function(){
          return
        }
      }
    },
    getHeaderRowRange: function () {
      return headerRange
    },
    getDataBodyRange: function () {
      return bodyDataRange
    },
    load: function () {
      return this
    },
    getRange: function () {
      return this.range;
    },
  }
}

function createTableCollection(tables, position) {
  return {
    position: position,
    items: tables,
    load: function () {
      return this
    },
    name: "tableCollection 1",
  }
}

function createWorksheet(name, position, tableCollection) {
  return {
    name: name,
    position: position,
    tables: tableCollection,
    load: function () {
      return this
    },
  }
}

const tableWorksheet = createWorksheet("worksheet 1", 1, [])

const table1 = createTable("table 1", 0, tableWorksheet)
const annotationTable1 = createTable("annotationTable 1", 1, tableWorksheet)

const tableCollection = createTableCollection([table1, annotationTable1], 0)

const workSheet = createWorksheet("worksheet 1", 1, tableCollection)

const MockData = {
  workbook: {
    name: "workbook",
    position: 0,
    range: {
      address: "C2:G3",
    },
    getSelectedRange: function () {
      return this.range;
    },
    tables: tableCollection,
    worksheets: {
      position: 0,
      items: [ workSheet ],
      activeWorkSheet: workSheet,
      getActiveWorksheet: function () {
        return workSheet
      },
    }
  },
};

export default new OfficeMockObject(MockData);
