import {
  Children,
  cloneElement,
  forwardRef,
  isValidElement,
  useEffect,
  useRef,
  useState
} from "react";
import {
  useFloating,
  autoUpdate,
  flip,
  offset,
  shift,
  useRole,
  useDismiss,
  useInteractions,
  useListNavigation,
  useTypeahead,
  FloatingPortal,
  FloatingFocusManager,
  FloatingOverlay
} from "@floating-ui/react";
import React from "react";

interface Props {
  label?: string;
  nested?: boolean;
}

export const Menu = forwardRef<
  HTMLButtonElement,
  Props & React.HTMLProps<HTMLButtonElement>
>(({ }, forwardedRef) => {
  const [activeIndex, setActiveIndex] = useState<number | null>(null);
  const [isOpen, setIsOpen] = useState(false);

  const childArr = Array.from({length: 5}, (_, i) => i + 1).map((i) => {
    return {
      label: `Item ${i}`,
      onClick: () => console.log(`Item ${i} clicked`),
      disabled: i === 3
    };
  });

  const listItemsRef = useRef<Array<HTMLButtonElement | null>>([]);
  const listContentRef = useRef(
    childArr.map(child =>
      child.label
    ) as Array<string | null>
  );
  const allowMouseUpCloseRef = useRef(false);

  const { refs, floatingStyles, context } = useFloating({
    open: isOpen,
    onOpenChange: setIsOpen,
    middleware: [
      offset({ mainAxis: 5, alignmentAxis: 4 }),
      flip({
        fallbackPlacements: ["left-start"]
      }),
      shift({ padding: 10 })
    ],
    placement: "right-start",
    strategy: "fixed",
    whileElementsMounted: autoUpdate
  });

  const role = useRole(context, { role: "menu" });
  const dismiss = useDismiss(context);
  const listNavigation = useListNavigation(context, {
    listRef: listItemsRef,
    onNavigate: setActiveIndex,
    activeIndex
  });
  const typeahead = useTypeahead(context, {
    enabled: isOpen,
    listRef: listContentRef,
    onMatch: setActiveIndex,
    activeIndex
  });

  const { getFloatingProps, getItemProps } = useInteractions([
    role,
    dismiss,
    listNavigation,
    // typeahead
  ]);

  useEffect(() => {
    let timeout: number;

    function onContextMenu(e: MouseEvent) {
      e.preventDefault();

      refs.setPositionReference({
        getBoundingClientRect() {
          return {
            width: 0,
            height: 0,
            x: e.clientX,
            y: e.clientY,
            top: e.clientY,
            right: e.clientX,
            bottom: e.clientY,
            left: e.clientX
          };
        }
      });

      setIsOpen(true);
      clearTimeout(timeout);

      allowMouseUpCloseRef.current = false;
      timeout = window.setTimeout(() => {
        allowMouseUpCloseRef.current = true;
      }, 300);
    }

    function onMouseUp() {
      if (allowMouseUpCloseRef.current) {
        setIsOpen(false);
      }
    }

    document.addEventListener("contextmenu", onContextMenu);
    document.addEventListener("mouseup", onMouseUp);
    return () => {
      document.removeEventListener("contextmenu", onContextMenu);
      document.removeEventListener("mouseup", onMouseUp);
      clearTimeout(timeout);
    };
  }, [refs]);

  return (
    <FloatingPortal>
      {isOpen && (
        <FloatingOverlay lockScroll>
          <FloatingFocusManager context={context} >
            <div
              className="bg-base-200 w-56 rounded-sm h-80"
              // ref={refs.setFloating}
              // style={floatingStyles}
              // {...getFloatingProps()}
            >
              {/* {childArr.map((child, index) => {
                // const props = getItemProps({
                //   tabIndex: activeIndex === index ? 0 : -1,
                //   ref(node: HTMLButtonElement) {
                //     listItemsRef.current[index] = node;
                //   },
                //   onClick() {
                //     child.onClick?.();
                //     setIsOpen(false);
                //   },
                //   onMouseUp() {
                //     child.onClick?.();
                //     setIsOpen(false);
                //   }
                // })
                return (
                  <button key={index} className="text-base-content px-2 py-1 hover:bg-base-300 w-full text-left">
                    {child.label}
                  </button>
                )
              })} */}
            </div>
          </FloatingFocusManager>
        </FloatingOverlay>
      )}
    </FloatingPortal>
  );
});
