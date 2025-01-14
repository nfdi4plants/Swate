import type { Preview } from "@storybook/react";
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
};

export default preview;
