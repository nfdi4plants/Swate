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

const tableValues = [
  ["header 0", "header 1", "header 2",], 
  ["body 0", "body 1", "body 2",]
]

//function createRange(address, values) {
//  return {
//    address: address || "A1",
//    values: values,
//    format: {
//      autofitColumns: function () {
//        return
//      },
//      autofitRows: function () {
//        return
//      },
//    },
//    load: function (property) {
//      if (Array.isArray(property)) {
//        property.forEach((prop) => {
//          this._loadedProperties[prop] = true;
//       });
//      } else {
//        this._loadedProperties[property] = true;
//      }
//    },
//  }
//}

function createRange(address, values) {
  return {
    address: address,
    values: values,
    format: {
      autofitColumns: function () {
        return
      },
      autofitRows: function () {
        return
      },
    },
  }
}

function createRangeWithLoad(address, values) {
  return {
    address: address,
    values: values,
    format: {
      autofitColumns: function () {
        return
      },
      autofitRows: function () {
        return
      },
    },
    load: function () {
      return this
    }
  }
}

function createTable(name, position, worksheet, range) {
  return {
    name: name,
    position: position,
    worksheet: worksheet,
    style: "Some Style",
    columns: {
      items: [],
      rowCount: 0,
      values: [],
      load: function () {
        return this.columns
      }
    },
    range: range,
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
      return this.range
    },
  }
}

function createTableCollection(tables, position, addTable) {
  return {
    name: "tableCollection 1",
    position: position,
    items: tables,
    load: function () {
      return this
    },
    add: function (range, forceReplace) {
      return addTable
    },
  }
}

function createWorksheet(name, position, tableCollection, range) {
  return {
    name: name,
    position: position,
    tables: tableCollection,
    load: function () {
      return this
    },
    delete: function () {
      return
    },
    range: range,
    getRangeByIndexes: function (startRow, startColumn, rowCount, columnCount) {
      return this.range
    },
    activate: function () {
      return
    },
    getUsedRange: function (valuesOnly = false) {
      return this.range
    },
  }
}


const range = createRange("C2:G3", tableValues)
const rangeWithLoad = createRangeWithLoad("C2:G3", tableValues)

const tableWorksheet = createWorksheet("worksheet 1", 1, [], rangeWithLoad)

const table1 = createTable("table 1", 0, tableWorksheet, rangeWithLoad)
const annotationTable1 = createTable("annotationTable 1", 1, tableWorksheet, rangeWithLoad)

const tableCollection = createTableCollection([table1, annotationTable1], 0, annotationTable1)

const workSheet = createWorksheet("worksheet 1", 1, tableCollection, rangeWithLoad)

const MockData = {
  workbook: {
    name: "workbook",
    position: 0,
    range: range,
    getSelectedRange: function () {
      return this.range;
    },
    tables: tableCollection,
    worksheets: {
      name: "worksheetCollection 1",
      position: 0,
      items: [ workSheet ],
      activeWorkSheet: workSheet,
      getActiveWorksheet: function () {
        return workSheet
      },
      getItemOrNullObject: function () {
        return workSheet
      },
      add: function (workSheetName) {
        return workSheet
      },
    }
  },
};

const MockData2 = {
  workbook: {
    name: "workbook",
    position: 0,
    range: range,
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
      getItemOrNullObject: function () {
        return workSheet
      },
      add: function (workSheetName) {
        return workSheet
      },
    }
  },
  sync: function() {
    return Promise.resolve();  // Simulate a successful sync
  },
};

//Mock Excel.Run and use mockdata for context
global.Excel = {
  run: async (callback) => {
    // Pass the mock context into the provided callback
    return callback(MockData2);
  }
};

export default new OfficeMockObject(MockData);
