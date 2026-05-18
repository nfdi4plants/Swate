import { Preview } from "@storybook/react-vite";
import { withThemeByDataAttribute } from '@storybook/addon-themes';
import '@uiw/react-markdown-preview/markdown.css';
import '../tailwind.css'
import { options } from "../src/fable_modules/fable-library-ts.5.0.0-alpha.21/RegExp";

const preview: Preview = {
  parameters: {
    options: {
      storySort: {
        order: [
          "Primitive Components",
          "Composite Components",
          "Page Components",
        ]
      },
    },
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
  decorators: [
    withThemeByDataAttribute ({
      themes: {
        light: 'sunrise',
        dark: 'finster'
      },
      defaultTheme: 'light',
      attributeName: 'data-theme'
    })
  ]
};

export default preview;
