import React from 'react';
import ReactDOM from 'react-dom';

import { experimental_VGrid as VGrid, VGridHandle } from "virtua";

export default function VirtualizedGrid() {

  const [rowCount, setRowCount] = React.useState(100_000);
  const [columnCount, setColumnCount] = React.useState(100);

  /// [col, row]
  const [scrollIndex, setScrollIndex] = React.useState<[number, number] | undefined>(undefined);

  /// [col, row]
  const [selectedIndex, setSelectedIndex] = React.useState<[number, number]>([-1, -1]);

  const ref = React.useRef<VGridHandle>(null);

  const scrollToIndex = () => {
    if (scrollIndex) {
      console.log("scrolling to", scrollIndex);
      ref.current?.scrollToIndex(scrollIndex[0], scrollIndex[1]);
    }
  }

  const setScrollToRowIndex = (e: React.ChangeEvent) => {
    if (e.target instanceof HTMLInputElement) {
      const index = parseInt(e.target.value)
      const column = scrollIndex ? scrollIndex[0] : 0
      setScrollIndex([column, index])
    }
  }

  const setScrollToColIndex = (e: React.ChangeEvent) => {
    if (e.target instanceof HTMLInputElement) {
      const row = scrollIndex ? scrollIndex[1] : 0
      const index = parseInt(e.target.value)
      setScrollIndex([index, row])
    }
  }

  const handleKeydown = (e: React.KeyboardEvent) => {
    const adjustIndex = (changeCol: 1 | -1 | 0, changeRow: 1 | -1 | 0) => {
      if (!ref.current) return;
      e.preventDefault();
      const nextColIndex = Math.max(selectedIndex[0] + changeCol, -1);
      const nextRowIndex = Math.max(selectedIndex[1] + changeRow, -1);
      const nextIndex: [number, number] = [nextColIndex, nextRowIndex];
      setSelectedIndex(nextIndex);
      ref.current.scrollToIndex(...nextIndex);
    }

    if (!ref.current) return;
    switch (e.key) {
      case "ArrowUp":
        adjustIndex(0, -1);
        break;
      case "ArrowDown":
        adjustIndex(0, 1);
        break;
      case "ArrowLeft":
        adjustIndex(-1, 0);
        break;
      case "ArrowRight":
        adjustIndex(1, 0);
        break;
      default:
        break;
    }
  }


  return (
    <div className='flex flex-col gap-2'>
      <div className='flex gap-2 flex-row'>
        <label className='form-control w-full max-w-xs'>
          <div className='label'>
            <span className='label-text'>Row Count</span>
          </div>
          <input className='input input-bordered input-warning' type="number" value={rowCount} onChange={(e) => setRowCount(parseInt(e.target.value))} />
        </label>

        <label className='form-control w-full max-w-xs'>
          <div className='label'>
            <span className='label-text'>Column Count</span>
          </div>
          <input className='input input-bordered input-warning' type="number" value={columnCount} onChange={(e) => setColumnCount(parseInt(e.target.value))} />
        </label>
      </div>
      <div className='join'>
        <input className='join-item input input-bordered shrink min-w-0' placeholder='row' onChange={setScrollToRowIndex} type='number'></input>
        <input className='join-item input input-bordered shrink min-w-0' placeholder='column' onChange={setScrollToColIndex} type='number'></input>
        <button className='btn btn-outline join-item' onClick={scrollToIndex}>Go!</button>
      </div>
      <div>
        Number of rows: {rowCount} - Number of columns: {columnCount}
      </div>
      <VGrid
        ref={ref}
        style={{
          height: "600px"
        }}
        row={rowCount}
        col={columnCount}
        tabIndex={0}
        onKeyDown={handleKeydown}
      >
        {({ rowIndex, colIndex }) =>
          <div
            style={{
                border: "solid 1px gray",
                background: selectedIndex[0] === colIndex && selectedIndex[1] === rowIndex ? "dodgerblue" : "white",
                padding: 4,
                height: (rowIndex % 2 + 1) * 100
            }}
            onClick={() => setSelectedIndex([colIndex, rowIndex])}
          >
            <div>
              {rowIndex} / {colIndex}
            </div>
          </div>
        }
      </VGrid>
    </div>
  );
};