import { OfficeMockObject } from "office-addin-mock";

const MockData = {
    workbook: {
      range: {
        address: "C2:G3",
      },
      getSelectedRange: function () {
        return this.range;
      },
    },
  };

export default new OfficeMockObject(MockData);
