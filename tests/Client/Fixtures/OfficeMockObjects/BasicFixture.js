import { OfficeMockObject } from "office-addin-mock";

function createRange(address) {
  return {
    address: address,
    }
}

const range = createRange("C2:G3")

const MockData = {
  workbook: {
    name: "workbook",
    position: 0,
    range: range,
    getSelectedRange: function () {
      return this.range;
    },
  },
};

export default new OfficeMockObject(MockData);
