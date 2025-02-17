import { Preview } from "@storybook/react";
import { withThemeByDataAttribute } from '@storybook/addon-themes';
import '../tailwind.css'
import '@fortawesome/fontawesome-free/css/all.min.css'

const preview: Preview = {
  parameters: {
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
        light: 'light',
        dark: 'dark'
      },
      defaultTheme: 'light',
      attributeName: 'data-theme'
    })
  ]
};

export default preview;
