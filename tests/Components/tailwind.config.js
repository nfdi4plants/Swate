console.log('tailwindcss config loaded');

/** @type {import('tailwindcss').Config} */
module.exports = {
    mode: "jit",
    content: [
        './tests/components/**/*.{js,ts,jsx,tsx}', // this must be relative to root :disappointed:
    ],
    daisyui: {
        themes: [
            {
                light: {
                    ...require("daisyui/src/theming/themes")["light"],
                    primary: "#1FC2A7",
                    secondary: "#2D3E50",
                    accent: "#B4CE82",
                },
                dark: {
                    ...require("daisyui/src/theming/themes")["dark"],
                    primary: "#1FC2A7",
                    secondary: "#2D3E50",
                    accent: "#B4CE82",
                }
            },
        ],
    },
    theme: {
        container: {
            center: true,
            padding: {
                DEFAULT: '1rem',
                sm: '2rem',
                lg: '4rem',
                xl: '5rem',
                '2xl': '6rem',
            }
        },
        extend: {
            containers: {
                sm: "640px",
                md: "768px",
                lg: "1024px",
                xl: "1280px",
                "2xl": "1536px",
            },
        },
    },
    plugins: [
        require('@tailwindcss/container-queries'),
        require('@tailwindcss/typography'),
        require('daisyui'),
    ],
    darkMode: ['selector', '[data-theme="dark"]']
}
