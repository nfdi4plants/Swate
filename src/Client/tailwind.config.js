/** @type {import('tailwindcss').Config} */
module.exports = {
    mode: "jit",
    content: [
        "./index.html",
        "./**/*.{fs,js,ts,jsx,tsx}",
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
        extend: {},
    },
    plugins: [
        require('@tailwindcss/container-queries'),
        require('@tailwindcss/typography'),
        require('daisyui'),
    ],
    darkMode: ['selector', '[data-theme="dark"]']
}
